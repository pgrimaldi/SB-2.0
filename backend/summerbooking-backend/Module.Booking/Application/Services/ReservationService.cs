using BuildingBlocks.Application.Validation;
using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Dtos;
using Module.Booking.Application.Validators;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Catalog;
using Shared.Contracts.Errors;
using Shared.Contracts.Identity;
using Shared.Contracts.Payment;
using Shared.Dtos;
using Shared.Utils;

namespace Module.Booking.Application.Services;

/// <summary>
/// Prenotazioni di postazioni dal gestionale: creazione (confermata subito), modifica, spostamento, dettaglio ed elenco,
/// incassi, check-in e check-out, ingressi. Le prenotazioni online, i servizi, lo sharing, i gruppi e il rinnovo hanno
/// servizi propri.
/// </summary>
public sealed class ReservationService
{
	private readonly IReservationRepository _reservations;
	private readonly ReservationWriter _writer;
	private readonly SpotDirectory _spots;
	private readonly ReservationNumbers _numbers;
	private readonly IBookingGroupRepository _groups;
	private readonly ICustomerDirectory _customers;
	private readonly IReservationPayments _payments;
	private readonly BookingScope _scope;
	private readonly BookingAudit _audit;
	private readonly CreateReservationValidator _createValidator;
	private readonly SaveReservationValidator _saveValidator;
	private readonly MoveReservationValidator _moveValidator;
	private readonly RecordPaymentValidator _paymentValidator;
	private readonly MarkPaidValidator _markPaidValidator;
	private readonly ReservationQueryValidator _queryValidator;
	private readonly SwapReservationsValidator _swapValidator;
	private readonly SharingLedger _sharing;
	private readonly IResaleRepository _resale;
	private readonly ServiceWriter _serviceWriter;

	public ReservationService(
		IReservationRepository reservations, ReservationWriter writer, SpotDirectory spots, ReservationNumbers numbers, IBookingGroupRepository groups,
		ICustomerDirectory customers,
		IReservationPayments payments, BookingScope scope, BookingAudit audit, CreateReservationValidator createValidator,
		SaveReservationValidator saveValidator, MoveReservationValidator moveValidator, RecordPaymentValidator paymentValidator,
		MarkPaidValidator markPaidValidator, ReservationQueryValidator queryValidator, SwapReservationsValidator swapValidator, SharingLedger sharing,
		IResaleRepository resale, ServiceWriter serviceWriter)
	{
		_reservations = reservations;
		_writer = writer;
		_spots = spots;
		_numbers = numbers;
		_groups = groups;
		_customers = customers;
		_payments = payments;
		_scope = scope;
		_audit = audit;
		_createValidator = createValidator;
		_saveValidator = saveValidator;
		_moveValidator = moveValidator;
		_paymentValidator = paymentValidator;
		_markPaidValidator = markPaidValidator;
		_queryValidator = queryValidator;
		_swapValidator = swapValidator;
		_sharing = sharing;
		_resale = resale;
		_serviceWriter = serviceWriter;
	}

	public async Task<ReservationDto> CreateAsync(CreateReservationRequest request, CancellationToken ct = default)
	{
		_createValidator.EnsureValid(request);
		var save = request.Reservation;
		var propertyId = _scope.PropertyId;
		var customerId = await ResolveCustomerAsync(save.Customer, null, save.ConfirmBlacklisted, requireCustomer: true, ct);

		var reservation = Reservation.CreateByManager(
			propertyId, await _numbers.NextNumberAsync(propertyId, ct), await _numbers.NewCodeAsync(ct), customerId,
			_scope.RequiredUserId, save.Seasonal, "EUR");
		reservation.SetNote(save.Note, save.CustomerTags, _scope.UserId, _scope.UtcNow);
		foreach (var guest in save.Guests ?? [])
			if (!string.IsNullOrWhiteSpace(guest))
				reservation.Guests.Add(BookingGuest.Create(reservation, guest, null));

		var plan = await _writer.ApplyAsync(reservation, save, ct);
		_reservations.Add(reservation);
		await SaveWithConsumptionsAsync(reservation, plan, ct);

		await _audit.RecordAsync(AuditActionCodes.Booking.ReservationCreated, reservation, new Dictionary<string, object?>
		{
			["from"] = reservation.StayFrom, ["to"] = reservation.StayTo, ["spots"] = plan.LiveLines.Select(l => l.SpotLabelSnapshot).ToList(),
			["total"] = reservation.Total, ["holdsRemoved"] = plan.HoldsToConvert.Count,
		}, ct: ct);
		await AuditPricingAsync(reservation, null, ct);
		await _audit.ActivityAsync(Shared.Contracts.Booking.BookingActivityKinds.ReservationCreated, reservation, ct: ct);

		if (request.PaidWith is { } mode && reservation.BalanceDue > 0)
			await MarkPaidCoreAsync(reservation, mode, ct);
		return await ToDtoAsync(reservation, plan.Warnings, ct);
	}

