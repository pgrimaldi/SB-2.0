namespace Module.Catalog.Application.Dtos;

/// <summary>Richiesta di preventivo: date, mezza o intera giornata, postazioni con i loro pacchetti, servizi, sconto.</summary>
/// <param name="DayPart">full_day (predefinito), morning, afternoon.</param>
/// <param name="Seasonal">Prenotazione stagionale: vale il prezzo stagionale della fila.</param>
/// <param name="DiscountCode">Codice sconto; senza, vale l'eventuale sconto automatico.</param>
public sealed record QuoteRequest(
	DateOnly From, DateOnly To, string? DayPart, bool Seasonal, IReadOnlyList<QuoteSpotRequest>? Spots, IReadOnlyList<QuoteServiceRequest>? Services,
	string? DiscountCode);

/// <summary>Una postazione: la fila viene dalla postazione o, senza, da <paramref name="SectorId"/>.</summary>
/// <param name="PriceListId">Pacchetto dello stabilimento.</param>
/// <param name="AgreementPriceListId">In alternativa, listino di una struttura convenzionata.</param>
/// <param name="VoucherPriceListId">Voucher della struttura convenzionata: il suo prezzo si sottrae dal pacchetto, fino a 0.</param>
/// <param name="ManualPrice">Prezzo deciso dall'operatore (solo gestionale).</param>
public sealed record QuoteSpotRequest(
	Guid? SpotId, Guid? SectorId, Guid? PriceListId, Guid? AgreementPriceListId, Guid? VoucherPriceListId, IReadOnlyList<DateOnly>? Absences,
	decimal? ManualPrice, IReadOnlyList<QuoteAdditionalRequest>? Additionals);

/// <param name="Dates">Solo per questi giorni (nell'intervallo della prenotazione); vuoto = tutti.</param>
public sealed record QuoteAdditionalRequest(Guid PriceListId, short Quantity, IReadOnlyList<DateOnly>? Dates);

/// <param name="Variant">Nome della variante (servizi bar).</param>
/// <param name="Date">Giorno del servizio (per i prezzi per periodo); vuoto = primo giorno della prenotazione.</param>
/// <param name="TimeSlotId">Fascia del servizio orario, se ha un prezzo dedicato.</param>
public sealed record QuoteServiceRequest(Guid ServiceId, short Quantity, string? Variant, DateOnly? Date, Guid? TimeSlotId);

/// <summary>Preventivo: righe, sconto, totale. Gli importi sono IVA compresa; la commissione online è di Payment.</summary>
public sealed record QuoteDto(
	IReadOnlyList<QuoteLineDto> Lines, decimal Subtotal, QuoteDiscountDto? Discount, decimal Total, bool RequiresManualConfirmation,
	IReadOnlyList<string> Warnings);

/// <param name="Kind">package, additional, voucher, service, automatic_price, surcharge.</param>
/// <param name="Rule">Come si è calcolato: manual, seasonal, reduced, cumulative, grid, revenue_suggested, revenue_original, revenue_discount, list.</param>
public sealed record QuoteLineDto(
	string Kind, Guid? RefId, Guid? SpotId, Guid? SectorId, string Description, short Quantity, decimal Amount, decimal? VatRate, string? VatNature,
	string Rule, IReadOnlyList<QuoteSegmentDto> Segments);

/// <param name="PeriodKey">Periodo tariffario; null = giorni fuori dai periodi (prezzo 0).</param>
public sealed record QuoteSegmentDto(DateOnly From, DateOnly To, short? PeriodKey, int Days, decimal Amount, string Rule);

public sealed record QuoteDiscountDto(Guid Id, string Code, string? Name, string Type, decimal Value, decimal Amount);
