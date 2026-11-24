using BuildingBlocks.Application.Validation;
using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Dtos;
using Module.Booking.Application.Validators;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Contracts.Identity;
using Shared.Utils;

namespace Module.Booking.Application.Services;

/// <summary>
/// Occupazione delle postazioni per la mappa (un giorno) e il planner (un intervallo), disponibilità per la scelta delle
/// postazioni e riserve senza cliente. Le postazioni e il loro disegno vengono da Catalog (<c>GET /api/catalog/map</c>):
/// qui solo chi le occupa. L'affiliato vede solo le sue postazioni.
/// </summary>
public sealed class MapService
{
	private readonly IReservationRepository _reservations;
	private readonly ISpotHoldRepository _holds;
	private readonly SpotDirectory _spots;
	private readonly ICustomerDirectory _customers;
	private readonly BookingScope _scope;
	private readonly BookingAudit _audit;
	private readonly CreateHoldsValidator _holdsValidator;
	private readonly IResaleRepository _resale;

	public MapService(
		IReservationRepository reservations, ISpotHoldRepository holds, SpotDirectory spots, ICustomerDirectory customers, BookingScope scope,
		BookingAudit audit, CreateHoldsValidator holdsValidator, IResaleRepository resale)
	{
		_resale = resale;
		_reservations = reservations;
		_holds = holds;
		_spots = spots;
		_customers = customers;
		_scope = scope;
		_audit = audit;
		_holdsValidator = holdsValidator;
	}

	/// <summary>Chi occupa ogni postazione nel giorno indicato (oggi se assente).</summary>
	public async Task<MapDayDto> DayAsync(DateOnly? date, CancellationToken ct = default)
	{
		var day = date ?? _scope.Today;
		var propertyId = _scope.PropertyId;
		var visible = await _spots.VisibleIdsAsync(ct);
		var lines = (await _reservations.ListLinesAsync(propertyId, day, day, ct)).Where(l => l.SpotId is { } s && visible.Contains(s)).ToList();

		// Righe liberate da un check-out anticipato: il giorno è di nuovo libero.
		var released = (await _reservations.ListLineConsumptionsAsync(lines.Select(l => l.Id).ToList(), ct))
			.Where(c => c.ConsumptionDate == day && c.State == ConsumptionState.Released && c.ResourceType != ResourceType.InventoryItem).Select(c => c.SourceId).ToHashSet();
		lines = lines.Where(l => !released.Contains(l.Id)).ToList();

		var entered = (await _reservations.ListEntrancesAsync(propertyId, day, ct)).Select(e => e.BookingLineId).ToHashSet();
		var names = await NamesAsync(lines.Select(l => l.Reservation.CustomerId), ct);
		var amounts = _scope.CanSeeAmounts;
		var occupants = lines.Select(l => new MapOccupantDto(
			l.SpotId!.Value, SpotKinds.CodeOf(l.BookingPeriod), "reservation", l.Reservation.PublicId, l.PublicId, null, l.Reservation.BookingNumber,
			l.Reservation.CustomerId is { } c ? names.GetValueOrDefault(c) : null, ReservationService.PaymentStatusOf(l.Reservation),
			amounts ? l.Reservation.BalanceDue : null, l.Reservation.HasSeasonalPass, l.StartDate == day, l.EndDate == day,
			l.Reservation.CheckedInAt is not null, l.Reservation.CheckedOutAt is not null, entered.Contains(l.Id), l.Quantity,
			!string.IsNullOrEmpty(l.Reservation.Note))).ToList();

		foreach (var hold in await _holds.ListActiveAsync(propertyId, day, day, null, ct))
			if (visible.Contains(hold.ResourceId))
				occupants.Add(new MapOccupantDto(hold.ResourceId, SpotKinds.CodeOf(hold.BookingPeriod), "hold", null, null, hold.PublicId, null, null,
					null, null, false, hold.StartDate == day, hold.EndDate == day, false, false, false, 1, false));
		var ceded = (await _resale.ListOnDayAsync(propertyId, day, ct)).Where(o => visible.Contains(o.ResourceId) && o.SourceReservation.DeletedAt is null)
			.Select(o => new MapReleasedDto(o.ResourceId, o.PublicId, SpotKinds.CodeOf(o.OfferedPeriod), o.Origin == ResaleOrigin.Absence ? ReleaseKinds.Absence : ReleaseKinds.Sharing,
				Codes.Of(o.OfferState), o.SourceReservation.PublicId, o.SourceReservation.BookingNumber)).ToList();
		return new MapDayDto(day, occupants, ceded);
	}

