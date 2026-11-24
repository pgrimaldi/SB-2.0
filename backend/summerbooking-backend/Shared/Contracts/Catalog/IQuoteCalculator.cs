namespace Shared.Contracts.Catalog;

/// <summary>Richiesta di preventivo dal gestionale, con le stesse regole di <c>POST /api/catalog/quotes</c>.</summary>
/// <param name="DayPart">full_day, morning, afternoon.</param>
/// <param name="NextSeason">Rinnovo: listino di oggi sui periodi della stagione successiva.</param>
/// <param name="Services">Servizi bar, standard e orari dell'ordine.</param>
/// <param name="RedeemedDiscountId">Sconto già usato dalla prenotazione che si sta modificando: non conta per il massimo di utilizzi.</param>
public sealed record PriceQuoteRequest(
	DateOnly From, DateOnly To, string DayPart, bool Seasonal, IReadOnlyList<PriceQuoteSpot> Spots, string? DiscountCode, bool NextSeason = false,
	IReadOnlyList<PriceQuoteService>? Services = null, Guid? RedeemedDiscountId = null);

/// <summary>Utilizzi dei codici sconto: si contano quando una prenotazione prende lo sconto, senza superare il massimo.</summary>
public interface IDiscountRedemption
{
	/// <summary>Conta un utilizzo; false se il codice ha già raggiunto il massimo (anche per richieste contemporanee).</summary>
	Task<bool> TryRedeemAsync(Guid propertyId, Guid discountId, CancellationToken ct = default);

	/// <summary>Restituisce un utilizzo (prenotazione annullata o sconto tolto).</summary>
	Task ReleaseAsync(Guid propertyId, Guid discountId, CancellationToken ct = default);
}

/// <param name="Date">Giorno di utilizzo (prenotazioni di più giorni, servizi orari).</param>
/// <param name="TimeSlotId">Servizi orari: la durata scelta.</param>
public sealed record PriceQuoteService(Guid ServiceId, short Quantity, string? Variant, DateOnly? Date, Guid? TimeSlotId);

/// <param name="AgreementPriceListId">Listino della struttura convenzionata, al posto del pacchetto.</param>
/// <param name="VoucherPriceListId">Voucher della struttura convenzionata, sottratto dal pacchetto.</param>
/// <param name="ManualPrice">Prezzo del pacchetto scritto a mano dall'operatore.</param>
public sealed record PriceQuoteSpot(
	Guid SpotId, Guid? PriceListId, Guid? AgreementPriceListId, Guid? VoucherPriceListId, IReadOnlyList<DateOnly>? Absences,
	decimal? ManualPrice, IReadOnlyList<PriceQuoteAdditional>? Additionals);

public sealed record PriceQuoteAdditional(Guid PriceListId, short Quantity, IReadOnlyList<DateOnly>? Dates);

/// <summary>Preventivo: importi IVA compresa.</summary>
/// <param name="Discount">Importo dello sconto (positivo), già tolto da <paramref name="Total"/>.</param>
public sealed record PriceQuote(
	IReadOnlyList<PriceQuoteLine> Lines, decimal Subtotal, decimal Discount, decimal Total, PriceQuoteDiscount? AppliedDiscount,
	IReadOnlyList<string> Warnings);

/// <param name="Kind">package, additional, voucher, surcharge, service, automatic_price.</param>
public sealed record PriceQuoteLine(string Kind, Guid? RefId, Guid? SpotId, string Description, short Quantity, decimal Amount, decimal? VatRate);

/// <param name="Type">percentage o fixed.</param>
public sealed record PriceQuoteDiscount(Guid Id, string Code, string? Name, string Type, decimal Value, decimal Amount);

/// <summary>Preventivo di Catalog per lo stabilimento della richiesta, usato da Booking per salvare gli importi.</summary>
public interface IQuoteCalculator
{
	/// <summary>Gli errori di listino (pacchetto inesistente, sconto non valido…) arrivano come <c>DomainException</c> di Catalog.</summary>
	Task<PriceQuote> QuoteAsync(PriceQuoteRequest request, CancellationToken ct = default);

	/// <summary>Preventivo della prenotazione online dello stabilimento (valgono le sue regole online).</summary>
	Task<PriceQuote> QuoteOnlineAsync(Guid propertyId, PriceQuoteRequest request, CancellationToken ct = default);

	/// <summary>
	/// Pacchetto cumulativo ripetuto sulla stessa postazione (prenotazione ripetuta): prezzo dei giorni totali di ogni
	/// periodo, ripartito fra i blocchi per i loro giorni, nello stesso ordine. Null se il pacchetto non è cumulativo o se un
	/// blocco non si prezza con la griglia (giorni senza periodo o senza prezzo, stagionale ridotto).
	/// </summary>
	Task<IReadOnlyList<decimal>?> QuoteCumulativeBlocksAsync(
		Guid propertyId, Guid spotId, Guid priceListId, IReadOnlyList<PriceQuoteBlock> blocks, bool nextSeason, CancellationToken ct = default);
}

/// <summary>Un blocco di giorni consecutivi della prenotazione ripetuta.</summary>
public sealed record PriceQuoteBlock(DateOnly From, DateOnly To);
