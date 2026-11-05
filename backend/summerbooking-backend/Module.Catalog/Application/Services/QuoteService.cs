using BuildingBlocks.Application.Validation;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Application.Validators;
using Module.Catalog.Domain;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Module.Catalog.Domain.Pricing;
using Shared.Contracts.Errors;
using Shared.Infrastructure;
using Shared.Localization;
using Shared.Utils;

namespace Module.Catalog.Application.Services;

/// <summary>
/// Preventivo di una prenotazione, calcolato sempre dal backend (mai dal client): postazioni con pacchetto e
/// articoli aggiuntivi, servizi e prezzi automatici, supplemento online, un solo sconto. Precedenze per una postazione:
/// prezzo dell'operatore, stagionale, stagionale ridotto (date identiche al periodo), poi tratto per tratto la griglia
/// del pacchetto con weekend e festivi, il prezzo suggerito scelto dal gestore (pacchetto predefinito, «giornaliero con
/// weekend», tratto di un giorno) e il cumulativo. Disponibilità, capienza e conteggio degli utilizzi sono di Booking.
/// </summary>
public sealed class QuoteService
{
	private static readonly TimeZoneInfo Rome = TimeZoneInfo.FindSystemTimeZoneById("Europe/Rome");

	private readonly IPriceListRepository _priceLists;
	private readonly IAgreementRepository _agreements;
	private readonly IPlatformRepository _platform;
	private readonly IPublicCatalogRepository _public;
	private readonly ISectorRepository _sectors;
	private readonly IMapRepository _map;
	private readonly IOnlineSettingsRepository _online;
	private readonly IRevenueRepository _revenue;
	private readonly IServiceCatalogRepository _services;
	private readonly CurrentProperty _property;
	private readonly IRequestLanguage _language;
	private readonly IDateTimeProvider _clock;
	private readonly QuoteRequestValidator _validator;

	public QuoteService(
		IPriceListRepository priceLists, IAgreementRepository agreements, IPlatformRepository platform, IPublicCatalogRepository publicCatalog,
		ISectorRepository sectors, IMapRepository map, IOnlineSettingsRepository online, IRevenueRepository revenue, IServiceCatalogRepository services,
		CurrentProperty property, IRequestLanguage language, IDateTimeProvider clock, QuoteRequestValidator validator)
	{
		_priceLists = priceLists;
		_agreements = agreements;
		_platform = platform;
		_public = publicCatalog;
		_sectors = sectors;
		_map = map;
		_online = online;
		_revenue = revenue;
		_services = services;
		_property = property;
		_language = language;
		_clock = clock;
		_validator = validator;
	}

	/// <summary>Preventivo dal gestionale, per lo stabilimento del token.</summary>
	public async Task<QuoteDto> QuoteAsync(QuoteRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		return await BuildAsync(await _property.IdAsync(ct), QuoteChannel.Manager, request, ct);
	}

	/// <summary>Preventivo dal sito pubblico: valgono le regole della prenotazione online dello stabilimento.</summary>
	public async Task<QuoteDto> QuoteOnlineAsync(string slug, QuoteRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var row = await _public.FindPublishedBySlugAsync(slug.Trim().ToLowerInvariant(), ct) ?? throw DomainException.NotFound(CatalogErrors.BathhouseNotFound);
		if (!row.IsOnlineBookable)
			throw DomainException.Unprocessable(CatalogErrors.QuoteOnlineUnavailable);
		return await BuildAsync(row.PropertyId, QuoteChannel.Online, request, ct);
	}

