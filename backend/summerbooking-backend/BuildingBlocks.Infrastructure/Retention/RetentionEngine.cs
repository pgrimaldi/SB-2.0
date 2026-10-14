using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>
/// Applica le policy di retention dichiarate in <c>privacy.privacy_retention_policies</c> alle tabelle
/// di <b>uno</b> schema, sulla connessione di quello schema.
/// </summary>
/// <remarks>
/// <para>
/// Vive in <c>BuildingBlocks.Infrastructure</c> e non in un modulo perché deve servirli tutti, e per
/// la stessa ragione non conosce nessuna entità: lavora su <see cref="DbConnection"/> e nomi di tabella. È la forma che la
/// tabella dichiarativa presuppone — «cambiare una durata è un UPDATE su una riga di policy, non una
/// release» — e l'unica che non richiede diciannove job scritti a mano.
/// </para>
/// <para>
/// <b>Non si ferma al primo errore.</b> Ogni policy è valutata per conto suo e il suo esito finisce in
/// <c>privacy.privacy_retention_runs</c>: una tabella con una chiave esterna che impedisce la
/// cancellazione non deve impedire alle altre 58 policy di girare. È il motivo per cui questo motore
/// raccoglie invece di propagare.
/// </para>
/// </remarks>
public sealed class RetentionEngine(IArchiveStore archive, ILogger<RetentionEngine> log)
{
	/// <summary>
	/// Tetto di durata applicato a ogni istruzione della corsa. Impostato una volta in
	/// <see cref="RunAsync"/> e usato da <c>Command</c>: il default di Npgsql e' 30 secondi, e una
	/// cancellazione di 650 righe con quattordici chiavi esterne addosso ne impiega 43.
	/// </summary>
	private int _statementTimeout = 1800;

	/// <summary>
	/// L'unico posto da cui nascono i comandi, perche' il tetto di durata non si possa dimenticare su
	/// uno di essi. Dimenticarlo su quello della cancellazione e' esattamente l'errore che ha fatto
	/// fallire la prima corsa reale <b>dopo</b> aver scritto l'archivio.
	/// </summary>
	private DbCommand Command(DbConnection connection, string sql)
	{
		var cmd = connection.CreateCommand();
		cmd.CommandText = sql;
		cmd.CommandTimeout = _statementTimeout;
		return cmd;
	}

	/// <summary>Valore scritto dalle policy <c>anonymize</c> sulle colonne testuali non annullabili.</summary>
	private const string AnonToken = "anonimizzato";

	public async Task<SchemaRetentionReport> RunAsync(
		DbConnection connection, string schema, RetentionRunOptions options, CancellationToken ct)
	{
		var report = new SchemaRetentionReport { Schema = schema };

		_statementTimeout = options.StatementTimeoutSeconds;

		if (connection.State != ConnectionState.Open)
			await connection.OpenAsync(ct);

		var policies = await ReadPoliciesAsync(connection, schema, ct);
		if (policies.Count == 0)
		{
			log.LogInformation("[{Schema}] nessuna policy attiva: niente da fare.", schema);
			return report;
		}

		log.LogInformation("[{Schema}] {Count} policy attive, modalita' {Mode}.",
			schema, policies.Count, options.Apply ? "ESECUZIONE" : "prova (nessuna scrittura)");

		foreach (var policy in policies)
		{
			ct.ThrowIfCancellationRequested();
			var outcome = new PolicyOutcome { Policy = policy };
			var sw = Stopwatch.StartNew();
			try
			{
				await EvaluateAsync(connection, policy, outcome, options, ct);
			}
			catch (Exception ex)
			{
				outcome.Status = PolicyRunStatus.Failed;
				outcome.Error = Describe(ex);
				log.LogError(ex, "[{Policy}] fallita: {Message}", policy, ex.Message);
			}
			finally
			{
				sw.Stop();
				outcome.Duration = sw.Elapsed;
				report.Outcomes.Add(outcome);
				if (!outcome.SuppressRunLog)
					await RecordRunAsync(connection, schema, policy, outcome, options, ct);
			}
		}

		return report;
	}

	// --- lettura delle regole -----------------------------------------------------------------

	/// <summary>
	/// Le policy del proprio schema. Le colonne enum sono proiettate con <c>::text</c>: gli enum nativi
	/// di <c>privacy</c> non sono mappati sulle connessioni degli altri moduli, e questa query deve
	/// funzionare da tutti.
	/// </summary>
	private async Task<List<RetentionPolicyRecord>> ReadPoliciesAsync(
		DbConnection connection, string schema, CancellationToken ct)
	{
		const string sql = """
			SELECT id, public_id, schema_name, table_name, field_names, reference_date_column,
			       duration_value, duration_unit::text, action::text, only_if_unreferenced,
			       reference_event::text, storage_prefix, storage_date_source::text,
			       basis_source::text, legal_basis, is_dry_run_only, target_filter::text,
			       linked_asset::text
			  FROM privacy.privacy_retention_policies
			 WHERE schema_name = @schema AND is_active AND deleted_at IS NULL
			 ORDER BY table_name,
			          storage_prefix NULLS FIRST,
			          -- L'ORDINE FRA POLICY DELLA STESSA TABELLA NON E' INDIFFERENTE: prima si minimizza
			          -- (azzeramento e anonimizzazione), poi si archivia, poi si cancella. Al contrario,
			          -- l'archivio di booking.reservations conterrebbe le note dei clienti che la policy
			          -- a una stagione deve azzerare - e quelle note possono contenere dati sulla salute
			          -- (art. 9 GDPR). L'archivio non si riscrive: va minimizzato prima di partire.
			          CASE action::text
			            WHEN 'null_fields'    THEN 0
			            WHEN 'anonymize'      THEN 1
			            WHEN 'archive_cold'   THEN 2
			            WHEN 'archive_object' THEN 3
			            WHEN 'delete_object'  THEN 4
			            WHEN 'delete_row'     THEN 5
			            ELSE 6
			          END,
			          duration_value
			""";

		await using var cmd = Command(connection, sql);
		AddParam(cmd, "@schema", schema);

		var rows = new List<RetentionPolicyRecord>();
		await using var reader = await cmd.ExecuteReaderAsync(ct);
		while (await reader.ReadAsync(ct))
		{
			rows.Add(new RetentionPolicyRecord
			{
				Id = reader.GetInt64(0),
				PublicId = reader.GetGuid(1),
				SchemaName = reader.GetString(2),
				TableName = reader.GetString(3),
				FieldNames = reader.IsDBNull(4) ? [] : (string[])reader.GetValue(4),
				ReferenceDateColumn = reader.GetString(5),
				DurationValue = reader.GetInt16(6),
				DurationUnit = reader.GetString(7),
				Action = reader.GetString(8),
				OnlyIfUnreferenced = reader.GetBoolean(9),
				ReferenceEvent = reader.IsDBNull(10) ? null : reader.GetString(10),
				StoragePrefix = reader.IsDBNull(11) ? null : reader.GetString(11),
				StorageDateSource = reader.IsDBNull(12) ? null : reader.GetString(12),
				BasisSource = reader.GetString(13),
				LegalBasis = reader.GetString(14),
				IsDryRunOnly = reader.GetBoolean(15),
				TargetFilter = reader.IsDBNull(16) ? null : reader.GetString(16),
				LinkedAsset = reader.IsDBNull(17) ? null : reader.GetString(17)
			});
		}

		return rows;
	}

