namespace Module.Booking.Domain.Enums;

/// <summary>
/// Stato del flusso di conferma online via reservation_code (requestBookingConfirmation).
/// </summary>
public enum OnlineConfirmationState
{
	NotRequired = 0,
	Pending = 1,
	Confirmed = 2
}
