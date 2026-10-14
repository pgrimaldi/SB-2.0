namespace Module.Payment.Domain.Enums;

/// <summary>Esito di un rimborso Stripe (<see cref="Entities.Refund"/>).</summary>
public enum RefundStatus
{
	Pending = 0,
	Succeeded = 1,
	Failed = 2,
	Canceled = 3
}
