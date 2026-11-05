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
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Impostazioni › Servizi › Prezzi automatici.</summary>
public sealed class AutomaticPricesService
{
	private readonly IServiceCatalogRepository _repository;
	private readonly ISectorRepository _sectors;
	private readonly IMediaAssetRepository _assets;
	private readonly IPropertyFileStorage _files;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly CatalogAudit _audit;
	private readonly CatalogMedia _media;
	private readonly VatPolicy _vat;
	private readonly SaveAutomaticPriceRequestValidator _validator;
	private readonly BulkIdsRequestValidator _bulkValidator;

	public AutomaticPricesService(
		IServiceCatalogRepository repository, ISectorRepository sectors, IMediaAssetRepository assets, IPropertyFileStorage files,
		IUnitOfWork unitOfWork, CurrentProperty property, CatalogAudit audit, CatalogMedia media, VatPolicy vat,
		SaveAutomaticPriceRequestValidator validator, BulkIdsRequestValidator bulkValidator)
	{
		_bulkValidator = bulkValidator;
		_repository = repository;
		_sectors = sectors;
		_assets = assets;
		_files = files;
		_unitOfWork = unitOfWork;
		_property = property;
		_audit = audit;
		_media = media;
		_vat = vat;
		_validator = validator;
	}

	public async Task<IReadOnlyList<AutomaticPriceDto>> ListAsync(CancellationToken ct = default)
	{
		var prices = await _repository.ListAutomaticAsync(await _property.IdAsync(ct), ct);
		var result = new List<AutomaticPriceDto>();
		foreach (var price in prices)
			result.Add(await ToDtoAsync(price, ct));
		return result;
	}

	public async Task<AutomaticPriceDto> GetAsync(Guid id, CancellationToken ct = default) => await ToDtoAsync(await LoadAsync(id, ct), ct);

