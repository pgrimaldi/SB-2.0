using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Dtos;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Module.Booking.Domain.Pricing;
using Shared.Contracts.Catalog;
using Shared.Contracts.Errors;
using Shared.Utils;

namespace Module.Booking.Application.Services;

/// <summary>Cosa resta da salvare dopo aver portato la prenotazione allo stato richiesto.</summary>
public sealed record WritePlan(
	IReadOnlyList<BookingLine> CreatedLines, IReadOnlyList<BookingLine> LiveLines, IReadOnlyList<SpotHold> HoldsToConvert,
	IReadOnlyList<string> Warnings, IReadOnlyDictionary<long, ResourceType> TypesByLine)
{
	/// <summary>Righe dei servizi create: i loro consumi si scrivono dopo il primo salvataggio.</summary>
	public IReadOnlyList<BookingServiceLine> CreatedServiceLines { get; init; } = [];

	/// <summary>I consumi nuovi nascono tenuti (prenotazione online da confermare o da pagare).</summary>
	public bool Hold { get; init; }

	public DateTime? HoldUntil { get; init; }

	/// <summary>Articoli di magazzino sotto la scorta minima dopo questa prenotazione (avviso nella campanella).</summary>
	public IReadOnlyList<string> LowStockItems { get; init; } = [];

	/// <summary>Articoli di magazzino impegnati dalle righe vive (si riscrivono a ogni salvataggio).</summary>
	public IReadOnlyList<ItemNeed> ItemNeeds { get; init; } = [];

	/// <summary>Codice sconto preso ora dalla prenotazione: il suo utilizzo si conta al salvataggio.</summary>
	public Guid? RedeemDiscountId { get; init; }

	/// <summary>Codice sconto che la prenotazione non ha più: l'utilizzo si restituisce dopo il salvataggio.</summary>
	public Guid? ReleaseDiscountId { get; init; }

	/// <summary>Tipo di consumo di una riga creata (le righe nuove non hanno ancora l'id: si cerca per riferimento).</summary>
	public Dictionary<BookingLine, ResourceType> CreatedTypes { get; } = new(ReferenceEqualityComparer.Instance);
}

/// <summary>Opzioni della scrittura.</summary>
/// <param name="NextSeason">Rinnovo: preventivo con il listino di oggi sui periodi della stagione successiva.</param>
/// <param name="ReleasingLineIds">
/// Righe di altre prenotazioni che si liberano nella stessa transazione (scambio, dividi conto): i loro giorni non
/// contano come occupati.
/// </param>
/// <param name="FixedTotal">
/// Il totale lo fissa l'operazione stessa (dividi conto), non l'operatore: niente controllo del permesso sui prezzi.
/// </param>
/// <param name="Online">Prenotazione dal sito: preventivo con le regole online, le riserve contano come occupate.</param>
/// <param name="Hold">I consumi nascono «tenuti» (da confermare o da pagare), fino a <paramref name="HoldUntil"/>.</param>
public sealed record WriteOptions(
	bool NextSeason = false, IReadOnlyCollection<long>? ReleasingLineIds = null, bool FixedTotal = false, bool Online = false, bool Hold = false,
	DateTime? HoldUntil = null)
{
	public static readonly WriteOptions Default = new();
}

/// <summary>
/// Porta una prenotazione allo stato richiesto: righe (tenute se postazione, date e fascia non cambiano, altrimenti
/// rifatte), aggiuntivi e convenzioni, importi dal preventivo di Catalog, controllo di disponibilità e capienza delle
/// aree, riserve da togliere. Il salvataggio (righe, poi consumi) lo fa il servizio, in una transazione.
/// </summary>
public sealed class ReservationWriter
{
	private readonly IReservationRepository _reservations;
	private readonly ISpotHoldRepository _holds;
	private readonly SpotDirectory _spots;
	private readonly IQuoteCalculator _quotes;
	private readonly BookingScope _scope;
	private readonly SharingLedger _sharing;
	private readonly ServiceWriter _services;
	private readonly IDiscountRedemption _redemptions;
	private readonly InventoryPlanner _inventory;
	private readonly BookingAudit _audit;

