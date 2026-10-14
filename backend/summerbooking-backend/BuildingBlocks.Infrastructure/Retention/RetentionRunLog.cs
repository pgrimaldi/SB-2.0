using System.Data.Common;

namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>
/// Scrive una riga di verbale in <c>privacy.privacy_retention_runs</c>.
/// </summary>
/// <remarks>
/// <para>
/// Esiste come pezzo a sé perché il verbale lo scrivono <b>due</b> tipi di esecutore: il motore
/// generico per le policy che sa valutare, e i job scritti a mano per le policy il cui perimetro non
/// è esprimibile in una riga di configurazione. Duplicare l'<c>INSERT</c> nei due posti sarebbe la
/// ricetta per farli divergere alla prima colonna aggiunta — e il verbale è l'unica cosa che una
/// persona legge prima di autorizzare una cancellazione.
/// </para>
/// <para>
/// ⚠️ Gli enum sono passati come <b>testo con cast esplicito</b>: la scrittura parte dalla connessione
/// del modulo proprietario dello schema valutato — quella di Booking, di Catalog — e su quelle
/// connessioni gli enum nativi di <c>privacy</c> non sono mappati.
/// </para>
/// </remarks>
public static class RetentionRunLog
{
	/// <summary>Una riga di verbale, già pronta.</summary>
	/// <param name="RunId">Raggruppa tutte le righe della stessa esecuzione, su tutti gli schemi.</param>
	/// <param name="TableName">⚠️ Nome <b>fisico</b>: in <c>privacy</c> e <c>switchboard</c> ha il prefisso di modulo.</param>
	/// <param name="IsDryRun">La corsa non ha scritto niente: o era una prova, o la policy è <c>is_dry_run_only</c>.</param>
	/// <param name="Detail">Il motivo del salto, o il testo dell'errore. Obbligatorio su <c>failed</c>.</param>
	public sealed record Entry(
		Guid RunId,
		long PolicyId,
		string SchemaName,
		string TableName,
		string Action,
		DateTime? CutoffAt,
		bool IsDryRun,
		long MatchedRows,
		long AffectedRows,
		string Status,
		string? Detail,
		DateTime StartedAt,
		long DurationMs,
		string? ArchiveOrigin = null,
		string? ArchivePath = null,
		long? ArchiveByteSize = null);

	/// <summary>Inserisce la riga. Non solleva: un verbale che fallisce non deve far fallire la corsa.</summary>
	/// <remarks>
	/// ⚠️ <c>finished_at</c> usa <b><c>clock_timestamp()</c> e non <c>now()</c></b>. Dentro una
	/// transazione esplicita <c>now()</c> è <b>congelato all'istante di apertura</b>, quindi risulta
	/// <i>anteriore</i> a <c>started_at</c> — che il motore passa come parametro, letto dall'orologio
	/// del processo — e il vincolo <c>ck_privacy_retention_runs_times</c> (<c>finished_at &gt;=
	/// started_at</c>) salta. In autocommit, che è come il motore gira oggi, i due sono
	/// indistinguibili e il difetto non si vede: si è visto scrivendo un test che avvolge la corsa in
	/// una transazione per poterla annullare.
	/// <para>
	/// La conseguenza era peggiore del sintomo: l'errore veniva <b>inghiottito</b> — un verbale che non
	/// si scrive non deve far fallire la corsa — ma lasciava la transazione interrotta, e <i>tutte</i>
	/// le policy successive fallivano con <c>25P02</c> senza che il motivo vero comparisse da nessuna
	/// parte.
	/// </para>
	/// </remarks>
	/// <returns><c>null</c> se è andata, altrimenti il messaggio dell'errore.</returns>
	public static async Task<string?> WriteAsync(DbConnection connection, Entry entry,
		int commandTimeoutSeconds, CancellationToken ct)
	{
		const string sql = """
			INSERT INTO privacy.privacy_retention_runs
			  (public_id, run_id, policy_id, schema_name, table_name, action, cutoff_at, is_dry_run,
			   matched_rows, affected_rows, archive_origin, archive_path, archive_byte_size,
			   status, detail, started_at, finished_at, duration_ms)
			VALUES
			  (@public_id, @run_id, @policy_id, @schema, @table, @action::privacy.retention_action,
			   @cutoff, @dry, @matched, @affected, @origin, @path, @bytes,
			   @status::privacy.retention_run_status, @detail, @started, clock_timestamp(), @ms)
			""";

		try
		{
			await using var cmd = connection.CreateCommand();
			cmd.CommandText = sql;
			cmd.CommandTimeout = commandTimeoutSeconds;

			Add(cmd, "@public_id", Guid.CreateVersion7());
			Add(cmd, "@run_id", entry.RunId);
			Add(cmd, "@policy_id", entry.PolicyId);
			Add(cmd, "@schema", entry.SchemaName);
			Add(cmd, "@table", entry.TableName);
			Add(cmd, "@action", entry.Action);
			Add(cmd, "@cutoff", entry.CutoffAt);
			Add(cmd, "@dry", entry.IsDryRun);
			Add(cmd, "@matched", entry.MatchedRows);
			Add(cmd, "@affected", entry.AffectedRows);
			Add(cmd, "@origin", entry.ArchiveOrigin);
			Add(cmd, "@path", entry.ArchivePath);
			Add(cmd, "@bytes", entry.ArchiveByteSize);
			Add(cmd, "@status", entry.Status);
			Add(cmd, "@detail", entry.Detail);
			Add(cmd, "@started", entry.StartedAt);
			Add(cmd, "@ms", entry.DurationMs);

			await cmd.ExecuteNonQueryAsync(ct);
			return null;
		}
		catch (Exception ex)
		{
			return ex.Message;
		}
	}

	private static void Add(DbCommand cmd, string name, object? value)
	{
		var p = cmd.CreateParameter();
		p.ParameterName = name;
		p.Value = value ?? DBNull.Value;
		cmd.Parameters.Add(p);
	}
}
