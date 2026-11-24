using BuildingBlocks.Application.Validation;
using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Dtos;
using Module.Booking.Application.Validators;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Dtos;
using Shared.Utils;

namespace Module.Booking.Application.Services;

/// <summary>
/// Assenze programmate e sharing dal gestionale. Il titolare cede alcuni giorni di una postazione: la postazione torna
/// prenotabile e, se qualcuno la riprende, con lo sharing il titolare matura il credito (manuale, voucher, ricarica della
/// tessera, come impostato). Un'assenza con «ricalcola prezzo» toglie i giorni dal prezzo. Un giorno già ripreso non si
/// ritira. Le impostazioni sono la pagina Impostazioni › Sharing.
/// </summary>
public sealed class SharingService
{
	private readonly IReservationRepository _reservations;
	private readonly IResaleRepository _resale;
	private readonly SharingLedger _sharing;
	private readonly ReservationWriter _writer;
	private readonly ReservationService _reservationService;
	private readonly SpotDirectory _spots;
	private readonly BookingScope _scope;
	private readonly BookingAudit _audit;
	private readonly SharingSettingsValidator _settingsValidator;
	private readonly ReleaseDaysValidator _releaseValidator;
	private readonly WithdrawDaysValidator _withdrawValidator;
	private readonly SharingQueryValidator _queryValidator;
	private readonly CreditAdjustmentValidator _creditValidator;

	public SharingService(
		IReservationRepository reservations, IResaleRepository resale, SharingLedger sharing, ReservationWriter writer, ReservationService reservationService,
		SpotDirectory spots, BookingScope scope, BookingAudit audit, SharingSettingsValidator settingsValidator, ReleaseDaysValidator releaseValidator,
		WithdrawDaysValidator withdrawValidator, SharingQueryValidator queryValidator, CreditAdjustmentValidator creditValidator)
	{
		_reservations = reservations;
		_resale = resale;
		_sharing = sharing;
		_writer = writer;
		_reservationService = reservationService;
		_spots = spots;
		_scope = scope;
		_audit = audit;
		_settingsValidator = settingsValidator;
		_releaseValidator = releaseValidator;
		_withdrawValidator = withdrawValidator;
		_queryValidator = queryValidator;
		_creditValidator = creditValidator;
	}

	// ----- Impostazioni -----

	public async Task<SharingSettingsDto> GetSettingsAsync(CancellationToken ct = default)
		=> ToDto(await _resale.GetSettingsAsync(_scope.PropertyId, ct) ?? ResaleSettings.Default(_scope.PropertyId));