	private async Task<QuoteDto> BuildAsync(long propertyId, QuoteChannel channel, QuoteRequest r, CancellationToken ct)
	{
		var part = r.DayPart switch { "morning" => DayPart.Morning, "afternoon" => DayPart.Afternoon, _ => DayPart.FullDay };
		var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(_clock.UtcNow, Rome);
		var today = DateOnly.FromDateTime(nowLocal);
		var periods = await _platform.FindSeasonPeriodsAsync(propertyId, ct);
		var sectors = (await _sectors.ListAsync(propertyId, ct)).ToDictionary(s => s.PublicId);
		var spotRows = (await _map.ListSpotsAsync(propertyId, ct)).ToDictionary(s => s.PublicId);
		var online = await _online.FindAsync(propertyId, ct);
		var warnings = new List<string>();
		var spots = r.Spots ?? [];

		// sorgenti dei prezzi
		var listIds = spots.SelectMany(s => (s.Additionals ?? []).Select(a => a.PriceListId)).Concat(spots.Where(s => s.PriceListId is not null).Select(s => s.PriceListId!.Value)).Distinct().ToList();
		var lists = (await _priceLists.FindManyAsync(propertyId, listIds, ct)).ToDictionary(l => l.PublicId);
		var agreementIds = spots.SelectMany(s => new[] { s.AgreementPriceListId, s.VoucherPriceListId }).Where(i => i is not null).Select(i => i!.Value)
			.Concat(spots.Where(s => s.AgreementPriceListId is not null).SelectMany(s => (s.Additionals ?? []).Select(a => a.PriceListId))).Distinct().ToList();
		var agreementLists = (await _agreements.FindPriceListsAsync(propertyId, agreementIds, ct)).ToDictionary(l => l.PublicId);
		var reducedSeasons = await _priceLists.ListReducedSeasonsAsync(propertyId, ct);
		var revenueSettings = await _revenue.FindSettingsAsync(propertyId, ct);
		var methods = (await _priceLists.ListCalculationMethodsAsync(ct)).ToDictionary(m => m.Id, m => m.Code);

		if (channel == QuoteChannel.Online)
			ValidateOnline(r, part, today, online, spots, spotRows, sectors);

		var lines = new List<Line>();
		var cumulativeCandidates = new List<(Line Line, PricingSource Source, long? Sector, IReadOnlyList<PricingSegment> Segments)>();
		foreach (var spot in spots)
		{
			var spotRow = spot.SpotId is { } sid ? spotRows.GetValueOrDefault(sid) ?? throw DomainException.NotFound(CatalogErrors.MapSpotNotFound) : null;
			var sectorId = spotRow?.SectorId ?? (spot.SectorId is { } sec ? sectors.GetValueOrDefault(sec)?.Id ?? throw DomainException.NotFound(CatalogErrors.SectorNotFound) : null);
			var sectorPublic = sectorId is { } x ? sectors.Values.FirstOrDefault(s => s.Id == x)?.PublicId : null;
			if (channel == QuoteChannel.Online && spot.ManualPrice is not null)
				throw DomainException.Unprocessable(CatalogErrors.QuoteManualPriceNotAllowed);

			var package = spot.PriceListId is { } pid
				? PricingSource.From(lists.GetValueOrDefault(pid) ?? throw DomainException.NotFound(CatalogErrors.PriceListNotFound), reducedSeasons)
				: PricingSource.From(agreementLists.GetValueOrDefault(spot.AgreementPriceListId!.Value) ?? throw DomainException.NotFound(CatalogErrors.PriceListNotFound), methods);
			if (package.Kind != PriceListKind.Package)
				throw DomainException.Unprocessable(CatalogErrors.QuoteNotAPackage);
			if (channel == QuoteChannel.Online && !package.IsOnline)
				throw DomainException.Unprocessable(CatalogErrors.QuotePackageNotOnline);
			if (package.IsSectorRestricted && (sectorId is null || !package.HasSector(sectorId.Value)))
				throw DomainException.Unprocessable(CatalogErrors.QuotePackageNotForSector);

			if (spot.PriceListId is not null && package.IsDefault && revenueSettings is { IsSuggestedPriceEnabled: true })
				package.WithSuggestedPrices(await _revenue.ListSuggestedPricesAsync(package.Id, r.From, r.To, ct));
			var absences = (spot.Absences ?? []).ToHashSet();
			var line = new Line("package", package.PublicId, spot.SpotId, sectorPublic, package.Name, 1, package.VatRate, package.VatNature);
			var segments = PackagePricer.Segments(periods, r.From, r.To, absences);
			PriceSpot(line, package, sectorId, segments, part, r, spot.ManualPrice, revenueSettings, spotRow?.Id, warnings);
			lines.Add(line);
			if (package.IsCumulative && part == DayPart.FullDay && line.Rule == "grid")
				cumulativeCandidates.Add((line, package, sectorId, segments));

			// voucher della struttura convenzionata: si sottrae dal pacchetto, fino a 0
			if (spot.VoucherPriceListId is { } vid)
			{
				var voucher = agreementLists.GetValueOrDefault(vid) is { ListType: AgreementListType.Voucher } v
					? PricingSource.From(v, methods) : throw DomainException.NotFound(CatalogErrors.QuoteVoucherNotFound);
				var voucherLine = new Line("voucher", voucher.PublicId, spot.SpotId, sectorPublic, voucher.Name, 1, voucher.VatRate, voucher.VatNature);
				PriceSpot(voucherLine, voucher, sectorId, segments, part, r, null, null, null, warnings);
				voucherLine.Amount = -Math.Min(voucherLine.Amount, line.Amount);
				voucherLine.Rule = "voucher";
				lines.Add(voucherLine);
			}

			// articoli aggiuntivi
			var additionalCount = 0;
			foreach (var a in spot.Additionals ?? [])
			{
				var additional = spot.PriceListId is not null
					? PricingSource.From(lists.GetValueOrDefault(a.PriceListId) ?? throw DomainException.NotFound(CatalogErrors.PriceListNotFound), reducedSeasons)
					: PricingSource.From(agreementLists.GetValueOrDefault(a.PriceListId) ?? throw DomainException.NotFound(CatalogErrors.PriceListNotFound), methods);
				if (additional.Kind != PriceListKind.Additional)
					throw DomainException.Unprocessable(CatalogErrors.QuoteNotAnAdditional);
				if (channel == QuoteChannel.Online && !additional.IsOnline)
					throw DomainException.Unprocessable(CatalogErrors.QuotePackageNotOnline);
				var links = additional.PackageLinks;
				if (links.Count > 0)
				{
					if (!links.TryGetValue(package.Id, out var maxCount))
						throw DomainException.Unprocessable(CatalogErrors.QuoteAdditionalNotLinked);
					if (maxCount > 0 && a.Quantity > maxCount)
						throw DomainException.Unprocessable(CatalogErrors.QuoteTooManyAdditionals, new Dictionary<string, object?> { ["max"] = maxCount });
				}
				additionalCount += a.Quantity;
				var onlyDays = a.Dates is { Count: > 0 } d ? d.Where(x => x >= r.From && x <= r.To).ToHashSet() : null;
				var addSegments = PackagePricer.Segments(periods, r.From, r.To, absences, onlyDays);
				var addLine = new Line("additional", additional.PublicId, spot.SpotId, sectorPublic, additional.Name, a.Quantity, additional.VatRate, additional.VatNature);
				PriceSpot(addLine, additional, sectorId, addSegments, part, r with { Seasonal = r.Seasonal && onlyDays is null }, null, null, null, warnings);
				addLine.Amount *= a.Quantity;
				lines.Add(addLine);
				if (additional.IsCumulative && package.IsCumulative && part == DayPart.FullDay && addLine.Rule == "grid" && a.Quantity == 1)
					cumulativeCandidates.Add((addLine, additional, sectorId, addSegments));
			}
			if (package.MaxAdditionalItems is { } maxItems && additionalCount > maxItems)
				throw DomainException.Unprocessable(CatalogErrors.QuoteTooManyAdditionals, new Dictionary<string, object?> { ["max"] = maxItems });
		}

		ApplyCumulative(cumulativeCandidates);

		// servizi e prezzi automatici
		var serviceLines = await PriceServicesAsync(propertyId, channel, r, periods, spots, spotRows, sectors, nowLocal, ct);
		lines.AddRange(serviceLines);

		// supplemento online
		if (channel == QuoteChannel.Online)
			lines.AddRange(await SurchargesAsync(propertyId, online, lines, ct));

		var subtotal = decimal.Round(lines.Sum(l => l.Amount), 2, MidpointRounding.AwayFromZero);
		var discount = await DiscountAsync(propertyId, channel, r, lines, nowLocal, ct);
		var total = Math.Max(0, subtotal - (discount?.Amount ?? 0));
		var requiresConfirmation = channel == QuoteChannel.Online && online is { RequiresManualConfirmation: true };
		return new QuoteDto(lines.Select(l => l.ToDto()).ToList(), subtotal, discount, decimal.Round(total, 2, MidpointRounding.AwayFromZero), requiresConfirmation,
			warnings.Distinct().ToList());
	}

