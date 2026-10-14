using Module.Privacy.Domain.Enums;
using Shared.Contracts;

namespace Module.Privacy.Domain.Entities;

/// <summary>
/// Flag esplicito che sospende cancellazione/anonimizzazione automatica di retention per un soggetto
/// (ed eventualmente un singolo modulo) in presenza di un contenzioso in corso. Impostato manualmente
/// dall'ufficio legale, mai da un job.
/// </summary>
public sealed class LegalHold : IAuditable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public SubjectKind SubjectKind { get; private set; }
	public Guid SubjectId { get; private set; }
	public ModuleCode? ModuleCode { get; private set; }
	public string Reason { get; private set; } = null!;
	public string? CaseReference { get; private set; }
	public Guid PlacedByUserId { get; private set; }
	public DateTime? ReleasedAt { get; private set; }
	public Guid? ReleasedByUserId { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	/// <summary>Presente per uniformità di schema, non usato funzionalmente — un hold rilasciato non si cancella.</summary>
	public DateTime? DeletedAt { get; private set; }

	private LegalHold() { }   // unico costruttore, per il materializzatore di EF Core
}
