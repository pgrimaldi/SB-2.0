using Module.Identity.Domain.Enums;
using Shared.Contracts;
using Shared.Contracts.Errors;

namespace Module.Identity.Domain.Entities;

/// <summary>
/// Abbonamento corrente alla piattaforma per uno stabilimento (property); una riga per property.
/// I nuovi abbonamenti sono sempre <see cref="SubscriptionTermType.FixedTerm"/>: la sospensione non sposta le date.
/// </summary>
public sealed class Subscription : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public long? OwnerUserId { get; private set; }
	public Account? OwnerUser { get; private set; }
	public long LicensePlanId { get; private set; }
	public LicensePlan LicensePlan { get; private set; } = null!;
	public SubscriptionTermType TermType { get; private set; }
	public SubscriptionStatus Status { get; private set; }
	public DateTime StartsAt { get; private set; }
	public DateTime EndsAt { get; private set; }
	public DateTime? SuspendedAt { get; private set; }
	public DateTime? ReactivatedAt { get; private set; }
	public int? RemainingDaysBalance { get; private set; }
	public ICollection<SubscriptionPeriod> Periods { get; private set; } = new List<SubscriptionPeriod>();
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private Subscription() { }

	public static Subscription Create(
		Guid propertyId, long? ownerUserId, LicensePlan plan, DateTime startsAt, DateTime endsAt,
		decimal? amount, string currency, long? openedByUserId)
	{
		EnsureDates(startsAt, endsAt);

		var subscription = new Subscription
		{
			PropertyId = propertyId,
			OwnerUserId = ownerUserId,
			LicensePlanId = plan.Id,
			LicensePlan = plan,
			TermType = SubscriptionTermType.FixedTerm,
			Status = SubscriptionStatus.Active,
			StartsAt = startsAt,
			EndsAt = endsAt,
		};
		subscription.Periods.Add(SubscriptionPeriod.Open(subscription, plan, startsAt, endsAt, amount, currency, SubscriptionPeriodKind.Initial, openedByUserId));
		return subscription;
	}

	/// <summary>Nuovo periodo contrattuale: chiude quello aperto come rinnovato e riattiva l'abbonamento.</summary>
	public void Renew(LicensePlan plan, DateTime startsAt, DateTime endsAt, decimal? amount, string currency, long? openedByUserId, DateTime now)
	{
		EnsureDates(startsAt, endsAt);
		if (Status == SubscriptionStatus.Cancelled)
			throw DomainException.Unprocessable(IdentityErrors.SubscriptionInvalidTransition);

		CloseOpenPeriod(SubscriptionPeriodCloseReason.Renewed, now);
		LicensePlanId = plan.Id;
		LicensePlan = plan;
		TermType = SubscriptionTermType.FixedTerm;
		StartsAt = startsAt;
		EndsAt = endsAt;
		Status = SubscriptionStatus.Active;
		SuspendedAt = null;
		Periods.Add(SubscriptionPeriod.Open(this, plan, startsAt, endsAt, amount, currency, SubscriptionPeriodKind.Renewal, openedByUserId));
	}

	public void Suspend(DateTime now)
	{
		if (Status != SubscriptionStatus.Active)
			throw DomainException.Unprocessable(IdentityErrors.SubscriptionInvalidTransition);
		Status = SubscriptionStatus.Suspended;
		SuspendedAt = now;
	}

	public void Reactivate(DateTime now)
	{
		if (Status != SubscriptionStatus.Suspended)
			throw DomainException.Unprocessable(IdentityErrors.SubscriptionInvalidTransition);
		Status = EndsAt > now ? SubscriptionStatus.Active : SubscriptionStatus.Expired;
		ReactivatedAt = now;
	}

	public void Cancel(DateTime now)
	{
		if (Status == SubscriptionStatus.Cancelled)
			throw DomainException.Unprocessable(IdentityErrors.SubscriptionInvalidTransition);
		Status = SubscriptionStatus.Cancelled;
		CloseOpenPeriod(SubscriptionPeriodCloseReason.Cancelled, now);
	}

	/// <summary>Usato dal batch di scadenza, oggi disattivato: PROMEMORIA-85-1.</summary>
	public void Expire(DateTime now)
	{
		if (Status != SubscriptionStatus.Active || EndsAt > now)
			return;
		Status = SubscriptionStatus.Expired;
		CloseOpenPeriod(SubscriptionPeriodCloseReason.Expired, now);
	}

	private void CloseOpenPeriod(SubscriptionPeriodCloseReason reason, DateTime now)
	{
		foreach (var period in Periods.Where(p => p.ClosedAt is null))
			period.Close(reason, now);
	}

	private static void EnsureDates(DateTime startsAt, DateTime endsAt)
	{
		if (endsAt <= startsAt)
			throw new ValidationFailedException("endsAt", ValidationErrorCodes.EndBeforeStart);
	}
}
