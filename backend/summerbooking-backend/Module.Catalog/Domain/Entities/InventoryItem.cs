using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Articolo fisico a corredo (ombrellone, lettino, tenda, sdraio, …) di cui uno stabilimento tiene la
/// quantità disponibile. Il consumo per data non vive qui (calcolato contro C1 resource_consumptions).
/// </summary>
public sealed class InventoryItem : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public string Name { get; private set; } = null!;
	public int TotalQuantity { get; private set; }
	public int? LowStockThreshold { get; private set; }
	public bool IsActive { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private InventoryItem() { }
}
