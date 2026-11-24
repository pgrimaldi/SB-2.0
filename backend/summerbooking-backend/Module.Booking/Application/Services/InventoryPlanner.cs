using Module.Booking.Domain;
using Module.Booking.Application.Dtos;
using Module.Booking.Application.Abstractions;
using Module.Booking.Domain.Entities;
using Shared.Contracts.Catalog;

namespace Module.Booking.Application.Services;

/// <summary>Quanto di un articolo di magazzino serve a una riga in un giorno.</summary>
public sealed record ItemNeed(BookingLine Line, Guid ItemId, string Name, DateOnly Day, int Quantity);

/// <summary>
/// Magazzino: i pacchetti e gli articoli aggiuntivi usano articoli di magazzino (ombrelloni, lettini, sdraio…) per
/// ogni giorno. Se in un giorno non bastano, dal gestionale è un avviso che il gestore conferma, online blocca. Sotto la
/// soglia di scorta impostata la risposta porta l'avviso <c>inventory_low</c>.
/// </summary>
public sealed class InventoryPlanner
{
	private readonly IBookingServices _catalog;
	private readonly IReservationRepository _reservations;

	public InventoryPlanner(IBookingServices catalog, IReservationRepository reservations)
	{
		_catalog = catalog;
		_reservations = reservations;
	}

	public async Task<(IReadOnlyList<ItemNeed> Needs, IReadOnlyList<CapacityViolation> Violations, bool Low)> PlanAsync(
		Reservation reservation, IReadOnlyList<BookingLine> lines, CancellationToken ct = default)
	{
		var additionals = reservation.AdditionalItems.Where(a => a.DeletedAt is null && a.CatalogItemId is not null).ToList();
		var listIds = lines.Where(l => l.PriceListId is not null).Select(l => l.PriceListId!.Value)
			.Concat(additionals.Select(a => a.CatalogItemId!.Value)).Distinct().ToList();
		if (listIds.Count == 0)
			return ([], [], false);
		var items = (await _catalog.ListPriceListItemsAsync(reservation.PropertyId, listIds, ct)).ToDictionary(p => p.PriceListId, p => p.Items);
		if (items.Values.All(v => v.Count == 0))
			return ([], [], false);
		var inventory = (await _catalog.ListInventoryAsync(reservation.PropertyId, ct)).ToDictionary(i => i.Id);

		var needs = new List<ItemNeed>();
		void Add(BookingLine line, Guid listId, int units, IEnumerable<DateOnly> days)
		{
			foreach (var (itemId, quantity) in items.GetValueOrDefault(listId) ?? [])
				if (inventory.TryGetValue(itemId, out var item))
					foreach (var day in days)
						needs.Add(new ItemNeed(line, itemId, item.Name, day, quantity * units));
		}
		foreach (var line in lines)
		{
			if (line.PriceListId is { } package)
				Add(line, package, line.Quantity, line.EachDay());
			foreach (var add in additionals.Where(a => a.BookingLine == line || (a.BookingLineId == line.Id && line.Id != 0)))
				Add(line, add.CatalogItemId!.Value, add.Quantity, add.Dates.Count > 0 ? add.Dates.Where(line.Covers) : line.EachDay());
		}
		if (needs.Count == 0)
			return ([], [], false);

		var usage = await _reservations.ListItemUsageAsync(reservation.PropertyId, needs.Select(n => n.ItemId).Distinct().ToList(),
			needs.Min(n => n.Day), needs.Max(n => n.Day), reservation.Id, ct);
		var violations = new List<CapacityViolation>();
		var low = false;
		foreach (var group in needs.GroupBy(n => (n.ItemId, n.Day)))
		{
			var item = inventory[group.Key.ItemId];
			var used = usage.Where(u => u.ItemId == group.Key.ItemId && u.Day == group.Key.Day).Sum(u => u.Quantity) + group.Sum(n => n.Quantity);
			if (used > item.TotalQuantity)
				violations.Add(new CapacityViolation(item.Id, item.Name, group.Key.Day, CapacityRules.Inventory, item.TotalQuantity, used));
			else if (item.LowStockThreshold is { } threshold && item.TotalQuantity - used <= threshold)
				low = true;
		}
		return (needs, violations, low);
	}
}
