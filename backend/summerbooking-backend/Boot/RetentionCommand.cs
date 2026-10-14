using BuildingBlocks.Infrastructure.Retention;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Module.Audit;
using Module.Booking;
using Module.Catalog;
using Module.Chatbot;
using Module.FiscalPrint;
using Module.Fiskaly;
using Module.Identity;
using Module.Infra;
using Module.Notification;
using Module.Payment;
using Module.Privacy;
using Module.Switchboard;

namespace Boot;

/// <summary>
/// Esecuzione da riga di comando dei job di retention. Sta in <c>Boot</c> perché Boot è il composition
/// root del monolite modulare: è l'unico posto che può registrare tutti i moduli, e nessun modulo
/// dipende da lui.
/// </summary>
/// <remarks>
/// <para>
/// Non è uno scheduler: la pianificazione (cron per job) arriverà con l'host
/// dei job. Questo è il modo per <b>eseguire</b> le policy ora, leggerne il report e decidere quali
/// accendere — che è il passo che la politica richiede prima dell'esecuzione reale.
/// </para>
/// <para>
/// Uso: <c>dotnet run --project Boot -- retention [--apply] [--schema NOME] [--no-seasonal]</c>
/// </para>
/// </remarks>
internal static class RetentionCommand
{
	public const string Verb = "retention";

	public static async Task<int> RunAsync(string[] args)
	{
		var apply = args.Contains("--apply");
		var includeSeasonal = !args.Contains("--no-seasonal");
		var onlySchema = ValueOf(args, "--schema");

		var builder = Host.CreateApplicationBuilder(args);

		// Niente validazione dei servizi all'avvio: i servizi delle API dipendono dal contesto della richiesta
		// HTTP (utente, registro di audit), che qui non esiste e che la retention non usa.
		builder.ConfigureContainer(new DefaultServiceProviderFactory(new ServiceProviderOptions { ValidateOnBuild = false, ValidateScopes = true }));

		// Gli stessi moduli di Program.Main: la retention gira sulle connessioni dei moduli, non su una
		// connessione centrale, perche' lo schema di un modulo e' privato al modulo.
		builder.Services.AddBookingModule(builder.Configuration);
		builder.Services.AddCatalogModule(builder.Configuration);
		builder.Services.AddChatbotModule(builder.Configuration);
		builder.Services.AddFiscalPrintModule(builder.Configuration);
		builder.Services.AddFiskalyModule(builder.Configuration);
		builder.Services.AddIdentityModule(builder.Configuration);
		builder.Services.AddInfraModule(builder.Configuration);
		builder.Services.AddNotificationModule(builder.Configuration);
		builder.Services.AddPaymentModule(builder.Configuration);
		builder.Services.AddPrivacyModule(builder.Configuration);
		builder.Services.AddSwitchboardModule(builder.Configuration);
		builder.Services.AddAuditModule(builder.Configuration);

		var archive = new ArchiveStoreOptions();
		builder.Configuration.GetSection("Retention:Archive").Bind(archive);
		builder.Services.AddRetention(archive);

		using var host = builder.Build();
		var log = host.Services.GetRequiredService<ILogger<Program>>();

		var options = new RetentionRunOptions
		{
			NowUtc = DateTime.UtcNow,
			Apply = apply,
			IncludeSeasonal = includeSeasonal
		};

		log.LogInformation("Retention — corsa {RunId}, {Mode}, stagionali {Seasonal}, archivio su {Bucket}@{Endpoint}",
			options.RunId,
			apply ? "ESECUZIONE REALE" : "PROVA (nessuna scrittura)",
			includeSeasonal ? "incluse" : "escluse",
			archive.Bucket, archive.EndpointUrl ?? "AWS");

		// L'archivio si prepara una volta sola, prima di cominciare: se il bucket non si puo' creare e'
		// meglio saperlo adesso che a metà della prima policy archive_cold.
		try
		{
			await host.Services.GetRequiredService<IArchiveStore>().EnsureReadyAsync(CancellationToken.None);
		}
		catch (Exception ex)
		{
			log.LogError(ex, "Archivio non raggiungibile: le policy archive_cold falliranno. {Message}", ex.Message);
		}

		// Quali policy le applica un job dedicato: si sa PRIMA di far girare il motore, altrimenti il
		// motore le valuta e il verbale finisce con due righe per la stessa policy.
		using (var scope = host.Services.CreateScope())
		{
			var delegate_ = scope.ServiceProvider.GetServices<IBespokeRetentionJob>()
				.Select(j => j.PolicyPublicId)
				.ToHashSet();

			if (delegate_.Count > 0)
				options = options with { PoliciesHandledElsewhere = delegate_ };
		}

		var reports = new List<SchemaRetentionReport>();
		using (var scope = host.Services.CreateScope())
		{
			var jobs = scope.ServiceProvider.GetServices<IModuleRetentionJob>()
				.Where(j => onlySchema is null || j.Schema == onlySchema)
				.OrderBy(j => j.Schema, StringComparer.Ordinal)
				.ToList();

			if (jobs.Count == 0)
			{
				log.LogWarning("Nessun job: lo schema '{Schema}' non corrisponde a nessun modulo.", onlySchema);
				return 1;
			}

			foreach (var job in jobs)
			{
				try
				{
					reports.Add(await job.RunAsync(options, CancellationToken.None));
				}
				catch (Exception ex)
				{
					// Un modulo che non si connette non deve impedire agli altri dieci di girare.
					log.LogError(ex, "[{Schema}] il job non è partito: {Message}", job.Schema, ex.Message);
					reports.Add(new SchemaRetentionReport { Schema = job.Schema });
				}
			}
		}

		Print(reports, options, archive);

		// Secondo passo: i job SCRITTI A MANO. Uno per ogni policy il cui perimetro non e' dichiarabile
		// in una riga di configurazione - oggi uno solo, le foto degli stabilimenti cessati. Girano
		// DOPO il motore perche' le loro policy sono fra quelle che il motore salta: l'ordine non cambia
		// il risultato, ma tiene il report leggibile nello stesso ordine in cui si legge la tabella.
		var bespoke = new List<BespokeRetentionOutcome>();
		using (var scope = host.Services.CreateScope())
		{
			var jobs = scope.ServiceProvider.GetServices<IBespokeRetentionJob>().ToList();
			foreach (var job in jobs)
			{
				try
				{
					bespoke.Add(await job.RunAsync(options, CancellationToken.None));
				}
				catch (Exception ex)
				{
					// Come per i moduli: un job che non parte non deve fermare gli altri.
					log.LogError(ex, "[{Job}] non e' partito: {Message}", job.Name, ex.Message);
					bespoke.Add(new BespokeRetentionOutcome
					{
						JobName = job.Name,
						Status = PolicyRunStatus.Failed,
						Error = ex.Message,
					});
				}
			}
		}

		PrintBespoke(bespoke, options);

		// Terzo passo: svuotare la coda delle cancellazioni su storage. Separato di proposito — le righe
		// le ha accodate il motore, ma rimuovere gli oggetti e' un'operazione che non partecipa alla
		// transazione del database, e va riconciliata per conto suo.
		ReconcileReport? queue = null;
		if (onlySchema is null or "privacy")
		{
			using var scope = host.Services.CreateScope();
			var job = scope.ServiceProvider.GetService<IFileDeletionQueueJob>();
			if (job is null)
				log.LogWarning("Nessun job della coda registrato: privacy_file_deletions non verra' svuotata.");
			else
			{
				try
				{
					queue = await job.RunAsync(apply, CancellationToken.None);
					PrintQueue(queue, apply);
				}
				catch (Exception ex)
				{
					log.LogError(ex, "Riconciliazione della coda non riuscita: {Message}", ex.Message);
				}
			}
		}

		return reports.Sum(r => r.Failed) == 0 && (queue?.Errors.Count ?? 0) == 0 ? 0 : 2;
	}

