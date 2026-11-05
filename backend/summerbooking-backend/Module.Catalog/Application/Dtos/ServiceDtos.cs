namespace Module.Catalog.Application.Dtos;

/// <param name="Kind">bar, normal, hourly.</param>
public sealed record ServiceListQuery(string? Kind, string? Search, int Page = 1, int PageSize = 50, string? SortBy = null, string? SortDir = null);

public static class ServiceSortFields
{
	public const string Name = "name";
	public const string Price = "price";
	public static readonly IReadOnlySet<string> All = new HashSet<string> { Name, Price };
}

public sealed record ServiceListItemDto(
	Guid Id, string Kind, string Name, decimal Price, string? Category, bool IsPurchasable, bool IsHighlighted, string? ImageUrl);

public sealed record ServiceLookupDto(Guid Id, string Kind, string Name);

/// <param name="Language">Codice di una lingua gestita diversa dall'italiano (en, fr, es, de, zh, ar).</param>
public sealed record TranslationDto(string Language, string Name, string? Description);

public sealed record VariantDto(string Name, decimal Price);

/// <param name="Weekdays">monday…sunday; vuoto = tutti i giorni.</param>
/// <param name="IntervalMinutes">Tempo minimo fra due ordini dello stesso cliente.</param>
/// <param name="MaxPerSlot">Servizi orari: massimo di prenotazioni per fascia.</param>
/// <param name="Hourly">Servizi orari: disponibilità e prezzi validi nelle date dell'opzione.</param>
public sealed record BookingOptionDto(
	DateOnly? StartsOn, DateOnly? EndsOn, IReadOnlyList<string> Weekdays, TimeOnly? ManagementFrom, TimeOnly? ManagementTo,
	TimeOnly? OnlineFrom, TimeOnly? OnlineTo, short? MaxPerHour, short? MaxPerDay, short? MaxPerSlot, short? IntervalMinutes,
	short? MinItems, short? MaxItems, HourlyOptionDto? Hourly = null);

/// <param name="PeriodKey">Chiave del periodo tariffario (<c>GET /tariff-periods</c>).</param>
public sealed record PeriodPriceDto(short PeriodKey, decimal Price);

/// <summary>Durata prenotabile di un servizio orario con il suo prezzo.</summary>
/// <param name="DurationMinutes">Durate standard: 10, 15, 30, 45, 60, 90, 120, 180, 240, 300, 360; la personalizzata da 1 a 1440.</param>
/// <param name="Id">Solo in risposta: l'id da indicare nel preventivo e nella prenotazione (<c>timeSlotId</c>).</param>
public sealed record HourlySlotDto(short DurationMinutes, bool IsEnabled, decimal? Price, Guid? Id = null);

/// <summary>Disponibilità di un servizio orario.</summary>
/// <param name="TimeFrom">Inizio della fascia del giorno in cui si prenota; null = dall'apertura.</param>
/// <param name="Weekdays">monday…sunday; vuoto = tutti i giorni.</param>
/// <param name="CustomSlot">Durata personalizzata, oltre a quelle standard.</param>
/// <param name="VatRate">Aliquota IVA; null = quella del servizio.</param>
/// <param name="HasFixedTimeSlots">Le prenotazioni partono a orari fissi, una dopo l'altra.</param>
public sealed record HourlyOptionDto(
	TimeOnly? TimeFrom, TimeOnly? TimeTo, IReadOnlyList<string> Weekdays, IReadOnlyList<HourlySlotDto> Slots, HourlySlotDto? CustomSlot,
	decimal? VatRate, short? MaxPerSlot, bool HasFixedTimeSlots);

/// <summary>Prezzi di un servizio orario per periodo tariffario.</summary>
/// <param name="PeriodKey">Periodo tariffario (<c>GET /tariff-periods</c>); null = senza periodo (con <c>hasPeriodPrices</c> = false).</param>
public sealed record HourlyPeriodDto(short? PeriodKey, IReadOnlyList<HourlyOptionDto> Options);

/// <summary>Servizio bar, normale o orario, completo.</summary>
/// <param name="VatRate">Aliquota in percentuale; null con <paramref name="VatNature"/> = esente.</param>
/// <param name="Category">Solo bar: bar, pizzeria, restaurant, fry_shop, sandwich_shop, appetizers, first_courses, second_courses, desserts, fruit, ice_cream_shop, drinks.</param>
/// <param name="IconId">Icona dei lookup (<c>GET /lookups/service-icons</c>); in alternativa all'immagine caricata.</param>
/// <param name="Allergens">Solo bar: gluten, crustaceans, eggs, fish, peanuts, soybeans, milk, nuts, celery, mustard, sesame, sulphites, lupin, molluscs.</param>
public sealed record ServiceDto(
	Guid Id, string Kind, string Name, string? Description, string? Category, decimal Price, decimal? VatRate, string? VatNature,
	Guid? IconId, string? ImageUrl, bool IsPurchasable, bool IsHighlighted, bool HasPeriodPrices, IReadOnlyList<PeriodPriceDto> PeriodPrices,
	bool IsSectorRestricted, IReadOnlyList<Guid> SectorIds, IReadOnlyList<string> Allergens, IReadOnlyList<VariantDto> Variants,
	IReadOnlyList<TranslationDto> Translations, IReadOnlyList<BookingOptionDto> BookingOptions, IReadOnlyList<HourlyPeriodDto> HourlyPrices);

