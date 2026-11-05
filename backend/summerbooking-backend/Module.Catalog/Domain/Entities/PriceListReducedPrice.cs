using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Prezzo del «stagionale ridotto» <see cref="ReducedSeason"/> per un pacchetto e una fila.</summary>
public sealed class PriceListReducedPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public long ReducedSeasonId { get; private set; }
	public long SectorId { get; private set; }
	public decimal Price { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private PriceListReducedPrice() { }

	internal static PriceListReducedPrice Create(long reducedSeasonId, long sectorId, decimal price)
		=> new() { ReducedSeasonId = reducedSeasonId, SectorId = sectorId, Price = price };

	internal void SetPrice(decimal price) => Price = price;
}
