namespace Module.Booking.Domain.Enums;

/// <summary>
/// Canale/natura della prenotazione: con postazioni (ex tabella bookings) o soli servizi (ex service_reservation).
/// </summary>
public enum ReservationKind
{
	Spot = 0,
	Service = 1
}