	public ReservationWriter(
		IReservationRepository reservations, ISpotHoldRepository holds, SpotDirectory spots, IQuoteCalculator quotes, BookingScope scope, SharingLedger sharing,
		ServiceWriter services, IDiscountRedemption redemptions, InventoryPlanner inventory, BookingAudit audit)
	{
		_audit = audit;
		_inventory = inventory;
		_services = services;
		_redemptions = redemptions;
		_reservations = reservations;
		_holds = holds;
		_spots = spots;
		_quotes = quotes;
		_scope = scope;
		_sharing = sharing;
	}

	private sealed record Segment(BookableSpot Spot, DateOnly From, DateOnly To, ReservationSpotInput Input)
	{
		public short Seats => Spot.IsArea ? Input.Seats ?? 1 : (short)1;
	}

	public Task<WritePlan> ApplyAsync(Reservation reservation, SaveReservationRequest request, CancellationToken ct = default)
		=> ApplyAsync(reservation, request, WriteOptions.Default, ct);

	public async Task<WritePlan> ApplyAsync(Reservation reservation, SaveReservationRequest request, WriteOptions options, CancellationToken ct = default)
	{
		var period = SpotKinds.ParsePeriod(request.DayPart);
		var segments = new List<Segment>();
		foreach (var input in request.Spots)
		{
			var spot = await _spots.RequireBookableAsync(input.SpotId, ct);
			segments.Add(new Segment(spot, input.From ?? request.From, input.To ?? request.To, input));
		}
		EnsureNoSelfOverlap(segments);
		// servizi: assenti = invariati (spostamenti, scambi, ricalcoli), lista vuota = tolti
		var services = request.Services is null ? null : await _services.ResolveAsync(reservation.PropertyId, request, ct);

		var existing = reservation.Lines.Where(l => l.DeletedAt is null).ToList();
		if (!options.FixedTotal)
			EnsurePricingAllowed(reservation, request, existing);

		// Righe: si tiene la riga con stessa postazione, date, fascia e posti; le altre si rifanno.
		var kept = new List<(BookingLine Line, Segment Segment)>();
		var toCreate = new List<Segment>();
		var unmatched = existing.ToList();
		foreach (var seg in segments)
		{
			var match = unmatched.FirstOrDefault(l => l.SpotId == seg.Spot.Id && l.StartDate == seg.From && l.EndDate == seg.To
				&& l.BookingPeriod == period && l.Quantity == seg.Seats);
			if (match is not null)
			{
				unmatched.Remove(match);
				kept.Add((match, seg));
			}
			else
				toCreate.Add(seg);
		}

		// Prezzo del pacchetto scritto a mano: cambiando le date si perde (vale di nuovo il listino).
		decimal? ManualPriceFor(Segment seg, BookingLine? keptLine)
		{
			if (seg.Input.ManualPrice is not { } price)
				return null;
			if (keptLine is not null)
				return price;
			var previous = existing.FirstOrDefault(l => l.SpotId == seg.Spot.Id && l.ManualPrice == price);
			return previous is not null && (previous.StartDate != seg.From || previous.EndDate != seg.To) ? null : price;
		}

		var manualPrices = new Dictionary<Segment, decimal?>(ReferenceEqualityComparer.Instance);
		foreach (var (line, seg) in kept)
			manualPrices[seg] = ManualPriceFor(seg, line);
		foreach (var seg in toCreate)
			manualPrices[seg] = ManualPriceFor(seg, null);

		// Preventivo di Catalog, per gruppi di postazioni con le stesse date.
		var excluded = await _sharing.ExcludedDaysAsync(reservation, ct);
		var previousDiscount = reservation.Discounts.Where(d => d.DeletedAt is null).Select(d => d.DiscountId).FirstOrDefault(d => d is not null);
		var quoted = await QuoteAsync(segments, manualPrices, request, period, options, excluded, services, reservation.PropertyId, previousDiscount, ct);

		// Righe tolte: si cancellano con i loro consumi.
		var removedIds = unmatched.Select(l => l.Id).ToList();
		var freedIds = removedIds.Concat(options.ReleasingLineIds ?? []).ToHashSet();
		foreach (var consumption in await _reservations.ListLineConsumptionsAsync(removedIds, ct))
			consumption.Release();
		await _sharing.LinesRemovedAsync(reservation, unmatched, ct);
		foreach (var line in unmatched)
		{
			_reservations.RemoveLine(line);
			reservation.Lines.Remove(line);
		}
		_reservations.RemoveChildren(reservation);
		reservation.AdditionalItems.Clear();
		reservation.AgreementLines.Clear();
		reservation.Discounts.Clear();

		var ordinal = reservation.Lines.Count == 0 ? 0 : reservation.Lines.Count;
		var created = new List<BookingLine>();
		var live = new List<(BookingLine Line, Segment Segment)>(kept);
		var plan = new List<BookingLine>();
		foreach (var seg in toCreate)
		{
			ordinal = NextOrdinal(reservation, ordinal);
			var line = BookingLine.Create(reservation, ordinal, SpotDirectory.Snapshot(seg.Spot), seg.From, seg.To, period,
				seg.Input.PriceListId, quoted.PackageNames.GetValueOrDefault(seg), manualPrices[seg], seg.Seats);
			reservation.Lines.Add(line);
			created.Add(line);
			live.Add((line, seg));
		}

		foreach (var (line, seg) in live)
		{
			line.ChangePackage(seg.Input.PriceListId, quoted.PackageNames.GetValueOrDefault(seg) ?? line.PackageNameSnapshot, manualPrices[seg]);
			line.SetPrice(quoted.Amounts.GetValueOrDefault(seg));
			foreach (var add in seg.Input.Additionals ?? [])
				reservation.AdditionalItems.Add(BookingLineAdditionalItem.FromPriceList(reservation, line, add.PriceListId,
					quoted.AdditionalNames.GetValueOrDefault((seg, add.PriceListId)) ?? "", add.Quantity, add.Dates,
					quoted.AdditionalAmounts.GetValueOrDefault((seg, add.PriceListId))));
			if (seg.Input.AgreementPartnerId is { } partner)
			{
				if (seg.Input.AgreementPriceListId is { } list)
					reservation.AgreementLines.Add(BookingAgreementLine.ForLine(reservation, line, partner, list, AgreementLineType.Regular, quoted.Amounts.GetValueOrDefault(seg)));
				if (seg.Input.VoucherPriceListId is { } voucher)
					reservation.AgreementLines.Add(BookingAgreementLine.ForLine(reservation, line, partner, voucher, AgreementLineType.Voucher, quoted.VoucherAmounts.GetValueOrDefault(seg)));
			}
			plan.Add(line);
		}

		var firstAgreement = segments.FirstOrDefault(s => s.Input.AgreementPartnerId is not null);
		reservation.SetAgreementSummary(firstAgreement?.Input.AgreementPartnerId,
			firstAgreement?.Input.AgreementPriceListId is not null ? ListingType.AgreementPriceList : ListingType.AgreementVoucher);
		reservation.SetSeasonal(request.Seasonal);

		// servizi e tavoli: righe rifatte con gli importi del preventivo; quelli invariati restano con il loro importo
		IReadOnlyList<BookingServiceLine> serviceLines = [];
		var subtotal = quoted.Subtotal;
		var violations = new List<CapacityViolation>();
		if (services is not null || request.Tables is not null)
		{
			var (createdServices, serviceViolations, manualDelta) = await _services.ApplyAsync(reservation, request, services, quoted.ServiceLines, plan, ct);
			serviceLines = createdServices;
			subtotal += manualDelta;
			violations.AddRange(serviceViolations);
		}
		// magazzino: gli articoli dei pacchetti e degli aggiuntivi delle righe vive si riscrivono a ogni salvataggio
		var (itemNeeds, itemViolations, lowStock) = await _inventory.PlanAsync(reservation, plan, ct);
		violations.AddRange(itemViolations);
		if (violations.Count > 0 && !request.ConfirmCapacity)
			throw DomainException.Conflict(BookingErrors.ServiceCapacityExceeded, new Dictionary<string, object?> { ["violations"] = violations });
		foreach (var c in await _reservations.ListLineConsumptionsAsync(kept.Select(k => k.Line.Id).ToList(), ct))
			if (c.ResourceType == ResourceType.InventoryItem)
				_reservations.RemoveConsumption(c);
		if (lowStock && !quoted.Warnings.Contains(BookingWarnings.InventoryLow))
			quoted.Warnings.Add(BookingWarnings.InventoryLow);
		var lowItems = lowStock ? itemNeeds.Select(n => n.Name).Distinct().ToList() : [];
		if (services is null)
			subtotal += reservation.ServiceLines.Where(s => s.DeletedAt is null).Sum(s => s.LineTotal);
		reservation.SetStay(plan, reservation.ServiceLines.Where(s => s.DeletedAt is null && s.ServiceDate is not null).Select(s => s.ServiceDate!.Value)
			.Concat(reservation.TableLines.Where(t => t.DeletedAt is null).Select(t => t.ServiceDate)).ToList());

		if (quoted.Discount is { } d)
			reservation.Discounts.Add(BookingDiscount.Applied(reservation, d.Id, d.Code, !string.IsNullOrWhiteSpace(request.DiscountCode),
				d.Type == "percentage" ? DiscountType.Percentage : DiscountType.Fixed, d.Value, d.Amount));

		reservation.ApplyPricing(ReservationPricing.Compute(
			subtotal, quoted.Discount?.Amount ?? 0m, Adjustment(request.ManualDiscount), Adjustment(request.Surcharge), request.PriceOverride,
			manualPrices.Values.Any(v => v is not null) || (request.Services ?? []).Any(s => s.ManualPrice is not null), recalculated: existing.Count > 0));

		// Disponibilità delle righe nuove: postazioni esclusive libere, aree con posti; le riserve si tolgono.
		var holdsToConvert = await CheckAvailabilityAsync(reservation, created, live.ToDictionary(x => x.Line, x => x.Segment, (IEqualityComparer<BookingLine>)ReferenceEqualityComparer.Instance), period, freedIds, options.Online, ct);
		await _sharing.ClaimAsync(reservation, created, ct);

		var result = new WritePlan(created, plan, holdsToConvert, quoted.Warnings,
			kept.ToDictionary(k => k.Line.Id, k => SpotKinds.ResourceTypeOf(k.Segment.Spot.SpotType)))
		{
			CreatedServiceLines = serviceLines, Hold = options.Hold, HoldUntil = options.HoldUntil, ItemNeeds = itemNeeds, LowStockItems = lowItems,
			RedeemDiscountId = quoted.Discount?.Id is { } newId && newId != previousDiscount ? newId : null,
			ReleaseDiscountId = previousDiscount is { } oldId && oldId != quoted.Discount?.Id ? oldId : null,
		};
		foreach (var (line, seg) in live.Where(x => created.Contains(x.Line)))
			result.CreatedTypes[line] = SpotKinds.ResourceTypeOf(seg.Spot.SpotType);
		return result;
	}

