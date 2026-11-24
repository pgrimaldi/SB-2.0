using Module.Identity.Application.Abstractions;
using Shared.Contracts.Identity;

namespace Module.Identity.Application.Services;

/// <summary>Nome e lingua di un account per gli altri moduli (nome dell'operatore nel registro di audit).</summary>
public sealed class UserDirectory : IUserDirectory
{
	private readonly IAccountRepository _accounts;
	private readonly IPasswordHasher _hasher;

	public UserDirectory(IAccountRepository accounts, IPasswordHasher hasher)
	{
		_accounts = accounts;
		_hasher = hasher;
	}

	public async Task<bool> VerifyPasswordAsync(Guid userId, string password, CancellationToken ct = default)
	{
		var account = await _accounts.GetByPublicIdAsync(userId, ct);
		return account?.PasswordHash is { } hash && !string.IsNullOrEmpty(password) && _hasher.Verify(hash, password) != PasswordCheck.Failed;
	}

	public async Task<UserResourceScope> GetResourceScopeAsync(Guid userId, CancellationToken ct = default)
	{
		var account = await _accounts.GetByPublicIdAsync(userId, ct);
		return account is null ? new UserResourceScope(false, []) : new UserResourceScope(account.IsResourceScopeRestricted, account.ResourceScopes);
	}

	public async Task<UserSummary?> FindAsync(Guid userId, CancellationToken ct = default)
	{
		var account = await _accounts.GetByPublicIdAsync(userId, ct);
		return account is null ? null : new UserSummary(account.PublicId, account.FullName ?? account.Username ?? account.Email, account.Language);
	}
}
