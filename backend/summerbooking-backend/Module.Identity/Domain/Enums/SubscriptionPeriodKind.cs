namespace Module.Identity.Domain.Enums;

/// <summary>
/// Natura di un periodo di abbonamento nello storico append-only
/// (identity_account_subscription_periods.period_kind).
/// </summary>
public enum SubscriptionPeriodKind
{
	Initial = 0,
	Renewal = 1,
	Migration = 2,
	ManualAdjustment = 3
}