	/// <summary>
	/// Salva una o più prenotazioni scritte con <see cref="ApplyAsync(Reservation, SaveReservationRequest, WriteOptions, CancellationToken)"/>:
	/// righe e testate (con i consumi liberati), poi i consumi nuovi, che vogliono l'id delle righe. Tutto in una transazione.
	/// </summary>
	public async Task SaveAsync(IReadOnlyList<(Reservation Reservation, WritePlan Plan)> writes, CancellationToken ct = default)
	{
		// utilizzi dei codici sconto: si contano prima (il massimo vale anche per prenotazioni contemporanee), si restituiscono se il salvataggio fallisce
		var redeemed = new List<(Guid Property, Guid Discount)>();
		foreach (var (reservation, plan) in writes)
			if (plan.RedeemDiscountId is { } discount)
			{
				if (!await _redemptions.TryRedeemAsync(reservation.PropertyId, discount, ct))
				{
					foreach (var (property, id) in redeemed)
						await _redemptions.ReleaseAsync(property, id, ct);
					throw DomainException.Conflict(BookingErrors.DiscountExhausted);
				}
				redeemed.Add((reservation.PropertyId, discount));
			}
		try
		{
			await using var tx = await _reservations.BeginTransactionAsync(ct);
			await _reservations.SaveChangesAsync(ct);
			foreach (var (reservation, plan) in writes)
				_reservations.AddConsumptions(ConsumptionsFor(reservation, plan));
			await _reservations.SaveChangesAsync(ct);
			await tx.CommitAsync(ct);
		}
		catch
		{
			foreach (var (property, id) in redeemed)
				await _redemptions.ReleaseAsync(property, id, CancellationToken.None);
			throw;
		}
		foreach (var (reservation, plan) in writes)
		{
			if (plan.ReleaseDiscountId is { } released)
				await _redemptions.ReleaseAsync(reservation.PropertyId, released, ct);
			if (plan.LowStockItems.Count > 0)
				await _audit.ActivityAsync(Shared.Contracts.Booking.BookingActivityKinds.InventoryLow, reservation,
					new Dictionary<string, object?> { ["items"] = plan.LowStockItems }, ct);
		}
		await _sharing.PublishPendingAsync(ct);
	}

