using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Blocco di una postazione senza prenotazione (manutenzione, VIP, planner, o blocco temporaneo di
/// checkout online); nessun cliente, nessun pagamento.
/// </summary>
public sealed class SpotHold : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public Guid ResourceId { get; private set; }
	public string? SpotLabelSnapshot { get; private set; }
	public HoldKind HoldKind { get; private set; }
	public HoldStatus HoldStatus { get; private set; }
	public DateOnly StartDate { get; private set; }
	public DateOnly EndDate { get; private set; }
	public DayPeriod BookingPeriod { get; private set; }
	public DateTime? HeldUntil { get; private set; }
	public string? ReasonNote { get; private set; }
	public Guid? CreatedByUserId { get; private set; }
	public DateTime? ReleasedAt { get; private set; }
	public Guid? ReleasedByUserId { get; private set; }
	public long? ConvertedReservationId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation? ConvertedReservation { get; private set; }

	private SpotHold() { }
}