	public async Task<ReservationDto> UpdateAsync(Guid id, SaveReservationRequest request, CancellationToken ct = default)
		=> await UpdateAsync(id, request, null, ct);

	/// <param name="expectedVersion">Versione letta dal client (header If-Match): se nel frattempo la prenotazione è cambiata, 412.</param>
	public async Task<ReservationDto> UpdateAsync(Guid id, SaveReservationRequest request, uint? expectedVersion, CancellationToken ct = default)
	{
		_saveValidator.EnsureValid(request);
		var reservation = await LoadActiveAsync(id, ct);
		if (expectedVersion is { } expected && expected != reservation.Version)
			throw new DomainException(BookingErrors.VersionConflict, 412, new Dictionary<string, object?> { ["version"] = reservation.Version });
		var before = PricingSnapshot(reservation);
		var customerId = await ResolveCustomerAsync(request.Customer, reservation.CustomerId, request.ConfirmBlacklisted, requireCustomer: false, ct);
		reservation.ChangeCustomer(customerId);
		reservation.SetNote(request.Note, request.CustomerTags, _scope.UserId, _scope.UtcNow);
		reservation.Touch(_scope.UserId);
		SyncGuests(reservation, request.Guests);

		var plan = await _writer.ApplyAsync(reservation, request, ct);
		await SaveWithConsumptionsAsync(reservation, plan, ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.ReservationUpdated, reservation, new Dictionary<string, object?>
		{
			["from"] = reservation.StayFrom, ["to"] = reservation.StayTo, ["spots"] = plan.LiveLines.Select(l => l.SpotLabelSnapshot).ToList(),
			["total"] = reservation.Total, ["previousTotal"] = before.Total,
		}, ct: ct);
		await AuditPricingAsync(reservation, before, ct);
		return await ToDtoAsync(reservation, plan.Warnings, ct);
	}

	/// <summary>
	/// Sposta una postazione su un'altra, anche solo per alcuni giorni: la riga si spezza e la differenza di prezzo si
	/// applica da sola. Il resto della prenotazione resta com'è.
	/// </summary>
	public async Task<ReservationDto> MoveAsync(Guid id, MoveReservationRequest request, CancellationToken ct = default)
	{
		_moveValidator.EnsureValid(request);
		var reservation = await LoadActiveAsync(id, ct);
		var line = reservation.Lines.FirstOrDefault(l => l.PublicId == request.LineId && l.DeletedAt is null)
			?? throw DomainException.NotFound(BookingErrors.LineNotFound);
		var from = request.From ?? line.StartDate;
		var to = request.To ?? line.EndDate;
		if (from < line.StartDate || to > line.EndDate)
			throw DomainException.Unprocessable(BookingErrors.MoveOutsideLine);

		var spots = new List<ReservationSpotInput>();
		foreach (var l in reservation.Lines.Where(x => x.DeletedAt is null).OrderBy(x => x.StartDate))
		{
			if (l != line)
			{
				spots.Add(ReservationRequests.InputOf(reservation, l, l.SpotId!.Value, l.StartDate, l.EndDate));
				continue;
			}
			if (from > l.StartDate)
				spots.Add(ReservationRequests.InputOf(reservation, l, l.SpotId!.Value, l.StartDate, from.AddDays(-1)));
			spots.Add(ReservationRequests.InputOf(reservation, l, request.ToSpotId, from, to) with { ManualPrice = null });
			if (to < l.EndDate)
				spots.Add(ReservationRequests.InputOf(reservation, l, l.SpotId!.Value, to.AddDays(1), l.EndDate));
		}

		var request2 = ReservationRequests.StateOf(reservation, spots);
		var plan = await _writer.ApplyAsync(reservation, request2, ct);
		reservation.Touch(_scope.UserId);
		await SaveWithConsumptionsAsync(reservation, plan, ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.ReservationMoved, reservation, new Dictionary<string, object?>
		{
			["fromSpot"] = line.SpotLabelSnapshot, ["toSpotId"] = request.ToSpotId, ["from"] = from, ["to"] = to, ["total"] = reservation.Total,
		}, line.SpotLabelSnapshot, ct);
		return await ToDtoAsync(reservation, plan.Warnings, ct);
	}

