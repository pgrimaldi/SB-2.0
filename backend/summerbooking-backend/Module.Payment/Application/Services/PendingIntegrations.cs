using Shared.Contracts.Booking;
using Shared.Contracts.Events;
using Shared.Contracts.Payment;

namespace Module.Payment.Application.Services;

/// <summary>
/// Pagamento con carta dal sito: non ancora attivo (manca il collegamento al circuito delle carte, Stripe). Restituisce
/// sempre null: Booking risponde senza pagamento aperto e la prenotazione scade da sola se non arriva l'incasso.
/// Da fare: sessione di pagamento, preautorizzazione per le richieste da confermare, notifiche del circuito che chiamano
/// <see cref="IOnlineBookingPayments"/>, rimborsi sulla carta.
/// </summary>
public sealed class InactiveOnlineCardPayments : IOnlineCardPayments
{
	public Task<OnlineCheckout?> StartCheckoutAsync(OnlineCheckoutRequest request, CancellationToken ct = default)
		=> Task.FromResult<OnlineCheckout?>(null);

	public Task<OnlineCheckout?> RequestPaymentAsync(OnlineCheckoutRequest request, CancellationToken ct = default)
		=> Task.FromResult<OnlineCheckout?>(null);

	public Task CancelCheckoutAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>
/// Credito dello sharing maturato dal titolare: la ricarica della tessera prepagata (<c>CardTopUpAmount</c>) va nel
/// borsellino del cliente. Da fare con la tessera prepagata: movimento del borsellino con il riferimento all'offerta;
/// il voucher (<c>VoucherAmount</c>) lo emette Catalog.
/// </summary>
public sealed class ResaleCreditEarnedHandler : IIntegrationEventHandler<ResaleCreditEarned>
{
	public Task HandleAsync(ResaleCreditEarned @event, CancellationToken ct = default) => Task.CompletedTask;
}

/// <summary>Credito dello sharing annullato: da fare con la tessera prepagata (storno della ricarica).</summary>
public sealed class ResaleCreditReversedHandler : IIntegrationEventHandler<ResaleCreditReversed>
{
	public Task HandleAsync(ResaleCreditReversed @event, CancellationToken ct = default) => Task.CompletedTask;
}
