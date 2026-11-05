using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class LookupRepository : ILookupRepository
{
	private readonly DbContext _db;

	public LookupRepository(DbContext db) => _db = db;

	public async Task<IReadOnlyList<Country>> ListCountriesAsync(CancellationToken ct = default)
		=> await _db.Countries.AsNoTracking().Include(c => c.Translations)
			.Where(c => c.IsActive).OrderBy(c => c.SortOrder).ThenBy(c => c.Code).ToListAsync(ct);

	public async Task<IReadOnlyList<AmenityCategory>> ListAmenityCategoriesAsync(CancellationToken ct = default)
		=> await _db.AmenityCategories.AsNoTracking().Where(c => c.IsActive).OrderBy(c => c.SortOrder).ToListAsync(ct);

	public async Task<IReadOnlyList<Amenity>> ListAmenitiesAsync(CancellationToken ct = default)
		=> await _db.Amenities.AsNoTracking().Include(a => a.Translations)
			.Where(a => a.IsActive).OrderBy(a => a.SortOrder).ThenBy(a => a.Id).ToListAsync(ct);

	public async Task<IReadOnlyList<ServiceTypeIcon>> ListServiceIconsAsync(ServiceSaleKind? type, CancellationToken ct = default)
		=> await _db.ServiceTypeIcons.AsNoTracking()
			.Where(i => i.IsActive && (type == null || i.Type == type)).OrderBy(i => i.Type).ThenBy(i => i.Name).ToListAsync(ct);

	public async Task<IReadOnlyList<CalculationMethod>> ListCalculationMethodsAsync(CancellationToken ct = default)
		=> await _db.CalculationMethods.AsNoTracking().Where(m => m.IsActive).OrderBy(m => m.Id).ToListAsync(ct);

	public async Task<IReadOnlyList<decimal>> ListVatRatesAsync(string countryCode, CancellationToken ct = default)
		=> await _db.CountryVatRates.AsNoTracking().Where(r => r.CountryCode == countryCode)
			.OrderBy(r => r.SortOrder).Select(r => r.Rate).ToListAsync(ct);

	public async Task<string?> PropertyCountryCodeAsync(long propertyId, CancellationToken ct = default)
		=> await _db.Properties.AsNoTracking().Where(p => p.Id == propertyId).Select(p => p.Country!.Code).FirstOrDefaultAsync(ct)
			?? await _db.Bathhouses.AsNoTracking().Where(b => b.PropertyId == propertyId).Select(b => b.Country!.Code).FirstOrDefaultAsync(ct);
}