	// --- valutazione di una policy ------------------------------------------------------------

	private async Task EvaluateAsync(DbConnection connection, RetentionPolicyRecord policy,
		PolicyOutcome outcome, RetentionRunOptions options, CancellationToken ct)
	{
		// Una policy applicata da un job dedicato non si valuta qui, e NON si mette nel verbale: ci
		// finirebbero due righe per la stessa policy nella stessa corsa, una «saltata» da questo motore
		// e una dal job che la applica davvero.
		if (options.PoliciesHandledElsewhere.Contains(policy.PublicId))
		{
			outcome.Status = PolicyRunStatus.Skipped;
			outcome.SkipReason = "applicata da un job dedicato";
			outcome.SuppressRunLog = true;
			log.LogInformation("[{Policy}] delegata a un job dedicato: il motore non la valuta.", policy);
			return;
		}

		// Il cancello sulle stagioni va PRIMA di ogni altra cosa: dice se questo job puo' applicare
		// questa policy, e non dipende dal tipo di azione. Messo dopo il ramo delle azioni su storage,
		// una policy su storage con durata in stagioni lo scavalcherebbe.
		if (SeasonBoundary.IsSeasonal(policy.DurationUnit) && !options.IncludeSeasonal)
		{
			outcome.Status = PolicyRunStatus.Skipped;
			outcome.SkipReason = "durata in stagioni: applicabile solo dal job annuale";
			return;
		}

		// Una policy su prefisso guarda il bucket, non il database: non ha nessuna riga da cui partire e
		// la deve lavorare un job che elenca lo storage. Non e' una cosa che questo motore puo' fare, e
		// soprattutto non e' una cosa da improvvisare: cancellare per prefisso senza una riga che
		// trattenga i file e' la forma piu' pericolosa di tutta la politica.
		if (policy.IsPrefixPolicy)
		{
			outcome.Status = PolicyRunStatus.Skipped;
			outcome.SkipReason = "policy su prefisso di storage: nessuna riga di database da valutare, "
				+ "serve un job che elenchi il bucket";
			return;
		}

		// Un'azione su storage riferita a una TABELLA si puo' valutare qui: le righe che nominano i file
		// stanno a database. Ma non si esegue in linea - si ACCODA in privacy_file_deletions, perche' una
		// DELETE su S3 non partecipa alla transazione del database.
		if (policy.IsStorageAction)
		{
			await EnqueueStorageAsync(connection, policy, outcome, options, ct);
			return;
		}

		// Una decorrenza da evento va derivata da un altro schema (la cessazione dello stabilimento si
		// ricava da identity.account_subscriptions). Non e' una query che questo motore puo' fare.
		if (!string.IsNullOrEmpty(policy.ReferenceEvent))
		{
			outcome.Status = PolicyRunStatus.Skipped;
			outcome.SkipReason = $"decorrenza dall'evento '{policy.ReferenceEvent}': va derivata fuori da questo motore";
			return;
		}

		if (string.IsNullOrEmpty(policy.ReferenceDateColumn))
		{
			outcome.Status = PolicyRunStatus.Skipped;
			outcome.SkipReason = "nessuna colonna di decorrenza e nessun evento: niente da cui contare";
			return;
		}

		var table = await ResolvePhysicalTableAsync(connection, policy.SchemaName, policy.TableName, ct);
		if (table is null)
		{
			outcome.Status = PolicyRunStatus.Failed;
			outcome.Error = $"tabella non trovata: ne '{policy.SchemaName}.{policy.TableName}' "
				+ $"ne '{policy.SchemaName}.{policy.SchemaName}_{policy.TableName}'";
			return;
		}

		outcome.PhysicalTable = table;

		var columns = await ReadColumnsAsync(connection, policy.SchemaName, table, ct);
		if (!columns.ContainsKey(policy.ReferenceDateColumn))
		{
			outcome.Status = PolicyRunStatus.Failed;
			outcome.Error = $"colonna di decorrenza assente: {policy.SchemaName}.{table}.{policy.ReferenceDateColumn}";
			return;
		}

		var cutoff = SeasonBoundary.CutoffFor(options.NowUtc, policy.DurationUnit, policy.DurationValue);
		outcome.CutoffAt = cutoff;

		// Il perimetro entra nell'UNICO punto in cui si costruisce il predicato delle azioni su riga:
		// da qui scende in tutte e quattro - conteggio, DELETE, UPDATE, export dell'archivio - e non
		// c'e' nessun ramo che possa dimenticarselo.
		var (filtroRighe, erroreFiltro) = TargetFilter.Build(policy.TargetFilter, columns.Keys.ToHashSet(),
			alias: null, Q(policy.SchemaName, table));
		if (erroreFiltro is not null)
		{
			outcome.Status = PolicyRunStatus.Failed;
			outcome.Error = erroreFiltro;
			return;
		}

		var where = $"\"{policy.ReferenceDateColumn}\" < @cutoff{filtroRighe}";
		outcome.MatchedRows = await CountAsync(connection, policy.SchemaName, table, where, cutoff, ct);
		if (outcome.MatchedRows == 0)
			return;

		// is_dry_run_only e' la decisione di una persona: il motore non la scavalca nemmeno con --apply.
		var write = options.Apply && !policy.IsDryRunOnly;
		if (!write)
		{
			log.LogInformation("[{Policy}] prova: {Rows} righe oltre {Cutoff:yyyy-MM-dd}{Why}",
				policy, outcome.MatchedRows, cutoff,
				options.Apply && policy.IsDryRunOnly ? " (policy marcata is_dry_run_only)" : "");
			return;
		}

		switch (policy.Action)
		{
			case "delete_row":
				outcome.AffectedRows = await ExecuteAsync(connection,
					$"DELETE FROM {Q(policy.SchemaName, table)} WHERE {where}", cutoff, ct);
				break;

			case "null_fields":
				outcome.AffectedRows = await NullFieldsAsync(connection, policy, table, columns, where, cutoff, outcome, ct);
				break;

			case "anonymize":
				outcome.AffectedRows = await AnonymizeAsync(connection, policy, table, columns, where, cutoff, outcome, ct);
				break;

			case "archive_cold":
				outcome.AffectedRows = await ArchiveColdAsync(connection, policy, table, where, cutoff, outcome, options, ct);
				break;

			default:
				outcome.Status = PolicyRunStatus.Failed;
				outcome.Error = $"azione non gestita dal motore sulle righe: '{policy.Action}'";
				break;
		}
	}

