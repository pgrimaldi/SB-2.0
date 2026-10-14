namespace Module.Identity.Domain.Enums;

/// <summary>
/// Tipo di abbonamento: a termine fisso con date contrattuali, o banca giorni a consumo (gli abbonamenti
/// nati prima del termine fisso). Condiviso fra identity_account_subscriptions.term_type e
/// identity_account_subscription_periods.term_type.
/// </summary>
public enum SubscriptionTermType
{
	FixedTerm = 0,
	LegacyDayBank = 1
}
