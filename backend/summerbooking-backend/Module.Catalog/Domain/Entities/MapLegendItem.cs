using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Voce della legenda mostrata sulla mappa di prenotazione online del sito pubblico (es. "Ombrellone
/// libero", con icona). Dato di mappa per stabilimento, non agganciato al canale online.
/// </summary>
public sealed class MapLegendItem : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public string Label { get; private set; } = null!;
	public long? IconAssetId { get; private set; }
	public MediaAsset? IconAsset { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MapLegendItem() { }
}