	private void PriceSpot(
		Line line, PricingSource source, long? sectorId, IReadOnlyList<PricingSegment> segments, DayPart part, QuoteRequest r, decimal? manualPrice,
		RevenueSettings? revenue, long? spotId, List<string> warnings)
	{
		if (manualPrice is { } manual)
		{
			line.Set(manual, "manual");
			return;
		}
		if (r.Seasonal)
		{
			var (seasonal, seasonalHalf) = source.Seasonal(sectorId);
			var price = part == DayPart.FullDay ? seasonal : seasonalHalf;
			if (price is null)
				warnings.Add("seasonal_price_missing");
			line.Set(price ?? 0, "seasonal");
			return;
		}
		if (source.Reduced(sectorId, r.From, r.To) is { } reduced)
		{
			line.Set(reduced, "reduced");
			return;
		}

		var rule = "grid";
		foreach (var segment in segments)
		{
			if (segment.PeriodGroup is not { } group)
			{
				warnings.Add("days_without_period");
				line.Segments.Add(new SegmentPrice(segment, 0, "no_period"));
				continue;
			}
			if (source.Rates(sectorId, group) is not { } rates)
			{
				warnings.Add("period_without_price");
				line.Segments.Add(new SegmentPrice(segment, 0, "no_price"));
				continue;
			}
			var amount = PackagePricer.Price(source.Rules, rates, segment, part);
			var segmentRule = "grid";
			if (revenue is { IsSuggestedPriceEnabled: true } && source.IsDefault && source.Rules.Method == PricingMethod.DailyWithWeekend
				&& part == DayPart.FullDay && segment.Span == 1 && segment.Days.Count == 1
				&& source.SuggestedPrice(segment.From, sectorId) is { } suggested)
			{
				var basePrice = PackagePricer.DayBasePrice(source.Rules, rates, segment.From);
				var spotAllowed = !revenue.IsDiscountLimitedToSpots || spotId is { } s && revenue.DiscountedSpots.Any(d => d.SpotId == s);
				(amount, segmentRule) = suggested.AppliedMode switch
				{
					SuggestedPriceMode.Suggested => (suggested.SuggestedPriceValue, "revenue_suggested"),
					SuggestedPriceMode.Original => (basePrice, "revenue_original"),
					SuggestedPriceMode.Discount when spotAllowed => (basePrice - basePrice * suggested.DiscountPercentage / 100, "revenue_discount"),
					_ => (amount, segmentRule),
				};
				if (segmentRule != "grid")
					rule = segmentRule;
			}
			line.Segments.Add(new SegmentPrice(segment, decimal.Round(amount, 2, MidpointRounding.AwayFromZero), segmentRule));
		}
		line.Amount = line.Segments.Sum(s => s.Amount);
		line.Rule = rule;
	}

