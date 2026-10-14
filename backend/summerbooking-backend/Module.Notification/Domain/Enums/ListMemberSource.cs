namespace Module.Notification.Domain.Enums;

/// <summary>Come un contatto marketing è entrato in una lista.</summary>
public enum ListMemberSource
{
	Manual = 0,
	BookingAutoEnroll = 1,
	Import = 2,
	Duplicate = 3
}
