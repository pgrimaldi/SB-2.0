using Module.Identity.Domain.Enums;
using Shared.Contracts;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Periodo (iniziale, rinnovo, migrazione, rettifica) nello storico append-only di un abbonamento.
/// Riga immutabile dopo la chiusura (ClosedAt).
/// </summary>
public sealed class SubscriptionPeriod : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long SubscriptionId { get; private set; }
	public Subscription Subscription { get; private set; } = null!;
	public Guid PropertyId { get; private set; }
	public long LicensePlanId { get; private set; }
	public LicensePlan LicensePlan { get; private set; } = null!;
	public string LicensePlanCode { get; private set; } = null!;
	public SubscriptionTermType TermType { get; private set; }
	public DateTime StartsAt { get; private set; }
	public DateTime? EndsAt { get; private set; }
	public int? DaysGranted { get; private set; }
	public decimal? Amount { get; private set; }
	public string Currency { get; private set; } = null!;
	public SubscriptionPeriodKind PeriodKind { get; private set; }
	public long? OpenedByUserId { get; private set; }
	public Account? OpenedByUser { get; private set; }
	public DateTime? ClosedAt { get; private set; }
	public SubscriptionPeriodCloseReason? CloseReason { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private SubscriptionPeriod() { }

	internal static SubscriptionPeriod Open(
		Subscription subscription, LicensePlan plan, DateTime startsAt, DateTime endsAt,
		decimal? amount, string currency, SubscriptionPeriodKind kind, long? openedByUserId) => new()
	{
		Subscription = subscription,
		PropertyId = subscription.PropertyId,
		LicensePlanId = plan.Id,
		LicensePlan = plan,
		LicensePlanCode = plan.Code,
		TermType = SubscriptionTermType.FixedTerm,
		StartsAt = startsAt,
		EndsAt = endsAt,
		Amount = amount,
		Currency = currency,
		PeriodKind = kind,
		OpenedByUserId = openedByUserId,
	};

	internal void Close(SubscriptionPeriodCloseReason reason, DateTime now)
	{
		if (ClosedAt is not null)
			return;
		ClosedAt = now;
		CloseReason = reason;
	}
}
