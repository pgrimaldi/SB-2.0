using BuildingBlocks.Application.Validation;
using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Dtos;
using Module.Booking.Application.Validators;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Booking;
using Shared.Contracts.Catalog;
using Shared.Contracts.Errors;
using Shared.Contracts.Identity;
using Shared.Contracts.Notification;
using Shared.Contracts.Payment;
using Shared.Localization;
using Shared.Security;

namespace Module.Booking.Application.Services;

/// <summary>Scadenza della richiesta da confermare: la prima fra 48 ore, le 23:00 della vigilia e, per il giorno stesso, un'ora.</summary>
public static class OnlineExpiry
{
	public static DateTime ConfirmationExpiresAt(DateTime nowUtc, DateOnly firstDay)
	{
		var today = DateOnly.FromDateTime(ItalianTime.FromUtc(nowUtc));
		if (firstDay <= today)
			return nowUtc.AddHours(1);
		var eve = ItalianTime.ToUtc(firstDay.AddDays(-1).ToDateTime(new TimeOnly(23, 0)));
		var expires = nowUtc.AddHours(48) < eve ? nowUtc.AddHours(48) : eve;
		// richiesta fatta dopo le 23:00 della vigilia: come per il giorno stesso
		return expires <= nowUtc ? nowUtc.AddHours(1) : expires;
	}
}

/// <summary>
/// Prenotazione dal sito dello stabilimento (anche da cliente registrato), conferma o rifiuto del gestore, pagamento
/// online e scadenze. Valgono le regole online di Catalog: periodi, postazioni, mezza giornata, stagionali, giorno stesso,
/// orizzonte, limiti di soggiorno (nel preventivo online), più quelle di Booking: solo contatti registrati, massimo di
/// postazioni al giorno per cliente (riconosciuto da email e telefono), blacklist, riserve occupate, limiti dei servizi.
/// </summary>
public sealed class OnlineBookingService : IOnlineBookingPayments
{
	public const int PaymentHoldMinutes = 5;
	public const int PaymentStartedMinutes = 10;
	private static readonly IReadOnlySet<string> OnlinePaid = OnlinePaymentMethods.PaidOnline;

	private readonly IReservationRepository _reservations;
	private readonly ReservationWriter _writer;
	private readonly ReservationNumbers _numbers;
	private readonly IBookingCatalog _catalog;
	private readonly ICustomerDirectory _customers;
	private readonly SpotDirectory _spots;
	private readonly BookingScope _scope;
	private readonly BookingAudit _audit;
	private readonly IHumanVerification _human;
	private readonly IRequestContext _request;
	private readonly BookingMessages _messages;
	private readonly IReservationPayments _payments;
	private readonly CustomerViews _views;
	private readonly OnlineReservationValidator _validator;
	private readonly IDiscountRedemption _redemptions;
	private readonly IOnlineCardPayments _cards;
	private readonly IRequestLanguage _language;

	public OnlineBookingService(
		IReservationRepository reservations, ReservationWriter writer, ReservationNumbers numbers, IBookingCatalog catalog, ICustomerDirectory customers,
		SpotDirectory spots, BookingScope scope, BookingAudit audit, IHumanVerification human, IRequestContext request, BookingMessages messages,
		IReservationPayments payments, CustomerViews views, OnlineReservationValidator validator, IDiscountRedemption redemptions,
		IOnlineCardPayments cards, IRequestLanguage language)
	{
		_cards = cards;
		_language = language;
		_redemptions = redemptions;
		_reservations = reservations;
		_writer = writer;
		_numbers = numbers;
		_catalog = catalog;
		_customers = customers;
		_spots = spots;
		_scope = scope;
		_audit = audit;
		_human = human;
		_request = request;
		_messages = messages;
		_payments = payments;
		_views = views;
		_validator = validator;
	}

	private async Task<OnlineBookingRules> RulesAsync(string slug, CancellationToken ct)
	{
		var rules = await _catalog.FindOnlineRulesBySlugAsync(slug, ct) ?? throw DomainException.NotFound(BookingErrors.PropertyNotFound);
		if (!rules.IsOnlineBookable)
			throw DomainException.Unprocessable(BookingErrors.OnlineUnavailable);
		_scope.UseProperty(rules.PropertyId);
		return rules;
	}

