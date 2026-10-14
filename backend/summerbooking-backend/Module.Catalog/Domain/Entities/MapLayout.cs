using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Layout grafico della mappa spiaggia di uno stabilimento (1:1 con Property): griglia, sfondo, colore,
/// icone per tipo elemento nei due stati. Lo stato cella-per-cella vive in MapSpot.
/// </summary>
public sealed class MapLayout : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public short GridRows { get; private set; }
	public short GridColumns { get; private set; }
	public bool IsCustomTemplate { get; private set; }
	public long? TemplateAssetId { get; private set; }
	public MediaAsset? TemplateAsset { get; private set; }
	public string? BackgroundColor { get; private set; }
	public long? UmbrellaOccupiedAssetId { get; private set; }
	public MediaAsset? UmbrellaOccupiedAsset { get; private set; }
	public long? UmbrellaFreeAssetId { get; private set; }
	public MediaAsset? UmbrellaFreeAsset { get; private set; }
	public Guid? CreatedByUserPublicId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<MapLayoutSpecialIcon> SpecialIcons { get; private set; } = new List<MapLayoutSpecialIcon>();

	private MapLayout() { }
}
