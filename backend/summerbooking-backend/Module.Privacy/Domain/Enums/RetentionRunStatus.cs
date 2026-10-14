namespace Module.Privacy.Domain.Enums;

/// <summary>Esito della valutazione di una policy di retention in una corsa.</summary>
public enum RetentionRunStatus
{
	/// <summary>Eseguita, oppure valutata in prova, senza errori.</summary>
	Succeeded = 0,

	/// <summary>
	/// Non eseguita per una ragione dichiarata e legittima: una durata in stagioni letta da un job
	/// mensile, una policy su prefisso di storage, una decorrenza da evento. <b>Non è un errore</b>, e
	/// tenerla distinta serve proprio a non farla sembrare tale.
	/// </summary>
	Skipped = 1,

	/// <summary>
	/// Tentata e fallita: una chiave esterna che trattiene le righe, una colonna che non esiste, una
	/// scrittura su storage rifiutata. Il motore non si ferma — raccoglie qui e passa alla policy
	/// successiva, perché una tabella bloccata non deve impedire alle altre di scadere.
	/// </summary>
	Failed = 2
}
