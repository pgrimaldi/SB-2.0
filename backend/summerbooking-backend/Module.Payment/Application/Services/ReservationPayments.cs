using Module.Payment.Application.Abstractions;
using Module.Payment.Application.Dtos;
using Module.Payment.Domain;
using Module.Payment.Domain.Entities;
using Module.Payment.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Contracts.Events;
using Shared.Contracts.Payment;
using Shared.Infrastructure;
using Shared.Security;
using Shared.Utils;

namespace Module.Payment.Application.Services;

/// <summary>
/// Nucleo minimo di Payment: registrare, annullare e rimborsare gli incassi manuali delle prenotazioni e pubblicarne i
/// totali. Nessuna regola di business oltre a questo. Da fare: limiti per metodo di pagamento, chiusura di cassa,
/// ricevute e scontrini, rimborsi sul circuito delle carte (Stripe), ripartizione dell'IVA per aliquota, riconciliazione
/// notturna dei totali e outbox degli eventi.
/// </summary>
public sealed class ReservationPayments : IReservationPayments
{
	private readonly ILedgerRepository _ledger;
	private readonly ICurrentUser _user;
	private readonly IDateTimeProvider _clock;
	private readonly IIntegrationEventPublisher _events;
	private readonly IAuditTrail _audit;

	public ReservationPayments(ILedgerRepository ledger, ICurrentUser user, IDateTimeProvider clock, IIntegrationEventPublisher events, IAuditTrail audit)
	{
		_ledger = ledger;
		_user = user;
		_clock = clock;
		_events = events;
		_audit = audit;
	}

	public async Task<Guid> RecordAsync(ReservationPaymentInput input, CancellationToken ct = default)
	{
		if (input.Amount <= 0 || decimal.Round(input.Amount, 2) != input.Amount)
			throw DomainException.Unprocessable(PaymentErrors.InvalidAmount);
		if (!Codes.TryParse<PaymentMode>(input.Mode, out var mode))
			throw DomainException.Unprocessable(PaymentErrors.InvalidMode);

		var entry = LedgerEntry.ManualIncome(
			input.PropertyId, input.ReservationId, input.Amount, mode, input.IsDeposit, input.CustomerId, _user.UserId, _clock.UtcNow,
			input.StayFrom, input.StayTo);
		_ledger.Add(entry);
		await _ledger.SaveChangesAsync(ct);
		await RecordAuditAsync(AuditActionCodes.Payment.PaymentRecorded, input.PropertyId, entry.PublicId,
			new Dictionary<string, object?> { ["reservationId"] = input.ReservationId, ["amount"] = input.Amount, ["mode"] = input.Mode, ["deposit"] = input.IsDeposit }, ct);
		await PublishAsync(input.PropertyId, input.ReservationId, ct);
		return entry.PublicId;
	}

	public async Task VoidAsync(Guid propertyId, Guid entryId, CancellationToken ct = default)
	{
		var entry = await _ledger.FindAsync(propertyId, entryId, ct) ?? throw DomainException.NotFound(PaymentErrors.EntryNotFound);
		if (entry.Source != LedgerSource.Manual || entry.ReservationPublicId is not { } reservationId)
			throw DomainException.Conflict(PaymentErrors.EntryNotVoidable);
		_ledger.Void(entry);
		await _ledger.SaveChangesAsync(ct);
		await RecordAuditAsync(AuditActionCodes.Payment.PaymentVoided, propertyId, entry.PublicId,
			new Dictionary<string, object?> { ["reservationId"] = reservationId, ["amount"] = entry.SignedAmount }, ct);
		await PublishAsync(propertyId, reservationId, ct);
	}

