using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Settore/fila in cui è divisa la spiaggia di uno stabilimento. PublicId è referenziato cross-modulo da
/// Booking (booking_lines.sector_id, booking_resource_availability_daily.sector_id).
/// </summary>
public sealed class MapSector : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public string Name { get; private set; } = null!;
	public short SortOrder { get; private set; }
	public Guid? CreatedByUserPublicId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MapSector() { }

	public const int NameMaxLength = 100;

	public static MapSector Create(long propertyId, string name, short sortOrder, Guid? createdByUserPublicId)
	{
		var sector = new MapSector
		{
			PublicId = Guid.CreateVersion7(),
			PropertyId = propertyId,
			SortOrder = sortOrder,
			CreatedByUserPublicId = createdByUserPublicId,
		};
		sector.Rename(name);
		return sector;
	}

	public void Rename(string name)
	{
		var trimmed = name.Trim();
		if (trimmed.Length is 0 or > NameMaxLength)
			throw new ArgumentException("Nome del settore vuoto o troppo lungo.", nameof(name));
		Name = trimmed;
	}
}
