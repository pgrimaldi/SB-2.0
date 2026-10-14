using Module.Privacy.Domain.Enums;
using Shared.Contracts;

namespace Module.Privacy.Domain.Entities;

/// <summary>
/// Testata di una richiesta dell'interessato (Art. 15/17/20 GDPR): cancellazione o export. Non
/// contiene mai un dato personale del soggetto, solo riferimenti e metadati di workflow.
/// </summary>
public sealed class DataSubjectRequest : IAuditable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public RequestKind RequestKind { get; private set; }
	public SubjectKind SubjectKind { get; private set; }
	public Guid SubjectId { get; private set; }
	public Guid? PropertyId { get; private set; }
	public RequestStatus Status { get; private set; }
	public RequesterKind RequestedByKind { get; private set; }
	public Guid? RequestedByUserId { get; private set; }
	public DateTime RequestedAt { get; private set; }
	public DateTime DueAt { get; private set; }
	public DateTime? CompletedAt { get; private set; }
	public string? RejectionReason { get; private set; }
	public ExportArtifactOrigin? ExportArtifactOrigin { get; private set; }
	public string? ExportArtifactPath { get; private set; }
	public DateTime? ExportArtifactExpiresAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	/// <summary>Presente per uniformità di schema, non usato funzionalmente (registro di conformità permanente).</summary>
	public DateTime? DeletedAt { get; private set; }

	public ICollection<ModuleOutcome> ModuleOutcomes { get; private set; } = new List<ModuleOutcome>();

	private DataSubjectRequest() { }   // unico costruttore, per il materializzatore di EF Core
}
