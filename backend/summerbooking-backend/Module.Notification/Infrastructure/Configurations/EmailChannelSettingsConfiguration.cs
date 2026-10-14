using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class EmailChannelSettingsConfiguration : IEntityTypeConfiguration<EmailChannelSettings>
{
	public void Configure(EntityTypeBuilder<EmailChannelSettings> builder)
	{
		builder.ToTable("email_channel_settings", "notification", t => t.HasComment(
			"Configurazione SMTP in uscita per stabilimento: mittente proprio al posto di noreply@summerbooking.it."));

		builder.HasKey(x => x.Id).HasName("pk_notification_email_channel_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");
		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_email_channel_settings_property");

		builder.Property(x => x.FromEmail)
			.HasColumnName("from_email")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Indirizzo mittente delle email inviate per conto della property.");

		builder.Property(x => x.FromName)
			.HasColumnName("from_name")
			.HasMaxLength(160)
			.HasComment("Nome visualizzato del mittente, usato nel From (\"from_name\" <from_email>).");

		builder.Property(x => x.SmtpHost)
			.HasColumnName("smtp_host")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Host del server SMTP.");

		builder.Property(x => x.SmtpPort)
			.HasColumnName("smtp_port")
			.HasDefaultValue(465)
			.IsRequired()
			.HasComment("Porta SMTP (465 TLS implicito, 587 STARTTLS, 25).");

		builder.Property(x => x.SmtpUsername)
			.HasColumnName("smtp_username")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Utente per l'autenticazione SMTP.");

		builder.Property(x => x.SmtpPasswordSecretRef)
			.HasColumnName("smtp_password_secret_ref")
			.HasMaxLength(255)
			.HasComment("Riferimento (ARN/nome) alla password SMTP nel secret manager; nessun materiale segreto in tabella, risolto a runtime.");

		builder.Property(x => x.Security)
			.HasColumnName("security")
			.HasColumnType("notification.smtp_security")
			.HasDefaultValueSql("'ssl'")
			.IsRequired()
			.HasComment("Modalità di cifratura del canale SMTP.");

		builder.Property(x => x.VerifyCertificate)
			.HasColumnName("verify_certificate")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Se validare il certificato TLS del server SMTP; false ammesso solo con motivazione.");

		builder.Property(x => x.TestRecipients)
			.HasColumnName("test_recipients")
			.HasColumnType("text[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Destinatari dell'invio di prova dal pannello gestionale.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Se il canale è usato per gli invii della property; false = fallback al mittente di piattaforma.");

		builder.Property(x => x.LastVerificationAt)
			.HasColumnName("last_verification_at")
			.HasComment("Istante dell'ultimo TENTATIVO di verifica SMTP (handshake + auth + invio di prova); NULL = mai verificata.");

		builder.Property(x => x.LastVerificationError)
			.HasColumnName("last_verification_error")
			.HasColumnType("text")
			.HasComment("Dettaglio testuale dell'errore dell'ultima verifica SMTP; NULL con last_verification_at valorizzato = verifica riuscita. Sostituisce il flag is_verified, che era derivabile da questa coppia.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione della configurazione.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete (ex deleteEmailConfig).");
	}
}
