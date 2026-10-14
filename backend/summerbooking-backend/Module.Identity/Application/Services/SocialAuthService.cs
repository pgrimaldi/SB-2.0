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
using Shared.Infrastructure;
using Shared.Localization;
using Shared.Security;
using Shared.Utils;

namespace Module.Identity.Application.Services;

/// <summary>
/// Login con Google, Apple e Facebook. Il token del provider è sempre verificato lato server; il primo
/// accesso crea un account consumer. L'account si riconosce dall'id del provider, non dall'email
/// (Apple può non mandarla dopo la prima autorizzazione).
/// </summary>
public sealed class SocialAuthService
{
	private readonly ISocialIdentityVerifier _verifier;
	private readonly IAccountRepository _accounts;
	private readonly IRoleRepository _roles;
	private readonly AuthService _auth;
	private readonly RegistrationService _registration;
	private readonly IAuditTrail _audit;
	private readonly IdentityAuthOptions _options;
	private readonly IDateTimeProvider _clock;
	private readonly SocialLoginRequestValidator _validator;

	public SocialAuthService(
		ISocialIdentityVerifier verifier, IAccountRepository accounts, IRoleRepository roles, AuthService auth,
		RegistrationService registration, IAuditTrail audit, IOptions<IdentityAuthOptions> options,
		IDateTimeProvider clock, SocialLoginRequestValidator validator)
	{
		_verifier = verifier;
		_accounts = accounts;
		_roles = roles;
		_auth = auth;
		_registration = registration;
		_audit = audit;
		_options = options.Value;
		_clock = clock;
		_validator = validator;
	}

	public async Task<LoginResult> LoginAsync(LoginMethod provider, SocialLoginRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);

		var identity = await _verifier.VerifyAsync(provider, request.Token, ct);
		if (identity is null)
		{
			await _audit.RecordAsync(new AuditEntry(AuditCategory.Security, IdentityAudit.Module, IdentityAudit.LoginFailed, AuditOutcome.Failed,
				Metadata: new Dictionary<string, object?> { ["method"] = Codes.Of(provider) }), ct);
			throw DomainException.Unauthorized(IdentityErrors.SocialTokenInvalid);
		}

		var account = await _accounts.FindByProviderAsync(provider, identity.Subject, ct);
		var created = false;

		if (account is null && identity.EmailVerified && identity.Email is { } verifiedEmail)
		{
			account = await _accounts.FindByEmailAsync(verifiedEmail, ct);
			account?.LinkProvider(provider, identity.Subject);
		}

		if (account is null)
		{
			account = await CreateConsumerAsync(provider, identity, request, ct);
			created = true;
		}

		var result = await _auth.CompleteAuthenticationAsync(
			account, provider, request.Remember, request.App, "social:" + identity.Subject, ct);

		if (created)
			await _registration.AfterRegistrationAsync(account, newsletterConsent: false, ct);

		return result;
	}

	private async Task<Account> CreateConsumerAsync(LoginMethod provider, SocialIdentity identity, SocialLoginRequest request, CancellationToken ct)
	{
		var email = identity.Email ?? request.Email?.Trim()
			?? throw DomainException.Unprocessable(IdentityErrors.SocialEmailRequired);

		// Un'email non verificata dal provider non può agganciarsi a un account esistente.
		if (await _accounts.EmailExistsAsync(email, ct: ct))
			throw DomainException.Conflict(IdentityErrors.EmailTaken);

		if (!request.TermsAccepted)
			throw new ValidationFailedException("termsAccepted", ValidationErrorCodes.MustAccept);

		var consumerRole = await _roles.GetByCodeAsync(RoleCodes.Consumer, ct)
			?? throw new InvalidOperationException("Ruolo consumer mancante nel seed.");
		SupportedLanguages.TryParse(request.Language, out var language);

		var account = Account.RegisterFromSocial(
			provider, identity.Subject, email, identity.FirstName ?? request.FirstName, identity.LastName ?? request.LastName,
			identity.PictureUrl, language, consumerRole.Id, _options.TermsVersion, _clock.UtcNow);
		_accounts.Add(account);
		return account;
	}
}
