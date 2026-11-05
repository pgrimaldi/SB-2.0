using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Variante di un servizio bar (es. «Crema», +0,50 €): il prezzo si somma a quello del servizio.</summary>
public sealed class ServiceSaleItemVariant : IAuditable, ISoftDeletable
{
	public const int NameMaxLength = 120;

	public long Id { get; private set; }
	public long SaleItemId { get; private set; }
	public string Name { get; private set; } = null!;
	public decimal Price { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ServiceSaleItemVariant() { }

	internal static ServiceSaleItemVariant Create(string name, decimal price, short sortOrder) => new()
	{
		Name = name.Trim(),
		Price = price,
		SortOrder = sortOrder,
	};
}
