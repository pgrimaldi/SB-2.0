using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class DiscountRepository : IDiscountRepository
{
	private readonly DbContext _db;

	public DiscountRepository(DbContext db) => _db = db;

	public async Task<(IReadOnlyList<Discount> Items, int Total)> ListAsync(DiscountFilter filter, int page, int pageSize, CancellationToken ct = default)
	{
		var discounts = WithChildren().AsNoTracking().Where(d => d.PropertyId == filter.PropertyId);
		if (filter.Search is { } search)
			discounts = discounts.Where(d => EF.Functions.ILike(d.Code, LikePattern.Contains(search)) || d.Name != null && EF.Functions.ILike(d.Name, LikePattern.Contains(search)));
		var total = await discounts.CountAsync(ct);
		var sorted = (filter.SortBy, filter.Descending) switch
		{
			(DiscountSortFields.Value, false) => discounts.OrderBy(d => d.Value),
			(DiscountSortFields.Value, true) => discounts.OrderByDescending(d => d.Value),
			(_, true) => discounts.OrderByDescending(d => d.Code),
			_ => discounts.OrderBy(d => d.Code),
		};
		return (await sorted.ThenBy(d => d.Id).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct), total);
	}

	public Task<Discount?> FindAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> WithChildren().FirstOrDefaultAsync(d => d.PropertyId == propertyId && d.PublicId == publicId, ct);

	public async Task<IReadOnlyList<Discount>> FindManyAsync(long propertyId, IReadOnlyCollection<Guid> publicIds, CancellationToken ct = default)
		=> await _db.Discounts.Where(d => d.PropertyId == propertyId && publicIds.Contains(d.PublicId)).ToListAsync(ct);

	public Task<bool> CodeExistsAsync(long propertyId, string code, long? exceptId, CancellationToken ct = default)
	{
		var lower = code.ToLower();
		return _db.Discounts.AnyAsync(d => d.PropertyId == propertyId && d.Code.ToLower() == lower && d.Id != exceptId, ct);
	}

	public void Add(Discount discount) => _db.Discounts.Add(discount);

	public void Remove(Discount discount) => _db.Discounts.Remove(discount);

	private IQueryable<Discount> WithChildren()
		=> _db.Discounts.Include(d => d.ValidityWindows).Include(d => d.RestrictedSectors).Include(d => d.TimeRanges).AsSplitQuery();
}
