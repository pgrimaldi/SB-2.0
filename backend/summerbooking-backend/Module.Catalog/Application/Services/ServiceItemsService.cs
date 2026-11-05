using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Security;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Impostazioni › Servizi: servizi bar, normali e orari.</summary>
public sealed class ServiceItemsService
{
	private readonly IServiceCatalogRepository _repository;
	private readonly ISectorRepository _sectors;
	private readonly ISeasonRepository _seasons;
	private readonly IMediaAssetRepository _assets;
	private readonly IPropertyFileStorage _files;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly CatalogAudit _audit;
	private readonly CatalogMedia _media;
	private readonly VatPolicy _vat;
	private readonly ServiceListQueryValidator _listValidator;
	private readonly SaveServiceRequestValidator _validator;
	private readonly BulkIdsRequestValidator _bulkValidator;

	public ServiceItemsService(
		IServiceCatalogRepository repository, ISectorRepository sectors, ISeasonRepository seasons, IMediaAssetRepository assets,
		IPropertyFileStorage files, IUnitOfWork unitOfWork, CurrentProperty property, ICurrentUser user, CatalogAudit audit, CatalogMedia media,
		VatPolicy vat, ServiceListQueryValidator listValidator, SaveServiceRequestValidator validator, BulkIdsRequestValidator bulkValidator)
	{
		_repository = repository;
		_sectors = sectors;
		_seasons = seasons;
		_assets = assets;
		_files = files;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_audit = audit;
		_media = media;
		_vat = vat;
		_listValidator = listValidator;
		_validator = validator;
		_bulkValidator = bulkValidator;
	}

	public async Task<PagedResult<ServiceListItemDto>> ListAsync(ServiceListQuery query, CancellationToken ct = default)
	{
		_listValidator.EnsureValid(query);
		ServiceSaleKind? kind = query.Kind is { } k && Codes.TryParse<ServiceSaleKind>(k, out var parsed) ? parsed : null;
		var (items, total) = await _repository.ListAsync(
			new ServiceFilter(await _property.IdAsync(ct), kind, string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(), query.SortBy,
				query.SortDir == SortDirections.Desc),
			query.Page, query.PageSize, ct);
		var images = await ImagesAsync(items, ct);
		return new PagedResult<ServiceListItemDto>(total, query.Page, query.PageSize, items
			.Select(s => new ServiceListItemDto(s.PublicId, Codes.Of(s.Kind), s.Name, s.Price, s.Category is { } c ? Codes.Of(c) : null,
				s.IsPurchasable, s.IsHighlighted, images.GetValueOrDefault(s.Id)))
			.ToList());
	}

	public async Task<IReadOnlyList<ServiceLookupDto>> LookupAsync(CancellationToken ct = default)
		=> (await _repository.ListAllAsync(await _property.IdAsync(ct), ct)).Select(s => new ServiceLookupDto(s.PublicId, Codes.Of(s.Kind), s.Name)).ToList();

	public async Task<ServiceDto> GetAsync(Guid id, CancellationToken ct = default)
		=> await ToDtoAsync(await LoadAsync(id, ct), ct);

