using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class ServiceCatalogRepository : IServiceCatalogRepository
{
	private readonly DbContext _db;

	public ServiceCatalogRepository(DbContext db) => _db = db;

	public async Task<(IReadOnlyList<ServiceSaleItem> Items, int Total)> ListAsync(ServiceFilter filter, int page, int pageSize, CancellationToken ct = default)
	{
		var items = _db.ServiceSaleItems.AsNoTracking().Where(s => s.PropertyId == filter.PropertyId);
		if (filter.Kind is { } kind)
			items = items.Where(s => s.Kind == kind);
		if (filter.Search is { } search)
			items = items.Where(s => EF.Functions.ILike(s.Name, LikePattern.Contains(search)));

		var total = await items.CountAsync(ct);
		var sorted = (filter.SortBy, filter.Descending) switch
		{
			(ServiceSortFields.Price, false) => items.OrderBy(s => s.Price),
			(ServiceSortFields.Price, true) => items.OrderByDescending(s => s.Price),
			(_, true) => items.OrderByDescending(s => s.Name),
			_ => items.OrderBy(s => s.Name),
		};
		var result = await sorted.ThenBy(s => s.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
		return (result, total);
	}

	public async Task<IReadOnlyList<ServiceSaleItem>> ListAllAsync(long propertyId, CancellationToken ct = default)
		=> await _db.ServiceSaleItems.AsNoTracking().Where(s => s.PropertyId == propertyId).OrderBy(s => s.Kind).ThenBy(s => s.Name).ToListAsync(ct);

	public Task<ServiceSaleItem?> FindAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> _db.ServiceSaleItems
			.Include(s => s.Variants).Include(s => s.Translations).Include(s => s.BookingOptions)
			.Include(s => s.PeriodPrices).Include(s => s.Sectors).Include(s => s.TimeSlots).Include(s => s.HourlyOptions)
			.AsSplitQuery()
			.FirstOrDefaultAsync(s => s.PropertyId == propertyId && s.PublicId == publicId, ct);

	public async Task<IReadOnlyList<ServiceSaleItem>> FindManyAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> await _db.ServiceSaleItems.Where(s => s.PropertyId == propertyId && publicIds.Contains(s.PublicId)).ToListAsync(ct);

	public async Task<IReadOnlyDictionary<Guid, long>> FindIdsAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> publicIds.Count == 0
			? new Dictionary<Guid, long>()
			: await _db.ServiceSaleItems.AsNoTracking().Where(s => s.PropertyId == propertyId && publicIds.Contains(s.PublicId))
				.ToDictionaryAsync(s => s.PublicId, s => s.Id, ct);

	public async Task<IReadOnlyDictionary<long, Guid>> FindPublicIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct = default)
		=> ids.Count == 0
			? new Dictionary<long, Guid>()
			: await _db.ServiceSaleItems.AsNoTracking().Where(s => ids.Contains(s.Id)).ToDictionaryAsync(s => s.Id, s => s.PublicId, ct);

	public void Add(ServiceSaleItem item) => _db.ServiceSaleItems.Add(item);

	public void Remove(ServiceSaleItem item) => _db.ServiceSaleItems.Remove(item);

	public async Task<int?> FindIconIdAsync(Guid publicId, CancellationToken ct = default)
		=> await _db.ServiceTypeIcons.AsNoTracking().Where(i => i.PublicId == publicId && i.IsActive).Select(i => (int?)i.Id).FirstOrDefaultAsync(ct);

	public async Task<IReadOnlyDictionary<int, (Guid PublicId, long AssetId)>> FindIconsAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
		=> ids.Count == 0
			? new Dictionary<int, (Guid, long)>()
			: (await _db.ServiceTypeIcons.AsNoTracking().Where(i => ids.Contains(i.Id)).Select(i => new { i.Id, i.PublicId, i.IconAssetId }).ToListAsync(ct))
				.ToDictionary(i => i.Id, i => (i.PublicId, i.IconAssetId));

	public async Task<IReadOnlyList<AutomaticPrice>> ListAutomaticAsync(long propertyId, CancellationToken ct = default)
		=> await AutomaticWithChildren().AsNoTracking().Where(a => a.PropertyId == propertyId).OrderBy(a => a.Name).ToListAsync(ct);

	public Task<AutomaticPrice?> FindAutomaticAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> AutomaticWithChildren().FirstOrDefaultAsync(a => a.PropertyId == propertyId && a.PublicId == publicId, ct);

	public async Task<IReadOnlyList<AutomaticPrice>> ListAutomaticForServiceAsync(long saleItemId, CancellationToken ct = default)
		=> await _db.AutomaticPrices.Include(a => a.Services).Where(a => a.Services.Any(s => s.SaleItemId == saleItemId)).ToListAsync(ct);

	public void AddAutomatic(AutomaticPrice price) => _db.AutomaticPrices.Add(price);

	public void RemoveAutomatic(AutomaticPrice price) => _db.AutomaticPrices.Remove(price);

	public async Task<IReadOnlyList<TableService>> ListTablesAsync(long propertyId, CancellationToken ct = default)
		=> await TablesWithChildren().AsNoTracking().Where(t => t.PropertyId == propertyId).OrderBy(t => t.Name).ToListAsync(ct);

	public Task<TableService?> FindTableAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> TablesWithChildren().FirstOrDefaultAsync(t => t.PropertyId == propertyId && t.PublicId == publicId, ct);

	public void AddTable(TableService service) => _db.TableServices.Add(service);

	public void RemoveTable(TableService service) => _db.TableServices.Remove(service);

	private IQueryable<AutomaticPrice> AutomaticWithChildren()
		=> _db.AutomaticPrices.Include(a => a.Translations).Include(a => a.Services).Include(a => a.Periods).ThenInclude(p => p.TimeRanges)
			.Include(a => a.Sectors).AsSplitQuery();

	private IQueryable<TableService> TablesWithChildren()
		=> _db.TableServices.Include(t => t.Translations).Include(t => t.TableTypes)
			.Include(t => t.Availabilities).ThenInclude(a => a.Slots).AsSplitQuery();
}
