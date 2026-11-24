namespace Shared.Contracts.Fiskaly;

/// <summary>Esito dell'annullo degli scontrini di una prenotazione.</summary>
/// <param name="Voided">Scontrini annullati.</param>
/// <param name="Pending">Scontrini che restano da annullare (servizio non attivo o non raggiungibile).</param>
public sealed record ReceiptVoidResult(int Voided, int Pending, bool Available);

/// <summary>Scontrini delle prenotazioni: lo implementa Fiskaly, lo chiama Booking («Annullare gli scontrini?»).</summary>
public interface IReservationReceipts
{
	/// <summary>Annulla tutti gli scontrini emessi per la prenotazione.</summary>
	Task<ReceiptVoidResult> VoidAllAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default);
}
