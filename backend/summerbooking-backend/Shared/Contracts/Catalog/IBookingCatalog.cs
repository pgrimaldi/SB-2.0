namespace Shared.Contracts.Catalog;

/// <summary>
/// Una postazione della mappa vista da Booking: il tipo è il codice snake_case di Catalog (<c>umbrella</c>,
/// <c>custom_article1</c>, <c>umbrella_area</c>…); fila e colonna servono a riconoscerla dopo «Reimposta mappa» e a
/// leggere le assegnazioni degli affiliati scritte con la posizione.
/// </summary>
public sealed record BookableSpot(
	Guid Id, string? Label, string SpotType, bool IsArea, short? AreaSeatCount, Guid? SectorId, string? SectorName,
	short? Row, short? Column, bool IsAgreementOnly, bool IsBookable);

/// <summary>Date della stagione dello stabilimento e, se impostata, della successiva (serve al rinnovo).</summary>
public sealed record BookingSeason(DateOnly StartsOn, DateOnly EndsOn, DateOnly? NextStartsOn, DateOnly? NextEndsOn);

/// <summary>
/// Regole della prenotazione online di uno stabilimento.
/// </summary>
/// <param name="PaymentMethods">pay_now, pay_at_property, pay_at_property_no_card, deposit_half, pay_at_property_prepaid.</param>
/// <param name="ContactsOnly">Prenotano online solo i contatti registrati (email e telefono coincidenti).</param>
/// <param name="CancellationAllowed">Il cliente può annullare online.</param>
/// <param name="CancellationNoticeHours">Fino a quante ore prima dell'inizio si annulla.</param>
/// <param name="CancellationPenalties">Trattenute: sotto quelle ore di preavviso si trattiene la percentuale.</param>
public sealed record OnlineBookingRules(
	Guid PropertyId, string PropertyName, bool IsOnlineBookable, bool RequiresManualConfirmation, bool AllowsSameDayBooking, short? MaxBookingsPerDay,
	short? MaxSpotsPerBooking, bool ContactsOnly, bool AllowsSharing, IReadOnlyList<string> PaymentMethods, bool IsOnlinePaymentBlocked,
	bool CancellationAllowed, short? CancellationNoticeHours, IReadOnlyList<OnlineCancellationPenalty> CancellationPenalties);

public sealed record OnlineCancellationPenalty(short NoticeHours, short Percent);

/// <summary>
/// Dati di Catalog che servono a Booking per prenotare: postazioni della mappa e prefisso del numero di prenotazione.
/// Lo implementa Catalog; Booking non legge le tabelle di Catalog.
/// </summary>
public interface IBookingCatalog
{
	/// <summary>Postazioni della mappa dello stabilimento (elementi spiaggia esclusi), anche le non prenotabili.</summary>
	Task<IReadOnlyList<BookableSpot>> ListSpotsAsync(Guid propertyId, CancellationToken ct = default);

	/// <summary>Prefisso dei numeri di prenotazione: l'id interno dello stabilimento in Catalog. Null se non esiste.</summary>
	Task<string?> FindNumberPrefixAsync(Guid propertyId, CancellationToken ct = default);

	/// <summary>Regole online dello stabilimento pubblicato con questo indirizzo; null se non c'è.</summary>
	Task<OnlineBookingRules?> FindOnlineRulesBySlugAsync(string slug, CancellationToken ct = default);

	/// <summary>Regole online di uno stabilimento; null se non c'è.</summary>
	Task<OnlineBookingRules?> FindOnlineRulesAsync(Guid propertyId, CancellationToken ct = default);

	/// <summary>Stagione dello stabilimento; null se non è impostata.</summary>
	Task<BookingSeason?> FindSeasonAsync(Guid propertyId, CancellationToken ct = default);
}
