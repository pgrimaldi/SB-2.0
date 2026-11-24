using Module.Booking.Application.Dtos;
using Module.Booking.Application.Abstractions;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Catalog;
using Shared.Contracts.Identity;

namespace Module.Booking.Application.Services;

/// <summary>Planner dei servizi orari e dei tavoli di un giorno (l'occupazione; il disegno dei servizi è di Catalog).</summary>
public sealed class ServicePlannerService
{
	private readonly IReservationRepository _reservations;
	private readonly ServiceWriter _services;
	private readonly ICustomerDirectory _customers;
	private readonly BookingScope _scope;

	public ServicePlannerService(IReservationRepository reservations, ServiceWriter services, ICustomerDirectory customers, BookingScope scope)
	{
		_reservations = reservations;
		_services = services;
		_customers = customers;
		_scope = scope;
	}

	public async Task<ServicePlannerDto> HourlyAsync(DateOnly? date, CancellationToken ct = default)
	{
		var day = date ?? _scope.Today;
		var propertyId = _scope.PropertyId;
		var hourly = (await _services.ServicesAsync(propertyId, ct)).Values.Where(s => s.Kind == ServiceKinds.Hourly).OrderBy(s => s.Name).ToList();
		var slots = await _reservations.ListServiceSlotsAsync(propertyId, day, ct);
		var names = await NamesAsync(slots.Select(s => s.Reservation.CustomerId), ct);
		var weekday = (int)day.DayOfWeek is 0 ? 7 : (int)day.DayOfWeek;

		var result = hourly.Select(service =>
		{
			var rule = ServiceWriter.RuleFor(service, day);
			var hours = rule?.Hourly.FirstOrDefault(h => h.Weekdays.Count == 0 || h.Weekdays.Contains(weekday));
			var bookings = slots.Where(s => s.ServiceLine.SaleItemId == service.Id)
				.Select(s => new ServiceBookingDto(s.Reservation.PublicId, s.Reservation.BookingNumber,
					s.Reservation.CustomerId is { } c ? names.GetValueOrDefault(c) : null, s.SlotStart, s.SlotEnd, s.Seats))
				.OrderBy(b => b.Start).ToList();
			var grid = new List<ServiceSlotDto>();
			if (hours is { From: { } from, To: { } to })
			{
				// fasce fisse: la durata più breve; altrimenti un'ora
				var step = rule!.HasFixedTimeSlots && hours.Slots.Where(s => s.IsActive && s.DurationMinutes > 0).Select(s => (int)s.DurationMinutes).DefaultIfEmpty(60).Min() is var m
					? m : 60;
				for (var t = from; t < to && grid.Count < 200; t = t.AddMinutes(step))
				{
					var end = t.AddMinutes(step) > to || t.AddMinutes(step) < t ? to : t.AddMinutes(step);
					grid.Add(new ServiceSlotDto(t, end, bookings.Where(b => b.Start < end && b.End > t).Sum(b => b.Quantity), rule.MaxPerSlot));
				}
			}
			return new HourlyServicePlanDto(service.Id, service.Name, hours is not null, hours?.From, hours?.To, rule?.HasFixedTimeSlots ?? false,
				rule?.MaxPerSlot, grid, bookings);
		}).ToList();
		return new ServicePlannerDto(day, result);
	}

	public async Task<TablePlannerDto> TablesAsync(DateOnly? date, CancellationToken ct = default)
	{
		var day = date ?? _scope.Today;
		var propertyId = _scope.PropertyId;
		var services = (await _services.TablesAsync(propertyId, ct)).Values.OrderBy(s => s.Name).ToList();
		var lines = await _reservations.ListTableLinesAsync(propertyId, services.Select(s => s.Id).ToList(), day, day, 0, ct);
		var names = await NamesAsync(lines.Select(l => l.Reservation.CustomerId), ct);
		var weekday = (int)day.DayOfWeek is 0 ? 7 : (int)day.DayOfWeek;

		return new TablePlannerDto(day, services.Select(service =>
		{
			var slots = service.Availabilities.Where(a => day >= a.From && day <= a.To).SelectMany(a => a.Slots)
				.Where(s => s.Weekday is null || s.Weekday == weekday).OrderBy(s => s.From).ToList();
			return new TableServicePlanDto(service.Id, service.Name, service.CapacityMode, slots.Select(slot =>
			{
				var booked = lines.Where(l => l.TableServiceId == service.Id && l.SlotStart == slot.From).ToList();
				var free = service.TableTypes.ToDictionary(t => t.Seats, t => t.Count - booked.SelectMany(b => b.TableSeats).Count(s => s == t.Seats));
				return new TableSlotPlanDto(slot.From, slot.To, booked.Count, booked.Sum(b => b.Covers), service.MaxBookingsPerSlot,
					service.CapacityMode == "seats" && service.TotalSeats is { } total ? total - booked.Sum(b => b.Covers) : null,
					free, booked.Select(b => new TableBookingDto(b.Reservation.PublicId, b.Reservation.BookingNumber,
						b.Reservation.CustomerId is { } c ? names.GetValueOrDefault(c) : null, b.Covers, b.TableSeats)).ToList());
			}).ToList());
		}).ToList());
	}

	private async Task<Dictionary<Guid, string?>> NamesAsync(IEnumerable<Guid?> ids, CancellationToken ct)
	{
		var list = ids.Where(i => i is not null).Select(i => i!.Value).Distinct().ToList();
		return list.Count == 0 ? [] : (await _customers.ListAsync(_scope.PropertyId, list, ct)).ToDictionary(c => c.Id, c => c.FullName);
	}
}
