using System.Data;
using System.Data.Common;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using BuildingBlocks.Infrastructure.Storage;

namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>Dove vive, davvero, un <c>storage_origin</c> della coda delle cancellazioni.</summary>
public sealed class StorageDeletionTarget
{
	/// <summary>
	/// Classe di storage su cui finiscono gli oggetti di una riga <c>archive_object</c>.
	/// </summary>
	/// <remarks>
	/// Sta nella configurazione e non sulla policy perche' «quale classe» e' una decisione di costo e
	/// dipende dall'ambiente: su LocalStack e' accettata e ignorata, su AWS si fattura. La policy
	/// decide <i>che cosa</i> succede al dato, la configurazione <i>dove</i> finisce.
	/// <para>
	/// ⚠️ <c>GLACIER</c> ha un <b>minimo di 90 giorni</b> di fatturazione e il recupero non e'
	/// immediato: un oggetto raffreddato e poi cancellato prima dei 90 giorni si paga comunque per 90.
	/// Per gli scontrini - raffreddati a 36 mesi e cancellati a 10 anni - non e' un problema.
	/// </para>
	/// </remarks>
	public string ArchiveStorageClass { get; set; } = "GLACIER";

	/// <summary>L'etichetta che la coda porta in <c>storage_origin</c>, per esempio <c>s3us_east2</c>.</summary>
	public string Origin { get; set; } = "";

	public string Bucket { get; set; } = "";
	public string Region { get; set; } = "us-east-2";
	public string? EndpointUrl { get; set; }
	public string? AccessKeyId { get; set; }
	public string? SecretAccessKey { get; set; }
}

public sealed class StorageDeletionOptions
{
	/// <summary>
	/// Le destinazioni configurate, una per <c>storage_origin</c>. ⚠️ Un'origine <b>non elencata qui
	/// non viene toccata</b>: è la sola cosa che impedisce a questo job di cancellare dal bucket di
	/// produzione perché nessuno ha pensato a configurarlo.
	/// </summary>
	public List<StorageDeletionTarget> Targets { get; set; } = [];

	/// <summary>Oggetti per richiesta. S3 accetta fino a 1000 chiavi per <c>DeleteObjects</c>.</summary>
	public int BatchSize { get; set; } = 500;

	/// <summary>Oltre questi tentativi la riga non si ritenta più da sola: va guardata da una persona.</summary>
	public int MaxAttempts { get; set; } = 5;
}

/// <summary>Esito di una corsa di riconciliazione.</summary>
public sealed class ReconcileReport
{
	public long InQueue { get; set; }
	public long Deleted { get; set; }

	/// <summary>Oggetti spostati su classe fredda. Non sono stati rimossi: ci sono ancora.</summary>
	public long Archived { get; set; }
	public long Failed { get; set; }
	public long Stuck { get; set; }

	/// <summary>Origini presenti in coda per cui non c'è nessuna destinazione configurata: non toccate.</summary>
	public Dictionary<string, long> UnconfiguredOrigins { get; } = [];

	public List<string> Errors { get; } = [];
}