	/// <summary>Consumi (un giorno per riga) delle righe create; dopo il primo salvataggio le righe hanno l'id.</summary>
	public static IEnumerable<ResourceConsumption> ConsumptionsFor(Reservation reservation, WritePlan plan)
	{
		var all = plan.CreatedLines.SelectMany(line => line.EachDay().Select(day => ResourceConsumption.ForLine(line, reservation, plan.CreatedTypes[line], day)))
			.Concat(ServiceWriter.ConsumptionsFor(reservation, plan.CreatedServiceLines))
			.Concat(plan.ItemNeeds.Select(n => ResourceConsumption.ForItem(n.Line, reservation, n.ItemId, n.Name, n.Day, n.Quantity))).ToList();
		if (plan.Hold)
			foreach (var c in all)
				c.Hold(plan.HoldUntil);
		return all;
	}

	private static int NextOrdinal(Reservation reservation, int current)
	{
		var used = reservation.Lines.Select(l => l.BookingLineNumber).ToHashSet(StringComparer.Ordinal);
		var next = current + 1;
		while (used.Contains($"{reservation.BookingNumber}-{next}"))
			next++;
		return next;
	}

	private static PriceAdjustment? Adjustment(AdjustmentInput? input)
		=> input is null ? null : new PriceAdjustment(input.Type == "percentage" ? AdjustmentType.Percentage : AdjustmentType.Amount, input.Value, input.Label?.Trim());

