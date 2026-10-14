using Module.Privacy.Domain.Enums;
using Shared.Contracts;

namespace Module.Privacy.Domain.Entities;

/// <summary>
/// Esito applicato da un singolo modulo a una richiesta dell'interessato: una riga per ogni modulo
/// coinvolto, dettaglio che la testata (DataSubjectRequest) riassume in Status.
/// </summary>
public sealed class ModuleOutcome : IAuditable
{
	public long Id { get; private set; }
	public long RequestId { get; private set; }
	public ModuleCode ModuleCode { get; private set; }
	public ModuleOutcomeResult Outcome { get; private set; }
	public string? OutcomeDetail { get; private set; }
	public DateOnly? RetainedUntil { get; private set; }
	public DateTime? RespondedAt { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	/// <summary>Presente per uniformità di schema, non usato funzionalmente — stesso ciclo di vita della testata.</summary>
	public DateTime? DeletedAt { get; private set; }

	public DataSubjectRequest Request { get; private set; } = null!;

	private ModuleOutcome() { }   // unico costruttore, per il materializzatore di EF Core
}
