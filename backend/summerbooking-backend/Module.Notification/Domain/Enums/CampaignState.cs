namespace Module.Notification.Domain.Enums;

/// <summary>Stato del ciclo di vita di una campagna email one-shot.</summary>
public enum CampaignState
{
	Draft = 0,
	Scheduled = 1,
	Sending = 2,
	Sent = 3,
	Canceled = 4
}
