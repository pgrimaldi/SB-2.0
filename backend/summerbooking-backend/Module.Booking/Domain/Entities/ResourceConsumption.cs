using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Una riga per (risorsa × giorno occupato × sorgente): unica fonte di verità della disponibilità di
/// postazioni, articoli, servizi e slot orari.
/// </summary>
public sealed class ResourceConsumption : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public ResourceType ResourceType { get; private set; }
	public Guid? ResourceId { get; private set; }
	public string? ResourceNameSnapshot { get; private set; }
	public DateOnly ConsumptionDate { get; private set; }
	public DayPeriod BookingPeriod { get; private set; }
	public TimeOnly? SlotStart { get; private set; }
	public TimeOnly? SlotEnd { get; private set; }
	public Guid? TimeSlotId { get; private set; }
	public int Quantity { get; private set; }
	public ConsumptionState State { get; private set; }
	public DateTime? HeldUntil { get; private set; }
	public ConsumptionSource SourceKind { get; private set; }
	public long? SourceId { get; private set; }
	public string? BookingNumber { get; private set; }
	public BookingChannel? BookingChannel { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ResourceConsumption() { }
}
