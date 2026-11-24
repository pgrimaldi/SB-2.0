using BuildingBlocks.Application.Validation;
using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Dtos;
using Module.Booking.Application.Validators;
using Module.Booking.Domain;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Catalog;
using Shared.Contracts.Errors;

namespace Module.Booking.Application.Services;

/// <summary>
/// Ricerche sulla disponibilità: tratti liberi di ogni postazione in un intervallo (ricerca smart della mappa), ricerca
/// guidata del planner (meno cambi di postazione) e piano di una prenotazione ripetuta. Un giorno è occupato se una
/// prenotazione o una riserva lo tiene, anche solo per mezza giornata; le postazioni non prenotabili non compaiono.
/// </summary>
public sealed class SearchService
{
	/// <summary>Limite di sicurezza della ricerca smart (il committente non vuole limiti: dieci anni bastano).</summary>
	private const int MaxFreeRangeDays = 3660;
	private const int Alternatives = 2;
	private static readonly IReadOnlySet<DateOnly> NoDays = new HashSet<DateOnly>();

	private readonly IReservationRepository _reservations;
	private readonly SpotDirectory _spots;
	private readonly BookingScope _scope;
	private readonly GuidedSearchValidator _guidedValidator;
	private readonly RepeatPlanValidator _repeatValidator;

	public SearchService(
		IReservationRepository reservations, SpotDirectory spots, BookingScope scope, GuidedSearchValidator guidedValidator, RepeatPlanValidator repeatValidator)
	{
		_reservations = reservations;
		_spots = spots;
		_scope = scope;
		_guidedValidator = guidedValidator;
		_repeatValidator = repeatValidator;
	}

	/// <summary>Per ogni postazione prenotabile: libera, occupata o libera solo in alcuni tratti (estremi compresi).</summary>
	public async Task<FreeRangesDto> FreeRangesAsync(DateOnly from, DateOnly to, Guid? sectorId, string? spotType, CancellationToken ct = default)
	{
		if (to < from || to.DayNumber - from.DayNumber >= MaxFreeRangeDays)
			throw DomainException.Unprocessable(ValidationErrorCodes.OutOfRange, new Dictionary<string, object?> { ["maxDays"] = MaxFreeRangeDays });
		var spots = await CandidatesAsync(sectorId, spotType, includeAreas: true, ct);
		var busy = await BusyDaysAsync(spots, from, to, DayPeriod.FullDay, null, ct);

		var result = spots.Select(spot =>
		{
			var ranges = FreeRuns(busy.GetValueOrDefault(spot.Id) ?? NoDays, from, to);
			var status = ranges.Count == 0 ? RangeStatuses.Occupied : ranges.Count == 1 && ranges[0].From == from && ranges[0].To == to ? RangeStatuses.Free : RangeStatuses.Partial;
			return new SpotFreeRangesDto(spot.Id, spot.Label, spot.SectorId, spot.SpotType, status, ranges);
		}).ToList();
		return new FreeRangesDto(from, to, result);
	}

	/// <summary>
	/// Ricerca guidata: le postazioni che coprono il periodo con meno cambi possibili. A ogni cambio si sceglie la
	/// postazione libera più a lungo; a parità, la più vicina alla precedente e poi a quella indicata («vicino a»), in
	/// caselle di distanza. Le due alternative partono da postazioni diverse.
	/// </summary>
	public async Task<GuidedSearchDto> GuidedAsync(GuidedSearchQuery query, CancellationToken ct = default)
	{
		_guidedValidator.EnsureValid(query);
		var period = SpotKinds.ParsePeriod(query.DayPart);
		var spots = await CandidatesAsync(query.SectorId, query.SpotType, includeAreas: false, ct);
		var all = await _spots.AllAsync(ct);
		var near = query.NearSpotId is { } nid && all.TryGetValue(nid, out var n) ? n : null;
		var busy = await BusyDaysAsync(spots, query.From, query.To, period, null, ct);

		var solutions = new List<GuidedSolutionDto>();
		var tried = new HashSet<Guid>();
		for (var attempt = 0; attempt < 6 && solutions.Count < Alternatives; attempt++)
		{
			var plan = Cover(spots, busy, query.From, query.To, near, tried);
			if (plan is null)
				break;
			tried.Add(plan[0].Spot.Id);
			var moves = plan.Count - 1;
			if (query.MaxMoves is { } max && moves > max)
				continue;
			var segments = plan.Select(p => new GuidedSegmentDto(p.Spot.Id, p.Spot.Label, p.Spot.SectorId, p.From, p.To)).ToList();
			if (!solutions.Any(s => s.Segments.SequenceEqual(segments)))
				solutions.Add(new GuidedSolutionDto(moves, segments));
		}
		return new GuidedSearchDto(solutions.OrderBy(s => s.Moves).ToList());
	}

