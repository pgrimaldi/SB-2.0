using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class MarketingEventConfiguration : IEntityTypeConfiguration<MarketingEvent>
{
	public void Configure(EntityTypeBuilder<MarketingEvent> builder)
	{
		builder.ToTable("marketing_events", "notification", t =>
		{
			t.HasComment("Evento/iniziativa di uno stabilimento (serata, torneo, offerta a tema), promuovibile in vetrina e con reminder.");
			t.HasCheckConstraint("ck_notification_marketing_events_ends_on", "ends_on >= starts_on");
			t.HasCheckConstraint("ck_notification_marketing_events_repeat_interval", "repeat_interval IS NULL OR repeat_interval >= 1");
		});

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_events");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile (API/URL), copiato anche nella proiezione verso la vetrina C2.");
		builder.HasIndex(x => x.PublicId).IsUnique()
			.HasDatabaseName("uq_notification_marketing_events_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(200)
			.IsRequired()
			.HasComment("Nome dell'evento.");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasColumnType("text")
			.HasComment("Testo descrittivo.");

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.IsRequired()
			.HasComment("Primo giorno dell'evento (della prima occorrenza se ricorrente).");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.IsRequired()
			.HasComment("Ultimo giorno dell'evento.");

		builder.Property(x => x.StartTime)
			.HasColumnName("start_time")
			.HasColumnType("time")
			.HasComment("Orario di inizio; ignorato se is_full_day.");

		builder.Property(x => x.EndTime)
			.HasColumnName("end_time")
			.HasColumnType("time")
			.HasComment("Orario di fine; ignorato se is_full_day.");

		builder.Property(x => x.IsFullDay)
			.HasColumnName("is_full_day")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Evento \"tutto il giorno\": se vero, start_time/end_time sono ignorati.");

		builder.Property(x => x.RepeatType)
			.HasColumnName("repeat_type")
			.HasColumnType("notification.event_repeat_type")
			.IsRequired()
			.HasDefaultValueSql("'never'")
			.HasComment("Regola di ricorrenza dell'evento.");

		builder.Property(x => x.RepeatInterval)
			.HasColumnName("repeat_interval")
			.HasDefaultValue((short)1)
			.HasComment("Ogni quante unità ripetere (solo repeat_type = custom).");

		builder.Property(x => x.RepeatUnit)
			.HasColumnName("repeat_unit")
			.HasColumnType("notification.event_repeat_unit")
			.HasComment("Unità di ripetizione per repeat_type = custom (giorno/settimana/mese/anno).");

		builder.Property(x => x.RepeatUntil)
			.HasColumnName("repeat_until")
			.HasComment("Data limite oltre cui non generare occorrenze; NULL = nessun limite / evento singolo.");

		builder.Property(x => x.CoverImageAssetId)
			.HasColumnName("cover_image_asset_id")
			.HasComment("Immagine di copertina dell'evento.");

		builder.Property(x => x.ContactListId)
			.HasColumnName("contact_list_id")
			.HasComment("Lista destinataria dei reminder dell'evento.");

		builder.Property(x => x.IsPublic)
			.HasColumnName("is_public")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se l'evento è mostrato sulla pagina pubblica dello stabilimento (filtra davvero la proiezione verso C2).");

		builder.Property(x => x.RemindersEnabled)
			.HasColumnName("reminders_enabled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se inviare i reminder pianificati dell'evento.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Evento visibile in gestionale/vetrina.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione dell'evento.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete.");

		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => new { x.PropertyId, x.StartsOn }, "ix_notification_marketing_events_property_active")
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_events_property_active");

		builder.HasIndex(x => new { x.PropertyId, x.StartsOn }, "ix_notification_marketing_events_public")
			.HasFilter("is_public AND is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_events_public");

		// Indici di supporto alle FK: nome esplicito per la convenzione del progetto (prefisso flat
		// ix_notification_*), che EF altrimenti deriva dal nome tabella canonico.
		builder.HasIndex(x => x.CoverImageAssetId)
			.HasDatabaseName("ix_notification_marketing_events_cover_image_asset_id");

		builder.HasIndex(x => x.ContactListId)
			.HasDatabaseName("ix_notification_marketing_events_contact_list_id");

		builder.HasOne(x => x.CoverImageAsset)
			.WithMany()
			.HasForeignKey(x => x.CoverImageAssetId)
			.HasConstraintName("fk_notification_marketing_events_cover_image_asset")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.ContactList)
			.WithMany()
			.HasForeignKey(x => x.ContactListId)
			.HasConstraintName("fk_notification_marketing_events_contact_list")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
