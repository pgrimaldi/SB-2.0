using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Module.Catalog.Domain.ValueObjects;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class MapRepository : IMapRepository
{
	private readonly DbContext _db;

	public MapRepository(DbContext db) => _db = db;

	public Task<MapLayout?> FindLayoutAsync(long propertyId, CancellationToken ct = default)
		=> _db.MapLayouts.Include(l => l.SpecialIcons).FirstOrDefaultAsync(l => l.PropertyId == propertyId, ct);

	public void AddLayout(MapLayout layout) => _db.MapLayouts.Add(layout);

	public Task<List<MapSpot>> ListSpotsAsync(long propertyId, CancellationToken ct = default)
		=> _db.MapSpots.Where(s => s.PropertyId == propertyId).ToListAsync(ct);

	public void AddSpots(IEnumerable<MapSpot> spots) => _db.MapSpots.AddRange(spots);

	public void RemoveSpots(IEnumerable<MapSpot> spots) => _db.MapSpots.RemoveRange(spots);

	public async Task<IReadOnlyList<MapIcon>> ListIconsAsync(MapIconCategoryCode? category, CancellationToken ct = default)
		=> await _db.MapIcons.AsNoTracking().Include(i => i.Category)
			.Where(i => i.IsActive && (category == null || i.Category!.Code == category))
			.OrderBy(i => i.CategoryId).ThenBy(i => i.Type).ThenByDescending(i => i.IsDefault).ThenBy(i => i.Id)
			.ToListAsync(ct);

	public Task<MapIcon?> FindIconAsync(Guid publicId, CancellationToken ct = default)
		=> _db.MapIcons.AsNoTracking().Include(i => i.Category).FirstOrDefaultAsync(i => i.PublicId == publicId && i.IsActive, ct);

	public async Task<IReadOnlyList<MapIcon>> ListIconsByAssetAsync(IReadOnlyCollection<long> mediaAssetIds, CancellationToken ct = default)
		=> mediaAssetIds.Count == 0
			? []
			: await _db.MapIcons.AsNoTracking().Where(i => mediaAssetIds.Contains(i.MediaAssetId)).ToListAsync(ct);

	public async Task<IReadOnlyList<MapIcon>> ListDefaultIconsAsync(CancellationToken ct = default)
		=> await _db.MapIcons.AsNoTracking().Include(i => i.Category).Where(i => i.IsDefault && i.IsActive).ToListAsync(ct);

	public async Task<IReadOnlyList<MapBeachElement>> ListBeachElementsAsync(bool activeOnly, CancellationToken ct = default)
		=> await _db.MapBeachElements.AsNoTracking()
			.Where(e => !activeOnly || e.IsActive)
			.OrderBy(e => e.SortOrder).ThenBy(e => e.Name)
			.ToListAsync(ct);

	public Task<MapBeachElement?> FindBeachElementAsync(Guid publicId, CancellationToken ct = default)
		=> _db.MapBeachElements.AsNoTracking().FirstOrDefaultAsync(e => e.PublicId == publicId, ct);
}

public sealed class MediaAssetRepository : IMediaAssetRepository
{
	private readonly DbContext _db;

	public MediaAssetRepository(DbContext db) => _db = db;

	public async Task<IReadOnlyDictionary<long, MediaRef>> FindRefsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default)
		=> ids.Count == 0
			? new Dictionary<long, MediaRef>()
			: await _db.MediaAssets.AsNoTracking().Where(a => ids.Contains(a.Id)).ToDictionaryAsync(a => a.Id, a => a.Ref, ct);

	public void Add(MediaAsset asset) => _db.MediaAssets.Add(asset);
}
