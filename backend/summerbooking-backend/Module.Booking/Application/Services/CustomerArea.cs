using System.Globalization;
using Module.Booking.Application.Abstractions;
using Module.Booking.Application.Dtos;
using Module.Booking.Domain;
using Module.Booking.Domain.Entities;
using Module.Booking.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Catalog;
using Shared.Contracts.Errors;
using Shared.Contracts.Identity;
using Shared.Contracts.Notification;
using Shared.Contracts.Payment;
using Shared.Localization;
using Shared.Utils;

namespace Module.Booking.Application.Services;

/// <summary>
/// Messaggi al cliente delle prenotazioni online (conferma, richiesta ricevuta, rifiuto, scadenza, annullamento, rimborso
/// del pagamento tardivo), sempre con il codice prenotazione. Senza email non parte nulla (il cliente non riceve messaggi).
/// </summary>
public sealed class BookingMessages
{
	private readonly ITransactionalEmailSender _email;
	private readonly ICustomerDirectory _customers;
	private readonly IRequestLanguage _language;

	public BookingMessages(ITransactionalEmailSender email, ICustomerDirectory customers, IRequestLanguage language)
	{
		_email = email;
		_customers = customers;
		_language = language;
	}

	public async Task SendAsync(
		TransactionalEmailKind kind, Reservation reservation, string propertyName, decimal? amount = null, string? paymentUrl = null, CancellationToken ct = default)
	{
		if (reservation.CustomerId is not { } customerId)
			return;
		var card = (await _customers.ListAsync(reservation.PropertyId, [customerId], ct)).FirstOrDefault();
		if (string.IsNullOrWhiteSpace(card?.Email))
			return;
		var it = CultureInfo.GetCultureInfo("it-IT");
		var values = new Dictionary<string, string>
		{
			[EmailPlaceholders.PropertyName] = propertyName,
			[EmailPlaceholders.BookingNumber] = reservation.BookingNumber,
			[EmailPlaceholders.ReservationCode] = reservation.ReservationCode,
			[EmailPlaceholders.Dates] = reservation.StayFrom == reservation.StayTo
				? $"{reservation.StayFrom:dd/MM/yyyy}" : $"{reservation.StayFrom:dd/MM/yyyy} - {reservation.StayTo:dd/MM/yyyy}",
			[EmailPlaceholders.Amount] = (amount ?? reservation.Total).ToString("0.00", it),
			[EmailPlaceholders.ExpiresAt] = reservation.ConfirmationExpiresAt is { } e ? ItalianTime.FromUtc(e).ToString("dd/MM/yyyy HH:mm", it) : "",
		};
		if (!string.IsNullOrEmpty(paymentUrl))
			values[EmailPlaceholders.PaymentUrl] = paymentUrl;
		await _email.SendAsync(new TransactionalEmail(kind, card.Email!, SupportedLanguages.Code(_language.Language), values), ct);
	}
}

/// <summary>La prenotazione vista dal cliente: niente note interne, operatori, scontrini.</summary>
public sealed class CustomerViews
{
	private readonly ServiceWriter _services;
	private readonly BookingScope _scope;

	public CustomerViews(ServiceWriter services, BookingScope scope)
	{
		_services = services;
		_scope = scope;
	}

	public async Task<CustomerReservationDto> ToDtoAsync(Reservation r, OnlineBookingRules? rules, CancellationToken ct)
	{
		var catalog = r.ServiceLines.Count == 0 ? new Dictionary<Guid, BookableService>() : await _services.ServicesAsync(r.PropertyId, ct);
		var deadline = CustomerCancellation.Deadline(r, rules);
		return new CustomerReservationDto(
			r.PublicId, r.PropertyId, rules?.PropertyName ?? "", r.BookingNumber, r.ReservationCode, Codes.Of(r.Status), Codes.Of(r.OnlineConfirmationState),
			r.CancellationReason is { } reason ? Codes.Of(reason) : null, r.StayFrom, r.StayTo,
			r.Lines.Where(l => l.DeletedAt is null).OrderBy(l => l.StartDate).Select(l => new CustomerLineDto(
				l.PublicId, l.SpotLabelSnapshot, l.SpotTypeSnapshot, l.StartDate, l.EndDate, SpotKinds.CodeOf(l.BookingPeriod), l.PackageNameSnapshot)).ToList(),
			r.ServiceLines.Where(s => s.DeletedAt is null).Select(s =>
			{
				var slot = s.TimeSlots.FirstOrDefault();
				return new ReservationServiceDto(s.PublicId, s.SaleItemId, s.SaleItemId is { } id && catalog.ContainsKey(id) ? Codes.Of(s.Kind) : QuoteLineKinds.AutomaticPrice,
					s.NameSnapshot, s.VariantLabel, (short)s.Quantity, s.ServiceDate, slot?.SlotStart, slot?.SlotEnd, slot?.TimeSlotId, s.LineTotal, s.BookingLine?.SpotId);
			}).ToList(),
			r.Total, r.Paid, r.BalanceDue, r.ConfirmationExpiresAt, r.PaymentHoldUntil,
			r.IsActive && deadline is { } d && d > _scope.UtcNow ? d : null,
			r.IsActive && rules is { AllowsSharing: true }, r.CreatedAt);
	}
}

