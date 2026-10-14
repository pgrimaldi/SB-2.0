namespace Module.Identity.Domain.Enums;

/// <summary>
/// Stato dell'abbonamento alla piattaforma di uno stabilimento (identity_account_subscriptions.status).
/// </summary>
public enum SubscriptionStatus
{
	Active = 0,
	Suspended = 1,
	Expired = 2,
	Cancelled = 3
}
