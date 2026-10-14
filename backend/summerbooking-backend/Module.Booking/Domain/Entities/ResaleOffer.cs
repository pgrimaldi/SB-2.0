using Module.Booking.Domain.Enums;
using Shared.Contracts;

namespace Module.Booking.Domain.Entities;

/// <summary>
/// Riga (riga prenotazione di origine × postazione × giorno ceduto): il titolare dichiara che non
/// userà la postazione quel giorno, che torna prenotabile (assenza pianificata o sharing).
/// </summary>
public sealed class ResaleOffer : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public ResaleOrigin Origin { get; private set; }
	public long SourceReservationId { get; private set; }
	public long? BookingLineId { get; private set; }
	public Guid ResourceId { get; private set; }
	public DateOnly OfferDate { get; private set; }
	public DayPeriod OfferedPeriod { get; private set; }
	public DayPeriod? RemainingPeriod { get; private set; }
	public ResaleOfferState OfferState { get; private set; }
	public long? TakenByReservationId { get; private set; }
	public DateTime? TakenAt { get; private set; }
	public bool CreditSettled { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation SourceReservation { get; private set; } = null!;
	public BookingLine? BookingLine { get; private set; }
	public Reservation? TakenByReservation { get; private set; }

	private ResaleOffer() { }
}