/// <summary>
/// Regole di cancellazione del cliente: si annulla fino al preavviso dello stabilimento; con meno ore di anticipo di
/// una trattenuta si trattiene la sua percentuale del pagato (la più alta fra quelle superate). I costi di servizio non si
/// rimborsano mai (li conosce Payment).
/// </summary>
public static class CustomerCancellation
{
	/// <summary>Inizio del primo giorno (ora italiana, apertura convenzionale alle 00:00) meno il preavviso; null = non annullabile.</summary>
	public static DateTime? Deadline(Reservation r, OnlineBookingRules? rules)
	{
		if (rules is not { CancellationAllowed: true } || r.StayFrom is not { } first)
			return null;
		return ItalianTime.ToUtc(first.ToDateTime(TimeOnly.MinValue)).AddHours(-(rules.CancellationNoticeHours ?? 0));
	}

	public static short PenaltyPercent(Reservation r, OnlineBookingRules rules, DateTime nowUtc)
	{
		if (r.StayFrom is not { } first)
			return 0;
		var hoursBefore = (ItalianTime.ToUtc(first.ToDateTime(TimeOnly.MinValue)) - nowUtc).TotalHours;
		return rules.CancellationPenalties.Where(p => hoursBefore < p.NoticeHours).Select(p => p.Percent).DefaultIfEmpty((short)0).Max();
	}
}

/// <summary>
/// Area cliente: le prenotazioni dell'account in tutti gli stabilimenti (quelle con la sua email, anche fatte al banco),
/// annullamento con le regole dello stabilimento e sharing se lo stabilimento lo permette. Niente modifiche.
/// </summary>
public sealed class CustomerAreaService
{
	private readonly IReservationRepository _reservations;
	private readonly ICustomerDirectory _customers;
	private readonly IBookingCatalog _catalog;
	private readonly OnlineBookingService _online;
	private readonly SharingService _sharing;
	private readonly CustomerViews _views;
	private readonly BookingMessages _messages;
	private readonly BookingAudit _audit;
	private readonly BookingScope _scope;
	private readonly IReservationPayments _payments;

	public CustomerAreaService(
		IReservationRepository reservations, ICustomerDirectory customers, IBookingCatalog catalog, OnlineBookingService online, SharingService sharing,
		CustomerViews views, BookingMessages messages, BookingAudit audit, BookingScope scope, IReservationPayments payments)
	{
		_payments = payments;
		_reservations = reservations;
		_customers = customers;
		_catalog = catalog;
		_online = online;
		_sharing = sharing;
		_views = views;
		_messages = messages;
		_audit = audit;
		_scope = scope;
	}

	private Guid Account => _scope.IsCustomer && _scope.UserId is { } id ? id : throw DomainException.Forbidden(BookingErrors.CustomerAreaOnly);

	public async Task<IReadOnlyList<CustomerReservationDto>> ListAsync(CancellationToken ct = default)
	{
		var mine = await _customers.ListForAccountAsync(Account, ct);
		var reservations = await _reservations.ListByCustomersAsync(mine.Select(m => m.CustomerId).ToList(), ct);
		var rules = new Dictionary<Guid, OnlineBookingRules?>();
		var result = new List<CustomerReservationDto>();
		foreach (var r in reservations.Where(r => mine.Any(m => m.PropertyId == r.PropertyId && m.CustomerId == r.CustomerId)))
		{
			if (!rules.TryGetValue(r.PropertyId, out var rule))
				rules[r.PropertyId] = rule = await _catalog.FindOnlineRulesAsync(r.PropertyId, ct);
			result.Add(await _views.ToDtoAsync(r, rule, ct));
		}
		return result;
	}

