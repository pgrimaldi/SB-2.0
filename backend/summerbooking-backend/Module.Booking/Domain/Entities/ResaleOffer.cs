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
	public Guid PublicId { get; private set; }
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
	/// <summary>Assenza con «ricalcola prezzo»: il giorno non si paga (il preventivo lo salta).</summary>
	public bool ExcludesFromPrice { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public Reservation SourceReservation { get; private set; } = null!;
	public BookingLine? BookingLine { get; private set; }
	public Reservation? TakenByReservation { get; private set; }

	private ResaleOffer() { }

	/// <summary>Un giorno (o mezza giornata) ceduto dal titolare: la postazione torna prenotabile.</summary>
	public static ResaleOffer Create(ResaleOrigin origin, Reservation reservation, BookingLine line, DateOnly day, DayPeriod period, bool excludesFromPrice)
	{
		if (!line.Covers(day))
			throw new ArgumentOutOfRangeException(nameof(day));
		return new ResaleOffer
		{
			PublicId = Guid.CreateVersion7(),
			PropertyId = reservation.PropertyId,
			Origin = origin,
			SourceReservation = reservation,
			BookingLine = line,
			ResourceId = line.SpotId!.Value,
			OfferDate = day,
			OfferedPeriod = period,
			OfferState = ResaleOfferState.Available,
			ExcludesFromPrice = excludesFromPrice,
		};
	}

	public bool IsLive => DeletedAt is null && OfferState != ResaleOfferState.Withdrawn;

	/// <summary>Il titolare matura il credito se il giorno viene ripreso (sharing, o assenza gestita come sharing).</summary>
	public bool EarnsCredit => Origin is ResaleOrigin.Sharing or ResaleOrigin.AbsenceAsSharing;

	/// <summary>Ritirata dal titolare: possibile solo finché nessuno l'ha ripresa.</summary>
	public void Withdraw()
	{
		if (OfferState != ResaleOfferState.Available)
			throw new InvalidOperationException("Offerta già ripresa.");
		OfferState = ResaleOfferState.Withdrawn;
	}

	/// <summary>Ripresa da un'altra prenotazione; se ne ha presa solo mezza, resta libera l'altra metà.</summary>
	public void Reassign(Reservation taker, DayPeriod takenPeriod, DateTime now)
	{
		OfferState = ResaleOfferState.Reassigned;
		TakenByReservation = taker;
		TakenAt = now;
		RemainingPeriod = OfferedPeriod == DayPeriod.FullDay && takenPeriod != DayPeriod.FullDay
			? (takenPeriod == DayPeriod.Morning ? DayPeriod.Afternoon : DayPeriod.Morning)
			: null;
	}

	/// <summary>Chi l'aveva ripresa ha tolto quel giorno: torna disponibile.</summary>
	public void ReturnToAvailable()
	{
		OfferState = ResaleOfferState.Available;
		TakenByReservation = null;
		TakenByReservationId = null;
		TakenAt = null;
		RemainingPeriod = null;
		CreditSettled = false;
	}

	public void MarkCreditSettled() => CreditSettled = true;
}
