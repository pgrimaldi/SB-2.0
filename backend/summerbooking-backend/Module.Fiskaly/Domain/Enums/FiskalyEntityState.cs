namespace Module.Fiskaly.Domain.Enums;

/// <summary>
/// Ciclo di vita di un'entita Fiskaly (Taxpayer/Location/System) su SIGN IT.
/// Riusato da fiskaly_accounts (taxpayer_state/location_state) e fiskaly_cash_registers (system_state).
/// </summary>
public enum FiskalyEntityState
{
	Acquired = 0,
	Commissioned = 1,
	Decommissioned = 2
}
