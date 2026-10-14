using BuildingBlocks.Application.Validation;
using FluentValidation;
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
using Shared.Contracts.Events;
using Shared.Contracts.Identity;
using Shared.Contracts.Notification;
using Shared.Infrastructure;
using Shared.Localization;
using Shared.Security;

namespace Module.Identity.Application.Services;

/// <summary>Registrazione del cliente finale (consumer) con accesso immediato.</summary>
public sealed class RegistrationService
{
	private readonly IAccountRepository _accounts;
	private readonly IRoleRepository _roles;
	private readonly IUnitOfWork _uow;
	private readonly IPasswordHasher _hasher;
	private readonly SessionFactory _sessions;
	private readonly IIntegrationEventPublisher _events;
	private readonly ITransactionalEmailSender _email;
	private readonly IAuditTrail _audit;
	private readonly IdentityAuthOptions _options;
	private readonly IDateTimeProvider _clock;
	private readonly RegisterRequestValidator _validator;
	private readonly ILogger<RegistrationService> _logger;

	public RegistrationService(
		IAccountRepository accounts, IRoleRepository roles, IUnitOfWork uow, IPasswordHasher hasher, SessionFactory sessions,
		IIntegrationEventPublisher events, ITransactionalEmailSender email, IAuditTrail audit,
		IOptions<IdentityAuthOptions> options, IDateTimeProvider clock, RegisterRequestValidator validator,
		ILogger<RegistrationService> logger)
	{
		_accounts = accounts;
		_roles = roles;
		_uow = uow;
		_hasher = hasher;
		_sessions = sessions;
		_events = events;
		_email = email;
		_audit = audit;
		_options = options.Value;
		_clock = clock;
		_validator = validator;
		_logger = logger;
	}

	public async Task<SessionResult> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
	{
		_validator.EnsureValid(request);
		var email = request.Email.Trim();

		if (await _accounts.EmailExistsAsync(email, ct: ct))
			throw DomainException.Conflict(IdentityErrors.EmailTaken);

		var consumerRole = await _roles.GetByCodeAsync(RoleCodes.Consumer, ct)
			?? throw new InvalidOperationException("Ruolo consumer mancante nel seed.");
		SupportedLanguages.TryParse(request.Language, out var language);
		var now = _clock.UtcNow;

		var account = Account.RegisterConsumer(
			email, _hasher.Hash(request.Password), Clean(request.FirstName), Clean(request.LastName),
			Clean(request.PhoneCountryCode), Clean(request.Phone), language, consumerRole.Id, _options.TermsVersion, now);
		_accounts.Add(account);
		account.RecordLogin(LoginMethod.Normal, now);

		var session = _sessions.Open(account, persistent: false);
		await _uow.SaveChangesAsync(ct);
		await AfterRegistrationAsync(account, request.NewsletterConsent, ct);
		return session;
	}

	/// <summary>Validazione separata dell'email: l'endpoint di disponibilità ha un rate limit dedicato.</summary>
	public async Task<EmailAvailabilityResponse> CheckEmailAsync(string? email, CancellationToken ct = default)
	{
		var validator = new InlineValidator<string?>();
		validator.RuleFor(x => x).NotEmpty().OptionalEmail().OverridePropertyName("email");
		validator.EnsureValid(email);
		return new EmailAvailabilityResponse(!await _accounts.EmailExistsAsync(email!.Trim(), ct: ct));
	}

	/// <summary>Effetti dopo il salvataggio: consenso newsletter a Notification, email di benvenuto, audit.</summary>
	internal async Task AfterRegistrationAsync(Account account, bool newsletterConsent, CancellationToken ct)
	{
		var language = SupportedLanguages.Code(account.Language);

		// PROMEMORIA-85-5: nessun gestore in Notification per ora.
		await _events.PublishAsync(new AccountRegistered(
			Guid.CreateVersion7(), _clock.UtcNow, account.PublicId, account.Email, language,
			newsletterConsent, newsletterConsent ? _options.TermsVersion : null), ct);

		try
		{
			await _email.SendAsync(new TransactionalEmail(TransactionalEmailKind.Welcome, account.Email, language,
				new Dictionary<string, string>
				{
					[EmailPlaceholders.FirstName] = account.FirstName ?? string.Empty,
					[EmailPlaceholders.LoginUrl] = _options.FrontendUrls.GetValueOrDefault(ClientApps.Customer) ?? string.Empty,
				}), ct);
		}
		catch (Exception ex)
		{
			// L'account esiste già: un benvenuto mancato non annulla la registrazione.
			_logger.LogWarning(ex, "Email di benvenuto non inviata all'account {UserId}", account.PublicId);
		}

		await _audit.RecordAsync(new AuditEntry(
			AuditCategory.Security, IdentityAudit.Module, IdentityAudit.AccountRegistered,
			TargetType: IdentityAudit.Targets.User, TargetId: account.PublicId, ActorUserId: account.PublicId,
			ActorKind: AuditActorKind.User, ActorRoleSnapshot: RoleCodes.Consumer), ct);
	}

	private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
