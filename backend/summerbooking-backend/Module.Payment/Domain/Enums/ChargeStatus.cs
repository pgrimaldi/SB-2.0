namespace Module.Payment.Domain.Enums;

/// <summary>Stato interno di una transazione online, derivato dallo stato Stripe (<see cref="Entities.Charge"/>).</summary>
public enum ChargeStatus
{
	Pending = 0,
	Authorized = 1,
	Succeeded = 2,
	Failed = 3,
	Canceled = 4,
	Refunded = 5,
	PartiallyRefunded = 6
}
