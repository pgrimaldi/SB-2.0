namespace Module.Notification.Domain.Enums;

/// <summary>Come il consenso marketing di un contatto è stato acquisito.</summary>
public enum ConsentSource
{
	BookingFlag = 0,
	PublicSignup = 1,
	ManualImport = 2,
	CampaignRecipientList = 3,
	Admin = 4,
	Unknown = 5
}
