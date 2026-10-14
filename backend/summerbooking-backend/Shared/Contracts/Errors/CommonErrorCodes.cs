namespace Shared.Contracts.Errors;

/// <summary>Codici trasversali a tutti i moduli. Un codice rilasciato non si rinomina.</summary>
public static class CommonErrorCodes
{
	public const string ValidationFailed = "validation.failed";
	public const string Unexpected = "server.unexpected";
	public const string Unauthenticated = "auth.unauthenticated";
	public const string Forbidden = "auth.forbidden";
	public const string NotFound = "resource.not_found";
	public const string TooManyRequests = "auth.too_many_attempts";
}
