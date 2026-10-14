using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Ospite di una prenotazione: persona presente ma non intestataria, senza addebito/comunicazioni a suo carico.
/// </summary>
public sealed class BookingGuest : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long ReservationId { get; private set; }
	public Guid? CustomerId { get; private set; }
	public string FullNameSnapshot { get; private set; } = null!;
	public long? BookingLineId { get; private set; }
	public DateTime? CheckedInAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;
	public BookingLine? BookingLine { get; private set; }

	private BookingGuest() { }
}
