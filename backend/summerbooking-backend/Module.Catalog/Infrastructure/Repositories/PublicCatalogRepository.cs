using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;
using Npgsql;
using NpgsqlTypes;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class PublicCatalogRepository : IPublicCatalogRepository
{
	private static readonly System.Text.RegularExpressions.Regex CapPrefix = new(@"^\d{5}\s*", System.Text.RegularExpressions.RegexOptions.Compiled);

	private readonly DbContext _db;

	public PublicCatalogRepository(DbContext db) => _db = db;

	public IQueryable<SearchBathhouse> Published(PublicSearchFilter filter)
	{
		var rows = _db.SearchBathhouses.AsNoTracking().Where(s => s.IsPublished);
		if (filter.Text is { } text)
		{
			var pattern = LikePattern.Contains(text);
			rows = rows.Where(s => EF.Functions.ILike(s.Name, pattern) || s.City != null && EF.Functions.ILike(s.City, pattern)
				|| s.Province != null && EF.Functions.ILike(s.Province, pattern) || s.Region != null && EF.Functions.ILike(s.Region, pattern));
		}
		if (filter.RegionId is { } region)
			rows = rows.Where(s => s.RegionId == region);
		foreach (var amenity in filter.AmenityIds)
		{
			var id = (short)amenity;
			rows = rows.Where(s => s.AmenityIds.Contains(id));
		}
		if (filter.OnlineBookableOnly)
			rows = rows.Where(s => s.IsOnlineBookable);
		return rows;
	}

	public async Task<(IReadOnlyList<PublicSearchRow> Items, int Total)> SearchAsync(
		PublicSearchFilter filter, string sortBy, bool descending, int page, int pageSize, IReadOnlyDictionary<long, decimal>? prices, CancellationToken ct = default)
	{
		var rows = Published(filter);
		var total = await rows.CountAsync(ct);
		List<SearchBathhouse> items;
		if (sortBy == "price" && prices is not null)
		{
			// prezzo per le date cercate: si ordina in memoria sugli id già filtrati (le strutture senza prezzo in fondo)
			var all = await rows.Select(s => new { s.BathhousePublicId, s.PropertyId, s.LikeCount, s.Name }).ToListAsync(ct);
			var ordered = all.OrderBy(s => prices.ContainsKey(s.PropertyId) ? 0 : 1)
				.ThenBy(s => descending ? -prices.GetValueOrDefault(s.PropertyId) : prices.GetValueOrDefault(s.PropertyId))
				.ThenByDescending(s => s.LikeCount).ThenBy(s => s.Name)
				.Skip((page - 1) * pageSize).Take(pageSize).Select(s => s.BathhousePublicId).ToList();
			var byId = (await rows.Where(s => ordered.Contains(s.BathhousePublicId)).ToListAsync(ct)).ToDictionary(s => s.BathhousePublicId);
			items = ordered.Select(id => byId[id]).ToList();
		}
		else
		{
			IOrderedQueryable<SearchBathhouse> sorted;
			if (sortBy == "distance" && filter.Near is { } near)
			{
				// distanza approssimata sul piano (basta per ordinare); le strutture senza coordinate in fondo
				var scale = Math.Cos(near.Latitude * Math.PI / 180);
				sorted = rows.OrderBy(s => s.Latitude == null || s.Longitude == null)
					.ThenBy(s => (s.Latitude!.Value - near.Latitude) * (s.Latitude!.Value - near.Latitude)
						+ (s.Longitude!.Value - near.Longitude) * scale * (s.Longitude!.Value - near.Longitude) * scale);
			}
			else
				sorted = (sortBy, descending) switch
				{
					("name", false) => rows.OrderBy(s => s.Name),
					("name", true) => rows.OrderByDescending(s => s.Name),
					("price", false) => rows.OrderBy(s => s.MinPrice == null).ThenBy(s => s.MinPrice),
					("price", true) => rows.OrderBy(s => s.MinPrice == null).ThenByDescending(s => s.MinPrice),
					_ => rows.OrderByDescending(s => s.LikeCount).ThenBy(s => s.SubscribedSince == null).ThenBy(s => s.SubscribedSince),
				};
			items = await sorted.ThenBy(s => s.Name).ThenBy(s => s.BathhousePublicId).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
		}
		return (items.Select(s => new PublicSearchRow(s, Distance(s, filter.Near))).ToList(), total);
	}

	public async Task<IReadOnlyDictionary<int, int>> CountAmenitiesAsync(PublicSearchFilter filter, CancellationToken ct = default)
	{
		var lists = await Published(filter).Select(s => s.AmenityIds).ToListAsync(ct);
		return lists.SelectMany(a => a).GroupBy(a => (int)a).ToDictionary(g => g.Key, g => g.Count());
	}

	public async Task<IReadOnlyList<long>> ListPropertyIdsAsync(PublicSearchFilter filter, CancellationToken ct = default)
		=> await Published(filter).Where(s => s.IsOnlineBookable).Select(s => s.PropertyId).Distinct().ToListAsync(ct);

	public async Task<IReadOnlyDictionary<long, decimal>> MinPricesAsync(IReadOnlyCollection<long> propertyIds, DateOnly from, DateOnly to, CancellationToken ct = default)
	{
		if (propertyIds.Count == 0)
			return new Dictionary<long, decimal>();
		var rows = await _db.Database.SqlQueryRaw<PropertyPrice>("""
			WITH day_groups AS (
			    SELECT p.property_id, (
			        SELECT tp.period_group_key FROM catalog.pricing_tariff_periods tp
			        JOIN catalog.pricing_seasons ps ON ps.id = tp.season_id
			        WHERE ps.property_id = p.property_id AND tp.deleted_at IS NULL AND tp.starts_on <= d.day AND d.day <= tp.ends_on
			        ORDER BY tp.ends_on - tp.starts_on, tp.id LIMIT 1) AS period_group
			    FROM unnest(@ids) AS p(property_id)
			    CROSS JOIN generate_series(@from::date, @to::date, interval '1 day') AS d(day))
			SELECT g.property_id, min(coalesce(pp.half_day_price, pp.daily_price)) AS price
			FROM (SELECT DISTINCT property_id, period_group FROM day_groups WHERE period_group IS NOT NULL) g
			JOIN catalog.pricing_price_lists pl ON pl.property_id = g.property_id AND pl.kind = 'package' AND pl.sales_channel <> 'manager_only' AND pl.deleted_at IS NULL
			JOIN catalog.pricing_price_list_period_prices pp ON pp.price_list_id = pl.id AND pp.tariff_period_group = g.period_group AND pp.deleted_at IS NULL
			WHERE coalesce(pp.half_day_price, pp.daily_price) > 0
			GROUP BY g.property_id
			""",
			new NpgsqlParameter("ids", NpgsqlDbType.Array | NpgsqlDbType.Bigint) { Value = propertyIds.ToArray() },
			new NpgsqlParameter("from", from), new NpgsqlParameter("to", to)).ToListAsync(ct);
		return rows.ToDictionary(r => r.PropertyId, r => r.Price);
	}

	public async Task<IReadOnlyList<SearchBathhouse>> FindPublishedAsync(IReadOnlyCollection<Guid> bathhouseIds, CancellationToken ct = default)
		=> await _db.SearchBathhouses.AsNoTracking().Where(s => s.IsPublished && bathhouseIds.Contains(s.BathhousePublicId)).ToListAsync(ct);

	public Task<SearchBathhouse?> FindPublishedBySlugAsync(string slug, CancellationToken ct = default)
		=> _db.SearchBathhouses.AsNoTracking().FirstOrDefaultAsync(s => s.IsPublished && s.Slug == slug, ct);

	public async Task<IReadOnlyList<PlaceSuggestion>> SuggestPlacesAsync(string text, int limit, CancellationToken ct = default)
	{
		// molte città importate hanno il CAP davanti («55049 Viareggio»): si cerca nel testo e si raggruppa senza CAP
		var pattern = LikePattern.Contains(text);
		var rows = await _db.SearchBathhouses.AsNoTracking()
			.Where(s => s.IsPublished && s.City != null && EF.Functions.ILike(s.City, pattern))
			.Select(s => new { s.City, s.Province, s.Region, s.Latitude, s.Longitude })
			.ToListAsync(ct);
		return rows
			.Select(r => (City: CapPrefix.Replace(r.City!, "").Trim(), r.Province, r.Region, r.Latitude, r.Longitude))
			.Where(r => r.City.Contains(text, StringComparison.OrdinalIgnoreCase))
			.GroupBy(r => (City: r.City.ToLowerInvariant(), Province: r.Province?.ToLowerInvariant()))
			.Select(g => new PlaceSuggestion(g.First().City, g.First().Province, g.First().Region,
				g.Average(r => r.Latitude), g.Average(r => r.Longitude), g.Count()))
			.OrderBy(p => p.City.StartsWith(text, StringComparison.OrdinalIgnoreCase) ? 0 : 1).ThenByDescending(p => p.Count).ThenBy(p => p.City)
			.Take(limit).ToList();
	}

	public async Task<IReadOnlyList<SearchBathhouse>> SuggestBathhousesAsync(string text, int limit, CancellationToken ct = default)
	{
		var pattern = LikePattern.Contains(text);
		return await _db.SearchBathhouses.AsNoTracking().Where(s => s.IsPublished && EF.Functions.ILike(s.Name, pattern))
			.OrderByDescending(s => s.LikeCount).ThenBy(s => s.Name).Take(limit).ToListAsync(ct);
	}

	public async Task<IReadOnlyDictionary<long, int>> CountByRegionAsync(CancellationToken ct = default)
		=> await _db.SearchBathhouses.AsNoTracking().Where(s => s.IsPublished && s.RegionId != null).GroupBy(s => s.RegionId!.Value)
			.Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, ct);

	public async Task<IReadOnlyDictionary<int, string>> AmenityCodesAsync(CancellationToken ct = default)
		=> await _db.Amenities.AsNoTracking().ToDictionaryAsync(a => a.Id, a => a.Code, ct);

	public async Task<IReadOnlyDictionary<string, int>> AmenityIdsAsync(CancellationToken ct = default)
		=> await _db.Amenities.AsNoTracking().ToDictionaryAsync(a => a.Code, a => a.Id, ct);

	public async Task<IReadOnlyList<ServiceSaleItem>> ListOnlineServicesAsync(long propertyId, CancellationToken ct = default)
		=> await _db.ServiceSaleItems.AsNoTracking().Include(s => s.Translations).Include(s => s.Variants).AsSplitQuery()
			.Where(s => s.PropertyId == propertyId && s.IsPurchasable).OrderBy(s => s.Kind).ThenByDescending(s => s.IsHighlighted).ThenBy(s => s.Name)
			.ToListAsync(ct);

	public async Task<IReadOnlyList<TableService>> ListOnlineTablesAsync(long propertyId, CancellationToken ct = default)
		=> await _db.TableServices.AsNoTracking().Include(t => t.Translations)
			.Where(t => t.PropertyId == propertyId && t.IsOnlineBookable).OrderByDescending(t => t.IsHighlighted).ThenBy(t => t.Name).ToListAsync(ct);

	public async Task<IReadOnlyList<PriceList>> ListOnlinePackagesAsync(long propertyId, CancellationToken ct = default)
		=> await _db.PriceLists.AsNoTracking().Include(p => p.Translations).Include(p => p.Items).AsSplitQuery()
			.Where(p => p.PropertyId == propertyId && p.Kind == PriceListKind.Package && p.SalesChannel != SalesChannel.ManagerOnly)
			.OrderByDescending(p => p.IsDefaultOnline).ThenBy(p => p.Name).ToListAsync(ct);

	public async Task<IReadOnlyList<ServiceSaleItem>> FindServicesAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> await _db.ServiceSaleItems.AsNoTracking().Include(s => s.Variants).Include(s => s.PeriodPrices).Include(s => s.TimeSlots).Include(s => s.HourlyOptions).Include(s => s.Translations)
			.AsSplitQuery().Where(s => s.PropertyId == propertyId && publicIds.Contains(s.PublicId)).ToListAsync(ct);

	public Task<Discount?> FindDiscountByCodeAsync(long propertyId, string code, CancellationToken ct = default)
	{
		var lower = code.Trim().ToLower();
		return Discounts().FirstOrDefaultAsync(d => d.PropertyId == propertyId && d.Code.ToLower() == lower, ct);
	}

	public async Task<IReadOnlyList<Discount>> ListAutomaticDiscountsAsync(long propertyId, CancellationToken ct = default)
		=> await Discounts().Where(d => d.PropertyId == propertyId && !d.RequiresCode && d.IsActive).OrderByDescending(d => d.Id).ToListAsync(ct);

	private IQueryable<Discount> Discounts()
		=> _db.Discounts.AsNoTracking().Include(d => d.ValidityWindows).Include(d => d.RestrictedSectors).Include(d => d.TimeRanges).AsSplitQuery();

	public Task<RevenueSettings?> FindRevenueSettingsAsync(long propertyId, CancellationToken ct = default)
		=> _db.RevenueSettings.AsNoTracking().Include(r => r.DiscountedSpots).FirstOrDefaultAsync(r => r.PropertyId == propertyId, ct);

	public async Task<bool> CountBannerViewAsync(long propertyId, short limit, CancellationToken ct = default)
		=> await _db.RevenueSettings.Where(r => r.PropertyId == propertyId && r.DailyBannerShownCount < limit)
			.ExecuteUpdateAsync(u => u.SetProperty(r => r.DailyBannerShownCount, r => (short)(r.DailyBannerShownCount + 1)), ct) > 0;

	public Task<int> ResetBannerCountersAsync(CancellationToken ct = default)
		=> _db.RevenueSettings.Where(r => r.DailyBannerShownCount != 0).ExecuteUpdateAsync(u => u.SetProperty(r => r.DailyBannerShownCount, (short)0), ct);

	public Task<SuggestedPrice?> FindAppliedDiscountAsync(long propertyId, DateOnly date, CancellationToken ct = default)
		=> _db.SuggestedPrices.AsNoTracking()
			.Where(p => p.PropertyId == propertyId && p.PriceDate == date && p.AppliedMode == SuggestedPriceMode.Discount)
			.OrderByDescending(p => p.DiscountPercentage).FirstOrDefaultAsync(ct);

	private static double? Distance(SearchBathhouse s, (double Latitude, double Longitude)? near)
	{
		if (near is not { } n || s.Latitude is not { } lat || s.Longitude is not { } lon)
			return null;
		// formula dell'emisenoverso, in km
		const double R = 6371;
		var dLat = (lat - n.Latitude) * Math.PI / 180;
		var dLon = (lon - n.Longitude) * Math.PI / 180;
		var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) + Math.Cos(n.Latitude * Math.PI / 180) * Math.Cos(lat * Math.PI / 180) * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
		return Math.Round(2 * R * Math.Asin(Math.Sqrt(a)), 1);
	}

	private sealed record PropertyPrice(long PropertyId, decimal Price);
}
