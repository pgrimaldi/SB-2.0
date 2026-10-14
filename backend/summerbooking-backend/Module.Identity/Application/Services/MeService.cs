using BuildingBlocks.Application.Validation;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Validators;
using Module.Identity.Domain;
using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Contracts.Events;
using Shared.Contracts.Identity;
using Shared.Infrastructure;
using Shared.Localization;
using Shared.Security;
using Shared.Utils;

namespace Module.Identity.Application.Services;

/// <summary>Profilo, password, preferenze, sessioni e chiusura dell'account dell'utente autenticato.</summary>
public sealed class MeService
{
	private readonly CurrentAccount _current;
	private readonly IPropertySecuritySettingsRepository _security;
	private readonly ISubscriptionRepository _subscriptions;
	private readonly IRefreshTokenRepository _tokens;
	private readonly ICustomerRepository _customers;
	private readonly IBathhouseLikeRepository _likes;
	private readonly IAccountRepository _accounts;
	private readonly IUnitOfWork _uow;
	private readonly IPasswordHasher _hasher;
	private readonly ISecureTokenGenerator _generator;
	private readonly ISocialIdentityVerifier _socialVerifier;
	private readonly IFileUrlResolver _files;
	private readonly SessionFactory _sessions;
	private readonly IIntegrationEventPublisher _events;
	private readonly IAuditTrail _audit;
	private readonly IDateTimeProvider _clock;
	private readonly UpdateProfileRequestValidator _profileValidator;
	private readonly ChangePasswordRequestValidator _passwordValidator;
	private readonly UpdatePreferencesRequestValidator _preferencesValidator;

	public MeService(
		CurrentAccount current, IPropertySecuritySettingsRepository security, ISubscriptionRepository subscriptions,
		IRefreshTokenRepository tokens, ICustomerRepository customers, IBathhouseLikeRepository likes,
		IAccountRepository accounts, IUnitOfWork uow, IPasswordHasher hasher, ISecureTokenGenerator generator,
		ISocialIdentityVerifier socialVerifier, IFileUrlResolver files, SessionFactory sessions,
		IIntegrationEventPublisher events, IAuditTrail audit, IDateTimeProvider clock,
		UpdateProfileRequestValidator profileValidator, ChangePasswordRequestValidator passwordValidator,
		UpdatePreferencesRequestValidator preferencesValidator)
	{
		_current = current;
		_security = security;
		_subscriptions = subscriptions;
		_tokens = tokens;
		_customers = customers;
		_likes = likes;
		_accounts = accounts;
		_uow = uow;
		_hasher = hasher;
		_generator = generator;
		_socialVerifier = socialVerifier;
		_files = files;
		_sessions = sessions;
		_events = events;
		_audit = audit;
		_clock = clock;
		_profileValidator = profileValidator;
		_passwordValidator = passwordValidator;
		_preferencesValidator = preferencesValidator;
	}

	public async Task<MeDto> GetAsync(CancellationToken ct = default)
	{
		var account = await _current.GetAsync(ct);
		var twoFactor = false;
		SubscriptionSummaryDto? subscription = null;

		if (account.PropertyId is { } propertyId)
		{
			twoFactor = (await _security.GetAsync(propertyId, ct))?.IsTwoFactorEnabled ?? false;
			if (await _subscriptions.GetByPropertyAsync(propertyId, ct) is { } s)
				subscription = new SubscriptionSummaryDto(s.LicensePlan.Code, Codes.Of(s.Status), s.StartsAt, s.EndsAt, MonthsUntil(s.EndsAt));
		}

		// PROMEMORIA-85-1: controlli di licenza disattivati finché il systemadmin non inserisce gli abbonamenti.
		// Da riattivare al posto della riga sotto:
		// var allFeaturesUnlocked = subscription is { Status: "active", LicensePlanCode: not "basic" };
		var allFeaturesUnlocked = true;

		return new MeDto(
			account.PublicId, account.Email, account.Username, account.FirstName, account.LastName,
			account.PhoneCountryCode, account.Phone, SupportedLanguages.Code(account.Language),
			_files.Resolve(account.ProfilePictureOrigin, account.ProfilePicturePath),
			account.Role.Code, account.PropertyId, RolePermissions.For(account.Role.Code), account.HasPassword,
			Codes.Of(account.LoginMethod), account.EnabledNotificationSounds.Select(Codes.Of).ToList(),
			account.NewsSectionEnabled, account.IsResourceScopeRestricted, account.ResourceScopes,
			twoFactor, subscription, allFeaturesUnlocked, _current.ImpersonatorUserId);
	}

