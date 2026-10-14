using Module.Privacy.Domain.Enums;

namespace Module.Privacy.Domain.Entities;

/// <summary>
/// Verbale di una policy di retention in una corsa: che cosa ha trovato, che cosa ha toccato, dove ha
/// archiviato, e se è andata male perché. Una riga per <c>(corsa, policy)</c>.
/// </summary>
/// <remarks>
/// <para>
/// Esiste per tre ragioni che nessun'altra tabella copre.
/// </para>
/// <para>
/// <b>1. Dove sono finite le righe archiviate.</b> Una policy <c>archive_cold</c> esporta le righe su
/// storage a oggetti e poi le cancella dal database caldo: senza <see cref="ArchivePath"/> la domanda
/// «dove sono le prenotazioni archiviate nel 2026?» si risponde solo elencando il bucket.
/// </para>
/// <para>
/// <b>2. Gli errori.</b> Il motore non si ferma alla prima policy che fallisce, perché una tabella
/// trattenuta da una chiave esterna non deve impedire alle altre di scadere. Gli errori vanno quindi
/// raccolti da qualche parte, e il log del processo non basta: va letto da chi decide se accendere una
/// policy.
/// </para>
/// <para>
/// <b>3. La prova di cosa farebbe.</b> Tutte le policy nascono con <c>is_dry_run_only = true</c>, e il
/// primo giro serve a leggere <see cref="MatchedRows"/> prima di autorizzare l'esecuzione reale. È il
/// report di dry-run che la politica richiede.
/// </para>
/// <para>
/// Da non confondere con <see cref="ComplianceAuditEntry"/>, che è il registro <b>per riga</b> previsto
/// dall'art. 5.2 GDPR. I due convivono: questo dice «la policy X ha cancellato 200.884 righe di
/// notification.deliveries», quello direbbe «la riga 1234 è stata cancellata». ⚠️ Su questi volumi il
/// registro per riga è impraticabile — 3,8 milioni di righe di audit per una sola corsa — e la
/// questione va affrontata prima di attivare le policy sulle tabelle grandi.
/// </para>
/// </remarks>
public sealed class RetentionRun
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }

	/// <summary>Raggruppa tutte le righe prodotte dalla stessa esecuzione, su tutti gli schemi.</summary>
	public Guid RunId { get; private set; }

	public long PolicyId { get; private set; }
	public RetentionPolicy? Policy { get; private set; }

	public string SchemaName { get; private set; } = null!;

	/// <summary>Nome <b>fisico</b> della tabella, come risolto dal motore: in <c>privacy</c> e <c>switchboard</c> ha il prefisso di modulo.</summary>
	public string TableName { get; private set; } = null!;

	public RetentionAction Action { get; private set; }

	/// <summary>Data di taglio calcolata. NULL quando la policy è stata saltata prima di calcolarla.</summary>
	public DateTime? CutoffAt { get; private set; }

	/// <summary>La corsa non ha scritto niente: o era una prova, o la policy è <c>is_dry_run_only</c>.</summary>
	public bool IsDryRun { get; private set; }

	/// <summary>Righe oltre la data di taglio.</summary>
	public long MatchedRows { get; private set; }

	/// <summary>Righe davvero cancellate, azzerate o anonimizzate. Zero in prova, anche con <see cref="MatchedRows"/> alto.</summary>
	public long AffectedRows { get; private set; }

	public string? ArchiveOrigin { get; private set; }

	/// <summary>Chiave dell'oggetto di archivio; più chiavi separate da spazio se l'export è andato in più lotti.</summary>
	public string? ArchivePath { get; private set; }

	public long? ArchiveByteSize { get; private set; }

	public RetentionRunStatus Status { get; private set; }

	/// <summary>Il motivo del salto, o il testo dell'errore. NULL se è andata liscia.</summary>
	public string? Detail { get; private set; }

	public DateTime StartedAt { get; private set; }
	public DateTime FinishedAt { get; private set; }
	public long DurationMs { get; private set; }

	public DateTime CreatedAt { get; private set; }

	private RetentionRun() { }   // unico costruttore, per il materializzatore di EF Core
}
