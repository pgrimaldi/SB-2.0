using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Microsoft.Extensions.Logging;
using Module.Catalog.Application.Services;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Repositories;

/// <summary>
/// Salvataggio di Catalog. Se cambiano dati che la ricerca pubblica mostra (vetrina, stabilimento, listini, stagione,
/// impostazioni online), dopo il salvataggio si ricalcola la proiezione di quegli stabilimenti; un errore nel ricalcolo
/// non annulla il salvataggio (lo ripara la ricostruzione notturna).
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
	private readonly DbContext _db;
	private readonly ISearchProjection _search;
	private readonly ILogger<UnitOfWork> _logger;

	private readonly ConfigSnapshotService _snapshots;

	public UnitOfWork(DbContext db, ISearchProjection search, ConfigSnapshotService snapshots, ILogger<UnitOfWork> logger)
	{
		_db = db;
		_search = search;
		_snapshots = snapshots;
		_logger = logger;
	}

	public async Task SaveChangesAsync(CancellationToken ct = default)
	{
		var changed = _db.ChangeTracker.Entries()
			.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted && e.Entity is not ConfigSnapshot)
			.ToList();
		var kinds = changed.SelectMany(e => SnapshotTriggers.For(e.Entity.GetType())).ToHashSet();
		var touched = _db.ChangeTracker.Entries()
			.Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
			.Select(e => e.Entity)
			.Where(e => e is Property or Bathhouse or BathhousePhoto or BathhouseAmenity or BathhouseBadge or PriceList or PriceListPeriodPrice
				or PricingSeason or TariffPeriod or OnlineBookingSettings)
			.ToList();
		await _db.SaveChangesAsync(ct);
		if (kinds.Count > 0)
			await RefreshSnapshotsAsync(kinds, ct);
		if (touched.Count == 0)
			return;

		var scope = new SearchScope(
			touched.Select(e => e switch { Property p => p.Id, Bathhouse b => b.PropertyId, PriceList l => l.PropertyId, PricingSeason s => s.PropertyId,
				OnlineBookingSettings o => o.PropertyId, _ => 0L }).Where(id => id > 0).Distinct().ToList(),
			touched.Select(e => e switch { BathhousePhoto p => p.BathhouseId, BathhouseAmenity a => a.BathhouseId, BathhouseBadge b => b.BathhouseId, _ => 0L })
				.Where(id => id > 0).Distinct().ToList(),
			touched.OfType<PriceListPeriodPrice>().Select(p => p.PriceListId).Distinct().ToList(),
			touched.OfType<TariffPeriod>().Select(p => p.SeasonId).Distinct().ToList());
		try
		{
			await _search.RefreshAsync(scope, ct);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Ricalcolo della ricerca pubblica non riuscito: lo ripara la ricostruzione notturna");
		}
	}

	/// <summary>
	/// Snapshot di configurazione degli stabilimenti toccati. Lo stabilimento si legge dalle entita' tracciate che lo
	/// hanno (le figlie arrivano con la loro radice); un errore non annulla il salvataggio (lo ripara la notte).
	/// </summary>
	private async Task RefreshSnapshotsAsync(IReadOnlyCollection<ConfigSnapshotKind> kinds, CancellationToken ct)
	{
		var propertyIds = _db.ChangeTracker.Entries()
			.Select(e => e.Entity switch
			{
				Property p => p.Id,
				_ => e.Metadata.FindProperty("PropertyId") is { } prop && e.Property(prop.Name).CurrentValue is long id ? id : 0L,
			})
			.Where(id => id > 0).Distinct().ToList();
		if (propertyIds.Count == 0)
			return;
		try
		{
			await _snapshots.RebuildAsync(propertyIds, kinds, ct: ct);
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			_logger.LogError(ex, "Snapshot di configurazione non aggiornati: li ripara la ricostruzione notturna");
		}
	}
}

public sealed class PropertyRepository : IPropertyRepository
{
	private readonly DbContext _db;

	public PropertyRepository(DbContext db) => _db = db;

	public async Task<long?> FindIdAsync(Guid publicId, CancellationToken ct = default)
		=> await _db.Properties.AsNoTracking()
			.Where(p => p.PublicId == publicId)
			.Select(p => (long?)p.Id)
			.FirstOrDefaultAsync(ct);
}

public sealed class InventoryItemRepository : IInventoryItemRepository
{
	private readonly DbContext _db;

	public InventoryItemRepository(DbContext db) => _db = db;

	public async Task<(IReadOnlyList<InventoryItem> Items, int Total)> ListAsync(InventoryItemFilter filter, int page, int pageSize, CancellationToken ct = default)
	{
		var items = _db.InventoryItems.AsNoTracking().Where(i => i.PropertyId == filter.PropertyId);
		if (filter.Search is { } search)
			items = items.Where(i => EF.Functions.ILike(i.Name, LikePattern.Contains(search)));

		var total = await items.CountAsync(ct);
		var sorted = (filter.SortBy, filter.Descending) switch
		{
			(InventorySortFields.TotalQuantity, false) => items.OrderBy(i => i.TotalQuantity),
			(InventorySortFields.TotalQuantity, true) => items.OrderByDescending(i => i.TotalQuantity),
			(InventorySortFields.LowStockThreshold, false) => items.OrderBy(i => i.LowStockThreshold),
			(InventorySortFields.LowStockThreshold, true) => items.OrderByDescending(i => i.LowStockThreshold),
			(_, true) => items.OrderByDescending(i => i.Name),
			_ => items.OrderBy(i => i.Name),
		};
		var result = await sorted.ThenBy(i => i.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
		return (result, total);
	}

	public async Task<IReadOnlyList<InventoryItem>> ListAllAsync(long propertyId, CancellationToken ct = default)
		=> await _db.InventoryItems.AsNoTracking().Where(i => i.PropertyId == propertyId).OrderBy(i => i.Name).ToListAsync(ct);

	public Task<InventoryItem?> FindAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> _db.InventoryItems.FirstOrDefaultAsync(i => i.PropertyId == propertyId && i.PublicId == publicId, ct);

	public async Task<IReadOnlyList<InventoryItem>> FindManyAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> await _db.InventoryItems.Where(i => i.PropertyId == propertyId && publicIds.Contains(i.PublicId)).ToListAsync(ct);

	public Task<bool> NameExistsAsync(long propertyId, string name, long? exceptId, CancellationToken ct = default)
	{
		var lower = name.ToLower();
		return _db.InventoryItems.AnyAsync(i => i.PropertyId == propertyId && i.Name.ToLower() == lower && i.Id != exceptId, ct);
	}

	public async Task<IReadOnlyList<InventoryItemUsage>> ListUsagesAsync(long inventoryItemId, CancellationToken ct = default)
	{
		var own = await (
			from item in _db.PriceListItems
			join list in _db.PriceLists on item.PriceListId equals list.Id
			where item.InventoryItemId == inventoryItemId
			select new InventoryItemUsage(list.Name, null)).ToListAsync(ct);

		var agreements = await (
			from item in _db.AgreementPriceListItems
			join list in _db.AgreementPriceLists on item.PriceListId equals list.Id
			join partner in _db.AgreementPartners on list.PartnerId equals partner.Id
			where item.InventoryItemId == inventoryItemId
			select new InventoryItemUsage(list.Name, partner.Name)).ToListAsync(ct);

		return [.. own, .. agreements];
	}

	public void Add(InventoryItem item) => _db.InventoryItems.Add(item);

	public void Remove(InventoryItem item) => _db.InventoryItems.Remove(item);
}