	public async Task<CustomerReservationDto> GetAsync(Guid id, CancellationToken ct = default)
	{
		var (reservation, rules) = await LoadAsync(id, ct);
		return await _views.ToDtoAsync(reservation, rules, ct);
	}

	/// <summary>Annullamento del cliente: entro il preavviso, con la trattenuta se si è sotto una delle sue soglie.</summary>
	public async Task<CustomerCancellationDto> CancelAsync(Guid id, CancellationToken ct = default)
	{
		var (reservation, rules) = await LoadAsync(id, ct);
		if (!reservation.IsActive)
			throw DomainException.Conflict(BookingErrors.ReservationCancelled);
		if (rules is null || CustomerCancellation.Deadline(reservation, rules) is not { } deadline || deadline <= _scope.UtcNow)
			throw DomainException.Unprocessable(BookingErrors.CancellationNotAllowed);
		var percent = CustomerCancellation.PenaltyPercent(reservation, rules, _scope.UtcNow);
		var paid = Math.Max(0, reservation.Paid);
		var penalty = decimal.Round(paid * percent / 100m, 2, MidpointRounding.AwayFromZero);
		_scope.UseProperty(reservation.PropertyId);
		await _online.CancelCoreAsync(reservation, CancellationReason.Customer, Account, ct);
		var refund = paid - penalty > 0
			? await _payments.RefundAsync(reservation.PropertyId, reservation.PublicId, paid - penalty, ct)
			: new ReservationRefundResult(0, 0);
		await _audit.RecordAsync(AuditActionCodes.Booking.CustomerCancelled, reservation, new Dictionary<string, object?>
		{
			["paid"] = paid, ["penaltyPercent"] = percent, ["refundDue"] = paid - penalty,
			["refunded"] = refund.Refunded, ["refundPendingOnline"] = refund.PendingOnline,
		}, ct: ct);
		await _audit.ActivityAsync(Shared.Contracts.Booking.BookingActivityKinds.ReservationCancelledByCustomer, reservation,
			new Dictionary<string, object?> { ["refundDue"] = paid - penalty }, ct);
		await _messages.SendAsync(TransactionalEmailKind.BookingCancelled, reservation, rules.PropertyName, paid - penalty, ct: ct);
		return new CustomerCancellationDto(await _views.ToDtoAsync(reservation, rules, ct), paid, percent, penalty, paid - penalty, refund.Refunded, refund.PendingOnline);
	}

	/// <summary>Sharing dall'area cliente: come dal gestionale, se lo stabilimento lo permette online.</summary>
	public async Task<CustomerReservationDto> ShareAsync(Guid id, ReleaseDaysRequest request, CancellationToken ct = default)
	{
		var (reservation, rules) = await LoadAsync(id, ct);
		if (rules is not { AllowsSharing: true } || request.Kind != ReleaseKinds.Sharing)
			throw DomainException.Unprocessable(BookingErrors.SharingDisabled);
		_scope.UseProperty(reservation.PropertyId);
		await _sharing.ReleaseAsync(reservation.PublicId, request with { RecalculatePrice = false }, ct);
		var fresh = await _reservations.GetAsync(reservation.PropertyId, reservation.PublicId, false, ct) ?? reservation;
		return await _views.ToDtoAsync(fresh, rules, ct);
	}

	private async Task<(Reservation Reservation, OnlineBookingRules? Rules)> LoadAsync(Guid id, CancellationToken ct)
	{
		var mine = await _customers.ListForAccountAsync(Account, ct);
		foreach (var propertyId in mine.Select(m => m.PropertyId).Distinct())
			if (await _reservations.GetAsync(propertyId, id, false, ct) is { } r && mine.Any(m => m.PropertyId == r.PropertyId && m.CustomerId == r.CustomerId))
				return (r, await _catalog.FindOnlineRulesAsync(r.PropertyId, ct));
		throw DomainException.NotFound(BookingErrors.ReservationNotFound);
	}
}
