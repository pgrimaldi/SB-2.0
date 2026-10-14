using Module.Identity.Application.Abstractions;
using Shared.Contracts.Identity;

namespace Module.Identity.Application.Services;

/// <summary>Nome e lingua di un account per gli altri moduli (nome dell'operatore nel registro di audit).</summary>
public sealed class UserDirectory : IUserDirectory
{
	private readonly IAccountRepository _accounts;

	public UserDirectory(IAccountRepository accounts) => _accounts = accounts;

	public async Task<UserSummary?> FindAsync(Guid userId, CancellationToken ct = default)
	{
		var account = await _accounts.GetByPublicIdAsync(userId, ct);
		return account is null ? null : new UserSummary(account.PublicId, account.FullName ?? account.Username ?? account.Email, account.Language);
	}
}
