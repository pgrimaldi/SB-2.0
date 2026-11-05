using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Fascia oraria di un periodo di validità del prezzo automatico.</summary>
public sealed class AutomaticPriceTimeRange : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PeriodId { get; private set; }
	public TimeOnly From { get; private set; }
	public TimeOnly To { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AutomaticPriceTimeRange() { }

	internal static AutomaticPriceTimeRange Create(TimeOnly from, TimeOnly to) => new() { From = from, To = to };
}