	public async Task<ReservationDto> GetAsync(Guid id, CancellationToken ct = default)
	{
		var reservation = await _reservations.GetAsync(_scope.PropertyId, id, includeTrashed: true, ct)
			?? throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		await EnsureVisibleAsync(reservation, ct);
		return await ToDtoAsync(reservation, [], ct);
	}

	public async Task<PagedResult<ReservationSummaryDto>> ListAsync(ReservationQuery query, bool trashed = false, CancellationToken ct = default)
	{
		_queryValidator.EnsureValid(query);
		var propertyId = _scope.PropertyId;
		IReadOnlyList<Guid>? customerIds = null;
		IReadOnlyList<Guid>? searchSpots = null;
		if (!string.IsNullOrWhiteSpace(query.Search))
		{
			var term = query.Search.Trim();
			customerIds = await _customers.SearchAsync(propertyId, term, 500, ct);
			searchSpots = (await _spots.AllAsync(ct)).Values
				.Where(s => s.Label is { } label && label.Contains(term, StringComparison.OrdinalIgnoreCase)).Select(s => s.Id).Take(200).ToList();
		}
		var filter = new ReservationFilter(
			propertyId, query.From, query.To, ParseStatus(query.Status), query.PaymentStatus,
			query.Channel is null ? null : query.Channel == "customer" ? BookingChannel.Customer : BookingChannel.Manager,
			customerIds, string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim(), query.AgreementPartnerId, query.Seasonal, trashed,
			query.SortBy ?? (trashed ? "deletedAt" : "from"), query.SortDir == SortDirections.Desc || (query.SortDir is null && trashed),
			query.Page, query.PageSize, query.SpotId is { } spot ? [spot] : null, searchSpots);
		var (items, total) = await _reservations.ListAsync(filter, ct);
		var cards = await CardsAsync(items.Select(r => r.CustomerId), ct);
		var amounts = _scope.CanSeeAmounts;
		return new PagedResult<ReservationSummaryDto>(total, query.Page, query.PageSize, items.Select(r => Summary(r, cards, amounts)).ToList());
	}

	/// <summary>Una prenotazione negli elenchi (pagina Prenotazioni, Database, Cestino, dettaglio di un gruppo).</summary>
	public static ReservationSummaryDto Summary(Reservation r, IReadOnlyDictionary<Guid, CustomerCard> cards, bool amounts)
		=> new(
			r.PublicId, r.BookingNumber, Codes.Of(r.Status), Codes.Of(r.Channel), r.HasSeasonalPass, r.StayFrom, r.StayTo, r.CustomerId,
			r.CustomerId is { } c && cards.TryGetValue(c, out var card) ? card.FullName : null,
			r.Lines.Where(l => l.DeletedAt is null).OrderBy(l => l.StartDate).Select(l => l.SpotLabelSnapshot ?? "").Distinct().ToList(),
			r.AgreementLines.Select(a => (Guid?)a.AgreementPartnerId).FirstOrDefault() ?? r.AgreementPartnerId,
			PaymentStatusOf(r), amounts ? r.Total : null, amounts ? r.BalanceDue : null, r.CheckedInAt is not null, r.IsTrashed, r.DeletedAt,
			r.CreatedAt);

