namespace Module.Booking.Domain.Enums;

/// <summary>
/// Conferma del gestore di una prenotazione online: non richiesta, in attesa (con scadenza), confermata, rifiutata, scaduta.
/// </summary>
public enum OnlineConfirmationState
{
	NotRequired = 0,
	Pending = 1,
	Confirmed = 2,
	Rejected = 3,
	Expired = 4
}
