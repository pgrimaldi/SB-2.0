namespace Module.Notification.Domain.Enums;

/// <summary>Modalità di cifratura del canale SMTP di una configurazione email per stabilimento.</summary>
public enum SmtpSecurity
{
	None = 0,
	Starttls = 1,
	Ssl = 2
}
