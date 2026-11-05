using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Fila/settore in cui il servizio è acquistabile, quando il servizio non vale per tutte.</summary>
public sealed class ServiceSaleItemSector : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long SaleItemId { get; private set; }
	public long SectorId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ServiceSaleItemSector() { }

	internal static ServiceSaleItemSector Create(long sectorId) => new() { SectorId = sectorId };
}
