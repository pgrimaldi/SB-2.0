namespace Module.Privacy.Domain.Enums;

/// <summary>Unità della durata di conservazione dichiarata in una policy di retention.</summary>
public enum RetentionDurationUnit
{
	Days = 0,
	Months = 1,
	Years = 2,

	/// <summary>
	/// Durata espressa in <b>stagioni</b>, risolta dal job annuale sui confini di
	/// catalog.pricing_seasons — non equivalente a 12 mesi.
	/// <para>
	/// Serve perché il blocco C1 (prenotazioni) è deciso dal titolare in stagioni (N-1/N-2) e la
	/// conversione in mesi archivia troppo presto: una prenotazione con stay_to 2024-09-15 e policy
	/// N-2 deve restare online per tutta la stagione 2026 (il taglio è l'apertura della stagione
	/// 2027), ma "24 mesi da stay_to" scadrebbe il 2026-09-15, in piena stagione. Non esiste un
	/// numero di mesi valido per tutte le prenotazioni della stessa stagione, perché la decorrenza
	/// reale è il confine di stagione e non la data della singola riga.
	/// </para>
	/// <para>
	/// Vincolo applicativo (non esprimibile a schema): una policy con questa unità è applicabile
	/// solo dal job annuale SeasonalArchive, mai da un job mensile.
	/// </para>
	/// </summary>
	Seasons = 3
}