	/// <summary>Postazioni libere online: prenotabili, non riservate alle convenzioni, senza prenotazioni né riserve.</summary>
	public async Task<OnlineAvailabilityDto> AvailabilityAsync(string slug, DateOnly from, DateOnly to, string? dayPart, CancellationToken ct = default)
	{
		if (to < from || to.DayNumber - from.DayNumber >= BookingRules.MaxDays)
			throw DomainException.Unprocessable(ValidationErrorCodes.OutOfRange, new Dictionary<string, object?> { ["maxDays"] = BookingRules.MaxDays });
		await RulesAsync(slug, ct);
		var period = SpotKinds.ParsePeriod(dayPart);
		var spots = (await _spots.AllAsync(ct)).Values
			.Where(s => s.IsBookable && !s.IsAgreementOnly && !s.IsArea && SpotKinds.IsExclusive(SpotKinds.ResourceTypeOf(s.SpotType))).ToList();
		var busy = (await _reservations.ListOccupiedAsync(_scope.PropertyId, spots.Select(s => s.Id).ToList(), from, to, ct))
			.Where(o => SpotKinds.Overlaps(o.Period, period)).Select(o => o.SpotId).ToHashSet();
		return new OnlineAvailabilityDto(from, to, spots.Where(s => !busy.Contains(s.Id)).Select(s => s.Id).ToList());
	}

