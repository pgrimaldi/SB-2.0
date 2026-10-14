using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;

namespace Module.Identity.Application.Abstractions;

/// <summary>Account di login; le letture includono sempre il ruolo. Gli account cancellati non compaiono.</summary>
public interface IAccountRepository
{
	Task<Account?> GetByIdAsync(long id, CancellationToken ct = default);
	Task<Account?> GetByPublicIdAsync(Guid publicId, CancellationToken ct = default);

	/// <summary>
	/// Account per email o username. Con più account vivi sullo stesso valore (dati storici, PROMEMORIA-85-2)
	/// vince quello con l'ultimo accesso più recente.
	/// </summary>
	Task<Account?> FindForLoginAsync(string identifier, CancellationToken ct = default);

	Task<Account?> FindByEmailAsync(string email, CancellationToken ct = default);
	Task<Account?> FindByProviderAsync(LoginMethod provider, string subject, CancellationToken ct = default);
	Task<bool> EmailExistsAsync(string email, long? excludeId = null, CancellationToken ct = default);
	Task<bool> UsernameExistsAsync(string username, long? excludeId = null, CancellationToken ct = default);

	Task<IReadOnlyList<Account>> ListByPropertyAsync(Guid propertyId, CancellationToken ct = default);
	Task<Account?> GetInPropertyAsync(Guid propertyId, Guid publicId, CancellationToken ct = default);
	Task<Account?> FindOwnerAsync(Guid propertyId, CancellationToken ct = default);

	void Add(Account account);
	void Remove(Account account);
}
