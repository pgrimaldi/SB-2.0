using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Concessione di visibilità: account consumer che ha agganciato la prenotazione col reservation_code.
/// </summary>
public sealed class BookingParticipant : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long ReservationId { get; private set; }
	public Guid UserId { get; private set; }
	public ParticipantRole Role { get; private set; }
	public DateTime LinkedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation Reservation { get; private set; } = null!;

	private BookingParticipant() { }
}
