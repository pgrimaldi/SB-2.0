namespace Module.Payment.Domain.Enums;

/// <summary>Verso di un movimento del borsellino (<see cref="Entities.WalletTransaction"/>).</summary>
public enum WalletMovementType
{
	Credit = 0,
	Debit = 1
}
