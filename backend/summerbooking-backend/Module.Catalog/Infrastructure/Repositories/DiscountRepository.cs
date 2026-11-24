using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Catalog.Application.Abstractions;
using Module.Catalog.Application.Dtos;
using Module.Catalog.Domain.Entities;
using Shared.Infrastructure;

namespace Module.Catalog.Infrastructure.Repositories;

public sealed class DiscountRepository : IDiscountRepository
{
	private readonly DbContext _db;
	private readonly IDateTimeProvider _clock;

	public DiscountRepository(DbContext db, IDateTimeProvider clock)
	{
		_db = db;
		_clock = clock;
	}

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

	/// <summary>
	/// Un solo UPDATE condizionato: due prenotazioni contemporanee non possono prendere entrambe l'ultimo utilizzo, perché
	/// la condizione sul massimo si valuta sulla riga bloccata dall'aggiornamento.
	/// </summary>
	public async Task<bool> TryRedeemAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> await _db.Discounts
			.Where(d => d.PropertyId == propertyId && d.PublicId == publicId && (d.MaxUses == null || d.UsedCount < d.MaxUses))
			.ExecuteUpdateAsync(u => u.SetProperty(d => d.UsedCount, d => d.UsedCount + 1).SetProperty(d => d.UpdatedAt, _clock.UtcNow), ct) == 1;

	public async Task ReleaseAsync(long propertyId, Guid publicId, CancellationToken ct = default)
		=> await _db.Discounts
			.Where(d => d.PropertyId == propertyId && d.PublicId == publicId && d.UsedCount > 0)
			.ExecuteUpdateAsync(u => u.SetProperty(d => d.UsedCount, d => d.UsedCount - 1).SetProperty(d => d.UpdatedAt, _clock.UtcNow), ct);

	public void Remove(Discount discount) => _db.Discounts.Remove(discount);

	private IQueryable<Discount> WithChildren()
		=> _db.Discounts.Include(d => d.ValidityWindows).Include(d => d.RestrictedSectors).Include(d => d.TimeRanges).AsSplitQuery();
}
