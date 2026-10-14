using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Categoria di raggruppamento delle dotazioni (<see cref="Amenity"/>) nella ricerca pubblica. Lookup
/// globale/di piattaforma, nessun property_id.
/// </summary>
public sealed class AmenityCategory : IAuditable, ISoftDeletable
{
	public short Id { get; private set; }
	public string Code { get; private set; } = null!;
	public bool IsActive { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AmenityCategory() { }
}
