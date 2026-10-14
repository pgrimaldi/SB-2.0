using BuildingBlocks.Application.Validation;
using FluentValidation;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Validators;
using Module.Identity.Domain;
using Module.Identity.Domain.Entities;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Contracts.Events;
using Shared.Contracts.Identity;
using Shared.Infrastructure;
using Shared.Localization;

namespace Module.Identity.Application.Services;

/// <summary>
/// Abbonamenti degli stabilimenti, gestiti dal systemadmin. Dopo la migrazione la tabella è vuota:
/// queste API servono a valorizzarla prima di riattivare i controlli (PROMEMORIA-85-1).
/// </summary>
public sealed class SubscriptionService
{
	private readonly ISubscriptionRepository _subscriptions;
	private readonly ILicensePlanRepository _plans;
	private readonly IAccountRepository _accounts;
	private readonly CurrentAccount _current;
	private readonly IUnitOfWork _uow;
	private readonly IIntegrationEventPublisher _events;
	private readonly IAuditTrail _audit;
	private readonly IDateTimeProvider _clock;
	private readonly SetSubscriptionRequestValidator _validator;

	public SubscriptionService(
		ISubscriptionRepository subscriptions, ILicensePlanRepository plans, IAccountRepository accounts, CurrentAccount current,
		IUnitOfWork uow, IIntegrationEventPublisher events, IAuditTrail audit, IDateTimeProvider clock,
		SetSubscriptionRequestValidator validator)
	{
		_subscriptions = subscriptions;
		_plans = plans;
		_accounts = accounts;
		_current = current;
		_uow = uow;
		_events = events;
		_audit = audit;
		_clock = clock;
		_validator = validator;
	}

	public async Task<SubscriptionDto> GetAsync(Guid propertyId, CancellationToken ct = default)
		=> SubscriptionDto.FromEntity(await LoadAsync(propertyId, ct));

	public async Task<IReadOnlyList<SubscriptionPeriodDto>> ListPeriodsAsync(Guid propertyId, CancellationToken ct = default)
		=> (await LoadAsync(propertyId, ct)).Periods.OrderByDescending(p => p.StartsAt).Select(SubscriptionPeriodDto.FromEntity).ToList();

	/// <summary>Crea l'abbonamento o apre un nuovo periodo contrattuale (date esplicite, mai calcolate dal client).</summary>
	public async Task<SubscriptionDto> SetAsync(Guid propertyId, SetSubscriptionRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var plan = await _plans.GetByCodeAsync(request.LicensePlanCode.Trim().ToLowerInvariant(), ct)
			?? throw DomainException.NotFound(IdentityErrors.LicensePlanNotFound);
		var admin = await _current.GetAsync(ct);
		var currency = request.Currency ?? CurrencyCodes.Default;
		var startsAt = DateTime.SpecifyKind(request.StartsAt, DateTimeKind.Utc);
		var endsAt = DateTime.SpecifyKind(request.EndsAt, DateTimeKind.Utc);

		var subscription = await _subscriptions.GetByPropertyAsync(propertyId, ct);
		if (subscription is null)
		{
			var owner = await _accounts.FindOwnerAsync(propertyId, ct);
			subscription = Subscription.Create(propertyId, owner?.Id, plan, startsAt, endsAt, request.Amount, currency, admin.Id);
			_subscriptions.Add(subscription);
		}
		else
		{
			subscription.Renew(plan, startsAt, endsAt, request.Amount, currency, admin.Id, _clock.UtcNow);
		}

		return await SaveAndNotifyAsync(subscription, "set", ct);
	}

	public async Task<SubscriptionDto> SuspendAsync(Guid propertyId, CancellationToken ct = default)
	{
		var subscription = await LoadAsync(propertyId, ct);
		subscription.Suspend(_clock.UtcNow);
		return await SaveAndNotifyAsync(subscription, "suspend", ct);
	}

	public async Task<SubscriptionDto> ReactivateAsync(Guid propertyId, CancellationToken ct = default)
	{
		var subscription = await LoadAsync(propertyId, ct);
		subscription.Reactivate(_clock.UtcNow);
		return await SaveAndNotifyAsync(subscription, "reactivate", ct);
	}

	public async Task<SubscriptionDto> CancelAsync(Guid propertyId, CancellationToken ct = default)
	{
		var subscription = await LoadAsync(propertyId, ct);
		subscription.Cancel(_clock.UtcNow);
		return await SaveAndNotifyAsync(subscription, "cancel", ct);
	}

	private async Task<SubscriptionDto> SaveAndNotifyAsync(Subscription subscription, string operation, CancellationToken ct)
	{
		await _uow.SaveChangesAsync(ct);
		var dto = SubscriptionDto.FromEntity(subscription);

		// PROMEMORIA-85-5: nessun gestore in Catalog per ora.
		await _events.PublishAsync(new PropertySubscriptionChanged(
			Guid.CreateVersion7(), _clock.UtcNow, subscription.PropertyId, dto.Status, dto.LicensePlanCode, dto.EndsAt), ct);
		await _audit.RecordAsync(new AuditEntry(
			AuditCategory.Platform, IdentityAudit.Module, IdentityAudit.SubscriptionChanged, PropertyId: subscription.PropertyId,
			Metadata: new Dictionary<string, object?> { ["operation"] = operation, ["status"] = dto.Status, ["plan"] = dto.LicensePlanCode, ["ends_at"] = dto.EndsAt }), ct);
		return dto;
	}

	private async Task<Subscription> LoadAsync(Guid propertyId, CancellationToken ct)
		=> await _subscriptions.GetByPropertyAsync(propertyId, ct) ?? throw DomainException.NotFound(IdentityErrors.SubscriptionNotFound);
}
