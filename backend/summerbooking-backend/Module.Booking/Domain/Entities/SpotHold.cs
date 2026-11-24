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
	public Guid PublicId { get; private set; }
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
	/// <summary>Cursore di sincronizzazione: cresce a ogni modifica della riserva.</summary>
	public long SyncSeq { get; private set; }

	public Reservation? ConvertedReservation { get; private set; }

	private SpotHold() { }

	/// <summary>Riserva del gestore, senza scadenza e senza motivo: la toglie chi prenota sopra.</summary>
	public static SpotHold CreateOperational(
		Guid propertyId, Guid spotId, string? label, DateOnly from, DateOnly to, DayPeriod period, Guid? userId)
	{
		if (to < from)
			throw new ArgumentOutOfRangeException(nameof(to));
		return new SpotHold
		{
			PublicId = Guid.CreateVersion7(),
			PropertyId = propertyId,
			ResourceId = spotId,
			SpotLabelSnapshot = label,
			HoldKind = HoldKind.Operational,
			HoldStatus = HoldStatus.Active,
			StartDate = from,
			EndDate = to,
			BookingPeriod = period,
			CreatedByUserId = userId,
		};
	}

	public bool IsActive => HoldStatus == HoldStatus.Active && DeletedAt is null;

	public IEnumerable<DateOnly> EachDay()
	{
		for (var d = StartDate; d <= EndDate; d = d.AddDays(1))
			yield return d;
	}

	public void Release(Guid? userId, DateTime now)
	{
		HoldStatus = HoldStatus.Released;
		ReleasedAt = now;
		ReleasedByUserId = userId;
	}

	/// <summary>Tolta perché qualcuno ha prenotato sopra.</summary>
	public void ConvertTo(Reservation reservation, Guid? userId, DateTime now)
	{
		HoldStatus = HoldStatus.Converted;
		ConvertedReservation = reservation;
		ReleasedAt = now;
		ReleasedByUserId = userId;
	}
}
