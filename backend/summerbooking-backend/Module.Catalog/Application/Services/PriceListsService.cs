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
using Shared.Localization;
using Shared.Security;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>Impostazioni › Listino: pacchetti e articoli aggiuntivi dello stabilimento del token.</summary>
public sealed class PriceListsService
{
	private readonly IPriceListRepository _repository;
	private readonly IServiceCatalogRepository _services;
	private readonly ISectorRepository _sectors;
	private readonly ISeasonRepository _seasons;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly CatalogAudit _audit;
	private readonly VatPolicy _vat;
	private readonly PriceListQueryValidator _queryValidator;
	private readonly SavePriceListRequestValidator _validator;
	private readonly BulkIdsRequestValidator _bulkValidator;

	public PriceListsService(
		IPriceListRepository repository, IServiceCatalogRepository services, ISectorRepository sectors, ISeasonRepository seasons,
		IUnitOfWork unitOfWork, CurrentProperty property, ICurrentUser user, CatalogAudit audit, VatPolicy vat,
		PriceListQueryValidator queryValidator, SavePriceListRequestValidator validator, BulkIdsRequestValidator bulkValidator)
	{
		_repository = repository;
		_services = services;
		_sectors = sectors;
		_seasons = seasons;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_audit = audit;
		_vat = vat;
		_queryValidator = queryValidator;
		_validator = validator;
		_bulkValidator = bulkValidator;
	}

	public async Task<PagedResult<PriceListListItemDto>> ListAsync(PriceListQuery query, CancellationToken ct = default)
	{
		_queryValidator.EnsureValid(query);
		PriceListKind? kind = query.Kind is { } k && Codes.TryParse<PriceListKind>(k, out var parsed) ? parsed : null;
		var (items, total) = await _repository.ListAsync(new PriceListFilter(await _property.IdAsync(ct), kind,
			string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(), query.SortDir == SortDirections.Desc), query.Page, query.PageSize, ct);
		return new PagedResult<PriceListListItemDto>(total, query.Page, query.PageSize, items.Select(p => new PriceListListItemDto(
			p.PublicId, Codes.Of(p.Kind), p.Name, Codes.Of(p.SalesChannel), p.IsDefault, p.IsDefaultOnline, p.IsCumulative, p.CalculationMethod?.Code)).ToList());
	}

	public async Task<IReadOnlyList<PriceListLookupDto>> LookupAsync(string? kind, CancellationToken ct = default)
	{
		PriceListKind? filter = null;
		if (!string.IsNullOrEmpty(kind))
			filter = Codes.TryParse<PriceListKind>(kind, out var parsed) ? parsed : throw DomainException.BadRequest(ValidationErrorCodes.Invalid);
		return (await _repository.ListAllAsync(await _property.IdAsync(ct), filter, ct)).Select(p => new PriceListLookupDto(p.PublicId, Codes.Of(p.Kind), p.Name)).ToList();
	}

	public async Task<PriceListDto> GetAsync(Guid id, CancellationToken ct = default) => await ToDtoAsync(await LoadAsync(id, ct), ct);

	public async Task<PriceListDto> CreateAsync(SavePriceListRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		Codes.TryParse<PriceListKind>(request.Kind, out var kind);
		var priceList = PriceList.Create(await _property.IdAsync(ct), kind, _user.UserId);
		await ApplyAsync(priceList, request, ct);
		_repository.Add(priceList);
		await _unitOfWork.SaveChangesAsync(ct);
		await ResetOtherDefaultsAsync(priceList, ct);
		await RecordAsync(AuditActionCodes.Catalog.PriceListCreated, priceList, ct);
		return await ToDtoAsync(priceList, ct);
	}

