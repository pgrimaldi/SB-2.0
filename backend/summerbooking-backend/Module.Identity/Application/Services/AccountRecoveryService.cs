using BuildingBlocks.Application.Validation;
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

namespace Module.Identity.Application.Services;

/// <summary>
/// Password dimenticata: link con token opaco monouso e a scadenza.
/// </summary>
public sealed class AccountRecoveryService
{
	private readonly IAccountRepository _accounts;
	private readonly IAuthChallengeRepository _challenges;
	private readonly IUnitOfWork _uow;
	private readonly IPasswordHasher _hasher;
	private readonly ISecureTokenGenerator _generator;
	private readonly ILoginThrottle _throttle;
	private readonly ITransactionalEmailSender _email;
	private readonly IAuditTrail _audit;
	private readonly SessionFactory _sessions;
	private readonly IdentityAuthOptions _options;
	private readonly IDateTimeProvider _clock;
	private readonly ForgotPasswordRequestValidator _forgotValidator;
	private readonly ResetPasswordRequestValidator _resetValidator;

	public AccountRecoveryService(
		IAccountRepository accounts, IAuthChallengeRepository challenges, IUnitOfWork uow, IPasswordHasher hasher,
		ISecureTokenGenerator generator, ILoginThrottle throttle, ITransactionalEmailSender email, IAuditTrail audit,
		SessionFactory sessions, IOptions<IdentityAuthOptions> options, IDateTimeProvider clock,
		ForgotPasswordRequestValidator forgotValidator, ResetPasswordRequestValidator resetValidator)
	{
		_accounts = accounts;
		_challenges = challenges;
		_uow = uow;
		_hasher = hasher;
		_generator = generator;
		_throttle = throttle;
		_email = email;
		_audit = audit;
		_sessions = sessions;
		_options = options.Value;
		_clock = clock;
		_forgotValidator = forgotValidator;
		_resetValidator = resetValidator;
	}

	/// <summary>Risponde sempre allo stesso modo: non rivela se l'email è registrata.</summary>
	public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
	{
		_forgotValidator.EnsureValid(request);
		var email = request.Email.Trim();
		var key = "forgot:" + email.ToLowerInvariant();

		if (_throttle.IsLocked(key))
			return;
		_throttle.RegisterFailure(key);

		var account = await _accounts.FindByEmailAsync(email, ct);
		if (account is null || !_options.FrontendUrls.TryGetValue(request.App, out var frontendUrl))
			return;

		var now = _clock.UtcNow;
		foreach (var open in await _challenges.ListOpenAsync(account.Id, AuthChallengePurpose.PasswordReset, ct))
			open.Invalidate(AuthChallengeInvalidation.Superseded, now);

		var token = _generator.NewOpaqueToken();
		_challenges.Add(AuthChallenge.Create(
			account.Id, AuthChallengePurpose.PasswordReset, AuthChallengeChannel.Email, _generator.Hash(token),
			account.Email, 1, now.AddMinutes(_options.PasswordResetMinutes), _sessions.ClientIp));
		await _uow.SaveChangesAsync(ct);

		var resetUrl = $"{frontendUrl.TrimEnd('/')}/reset-password?token={Uri.EscapeDataString(token)}";
		await _email.SendAsync(new TransactionalEmail(
			TransactionalEmailKind.PasswordReset, account.Email, SupportedLanguages.Code(account.Language),
			new Dictionary<string, string> { [EmailPlaceholders.FirstName] = account.FirstName ?? string.Empty, [EmailPlaceholders.ResetUrl] = resetUrl, [EmailPlaceholders.Minutes] = _options.PasswordResetMinutes.ToString() }), ct);

		await Record(IdentityAudit.PasswordResetRequested, account, ct);
	}

	/// <summary>Nuova password: chiude tutte le sessioni aperte dell'account.</summary>
	public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
	{
		_resetValidator.EnsureValid(request);
		var now = _clock.UtcNow;

		var challenge = await _challenges.FindBySecretHashAsync(AuthChallengePurpose.PasswordReset, _generator.Hash(request.Token), ct);
		if (challenge is null || !challenge.IsUsable(now))
			throw DomainException.BadRequest(IdentityErrors.ResetTokenInvalid);

		var account = await _accounts.GetByIdAsync(challenge.UserId, ct)
			?? throw DomainException.BadRequest(IdentityErrors.ResetTokenInvalid);

		account.ChangePassword(_hasher.Hash(request.NewPassword), now);
		challenge.Consume(now);
		await _sessions.RevokeAllAsync(account.Id, RefreshRevokeReason.PasswordChanged, ct);
		await _uow.SaveChangesAsync(ct);

		_throttle.Reset(AuthService.LoginThrottleKey(account.Email));
		await Record(IdentityAudit.PasswordResetCompleted, account, ct);
	}

	private Task Record(string action, Account account, CancellationToken ct)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Security, IdentityAudit.Module, action, PropertyId: account.PropertyId,
			TargetType: IdentityAudit.Targets.User, TargetId: account.PublicId, ActorUserId: account.PublicId, ActorKind: AuditActorKind.User,
			ActorNameSnapshot: account.FullName, ActorRoleSnapshot: account.Role.Code), ct);
}
