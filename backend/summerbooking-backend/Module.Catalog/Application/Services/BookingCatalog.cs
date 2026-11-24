using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Shared.Contracts.Catalog;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>
/// Implementazione in-process di <see cref="IBookingCatalog"/> e <see cref="IQuoteCalculator"/> per Booking: postazioni
/// della mappa, prefisso del numero di prenotazione e preventivo del gestionale. Booking non legge le tabelle di Catalog.
/// </summary>
public sealed class BookingCatalog : IBookingCatalog, IQuoteCalculator, IBookingServices, IDiscountRedemption
{
	private readonly IPropertyRepository _properties;
	private readonly IMapRepository _map;
	private readonly ISectorRepository _sectors;
	private readonly ISeasonRepository _seasons;
	private readonly IServiceCatalogRepository _services;
	private readonly IOnlineSettingsRepository _online;
	private readonly IPublicCatalogRepository _public;
	private readonly IPropertyInfoRepository _info;
	private readonly IPlatformRepository _platform;
	private readonly IDiscountRepository _discounts;
	private readonly IInventoryItemRepository _inventory;
	private readonly IPriceListRepository _priceLists;
	private readonly QuoteService _quotes;

	public BookingCatalog(
		IPropertyRepository properties, IMapRepository map, ISectorRepository sectors, ISeasonRepository seasons, IServiceCatalogRepository services,
		IOnlineSettingsRepository online, IPublicCatalogRepository publicCatalog, IPropertyInfoRepository info, IPlatformRepository platform,
		IDiscountRepository discounts, IInventoryItemRepository inventory, IPriceListRepository priceLists, QuoteService quotes)
	{
		_discounts = discounts;
		_inventory = inventory;
		_priceLists = priceLists;
		_online = online;
		_public = publicCatalog;
		_info = info;
		_platform = platform;
		_properties = properties;
		_map = map;
		_sectors = sectors;
		_seasons = seasons;
		_services = services;
		_quotes = quotes;
	}

	public async Task<IReadOnlyList<BookableService>> ListServicesAsync(Guid propertyId, CancellationToken ct = default)
	{
		if (await _properties.FindIdAsync(propertyId, ct) is not { } id)
			return [];
		static int Iso(Weekday d) => (int)d + 1;
		static HourlyRule Rule(ServiceHourlyOption h) => new(
			h.Weekdays.Select(Iso).ToList(), h.TimeFrom, h.TimeTo,
			h.Slots.Where(s => s.DeletedAt is null).OrderBy(s => s.SortOrder)
				.Select(s => new HourlySlotRule(s.PublicId, s.DurationMinutes ?? 0, s.Price, s.IsActive)).ToList());
		return (await _services.ListWithAvailabilityAsync(id, ct)).Select(s =>
		{
			var hourly = s.HourlyOptions.Where(h => h.DeletedAt is null).ToList();
			var rules = s.BookingOptions.Where(o => o.DeletedAt is null).OrderBy(o => o.SortOrder).Select(o =>
			{
				var own = hourly.Where(h => h.BookingOptionId == o.Id).OrderBy(h => h.SortOrder).ToList();
				return new ServiceAvailabilityRule(
					o.StartsOn, o.EndsOn, o.ManagementFrom, o.ManagementTo, o.OnlineFrom, o.OnlineTo, o.MaxPerHour, o.MaxPerDay,
					o.MaxPerSlot ?? own.Select(h => h.MaxPerSlot).FirstOrDefault(m => m is not null), o.IntervalMinutes, o.MinItems, o.MaxItems,
					own.Any(h => h.HasFixedTimeSlots), own.Select(Rule).ToList());
			}).ToList();
			// disponibilità orarie dei prezzi per periodo tariffario: valgono per tutta la stagione
			var byPeriod = hourly.Where(h => h.BookingOptionId is null).OrderBy(h => h.SortOrder).ToList();
			if (byPeriod.Count > 0)
				rules.Add(new ServiceAvailabilityRule(null, null, null, null, null, null, null, null,
					byPeriod.Select(h => h.MaxPerSlot).FirstOrDefault(m => m is not null), null, null, null,
					byPeriod.Any(h => h.HasFixedTimeSlots), byPeriod.Select(Rule).ToList()));
			return new BookableService(s.PublicId, Codes.Of(s.Kind), s.Name, s.IsPurchasable, rules);
		}).ToList();
	}