	/// <summary>
	/// Prenotazione ripetuta: i giorni della settimana scelti nel periodo, raggruppati in blocchi di giorni consecutivi
	/// (ogni blocco si paga per la sua lunghezza), con i giorni già occupati. Il gestore sceglie e salva le postazioni con
	/// <c>PUT /reservations/{id}</c> o <c>POST /reservations</c>, un blocco per postazione.
	/// </summary>
	public async Task<RepeatPlanDto> RepeatPlanAsync(RepeatPlanRequest request, CancellationToken ct = default)
	{
		_repeatValidator.EnsureValid(request);
		var spot = await _spots.RequireBookableAsync(request.SpotId, ct);
		var period = SpotKinds.ParsePeriod(request.DayPart);
		var own = new HashSet<long>();
		if (request.ExcludeReservationId is { } exclude && await _reservations.GetAsync(_scope.PropertyId, exclude, false, ct) is { } reservation)
			own = reservation.Lines.Select(l => l.Id).ToHashSet();
		var busy = (await BusyDaysAsync([spot], request.From, request.To, period, own, ct)).GetValueOrDefault(spot.Id) ?? NoDays;

		var weekdays = request.Weekdays.Select(d => (DayOfWeek)(d % 7)).ToHashSet();
		var blocks = new List<RepeatBlockDto>();
		DateOnly? start = null;
		var previous = request.From;
		for (var day = request.From; day <= request.To; day = day.AddDays(1))
		{
			if (!weekdays.Contains(day.DayOfWeek))
				continue;
			if (start is not null && day != previous.AddDays(1))
			{
				blocks.Add(Block(start.Value, previous, busy));
				start = null;
			}
			start ??= day;
			previous = day;
		}
		if (start is not null)
			blocks.Add(Block(start.Value, previous, busy));
		return new RepeatPlanDto(spot.Id, blocks);
	}

	private static RepeatBlockDto Block(DateOnly from, DateOnly to, IReadOnlySet<DateOnly> busy)
		=> new(from, to, busy.Where(d => d >= from && d <= to).Order().ToList());

	// ----- Supporto -----

	private async Task<List<BookableSpot>> CandidatesAsync(Guid? sectorId, string? spotType, bool includeAreas, CancellationToken ct)
	{
		var visible = await _spots.VisibleIdsAsync(ct);
		return (await _spots.AllAsync(ct)).Values
			.Where(s => s.IsBookable && visible.Contains(s.Id) && (includeAreas || !s.IsArea)
				&& (s.IsArea || SpotKinds.IsExclusive(SpotKinds.ResourceTypeOf(s.SpotType)))
				&& (sectorId is null || s.SectorId == sectorId)
				&& (spotType is null || string.Equals(s.SpotType, spotType, StringComparison.OrdinalIgnoreCase)))
			.OrderBy(s => s.Row ?? short.MaxValue).ThenBy(s => s.Column ?? short.MaxValue).ThenBy(s => s.Label)
			.ToList();
	}

