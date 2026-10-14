using Module.Privacy.Domain.Enums;
using Shared.Contracts;

namespace Module.Privacy.Domain.Entities;

/// <summary>
/// Dove sta un archivio prodotto da una policy <c>archive_cold</c>: una riga per **oggetto** scritto
/// su storage. È il registro che permette di ritrovare le righe uscite dal database caldo — e, dopo,
/// di cancellarle quando scade il termine di legge.
/// </summary>
/// <remarks>
/// <para>
/// <b>Perché è una tabella sua e non una colonna di <see cref="RetentionRun"/>.</b> Il verbale di una
/// corsa è un dato <i>operativo</i>: dice com'è andata quella notte, interessa per qualche mese e ha
/// una sua retention a 24 mesi. Il manifesto è un dato <i>di inventario</i>: deve vivere **quanto
/// l'oggetto che indirizza**, che per gli scontrini fiscali è dieci anni. Tenerli nella stessa tabella
/// significava che la retention del primo cancellava il secondo, lasciando sullo storage oggetti di
/// cui il database non sapeva più niente — e togliendo alla policy di cancellazione dell'archivio la
/// fonte da cui parte.
/// </para>
/// <para>
/// <b>Che cosa la rende una tabella facile da trattare.</b> Ogni riga nomina **esattamente un
/// oggetto**, e tutte le righe sono della stessa natura: non c'è nessun sottoinsieme da distinguere.
/// È il motivo per cui una policy su questa tabella non ha bisogno di dichiarare un perimetro, al
/// contrario di quelle sulle tabelle di asset, che mescolano foto, PDF e icone.
/// </para>
/// <para>
/// ⚠️ Non implementa <c>ISoftDeletable</c>: un registro di inventario non si cancella logicamente.
/// Quando l'oggetto viene rimosso dallo storage si valorizza <see cref="PurgedAt"/>, e la riga resta
/// come prova di averlo rimosso nei termini.
/// </para>
/// </remarks>
public sealed class ArchiveManifest : IAuditable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }

	/// <summary>Corsa che ha prodotto l'archivio. Riferimento <b>logico</b>: le righe del verbale scadono prima di questa.</summary>
	public Guid RunId { get; private set; }

	public long PolicyId { get; private set; }
	public RetentionPolicy? Policy { get; private set; }

	/// <summary>Schema della tabella le cui righe sono dentro l'archivio.</summary>
	public string SourceSchema { get; private set; } = null!;

	/// <summary>Nome fisico della tabella le cui righe sono dentro l'archivio.</summary>
	public string SourceTable { get; private set; } = null!;

	public string StorageOrigin { get; private set; } = null!;

	/// <summary>Chiave dell'oggetto. Unica: ogni lotto di ogni corsa ha la sua.</summary>
	public string StoragePath { get; private set; } = null!;

	/// <summary>
	/// Byte dell'oggetto. NULL solo per un file collegato di cui la dimensione non era registrata:
	/// <c>byte_size</c> è annullabile su tutte le tabelle di asset.
	/// </summary>
	public long? ByteSize { get; private set; }

	/// <summary>
	/// Che cosa è questo oggetto: un archivio di righe, oppure un file che le ha seguite.
	/// </summary>
	public ArchiveObjectKind ObjectKind { get; private set; }

	/// <summary>
	/// Quante righe di database sono dentro questo oggetto. NULL quando l'oggetto **non è** un archivio
	/// di righe — vedi <see cref="ArchiveObjectKind.LinkedAsset"/>.
	/// </summary>
	public long? RowCount { get; private set; }

	/// <summary>Data di taglio della policy che ha prodotto l'archivio: dice **che cosa** c'è dentro.</summary>
	public DateTime CutoffAt { get; private set; }

	/// <summary>
	/// Quando l'archivio è stato scritto. È la **decorrenza** della policy che lo cancellerà: per gli
	/// scontrini, dieci anni da qui.
	/// </summary>
	public DateTime ArchivedAt { get; private set; }

	/// <summary>
	/// Quando l'oggetto è stato rimosso dallo storage. NULL = c'è ancora. Valorizzandolo, la policy di
	/// cancellazione dell'archivio smette di riaccodarlo.
	/// </summary>
	public DateTime? PurgedAt { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }

	private ArchiveManifest() { }   // unico costruttore, per il materializzatore di EF Core
}
