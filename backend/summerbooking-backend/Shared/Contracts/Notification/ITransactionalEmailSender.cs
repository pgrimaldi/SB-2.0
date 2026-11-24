namespace Shared.Contracts.Notification;

public enum TransactionalEmailKind
{
	PasswordReset = 0,
	SecondFactorCode = 1,
	Welcome = 2,
	/// <summary>Prenotazione online confermata (anche quella che non chiede conferma), con il codice prenotazione.</summary>
	BookingConfirmed = 3,
	/// <summary>Richiesta online ricevuta: lo stabilimento deve confermarla entro la scadenza.</summary>
	BookingRequestReceived = 4,
	/// <summary>Richiesta online rifiutata dallo stabilimento.</summary>
	BookingRejected = 5,
	/// <summary>Richiesta online scaduta senza conferma.</summary>
	BookingExpired = 6,
	/// <summary>Prenotazione annullata dal cliente, con il rimborso.</summary>
	BookingCancelled = 7,
	/// <summary>Pagamento arrivato quando la postazione era già stata presa: rimborso totale.</summary>
	BookingPaymentRefunded = 8,
}

/// <summary>Nomi dei segnaposto dei modelli di email, condivisi fra chi invia e i modelli.</summary>
public static class EmailPlaceholders
{
	public const string FirstName = "firstName";
	public const string Code = "code";
	public const string Minutes = "minutes";
	public const string ResetUrl = "resetUrl";
	public const string LoginUrl = "loginUrl";
	public const string PropertyName = "propertyName";
	public const string BookingNumber = "bookingNumber";
	public const string ReservationCode = "reservationCode";
	public const string Dates = "dates";
	public const string Amount = "amount";
	public const string ExpiresAt = "expiresAt";
	public const string PaymentUrl = "paymentUrl";
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
