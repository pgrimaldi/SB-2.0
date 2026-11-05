using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class PropertyInfoRepository : IPropertyInfoRepository
{
	private readonly DbContext _db;

	public PropertyInfoRepository(DbContext db) => _db = db;

	public Task<Property?> FindPropertyAsync(long propertyId, CancellationToken ct = default)
		=> _db.Properties.FirstOrDefaultAsync(p => p.Id == propertyId, ct);

	public void AddProperty(Property property) => _db.Properties.Add(property);

	public Task<Bathhouse?> FindBathhouseAsync(long propertyId, CancellationToken ct = default)
		=> _db.Bathhouses.Include(b => b.Photos).Include(b => b.Amenities)
			.Where(b => b.PropertyId == propertyId).OrderBy(b => b.Id).FirstOrDefaultAsync(ct);

	public void AddBathhouse(Bathhouse bathhouse) => _db.Bathhouses.Add(bathhouse);

	public Task<bool> SlugExistsAsync(string slug, long? exceptBathhouseId, CancellationToken ct = default)
		=> _db.Bathhouses.IgnoreQueryFilters().AnyAsync(b => b.Slug == slug && b.Id != exceptBathhouseId, ct);

	public async Task<short?> FindCountryIdAsync(string code, CancellationToken ct = default)
		=> await _db.Countries.AsNoTracking().Where(c => c.Code == code).Select(c => (short?)c.Id).FirstOrDefaultAsync(ct);

	public Task<string?> FindCountryCodeAsync(short id, CancellationToken ct = default)
		=> _db.Countries.AsNoTracking().Where(c => c.Id == id).Select(c => c.Code).FirstOrDefaultAsync(ct);

	public async Task<IReadOnlyDictionary<string, int>> FindAmenityIdsAsync(IReadOnlyCollection<string> codes, CancellationToken ct = default)
		=> await _db.Amenities.AsNoTracking().Where(a => codes.Contains(a.Code)).ToDictionaryAsync(a => a.Code, a => a.Id, ct);

	public async Task<IReadOnlyList<string>> FindAmenityCodesAsync(IReadOnlyCollection<int> ids, CancellationToken ct = default)
		=> await _db.Amenities.AsNoTracking().Where(a => ids.Contains(a.Id)).OrderBy(a => a.SortOrder).Select(a => a.Code).ToListAsync(ct);
}
