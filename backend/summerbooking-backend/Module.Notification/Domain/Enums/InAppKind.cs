namespace Module.Notification.Domain.Enums;

/// <summary>Tipo di evento di una notifica in-app: determina template di rendering e navigazione UI al click.</summary>
public enum InAppKind
{
	BookingCreated = 0,
	ServiceReservationCreated = 1,
	PaymentReceived = 2,
	EreceiptDataError = 3,
	AutoReceiptFailed = 4,
	ReceiptedBookingMoveBlocked = 5,
	Generic = 6
}
