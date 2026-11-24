using Module.Booking.Application.Dtos;
using Module.Booking.Application.Abstractions;
using Module.Booking.Domain;
using Module.Booking.Domain.Enums;
using Shared.Utils;

namespace Module.Booking.Application.Services;

/// <summary>
/// Sincronizzazione dei dispositivi (offline): le prenotazioni e le riserve cambiate dopo il cursore, in ordine. Si
/// restituiscono solo le modifiche più vecchie di qualche secondo, perché una transazione ancora aperta potrebbe prendere un
/// numero più basso dopo che il dispositivo ha già letto quelli successivi.
/// </summary>
public sealed class SyncService
{
	private static readonly TimeSpan Settle = TimeSpan.FromSeconds(3);

	private readonly IReservationRepository _reservations;
	private readonly SpotDirectory _spots;
	private readonly BookingScope _scope;

	public SyncService(IReservationRepository reservations, SpotDirectory spots, BookingScope scope)
	{
		_reservations = reservations;
		_spots = spots;
		_scope = scope;
	}

	public async Task<SyncDto> ChangesAsync(long since, int limit, CancellationToken ct = default)
	{
		limit = Math.Clamp(limit, 1, 1000);
		var changes = await _reservations.ListChangesAsync(_scope.PropertyId, since, _scope.UtcNow - Settle, limit, ct);
		var visible = await _spots.VisibleIdsAsync(ct);
		var reservations = changes.Reservations.Where(r => r.Lines.Count == 0 || r.Lines.Any(l => l.SpotId is { } s && visible.Contains(s))).ToList();
		// il cursore avanza fino al più basso fra gli ultimi numeri letti, così nessuna delle due liste salta righe
		var lastReservation = changes.Reservations.Count == limit ? changes.Reservations[^1].SyncSeq : long.MaxValue;
		var lastHold = changes.Holds.Count == limit ? changes.Holds[^1].SyncSeq : long.MaxValue;
		var bound = Math.Min(lastReservation, lastHold);
		var cursor = new[] { since }
			.Concat(changes.Reservations.Where(r => r.SyncSeq <= bound).Select(r => r.SyncSeq))
			.Concat(changes.Holds.Where(h => h.SyncSeq <= bound).Select(h => h.SyncSeq)).Max();
		return new SyncDto(
			cursor, bound != long.MaxValue,
			reservations.Where(r => r.SyncSeq <= bound).Select(r => new SyncReservationDto(
				r.PublicId, r.BookingNumber, Codes.Of(r.Status), r.Status == ReservationStatus.Cancelled || r.DeletedAt is not null, r.Version, r.SyncSeq,
				r.StayFrom, r.StayTo,
				r.Lines.Select(l => new SyncLineDto(l.PublicId, l.SpotId, l.StartDate, l.EndDate, SpotKinds.CodeOf(l.BookingPeriod))).ToList())).ToList(),
			changes.Holds.Where(h => h.SyncSeq <= bound && visible.Contains(h.ResourceId)).Select(h => new SyncHoldDto(
				h.PublicId, h.ResourceId, h.StartDate, h.EndDate, SpotKinds.CodeOf(h.BookingPeriod), h.IsActive, h.SyncSeq)).ToList());
	}
}
