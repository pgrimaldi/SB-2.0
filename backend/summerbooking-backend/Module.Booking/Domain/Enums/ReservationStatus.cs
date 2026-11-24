namespace Module.Booking.Domain.Enums;

/// <summary>
/// Stato del ciclo di vita della prenotazione, distinto dal cestino (deleted_at) e dal vincolo anti-doppia
/// prenotazione. Non esiste «non presentato»: basta il check-in.
/// </summary>
public enum ReservationStatus
{
	Pending = 0,
	Confirmed = 1,
	Cancelled = 2
}