	/// <summary>Giorni occupati per postazione; le aree sono occupate quando sono piene.</summary>
	private async Task<Dictionary<Guid, IReadOnlySet<DateOnly>>> BusyDaysAsync(
		IReadOnlyList<BookableSpot> spots, DateOnly from, DateOnly to, DayPeriod period, IReadOnlySet<long>? ownLines, CancellationToken ct)
	{
		var areas = spots.Where(s => s.IsArea).ToDictionary(s => s.Id);
		var occupied = (await _reservations.ListOccupiedAsync(_scope.PropertyId, spots.Select(s => s.Id).ToList(), from, to, ct))
			.Where(o => SpotKinds.Overlaps(o.Period, period)
				&& !(ownLines is not null && o.Source == ConsumptionSource.ReservationLine && o.SourceId is { } id && ownLines.Contains(id)));
		var result = new Dictionary<Guid, IReadOnlySet<DateOnly>>();
		foreach (var group in occupied.GroupBy(o => o.SpotId))
			result[group.Key] = areas.TryGetValue(group.Key, out var area)
				? group.GroupBy(o => o.Day).Where(d => d.Sum(o => o.Quantity) >= (area.AreaSeatCount ?? 0)).Select(d => d.Key).ToHashSet()
				: group.Select(o => o.Day).ToHashSet();
		return result;
	}

	private static List<DateRangeDto> FreeRuns(IReadOnlySet<DateOnly> busy, DateOnly from, DateOnly to)
	{
		var runs = new List<DateRangeDto>();
		DateOnly? start = null;
		for (var day = from; day <= to; day = day.AddDays(1))
		{
			if (busy.Contains(day))
			{
				if (start is not null)
					runs.Add(new DateRangeDto(start.Value, day.AddDays(-1)));
				start = null;
			}
			else
				start ??= day;
		}
		if (start is not null)
			runs.Add(new DateRangeDto(start.Value, to));
		return runs;
	}

	private sealed record Step(BookableSpot Spot, DateOnly From, DateOnly To);

	/// <summary>Copertura del periodo con meno tratti (scelta golosa della postazione libera più a lungo).</summary>
	private static List<Step>? Cover(
		IReadOnlyList<BookableSpot> spots, Dictionary<Guid, IReadOnlySet<DateOnly>> busy, DateOnly from, DateOnly to, BookableSpot? near,
		IReadOnlySet<Guid> excludedFirst)
	{
		var steps = new List<Step>();
		var day = from;
		BookableSpot? previous = null;
		while (day <= to)
		{
			var best = spots
				.Where(s => steps.Count > 0 || !excludedFirst.Contains(s.Id))
				.Select(s => (Spot: s, Reach: Reach(busy.GetValueOrDefault(s.Id), day, to)))
				.Where(x => x.Reach is not null)
				.OrderByDescending(x => x.Reach!.Value)
				.ThenBy(x => previous is null ? 0 : Distance(previous, x.Spot))
				.ThenBy(x => near is null ? 0 : Distance(near, x.Spot))
				.FirstOrDefault();
			if (best.Spot is null)
				return null;
			steps.Add(new Step(best.Spot, day, best.Reach!.Value));
			previous = best.Spot;
			day = best.Reach.Value.AddDays(1);
		}
		return steps;
	}

	/// <summary>Ultimo giorno libero di fila da <paramref name="day"/>; null se quel giorno è occupato.</summary>
	private static DateOnly? Reach(IReadOnlySet<DateOnly>? busy, DateOnly day, DateOnly to)
	{
		if (busy is not null && busy.Contains(day))
			return null;
		var last = day;
		while (last < to && (busy is null || !busy.Contains(last.AddDays(1))))
			last = last.AddDays(1);
		return last;
	}

	/// <summary>Caselle di distanza nel reticolo della mappa; senza posizione, lontanissime.</summary>
	private static int Distance(BookableSpot a, BookableSpot b)
		=> a.Row is { } ar && a.Column is { } ac && b.Row is { } br && b.Column is { } bc ? Math.Abs(ar - br) + Math.Abs(ac - bc) : 10_000;
}
