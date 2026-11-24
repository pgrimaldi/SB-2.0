using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// «Segnala ingresso»: il cliente è entrato oggi su questa postazione. Vale per il giorno (si «azzera» da solo a
/// mezzanotte perché il giorno dopo non c'è la riga); lo toglie il gestore, con traccia nel registro.
/// </summary>
public sealed class BookingEntrance : IAuditable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public long BookingLineId { get; private set; }
	public DateOnly EntryDate { get; private set; }
	public DateTime EnteredAt { get; private set; }
	public Guid? OperatorUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	public BookingLine BookingLine { get; private set; } = null!;

	private BookingEntrance() { }

	public static BookingEntrance Create(BookingLine line, DateOnly day, DateTime now, Guid? userId)
	{
		if (!line.Covers(day))
			throw new ArgumentOutOfRangeException(nameof(day));
		return new BookingEntrance
		{
			PropertyId = line.PropertyId,
			BookingLine = line,
			EntryDate = day,
			EnteredAt = now,
			OperatorUserId = userId,
		};
	}
}
