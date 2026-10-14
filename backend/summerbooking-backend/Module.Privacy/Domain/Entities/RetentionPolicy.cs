using Module.Privacy.Domain.Enums;
using Shared.Contracts;

namespace Module.Privacy.Domain.Entities;

/// <summary>
/// Configurazione dichiarativa di retention: su schema.tabella (campo/i X), calcolato dalla colonna
/// data Y, oltre la durata Z, esegui l'azione W. Letta dal job del modulo proprietario, mai eseguita
/// da qui. Ambito piattaforma, valida per tutti gli stabilimenti.
/// </summary>
public sealed class RetentionPolicy : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public string SchemaName { get; private set; } = null!;
	public string TableName { get; private set; } = null!;
	public string[] FieldNames { get; private set; } = null!;
	public string ReferenceDateColumn { get; private set; } = null!;
	public short DurationValue { get; private set; }
	public RetentionDurationUnit DurationUnit { get; private set; }
	public RetentionAction Action { get; private set; }

	/// <summary>
	/// Cancella/archivia l'oggetto <b>solo se nessun'altra riga lo usa</b>. Riguarda le sole azioni su
	/// storage: un file puo' essere referenziato da piu' righe (la stessa foto su due vetrine, lo
	/// stesso allegato su due campagne) e la scadenza della prima non lo rende inutile.
	/// </summary>
	public bool OnlyIfUnreferenced { get; private set; }

	/// <summary>
	/// Evento da cui decorre la durata, in alternativa a <see cref="ReferenceDateColumn"/>. Vedi
	/// <see cref="RetentionReferenceEvent"/>: serve alle regole sui file, che quasi mai decorrono da un
	/// istante scritto sulla riga.
	/// </summary>
	public RetentionReferenceEvent? ReferenceEvent { get; private set; }

	/// <summary>
	/// Prefisso di storage su cui agisce la policy, quando non c'e' nessuna riga a database che nomini
	/// i file (i QR delle prenotazioni, i file orfani). Alternativo a <see cref="TableName"/>: uno dei
	/// due e' valorizzato, mai entrambi, mai nessuno (vincolo <c>ck_..._target_xor</c>).
	/// </summary>
	public string? StoragePrefix { get; private set; }

	/// <summary>Da dove prende la data una policy su prefisso. Valorizzato se e solo se <see cref="StoragePrefix"/> lo e'.</summary>
	public RetentionStorageDateSource? StorageDateSource { get; private set; }

	/// <summary>
	/// <b>Quali</b> righe della tabella riguarda questa policy, quando non sono tutte. JSON, NULL
	/// quando il perimetro non è stato dichiarato.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Le altre colonne dicono <i>quando</i> un dato scade con grande precisione, ma sul <i>quali</i>
	/// sapevano dire solo «le righe di questa tabella». Basta per le policy sulle righe — una tabella
	/// contiene una cosa sola — e <b>non</b> basta per i file: una tabella di asset mescola foto, PDF e
	/// icone, e «tutti i file oltre 24 mesi» è una regola più larga di quella decisa.
	/// </para>
	/// <para>Due forme ammesse, entrambe oggetti JSON:</para>
	/// <code>
	/// {"scope": "whole_table"}                                  -- tutte le righe, DICHIARATO
	/// {"column": "kind", "op": "=", "value": "rendered_pdf"}    -- un filtro
	/// {"column": "kind", "op": "in", "values": ["a", "b"]}
	/// {"column": "purged_at", "op": "is null"}
	/// </code>
	/// <para>
	/// ⚠️ <c>{"scope": "whole_table"}</c> non è ridondante: trasforma «nessuno ha detto quali» in
	/// «qualcuno ha detto tutte». Sulle azioni su storage è la differenza fra una policy che il motore
	/// esegue e una che salta, perché su un file un perimetro indovinato non si annulla.
	/// </para>
	/// <para>
	/// Operatori ammessi: <c>=</c>, <c>&lt;&gt;</c>, <c>in</c>, <c>not in</c>, <c>is null</c>,
	/// <c>is not null</c>. I valori sono limitati a token identificativi (lettere, cifre,
	/// <c>_ . : -</c>, max 64 caratteri): vedi <c>RetentionEngine.BuildTargetFilter</c> per il perché.
	/// </para>
	/// </remarks>
	public string? TargetFilter { get; private set; }

	/// <summary>
	/// Il file che deve **seguire** le righe archiviate da questa policy. JSON, NULL quando non c'è
	/// nessun file collegato.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Riguarda le sole policy <c>archive_cold</c>. Alcune righe nominano un file che è parte del dato,
	/// non un accessorio: la riga di <c>fiskaly.receipts</c> nomina il PDF dello scontrino. Archiviare
	/// la riga e lasciare il PDF dov'era significa che <b>il PDF resta sullo storage senza nessuna riga
	/// calda che lo nomini</b>, e con la sua data di firma uscita dal database insieme alla riga: non
	/// c'è più niente che possa decidere quando scade.
	/// </para>
	/// <code>
	/// {"column": "pdf_asset_id", "table": "media_assets", "action": "archive_object"}
	/// </code>
	/// <list type="bullet">
	/// <item><c>column</c> — la colonna di <b>questa</b> tabella che porta l'id dell'asset;</item>
	/// <item><c>table</c> — la tabella di asset, nella forma canonica, <b>nello stesso schema</b>;</item>
	/// <item><c>action</c> — che fare dell'oggetto al momento dell'archiviazione:
	/// <c>archive_object</c> (lo raffredda) oppure <c>delete_object</c> (lo rimuove).</item>
	/// </list>
	/// <para>
	/// ⚠️ Il file viene anche <b>inventariato</b> in <c>privacy_archive_manifests</c> con
	/// <c>object_kind = 'linked_asset'</c>, così la policy decennale su quell'inventario lo cancella
	/// insieme all'archivio delle righe che accompagnava. È quello che rende la coppia
	/// «riga + file» una cosa sola anche dopo che la riga è uscita dal database.
	/// </para>
	/// </remarks>
	public string? LinkedAsset { get; private set; }

	public string LegalBasis { get; private set; } = null!;
	public RetentionBasisSource BasisSource { get; private set; }
	public bool IsDryRunOnly { get; private set; }
	public bool IsActive { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private RetentionPolicy() { }   // unico costruttore, per il materializzatore di EF Core
}