	/// <summary>
	/// Accoda l'azione su storage per i file che <b>seguono</b> le righe archiviate, e li inventaria.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <b>Il problema che risolve.</b> Alcune righe nominano un file che è parte del dato: la riga di
	/// <c>fiskaly.receipts</c> nomina il PDF dello scontrino. Archiviare la riga e lasciare il PDF dov'era
	/// produce due guai insieme: il PDF resta sullo storage <b>senza nessuna riga calda che lo nomini</b>,
	/// e la data da cui scade — <c>signed_at</c> — è uscita dal database insieme alla riga. Non resta
	/// niente che possa decidere quando cancellarlo.
	/// </para>
	/// <para>
	/// Due scritture, entrambe prima della cancellazione delle righe:
	/// </para>
	/// <list type="number">
	/// <item>l'<b>azione</b> dichiarata nella policy va in coda (<c>archive_object</c> lo raffredda,
	/// <c>delete_object</c> lo rimuove);</item>
	/// <item>una riga di <b>inventario</b> con <c>object_kind = 'linked_asset'</c>, così la policy
	/// decennale su <c>privacy_archive_manifests</c> cancella il file insieme all'archivio delle righe
	/// che accompagnava. È questo che rende la coppia «riga + file» una cosa sola anche dopo che la riga
	/// è uscita dal database.</item>
	/// </list>
	/// <para>
	/// ⚠️ L'inventario del file porta <c>cutoff_at</c> e <c>archived_at</c> <b>della corsa</b>, non del
	/// file: la sua scadenza decorre da quando ha lasciato il database caldo, come per l'archivio delle
	/// righe. È la stessa logica, e serve che sia la stessa o i due pezzi della coppia scadrebbero in
	/// momenti diversi.
	/// </para>
	/// </remarks>
	private async Task HandleLinkedAssetsAsync(DbConnection connection, RetentionPolicyRecord policy,
		string table, string where, DateTime cutoff, RetentionRunOptions options, CancellationToken ct)
	{
		if (string.IsNullOrWhiteSpace(policy.LinkedAsset))
			return;

		var (link, errore) = LinkedAssetSpec.Parse(policy.LinkedAsset);
		if (errore is not null)
			throw new InvalidOperationException($"linked_asset della policy {policy.PublicId}: {errore}");

		var assetTable = await ResolvePhysicalTableAsync(connection, policy.SchemaName, link!.Table, ct);
		if (assetTable is null)
			throw new InvalidOperationException(
				$"linked_asset della policy {policy.PublicId}: tabella di asset non trovata, "
				+ $"ne '{policy.SchemaName}.{link.Table}' ne '{policy.SchemaName}.{policy.SchemaName}_{link.Table}'");

		var colonne = await ReadColumnsAsync(connection, policy.SchemaName, table, ct);
		if (!colonne.ContainsKey(link.Column))
			throw new InvalidOperationException(
				$"linked_asset della policy {policy.PublicId}: la colonna '{link.Column}' non esiste "
				+ $"su {policy.SchemaName}.{table}");

		var colonneAsset = await ReadColumnsAsync(connection, policy.SchemaName, assetTable, ct);
		var originColumn = colonneAsset.ContainsKey("origin") ? "origin"
			: colonneAsset.ContainsKey("storage_origin") ? "storage_origin" : null;
		var pathColumn = colonneAsset.ContainsKey("path") ? "path"
			: colonneAsset.ContainsKey("storage_path") ? "storage_path" : null;

		if (originColumn is null || pathColumn is null)
			throw new InvalidOperationException(
				$"linked_asset della policy {policy.PublicId}: su {policy.SchemaName}.{assetTable} "
				+ "manca 'origin'/'storage_origin' oppure 'path'/'storage_path'");

		var sizeColumn = colonneAsset.ContainsKey("byte_size") ? "a.\"byte_size\"" : "NULL::bigint";

		// La giunzione e lo stesso predicato delle righe archiviate: i file che seguono sono esattamente
		// quelli nominati da QUELLE righe, non un insieme calcolato per conto suo.
		var from = $"""
			  FROM {Q(policy.SchemaName, table)} t
			  JOIN {Q(policy.SchemaName, assetTable)} a ON a."id" = t."{link.Column}"
			 WHERE {where} AND t."{link.Column}" IS NOT NULL
			""";

		await using (var cmd = Command(connection, $"""
			INSERT INTO privacy.privacy_file_deletions
			  (public_id, asset_schema, asset_table, asset_id, storage_origin, storage_path, byte_size,
			   action, policy_id, reason, decided_at, created_at, updated_at)
			SELECT gen_random_uuid(), @schema, @asset_table, a."id", a."{originColumn}"::text,
			       a."{pathColumn}", {sizeColumn}::integer, @action::privacy.retention_action, @policy,
			       @reason, now(), now(), now()
			{from}
			ON CONFLICT ("storage_origin", "storage_path") WHERE executed_at IS NULL DO NOTHING
			"""))
		{
			AddParam(cmd, "@cutoff", cutoff);
			AddParam(cmd, "@schema", policy.SchemaName);
			AddParam(cmd, "@asset_table", assetTable);
			AddParam(cmd, "@action", link.Action);
			AddParam(cmd, "@policy", policy.Id);
			AddParam(cmd, "@reason", $"Retention: il file segue le righe di {policy.SchemaName}.{table} "
				+ $"archiviate oltre {policy.DurationValue} {policy.DurationUnit}. {policy.LegalBasis}");
			var accodati = await cmd.ExecuteNonQueryAsync(ct);
			log.LogInformation("[{Policy}] {Rows} file collegati accodati come '{Action}'.",
				policy, accodati, link.Action);
		}

		await using (var cmd = Command(connection, $"""
			INSERT INTO privacy.privacy_archive_manifests
			  (public_id, run_id, policy_id, source_schema, source_table, object_kind, storage_origin,
			   storage_path, byte_size, row_count, cutoff_at, archived_at, created_at, updated_at)
			SELECT gen_random_uuid(), @run_id, @policy, @schema, @asset_table,
			       'linked_asset'::privacy.archive_object_kind, a."{originColumn}"::text, a."{pathColumn}",
			       NULLIF({sizeColumn}, 0)::bigint, NULL, @cutoff, now(), now(), now()
			{from}
			ON CONFLICT ("storage_origin", "storage_path") DO NOTHING
			"""))
		{
			AddParam(cmd, "@cutoff", cutoff);
			AddParam(cmd, "@schema", policy.SchemaName);
			AddParam(cmd, "@asset_table", assetTable);
			AddParam(cmd, "@policy", policy.Id);
			AddParam(cmd, "@run_id", options.RunId);
			var inventariati = await cmd.ExecuteNonQueryAsync(ct);
			log.LogInformation("[{Policy}] {Rows} file collegati inventariati: la policy decennale "
				+ "sull'inventario li cancellera' insieme all'archivio.", policy, inventariati);
		}
	}

	// --- le quattro azioni su riga -------------------------------------------------------------

