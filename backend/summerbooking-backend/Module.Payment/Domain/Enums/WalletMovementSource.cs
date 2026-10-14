namespace Module.Payment.Domain.Enums;

/// <summary>Causale di un movimento del borsellino (<see cref="Entities.WalletTransaction"/>).</summary>
public enum WalletMovementSource
{
	TopUp = 0,
	ServiceConsumption = 1,
	ResaleCreditEarned = 2,
	ReservationSettlement = 3,
	Reversal = 4,
	Adjustment = 5,
	Expiry = 6
}
