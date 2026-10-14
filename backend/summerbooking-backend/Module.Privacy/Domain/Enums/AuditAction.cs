namespace Module.Privacy.Domain.Enums;

/// <summary>Azione eseguita da un modulo proprietario sui propri dati, tracciata nel registro di audit.</summary>
public enum AuditAction
{
	DeleteRow = 0,
	NullFields = 1,
	Anonymize = 2,
	ArchiveCold = 3,
	Exported = 4,

	/// <summary>
	/// Oggetto rimosso dallo storage. In coppia con <see cref="RetentionAction.DeleteObject"/>: senza
	/// questa etichetta la cancellazione di un file non sarebbe tracciabile in
	/// <c>privacy.privacy_compliance_audit_logs</c>, e un file cancellato per policy non si
	/// distinguerebbe da un file mai esistito.
	/// </summary>
	DeleteObject = 5,

	/// <summary>Oggetto spostato su classe di storage fredda. In coppia con <see cref="RetentionAction.ArchiveObject"/>.</summary>
	ArchiveObject = 6
}
