using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Catalogo master (globale, nessun property_id) delle dotazioni/servizi che uno stabilimento può
/// dichiarare di offrire (es. Wi-Fi, Doccia, Bar). Aggregate root minimale, possiede le traduzioni
/// (<see cref="AmenityTranslation"/>).
/// </summary>
public sealed class Amenity : IAuditable, ISoftDeletable
{
	public int Id { get; private set; }
	public string Name { get; private set; } = null!;
	public short? CategoryId { get; private set; }
	public long? IconAssetId { get; private set; }
	public bool IsActive { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<AmenityTranslation> Translations { get; private set; } = new List<AmenityTranslation>();

	private Amenity() { }
}