	/// <summary>Cumulativo: per pacchetto (o articolo aggiuntivo) e fila, con almeno due postazioni, periodo per periodo.</summary>
	private static void ApplyCumulative(List<(Line Line, PricingSource Source, long? Sector, IReadOnlyList<PricingSegment> Segments)> candidates)
	{
		foreach (var group in candidates.GroupBy(c => (c.Source.Kind, c.Source.Id, c.Sector)).Where(g => g.Count() >= 2))
		{
			var items = group.ToList();
			foreach (var item in items)
			{
				item.Line.Segments.Clear();
				item.Line.Amount = 0;
				item.Line.Rule = "cumulative";
			}
			foreach (var period in items.SelectMany(i => i.Segments).Where(s => s.PeriodGroup is not null).Select(s => s.PeriodGroup!.Value).Distinct())
			{
				var perItem = items.Select(i => i.Segments.Where(s => s.PeriodGroup == period).ToList()).ToList();
				var days = perItem.Select(s => s.Sum(x => x.Days.Count)).ToList();
				if (items[0].Source.Rates(items[0].Sector, period) is not { } rates)
					continue;
				var first = perItem.SelectMany(s => s).Min(s => s.From);
				var total = PackagePricer.FullDayGrid(items[0].Source.Rules, rates, first, days.Sum()).For(days.Sum());
				var shares = CumulativePricing.Distribute(total, days);
				for (var i = 0; i < items.Count; i++)
				{
					foreach (var segment in perItem[i])
						items[i].Line.Segments.Add(new SegmentPrice(segment, segment == perItem[i][^1] ? shares[i] : 0, "cumulative"));
					items[i].Line.Amount += shares[i];
				}
			}
		}
	}

