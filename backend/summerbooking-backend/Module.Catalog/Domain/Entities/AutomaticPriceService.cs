using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Servizio a cui si applica il prezzo automatico.</summary>
public sealed class AutomaticPriceService : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long AutomaticPriceId { get; private set; }
	public long SaleItemId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AutomaticPriceService() { }

	internal static AutomaticPriceService Create(long saleItemId) => new() { SaleItemId = saleItemId };
}
