using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Una foto della galleria pubblica di una vetrina, in posizione SortOrder. Sostituisce l'array JSON
/// legacy bathouses.photos.
/// </summary>
public sealed class BathhousePhoto : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long BathhouseId { get; private set; }
	public Bathhouse? Bathhouse { get; private set; }
	public long MediaAssetId { get; private set; }
	public MediaAsset? MediaAsset { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private BathhousePhoto() { }
}
