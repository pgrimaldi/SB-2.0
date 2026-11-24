namespace Module.Catalog.Application.Dtos;

/// <summary>Esito di una rigenerazione: snapshot scritti, invariati, tolti (non piu' dovuti), in errore.</summary>
public sealed record SnapshotRebuildResult(int Written, int Unchanged, int Removed, int Failed, IReadOnlyList<string> Errors);

/// <param name="PropertyIds">Vuoto = tutti gli stabilimenti.</param>
/// <param name="Kinds">Vuoto = tutti i tipi.</param>
/// <param name="OnlyMissing">Solo quelli che mancano.</param>
public sealed record RebuildSnapshotsRequest(IReadOnlyList<Guid>? PropertyIds, IReadOnlyList<string>? Kinds, bool OnlyMissing = false);

/// <summary>Uno snapshot pronto da servire: il JSON e il suo ETag (dall'hash del contenuto).</summary>
public sealed record SnapshotContent(string Payload, string ETag);
