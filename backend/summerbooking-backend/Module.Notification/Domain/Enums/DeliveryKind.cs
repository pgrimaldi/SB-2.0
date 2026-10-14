namespace Module.Notification.Domain.Enums;

/// <summary>
/// Scopo funzionale di un invio: determina template, priorità, retention e regole anti-spam.
/// </summary>
public enum DeliveryKind
{
	BookingConfirmation = 0,
	ReservationSummary = 1,
	PaymentLink = 2,
	PaymentReceipt = 3,
	EReceipt = 4,
	NonFiscalReceipt = 5,
	AgreementReport = 6,
	ResaleCreditEarned = 7,
	WalletStatement = 8,
	LifeguardAttendance = 9,
	ForgotPassword = 10,
	EmailVerification = 11,
	AccountLifecycle = 12,
	Newsletter = 13,
	MarketingCampaign = 14,
	MarketingAutomation = 15,
	EstablishmentClaim = 16,
	Test = 17,
	Other = 18
}
