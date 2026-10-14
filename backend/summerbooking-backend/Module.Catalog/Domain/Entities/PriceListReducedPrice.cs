using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Prezzo ridotto assoluto per settore, valido per una finestra di date con match esatto. Entità figlia di
/// secondo livello, appesa a <see cref="PriceListReducedWindow"/>.
/// </summary>
public sealed class PriceListReducedPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long WindowId { get; private set; }
	public long SectorId { get; private set; }
	public decimal Price { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private PriceListReducedPrice() { }
}
