using Shared.Contracts.Events;

namespace Shared.Contracts.Audit;

public enum AuditCategory { Operator = 0, Security = 1, Platform = 2 }

public enum AuditOutcome { Succeeded = 0, Failed = 1 }

public enum AuditActorKind { User = 0, System = 1, ExternalClient = 2, Anonymous = 3 }

/// <summary>
/// Voce del registro trasversale "chi ha fatto cosa". La consuma Module.Audit;
/// le etichette sono fotografate al momento dell'azione.
/// </summary>
public sealed record AuditEntryRecorded(
	Guid EventId,
	DateTime OccurredAt,
	AuditCategory Category,
	string SourceModule,
	string ActionCode,
	AuditOutcome Outcome,
	Guid? PropertyId,
	AuditActorKind ActorKind,
	Guid? ActorUserId,
	string? ActorNameSnapshot,
	string? ActorRoleSnapshot,
	Guid? ImpersonatorUserId,
	string? TargetType,
	Guid? TargetId,
	string? BookingNumberSnapshot,
	string? SpotLabelSnapshot,
	IReadOnlyDictionary<string, object?> Metadata,
	string? ClientIp,
	string? ClientUserAgent,
	string? TraceId) : IIntegrationEvent;
