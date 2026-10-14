namespace BuildingBlocks.Infrastructure.Retention;

/// <summary>
/// Una riga di <c>privacy.privacy_retention_policies</c>, letta come testo.
/// </summary>
/// <remarks>
/// Le colonne enum sono <c>string</c> e non enum .NET di proposito: ogni modulo ha il proprio
/// <c>NpgsqlDataSource</c> con le proprie mappature, e gli enum nativi dello schema <c>privacy</c>
/// non sono mappati sulla connessione di Booking o di Catalog. La lettura li proietta con
/// <c>::text</c>, così la stessa query funziona da qualunque modulo senza che nessuno debba conoscere
/// gli enum di un altro.
/// </remarks>
public sealed record RetentionPolicyRecord
{
	public long Id { get; init; }
	public Guid PublicId { get; init; }
	public string SchemaName { get; init; } = "";

	/// <summary>Forma canonica relativa allo schema (senza prefisso di modulo). Vuota sulle policy su prefisso.</summary>
	public string TableName { get; init; } = "";

	public string[] FieldNames { get; init; } = [];

	/// <summary>Vuota quando nessuna colonna locale porta la data: la decorrenza la dà l'evento o lo storage.</summary>
	public string ReferenceDateColumn { get; init; } = "";

	public short DurationValue { get; init; }
	public string DurationUnit { get; init; } = "";
	public string Action { get; init; } = "";
	public bool OnlyIfUnreferenced { get; init; }
	public string? ReferenceEvent { get; init; }
	public string? StoragePrefix { get; init; }
	public string? StorageDateSource { get; init; }

	/// <summary>
	/// Perimetro dichiarato: <b>quali</b> righe riguarda la policy. JSON, NULL quando non è
	/// dichiarato. Vedi <c>RetentionEngine.BuildTargetFilter</c> per le forme ammesse.
	/// </summary>
	public string? TargetFilter { get; init; }

	/// <summary>
	/// Il file che deve seguire le righe archiviate. JSON, NULL quando non c'è. Vedi
	/// <c>LinkedAsset</c> in <c>RetentionEngine</c>.
	/// </summary>
	public string? LinkedAsset { get; init; }
	public string BasisSource { get; init; } = "";
	public string LegalBasis { get; init; } = "";
	public bool IsDryRunOnly { get; init; }

	/// <summary>Agisce su un oggetto di storage invece che su una riga.</summary>
	public bool IsStorageAction => Action is "delete_object" or "archive_object";

	/// <summary>
	/// La policy dice <b>quali</b> righe riguarda. È la condizione che il motore pretende per eseguire
	/// un'azione su storage: <c>only_if_unreferenced</c> («solo i file che nessuna riga nomina più»)
	/// oppure un <c>target_filter</c> dichiarato.
	/// </summary>
	public bool HasDeclaredScope => OnlyIfUnreferenced || !string.IsNullOrWhiteSpace(TargetFilter);

	/// <summary>Policy su prefisso: nessuna riga del database nomina quei file.</summary>
	public bool IsPrefixPolicy => !string.IsNullOrEmpty(StoragePrefix);

	public override string ToString()
		=> IsPrefixPolicy
			? $"{SchemaName}:{StoragePrefix} {Action} {DurationValue} {DurationUnit}"
			: $"{SchemaName}.{TableName} {Action} {DurationValue} {DurationUnit}";
}

/// <summary>
/// Esito di una singola policy nel report del motore. Distinto da
/// <c>Module.Privacy.Domain.Enums.RetentionRunStatus</c>, che e' il tipo enum nativo scritto su
/// <c>privacy_retention_runs.status</c>: BuildingBlocks non puo' dipendere da un modulo, e due nomi diversi
/// evitano a ogni chiamante di dover disambiguare con un alias.
/// </summary>
public enum PolicyRunStatus
{
	/// <summary>Eseguita (o valutata, in prova) senza errori.</summary>
	Succeeded,

	/// <summary>Non eseguita per una ragione dichiarata e legittima — non è un errore.</summary>
	Skipped,

	/// <summary>Tentata e fallita. È il motivo per cui questa tabella esiste.</summary>
	Failed
}

/// <summary>Esito della valutazione di una policy, una riga per policy per corsa.</summary>
public sealed class PolicyOutcome
{
	public required RetentionPolicyRecord Policy { get; init; }
	public PolicyRunStatus Status { get; set; } = PolicyRunStatus.Succeeded;

	/// <summary>Nome fisico risolto della tabella: in <c>privacy</c> e <c>switchboard</c> ha il prefisso di modulo.</summary>
	public string? PhysicalTable { get; set; }

	public DateTime? CutoffAt { get; set; }
	public long MatchedRows { get; set; }
	public long AffectedRows { get; set; }

