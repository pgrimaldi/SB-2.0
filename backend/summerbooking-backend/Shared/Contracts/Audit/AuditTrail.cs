using Shared.Contracts.Events;
using Shared.Contracts.Identity;
using Shared.Infrastructure;
using Shared.Security;

namespace Shared.Contracts.Audit;

/// <summary>
/// Completa la voce con attore e contesto della richiesta e la pubblica come <see cref="AuditEntryRecorded"/>.
/// Il nome dell'operatore, se il modulo non lo passa, si fotografa qui dall'account.
/// </summary>
public sealed class AuditTrail : IAuditTrail
{
	private readonly IIntegrationEventPublisher _publisher;
	private readonly ICurrentUser _currentUser;
	private readonly IRequestContext _request;
	private readonly IDateTimeProvider _clock;
	private readonly IUserDirectory _users;

	public AuditTrail(
		IIntegrationEventPublisher publisher, ICurrentUser currentUser, IRequestContext request, IDateTimeProvider clock, IUserDirectory users)
	{
		_publisher = publisher;
		_currentUser = currentUser;
		_request = request;
		_clock = clock;
		_users = users;
	}

	public async Task RecordAsync(AuditEntry entry, CancellationToken ct = default)
	{
		var actorUserId = entry.ActorUserId ?? _currentUser.UserId;
		var actorKind = entry.ActorKind ?? (actorUserId is null ? AuditActorKind.Anonymous : AuditActorKind.User);
		var actorRole = entry.ActorRoleSnapshot ?? (entry.ActorUserId is null ? _currentUser.Role : null);
		var actorName = entry.ActorNameSnapshot;
		if (actorName is null && actorKind == AuditActorKind.User && actorUserId is { } userId)
			actorName = (await _users.FindAsync(userId, ct))?.DisplayName;

		var @event = new AuditEntryRecorded(
			Guid.CreateVersion7(),
			_clock.UtcNow,
			entry.Category,
			entry.SourceModule,
			entry.ActionCode,
			entry.Outcome,
			entry.PropertyId ?? _currentUser.PropertyId,
			actorKind,
			actorUserId,
			actorName,
			actorRole,
			_currentUser.ImpersonatorUserId,
			entry.TargetType,
			entry.TargetId,
			entry.BookingNumberSnapshot,
			entry.SpotLabelSnapshot,
			entry.Metadata ?? new Dictionary<string, object?>(),
			_request.ClientIp,
			_request.UserAgent,
			_request.TraceId);

		await _publisher.PublishAsync(@event, ct);
	}
}
