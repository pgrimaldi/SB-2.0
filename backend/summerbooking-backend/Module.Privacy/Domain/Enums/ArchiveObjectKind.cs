namespace Module.Privacy.Domain.Enums;

/// <summary>
/// Che cosa è l'oggetto inventariato in <c>privacy_archive_manifests</c>.
/// </summary>
/// <remarks>
/// <para>
/// Serve perché nell'inventario finiscono **due cose diverse**, che però devono avere la stessa
/// scadenza: l'archivio delle righe uscite dal database, e i **file che hanno seguito quelle righe**.
/// </para>
/// <para>
/// Il caso che l'ha richiesta: lo scontrino fiscale. La riga di <c>fiskaly.receipts</c> viene
/// archiviata a 36 mesi, e il suo PDF deve andarsene con lei — altrimenti il PDF resta sullo storage
/// senza nessuna riga calda che lo nomini, con la sua data di firma uscita dal database insieme alla
/// riga. Inventariando anche il PDF, l'**unica** policy decennale su questa tabella li cancella
/// entrambi al momento giusto.
/// </para>
/// <para>
/// ⚠️ È un'etichetta e non un <c>row_count IS NULL</c>: dedurre la natura di una riga dall'assenza di
/// un valore è il genere di implicito che qualcuno, fra due anni, legge al contrario.
/// </para>
/// </remarks>
public enum ArchiveObjectKind
{
	/// <summary>
	/// Un archivio di righe: JSON Lines compresso prodotto da una policy <c>archive_cold</c>.
	/// <c>row_count</c> dice quante righe contiene.
	/// </summary>
	RowArchive = 0,

	/// <summary>
	/// Un file che ha seguito le righe archiviate — il PDF di uno scontrino, per esempio. Non contiene
	/// righe di database, quindi <c>row_count</c> è NULL.
	/// </summary>
	LinkedAsset = 1,
}
