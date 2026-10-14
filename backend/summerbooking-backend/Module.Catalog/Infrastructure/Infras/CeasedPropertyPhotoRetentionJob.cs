using System.Data;
using System.Data.Common;
using System.Diagnostics;
using BuildingBlocks.Infrastructure.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Module.Catalog.Infrastructure.Infras;

/// <summary>
/// La policy delle foto degli stabilimenti <b>cessati</b>: accoda alla cancellazione le foto oltre le
/// prime tre, mai la copertina.
/// </summary>
/// <remarks>
/// <para>
/// <b>Perché questa policy ha un job scritto a mano e le altre sessanta no.</b> Il motore generico
/// sa applicare qualunque policy il cui perimetro sia esprimibile come confronto su una colonna —
/// dal 2026-10-05 anche con <c>target_filter</c>. Questa non lo è:
/// </para>
/// <blockquote>
/// «le prime 3 foto per <c>sort_order</c> più la copertina restano pubblicate <b>per sempre</b>, le
/// altre seguono la cessazione» (decisione del titolare).
/// </blockquote>
/// <para>
/// Sono due condizioni che nessuna sintassi di filtro ragionevole porta: «escludi le prime N secondo
/// un <b>ordinamento</b>» e «escludi quella puntata da <c>bathhouses.main_photo_id</c>, che sta su
/// <b>un'altra tabella</b>». Un perimetro approssimato qui cancellerebbe la scheda pubblica che il
/// titolare ha chiesto esplicitamente di conservare, e su un file l'errore non si annulla.
/// </para>
/// <para>
/// ⚠️ <b>Il job non caba nessun tempo.</b> Legge la sua policy da
/// <c>privacy.privacy_retention_policies</c> e ne prende durata, unità e <c>is_dry_run_only</c>: nel
/// codice sta <b>solo il perimetro</b>. Se la policy non c'è, è disattivata o è cancellata
/// logicamente, il job non fa niente e lo dice. Cablare anche i 24 mesi significherebbe tornare a
/// dover fare una release per cambiare un numero.
/// </para>
/// <para>
/// ⚠️ <b>Non cancella niente su S3.</b> Come tutte le azioni su storage, <b>accoda</b> in
/// <c>privacy.privacy_file_deletions</c>: una <c>DELETE</c> su S3 non partecipa alla transazione del
/// database.
/// </para>
/// </remarks>
public sealed class CeasedPropertyPhotoRetentionJob(
	// `DbContext` nudo e' quello di Catalog: il namespace vince sulla using di EF Core, ed e' la
	// convenzione del progetto. Scriverlo qualificato non si puo':
	// la classe `Module` del modulo oscura il namespace `Module`.
	DbContext context,
	ILogger<CeasedPropertyPhotoRetentionJob> log) : IBespokeRetentionJob
{
	/// <summary>
	/// La policy implementata: <c>catalog · media_assets · property_ceased · 24 mesi · delete_object</c>.
	/// </summary>
	/// <remarks>
	/// ⚠️ Il motore <b>salta</b> questa policy — la sua <c>target_filter</c> è NULL di proposito, e il
	/// cancello sul perimetro dichiarato la ferma. È quello che evita che policy e job facciano lo
	/// stesso lavoro con due perimetri diversi: se un domani qualcuno le dichiarasse un perimetro, il
	/// motore comincerebbe a eseguirla <b>oltre</b> a questo job.
	/// </remarks>
	public Guid PolicyPublicId { get; } = Guid.Parse("01a0ff0f-7c00-7458-a66d-67207436a34b");

	public string Name => "Foto degli stabilimenti cessati (oltre le prime 3, mai la copertina)";

	public async Task<BespokeRetentionOutcome> RunAsync(RetentionRunOptions options, CancellationToken ct)
	{
		var cronometro = Stopwatch.StartNew();
		var esito = new BespokeRetentionOutcome
		{
			JobName = Name,
			SchemaName = "catalog",
			TableName = "media_assets",
		};

		var connection = context.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
			await connection.OpenAsync(ct);

		try
		{
			await EseguiAsync(connection, esito, options, ct);
		}
		catch (Exception ex)
		{
			esito.Status = PolicyRunStatus.Failed;
			esito.Error = Descrivi(ex);
			log.LogError(ex, "[{Job}] fallito: {Message}", Name, ex.Message);
		}

		Chiudi(esito, cronometro);

		// Il verbale si scrive SEMPRE: quando il job e' riuscito, quando si e' fermato e quando e'
		// fallito. E' il posto da cui una persona legge com'e' andata una corsa, e un job muto e'
		// indistinguibile da un job che non e' partito.
		//
		// ⚠️ E' il motivo per cui il lavoro sta in un metodo a parte invece che qui dentro: la prima
		// versione aveva i `return` anticipati in questo metodo - «policy assente», «zero righe»,
		// «prova» - e ognuno di essi scavalcava questa riga. Il risultato era che il job NON scriveva
		// niente nel caso piu' comune, quello di zero righe, e il verbale di una corsa aveva 60 righe
		// invece di 61 senza che niente lo segnalasse. Trovato verificando il conteggio, non leggendo.
		await ScriviVerbaleAsync(connection, esito, options, ct);
		return esito;
	}

	/// <summary>
	/// Il lavoro vero. Puo' uscire in qualunque punto: il verbale lo scrive <see cref="RunAsync"/>
	/// <b>dopo</b>, qualunque cosa succeda qui.
	/// </summary>
	private async Task EseguiAsync(DbConnection connection, BespokeRetentionOutcome esito,
		RetentionRunOptions options, CancellationToken ct)
	{
		var policy = await LeggiPolicyAsync(connection, options, ct);
		if (policy is null)
		{
			esito.Status = PolicyRunStatus.Skipped;
			esito.SkipReason = $"la policy {PolicyPublicId} non esiste, non e' attiva o e' stata "
				+ "cancellata: il job non inventa una durata";
			return;
		}

		if (SeasonBoundary.IsSeasonal(policy.Value.Unit) && !options.IncludeSeasonal)
		{
			esito.Status = PolicyRunStatus.Skipped;
			esito.SkipReason = "durata in stagioni: applicabile solo dal job annuale";
			return;
		}

		var cutoff = SeasonBoundary.CutoffFor(options.NowUtc, policy.Value.Unit, policy.Value.Value);
		esito.CutoffAt = cutoff;

		esito.MatchedRows = await ContaAsync(connection, cutoff, ct);
		if (esito.MatchedRows == 0)
			return;

		// is_dry_run_only e' la decisione di una persona: il job non la scavalca nemmeno con --apply.
		var scrive = options.Apply && !policy.Value.IsDryRunOnly;
		if (!scrive)
		{
			log.LogInformation("[{Job}] prova: {Rows} foto da accodare, cessazione oltre {Cutoff:yyyy-MM-dd}{Why}",
				Name, esito.MatchedRows, cutoff,
				options.Apply && policy.Value.IsDryRunOnly ? " (policy marcata is_dry_run_only)" : "");
			return;
		}

		esito.AffectedRows = await AccodaAsync(connection, policy.Value.Id, cutoff, policy.Value.LegalBasis, ct);
		log.LogInformation("[{Job}] accodate {Rows} foto in privacy_file_deletions "
			+ "(nessun oggetto ancora rimosso).", Name, esito.AffectedRows);
	}

	private async Task ScriviVerbaleAsync(DbConnection connection, BespokeRetentionOutcome esito,
		RetentionRunOptions options, CancellationToken ct)
	{
		var policy = await LeggiPolicyAsync(connection, options, ct);
		if (policy is null)
			return;   // senza policy non c'e' nessun policy_id da scrivere, e la FK lo pretende

		var entry = new RetentionRunLog.Entry(
			RunId: options.RunId,
			PolicyId: policy.Value.Id,
			SchemaName: esito.SchemaName,
			TableName: esito.TableName,
			Action: "delete_object",
			CutoffAt: esito.CutoffAt,
			IsDryRun: !(options.Apply && !policy.Value.IsDryRunOnly),
			MatchedRows: esito.MatchedRows,
			AffectedRows: esito.AffectedRows,
			Status: esito.Status switch
			{
				PolicyRunStatus.Succeeded => "succeeded",
				PolicyRunStatus.Skipped => "skipped",
				_ => "failed"
			},
			Detail: esito.Error ?? esito.SkipReason,
			StartedAt: options.NowUtc,
			DurationMs: (long)esito.Duration.TotalMilliseconds);

		var errore = await RetentionRunLog.WriteAsync(connection, entry, 1800, ct);
		if (errore is not null)
			log.LogError("[{Job}] esito non registrato su privacy_retention_runs: {Message}", Name, errore);
	}

	// =============================================================================================

	/// <summary>
	/// Il perimetro, in una sola stringa usata <b>sia</b> dal conteggio <b>sia</b> dall'accodamento.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Una sola definizione di proposito: se il conteggio e l'accodamento avessero due predicati, il
	/// report direbbe un numero e il job ne toccherebbe un altro — ed è il report che una persona legge
	/// per autorizzare l'esecuzione.
	/// </para>
	/// <para>Le quattro condizioni, e perché ognuna c'è:</para>
	/// <list type="number">
	/// <item><b>posizione &gt; 3</b> — le prime tre per <c>sort_order</c> restano. L'ordinamento
	/// secondario su <c>bp.id</c> rende la posizione deterministica anche quando due foto hanno lo
	/// stesso <c>sort_order</c>: senza, due corse potrebbero scegliere due terzine diverse;</item>
	/// <item><b>non è la copertina</b> — <c>bathhouses.main_photo_id</c>. Può non essere fra le prime
	/// tre per <c>sort_order</c>, quindi la condizione 1 non la copre;</item>
	/// <item><b>lo stabilimento ha cessato</b> prima del taglio, derivato da
	/// <see cref="PropertyCeasedPredicate"/>. ⚠️ Si passa per <c>properties.public_id</c>:
	/// <c>bathhouses.property_id</c> è un <c>int8</c> e punta all'<c>id</c>, mentre identity usa
	/// l'<c>uuid</c>;</item>
	/// <item><b>non è già in coda per la stessa azione</b> — l'indice unico della coda protegge solo le
	/// righe pendenti, quindi dopo l'esecuzione la protezione sparisce e senza questa condizione il job
	/// riaccoderebbe le stesse foto a ogni corsa.</item>
	/// </list>
	/// <para>
	/// ⚠️ <c>bathhouse_photos.deleted_at IS NULL</c> sta nella finestra che calcola la posizione, non
	/// fuori: una foto rimossa logicamente non deve occupare uno dei tre posti protetti, altrimenti
	/// basta cancellare la prima foto per salvare la quarta.
	/// </para>
	/// </remarks>
	/// <remarks>
	/// ⚠️ <c>internal</c> e non <c>private</c> perche' <c>Module.Catalog.Tests</c> lo esegue contro un
	/// PostgreSQL vero. E' il solo modo di verificarlo: il legacy non ha mai avuto gli abbonamenti
	/// (Q10 del 2026-10-02), quindi sui dati migrati questo perimetro trova zero righe per costruzione,
	/// e un doppio proverebbe solo che il doppio funziona. Il test costruisce la SQL da QUESTO metodo,
	/// non da una copia: una copia divergerebbe al primo join cambiato.
	/// </remarks>
	internal static string Perimetro() => $"""
		  FROM (SELECT bp.media_asset_id,
		               bp.bathhouse_id,
		               row_number() OVER (PARTITION BY bp.bathhouse_id
		                                      ORDER BY bp.sort_order, bp.id) AS posizione
		          FROM catalog.bathhouse_photos bp
		         WHERE bp.deleted_at IS NULL) o
		  JOIN catalog.media_assets m   ON m.id = o.media_asset_id
		  JOIN catalog.bathhouses b     ON b.id = o.bathhouse_id
		  JOIN catalog.properties p     ON p.id = b.property_id
		 WHERE o.posizione > 3
		   AND (b.main_photo_id IS NULL OR b.main_photo_id <> m.id)
		   AND {PropertyCeasedPredicate.For("p.\"public_id\"")}
		   AND NOT EXISTS (SELECT 1
		                     FROM privacy.privacy_file_deletions d
		                    WHERE d.asset_schema = 'catalog'
		                      AND d.asset_table  = 'media_assets'
		                      AND d.asset_id     = m.id
		                      AND d.action       = 'delete_object'::privacy.retention_action)
		""";

	private async Task<long> ContaAsync(DbConnection connection, DateTime cutoff, CancellationToken ct)
	{
		await using var cmd = Comando(connection, "SELECT count(*)\n" + Perimetro());
		Param(cmd, "@cutoff", cutoff);
		return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
	}

	private async Task<long> AccodaAsync(DbConnection connection, long policyId, DateTime cutoff,
		string legalBasis, CancellationToken ct)
	{
		var sql = $"""
			INSERT INTO privacy.privacy_file_deletions
			  (public_id, asset_schema, asset_table, asset_id, storage_origin, storage_path, byte_size,
			   action, policy_id, reason, decided_at, created_at, updated_at)
			SELECT gen_random_uuid(), 'catalog', 'media_assets', m.id, m.origin::text, m.path, m.byte_size,
			       'delete_object'::privacy.retention_action, @policy, @reason, now(), now(), now()
			{Perimetro()}
			ON CONFLICT ("storage_origin", "storage_path") WHERE executed_at IS NULL DO NOTHING
			""";

		await using var cmd = Comando(connection, sql);
		Param(cmd, "@cutoff", cutoff);
		Param(cmd, "@policy", policyId);
		Param(cmd, "@reason", "Retention: foto di stabilimento cessato, oltre le prime tre per "
			+ "sort_order e diversa dalla copertina. " + legalBasis);
		return await cmd.ExecuteNonQueryAsync(ct);
	}

	/// <summary>
	/// Legge durata, unità e <c>is_dry_run_only</c> dalla policy. Gli enum con <c>::text</c>: questa è
	/// la connessione di Catalog, e gli enum di <c>privacy</c> non vi sono mappati.
	/// </summary>
	private async Task<(long Id, short Value, string Unit, bool IsDryRunOnly, string LegalBasis)?>
		LeggiPolicyAsync(DbConnection connection, RetentionRunOptions options, CancellationToken ct)
	{
		await using var cmd = Comando(connection, """
			SELECT id, duration_value, duration_unit::text, is_dry_run_only, legal_basis
			  FROM privacy.privacy_retention_policies
			 WHERE public_id = @id AND is_active AND deleted_at IS NULL
			""");
		Param(cmd, "@id", PolicyPublicId);

		await using var reader = await cmd.ExecuteReaderAsync(ct);
		if (!await reader.ReadAsync(ct))
			return null;

		return (reader.GetInt64(0), reader.GetInt16(1), reader.GetString(2),
			reader.GetBoolean(3), reader.GetString(4));
	}

	private DbCommand Comando(DbConnection connection, string sql)
	{
		var cmd = connection.CreateCommand();
		cmd.CommandText = sql;
		// Lo stesso tetto del motore, e per lo stesso motivo: il default di Npgsql e' 30 secondi, che
		// non e' una durata da job batch.
		cmd.CommandTimeout = 1800;
		return cmd;
	}

	private static void Param(DbCommand cmd, string name, object? value)
	{
		var p = cmd.CreateParameter();
		p.ParameterName = name;
		p.Value = value ?? DBNull.Value;
		cmd.Parameters.Add(p);
	}

	private static void Chiudi(BespokeRetentionOutcome esito, Stopwatch cronometro)
	{
		cronometro.Stop();
		esito.Duration = cronometro.Elapsed;
	}

	/// <summary>Il messaggio e quello delle cause: su Npgsql il dettaglio utile sta nella seconda.</summary>
	private static string Descrivi(Exception ex)
	{
		var sb = new System.Text.StringBuilder(ex.Message);
		var inner = ex.InnerException;
		while (inner is not null)
		{
			sb.Append(" | ").Append(inner.Message);
			inner = inner.InnerException;
		}

		return sb.ToString();
	}
}
