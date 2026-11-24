namespace Module.Booking.Application.Dtos;

/// <summary>Il cliente della prenotazione online: nome sempre; email e telefono obbligatori con il limite giornaliero.</summary>
public sealed record OnlineCustomerInput(string FullName, string? Email, string? PhoneCountryCode, string? Phone);

/// <summary>Una postazione scelta online, con il suo pacchetto.</summary>
public sealed record OnlineSpotInput(Guid SpotId, Guid PriceListId, IReadOnlyList<AdditionalInput>? Additionals);

/// <summary>Prenotazione dal sito dello stabilimento (anche da cliente registrato).</summary>
/// <param name="PaymentMethod">pay_now, pay_at_property, pay_at_property_no_card, deposit_half, pay_at_property_prepaid: fra quelli dello stabilimento.</param>
/// <param name="HumanToken">Verifica anti-robot, obbligatoria senza accesso.</param>
public sealed record OnlineReservationRequest(
	OnlineCustomerInput Customer, DateOnly From, DateOnly To, string? DayPart, bool Seasonal, IReadOnlyList<OnlineSpotInput> Spots,
	IReadOnlyList<ServiceInput>? Services, string? DiscountCode, string PaymentMethod, string? Note, string? HumanToken);

/// <summary>Stato del pagamento online richiesto.</summary>
/// <param name="HoldUntil">Le postazioni sono tenute fino a quest'ora; poi la prenotazione si annulla da sola.</param>
/// <param name="Provider">Circuito del pagamento aperto; null = pagamento con carta non attivo.</param>
/// <param name="RedirectUrl">Pagina di pagamento su cui portare il cliente.</param>
/// <param name="ClientSecret">In alternativa al redirect, per il modulo di pagamento incorporato.</param>
public sealed record OnlinePaymentDto(bool Required, decimal Amount, DateTime? HoldUntil, string? Provider = null, string? RedirectUrl = null, string? ClientSecret = null);

/// <summary>Una postazione della prenotazione vista dal cliente.</summary>
public sealed record CustomerLineDto(Guid LineId, string? SpotLabel, string? SpotType, DateOnly From, DateOnly To, string DayPart, string? PackageName);

/// <summary>Prenotazione vista dal cliente (sito e area cliente).</summary>
/// <param name="Status">pending, confirmed, cancelled.</param>
/// <param name="ConfirmationState">not_required, pending, confirmed, rejected, expired.</param>
/// <param name="CancellationDeadline">Fino a quando il cliente può annullare; null = annullamento non permesso.</param>
public sealed record CustomerReservationDto(
	Guid Id, Guid PropertyId, string PropertyName, string BookingNumber, string ReservationCode, string Status, string ConfirmationState,
	string? CancellationReason, DateOnly? From, DateOnly? To, IReadOnlyList<CustomerLineDto> Lines, IReadOnlyList<ReservationServiceDto> Services,
	decimal Total, decimal Paid, decimal BalanceDue, DateTime? ConfirmationExpiresAt, DateTime? PaymentHoldUntil, DateTime? CancellationDeadline,
	bool CanShare, DateTime CreatedAt);

public sealed record OnlineReservationResultDto(CustomerReservationDto Reservation, OnlinePaymentDto Payment);

/// <summary>Postazioni prenotabili online in un intervallo (sito dello stabilimento).</summary>
public sealed record OnlineAvailabilityDto(DateOnly From, DateOnly To, IReadOnlyList<Guid> FreeSpotIds);

/// <summary>Esito dell'annullamento del cliente: rimborso dovuto e trattenuta.</summary>
/// <param name="RefundDue">Pagato meno la trattenuta; il rimborso sulla carta lo fa Payment (con Stripe).</param>
/// <param name="Refunded">Rimborso già registrato (incassi al banco).</param>
/// <param name="RefundPendingOnline">Parte da rimborsare sulla carta (la fa il circuito dei pagamenti online).</param>
public sealed record CustomerCancellationDto(
	CustomerReservationDto Reservation, decimal Paid, short PenaltyPercent, decimal Penalty, decimal RefundDue, decimal Refunded, decimal RefundPendingOnline);
