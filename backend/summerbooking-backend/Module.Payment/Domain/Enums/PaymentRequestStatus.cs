namespace Module.Payment.Domain.Enums;

/// <summary>Stato di un link di pagamento Stripe (<see cref="Entities.PaymentRequestLink"/>).</summary>
public enum PaymentRequestStatus
{
	Pending = 0,
	Paid = 1,
	Canceled = 2,
	Expired = 3,
	Failed = 4
}
