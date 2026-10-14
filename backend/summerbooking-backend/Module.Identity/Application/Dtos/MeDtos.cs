using Module.Identity.Domain.Entities;
using Module.Identity.Domain.Enums;
using Shared.Localization;
using Shared.Utils;

namespace Module.Identity.Application.Dtos;

/// <param name="AllFeaturesUnlocked">
/// True finché i controlli di licenza sono disattivati (PROMEMORIA-85-1).
/// </param>
public sealed record MeDto(
	Guid Id,
	string Email,
	string? Username,
	string? FirstName,
	string? LastName,
	string? PhoneCountryCode,
	string? Phone,
	string Language,
	string? ProfilePictureUrl,
	string Role,
	Guid? PropertyId,
	IReadOnlyCollection<string> Permissions,
	bool HasPassword,
	string LoginMethod,
	IReadOnlyList<string> NotificationSounds,
	bool NewsSectionEnabled,
	bool IsResourceScopeRestricted,
	IReadOnlyList<string> ResourceScopes,
	bool TwoFactorEnabled,
	SubscriptionSummaryDto? Subscription,
	bool AllFeaturesUnlocked,
	Guid? ImpersonatedBy);

public sealed record SubscriptionSummaryDto(string LicensePlanCode, string Status, DateTime StartsAt, DateTime EndsAt, int MonthsToExpiry);

public sealed record UpdateProfileRequest(string? FirstName, string? LastName, string? PhoneCountryCode, string? Phone);

/// <param name="CurrentPassword">Obbligatoria se l'account ha già una password.</param>
public sealed record ChangePasswordRequest(string? CurrentPassword, string NewPassword);

/// <summary>Campi null = invariati.</summary>
public sealed record UpdatePreferencesRequest(string? Language, IReadOnlyList<string>? NotificationSounds, bool? NewsSectionEnabled);

/// <summary>Conferma con la password, oppure con un nuovo login presso il provider per gli account solo social.</summary>
public sealed record DeleteAccountRequest(string? Password, string? Provider, string? ProviderToken);

public sealed record SessionDto(Guid Id, DateTime LastActivityAt, DateTime ExpiresAt, string? UserAgent, string? Ip, bool IsPersistent, bool IsCurrent);

public sealed record LikedBathhouseDto(Guid BathhouseId, DateTime LikedAt)
{
	public static LikedBathhouseDto FromEntity(BathhouseLike like) => new(like.BathhouseId, like.LikedAt);
}

public sealed record StaffUserDto(
	Guid Id,
	string Email,
	string? Username,
	string? FirstName,
	string? LastName,
	string? PhoneCountryCode,
	string? Phone,
	string Role,
	string Language,
	bool IsResourceScopeRestricted,
	IReadOnlyList<string> ResourceScopes,
	DateTime? LastLoginAt,
	DateTime CreatedAt)
{
	public static StaffUserDto FromEntity(Account a) => new(
		a.PublicId, a.Email, a.Username, a.FirstName, a.LastName, a.PhoneCountryCode, a.Phone,
		a.Role.Code, SupportedLanguages.Code(a.Language), a.IsResourceScopeRestricted, a.ResourceScopes,
		a.LastLoginAt, a.CreatedAt);
}

public sealed record CreateStaffUserRequest(
	string Email,
	string? Username,
	string Password,
	string? FirstName,
	string? LastName,
	string? PhoneCountryCode,
	string? Phone,
	string Role,
	string? Language,
	bool EmailIsContact,
	bool PhoneIsContact,
	bool IsResourceScopeRestricted,
	IReadOnlyList<string>? ResourceScopes);

/// <summary>Campi null = invariati; stringa vuota = cancella il valore.</summary>
public sealed record UpdateStaffUserRequest(
	string? Email,
	string? Username,
	string? Password,
	string? FirstName,
	string? LastName,
	string? PhoneCountryCode,
	string? Phone,
	string? Role,
	string? Language,
	bool? EmailIsContact,
	bool? PhoneIsContact,
	bool? IsResourceScopeRestricted,
	IReadOnlyList<string>? ResourceScopes);

public sealed record RoleDto(string Code);

public sealed record SecuritySettingsDto(bool TwoFactorEnabled, string Channel)
{
	public static SecuritySettingsDto FromEntity(PropertySecuritySettings? s)
		=> new(s?.IsTwoFactorEnabled ?? false, Codes.Of(s?.TwoFactorChannel ?? SecondFactorChannel.Email));
}

public sealed record UpdateSecuritySettingsRequest(bool TwoFactorEnabled);
