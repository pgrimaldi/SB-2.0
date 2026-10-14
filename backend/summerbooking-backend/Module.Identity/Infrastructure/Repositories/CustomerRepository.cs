using BuildingBlocks.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Repositories;

public sealed class CustomerRepository : ICustomerRepository
{
	private readonly DbContext _db;

	public CustomerRepository(DbContext db) => _db = db;

	public async Task<(IReadOnlyList<Customer> Items, int Total)> ListAsync(CustomerQuery query, CancellationToken ct = default)
	{
		var filtered = Filter(query);
		var total = await filtered.CountAsync(ct);
		var items = await Sort(filtered, query)
			.Skip((query.Page - 1) * query.PageSize)
			.Take(query.PageSize)
			.ToListAsync(ct);
		return (items, total);
	}

	public async Task<IReadOnlyList<Customer>> ListAllAsync(CustomerQuery query, CancellationToken ct = default)
		=> await Sort(Filter(query), query).AsNoTracking().ToListAsync(ct);

	public Task<Customer?> GetAsync(Guid propertyId, Guid publicId, CancellationToken ct = default)
		=> _db.Customers.FirstOrDefaultAsync(c => c.PropertyId == propertyId && c.PublicId == publicId, ct);

	public async Task<IReadOnlyList<Customer>> FindByContactAsync(Guid propertyId, string? email, string? phone, CancellationToken ct = default)
	{
		if (email is null && phone is null)
			return [];

		var lowered = email?.ToLower();
		return await _db.Customers
			.Where(c => c.PropertyId == propertyId
				&& ((lowered != null && c.Email != null && c.Email.ToLower() == lowered) || (phone != null && c.Phone == phone)))
			.OrderByDescending(c => c.LastActivityAt)
			.ToListAsync(ct);
	}

	public async Task<IReadOnlyList<Customer>> ListByUserAsync(long userId, CancellationToken ct = default)
		=> await _db.Customers.Where(c => c.UserId == userId).ToListAsync(ct);

	public void Add(Customer customer) => _db.Customers.Add(customer);

	public void Remove(Customer customer) => _db.Customers.Remove(customer);

	private IQueryable<Customer> Filter(CustomerQuery query)
	{
		var customers = _db.Customers.Where(c => c.PropertyId == query.PropertyId);

		if (query.BlacklistOnly)
			customers = customers.Where(c => c.IsBlacklisted);

		if (query.Search is { } search)
		{
			var pattern = LikePattern.Contains(search);
			customers = customers.Where(c =>
				EF.Functions.ILike(c.FullName ?? "", pattern)
				|| EF.Functions.ILike(c.Email ?? "", pattern)
				|| EF.Functions.ILike(c.Phone ?? "", pattern)
				|| EF.Functions.ILike(c.PhoneSecondary ?? "", pattern));
		}

		return customers;
	}

	private static IQueryable<Customer> Sort(IQueryable<Customer> customers, CustomerQuery query) => (query.SortBy, query.Descending) switch
	{
		(CustomerSortFields.Email, false) => customers.OrderBy(c => c.Email).ThenBy(c => c.Id),
		(CustomerSortFields.Email, true) => customers.OrderByDescending(c => c.Email).ThenByDescending(c => c.Id),
		(CustomerSortFields.Phone, false) => customers.OrderBy(c => c.Phone).ThenBy(c => c.Id),
		(CustomerSortFields.Phone, true) => customers.OrderByDescending(c => c.Phone).ThenByDescending(c => c.Id),
		(CustomerSortFields.CreatedAt, false) => customers.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id),
		(CustomerSortFields.CreatedAt, true) => customers.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id),
		(CustomerSortFields.LastActivityAt, false) => customers.OrderBy(c => c.LastActivityAt).ThenBy(c => c.Id),
		(CustomerSortFields.LastActivityAt, true) => customers.OrderByDescending(c => c.LastActivityAt).ThenByDescending(c => c.Id),
		(_, true) => customers.OrderByDescending(c => c.FullName).ThenByDescending(c => c.Id),
		_ => customers.OrderBy(c => c.FullName).ThenBy(c => c.Id),
	};
}
