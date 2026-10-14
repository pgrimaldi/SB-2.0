using Module.Booking.Domain.Enums;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Proiezione di disponibilità per (risorsa/tipo × giorno), mantenuta dal DB via trigger su
/// booking_resource_consumptions: sostituisce il GROUP BY a runtime del legacy. Nessun ciclo di vita
/// applicativo da tracciare: non implementa ISoftDeletable/IAuditable, i timestamp sono mappati solo
/// per uniformità di schema.
/// </summary>
public sealed class ResourceAvailabilityDaily
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public ResourceType ResourceType { get; private set; }
	public Guid? ResourceId { get; private set; }
	public Guid? SectorId { get; private set; }
	public DateOnly ConsumptionDate { get; private set; }
	public DayPeriod BookingPeriod { get; private set; }
	public int BookedQty { get; private set; }
	public int HeldQty { get; private set; }
	public int? TotalQty { get; private set; }
	public int? AvailableQty { get; private set; }
	public DateTime RebuiltAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private ResourceAvailabilityDaily() { }
}
