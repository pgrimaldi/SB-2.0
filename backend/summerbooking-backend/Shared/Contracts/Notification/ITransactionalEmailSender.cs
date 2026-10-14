namespace Shared.Contracts.Notification;

public enum TransactionalEmailKind
{
	PasswordReset = 0,
	SecondFactorCode = 1,
	Welcome = 2,
}

/// <summary>Nomi dei segnaposto dei modelli di email, condivisi fra chi invia e i modelli.</summary>
public static class EmailPlaceholders
{
	public const string FirstName = "firstName";
	public const string Code = "code";
	public const string Minutes = "minutes";
	public const string ResetUrl = "resetUrl";
	public const string LoginUrl = "loginUrl";
}

/// <summary>
/// Email di servizio. <see cref="Values"/> sono i segnaposto del modello (<see cref="EmailPlaceholders"/>);
/// <see cref="Language"/> è il codice di una lingua gestita (<c>Shared.Localization.SupportedLanguages</c>).
/// </summary>
public sealed record TransactionalEmail(
	TransactionalEmailKind Kind,
	string ToEmail,
	string Language,
	IReadOnlyDictionary<string, string> Values);

/// <summary>Invio di email di servizio, implementato da Module.Notification.</summary>
public interface ITransactionalEmailSender
{
	Task SendAsync(TransactionalEmail email, CancellationToken ct = default);
}