/// <summary>
/// Svuota la coda <c>privacy.privacy_file_deletions</c>: prende le righe con <c>executed_at IS NULL</c>,
/// rimuove gli oggetti dallo storage e segna l'esito. È il job <c>ReconcileStorageDeletions</c>.
/// </summary>
/// <remarks>
/// <para>
/// Esiste perché una <c>DELETE</c> su S3 <b>non partecipa</b> alla transazione del database. Il
/// gestionale precedente non aveva questa separazione — chiedeva la rimozione senza attenderne l'esito,
/// ingoiava gli errori e riscriveva il database come se fosse andata — e il risultato misurato sono
/// ~12.000 file orfani sul bucket.
/// </para>
/// <para>
/// ⚠️ <b>Due protezioni, entrambe deliberate.</b> La prima: un <c>storage_origin</c> per cui non esiste
/// una destinazione configurata <b>non viene toccato</b>, e il job lo riporta invece di indovinare un
/// bucket. La seconda: in modalità di prova (<c>apply = false</c>) non parte nessuna richiesta di
/// cancellazione. Insieme, impediscono che un ambiente di sviluppo cancelli dal bucket di produzione
/// perché nessuno ha letto la configurazione.
/// </para>
/// </remarks>
public sealed class StorageDeletionReconciler(StorageDeletionOptions options, ILogger<StorageDeletionReconciler> log)
{
	public async Task<ReconcileReport> RunAsync(DbConnection connection, bool apply, CancellationToken ct)
	{
		var report = new ReconcileReport();

		if (connection.State != ConnectionState.Open)
			await connection.OpenAsync(ct);

		var byOrigin = options.Targets.ToDictionary(t => t.Origin, StringComparer.Ordinal);

		report.InQueue = await ScalarAsync(connection,
			"SELECT count(*) FROM privacy.privacy_file_deletions WHERE executed_at IS NULL", ct);
		report.Stuck = await ScalarAsync(connection,
			"SELECT count(*) FROM privacy.privacy_file_deletions WHERE executed_at IS NULL AND attempts >= @max",
			ct, ("@max", options.MaxAttempts));

		if (report.InQueue == 0)
		{
			log.LogInformation("Coda delle cancellazioni vuota: niente da riconciliare.");
			return report;
		}

		// Le origini in coda, con il loro conteggio: serve a dire quali non sono configurate.
		var origins = new Dictionary<string, long>(StringComparer.Ordinal);
		await using (var cmd = connection.CreateCommand())
		{
			cmd.CommandText = """
				SELECT storage_origin, count(*) FROM privacy.privacy_file_deletions
				 WHERE executed_at IS NULL AND attempts < @max GROUP BY 1
				""";
			AddParam(cmd, "@max", options.MaxAttempts);
			await using var reader = await cmd.ExecuteReaderAsync(ct);
			while (await reader.ReadAsync(ct))
				origins[reader.GetString(0)] = reader.GetInt64(1);
		}

		foreach (var (origin, count) in origins)
		{
			if (!byOrigin.TryGetValue(origin, out var target))
			{
				report.UnconfiguredOrigins[origin] = count;
				log.LogWarning("storage_origin '{Origin}': {Count} oggetti in coda e NESSUNA destinazione "
					+ "configurata. Non vengono toccati: configurare Retention:Deletion:Targets, oppure "
					+ "lasciarli dove sono e' la scelta giusta.", origin, count);
				continue;
			}

			await ReconcileOriginAsync(connection, origin, target, apply, report, ct);
		}

		return report;
	}

