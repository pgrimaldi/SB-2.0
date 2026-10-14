using Module.Privacy.Domain.Enums;
using Shared.Contracts;

namespace Module.Privacy.Domain.Entities;

/// <summary>
/// Coda delle <b>azioni su storage</b> decise da una policy di retention (o a mano): cancellare un
/// oggetto, oppure spostarlo su una classe di storage piu' fredda. Indispensabile prima di toccare
/// qualunque file: un'operazione su S3 <b>non partecipa</b> alla transazione del database, quindi
/// senza una coda un errore di rete lascia il database che dice una cosa e lo storage che ne dice
/// un'altra. La riga nasce quando la decisione e' presa (<see cref="DecidedAt"/>) e si chiude quando
/// l'operazione e' stata davvero eseguita (<see cref="ExecutedAt"/>).
/// </summary>
/// <remarks>
/// ⚠️ <b>Il nome della tabella dice meno di quello che fa.</b> <c>privacy_file_deletions</c> nasce il
/// 2026-10-03 quando l'unica azione era la cancellazione; dal 2026-10-05 porta anche
/// <c>archive_object</c>, che <b>non cancella niente</b>. Il nome e' rimasto per non rinominare una
/// tabella gia' documentata in cinque posti: <see cref="Action"/> e' la colonna da guardare, e il suo
/// default e' <c>delete_object</c> perche' e' quello che erano tutte le righe prima.
/// </remarks>
/// <remarks>
/// La forma e' la stessa del checkpoint di <c>utility/02-migrazione-file</c>, e per la stessa ragione:
/// il numero di tentativi e' l'unica cosa che distingue «non ancora fatto» da «non si riesce».
/// <para>
/// Non c'e' nessuna colonna per la copia locale, di proposito: la cancellazione per policy agisce
/// <b>solo sulla copia AWS</b> (scelta del titolare). Una colonna sempre
/// NULL suggerirebbe un lavoro che nessuno ha deciso di fare.
/// </para>
/// </remarks>
public sealed class FileDeletion : IAuditable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }

	/// <summary>Schema del modulo proprietario della riga che nominava il file. NULL per i file che nessuna riga nomina (QR, orfani).</summary>
	public string? AssetSchema { get; private set; }

	/// <summary>Tabella di asset nella forma canonica relativa allo schema. NULL insieme agli altri due.</summary>
	public string? AssetTable { get; private set; }

	/// <summary>Chiave della riga di asset. NULL insieme agli altri due: o si sa quale riga lo usava, o non si sa, mai mezza informazione.</summary>
	public long? AssetId { get; private set; }

	/// <summary>Host dello storage come era al momento della decisione. Testo e non enum: i quattro moduli di asset hanno enum <c>media_origin</c> distinti.</summary>
	public string StorageOrigin { get; private set; } = null!;

	/// <summary>Chiave dell'oggetto come era al momento della decisione: se il path della riga cambia dopo, si cancella comunque quello giusto.</summary>
	public string StoragePath { get; private set; } = null!;

	/// <summary>Dimensione nota dell'oggetto, per rendicontare lo spazio liberato. NULL se il metadato non era stato calcolato.</summary>
	public int? ByteSize { get; private set; }

	/// <summary>
	/// Che cosa va fatto all'oggetto: <c>delete_object</c> lo rimuove, <c>archive_object</c> lo sposta
	/// su una classe di storage piu' fredda lasciandolo dov'e'.
	/// </summary>
	/// <remarks>
	/// ⚠️ Senza questa colonna le due azioni erano indistinguibili in coda, e il job che la svuota
	/// avrebbe <b>cancellato</b> i PDF degli scontrini fiscali che la policy a 36 mesi vuole soltanto
	/// raffreddare — e che devono vivere dieci anni. Fino al 2026-10-05 il motore saltava quelle due
	/// policy proprio per questo.
	/// <para>
	/// Riusa l'enum <c>privacy.retention_action</c> invece di averne uno suo, cosi' il valore in coda
	/// e' <b>lo stesso</b> che si legge sulla policy che l'ha deciso. Un vincolo CHECK limita la
	/// colonna alle due etichette che hanno senso qui: le altre quattro agiscono sulle righe.
	/// </para>
	/// <para>
	/// La <b>classe di destinazione</b> non sta qui: sta nella configurazione della destinazione
	/// (<c>Retention:Deletion:Targets[].ArchiveStorageClass</c>). Il motivo e' che «quale classe» e'
	/// una decisione di costo e dipende dall'ambiente — LocalStack la ignora, AWS la fattura — mentre
	/// la policy decide <i>che cosa</i> succede al dato, non <i>dove</i> finisce.
	/// </para>
	/// </remarks>
	public RetentionAction Action { get; private set; }

	/// <summary>Policy che ha deciso l'azione. NULL = azione manuale di un amministratore.</summary>
	public long? PolicyId { get; private set; }

	/// <summary>Perche' questo oggetto e' in coda, in forma leggibile da chi apre il pannello.</summary>
	public string Reason { get; private set; } = null!;

	public DateTime DecidedAt { get; private set; }

	/// <summary>Quando l'oggetto e' stato davvero rimosso dallo storage. NULL = ancora da fare.</summary>
	public DateTime? ExecutedAt { get; private set; }

	public short Attempts { get; private set; }
	public string? LastError { get; private set; }
	public DateTime? LastErrorAt { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	private FileDeletion() { }   // unico costruttore, per il materializzatore di EF Core
}