	public async Task<SharingSettingsDto> SaveSettingsAsync(SharingSettingsDto request, CancellationToken ct = default)
	{
		_settingsValidator.EnsureValid(request);
		var settings = await _resale.GetSettingsAsync(_scope.PropertyId, ct);
		if (settings is null)
		{
			settings = ResaleSettings.Default(_scope.PropertyId);
			_resale.AddSettings(settings);
		}
		var scopes = new List<ResaleScope>();
		if (request.SeasonalOnly)
			scopes.Add(ResaleScope.SeasonalOnly);
		if (request.AllArticles)
			scopes.Add(ResaleScope.AllArticles);
		settings.Update(request.IsEnabled, request.MinBookingDays, scopes, request.AbsencesAsSharing, request.ManualCreditEnabled,
			request.ManualCreditUnitsPerDay, request.ManualCreditAmount, request.VoucherCreditEnabled, request.VoucherCreditAmount,
			request.CardTopUpEnabled, request.CardTopUpAmount);
		await _resale.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.SharingSettingsUpdated, settings.PropertyId, AuditActionCodes.Booking.Targets.SharingSettings,
			null, new Dictionary<string, object?> { ["enabled"] = settings.IsEnabled }, ct: ct);
		return ToDto(settings);
	}

	// ----- Giorni ceduti di una prenotazione -----

	/// <summary>Assenza programmata o sharing su alcuni giorni di una postazione della prenotazione.</summary>
	public async Task<ReservationDto> ReleaseAsync(Guid reservationId, ReleaseDaysRequest request, CancellationToken ct = default)
	{
		_releaseValidator.EnsureValid(request);
		var reservation = await LoadAsync(reservationId, ct);
		var line = reservation.Lines.FirstOrDefault(l => l.PublicId == request.LineId && l.DeletedAt is null)
			?? throw DomainException.NotFound(BookingErrors.LineNotFound);
		if (line.IsArea || line.SpotId is null)
			throw DomainException.Unprocessable(BookingErrors.DayNotReleasable);
		var sharing = request.Kind == ReleaseKinds.Sharing;
		var settings = await _sharing.SettingsAsync(reservation.PropertyId, ct);
		if (sharing)
			EnsureSharingAllowed(reservation, line, settings);

		var period = SpotKinds.ParsePeriod(request.DayPart);
		if (line.BookingPeriod != DayPeriod.FullDay)
		{
			if (period != DayPeriod.FullDay && period != line.BookingPeriod)
				throw DomainException.Unprocessable(ValidationErrorCodes.Invalid, new Dictionary<string, object?> { ["field"] = "dayPart" });
			period = line.BookingPeriod;
		}

		var days = request.Dates.Distinct().Order().ToList();
		var today = _scope.Today;
		foreach (var day in days)
		{
			if (!line.Covers(day))
				throw DomainException.Unprocessable(BookingErrors.DayNotInReservation, new Dictionary<string, object?> { ["day"] = day });
			if (day < today)
				throw DomainException.Unprocessable(BookingErrors.DayNotReleasable, new Dictionary<string, object?> { ["day"] = day });
		}
		var existing = (await _resale.ListBySourceAsync([reservation.Id], ct)).Where(o => o.BookingLineId == line.Id).Select(o => o.OfferDate).ToHashSet();
		if (days.FirstOrDefault(existing.Contains) is var taken && existing.Contains(taken))
			throw DomainException.Conflict(BookingErrors.DayAlreadyReleased, new Dictionary<string, object?> { ["day"] = taken });

		var consumptions = (await _reservations.ListLineConsumptionsAsync([line.Id], ct))
			.Where(c => c.State != ConsumptionState.Released && c.ResourceId == line.SpotId).ToDictionary(c => c.ConsumptionDate);
		var origin = sharing ? ResaleOrigin.Sharing : settings is { IsEnabled: true, AbsencesAsSharing: true } ? ResaleOrigin.AbsenceAsSharing : ResaleOrigin.Absence;
		foreach (var day in days)
		{
			// giorno già liberato (uscita anticipata): non c'è niente da cedere
			if (!consumptions.TryGetValue(day, out var consumption))
				throw DomainException.Unprocessable(BookingErrors.DayNotReleasable, new Dictionary<string, object?> { ["day"] = day });
			if (period == line.BookingPeriod)
				consumption.Release();
			else
				consumption.KeepOnly(period == DayPeriod.Morning ? DayPeriod.Afternoon : DayPeriod.Morning);
			_resale.AddOffer(ResaleOffer.Create(origin, reservation, line, day, period, !sharing && request.RecalculatePrice));
		}

		var warnings = await SaveAndRepriceAsync(reservation, !sharing && request.RecalculatePrice, ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.DaysReleased, reservation, new Dictionary<string, object?>
		{
			["kind"] = request.Kind, ["origin"] = Codes.Of(origin), ["days"] = days, ["dayPart"] = SpotKinds.CodeOf(period),
			["recalculatePrice"] = request.RecalculatePrice, ["total"] = reservation.Total,
		}, line.SpotLabelSnapshot, ct);
		return await _reservationService.ToDtoAsync(reservation, warnings, ct);
	}

	/// <summary>Ritiro di giorni ceduti di una prenotazione: tornano del titolare (e, se erano fuori prezzo, si ripagano).</summary>
	public async Task<ReservationDto> WithdrawAsync(Guid reservationId, WithdrawDaysRequest request, CancellationToken ct = default)
	{
		_withdrawValidator.EnsureValid(request);
		var reservation = await LoadAsync(reservationId, ct);
		var offers = await _resale.GetOffersAsync(reservation.PropertyId, request.OfferIds.Distinct().ToList(), ct);
		if (offers.Count != request.OfferIds.Distinct().Count() || offers.Any(o => o.SourceReservationId != reservation.Id))
			throw DomainException.NotFound(BookingErrors.OfferNotFound);
		var warnings = await WithdrawCoreAsync(reservation, offers, ct);
		return await _reservationService.ToDtoAsync(reservation, warnings, ct);
	}

	/// <summary>Ritiro dalla pagina «Sharing creati»: ogni giorno ha il suo esito.</summary>
	public async Task<BulkResult> WithdrawManyAsync(WithdrawDaysRequest request, CancellationToken ct = default)
	{
		_withdrawValidator.EnsureValid(request);
		var ids = request.OfferIds.Distinct().ToList();
		var offers = await _resale.GetOffersAsync(_scope.PropertyId, ids, ct);
		var failed = ids.Where(i => offers.All(o => o.PublicId != i)).Select(i => new BulkFailure(i, BookingErrors.OfferNotFound)).ToList();
		var succeeded = new List<Guid>();
		foreach (var group in offers.GroupBy(o => o.SourceReservationId))
		{
			var reservation = await _reservations.GetAsync(_scope.PropertyId, group.First().SourceReservation.PublicId, false, ct);
			if (reservation is null || !reservation.IsActive || !await _spots.CanSeeAsync(reservation, ct))
			{
				failed.AddRange(group.Select(o => new BulkFailure(o.PublicId, BookingErrors.ReservationNotFound)));
				continue;
			}
			var taken = group.Where(o => o.OfferState == ResaleOfferState.Reassigned).ToList();
			failed.AddRange(taken.Select(o => new BulkFailure(o.PublicId, BookingErrors.OfferAlreadyTaken)));
			var free = group.Except(taken).ToList();
			try
			{
				await WithdrawCoreAsync(reservation, free, ct);
				succeeded.AddRange(free.Select(o => o.PublicId));
			}
			catch (DomainException ex)
			{
				_reservations.DiscardChanges();
				failed.AddRange(free.Select(o => new BulkFailure(o.PublicId, ex.Code, ex.Args)));
			}
		}
		return new BulkResult(succeeded, failed);
	}

	// ----- Pagina «Sharing creati» -----

	public async Task<PagedResult<SharingRowDto>> ListAsync(SharingQuery query, CancellationToken ct = default)
	{
		_queryValidator.EnsureValid(query);
		IReadOnlyCollection<ResaleOrigin>? origins = (query.Kind ?? ReleaseKinds.Sharing) switch
		{
			ReleaseKinds.Absence => [ResaleOrigin.Absence],
			ReleaseKinds.All => null,
			_ => [ResaleOrigin.Sharing, ResaleOrigin.AbsenceAsSharing],
		};
		var offers = await _resale.SearchAsync(_scope.PropertyId, query.From, query.To, origins, 10_000, ct);
		var visible = await _spots.VisibleIdsAsync(ct);
		offers = offers.Where(o => visible.Contains(o.ResourceId)).ToList();
		var cards = await _reservationService.CardsAsync(offers.Select(o => o.SourceReservation.CustomerId), ct);

		var rows = offers
			.GroupBy(o => (o.SourceReservationId, o.BookingLineId, Sharing: o.Origin != ResaleOrigin.Absence))
			.Select(g =>
			{
				var first = g.First();
				var available = g.Where(o => o.OfferState == ResaleOfferState.Available).OrderBy(o => o.OfferDate).ToList();
				var reservation = first.SourceReservation;
				return new SharingRowDto(
					reservation.PublicId, reservation.BookingNumber,
					reservation.CustomerId is { } c && cards.TryGetValue(c, out var card) ? card.FullName : null,
					first.BookingLine?.PublicId, first.BookingLine?.SpotLabelSnapshot, g.Key.Sharing ? ReleaseKinds.Sharing : ReleaseKinds.Absence,
					available.Count > 0 ? "available" : "completed",
					g.Select(o => o.OfferDate).Distinct().Order().ToList(), available.Select(o => o.OfferDate).ToList(), available.Select(o => o.PublicId).ToList());
			})
			.Where(r => query.Status is null || r.Status == query.Status)
			.Where(r => string.IsNullOrWhiteSpace(query.Search)
				|| r.BookingNumber.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase)
				|| (r.CustomerName?.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase) ?? false)
				|| (r.SpotLabel?.Contains(query.Search.Trim(), StringComparison.OrdinalIgnoreCase) ?? false))
			.OrderByDescending(r => r.Dates.Max())
			.ToList();
		return new PagedResult<SharingRowDto>(rows.Count, query.Page, query.PageSize,
			rows.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToList());
	}

	// ----- Crediti -----

	public async Task<CustomerCreditsDto> GetCreditsAsync(Guid customerId, CancellationToken ct = default)
	{
		var entries = await _resale.ListCreditsAsync(_scope.PropertyId, customerId, ct);
		return new CustomerCreditsDto(customerId, entries.Sum(e => e.CreditUnits), entries.Sum(e => e.Amount),
			entries.Select(e => new CreditEntryDto(e.OccurredAt, Codes.Of(e.EntryType), e.CreditUnits, e.Amount, e.SourceReservation?.BookingNumber, e.Note)).ToList());
	}

	/// <summary>Il gestore usa i crediti (valori negativi) o ne aggiunge a mano.</summary>
	public async Task<CustomerCreditsDto> AdjustCreditsAsync(CreditAdjustmentRequest request, CancellationToken ct = default)
	{
		_creditValidator.EnsureValid(request);
		_resale.AddCredit(ResaleCreditLedgerEntry.ManualAdjustment(_scope.PropertyId, request.CustomerId, request.Units, request.Amount, request.Note, _scope.UtcNow));
		await _resale.SaveChangesAsync(ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.SharingCreditAdjusted, _scope.PropertyId, AuditActionCodes.Booking.Targets.SharingCredit,
			request.CustomerId, new Dictionary<string, object?> { ["units"] = request.Units, ["amount"] = request.Amount, ["note"] = request.Note }, ct: ct);
		return await GetCreditsAsync(request.CustomerId, ct);
	}

	// ----- Supporto -----

	private async Task<Reservation> LoadAsync(Guid id, CancellationToken ct)
	{
		var reservation = await _reservations.GetAsync(_scope.PropertyId, id, includeTrashed: false, ct)
			?? throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		if (!await _spots.CanSeeAsync(reservation, ct))
			throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		if (reservation.Status == ReservationStatus.Cancelled)
			throw DomainException.Conflict(BookingErrors.ReservationCancelled);
		return reservation;
	}

	/// <summary>Sharing acceso, prenotazione abbastanza lunga, stagionale se richiesto, ombrellone salvo «tutti gli articoli».</summary>
	private static void EnsureSharingAllowed(Reservation reservation, BookingLine line, ResaleSettings? settings)
	{
		if (settings is not { IsEnabled: true })
			throw DomainException.Unprocessable(BookingErrors.SharingDisabled);
		var days = reservation.StayFrom is { } f && reservation.StayTo is { } t ? t.DayNumber - f.DayNumber + 1 : 0;
		if (settings.MinBookingDays is { } min && days < min)
			throw DomainException.Unprocessable(BookingErrors.SharingMinDays, new Dictionary<string, object?> { ["minDays"] = min });
		if (settings.AppliesTo.Contains(ResaleScope.SeasonalOnly) && !reservation.HasSeasonalPass)
			throw DomainException.Unprocessable(BookingErrors.SharingSeasonalOnly);
		if (!settings.AppliesTo.Contains(ResaleScope.AllArticles) && line.SpotTypeSnapshot is { } type && type != "umbrella")
			throw DomainException.Unprocessable(BookingErrors.SharingNotForArticle, new Dictionary<string, object?> { ["spotType"] = type });
	}

	private async Task<IReadOnlyList<string>> WithdrawCoreAsync(Reservation reservation, IReadOnlyCollection<ResaleOffer> offers, CancellationToken ct)
	{
		var live = offers.Where(o => o.OfferState != ResaleOfferState.Withdrawn).ToList();
		if (live.FirstOrDefault(o => o.OfferState == ResaleOfferState.Reassigned) is { } taken)
			throw DomainException.Conflict(BookingErrors.OfferAlreadyTaken, new Dictionary<string, object?> { ["offerId"] = taken.PublicId, ["day"] = taken.OfferDate });
		if (live.Count == 0)
			return [];
		var lines = reservation.Lines.Where(l => l.DeletedAt is null).ToDictionary(l => l.Id);
		var consumptions = await _reservations.ListLineConsumptionsAsync(live.Where(o => o.BookingLineId is not null).Select(o => o.BookingLineId!.Value).Distinct().ToList(), ct);
		foreach (var offer in live)
		{
			if (offer.BookingLineId is { } lineId && lines.TryGetValue(lineId, out var line)
				&& consumptions.FirstOrDefault(c => c.SourceId == lineId && c.ConsumptionDate == offer.OfferDate && c.ResourceId == offer.ResourceId) is { } consumption)
			{
				consumption.KeepOnly(line.BookingPeriod);
				consumption.Reactivate();
			}
			offer.Withdraw();
		}
		var warnings = await SaveAndRepriceAsync(reservation, live.Any(o => o.ExcludesFromPrice), ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.DaysWithdrawn, reservation, new Dictionary<string, object?>
		{
			["days"] = live.Select(o => o.OfferDate).Order().ToList(), ["total"] = reservation.Total,
		}, ct: ct);
		return warnings;
	}

	/// <summary>Salva i giorni ceduti o ritirati e, se cambiano i giorni da pagare, ricalcola il prezzo con il listino di oggi.</summary>
	private async Task<IReadOnlyList<string>> SaveAndRepriceAsync(Reservation reservation, bool reprice, CancellationToken ct)
	{
		await using var tx = await _reservations.BeginTransactionAsync(ct);
		await _reservations.SaveChangesAsync(ct);
		IReadOnlyList<string> warnings = [];
		if (reprice)
		{
			var spots = reservation.Lines.Where(l => l.DeletedAt is null).OrderBy(l => l.StartDate)
				.Select(l => ReservationRequests.InputOf(reservation, l, l.SpotId!.Value, l.StartDate, l.EndDate)).ToList();
			var plan = await _writer.ApplyAsync(reservation, ReservationRequests.StateOf(reservation, spots), ct);
			reservation.Touch(_scope.UserId);
			await _reservations.SaveChangesAsync(ct);
			warnings = plan.Warnings;
		}
		await tx.CommitAsync(ct);
		return warnings;
	}

	private static SharingSettingsDto ToDto(ResaleSettings s)
		=> new(s.IsEnabled, s.MinBookingDays, s.AppliesTo.Contains(ResaleScope.SeasonalOnly), s.AppliesTo.Contains(ResaleScope.AllArticles),
			s.AbsencesAsSharing, s.ManualCreditEnabled, s.ManualCreditUnitsPerDay, s.ManualCreditAmount, s.VoucherCreditEnabled, s.VoucherCreditAmount,
			s.CardTopUpEnabled, s.CardTopUpAmount);
}