	private async Task ReconcileOriginAsync(DbConnection connection, string origin, StorageDeletionTarget target,
		bool apply, ReconcileReport report, CancellationToken ct)
	{
		using var s3 = BuildClient(target);

		while (!ct.IsCancellationRequested)
		{
			// ⚠️ asset_schema/asset_table/asset_id sono ANNULLABILI per progetto: una riga in coda puo'
			// venire da una policy su PREFISSO, che indirizza oggetti senza nessuna riga di asset dietro.
			// Leggerle come non annullabili faceva fallire la cancellazione dell'oggetto - che e' il
			// lavoro - per non poter scrivere una data accessoria. Le righe senza asset si saltano.
			var batch = new List<QueuedAction>();
			await using (var cmd = connection.CreateCommand())
			{
				// L'azione si proietta con ::text: questa connessione e' quella di Privacy, ma il job
				// deve restare leggibile anche se un domani girasse da un'altra.
				cmd.CommandText = """
					SELECT id, storage_path, action::text, asset_schema, asset_table, asset_id
					  FROM privacy.privacy_file_deletions
					 WHERE executed_at IS NULL AND attempts < @max AND storage_origin = @origin
					 ORDER BY decided_at
					 LIMIT @limit
					""";
				AddParam(cmd, "@max", options.MaxAttempts);
				AddParam(cmd, "@origin", origin);
				AddParam(cmd, "@limit", options.BatchSize);
				await using var reader = await cmd.ExecuteReaderAsync(ct);
				while (await reader.ReadAsync(ct))
					batch.Add(new QueuedAction(
						reader.GetInt64(0),
						reader.GetString(1),
						reader.GetString(2),
						reader.IsDBNull(3) ? null : reader.GetString(3),
						reader.IsDBNull(4) ? null : reader.GetString(4),
						reader.IsDBNull(5) ? null : reader.GetInt64(5)));
			}

			if (batch.Count == 0)
				return;

			// ⚠️ Le due azioni si eseguono SEPARATAMENTE, anche quando arrivano nello stesso lotto: una
			// cancella, l'altra no, e una richiesta sbagliata di massa non si annulla. Il raggruppamento
			// e' per azione e non per chiave, cosi' non esiste nessun percorso in cui una riga
			// archive_object finisca in una DeleteObjectsRequest.
			var daCancellare = batch.Where(b => b.Action == "delete_object").ToList();
			var daRaffreddare = batch.Where(b => b.Action == "archive_object").ToList();

			if (!apply)
			{
				log.LogInformation("[prova] '{Origin}' su {Bucket}: {Del} oggetti verrebbero rimossi, "
					+ "{Arc} spostati su {Class}.",
					origin, target.Bucket, daCancellare.Count, daRaffreddare.Count, target.ArchiveStorageClass);
				report.Deleted += batch.Count;   // in prova e' "quanti ne farebbe", e non si scrive niente
				return;
			}

			try
			{
				if (daCancellare.Count > 0)
				{
					// DeleteObjects e' idempotente: una chiave già assente non e' un errore, ed e' il caso
					// normale quando una corsa precedente e' stata interrotta dopo la rimozione e prima
					// dell'aggiornamento della coda.
					var request = new DeleteObjectsRequest
					{
						BucketName = target.Bucket,
						Objects = daCancellare.Select(b => new KeyVersion { Key = b.Path }).ToList(),
						Quiet = true
					};
					await s3.DeleteObjectsAsync(request, ct);
				}

				foreach (var riga in daRaffreddare)
					await ChangeStorageClassAsync(s3, target, riga.Path, ct);

				await ExecuteAsync(connection, """
					UPDATE privacy.privacy_file_deletions
					   SET executed_at = now(), attempts = attempts + 1, updated_at = now(),
					       last_error = NULL, last_error_at = NULL
					 WHERE id = ANY(@ids)
					""", ct, ("@ids", batch.Select(b => b.Id).ToArray()));

				// Solo le cancellazioni valorizzano purged_at: un oggetto raffreddato c'e' ancora.
				await MarkAssetsPurgedAsync(connection, daCancellare, ct);

				report.Deleted += daCancellare.Count;
				report.Archived += daRaffreddare.Count;

				if (daCancellare.Count > 0)
					log.LogInformation("'{Origin}' su {Bucket}: rimossi {Count} oggetti.",
						origin, target.Bucket, daCancellare.Count);
				if (daRaffreddare.Count > 0)
					log.LogInformation("'{Origin}' su {Bucket}: {Count} oggetti spostati su {Class}.",
						origin, target.Bucket, daRaffreddare.Count, target.ArchiveStorageClass);
			}
			catch (Exception ex)
			{
				var message = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
				report.Failed += batch.Count;
				report.Errors.Add($"{origin}: {message}");

				// Il tentativo si registra sulla riga: e' l'unica cosa che distingue "non ancora fatto"
				// da "non si riesce", e oltre MaxAttempts la riga smette di ritentarsi da sola.
				await ExecuteAsync(connection, """
					UPDATE privacy.privacy_file_deletions
					   SET attempts = attempts + 1, last_error = @err, last_error_at = now(), updated_at = now()
					 WHERE id = ANY(@ids)
					""", ct, ("@err", message), ("@ids", batch.Select(b => b.Id).ToArray()));

				log.LogError(ex, "'{Origin}' su {Bucket}: lotto di {Count} non rimosso. {Message}",
					origin, target.Bucket, batch.Count, ex.Message);
				return;   // non insistere sullo stesso errore per tutta la coda
			}
		}
	}

