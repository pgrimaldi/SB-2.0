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

/// <summary>Convenzioni: strutture convenzionate, i loro listini e voucher, i report. Prenotazioni e contenuto dei report sono di Booking.</summary>
public sealed class AgreementsService
{
	private readonly IAgreementRepository _repository;
	private readonly IPriceListRepository _priceLists;
	private readonly IServiceCatalogRepository _services;
	private readonly ISectorRepository _sectors;
	private readonly ISeasonRepository _seasons;
	private readonly IUnitOfWork _unitOfWork;
	private readonly CurrentProperty _property;
	private readonly ICurrentUser _user;
	private readonly CatalogAudit _audit;
	private readonly VatPolicy _vat;
	private readonly AgreementPartnerQueryValidator _partnerQueryValidator;
	private readonly SaveAgreementPartnerRequestValidator _partnerValidator;
	private readonly SaveAgreementPriceListRequestValidator _priceListValidator;
	private readonly AgreementReportQueryValidator _reportQueryValidator;
	private readonly BulkIdsRequestValidator _bulkValidator;

	public AgreementsService(
		IAgreementRepository repository, IPriceListRepository priceLists, IServiceCatalogRepository services, ISectorRepository sectors,
		ISeasonRepository seasons, IUnitOfWork unitOfWork, CurrentProperty property, ICurrentUser user, CatalogAudit audit, VatPolicy vat,
		AgreementPartnerQueryValidator partnerQueryValidator, SaveAgreementPartnerRequestValidator partnerValidator,
		SaveAgreementPriceListRequestValidator priceListValidator, AgreementReportQueryValidator reportQueryValidator, BulkIdsRequestValidator bulkValidator)
	{
		_repository = repository;
		_priceLists = priceLists;
		_services = services;
		_sectors = sectors;
		_seasons = seasons;
		_unitOfWork = unitOfWork;
		_property = property;
		_user = user;
		_audit = audit;
		_vat = vat;
		_partnerQueryValidator = partnerQueryValidator;
		_partnerValidator = partnerValidator;
		_priceListValidator = priceListValidator;
		_reportQueryValidator = reportQueryValidator;
		_bulkValidator = bulkValidator;
	}

	public async Task<PagedResult<AgreementPartnerDto>> ListPartnersAsync(AgreementPartnerQuery query, CancellationToken ct = default)
	{
		_partnerQueryValidator.EnsureValid(query);
		var (items, total) = await _repository.ListPartnersAsync(new AgreementPartnerFilter(await _property.IdAsync(ct),
			string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(), query.SortBy, query.SortDir == SortDirections.Desc), query.Page, query.PageSize, ct);
		var types = await _repository.ListTypesAsync(items.Select(p => p.Id).ToList(), ct);
		return new PagedResult<AgreementPartnerDto>(total, query.Page, query.PageSize, items.Select(p => ToDto(p, types.GetValueOrDefault(p.Id))).ToList());
	}

	public async Task<IReadOnlyList<AgreementPartnerLookupDto>> LookupPartnersAsync(CancellationToken ct = default)
		=> (await _repository.ListAllPartnersAsync(await _property.IdAsync(ct), ct)).Select(p => new AgreementPartnerLookupDto(p.PublicId, p.Name)).ToList();

	public async Task<AgreementPartnerDto> GetPartnerAsync(Guid id, CancellationToken ct = default)
	{
		var partner = await LoadPartnerAsync(id, ct);
		return ToDto(partner, (await _repository.ListTypesAsync([partner.Id], ct)).GetValueOrDefault(partner.Id));
	}

	public async Task<AgreementPartnerDto> CreatePartnerAsync(SaveAgreementPartnerRequest request, CancellationToken ct = default)
	{
		_partnerValidator.EnsureValid(request);
		var partner = AgreementPartner.Create(await _property.IdAsync(ct));
		partner.Update(Data(request));
		_repository.AddPartner(partner);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordPartnerAsync(AuditActionCodes.Catalog.AgreementPartnerCreated, partner, ct);
		return ToDto(partner, null);
	}

