namespace Module.Identity.Domain.Enums;

/// <summary>
/// Stato del ciclo di vita di un documento contrattuale (identity_account_contract_documents.status).
/// </summary>
public enum ContractDocumentStatus
{
	Draft = 0,
	Published = 1,
	Retired = 2
}