	/// <summary>
	/// Una riga della coda, pronta da eseguire. Un record e non una tupla: sei membri posizionali,
	/// di cui tre annullabili e due stringhe adiacenti, sono un invito a scambiarne due.
	/// </summary>
	/// <param name="Action">
	/// <c>delete_object</c> oppure <c>archive_object</c>. ⚠️ È il membro che decide se l'oggetto viene
	/// rimosso o soltanto spostato di classe: non si ignora.
	/// </param>
	private sealed record QueuedAction(
		long Id, string Path, string Action, string? AssetSchema, string? AssetTable, long? AssetId);

	/// <summary>
	/// Segna <c>purged_at</c> sulle righe di asset il cui oggetto e' stato appena rimosso, per le
	/// tabelle che hanno quella colonna.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Chiude il giro su <c>privacy.privacy_archive_manifests</c>: l'inventario degli archivi non si
	/// cancella mai - la riga e' la prova di aver rimosso l'oggetto nei termini - quindi l'unico modo
	/// che ha di dire «questo non c'e' piu'» e' <c>purged_at</c>. Senza questo aggiornamento la riga
	/// resterebbe per sempre nell'indice «archivi da rimuovere» e la colonna sarebbe sempre NULL.
	/// </para>
	/// <para>
	/// La regola e' <b>generica e dichiarata dallo schema</b>, non un caso speciale per Privacy: si
	/// aggiorna qualunque tabella di asset che abbia una colonna <c>purged_at</c>. Le tabelle di media
	/// non ce l'hanno - loro usano la cancellazione logica - e vengono semplicemente saltate.
	/// </para>
	/// <para>
	/// ⚠️ Un errore qui <b>non</b> fa fallire il lotto: l'oggetto e' gia' rimosso e la coda e' gia'
	/// aggiornata, che sono i due fatti che contano. Ritentare la cancellazione per non aver potuto
	/// scrivere una data sarebbe il rimedio peggiore del male.
	/// </para>
	/// </remarks>
	private async Task MarkAssetsPurgedAsync(DbConnection connection,
		IReadOnlyCollection<QueuedAction> batch, CancellationToken ct)
	{
		var conAsset = batch.Where(b => b.AssetSchema is not null && b.AssetTable is not null
			&& b.AssetId is not null);

		foreach (var gruppo in conAsset.GroupBy(b => (b.AssetSchema, b.AssetTable)))
		{
			var schema = gruppo.Key.AssetSchema!;
			var table = gruppo.Key.AssetTable!;

			try
			{
				bool haColonna;
				await using (var probe = connection.CreateCommand())
				{
					probe.CommandText = """
						SELECT EXISTS (SELECT 1 FROM information_schema.columns
						                WHERE table_schema = @schema AND table_name = @table
						                  AND column_name = 'purged_at')
						""";
					AddParam(probe, "@schema", schema);
					AddParam(probe, "@table", table);
					haColonna = (bool)(await probe.ExecuteScalarAsync(ct))!;
				}

				if (!haColonna)
					continue;

				// Schema e tabella arrivano da privacy_file_deletions, dove li ha scritti il motore
				// leggendoli dal catalogo: sono identificativi noti, non input esterno. Il filtro
				// `purged_at IS NULL` rende l'aggiornamento idempotente e conserva la prima data.
				var updated = await ExecuteAsync(connection,
					$"""
					UPDATE "{schema}"."{table}"
					   SET purged_at = now(), updated_at = now()
					 WHERE id = ANY(@ids) AND purged_at IS NULL
					""", ct, ("@ids", gruppo.Select(b => b.AssetId!.Value).ToArray()));

				if (updated > 0)
					log.LogInformation("{Schema}.{Table}: {Count} righe segnate come rimosse dallo storage.",
						schema, table, updated);
			}
			catch (Exception ex)
			{
				log.LogWarning(ex, "{Schema}.{Table}: purged_at non aggiornato. Gli oggetti SONO stati "
					+ "rimossi e la coda e' aggiornata; resta solo la data mancante. {Message}",
					schema, table, ex.Message);
			}
		}
	}

