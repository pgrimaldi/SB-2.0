using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Struttura partner ("convenzione"/hotel) con cui uno stabilimento ha un accordo commerciale. Aggregate
/// root dell'area Agreement, radice della gerarchia listini/voucher partner.
/// </summary>
public sealed class AgreementPartner : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public string Name { get; private set; } = null!;
	public string? City { get; private set; }
	public string? PostalCode { get; private set; }
	public string? Province { get; private set; }
	public string? StreetName { get; private set; }
	public string? StreetNumber { get; private set; }
	public string? Phone { get; private set; }
	public string? Email { get; private set; }
	public string? Note { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<AgreementPriceList> PriceLists { get; private set; } = new List<AgreementPriceList>();
	public ICollection<AgreementReport> Reports { get; private set; } = new List<AgreementReport>();

	private AgreementPartner() { }
}
