namespace Shared.Contracts.Audit;

/// <summary>Dati dell'azione da registrare; attore e contesto della richiesta li aggiunge il registro.</summary>
public sealed record AuditEntry(
	AuditCategory Category,
	string SourceModule,
	string ActionCode,
	AuditOutcome Outcome = AuditOutcome.Succeeded,
	Guid? PropertyId = null,
	string? TargetType = null,
	Guid? TargetId = null,
	IReadOnlyDictionary<string, object?>? Metadata = null,
	AuditActorKind? ActorKind = null,
	Guid? ActorUserId = null,
	string? ActorNameSnapshot = null,
	string? ActorRoleSnapshot = null,
	string? BookingNumberSnapshot = null,
	string? SpotLabelSnapshot = null);

/// <summary>Punto unico con cui i moduli scrivono nel registro di audit.</summary>
public interface IAuditTrail
{
	Task RecordAsync(AuditEntry entry, CancellationToken ct = default);
}
