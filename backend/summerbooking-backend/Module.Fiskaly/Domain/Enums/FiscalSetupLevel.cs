namespace Module.Fiskaly.Domain.Enums;

/// <summary>
/// Stato di configurazione fiscale di uno stabilimento (fiskaly_accounts.fiscal_setup_level).
/// FiskalyActive è derivato dal dominio (non liberamente scrivibile).
/// </summary>
public enum FiscalSetupLevel
{
	NotConfigured = 0,
	VatOnly = 1,
	FiskalyActive = 2
}
