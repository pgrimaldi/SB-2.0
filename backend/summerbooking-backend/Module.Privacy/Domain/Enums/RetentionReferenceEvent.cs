namespace Module.Privacy.Domain.Enums;

/// <summary>
/// Evento da cui decorre la durata di una policy, in alternativa a una colonna data della riga.
/// Serve alle regole sui file, che nella maggior parte dei casi non decorrono da un istante scritto
/// sulla riga ma da un fatto accaduto altrove.
/// </summary>
/// <remarks>
/// Quando e' valorizzato, <c>reference_date_column</c> resta il fallback dichiarato per leggibilita'
/// ma la decorrenza effettiva la calcola il job dall'evento. Le due cose non si escludono: una
/// policy su <c>asset_unreferenced</c> ha comunque bisogno di sapere quale colonna guardare quando
/// l'evento non e' ricostruibile.
/// </remarks>
public enum RetentionReferenceEvent
{
	/// <summary>
	/// Cessazione dello stabilimento. ⚠️ A database quello stato <b>non esiste</b>: si deriva da
	/// <c>identity.account_subscriptions.status</c> (<c>cancelled</c> = cessazione esplicita,
	/// <c>expired</c> senza rinnovo = cessazione di fatto, <c>suspended</c> = nessuna delle due) e la
	/// data e' <c>closed_at</c> dell'ultimo periodo in
	/// <c>identity.account_subscription_periods</c>. Decisione del titolare del 2026-10-02: derivarla
	/// invece di aggiungere uno stato che qualcuno deve ricordarsi di scrivere.
	/// </summary>
	PropertyCeased = 0,

	/// <summary>
	/// L'ultima riga che usava il file e' stata rimossa. Da usare insieme a
	/// <c>only_if_unreferenced</c>: la durata e' il margine fra «non serve piu'» e «rimosso».
	/// </summary>
	AssetUnreferenced = 1,

	/// <summary>Il file e' stato sostituito da una versione nuova (listino ricaricato, sfondo di mappa ridisegnato).</summary>
	AssetReplaced = 2
}
