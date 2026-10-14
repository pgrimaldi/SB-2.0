namespace Module.Notification.Domain.Enums;

/// <summary>Tipo dell'entità (di un altro modulo o di questo) che ha originato un invio.</summary>
public enum NotificationSubjectType
{
	BookingReservation = 0,
	ServiceReservation = 1,
	PaymentRequestLink = 2,
	PaymentTransaction = 3,
	FiskalyReceipt = 4,
	AgreementReport = 5,
	ResaleOffer = 6,
	WalletTransaction = 7,
	MarketingCampaign = 8,
	MarketingAutomation = 9,
	MarketingEvent = 10,
	NewsletterSubscription = 11,
	UserAccount = 12,
	None = 13
}
