using Shared.Utils;
using System.Text.Json.Serialization;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Application.Dtos;

public sealed record SubscriptionDto(
	Guid PropertyId, string LicensePlanCode, string Status, string TermType,
	DateTime StartsAt, DateTime EndsAt, DateTime? SuspendedAt, DateTime? ReactivatedAt, int? RemainingDaysBalance)
{
	public static SubscriptionDto FromEntity(Subscription s) => new(
		s.PropertyId, s.LicensePlan.Code, Codes.Of(s.Status), Codes.Of(s.TermType),
		s.StartsAt, s.EndsAt, s.SuspendedAt, s.ReactivatedAt, s.RemainingDaysBalance);
}

public sealed record SubscriptionPeriodDto(
	string LicensePlanCode, string Kind, DateTime StartsAt, DateTime? EndsAt,
	decimal? Amount, string Currency, DateTime? ClosedAt, string? CloseReason)
{
	public static SubscriptionPeriodDto FromEntity(SubscriptionPeriod p) => new(
		p.LicensePlanCode, Codes.Of(p.PeriodKind), p.StartsAt, p.EndsAt, p.Amount, p.Currency, p.ClosedAt,
		p.CloseReason is { } r ? Codes.Of(r) : null);
}

/// <summary>Crea l'abbonamento o, se esiste, apre un nuovo periodo (rinnovo).</summary>
public sealed record SetSubscriptionRequest(string LicensePlanCode, DateTime StartsAt, DateTime EndsAt, decimal? Amount, string? Currency);

public sealed record LicensePlanTranslationDto(string Culture, string Label, string? Description);

public sealed record LicensePlanDto(string Code, short DurationMonths, short SortOrder, bool IsActive, IReadOnlyList<LicensePlanTranslationDto> Translations)
{
	public static LicensePlanDto FromEntity(LicensePlan p) => new(
		p.Code, p.DurationMonths, p.SortOrder, p.IsActive,
		p.Translations.OrderBy(t => t.Culture).Select(t => new LicensePlanTranslationDto(t.Culture, t.Label, t.Description)).ToList());
}

public sealed record CreateLicensePlanRequest(string Code, short DurationMonths, short SortOrder, IReadOnlyList<LicensePlanTranslationDto> Translations);

public sealed record UpdateLicensePlanRequest(short DurationMonths, short SortOrder, bool IsActive, IReadOnlyList<LicensePlanTranslationDto> Translations);

public sealed record ContractDocumentDto(
	Guid Id, string VersionCode, string? Title, string Status, string? ContentSha256,
	DateTime? PublishedAt, DateTime? SupersededAt, string? Notes, DateTime CreatedAt)
{
	public static ContractDocumentDto FromEntity(ContractDocument d) => new(
		d.PublicId, d.VersionCode, d.Title, Codes.Of(d.Status), d.ContentSha256, d.PublishedAt, d.SupersededAt, d.Notes, d.CreatedAt);
}

public sealed record CreateContractDocumentRequest(string VersionCode, string? Title, string? Notes);

public sealed record CurrentContractDto(
	Guid Id, string VersionCode, string? Title, DateTime? PublishedAt, string DownloadUrl, bool Accepted, DateTime? AcceptedAt);

public sealed record SetContractAcceptanceRequest(bool Accepted);

public sealed record CreateOwnerRequest(
	string Email,
	string? Username,
	string Password,
	string? FirstName,
	string? LastName,
	string? PhoneCountryCode,
	string? Phone,
	string? Language,
	bool? OnlineReservationEnabled);

/// <summary>Email e username non modificabili; campi null = invariati.</summary>
public sealed record UpdateOwnerRequest(
	string? Password,
	string? FirstName,
	string? LastName,
	string? PhoneCountryCode,
	string? Phone,
	string? Language,
	bool? OnlineReservationEnabled);

/// <summary>Valori fissati dal protocollo OAuth 2.0 (RFC 6749) usati dai token dei partner.</summary>
public static class OAuthValues
{
	public const string GrantClientCredentials = "client_credentials";
	public const string TokenTypeBearer = "Bearer";
	public const string ErrorInvalidRequest = "invalid_request";
	public const string ErrorInvalidClient = "invalid_client";
	public const string ErrorServerError = "server_error";
}

/// <summary>client_credentials (nomi OAuth 2.0).</summary>
public sealed record ExternalTokenRequest(
	[property: JsonPropertyName("client_id")] string ClientId,
	[property: JsonPropertyName("client_secret")] string ClientSecret,
	[property: JsonPropertyName("grant_type")] string? GrantType);

public sealed record ExternalTokenResponse(
	[property: JsonPropertyName("access_token")] string AccessToken,
	[property: JsonPropertyName("token_type")] string TokenType,
	[property: JsonPropertyName("expires_in")] int ExpiresIn);

/// <summary>Contratto dei partner OfSale e Transactions su <c>POST /api/auth/signinApi</c>: forma fissata dai partner.</summary>
public sealed record SignedPartnerTokenRequest(string? ApiKey, string? Signature, string? Timestamp);
