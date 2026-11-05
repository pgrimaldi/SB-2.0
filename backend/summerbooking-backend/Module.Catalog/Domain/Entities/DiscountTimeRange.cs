using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Fascia oraria di validità di uno sconto.</summary>
public sealed class DiscountTimeRange : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long DiscountId { get; private set; }
	public TimeOnly From { get; private set; }
	public TimeOnly To { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private DiscountTimeRange() { }

	internal static DiscountTimeRange Create(TimeOnly from, TimeOnly to) => new() { From = from, To = to };
}
