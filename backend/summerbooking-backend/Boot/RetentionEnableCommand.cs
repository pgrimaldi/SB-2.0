using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Module.Privacy;
using Npgsql;

namespace Boot;

/// <summary>
/// Accende o rimette in prova una policy di retention: <c>is_dry_run_only</c>, da riga di comando.
/// </summary>
/// <remarks>
/// <para>
/// <b>Perché è un comando e non una migration.</b> Una policy nasce con
/// <c>is_dry_run_only = true</c>: calcola, riporta e non tocca niente, finché una persona non la
/// accende dopo aver letto il report. È la protezione che regge tutto l'impianto. Metterla a
/// <c>false</c> in una migration la farebbe nascere <b>accesa</b> in ogni ambiente, produzione
/// compresa, il che è esattamente il contrario.
/// </para>
/// <para>
/// <b>Perché è un comando e non un <c>UPDATE</c> a mano.</b> Perché un <c>UPDATE</c> a mano non lascia
/// traccia, non si rivede, e si perde al primo ricaricamento del database da zero — è già accaduto il
/// 2026-10-05, quando un drop e rimigrazione ha riportato tutte le 61 policy in prova e le corse
/// successive non archiviavano più niente senza che niente lo dicesse. Un comando nel codice si legge,
/// si discute e si rifà con una riga.
/// </para>
/// <para>Uso:</para>
/// <code>
/// dotnet run --project Boot -- retention-enable --list
/// dotnet run --project Boot -- retention-enable --all
/// dotnet run --project Boot -- retention-enable 01a0ff0f-7c00-74ce-9164-b43d9c90ee42
/// dotnet run --project Boot -- retention-enable --all --dry-run-again   // torna indietro
/// </code>
/// <para>
/// ⚠️ <c>--all</c> chiede una conferma esplicita da tastiera, perché accende <b>tutte</b> le policy
/// attive in una volta. Con <c>--yes</c> non la chiede, per gli usi non interattivi.
/// </para>
/// </remarks>
internal static class RetentionEnableCommand
{
	public const string Verb = "retention-enable";

	public static async Task<int> RunAsync(string[] args)
	{
		var builder = Host.CreateApplicationBuilder(args);

		// Serve il solo modulo Privacy: la tabella delle policy e' sua, e non si passa per nessun'altra
		// connessione. Accendere una policy non e' un'operazione di retention: e' una modifica a una
		// riga di configurazione.
		builder.Services.AddPrivacyModule(builder.Configuration);

		using var host = builder.Build();
		using var scope = host.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<Module.Privacy.Infrastructure.DbContext>();
		var connection = context.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
			await connection.OpenAsync();

		var torna = args.Contains("--dry-run-again");
		var tutte = args.Contains("--all");
		var senzaChiedere = args.Contains("--yes");
		var id = args.FirstOrDefault(a => Guid.TryParse(a, out _));

		if (args.Contains("--list") || (!tutte && id is null))
		{
			await StampaAsync(connection);
			if (!args.Contains("--list"))
			{
				Console.WriteLine();
				Console.WriteLine("Nessuna policy indicata. Passa un public_id, oppure --all per tutte.");
				return 1;
			}

			return 0;
		}

		var valore = torna;   // --dry-run-again rimette in prova
		var azione = torna ? "rimesse in PROVA" : "ACCESE";

		if (tutte && !senzaChiedere)
		{
			Console.WriteLine();
			Console.WriteLine(torna
				? "Stai per rimettere in prova TUTTE le policy attive."
				: "⚠️  Stai per ACCENDERE tutte le policy attive: dalla prossima corsa cancelleranno, "
					+ "anonimizzeranno e archivieranno per davvero.");
			Console.Write("Scrivi 'confermo' per procedere: ");
			if (Console.ReadLine()?.Trim() != "confermo")
			{
				Console.WriteLine("Annullato: niente e' stato modificato.");
				return 1;
			}
		}

		await using var cmd = connection.CreateCommand();
		cmd.CommandText = tutte
			? """
				UPDATE privacy.privacy_retention_policies
				   SET is_dry_run_only = @valore, updated_at = now()
				 WHERE is_active AND deleted_at IS NULL AND is_dry_run_only <> @valore
				RETURNING public_id
				"""
			: """
				UPDATE privacy.privacy_retention_policies
				   SET is_dry_run_only = @valore, updated_at = now()
				 WHERE public_id = @id AND is_active AND deleted_at IS NULL
				RETURNING public_id
				""";

		cmd.Parameters.Add(new NpgsqlParameter("@valore", valore));
		if (!tutte)
			cmd.Parameters.Add(new NpgsqlParameter("@id", Guid.Parse(id!)));

		var toccate = new List<Guid>();
		await using (var reader = await cmd.ExecuteReaderAsync())
		{
			while (await reader.ReadAsync())
				toccate.Add(reader.GetGuid(0));
		}

		if (toccate.Count == 0)
		{
			Console.WriteLine(tutte
				? $"Nessuna policy da cambiare: erano gia' tutte {azione}."
				: "Nessuna policy cambiata: l'id non esiste, non e' attiva, o era gia' in quello stato.");
			return tutte ? 0 : 1;
		}

		Console.WriteLine($"{toccate.Count} policy {azione}.");
		foreach (var g in toccate)
			Console.WriteLine($"  {g}");

		Console.WriteLine();
		Console.WriteLine("Verifica con: dotnet run --project Boot -- retention-enable --list");
		return 0;
	}

	private static async Task StampaAsync(System.Data.Common.DbConnection connection)
	{
		await using var cmd = connection.CreateCommand();
		cmd.CommandText = """
			SELECT public_id, schema_name,
			       CASE WHEN table_name = '' THEN storage_prefix ELSE table_name END AS bersaglio,
			       duration_value, duration_unit::text, action::text, is_dry_run_only
			  FROM privacy.privacy_retention_policies
			 WHERE is_active AND deleted_at IS NULL
			 ORDER BY is_dry_run_only DESC, schema_name, bersaglio
			""";

		Console.WriteLine();
		Console.WriteLine("  stato   public_id                             bersaglio                               termine        azione");
		Console.WriteLine("  ------- ------------------------------------- --------------------------------------- -------------- --------------");

		var inProva = 0;
		var accese = 0;

		await using var reader = await cmd.ExecuteReaderAsync();
		while (await reader.ReadAsync())
		{
			var dry = reader.GetBoolean(6);
			if (dry) inProva++; else accese++;

			var bersaglio = $"{reader.GetString(1)}.{reader.GetString(2)}";
			var termine = $"{reader.GetInt16(3)} {reader.GetString(4)}";
			Console.WriteLine($"  {(dry ? "PROVA  " : "accesa ")} {reader.GetGuid(0)} "
				+ $"{bersaglio,-39} {termine,-14} {reader.GetString(5)}");
		}

		Console.WriteLine();
		Console.WriteLine($"  {accese} accese, {inProva} in prova.");
	}
}