	/// <summary>Righe e riserve nell'intervallo, al massimo 62 giorni.</summary>
	public async Task<PlannerDto> PlannerAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
	{
		if (to < from || to.DayNumber - from.DayNumber >= BookingRules.MaxPlannerDays)
			throw DomainException.Unprocessable(ValidationErrorCodes.OutOfRange, new Dictionary<string, object?> { ["maxDays"] = BookingRules.MaxPlannerDays });
		var propertyId = _scope.PropertyId;
		var visible = await _spots.VisibleIdsAsync(ct);
		var lines = (await _reservations.ListLinesAsync(propertyId, from, to, ct)).Where(l => l.SpotId is { } s && visible.Contains(s)).ToList();
		var names = await NamesAsync(lines.Select(l => l.Reservation.CustomerId), ct);
		var holds = (await _holds.ListActiveAsync(propertyId, from, to, null, ct)).Where(h => visible.Contains(h.ResourceId))
			.Select(h => new PlannerHoldDto(h.PublicId, h.ResourceId, h.StartDate, h.EndDate, SpotKinds.CodeOf(h.BookingPeriod))).ToList();
		return new PlannerDto(from, to, lines.Select(l => new PlannerLineDto(
			l.PublicId, l.Reservation.PublicId, l.SpotId, l.StartDate, l.EndDate, SpotKinds.CodeOf(l.BookingPeriod), l.Reservation.BookingNumber,
			l.Reservation.CustomerId is { } c ? names.GetValueOrDefault(c) : null, Codes.Of(l.Reservation.Status),
			ReservationService.PaymentStatusOf(l.Reservation), l.Reservation.HasSeasonalPass, l.Quantity)).ToList(), holds);
	}

	/// <summary>
	/// Disponibilità delle postazioni nell'intervallo (per la scelta in «Prenota»): libera se nessuno la occupa; una
	/// postazione occupata solo da riserve si può prenotare comunque (la riserva si toglie).
	/// </summary>
	public async Task<IReadOnlyList<SpotAvailabilityDto>> AvailabilityAsync(DateOnly from, DateOnly to, string? dayPart, Guid? excludeReservationId, CancellationToken ct = default)
	{
		if (to < from || to.DayNumber - from.DayNumber >= BookingRules.MaxDays)
			throw DomainException.Unprocessable(ValidationErrorCodes.OutOfRange, new Dictionary<string, object?> { ["maxDays"] = BookingRules.MaxDays });
		var period = SpotKinds.ParsePeriod(dayPart);
		var propertyId = _scope.PropertyId;
		var spots = (await _spots.AllAsync(ct)).Values.Where(s => s.IsBookable).ToList();
		var visible = await _spots.VisibleIdsAsync(ct);
		spots = spots.Where(s => visible.Contains(s.Id)).ToList();
		var own = new HashSet<long>();
		if (excludeReservationId is { } exclude && await _reservations.GetAsync(propertyId, exclude, false, ct) is { } reservation)
			own = reservation.Lines.Select(l => l.Id).ToHashSet();
		var occupied = (await _reservations.ListOccupiedAsync(propertyId, spots.Select(s => s.Id).ToList(), from, to, ct))
			.Where(o => SpotKinds.Overlaps(o.Period, period) && !(o.Source == ConsumptionSource.ReservationLine && o.SourceId is { } id && own.Contains(id)))
			.GroupBy(o => o.SpotId).ToDictionary(g => g.Key, g => g.ToList());

		var result = new List<SpotAvailabilityDto>();
		foreach (var spot in spots)
		{
			var busy = occupied.GetValueOrDefault(spot.Id) ?? [];
			if (spot.IsArea)
			{
				var full = busy.GroupBy(o => o.Day).Where(g => g.Sum(o => o.Quantity) >= (spot.AreaSeatCount ?? 0)).Select(g => g.Key).Order().ToList();
				result.Add(new SpotAvailabilityDto(spot.Id, full.Count == 0 ? AvailabilityStatuses.Free : AvailabilityStatuses.Occupied, full));
				continue;
			}
			if (!SpotKinds.IsExclusive(SpotKinds.ResourceTypeOf(spot.SpotType)))
			{
				result.Add(new SpotAvailabilityDto(spot.Id, AvailabilityStatuses.Free, []));
				continue;
			}
			var days = busy.Select(o => o.Day).Distinct().Order().ToList();
			var status = days.Count == 0 ? AvailabilityStatuses.Free
				: busy.All(o => o.Source == ConsumptionSource.ManualHold) ? AvailabilityStatuses.Held : AvailabilityStatuses.Occupied;
			result.Add(new SpotAvailabilityDto(spot.Id, status, days));
		}
		return result;
	}

