using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Giorno marcato dall'operatore per un'azione rapida del planner su questa prenotazione.
/// </summary>
public sealed class BookingQuickActionDate : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long ReservationId { get; private set; }
	public Guid PropertyId { get; private set; }
	public DateOnly ActionDate { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;

	private BookingQuickActionDate() { }
}