	private static void EnsureNoSelfOverlap(List<Segment> segments)
	{
		foreach (var group in segments.GroupBy(s => s.Spot.Id))
		{
			var ordered = group.OrderBy(s => s.From).ToList();
			for (var i = 1; i < ordered.Count; i++)
				if (ordered[i].From <= ordered[i - 1].To)
					throw DomainException.Unprocessable(ValidationErrorCodes.Invalid, new Dictionary<string, object?> { ["field"] = "spots", ["spotId"] = group.Key });
		}
	}

	/// <summary>Prezzi scritti a mano, sconto manuale e incremento: solo chi ha il permesso, salvo lasciarli come sono.</summary>
	private void EnsurePricingAllowed(Reservation reservation, SaveReservationRequest request, IReadOnlyList<BookingLine> existing)
	{
		if (_scope.CanSetPrices)
			return;
		var changed = request.PriceOverride != reservation.PriceOverride
			|| (request.ManualDiscount?.Value ?? 0) != (reservation.ManualDiscountValue ?? 0)
			|| (request.Surcharge?.Value ?? 0) != (reservation.SurchargeValue ?? 0)
			|| request.Spots.Any(s => s.ManualPrice is { } p && !existing.Any(l => l.SpotId == s.SpotId && l.ManualPrice == p))
			|| (request.Services ?? []).Any(s => s.ManualPrice is { } p
				&& !reservation.ServiceLines.Any(l => l.DeletedAt is null && l.SaleItemId == s.ServiceId && l.LineTotal == p));
		if (changed)
			throw DomainException.Forbidden(BookingErrors.PricingForbidden);
	}

	private sealed class Quoted
	{
		public Dictionary<Segment, decimal> Amounts { get; } = new(ReferenceEqualityComparer.Instance);
		public Dictionary<Segment, decimal> VoucherAmounts { get; } = new(ReferenceEqualityComparer.Instance);
		public Dictionary<Segment, string> PackageNames { get; } = new(ReferenceEqualityComparer.Instance);
		public Dictionary<Segment, decimal> PackageAmounts { get; } = new(ReferenceEqualityComparer.Instance);
		public Dictionary<(Segment, Guid), decimal> AdditionalAmounts { get; } = new();
		public Dictionary<(Segment, Guid), string> AdditionalNames { get; } = new();
		public decimal Subtotal { get; set; }
		public PriceQuoteDiscount? Discount { get; set; }
		/// <summary>Righe dei servizi e dei prezzi automatici, nell'ordine delle richieste.</summary>
		public List<PriceQuoteLine> ServiceLines { get; } = [];
		public List<string> Warnings { get; } = [];
	}

