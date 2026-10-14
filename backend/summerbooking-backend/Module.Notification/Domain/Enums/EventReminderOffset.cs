namespace Module.Notification.Domain.Enums;

/// <summary>Anticipo di un reminder rispetto a un evento marketing.</summary>
public enum EventReminderOffset
{
	OneDayBefore = 0,
	ThreeDaysBefore = 1,
	OneWeekBefore = 2,
	Custom = 3
}