/// <param name="Kind">Solo alla creazione; nel PUT deve restare uguale.</param>
public sealed record SaveServiceRequest(
	string Kind, string Name, string? Description, string? Category, decimal Price, decimal? VatRate, string? VatNature, Guid? IconId,
	bool IsPurchasable, bool IsHighlighted, bool HasPeriodPrices, IReadOnlyList<PeriodPriceDto>? PeriodPrices, bool IsSectorRestricted,
	IReadOnlyList<Guid>? SectorIds, IReadOnlyList<string>? Allergens, IReadOnlyList<VariantDto>? Variants,
	IReadOnlyList<TranslationDto>? Translations, IReadOnlyList<BookingOptionDto>? BookingOptions, IReadOnlyList<HourlyPeriodDto>? HourlyPrices);

public sealed record DateRangeTimeDto(TimeOnly From, TimeOnly To);

/// <summary>Periodo di validità di un prezzo automatico.</summary>
/// <param name="StartsOn">Primo giorno; null = dall'inizio della stagione.</param>
/// <param name="Weekdays">monday…sunday; vuoto = tutti i giorni.</param>
/// <param name="TimeRanges">Fasce orarie; vuoto = tutto il giorno.</param>
public sealed record AutomaticPricePeriodDto(DateOnly? StartsOn, DateOnly? EndsOn, IReadOnlyList<string> Weekdays, IReadOnlyList<DateRangeTimeDto> TimeRanges);

/// <summary>Prezzo automatico (es. «Coperto»).</summary>
/// <param name="AppliesPerService">true = per ogni servizio dell'ordine; false = una volta per ordine.</param>
/// <param name="ServiceIds">Servizi a cui si applica, se non <paramref name="AppliesToAllServices"/>.</param>
public sealed record AutomaticPriceDto(
	Guid Id, string Name, string? Description, decimal Price, decimal? VatRate, string? VatNature, Guid? IconId, string? ImageUrl,
	bool IsPurchasable, bool AppliesPerService, bool AppliesToAllServices, IReadOnlyList<Guid> ServiceIds, IReadOnlyList<AutomaticPricePeriodDto> Periods,
	bool IsSectorRestricted, IReadOnlyList<Guid> SectorIds,
	IReadOnlyList<TranslationDto> Translations);

public sealed record SaveAutomaticPriceRequest(
	string Name, string? Description, decimal Price, decimal? VatRate, string? VatNature, Guid? IconId, bool IsPurchasable,
	bool AppliesPerService, bool AppliesToAllServices, IReadOnlyList<Guid>? ServiceIds, IReadOnlyList<AutomaticPricePeriodDto>? Periods,
	bool IsSectorRestricted, IReadOnlyList<Guid>? SectorIds,
	IReadOnlyList<TranslationDto>? Translations);

public sealed record TableTypeDto(short Seats, short Count);

/// <param name="Weekday">null = stessa fascia per tutti i giorni del periodo.</param>
public sealed record TableSlotDto(string? Weekday, TimeOnly From, TimeOnly To);

public sealed record TableAvailabilityDto(DateOnly StartsOn, DateOnly EndsOn, IReadOnlyList<TableSlotDto> Slots);

/// <summary>Servizio tavoli (pranzo, cena).</summary>
/// <param name="CapacityMode">tables (con <paramref name="TableTypes"/>) o seats (con <paramref name="TotalSeats"/>).</param>
/// <param name="DepositMode">none, total, per_person.</param>
/// <param name="DepositType">percentage, amount.</param>
public sealed record TableServiceDto(
	Guid Id, string Name, string? Description, bool IsOnlineBookable, bool IsHighlighted, string CapacityMode, IReadOnlyList<TableTypeDto> TableTypes,
	short? TotalSeats, short AverageMealMinutes, short? MaxBookingsPerSlot, short? MinSeatsPerBooking, short? MaxSeatsPerBooking,
	string DepositMode, string? DepositType, decimal? DepositValue, IReadOnlyList<TableAvailabilityDto> Availabilities,
	IReadOnlyList<TranslationDto> Translations);

public sealed record SaveTableServiceRequest(
	string Name, string? Description, bool IsOnlineBookable, bool IsHighlighted, string CapacityMode, IReadOnlyList<TableTypeDto>? TableTypes,
	short? TotalSeats, short AverageMealMinutes, short? MaxBookingsPerSlot, short? MinSeatsPerBooking, short? MaxSeatsPerBooking,
	string DepositMode, string? DepositType, decimal? DepositValue, IReadOnlyList<TableAvailabilityDto>? Availabilities,
	IReadOnlyList<TranslationDto>? Translations);
