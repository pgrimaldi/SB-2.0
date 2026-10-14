namespace Module.Notification.Domain.Enums;

/// <summary>Sistema esterno usato per la consegna di un invio.</summary>
public enum DeliveryProvider
{
	Smtp = 0,
	Ses = 1,
	TwilioWhatsapp = 2,
	TwilioSms = 3,
	Fcm = 4,
	Apns = 5,
	WebPush = 6,
	Internal = 7
}