	/// <summary>
	/// Sposta un oggetto su una classe di storage piu' fredda, lasciandolo dov'e'.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Su S3 la classe di un oggetto non si cambia con una modifica: si <b>ricopia l'oggetto su se
	/// stesso</b> dichiarando la classe nuova. <c>MetadataDirective = COPY</c> conserva i metadati
	/// esistenti — senza, il content type e le intestazioni si perdono e un PDF torna indietro come
	/// <c>application/octet-stream</c>.
	/// </para>
	/// <para>
	/// ⚠️ Non e' una cancellazione e non deve diventarlo per errore: l'oggetto resta, con la stessa
	/// chiave. È il motivo per cui questa operazione ha un metodo suo invece di un ramo dentro quello
	/// che cancella.
	/// </para>
	/// <para>
	/// ⚠️ Non e' del tutto idempotente come la <c>DELETE</c>: ricopiare un oggetto gia' in classe
	/// fredda rifa' la copia e riazzera il minimo di fatturazione. Non e' un problema perche' la coda
	/// non riaccoda un'azione gia' eseguita, ma e' il motivo per cui questa non si ritenta alla
	/// leggera.
	/// </para>
	/// </remarks>
	private async Task ChangeStorageClassAsync(IAmazonS3 s3, StorageDeletionTarget target,
		string key, CancellationToken ct)
	{
		var request = new CopyObjectRequest
		{
			SourceBucket = target.Bucket,
			SourceKey = key,
			DestinationBucket = target.Bucket,
			DestinationKey = key,
			StorageClass = S3StorageClass.FindValue(target.ArchiveStorageClass),
			MetadataDirective = S3MetadataDirective.COPY,
		};

		await s3.CopyObjectAsync(request, ct);
		log.LogDebug("'{Key}' spostato su {Class}.", key, target.ArchiveStorageClass);
	}

	private static AmazonS3Client BuildClient(StorageDeletionTarget target)
		=> S3ClientFactory.Create(target.Region, target.EndpointUrl, target.AccessKeyId, target.SecretAccessKey);

	private static async Task<long> ScalarAsync(DbConnection connection, string sql, CancellationToken ct,
		params (string Name, object Value)[] ps)
	{
		await using var cmd = connection.CreateCommand();
		cmd.CommandText = sql;
		foreach (var (n, v) in ps)
			AddParam(cmd, n, v);
		return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
	}

	/// <summary>Esegue e restituisce le righe toccate: i chiamanti che non le usano le ignorano.</summary>
	private static async Task<int> ExecuteAsync(DbConnection connection, string sql, CancellationToken ct,
		params (string Name, object Value)[] ps)
	{
		await using var cmd = connection.CreateCommand();
		cmd.CommandText = sql;
		foreach (var (n, v) in ps)
			AddParam(cmd, n, v);
		return await cmd.ExecuteNonQueryAsync(ct);
	}

	private static void AddParam(DbCommand cmd, string name, object? value)
	{
		var p = cmd.CreateParameter();
		p.ParameterName = name;
		p.Value = value ?? DBNull.Value;
		cmd.Parameters.Add(p);
	}
}

/// <summary>Il job che svuota la coda, legato al <c>DbContext</c> del modulo che possiede la coda.</summary>
public interface IFileDeletionQueueJob
{
	Task<ReconcileReport> RunAsync(bool apply, CancellationToken ct);
}

internal sealed class FileDeletionQueueJob<TContext>(TContext context, StorageDeletionReconciler reconciler)
	: IFileDeletionQueueJob
	where TContext : DbContext
{
	public Task<ReconcileReport> RunAsync(bool apply, CancellationToken ct)
		=> reconciler.RunAsync(context.Database.GetDbConnection(), apply, ct);
}

public static class FileDeletionRegistration
{
	/// <summary>
	/// Registra il riconciliatore e il job della coda. Va chiamata dal <b>solo</b> modulo che possiede
	/// la tabella della coda: la coda è una, non una per modulo.
	/// </summary>
	public static IServiceCollection AddFileDeletionQueue<TContext>(
		this IServiceCollection services, StorageDeletionOptions options)
		where TContext : DbContext
	{
		services.AddSingleton(options);
		services.AddSingleton<StorageDeletionReconciler>();
		services.AddScoped<IFileDeletionQueueJob>(sp => new FileDeletionQueueJob<TContext>(
			sp.GetRequiredService<TContext>(), sp.GetRequiredService<StorageDeletionReconciler>()));
		return services;
	}
}
