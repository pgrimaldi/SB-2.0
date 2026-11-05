using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Fila/settore in cui vale il prezzo automatico.</summary>
public sealed class AutomaticPriceSector : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long AutomaticPriceId { get; private set; }
	public long SectorId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AutomaticPriceSector() { }

	internal static AutomaticPriceSector Create(long sectorId) => new() { SectorId = sectorId };
}
