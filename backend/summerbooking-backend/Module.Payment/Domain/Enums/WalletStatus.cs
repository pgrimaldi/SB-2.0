namespace Module.Payment.Domain.Enums;

/// <summary>Stato operativo di un conto borsellino (<see cref="Entities.WalletAccount"/>).</summary>
public enum WalletStatus
{
	Active = 0,
	Blocked = 1,
	Closed = 2
}
