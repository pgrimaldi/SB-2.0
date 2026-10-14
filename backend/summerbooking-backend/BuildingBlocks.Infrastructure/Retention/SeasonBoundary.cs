namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>
/// L'<b>unica</b> funzione che traduce una durata espressa in <i>stagioni</i> in una data di taglio.
/// </summary>
/// <remarks>
/// Esiste come punto singolo di proposito. La regola è: <b>il confine è il 1 gennaio dell'anno
/// N−k</b>, ricalcolato a ogni esecuzione — non il 1 aprile, e non la data in cui il job gira. Se
/// ogni job stagionale se la ricalcolasse, prima o poi uno userebbe il 1 aprile: è l'errore che la
/// documentazione del progetto conteneva fino al 2026-10-01, e riguarda 10 policy (le 9 del blocco C1
/// più quella dei QR).
/// <para>
/// Esempio dalla documentazione: eseguita nell'ottobre 2026, una policy «2 stagioni» taglia tutto ciò
/// che precede il <b>1 gennaio 2024</b>.
/// </para>
/// </remarks>
public static class SeasonBoundary
{
	/// <summary>
	/// Data di taglio per una durata di <paramref name="seasons"/> stagioni, valutata a
	/// <paramref name="nowUtc"/>. Tutto ciò che è <b>strettamente anteriore</b> al valore restituito
	/// è scaduto.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">se <paramref name="seasons"/> è negativo.</exception>
	public static DateTime CutoffFor(DateTime nowUtc, int seasons)
	{
		if (seasons < 0)
			throw new ArgumentOutOfRangeException(nameof(seasons), seasons, "Le stagioni non possono essere negative.");

		return new DateTime(nowUtc.Year - seasons, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	}

	/// <summary>
	/// Data di taglio per una durata e un'unità qualsiasi. <c>seasons</c> passa da
	/// <see cref="CutoffFor(DateTime,int)"/>; le altre unità sono una sottrazione diretta.
	/// </summary>
	/// <param name="nowUtc">istante di valutazione.</param>
	/// <param name="unit">etichetta dell'enum <c>privacy.retention_duration_unit</c>, come arriva dal database.</param>
	/// <param name="value">durata numerica. Lo <b>zero è legittimo</b>: significa «senza attesa», e lo
	/// usano le policy sugli orfani e quella dell'audio del centralino alla cessazione.</param>
	public static DateTime CutoffFor(DateTime nowUtc, string unit, int value) => unit switch
	{
		"days" => nowUtc.AddDays(-value),
		"months" => nowUtc.AddMonths(-value),
		"years" => nowUtc.AddYears(-value),
		"seasons" => CutoffFor(nowUtc, value),
		_ => throw new ArgumentException($"Unità di durata non riconosciuta: '{unit}'.", nameof(unit))
	};

	/// <summary>
	/// Una durata in stagioni è applicabile <b>solo</b> da un job annuale: un job mensile che la
	/// leggesse non saprebbe tradurla in una data di taglio coerente con l'apertura della stagione.
	/// Vincolo applicativo, non esprimibile a schema.
	/// </summary>
	public static bool IsSeasonal(string unit) => unit == "seasons";
}
