namespace Shared.Security;

/// <summary>Schemi di autenticazione.</summary>
public static class AuthSchemes
{
	public const string UserBearer = "UserBearer";
	public const string ApiBearer = "ApiBearer";
}

public static class AuthPolicies
{
	public const string ExternalApi = "ExternalApi";
}

/// <summary>Policy di rate limiting definite nell'host.</summary>
public static class RateLimitPolicies
{
	/// <summary>Login, OTP, registrazione, recupero password: per indirizzo IP.</summary>
	public const string Auth = "auth";
}

/// <summary>Nomi dei claim nei JWT emessi da Identity.</summary>
public static class AuthClaims
{
	public const string Subject = "sub";
	public const string Role = "role";
	public const string PropertyId = "property_id";
	/// <summary>
	/// Attore (RFC 8693): presente solo in impersonazione, <c>{ "sub": "&lt;public_id del systemadmin&gt;" }</c>.
	/// Il token resta del titolare; questo claim dice chi sta agendo davvero.
	/// </summary>
	public const string Actor = "act";
	public const string ClientId = "client_id";
	public const string PartnerApp = "partner_app";
}

/// <summary>
/// Applicazione da cui arriva il login e ruoli ammessi su ciascuna (consumer rifiutato sull'hub, solo
/// systemadmin sul back-office).
/// </summary>
public static class ClientApps
{
	public const string Hub = "hub";
	public const string Customer = "customer";
	public const string Admin = "admin";

	public static bool IsKnown(string? app) => app is Hub or Customer or Admin;

	public static bool IsRoleAllowed(string app, string role, bool isStaffRole) => app switch
	{
		Hub => isStaffRole,
		Customer => role == RoleCodes.Consumer || isStaffRole,
		Admin => role == RoleCodes.SystemAdmin,
		_ => false,
	};
}
