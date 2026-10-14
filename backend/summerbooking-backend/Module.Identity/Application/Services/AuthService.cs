using BuildingBlocks.Application.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.Identity.Application.Abstractions;
using Module.Identity.Application.Dtos;
using Module.Identity.Application.Validators;
using Module.Identity.Domain;
using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;
using Shared.Contracts.Audit;
using Shared.Contracts.Errors;
using Shared.Contracts.Notification;
using Shared.Infrastructure;
using Shared.Localization;
using Shared.Security;
using Shared.Utils;

namespace Module.Identity.Application.Services;

/// <summary>Login con password e secondo fattore, refresh, logout, ingresso in impersonazione.</summary>
public sealed class AuthService
{
	private readonly IAccountRepository _accounts;
	private readonly IAuthChallengeRepository _challenges;
	private readonly IPropertySecuritySettingsRepository _security;
	private readonly IRefreshTokenRepository _tokens;
	private readonly IUnitOfWork _uow;
	private readonly IPasswordHasher _hasher;
	private readonly ISecureTokenGenerator _generator;
	private readonly ITokenIssuer _issuer;
	private readonly ILoginThrottle _throttle;
	private readonly IReplayGuard _replay;
	private readonly ITransactionalEmailSender _email;
	private readonly IAuditTrail _audit;
	private readonly SessionFactory _sessions;
	private readonly IdentityAuthOptions _options;
	private readonly IDateTimeProvider _clock;
	private readonly LoginRequestValidator _loginValidator;
	private readonly VerifyOtpRequestValidator _otpValidator;
	private readonly ILogger<AuthService> _logger;

	public AuthService(
		IAccountRepository accounts, IAuthChallengeRepository challenges, IPropertySecuritySettingsRepository security,
		IRefreshTokenRepository tokens, IUnitOfWork uow, IPasswordHasher hasher, ISecureTokenGenerator generator,
		ITokenIssuer issuer, ILoginThrottle throttle, IReplayGuard replay, ITransactionalEmailSender email,
		IAuditTrail audit, SessionFactory sessions, IOptions<IdentityAuthOptions> options, IDateTimeProvider clock,
		LoginRequestValidator loginValidator, VerifyOtpRequestValidator otpValidator, ILogger<AuthService> logger)
	{
		_accounts = accounts;
		_challenges = challenges;
		_security = security;
		_tokens = tokens;
		_uow = uow;
		_hasher = hasher;
		_generator = generator;
		_issuer = issuer;
		_throttle = throttle;
		_replay = replay;
		_email = email;
		_audit = audit;
		_sessions = sessions;
		_options = options.Value;
		_clock = clock;
		_loginValidator = loginValidator;
		_otpValidator = otpValidator;
		_logger = logger;
	}

	public static string LoginThrottleKey(string identifier) => "login:" + identifier.Trim().ToLowerInvariant();

	public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
	{
		_loginValidator.EnsureValid(request);
		var key = LoginThrottleKey(request.Username);

		if (_throttle.IsLocked(key))
		{
			await RecordSecurityAsync(IdentityAudit.LoginLocked, null, AuditOutcome.Failed, ct);
			throw DomainException.TooManyRequests(IdentityErrors.TooManyAttempts);
		}

		var account = await _accounts.FindForLoginAsync(request.Username.Trim(), ct);
		var check = account?.PasswordHash is { } hash ? _hasher.Verify(hash, request.Password) : PasswordCheck.Failed;

		if (account is null || check == PasswordCheck.Failed)
		{
			_throttle.RegisterFailure(key);
			await RecordSecurityAsync(IdentityAudit.LoginFailed, account, AuditOutcome.Failed, ct);
			throw DomainException.Unauthorized(IdentityErrors.InvalidCredentials);
		}

		if (check == PasswordCheck.SuccessRehashNeeded)
			account.UpgradePasswordHash(_hasher.Hash(request.Password));

		return await CompleteAuthenticationAsync(account, LoginMethod.Normal, request.Remember, request.App, key, ct);
	}

