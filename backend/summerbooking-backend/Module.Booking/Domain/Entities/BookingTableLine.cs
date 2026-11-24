using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Un tavolo prenotato (pranzo, cena): servizio tavoli di Catalog, giorno, fascia, coperti e tavoli assegnati. Dentro una
/// prenotazione di postazioni è il tavolo legato all'ombrellone; da solo è una prenotazione di soli servizi.
/// </summary>
public sealed class BookingTableLine : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long ReservationId { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid TableServiceId { get; private set; }
	public string TableServiceNameSnapshot { get; private set; } = null!;
	public DateOnly ServiceDate { get; private set; }
	public TimeOnly SlotStart { get; private set; }
	public TimeOnly SlotEnd { get; private set; }
	public short Covers { get; private set; }
	/// <summary>Posti di ciascun tavolo assegnato (es. un tavolo da 4 = [4]); vuoto con la capienza a posti o senza tavolo libero.</summary>
	public short[] TableSeats { get; private set; } = [];
	public decimal DepositDue { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;

	private BookingTableLine() { }

	public static BookingTableLine Create(
		Reservation reservation, Guid tableServiceId, string name, DateOnly date, TimeOnly start, TimeOnly end, short covers,
		IReadOnlyCollection<short> tableSeats, decimal depositDue)
	{
		if (covers < 1)
			throw new ArgumentOutOfRangeException(nameof(covers));
		return new BookingTableLine
		{
			PublicId = Guid.CreateVersion7(),
			Reservation = reservation,
			PropertyId = reservation.PropertyId,
			TableServiceId = tableServiceId,
			TableServiceNameSnapshot = name,
			ServiceDate = date,
			SlotStart = start,
			SlotEnd = end,
			Covers = covers,
			TableSeats = tableSeats.Order().ToArray(),
			DepositDue = depositDue,
		};
	}
}
