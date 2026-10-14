namespace Module.Fiskaly.Domain.Enums;

/// <summary>
/// Modalita operativa di un'entita Fiskaly (Taxpayer/Location/System) su SIGN IT.
/// Riusato da fiskaly_accounts (taxpayer_mode/location_mode) e fiskaly_cash_registers (system_mode).
/// </summary>
public enum FiskalyEntityMode
{
	Inactive = 0,
	Operative = 1,
	Suspended = 2,
	Degraded = 3
}
