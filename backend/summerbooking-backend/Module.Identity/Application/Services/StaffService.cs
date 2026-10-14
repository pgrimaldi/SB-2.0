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

namespace Module.Identity.Application.Services;

/// <summary>
/// Utenti dello stabilimento (Impostazioni → Gestione utenti). Lo stabilimento viene dal token; il
/// titolare e l'utente stesso non si modificano da qui (il profilo personale è in <see cref="MeService"/>).
/// </summary>
public sealed class StaffService
{
	private readonly CurrentAccount _current;
	private readonly IAccountRepository _accounts;
	private readonly IRoleRepository _roles;
	private readonly IUnitOfWork _uow;
	private readonly IPasswordHasher _hasher;
	private readonly SessionFactory _sessions;
	private readonly IIntegrationEventPublisher _events;
	private readonly IAuditTrail _audit;
	private readonly IDateTimeProvider _clock;
	private readonly CreateStaffUserRequestValidator _createValidator;
	private readonly UpdateStaffUserRequestValidator _updateValidator;

	public StaffService(
		CurrentAccount current, IAccountRepository accounts, IRoleRepository roles, IUnitOfWork uow,
		IPasswordHasher hasher, SessionFactory sessions, IIntegrationEventPublisher events, IAuditTrail audit,
		IDateTimeProvider clock, CreateStaffUserRequestValidator createValidator, UpdateStaffUserRequestValidator updateValidator)
	{
		_current = current;
		_accounts = accounts;
		_roles = roles;
		_uow = uow;
		_hasher = hasher;
		_sessions = sessions;
		_events = events;
		_audit = audit;
		_clock = clock;
		_createValidator = createValidator;
		_updateValidator = updateValidator;
	}

	public async Task<IReadOnlyList<StaffUserDto>> ListAsync(CancellationToken ct = default)
	{
		var propertyId = _current.RequirePropertyId();
		return (await _accounts.ListByPropertyAsync(propertyId, ct)).Select(StaffUserDto.FromEntity).ToList();
	}

	public async Task<IReadOnlyList<RoleDto>> ListAssignableRolesAsync(CancellationToken ct = default)
		=> (await _roles.ListAssignableAsync(ct)).Select(r => new RoleDto(r.Code)).ToList();

	public async Task<StaffUserDto> CreateAsync(CreateStaffUserRequest request, CancellationToken ct = default)
	{
		_createValidator.EnsureValid(request);
		var propertyId = _current.RequirePropertyId();
		var creator = await _current.GetAsync(ct);
		var role = await GetAssignableRoleAsync(request.Role, ct);
		var email = request.Email.Trim();
		var username = Clean(request.Username);

		if (await _accounts.EmailExistsAsync(email, ct: ct))
			throw DomainException.Conflict(IdentityErrors.EmailTaken);
		if (username is not null && await _accounts.UsernameExistsAsync(username, ct: ct))
			throw DomainException.Conflict(IdentityErrors.UsernameTaken);

		SupportedLanguages.TryParse(request.Language, out var language);
		var account = Account.CreateStaff(
			email, username, _hasher.Hash(request.Password), Clean(request.FirstName), Clean(request.LastName),
			Clean(request.PhoneCountryCode), Clean(request.Phone), role.Id, propertyId, creator.Id,
			request.Language is null ? creator.Language : language, _clock.UtcNow);
		account.SetResourceScopes(request.IsResourceScopeRestricted, request.ResourceScopes ?? []);
		_accounts.Add(account);
		await _uow.SaveChangesAsync(ct);

		await PublishContactChannelsAsync(account, propertyId, request.EmailIsContact, request.PhoneIsContact, ct);
		await RecordAsync(IdentityAudit.UserCreated, account, ct);
		return StaffUserDto.FromEntity(account);
	}

