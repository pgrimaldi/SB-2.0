using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Settore/fila in cui è divisa la spiaggia di uno stabilimento. PublicId è referenziato cross-modulo da
/// C1 (booking_lines.sector_id, booking_resource_availability_daily.sector_id).
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
}
