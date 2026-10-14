namespace Module.Notification.Domain.Enums;

/// <summary>
/// Mezzo di consegna di un invio: un messaggio è su un solo canale (un invio "email + WhatsApp"
/// è due righe distinte, non un flag combinato).
/// </summary>
public enum NotificationChannel
{
	Email = 0,
	Whatsapp = 1,
	Sms = 2,
	Push = 3,
	InApp = 4
}
