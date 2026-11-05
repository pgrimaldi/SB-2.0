using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Application.Abstractions;

/// <summary>Stabilimento per cui costruire gli snapshot: gestionale (non importato) e/o vetrina pubblicata (slug).</summary>
public sealed record SnapshotTarget(long PropertyId, Guid PublicId, bool HasManagement, string? PublishedSlug);

public interface IConfigSnapshotRepository
{
	Task<ConfigSnapshot?> FindAsync(long propertyId, ConfigSnapshotKind kind, CancellationToken ct = default);
	void Add(ConfigSnapshot snapshot);
	void Remove(ConfigSnapshot snapshot);
	/// <summary>Tutti gli stabilimenti (o quelli indicati); esclusi quelli eliminati definitivamente.</summary>
	Task<IReadOnlyList<SnapshotTarget>> ListTargetsAsync(IReadOnlyCollection<long>? propertyIds, CancellationToken ct = default);
	/// <summary>Coppie (stabilimento, tipo) gia' presenti.</summary>
	Task<IReadOnlySet<(long, ConfigSnapshotKind)>> ListExistingAsync(CancellationToken ct = default);
	Task SaveChangesAsync(CancellationToken ct = default);
}
