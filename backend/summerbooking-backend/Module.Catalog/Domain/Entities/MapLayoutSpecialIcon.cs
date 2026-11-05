using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Icona di un articolo personalizzato (1-8) per stato occupato/libero.
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

	public const short MaxVariant = 8;

	private MapLayoutSpecialIcon() { }

	internal static MapLayoutSpecialIcon Create(short variant, MapIconState state, long mediaAssetId) => new()
	{
		Variant = variant,
		State = state,
		MediaAssetId = mediaAssetId,
	};

	internal void SetAsset(long mediaAssetId) => MediaAssetId = mediaAssetId;
}
