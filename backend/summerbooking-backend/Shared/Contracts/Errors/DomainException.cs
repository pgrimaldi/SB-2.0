namespace Shared.Contracts.Errors;

/// <summary>
/// Errore prevedibile con un codice stabile che il frontend traduce.
/// Il gestore globale dell'host lo converte in Problem Details con lo status indicato.
/// </summary>
public sealed class DomainException : Exception
{
	public string Code { get; }
	public int HttpStatus { get; }
	public IReadOnlyDictionary<string, object?>? Args { get; }

	public DomainException(string code, int httpStatus, IReadOnlyDictionary<string, object?>? args = null)
		: base(code)
	{
		Code = code;
		HttpStatus = httpStatus;
		Args = args;
	}

	public static DomainException BadRequest(string code, IReadOnlyDictionary<string, object?>? args = null) => new(code, 400, args);
	public static DomainException Unauthorized(string code) => new(code, 401);
	public static DomainException Forbidden(string code) => new(code, 403);
	public static DomainException NotFound(string code) => new(code, 404);
	public static DomainException Conflict(string code, IReadOnlyDictionary<string, object?>? args = null) => new(code, 409, args);
	public static DomainException Unprocessable(string code, IReadOnlyDictionary<string, object?>? args = null) => new(code, 422, args);
	public static DomainException TooManyRequests(string code) => new(code, 429);
}
