using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Icona di una variante speciale di ombrellone (1-5) per stato occupato/libero. Sostituisce le colonne
/// specialUmbrella1..5[_free] di configure_maps.
/// </summary>
public sealed class MapLayoutSpecialIcon : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long LayoutId { get; private set; }
	public MapLayout? Layout { get; private set; }
	public short Variant { get; private set; }
	public MapIconState State { get; private set; }
	public long MediaAssetId { get; private set; }
	public MediaAsset? MediaAsset { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MapLayoutSpecialIcon() { }
}
