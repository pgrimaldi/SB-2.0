using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Slot orario prenotato per una riga servizio hourly: giorno, fascia e posti.
/// </summary>
public sealed class BookingServiceTimeSlot : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long ServiceLineId { get; private set; }
	public long ReservationId { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid? TimeSlotId { get; private set; }
	public TimeOnly SlotStart { get; private set; }
	public TimeOnly SlotEnd { get; private set; }
	public string? SlotLabelSnapshot { get; private set; }
	public DateOnly ServiceDate { get; private set; }
	public short Seats { get; private set; }
	public decimal? SlotPrice { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public BookingServiceLine ServiceLine { get; private set; } = null!;
	public Reservation Reservation { get; private set; } = null!;

	private BookingServiceTimeSlot() { }

	internal static BookingServiceTimeSlot Create(
		BookingServiceLine line, Guid? timeSlotId, TimeOnly start, TimeOnly end, DateOnly date, short seats, string? label, decimal? price)
		=> new()
		{
			ServiceLine = line,
			Reservation = line.Reservation,
			PropertyId = line.PropertyId,
			TimeSlotId = timeSlotId,
			SlotStart = start,
			SlotEnd = end,
			ServiceDate = date,
			Seats = seats,
			SlotLabelSnapshot = label,
			SlotPrice = price,
		};
}