	/// <summary>
	/// Un preventivo per ogni gruppo di postazioni con le stesse date; lo sconto vale una volta, sul primo gruppo.
	/// Prenotazione continua che passa da una postazione all'altra: il pacchetto si prezza come un periodo unico,
	/// proporzionato fra i tratti secondo il prezzo della fila di ciascuno (vedi <see cref="ContinuousPriceAsync"/>).
	/// </summary>
	private async Task<Quoted> QuoteAsync(
		List<Segment> segments, Dictionary<Segment, decimal?> manualPrices, SaveReservationRequest request, DayPeriod period, WriteOptions options,
		IReadOnlySet<(Guid SpotId, DateOnly Day)> excluded, IReadOnlyList<ResolvedService>? services, Guid propertyId, Guid? redeemedDiscountId,
		CancellationToken ct)
	{
		var nextSeason = options.NextSeason;
		var result = new Quoted();
		var first = true;
		var serviceRequests = services is { Count: > 0 } ? ServiceWriter.QuoteRequests(services) : null;
		var groups = segments.GroupBy(s => (s.From, s.To)).OrderBy(g => g.Key.From).Select(g => (g.Key.From, g.Key.To, List: g.ToList())).ToList();
		// soli servizi: un preventivo senza postazioni
		if (groups.Count == 0 && serviceRequests is not null)
			groups.Add((request.From, request.To, []));
		foreach (var (groupFrom, groupTo, list) in groups)
		{
			var quoteRequest = new PriceQuoteRequest(
				groupFrom, groupTo, SpotKinds.CodeOf(period), request.Seasonal,
				list.Select(s => new PriceQuoteSpot(s.Spot.Id, s.Input.PriceListId, s.Input.AgreementPriceListId, s.Input.VoucherPriceListId,
					excluded.Where(e => e.SpotId == s.Spot.Id && e.Day >= s.From && e.Day <= s.To).Select(e => e.Day).Order().ToList() is { Count: > 0 } days ? days : null,
					manualPrices[s], s.Input.Additionals?.Select(a => new PriceQuoteAdditional(a.PriceListId, a.Quantity, a.Dates)).ToList())).ToList(),
				first ? request.DiscountCode : null, nextSeason, first ? serviceRequests : null, redeemedDiscountId);
			var quote = options.Online ? await _quotes.QuoteOnlineAsync(propertyId, quoteRequest, ct) : await _quotes.QuoteAsync(quoteRequest, ct);
			if (first)
				result.ServiceLines.AddRange(quote.Lines.Where(l => l.Kind is QuoteLineKinds.Service or QuoteLineKinds.AutomaticPrice));

			foreach (var seg in list)
			{
				var lines = quote.Lines.Where(l => l.SpotId == seg.Spot.Id && l.Kind != QuoteLineKinds.Surcharge).ToList();
				result.Amounts[seg] = lines.Sum(l => l.Amount);
				result.VoucherAmounts[seg] = -lines.Where(l => l.Kind == QuoteLineKinds.Voucher).Sum(l => l.Amount);
				if (lines.FirstOrDefault(l => l.Kind == QuoteLineKinds.Package) is { } package)
				{
					result.PackageNames[seg] = package.Description;
					result.PackageAmounts[seg] = package.Amount;
				}
				foreach (var add in lines.Where(l => l.Kind == QuoteLineKinds.Additional && l.RefId is not null))
				{
					result.AdditionalAmounts[(seg, add.RefId!.Value)] = result.AdditionalAmounts.GetValueOrDefault((seg, add.RefId.Value)) + add.Amount;
					result.AdditionalNames[(seg, add.RefId.Value)] = add.Description;
				}
			}
			result.Subtotal += quote.Subtotal;
			if (first)
				result.Discount = quote.AppliedDiscount;
			result.Warnings.AddRange(quote.Warnings.Where(w => !result.Warnings.Contains(w)));
			first = false;
		}
		await ContinuousPriceAsync(result, segments, manualPrices, request, period, options, propertyId, ct);
		await CumulativeBlocksAsync(result, segments, manualPrices, request, period, options, propertyId, ct);
		return result;
	}

