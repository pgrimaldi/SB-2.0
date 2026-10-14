namespace Module.Payment.Domain.Enums;

/// <summary>Motivo di un rimborso, per report e per il parametro reason di Stripe (<see cref="Entities.Refund"/>).</summary>
public enum RefundReason
{
	RequestedByCustomer = 0,
	BookingCanceled = 1,
	Duplicate = 2,
	Fraudulent = 3,
	Other = 4
}
