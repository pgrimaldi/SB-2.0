using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class UserChannelPreferencesConfiguration : IEntityTypeConfiguration<UserChannelPreferences>
{
	public void Configure(EntityTypeBuilder<UserChannelPreferences> builder)
	{
		builder.ToTable("user_channel_preferences", "notification", t => t.HasComment(
			"Preferenze di notifica di un utente C5 (staff/gestore): canali abilitati, non disturbare, tipi silenziati."));

		builder.HasKey(x => x.Id).HasName("pk_notification_user_channel_preferences");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");
		builder.HasIndex(x => x.UserId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_user_channel_preferences_user");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per scoping/retention; NULL per utenti multi-property/piattaforma.");

		builder.Property(x => x.EnabledChannels)
			.HasColumnName("enabled_channels")
			.HasColumnType("notification.staff_channel[]")
			.HasDefaultValueSql("'{in_app}'")
			.IsRequired()
			.HasComment("Canali su cui l'utente vuole ricevere le notifiche operative.");

		builder.Property(x => x.IsMuted)
			.HasColumnName("is_muted")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("\"Non disturbare\" globale: nessuna notifica operativa finché attivo (rispettato dal modulo, il legacy lo ignorava).");

		builder.Property(x => x.MutedKinds)
			.HasColumnName("muted_kinds")
			.HasColumnType("notification.notification_kind[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Tipi di notifica silenziati singolarmente, senza spegnere tutto.");

		builder.Property(x => x.QuietHoursStart)
			.HasColumnName("quiet_hours_start")
			.HasColumnType("time")
			.HasComment("Inizio della finestra oraria in cui non recapitare push/WhatsApp, interpretata nel fuso della property.");

		builder.Property(x => x.QuietHoursEnd)
			.HasColumnName("quiet_hours_end")
			.HasColumnType("time")
			.HasComment("Fine della finestra oraria di non recapito push/WhatsApp.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione delle preferenze.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete; di fatto non usato — si torna ai default cancellando la riga.");
	}
}
