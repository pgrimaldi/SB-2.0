namespace Module.Privacy.Domain.Enums;

/// <summary>Azione da applicare al superamento della durata dichiarata in una policy di retention.</summary>
public enum RetentionAction
{
	DeleteRow = 0,
	NullFields = 1,
	Anonymize = 2,
	ArchiveCold = 3,

	/// <summary>
	/// Rimuovi l'<b>oggetto dallo storage</b> (non la riga). Introdotta dalla politica sui file del
	/// 2026-10-01: le tre azioni precedenti agiscono tutte su una riga, e <c>archive_cold</c> sposta la
	/// riga, non il file.
	/// <para>
	/// Vincolo applicativo (non esprimibile a schema): la cancellazione non va eseguita in linea ma
	/// accodata in <c>privacy.privacy_file_deletions</c>, perche' una DELETE su S3 non partecipa alla
	/// transazione del database.
	/// </para>
	/// </summary>
	DeleteObject = 4,

	/// <summary>
	/// Sposta l'<b>oggetto</b> su classe di storage fredda (Glacier), lasciando la riga dov'e'. Da non
	/// confondere con <c>archive_cold</c>, che archivia la riga: sullo stesso target le due possono
	/// convivere (gli scontrini fiskaly hanno entrambe).
	/// </summary>
	ArchiveObject = 5
}
