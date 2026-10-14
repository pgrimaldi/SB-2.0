using BuildingBlocks.Infrastructure.Retention;

namespace BuildingBlocks.Infrastructure.Tests;

/// <summary>
/// Il confine stagionale è il pezzo più facile da sbagliare di tutta la retention, e la
/// documentazione del progetto lo ha sbagliato per settimane: diceva 1 aprile invece di 1 gennaio.
/// Dieci policy dipendono da questa funzione, una delle quali azzera le note dei clienti — che è
/// irreversibile. Questi test esistono perché quell'errore non possa rientrare in silenzio.
/// </summary>
public sealed class SeasonBoundaryTests
{
	/// <summary>L'esempio letterale della documentazione.</summary>
	[Fact]
	public void Due_stagioni_valutate_nell_ottobre_2026_tagliano_al_primo_gennaio_2024()
	{
		var cutoff = SeasonBoundary.CutoffFor(new DateTime(2026, 10, 3, 14, 30, 0, DateTimeKind.Utc), 2);

		Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), cutoff);
	}

	/// <summary>
	/// Il confine è il <b>1 gennaio</b>, non il 1 aprile: è l'errore che la documentazione conteneva
	/// fino al 2026-10-01 e che una conversione improvvisata in mesi riporterebbe dentro.
	/// </summary>
	[Fact]
	public void Il_confine_e_il_primo_gennaio_non_il_primo_aprile()
	{
		var cutoff = SeasonBoundary.CutoffFor(new DateTime(2027, 4, 1, 4, 0, 0, DateTimeKind.Utc), 2);

		Assert.Equal(1, cutoff.Month);
		Assert.Equal(1, cutoff.Day);
		Assert.Equal(2025, cutoff.Year);
	}

	/// <summary>
	/// La soglia <b>non</b> dipende dal giorno in cui il job gira: due esecuzioni nello stesso anno
	/// civile tagliano allo stesso istante. È la differenza fra «N−2 stagioni» e «24 mesi».
	/// </summary>
	[Theory]
	[InlineData(1, 1)]
	[InlineData(3, 31)]
	[InlineData(4, 1)]
	[InlineData(12, 31)]
	public void La_soglia_non_dipende_dal_giorno_di_esecuzione(int month, int day)
	{
		var cutoff = SeasonBoundary.CutoffFor(new DateTime(2026, month, day, 0, 0, 0, DateTimeKind.Utc), 2);

		Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), cutoff);
	}

	/// <summary>
	/// Una prenotazione che finisce il 15 settembre 2024 con policy N−2 deve restare online per tutta
	/// la stagione 2026: è il caso che la documentazione porta per spiegare perché «24 mesi» non
	/// equivale a «2 stagioni». Con 24 mesi scadrebbe il 15 settembre 2026, in piena stagione.
	/// </summary>
	[Fact]
	public void Due_stagioni_non_equivalgono_a_ventiquattro_mesi()
	{
		var now = new DateTime(2026, 9, 20, 0, 0, 0, DateTimeKind.Utc);
		var stayTo = new DateTime(2024, 9, 15, 0, 0, 0, DateTimeKind.Utc);

		var seasonal = SeasonBoundary.CutoffFor(now, "seasons", 2);
		var months = SeasonBoundary.CutoffFor(now, "months", 24);

		Assert.True(stayTo >= seasonal, "con 2 stagioni la prenotazione deve sopravvivere alla stagione 2026");
		Assert.True(stayTo < months, "con 24 mesi sarebbe invece scaduta in piena stagione: e' il motivo dell'unita' 'seasons'");
	}

	[Fact]
	public void Zero_stagioni_taglia_al_primo_gennaio_dell_anno_corrente()
	{
		var cutoff = SeasonBoundary.CutoffFor(new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc), 0);

		Assert.Equal(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), cutoff);
	}

	/// <summary>
	/// Lo zero è legittimo e va ammesso: otto delle quindici policy sui file lo usano per dire «senza
	/// attesa». Una validazione che pretendesse un valore positivo le scarterebbe tutte.
	/// </summary>
	[Theory]
	[InlineData("days")]
	[InlineData("months")]
	[InlineData("years")]
	[InlineData("seasons")]
	public void Durata_zero_e_ammessa(string unit)
	{
		var now = new DateTime(2026, 10, 3, 12, 0, 0, DateTimeKind.Utc);

		var cutoff = SeasonBoundary.CutoffFor(now, unit, 0);

		// Con durata zero non c'e' attesa: tutto cio' che precede l'istante di valutazione e' scaduto
		// (per le stagioni, tutto cio' che precede il 1 gennaio dell'anno in corso).
		Assert.True(cutoff <= now);
	}

	[Theory]
	[InlineData("days", 30, "2026-09-03")]
	[InlineData("months", 24, "2024-10-03")]
	[InlineData("years", 10, "2016-10-03")]
	public void Le_altre_unita_sono_una_sottrazione_diretta(string unit, int value, string expected)
	{
		var now = new DateTime(2026, 10, 3, 0, 0, 0, DateTimeKind.Utc);

		var cutoff = SeasonBoundary.CutoffFor(now, unit, value);

		Assert.Equal(DateTime.Parse(expected).Date, cutoff.Date);
	}

	[Fact]
	public void Una_unita_non_riconosciuta_non_viene_indovinata()
	{
		var now = DateTime.UtcNow;

		// Meglio un'eccezione che una conversione improvvisata: un'unita' nuova aggiunta all'enum a
		// database e non gestita qui deve farsi notare, non produrre una data plausibile e sbagliata.
		Assert.Throws<ArgumentException>(() => SeasonBoundary.CutoffFor(now, "decadi", 1));
	}

	[Fact]
	public void Solo_le_stagioni_sono_stagionali()
	{
		Assert.True(SeasonBoundary.IsSeasonal("seasons"));
		Assert.False(SeasonBoundary.IsSeasonal("months"));
		Assert.False(SeasonBoundary.IsSeasonal("days"));
		Assert.False(SeasonBoundary.IsSeasonal("years"));
	}

	[Fact]
	public void Stagioni_negative_sono_rifiutate()
	{
		Assert.Throws<ArgumentOutOfRangeException>(() => SeasonBoundary.CutoffFor(DateTime.UtcNow, -1));
	}
}
