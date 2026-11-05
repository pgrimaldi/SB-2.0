namespace Module.Catalog.Application.Dtos;

public sealed record AgreementPartnerQuery(string? Search, int Page = 1, int PageSize = 10, string? SortBy = null, string? SortDir = null);

public static class AgreementPartnerSortFields
{
	public const string Name = "name";
	public const string City = "city";
	public static readonly IReadOnlySet<string> All = new HashSet<string> { Name, City };
}

/// <summary>Struttura convenzionata (hotel, campeggio…).</summary>
public sealed record AgreementPartnerDto(
	Guid Id, string Name, string? City, string? PostalCode, string? Province, string? StreetName, string? StreetNumber, string? Phone,
	string? Email, string? Note, bool HasPriceList, bool HasVoucher);

public sealed record SaveAgreementPartnerRequest(
	string Name, string? City, string? PostalCode, string? Province, string? StreetName, string? StreetNumber, string? Phone, string? Email, string? Note);

public sealed record AgreementPartnerLookupDto(Guid Id, string Name);

/// <param name="ListType">regular (listino) o voucher.</param>
/// <param name="MinBookingDays">Solo voucher: giorni minimi della prenotazione.</param>
/// <param name="MaxVoucherQuantity">Solo voucher: massimo di voucher per prenotazione.</param>
public sealed record AgreementPriceListDto(
	Guid Id, Guid PartnerId, string ListType, string Kind, string Name, short? MaxPeople, IReadOnlyList<PriceListItemDto> Items,
	IReadOnlyList<Guid> IncludedServiceIds, IReadOnlyList<PackageLinkDto> PackageLinks, string? CalculationMethod, string? MonthlySetting,
	string? WeekendSetting, bool AllowsPublicHolidays, IReadOnlyList<DateOnly> HolidayDates, short? MinBookingDays, short? MaxVoucherQuantity,
	bool IsCumulative, decimal? VatRate, string? VatNature, IReadOnlyList<SectorPriceDto> SectorPrices, IReadOnlyList<PeriodPricesDto> PeriodPrices);

/// <param name="PackageLinks">Aggiuntivi: pacchetti della stessa convenzione e dello stesso tipo di listino; vuoto = tutti.</param>
public sealed record SaveAgreementPriceListRequest(
	string ListType, string Kind, string Name, short? MaxPeople, IReadOnlyList<PriceListItemDto>? Items, IReadOnlyList<Guid>? IncludedServiceIds,
	IReadOnlyList<PackageLinkDto>? PackageLinks, string? CalculationMethod, string? MonthlySetting, string? WeekendSetting,
	bool AllowsPublicHolidays, IReadOnlyList<DateOnly>? HolidayDates, short? MinBookingDays, short? MaxVoucherQuantity, bool IsCumulative,
	decimal? VatRate, string? VatNature, IReadOnlyList<SectorPriceDto>? SectorPrices, IReadOnlyList<PeriodPricesDto>? PeriodPrices);

public sealed record AgreementReportQuery(Guid? PartnerId, string? Status, int Page = 1, int PageSize = 10);

/// <param name="SearchType">check_in o check_out: prenotazioni filtrate per data di inizio o di fine.</param>
/// <param name="Status">pending, created, cancelled.</param>
public sealed record AgreementReportDto(
	Guid Id, Guid PartnerId, string PartnerName, string SearchType, DateOnly PeriodStart, DateOnly PeriodEnd, string Status,
	int TotalReservations, decimal TotalRevenue, string? Notes, DateTime CreatedAt);
