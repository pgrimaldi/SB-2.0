using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Settore a cui un voucher (<see cref="Discount"/>) è ristretto; assenza di righe per un
/// <see cref="DiscountId"/> significa "nessuna restrizione di settore".
/// </summary>
public sealed class DiscountSector : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long DiscountId { get; private set; }
	public Discount? Discount { get; private set; }
	public long SectorId { get; private set; }
	public MapSector? Sector { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private DiscountSector() { }
}
