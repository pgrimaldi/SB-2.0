using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class ConfigSnapshotRepository : IConfigSnapshotRepository
{
	private readonly DbContext _db;

	public ConfigSnapshotRepository(DbContext db) => _db = db;

	public Task<ConfigSnapshot?> FindAsync(long propertyId, ConfigSnapshotKind kind, CancellationToken ct = default)
		=> _db.ConfigSnapshots.FirstOrDefaultAsync(s => s.PropertyId == propertyId && s.SnapshotKind == kind, ct);

	public void Add(ConfigSnapshot snapshot) => _db.ConfigSnapshots.Add(snapshot);

	public void Remove(ConfigSnapshot snapshot) => _db.ConfigSnapshots.Remove(snapshot);

	public async Task<IReadOnlyList<SnapshotTarget>> ListTargetsAsync(IReadOnlyCollection<long>? propertyIds, CancellationToken ct = default)
	{
		var properties = _db.Properties.IgnoreQueryFilters().AsNoTracking().Where(p => p.PurgedAt == null);
		if (propertyIds is not null)
			properties = properties.Where(p => propertyIds.Contains(p.Id));
		var rows = await properties
			.Select(p => new
			{
				p.Id,
				p.PublicId,
				Management = p.DeletedAt == null && p.Status != PropertyStatus.Imported,
				Slug = _db.SearchBathhouses.Where(s => s.PropertyId == p.Id && s.IsPublished).Select(s => s.Slug).FirstOrDefault(),
			})
			.ToListAsync(ct);
		return rows.Select(r => new SnapshotTarget(r.Id, r.PublicId, r.Management, r.Slug)).ToList();
	}

	public async Task<IReadOnlySet<(long, ConfigSnapshotKind)>> ListExistingAsync(CancellationToken ct = default)
		=> (await _db.ConfigSnapshots.AsNoTracking().Select(s => new { s.PropertyId, s.SnapshotKind }).ToListAsync(ct))
			.Select(s => (s.PropertyId, s.SnapshotKind)).ToHashSet();

	public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