	public async Task<AutomaticPriceDto> CreateAsync(SaveAutomaticPriceRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var price = AutomaticPrice.Create(await _property.IdAsync(ct));
		await ApplyAsync(price, request, ct);
		_repository.AddAutomatic(price);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.AutomaticPriceCreated, price, ct);
		return await ToDtoAsync(price, ct);
	}

	public async Task<AutomaticPriceDto> UpdateAsync(Guid id, SaveAutomaticPriceRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var price = await LoadAsync(id, ct);
		await ApplyAsync(price, request, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.AutomaticPriceUpdated, price, ct);
		return await ToDtoAsync(price, ct);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		var price = await LoadAsync(id, ct);
		_repository.RemoveAutomatic(price);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.AutomaticPriceDeleted, price, ct);
	}

	public async Task<BulkResult> DeleteManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var propertyId = await _property.IdAsync(ct);
		var found = new List<AutomaticPrice>();
		foreach (var id in ids)
			if (await _repository.FindAutomaticAsync(propertyId, id, ct) is { } price)
				found.Add(price);
		foreach (var price in found)
			_repository.RemoveAutomatic(price);
		await _unitOfWork.SaveChangesAsync(ct);
		foreach (var price in found)
			await RecordAsync(AuditActionCodes.Catalog.AutomaticPriceDeleted, price, ct);
		return Bulk(ids, found.Select(p => p.PublicId).ToHashSet());
	}

	/// <summary>Copia con il primo nome libero fra « copia», « copia 2»…; stessa icona o immagine.</summary>
	public async Task<AutomaticPriceDto> DuplicateAsync(Guid id, CancellationToken ct = default)
	{
		var source = await LoadAsync(id, ct);
		var copy = await CopyAsync(source, await TakenNamesAsync(source.PropertyId, ct), ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.AutomaticPriceDuplicated, copy, ct);
		return await ToDtoAsync(copy, ct);
	}

	public async Task<BulkResult> DuplicateManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var propertyId = await _property.IdAsync(ct);
		var taken = await TakenNamesAsync(propertyId, ct);
		var copied = new HashSet<Guid>();
		var copies = new List<AutomaticPrice>();
		foreach (var id in ids)
			if (await _repository.FindAutomaticAsync(propertyId, id, ct) is { } source)
			{
				copies.Add(await CopyAsync(source, taken, ct));
				copied.Add(id);
			}
		await _unitOfWork.SaveChangesAsync(ct);
		foreach (var copy in copies)
			await RecordAsync(AuditActionCodes.Catalog.AutomaticPriceDuplicated, copy, ct);
		return Bulk(ids, copied);
	}

	private async Task<AutomaticPrice> CopyAsync(AutomaticPrice source, HashSet<string> taken, CancellationToken ct)
	{
		var dto = await ToDtoAsync(source, ct);
		var copy = AutomaticPrice.Create(source.PropertyId);
		await ApplyAsync(copy, ToRequest(dto) with { Name = CopyNames.Next(source.Name, AutomaticPrice.NameMaxLength, taken) }, ct);
		if (source.IconId is null && source.IconAssetId is { } asset)
			copy.Update(Data(copy, null, asset));
		_repository.AddAutomatic(copy);
		return copy;
	}

	private async Task<HashSet<string>> TakenNamesAsync(long propertyId, CancellationToken ct)
		=> CopyNames.Taken((await _repository.ListAutomaticAsync(propertyId, ct)).Select(p => p.Name));

	private static SaveAutomaticPriceRequest ToRequest(AutomaticPriceDto d) => new(
		d.Name, d.Description, d.Price, d.VatRate, d.VatNature, d.IconId, d.IsPurchasable, d.AppliesPerService, d.AppliesToAllServices,
		d.ServiceIds, d.Periods, d.IsSectorRestricted, d.SectorIds, d.Translations);

	private static BulkResult Bulk(IReadOnlyList<Guid> ids, IReadOnlySet<Guid> done)
		=> new(ids.Where(done.Contains).ToList(), ids.Where(i => !done.Contains(i)).Select(i => new BulkFailure(i, CatalogErrors.AutomaticPriceNotFound, null)).ToList());

	public async Task<AutomaticPriceDto> UploadImageAsync(Guid id, UploadedFile file, CancellationToken ct = default)
	{
		UploadRules.EnsureImage(file.ContentType, file.Length);
		var price = await LoadAsync(id, ct);
		var stored = await _files.SaveAsync(file.FileName, file.Content, file.ContentType, ct);
		var asset = MediaAsset.Upload(stored.Ref, MediaKind.Photo, stored.Sha256, stored.ByteSize, stored.ContentType, price.PropertyId);
		_assets.Add(asset);
		await _unitOfWork.SaveChangesAsync(ct);
		price.Update(Data(price, null, asset.Id));
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.AutomaticPriceUpdated, price, ct);
		return await ToDtoAsync(price, ct);
	}

	private async Task ApplyAsync(AutomaticPrice price, SaveAutomaticPriceRequest r, CancellationToken ct)
	{
		var (rate, nature) = await _vat.ResolveAsync(price.PropertyId, r.VatRate, r.VatNature, ct);
		int? iconId = null;
		if (r.IconId is { } iconPublicId)
			iconId = await _repository.FindIconIdAsync(iconPublicId, ct) ?? throw DomainException.NotFound(CatalogErrors.ServiceIconNotFound);

		var serviceIds = new List<long>();
		if (!r.AppliesToAllServices)
		{
			var requested = (r.ServiceIds ?? []).Distinct().ToList();
			var found = await _repository.FindIdsAsync(price.PropertyId, requested, ct);
			if (found.Count != requested.Count)
				throw DomainException.NotFound(CatalogErrors.ServiceNotFound);
			serviceIds = found.Values.ToList();
		}

		price.Update(new AutomaticPriceData(
			r.Name, ContentMapping.Clean(r.Description), r.Price, rate, nature, iconId, iconId is null ? price.IconAssetId : null, r.IsPurchasable,
			r.AppliesPerService, r.AppliesToAllServices, r.IsSectorRestricted));
		price.SetTranslations(ContentMapping.Translations(r.Translations));
		price.SetServices(serviceIds);
		price.SetSectors(r.IsSectorRestricted ? await ContentMapping.SectorIdsAsync(_sectors, price.PropertyId, r.SectorIds, ct) : []);
		price.ReplacePeriods((r.Periods ?? []).OrderBy(p => p.StartsOn).Select(p => new AutomaticPricePeriodData(
			p.StartsOn, p.EndsOn, ContentMapping.Weekdays(p.Weekdays), (p.TimeRanges ?? []).Select(x => (x.From, x.To)).ToList())));
	}

	private static AutomaticPriceData Data(AutomaticPrice p, int? iconId, long? assetId) => new(
		p.Name, p.Description, p.Price, p.VatRate, p.VatNature, iconId, assetId, p.IsPurchasable, p.AppliesPerService, p.AppliesToAllServices,
		p.IsSectorRestricted);

	private async Task<AutomaticPrice> LoadAsync(Guid id, CancellationToken ct)
		=> await _repository.FindAutomaticAsync(await _property.IdAsync(ct), id, ct) ?? throw DomainException.NotFound(CatalogErrors.AutomaticPriceNotFound);

	private async Task<AutomaticPriceDto> ToDtoAsync(AutomaticPrice p, CancellationToken ct)
	{
		var icons = p.IconId is { } iconId ? await _repository.FindIconsAsync([iconId], ct) : new Dictionary<int, (Guid, long)>();
		var urls = await _media.UrlsAsync([p.IconAssetId, .. icons.Values.Select(v => (long?)v.AssetId)], ct);
		var services = await _repository.FindPublicIdsAsync(p.Services.Select(s => s.SaleItemId).ToList(), ct);
		var sectors = await ContentMapping.SectorPublicIdsAsync(_sectors, p.PropertyId, ct);
		var icon = p.IconId is { } id && icons.TryGetValue(id, out var v) ? v : default((Guid PublicId, long AssetId)?);
		return new AutomaticPriceDto(
			p.PublicId, p.Name, p.Description, p.Price, p.VatRate, p.VatNature is { } n ? Codes.Of(n) : null, icon?.PublicId,
			p.IconAssetId is { } a ? urls.UrlOf(a) : icon is { } i ? urls.UrlOf(i.AssetId) : null, p.IsPurchasable, p.AppliesPerService,
			p.AppliesToAllServices, p.Services.Select(s => services.GetValueOrDefault(s.SaleItemId)).Where(g => g != Guid.Empty).ToList(),
			p.Periods.OrderBy(x => x.StartsOn).Select(x => new AutomaticPricePeriodDto(x.StartsOn, x.EndsOn, ContentMapping.CodesOf(x.Weekdays),
				x.TimeRanges.OrderBy(r => r.From).Select(r => new DateRangeTimeDto(r.From, r.To)).ToList())).ToList(), p.IsSectorRestricted,
			p.Sectors.Select(x => sectors.GetValueOrDefault(x.SectorId)).Where(g => g != Guid.Empty).ToList(),
			p.Translations.OrderBy(t => t.Language).Select(t => ContentMapping.Dto(t.Language, t.Name, t.Description)).ToList());
	}

	private Task RecordAsync(string actionCode, AutomaticPrice price, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.AutomaticPrice, price.PublicId,
			new Dictionary<string, object?> { ["name"] = price.Name, ["price"] = price.Price }, ct);
}