	public async Task<OnlineReservationResultDto> CreateAsync(string slug, OnlineReservationRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var rules = await RulesAsync(slug, ct);
		var accountId = _scope.IsCustomer ? _scope.UserId : null;
		if (accountId is null && !await _human.VerifyAsync(request.HumanToken, _request.ClientIp, ct))
			throw DomainException.Forbidden(BookingErrors.HumanVerificationFailed);
		if (!rules.PaymentMethods.Contains(request.PaymentMethod) || (OnlinePaid.Contains(request.PaymentMethod) && rules.IsOnlinePaymentBlocked))
			throw DomainException.Unprocessable(BookingErrors.OnlinePaymentMethodNotAllowed, new Dictionary<string, object?> { ["methods"] = rules.PaymentMethods });

		var c = request.Customer;
		var hasContacts = !string.IsNullOrWhiteSpace(c.Email) && !string.IsNullOrWhiteSpace(c.Phone);
		if ((rules.MaxBookingsPerDay is not null || rules.ContactsOnly) && !hasContacts)
			throw DomainException.Unprocessable(BookingErrors.OnlineContactsRequired);
		if (rules.ContactsOnly && !await _customers.IsRegisteredContactAsync(rules.PropertyId, c.Email!, c.Phone!, ct))
			throw DomainException.Forbidden(BookingErrors.OnlineContactsOnly);
		if (await _customers.IsBlacklistedAsync(rules.PropertyId, c.Email, c.Phone, ct))
			throw DomainException.Forbidden(BookingErrors.OnlineNotAllowed);

		var all = await _spots.AllAsync(ct);
		foreach (var s in request.Spots)
			if (all.TryGetValue(s.SpotId, out var spot) && spot.IsAgreementOnly)
				throw DomainException.Unprocessable(BookingErrors.SpotNotOnline, new Dictionary<string, object?> { ["spotId"] = s.SpotId, ["label"] = spot.Label });

		var contact = new CustomerContact(c.FullName, c.Email, c.PhoneCountryCode, c.Phone);
		var customerId = accountId is { } account
			? await _customers.FindOrCreateForAccountAsync(rules.PropertyId, account, contact, ct)
			: hasContacts || !string.IsNullOrWhiteSpace(c.Email) || !string.IsNullOrWhiteSpace(c.Phone)
				? await _customers.FindOrCreateAsync(rules.PropertyId, contact, ct)
				: await _customers.CreateAsync(rules.PropertyId, contact, ct);

		// massimo di postazioni al giorno per cliente, solo online
		if (rules.MaxBookingsPerDay is { } max)
		{
			var counts = await _reservations.CountOnlineSpotsPerDayAsync(rules.PropertyId, customerId, request.From, request.To, ct);
			for (var day = request.From; day <= request.To; day = day.AddDays(1))
				if (counts.GetValueOrDefault(day) + request.Spots.Count > max)
					throw DomainException.Unprocessable(BookingErrors.OnlineDailyLimit, new Dictionary<string, object?> { ["max"] = max, ["day"] = day });
		}

		var now = _scope.UtcNow;
		var payOnline = OnlinePaid.Contains(request.PaymentMethod);
		DateTime? confirmBy = rules.RequiresManualConfirmation ? OnlineExpiry.ConfirmationExpiresAt(now, request.From) : null;
		// con la conferma del gestore si incassa dopo: la carta non si addebita subito
		DateTime? payBy = payOnline && confirmBy is null ? now.AddMinutes(PaymentHoldMinutes) : null;
		var reservation = Reservation.CreateOnline(rules.PropertyId, await _numbers.NextNumberAsync(rules.PropertyId, ct), await _numbers.NewCodeAsync(ct),
			customerId, accountId, request.Seasonal, "EUR", confirmBy, payBy, payOnline);
		reservation.SetNote(request.Note, null, accountId, now);

		var save = new SaveReservationRequest(
			null, request.From, request.To, request.DayPart, request.Seasonal,
			request.Spots.Select(s => new ReservationSpotInput(s.SpotId, null, null, s.PriceListId, null, null, null, null, null, s.Additionals)).ToList(),
			request.DiscountCode, null, null, null, null, null, null, false, request.Services ?? [], null, false);
		var plan = await _writer.ApplyAsync(reservation, save, new WriteOptions(Online: true, Hold: confirmBy is not null || payBy is not null, HoldUntil: payBy ?? confirmBy), ct);
		_reservations.Add(reservation);
		await _writer.SaveAsync([(reservation, plan)], ct);

		await _audit.RecordAsync(AuditActionCodes.Booking.ReservationCreated, reservation, new Dictionary<string, object?>
		{
			["channel"] = "online", ["from"] = reservation.StayFrom, ["to"] = reservation.StayTo, ["total"] = reservation.Total,
			["paymentMethod"] = request.PaymentMethod, ["confirmationExpiresAt"] = confirmBy,
		}, ct: ct);
		await _audit.ActivityAsync(confirmBy is not null ? BookingActivityKinds.OnlineAwaitingConfirmation : BookingActivityKinds.OnlineReservationCreated,
			reservation, new Dictionary<string, object?> { ["confirmationExpiresAt"] = confirmBy }, ct);
		if (confirmBy is not null)
			await _messages.SendAsync(TransactionalEmailKind.BookingRequestReceived, reservation, rules.PropertyName, ct: ct);
		else if (payBy is null)
			await _messages.SendAsync(TransactionalEmailKind.BookingConfirmed, reservation, rules.PropertyName, ct: ct);

		var amount = request.PaymentMethod == OnlinePaymentMethods.DepositHalf ? decimal.Round(reservation.Total / 2, 2, MidpointRounding.AwayFromZero) : reservation.Total;
		OnlineCheckout? checkout = null;
		if (payBy is not null && amount > 0)
			checkout = await _cards.StartCheckoutAsync(Checkout(reservation, amount, payBy, c.Email), ct);
		return new OnlineReservationResultDto(await _views.ToDtoAsync(reservation, rules, ct),
			new OnlinePaymentDto(payBy is not null, payBy is null ? 0 : amount, payBy, checkout?.Provider, checkout?.RedirectUrl, checkout?.ClientSecret));
	}

	// ----- Conferma del gestore -----

	public async Task<ReservationDto> ConfirmAsync(Guid id, ReservationService reservations, CancellationToken ct = default)
	{
		var reservation = await LoadPendingAsync(id, ct);
		var now = _scope.UtcNow;
		reservation.ConfirmOnline(_scope.UserId, now);
		foreach (var c in await _reservations.ListAllConsumptionsAsync(reservation, ct))
			if (c.State == ConsumptionState.Held)
				c.Reactivate();
		await _reservations.SaveChangesAsync(ct);
		// pagamento con carta scelto online: dopo la conferma si chiede al cliente (link nell'email di conferma)
		OnlineCheckout? checkout = null;
		if (reservation.PayNow && reservation.BalanceDue > 0)
			checkout = await _cards.RequestPaymentAsync(Checkout(reservation, reservation.BalanceDue, null, await EmailOfAsync(reservation, ct)), ct);
		await _audit.RecordAsync(AuditActionCodes.Booking.OnlineConfirmed, reservation,
			new Dictionary<string, object?> { ["paymentRequested"] = checkout is not null }, ct: ct);
		await _messages.SendAsync(TransactionalEmailKind.BookingConfirmed, reservation, await PropertyNameAsync(reservation, ct),
			paymentUrl: checkout?.RedirectUrl, ct: ct);
		return await reservations.ToDtoAsync(reservation, [], ct);
	}

