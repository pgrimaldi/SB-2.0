using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Dtos;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Catalog;
using Shared.Contracts.Errors;

namespace Module.Booking.Application.Services;

/// <summary>Un servizio dell'ordine riconosciuto in Catalog, con la durata scelta se è orario.</summary>
public sealed record ResolvedService(ServiceInput Input, BookableService Service, HourlySlotRule? Slot, DateOnly Date)
{
	public TimeOnly? End => Slot is { } s && Input.StartTime is { } start ? start.AddMinutes(s.DurationMinutes) : null;
}

/// <summary>
/// Servizi (bar, standard, orari) e tavoli di una prenotazione: righe rifatte a ogni salvataggio con gli importi del
/// preventivo, limiti dei servizi (massimo al giorno, all'ora, per fascia, minimo e massimo di articoli, orari) e
/// assegnazione dei tavoli (il tavolo con i posti uguali o più vicini per eccesso). Dal gestionale un limite superato è un
/// avviso che il gestore conferma; online blocca.
/// </summary>
public sealed class ServiceWriter
{
	private readonly IBookingServices _catalog;
	private readonly IReservationRepository _reservations;
	private readonly BookingScope _scope;
	private IReadOnlyDictionary<Guid, BookableService>? _services;
	private IReadOnlyDictionary<Guid, BookableTableService>? _tables;

	public ServiceWriter(IBookingServices catalog, IReservationRepository reservations, BookingScope scope)
	{
		_catalog = catalog;
		_reservations = reservations;
		_scope = scope;
	}

	public async Task<IReadOnlyDictionary<Guid, BookableService>> ServicesAsync(Guid propertyId, CancellationToken ct = default)
		=> _services ??= (await _catalog.ListServicesAsync(propertyId, ct)).ToDictionary(s => s.Id);

	public async Task<IReadOnlyDictionary<Guid, BookableTableService>> TablesAsync(Guid propertyId, CancellationToken ct = default)
		=> _tables ??= (await _catalog.ListTableServicesAsync(propertyId, ct)).ToDictionary(t => t.Id);

	/// <summary>Servizi richiesti riconosciuti in Catalog (404 se uno non c'è, 404 se la durata non è del servizio).</summary>
	public async Task<IReadOnlyList<ResolvedService>> ResolveAsync(Guid propertyId, SaveReservationRequest request, CancellationToken ct = default)
	{
		var services = await ServicesAsync(propertyId, ct);
		var result = new List<ResolvedService>();
		foreach (var input in request.Services ?? [])
		{
			if (!services.TryGetValue(input.ServiceId, out var service))
				throw DomainException.NotFound(BookingErrors.ServiceNotFound);
			HourlySlotRule? slot = null;
			if (input.TimeSlotId is { } slotId)
				slot = service.Availability.SelectMany(a => a.Hourly).SelectMany(h => h.Slots).FirstOrDefault(s => s.Id == slotId)
					?? throw DomainException.NotFound(BookingErrors.ServiceNotFound);
			else if (service.Kind == ServiceKinds.Hourly)
				throw DomainException.Unprocessable(ValidationErrorCodes.Required, new Dictionary<string, object?> { ["field"] = "timeSlotId" });
			result.Add(new ResolvedService(input, service, slot, input.Date ?? request.From));
		}
		return result;
	}

	/// <summary>Richieste di preventivo dei servizi, nello stesso ordine.</summary>
	public static IReadOnlyList<PriceQuoteService> QuoteRequests(IReadOnlyList<ResolvedService> services)
		=> services.Select(s => new PriceQuoteService(s.Service.Id, s.Input.Quantity, s.Input.Variant, s.Date, s.Input.TimeSlotId)).ToList();

