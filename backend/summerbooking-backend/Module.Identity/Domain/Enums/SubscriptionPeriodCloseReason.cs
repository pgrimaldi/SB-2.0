namespace Module.Identity.Domain.Enums;

/// <summary>
/// Motivo di chiusura di un periodo di abbonamento (identity_account_subscription_periods.close_reason).
/// </summary>
public enum SubscriptionPeriodCloseReason
{
	Renewed = 0,
	Expired = 1,
	Cancelled = 2,
	Superseded = 3
}
