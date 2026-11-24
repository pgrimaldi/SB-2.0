using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Module.Notification.Infrastructure.Outbound;
using Shared.Contracts.Notification;

namespace Module.Notification.Infrastructure.ExternalApi;

/// <summary>Sezione <c>Notification:Smtp</c>; la password arriva dal secret manager.</summary>
public sealed class SmtpOptions
{
	public const string Section = "Notification:Smtp";

	/// <summary>Vuoto = nessun invio: l'email finisce solo nel log (sviluppo).</summary>
	public string? Host { get; set; }
	public int Port { get; set; } = 587;
	public bool EnableSsl { get; set; } = true;
	public string? Username { get; set; }
	public string? Password { get; set; }
	public string FromAddress { get; set; } = "noreply@summerbooking.it";
	public string FromName { get; set; } = "Summer Booking";
}

/// <summary>
/// Invio minimo delle email di servizio di Identity (reset password, codice 2FA, benvenuto), con testi
/// fissi IT/EN. Sarà sostituito dal motore di template di Notification (PROMEMORIA-85-5).
/// </summary>
public sealed class SmtpTransactionalEmailSender : ITransactionalEmailSender
{
	private readonly SmtpOptions _options;
	private readonly OutboundGuard _guard;
	private readonly ILogger<SmtpTransactionalEmailSender> _logger;

	public SmtpTransactionalEmailSender(IOptions<SmtpOptions> options, OutboundGuard guard, ILogger<SmtpTransactionalEmailSender> logger)
	{
		_options = options.Value;
		_guard = guard;
		_logger = logger;
	}

	public async Task SendAsync(TransactionalEmail email, CancellationToken ct = default)
	{
		var (subject, body) = Render(email);

		if (!_guard.Allows(email.ToEmail))
		{
			// Mai il corpo nel log: contiene codici e link monouso.
			_logger.LogWarning("Email {Kind} soppressa (Notification:Outbound:Mode={Mode})", email.Kind, _guard.Mode);
			return;
		}

		if (string.IsNullOrEmpty(_options.Host))
		{
			// Mai il corpo nel log: contiene codici e link monouso.
			_logger.LogWarning("SMTP non configurato: email {Kind} a {To} non inviata", email.Kind, email.ToEmail);
			return;
		}

		using var message = new MailMessage(new MailAddress(_options.FromAddress, _options.FromName), new MailAddress(email.ToEmail))
		{
			Subject = subject,
			Body = body,
			IsBodyHtml = false,
		};
		using var client = new SmtpClient(_options.Host, _options.Port) { EnableSsl = _options.EnableSsl };
		if (!string.IsNullOrEmpty(_options.Username))
			client.Credentials = new NetworkCredential(_options.Username, _options.Password);

		await client.SendMailAsync(message, ct);
		_logger.LogInformation("Email {Kind} inviata a {To}", email.Kind, email.ToEmail);
	}

	private static (string Subject, string Body) Render(TransactionalEmail email)
	{
		var en = email.Language == "en";
		string V(string key) => email.Values.TryGetValue(key, out var value) ? value : string.Empty;
		string Pay(string label) => V("paymentUrl") is { Length: > 0 } url ? label + url : string.Empty;

		return email.Kind switch
		{
			TransactionalEmailKind.PasswordReset => en
				? ("Reset your password", $"To choose a new password open this link within {V("minutes")} minutes:\n\n{V("resetUrl")}\n\nIf you did not ask for it, ignore this email.")
				: ("Reimposta la password", $"Per scegliere una nuova password apri questo link entro {V("minutes")} minuti:\n\n{V("resetUrl")}\n\nSe non l'hai chiesto tu, ignora questa email."),
			TransactionalEmailKind.SecondFactorCode => en
				? ("Your access code", $"Your access code is {V("code")}. It expires in {V("minutes")} minutes.")
				: ("Il tuo codice di accesso", $"Il tuo codice di accesso è {V("code")}. Scade tra {V("minutes")} minuti."),
			TransactionalEmailKind.Welcome => en
				? ("Welcome to Summer Booking", $"Hi {V("firstName")},\n\nyour Summer Booking account is ready.")
				: ("Benvenuto su Summer Booking", $"Ciao {V("firstName")},\n\nil tuo account Summer Booking è pronto."),
			TransactionalEmailKind.BookingConfirmed => en
				? ($"Booking confirmed - {V("propertyName")}", $"Your booking {V("bookingNumber")} at {V("propertyName")} ({V("dates")}) is confirmed.\n\nBooking code: {V("reservationCode")}\nTotal: {V("amount")} EUR{Pay("\n\nPay here: ")}")
				: ($"Prenotazione confermata - {V("propertyName")}", $"La tua prenotazione {V("bookingNumber")} presso {V("propertyName")} ({V("dates")}) è confermata.\n\nCodice prenotazione: {V("reservationCode")}\nTotale: {V("amount")} EUR{Pay("\n\nPaga qui: ")}"),
			TransactionalEmailKind.BookingRequestReceived => en
				? ($"Booking request received - {V("propertyName")}", $"We have received your request {V("bookingNumber")} at {V("propertyName")} ({V("dates")}). The beach club will confirm it by {V("expiresAt")}.\n\nBooking code: {V("reservationCode")}")
				: ($"Richiesta di prenotazione ricevuta - {V("propertyName")}", $"Abbiamo ricevuto la tua richiesta {V("bookingNumber")} presso {V("propertyName")} ({V("dates")}). Lo stabilimento la confermerà entro {V("expiresAt")}.\n\nCodice prenotazione: {V("reservationCode")}"),
			TransactionalEmailKind.BookingRejected => en
				? ($"Booking request declined - {V("propertyName")}", $"{V("propertyName")} could not accept your request {V("bookingNumber")} ({V("dates")}).")
				: ($"Richiesta di prenotazione non accettata - {V("propertyName")}", $"{V("propertyName")} non ha potuto accettare la tua richiesta {V("bookingNumber")} ({V("dates")})."),
			TransactionalEmailKind.BookingExpired => en
				? ($"Booking request expired - {V("propertyName")}", $"Your request {V("bookingNumber")} at {V("propertyName")} ({V("dates")}) was not confirmed in time and has expired.")
				: ($"Richiesta di prenotazione scaduta - {V("propertyName")}", $"La tua richiesta {V("bookingNumber")} presso {V("propertyName")} ({V("dates")}) non è stata confermata in tempo ed è scaduta."),
			TransactionalEmailKind.BookingCancelled => en
				? ($"Booking cancelled - {V("propertyName")}", $"Your booking {V("bookingNumber")} at {V("propertyName")} ({V("dates")}) has been cancelled. Refund: {V("amount")} EUR.")
				: ($"Prenotazione annullata - {V("propertyName")}", $"La tua prenotazione {V("bookingNumber")} presso {V("propertyName")} ({V("dates")}) è stata annullata. Rimborso: {V("amount")} EUR."),
			TransactionalEmailKind.BookingPaymentRefunded => en
				? ($"Payment refunded - {V("propertyName")}", $"Your payment for {V("bookingNumber")} at {V("propertyName")} arrived when the spot was no longer available: we refund {V("amount")} EUR in full.")
				: ($"Pagamento rimborsato - {V("propertyName")}", $"Il tuo pagamento per {V("bookingNumber")} presso {V("propertyName")} è arrivato quando la postazione non era più libera: ti rimborsiamo {V("amount")} EUR per intero."),
			_ => throw new ArgumentOutOfRangeException(nameof(email), email.Kind, null),
		};
	}
}
