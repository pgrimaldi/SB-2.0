using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Anagrafica gestionale/di piattaforma dello stabilimento (tenant): stato di approvazione, contatti
/// amministrativi, titolare. Id è il discriminatore multi-tenant copiato per valore nei moduli a valle.
/// </summary>
public sealed class Property : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public string Name { get; private set; } = null!;
	public PropertyStatus Status { get; private set; }
	public string? AdminComment { get; private set; }
	public Guid? OwnerUserPublicId { get; private set; }
	public string? ContactFullName { get; private set; }
	public string? ContactEmail { get; private set; }
	public string? ContactPhone { get; private set; }
	public string? ContactPhoneCountryCode { get; private set; }
	public string? City { get; private set; }
	public short? CountryId { get; private set; }
	public Country? Country { get; private set; }
	public bool IsOnlineSalesSuspended { get; private set; }
	public Guid? ContractDocumentId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<MapLegendItem> MapLegendItems { get; private set; } = new List<MapLegendItem>();

	private Property() { }
}
