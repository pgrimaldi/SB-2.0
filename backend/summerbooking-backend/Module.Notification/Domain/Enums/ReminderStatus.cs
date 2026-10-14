namespace Module.Notification.Domain.Enums;

/// <summary>Stato di invio di un reminder di evento marketing.</summary>
public enum ReminderStatus
{
	Pending = 0,
	Sent = 1,
	Failed = 2,
	Canceled = 3
}
