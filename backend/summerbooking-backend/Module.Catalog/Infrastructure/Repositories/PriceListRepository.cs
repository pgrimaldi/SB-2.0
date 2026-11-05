using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class PriceListRepository : IPriceListRepository
{
	private readonly DbContext _db;

	public PriceListRepository(DbContext db) => _db = db;

	public async Task<(IReadOnlyList<PriceList> Items, int Total)> ListAsync(PriceListFilter filter, int page, int pageSize, CancellationToken ct = default)
	{
		var lists = _db.PriceLists.AsNoTracking().Include(p => p.CalculationMethod).Where(p => p.PropertyId == filter.PropertyId);
		if (filter.Kind is { } kind)
			lists = lists.Where(p => p.Kind == kind);
		if (filter.Search is { } search)
			lists = lists.Where(p => EF.Functions.ILike(p.Name, LikePattern.Contains(search)));
		var total = await lists.CountAsync(ct);
		var sorted = filter.Descending ? lists.OrderByDescending(p => p.Name) : lists.OrderBy(p => p.Name);
		return (await sorted.ThenBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct), total);
	}

	public async Task<IReadOnlyList<PriceList>> ListAllAsync(long propertyId, PriceListKind? kind, CancellationToken ct = default)
		=> await _db.PriceLists.AsNoTracking().Where(p => p.PropertyId == propertyId && (kind == null || p.Kind == kind))
			.OrderBy(p => p.Kind).ThenBy(p => p.Name).ToListAsync(ct);

	public async Task<IReadOnlyList<PriceList>> ListFullAsync(long propertyId, CancellationToken ct = default)
		=> await Full().Where(p => p.PropertyId == propertyId).OrderBy(p => p.Kind).ThenBy(p => p.Name).ToListAsync(ct);

	public Task<PriceList?> FindAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> Full().FirstOrDefaultAsync(p => p.PropertyId == propertyId && p.PublicId == publicId, ct);

	public async Task<IReadOnlyList<PriceList>> FindManyAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> await Full().Where(p => p.PropertyId == propertyId && publicIds.Contains(p.PublicId)).ToListAsync(ct);

	public async Task<IReadOnlyDictionary<Guid, long>> FindIdsAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, PriceListKind? kind, CancellationToken ct = default)
		=> publicIds.Count == 0
			? new Dictionary<Guid, long>()
			: await _db.PriceLists.AsNoTracking()
				.Where(p => p.PropertyId == propertyId && publicIds.Contains(p.PublicId) && (kind == null || p.Kind == kind))
				.ToDictionaryAsync(p => p.PublicId, p => p.Id, ct);

	public async Task<IReadOnlyDictionary<long, Guid>> FindPublicIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default)
		=> ids.Count == 0
			? new Dictionary<long, Guid>()
			: await _db.PriceLists.AsNoTracking().Where(p => ids.Contains(p.Id)).ToDictionaryAsync(p => p.Id, p => p.PublicId, ct);

	public Task<bool> NameExistsAsync(long propertyId, PriceListKind kind, string name, long? exceptId, CancellationToken ct = default)
	{
		var lower = name.ToLower();
		return _db.PriceLists.AnyAsync(p => p.PropertyId == propertyId && p.Kind == kind && p.Name.ToLower() == lower && p.Id != exceptId, ct);
	}

	public async Task<IReadOnlyList<PriceList>> ListDefaultsAsync(long propertyId, PriceListKind kind, long exceptId, CancellationToken ct = default)
		=> await _db.PriceLists.Where(p => p.PropertyId == propertyId && p.Kind == kind && p.Id != exceptId && (p.IsDefault || p.IsDefaultOnline))
			.ToListAsync(ct);

	public async Task<IReadOnlyList<PriceList>> ListLinkedAdditionalsAsync(long packageId, CancellationToken ct = default)
		=> await _db.PriceLists.Include(p => p.Items)
			.Where(p => p.Items.Any(i => i.ItemKind == PackageItemKind.LinkedPackage && i.LinkedPriceListId == packageId)).ToListAsync(ct);

	public void Add(PriceList priceList) => _db.PriceLists.Add(priceList);

	public void Remove(PriceList priceList) => _db.PriceLists.Remove(priceList);

	public async Task<IReadOnlyList<ReducedSeason>> ListReducedSeasonsAsync(long propertyId, CancellationToken ct = default)
		=> await _db.ReducedSeasons.AsNoTracking().Where(s => s.PropertyId == propertyId).OrderBy(s => s.StartsOn).ThenBy(s => s.Name).ToListAsync(ct);

	public Task<ReducedSeason?> FindReducedSeasonAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> _db.ReducedSeasons.FirstOrDefaultAsync(s => s.PropertyId == propertyId && s.PublicId == publicId, ct);

	public Task<int> CountReducedPricesAsync(long reducedSeasonId, CancellationToken ct = default)
		=> _db.PriceListReducedPrices.CountAsync(p => p.ReducedSeasonId == reducedSeasonId, ct);

	public async Task RemoveReducedPricesAsync(long reducedSeasonId, CancellationToken ct = default)
		=> _db.PriceListReducedPrices.RemoveRange(await _db.PriceListReducedPrices.Where(p => p.ReducedSeasonId == reducedSeasonId).ToListAsync(ct));

	public void AddReducedSeason(ReducedSeason season) => _db.ReducedSeasons.Add(season);

	public void RemoveReducedSeason(ReducedSeason season) => _db.ReducedSeasons.Remove(season);

	public async Task<IReadOnlyList<CalculationMethod>> ListCalculationMethodsAsync(CancellationToken ct = default)
		=> await _db.CalculationMethods.AsNoTracking().ToListAsync(ct);

	public async Task<IReadOnlyDictionary<Guid, long>> FindInventoryIdsAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> publicIds.Count == 0
			? new Dictionary<Guid, long>()
			: await _db.InventoryItems.AsNoTracking().Where(i => i.PropertyId == propertyId && publicIds.Contains(i.PublicId))
				.ToDictionaryAsync(i => i.PublicId, i => i.Id, ct);

	public async Task<IReadOnlyDictionary<long, Guid>> FindInventoryPublicIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default)
		=> ids.Count == 0
			? new Dictionary<long, Guid>()
			: await _db.InventoryItems.AsNoTracking().Where(i => ids.Contains(i.Id)).ToDictionaryAsync(i => i.Id, i => i.PublicId, ct);

	private IQueryable<PriceList> Full()
		=> _db.PriceLists
			.Include(p => p.CalculationMethod)
			.Include(p => p.Items).Include(p => p.SectorPrices)
			.Include(p => p.PeriodPrices).ThenInclude(pp => pp.StayPriceOverrides)
			.Include(p => p.DatePrices).Include(p => p.ReducedPrices).Include(p => p.Translations)
			.AsSplitQuery();
}
