using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Elemento spiaggia del catalogo di piattaforma (passerella, doccia, bar, torretta…): solo grafico, il gestore lo
/// sceglie nella configurazione della mappa e il systemadmin ne aggiunge di nuovi.
/// </summary>
public sealed class MapBeachElement : IAuditable, ISoftDeletable
{
	public const int NameMaxLength = 80;

	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public string Name { get; private set; } = null!;
	public long? MediaAssetId { get; private set; }
	public MediaAsset? MediaAsset { get; private set; }
	public short SortOrder { get; private set; }
	public bool IsActive { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MapBeachElement() { }

	public static MapBeachElement Create(string name, short sortOrder, long? mediaAssetId)
	{
		var element = new MapBeachElement { PublicId = Guid.CreateVersion7(), SortOrder = sortOrder, IsActive = true };
		element.Update(name, mediaAssetId, true);
		return element;
	}

	public void Reorder(short sortOrder) => SortOrder = sortOrder;

	public void Update(string name, long? mediaAssetId, bool isActive)
	{
		var trimmed = name.Trim();
		if (trimmed.Length is 0 or > NameMaxLength)
			throw new ArgumentException("Nome dell'elemento vuoto o troppo lungo.", nameof(name));
		Name = trimmed;
		MediaAssetId = mediaAssetId;
		IsActive = isActive;
	}
}
