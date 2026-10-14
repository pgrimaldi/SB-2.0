namespace Module.Identity.Domain.Enums;

/// <summary>
/// Tipo di evento nel registro append-only delle accettazioni contrattuali
/// (identity_account_contract_acceptances.event_type).
/// </summary>
public enum ContractAcceptanceEventType
{
	Accepted = 0,
	Revoked = 1
}
