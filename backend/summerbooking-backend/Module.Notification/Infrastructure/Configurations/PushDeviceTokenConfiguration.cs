using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class PushDeviceTokenConfiguration : IEntityTypeConfiguration<PushDeviceToken>
{
	public void Configure(EntityTypeBuilder<PushDeviceToken> builder)
	{
		builder.ToTable("push_device_tokens", "notification", t => t.HasComment(
			"Un token di dispositivo registrato da un utente C5 per ricevere notifiche push, con piattaforma, ultimo uso e stato."));

		builder.HasKey(x => x.Id).HasName("pk_notification_push_device_tokens");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per scoping/retention.");

		builder.Property(x => x.Token)
			.HasColumnName("token")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Token di registrazione push del provider (FCM/APNs/web push), valore opaco.");

		builder.Property(x => x.TokenHash)
			.HasColumnName("token_hash")
			.HasColumnType("char(64)")
			.IsRequired()
			.HasComment("SHA-256 del token, usato per l'indice UNIQUE (i token FCM sono lunghi e variabili).");
		builder.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("uq_notification_push_device_tokens_token");

		builder.Property(x => x.Platform)
			.HasColumnName("platform")
			.HasColumnType("notification.push_platform")
			.HasDefaultValueSql("'fcm_android'")
			.IsRequired()
			.HasComment("Piattaforma del dispositivo; determina l'adapter di invio.");

		builder.Property(x => x.DeviceLabel)
			.HasColumnName("device_label")
			.HasMaxLength(120)
			.HasComment("Descrizione leggibile del dispositivo (modello/user-agent abbreviato).");

		builder.Property(x => x.AppVersion)
			.HasColumnName("app_version")
			.HasMaxLength(20)
			.HasComment("Versione dell'app mobile al momento della registrazione, per diagnostica.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Token utilizzabile; portato a false quando il provider segnala UNREGISTERED/InvalidRegistration o l'utente rimuove il dispositivo.");

		builder.Property(x => x.RegisteredAt)
			.HasColumnName("registered_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Istante di prima registrazione del token.");

		builder.Property(x => x.LastSeenAt)
			.HasColumnName("last_seen_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Istante dell'ultimo refresh o dell'ultimo invio riuscito; guida il purge dei token stantii.");

		builder.Property(x => x.LastError)
			.HasColumnName("last_error")
			.HasColumnType("text")
			.HasComment("Ultimo errore del provider riportato per questo token.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: dispositivo rimosso. Retention: token inattivo da N mesi → hard-delete.");

		builder.HasIndex(x => x.UserId)
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_push_device_tokens_user_active");

		builder.HasIndex(x => x.LastSeenAt)
			.HasFilter("is_active")
			.HasDatabaseName("ix_notification_push_device_tokens_stale");
	}
}
