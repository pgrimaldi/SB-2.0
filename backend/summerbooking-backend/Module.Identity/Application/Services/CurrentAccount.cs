using Module.Identity.Application.Abstractions;
using Module.Identity.Domain;
using Module.Identity.Domain.Entities;
using Shared.Contracts.Errors;
using Shared.Security;

namespace Module.Identity.Application.Services;

/// <summary>Account e stabilimento dell'utente autenticato, risolti dal token.</summary>
public sealed class CurrentAccount
{
	private readonly ICurrentUser _currentUser;
	private readonly IAccountRepository _accounts;

	public CurrentAccount(ICurrentUser currentUser, IAccountRepository accounts)
	{
		_currentUser = currentUser;
		_accounts = accounts;
	}

	public bool IsImpersonated => _currentUser.ImpersonatorUserId is not null;

	public Guid? ImpersonatorUserId => _currentUser.ImpersonatorUserId;

	/// <summary>Account vivo dell'utente; 401 se nel frattempo è stato cancellato.</summary>
	public async Task<Account> GetAsync(CancellationToken ct = default)
	{
		var id = _currentUser.UserId ?? throw DomainException.Unauthorized(CommonErrorCodes.Unauthenticated);
		return await _accounts.GetByPublicIdAsync(id, ct)
			?? throw DomainException.Unauthorized(IdentityErrors.SessionExpired);
	}

	/// <summary>Stabilimento dal claim del token, mai dal corpo della richiesta.</summary>
	public Guid RequirePropertyId()
		=> _currentUser.PropertyId ?? throw DomainException.Forbidden(IdentityErrors.PropertyRequired);
}
