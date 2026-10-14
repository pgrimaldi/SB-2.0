using Module.Identity.Domain.Enums;

namespace Module.Identity.Application.Abstractions;

public enum PasswordCheck { Failed = 0, Success = 1, SuccessRehashNeeded = 2 }

/// <summary>Hash in formato PHC (Argon2id + pepper); verifica anche gli hash bcrypt, che sostituisce al primo login.</summary>
public interface IPasswordHasher
{
	string Hash(string password);
	PasswordCheck Verify(string hash, string password);
}

/// <summary>Elenco delle password più comuni, rifiutate alla scelta di una nuova password.</summary>
public interface ICommonPasswordList
{
	bool Contains(string password);
}

/// <summary>Token opachi casuali e codici numerici; mai salvati in chiaro, solo il loro hash.</summary>
public interface ISecureTokenGenerator
{
	string NewOpaqueToken();
	string NewNumericCode(int digits);
	string Hash(string value);
	bool FixedTimeEquals(string hashA, string hashB);
}

/// <summary>Contenuto del token di accesso di un utente.</summary>
public sealed record AccessTokenSubject(Guid UserId, string Role, Guid? PropertyId, Guid? ImpersonatorUserId = null);

/// <summary>Contenuto del token intermedio del login con 2FA.</summary>
public sealed record MfaTicket(Guid UserId, bool Remember, string App, LoginMethod Method);

/// <summary>Contenuto del codice di impersonazione (monouso, vita brevissima).</summary>
public sealed record ImpersonationTicket(string TicketId, Guid TargetUserId, Guid ImpersonatorUserId);

/// <summary>Contenuto del token di un client M2M.</summary>
public sealed record ExternalTokenSubject(Guid ClientPublicId, string ClientId, string App, Guid? PropertyId);

public sealed record IssuedToken(string Token, int ExpiresInSeconds);

/// <summary>Emissione e lettura dei JWT firmati da Identity.</summary>
public interface ITokenIssuer
{
	IssuedToken IssueAccessToken(AccessTokenSubject subject, TimeSpan? lifetime = null);
	IssuedToken IssueMfaTicket(MfaTicket ticket);
	Task<MfaTicket?> ReadMfaTicketAsync(string token);
	IssuedToken IssueImpersonationTicket(ImpersonationTicket ticket);
	Task<ImpersonationTicket?> ReadImpersonationTicketAsync(string token);
	IssuedToken IssueExternalToken(ExternalTokenSubject subject, TimeSpan lifetime);
}

/// <summary>Identità verificata presso il provider social.</summary>
public sealed record SocialIdentity(string Subject, string? Email, bool EmailVerified, string? FirstName, string? LastName, string? PictureUrl);

/// <summary>Verifica del token del provider sempre lato server: mai fidarsi di email o id mandati dal client.</summary>
public interface ISocialIdentityVerifier
{
	Task<SocialIdentity?> VerifyAsync(LoginMethod provider, string token, CancellationToken ct = default);
}

/// <summary>Conteggio dei tentativi falliti per chiave (account, email): blocco temporaneo oltre la soglia.</summary>
public interface ILoginThrottle
{
	bool IsLocked(string key);
	void RegisterFailure(string key);
	void Reset(string key);
}

/// <summary>Segna un identificativo come usato; false se lo era già (codici monouso).</summary>
public interface IReplayGuard
{
	bool TryConsume(string id, TimeSpan window);
}