	/// <summary>
	/// Scambia le postazioni di due prenotazioni nei giorni in comune (eventualmente solo in una parte): le righe si
	/// spezzano dove serve e la differenza di prezzo si applica da sola a entrambe.
	/// </summary>
	public async Task<SwapResultDto> SwapAsync(Guid id, SwapReservationsRequest request, CancellationToken ct = default)
	{
		_swapValidator.EnsureValid(request);
		var first = await LoadActiveAsync(id, ct);
		var firstLine = first.Lines.FirstOrDefault(l => l.PublicId == request.LineId && l.DeletedAt is null)
			?? throw DomainException.NotFound(BookingErrors.LineNotFound);
		var second = await _reservations.FindByLineAsync(_scope.PropertyId, request.WithLineId, ct)
			?? throw DomainException.NotFound(BookingErrors.LineNotFound);
		if (second.Id == first.Id)
			throw DomainException.Unprocessable(BookingErrors.SwapSameReservation);
		if (second.Status == ReservationStatus.Cancelled)
			throw DomainException.Conflict(BookingErrors.ReservationCancelled);
		await EnsureVisibleAsync(second, ct);
		var secondLine = second.Lines.First(l => l.PublicId == request.WithLineId);

		var from = new[] { firstLine.StartDate, secondLine.StartDate, request.From ?? DateOnly.MinValue }.Max();
		var to = new[] { firstLine.EndDate, secondLine.EndDate, request.To ?? DateOnly.MaxValue }.Min();
		if (from > to)
			throw DomainException.Unprocessable(BookingErrors.SwapNoCommonDays);

		var firstSpot = firstLine.SpotId!.Value;
		var secondSpot = secondLine.SpotId!.Value;
		var firstPlan = await _writer.ApplyAsync(first, ReservationRequests.StateOf(first, SwappedSpots(first, firstLine, secondSpot, from, to)),
			new WriteOptions(ReleasingLineIds: [secondLine.Id]), ct);
		var secondPlan = await _writer.ApplyAsync(second, ReservationRequests.StateOf(second, SwappedSpots(second, secondLine, firstSpot, from, to)),
			new WriteOptions(ReleasingLineIds: [firstLine.Id]), ct);
		first.Touch(_scope.UserId);
		second.Touch(_scope.UserId);
		await _writer.SaveAsync([(first, firstPlan), (second, secondPlan)], ct);

		foreach (var (reservation, spotFrom, spotTo, other) in new[] { (first, firstLine, secondLine, second), (second, secondLine, firstLine, first) })
			await _audit.RecordAsync(AuditActionCodes.Booking.ReservationsSwapped, reservation, new Dictionary<string, object?>
			{
				["fromSpot"] = spotFrom.SpotLabelSnapshot, ["toSpot"] = spotTo.SpotLabelSnapshot, ["from"] = from, ["to"] = to,
				["otherReservation"] = other.BookingNumber, ["total"] = reservation.Total,
			}, spotFrom.SpotLabelSnapshot, ct);
		return new SwapResultDto(await ToDtoAsync(first, firstPlan.Warnings, ct), await ToDtoAsync(second, secondPlan.Warnings, ct), from, to);
	}

	/// <summary>Le postazioni della prenotazione con la riga indicata spostata su <paramref name="toSpot"/> fra le due date.</summary>
	private static List<ReservationSpotInput> SwappedSpots(Reservation reservation, BookingLine line, Guid toSpot, DateOnly from, DateOnly to)
	{
		var spots = new List<ReservationSpotInput>();
		foreach (var l in reservation.Lines.Where(x => x.DeletedAt is null).OrderBy(x => x.StartDate))
		{
			if (l != line)
			{
				spots.Add(ReservationRequests.InputOf(reservation, l, l.SpotId!.Value, l.StartDate, l.EndDate));
				continue;
			}
			if (from > l.StartDate)
				spots.Add(ReservationRequests.InputOf(reservation, l, l.SpotId!.Value, l.StartDate, from.AddDays(-1)));
			spots.Add(ReservationRequests.InputOf(reservation, l, toSpot, from, to) with { ManualPrice = null });
			if (to < l.EndDate)
				spots.Add(ReservationRequests.InputOf(reservation, l, l.SpotId!.Value, to.AddDays(1), l.EndDate));
		}
		return spots;
	}

	/// <summary>Incasso dal gestionale; oltre il residuo serve la conferma del gestore.</summary>
	public async Task<ReservationDto> RecordPaymentAsync(Guid id, RecordPaymentRequest request, CancellationToken ct = default)
	{
		_paymentValidator.EnsureValid(request);
		var reservation = await LoadActiveAsync(id, ct);
		if (request.Amount > reservation.BalanceDue && !request.ConfirmOverBalance)
			throw DomainException.Conflict(BookingErrors.PaymentExceedsBalance, new Dictionary<string, object?> { ["balanceDue"] = reservation.BalanceDue });
		await _payments.RecordAsync(new ReservationPaymentInput(
			reservation.PropertyId, reservation.PublicId, request.Amount, request.Mode, request.IsDeposit, reservation.CustomerId,
			reservation.StayFrom, reservation.StayTo), ct);
		return await ToDtoAsync(reservation, [], ct);
	}