	/// <summary>
	/// Rifà le righe dei servizi e dei tavoli. Le righe del preventivo arrivano nell'ordine delle richieste, seguite dai prezzi
	/// automatici (es. «Coperto»), che diventano righe a sé. Restituisce le righe nuove (i consumi si scrivono dopo il primo
	/// salvataggio) e i limiti superati.
	/// </summary>
	public async Task<(IReadOnlyList<BookingServiceLine> Lines, IReadOnlyList<CapacityViolation> Violations, decimal ManualDelta)> ApplyAsync(
		Reservation reservation, SaveReservationRequest request, IReadOnlyList<ResolvedService>? requested, IReadOnlyList<PriceQuoteLine> quoteLines,
		IReadOnlyList<BookingLine> spotLines, CancellationToken ct = default)
	{
		var tableViolations = request.Tables is null ? [] : await ApplyTablesAsync(reservation, request, ct);
		if (requested is not { } services)
			return ([], tableViolations, 0m);

		// righe di prima: si tolgono, con i loro consumi
		var old = reservation.ServiceLines.Where(s => s.DeletedAt is null).ToList();
		var before = old.Where(o => o.SaleItemId is not null).GroupBy(o => o.SaleItemId!.Value).ToDictionary(g => g.Key, g => g.Sum(o => o.Quantity));
		// tolti, non liberati: un ripristino dal cestino non li deve riportare in vita accanto a quelli nuovi
		foreach (var consumption in await _reservations.ListServiceConsumptionsAsync(old.Where(o => o.Id != 0).Select(o => o.Id).ToList(), ct))
			_reservations.RemoveConsumption(consumption);
		foreach (var line in old)
		{
			_reservations.RemoveServiceLine(line);
			reservation.ServiceLines.Remove(line);
		}

		var serviceQuotes = quoteLines.Where(l => l.Kind == QuoteLineKinds.Service).ToList();
		var created = new List<BookingServiceLine>();
		decimal manualDelta = 0;
		for (var i = 0; i < services.Count; i++)
		{
			var s = services[i];
			var quoted = i < serviceQuotes.Count ? serviceQuotes[i] : null;
			var amount = quoted?.Amount ?? 0m;
			if (s.Input.ManualPrice is { } manual)
			{
				manualDelta += manual - amount;
				amount = manual;
			}
			BookingLine? spotLine = null;
			if (s.Input.SpotId is { } spotId)
				spotLine = spotLines.FirstOrDefault(l => l.SpotId == spotId && l.Covers(s.Date)) ?? spotLines.FirstOrDefault(l => l.SpotId == spotId)
					?? throw DomainException.Unprocessable(BookingErrors.ServiceLineSpotMissing, new Dictionary<string, object?> { ["spotId"] = spotId });
			var line = BookingServiceLine.Create(reservation, spotLine, KindOf(s.Service.Kind), s.Service.Id, quoted?.Description ?? s.Service.Name,
				s.Input.Variant, s.Input.Quantity, amount, quoted?.VatRate, s.Date);
			if (s.Slot is { } slot && s.Input.StartTime is { } start)
				line.AddSlot(slot.Id, start, start.AddMinutes(slot.DurationMinutes), s.Date, $"{slot.DurationMinutes} min");
			reservation.ServiceLines.Add(line);
			created.Add(line);
		}
		foreach (var auto in quoteLines.Where(l => l.Kind == QuoteLineKinds.AutomaticPrice))
		{
			var line = BookingServiceLine.Create(reservation, null, ServiceCategory.Normal, auto.RefId, auto.Description, null, Math.Max((short)1, auto.Quantity),
				auto.Amount, auto.VatRate, services.Count > 0 ? services[0].Date : request.From);
			reservation.ServiceLines.Add(line);
		}

		var violations = await CheckServicesAsync(reservation, services, ct);
		violations.AddRange(await CheckIntervalsAsync(reservation, services, before, ct));
		violations.AddRange(tableViolations);
		return (created, violations, manualDelta);
	}

	/// <summary>Consumi dei servizi creati, dopo il primo salvataggio (vogliono l'id delle righe).</summary>
	public static IEnumerable<ResourceConsumption> ConsumptionsFor(Reservation reservation, IEnumerable<BookingServiceLine> lines)
		=> lines.Where(l => l.SaleItemId is not null && l.ServiceDate is not null).Select(l =>
		{
			var slot = l.TimeSlots.FirstOrDefault();
			var type = l.Kind switch { ServiceCategory.Bar => ResourceType.BarService, ServiceCategory.Hourly => ResourceType.HourlyService, _ => ResourceType.NormalService };
			return ResourceConsumption.ForService(l, reservation, type, l.ServiceDate!.Value, slot?.SlotStart, slot?.SlotEnd);
		});

	public static ServiceCategory KindOf(string kind) => kind switch
	{
		ServiceKinds.Bar => ServiceCategory.Bar,
		ServiceKinds.Hourly => ServiceCategory.Hourly,
		_ => ServiceCategory.Normal,
	};

	// ----- Limiti dei servizi -----

