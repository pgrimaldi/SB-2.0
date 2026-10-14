namespace Module.Identity.Application;

/// <summary>
/// Parametri di autenticazione, sezione <c>Auth</c> della configurazione: mai costanti nel codice.
/// </summary>
public sealed class IdentityAuthOptions
{
	public const string Section = "Auth";

	public int AccessTokenMinutes { get; set; } = 10080;
	public int RefreshTokenDays { get; set; } = 30;
	public int RefreshTokenRememberMeDays { get; set; } = 90;
	public int ExternalTokenMinutes { get; set; } = 60;
	/// <summary>Durata del token emesso da <c>auth/signinApi</c> (OfSale, Transactions).</summary>
	public int SignedPartnerTokenHours { get; set; } = 24;
	/// <summary>Durata del token TeamSystem.</summary>
	public int TeamSystemTokenDays { get; set; } = 7;
	public int OtpChallengeMinutes { get; set; } = 10;
	public int OtpDigits { get; set; } = 6;
	public short OtpMaxAttempts { get; set; } = 5;
	public int PasswordResetMinutes { get; set; } = 60;
	public int LockoutThreshold { get; set; } = 5;
	public int LockoutMinutes { get; set; } = 15;
	public int ImpersonationTicketSeconds { get; set; } = 60;
	public int ImpersonationSessionMinutes { get; set; } = 60;
	/// <summary>
	/// Tolleranza sul timestamp (Unix in millisecondi) della firma HMAC dei partner, contro il replay.
	/// 0 disattiva il controllo.
	/// </summary>
	public int PartnerSignatureToleranceSeconds { get; set; } = 300;
	/// <summary>Versione corrente del testo di privacy e termini accettato alla registrazione.</summary>
	public string TermsVersion { get; set; } = "2026-01";
	/// <summary>Origini ammesse su refresh e logout (controllo dell'header Origin, oltre a SameSite=Strict) e per CORS.</summary>
	public List<string> AllowedOrigins { get; set; } = [];
	/// <summary>Indirizzo base di ciascun frontend (<c>hub</c>, <c>customer</c>, <c>admin</c>) per i link nelle email.</summary>
	public Dictionary<string, string> FrontendUrls { get; set; } = new();
}
