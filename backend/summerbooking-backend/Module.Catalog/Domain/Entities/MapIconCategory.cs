using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Lookup globale/di piattaforma: le categorie in cui sono raggruppate le icone della mappa spiaggia.</summary>
public sealed class MapIconCategory : IAuditable, ISoftDeletable
{
	public short Id { get; private set; }
	public MapIconCategoryCode Code { get; private set; }
	public bool IsActive { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MapIconCategory() { }
}