	private async Task<List<Line>> PriceServicesAsync(
		long propertyId, QuoteChannel channel, QuoteRequest r, IReadOnlyList<TariffPeriod> periods, IReadOnlyList<QuoteSpotRequest> spots,
		IReadOnlyDictionary<Guid, MapSpot> spotRows, IReadOnlyDictionary<Guid, MapSector> sectors, DateTime nowLocal, CancellationToken ct)
	{
		var lines = new List<Line>();
		var requests = r.Services ?? [];
		if (requests.Count == 0)
			return lines;
		var items = (await _public.FindServicesAsync(propertyId, requests.Select(s => s.ServiceId).Distinct().ToList(), ct)).ToDictionary(s => s.PublicId);
		var language = _language.Language;
		var ordered = new List<(ServiceSaleItem Item, short Quantity, DateOnly Date)>();
		foreach (var s in requests)
		{
			var item = items.GetValueOrDefault(s.ServiceId) ?? throw DomainException.NotFound(CatalogErrors.ServiceNotFound);
			if (channel == QuoteChannel.Online && !item.IsPurchasable)
				throw DomainException.Unprocessable(CatalogErrors.QuoteServiceNotOnline);
			var date = s.Date ?? r.From;
			var unit = item.Price;
			if (item.HasPeriodPrices && TariffCalendar.Resolve(periods, date) is { } period
				&& item.PeriodPrices.FirstOrDefault(p => p.TariffPeriodGroup == period.PeriodGroupKey) is { } periodPrice)
				unit = periodPrice.Price;
			var vatRate = item.VatRate;
			if (s.TimeSlotId is { } slotId)
			{
				var slot = item.TimeSlots.FirstOrDefault(t => t.PublicId == slotId && t.IsActive) ?? throw DomainException.NotFound(CatalogErrors.QuoteTimeSlotNotFound);
				unit = slot.Price ?? unit;
				// servizi orari: l'IVA della disponibilità della durata, se ne ha una
				if (item.HourlyOptions.FirstOrDefault(o => o.Id == slot.OptionId)?.VatRate is { } optionRate && item.VatNature is null)
					vatRate = optionRate;
			}
			if (!string.IsNullOrWhiteSpace(s.Variant))
			{
				var variant = item.Variants.FirstOrDefault(v => string.Equals(v.Name, s.Variant.Trim(), StringComparison.OrdinalIgnoreCase))
					?? throw DomainException.NotFound(CatalogErrors.QuoteVariantNotFound);
				unit += variant.Price;
			}
			var name = Translations.Pick(item.Translations, t => t.Language, t => t.Name, language, item.Name) ?? item.Name;
			var line = new Line("service", item.PublicId, null, null, string.IsNullOrWhiteSpace(s.Variant) ? name : $"{name} ({s.Variant.Trim()})",
				s.Quantity, vatRate, item.VatNature is { } n ? Codes.Of(n) : null) { Category = Category(item.Kind) };
			line.Set(decimal.Round(unit * s.Quantity, 2, MidpointRounding.AwayFromZero), "list");
			lines.Add(line);
			ordered.Add((item, s.Quantity, date));
		}

		// prezzi automatici (es. «Coperto»): una volta per ordine o per ogni servizio, nei periodi, giorni e fasce di validità
		var spotSectors = spots.Select(s => s.SpotId is { } id && spotRows.TryGetValue(id, out var row) ? row.SectorId : s.SectorId is { } sec ? sectors.GetValueOrDefault(sec)?.Id : null)
			.Where(x => x is not null).Select(x => x!.Value).ToHashSet();
		var time = TimeOnly.FromDateTime(nowLocal);
		foreach (var auto in await _services.ListAutomaticAsync(propertyId, ct))
		{
			if (channel == QuoteChannel.Online && !auto.IsPurchasable)
				continue;
			if (auto.IsSectorRestricted && !auto.Sectors.Any(s => spotSectors.Contains(s.SectorId)))
				continue;
			// nessun periodo = sempre; altrimenti uno dei periodi copre giorno, giorno della settimana e ora
			var matching = ordered.Where(o => (auto.AppliesToAllServices || auto.Services.Any(s => s.SaleItemId == o.Item.Id))
				&& (auto.Periods.Count == 0 || auto.Periods.Any(p => p.Covers(o.Date, DiscountRules.ToWeekday(o.Date.DayOfWeek), time)))).ToList();
			if (matching.Count == 0)
				continue;
			var count = (short)(auto.AppliesPerService ? matching.Sum(m => m.Quantity) : 1);
			var name = Translations.Pick(auto.Translations, t => t.Language, t => t.Name, language, auto.Name) ?? auto.Name;
			var line = new Line("automatic_price", auto.PublicId, null, null, name, count, auto.VatRate, auto.VatNature is { } n ? Codes.Of(n) : null);
			line.Set(decimal.Round(auto.Price * count, 2, MidpointRounding.AwayFromZero), "list");
			lines.Add(line);
		}
		return lines;
	}

