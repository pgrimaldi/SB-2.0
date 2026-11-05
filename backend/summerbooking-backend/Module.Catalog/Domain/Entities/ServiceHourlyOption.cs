using Module.Catalog.Domain.Enums;
using Shared.Contracts;

namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Disponibilità di un servizio orario: in quale fascia del giorno e in quali giorni si prenota, con quali durate e a che
/// prezzo, IVA e massimo di prenotazioni per fascia. Appartiene ai prezzi del servizio (per periodo tariffario, o senza
/// periodo) oppure a un'opzione di prenotazione con date.
/// </summary>
public sealed class ServiceHourlyOption : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long SaleItemId { get; private set; }
	/// <summary>Periodo tariffario dei prezzi; null = vale per tutta la stagione (o è legata a un'opzione di prenotazione).</summary>
	public short? PeriodGroupKey { get; private set; }
	public long? BookingOptionId { get; private set; }
	public ServiceBookingOption? BookingOption { get; private set; }
	public TimeOnly? TimeFrom { get; private set; }
	public TimeOnly? TimeTo { get; private set; }
	public Weekday[] Weekdays { get; private set; } = [];
	public decimal? VatRate { get; private set; }
	public short? MaxPerSlot { get; private set; }
	public bool HasFixedTimeSlots { get; private set; }
	public short SortOrder { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public ICollection<ServiceSaleItemTimeSlot> Slots { get; private set; } = new List<ServiceSaleItemTimeSlot>();

	private ServiceHourlyOption() { }

	internal static ServiceHourlyOption Create(HourlyOptionData d, short? periodKey, ServiceBookingOption? bookingOption, short sortOrder) => new()
	{
		PublicId = Guid.CreateVersion7(),
		PeriodGroupKey = bookingOption is null ? periodKey : null,
		BookingOption = bookingOption,
		TimeFrom = d.TimeFrom,
		TimeTo = d.TimeTo,
		Weekdays = d.Weekdays.Distinct().Order().ToArray(),
		VatRate = d.VatRate,
		MaxPerSlot = d.MaxPerSlot,
		HasFixedTimeSlots = d.HasFixedTimeSlots,
		SortOrder = sortOrder,
	};
}

/// <param name="Slots">Durate standard; la durata personalizzata è <paramref name="Custom"/>.</param>
public sealed record HourlyOptionData(
	TimeOnly? TimeFrom, TimeOnly? TimeTo, IReadOnlyList<Weekday> Weekdays, IReadOnlyList<HourlySlotData> Slots, HourlySlotData? Custom,
	decimal? VatRate, short? MaxPerSlot, bool HasFixedTimeSlots);

public sealed record HourlySlotData(short? DurationMinutes, bool IsEnabled, decimal? Price);
