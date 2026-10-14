namespace Module.Booking.Domain.Enums;

/// <summary>
/// Stato del ciclo di vita della prenotazione, distinto dal soft-delete e dal lock anti-doppia prenotazione.
/// </summary>
public enum ReservationStatus
{
	Pending = 0,
	Confirmed = 1,
	Cancelled = 2,
	NoShow = 3
}