	public async Task<MeDto> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
	{
		_profileValidator.EnsureValid(request);
		var account = await _current.GetAsync(ct);
		account.UpdateProfile(Clean(request.FirstName), Clean(request.LastName), Clean(request.PhoneCountryCode), Clean(request.Phone));
		await _uow.SaveChangesAsync(ct);
		return await GetAsync(ct);
	}

	public async Task<MeDto> UpdatePreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default)
	{
		_preferencesValidator.EnsureValid(request);
		var account = await _current.GetAsync(ct);

		if (SupportedLanguages.TryParse(request.Language, out var language))
			account.SetLanguage(language);
		if (request.NotificationSounds is { } sounds)
			account.SetNotificationSounds(sounds.Select(s => { Codes.TryParse<NotificationSoundScope>(s, out var v); return v; }));
		if (request.NewsSectionEnabled is { } news)
			account.SetNewsSection(news);

		await _uow.SaveChangesAsync(ct);
		return await GetAsync(ct);
	}

	/// <summary>Chiude tutte le sessioni e ne apre una nuova per questo dispositivo.</summary>
	public async Task<SessionResult> ChangePasswordAsync(ChangePasswordRequest request, string? currentRefreshToken, CancellationToken ct = default)
	{
		ForbidWhenImpersonated();
		_passwordValidator.EnsureValid(request);
		var account = await _current.GetAsync(ct);

		if (account.PasswordHash is { } hash
			&& (request.CurrentPassword is null || _hasher.Verify(hash, request.CurrentPassword) == PasswordCheck.Failed))
			throw DomainException.Unprocessable(IdentityErrors.CurrentPasswordIncorrect);

		var persistent = currentRefreshToken is not null
			&& (await _tokens.FindByHashAsync(_generator.Hash(currentRefreshToken), ct))?.IsPersistent == true;

		account.ChangePassword(_hasher.Hash(request.NewPassword), _clock.UtcNow);
		await _sessions.RevokeAllAsync(account.Id, RefreshRevokeReason.PasswordChanged, ct);
		var session = _sessions.Open(account, persistent);
		await _uow.SaveChangesAsync(ct);

		await RecordAsync(IdentityAudit.PasswordChanged, account, ct);
		return session;
	}

	public async Task<IReadOnlyList<SessionDto>> ListSessionsAsync(string? currentRefreshToken, CancellationToken ct = default)
	{
		var account = await _current.GetAsync(ct);
		var currentHash = currentRefreshToken is null ? null : _generator.Hash(currentRefreshToken);

		return (await _tokens.ListActiveByUserAsync(account.Id, _clock.UtcNow, ct))
			.OrderByDescending(t => t.IssuedAt)
			.Select(t => new SessionDto(t.FamilyId, t.IssuedAt, t.ExpiresAt, t.ClientUserAgent, t.ClientIp?.ToString(),
				t.IsPersistent, currentHash is not null && t.TokenHash == currentHash))
			.ToList();
	}

	public async Task RevokeSessionAsync(Guid sessionId, CancellationToken ct = default)
	{
		var account = await _current.GetAsync(ct);
		var tokens = (await _tokens.ListActiveByFamilyAsync(sessionId, ct)).Where(t => t.UserId == account.Id).ToList();
		if (tokens.Count == 0)
			throw DomainException.NotFound(CommonErrorCodes.NotFound);

		foreach (var token in tokens)
			token.Revoke(RefreshRevokeReason.Manual, _clock.UtcNow);
		await _uow.SaveChangesAsync(ct);
		await RecordAsync(IdentityAudit.SessionRevoked, account, ct);
	}

	/// <summary>
	/// Chiusura dell'account del cliente: anonimizzato subito; le
	/// prenotazioni restano valide e la rubrica degli stabilimenti viene solo scollegata.
	/// </summary>
	public async Task DeleteAsync(DeleteAccountRequest request, CancellationToken ct = default)
	{
		ForbidWhenImpersonated();
		var account = await _current.GetAsync(ct);
		if (account.Role.Code != RoleCodes.Consumer)
			throw DomainException.Forbidden(CommonErrorCodes.Forbidden);

		await EnsureReauthenticatedAsync(account, request, ct);

		var now = _clock.UtcNow;
		await _sessions.RevokeAllAsync(account.Id, RefreshRevokeReason.AccountDisabled, ct);

		var likes = await _likes.ListAllByUserAsync(account.Id, ct);
		foreach (var like in likes)
			_likes.Remove(like);
		foreach (var customer in await _customers.ListByUserAsync(account.Id, ct))
			customer.UnlinkUser();

		account.Anonymize();
		_accounts.Remove(account);
		await _uow.SaveChangesAsync(ct);

		await _events.PublishAsync(new AccountDeleted(Guid.CreateVersion7(), now, account.PublicId), ct);
		foreach (var like in likes)
			await _events.PublishAsync(new BathhouseLikeChanged(Guid.CreateVersion7(), now, like.BathhouseId, account.PublicId, false), ct);
		await RecordAsync(IdentityAudit.AccountDeleted, account, ct);
	}

	private async Task EnsureReauthenticatedAsync(Account account, DeleteAccountRequest request, CancellationToken ct)
	{
		if (account.PasswordHash is { } hash)
		{
			if (request.Password is null || _hasher.Verify(hash, request.Password) == PasswordCheck.Failed)
				throw DomainException.Unprocessable(IdentityErrors.CurrentPasswordIncorrect);
			return;
		}

		// Account solo social: conferma con un nuovo login presso il provider collegato.
		if (!Codes.TryParse<LoginMethod>(request.Provider, out var provider) || provider == LoginMethod.Normal
			|| string.IsNullOrEmpty(request.ProviderToken))
			throw DomainException.Unprocessable(IdentityErrors.ReauthenticationRequired);

		var identity = await _socialVerifier.VerifyAsync(provider, request.ProviderToken, ct);
		var linked = provider switch
		{
			LoginMethod.Google => account.GoogleId,
			LoginMethod.Facebook => account.FacebookId,
			LoginMethod.Apple => account.AppleSubject,
			_ => null,
		};
		if (identity is null || linked is null || identity.Subject != linked)
			throw DomainException.Unprocessable(IdentityErrors.ReauthenticationRequired);
	}

	private void ForbidWhenImpersonated()
	{
		if (_current.IsImpersonated)
			throw DomainException.Forbidden(CommonErrorCodes.Forbidden);
	}

	private Task RecordAsync(string action, Account account, CancellationToken ct)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Security, IdentityAudit.Module, action, PropertyId: account.PropertyId,
			TargetType: IdentityAudit.Targets.User, TargetId: account.PublicId, ActorNameSnapshot: account.FullName), ct);

	private int MonthsUntil(DateTime endsAt)
	{
		var now = _clock.UtcNow;
		if (endsAt <= now)
			return 0;
		var months = (endsAt.Year - now.Year) * 12 + endsAt.Month - now.Month;
		return endsAt.Day < now.Day ? Math.Max(months - 1, 0) : months;
	}

	private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