	public async Task<IReadOnlyList<InventoryRule>> ListInventoryAsync(Guid propertyId, CancellationToken ct = default)
		=> await _properties.FindIdAsync(propertyId, ct) is { } id
			? (await _inventory.ListAllAsync(id, ct)).Select(i => new InventoryRule(i.PublicId, i.Name, i.TotalQuantity, i.LowStockAlertEnabled ? i.LowStockThreshold : null)).ToList()
			: [];

	public async Task<IReadOnlyList<PriceListItemsRule>> ListPriceListItemsAsync(Guid propertyId, IReadOnlyCollection<Guid> priceListIds, CancellationToken ct = default)
	{
		if (priceListIds.Count == 0 || await _properties.FindIdAsync(propertyId, ct) is not { } id)
			return [];
		var inventory = (await _inventory.ListAllAsync(id, ct)).ToDictionary(i => i.Id, i => i.PublicId);
		return (await _priceLists.FindManyAsync(id, priceListIds, ct)).Select(p => new PriceListItemsRule(p.PublicId,
			p.Items.Where(i => i.DeletedAt is null && i.InventoryItemId is { } inv && inventory.ContainsKey(inv))
				.Select(i => (inventory[i.InventoryItemId!.Value], i.Quantity)).ToList())).ToList();
	}

	public async Task<IReadOnlyList<BookableTableService>> ListTableServicesAsync(Guid propertyId, CancellationToken ct = default)
	{
		if (await _properties.FindIdAsync(propertyId, ct) is not { } id)
			return [];
		return (await _services.ListTablesAsync(id, ct)).Select(t => new BookableTableService(
			t.PublicId, t.Name, t.IsOnlineBookable, Codes.Of(t.CapacityMode), t.TotalSeats,
			t.TableTypes.Where(x => x.DeletedAt is null).OrderBy(x => x.Seats).Select(x => new TableTypeRule(x.Seats, x.Count)).ToList(),
			t.AverageMealMinutes, t.MaxBookingsPerSlot, t.MinSeatsPerBooking, t.MaxSeatsPerBooking, Codes.Of(t.DepositMode),
			t.DepositType is { } dt ? Codes.Of(dt) : null, t.DepositValue,
			t.Availabilities.Where(a => a.DeletedAt is null).OrderBy(a => a.StartsOn).Select(a => new TableAvailabilityRule(a.StartsOn, a.EndsOn,
				a.Slots.Where(x => x.DeletedAt is null).OrderBy(x => x.From)
					.Select(x => new TableSlotRule(x.Weekday is { } w ? (int)w + 1 : null, x.From, x.To)).ToList())).ToList())).ToList();
	}

	public async Task<IReadOnlyList<BookableSpot>> ListSpotsAsync(Guid propertyId, CancellationToken ct = default)
	{
		if (await _properties.FindIdAsync(propertyId, ct) is not { } id)
			return [];

		var sectors = (await _sectors.ListAsync(id, ct)).ToDictionary(s => s.Id);
		return (await _map.ListSpotsAsync(id, ct))
			.Where(s => s.SpotType != SpotType.BeachElement)
			.Select(s =>
			{
				var sector = s.SectorId is { } sid && sectors.TryGetValue(sid, out var found) ? found : null;
				return new BookableSpot(
					s.PublicId, s.Label, Codes.Of(s.SpotType), s.IsArea, s.AreaSeatCount, sector?.PublicId, sector?.Name,
					s.GridRow, s.GridColumn, s.IsAgreementOnly, s.IsBookable);
			})
			.ToList();
	}

	public async Task<string?> FindNumberPrefixAsync(Guid propertyId, CancellationToken ct = default)
		=> await _properties.FindIdAsync(propertyId, ct) is { } id ? id.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;

	public async Task<OnlineBookingRules?> FindOnlineRulesBySlugAsync(string slug, CancellationToken ct = default)
		=> await _public.FindPublishedBySlugAsync(slug.Trim().ToLowerInvariant(), ct) is { } row ? await RulesAsync(row.PropertyId, ct) : null;

	public async Task<OnlineBookingRules?> FindOnlineRulesAsync(Guid propertyId, CancellationToken ct = default)
		=> await _properties.FindIdAsync(propertyId, ct) is { } id ? await RulesAsync(id, ct) : null;

