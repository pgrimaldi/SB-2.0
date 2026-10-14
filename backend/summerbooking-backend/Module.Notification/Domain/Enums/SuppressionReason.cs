namespace Module.Notification.Domain.Enums;

/// <summary>Motivo di una soppressione (opt-out) marketing.</summary>
public enum SuppressionReason
{
	UnsubscribeLink = 0,
	ManualRequest = 1,
	SpamComplaint = 2,
	HardBounce = 3,
	GdprErasure = 4,
	Admin = 5
}