	private async Task<long> NullFieldsAsync(DbConnection connection, RetentionPolicyRecord policy,
		string table, IReadOnlyDictionary<string, ColumnInfo> columns, string where, DateTime cutoff,
		PolicyOutcome outcome, CancellationToken ct)
	{
		var (sets, notes) = BuildNullSets(policy.FieldNames, columns);
		if (sets.Count == 0)
		{
			outcome.Status = PolicyRunStatus.Failed;
			outcome.Error = "nessun campo azzerabile: " + string.Join("; ", notes);
			return 0;
		}

		if (notes.Count > 0)
			outcome.SkipReason = "campi non azzerati: " + string.Join("; ", notes);

		// Tocca solo le righe che hanno ancora qualcosa da azzerare. Senza questa guardia la policy
		// riscrive a ogni corsa tutte le righe oltre il taglio, comprese quelle gia' azzerate:
		// misurato, 41.943 righe di booking.reservations riscritte per niente alla seconda corsa. Su un
		// job giornaliero sono 41.943 versioni di riga al giorno da far ripulire al vacuum.
		var guard = string.Join(" OR ", policy.FieldNames
			.Where(f => columns.TryGetValue(f, out var c) && c.IsNullable)
			.Select(f => $"\"{f}\" IS NOT NULL"));
		var residue = $"{where} AND ({guard})";

		// Il conteggio usa la stessa condizione, cosi' il verbale dice quanto RESTA da fare e non
		// quante righe cadono nella finestra: e' il numero su cui si decide, e i due vincoli
		// affected_rows <= matched_rows restano coerenti.
		outcome.MatchedRows = await CountAsync(connection, policy.SchemaName, table, residue, cutoff, ct);
		if (outcome.MatchedRows == 0)
			return 0;

		var sql = $"UPDATE {Q(policy.SchemaName, table)} SET {string.Join(", ", sets)} WHERE {residue}";
		return await ExecuteAsync(connection, sql, cutoff, ct);
	}

	private static (List<string> Sets, List<string> Notes) BuildNullSets(
		IEnumerable<string> fields, IReadOnlyDictionary<string, ColumnInfo> columns)
	{
		var sets = new List<string>();
		var notes = new List<string>();
		foreach (var f in fields)
		{
			if (!columns.TryGetValue(f, out var col))
			{
				notes.Add($"'{f}' non esiste");
				continue;
			}

			if (!col.IsNullable)
			{
				notes.Add($"'{f}' e' NOT NULL");
				continue;
			}

			sets.Add($"\"{f}\" = NULL");
		}

		return (sets, notes);
	}

	private async Task<long> AnonymizeAsync(DbConnection connection, RetentionPolicyRecord policy,
		string table, IReadOnlyDictionary<string, ColumnInfo> columns, string where, DateTime cutoff,
		PolicyOutcome outcome, CancellationToken ct)
	{
		var pk = await ReadPrimaryKeyAsync(connection, policy.SchemaName, table, ct);
		var sets = new List<string>();
		var notes = new List<string>();

		foreach (var f in policy.FieldNames)
		{
			if (!columns.TryGetValue(f, out var col))
			{
				notes.Add($"'{f}' non esiste");
				continue;
			}

			var expr = AnonymizeExpression(col, pk);
			if (expr is null)
			{
				notes.Add($"'{f}' di tipo {col.DataType} non ha una forma anonima definita");
				continue;
			}

			sets.Add($"\"{f}\" = {expr}");
		}

		if (sets.Count == 0)
		{
			outcome.Status = PolicyRunStatus.Failed;
			outcome.Error = "nessun campo anonimizzabile: " + string.Join("; ", notes);
			return 0;
		}

		if (notes.Count > 0)
			outcome.SkipReason = "campi non anonimizzati: " + string.Join("; ", notes);

		// Come per l'azzeramento: solo le righe non ancora anonimizzate. `IS DISTINCT FROM` e non `<>`
		// perche' su un valore NULL il confronto normale non e' vero ne' falso, e la riga sfuggirebbe.
		var guard = string.Join(" OR ", sets.Select(set =>
		{
			var i = set.IndexOf(" = ", StringComparison.Ordinal);
			return $"{set[..i]} IS DISTINCT FROM {set[(i + 3)..]}";
		}));
		var residue = $"{where} AND ({guard})";

		outcome.MatchedRows = await CountAsync(connection, policy.SchemaName, table, residue, cutoff, ct);
		if (outcome.MatchedRows == 0)
			return 0;

		var sql = $"UPDATE {Q(policy.SchemaName, table)} SET {string.Join(", ", sets)} WHERE {residue}";
		return await ExecuteAsync(connection, sql, cutoff, ct);
	}

	/// <summary>
	/// Come si anonimizza una colonna. <b>Non</b> si azzera: anonimizzare deve lasciare la riga usabile
	/// — i conteggi, i collegamenti e gli importi restano — togliendo solo la riconoscibilità.
	/// </summary>
	/// <remarks>
	/// Sulle colonne testuali il valore porta la chiave primaria (<c>anonimizzato-1234</c>) e non una
	/// costante: su una colonna con un indice unico — un'email, per dire — una costante farebbe
	/// collidere la seconda riga anonimizzata con la prima e la policy fallirebbe a metà.
	/// </remarks>
	private static string? AnonymizeExpression(ColumnInfo col, string? pk)
	{
		var token = pk is null ? $"'{AnonToken}'" : $"'{AnonToken}-' || \"{pk}\"::text";

		switch (col.DataType)
		{
			case "character varying":
			case "text":
			case "character":
				// Rispetta il limite di lunghezza: una colonna varchar(20) non accetta un token lungo.
				return col.MaxLength is > 0 and < 32 ? $"left({token}, {col.MaxLength})" : token;

			case "inet":
			case "cidr":
				// L'indirizzo IP prova l'accettazione: si tronca l'ultimo ottetto, non si cancella.
				return "network(set_masklen(\"" + col.Name + "\", 24))";

			case "date":
			case "timestamp with time zone":
			case "timestamp without time zone":
				return col.IsNullable ? "NULL" : null;

			default:
				return col.IsNullable ? "NULL" : null;
		}
	}

