using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Una foto della galleria pubblica di una vetrina, in posizione SortOrder.
/// </summary>
public sealed class BathhousePhoto : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long BathhouseId { get; private set; }
	public Bathhouse? Bathhouse { get; private set; }
	public long MediaAssetId { get; private set; }
	public MediaAsset? MediaAsset { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private BathhousePhoto() { }

	internal static BathhousePhoto Create(long mediaAssetId, short sortOrder) => new() { PublicId = Guid.CreateVersion7(), MediaAssetId = mediaAssetId, SortOrder = sortOrder };

	public void MoveTo(short sortOrder) => SortOrder = sortOrder;
}