	/// <summary>Spunta «Pagato»: incasso del residuo con il metodo scelto, data di oggi.</summary>
	public async Task<ReservationDto> MarkPaidAsync(Guid id, MarkPaidRequest request, CancellationToken ct = default)
	{
		_markPaidValidator.EnsureValid(request);
		var reservation = await LoadActiveAsync(id, ct);
		if (reservation.PaidMarkEntryId is not null)
			throw DomainException.Conflict(BookingErrors.AlreadyMarkedPaid);
		if (reservation.BalanceDue <= 0)
			throw DomainException.Unprocessable(BookingErrors.NothingToPay);
		await MarkPaidCoreAsync(reservation, request.Mode, ct);
		return await ToDtoAsync(reservation, [], ct);
	}

	/// <summary>
	/// Spunta «Pagato» tolta: si annulla l'incasso che aveva creato. Se nel frattempo era stato emesso lo scontrino, va
	/// annullato anche quello (il frontend avvisa il gestore della procedura).
	/// </summary>
	public async Task<ReservationDto> UnmarkPaidAsync(Guid id, CancellationToken ct = default)
	{
		var reservation = await LoadActiveAsync(id, ct);
		if (reservation.PaidMarkEntryId is not { } entry)
			throw DomainException.Conflict(BookingErrors.NotMarkedPaid);
		await _payments.VoidAsync(reservation.PropertyId, entry, ct);
		reservation.MarkPaidEntry(null);
		await _reservations.SaveChangesAsync(ct);
		return await ToDtoAsync(reservation, [], ct);
	}