	/// <summary>
	/// Esporta le righe scadute su storage a oggetti, poi le cancella dal database caldo.
	/// </summary>
	/// <remarks>
	/// ⚠️ <b>L'ordine non è negoziabile</b>: prima l'archivio, poi la cancellazione. Se la scrittura su
	/// S3 fallisce non si cancella niente e la policy risulta fallita; se fallisce la cancellazione
	/// resta un oggetto in più sull'archivio, che è il verso innocuo dell'errore.
	/// </remarks>
	private async Task<long> ArchiveColdAsync(DbConnection connection, RetentionPolicyRecord policy,
		string table, string where, DateTime cutoff, PolicyOutcome outcome, RetentionRunOptions options,
		CancellationToken ct)
	{
		await archive.EnsureReadyAsync(ct);

		var keys = new List<string>();
		long totalBytes = 0;
		var part = 0;
		var written = 0L;

		// Un lotto per volta, compresso dentro il suo blocco: la prima versione teneva lo stream aperto
		// fra i lotti e lo chiudeva due volte, e tutte e tre le policy archive_cold fallivano con
		// "Cannot access a disposed object: GZipStream" DOPO aver caricato l'oggetto su S3 — lasciando
		// l'archivio scritto e le righe non cancellate. Qui l'ambito di vita degli stream e' uno solo.
		var batch = new List<string>(options.ArchiveBatchSize);

		async Task FlushBatchAsync()
		{
			var righeNelLotto = batch.Count;
			using var buffer = new MemoryStream();
			// L'ordine di chiusura conta: lo StreamWriter si chiude per primo e svuota nel GZipStream,
			// il GZipStream per secondo e scrive la propria coda nel MemoryStream. Solo dopo il buffer
			// contiene un archivio completo e rileggibile.
			using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
			await using (var writer = new StreamWriter(gzip, new UTF8Encoding(false)))
			{
				foreach (var line in batch)
					await writer.WriteLineAsync(line.AsMemory(), ct);
			}

			buffer.Position = 0;
			var key = archive.BuildKey(policy.SchemaName, table, policy.PublicId, options.NowUtc, part++);
			var result = await archive.WriteAsync(key, buffer, ct);
			keys.Add(result.Key);
			totalBytes += result.ByteSize;

			// Il manifesto si scrive subito dopo l'oggetto e PRIMA della cancellazione delle righe: e'
			// l'unico posto che lega quelle righe a quell'oggetto, e deve esistere prima che le righe
			// spariscano. Se questa INSERT fallisce, l'eccezione sale, la policy risulta fallita e le
			// righe restano al loro posto: il verso innocuo.
			await RecordManifestAsync(connection, policy, table, result, righeNelLotto, cutoff, options, ct);
			batch.Clear();
		}

		await using (var cmd = Command(connection,
			$"SELECT row_to_json(x) FROM (SELECT * FROM {Q(policy.SchemaName, table)} WHERE {where}) x"))
		{
			AddParam(cmd, "@cutoff", cutoff);

			await using var reader = await cmd.ExecuteReaderAsync(ct);
			while (await reader.ReadAsync(ct))
			{
				batch.Add(reader.GetString(0));
				written++;
				if (batch.Count >= options.ArchiveBatchSize)
					await FlushBatchAsync();
			}
		}

		if (batch.Count > 0)
			await FlushBatchAsync();

		// ⚠️ I file che seguono le righe si trattano QUI: dopo che l'archivio e' scritto e PRIMA che le
		// righe vengano cancellate. Dopo la cancellazione sarebbe troppo tardi - il legame fra la riga e
		// il suo file vive nella riga, e con la riga sparisce.
		await HandleLinkedAssetsAsync(connection, policy, table, where, cutoff, options, ct);

		outcome.ArchiveOrigin = archive.Origin;
		outcome.ArchivePath = keys.Count == 1 ? keys[0] : string.Join(" ", keys);
		outcome.ArchiveByteSize = totalBytes;

		if (written == 0)
			return 0;

		var deleted = await ExecuteAsync(connection,
			$"DELETE FROM {Q(policy.SchemaName, table)} WHERE {where}", cutoff, ct);

		log.LogInformation("[{Policy}] archiviate {Written} righe in {Parts} oggetti ({Bytes} byte), cancellate {Deleted}.",
			policy, written, keys.Count, totalBytes, deleted);

		return deleted;
	}

	// --- accodamento delle cancellazioni su storage ---------------------------------------------

