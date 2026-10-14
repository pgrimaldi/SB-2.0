using Module.Switchboard.Domain.Enums.Voice;
using Shared.Contracts;

namespace Module.Switchboard.Domain.Entities.Voice;

/// <summary>
/// Stato dell'ordine del numero PSTN italiano su Telnyx e fascicolo dei requisiti regolatori (KYC) del
/// centralino, 1:1 con la configurazione. I campi PII ad alto rischio sono cifrati a riposo (KMS) e
/// mai esposti in chiaro come string/DateOnly.
/// </summary>
public sealed class VoiceNumberRegistration : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long ConfigId { get; private set; }
	public Guid PropertyId { get; private set; }
	public NumberProvisioningState ProvisioningState { get; private set; }
	public string? TelnyxStatusRaw { get; private set; }
	public string? TelnyxNumberOrderId { get; private set; }
	public string? TelnyxPhoneNumberId { get; private set; }
	public string? RegRequirementGroupId { get; private set; }
	public string? RegAddressId { get; private set; }
	public string? RegSupplementalAddressId { get; private set; }
	public string? RegProofOfAddressDocId { get; private set; }
	public string? RegIdDocumentCopyDocId { get; private set; }
	public string? RegCompanyRegCertDocId { get; private set; }
	public ItCustomerType? RegItCustomerType { get; private set; }
	public string? RegEndUserName { get; private set; }
	public IdentityDocType? RegIdentityDocType { get; private set; }
	public byte[]? RegIdentityDocNumberEncrypted { get; private set; }
	public string? RegIdentityDocIssuer { get; private set; }
	public DateOnly? RegIdentityDocExpiry { get; private set; }
	public byte[]? RegDateOfBirthEncrypted { get; private set; }
	public byte[]? RegPlaceOfBirthEncrypted { get; private set; }
	public byte[]? RegPersonalTaxCodeEncrypted { get; private set; }
	public string? RegCompanyTaxCode { get; private set; }
	public string? RegVatNumber { get; private set; }
	public DateTime? SubmittedAt { get; private set; }
	public DateTime? ActivatedAt { get; private set; }
	public DateTime? ReleasedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public SwitchboardConfig Config { get; private set; } = null!;

	private VoiceNumberRegistration() { }   // unico costruttore, per il materializzatore di EF Core
}
