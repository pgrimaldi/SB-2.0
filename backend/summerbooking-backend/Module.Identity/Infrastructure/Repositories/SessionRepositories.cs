using Microsoft.EntityFrameworkCore;
using Module.Identity.Application.Abstractions;
using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;

namespace Module.Identity.Infrastructure.Repositories;

public sealed class RoleRepository : IRoleRepository
{
	private readonly DbContext _db;

	public RoleRepository(DbContext db) => _db = db;

	public Task<Role?> GetByCodeAsync(string code, CancellationToken ct = default)
		=> _db.Roles.FirstOrDefaultAsync(r => r.Code == code, ct);

	public async Task<IReadOnlyList<Role>> ListAssignableAsync(CancellationToken ct = default)
		=> await _db.Roles.Where(r => r.IsAssignableViaPanel && r.IsStaff).OrderBy(r => r.SortOrder).ToListAsync(ct);
}

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
	private readonly DbContext _db;

	public RefreshTokenRepository(DbContext db) => _db = db;

	public Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default)
		=> _db.RefreshTokens.Include(t => t.User).ThenInclude(u => u.Role).FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

	public async Task<IReadOnlyList<RefreshToken>> ListActiveByUserAsync(long userId, DateTime now, CancellationToken ct = default)
		=> await _db.RefreshTokens
			.Where(t => t.UserId == userId && t.ConsumedAt == null && t.RevokedAt == null && t.ExpiresAt > now)
			.ToListAsync(ct);

	public async Task<IReadOnlyList<RefreshToken>> ListActiveByFamilyAsync(Guid familyId, CancellationToken ct = default)
		=> await _db.RefreshTokens.Where(t => t.FamilyId == familyId && t.ConsumedAt == null && t.RevokedAt == null).ToListAsync(ct);

	public void Add(RefreshToken token) => _db.RefreshTokens.Add(token);
}

public sealed class AuthChallengeRepository : IAuthChallengeRepository
{
	private readonly DbContext _db;

	public AuthChallengeRepository(DbContext db) => _db = db;

	public async Task<IReadOnlyList<AuthChallenge>> ListOpenAsync(long userId, AuthChallengePurpose purpose, CancellationToken ct = default)
		=> await _db.AuthChallenges
			.Where(c => c.UserId == userId && c.Purpose == purpose && c.ConsumedAt == null && c.InvalidatedAt == null)
			.OrderByDescending(c => c.Id)
			.ToListAsync(ct);

	public Task<AuthChallenge?> FindBySecretHashAsync(AuthChallengePurpose purpose, string secretHash, CancellationToken ct = default)
		=> _db.AuthChallenges.FirstOrDefaultAsync(c => c.SecretHash == secretHash && c.Purpose == purpose, ct);

	public void Add(AuthChallenge challenge) => _db.AuthChallenges.Add(challenge);
}

public sealed class PropertySecuritySettingsRepository : IPropertySecuritySettingsRepository
{
	private readonly DbContext _db;

	public PropertySecuritySettingsRepository(DbContext db) => _db = db;

	public Task<PropertySecuritySettings?> GetAsync(Guid propertyId, CancellationToken ct = default)
		=> _db.PropertySecuritySettings.FirstOrDefaultAsync(s => s.PropertyId == propertyId, ct);

	public void Add(PropertySecuritySettings settings) => _db.PropertySecuritySettings.Add(settings);
}

public sealed class UnitOfWork : IUnitOfWork
{
	private readonly DbContext _db;

	public UnitOfWork(DbContext db) => _db = db;

	public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
