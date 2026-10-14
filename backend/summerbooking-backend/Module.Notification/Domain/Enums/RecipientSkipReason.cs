namespace Module.Notification.Domain.Enums;

/// <summary>Perché un destinatario marketing è stato saltato.</summary>
public enum RecipientSkipReason
{
	NoConsent = 0,
	Suppressed = 1,
	EmptyAddress = 2,
	InvalidAddress = 3,
	Duplicate = 4
}
