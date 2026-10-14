namespace Module.Booking.Domain.Enums;

/// <summary>
/// Cosa ha generato il consumo di una risorsa.
/// </summary>
public enum ConsumptionSource
{
	ReservationLine = 0,
	ServiceLine = 1,
	AdditionalItem = 2,
	ManualHold = 3,
	Resale = 4
}
