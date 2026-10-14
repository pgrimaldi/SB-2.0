namespace Module.ZTest.Domain.Exceptions;

/// <summary>
/// Eccezione di dominio sollevata quando la prenotazione richiesta non esiste.
/// </summary>
public sealed class ReservationNotFoundException : Exception
{
	public ReservationNotFoundException(Guid reservationId)
		: base($"Prenotazione con id '{reservationId}' non trovata.")
	{
		ReservationId = reservationId;
	}

	public Guid ReservationId { get; }
}
