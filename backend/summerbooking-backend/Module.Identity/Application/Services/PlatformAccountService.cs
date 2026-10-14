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
using Shared.Contracts.Events;
using Shared.Contracts.Identity;
using Shared.Infrastructure;
using Shared.Localization;
using Shared.Security;

namespace Module.Identity.Application.Services;

/// <summary>
/// Titolare dello stabilimento gestito dal systemadmin: creazione con password scelta dal systemadmin
/// (cambiabile poi dal titolare), modifica, accesso come gestore con codice monouso e tracciato.
/// </summary>
public sealed class PlatformAccountService
{
	private readonly IAccountRepository _accounts;
	private readonly IRoleRepository _roles;
	private readonly CurrentAccount _current;
	private readonly IUnitOfWork _uow;
	private readonly IPasswordHasher _hasher;
	private readonly ITokenIssuer _issuer;
	private readonly SessionFactory _sessions;
	private readonly IIntegrationEventPublisher _events;
	private readonly IAuditTrail _audit;
	private readonly IdentityAuthOptions _options;
	private readonly IDateTimeProvider _clock;
	private readonly CreateOwnerRequestValidator _createValidator;
	private readonly UpdateOwnerRequestValidator _updateValidator;

	public PlatformAccountService(
		IAccountRepository accounts, IRoleRepository roles, CurrentAccount current, IUnitOfWork uow, IPasswordHasher hasher,
		ITokenIssuer issuer, SessionFactory sessions, IIntegrationEventPublisher events, IAuditTrail audit,
		IOptions<IdentityAuthOptions> options, IDateTimeProvider clock,
		CreateOwnerRequestValidator createValidator, UpdateOwnerRequestValidator updateValidator)
	{
		_accounts = accounts;
		_roles = roles;
		_current = current;
		_uow = uow;
		_hasher = hasher;
		_issuer = issuer;
		_sessions = sessions;
		_events = events;
		_audit = audit;
		_options = options.Value;
		_clock = clock;
		_createValidator = createValidator;
		_updateValidator = updateValidator;
	}

	public async Task<StaffUserDto> GetOwnerAsync(Guid propertyId, CancellationToken ct = default)
		=> StaffUserDto.FromEntity(await LoadOwnerAsync(propertyId, ct));

	public async Task<StaffUserDto> CreateOwnerAsync(Guid propertyId, CreateOwnerRequest request, CancellationToken ct = default)
	{
		_createValidator.EnsureValid(request);
		if (await _accounts.FindOwnerAsync(propertyId, ct) is not null)
			throw DomainException.Conflict(IdentityErrors.OwnerAlreadyExists);

		var email = request.Email.Trim();
		var username = string.IsNullOrWhiteSpace(request.Username) ? null : request.Username.Trim();
		if (await _accounts.EmailExistsAsync(email, ct: ct))
			throw DomainException.Conflict(IdentityErrors.EmailTaken);
		if (username is not null && await _accounts.UsernameExistsAsync(username, ct: ct))
			throw DomainException.Conflict(IdentityErrors.UsernameTaken);

		var role = await _roles.GetByCodeAsync(RoleCodes.SuperAdmin, ct)
			?? throw new InvalidOperationException("Ruolo superadmin mancante nel seed.");
		var admin = await _current.GetAsync(ct);
		SupportedLanguages.TryParse(request.Language, out var language);

		var owner = Account.CreateStaff(
			email, username, _hasher.Hash(request.Password), Clean(request.FirstName), Clean(request.LastName),
			Clean(request.PhoneCountryCode), Clean(request.Phone), role.Id, propertyId, admin.Id, language, _clock.UtcNow);
		_accounts.Add(owner);
		await _uow.SaveChangesAsync(ct);

		// PROMEMORIA-85-5: nessun gestore in Catalog per ora.
		await _events.PublishAsync(new PropertyOwnerAssigned(Guid.CreateVersion7(), _clock.UtcNow, propertyId, owner.PublicId), ct);
		await PublishOnlineReservationAsync(propertyId, request.OnlineReservationEnabled, ct);
		await RecordAsync(IdentityAudit.OwnerCreated, owner, ct);
		return StaffUserDto.FromEntity(owner);
	}

	public async Task<StaffUserDto> UpdateOwnerAsync(Guid propertyId, UpdateOwnerRequest request, CancellationToken ct = default)
	{
		_updateValidator.EnsureValid(request);
		var owner = await LoadOwnerAsync(propertyId, ct);

		owner.UpdateProfile(
			request.FirstName is null ? owner.FirstName : Clean(request.FirstName),
			request.LastName is null ? owner.LastName : Clean(request.LastName),
			request.PhoneCountryCode is null ? owner.PhoneCountryCode : Clean(request.PhoneCountryCode),
			request.Phone is null ? owner.Phone : Clean(request.Phone));
		if (SupportedLanguages.TryParse(request.Language, out var language))
			owner.SetLanguage(language);
		if (request.Password is not null)
		{
			owner.ChangePassword(_hasher.Hash(request.Password), _clock.UtcNow);
			await _sessions.RevokeAllAsync(owner.Id, RefreshRevokeReason.PasswordChanged, ct);
		}
		await _uow.SaveChangesAsync(ct);

		await PublishOnlineReservationAsync(propertyId, request.OnlineReservationEnabled, ct);
		await RecordAsync(IdentityAudit.OwnerUpdated, owner, ct);
		return StaffUserDto.FromEntity(owner);
	}

	/// <summary>"Accesso diretto": codice monouso di pochi secondi, scambiato dall'hub con un access token del titolare.</summary>
	public async Task<ImpersonationCodeResponse> StartImpersonationAsync(Guid propertyId, CancellationToken ct = default)
	{
		var owner = await LoadOwnerAsync(propertyId, ct);
		var admin = await _current.GetAsync(ct);

		var ticket = _issuer.IssueImpersonationTicket(new ImpersonationTicket(Guid.CreateVersion7().ToString("N"), owner.PublicId, admin.PublicId));
		await RecordAsync(IdentityAudit.ImpersonationStarted, owner, ct);
		return new ImpersonationCodeResponse(ticket.Token, ticket.ExpiresInSeconds);
	}

	private async Task<Account> LoadOwnerAsync(Guid propertyId, CancellationToken ct)
		=> await _accounts.FindOwnerAsync(propertyId, ct) ?? throw DomainException.NotFound(IdentityErrors.AccountNotFound);

	private async Task PublishOnlineReservationAsync(Guid propertyId, bool? enabled, CancellationToken ct)
	{
		if (enabled is { } value)
			await _events.PublishAsync(new PropertyOnlineReservationSet(Guid.CreateVersion7(), _clock.UtcNow, propertyId, value), ct);
	}

	private Task RecordAsync(string action, Account owner, CancellationToken ct)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Platform, IdentityAudit.Module, action, PropertyId: owner.PropertyId,
			TargetType: IdentityAudit.Targets.User, TargetId: owner.PublicId, Metadata: new Dictionary<string, object?> { ["email"] = owner.Email }), ct);

	private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
