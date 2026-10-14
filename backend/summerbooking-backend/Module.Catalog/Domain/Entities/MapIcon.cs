using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Catalogo globale/master delle icone/immagini per comporre la mappa spiaggia: sfondi, varianti
/// ombrellone/cabina/tenda/lettino nei due stati occupied/free.
/// </summary>
public sealed class MapIcon : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public short CategoryId { get; private set; }
	public MapIconCategory? Category { get; private set; }
	public MapIconState Type { get; private set; }
	public string Name { get; private set; } = null!;
	public long MediaAssetId { get; private set; }
	public MediaAsset? MediaAsset { get; private set; }
	public bool IsActive { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MapIcon() { }
}