	public async Task<PriceListDto> UpdateAsync(Guid id, SavePriceListRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var priceList = await LoadAsync(id, ct);
		if (request.Kind != Codes.Of(priceList.Kind))
			throw DomainException.Unprocessable(CatalogErrors.PriceListKindImmutable);
		await ApplyAsync(priceList, request, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await ResetOtherDefaultsAsync(priceList, ct);
		await RecordAsync(AuditActionCodes.Catalog.PriceListUpdated, priceList, ct);
		return await ToDtoAsync(priceList, ct);
	}

	public async Task DeleteAsync(Guid id, CancellationToken ct = default)
	{
		var priceList = await LoadAsync(id, ct);
		await RemoveAsync(priceList, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PriceListDeleted, priceList, ct);
	}

	public async Task<BulkResult> DeleteManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var found = await _repository.FindManyAsync(await _property.IdAsync(ct), ids, ct);
		foreach (var priceList in found)
			await RemoveAsync(priceList, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		foreach (var priceList in found)
			await RecordAsync(AuditActionCodes.Catalog.PriceListDeleted, priceList, ct);
		return Bulk(ids, found.Select(p => p.PublicId).ToHashSet());
	}

	/// <summary>Copia con il primo nome libero fra « copia», « copia 2»…, mai predefinita.</summary>
	public async Task<PriceListDto> DuplicateAsync(Guid id, CancellationToken ct = default)
	{
		var source = await LoadAsync(id, ct);
		var copy = await CopyAsync(source, await TakenNamesAsync(source.PropertyId, ct), ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordAsync(AuditActionCodes.Catalog.PriceListDuplicated, copy, ct);
		return await ToDtoAsync(copy, ct);
	}

	public async Task<BulkResult> DuplicateManyAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var found = await _repository.FindManyAsync(await _property.IdAsync(ct), ids, ct);
		var copies = new List<PriceList>();
		// i nomi delle copie appena fatte contano già, anche se non ancora salvate
		var taken = await TakenNamesAsync(await _property.IdAsync(ct), ct);
		foreach (var priceList in found)
			copies.Add(await CopyAsync(priceList, taken, ct));
		await _unitOfWork.SaveChangesAsync(ct);
		foreach (var copy in copies)
			await RecordAsync(AuditActionCodes.Catalog.PriceListDuplicated, copy, ct);
		return Bulk(ids, found.Select(p => p.PublicId).ToHashSet());
	}

	private async Task<PriceList> CopyAsync(PriceList source, Dictionary<PriceListKind, HashSet<string>> taken, CancellationToken ct)
	{
		if (!taken.TryGetValue(source.Kind, out var names))
			taken[source.Kind] = names = CopyNames.Taken([]);
		var dto = await ToDtoAsync(source, ct);
		var copy = PriceList.Create(source.PropertyId, source.Kind, _user.UserId);
		await ApplyAsync(copy, ToRequest(dto) with
		{
			Name = CopyNames.Next(source.Name, PriceList.NameMaxLength, names),
			IsDefault = false,
			IsDefaultOnline = false,
		}, ct);
		_repository.Add(copy);
		return copy;
	}

	/// <summary>Nomi già usati dai listini dello stabilimento, per tipo.</summary>
	private async Task<Dictionary<PriceListKind, HashSet<string>>> TakenNamesAsync(long propertyId, CancellationToken ct)
		=> (await _repository.ListAllAsync(propertyId, null, ct)).GroupBy(p => p.Kind).ToDictionary(g => g.Key, g => CopyNames.Taken(g.Select(p => p.Name)));

	internal static SavePriceListRequest ToRequest(PriceListDto d) => new(
		d.Kind, d.Name, d.Translations, d.MaxPeople, d.MaxAdditionalItems, d.Items, d.IsAreaPackage, d.IncludedServiceIds, d.PackageLinks,
		d.IsSectorRestricted, d.SectorIds, d.SalesChannel, d.IsDefault, d.IsDefaultOnline, d.IsMapAssignable, d.CalculationMethod, d.MonthlySetting,
		d.WeekendSetting, d.AllowsPublicHolidays, d.HolidayDates, d.IsCumulative, d.VatRate, d.VatNature, d.SectorPrices, d.PeriodPrices, d.ReducedPrices);

	/// <summary>I legami degli aggiuntivi con il pacchetto eliminato si tolgono.</summary>
	private async Task RemoveAsync(PriceList priceList, CancellationToken ct)
	{
		if (priceList.Kind == PriceListKind.Package)
			foreach (var additional in await _repository.ListLinkedAdditionalsAsync(priceList.Id, ct))
				additional.ReplaceItems(additional.Items
					.Where(i => !(i.ItemKind == PackageItemKind.LinkedPackage && i.LinkedPriceListId == priceList.Id))
					.Select(i => new PriceListItemData(i.ItemKind, i.InventoryItemId, i.IncludedSaleItemId, i.LinkedPriceListId, i.Quantity))
					.ToList());
		_repository.Remove(priceList);
	}

	/// <summary>Un solo predefinito (e un solo predefinito online) per tipo di listino.</summary>
	private async Task ResetOtherDefaultsAsync(PriceList priceList, CancellationToken ct)
	{
		if (!priceList.IsDefault && !priceList.IsDefaultOnline)
			return;
		foreach (var other in await _repository.ListDefaultsAsync(priceList.PropertyId, priceList.Kind, priceList.Id, ct))
			other.SetDefaults(other.IsDefault && !priceList.IsDefault, other.IsDefaultOnline && !priceList.IsDefaultOnline);
		await _unitOfWork.SaveChangesAsync(ct);
	}

	internal async Task ApplyAsync(PriceList priceList, SavePriceListRequest r, CancellationToken ct)
	{
		var propertyId = priceList.PropertyId;
		var (rate, nature) = await _vat.ResolveAsync(propertyId, r.VatRate, r.VatNature, ct);

		short? methodId = null;
		if (r.CalculationMethod is { } methodCode)
			methodId = (await _repository.ListCalculationMethodsAsync(ct)).FirstOrDefault(m => m.Code == methodCode)?.Id
				?? throw DomainException.Unprocessable(CatalogErrors.CalculationMethodNotFound);

		var sectors = (await _sectors.ListAsync(propertyId, ct)).ToDictionary(s => s.PublicId, s => s.Id);
		long SectorId(Guid id) => sectors.TryGetValue(id, out var sectorId) ? sectorId : throw DomainException.NotFound(CatalogErrors.SectorNotFound);

		var season = await _seasons.FindAsync(propertyId, ct);
		var periodKeys = season?.TariffPeriods.Select(p => p.PeriodGroupKey).ToHashSet() ?? [];
		if ((r.PeriodPrices ?? []).Any(p => p.PeriodKey is { } key && !periodKeys.Contains(key)))
			throw DomainException.Unprocessable(CatalogErrors.PeriodNotFound);

		var items = new List<PriceListItemData>();
		var inventory = await _repository.FindInventoryIdsAsync(propertyId, (r.Items ?? []).Select(i => i.InventoryItemId).ToList(), ct);
		var itemKind = r.IsAreaPackage ? PackageItemKind.AreaItem : PackageItemKind.InventoryItem;
		foreach (var item in r.Items ?? [])
			items.Add(new PriceListItemData(itemKind,
				inventory.TryGetValue(item.InventoryItemId, out var inventoryId) ? inventoryId : throw DomainException.NotFound(CatalogErrors.InventoryItemNotFound),
				null, null, item.Quantity));
		if (priceList.Kind == PriceListKind.Package)
		{
			var included = await _services.FindIdsAsync(propertyId, (r.IncludedServiceIds ?? []).Distinct().ToList(), ct);
			if (included.Count != (r.IncludedServiceIds ?? []).Distinct().Count())
				throw DomainException.NotFound(CatalogErrors.ServiceNotFound);
			items.AddRange(included.Values.Select(id => new PriceListItemData(PackageItemKind.IncludedService, null, id, null, 1)));
		}
		else
		{
			var links = r.PackageLinks ?? [];
			var packages = await _repository.FindIdsAsync(propertyId, links.Select(l => l.PackageId).ToList(), PriceListKind.Package, ct);
			if (packages.Count != links.Count)
				throw DomainException.NotFound(CatalogErrors.PriceListNotFound);
			items.AddRange(links.Select(l => new PriceListItemData(PackageItemKind.LinkedPackage, null, null, packages[l.PackageId], l.MaxCount)));
		}

		var reducedSeasons = (await _repository.ListReducedSeasonsAsync(propertyId, ct)).ToDictionary(s => s.PublicId, s => s.Id);

		priceList.Update(new PriceListData(
			r.Name, r.MaxPeople, r.MaxAdditionalItems, methodId,
			r.MonthlySetting is { } m && Codes.TryParse<MonthlySetting>(m, out var monthly) ? monthly : null,
			r.WeekendSetting is { } w && Codes.TryParse<WeekendSetting>(w, out var weekend) ? weekend : null,
			Codes.TryParse<SalesChannel>(r.SalesChannel, out var channel) ? channel : SalesChannel.ManagerOnly,
			r.IsAreaPackage, r.IsCumulative, r.AllowsPublicHolidays, rate, nature, r.IsSectorRestricted, r.IsMapAssignable));
		priceList.SetDefaults(r.IsDefault, r.IsDefaultOnline);
		priceList.SetTranslations((r.Translations ?? []).ToDictionary(t => SupportedLanguages.ParseOrDefault(t.Language), t => t.Name.Trim()));
		priceList.ReplaceItems(items);
		priceList.SetSectorPrices((r.SectorPrices ?? []).ToDictionary(p => SectorId(p.SectorId), p => (p.SeasonalPrice, p.SeasonalHalfDayPrice)));
		priceList.SetPeriodPrices((r.PeriodPrices ?? []).Select(p => new PeriodPriceData(
			p.SectorId is { } s ? SectorId(s) : null, p.PeriodKey, p.HalfDay, p.HalfDayWeekend, p.Daily, p.Weekend, p.HalfDay7, p.Days7, p.HalfDay14,
			p.Days14, p.HalfDayMonthly, p.Monthly, (p.StayOverrides ?? []).ToDictionary(o => o.StayDays, o => o.Price))).ToList());
		priceList.SetReducedPrices((r.ReducedPrices ?? []).ToDictionary(
			p => (reducedSeasons.TryGetValue(p.ReducedSeasonId, out var seasonId) ? seasonId : throw DomainException.NotFound(CatalogErrors.ReducedSeasonNotFound),
				SectorId(p.SectorId)),
			p => p.Price));
		priceList.SetHolidays(r.AllowsPublicHolidays ? (r.HolidayDates ?? []).Distinct().ToList() : []);
	}

	private async Task<PriceList> LoadAsync(Guid id, CancellationToken ct)
		=> await _repository.FindAsync(await _property.IdAsync(ct), id, ct) ?? throw DomainException.NotFound(CatalogErrors.PriceListNotFound);

	internal async Task<PriceListDto> ToDtoAsync(PriceList p, CancellationToken ct)
	{
		var sectors = await ContentMapping.SectorPublicIdsAsync(_sectors, p.PropertyId, ct);
		Guid Sector(long id) => sectors.GetValueOrDefault(id);
		var inventory = await _repository.FindInventoryPublicIdsAsync(p.Items.Where(i => i.InventoryItemId is not null).Select(i => i.InventoryItemId!.Value).ToList(), ct);
		var services = await _services.FindPublicIdsAsync(p.Items.Where(i => i.IncludedSaleItemId is not null).Select(i => i.IncludedSaleItemId!.Value).ToList(), ct);
		var packages = await _repository.FindPublicIdsAsync(p.Items.Where(i => i.LinkedPriceListId is not null).Select(i => i.LinkedPriceListId!.Value).ToList(), ct);
		var reduced = (await _repository.ListReducedSeasonsAsync(p.PropertyId, ct)).ToDictionary(s => s.Id, s => s.PublicId);
		var method = p.CalculationMethod?.Code ?? (p.CalculationMethodId is { } mid
			? (await _repository.ListCalculationMethodsAsync(ct)).FirstOrDefault(m => m.Id == mid)?.Code
			: null);

		return new PriceListDto(
			p.PublicId, Codes.Of(p.Kind), p.Name,
			p.Translations.OrderBy(t => t.Language).Select(t => new NameTranslationDto(SupportedLanguages.Code(t.Language), t.Name)).ToList(),
			p.MaxPeople, p.MaxAdditionalItems,
			p.Items.Where(i => i.InventoryItemId is not null && inventory.ContainsKey(i.InventoryItemId.Value))
				.Select(i => new PriceListItemDto(inventory[i.InventoryItemId!.Value], i.Quantity)).ToList(),
			p.IsAreaPackage,
			p.Items.Where(i => i.IncludedSaleItemId is { } s && services.ContainsKey(s)).Select(i => services[i.IncludedSaleItemId!.Value]).ToList(),
			p.Items.Where(i => i.LinkedPriceListId is { } l && packages.ContainsKey(l)).Select(i => new PackageLinkDto(packages[i.LinkedPriceListId!.Value], i.Quantity)).ToList(),
			p.IsSectorRestricted, p.IsSectorRestricted ? p.SectorPrices.Select(s => Sector(s.SectorId)).Where(g => g != Guid.Empty).ToList() : [],
			Codes.Of(p.SalesChannel), p.IsDefault, p.IsDefaultOnline, p.IsMapAssignable, method,
			p.MonthlySetting is { } monthly ? Codes.Of(monthly) : null, p.WeekendSetting is { } weekend ? Codes.Of(weekend) : null,
			p.AllowsPublicHolidays, p.DatePrices.Where(d => d.Kind == DatePriceKind.Holiday).Select(d => d.PriceDate).Order().ToList(),
			p.IsCumulative, p.VatRate, p.VatNature is { } n ? Codes.Of(n) : null,
			p.SectorPrices.Where(s => sectors.ContainsKey(s.SectorId)).Select(s => new SectorPriceDto(Sector(s.SectorId), s.BasePrice, s.HalfDayPrice)).ToList(),
			p.PeriodPrices.OrderBy(x => x.SectorId).ThenBy(x => x.TariffPeriodGroup).Select(x => new PeriodPricesDto(
				x.SectorId is { } s ? Sector(s) : null, x.TariffPeriodGroup, x.HalfDayPrice, x.HalfDayWeekendPrice, x.DailyPrice, x.WeekendPrice,
				x.HalfDay7Price, x.Days7Price, x.HalfDay14Price, x.Days14Price, x.HalfDayMonthlyPrice, x.MonthlyPrice,
				x.StayPriceOverrides.OrderBy(o => o.StayDays).Select(o => new StayOverrideDto(o.StayDays, o.Price)).ToList())).ToList(),
			p.ReducedPrices.Where(x => reduced.ContainsKey(x.ReducedSeasonId) && sectors.ContainsKey(x.SectorId))
				.Select(x => new ReducedPriceDto(reduced[x.ReducedSeasonId], Sector(x.SectorId), x.Price)).ToList());
	}

	private static BulkResult Bulk(IReadOnlyList<Guid> ids, IReadOnlySet<Guid> done)
		=> new(ids.Where(done.Contains).ToList(), ids.Where(i => !done.Contains(i)).Select(i => new BulkFailure(i, CatalogErrors.PriceListNotFound, null)).ToList());

	private Task RecordAsync(string actionCode, PriceList priceList, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.PriceList, priceList.PublicId,
			new Dictionary<string, object?> { ["name"] = priceList.Name, ["kind"] = Codes.Of(priceList.Kind) }, ct);
}
