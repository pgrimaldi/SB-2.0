namespace Module.Catalog.Application.Dtos;

/// <param name="Kind">package, additional.</param>
public sealed record PriceListQuery(string? Kind, string? Search, int Page = 1, int PageSize = 50, string? SortBy = null, string? SortDir = null);

public static class PriceListSortFields
{
	public const string Name = "name";
	public static readonly IReadOnlySet<string> All = new HashSet<string> { Name };
}

public sealed record PriceListListItemDto(
	Guid Id, string Kind, string Name, string SalesChannel, bool IsDefault, bool IsDefaultOnline, bool IsCumulative, string? CalculationMethod);

public sealed record PriceListLookupDto(Guid Id, string Kind, string Name);

/// <param name="InventoryItemId">Articolo del magazzino che compone il pacchetto o che l'aggiuntivo vende.</param>
public sealed record PriceListItemDto(Guid InventoryItemId, short Quantity);

/// <summary>Pacchetto a cui è legato un articolo aggiuntivo, con il massimo per prenotazione.</summary>
public sealed record PackageLinkDto(Guid PackageId, short MaxCount);

/// <param name="SeasonalPrice">Prezzo stagionale (intera stagione) nella fila.</param>
public sealed record SectorPriceDto(Guid SectorId, decimal? SeasonalPrice, decimal? SeasonalHalfDayPrice);

public sealed record StayOverrideDto(short StayDays, decimal Price);

/// <summary>Tariffe di una fila in un periodo; i prezzi a 7, 14 giorni e mensili fanno da riferimento al calcolo.</summary>
/// <param name="SectorId">null = tutte le file.</param>
/// <param name="PeriodKey">Periodo tariffario (<c>GET /tariff-periods</c>).</param>
/// <param name="StayOverrides">«Modifica prezzi intermedi»: prezzi corretti a mano per numero di giorni.</param>
public sealed record PeriodPricesDto(
	Guid? SectorId, short? PeriodKey, decimal? HalfDay, decimal? HalfDayWeekend, decimal? Daily, decimal? Weekend, decimal? HalfDay7,
	decimal? Days7, decimal? HalfDay14, decimal? Days14, decimal? HalfDayMonthly, decimal? Monthly, IReadOnlyList<StayOverrideDto> StayOverrides);

/// <param name="ReducedSeasonId">Voce di <c>GET /reduced-seasons</c>.</param>
public sealed record ReducedPriceDto(Guid ReducedSeasonId, Guid SectorId, decimal Price);

public sealed record NameTranslationDto(string Language, string Name);

/// <summary>Pacchetto o articolo aggiuntivo.</summary>
/// <param name="SalesChannel">manager_only, manager_and_online, online_only.</param>
/// <param name="CalculationMethod">proportional, daily, daily_with_weekend, logarithmic.</param>
/// <param name="MonthlySetting">effective_month_duration, actual_month_duration, fixed30days.</param>
/// <param name="WeekendSetting">saturday_sunday, sunday_only.</param>
/// <param name="SectorIds">File/settori abilitate, se <paramref name="IsSectorRestricted"/>.</param>
/// <param name="PackageLinks">Solo aggiuntivi: pacchetti a cui è legato; vuoto = tutti.</param>
/// <param name="IncludedServiceIds">Solo pacchetti: servizi inclusi.</param>
public sealed record PriceListDto(
	Guid Id, string Kind, string Name, IReadOnlyList<NameTranslationDto> Translations, short? MaxPeople, short? MaxAdditionalItems,
	IReadOnlyList<PriceListItemDto> Items, bool IsAreaPackage, IReadOnlyList<Guid> IncludedServiceIds, IReadOnlyList<PackageLinkDto> PackageLinks,
	bool IsSectorRestricted, IReadOnlyList<Guid> SectorIds, string SalesChannel, bool IsDefault, bool IsDefaultOnline, bool IsMapAssignable,
	string? CalculationMethod, string? MonthlySetting, string? WeekendSetting, bool AllowsPublicHolidays, IReadOnlyList<DateOnly> HolidayDates,
	bool IsCumulative, decimal? VatRate, string? VatNature, IReadOnlyList<SectorPriceDto> SectorPrices, IReadOnlyList<PeriodPricesDto> PeriodPrices,
	IReadOnlyList<ReducedPriceDto> ReducedPrices);

public sealed record SavePriceListRequest(
	string Kind, string Name, IReadOnlyList<NameTranslationDto>? Translations, short? MaxPeople, short? MaxAdditionalItems,
	IReadOnlyList<PriceListItemDto>? Items, bool IsAreaPackage, IReadOnlyList<Guid>? IncludedServiceIds, IReadOnlyList<PackageLinkDto>? PackageLinks,
	bool IsSectorRestricted, IReadOnlyList<Guid>? SectorIds, string SalesChannel, bool IsDefault, bool IsDefaultOnline, bool IsMapAssignable,
	string? CalculationMethod, string? MonthlySetting, string? WeekendSetting, bool AllowsPublicHolidays, IReadOnlyList<DateOnly>? HolidayDates,
	bool IsCumulative, decimal? VatRate, string? VatNature, IReadOnlyList<SectorPriceDto>? SectorPrices, IReadOnlyList<PeriodPricesDto>? PeriodPrices,
	IReadOnlyList<ReducedPriceDto>? ReducedPrices);

/// <summary>«Prezzo stagionale ridotto»: voce dello stabilimento con il suo periodo, prezzata in ogni pacchetto.</summary>
public sealed record ReducedSeasonDto(Guid Id, string Name, DateOnly StartsOn, DateOnly EndsOn);

public sealed record SaveReducedSeasonRequest(string Name, DateOnly StartsOn, DateOnly EndsOn);

/// <summary>Esito dell'importazione del listino: righe applicate e righe scartate con il motivo.</summary>
public sealed record PriceListImportResult(int Created, int Updated, IReadOnlyList<ImportRowError> Errors);

public sealed record ImportRowError(int Row, string Code, IReadOnlyDictionary<string, object?>? Args);