	public async Task<ServiceDto> CreateAsync(SaveServiceRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		Codes.TryParse<ServiceSaleKind>(request.Kind, out var kind);
		var item = ServiceSaleItem.Create(await _property.IdAsync(ct), kind, _user.UserId);
		await ApplyAsync(item, request, ct);
		_repository.Add(item);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.ServiceCreated, item, ct);
		return await ToDtoAsync(item, ct);
	}

	public async Task<ServiceDto> UpdateAsync(Guid id, SaveServiceRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var item = await LoadAsync(id, ct);
		if (request.Kind != Codes.Of(item.Kind))
			throw DomainException.Unprocessable(CatalogErrors.ServiceKindImmutable);
		await ApplyAsync(item, request, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.ServiceUpdated, item, ct);
		return await ToDtoAsync(item, ct);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		var item = await LoadAsync(id, ct);
		await RemoveAsync(item, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.ServiceDeleted, item, ct);
	}

	public async Task<BulkResult> DeleteManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var found = await _repository.FindManyAsync(await _property.IdAsync(ct), ids, ct);
		foreach (var item in found)
			await RemoveAsync(item, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		foreach (var item in found)
			await RecordAsync(AuditActionCodes.Catalog.ServiceDeleted, item, ct);
		var deleted = found.Select(i => i.PublicId).ToHashSet();
		return new BulkResult(deleted.ToList(), ids.Where(i => !deleted.Contains(i)).Select(i => new BulkFailure(i, CatalogErrors.ServiceNotFound, null)).ToList());
	}

	/// <summary>Copia con il primo nome libero fra « copia», « copia 2»… fra i servizi dello stesso tipo; stessa icona o immagine.</summary>
	public async Task<ServiceDto> DuplicateAsync(Guid id, CancellationToken ct = default)
	{
		var source = await LoadAsync(id, ct);
		var copy = await CopyAsync(source, await TakenNamesAsync(source.PropertyId, ct), ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.ServiceDuplicated, copy, ct);
		return await ToDtoAsync(copy, ct);
	}

	public async Task<BulkResult> DuplicateManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var propertyId = await _property.IdAsync(ct);
		var taken = await TakenNamesAsync(propertyId, ct);
		var copied = new HashSet<Guid>();
		var copies = new List<ServiceSaleItem>();
		foreach (var id in ids)
			if (await _repository.FindAsync(propertyId, id, ct) is { } source)
			{
				copies.Add(await CopyAsync(source, taken, ct));
				copied.Add(id);
			}
		await _unitOfWork.SaveChangesAsync(ct);
		foreach (var copy in copies)
			await RecordAsync(AuditActionCodes.Catalog.ServiceDuplicated, copy, ct);
		return new BulkResult(ids.Where(copied.Contains).ToList(),
			ids.Where(i => !copied.Contains(i)).Select(i => new BulkFailure(i, CatalogErrors.ServiceNotFound, null)).ToList());
	}

	private async Task<ServiceSaleItem> CopyAsync(ServiceSaleItem source, Dictionary<ServiceSaleKind, HashSet<string>> taken, CancellationToken ct)
	{
		if (!taken.TryGetValue(source.Kind, out var names))
			taken[source.Kind] = names = CopyNames.Taken([]);
		var dto = await ToDtoAsync(source, ct);
		var copy = ServiceSaleItem.Create(source.PropertyId, source.Kind, _user.UserId);
		await ApplyAsync(copy, ToRequest(dto) with { Name = CopyNames.Next(source.Name, ServiceSaleItem.NameMaxLength, names) }, ct);
		if (source.IconId is null && source.IconAssetId is { } asset)
			copy.Update(Data(copy, null, asset));
		_repository.Add(copy);
		return copy;
	}

	/// <summary>Nomi già usati dai servizi dello stabilimento, per tipo.</summary>
	private async Task<Dictionary<ServiceSaleKind, HashSet<string>>> TakenNamesAsync(long propertyId, CancellationToken ct)
		=> (await _repository.ListAllAsync(propertyId, ct)).GroupBy(s => s.Kind).ToDictionary(g => g.Key, g => CopyNames.Taken(g.Select(s => s.Name)));

	private static SaveServiceRequest ToRequest(ServiceDto d) => new(
		d.Kind, d.Name, d.Description, d.Category, d.Price, d.VatRate, d.VatNature, d.IconId, d.IsPurchasable, d.IsHighlighted,
		d.HasPeriodPrices, d.PeriodPrices, d.IsSectorRestricted, d.SectorIds, d.Allergens, d.Variants, d.Translations, d.BookingOptions,
		d.HourlyPrices);

	public async Task<ServiceDto> UploadImageAsync(Guid id, UploadedFile file, CancellationToken ct = default)
	{
		UploadRules.EnsureImage(file.ContentType, file.Length);
		var item = await LoadAsync(id, ct);
		var stored = await _files.SaveAsync(file.FileName, file.Content, file.ContentType, ct);
		var asset = MediaAsset.Upload(stored.Ref, MediaKind.Photo, stored.Sha256, stored.ByteSize, stored.ContentType, item.PropertyId);
		_assets.Add(asset);
		await _unitOfWork.SaveChangesAsync(ct);
		item.Update(Data(item, iconId: null, iconAssetId: asset.Id));
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.ServiceUpdated, item, ct);
		return await ToDtoAsync(item, ct);
	}

	public async Task<ServiceDto> RemoveImageAsync(Guid id, CancellationToken ct = default)
	{
		var item = await LoadAsync(id, ct);
		item.Update(Data(item, item.IconId, iconAssetId: null));
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.ServiceUpdated, item, ct);
		return await ToDtoAsync(item, ct);
	}

	/// <summary>Tolto dai prezzi automatici che lo citavano; le prenotazioni restano a Booking.</summary>
	private async Task RemoveAsync(ServiceSaleItem item, CancellationToken ct)
	{
		foreach (var price in await _repository.ListAutomaticForServiceAsync(item.Id, ct))
			price.SetServices(price.Services.Where(s => s.SaleItemId != item.Id).Select(s => s.SaleItemId).ToList());
		_repository.Remove(item);
	}

	private async Task ApplyAsync(ServiceSaleItem item, SaveServiceRequest r, CancellationToken ct)
	{
		var propertyId = item.PropertyId;
		// servizio orario con l'IVA solo nelle disponibilità: quella del servizio è la prima
		var options = item.Kind == ServiceSaleKind.Hourly ? ContentRules.HourlyOptions(r).ToList() : [];
		var vatRate = r.VatRate is null && r.VatNature is null ? options.Select(o => o.VatRate).FirstOrDefault(v => v is not null) : r.VatRate;
		var (rate, nature) = await _vat.ResolveAsync(propertyId, vatRate, r.VatNature, ct);
		foreach (var optionRate in options.Select(o => o.VatRate).OfType<decimal>().Distinct())
			await _vat.ResolveAsync(propertyId, optionRate, null, ct);
		int? iconId = null;
		if (r.IconId is { } iconPublicId)
			iconId = await _repository.FindIconIdAsync(iconPublicId, ct) ?? throw DomainException.NotFound(CatalogErrors.ServiceIconNotFound);

		var periodPrices = (r.PeriodPrices ?? []).ToDictionary(p => p.PeriodKey, p => p.Price);
		var hourlyKeys = (r.HourlyPrices ?? []).Where(p => p.PeriodKey is not null).Select(p => p.PeriodKey!.Value).ToList();
		if (periodPrices.Count > 0 || hourlyKeys.Count > 0)
		{
			var season = await _seasons.FindAsync(propertyId, ct);
			var keys = season?.TariffPeriods.Select(p => p.PeriodGroupKey).ToHashSet() ?? [];
			if (periodPrices.Keys.Concat(hourlyKeys).Any(k => !keys.Contains(k)))
				throw DomainException.Unprocessable(CatalogErrors.PeriodNotFound);
		}

		item.Update(new ServiceSaleItemData(
			r.Name, ContentMapping.Clean(r.Description), r.Category is { } c && Codes.TryParse<BarCategory>(c, out var category) ? category : null,
			r.Price, rate, nature, iconId, iconId is null ? item.IconAssetId : null, r.IsPurchasable, r.IsHighlighted, r.HasPeriodPrices,
			r.IsSectorRestricted, (r.Allergens ?? []).Select(a => Codes.TryParse<Allergen>(a, out var v) ? v : default).ToList()));
		item.ReplaceVariants((r.Variants ?? []).Select(v => (v.Name, v.Price)).ToList());
		item.SetTranslations(ContentMapping.Translations(r.Translations));
		var bookingOptions = (r.BookingOptions ?? []).Select(o => new ServiceBookingOptionData(
			o.StartsOn, o.EndsOn, ContentMapping.Weekdays(o.Weekdays), o.ManagementFrom, o.ManagementTo, o.OnlineFrom, o.OnlineTo,
			o.MaxPerHour, o.MaxPerDay, o.MaxPerSlot, o.IntervalMinutes, o.MinItems, o.MaxItems, o.Hourly is { } h ? Hourly(h) : null)).ToList();
		item.ReplaceBookingOptions(bookingOptions);
		item.SetPeriodPrices(periodPrices);
		item.SetSectors(r.IsSectorRestricted ? await ContentMapping.SectorIdsAsync(_sectors, propertyId, r.SectorIds, ct) : []);
		item.ReplaceHourly((r.HourlyPrices ?? []).SelectMany(p => (p.Options ?? []).Select(o => (p.PeriodKey, Hourly(o)))).ToList(), bookingOptions);
	}

	private static HourlyOptionData Hourly(HourlyOptionDto o) => new(
		o.TimeFrom, o.TimeTo, ContentMapping.Weekdays(o.Weekdays),
		(o.Slots ?? []).OrderBy(s => s.DurationMinutes).Select(s => new HourlySlotData(s.DurationMinutes, s.IsEnabled, s.Price)).ToList(),
		o.CustomSlot is { } c ? new HourlySlotData(c.DurationMinutes, c.IsEnabled, c.Price) : null, o.VatRate, o.MaxPerSlot, o.HasFixedTimeSlots);

	private static HourlyOptionDto HourlyDto(ServiceHourlyOption o)
	{
		var slots = o.Slots.Where(s => s.DeletedAt is null).OrderBy(s => s.SortOrder).ToList();
		static HourlySlotDto Slot(ServiceSaleItemTimeSlot s) => new(s.DurationMinutes ?? 0, s.IsActive, s.Price, s.PublicId);
		return new HourlyOptionDto(o.TimeFrom, o.TimeTo, ContentMapping.CodesOf(o.Weekdays), slots.Where(s => !s.IsCustom).Select(Slot).ToList(),
			slots.FirstOrDefault(s => s.IsCustom) is { } custom ? Slot(custom) : null, o.VatRate, o.MaxPerSlot, o.HasFixedTimeSlots);
	}

	private static ServiceSaleItemData Data(ServiceSaleItem s, int? iconId, long? iconAssetId) => new(
		s.Name, s.Description, s.Category, s.Price, s.VatRate, s.VatNature, iconId, iconAssetId, s.IsPurchasable, s.IsHighlighted,
		s.HasPeriodPrices, s.IsSectorRestricted, s.Allergens);

	private async Task<ServiceSaleItem> LoadAsync(Guid id, CancellationToken ct)
		=> await _repository.FindAsync(await _property.IdAsync(ct), id, ct) ?? throw DomainException.NotFound(CatalogErrors.ServiceNotFound);

	/// <summary>Immagine caricata oppure icona del catalogo.</summary>
	private async Task<IReadOnlyDictionary<long, string?>> ImagesAsync(IReadOnlyList<ServiceSaleItem> items, CancellationToken ct)
	{
		var icons = await _repository.FindIconsAsync(items.Where(i => i.IconId is not null).Select(i => i.IconId!.Value).Distinct().ToList(), ct);
		var urls = await _media.UrlsAsync(items.Select(i => i.IconAssetId).Concat(icons.Values.Select(v => (long?)v.AssetId)), ct);
		return items.ToDictionary(i => i.Id, i => i.IconAssetId is { } a ? urls.UrlOf(a)
			: i.IconId is { } icon && icons.TryGetValue(icon, out var v) ? urls.UrlOf(v.AssetId) : null);
	}

	private async Task<ServiceDto> ToDtoAsync(ServiceSaleItem s, CancellationToken ct)
	{
		var images = await ImagesAsync([s], ct);
		var icons = s.IconId is { } iconId ? await _repository.FindIconsAsync([iconId], ct) : new Dictionary<int, (Guid, long)>();
		var sectors = await ContentMapping.SectorPublicIdsAsync(_sectors, s.PropertyId, ct);
		return new ServiceDto(
			s.PublicId, Codes.Of(s.Kind), s.Name, s.Description, s.Category is { } c ? Codes.Of(c) : null, s.Price, s.VatRate,
			s.VatNature is { } n ? Codes.Of(n) : null, s.IconId is { } i && icons.TryGetValue(i, out var icon) ? icon.PublicId : null,
			images.GetValueOrDefault(s.Id), s.IsPurchasable, s.IsHighlighted, s.HasPeriodPrices,
			s.PeriodPrices.OrderBy(p => p.TariffPeriodGroup).Select(p => new PeriodPriceDto(p.TariffPeriodGroup, p.Price)).ToList(),
			s.IsSectorRestricted, s.Sectors.Select(x => sectors.GetValueOrDefault(x.SectorId)).Where(g => g != Guid.Empty).ToList(),
			ContentMapping.CodesOf(s.Allergens), s.Variants.OrderBy(v => v.SortOrder).Select(v => new VariantDto(v.Name, v.Price)).ToList(),
			s.Translations.OrderBy(t => t.Language).Select(t => ContentMapping.Dto(t.Language, t.Name, t.Description)).ToList(),
			s.BookingOptions.OrderBy(o => o.SortOrder).Select(o => new BookingOptionDto(
				o.StartsOn, o.EndsOn, ContentMapping.CodesOf(o.Weekdays), o.ManagementFrom, o.ManagementTo, o.OnlineFrom, o.OnlineTo, o.MaxPerHour,
				o.MaxPerDay, o.MaxPerSlot, o.IntervalMinutes, o.MinItems, o.MaxItems,
				o.HourlyOption is { DeletedAt: null } h ? HourlyDto(h) : null)).ToList(),
			s.HourlyOptions.Where(h => h.BookingOption is null && h.BookingOptionId is null).GroupBy(h => h.PeriodGroupKey).OrderBy(g => g.Key)
				.Select(g => new HourlyPeriodDto(g.Key, g.OrderBy(h => h.SortOrder).Select(HourlyDto).ToList())).ToList());
	}

	private Task RecordAsync(string actionCode, ServiceSaleItem item, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.Service, item.PublicId,
			new Dictionary<string, object?> { ["name"] = item.Name, ["kind"] = Codes.Of(item.Kind) }, ct);
}