	public async Task<ReservationDto> CheckInAsync(Guid id, CancellationToken ct = default)
	{
		var reservation = await LoadActiveAsync(id, ct);
		reservation.CheckIn(_scope.UtcNow);
		await _reservations.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.ReservationCheckedIn, reservation, ct: ct);
		return await ToDtoAsync(reservation, [], ct);
	}

	/// <summary>Check-in tolto (anche il check-out): i giorni liberati dal check-out tornano occupati, se nessuno li ha presi.</summary>
	public async Task<ReservationDto> UndoCheckInAsync(Guid id, CancellationToken ct = default)
	{
		var reservation = await LoadActiveAsync(id, ct);
		if (reservation.CheckoutEffectiveAt is { } checkout)
		{
			var since = DateOnly.FromDateTime(Shared.Localization.ItalianTime.FromUtc(checkout));
			var ceded = await _sharing.CededDaysAsync(reservation, ct);
			foreach (var c in await _reservations.ListLineConsumptionsAsync(reservation.Lines.Where(l => l.DeletedAt is null).Select(l => l.Id).ToList(), ct))
				if (c.ConsumptionDate >= since && c.State == ConsumptionState.Released && !(c.SourceId is { } sid && ceded.Contains((sid, c.ConsumptionDate))))
					c.Reactivate();
		}
		reservation.UndoCheckIn();
		await _reservations.SaveChangesAsync(ct);
		return await ToDtoAsync(reservation, [], ct);
	}

	/// <summary>Check-out, anche anticipato: la postazione si libera subito, oggi compreso, senza rimborso.</summary>
	public async Task<ReservationDto> CheckOutAsync(Guid id, CancellationToken ct = default)
	{
		var reservation = await LoadActiveAsync(id, ct);
		var today = _scope.Today;
		reservation.CheckOut(_scope.UtcNow);
		var released = 0;
		foreach (var c in await _reservations.ListLineConsumptionsAsync(reservation.Lines.Where(l => l.DeletedAt is null).Select(l => l.Id).ToList(), ct))
			if (c.ConsumptionDate >= today && c.State != ConsumptionState.Released)
			{
				c.Release();
				released++;
			}
		await _reservations.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.ReservationCheckedOut, reservation,
			new Dictionary<string, object?> { ["releasedDays"] = released, ["early"] = reservation.StayTo > today }, ct: ct);
		return await ToDtoAsync(reservation, [], ct);
	}

	/// <summary>«Segnala ingresso» su una postazione della prenotazione, per oggi.</summary>
	public async Task<ReservationDto> AddEntranceAsync(Guid id, EntranceRequest request, CancellationToken ct = default)
	{
		var reservation = await LoadActiveAsync(id, ct);
		var line = reservation.Lines.FirstOrDefault(l => l.PublicId == request.LineId && l.DeletedAt is null)
			?? throw DomainException.NotFound(BookingErrors.LineNotFound);
		var today = _scope.Today;
		if (!line.Covers(today))
			throw DomainException.Unprocessable(BookingErrors.DayNotInReservation);
		var entrances = await _reservations.ListEntrancesAsync(reservation.PropertyId, today, ct);
		if (!entrances.Any(e => e.BookingLineId == line.Id))
		{
			_reservations.AddEntrance(BookingEntrance.Create(line, today, _scope.UtcNow, _scope.UserId));
			await _reservations.SaveChangesAsync(ct);
		}
		return await ToDtoAsync(reservation, [], ct);
	}

	public async Task<ReservationDto> RemoveEntranceAsync(Guid id, Guid lineId, CancellationToken ct = default)
	{
		var reservation = await LoadActiveAsync(id, ct);
		var line = reservation.Lines.FirstOrDefault(l => l.PublicId == lineId && l.DeletedAt is null)
			?? throw DomainException.NotFound(BookingErrors.LineNotFound);
		var entrance = (await _reservations.ListEntrancesAsync(reservation.PropertyId, _scope.Today, ct)).FirstOrDefault(e => e.BookingLineId == line.Id);
		if (entrance is not null)
		{
			_reservations.RemoveEntrance(entrance);
			await _reservations.SaveChangesAsync(ct);
			await _audit.RecordAsync(AuditActionCodes.Booking.EntranceRemoved, reservation, null, line.SpotLabelSnapshot, ct);
		}
		return await ToDtoAsync(reservation, [], ct);
	}

	// ----- Supporto -----

	private async Task MarkPaidCoreAsync(Reservation reservation, string mode, CancellationToken ct)
	{
		var entry = await _payments.RecordAsync(new ReservationPaymentInput(
			reservation.PropertyId, reservation.PublicId, reservation.BalanceDue, mode, false, reservation.CustomerId,
			reservation.StayFrom, reservation.StayTo), ct);
		reservation.MarkPaidEntry(entry);
		await _reservations.SaveChangesAsync(ct);
	}

	/// <summary>Righe e testata, poi i consumi (vogliono l'id delle righe), nella stessa transazione.</summary>
	private Task SaveWithConsumptionsAsync(Reservation reservation, WritePlan plan, CancellationToken ct)
		=> _writer.SaveAsync([(reservation, plan)], ct);

	private async Task<Reservation> LoadActiveAsync(Guid id, CancellationToken ct)
	{
		var reservation = await _reservations.GetAsync(_scope.PropertyId, id, includeTrashed: false, ct)
			?? throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		if (reservation.Status == ReservationStatus.Cancelled)
			throw DomainException.Conflict(BookingErrors.ReservationCancelled);
		await EnsureVisibleAsync(reservation, ct);
		return reservation;
	}

	/// <summary>L'affiliato vede solo le prenotazioni che hanno almeno una delle sue postazioni.</summary>
	private async Task EnsureVisibleAsync(Reservation reservation, CancellationToken ct)
	{
		if (!await _spots.CanSeeAsync(reservation, ct))
			throw DomainException.NotFound(BookingErrors.ReservationNotFound);
	}

	private async Task<Guid?> ResolveCustomerAsync(CustomerInput? input, Guid? current, bool confirmBlacklisted, bool requireCustomer, CancellationToken ct)
	{
		if (input is null)
			return requireCustomer ? throw DomainException.Unprocessable(BookingErrors.CustomerNameRequired) : current;
		if (input.IsGeneric)
			return null;

		var propertyId = _scope.PropertyId;
		if (input.CustomerId is { } existing)
		{
			var card = (await _customers.ListAsync(propertyId, [existing], ct)).FirstOrDefault()
				?? throw DomainException.NotFound(BookingErrors.CustomerNotFound);
			if (card.IsBlacklisted && !confirmBlacklisted && existing != current)
				throw DomainException.Conflict(BookingErrors.CustomerBlacklisted, new Dictionary<string, object?> { ["customerId"] = existing });
			return existing;
		}

		if (string.IsNullOrWhiteSpace(input.FullName))
			throw DomainException.Unprocessable(BookingErrors.CustomerNameRequired);
		var contact = new CustomerContact(input.FullName, input.Email, input.PhoneCountryCode, input.Phone);
		var hasContact = !string.IsNullOrWhiteSpace(input.Email) || !string.IsNullOrWhiteSpace(input.Phone);
		if (hasContact && !confirmBlacklisted && await _customers.IsBlacklistedAsync(propertyId, input.Email, input.Phone, ct))
			throw DomainException.Conflict(BookingErrors.CustomerBlacklisted);
		return hasContact ? await _customers.FindOrCreateAsync(propertyId, contact, ct) : await _customers.CreateAsync(propertyId, contact, ct);
	}

	private static void SyncGuests(Reservation reservation, IReadOnlyList<string>? guests)
	{
		var wanted = (guests ?? []).Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim()).ToList();
		foreach (var guest in reservation.Guests.Where(g => !wanted.Contains(g.FullNameSnapshot)).ToList())
			reservation.Guests.Remove(guest);
		foreach (var name in wanted.Where(n => !reservation.Guests.Any(g => g.FullNameSnapshot == n)))
			reservation.Guests.Add(BookingGuest.Create(reservation, name, null));
	}

	private sealed record PricingState(decimal Total, decimal? Override, decimal? ManualDiscount, decimal? Surcharge, bool ManualPackage);

	private static PricingState PricingSnapshot(Reservation r)
		=> new(r.Total, r.PriceOverride, r.ManualDiscountValue, r.SurchargeValue, r.Lines.Any(l => l.DeletedAt is null && l.ManualPrice is not null));

	/// <summary>Prezzo scritto a mano, sconto manuale o incremento cambiati: voce a sé nel registro.</summary>
	private async Task AuditPricingAsync(Reservation r, PricingState? before, CancellationToken ct)
	{
		var now = PricingSnapshot(r);
		var changed = before is null
			? now.Override is not null || now.ManualDiscount is not null || now.Surcharge is not null || now.ManualPackage
			: now with { Total = 0 } != before with { Total = 0 };
		if (changed)
			await _audit.RecordAsync(AuditActionCodes.Booking.ReservationPriceSet, r, new Dictionary<string, object?>
			{
				["priceOverride"] = r.PriceOverride, ["manualDiscount"] = r.ManualDiscountValue, ["manualDiscountType"] = r.ManualDiscountType is { } t ? Codes.Of(t) : null,
				["surcharge"] = r.SurchargeValue, ["surchargeLabel"] = r.SurchargeLabel, ["total"] = r.Total,
			}, ct: ct);
	}

	private static ReservationStatus? ParseStatus(string? status)
		=> status is null ? null : Codes.TryParse<ReservationStatus>(status, out var s) ? s : null;

	/// <summary>Stato del pagamento calcolato, uguale per mappa, planner ed elenchi.</summary>
	public static string PaymentStatusOf(Reservation r)
		=> r.Total <= 0 || r.Paid >= r.Total ? PaymentStatuses.Paid : r.Paid <= 0 ? PaymentStatuses.Unpaid : PaymentStatuses.Partial;

	/// <summary>Servizi della prenotazione; le righe che non sono servizi di Catalog sono prezzi automatici.</summary>
	private async Task<IReadOnlyList<ReservationServiceDto>> ServicesOfAsync(Reservation r, bool amounts, CancellationToken ct)
	{
		var lines = r.ServiceLines.Where(s => s.DeletedAt is null).ToList();
		if (lines.Count == 0)
			return [];
		var catalog = await _serviceWriter.ServicesAsync(r.PropertyId, ct);
		return lines.OrderBy(s => s.ServiceDate).ThenBy(s => s.Id).Select(s =>
		{
			var slot = s.TimeSlots.FirstOrDefault(t => t.DeletedAt is null);
			var kind = s.SaleItemId is { } id && catalog.ContainsKey(id) ? Codes.Of(s.Kind) : QuoteLineKinds.AutomaticPrice;
			return new ReservationServiceDto(s.PublicId, s.SaleItemId, kind, s.NameSnapshot, s.VariantLabel, (short)s.Quantity, s.ServiceDate,
				slot?.SlotStart, slot?.SlotEnd, slot?.TimeSlotId, amounts ? s.LineTotal : null, s.BookingLine?.SpotId);
		}).ToList();
	}

	public async Task<Dictionary<Guid, CustomerCard>> CardsAsync(IEnumerable<Guid?> ids, CancellationToken ct)
	{
		var list = ids.Where(i => i is not null).Select(i => i!.Value).Distinct().ToList();
		return list.Count == 0 ? [] : (await _customers.ListAsync(_scope.PropertyId, list, ct)).ToDictionary(c => c.Id);
	}

	public async Task<ReservationDto> ToDtoAsync(Reservation r, IReadOnlyList<string> warnings, CancellationToken ct)
	{
		var card = r.CustomerId is { } c ? (await CardsAsync([c], ct)).GetValueOrDefault(c) : null;
		var entered = r.IsTrashed ? [] : (await _reservations.ListEntrancesAsync(r.PropertyId, _scope.Today, ct)).Select(e => e.BookingLineId).ToHashSet();
		var amounts = _scope.CanSeeAmounts;
		var offers = r.Id == 0 ? [] : await _resale.ListBySourceAsync([r.Id], ct);
		var lines = r.Lines.Where(l => l.DeletedAt is null).OrderBy(l => l.StartDate).ThenBy(l => l.SpotLabelSnapshot).Select(l =>
		{
			var agreements = r.AgreementLines.Where(a => a.BookingLineId == l.Id || a.BookingLine == l).ToList();
			return new ReservationLineDto(
				l.PublicId, l.BookingLineNumber, l.SpotId, l.SpotLabelSnapshot, l.SpotTypeSnapshot, l.SectorId, l.SectorLabelSnapshot, l.StartDate, l.EndDate,
				SpotKinds.CodeOf(l.BookingPeriod), l.Quantity, l.IsArea, l.PriceListId, l.PackageNameSnapshot, amounts ? l.ManualPrice : null,
				amounts ? l.LinePrice : null,
				agreements.Select(a => (Guid?)a.AgreementPartnerId).FirstOrDefault(),
				agreements.FirstOrDefault(a => a.ListType == AgreementLineType.Regular)?.AgreementPriceListId,
				agreements.FirstOrDefault(a => a.ListType == AgreementLineType.Voucher)?.AgreementPriceListId,
				r.AdditionalItems.Where(a => a.BookingLineId == l.Id || a.BookingLine == l)
					.Select(a => new ReservationAdditionalDto(a.CatalogItemId, a.NameSnapshot, a.Quantity, a.Dates, amounts ? a.LineTotal : null)).ToList(),
				entered.Contains(l.Id),
				offers.Where(o => o.BookingLineId == l.Id).OrderBy(o => o.OfferDate).Select(o => new ReleasedDayDto(
					o.PublicId, o.OfferDate, SpotKinds.CodeOf(o.OfferedPeriod), o.Origin == ResaleOrigin.Absence ? ReleaseKinds.Absence : ReleaseKinds.Sharing,
					Codes.Of(o.OfferState), o.ExcludesFromPrice, null)).ToList());
		}).ToList();
		var code = r.Discounts.FirstOrDefault();
		var group = r.Id == 0 ? null : (await _groups.GroupsOfAsync([r.Id], ct)).GetValueOrDefault(r.Id);
		return new ReservationDto(
			r.PublicId, r.BookingNumber, r.ReservationCode, Codes.Of(r.Status), r.CancellationReason is { } reason ? Codes.Of(reason) : null,
			Codes.Of(r.Channel), r.HasSeasonalPass, r.StayFrom, r.StayTo,
			card is null ? null : new ReservationCustomerDto(card.Id, card.FullName, card.Email, card.PhoneCountryCode, card.Phone, card.IsBlacklisted),
			PaymentStatusOf(r),
			amounts
				? new ReservationAmountsDto(
					r.Subtotal, code?.AppliedAmount ?? 0, code?.CodeSnapshot,
					r.ManualDiscountType is { } mt ? new AdjustmentInput(Codes.Of(mt), r.ManualDiscountValue ?? 0, null) : null, r.ManualDiscountAmount,
					r.SurchargeType is { } st ? new AdjustmentInput(Codes.Of(st), r.SurchargeValue ?? 0, r.SurchargeLabel) : null, r.Surcharge,
					r.PriceOverride, r.Total, r.Deposit, r.Paid, r.BalanceDue, Math.Max(0, -r.BalanceDue),
					r.PrevalentPaymentMode is { } pm ? Codes.Of(pm) : null, r.PaidMarkEntryId is not null)
				: null,
			r.Note, r.NoteUpdatedAt, r.NoteUpdatedByUserId, r.CustomerTags, r.Guests.Select(g => g.FullNameSnapshot).ToList(), lines,
			r.CheckedInAt, r.CheckedOutAt, r.IsTrashed, r.DeletedAt, r.CreatedAt, r.OperatorUserId, r.UpdatedAt, warnings,
			group is null ? null : new ReservationGroupDto(group.PublicId, group.Name, group.ReservationCount),
			r.RenewedFromReservationId is not null, r.SplitFromReservationId is not null, Codes.Of(r.Kind),
			await ServicesOfAsync(r, amounts, ct),
			r.TableLines.Where(t => t.DeletedAt is null).OrderBy(t => t.ServiceDate).ThenBy(t => t.SlotStart).Select(t => new ReservationTableDto(
				t.PublicId, t.TableServiceId, t.TableServiceNameSnapshot, t.ServiceDate, t.SlotStart, t.SlotEnd, t.Covers, t.TableSeats,
				amounts ? t.DepositDue : null)).ToList(), r.Version);
	}
}