	/// <summary>
	/// Prezzo unico di una prenotazione continua su più postazioni (anche dalla ricerca smart): i tratti si susseguono
	/// senza buchi né sovrapposizioni con lo stesso pacchetto. Il pacchetto di ogni tratto vale il prezzo del periodo intero
	/// sulla sua fila per la quota dei suoi giorni (il resto dell'arrotondamento all'ultimo tratto); aggiuntivi e prezzi
	/// scritti a mano restano quelli del tratto.
	/// </summary>
	private async Task ContinuousPriceAsync(
		Quoted result, List<Segment> segments, Dictionary<Segment, decimal?> manualPrices, SaveReservationRequest request, DayPeriod period,
		WriteOptions options, Guid propertyId, CancellationToken ct)
	{
		var chain = segments.OrderBy(s => s.From).ToList();
		if (chain.Count < 2 || chain.Any(s => s.Spot.IsArea || s.Input.PriceListId is null || s.Input.PriceListId != chain[0].Input.PriceListId
				|| s.Input.AgreementPriceListId is not null || manualPrices[s] is not null))
			return;
		for (var i = 1; i < chain.Count; i++)
			if (chain[i].From != chain[i - 1].To.AddDays(1))
				return;

		var from = chain[0].From;
		var to = chain[^1].To;
		var days = to.DayNumber - from.DayNumber + 1;
		var scaled = new List<decimal>();
		foreach (var seg in chain)
		{
			var request2 = new PriceQuoteRequest(from, to, SpotKinds.CodeOf(period), request.Seasonal,
				[new PriceQuoteSpot(seg.Spot.Id, seg.Input.PriceListId, null, null, null, null, null)], null, options.NextSeason);
			var quote = options.Online ? await _quotes.QuoteOnlineAsync(propertyId, request2, ct) : await _quotes.QuoteAsync(request2, ct);
			var full = quote.Lines.Where(l => l.SpotId == seg.Spot.Id && l.Kind == QuoteLineKinds.Package).Sum(l => l.Amount);
			scaled.Add(full * (seg.To.DayNumber - seg.From.DayNumber + 1) / days);
		}
		// arrotondamento: il centesimo che avanza all'ultimo tratto
		var rounded = scaled.Select(v => decimal.Round(v, 2, MidpointRounding.AwayFromZero)).ToList();
		rounded[^1] += decimal.Round(scaled.Sum(), 2, MidpointRounding.AwayFromZero) - rounded.Sum();
		for (var i = 0; i < chain.Count; i++)
		{
			var delta = rounded[i] - result.PackageAmounts.GetValueOrDefault(chain[i]);
			result.Amounts[chain[i]] = result.Amounts.GetValueOrDefault(chain[i]) + delta;
			result.Subtotal += delta;
		}
		if (!result.Warnings.Contains(BookingWarnings.ContinuousPrice))
			result.Warnings.Add(BookingWarnings.ContinuousPrice);
	}

	/// <summary>
	/// Prenotazione ripetuta con un pacchetto cumulativo: i blocchi separati della stessa postazione con lo stesso
	/// pacchetto si prezzano sui giorni totali (es. ven-sab-dom per 4 settimane = prezzo di 12 giorni), ripartito per i giorni
	/// di ciascun blocco. Solo a giornata intera, senza stagionale né prezzi scritti a mano; aggiuntivi invariati.
	/// </summary>
	private async Task CumulativeBlocksAsync(
		Quoted result, List<Segment> segments, Dictionary<Segment, decimal?> manualPrices, SaveReservationRequest request, DayPeriod period,
		WriteOptions options, Guid propertyId, CancellationToken ct)
	{
		if (request.Seasonal || period != DayPeriod.FullDay || options.Online)
			return;
		var groups = segments
			.Where(s => !s.Spot.IsArea && s.Input.PriceListId is not null && s.Input.AgreementPriceListId is null && manualPrices[s] is null)
			.GroupBy(s => (s.Spot.Id, s.Input.PriceListId!.Value))
			.Select(g => g.OrderBy(s => s.From).ToList())
			.Where(g => g.Count >= 2)
			.ToList();
		foreach (var blocks in groups)
		{
			// blocchi davvero separati: con tratti attaccati vale il prezzo del periodo intero
			var separate = true;
			for (var i = 1; i < blocks.Count; i++)
				if (blocks[i].From <= blocks[i - 1].To.AddDays(1))
					separate = false;
			if (!separate)
				continue;
			var shares = await _quotes.QuoteCumulativeBlocksAsync(propertyId, blocks[0].Spot.Id, blocks[0].Input.PriceListId!.Value,
				blocks.Select(b => new PriceQuoteBlock(b.From, b.To)).ToList(), options.NextSeason, ct);
			if (shares is null)
				continue;
			for (var i = 0; i < blocks.Count; i++)
			{
				var delta = shares[i] - result.PackageAmounts.GetValueOrDefault(blocks[i]);
				result.Amounts[blocks[i]] = result.Amounts.GetValueOrDefault(blocks[i]) + delta;
				result.Subtotal += delta;
			}
			if (!result.Warnings.Contains(BookingWarnings.CumulativeBlocks))
				result.Warnings.Add(BookingWarnings.CumulativeBlocks);
		}
	}

