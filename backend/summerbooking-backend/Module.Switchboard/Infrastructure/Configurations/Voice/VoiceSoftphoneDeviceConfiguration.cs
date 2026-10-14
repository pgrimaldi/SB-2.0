using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Switchboard.Domain.Entities.Voice;

namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceSoftphoneDeviceConfiguration : IEntityTypeConfiguration<VoiceSoftphoneDevice>
{
	public void Configure(EntityTypeBuilder<VoiceSoftphoneDevice> builder)
	{
		builder.ToTable("switchboard_voice_softphone_devices", "switchboard", t => t.HasComment(
			"Dispositivo softphone registrato per ricevere il push wake prima dell'inoltro di una chiamata al centralino."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_softphone_devices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino a cui è legato il dispositivo (l'endpoint SIP è per-stabilimento).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping/retention.");

		builder.Property(x => x.RegisteredByUserId)
			.HasColumnName("registered_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha registrato il dispositivo dal gestionale, se applicabile.");

		builder.Property(x => x.Platform)
			.HasColumnName("platform")
			.HasColumnType("switchboard.softphone_push_platform")
			.IsRequired()
			.HasComment("Piattaforma del dispositivo; determina l'adapter di invio del push wake.");

		builder.Property(x => x.Token)
			.HasColumnName("token")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Token di registrazione push del provider (FCM registration token o APNs VoIP token).");

		builder.Property(x => x.TokenHash)
			.HasColumnName("token_hash")
			.HasMaxLength(64)
			.IsFixedLength()
			.IsRequired()
			.HasComment("SHA-256 del token, usato per l'indice univoco (i token sono lunghi e variabili).");

		builder.Property(x => x.DeviceLabel)
			.HasColumnName("device_label")
			.HasMaxLength(120)
			.HasComment("Descrizione leggibile del dispositivo (modello / user-agent abbreviato).");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Token utilizzabile; portato a false su UNREGISTERED/InvalidRegistration o rimozione del dispositivo.");

		builder.Property(x => x.RegisteredAt)
			.HasColumnName("registered_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Prima registrazione del dispositivo.");

		builder.Property(x => x.LastSeenAt)
			.HasColumnName("last_seen_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultimo refresh o ultimo push riuscito; guida il purge dei dispositivi stantii.");

		builder.Property(x => x.LastError)
			.HasColumnName("last_error")
			.HasColumnType("text")
			.HasComment("Ultimo errore del provider push per questo token.");

		// bug reale osservato il 2026-09-16: created_at/updated_at non avevano default now() a
		// schema (nessuna convenzione EF globale lo applica, verificato in ModuleDbContextConventions
		// - solo converter UTC e query filter soft-delete) - un ETL che omette queste colonne
		// dall'INSERT (comportamento normale, per lasciare scattare il default) falliva NOT NULL.
		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");


		builder.HasIndex(x => x.TokenHash)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_voice_softphone_devices_token");

		builder.HasIndex(x => x.ConfigId)
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_voice_softphone_devices_config_active");

		builder.HasIndex(x => x.LastSeenAt)
			.HasFilter("is_active")
			.HasDatabaseName("ix_switchboard_voice_softphone_devices_stale");

		builder.HasOne(x => x.Config)
			.WithMany(c => c.SoftphoneDevices)
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_voice_softphone_devices_config")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
