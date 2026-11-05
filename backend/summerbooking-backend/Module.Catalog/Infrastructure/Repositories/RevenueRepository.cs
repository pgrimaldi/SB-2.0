using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class RevenueRepository : IRevenueRepository
{
	private readonly DbContext _db;

	public RevenueRepository(DbContext db) => _db = db;

	public Task<RevenueSettings?> FindSettingsAsync(long propertyId, CancellationToken ct = default)
		=> _db.RevenueSettings.Include(s => s.DiscountedSpots).FirstOrDefaultAsync(s => s.PropertyId == propertyId, ct);

	public void AddSettings(RevenueSettings settings) => _db.RevenueSettings.Add(settings);

	public Task<List<RevenueStandardPeriod>> ListStandardPeriodsAsync(CancellationToken ct = default)
		=> _db.RevenueStandardPeriods.OrderBy(p => p.SortOrder).ThenBy(p => p.Id).ToListAsync(ct);

	public void AddStandardPeriod(RevenueStandardPeriod period) => _db.RevenueStandardPeriods.Add(period);

	public void RemoveStandardPeriod(RevenueStandardPeriod period) => _db.RevenueStandardPeriods.Remove(period);

	public Task<RevenueWeights> GetWeightsAsync(CancellationToken ct = default)
		=> _db.RevenueWeights.SingleAsync(w => w.Id == RevenueWeights.SingletonId, ct);

	public Task<List<SuggestedPrice>> ListSuggestedPricesAsync(long priceListId, DateOnly from, DateOnly to, CancellationToken ct = default)
		=> _db.SuggestedPrices.Where(p => p.PriceListId == priceListId && p.PriceDate >= from && p.PriceDate <= to).ToListAsync(ct);

	public void AddSuggestedPrice(SuggestedPrice price) => _db.SuggestedPrices.Add(price);
}
