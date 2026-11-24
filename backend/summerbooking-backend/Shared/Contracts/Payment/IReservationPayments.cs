using Shared.Contracts.Events;

namespace Shared.Contracts.Payment;

/// <summary>Totali dei movimenti di una prenotazione: <c>Paid</c> = entrate meno uscite (rimborsi), acconti compresi.</summary>
/// <param name="PrevalentMode">cash, credit_card, bank_transfer, other; null senza movimenti.</param>
public sealed record ReservationPaymentTotals(Guid ReservationId, decimal Paid, decimal Deposit, string? PrevalentMode);

/// <summary>Un incasso registrato dal gestionale per una prenotazione.</summary>
/// <param name="Mode">cash, credit_card, bank_transfer, other.</param>
/// <param name="IsDeposit">Acconto invece di saldo.</param>
public sealed record ReservationPaymentInput(
	Guid PropertyId, Guid ReservationId, decimal Amount, string Mode, bool IsDeposit, Guid? CustomerId, DateOnly? StayFrom, DateOnly? StayTo);

/// <summary>Esito di un rimborso dei movimenti di una prenotazione.</summary>
/// <param name="Refunded">Totale registrato come uscita.</param>
/// <param name="PendingOnline">Parte pagata online con carta, il cui rimborso sul circuito non è ancora automatico.</param>
public sealed record ReservationRefundResult(decimal Refunded, decimal PendingOnline);

/// <summary>
/// Incassi delle prenotazioni per Booking: lo implementa Payment. Ogni operazione salva e pubblica
/// <see cref="ReservationPaymentsChanged"/> con i totali aggiornati.
/// </summary>
public interface IReservationPayments
{
	/// <summary>Registra un incasso; restituisce l'id del movimento.</summary>
	Task<Guid> RecordAsync(ReservationPaymentInput input, CancellationToken ct = default);

	/// <summary>Annulla un movimento (incasso registrato per errore o spunta «Pagato» tolta).</summary>
	Task VoidAsync(Guid propertyId, Guid entryId, CancellationToken ct = default);

	/// <summary>Rimborsa tutto l'incassato: un'uscita per ogni metodo, con lo stesso metodo e importo.</summary>
	Task<ReservationRefundResult> RefundAllAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default);

	/// <summary>
	/// Rimborsa al massimo <paramref name="amount"/> dell'incassato (annullamento del cliente con trattenuta, rifiuto del
	/// gestore): prima la parte pagata online, poi gli altri metodi.
	/// </summary>
	Task<ReservationRefundResult> RefundAsync(Guid propertyId, Guid reservationId, decimal amount, CancellationToken ct = default);

	/// <summary>
	/// Sposta <paramref name="amount"/> dell'incassato di una prenotazione su un'altra («Dividi conto»), ripartito in
	/// proporzione fra metodi e fra acconto e saldo. Non tocca la cassa del giorno.
	/// </summary>
	Task TransferAsync(Guid propertyId, Guid fromReservationId, Guid toReservationId, decimal amount, CancellationToken ct = default);

	Task<ReservationPaymentTotals> GetTotalsAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default);
}

/// <summary>I movimenti di una prenotazione sono cambiati: Booking aggiorna pagato, acconto e residuo.</summary>
public sealed record ReservationPaymentsChanged(
	Guid EventId, DateTime OccurredAt, Guid PropertyId, Guid ReservationId, decimal Paid, decimal Deposit, string? PrevalentMode) : IIntegrationEvent;
