namespace Module.Notification.Domain.Enums;

/// <summary>Esito dell'invio a un destinatario di una campagna/automazione marketing.</summary>
public enum RecipientStatus
{
	Pending = 0,
	Sent = 1,
	Delivered = 2,
	Failed = 3,
	Bounced = 4,
	Skipped = 5
}
