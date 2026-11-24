namespace Shared.Contracts.Booking;

/// <summary>Esito del pagamento online per Booking.</summary>
/// <param name="Confirmed">La prenotazione vale (anche arrivata tardi, se le postazioni erano ancora libere).</param>
/// <param name="RefundInFull">Le postazioni erano state prese nel frattempo: Payment rimborsa tutto, costi di servizio compresi.</param>
public sealed record OnlinePaymentOutcome(bool Confirmed, bool RefundInFull);

/// <summary>
/// Pagamento online di una prenotazione dal sito: lo implementa Booking, lo chiama Payment quando il circuito di pagamento
/// (Stripe) risponde. Booking tiene le postazioni 5 minuti mentre il cliente paga e fino a 10 dopo «Paga», poi la
/// prenotazione si annulla da sola.
/// </summary>
public interface IOnlineBookingPayments
{
	/// <summary>Il cliente ha premuto «Paga»: le postazioni restano tenute finché la banca risponde (al massimo 10 minuti).</summary>
	Task PaymentStartedAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default);

	/// <summary>Pagamento riuscito (incasso già registrato da Payment).</summary>
	Task<OnlinePaymentOutcome> PaymentSucceededAsync(Guid propertyId, Guid reservationId, decimal amount, CancellationToken ct = default);

	/// <summary>Pagamento non riuscito: la prenotazione si annulla e le postazioni si liberano.</summary>
	Task PaymentFailedAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default);
}
