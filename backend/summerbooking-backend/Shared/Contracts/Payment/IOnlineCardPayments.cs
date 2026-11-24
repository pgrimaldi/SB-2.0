namespace Shared.Contracts.Payment;

/// <summary>Pagamento con carta di una prenotazione fatta dal sito.</summary>
/// <param name="Amount">Importo da addebitare (totale o metà come acconto).</param>
/// <param name="ExpiresAt">Oltre questo momento il pagamento non vale più (le postazioni si liberano).</param>
/// <param name="Language">Lingua della pagina di pagamento (codice di due lettere).</param>
public sealed record OnlineCheckoutRequest(
	Guid PropertyId, Guid ReservationId, string BookingNumber, decimal Amount, string Currency, DateTime? ExpiresAt,
	string? CustomerEmail, string Language);

/// <summary>Pagamento aperto sul circuito: il front porta il cliente su <c>RedirectUrl</c> o usa <c>ClientSecret</c>.</summary>
public sealed record OnlineCheckout(string Provider, string SessionId, string? RedirectUrl, string? ClientSecret, DateTime? ExpiresAt);

/// <summary>
/// Pagamenti con carta dal sito (circuito delle carte): lo implementa Payment, lo chiama Booking. L'esito arriva a Booking
/// con <c>IOnlineBookingPayments</c>.
/// </summary>
public interface IOnlineCardPayments
{
	/// <summary>Apre il pagamento subito dopo la prenotazione; null = pagamento con carta non attivo.</summary>
	Task<OnlineCheckout?> StartCheckoutAsync(OnlineCheckoutRequest request, CancellationToken ct = default);

	/// <summary>Richiesta di pagamento dopo la conferma del gestore (link da mandare al cliente); null = non attivo.</summary>
	Task<OnlineCheckout?> RequestPaymentAsync(OnlineCheckoutRequest request, CancellationToken ct = default);

	/// <summary>La prenotazione è stata annullata o è scaduta prima del pagamento: il pagamento aperto si chiude.</summary>
	Task CancelCheckoutAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default);
}
