using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Periodo di disponibilità del servizio tavoli con le sue fasce orarie.</summary>
public sealed class TableServiceAvailability : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long TableServiceId { get; private set; }
	public DateOnly StartsOn { get; private set; }
	public DateOnly EndsOn { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<TableServiceSlot> Slots { get; private set; } = new List<TableServiceSlot>();

	private TableServiceAvailability() { }

	internal static TableServiceAvailability Create(TableServiceAvailabilityData d)
	{
		var availability = new TableServiceAvailability { StartsOn = d.StartsOn, EndsOn = d.EndsOn };
		foreach (var (weekday, from, to) in d.Slots)
			availability.Slots.Add(TableServiceSlot.Create(weekday, from, to));
		return availability;
	}
}