	/// <summary>Supplemento della prenotazione online: postazioni (impostazioni online) e servizi (impostazioni dei servizi).</summary>
	private async Task<List<Line>> SurchargesAsync(long propertyId, OnlineBookingSettings? online, List<Line> lines, CancellationToken ct)
	{
		var result = new List<Line>();
		void Add(SurchargeType type, decimal value, IEnumerable<Line> basis, string description)
		{
			var amount = type switch
			{
				SurchargeType.Percentage => basis.Sum(l => l.Amount) * value / 100,
				SurchargeType.Fixed when basis.Any() => value,
				_ => 0,
			};
			if (amount > 0)
			{
				var line = new Line("surcharge", null, null, null, description, 1, null, null);
				line.Set(decimal.Round(amount, 2, MidpointRounding.AwayFromZero), "list");
				result.Add(line);
			}
		}
		if (online is not null)
			Add(online.SurchargeType, online.SurchargeValue, lines.Where(l => l.Kind is "package" or "additional" or "voucher"), "spots");
		if (await _online.FindServiceSettingsAsync(propertyId, ct) is { } services)
			Add(services.SurchargeType, services.SurchargeValue, lines.Where(l => l.Kind is "service" or "automatic_price"), "services");
		return result;
	}

	private async Task<QuoteDiscountDto?> DiscountAsync(long propertyId, QuoteChannel channel, QuoteRequest r, List<Line> lines, DateTime nowLocal, CancellationToken ct)
	{
		Discount? discount;
		if (!string.IsNullOrWhiteSpace(r.DiscountCode))
		{
			discount = await _public.FindDiscountByCodeAsync(propertyId, r.DiscountCode, ct);
			if (discount is null || !discount.RequiresCode || !DiscountRules.IsValid(discount, channel, r.From, r.To, nowLocal))
				throw DomainException.Unprocessable(CatalogErrors.QuoteDiscountNotValid);
		}
		else
			discount = (await _public.ListAutomaticDiscountsAsync(propertyId, ct)).FirstOrDefault(d => DiscountRules.IsValid(d, channel, r.From, r.To, nowLocal));
		if (discount is null)
			return null;
		var sectorIds = (await _sectors.ListAsync(propertyId, ct)).ToDictionary(s => s.PublicId, s => s.Id);
		var amount = DiscountRules.Amount(discount, lines.Where(l => l.Kind != "surcharge")
			.Select(l => new DiscountableLine(l.Amount, l.SectorId is { } s ? sectorIds.GetValueOrDefault(s) : null,
				l.Kind == "additional" ? DiscountServiceCategory.Additional : l.Category)).ToList());
		return new QuoteDiscountDto(discount.PublicId, discount.Code, discount.Name, Codes.Of(discount.Type), discount.Value, amount);
	}

