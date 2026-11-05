namespace Module.Catalog.Application.Dtos;

/// <summary>Revenue dello stabilimento.</summary>
/// <param name="IsSuggestedPriceEnabled">Il motore di prezzo usa il prezzo suggerito scelto dal gestore.</param>
/// <param name="IsPriceDiscountEnabled">Banner sconto sulla pagina pubblica.</param>
/// <param name="DailyBannerCount">Quante volte al giorno si mostra il banner (fascia 10–20).</param>
/// <param name="DailyBannerShownCount">Visualizzazioni di oggi (sola lettura).</param>
/// <param name="SpotIds">Postazioni a cui si limita lo sconto, se <paramref name="IsDiscountLimitedToSpots"/>.</param>
public sealed record RevenueSettingsDto(
	bool IsSuggestedPriceEnabled, bool IsPriceDiscountEnabled, bool IsShownOnPublicPage, short DailyBannerCount, short DailyBannerShownCount,
	bool IsDiscountLimitedToSpots, IReadOnlyList<Guid> SpotIds);

public sealed record SaveRevenueSettingsRequest(
	bool IsSuggestedPriceEnabled, bool IsPriceDiscountEnabled, bool IsShownOnPublicPage, short DailyBannerCount, bool IsDiscountLimitedToSpots,
	IReadOnlyList<Guid>? SpotIds);

/// <param name="PriceListId">Pacchetto; vuoto = il pacchetto predefinito.</param>
public sealed record SuggestedPriceQuery(Guid? PriceListId, DateOnly? From, DateOnly? To);

public sealed record ComputeSuggestedPricesRequest(Guid? PriceListId, DateOnly From, DateOnly To);

/// <summary>Prezzo suggerito di un giorno per una fila del pacchetto.</summary>
/// <param name="SectorId">Fila del prezzo di listino; null = tutte le file.</param>
/// <param name="PeriodKey">Periodo tariffario del giorno.</param>
/// <param name="DiscountPercentage">Rispetto al prezzo di listino del giorno (negativo = aumento); con mode = discount è quella scelta.</param>
/// <param name="AppliedMode">none, suggested, original, discount.</param>
public sealed record SuggestedPriceDto(
	Guid PriceListId, DateOnly Date, Guid? SectorId, short? PeriodKey, decimal? ListDailyPrice, decimal? ListWeekendPrice, decimal SuggestedPrice,
	decimal DiscountPercentage, string AppliedMode, DateTime ComputedAt, DateTime? AppliedAt);

public sealed record ApplySuggestedPricesRequest(Guid PriceListId, IReadOnlyList<ApplySuggestedPriceItem> Items);

/// <param name="Mode">suggested, original, discount (con <paramref name="DiscountPercent"/>), none.</param>
public sealed record ApplySuggestedPriceItem(DateOnly Date, Guid? SectorId, string Mode, decimal? DiscountPercent);

/// <summary>Pesi del prezzo suggerito, uguali per tutti gli stabilimenti (moltiplicatori: 0,70 = -30%, 1,20 = +20%).</summary>
/// <param name="FreeSpotsThresholdPercent">Dalla soglia in su il prezzo scende, sotto sale.</param>
public sealed record RevenueWeightsDto(
	decimal RainLowSeasonFactor, decimal RainHighSeasonFactor, decimal BeforeWeekendFactor, decimal HighSeasonFactor,
	decimal FreeSpotsThresholdPercent, decimal FreeSpotsAboveFactor, decimal FreeSpotsBelowFactor);

/// <summary>Periodo standard ripetuto ogni anno (giorno e mese); può scavalcare il capodanno.</summary>
/// <param name="Tier">low, high.</param>
public sealed record RevenueStandardPeriodDto(string Name, short StartMonth, short StartDay, short EndMonth, short EndDay, string Tier);

/// <summary>Elenco completo: sostituisce quello esistente. I giorni fuori da ogni periodo sono di bassa stagione.</summary>
public sealed record SaveRevenueStandardPeriodsRequest(IReadOnlyList<RevenueStandardPeriodDto> Periods);
