namespace Module.Booking.Domain.Enums;

/// <summary>
/// Come si è formato il prezzo finale — flag non esclusivi, mappato come enum[].
/// </summary>
public enum ReservationPriceFlag
{
	SuggestedPriceAccepted = 0,
	Recalculated = 1,
	ManuallyEdited = 2
}
