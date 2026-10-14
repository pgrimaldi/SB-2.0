using Module.Fiskaly.Domain.Enums;
using Shared.Contracts;

namespace Module.Fiskaly.Domain.Entities;

/// <summary>
/// Account fiscale di uno stabilimento presso il fornitore esterno Fiskaly SIGN IT: identità delle
/// 4 entità Fiskaly (Organization/Subject/Taxpayer/Location), credenziali (per riferimento) e stato di
/// configurazione fiscale dello stabilimento. Aggregate root del modulo Module.Fiskaly; possiede come
/// collezione le CashRegisters dello stabilimento (cascade su soft-delete).
/// </summary>
public sealed class FiskalyAccount : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public Guid PropertyId { get; private set; }
	public FiscalSetupLevel FiscalSetupLevel { get; private set; }
	public DateTime? TermsAcceptedAt { get; private set; }
	public bool IsEnabled { get; private set; }
	public bool AutoreceiptEnabled { get; private set; }
	public string? OrganizationId { get; private set; }
	public string? SubjectId { get; private set; }
	public string? TaxpayerId { get; private set; }
	public string? LocationId { get; private set; }
	public string? OrganizationName { get; private set; }
	public string? SubjectApiKeyName { get; private set; }
	public FiskalyOrgState? OrganizationState { get; private set; }
	public FiskalyOrgState? SubjectState { get; private set; }
	public FiskalyEntityState? TaxpayerState { get; private set; }
	public FiskalyEntityState? LocationState { get; private set; }
	public FiskalyEntityMode? TaxpayerMode { get; private set; }
	public FiskalyEntityMode? LocationMode { get; private set; }
	public string? SubjectCredentialsSecretRef { get; private set; }
	public string? TaxpayerLegalName { get; private set; }
	public string? TaxpayerTradeName { get; private set; }
	public string? TaxpayerVatNumber { get; private set; }
	public string? TaxpayerFiscalCode { get; private set; }
	public string? TaxpayerAddressStreet { get; private set; }
	public string? TaxpayerAddressStreetNumber { get; private set; }
	public string? TaxpayerAddressPostalCode { get; private set; }
	public string? TaxpayerAddressCity { get; private set; }
	public string TaxpayerAddressCountryCode { get; private set; } = null!;
	public string? FisconlineHolderFiscalCode { get; private set; }
	public string? FisconlineCredentialsSecretRef { get; private set; }
	public DateTime? FisconlineUpdatedAt { get; private set; }
	public bool SoftwareCashRegisterIsDefault { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public Guid? UpdatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<CashRegister> CashRegisters { get; private set; } = new List<CashRegister>();

	private FiskalyAccount() { }   // unico costruttore, per il materializzatore di EF Core
}