	/// <summary>
	/// Accoda in <c>privacy.privacy_file_deletions</c> i file che una policy su storage dichiara
	/// scaduti. <b>Non tocca S3</b>: lo fa il job che lavora la coda, e la separazione è il punto —
	/// una <c>DELETE</c> su S3 non partecipa alla transazione del database.
	/// </summary>
	/// <remarks>
	/// ⚠️ <b>Accoda solo le policy con <c>only_if_unreferenced = true</c></b>, e le altre le salta
	/// dicendo perché. Non è pignoleria: una policy che cancella file per data senza controllare se
	/// qualcuno li usa ancora rimuoverebbe file vivi, e il perimetro che la rende sicura (per esempio
	/// «solo i PDF dei report», <c>kind = 'rendered_pdf'</c>) <b>non è esprimibile nella policy</b> —
	/// <c>field_names</c> è vuoto su quelle righe. Meglio un salto dichiarato che una cancellazione su
	/// un perimetro indovinato.
	/// </remarks>
	private async Task EnqueueStorageAsync(DbConnection connection, RetentionPolicyRecord policy,
		PolicyOutcome outcome, RetentionRunOptions options, CancellationToken ct)
	{
		// Il perimetro deve essere DICHIARATO: o only_if_unreferenced («solo i file che nessuna riga
		// nomina piu'»), o un target_filter. Non basta che la policy abbia una data: su un file una
		// cancellazione di troppo non si annulla, e una tabella di asset mescola foto, PDF e icone.
		if (!policy.HasDeclaredScope)
		{
			outcome.Status = PolicyRunStatus.Skipped;
			outcome.SkipReason = "azione su storage senza perimetro dichiarato: serve only_if_unreferenced "
				+ "oppure target_filter. Senza, la policy dice quando un file scade ma non quali file "
				+ "riguarda, e cancellare per sola data rimuoverebbe file ancora in uso. Se il perimetro "
				+ "e' davvero l'intera tabella, va scritto: {\"scope\": \"whole_table\"}";
			return;
		}

		var daEvento = string.IsNullOrEmpty(policy.ReferenceDateColumn);
		if (daEvento && policy.ReferenceEvent != PropertyCeasedPredicate.Event)
		{
			outcome.Status = PolicyRunStatus.Skipped;
			// Il motivo vero, quando c'e' un evento, e' che l'evento va derivato: dirlo come «manca la
			// colonna» mandava a cercare una colonna che non deve esistere (era P8). L'unico evento che
			// il motore sa derivare e' property_ceased, vedi PropertyCeasedPredicate.
			outcome.SkipReason = string.IsNullOrEmpty(policy.ReferenceEvent)
				? "azione su storage senza colonna di decorrenza e senza evento: niente da cui contare"
				: $"azione su storage con decorrenza dall'evento '{policy.ReferenceEvent}': il motore sa "
					+ $"derivare solo '{PropertyCeasedPredicate.Event}'";
			return;
		}

		var table = await ResolvePhysicalTableAsync(connection, policy.SchemaName, policy.TableName, ct);
		if (table is null)
		{
			outcome.Status = PolicyRunStatus.Failed;
			outcome.Error = $"tabella non trovata: ne '{policy.SchemaName}.{policy.TableName}' "
				+ $"ne '{policy.SchemaName}.{policy.SchemaName}_{policy.TableName}'";
			return;
		}

		outcome.PhysicalTable = table;
		var columns = await ReadColumnsAsync(connection, policy.SchemaName, table, ct);

		// Le tabelle di asset chiamano le due colonne `origin` e `path`; privacy_archive_manifests le
		// chiama `storage_origin` e `storage_path`, come la coda delle cancellazioni a cui e' affine.
		// Il motore accetta entrambe le convenzioni invece di imporne una: l'alternativa era rinominare
		// le colonne di una tabella per far contento il motore, che e' il verso sbagliato.
		var originColumn = columns.ContainsKey("origin") ? "origin"
			: columns.ContainsKey("storage_origin") ? "storage_origin" : null;
		var pathColumn = columns.ContainsKey("path") ? "path"
			: columns.ContainsKey("storage_path") ? "storage_path" : null;

		if (originColumn is null || pathColumn is null
			|| (!daEvento && !columns.ContainsKey(policy.ReferenceDateColumn)))
		{
			// Il messaggio nomina SOLO quello che manca, non l'elenco dei requisiti. La prima versione
			// elencava tutti e tre - decorrenza, origine, percorso - e su fiskaly.media_assets, dove
			// mancava solo la colonna di decorrenza, mandava a cercare un problema su `origin` e `path`
			// che c'erano. Un errore che elenca i requisiti invece dei fatti fa perdere tempo.
			var mancanti = new List<string>();
			if (!daEvento && !columns.ContainsKey(policy.ReferenceDateColumn))
				mancanti.Add($"la colonna di decorrenza '{policy.ReferenceDateColumn}'");
			if (originColumn is null)
				mancanti.Add("'origin' oppure 'storage_origin'");
			if (pathColumn is null)
				mancanti.Add("'path' oppure 'storage_path'");

			outcome.Status = PolicyRunStatus.Failed;
			outcome.Error = $"su {policy.SchemaName}.{table} manca " + string.Join(", ", mancanti)
				+ ". Le colonne presenti sono: " + string.Join(", ", columns.Keys.OrderBy(k => k));
			return;
		}

		// ⚠️ La cessazione si deriva dallo stabilimento, quindi la tabella deve dire a quale appartiene,
		// e con lo stesso tipo con cui lo dice identity: un `uuid` che vale catalog.properties.public_id.
		// Dentro lo schema `catalog` la stessa colonna e' invece un int8 verso `properties.id` (17
		// tabelle, con vera FK).
		//
		// Il controllo NON serve a evitare un errore silenzioso: PostgreSQL rifiuta `uuid = bigint`
		// con «l'operatore non esiste», quindi una policy sbagliata si farebbe sentire comunque. Serve
		// a farla sentire PRIMA e MEGLIO: l'errore di PostgreSQL arriverebbe a meta' corsa, dopo che
		// altre policy hanno gia' scritto, e direbbe «l'operatore non esiste» invece di «questa tabella
		// non ha il riferimento che serve».
		if (daEvento && (!columns.TryGetValue("property_id", out var colProperty)
			|| colProperty.DataType != "uuid"))
		{
			outcome.Status = PolicyRunStatus.Failed;
			outcome.Error = $"decorrenza da '{policy.ReferenceEvent}' su {policy.SchemaName}.{table}: "
				+ "serve una colonna 'property_id' di tipo uuid (il public_id dello stabilimento), "
				+ "che questa tabella non ha";
			return;
		}

		var cutoff = SeasonBoundary.CutoffFor(options.NowUtc, policy.DurationUnit, policy.DurationValue);
		outcome.CutoffAt = cutoff;

		var unreferenced = await BuildUnreferencedPredicateAsync(connection, policy.SchemaName, table, ct);

		// `property_id` dichiara un proprietario anche quando nessuna riga aggancia il file: sono le foto
		// di stabilimenti che non hanno aperto la vetrina, e NON sono orfane. Senza questa esclusione la
		// policy le cancellerebbe il giorno dopo la migrazione.
		//
		// ⚠️ Vale SOLO per le policy «solo i file che nessuna riga nomina piu'»: e' un raffinamento di
		// quel perimetro, non una regola universale. Applicarla a ogni tabella che abbia la colonna la
		// rendeva sbagliata nel verso peggiore, quello silenzioso: su notification.media_assets, dove
		// `property_id` e' valorizzato, la policy dei PDF dei report a 24 mesi avrebbe trovato ZERO
		// righe e scritto nel verbale «niente da fare», mentre i file c'erano. Lo stesso su
		// switchboard.switchboard_media_assets, dove ogni audio ha il suo stabilimento.
		var ownerClause = policy.OnlyIfUnreferenced && columns.ContainsKey("property_id")
			? " AND t.\"property_id\" IS NULL"
			: "";

		// ⚠️ Un oggetto gia' cancellato non si riaccoda. L'indice unico di privacy_file_deletions
		// protegge solo finche' la cancellazione e' PENDENTE - il suo filtro e' `executed_at IS NULL` -
		// quindi appena il job la esegue la protezione sparisce, e la riga di asset (che esiste ancora,
		// con il suo deleted_at) torna a cadere nel perimetro a ogni corsa successiva. Il confronto e'
		// sull'ASSET e non sul percorso: un percorso riusato e' un oggetto nuovo, mentre un asset gia'
		// ripulito resta ripulito.
		// ⚠️ La guardia e' per (ASSET, AZIONE) e non per asset soltanto. Un oggetto raffreddato a 36 mesi
		// deve poter essere cancellato a 10 anni: se la guardia guardasse solo l'asset, la prima azione
		// eseguita bloccherebbe per sempre la seconda, e gli scontrini non scadrebbero mai.
		var giaFatto = " AND NOT EXISTS (SELECT 1 FROM privacy.privacy_file_deletions d"
			+ " WHERE d.asset_schema = @schema AND d.asset_table = @table AND d.asset_id = t.\"id\""
			+ " AND d.action = @action::privacy.retention_action)";

		var (filtro, erroreFiltro) = TargetFilter.Build(policy.TargetFilter, columns.Keys.ToHashSet(),
			alias: "t", Q(policy.SchemaName, table));
		if (erroreFiltro is not null)
		{
			outcome.Status = PolicyRunStatus.Failed;
			outcome.Error = erroreFiltro;
			return;
		}

		// Con una decorrenza da evento non c'e' nessuna colonna data da confrontare: il confronto col
		// taglio sta dentro il predicato della cessazione, che usa lo stesso @cutoff.
		var termineData = daEvento
			? PropertyCeasedPredicate.For("t.\"property_id\"")
			: $"t.\"{policy.ReferenceDateColumn}\" < @cutoff";

		var where = $"{termineData}{ownerClause}{filtro}{unreferenced}{giaFatto}";
		outcome.MatchedRows = await CountStorageAsync(connection, policy.SchemaName, table, where, cutoff,
			policy.Action, ct);
		if (outcome.MatchedRows == 0)
			return;

		var write = options.Apply && !policy.IsDryRunOnly;
		if (!write)
		{
			log.LogInformation("[{Policy}] prova: {Rows} file da accodare, oltre {Cutoff:yyyy-MM-dd}",
				policy, outcome.MatchedRows, cutoff);
			return;
		}

		var sizeColumn = columns.ContainsKey("byte_size") ? "t.\"byte_size\"" : "NULL::integer";
		var sql = $"""
			INSERT INTO privacy.privacy_file_deletions
			  (public_id, asset_schema, asset_table, asset_id, storage_origin, storage_path, byte_size,
			   action, policy_id, reason, decided_at, created_at, updated_at)
			SELECT gen_random_uuid(), @schema, @table, t."id", t."{originColumn}"::text, t."{pathColumn}", {sizeColumn},
			       @action::privacy.retention_action, @policy, @reason, now(), now(), now()
			  FROM {Q(policy.SchemaName, table)} t
			 WHERE {where}
			ON CONFLICT ("storage_origin", "storage_path") WHERE executed_at IS NULL DO NOTHING
			""";

		await using var cmd = Command(connection, sql);
		AddParam(cmd, "@cutoff", cutoff);
		AddParam(cmd, "@schema", policy.SchemaName);
		AddParam(cmd, "@table", table);
		// L'azione viaggia come testo con cast esplicito, come tutti gli enum di privacy: la connessione
		// e' quella del modulo proprietario dello schema valutato, e la' gli enum di privacy non sono
		// mappati. Si copia DALLA POLICY, non si deduce: la coda deve dire che cosa e' stato deciso.
		AddParam(cmd, "@action", policy.Action);
		AddParam(cmd, "@policy", policy.Id);
		AddParam(cmd, "@reason", $"Retention: {policy.Action} oltre {policy.DurationValue} "
			+ $"{policy.DurationUnit}, nessuna riga referenzia piu' il file. {policy.LegalBasis}");
		outcome.AffectedRows = await cmd.ExecuteNonQueryAsync(ct);

		log.LogInformation("[{Policy}] accodate {Rows} azioni '{Action}' in privacy_file_deletions "
			+ "(nessun oggetto ancora toccato).", policy, outcome.AffectedRows, policy.Action);
	}

