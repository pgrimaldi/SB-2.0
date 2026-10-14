namespace Module.Notification.Domain.Enums;

/// <summary>Motivo per cui un invio (Delivery) non è stato tentato (stato skipped).</summary>
public enum SkipReason
{
	OptedOut = 0,
	Suppressed = 1,
	MissingAddress = 2,
	NonProdEnv = 3,
	HardBounceHistory = 4,
	ConsentNotVerified = 5
}
