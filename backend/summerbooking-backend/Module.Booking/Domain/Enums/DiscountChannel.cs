namespace Module.Booking.Domain.Enums;

/// <summary>
/// Canale su cui il voucher era ammesso, congelato dalla regola di Catalog.
/// </summary>
public enum DiscountChannel
{
	Any = 0,
	ManagerOnly = 1,
	OnlineOnly = 2
}