	private async Task<OnlineBookingRules?> RulesAsync(long propertyId, CancellationToken ct)
	{
		var publicIds = await _platform.FindPropertyPublicIdsAsync([propertyId], ct);
		if (!publicIds.TryGetValue(propertyId, out var publicId))
			return null;
		var settings = await _online.FindAsync(propertyId, ct);
		var bathhouse = await _info.FindBathhouseAsync(propertyId, ct);
		var policy = bathhouse?.CancellationPolicy;
		return new OnlineBookingRules(
			publicId, bathhouse?.Name ?? "", settings?.IsEnabled ?? false, settings?.RequiresManualConfirmation ?? false,
			settings?.AllowsSameDayBooking ?? false, settings?.MaxBookingsPerDay, settings?.MaxSpotsPerBooking,
			settings?.AllowOnlineBookingScope == OnlineBookingScope.ContactsOnly, settings?.AllowsSharing ?? false,
			(settings?.EnabledPaymentMethods ?? []).Select(m => Codes.Of(m)).ToList(), settings?.IsOnlinePaymentBlocked ?? false,
			policy?.IsFreeCancellation ?? false, policy?.NoticeHours,
			bathhouse is null ? [] : bathhouse.CancellationPenaltyHours.Zip(bathhouse.CancellationPenaltyPercents, (h, p) => new OnlineCancellationPenalty(h, p)).ToList());
	}

	public async Task<PriceQuote> QuoteOnlineAsync(Guid propertyId, PriceQuoteRequest request, CancellationToken ct = default)
	{
		var id = await _properties.FindIdAsync(propertyId, ct) ?? throw new InvalidOperationException("Stabilimento inesistente.");
		return ToQuote(await _quotes.QuoteOnlineForPropertyAsync(id, ToCatalogRequest(request), ct));
	}

	public async Task<BookingSeason?> FindSeasonAsync(Guid propertyId, CancellationToken ct = default)
		=> await _properties.FindIdAsync(propertyId, ct) is { } id && await _seasons.FindAsync(id, ct) is { } s
			? new BookingSeason(s.SeasonStartsOn, s.SeasonEndsOn, s.NextSeasonStartsOn, s.NextSeasonEndsOn)
			: null;

	public async Task<PriceQuote> QuoteAsync(PriceQuoteRequest request, CancellationToken ct = default)
	{
		var catalogRequest = ToCatalogRequest(request);
		var quote = request.NextSeason ? await _quotes.QuoteNextSeasonAsync(catalogRequest, ct) : await _quotes.QuoteAsync(catalogRequest, request.RedeemedDiscountId, ct);
		return ToQuote(quote);
	}

	public async Task<IReadOnlyList<decimal>?> QuoteCumulativeBlocksAsync(
		Guid propertyId, Guid spotId, Guid priceListId, IReadOnlyList<PriceQuoteBlock> blocks, bool nextSeason, CancellationToken ct = default)
		=> await _properties.FindIdAsync(propertyId, ct) is { } id
			? await _quotes.CumulativeBlocksAsync(id, spotId, priceListId, blocks.Select(b => (b.From, b.To)).ToList(), nextSeason, ct)
			: null;

	public async Task<bool> TryRedeemAsync(Guid propertyId, Guid discountId, CancellationToken ct = default)
		=> await _properties.FindIdAsync(propertyId, ct) is { } id && await _discounts.TryRedeemAsync(id, discountId, ct);

	public async Task ReleaseAsync(Guid propertyId, Guid discountId, CancellationToken ct = default)
	{
		if (await _properties.FindIdAsync(propertyId, ct) is { } id)
			await _discounts.ReleaseAsync(id, discountId, ct);
	}

	private static QuoteRequest ToCatalogRequest(PriceQuoteRequest request)
		=> new(
			request.From, request.To, request.DayPart, request.Seasonal,
			request.Spots.Select(s => new QuoteSpotRequest(
				s.SpotId, null, s.PriceListId, s.AgreementPriceListId, s.VoucherPriceListId, s.Absences, s.ManualPrice,
				s.Additionals?.Select(a => new QuoteAdditionalRequest(a.PriceListId, a.Quantity, a.Dates)).ToList())).ToList(),
			request.Services?.Select(s => new QuoteServiceRequest(s.ServiceId, s.Quantity, s.Variant, s.Date, s.TimeSlotId)).ToList(),
			request.DiscountCode);

	private static PriceQuote ToQuote(QuoteDto quote)
		=> new(
			quote.Lines.Select(l => new PriceQuoteLine(l.Kind, l.RefId, l.SpotId, l.Description, l.Quantity, l.Amount, l.VatRate)).ToList(),
			quote.Subtotal, quote.Discount?.Amount ?? 0m, quote.Total,
			quote.Discount is { } d ? new PriceQuoteDiscount(d.Id, d.Code, d.Name, d.Type, d.Value, d.Amount) : null,
			quote.Warnings);
}