	public async Task<ReservationDto> RejectAsync(Guid id, ReservationService reservations, CancellationToken ct = default)
	{
		var reservation = await LoadPendingAsync(id, ct);
		await CancelCoreAsync(reservation, CancellationReason.Rejected, _scope.UserId, ct);
		// rifiuto: si restituisce tutto quello che è stato incassato (di solito nulla, la carta si addebita dopo la conferma)
		var refund = reservation.Paid > 0 ? await _payments.RefundAsync(reservation.PropertyId, reservation.PublicId, reservation.Paid, ct) : null;
		await _audit.RecordAsync(AuditActionCodes.Booking.OnlineRejected, reservation, new Dictionary<string, object?>
		{
			["refunded"] = refund?.Refunded ?? 0, ["refundPendingOnline"] = refund?.PendingOnline ?? 0,
		}, ct: ct);
		await _messages.SendAsync(TransactionalEmailKind.BookingRejected, reservation, await PropertyNameAsync(reservation, ct), ct: ct);
		return await reservations.ToDtoAsync(reservation, [], ct);
	}

	private async Task<Reservation> LoadPendingAsync(Guid id, CancellationToken ct)
	{
		var reservation = await _reservations.GetAsync(_scope.PropertyId, id, false, ct) ?? throw DomainException.NotFound(BookingErrors.ReservationNotFound);
		if (!reservation.AwaitsConfirmation)
			throw DomainException.Conflict(BookingErrors.NotAwaitingConfirmation);
		return reservation;
	}

	// ----- Pagamento online (lo chiama Payment) -----

