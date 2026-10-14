namespace Module.Notification.Domain.Enums;

/// <summary>Avanzamento della gestione di un lead "rivendica scheda stabilimento".</summary>
public enum ClaimStatus
{
	New = 0,
	InReview = 1,
	Contacted = 2,
	Approved = 3,
	Rejected = 4,
	Spam = 5
}
