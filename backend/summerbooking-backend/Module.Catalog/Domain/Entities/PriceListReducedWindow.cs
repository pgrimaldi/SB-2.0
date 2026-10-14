using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Finestra di date che, se coincide esattamente con quella prenotata, attiva i prezzi ridotti per
/// settore. Entità figlia dell'aggregate <see cref="PriceList"/>.
/// </summary>
public sealed class PriceListReducedWindow : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public DateOnly StartsOn { get; private set; }
	public DateOnly EndsOn { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<PriceListReducedPrice> SectorPrices { get; private set; } = new List<PriceListReducedPrice>();

	private PriceListReducedWindow() { }
}
