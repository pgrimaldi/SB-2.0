namespace Module.Booking.Application.Dtos;

/// <summary>Una prenotazione di un servizio orario nel planner del giorno.</summary>
public sealed record ServiceBookingDto(Guid ReservationId, string BookingNumber, string? CustomerName, TimeOnly Start, TimeOnly End, short Quantity);

/// <summary>Una fascia del planner: quanto è prenotato e il massimo, se c'è.</summary>
public sealed record ServiceSlotDto(TimeOnly Start, TimeOnly End, int Booked, int? Capacity);

/// <summary>Un servizio orario nel giorno: orari, fasce (con le fasce fisse) e prenotazioni.</summary>
public sealed record HourlyServicePlanDto(
	Guid ServiceId, string Name, bool IsAvailable, TimeOnly? From, TimeOnly? To, bool HasFixedTimeSlots, short? MaxPerSlot,
	IReadOnlyList<ServiceSlotDto> Slots, IReadOnlyList<ServiceBookingDto> Bookings);

public sealed record ServicePlannerDto(DateOnly Date, IReadOnlyList<HourlyServicePlanDto> Services);

/// <summary>Un tavolo prenotato nel giorno.</summary>
public sealed record TableBookingDto(Guid ReservationId, string BookingNumber, string? CustomerName, short Covers, IReadOnlyList<short> TableSeats);

/// <summary>Una fascia di un servizio tavoli: prenotazioni, coperti, tavoli liberi per posti.</summary>
public sealed record TableSlotPlanDto(
	TimeOnly From, TimeOnly To, int Bookings, int Covers, int? MaxBookings, int? SeatsLeft, IReadOnlyDictionary<short, int> FreeTables,
	IReadOnlyList<TableBookingDto> Reservations);

public sealed record TableServicePlanDto(Guid TableServiceId, string Name, string CapacityMode, IReadOnlyList<TableSlotPlanDto> Slots);

public sealed record TablePlannerDto(DateOnly Date, IReadOnlyList<TableServicePlanDto> Services);