	private async Task<List<CapacityViolation>> CheckServicesAsync(Reservation reservation, IReadOnlyList<ResolvedService> services, CancellationToken ct)
	{
		var violations = new List<CapacityViolation>();
		if (services.Count == 0)
			return violations;
		var usage = await _reservations.ListServiceUsageAsync(reservation.PropertyId, services.Select(s => s.Service.Id).Distinct().ToList(),
			services.Min(s => s.Date), services.Max(s => s.Date), reservation.Id, ct);

		foreach (var group in services.GroupBy(s => (s.Service.Id, s.Date)))
		{
			var first = group.First();
			var service = first.Service;
			var rule = RuleFor(service, first.Date);
			void Add(string name, int? limit, int? requested) => violations.Add(new CapacityViolation(service.Id, service.Name, first.Date, name, limit, requested));
			if (rule is null)
			{
				if (service.Availability.Count > 0)
					Add(CapacityRules.NotAvailable, null, null);
				continue;
			}
			var quantity = group.Sum(s => s.Input.Quantity);
			if (rule.MinItems is { } min && quantity < min)
				Add(CapacityRules.MinItems, min, quantity);
			if (rule.MaxItems is { } max && quantity > max)
				Add(CapacityRules.MaxItems, max, quantity);
			var sameDay = usage.Where(u => u.ServiceId == service.Id && u.Day == first.Date).ToList();
			if (rule.MaxPerDay is { } perDay && sameDay.Sum(u => u.Quantity) + quantity > perDay)
				Add(CapacityRules.MaxPerDay, perDay, sameDay.Sum(u => u.Quantity) + quantity);

			foreach (var s in group.Where(x => x.Input.StartTime is not null))
			{
				var start = s.Input.StartTime!.Value;
				var end = s.End ?? start;
				var weekday = (int)s.Date.DayOfWeek is 0 ? 7 : (int)s.Date.DayOfWeek;
				var hours = rule.Hourly.FirstOrDefault(h => h.Weekdays.Count == 0 || h.Weekdays.Contains(weekday));
				if (hours is null || (hours.From is { } f && start < f) || (hours.To is { } t && end > t)
					|| (rule.ManagementFrom is { } mf && start < mf) || (rule.ManagementTo is { } mt && end > mt))
					Add(CapacityRules.OutsideHours, null, null);
				else if (rule.HasFixedTimeSlots && hours.From is { } from && s.Slot is { DurationMinutes: > 0 } slot
					&& (int)(start - from).TotalMinutes % slot.DurationMinutes != 0)
					Add(CapacityRules.SlotNotAligned, slot.DurationMinutes, null);
				var inHour = sameDay.Where(u => u.Start is { } us && us.Hour == start.Hour).Sum(u => u.Quantity) + s.Input.Quantity;
				if (rule.MaxPerHour is { } perHour && inHour > perHour)
					Add(CapacityRules.MaxPerHour, perHour, inHour);
				var overlapping = sameDay.Where(u => u.Start is { } us && u.End is { } ue && us < end && ue > start).Sum(u => u.Quantity) + s.Input.Quantity;
				if (rule.MaxPerSlot is { } perSlot && overlapping > perSlot)
					Add(CapacityRules.MaxPerSlot, perSlot, overlapping);
			}
		}
		return violations;
	}

	/// <summary>
	/// Intervallo fra due ordini: se il cliente ha già ordinato lo stesso servizio in un'altra prenotazione da meno dei minuti
	/// impostati, il nuovo ordine (servizio aggiunto o quantità aumentata) supera il limite. Senza cliente non si controlla.
	/// </summary>
	private async Task<List<CapacityViolation>> CheckIntervalsAsync(
		Reservation reservation, IReadOnlyList<ResolvedService> services, IReadOnlyDictionary<Guid, int> before, CancellationToken ct)
	{
		var violations = new List<CapacityViolation>();
		if (reservation.CustomerId is not { } customerId || services.Count == 0)
			return violations;
		var now = _scope.UtcNow;
		var added = services.GroupBy(s => s.Service.Id)
			.Select(g => (First: g.OrderBy(s => s.Date).First(), Quantity: g.Sum(s => s.Input.Quantity), Id: g.Key))
			.Where(g => g.Quantity > before.GetValueOrDefault(g.Id))
			.Select(g => (g.First, Interval: RuleFor(g.First.Service, g.First.Date)?.IntervalMinutes))
			.Where(g => g.Interval is > 0)
			.ToList();
		if (added.Count == 0)
			return violations;
		var longest = added.Max(a => a.Interval!.Value);
		var last = await _reservations.LastServiceOrdersAsync(reservation.PropertyId, customerId, added.Select(a => a.First.Service.Id).ToList(),
			now.AddMinutes(-longest), reservation.Id, ct);
		foreach (var (first, interval) in added)
			if (last.TryGetValue(first.Service.Id, out var at) && now - at < TimeSpan.FromMinutes(interval!.Value))
				violations.Add(new CapacityViolation(first.Service.Id, first.Service.Name, first.Date, CapacityRules.Interval, interval, (int)(now - at).TotalMinutes));
		return violations;
	}

