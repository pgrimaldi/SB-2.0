using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class SeasonRepository : ISeasonRepository
{
	private readonly DbContext _db;

	public SeasonRepository(DbContext db) => _db = db;

	public Task<PricingSeason?> FindAsync(long propertyId, CancellationToken ct = default)
		=> _db.PricingSeasons.Include(s => s.TariffPeriods).Include(s => s.NextSeasonPeriods).AsSplitQuery().FirstOrDefaultAsync(s => s.PropertyId == propertyId, ct);

	public void Add(PricingSeason season) => _db.PricingSeasons.Add(season);

	public async Task ReplacePeriodAsync(PricingSeason season, short groupKey, IReadOnlyList<TariffPeriod> ranges, CancellationToken ct = default)
	{
		// Il vincolo di non sovrapposizione non è differibile: i vecchi intervalli vanno chiusi prima di inserire i nuovi.
		await using var transaction = await _db.Database.BeginTransactionAsync(ct);
		foreach (var old in season.TariffPeriods.Where(p => p.PeriodGroupKey == groupKey).ToList())
			_db.TariffPeriods.Remove(old);
		await _db.SaveChangesAsync(ct);

		_db.TariffPeriods.AddRange(ranges);
		await _db.SaveChangesAsync(ct);
		await transaction.CommitAsync(ct);
	}

	public async Task<IReadOnlyList<string>> ListPeriodUsagesAsync(long propertyId, short groupKey, string periodName, CancellationToken ct = default)
	{
		var own = await (
			from price in _db.PriceListPeriodPrices
			join list in _db.PriceLists on price.PriceListId equals list.Id
			where list.PropertyId == propertyId && price.TariffPeriodGroup == groupKey
			select list.Name).Distinct().ToListAsync(ct);

		var agreements = await (
			from price in _db.AgreementPriceListPeriodPrices
			join list in _db.AgreementPriceLists on price.PriceListId equals list.Id
			join partner in _db.AgreementPartners on list.PartnerId equals partner.Id
			where partner.PropertyId == propertyId && price.TariffPeriodGroup == groupKey
			select partner.Name + " – " + list.Name).Distinct().ToListAsync(ct);

		return [.. own, .. agreements];
	}
}

public sealed class SectorRepository : ISectorRepository
{
	private readonly DbContext _db;

	public SectorRepository(DbContext db) => _db = db;

	public async Task<IReadOnlyList<MapSector>> ListAsync(long propertyId, CancellationToken ct = default)
		=> await _db.MapSectors.AsNoTracking().Where(s => s.PropertyId == propertyId).OrderBy(s => s.SortOrder).ThenBy(s => s.Id).ToListAsync(ct);

	public Task<MapSector?> FindAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> _db.MapSectors.FirstOrDefaultAsync(s => s.PropertyId == propertyId && s.PublicId == publicId, ct);

	public Task<bool> NameExistsAsync(long propertyId, string name, long? exceptId, CancellationToken ct = default)
	{
		var lower = name.ToLower();
		return _db.MapSectors.AnyAsync(s => s.PropertyId == propertyId && s.Name.ToLower() == lower && s.Id != exceptId, ct);
	}

	public async Task<short> NextSortOrderAsync(long propertyId, CancellationToken ct = default)
		=> (short)((await _db.MapSectors.Where(s => s.PropertyId == propertyId).MaxAsync(s => (short?)s.SortOrder, ct) ?? -1) + 1);

	public async Task<SectorUsage> CountUsagesAsync(long sectorId, CancellationToken ct = default) => new(
		await _db.MapSpots.CountAsync(s => s.SectorId == sectorId, ct),
		await _db.PriceListSectorPrices.CountAsync(p => p.SectorId == sectorId, ct)
			+ await _db.AgreementPriceListSectorPrices.CountAsync(p => p.SectorId == sectorId, ct),
		await _db.OnlineStayLimits.CountAsync(l => l.SectorId == sectorId, ct),
		await _db.DiscountSectors.CountAsync(d => d.SectorId == sectorId, ct));

	public void Add(MapSector sector) => _db.MapSectors.Add(sector);

	public void Remove(MapSector sector) => _db.MapSectors.Remove(sector);
}
