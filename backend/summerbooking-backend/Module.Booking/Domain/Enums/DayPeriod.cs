namespace Module.Booking.Domain.Enums;

/// <summary>
/// Fascia oraria della riga — vocabolario condiviso anche da booking_resource_consumptions,
/// booking_resale_offers, booking_spot_holds e booking_resource_availability_daily.
/// </summary>
public enum DayPeriod
{
	FullDay = 0,
	Morning = 1,
	Afternoon = 2
}