	public async Task<StaffUserDto> UpdateAsync(Guid userId, UpdateStaffUserRequest request, CancellationToken ct = default)
	{
		_updateValidator.EnsureValid(request);
		var (account, propertyId) = await GetManageableAsync(userId, ct);

		if (request.Email is { } email && !string.Equals(email.Trim(), account.Email, StringComparison.OrdinalIgnoreCase))
		{
			if (await _accounts.EmailExistsAsync(email.Trim(), account.Id, ct))
				throw DomainException.Conflict(IdentityErrors.EmailTaken);
			account.ChangeEmail(email);
		}

		if (request.Username is not null)
		{
			var username = Clean(request.Username);
			if (username is not null && await _accounts.UsernameExistsAsync(username, account.Id, ct))
				throw DomainException.Conflict(IdentityErrors.UsernameTaken);
			account.ChangeUsername(username);
		}

		if (request.Role is not null)
			account.AssignRole((await GetAssignableRoleAsync(request.Role, ct)).Id);

		account.UpdateProfile(
			Merge(request.FirstName, account.FirstName), Merge(request.LastName, account.LastName),
			Merge(request.PhoneCountryCode, account.PhoneCountryCode), Merge(request.Phone, account.Phone));

		if (SupportedLanguages.TryParse(request.Language, out var language))
			account.SetLanguage(language);

		if (request.IsResourceScopeRestricted is not null || request.ResourceScopes is not null)
			account.SetResourceScopes(request.IsResourceScopeRestricted ?? account.IsResourceScopeRestricted, request.ResourceScopes ?? account.ResourceScopes);

		if (request.Password is not null)
		{
			account.ChangePassword(_hasher.Hash(request.Password), _clock.UtcNow);
			await _sessions.RevokeAllAsync(account.Id, RefreshRevokeReason.PasswordChanged, ct);
		}
		else if (request.Role is not null)
		{
			// Il ruolo viaggia nel token: senza revoca il vecchio resterebbe valido fino al refresh.
			await _sessions.RevokeAllAsync(account.Id, RefreshRevokeReason.Manual, ct);
		}

		await _uow.SaveChangesAsync(ct);

		if (request.EmailIsContact is not null || request.PhoneIsContact is not null)
			await PublishContactChannelsAsync(account, propertyId, request.EmailIsContact ?? false, request.PhoneIsContact ?? false, ct);
		await RecordAsync(IdentityAudit.UserUpdated, account, ct);

		var reloaded = await _accounts.GetByIdAsync(account.Id, ct) ?? account;
		return StaffUserDto.FromEntity(reloaded);
	}

	public async Task DeleteAsync(Guid userId, CancellationToken ct = default)
	{
		var (account, _) = await GetManageableAsync(userId, ct);
		await _sessions.RevokeAllAsync(account.Id, RefreshRevokeReason.AccountDisabled, ct);
		_accounts.Remove(account);
		await _uow.SaveChangesAsync(ct);
		await RecordAsync(IdentityAudit.UserDeleted, account, ct);
	}

	private async Task<(Account Account, Guid PropertyId)> GetManageableAsync(Guid userId, CancellationToken ct)
	{
		var propertyId = _current.RequirePropertyId();
		var me = await _current.GetAsync(ct);
		var account = await _accounts.GetInPropertyAsync(propertyId, userId, ct)
			?? throw DomainException.NotFound(IdentityErrors.AccountNotFound);

		if (account.Id == me.Id)
			throw DomainException.Unprocessable(IdentityErrors.CannotModifySelf);
		if (!account.Role.IsAssignableViaPanel)
			throw DomainException.Forbidden(IdentityErrors.RoleNotAssignable);
		return (account, propertyId);
	}

	private async Task<Role> GetAssignableRoleAsync(string code, CancellationToken ct)
	{
		var role = await _roles.GetByCodeAsync(code.Trim(), ct);
		if (role is null || !role.IsAssignableViaPanel || !role.IsStaff)
			throw DomainException.Unprocessable(IdentityErrors.RoleNotAssignable);
		return role;
	}

	// PROMEMORIA-85-5: le spunte «come contatto» vivono in Notification, che non ha ancora un gestore.
	private Task PublishContactChannelsAsync(Account account, Guid propertyId, bool emailIsContact, bool phoneIsContact, CancellationToken ct)
		=> _events.PublishAsync(new StaffContactChannelsChanged(
			Guid.CreateVersion7(), _clock.UtcNow, account.PublicId, propertyId, emailIsContact, phoneIsContact), ct);

	private Task RecordAsync(string action, Account target, CancellationToken ct)
		=> _audit.RecordAsync(new AuditEntry(
			AuditCategory.Security, IdentityAudit.Module, action, TargetType: IdentityAudit.Targets.User, TargetId: target.PublicId,
			Metadata: new Dictionary<string, object?> { ["email"] = target.Email }), ct);

	/// <summary>null = invariato, stringa vuota = cancella.</summary>
	private static string? Merge(string? value, string? current) => value is null ? current : Clean(value);

	private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
