namespace Module.Booking.Domain.Enums;

/// <summary>
/// Tipo di risorsa consumata — vocabolario condiviso anche da booking_resource_availability_daily.
/// </summary>
public enum ResourceType
{
	Umbrella = 0,
	Sunbed = 1,
	SunbedXl = 2,
	Cabin = 3,
	SpecialCabin = 4,
	Deckchair = 5,
	Chair = 6,
	Tent = 7,
	Parking = 8,
	Ticket = 9,
	AreaSeat = 10,
	BarService = 11,
	NormalService = 12,
	HourlyService = 13,
	InventoryItem = 14
}
