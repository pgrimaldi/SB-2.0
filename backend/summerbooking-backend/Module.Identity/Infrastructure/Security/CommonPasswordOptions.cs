namespace Module.Identity.Infrastructure.Security;

/// <summary>Sezione <c>Identity:CommonPasswords</c>: aggiornamento mensile dell'elenco delle password diffuse.</summary>
public sealed class CommonPasswordOptions
{
	public const string Section = "Identity:CommonPasswords";

	public bool RefreshEnabled { get; set; } = true;

	/// <summary>Elenchi da scaricare e unire (testo, una password per riga).</summary>
	public List<string> Sources { get; set; } =
	[
		"https://raw.githubusercontent.com/danielmiessler/SecLists/master/Passwords/Common-Credentials/100k-most-used-passwords-NCSC.txt",
		"https://raw.githubusercontent.com/danielmiessler/SecLists/master/Passwords/Common-Credentials/Language-Specific/Italian_Pwdb_common-password-list-top-150.txt",
	];

	/// <summary>
	/// Dove salvare l'elenco scaricato. Se il file manca (primo avvio, container ricreato) vale la copia
	/// incorporata nel modulo fino all'aggiornamento successivo.
	/// </summary>
	public string FilePath { get; set; } = "data/identity/common-passwords.txt";

	/// <summary>Giorno del mese (sui mesi più corti vale l'ultimo giorno) e ora locale dell'aggiornamento.</summary>
	public int DayOfMonth { get; set; } = 1;
	public int Hour { get; set; } = 3;
	public string TimeZone { get; set; } = "Europe/Rome";

	/// <summary>Sotto questa soglia il download è considerato rotto e l'elenco in uso non viene toccato.</summary>
	public int MinimumEntries { get; set; } = 10_000;

	/// <summary>Limite per fonte, contro risposte anomale.</summary>
	public long MaxBytesPerSource { get; set; } = 20 * 1024 * 1024;
}