	/// <summary>
	/// Passo comune a password e social: applicazione ammessa, secondo fattore se lo stabilimento lo
	/// richiede, altrimenti sessione. Salva le modifiche già fatte all'account.
	/// </summary>
	internal async Task<LoginResult> CompleteAuthenticationAsync(
		Account account, LoginMethod method, bool remember, string app, string throttleKey, CancellationToken ct)
	{
		if (!ClientApps.IsRoleAllowed(app, account.Role.Code, account.Role.IsStaff))
		{
			await RecordSecurityAsync(IdentityAudit.LoginAppDenied, account, AuditOutcome.Failed, ct, new() { ["app"] = app });
			throw DomainException.Forbidden(IdentityErrors.AppNotAllowed);
		}

		// PROMEMORIA-85-1: blocco dello staff con abbonamento non attivo.
		// Disattivato finché il systemadmin non ha inserito gli abbonamenti; per riattivarlo iniettare
		// ISubscriptionRepository e togliere il commento. Il systemadmin non ha stabilimento e non è toccato.
		// if (account.Role.IsStaff && account.PropertyId is { } propertyId
		// 	&& (await _subscriptions.GetByPropertyAsync(propertyId, ct))?.Status is not SubscriptionStatus.Active)
		// {
		// 	await RecordSecurityAsync(IdentityAudit.LoginAppDenied, account, AuditOutcome.Failed, ct, new() { ["reason"] = "subscription_inactive" });
		// 	throw DomainException.Forbidden("account.subscription_inactive");
		// }

		if (await RequiresSecondFactorAsync(account, ct))
			return new LoginResult(null, await StartSecondFactorAsync(account, method, remember, app, ct));

		account.RecordLogin(method, _clock.UtcNow);
		var session = _sessions.Open(account, remember);
		await _uow.SaveChangesAsync(ct);
		_throttle.Reset(throttleKey);
		await RecordSecurityAsync(IdentityAudit.LoginSucceeded, account, AuditOutcome.Succeeded, ct, new() { ["method"] = Codes.Of(method), ["app"] = app });
		return new LoginResult(session, null);
	}

