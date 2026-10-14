namespace Module.Notification.Domain.Enums;

/// <summary>Regola di ricorrenza di un evento marketing.</summary>
public enum EventRepeatType
{
	Never = 0,
	Daily = 1,
	Weekly = 2,
	Monthly = 3,
	Yearly = 4,
	Custom = 5
}