	/// <summary>
	/// Costruisce il predicato «nessun'altra riga nomina questo file», una <c>NOT EXISTS</c> per ogni
	/// chiave esterna che punta alla tabella di asset.
	/// </summary>
	/// <remarks>
	/// Le chiavi esterne si leggono da <c>pg_constraint</c> invece di essere elencate a mano. Il
	/// progetto ne ha diciannove e cambiano quando cambia lo schema: un elenco cablato si sarebbe
	/// disallineato alla prima tabella nuova, e il modo in cui si disallinea è silenzioso — una FK
	/// dimenticata non dà errore, fa cancellare un file ancora in uso.
	/// </remarks>
	private async Task<string> BuildUnreferencedPredicateAsync(
		DbConnection connection, string schema, string table, CancellationToken ct)
	{
		await using var cmd = Command(connection, """
			SELECT n2.nspname AS child_schema, c2.relname AS child_table, a2.attname AS child_column,
			       a1.attname AS parent_column
			  FROM pg_constraint con
			  JOIN pg_class c1 ON c1.oid = con.confrelid
			  JOIN pg_namespace n1 ON n1.oid = c1.relnamespace
			  JOIN pg_class c2 ON c2.oid = con.conrelid
			  JOIN pg_namespace n2 ON n2.oid = c2.relnamespace
			  JOIN pg_attribute a1 ON a1.attrelid = c1.oid AND a1.attnum = con.confkey[1]
			  JOIN pg_attribute a2 ON a2.attrelid = c2.oid AND a2.attnum = con.conkey[1]
			 WHERE con.contype = 'f' AND n1.nspname = @schema AND c1.relname = @table
			   AND array_length(con.conkey, 1) = 1
			""");
		AddParam(cmd, "@schema", schema);
		AddParam(cmd, "@table", table);

		var parts = new StringBuilder();
		await using var reader = await cmd.ExecuteReaderAsync(ct);
		while (await reader.ReadAsync(ct))
		{
			var cs = reader.GetString(0);
			var ct2 = reader.GetString(1);
			var cc = reader.GetString(2);
			var pc = reader.GetString(3);
			parts.Append($" AND NOT EXISTS (SELECT 1 FROM \"{cs}\".\"{ct2}\" r WHERE r.\"{cc}\" = t.\"{pc}\")");
		}

		return parts.ToString();
	}

	/// <summary>
	/// Inventaria un oggetto di archivio in <c>privacy.privacy_archive_manifests</c>: una riga per
	/// oggetto scritto, con quante righe contiene e da quale taglio provengono.
	/// </summary>
	/// <remarks>
	/// <para>
	/// È una tabella diversa dal verbale della corsa di proposito. Il verbale è operativo e scade a 24
	/// mesi; questo inventario deve vivere <b>quanto l'oggetto che indirizza</b> — dieci anni per gli
	/// scontrini fiscali. Tenerli insieme significava che la retention del primo cancellava il secondo,
	/// lasciando sullo storage oggetti di cui il database non sapeva più niente e togliendo alla policy
	/// di cancellazione dell'archivio la fonte da cui partire.
	/// </para>
	/// <para>
	/// ⚠️ Scritta in SQL e non con EF Core per lo stesso motivo del resto del motore: BuildingBlocks non
	/// conosce i <c>DbContext</c> dei moduli, e il manifesto è di un modulo (Privacy). Il nome della
	/// tabella è una costante, non viene da nessun dato esterno.
	/// </para>
	/// </remarks>
	private async Task RecordManifestAsync(DbConnection connection, RetentionPolicyRecord policy,
		string table, ArchiveWriteResult result, int rowCount, DateTime cutoff,
		RetentionRunOptions options, CancellationToken ct)
	{
		const string sql = """
			INSERT INTO privacy.privacy_archive_manifests
			  (public_id, run_id, policy_id, source_schema, source_table, object_kind, storage_origin,
			   storage_path, byte_size, row_count, cutoff_at, archived_at, created_at, updated_at)
			VALUES (@public_id, @run_id, @policy, @schema, @table,
			        'row_archive'::privacy.archive_object_kind, @origin, @path,
			        @bytes, @rows, @cutoff, now(), now(), now())
			ON CONFLICT ("storage_origin", "storage_path") DO NOTHING
			""";

		await using var cmd = Command(connection, sql);
		// UUIDv7 e non gen_random_uuid(): e' la convenzione del progetto per gli id pubblici, e su una
		// tabella append-only l'ordinamento per id pubblico coincide con quello temporale.
		AddParam(cmd, "@public_id", Guid.CreateVersion7());
		AddParam(cmd, "@run_id", options.RunId);
		AddParam(cmd, "@policy", policy.Id);
		AddParam(cmd, "@schema", policy.SchemaName);
		AddParam(cmd, "@table", table);
		AddParam(cmd, "@origin", archive.Origin);
		AddParam(cmd, "@path", result.Key);
		AddParam(cmd, "@bytes", result.ByteSize);
		AddParam(cmd, "@rows", (long)rowCount);
		AddParam(cmd, "@cutoff", cutoff);
		await cmd.ExecuteNonQueryAsync(ct);
	}

	// --- il verbale della corsa ----------------------------------------------------------------