	public string? ArchiveOrigin { get; set; }
	public string? ArchivePath { get; set; }
	public long? ArchiveByteSize { get; set; }

	/// <summary>Perché è stata saltata. Valorizzato solo con <see cref="PolicyRunStatus.Skipped"/>.</summary>
	public string? SkipReason { get; set; }

	/// <summary>
	/// Non scrivere questo esito nel verbale. Vero solo quando la policy è applicata da un job
	/// dedicato, che scrive la sua riga: vedi <see cref="RetentionRunOptions.PoliciesHandledElsewhere"/>.
	/// </summary>
	public bool SuppressRunLog { get; set; }

	/// <summary>L'errore, testuale. Valorizzato solo con <see cref="PolicyRunStatus.Failed"/>.</summary>
	public string? Error { get; set; }

	public TimeSpan Duration { get; set; }
}

/// <summary>Esito della corsa su un singolo schema.</summary>
public sealed class SchemaRetentionReport
{
	public required string Schema { get; init; }
	public List<PolicyOutcome> Outcomes { get; } = [];

	public int Succeeded => Outcomes.Count(o => o.Status == PolicyRunStatus.Succeeded);
	public int Skipped => Outcomes.Count(o => o.Status == PolicyRunStatus.Skipped);
	public int Failed => Outcomes.Count(o => o.Status == PolicyRunStatus.Failed);
	public long AffectedRows => Outcomes.Sum(o => o.AffectedRows);
}

/// <summary>Come si comporta la corsa.</summary>
// `record` e non `class`: chi orchestra ne deriva una copia con le policy delegate (`options with
// { ... }`) dopo averle scoperte, e una copia e' piu' sicura di una proprieta' mutabile su un oggetto
// che il motore legge in piu' punti.
public sealed record RetentionRunOptions
{
	/// <summary>Identificatore della corsa: raggruppa le righe di <c>privacy_retention_runs</c>.</summary>
	public Guid RunId { get; init; } = Guid.CreateVersion7();

	/// <summary>Istante di valutazione. Fissato una volta per corsa: due policy della stessa corsa non devono vedere due «adesso» diversi.</summary>
	public DateTime NowUtc { get; init; } = DateTime.UtcNow;

	/// <summary>
	/// <c>false</c> (default) = nessuna scrittura distruttiva: conta cosa farebbe e lo riporta.
	/// <c>true</c> = esegue. ⚠️ Una policy con <c>is_dry_run_only = true</c> resta in prova
	/// <b>anche qui</b>: quel flag è una decisione di una persona e il motore non lo scavalca.
	/// </summary>
	public bool Apply { get; init; }

	/// <summary>Esegue anche le policy con durata in stagioni. Vale solo per il job annuale.</summary>
	public bool IncludeSeasonal { get; init; } = true;

	/// <summary>Quante righe per lotto nell'export di archiviazione.</summary>
	/// <summary>
	/// Quante righe per lotto nell'export di archiviazione. Il lotto sta in memoria compresso, quindi
	/// non va alzato senza guardare quanto e' larga la riga: 20.000 righe di booking.reservations sono
	/// decine di MB prima della compressione.
	/// </summary>
	public int ArchiveBatchSize { get; init; } = 20_000;

	/// <summary>
	/// Tetto di durata per ogni istruzione, in secondi. <b>Il default di Npgsql è 30 secondi e per un
	/// job batch è sbagliato</b>: misurato, cancellare 650 prenotazioni richiede <b>43 secondi</b> —
	/// non per il volume, ma per i trigger di verifica delle 14 chiavi esterne che puntano alla
	/// tabella, uno per riga cancellata per vincolo. Con il default la policy fallisce dopo aver già
	/// scritto l'archivio.
	/// </summary>
	public int StatementTimeoutSeconds { get; init; } = 1800;

	/// <summary>
	/// Le policy che un <see cref="IBespokeRetentionJob"/> applica al posto del motore, per
	/// <c>public_id</c>.
	/// </summary>
	/// <remarks>
	/// ⚠️ Serve a non avere <b>due righe di verbale per la stessa policy</b> nella stessa corsa: una
	/// «saltata» dal motore e una dal job dedicato. Chi apre il verbale per decidere se accendere una
	/// regola leggerebbe due esiti diversi dello stesso identificativo, e il più visibile — quello del
	/// motore — è quello sbagliato.
	/// <para>
	/// Il motore salta queste policy <b>senza scrivere niente</b>: l'esito lo scrive il job che le
	/// applica davvero. Lo riporta solo nel log, perché un salto invisibile è peggio di un salto
	/// rumoroso.
	/// </para>
	/// </remarks>
	public IReadOnlySet<Guid> PoliciesHandledElsewhere { get; init; } = new HashSet<Guid>();
}