	public async Task PaymentStartedAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default)
	{
		if (await _reservations.GetAsync(propertyId, reservationId, false, ct) is not { AwaitsPayment: true } reservation)
			return;
		var until = _scope.UtcNow.AddMinutes(PaymentStartedMinutes);
		reservation.ExtendPaymentHold(until);
		foreach (var c in await _reservations.ListAllConsumptionsAsync(reservation, ct))
			if (c.State == ConsumptionState.Held)
				c.Hold(until);
		await _reservations.SaveChangesAsync(ct);
	}

	public async Task<OnlinePaymentOutcome> PaymentSucceededAsync(Guid propertyId, Guid reservationId, decimal amount, CancellationToken ct = default)
	{
		var reservation = await _reservations.GetAsync(propertyId, reservationId, false, ct);
		if (reservation is null)
			return new OnlinePaymentOutcome(false, true);
		var consumptions = await _reservations.ListAllConsumptionsAsync(reservation, ct);
		if (reservation.AwaitsPayment)
		{
			reservation.PaymentReceived();
			foreach (var c in consumptions.Where(c => c.State == ConsumptionState.Held))
				if (reservation.AwaitsConfirmation)
					c.Hold(reservation.ConfirmationExpiresAt);
				else
					c.Reactivate();
			await _reservations.SaveChangesAsync(ct);
			await NotifyPaidAsync(reservation, ct);
			return new OnlinePaymentOutcome(true, false);
		}
		// arrivato tardi: se le postazioni sono ancora libere si conferma, altrimenti rimborso totale ed email
		if (reservation.Status == ReservationStatus.Cancelled && reservation.CancellationReason == CancellationReason.PaymentFailed)
		{
			try
			{
				foreach (var c in consumptions.Where(c => c.State == ConsumptionState.Released))
					c.Reactivate();
				reservation.ReviveAfterLatePayment();
				await _reservations.SaveChangesAsync(ct);
				await NotifyPaidAsync(reservation, ct);
				return new OnlinePaymentOutcome(true, false);
			}
			catch (DomainException ex) when (ex.Code == BookingErrors.SpotUnavailable)
			{
				_reservations.DiscardChanges();
				var fresh = await _reservations.GetAsync(propertyId, reservationId, false, ct);
				if (fresh is not null)
					await _messages.SendAsync(TransactionalEmailKind.BookingPaymentRefunded, fresh, await PropertyNameAsync(fresh, ct), amount, ct: ct);
				return new OnlinePaymentOutcome(false, true);
			}
		}
		return new OnlinePaymentOutcome(reservation.IsActive, !reservation.IsActive);
	}

	public async Task PaymentFailedAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default)
	{
		if (await _reservations.GetAsync(propertyId, reservationId, false, ct) is not { AwaitsPayment: true } reservation)
			return;
		await CancelCoreAsync(reservation, CancellationReason.PaymentFailed, null, ct);
	}

	private async Task NotifyPaidAsync(Reservation reservation, CancellationToken ct)
	{
		var kind = reservation.AwaitsConfirmation ? TransactionalEmailKind.BookingRequestReceived : TransactionalEmailKind.BookingConfirmed;
		await _messages.SendAsync(kind, reservation, await PropertyNameAsync(reservation, ct), ct: ct);
	}

	// ----- Scadenze (job) -----

	/// <summary>Richieste non confermate in tempo e pagamenti mai arrivati: annullate, postazioni libere, messaggio al cliente.</summary>
	public async Task<int> ExpireAsync(DateTime nowUtc, CancellationToken ct = default)
	{
		var expired = await _reservations.ListOnlineExpiredAsync(nowUtc, 200, ct);
		foreach (var reservation in expired)
		{
			var payment = reservation.PaymentHoldUntil is { } hold && hold < nowUtc;
			await CancelCoreAsync(reservation, payment ? CancellationReason.PaymentFailed : CancellationReason.ConfirmationExpired, null, ct);
			await _audit.RecordAsync(payment ? AuditActionCodes.Booking.OnlinePaymentExpired : AuditActionCodes.Booking.OnlineExpired, reservation.PropertyId,
				AuditActionCodes.Booking.Targets.Reservation, reservation.PublicId, new Dictionary<string, object?> { ["automatic"] = true },
				reservation.BookingNumber, ct: ct);
			if (!payment)
				await _messages.SendAsync(TransactionalEmailKind.BookingExpired, reservation, await PropertyNameAsync(reservation, ct), ct: ct);
		}
		return expired.Count;
	}

	/// <summary>Annullamento senza cestino: postazioni e servizi liberi; gli incassi online in attesa li rimborsa Payment.</summary>
	public async Task CancelCoreAsync(Reservation reservation, CancellationReason reason, Guid? userId, CancellationToken ct)
	{
		var awaitedPayment = reservation.AwaitsPayment;
		foreach (var c in await _reservations.ListAllConsumptionsAsync(reservation, ct))
			if (c.State != ConsumptionState.Released)
				c.Release();
		reservation.Cancel(reason, userId, _scope.UtcNow);
		await _reservations.SaveChangesAsync(ct);
		if (reservation.Discounts.Select(d => d.DiscountId).FirstOrDefault(d => d is not null) is { } discount)
			await _redemptions.ReleaseAsync(reservation.PropertyId, discount, ct);
		if (awaitedPayment && reason != CancellationReason.PaymentFailed)
			await _cards.CancelCheckoutAsync(reservation.PropertyId, reservation.PublicId, ct);
	}

	private OnlineCheckoutRequest Checkout(Reservation reservation, decimal amount, DateTime? expiresAt, string? email)
		=> new(reservation.PropertyId, reservation.PublicId, reservation.BookingNumber, amount, reservation.Currency, expiresAt, email,
			SupportedLanguages.Code(_language.Language));

	private async Task<string?> EmailOfAsync(Reservation reservation, CancellationToken ct)
		=> reservation.CustomerId is { } id ? (await _customers.ListAsync(reservation.PropertyId, [id], ct)).FirstOrDefault()?.Email : null;

	private async Task<string> PropertyNameAsync(Reservation reservation, CancellationToken ct)
		=> (await _catalog.FindOnlineRulesAsync(reservation.PropertyId, ct))?.PropertyName ?? "";
}
