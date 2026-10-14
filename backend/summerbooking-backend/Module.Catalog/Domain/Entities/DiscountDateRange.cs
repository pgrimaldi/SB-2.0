using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Finestra di validità (una fra eventualmente più) di un voucher sconto. Entità figlia dell'aggregate
/// <see cref="Discount"/>.
/// </summary>
public sealed class DiscountDateRange : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long DiscountId { get; private set; }
	public DateOnly StartsOn { get; private set; }
	public DateOnly EndsOn { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private DiscountDateRange() { }
}
