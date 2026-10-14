using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Anagrafica delle postazioni (ombrelloni, lettini, cabine, parcheggi, ...) di uno stabilimento: una
/// riga per postazione. È l'anagrafica delle risorse prenotabili referenziate da C1 come resource_id.
/// </summary>
public sealed class MapSpot : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public long? SectorId { get; private set; }
	public MapSector? Sector { get; private set; }
	public string Label { get; private set; } = null!;
	public SpotType SpotType { get; private set; }
	public bool IsArea { get; private set; }
	public short? AreaSeatCount { get; private set; }
	public short? GridRow { get; private set; }
	public short? GridColumn { get; private set; }
	public bool IsBookable { get; private set; }
	public Guid? CreatedByUserPublicId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MapSpot() { }
}
