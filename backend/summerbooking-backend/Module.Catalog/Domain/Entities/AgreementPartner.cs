using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Struttura convenzionata con cui uno stabilimento ha un accordo commerciale. Aggregate
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

	public const int NameMaxLength = 255;

	private AgreementPartner() { }

	public static AgreementPartner Create(long propertyId) => new() { PublicId = Guid.CreateVersion7(), PropertyId = propertyId };

	public void Update(AgreementPartnerData d)
	{
		Name = d.Name.Trim();
		City = d.City;
		PostalCode = d.PostalCode;
		Province = d.Province;
		StreetName = d.StreetName;
		StreetNumber = d.StreetNumber;
		Phone = d.Phone;
		Email = d.Email;
		Note = d.Note;
	}
}