	public async Task<SessionResult> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken ct = default)
	{
		_otpValidator.EnsureValid(request);
		var ticket = await _issuer.ReadMfaTicketAsync(request.MfaToken) ?? throw DomainException.Unauthorized(IdentityErrors.OtpInvalid);
		var account = await _accounts.GetByPublicIdAsync(ticket.UserId, ct) ?? throw DomainException.Unauthorized(IdentityErrors.OtpInvalid);

		var now = _clock.UtcNow;
		var challenge = (await _challenges.ListOpenAsync(account.Id, AuthChallengePurpose.SecondFactorLogin, ct)).FirstOrDefault(c => c.IsUsable(now))
			?? throw DomainException.Unauthorized(IdentityErrors.OtpInvalid);

		if (!_generator.FixedTimeEquals(challenge.SecretHash, _generator.Hash(request.Code)))
		{
			challenge.RegisterFailedAttempt(now);
			await _uow.SaveChangesAsync(ct);
			await RecordSecurityAsync(IdentityAudit.SecondFactorFailed, account, AuditOutcome.Failed, ct);
			throw DomainException.Unauthorized(IdentityErrors.OtpInvalid);
		}

		challenge.Consume(now);
		account.RecordLogin(ticket.Method, now);
		var session = _sessions.Open(account, ticket.Remember);
		await _uow.SaveChangesAsync(ct);
		_throttle.Reset(LoginThrottleKey(account.Email));
		await RecordSecurityAsync(IdentityAudit.LoginSucceeded, account, AuditOutcome.Succeeded, ct,
			new() { ["method"] = Codes.Of(ticket.Method), ["app"] = ticket.App, ["second_factor"] = true });
		return session;
	}

	/// <summary>Rotazione: il token usato si consuma; se torna una seconda volta si revoca l'intera famiglia.</summary>
	public async Task<SessionResult> RefreshAsync(string? refreshToken, CancellationToken ct = default)
	{
		if (string.IsNullOrEmpty(refreshToken))
			throw DomainException.Unauthorized(IdentityErrors.SessionExpired);

		var now = _clock.UtcNow;
		var token = await _tokens.FindByHashAsync(_generator.Hash(refreshToken), ct)
			?? throw DomainException.Unauthorized(IdentityErrors.SessionExpired);

		if (token.IsReplay)
		{
			foreach (var sibling in await _tokens.ListActiveByFamilyAsync(token.FamilyId, ct))
				sibling.Revoke(RefreshRevokeReason.ReuseDetected, now);
			await _uow.SaveChangesAsync(ct);
			await RecordSecurityAsync(IdentityAudit.RefreshReuseDetected, token.User, AuditOutcome.Failed, ct);
			throw DomainException.Unauthorized(IdentityErrors.SessionExpired);
		}

		if (!token.IsActive(now))
			throw DomainException.Unauthorized(IdentityErrors.SessionExpired);

		var session = _sessions.Open(token.User, token.IsPersistent, token);
		await _uow.SaveChangesAsync(ct);
		return session;
	}

	/// <summary>Idempotente: senza sessione valida non fa nulla.</summary>
	public async Task LogoutAsync(string? refreshToken, CancellationToken ct = default)
	{
		if (string.IsNullOrEmpty(refreshToken))
			return;

		var token = await _tokens.FindByHashAsync(_generator.Hash(refreshToken), ct);
		if (token is null || !token.IsActive(_clock.UtcNow))
			return;

		token.Revoke(RefreshRevokeReason.Logout, _clock.UtcNow);
		await _uow.SaveChangesAsync(ct);
		await RecordSecurityAsync(IdentityAudit.Logout, token.User, AuditOutcome.Succeeded, ct);
	}

	/// <summary>Il codice dato al systemadmin diventa un access token del titolare, senza refresh.</summary>
	public async Task<ImpersonationSessionResponse> ExchangeImpersonationCodeAsync(ImpersonationExchangeRequest request, CancellationToken ct = default)
	{
		var ticket = string.IsNullOrEmpty(request.Code) ? null : await _issuer.ReadImpersonationTicketAsync(request.Code);
		if (ticket is null || !_replay.TryConsume(ticket.TicketId, TimeSpan.FromSeconds(_options.ImpersonationTicketSeconds * 2)))
			throw DomainException.Unauthorized(IdentityErrors.ImpersonationCodeInvalid);

		var account = await _accounts.GetByPublicIdAsync(ticket.TargetUserId, ct)
			?? throw DomainException.Unauthorized(IdentityErrors.ImpersonationCodeInvalid);

		var access = _issuer.IssueAccessToken(
			new AccessTokenSubject(account.PublicId, account.Role.Code, account.PropertyId, ticket.ImpersonatorUserId),
			TimeSpan.FromMinutes(_options.ImpersonationSessionMinutes));

		await _audit.RecordAsync(new AuditEntry(
			AuditCategory.Platform, IdentityAudit.Module, IdentityAudit.ImpersonationSessionOpened,
			PropertyId: account.PropertyId, TargetType: IdentityAudit.Targets.User, TargetId: account.PublicId,
			ActorUserId: ticket.ImpersonatorUserId, ActorKind: AuditActorKind.User), ct);

		return new ImpersonationSessionResponse(access.Token, access.ExpiresInSeconds, SessionFactory.ToAuthUser(account), ticket.ImpersonatorUserId);
	}

	private async Task<bool> RequiresSecondFactorAsync(Account account, CancellationToken ct)
	{
		if (!account.Role.IsStaff || account.PropertyId is not { } propertyId)
			return false;
		return (await _security.GetAsync(propertyId, ct))?.IsTwoFactorEnabled == true;
	}

	private async Task<TwoFactorRequiredResponse> StartSecondFactorAsync(Account account, LoginMethod method, bool remember, string app, CancellationToken ct)
	{
		var now = _clock.UtcNow;
		foreach (var open in await _challenges.ListOpenAsync(account.Id, AuthChallengePurpose.SecondFactorLogin, ct))
			open.Invalidate(AuthChallengeInvalidation.Superseded, now);

		var code = _generator.NewNumericCode(_options.OtpDigits);
		_challenges.Add(AuthChallenge.Create(
			account.Id, AuthChallengePurpose.SecondFactorLogin, AuthChallengeChannel.Email, _generator.Hash(code),
			account.Email, _options.OtpMaxAttempts, now.AddMinutes(_options.OtpChallengeMinutes), _sessions.ClientIp));
		await _uow.SaveChangesAsync(ct);

		await _email.SendAsync(new TransactionalEmail(
			TransactionalEmailKind.SecondFactorCode, account.Email, SupportedLanguages.Code(account.Language),
			new Dictionary<string, string> { [EmailPlaceholders.FirstName] = account.FirstName ?? string.Empty, [EmailPlaceholders.Code] = code, [EmailPlaceholders.Minutes] = _options.OtpChallengeMinutes.ToString() }), ct);

		var ticket = _issuer.IssueMfaTicket(new MfaTicket(account.PublicId, remember, app, method));
		await RecordSecurityAsync(IdentityAudit.SecondFactorRequested, account, AuditOutcome.Succeeded, ct);
		_logger.LogInformation("Secondo fattore richiesto per l'account {UserId}", account.PublicId);
		return new TwoFactorRequiredResponse(true, ticket.Token, ticket.ExpiresInSeconds);
	}

	private Task RecordSecurityAsync(string action, Account? account, AuditOutcome outcome, CancellationToken ct, Dictionary<string, object?>? metadata = null)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Security, IdentityAudit.Module, action, outcome,
			PropertyId: account?.PropertyId,
			TargetType: account is null ? null : IdentityAudit.Targets.User,
			TargetId: account?.PublicId,
			Metadata: metadata,
			ActorKind: account is null ? AuditActorKind.Anonymous : AuditActorKind.User,
			ActorUserId: account?.PublicId,
			ActorNameSnapshot: account?.FullName,
			ActorRoleSnapshot: account?.Role.Code), ct);
}
