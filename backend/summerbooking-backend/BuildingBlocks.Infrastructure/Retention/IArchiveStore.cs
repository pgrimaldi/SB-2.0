namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>Dove finiscono le righe archiviate da una policy <c>archive_cold</c>.</summary>
public interface IArchiveStore
{
	/// <summary>Etichetta dell'host di storage, scritta su <c>privacy_retention_runs.archive_origin</c>.</summary>
	string Origin { get; }

	/// <summary>Prepara il contenitore (bucket) se non esiste. Idempotente.</summary>
	Task EnsureReadyAsync(CancellationToken ct);

	/// <summary>
	/// Scrive l'archivio e restituisce la chiave e la dimensione dell'oggetto. Va chiamata
	/// <b>prima</b> di cancellare le righe dal database caldo: se fallisce, non si cancella niente.
	/// </summary>
	Task<ArchiveWriteResult> WriteAsync(string key, Stream content, CancellationToken ct);

	/// <summary>Chiave sotto cui archiviare le righe di una policy, deterministica e leggibile.</summary>
	string BuildKey(string schema, string table, Guid policyPublicId, DateTime nowUtc, int part);
}

/// <summary>Esito della scrittura di un archivio.</summary>
public sealed record ArchiveWriteResult(string Key, long ByteSize);
