namespace Module.Payment.Domain.Enums;

/// <summary>Natura del movimento contabile (<see cref="Entities.LedgerEntry"/>).</summary>
public enum LedgerEntryType
{
	Deposit = 0,
	Payment = 1,
	Expense = 2,
	Refund = 3,
	Adjustment = 4
}
