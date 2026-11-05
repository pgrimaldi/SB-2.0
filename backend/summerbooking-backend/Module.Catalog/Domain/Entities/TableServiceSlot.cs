using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>Fascia oraria prenotabile del servizio tavoli; senza giorno vale per tutti i giorni del periodo.</summary>
public sealed class TableServiceSlot : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long AvailabilityId { get; private set; }
	public Weekday? Weekday { get; private set; }
	public TimeOnly From { get; private set; }
	public TimeOnly To { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private TableServiceSlot() { }

	internal static TableServiceSlot Create(Weekday? weekday, TimeOnly from, TimeOnly to) => new() { Weekday = weekday, From = from, To = to };
}
