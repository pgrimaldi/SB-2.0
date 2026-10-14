namespace Shared.Security;

/// <summary>Chiave di firma con il suo identificativo (header <c>kid</c>); <see cref="Key"/> in base64, almeno 256 bit.</summary>
public sealed class JwtKey
{
	public string Kid { get; set; } = string.Empty;
	public string Key { get; set; } = string.Empty;
}

/// <summary>
/// Sezione <c>Jwt</c>: Identity firma con la prima chiave dell'elenco, chi valida le accetta tutte, così
/// rotazione e passaggio a RS256 sono configurazione.
/// Le chiavi vengono dal secret manager, mai da appsettings committati.
/// </summary>
public sealed class JwtOptions
{
	public const string Section = "Jwt";

	public string Issuer { get; set; } = "summerbooking-identity";
	public string UserAudience { get; set; } = "summerbooking-app";
	public string ApiAudience { get; set; } = "summerbooking-external-api";
	public int ClockSkewSeconds { get; set; } = 30;
	public List<JwtKey> UserKeys { get; set; } = [];
	public List<JwtKey> ApiKeys { get; set; } = [];
}