	/// <summary>
	/// Scrive l'esito in <c>privacy.privacy_retention_runs</c>. Gli enum sono passati come testo con un
	/// cast esplicito, così la scrittura funziona da qualunque connessione di modulo.
	/// </summary>
	/// <summary>
	/// Scrive l'esito della valutazione nel verbale. L'<c>INSERT</c> vive in
	/// <see cref="RetentionRunLog"/> perche' lo condivide con i job scritti a mano.
	/// </summary>
	private async Task RecordRunAsync(DbConnection connection, string schema,
		RetentionPolicyRecord policy, PolicyOutcome outcome, RetentionRunOptions options, CancellationToken ct)
	{
		var entry = new RetentionRunLog.Entry(
			RunId: options.RunId,
			PolicyId: policy.Id,
			SchemaName: schema,
			TableName: outcome.PhysicalTable ?? policy.TableName,
			Action: policy.Action,
			CutoffAt: outcome.CutoffAt,
			IsDryRun: !(options.Apply && !policy.IsDryRunOnly),
			MatchedRows: outcome.MatchedRows,
			AffectedRows: outcome.AffectedRows,
			Status: outcome.Status switch
			{
				PolicyRunStatus.Succeeded => "succeeded",
				PolicyRunStatus.Skipped => "skipped",
				_ => "failed"
			},
			Detail: outcome.Error ?? outcome.SkipReason,
			StartedAt: options.NowUtc,
			DurationMs: (long)outcome.Duration.TotalMilliseconds,
			ArchiveOrigin: outcome.ArchiveOrigin,
			ArchivePath: outcome.ArchivePath,
			ArchiveByteSize: outcome.ArchiveByteSize);

		var errore = await RetentionRunLog.WriteAsync(connection, entry, _statementTimeout, ct);
		if (errore is not null)
		{
			// Non far fallire la corsa perche' il verbale non si scrive: l'azione e' gia' avvenuta.
			log.LogError("[{Policy}] esito non registrato su privacy_retention_runs: {Message}", policy, errore);
		}
	}

	// --- introspezione e utilità ---------------------------------------------------------------

	private sealed record ColumnInfo(string Name, string DataType, bool IsNullable, int? MaxLength);

	/// <summary>
	/// Risolve il nome fisico della tabella. <c>table_name</c> è nella forma canonica <b>senza</b>
	/// prefisso di modulo, ma in <c>privacy</c> e <c>switchboard</c> le tabelle fisiche il prefisso lo
	/// hanno: <c>switchboard.voice_call_records</c> sta su <c>switchboard.switchboard_voice_call_records</c>.
	/// </summary>
	private async Task<string?> ResolvePhysicalTableAsync(
		DbConnection connection, string schema, string table, CancellationToken ct)
	{
		await using var cmd = Command(connection, """
			SELECT table_name FROM information_schema.tables
			 WHERE table_schema = @schema AND table_name IN (@plain, @prefixed)
			 ORDER BY (table_name = @plain) DESC
			 LIMIT 1
			""");
		AddParam(cmd, "@schema", schema);
		AddParam(cmd, "@plain", table);
		AddParam(cmd, "@prefixed", $"{schema}_{table}");

		var found = await cmd.ExecuteScalarAsync(ct);
		return found as string;
	}

	private async Task<Dictionary<string, ColumnInfo>> ReadColumnsAsync(
		DbConnection connection, string schema, string table, CancellationToken ct)
	{
		await using var cmd = Command(connection, """
			SELECT column_name, data_type, is_nullable, character_maximum_length
			  FROM information_schema.columns
			 WHERE table_schema = @schema AND table_name = @table
			""");
		AddParam(cmd, "@schema", schema);
		AddParam(cmd, "@table", table);

		var map = new Dictionary<string, ColumnInfo>(StringComparer.Ordinal);
		await using var reader = await cmd.ExecuteReaderAsync(ct);
		while (await reader.ReadAsync(ct))
		{
			var name = reader.GetString(0);
			map[name] = new ColumnInfo(
				name,
				reader.GetString(1),
				reader.GetString(2) == "YES",
				reader.IsDBNull(3) ? null : reader.GetInt32(3));
		}

		return map;
	}

	private async Task<string?> ReadPrimaryKeyAsync(
		DbConnection connection, string schema, string table, CancellationToken ct)
	{
		await using var cmd = Command(connection, """
			SELECT a.attname
			  FROM pg_index i
			  JOIN pg_class c ON c.oid = i.indrelid
			  JOIN pg_namespace n ON n.oid = c.relnamespace
			  JOIN pg_attribute a ON a.attrelid = c.oid AND a.attnum = ANY (i.indkey)
			 WHERE n.nspname = @schema AND c.relname = @table AND i.indisprimary
			 LIMIT 1
			""");
		AddParam(cmd, "@schema", schema);
		AddParam(cmd, "@table", table);
		return await cmd.ExecuteScalarAsync(ct) as string;
	}

	/// <summary>
	/// Conteggio per le azioni su storage: lo stesso predicato dell'accodamento, che oltre al taglio
	/// usa anche schema e tabella — serve alla guardia «oggetto gia' cancellato».
	/// </summary>
	private async Task<long> CountStorageAsync(DbConnection connection, string schema, string table,
		string where, DateTime cutoff, string action, CancellationToken ct)
	{
		await using var cmd = Command(connection, $"SELECT count(*) FROM {Q(schema, table)} t WHERE {where}");
		AddParam(cmd, "@cutoff", cutoff);
		AddParam(cmd, "@schema", schema);
		AddParam(cmd, "@table", table);
		AddParam(cmd, "@action", action);
		return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
	}

	private async Task<long> CountAsync(DbConnection connection, string schema, string table,
		string where, DateTime cutoff, CancellationToken ct, string? alias = null)
	{
		var from = alias is null ? Q(schema, table) : $"{Q(schema, table)} {alias}";
		await using var cmd = Command(connection, $"SELECT count(*) FROM {from} WHERE {where}");
		AddParam(cmd, "@cutoff", cutoff);
		return Convert.ToInt64(await cmd.ExecuteScalarAsync(ct));
	}

	private async Task<long> ExecuteAsync(DbConnection connection, string sql, DateTime cutoff, CancellationToken ct)
	{
		await using var cmd = Command(connection, sql);
		AddParam(cmd, "@cutoff", cutoff);
		return await cmd.ExecuteNonQueryAsync(ct);
	}

	private static string Q(string schema, string table) => $"\"{schema}\".\"{table}\"";

	private static void AddParam(DbCommand cmd, string name, object? value)
	{
		var p = cmd.CreateParameter();
		p.ParameterName = name;
		p.Value = value ?? DBNull.Value;
		cmd.Parameters.Add(p);
	}

	/// <summary>Il messaggio dell'eccezione e quello delle cause: su Npgsql il dettaglio utile sta spesso nella seconda.</summary>
	private static string Describe(Exception ex)
	{
		var sb = new StringBuilder(ex.Message);
		var inner = ex.InnerException;
		while (inner is not null)
		{
			sb.Append(" | ").Append(inner.Message);
			inner = inner.InnerException;
		}

		return sb.ToString();
	}
}
