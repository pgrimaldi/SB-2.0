namespace Module.Notification.Domain.Enums;

/// <summary>Unione dei tipi di notifica silenziabili singolarmente da un utente staff (inapp_kind + delivery_kind rilevanti).</summary>
public enum NotificationKind
{
	BookingCreated = 0,
	ServiceReservationCreated = 1,
	PaymentReceived = 2,
	EreceiptDataError = 3,
	AutoReceiptFailed = 4,
	ReceiptedBookingMoveBlocked = 5,
	BookingConfirmation = 6,
	ReservationSummary = 7,
	PaymentLink = 8,
	PaymentReceipt = 9,
	EReceipt = 10,
	NonFiscalReceipt = 11,
	AgreementReport = 12,
	WalletStatement = 13,
	LifeguardAttendance = 14
}
