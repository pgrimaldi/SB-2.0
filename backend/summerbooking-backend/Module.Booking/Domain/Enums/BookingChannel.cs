namespace Module.Booking.Domain.Enums;

/// <summary>
/// Origine della prenotazione — vocabolario condiviso anche da booking_service_lines e
/// booking_resource_consumptions.
/// </summary>
public enum BookingChannel
{
	Customer = 0,
	Manager = 1
}
