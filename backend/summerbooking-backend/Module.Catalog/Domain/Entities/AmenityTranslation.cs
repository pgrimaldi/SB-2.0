using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Nome di una dotazione (<see cref="Amenity"/>) in una specifica culture. Entità figlia, una riga per
/// (dotazione × lingua).
/// </summary>
public sealed class AmenityTranslation : IAuditable, ISoftDeletable
{
	public int Id { get; private set; }
	public int AmenityId { get; private set; }
	public string Culture { get; private set; } = null!;
	public string Name { get; private set; } = null!;
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AmenityTranslation() { }
}