	/// <summary>Il periodo di disponibilità che copre il giorno; null se il servizio quel giorno non c'è.</summary>
	public static ServiceAvailabilityRule? RuleFor(BookableService service, DateOnly date)
		=> service.Availability.FirstOrDefault(a => (a.From is null || date >= a.From) && (a.To is null || date <= a.To));

	// ----- Tavoli -----

	private async Task<List<CapacityViolation>> ApplyTablesAsync(Reservation reservation, SaveReservationRequest request, CancellationToken ct)
	{
		var violations = new List<CapacityViolation>();
		foreach (var line in reservation.TableLines.Where(t => t.DeletedAt is null).ToList())
		{
			_reservations.RemoveTableLine(line);
			reservation.TableLines.Remove(line);
		}
		var inputs = request.Tables ?? [];
		if (inputs.Count == 0)
			return violations;
		var tables = await TablesAsync(reservation.PropertyId, ct);
		var existing = await _reservations.ListTableLinesAsync(reservation.PropertyId, inputs.Select(t => t.TableServiceId).Distinct().ToList(),
			inputs.Min(t => t.Date), inputs.Max(t => t.Date), reservation.Id, ct);
		var taken = existing.Select(e => (e.TableServiceId, e.ServiceDate, e.SlotStart, e.Covers, Seats: (IReadOnlyList<short>)e.TableSeats)).ToList();

		foreach (var input in inputs)
		{
			if (!tables.TryGetValue(input.TableServiceId, out var service))
				throw DomainException.NotFound(BookingErrors.TableServiceNotFound);
			void Add(string name, int? limit, int? requested) => violations.Add(new CapacityViolation(service.Id, service.Name, input.Date, name, limit, requested));
			var weekday = (int)input.Date.DayOfWeek is 0 ? 7 : (int)input.Date.DayOfWeek;
			var slot = service.Availabilities.Where(a => input.Date >= a.From && input.Date <= a.To)
				.SelectMany(a => a.Slots).FirstOrDefault(s => (s.Weekday is null || s.Weekday == weekday) && s.From == input.SlotStart);
			if (slot is null)
				Add(CapacityRules.TableSlot, null, null);
			var end = slot?.To ?? input.SlotStart.AddMinutes(service.AverageMealMinutes);
			if (service.MinSeatsPerBooking is { } min && input.Covers < min || service.MaxSeatsPerBooking is { } max && input.Covers > max)
				Add(CapacityRules.TableSeats, service.MaxSeatsPerBooking ?? service.MinSeatsPerBooking, input.Covers);
			var sameSlot = taken.Where(t => t.TableServiceId == service.Id && t.ServiceDate == input.Date && t.SlotStart == input.SlotStart).ToList();
			if (service.MaxBookingsPerSlot is { } perSlot && sameSlot.Count + 1 > perSlot)
				Add(CapacityRules.MaxBookingsPerSlot, perSlot, sameSlot.Count + 1);

			IReadOnlyList<short> seats = [];
			if (service.CapacityMode == TableServiceCodes.CapacitySeats)
			{
				var used = sameSlot.Sum(t => t.Covers) + input.Covers;
				if (service.TotalSeats is { } total && used > total)
					Add(CapacityRules.SeatsFull, total, used);
			}
			else
			{
				var free = service.TableTypes.ToDictionary(t => t.Seats, t => t.Count - sameSlot.SelectMany(x => x.Seats).Count(s => s == t.Seats));
				if (input.TableSeats is { Count: > 0 } chosen)
				{
					seats = chosen.ToList();
					if (chosen.GroupBy(c => c).Any(g => free.GetValueOrDefault(g.Key) < g.Count()) || chosen.Sum(c => c) < input.Covers)
						Add(CapacityRules.NoTable, null, input.Covers);
				}
				else if (free.Where(f => f.Value > 0 && f.Key >= input.Covers).OrderBy(f => f.Key).Select(f => (short?)f.Key).FirstOrDefault() is { } best)
					seats = [best];
				else
					Add(CapacityRules.NoTable, null, input.Covers);
			}
			var deposit = service.DepositMode switch
			{
				TableServiceCodes.DepositPerPerson when service.DepositValue is { } v => v * input.Covers,
				TableServiceCodes.DepositTotal when service.DepositType == TableServiceCodes.DepositAmount && service.DepositValue is { } v => v,
				_ => 0m,
			};
			reservation.TableLines.Add(BookingTableLine.Create(reservation, service.Id, service.Name, input.Date, input.SlotStart, end, input.Covers, seats, deposit));
			taken.Add((service.Id, input.Date, input.SlotStart, input.Covers, seats));
		}
		return violations;
	}
}
