namespace Shared.Contracts.Catalog;

/// <summary>Durata prenotabile di un servizio orario.</summary>
public sealed record HourlySlotRule(Guid Id, short DurationMinutes, decimal? Price, bool IsActive);

/// <summary>Orari, durate e prezzi di un servizio orario per un insieme di giorni della settimana.</summary>
/// <param name="Weekdays">1 = lunedì … 7 = domenica; vuoto = tutti i giorni.</param>
public sealed record HourlyRule(IReadOnlyList<int> Weekdays, TimeOnly? From, TimeOnly? To, IReadOnlyList<HourlySlotRule> Slots);

/// <summary>
/// Un periodo di disponibilità di un servizio con i suoi limiti. Senza date vale per tutta la stagione. Per i servizi orari
/// fasce fisse e massimo per fascia valgono per tutto il periodo.
/// </summary>
public sealed record ServiceAvailabilityRule(
	DateOnly? From, DateOnly? To, TimeOnly? ManagementFrom, TimeOnly? ManagementTo, TimeOnly? OnlineFrom, TimeOnly? OnlineTo,
	short? MaxPerHour, short? MaxPerDay, short? MaxPerSlot, short? IntervalMinutes, short? MinItems, short? MaxItems,
	bool HasFixedTimeSlots, IReadOnlyList<HourlyRule> Hourly);

/// <param name="Kind">bar, normal, hourly.</param>
public sealed record BookableService(Guid Id, string Kind, string Name, bool IsPurchasable, IReadOnlyList<ServiceAvailabilityRule> Availability);

/// <param name="Weekday">1 = lunedì … 7 = domenica; null = tutti i giorni del periodo.</param>
public sealed record TableSlotRule(int? Weekday, TimeOnly From, TimeOnly To);

public sealed record TableAvailabilityRule(DateOnly From, DateOnly To, IReadOnlyList<TableSlotRule> Slots);

public sealed record TableTypeRule(short Seats, short Count);

/// <summary>Servizio tavoli (pranzo, cena) visto da Booking.</summary>
/// <param name="CapacityMode">tables (con <paramref name="TableTypes"/>) o seats (con <paramref name="TotalSeats"/>).</param>
/// <param name="DepositMode">none, total, per_person.</param>
/// <param name="DepositType">percentage, amount.</param>
public sealed record BookableTableService(
	Guid Id, string Name, bool IsOnlineBookable, string CapacityMode, short? TotalSeats, IReadOnlyList<TableTypeRule> TableTypes,
	short AverageMealMinutes, short? MaxBookingsPerSlot, short? MinSeatsPerBooking, short? MaxSeatsPerBooking, string DepositMode,
	string? DepositType, decimal? DepositValue, IReadOnlyList<TableAvailabilityRule> Availabilities);

/// <summary>Un articolo di magazzino (ombrelloni, lettini, sdraio…) con la quantità totale.</summary>
public sealed record InventoryRule(Guid Id, string Name, int TotalQuantity, int? LowStockThreshold);

/// <summary>Articoli di magazzino che un pacchetto o un articolo aggiuntivo usa, per ogni unità venduta.</summary>
public sealed record PriceListItemsRule(Guid PriceListId, IReadOnlyList<(Guid InventoryItemId, short Quantity)> Items);

/// <summary>Servizi e tavoli di uno stabilimento per Booking (disponibilità, limiti, fasce): lo implementa Catalog.</summary>
public interface IBookingServices
{
	Task<IReadOnlyList<BookableService>> ListServicesAsync(Guid propertyId, CancellationToken ct = default);

	Task<IReadOnlyList<BookableTableService>> ListTableServicesAsync(Guid propertyId, CancellationToken ct = default);

	/// <summary>Magazzino dello stabilimento.</summary>
	Task<IReadOnlyList<InventoryRule>> ListInventoryAsync(Guid propertyId, CancellationToken ct = default);

	/// <summary>Articoli di magazzino dei pacchetti e degli aggiuntivi indicati.</summary>
	Task<IReadOnlyList<PriceListItemsRule>> ListPriceListItemsAsync(Guid propertyId, IReadOnlyCollection<Guid> priceListIds, CancellationToken ct = default);
}
