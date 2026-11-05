using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class OnlineSettingsRepository : IOnlineSettingsRepository
{
	private readonly DbContext _db;

	public OnlineSettingsRepository(DbContext db) => _db = db;

	public Task<OnlineBookingSettings?> FindAsync(long propertyId, CancellationToken ct = default)
		=> _db.OnlineBookingSettings
			.Include(s => s.AllowedPeriods).Include(s => s.AllowedLocations).Include(s => s.StayLimits)
			.FirstOrDefaultAsync(s => s.PropertyId == propertyId, ct);

	public void Add(OnlineBookingSettings settings) => _db.OnlineBookingSettings.Add(settings);

	public Task<ServiceBookingSettings?> FindServiceSettingsAsync(long propertyId, CancellationToken ct = default)
		=> _db.ServiceBookingSettings.FirstOrDefaultAsync(s => s.PropertyId == propertyId, ct);

	public void AddServiceSettings(ServiceBookingSettings settings) => _db.ServiceBookingSettings.Add(settings);

	public Task<bool> IsOnlineSalesSuspendedAsync(long propertyId, CancellationToken ct = default)
		=> _db.Properties.AsNoTracking().Where(p => p.Id == propertyId).Select(p => p.IsOnlineSalesSuspended).FirstOrDefaultAsync(ct);

	public async Task<IReadOnlyDictionary<Guid, long>> FindSpotIdsAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> publicIds.Count == 0
			? new Dictionary<Guid, long>()
			: await _db.MapSpots.AsNoTracking()
				.Where(s => s.PropertyId == propertyId && publicIds.Contains(s.PublicId))
				.ToDictionaryAsync(s => s.PublicId, s => s.Id, ct);

	public async Task<IReadOnlyList<Guid>> FindSpotPublicIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default)
		=> ids.Count == 0
			? []
			: await _db.MapSpots.AsNoTracking().Where(s => ids.Contains(s.Id))
				.OrderBy(s => s.GridRow).ThenBy(s => s.GridColumn).Select(s => s.PublicId).ToListAsync(ct);

	public Task<List<MapLegendItem>> ListLegendAsync(long propertyId, CancellationToken ct = default)
		=> _db.MapLegendItems.Where(l => l.PropertyId == propertyId).OrderBy(l => l.SortOrder).ThenBy(l => l.Id).ToListAsync(ct);

	public void AddLegend(IEnumerable<MapLegendItem> items) => _db.MapLegendItems.AddRange(items);

	public void RemoveLegend(IEnumerable<MapLegendItem> items) => _db.MapLegendItems.RemoveRange(items);
}