	/// <summary>
	/// Riserva di una o più postazioni libere, senza cliente e senza scadenza: tutte con le stesse date, oppure ognuna con
	/// il suo periodo (ricerca smart).
	/// </summary>
	public async Task<IReadOnlyList<HoldDto>> CreateHoldsAsync(CreateHoldsRequest request, CancellationToken ct = default)
	{
		_holdsValidator.EnsureValid(request);
		var propertyId = _scope.PropertyId;
		var period = SpotKinds.ParsePeriod(request.DayPart);
		var segments = request.Segments is { Count: > 0 } given
			? given.ToList()
			: request.SpotIds!.Distinct().Select(id => new HoldSegmentInput(id, request.From, request.To)).ToList();

		var holds = new List<(Shared.Contracts.Catalog.BookableSpot Spot, SpotHold Hold)>();
		var conflicts = new List<object>();
		foreach (var segment in segments)
		{
			var spot = await _spots.RequireBookableAsync(segment.SpotId, ct);
			var busy = (await _reservations.ListOccupiedAsync(propertyId, [spot.Id], segment.From, segment.To, ct))
				.Where(o => SpotKinds.Overlaps(o.Period, period));
			conflicts.AddRange(busy.Select(o => new { spotId = o.SpotId, day = o.Day }));
			if (holds.Any(h => h.Spot.Id == spot.Id && h.Hold.StartDate <= segment.To && h.Hold.EndDate >= segment.From))
				throw DomainException.Unprocessable(ValidationErrorCodes.Invalid, new Dictionary<string, object?> { ["field"] = "segments", ["spotId"] = spot.Id });
			holds.Add((spot, SpotHold.CreateOperational(propertyId, spot.Id, spot.Label, segment.From, segment.To, period, _scope.UserId)));
		}
		if (conflicts.Count > 0)
			throw DomainException.Conflict(BookingErrors.SpotUnavailable, new Dictionary<string, object?> { ["conflicts"] = conflicts.Take(50).ToList() });

		await using var tx = await _reservations.BeginTransactionAsync(ct);
		foreach (var (_, hold) in holds)
			_holds.Add(hold);
		await _reservations.SaveChangesAsync(ct);
		_reservations.AddConsumptions(holds.SelectMany(h => h.Hold.EachDay().Select(d => ResourceConsumption.ForHold(h.Hold, SpotKinds.ResourceTypeOf(h.Spot.SpotType), d))));
		await _reservations.SaveChangesAsync(ct);
		await tx.CommitAsync(ct);

		foreach (var (spot, hold) in holds)
			await _audit.RecordAsync(AuditActionCodes.Booking.SpotHoldCreated, propertyId, AuditActionCodes.Booking.Targets.SpotHold, hold.PublicId,
				new Dictionary<string, object?> { ["from"] = hold.StartDate, ["to"] = hold.EndDate }, spotLabel: spot.Label, ct: ct);
		return holds.Select(h => ToDto(h.Hold)).ToList();
	}

	public async Task RemoveHoldAsync(Guid id, CancellationToken ct = default)
	{
		var hold = await _holds.GetAsync(_scope.PropertyId, id, ct);
		if (hold is null || !hold.IsActive)
			throw DomainException.NotFound(BookingErrors.HoldNotFound);
		foreach (var consumption in await _holds.ListConsumptionsAsync([hold.Id], ct))
			consumption.Release();
		hold.Release(_scope.UserId, _scope.UtcNow);
		await _reservations.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.SpotHoldRemoved, hold.PropertyId, AuditActionCodes.Booking.Targets.SpotHold, hold.PublicId,
			new Dictionary<string, object?> { ["from"] = hold.StartDate, ["to"] = hold.EndDate }, spotLabel: hold.SpotLabelSnapshot, ct: ct);
	}

	private static HoldDto ToDto(SpotHold h)
		=> new(h.PublicId, h.ResourceId, h.SpotLabelSnapshot, h.StartDate, h.EndDate, SpotKinds.CodeOf(h.BookingPeriod), h.CreatedAt, h.CreatedByUserId);

	private async Task<Dictionary<Guid, string?>> NamesAsync(IEnumerable<Guid?> ids, CancellationToken ct)
	{
		var list = ids.Where(i => i is not null).Select(i => i!.Value).Distinct().ToList();
		return list.Count == 0 ? [] : (await _customers.ListAsync(_scope.PropertyId, list, ct)).ToDictionary(c => c.Id, c => c.FullName);
	}
}
