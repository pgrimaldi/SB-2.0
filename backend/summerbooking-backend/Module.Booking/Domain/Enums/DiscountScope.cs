namespace Module.Booking.Domain.Enums;

/// <summary>
/// Ambito di validità del voucher, congelato dalla regola di Catalog al momento dell'applicazione.
/// </summary>
public enum DiscountScope
{
	All = 0,
	Custom = 1,
	WithoutCustom = 2
}