	private static string? ValueOf(string[] args, string name)
	{
		var i = Array.IndexOf(args, name);
		return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
	}

	private static void Print(List<SchemaRetentionReport> reports, RetentionRunOptions options, ArchiveStoreOptions archive)
	{
		var all = reports.SelectMany(r => r.Outcomes).ToList();

		Console.WriteLine();
		Console.WriteLine(new string('=', 100));
		Console.WriteLine($"RETENTION — corsa {options.RunId}");
		Console.WriteLine($"  modalita':  {(options.Apply ? "ESECUZIONE REALE" : "PROVA (nessuna scrittura)")}");
		Console.WriteLine($"  valutata a: {options.NowUtc:yyyy-MM-dd HH:mm:ss} UTC");
		Console.WriteLine($"  archivio:   {archive.Bucket} su {archive.EndpointUrl ?? "AWS"}");
		Console.WriteLine(new string('=', 100));
		Console.WriteLine($"{"SCHEMA.TABELLA",-44} {"AZIONE",-14} {"TAGLIO",-11} {"TROVATE",9} {"TOCCATE",9}  ESITO");

		foreach (var o in all.OrderBy(o => o.Policy.SchemaName, StringComparer.Ordinal)
					.ThenBy(o => o.Policy.TableName, StringComparer.Ordinal))
		{
			var target = o.Policy.IsPrefixPolicy
				? $"{o.Policy.SchemaName}:{o.Policy.StoragePrefix}"
				: $"{o.Policy.SchemaName}.{o.Policy.TableName}";
			var esito = o.Status switch
			{
				PolicyRunStatus.Succeeded => "ok",
				PolicyRunStatus.Skipped => "saltata: " + o.SkipReason,
				_ => "FALLITA: " + o.Error
			};
			Console.WriteLine($"{Trunc(target, 44),-44} {o.Policy.Action,-14} "
				+ $"{(o.CutoffAt?.ToString("yyyy-MM-dd") ?? "-"),-11} {o.MatchedRows,9} {o.AffectedRows,9}  {esito}");
		}

		Console.WriteLine(new string('-', 100));
		Console.WriteLine($"policy: {all.Count}  ·  ok: {all.Count(o => o.Status == PolicyRunStatus.Succeeded)}"
			+ $"  ·  saltate: {all.Count(o => o.Status == PolicyRunStatus.Skipped)}"
			+ $"  ·  FALLITE: {all.Count(o => o.Status == PolicyRunStatus.Failed)}");
		Console.WriteLine($"righe trovate: {all.Sum(o => o.MatchedRows):N0}  ·  righe toccate: {all.Sum(o => o.AffectedRows):N0}");

		var archived = all.Where(o => o.ArchivePath is not null).ToList();
		if (archived.Count > 0)
		{
			Console.WriteLine($"archiviati {archived.Count} insiemi di righe, "
				+ $"{archived.Sum(o => o.ArchiveByteSize ?? 0):N0} byte compressi:");
			foreach (var o in archived)
				Console.WriteLine($"  {o.Policy.SchemaName}.{o.PhysicalTable}: {o.ArchivePath}");
		}

		var failed = all.Where(o => o.Status == PolicyRunStatus.Failed).ToList();
		if (failed.Count > 0)
		{
			Console.WriteLine();
			Console.WriteLine($"ERRORI DA SISTEMARE ({failed.Count}):");
			foreach (var o in failed)
				Console.WriteLine($"  - {o.Policy.SchemaName}.{o.Policy.TableName} [{o.Policy.Action}]: {o.Error}");
		}

		Console.WriteLine();
		Console.WriteLine($"Verbale completo: SELECT * FROM privacy.privacy_retention_runs WHERE run_id = '{options.RunId}';");
		Console.WriteLine(new string('=', 100));
	}

