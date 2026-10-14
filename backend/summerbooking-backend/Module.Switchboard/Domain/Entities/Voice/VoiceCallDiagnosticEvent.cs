using Module.Switchboard.Domain.Enums.Voice;

namespace Module.Switchboard.Domain.Entities.Voice;

/// <summary>
/// Evento del flusso chiamata (webhook Telnyx o passo interno IVR); log diagnostico write-only, nessun
/// lettore applicativo confermato. Non implementa ISoftDeletable/IAuditable pieni: created_at/
/// updated_at/deleted_at sono esposti solo per uniformità di schema, le righe sono immutabili e
/// cancellate in blocco dal job di retention, mai da un flusso di dominio.
/// </summary>
public sealed class VoiceCallDiagnosticEvent
{
	public long Id { get; private set; }
	public long? ConfigId { get; private set; }
	public Guid? PropertyId { get; private set; }
	public long? CallRecordId { get; private set; }
	public CallEventSource EventSource { get; private set; }
	public string EventName { get; private set; } = null!;
	public CallDirectionRaw? Direction { get; private set; }
	public string? CallerNumber { get; private set; }
	public string? CalleeNumber { get; private set; }
	public string? DtmfDigits { get; private set; }
	public string? TelnyxCallSessionId { get; private set; }
	public string? TelnyxCallControlId { get; private set; }
	public string? TelnyxCallLegId { get; private set; }
	public string? HandlerResult { get; private set; }
	public string? RawPayload { get; private set; }
	public string? ErrorMessage { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public SwitchboardConfig? Config { get; private set; }
	public VoiceCallRecord? CallRecord { get; private set; }

	private VoiceCallDiagnosticEvent() { }   // unico costruttore, per il materializzatore di EF Core
}
