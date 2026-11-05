using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Articolo fisico a corredo (ombrellone, lettino, tenda, sdraio, …) di cui uno stabilimento tiene la quantità
/// totale. La disponibilità per data è la quantità meno i consumi delle prenotazioni (modulo Booking).
/// </summary>
public sealed class InventoryItem : IAuditable, ISoftDeletable
{
	public const int NameMaxLength = 120;

	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property? Property { get; private set; }
	public InventoryArticle? ArticleCode { get; private set; }
	public string Name { get; private set; } = null!;
	public int TotalQuantity { get; private set; }
	public int? LowStockThreshold { get; private set; }
	public bool LowStockAlertEnabled { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private InventoryItem() { }

	public static InventoryItem Create(long propertyId, InventoryItemData data, Guid? createdByUserId)
	{
		var item = new InventoryItem
		{
			PublicId = Guid.CreateVersion7(),
			PropertyId = propertyId,
			CreatedByUserId = createdByUserId,
		};
		item.Update(data);
		return item;
	}

	public void Update(InventoryItemData data)
	{
		var name = data.Name.Trim();
		if (name.Length is 0 or > NameMaxLength)
			throw new ArgumentException("Nome dell'articolo vuoto o troppo lungo.", nameof(data));
		if (data.TotalQuantity < 0)
			throw new ArgumentException("Quantità negativa.", nameof(data));
		if (data.LowStockThreshold is { } threshold && (threshold < 0 || threshold > data.TotalQuantity))
			throw new ArgumentException("Soglia fuori dall'intervallo 0..quantità.", nameof(data));

		ArticleCode = data.ArticleCode;
		Name = name;
		TotalQuantity = data.TotalQuantity;
		LowStockThreshold = data.LowStockThreshold;
		LowStockAlertEnabled = data.LowStockAlertEnabled;
	}

	/// <summary>Le quantità sotto la soglia fanno scattare l'avviso solo se è attivo e la soglia è impostata.</summary>
	public bool IsBelowThreshold(int available)
		=> LowStockAlertEnabled && LowStockThreshold is { } threshold && available < threshold;
}

/// <summary>Dati modificabili di un articolo; la validazione dei campi è in Application.</summary>
public sealed record InventoryItemData(
	InventoryArticle? ArticleCode, string Name, int TotalQuantity, int? LowStockThreshold, bool LowStockAlertEnabled);