	private async Task<IReadOnlyList<SpotHold>> CheckAvailabilityAsync(
		Reservation reservation, List<BookingLine> created, Dictionary<BookingLine, Segment> segments, DayPeriod period,
		IReadOnlyCollection<long> releasedLineIds, bool online, CancellationToken ct)
	{
		if (created.Count == 0)
			return [];
		var spotIds = created.Select(l => l.SpotId!.Value).Distinct().ToList();
		var from = created.Min(l => l.StartDate);
		var to = created.Max(l => l.EndDate);
		var ownLines = reservation.Lines.Where(l => l.Id != 0).Select(l => l.Id).ToHashSet();
		var occupied = (await _reservations.ListOccupiedAsync(reservation.PropertyId, spotIds, from, to, ct))
			.Where(o => !(o.Source == ConsumptionSource.ReservationLine && o.SourceId is { } id && (releasedLineIds.Contains(id) || ownLines.Contains(id))))
			.ToList();

		var holds = new Dictionary<long, SpotHold>();
		var activeHolds = (await _holds.ListActiveAsync(reservation.PropertyId, from, to, spotIds, ct)).ToDictionary(h => h.Id);
		var conflicts = new List<object>();
		foreach (var line in created)
		{
			var seg = segments[line];
			var type = SpotKinds.ResourceTypeOf(seg.Spot.SpotType);
			var sameSpot = occupied.Where(o => o.SpotId == line.SpotId && line.Covers(o.Day) && SpotKinds.Overlaps(o.Period, period)).ToList();
			if (seg.Spot.IsArea)
			{
				var capacity = seg.Spot.AreaSeatCount ?? 0;
				var full = sameSpot.GroupBy(o => o.Day).Where(g => g.Sum(o => o.Quantity) + line.Quantity > capacity).Select(g => g.Key).ToList();
				if (line.Quantity > capacity || full.Count > 0)
					throw DomainException.Conflict(BookingErrors.AreaFull, new Dictionary<string, object?>
					{
						["spotId"] = seg.Spot.Id, ["label"] = seg.Spot.Label, ["capacity"] = capacity, ["days"] = full,
					});
				continue;
			}
			if (!SpotKinds.IsExclusive(type))
				continue;
			foreach (var o in sameSpot)
			{
				// dal gestionale si prenota sopra una riserva (che si toglie); online la riserva è occupata
				if (!online && o.Source == ConsumptionSource.ManualHold && o.SourceId is { } holdId && activeHolds.TryGetValue(holdId, out var hold))
					holds[holdId] = hold;
				else
					conflicts.Add(new { spotId = seg.Spot.Id, label = seg.Spot.Label, day = o.Day });
			}
		}
		if (conflicts.Count > 0)
			throw DomainException.Conflict(BookingErrors.SpotUnavailable, new Dictionary<string, object?> { ["conflicts"] = conflicts.Take(50).ToList() });

		foreach (var consumption in await _holds.ListConsumptionsAsync(holds.Keys.ToList(), ct))
			consumption.Release();
		foreach (var hold in holds.Values)
			hold.ConvertTo(reservation, _scope.UserId, _scope.UtcNow);
		return holds.Values.ToList();
	}

	/// <summary>Codice snake_case di un enum per le risposte.</summary>
	public static string Code<TEnum>(TEnum value) where TEnum : struct, Enum => Codes.Of(value);
}