	/// <summary>Regole della prenotazione online: periodi, posti, mezza giornata, stagionali, giorno stesso, orizzonte, limiti di soggiorno.</summary>
	private static void ValidateOnline(
		QuoteRequest r, DayPart part, DateOnly today, OnlineBookingSettings? online, IReadOnlyList<QuoteSpotRequest> spots,
		IReadOnlyDictionary<Guid, MapSpot> spotRows, IReadOnlyDictionary<Guid, MapSector> sectors)
	{
		if (online is not { IsEnabled: true })
			throw DomainException.Unprocessable(CatalogErrors.QuoteOnlineUnavailable);
		if (r.From < today)
			throw DomainException.Unprocessable(CatalogErrors.QuotePastDates);
		if (r.From == today && !online.AllowsSameDayBooking)
			throw DomainException.Unprocessable(CatalogErrors.QuoteSameDayNotAllowed);
		if (online.BookingHorizonDays is { } horizon && r.To > today.AddDays(horizon))
			throw DomainException.Unprocessable(CatalogErrors.QuoteBeyondHorizon, new Dictionary<string, object?> { ["days"] = horizon });
		if (part != DayPart.FullDay && !online.AllowsHalfDay)
			throw DomainException.Unprocessable(CatalogErrors.QuoteHalfDayNotAllowed);
		if (r.Seasonal && !online.AllowsSeasonal)
			throw DomainException.Unprocessable(CatalogErrors.QuoteSeasonalNotAllowed);
		if (online.IsPeriodRestricted && !online.AllowedPeriods.Any(p => p.StartsOn <= r.From && r.To <= p.EndsOn))
			throw DomainException.Unprocessable(CatalogErrors.QuoteDatesNotAllowed);
		var days = r.To.DayNumber - r.From.DayNumber + 1;
		foreach (var spot in spots)
		{
			var row = spot.SpotId is { } id ? spotRows.GetValueOrDefault(id) : null;
			if (online.IsLocationRestricted && (row is null || online.AllowedLocations.All(l => l.SpotId != row.Id)))
				throw DomainException.Unprocessable(CatalogErrors.QuoteSpotNotOnline);
			var sectorId = row?.SectorId ?? (spot.SectorId is { } s ? sectors.GetValueOrDefault(s)?.Id : null);
			foreach (var limit in online.StayLimits.Where(l => l.SectorId == sectorId && l.StartsOn <= r.To && r.From <= l.EndsOn))
				if (limit.MinDays is { } min && days < min || limit.MaxDays is { } max && days > max)
					throw DomainException.Unprocessable(CatalogErrors.QuoteStayLimit, new Dictionary<string, object?> { ["minDays"] = limit.MinDays, ["maxDays"] = limit.MaxDays });
		}
	}

	private static DiscountServiceCategory Category(ServiceSaleKind kind) => kind switch
	{
		ServiceSaleKind.Bar => DiscountServiceCategory.Bar,
		ServiceSaleKind.Hourly => DiscountServiceCategory.Hourly,
		_ => DiscountServiceCategory.Normal,
	};

	/// <summary>Riga del preventivo in costruzione.</summary>
	private sealed class Line(string kind, Guid? refId, Guid? spotId, Guid? sectorId, string description, short quantity, decimal? vatRate, string? vatNature)
	{
		public string Kind { get; } = kind;
		public Guid? SectorId { get; } = sectorId;
		public decimal Amount { get; set; }
		public string Rule { get; set; } = "grid";
		public DiscountServiceCategory? Category { get; init; }
		public List<SegmentPrice> Segments { get; } = [];

		public void Set(decimal amount, string rule)
		{
			Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
			Rule = rule;
		}

		public QuoteLineDto ToDto() => new(
			Kind, refId, spotId, SectorId, description, quantity, decimal.Round(Amount, 2, MidpointRounding.AwayFromZero), vatRate, vatNature, Rule,
			Segments.Select(s => new QuoteSegmentDto(s.Segment.From, s.Segment.To, s.Segment.PeriodGroup, s.Segment.Days.Count, s.Amount, s.Rule)).ToList());
	}
}
