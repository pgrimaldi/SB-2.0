using Shared.Contracts.Fiskaly;

namespace Module.Fiskaly.Application.Services;

/// <summary>
/// Annullo degli scontrini delle prenotazioni: non ancora attivo (il modulo Fiskaly non ha ancora i suoi servizi). Risponde
/// che non c'è nulla di annullato e che il servizio non è disponibile, così Booking registra la richiesta come da fare.
/// Da fare: cercare gli scontrini emessi per la prenotazione e annullarli sulla cassa dello stabilimento.
/// </summary>
public sealed class InactiveReservationReceipts : IReservationReceipts
{
	public Task<ReceiptVoidResult> VoidAllAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default)
		=> Task.FromResult(new ReceiptVoidResult(0, 0, false));
}