	public async Task<ReservationRefundResult> RefundAllAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default)
	{
		var entries = await _ledger.ListByReservationAsync(propertyId, reservationId, ct);
		var now = _clock.UtcNow;
		var customerId = entries.Select(e => e.CustomerPublicId).FirstOrDefault(c => c is not null);
		decimal refunded = 0, pendingOnline = 0;

		foreach (var group in entries.GroupBy(e => (Online: IsOnline(e.Source), e.PaymentMode)))
		{
			var balance = group.Sum(e => e.SignedAmount);
			if (balance <= 0)
				continue;
			if (group.Key.Online)
			{
				// Il rimborso sul circuito della carta non è ancora automatico: resta da fare a mano.
				pendingOnline += balance;
				continue;
			}
			_ledger.Add(LedgerEntry.RefundOf(propertyId, reservationId, balance, group.Key.PaymentMode, LedgerSource.Manual, customerId, _user.UserId, now));
			refunded += balance;
		}

		if (refunded > 0)
		{
			await _ledger.SaveChangesAsync(ct);
			await RecordAuditAsync(AuditActionCodes.Payment.PaymentsRefunded, propertyId, null,
				new Dictionary<string, object?> { ["reservationId"] = reservationId, ["amount"] = refunded, ["pendingOnline"] = pendingOnline }, ct);
			await PublishAsync(propertyId, reservationId, ct);
		}
		return new ReservationRefundResult(refunded, pendingOnline);
	}

	public async Task<ReservationRefundResult> RefundAsync(Guid propertyId, Guid reservationId, decimal amount, CancellationToken ct = default)
	{
		if (amount <= 0)
			return new ReservationRefundResult(0, 0);
		var entries = await _ledger.ListByReservationAsync(propertyId, reservationId, ct);
		var now = _clock.UtcNow;
		var customerId = entries.Select(e => e.CustomerPublicId).FirstOrDefault(c => c is not null);
		decimal refunded = 0, pendingOnline = 0, left = amount;

		foreach (var group in entries.GroupBy(e => (Online: IsOnline(e.Source), e.PaymentMode)).OrderByDescending(g => g.Key.Online))
		{
			var share = Math.Min(left, group.Sum(e => e.SignedAmount));
			if (share <= 0)
				continue;
			left -= share;
			if (group.Key.Online)
			{
				// rimborso sulla carta: lo farà il circuito dei pagamenti online
				pendingOnline += share;
				continue;
			}
			_ledger.Add(LedgerEntry.RefundOf(propertyId, reservationId, share, group.Key.PaymentMode, LedgerSource.Manual, customerId, _user.UserId, now));
			refunded += share;
			if (left <= 0)
				break;
		}

		if (refunded > 0)
		{
			await _ledger.SaveChangesAsync(ct);
			await RecordAuditAsync(AuditActionCodes.Payment.PaymentsRefunded, propertyId, null,
				new Dictionary<string, object?> { ["reservationId"] = reservationId, ["amount"] = refunded, ["pendingOnline"] = pendingOnline, ["requested"] = amount }, ct);
			await PublishAsync(propertyId, reservationId, ct);
		}
		return new ReservationRefundResult(refunded, pendingOnline);
	}

	public async Task TransferAsync(Guid propertyId, Guid fromReservationId, Guid toReservationId, decimal amount, CancellationToken ct = default)
	{
		if (amount <= 0)
			return;
		var groups = (await _ledger.ListByReservationAsync(propertyId, fromReservationId, ct))
			.GroupBy(e => (e.PaymentMode, Deposit: e.EntryType == LedgerEntryType.Deposit))
			.Select(g => (g.Key, Balance: g.Sum(e => e.SignedAmount), Customer: g.Select(e => e.CustomerPublicId).FirstOrDefault(c => c is not null)))
			.Where(g => g.Balance > 0)
			.OrderBy(g => g.Balance)
			.ToList();
		var available = groups.Sum(g => g.Balance);
		if (amount > available)
			throw DomainException.Unprocessable(PaymentErrors.InvalidAmount, new Dictionary<string, object?> { ["available"] = available });

		var now = _clock.UtcNow;
		var left = amount;
		for (var i = 0; i < groups.Count; i++)
		{
			var g = groups[i];
			// in proporzione al saldo di ciascun gruppo; il resto dell'arrotondamento sull'ultimo
			var share = i == groups.Count - 1 ? left : Math.Min(g.Balance, decimal.Round(amount * g.Balance / available, 2, MidpointRounding.AwayFromZero));
			if (share <= 0)
				continue;
			_ledger.Add(LedgerEntry.SplitTransfer(propertyId, fromReservationId, share, g.Key.PaymentMode, g.Key.Deposit, LedgerDirection.Out, g.Customer, _user.UserId, now));
			_ledger.Add(LedgerEntry.SplitTransfer(propertyId, toReservationId, share, g.Key.PaymentMode, g.Key.Deposit, LedgerDirection.In, g.Customer, _user.UserId, now));
			left -= share;
		}
		await _ledger.SaveChangesAsync(ct);
		await RecordAuditAsync(AuditActionCodes.Payment.PaymentsTransferred, propertyId, null,
			new Dictionary<string, object?> { ["fromReservationId"] = fromReservationId, ["toReservationId"] = toReservationId, ["amount"] = amount }, ct);
		await PublishAsync(propertyId, fromReservationId, ct);
		await PublishAsync(propertyId, toReservationId, ct);
	}

	public async Task<ReservationPaymentTotals> GetTotalsAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default)
		=> Totals(reservationId, await _ledger.ListByReservationAsync(propertyId, reservationId, ct));

	/// <summary>Movimenti di una prenotazione dello stabilimento.</summary>
	public async Task<IReadOnlyList<LedgerEntryDto>> ListAsync(Guid propertyId, Guid reservationId, CancellationToken ct = default)
		=> (await _ledger.ListByReservationAsync(propertyId, reservationId, ct)).Select(LedgerEntryDto.FromEntity).ToList();

	private static bool IsOnline(LedgerSource source) => source is LedgerSource.OnlineGateway or LedgerSource.PaymentLink;

	private static ReservationPaymentTotals Totals(Guid reservationId, IReadOnlyList<LedgerEntry> entries)
	{
		var paid = entries.Sum(e => e.SignedAmount);
		var deposit = entries.Where(e => e.EntryType == LedgerEntryType.Deposit).Sum(e => e.SignedAmount);
		var prevalent = entries.Where(e => e.Direction == LedgerDirection.In && e.PaymentMode is not null)
			.GroupBy(e => e.PaymentMode!.Value)
			.OrderByDescending(g => g.Sum(e => e.SignedAmount))
			.Select(g => (PaymentMode?)g.Key)
			.FirstOrDefault();
		return new ReservationPaymentTotals(reservationId, paid, deposit, prevalent is { } m ? Codes.Of(m) : null);
	}

	private async Task PublishAsync(Guid propertyId, Guid reservationId, CancellationToken ct)
	{
		var totals = Totals(reservationId, await _ledger.ListByReservationAsync(propertyId, reservationId, ct));
		await _events.PublishAsync(new ReservationPaymentsChanged(
			Guid.CreateVersion7(), _clock.UtcNow, propertyId, reservationId, totals.Paid, totals.Deposit, totals.PrevalentMode), ct);
	}

	private Task RecordAuditAsync(string action, Guid propertyId, Guid? targetId, IReadOnlyDictionary<string, object?> metadata, CancellationToken ct)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Operator, AuditActionCodes.Payment.Module, action, PropertyId: propertyId,
			TargetType: AuditActionCodes.Payment.Targets.LedgerEntry, TargetId: targetId, Metadata: metadata), ct);
}
