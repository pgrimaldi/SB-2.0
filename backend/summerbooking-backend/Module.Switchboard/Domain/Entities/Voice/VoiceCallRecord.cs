using Module.Switchboard.Domain.Enums.Voice;
using Shared.Contracts;

namespace Module.Switchboard.Domain.Entities.Voice;

/// <summary>
/// Una riga per chiamata gestita dal centralino: esito, durata, percorso IVR — tabella di dominio, non
/// log diagnostico (quello è VoiceCallDiagnosticEvent).
/// </summary>
public sealed class VoiceCallRecord : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long ConfigId { get; private set; }
	public Guid PropertyId { get; private set; }
	public CallDirection Direction { get; private set; }
	public string? CallerNumber { get; private set; }
	public string? CalleeNumber { get; private set; }
	public string? SipDestination { get; private set; }
	/// <summary>
	/// Esito della chiamata.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Sulle righe storiche migrate questo valore e' un segnaposto, non un esito osservato.</b> Il
	/// registro del vecchio centralino aveva quattro colonne - id, chiamante, destinazione, data - e
	/// <b>non tracciava l'esito</b>. Dato che la colonna e' obbligatoria, la migrazione vi scrive
	/// <see cref="CallOutcome.ForwardedAnswered"/> come assunzione dichiarata (decisione del titolare
	/// del 2026-09-30). E' accettabile solo perche' quelle 214
	/// righe sono dati di test, presenti nel solo ambiente di sviluppo e assenti in produzione: il
	/// valore storico e' nullo e il segnaposto non falsifica nulla di reale.
	/// </para>
	/// <para>
	/// <b>Il codice nuovo DEVE valorizzare questo campo con l'esito reale</b> (risposta dall'IVR,
	/// inoltro con risposta, senza risposta, occupato, fallito, riagganciata dal chiamante...). Non e'
	/// una buona pratica fra le altre: e' l'unico modo di distinguere una riga vera da una migrata.
	/// Se il nuovo backend scrivesse a sua volta un valore di comodo, le due popolazioni diventerebbero
	/// indistinguibili e ogni statistica sugli esiti perderebbe significato - senza che niente segnali
	/// il problema.
	/// </para>
	/// <para>
	/// Lo stesso vale per <see cref="ForwardResult"/>, che segue l'esito: sulle righe storiche e'
	/// <c>null</c> perche' non conoscendo l'esito non si conosce nemmeno il risultato dell'inoltro.
	/// </para>
	/// </remarks>
	public CallOutcome Outcome { get; private set; }

	/// <summary>
	/// Risultato dell'inoltro, valorizzato solo quando <see cref="Outcome"/> riguarda un inoltro.
	/// </summary>
	/// <remarks>
	/// <c>null</c> su tutte le righe storiche migrate, per la ragione spiegata su
	/// <see cref="Outcome"/>: il vecchio registro non tracciava l'esito. Il codice nuovo deve
	/// valorizzarlo insieme all'esito.
	/// </remarks>
	public ForwardResult? ForwardResult { get; private set; }
	public string? IvrPath { get; private set; }
	public Guid? FinalMenuItemPublicId { get; private set; }
	public string? TelnyxCallSessionId { get; private set; }
	public string? TelnyxCallControlId { get; private set; }
	public string? TelnyxCallLegId { get; private set; }
	public DateTime StartedAt { get; private set; }
	public DateTime? AnsweredAt { get; private set; }
	public DateTime? EndedAt { get; private set; }
	public int? DurationSeconds { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public SwitchboardConfig Config { get; private set; } = null!;
	public ICollection<VoiceCallDiagnosticEvent> DiagnosticEvents { get; private set; } = new List<VoiceCallDiagnosticEvent>();

	private VoiceCallRecord() { }   // unico costruttore, per il materializzatore di EF Core
}