	/// <summary>
	/// Il riquadro dei job scritti a mano. Separato da quello del motore perché sono cose diverse: qui
	/// il perimetro sta nel codice, e chi legge deve sapere che per questi non basta guardare la
	/// tabella delle policy.
	/// </summary>
	private static void PrintBespoke(List<BespokeRetentionOutcome> esiti, RetentionRunOptions options)
	{
		if (esiti.Count == 0)
			return;

		Console.WriteLine();
		Console.WriteLine("--- job con perimetro scritto nel codice ------------------------------------");

		foreach (var e in esiti)
		{
			var stato = e.Status switch
			{
				PolicyRunStatus.Succeeded => "ok     ",
				PolicyRunStatus.Skipped => "saltato",
				_ => "FALLITO",
			};

			Console.WriteLine($"  [{stato}] {e.SchemaName}.{e.TableName} — {e.JobName}");
			Console.WriteLine($"            trovate {e.MatchedRows}, toccate {e.AffectedRows}"
				+ (e.CutoffAt is null ? "" : $", taglio {e.CutoffAt:yyyy-MM-dd}")
				+ $", {e.Duration.TotalMilliseconds:F0} ms");

			var dettaglio = e.Error ?? e.SkipReason;
			if (dettaglio is not null)
				Console.WriteLine($"            {dettaglio}");
		}

		if (!options.Apply)
			Console.WriteLine("  (prova: nessun oggetto accodato)");
	}

	private static void PrintQueue(ReconcileReport r, bool apply)
	{
		Console.WriteLine();
		Console.WriteLine(new string('=', 100));
		Console.WriteLine($"CODA DELLE CANCELLAZIONI SU STORAGE — {(apply ? "ESECUZIONE REALE" : "PROVA")}");
		Console.WriteLine(new string('=', 100));
		Console.WriteLine($"  in coda (non ancora eseguite): {r.InQueue:N0}");
		Console.WriteLine($"  {(apply ? "rimossi" : "da rimuovere")}:                 {r.Deleted:N0}");
		if (r.Failed > 0)
			Console.WriteLine($"  non rimossi (errore):          {r.Failed:N0}");
		if (r.Stuck > 0)
			Console.WriteLine($"  ⚠️ fermi oltre il tetto di tentativi: {r.Stuck:N0} — vanno guardati a mano");

		foreach (var (origin, count) in r.UnconfiguredOrigins)
			Console.WriteLine($"  ⚠️ '{origin}': {count:N0} oggetti NON toccati — nessuna destinazione configurata "
				+ "(Retention:Deletion:Targets). Non e' un errore: e' la protezione che impedisce di "
				+ "cancellare da un bucket che nessuno ha dichiarato.");

		foreach (var e in r.Errors)
			Console.WriteLine($"  ERRORE: {e}");

		Console.WriteLine(new string('=', 100));
	}

	private static string Trunc(string s, int max) => s.Length <= max ? s : s[..(max - 1)] + "…";
}
