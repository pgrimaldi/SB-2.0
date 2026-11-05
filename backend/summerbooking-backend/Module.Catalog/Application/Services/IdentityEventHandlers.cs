using Module.Catalog.Application.Abstractions;
using Shared.Contracts.Events;
using Shared.Contracts.Identity;

namespace Module.Catalog.Application.Services;

/// <summary>Titolare dello stabilimento assegnato in Identity.</summary>
public sealed class PropertyOwnerAssignedHandler : IIntegrationEventHandler<PropertyOwnerAssigned>
{
	private readonly IPlatformRepository _repository;
	private readonly IUnitOfWork _unitOfWork;

	public PropertyOwnerAssignedHandler(IPlatformRepository repository, IUnitOfWork unitOfWork)
	{
		_repository = repository;
		_unitOfWork = unitOfWork;
	}

	public async Task HandleAsync(PropertyOwnerAssigned @event, CancellationToken ct = default)
	{
		if (await _repository.FindPropertyAsync(@event.PropertyId, ct) is not { } row)
			return;
		row.Property.AssignOwner(@event.OwnerUserId);
		await _unitOfWork.SaveChangesAsync(ct);
	}
}

/// <summary>Il systemadmin abilita o disabilita la prenotazione online (in Identity): disabilitata = vendite sospese.</summary>
public sealed class PropertyOnlineReservationSetHandler : IIntegrationEventHandler<PropertyOnlineReservationSet>
{
	private readonly IPlatformRepository _repository;
	private readonly IUnitOfWork _unitOfWork;

	public PropertyOnlineReservationSetHandler(IPlatformRepository repository, IUnitOfWork unitOfWork)
	{
		_repository = repository;
		_unitOfWork = unitOfWork;
	}

	public async Task HandleAsync(PropertyOnlineReservationSet @event, CancellationToken ct = default)
	{
		if (await _repository.FindPropertyAsync(@event.PropertyId, ct) is not { } row)
			return;
		row.Property.SetOnlineSalesSuspended(!@event.Enabled);
		await _unitOfWork.SaveChangesAsync(ct);
	}
}

/// <summary>Abbonamento al gestionale: solo «active» lascia le vendite online.</summary>
public sealed class PropertySubscriptionChangedHandler : IIntegrationEventHandler<PropertySubscriptionChanged>
{
	private const string ActiveStatus = "active";

	private readonly IPlatformRepository _repository;
	private readonly IUnitOfWork _unitOfWork;

	public PropertySubscriptionChangedHandler(IPlatformRepository repository, IUnitOfWork unitOfWork)
	{
		_repository = repository;
		_unitOfWork = unitOfWork;
	}

	public async Task HandleAsync(PropertySubscriptionChanged @event, CancellationToken ct = default)
	{
		if (await _repository.FindPropertyAsync(@event.PropertyId, ct) is not { } row)
			return;
		row.Property.SetSubscriptionActive(string.Equals(@event.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase), DateOnly.FromDateTime(@event.OccurredAt));
		await _unitOfWork.SaveChangesAsync(ct);
	}
}

/// <summary>«Mi piace» aggiunto o tolto da un cliente.</summary>
public sealed class BathhouseLikeChangedHandler : IIntegrationEventHandler<BathhouseLikeChanged>
{
	private readonly IPlatformRepository _repository;
	private readonly IUnitOfWork _unitOfWork;

	public BathhouseLikeChangedHandler(IPlatformRepository repository, IUnitOfWork unitOfWork)
	{
		_repository = repository;
		_unitOfWork = unitOfWork;
	}

	public async Task HandleAsync(BathhouseLikeChanged @event, CancellationToken ct = default)
	{
		if (await _repository.FindBathhouseByPublicIdAsync(@event.BathhouseId, ct) is not { } bathhouse)
			return;
		bathhouse.ChangeLikes(@event.Liked ? 1 : -1);
		await _unitOfWork.SaveChangesAsync(ct);
	}
}
