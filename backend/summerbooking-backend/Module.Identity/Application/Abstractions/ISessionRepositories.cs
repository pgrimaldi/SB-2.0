using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;

namespace Module.Identity.Application.Abstractions;

public interface IRefreshTokenRepository
{
	/// <summary>Token per hash, con account e ruolo; null anche se l'account è stato cancellato.</summary>
	Task<RefreshToken?> FindByHashAsync(string tokenHash, CancellationToken ct = default);
	Task<IReadOnlyList<RefreshToken>> ListActiveByUserAsync(long userId, DateTime now, CancellationToken ct = default);
	Task<IReadOnlyList<RefreshToken>> ListActiveByFamilyAsync(Guid familyId, CancellationToken ct = default);
	void Add(RefreshToken token);
}

public interface IAuthChallengeRepository
{
	Task<IReadOnlyList<AuthChallenge>> ListOpenAsync(long userId, AuthChallengePurpose purpose, CancellationToken ct = default);
	Task<AuthChallenge?> FindBySecretHashAsync(AuthChallengePurpose purpose, string secretHash, CancellationToken ct = default);
	void Add(AuthChallenge challenge);
}

public interface IPropertySecuritySettingsRepository
{
	Task<PropertySecuritySettings?> GetAsync(Guid propertyId, CancellationToken ct = default);
	void Add(PropertySecuritySettings settings);
}

public interface IUnitOfWork
{
	Task SaveChangesAsync(CancellationToken ct = default);
}
