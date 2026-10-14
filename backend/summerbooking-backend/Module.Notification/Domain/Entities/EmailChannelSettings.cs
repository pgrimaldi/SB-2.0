using Module.Notification.Domain.Enums;
using Shared.Contracts;

namespace Module.Notification.Domain.Entities;

/// <summary>
/// Configurazione SMTP in uscita per stabilimento: mittente proprio al posto di
/// noreply@summerbooking.it. Nessuna riga = fallback al mittente di piattaforma.
/// </summary>
public sealed class EmailChannelSettings : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PropertyId { get; private set; }
	public string FromEmail { get; private set; } = null!;
	public string? FromName { get; private set; }
	public string SmtpHost { get; private set; } = null!;
	public int SmtpPort { get; private set; }
	public string SmtpUsername { get; private set; } = null!;
	public string? SmtpPasswordSecretRef { get; private set; }
	public SmtpSecurity Security { get; private set; }
	public bool VerifyCertificate { get; private set; }
	public string[] TestRecipients { get; private set; } = null!;
	public bool IsActive { get; private set; }
	public DateTime? LastVerificationAt { get; private set; }
	public string? LastVerificationError { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private EmailChannelSettings() { }   // unico costruttore, per il materializzatore di EF Core
}
