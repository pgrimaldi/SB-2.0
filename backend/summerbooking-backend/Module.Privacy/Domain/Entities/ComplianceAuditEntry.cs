using Module.Privacy.Domain.Enums;

namespace Module.Privacy.Domain.Entities;

/// <summary>
/// Registro append-only di ogni azione di cancellazione/anonimizzazione/export eseguita da un modulo
/// proprietario sui propri dati — prova di conformità Art. 5.2 GDPR (chi/cosa/quando/perché). Non
/// implementa IAuditable (UpdatedAt non cambia mai dopo l'inserimento, niente AuditInterceptor) né
/// ISoftDeletable (DeletedAt presente solo per uniformità di schema, hard-delete a scadenza retention).
/// </summary>
public sealed class ComplianceAuditEntry
{
	public long Id { get; private set; }
	public DateTime OccurredAt { get; private set; }
	public ModuleCode ModuleCode { get; private set; }
	public string SchemaName { get; private set; } = null!;
	public string TableName { get; private set; } = null!;
	public long RecordId { get; private set; }
	public List<string> FieldNames { get; private set; } = null!;
	public AuditAction Action { get; private set; }
	public long? RetentionPolicyId { get; private set; }
	public long? DataSubjectRequestId { get; private set; }
	public string Reason { get; private set; } = null!;
	public ActorKind PerformedBy { get; private set; }
	public Guid? PerformedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }

	/// <summary>Presente per uniformità di schema, non usato: registro append-only, non cambia mai dopo l'inserimento.</summary>
	public DateTime UpdatedAt { get; private set; }

	/// <summary>Presente per uniformità di schema, non usato dall'applicazione — hard-delete a scadenza retention.</summary>
	public DateTime? DeletedAt { get; private set; }

	public RetentionPolicy? RetentionPolicy { get; private set; }
	public DataSubjectRequest? DataSubjectRequest { get; private set; }

	private ComplianceAuditEntry() { }   // unico costruttore, per il materializzatore di EF Core
}