	public async Task<AgreementPartnerDto> UpdatePartnerAsync(Guid id, SaveAgreementPartnerRequest request, CancellationToken ct = default)
	{
		_partnerValidator.EnsureValid(request);
		var partner = await LoadPartnerAsync(id, ct);
		partner.Update(Data(request));
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordPartnerAsync(AuditActionCodes.Catalog.AgreementPartnerUpdated, partner, ct);
		return await GetPartnerAsync(id, ct);
	}

	/// <summary>La struttura e i suoi listini; le prenotazioni già fatte restano a Booking.</summary>
	public async Task DeletePartnerAsync(Guid id, CancellationToken ct = default)
	{
		var partner = await LoadPartnerAsync(id, ct);
		await RemovePartnerAsync(partner, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordPartnerAsync(AuditActionCodes.Catalog.AgreementPartnerDeleted, partner, ct);
	}

	public async Task<BulkResult> DeletePartnersAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var found = await _repository.FindPartnersAsync(await _property.IdAsync(ct), ids, ct);
		foreach (var partner in found)
			await RemovePartnerAsync(partner, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		foreach (var partner in found)
			await RecordPartnerAsync(AuditActionCodes.Catalog.AgreementPartnerDeleted, partner, ct);
		return Bulk(ids, found.Select(p => p.PublicId).ToHashSet(), CatalogErrors.AgreementPartnerNotFound);
	}

	/// <summary>Copia con il primo nome libero fra « copia», « copia 2»…: città, CAP, provincia, listini e voucher; non indirizzo, contatti, note e report.</summary>
	public async Task<AgreementPartnerDto> DuplicatePartnerAsync(Guid id, CancellationToken ct = default)
	{
		var source = await LoadPartnerAsync(id, ct);
		var copy = AgreementPartner.Create(source.PropertyId);
		var partnerNames = CopyNames.Taken((await _repository.ListAllPartnersAsync(source.PropertyId, ct)).Select(p => p.Name));
		copy.Update(new AgreementPartnerData(CopyNames.Next(source.Name, AgreementPartner.NameMaxLength, partnerNames), source.City, source.PostalCode, source.Province, null, null, null, null, null));
		_repository.AddPartner(copy);
		await _unitOfWork.SaveChangesAsync(ct);

		var lists = await _repository.ListPriceListsAsync(source.Id, null, null, ct);
		var copies = new Dictionary<long, AgreementPriceList>();
		foreach (var list in lists.OrderBy(l => l.Kind))
		{
			var dto = await ToDtoAsync(list, source.PublicId, ct);
			var clone = AgreementPriceList.Create(copy.Id, list.ListType, list.Kind, _user.UserId);
			await ApplyAsync(clone, copy.PropertyId, ToRequest(dto) with { PackageLinks = [] }, ct);
			_repository.AddPriceList(clone);
			copies[list.Id] = clone;
		}
		await _unitOfWork.SaveChangesAsync(ct);
		// I legami degli aggiuntivi puntano ai pacchetti copiati.
		foreach (var list in lists.Where(l => l.Kind == PriceListKind.Additional))
		{
			var links = list.Items.Where(i => i.ItemKind == PackageItemKind.LinkedPackage && i.LinkedPriceListId is { } l && copies.ContainsKey(l)).ToList();
			if (links.Count == 0)
				continue;
			var clone = copies[list.Id];
			clone.ReplaceItems(clone.Items.Select(i => new PriceListItemData(i.ItemKind, i.InventoryItemId, i.IncludedSaleItemId, i.LinkedPriceListId, i.Quantity))
				.Concat(links.Select(i => new PriceListItemData(PackageItemKind.LinkedPackage, null, null, copies[i.LinkedPriceListId!.Value].Id, i.Quantity)))
				.ToList());
		}
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordPartnerAsync(AuditActionCodes.Catalog.AgreementPartnerDuplicated, copy, ct);
		return await GetPartnerAsync(copy.PublicId, ct);
	}

	public async Task<IReadOnlyList<AgreementPriceListDto>> ListPriceListsAsync(Guid partnerId, string? listType, string? kind, CancellationToken ct = default)
	{
		var partner = await LoadPartnerAsync(partnerId, ct);
		AgreementListType? type = null;
		PriceListKind? listKind = null;
		if (!string.IsNullOrEmpty(listType))
			type = Codes.TryParse<AgreementListType>(listType, out var t) ? t : throw DomainException.BadRequest(ValidationErrorCodes.Invalid);
		if (!string.IsNullOrEmpty(kind))
			listKind = Codes.TryParse<PriceListKind>(kind, out var k) ? k : throw DomainException.BadRequest(ValidationErrorCodes.Invalid);
		var result = new List<AgreementPriceListDto>();
		foreach (var list in await _repository.ListPriceListsAsync(partner.Id, type, listKind, ct))
			result.Add(await ToDtoAsync(list, partner.PublicId, ct));
		return result;
	}

	public async Task<AgreementPriceListDto> GetPriceListAsync(Guid id, CancellationToken ct = default)
	{
		var list = await LoadPriceListAsync(id, ct);
		return await ToDtoAsync(list, list.Partner!.PublicId, ct);
	}

	public async Task<AgreementPriceListDto> CreatePriceListAsync(Guid partnerId, SaveAgreementPriceListRequest request, CancellationToken ct = default)
	{
		_priceListValidator.EnsureValid(request);
		var partner = await LoadPartnerAsync(partnerId, ct);
		Codes.TryParse<AgreementListType>(request.ListType, out var type);
		Codes.TryParse<PriceListKind>(request.Kind, out var kind);
		var list = AgreementPriceList.Create(partner.Id, type, kind, _user.UserId);
		await ApplyAsync(list, partner.PropertyId, request, ct);
		_repository.AddPriceList(list);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordPriceListAsync(AuditActionCodes.Catalog.AgreementPriceListCreated, list, partner, ct);
		return await ToDtoAsync(list, partner.PublicId, ct);
	}

	public async Task<AgreementPriceListDto> UpdatePriceListAsync(Guid id, SaveAgreementPriceListRequest request, CancellationToken ct = default)
	{
		_priceListValidator.EnsureValid(request);
		var list = await LoadPriceListAsync(id, ct);
		if (request.Kind != Codes.Of(list.Kind) || request.ListType != Codes.Of(list.ListType))
			throw DomainException.Unprocessable(CatalogErrors.PriceListKindImmutable);
		await ApplyAsync(list, list.Partner!.PropertyId, request, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordPriceListAsync(AuditActionCodes.Catalog.AgreementPriceListUpdated, list, list.Partner!, ct);
		return await ToDtoAsync(list, list.Partner!.PublicId, ct);
	}

	public async Task DeletePriceListAsync(Guid id, CancellationToken ct = default)
	{
		var list = await LoadPriceListAsync(id, ct);
		await RemovePriceListAsync(list, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordPriceListAsync(AuditActionCodes.Catalog.AgreementPriceListDeleted, list, list.Partner!, ct);
	}

	public async Task<BulkResult> DeletePriceListsAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var found = await _repository.FindPriceListsAsync(await _property.IdAsync(ct), ids, ct);
		foreach (var list in found)
			await RemovePriceListAsync(list, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		return Bulk(ids, found.Select(l => l.PublicId).ToHashSet(), CatalogErrors.PriceListNotFound);
	}

	public async Task<AgreementPriceListDto> DuplicatePriceListAsync(Guid id, CancellationToken ct = default)
	{
		var source = await LoadPriceListAsync(id, ct);
		var copy = await CopyPriceListAsync(source, await TakenListNamesAsync([source.PartnerId], ct), ct);
		await _unitOfWork.SaveChangesAsync(ct);
		await RecordPriceListAsync(AuditActionCodes.Catalog.AgreementPriceListCreated, copy, source.Partner!, ct);
		return await ToDtoAsync(copy, source.Partner!.PublicId, ct);
	}

	public async Task<BulkResult> DuplicatePriceListsAsync(BulkIdsRequest request, CancellationToken ct = default)
	{
		_bulkValidator.EnsureValid(request);
		var ids = request.Ids.Distinct().ToList();
		var found = await _repository.FindPriceListsAsync(await _property.IdAsync(ct), ids, ct);
		// i nomi delle copie appena fatte contano già, anche se non ancora salvate
		var taken = await TakenListNamesAsync(found.Select(l => l.PartnerId).Distinct().ToList(), ct);
		foreach (var list in found)
			await CopyPriceListAsync(list, taken, ct);
		await _unitOfWork.SaveChangesAsync(ct);
		return Bulk(ids, found.Select(l => l.PublicId).ToHashSet(), CatalogErrors.PriceListNotFound);
	}

	public async Task<PagedResult<AgreementReportDto>> ListReportsAsync(AgreementReportQuery query, CancellationToken ct = default)
	{
		_reportQueryValidator.EnsureValid(query);
		var propertyId = await _property.IdAsync(ct);
		long? partnerId = query.PartnerId is { } p ? (await LoadPartnerAsync(p, ct)).Id : null;
		AgreementReportStatus? status = query.Status is { } s && Codes.TryParse<AgreementReportStatus>(s, out var parsed) ? parsed : null;
		var (items, total) = await _repository.ListReportsAsync(propertyId, partnerId, status, query.Page, query.PageSize, ct);
		return new PagedResult<AgreementReportDto>(total, query.Page, query.PageSize, items.Select(ToDto).ToList());
	}

	public async Task<AgreementReportDto> GetReportAsync(Guid id, CancellationToken ct = default) => ToDto(await LoadReportAsync(id, ct));

	public async Task<AgreementReportDto> CancelReportAsync(Guid id, CancellationToken ct = default)
	{
		var report = await LoadReportAsync(id, ct);
		report.Cancel();
		await _unitOfWork.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Catalog.AgreementReportCancelled, AuditActionCodes.Catalog.Targets.AgreementReport, report.PublicId,
			new Dictionary<string, object?> { ["partner"] = report.Partner?.Name, ["periodStart"] = report.PeriodStart, ["periodEnd"] = report.PeriodEnd }, ct);
		return ToDto(report);
	}

	private async Task<AgreementPriceList> CopyPriceListAsync(AgreementPriceList source, Dictionary<long, HashSet<string>> taken, CancellationToken ct)
	{
		if (!taken.TryGetValue(source.PartnerId, out var names))
			taken[source.PartnerId] = names = CopyNames.Taken([]);
		var dto = await ToDtoAsync(source, source.Partner!.PublicId, ct);
		var copy = AgreementPriceList.Create(source.PartnerId, source.ListType, source.Kind, _user.UserId);
		await ApplyAsync(copy, source.Partner!.PropertyId, ToRequest(dto) with { Name = CopyNames.Next(source.Name, PriceList.NameMaxLength, names) }, ct);
		_repository.AddPriceList(copy);
		return copy;
	}

	private async Task RemovePartnerAsync(AgreementPartner partner, CancellationToken ct)
	{
		foreach (var list in await _repository.ListPriceListsAsync(partner.Id, null, null, ct))
			_repository.RemovePriceList(list);
		_repository.RemovePartner(partner);
	}

	private async Task RemovePriceListAsync(AgreementPriceList list, CancellationToken ct)
	{
		if (list.Kind == PriceListKind.Package)
			foreach (var additional in await _repository.ListLinkedAdditionalsAsync(list.Id, ct))
				additional.ReplaceItems(additional.Items
					.Where(i => !(i.ItemKind == PackageItemKind.LinkedPackage && i.LinkedPriceListId == list.Id))
					.Select(i => new PriceListItemData(i.ItemKind, i.InventoryItemId, i.IncludedSaleItemId, i.LinkedPriceListId, i.Quantity)).ToList());
		_repository.RemovePriceList(list);
	}

	private async Task ApplyAsync(AgreementPriceList list, long propertyId, SaveAgreementPriceListRequest r, CancellationToken ct)
	{
		var (rate, nature) = await _vat.ResolveAsync(propertyId, r.VatRate, r.VatNature, ct);
		short? methodId = null;
		if (r.CalculationMethod is { } code)
			methodId = (await _priceLists.ListCalculationMethodsAsync(ct)).FirstOrDefault(m => m.Code == code)?.Id
				?? throw DomainException.Unprocessable(CatalogErrors.CalculationMethodNotFound);

		var sectors = (await _sectors.ListAsync(propertyId, ct)).ToDictionary(s => s.PublicId, s => s.Id);
		long SectorId(Guid id) => sectors.TryGetValue(id, out var sectorId) ? sectorId : throw DomainException.NotFound(CatalogErrors.SectorNotFound);
		var season = await _seasons.FindAsync(propertyId, ct);
		var periodKeys = season?.TariffPeriods.Select(p => p.PeriodGroupKey).ToHashSet() ?? [];
		if ((r.PeriodPrices ?? []).Any(p => p.PeriodKey is { } key && !periodKeys.Contains(key)))
			throw DomainException.Unprocessable(CatalogErrors.PeriodNotFound);

		var items = new List<PriceListItemData>();
		var inventory = await _priceLists.FindInventoryIdsAsync(propertyId, (r.Items ?? []).Select(i => i.InventoryItemId).ToList(), ct);
		foreach (var item in r.Items ?? [])
			items.Add(new PriceListItemData(PackageItemKind.InventoryItem,
				inventory.TryGetValue(item.InventoryItemId, out var inventoryId) ? inventoryId : throw DomainException.NotFound(CatalogErrors.InventoryItemNotFound),
				null, null, item.Quantity));
		if (list.Kind == PriceListKind.Package)
		{
			var requested = (r.IncludedServiceIds ?? []).Distinct().ToList();
			var included = await _services.FindIdsAsync(propertyId, requested, ct);
			if (included.Count != requested.Count)
				throw DomainException.NotFound(CatalogErrors.ServiceNotFound);
			items.AddRange(included.Values.Select(id => new PriceListItemData(PackageItemKind.IncludedService, null, id, null, 1)));
		}
		else
		{
			var links = r.PackageLinks ?? [];
			var packages = await _repository.FindPackageIdsAsync(list.PartnerId, list.ListType, links.Select(l => l.PackageId).ToList(), ct);
			if (packages.Count != links.Count)
				throw DomainException.NotFound(CatalogErrors.PriceListNotFound);
			items.AddRange(links.Select(l => new PriceListItemData(PackageItemKind.LinkedPackage, null, null, packages[l.PackageId], l.MaxCount)));
		}

		list.Update(new AgreementPriceListData(
			r.Name, r.MaxPeople, methodId,
			r.MonthlySetting is { } m && Codes.TryParse<MonthlySetting>(m, out var monthly) ? monthly : null,
			r.WeekendSetting is { } w && Codes.TryParse<WeekendSetting>(w, out var weekend) ? weekend : null,
			r.AllowsPublicHolidays, r.MinBookingDays, r.MaxVoucherQuantity, r.IsCumulative, rate, nature));
		list.ReplaceItems(items);
		list.SetSectorPrices((r.SectorPrices ?? []).ToDictionary(p => SectorId(p.SectorId), p => (p.SeasonalPrice, p.SeasonalHalfDayPrice)));
		list.SetPeriodPrices((r.PeriodPrices ?? []).Select(p => new PeriodPriceData(
			p.SectorId is { } s ? SectorId(s) : null, p.PeriodKey, p.HalfDay, p.HalfDayWeekend, p.Daily, p.Weekend, p.HalfDay7, p.Days7, p.HalfDay14,
			p.Days14, p.HalfDayMonthly, p.Monthly, (p.StayOverrides ?? []).ToDictionary(o => o.StayDays, o => o.Price))).ToList());
		list.SetHolidays(r.AllowsPublicHolidays ? (r.HolidayDates ?? []).Distinct().ToList() : []);
	}

	private static SaveAgreementPriceListRequest ToRequest(AgreementPriceListDto d) => new(
		d.ListType, d.Kind, d.Name, d.MaxPeople, d.Items, d.IncludedServiceIds, d.PackageLinks, d.CalculationMethod, d.MonthlySetting, d.WeekendSetting,
		d.AllowsPublicHolidays, d.HolidayDates, d.MinBookingDays, d.MaxVoucherQuantity, d.IsCumulative, d.VatRate, d.VatNature, d.SectorPrices, d.PeriodPrices);

	private async Task<AgreementPriceListDto> ToDtoAsync(AgreementPriceList l, Guid partnerPublicId, CancellationToken ct)
	{
		var propertyId = l.Partner?.PropertyId ?? await _property.IdAsync(ct);
		var sectors = await ContentMapping.SectorPublicIdsAsync(_sectors, propertyId, ct);
		Guid Sector(long id) => sectors.GetValueOrDefault(id);
		var inventory = await _priceLists.FindInventoryPublicIdsAsync(l.Items.Where(i => i.InventoryItemId is not null).Select(i => i.InventoryItemId!.Value).ToList(), ct);
		var services = await _services.FindPublicIdsAsync(l.Items.Where(i => i.IncludedSaleItemId is not null).Select(i => i.IncludedSaleItemId!.Value).ToList(), ct);
		var packages = await _repository.FindPriceListPublicIdsAsync(l.Items.Where(i => i.LinkedPriceListId is not null).Select(i => i.LinkedPriceListId!.Value).ToList(), ct);
		var method = l.CalculationMethodId is { } mid ? (await _priceLists.ListCalculationMethodsAsync(ct)).FirstOrDefault(m => m.Id == mid)?.Code : null;
		return new AgreementPriceListDto(
			l.PublicId, partnerPublicId, Codes.Of(l.ListType), Codes.Of(l.Kind), l.Name, l.MaxPeople,
			l.Items.Where(i => i.InventoryItemId is { } inv && inventory.ContainsKey(inv)).Select(i => new PriceListItemDto(inventory[i.InventoryItemId!.Value], i.Quantity)).ToList(),
			l.Items.Where(i => i.IncludedSaleItemId is { } s && services.ContainsKey(s)).Select(i => services[i.IncludedSaleItemId!.Value]).ToList(),
			l.Items.Where(i => i.LinkedPriceListId is { } p && packages.ContainsKey(p)).Select(i => new PackageLinkDto(packages[i.LinkedPriceListId!.Value], i.Quantity)).ToList(),
			method, l.MonthlySetting is { } monthly ? Codes.Of(monthly) : null, l.WeekendSetting is { } weekend ? Codes.Of(weekend) : null,
			l.AllowsPublicHolidays, l.DatePrices.Where(d => d.Kind == DatePriceKind.Holiday).Select(d => d.PriceDate).Order().ToList(),
			l.MinBookingDays, l.MaxVoucherQuantity, l.IsCumulative, l.VatRate, l.VatNature is { } n ? Codes.Of(n) : null,
			l.SectorPrices.Where(s => sectors.ContainsKey(s.SectorId)).Select(s => new SectorPriceDto(Sector(s.SectorId), s.BasePrice, s.HalfDayPrice)).ToList(),
			l.PeriodPrices.OrderBy(x => x.SectorId).ThenBy(x => x.TariffPeriodGroup).Select(x => new PeriodPricesDto(
				x.SectorId is { } s ? Sector(s) : null, x.TariffPeriodGroup, x.HalfDayPrice, x.HalfDayWeekendPrice, x.DailyPrice, x.WeekendPrice, x.HalfDay7Price,
				x.Days7Price, x.HalfDay14Price, x.Days14Price, x.HalfDayMonthlyPrice, x.MonthlyPrice,
				x.StayPriceOverrides.OrderBy(o => o.StayDays).Select(o => new StayOverrideDto(o.StayDays, o.Price)).ToList())).ToList());
	}

	private static AgreementPartnerData Data(SaveAgreementPartnerRequest r) => new(
		r.Name, ContentMapping.Clean(r.City), ContentMapping.Clean(r.PostalCode), ContentMapping.Clean(r.Province), ContentMapping.Clean(r.StreetName),
		ContentMapping.Clean(r.StreetNumber), ContentMapping.Clean(r.Phone), ContentMapping.Clean(r.Email), ContentMapping.Clean(r.Note));

	private static AgreementPartnerDto ToDto(AgreementPartner p, IReadOnlySet<AgreementListType>? types) => new(
		p.PublicId, p.Name, p.City, p.PostalCode, p.Province, p.StreetName, p.StreetNumber, p.Phone, p.Email, p.Note,
		types?.Contains(AgreementListType.Regular) == true, types?.Contains(AgreementListType.Voucher) == true);

	private static AgreementReportDto ToDto(AgreementReport r) => new(
		r.PublicId, r.Partner?.PublicId ?? Guid.Empty, r.Partner?.Name ?? string.Empty, Codes.Of(r.SearchType), r.PeriodStart, r.PeriodEnd,
		Codes.Of(r.Status), r.TotalReservations, r.TotalRevenue, r.Notes, r.CreatedAt);

	/// <summary>Nomi già usati dai listini e voucher di ogni struttura.</summary>
	private async Task<Dictionary<long, HashSet<string>>> TakenListNamesAsync(IReadOnlyCollection<long> partnerIds, CancellationToken ct)
		=> (await _repository.ListPriceListNamesAsync(partnerIds, ct)).ToDictionary(e => e.Key, e => CopyNames.Taken(e.Value));

	private async Task<AgreementPartner> LoadPartnerAsync(Guid id, CancellationToken ct)
		=> await _repository.FindPartnerAsync(await _property.IdAsync(ct), id, ct) ?? throw DomainException.NotFound(CatalogErrors.AgreementPartnerNotFound);

	private async Task<AgreementPriceList> LoadPriceListAsync(Guid id, CancellationToken ct)
		=> await _repository.FindPriceListAsync(await _property.IdAsync(ct), id, ct) ?? throw DomainException.NotFound(CatalogErrors.PriceListNotFound);

	private async Task<AgreementReport> LoadReportAsync(Guid id, CancellationToken ct)
		=> await _repository.FindReportAsync(await _property.IdAsync(ct), id, ct) ?? throw DomainException.NotFound(CatalogErrors.AgreementReportNotFound);

	private static BulkResult Bulk(IReadOnlyList<Guid> ids, IReadOnlySet<Guid> done, string code)
		=> new(ids.Where(done.Contains).ToList(), ids.Where(i => !done.Contains(i)).Select(i => new BulkFailure(i, code, null)).ToList());

	private Task RecordPartnerAsync(string actionCode, AgreementPartner partner, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.AgreementPartner, partner.PublicId,
			new Dictionary<string, object?> { ["name"] = partner.Name }, ct);

	private Task RecordPriceListAsync(string actionCode, AgreementPriceList list, AgreementPartner partner, CancellationToken ct)
		=> _audit.RecordAsync(actionCode, AuditActionCodes.Catalog.Targets.AgreementPriceList, list.PublicId,
			new Dictionary<string, object?> { ["name"] = list.Name, ["partner"] = partner.Name, ["listType"] = Codes.Of(list.ListType) }, ct);
}
