using System.Net;
using Module.Audit.Domain.Enums;

namespace Module.Audit.Domain.Entities;

/// <summary>
/// Voce del registro trasversale "chi ha fatto cosa". Append-only: nessuna
/// modifica dopo l'inserimento, la sola rimozione è l'hard-delete della retention.
/// </summary>
/// <remarks>
/// Non implementa IAuditable né ISoftDeletable: created_at/updated_at/deleted_at esistono a schema per
/// uniformità ma non passano dagli interceptor (stesso pattern di privacy_compliance_audit_logs).
/// </remarks>
public sealed class AuditTrailEntry
{
	public long Id { get; private set; }
	public Guid SourceEventId { get; private set; }
	public DateTime OccurredAt { get; private set; }
	public DateTime RecordedAt { get; private set; }
	public EntryCategory Category { get; private set; }
	public SourceModule SourceModule { get; private set; }
	public string ActionCode { get; private set; } = null!;
	public EntryOutcome Outcome { get; private set; }
	public Guid? PropertyId { get; private set; }
	public ActorKind ActorKind { get; private set; }
	public Guid? ActorUserId { get; private set; }
	public string? ActorNameSnapshot { get; private set; }
	public string? ActorRoleSnapshot { get; private set; }
	public Guid? ImpersonatorUserId { get; private set; }
	public string? TargetType { get; private set; }
	public Guid? TargetId { get; private set; }
	public string? BookingNumberSnapshot { get; private set; }
	public string? SpotLabelSnapshot { get; private set; }
	public string Metadata { get; private set; } = null!;
	public IPAddress? ClientIp { get; private set; }
	public string? ClientUserAgent { get; private set; }
	public string? TraceId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private AuditTrailEntry() { }

	/// <summary>Voce da un evento ricevuto; i testi oltre la lunghezza della colonna si troncano invece di perdere la voce.</summary>
	public static AuditTrailEntry Record(
		Guid sourceEventId, DateTime occurredAt, EntryCategory category, SourceModule sourceModule, string actionCode,
		EntryOutcome outcome, Guid? propertyId, ActorKind actorKind, Guid? actorUserId, string? actorName, string? actorRole,
		Guid? impersonatorUserId, string? targetType, Guid? targetId, string? bookingNumber, string? spotLabel,
		string metadata, IPAddress? clientIp, string? clientUserAgent, string? traceId) => new()
	{
		SourceEventId = sourceEventId,
		OccurredAt = occurredAt,
		Category = category,
		SourceModule = sourceModule,
		ActionCode = Clip(actionCode, 100)!,
		Outcome = outcome,
		PropertyId = propertyId,
		ActorKind = actorKind,
		ActorUserId = actorUserId,
		ActorNameSnapshot = Clip(actorName, 200),
		ActorRoleSnapshot = Clip(actorRole, 50),
		ImpersonatorUserId = impersonatorUserId,
		TargetType = Clip(targetType, 50),
		TargetId = targetId,
		BookingNumberSnapshot = Clip(bookingNumber, 50),
		SpotLabelSnapshot = Clip(spotLabel, 100),
		Metadata = metadata,
		ClientIp = clientIp,
		ClientUserAgent = Clip(clientUserAgent, 400),
		TraceId = Clip(traceId, 55),
	};

	private static string? Clip(string? value, int max) => value is { Length: > 0 } v && v.Length > max ? v[..max] : value;
}
