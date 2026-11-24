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

	/// <summary>Un giorno di una postazione occupato da una riga di prenotazione.</summary>
	public static ResourceConsumption ForLine(BookingLine line, Reservation reservation, ResourceType type, DateOnly day)
		=> new()
		{
			PropertyId = line.PropertyId,
			ResourceType = type,
			ResourceId = line.SpotId,
			ResourceNameSnapshot = line.SpotLabelSnapshot,
			ConsumptionDate = day,
			BookingPeriod = line.BookingPeriod,
			Quantity = line.Quantity,
			State = ConsumptionState.Confirmed,
			SourceKind = ConsumptionSource.ReservationLine,
			SourceId = line.Id,
			BookingNumber = reservation.BookingNumber,
			BookingChannel = reservation.Channel,
		};

	/// <summary>Un articolo di magazzino impegnato da una riga (pacchetto o aggiuntivo) in un giorno.</summary>
	public static ResourceConsumption ForItem(BookingLine line, Reservation reservation, Guid itemId, string name, DateOnly day, int quantity)
		=> new()
		{
			PropertyId = line.PropertyId,
			ResourceType = ResourceType.InventoryItem,
			ResourceId = itemId,
			ResourceNameSnapshot = name.Length > 160 ? name[..160] : name,
			ConsumptionDate = day,
			BookingPeriod = line.BookingPeriod,
			Quantity = quantity,
			State = ConsumptionState.Confirmed,
			SourceKind = ConsumptionSource.ReservationLine,
			SourceId = line.Id,
			BookingNumber = reservation.BookingNumber,
			BookingChannel = reservation.Channel,
		};

	/// <summary>Un giorno di una postazione riservata senza cliente.</summary>
	public static ResourceConsumption ForHold(SpotHold hold, ResourceType type, DateOnly day)
		=> new()
		{
			PropertyId = hold.PropertyId,
			ResourceType = type,
			ResourceId = hold.ResourceId,
			ResourceNameSnapshot = hold.SpotLabelSnapshot,
			ConsumptionDate = day,
			BookingPeriod = hold.BookingPeriod,
			Quantity = 1,
			State = ConsumptionState.Confirmed,
			SourceKind = ConsumptionSource.ManualHold,
			SourceId = hold.Id,
		};

	/// <summary>Un servizio dell'ordine nel suo giorno (e, per gli orari, nella sua fascia): conta per i limiti del servizio.</summary>
	public static ResourceConsumption ForService(BookingServiceLine line, Reservation reservation, ResourceType type, DateOnly day, TimeOnly? start, TimeOnly? end)
		=> new()
		{
			PropertyId = line.PropertyId,
			ResourceType = type,
			ResourceId = line.SaleItemId,
			ResourceNameSnapshot = line.NameSnapshot.Length > 160 ? line.NameSnapshot[..160] : line.NameSnapshot,
			ConsumptionDate = day,
			BookingPeriod = DayPeriod.FullDay,
			SlotStart = start,
			SlotEnd = end,
			Quantity = line.Quantity,
			State = ConsumptionState.Confirmed,
			SourceKind = ConsumptionSource.ServiceLine,
			SourceId = line.Id,
			BookingNumber = reservation.BookingNumber,
			BookingChannel = reservation.Channel,
		};

	/// <summary>Il giorno torna libero (uscita anticipata, annullamento, riserva tolta).</summary>
	public void Release() => State = ConsumptionState.Released;

	public void Reactivate()
	{
		State = ConsumptionState.Confirmed;
		HeldUntil = null;
	}

	/// <summary>La riga è stata ricollegata a una postazione nuova (dopo «Reimposta mappa»).</summary>
	public void MoveTo(Guid spotId, string? label)
	{
		ResourceId = spotId;
		ResourceNameSnapshot = label;
	}

	/// <summary>Tenuto (prenotazione online da confermare o da pagare): occupa come un consumo confermato.</summary>
	public void Hold(DateTime? until)
	{
		State = ConsumptionState.Held;
		HeldUntil = until;
	}

	/// <summary>Mezza giornata ceduta: il consumo resta solo sulla metà che il titolare tiene.</summary>
	public void KeepOnly(DayPeriod period) => BookingPeriod = period;
}
