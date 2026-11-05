using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class PlatformRepository : IPlatformRepository
{
	private readonly DbContext _db;

	public PlatformRepository(DbContext db) => _db = db;

	public async Task<(IReadOnlyList<AdminPropertyRow> Items, int Total)> ListPropertiesAsync(AdminPropertyFilter filter, int page, int pageSize, CancellationToken ct = default)
	{
		var properties = Properties().AsNoTracking().Where(p => filter.Trashed ? p.DeletedAt != null : p.DeletedAt == null);
		if (filter.Status is { } status)
			properties = properties.Where(p => p.Status == status);
		if (filter.Search is { } search)
		{
			var pattern = LikePattern.Contains(search);
			properties = properties.Where(p => EF.Functions.ILike(p.Name, pattern) || p.City != null && EF.Functions.ILike(p.City, pattern)
				|| p.ContactEmail != null && EF.Functions.ILike(p.ContactEmail, pattern));
		}
		var total = await properties.CountAsync(ct);
		var items = await properties.OrderByDescending(p => p.CreatedAt).ThenByDescending(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
		var ids = items.Select(p => p.Id).ToList();
		var bathhouses = (await _db.Bathhouses.IgnoreQueryFilters().AsNoTracking().Include(b => b.Badges.Where(x => x.DeletedAt == null))
				.Where(b => ids.Contains(b.PropertyId) && b.DeletedAt == null).ToListAsync(ct))
			.GroupBy(b => b.PropertyId).ToDictionary(g => g.Key, g => g.OrderBy(b => b.Id).First());
		return (items.Select(p => new AdminPropertyRow(p, bathhouses.GetValueOrDefault(p.Id))).ToList(), total);
	}

	public async Task<AdminPropertyRow?> FindPropertyAsync(Guid publicId, CancellationToken ct = default)
	{
		var property = await Properties().FirstOrDefaultAsync(p => p.PublicId == publicId, ct);
		if (property is null)
			return null;
		var bathhouse = await _db.Bathhouses.IgnoreQueryFilters().Include(b => b.Badges.Where(x => x.DeletedAt == null))
			.Where(b => b.PropertyId == property.Id && b.DeletedAt == null).OrderBy(b => b.Id).FirstOrDefaultAsync(ct);
		return new AdminPropertyRow(property, bathhouse);
	}

	public void TrashProperty(Property property, DateTime now) => _db.Entry(property).Property(p => p.DeletedAt).CurrentValue = now;

	public void RestoreProperty(Property property) => _db.Entry(property).Property(p => p.DeletedAt).CurrentValue = null;

	public Task<Bathhouse?> FindBathhouseByPublicIdAsync(Guid publicId, CancellationToken ct = default)
		=> _db.Bathhouses.IgnoreQueryFilters().FirstOrDefaultAsync(b => b.PublicId == publicId && b.DeletedAt == null, ct);

	public async Task<IReadOnlyDictionary<Guid, long>> FindBathhouseIdsByPropertyAsync(IReadOnlyCollection<Guid> propertyPublicIds, CancellationToken ct = default)
		=> (await _db.Bathhouses.AsNoTracking().Where(b => propertyPublicIds.Contains(b.Property!.PublicId))
				.Select(b => new { b.Id, b.Property!.PublicId }).ToListAsync(ct))
			.GroupBy(b => b.PublicId).ToDictionary(g => g.Key, g => g.Min(b => b.Id));

	public async Task<IReadOnlyList<MapIcon>> ListIconsAsync(MapIconCategoryCode? category, CancellationToken ct = default)
		=> await _db.MapIcons.AsNoTracking().Include(i => i.Category)
			.Where(i => category == null || i.Category!.Code == category)
			.OrderBy(i => i.Category!.Code).ThenBy(i => i.Type).ThenByDescending(i => i.IsDefault).ThenBy(i => i.Name).ToListAsync(ct);

	public Task<MapIcon?> FindIconAsync(Guid publicId, CancellationToken ct = default)
		=> _db.MapIcons.Include(i => i.Category).FirstOrDefaultAsync(i => i.PublicId == publicId, ct);

	public Task<MapIconCategory?> FindIconCategoryAsync(MapIconCategoryCode code, CancellationToken ct = default)
		=> _db.MapIconCategories.FirstOrDefaultAsync(c => c.Code == code, ct);

	public async Task<IReadOnlyList<MapIcon>> ListDefaultIconsAsync(short categoryId, MapIconState type, CancellationToken ct = default)
		=> await _db.MapIcons.Where(i => i.CategoryId == categoryId && i.Type == type && i.IsDefault).ToListAsync(ct);

	public void AddIcon(MapIcon icon) => _db.MapIcons.Add(icon);

	public void RemoveIcon(MapIcon icon) => _db.MapIcons.Remove(icon);

	public async Task<int> RepointAssetAsync(long fromAssetId, long toAssetId, CancellationToken ct = default)
	{
		var count = await _db.MapLayouts.Where(l => l.TemplateAssetId == fromAssetId)
			.ExecuteUpdateAsync(u => u.SetProperty(l => l.TemplateAssetId, toAssetId), ct);
		count += await _db.MapLayouts.Where(l => l.UmbrellaOccupiedAssetId == fromAssetId)
			.ExecuteUpdateAsync(u => u.SetProperty(l => l.UmbrellaOccupiedAssetId, toAssetId), ct);
		count += await _db.MapLayouts.Where(l => l.UmbrellaFreeAssetId == fromAssetId)
			.ExecuteUpdateAsync(u => u.SetProperty(l => l.UmbrellaFreeAssetId, toAssetId), ct);
		count += await _db.Set<MapLayoutSpecialIcon>().Where(i => i.MediaAssetId == fromAssetId)
			.ExecuteUpdateAsync(u => u.SetProperty(i => i.MediaAssetId, toAssetId), ct);
		count += await _db.Set<MapLegendItem>().Where(i => i.IconAssetId == fromAssetId)
			.ExecuteUpdateAsync(u => u.SetProperty(i => i.IconAssetId, toAssetId), ct);
		return count;
	}

	public async Task<IReadOnlyList<MapBeachElement>> ListBeachElementsAsync(CancellationToken ct = default)
		=> await _db.MapBeachElements.AsNoTracking().OrderBy(e => e.SortOrder).ThenBy(e => e.Name).ToListAsync(ct);

	public Task<MapBeachElement?> FindBeachElementAsync(Guid publicId, CancellationToken ct = default)
		=> _db.MapBeachElements.FirstOrDefaultAsync(e => e.PublicId == publicId, ct);

	public async Task<IReadOnlyDictionary<long, int>> CountBeachElementUsagesAsync(CancellationToken ct = default)
		=> await _db.MapSpots.Where(s => s.BeachElementId != null).GroupBy(s => s.BeachElementId!.Value)
			.Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, ct);

	public void AddBeachElement(MapBeachElement element) => _db.MapBeachElements.Add(element);

	public void RemoveBeachElement(MapBeachElement element) => _db.MapBeachElements.Remove(element);

	public async Task<IReadOnlyList<ShowcaseBadge>> ListBadgesAsync(CancellationToken ct = default)
		=> await _db.ShowcaseBadges.AsNoTracking().Include(b => b.Translations).OrderBy(b => b.SortOrder).ThenBy(b => b.Name).ToListAsync(ct);

	public Task<ShowcaseBadge?> FindBadgeAsync(Guid publicId, CancellationToken ct = default)
		=> _db.ShowcaseBadges.Include(b => b.Translations).FirstOrDefaultAsync(b => b.PublicId == publicId, ct);

	public void AddBadge(ShowcaseBadge badge) => _db.ShowcaseBadges.Add(badge);

	public void RemoveBadge(ShowcaseBadge badge) => _db.ShowcaseBadges.Remove(badge);

	public Task RemoveBadgeAssignmentsAsync(long badgeId, DateTime now, CancellationToken ct = default)
		=> _db.Set<BathhouseBadge>().Where(b => b.BadgeId == badgeId).ExecuteUpdateAsync(u => u.SetProperty(b => b.DeletedAt, now), ct);

	public async Task<IReadOnlyList<ShowcaseRegion>> ListRegionsAsync(CancellationToken ct = default)
		=> await _db.ShowcaseRegions.AsNoTracking().Include(r => r.Translations).OrderBy(r => r.SortOrder).ThenBy(r => r.Name).ToListAsync(ct);

	public Task<ShowcaseRegion?> FindRegionAsync(Guid publicId, CancellationToken ct = default)
		=> _db.ShowcaseRegions.Include(r => r.Translations).FirstOrDefaultAsync(r => r.PublicId == publicId, ct);

	public Task<bool> RegionNameExistsAsync(short? countryId, string name, long? exceptId, CancellationToken ct = default)
	{
		var lower = name.Trim().ToLower();
		return _db.ShowcaseRegions.AnyAsync(r => r.CountryId == countryId && r.Name.ToLower() == lower && r.Id != exceptId, ct);
	}

	public void AddRegion(ShowcaseRegion region) => _db.ShowcaseRegions.Add(region);

	public void RemoveRegion(ShowcaseRegion region) => _db.ShowcaseRegions.Remove(region);

	public async Task<IReadOnlyList<Bathhouse>> ListAutomaticRegionBathhousesAsync(CancellationToken ct = default)
		=> await _db.Bathhouses.Where(b => !b.IsRegionManual).ToListAsync(ct);

	public Task ClearRegionAsync(long regionId, CancellationToken ct = default)
		=> _db.Bathhouses.IgnoreQueryFilters().Where(b => b.RegionId == regionId)
			.ExecuteUpdateAsync(u => u.SetProperty(b => b.RegionId, (long?)null).SetProperty(b => b.IsRegionManual, false), ct);

	public async Task<short?> FindCountryIdAsync(string code, CancellationToken ct = default)
	{
		var upper = code.Trim().ToUpperInvariant();
		return await _db.Countries.Where(c => c.Code == upper).Select(c => (short?)c.Id).FirstOrDefaultAsync(ct);
	}

	public async Task<IReadOnlyDictionary<short, string>> CountryCodesAsync(CancellationToken ct = default)
		=> await _db.Countries.AsNoTracking().ToDictionaryAsync(c => c.Id, c => c.Code, ct);

	public Task<List<ShowcaseEntry>> ListShowcaseAsync(ShowcaseSection section, CancellationToken ct = default)
		=> _db.ShowcaseEntries.Include(e => e.Bathhouse).Where(e => e.Section == section).OrderBy(e => e.SortOrder).ToListAsync(ct);

	public void AddShowcaseEntry(ShowcaseEntry entry) => _db.ShowcaseEntries.Add(entry);

	public void RemoveShowcaseEntry(ShowcaseEntry entry) => _db.ShowcaseEntries.Remove(entry);

	public async Task<IReadOnlyDictionary<long, Guid>> FindPropertyPublicIdsAsync(IReadOnlyCollection<long> propertyIds, CancellationToken ct = default)
		=> await _db.Properties.IgnoreQueryFilters().AsNoTracking().Where(p => propertyIds.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.PublicId, ct);

	public async Task<IReadOnlyList<TariffPeriod>> FindSeasonPeriodsAsync(long propertyId, CancellationToken ct = default)
		=> await _db.PricingSeasons.AsNoTracking().Where(s => s.PropertyId == propertyId).SelectMany(s => s.TariffPeriods).ToListAsync(ct);

	public async Task<IReadOnlyList<PriceListPeriodPrice>> ListPeriodPricesAsync(long priceListId, CancellationToken ct = default)
		=> await _db.Set<PriceListPeriodPrice>().AsNoTracking().Where(p => p.PriceListId == priceListId).ToListAsync(ct);

	/// <summary>Anche nel cestino; mai quelli eliminati definitivamente.</summary>
	private IQueryable<Property> Properties() => _db.Properties.IgnoreQueryFilters().Where(p => p.PurgedAt == null);
}
