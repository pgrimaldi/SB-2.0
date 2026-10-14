namespace Module.Booking.Domain.Enums;

/// <summary>
/// Stato del blocco di una postazione.
/// </summary>
public enum HoldStatus
{
	Active = 0,
	Released = 1,
	Expired = 2,
	Converted = 3
}
