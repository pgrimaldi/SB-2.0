using Microsoft.EntityFrameworkCore;
using Module.Identity.Application.Abstractions;
using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;
using Shared.Security;

namespace Module.Identity.Infrastructure.Repositories;

public sealed class AccountRepository : IAccountRepository
{
	private readonly DbContext _db;

	public AccountRepository(DbContext db) => _db = db;

	private IQueryable<Account> Accounts => _db.Accounts.Include(a => a.Role);

	public Task<Account?> GetByIdAsync(long id, CancellationToken ct = default)
		=> Accounts.FirstOrDefaultAsync(a => a.Id == id, ct);

	public Task<Account?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default)
		=> Accounts.FirstOrDefaultAsync(a => a.PublicId == publicId, ct);

	public Task<Account?> FindForLoginAsync(string identifier, CancellationToken ct = default)
	{
		// lower() su entrambi i lati: email è citext, ma il confronto con un parametro text sarebbe case-sensitive.
		var value = identifier.Trim().ToLower();
		return Accounts
			.Where(a => a.Email.ToLower() == value || (a.Username != null && a.Username.ToLower() == value))
			.OrderByDescending(a => a.LastLoginAt.HasValue)
			.ThenByDescending(a => a.LastLoginAt)
			.ThenByDescending(a => a.Id)
			.FirstOrDefaultAsync(ct);
	}

	public Task<Account?> FindByEmailAsync(string email, CancellationToken ct = default)
	{
		var value = email.Trim().ToLower();
		return Accounts
			.Where(a => a.Email.ToLower() == value)
			.OrderByDescending(a => a.LastLoginAt.HasValue)
			.ThenByDescending(a => a.LastLoginAt)
			.ThenByDescending(a => a.Id)
			.FirstOrDefaultAsync(ct);
	}

	public Task<Account?> FindByProviderAsync(LoginMethod provider, string subject, CancellationToken ct = default) => provider switch
	{
		LoginMethod.Google => Accounts.FirstOrDefaultAsync(a => a.GoogleId == subject, ct),
		LoginMethod.Facebook => Accounts.FirstOrDefaultAsync(a => a.FacebookId == subject, ct),
		LoginMethod.Apple => Accounts.FirstOrDefaultAsync(a => a.AppleSubject == subject, ct),
		_ => Task.FromResult<Account?>(null),
	};

	public Task<bool> EmailExistsAsync(string email, long? excludeId = null, CancellationToken ct = default)
	{
		var value = email.Trim().ToLower();
		return _db.Accounts.AnyAsync(a => a.Email.ToLower() == value && a.Id != excludeId, ct);
	}

	public Task<bool> UsernameExistsAsync(string username, long? excludeId = null, CancellationToken ct = default)
	{
		var value = username.Trim().ToLower();
		return _db.Accounts.AnyAsync(a => a.Username != null && a.Username.ToLower() == value && a.Id != excludeId, ct);
	}

	public async Task<IReadOnlyList<Account>> ListByPropertyAsync(Guid propertyId, CancellationToken ct = default)
		=> await Accounts
			.Where(a => a.PropertyId == propertyId)
			.OrderBy(a => a.Role.SortOrder).ThenBy(a => a.Email)
			.ToListAsync(ct);

	public Task<Account?> GetInPropertyAsync(Guid propertyId, Guid publicId, CancellationToken ct = default)
		=> Accounts.FirstOrDefaultAsync(a => a.PropertyId == propertyId && a.PublicId == publicId, ct);

	public Task<Account?> FindOwnerAsync(Guid propertyId, CancellationToken ct = default)
		=> Accounts
			.Where(a => a.PropertyId == propertyId && a.Role.Code == RoleCodes.SuperAdmin)
			.OrderBy(a => a.CreatedAt)
			.FirstOrDefaultAsync(ct);

	public void Add(Account account) => _db.Accounts.Add(account);

	public void Remove(Account account) => _db.Accounts.Remove(account);
}
