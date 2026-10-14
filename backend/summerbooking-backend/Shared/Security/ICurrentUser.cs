namespace Shared.Security;

/// <summary>Utente autenticato della richiesta, letto dai claim del token già validato.</summary>
public interface ICurrentUser
{
	bool IsAuthenticated { get; }
	Guid? UserId { get; }
	string? Role { get; }
	Guid? PropertyId { get; }
	Guid? ImpersonatorUserId { get; }

	bool HasPermission(string permission);
}

/// <summary>Dati tecnici della richiesta HTTP corrente, per audit e sessioni.</summary>
public interface IRequestContext
{
	string? ClientIp { get; }
	string? UserAgent { get; }
	string? TraceId { get; }
}
