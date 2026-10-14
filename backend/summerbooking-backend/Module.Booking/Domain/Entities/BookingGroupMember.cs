using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Giunzione gruppo × prenotazione: una prenotazione appartiene ad al più un gruppo.
/// </summary>
public sealed class BookingGroupMember : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long GroupId { get; private set; }
	public long ReservationId { get; private set; }
	public Guid PropertyId { get; private set; }
	public DateTime AddedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public BookingGroup Group { get; private set; } = null!;
	public Reservation Reservation { get; private set; } = null!;

	private BookingGroupMember() { }
}
