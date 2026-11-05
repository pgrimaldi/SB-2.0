using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Giunzione N:N: quali dotazioni/servizi (amenity) offre una vetrina.
/// </summary>
public sealed class BathhouseAmenity : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long BathhouseId { get; private set; }
	public Bathhouse? Bathhouse { get; private set; }
	public int AmenityId { get; private set; }
	public Amenity? Amenity { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private BathhouseAmenity() { }

	internal static BathhouseAmenity Create(int amenityId) => new() { AmenityId = amenityId };
}
