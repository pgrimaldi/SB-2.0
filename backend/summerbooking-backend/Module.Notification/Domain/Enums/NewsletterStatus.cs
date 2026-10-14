namespace Module.Notification.Domain.Enums;

/// <summary>Stato del ciclo di vita double opt-in di un'iscrizione newsletter.</summary>
public enum NewsletterStatus
{
	PendingConfirmation = 0,
	Subscribed = 1,
	Unsubscribed = 2
}
