namespace Module.Identity.Application.Dtos;

/// <param name="Username">Email oppure username (staff di cassa).</param>
/// <param name="App">Applicazione di provenienza: <c>hub</c>, <c>customer</c>, <c>admin</c>.</param>
public sealed record LoginRequest(string Username, string Password, bool Remember, string App);

public sealed record VerifyOtpRequest(string MfaToken, string Code);

/// <param name="Token">Token del provider: ID token per Google e Apple, access token per Facebook.</param>
/// <param name="Email">Solo per Apple, quando il provider non la fornisce.</param>
/// <param name="TermsAccepted">Obbligatorio a true se l'account non esiste ancora.</param>
public sealed record SocialLoginRequest(
	string Token,
	string App,
	bool Remember,
	string? Email,
	string? FirstName,
	string? LastName,
	string? Language,
	bool TermsAccepted);

public sealed record RegisterRequest(
	string Email,
	string Password,
	string? FirstName,
	string? LastName,
	string? PhoneCountryCode,
	string? Phone,
	string Language,
	bool TermsAccepted,
	bool NewsletterConsent);

public sealed record ForgotPasswordRequest(string Email, string App);

public sealed record ResetPasswordRequest(string Token, string NewPassword);

public sealed record ImpersonationExchangeRequest(string Code);

public sealed record EmailAvailabilityResponse(bool Available);

/// <summary>Dati dell'utente per l'interfaccia; il profilo completo è in <c>GET /api/identity/me</c>.</summary>
public sealed record AuthUserDto(Guid Id, string Email, string Role, Guid? PropertyId);

public sealed record AuthSessionResponse(string AccessToken, int ExpiresIn, AuthUserDto User);

/// <summary>Login con 2FA: nessun token, il codice è stato inviato per email.</summary>
public sealed record TwoFactorRequiredResponse(bool TwoFactorRequired, string MfaToken, int ExpiresIn);

/// <summary>Sessione aperta: la risposta va nel corpo, il refresh token nel cookie.</summary>
public sealed record SessionResult(AuthSessionResponse Response, string RefreshToken, DateTime RefreshExpiresAt, bool IsPersistent);

/// <summary>Esito del login: una sessione oppure la richiesta del secondo fattore.</summary>
public sealed record LoginResult(SessionResult? Session, TwoFactorRequiredResponse? TwoFactor);

/// <summary>Sessione di impersonazione: solo access token, nessun refresh.</summary>
public sealed record ImpersonationSessionResponse(string AccessToken, int ExpiresIn, AuthUserDto User, Guid ImpersonatorId);

public sealed record ImpersonationCodeResponse(string Code, int ExpiresIn);
