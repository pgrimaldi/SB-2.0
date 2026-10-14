using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class InAppMessageConfiguration : IEntityTypeConfiguration<InAppMessage>
{
	public void Configure(EntityTypeBuilder<InAppMessage> builder)
	{
		builder.ToTable("inapp_messages", "notification", t => t.HasComment(
			"Un evento operativo di uno stabilimento mostrato nel feed della campanella del gestionale; testo renderizzato alla lettura da kind+payload."));

		builder.HasKey(x => x.Id).HasName("pk_notification_inapp_messages");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile per il deep-link dalla campanella.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_notification_inapp_messages_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Unico criterio di targeting: visibile a tutti gli operatori della property.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("notification.inapp_kind")
			.HasDefaultValueSql("'generic'")
			.IsRequired()
			.HasComment("Tipo di evento: determina template di rendering e navigazione UI al click.");

		builder.Property(x => x.SubjectKind)
			.HasColumnName("subject_kind")
			.HasColumnType("notification.inapp_subject_kind")
			.HasDefaultValueSql("'booking'")
			.IsRequired()
			.HasComment("Soggetto della notifica (rotta di navigazione): prenotazione postazioni o servizio.");

		builder.Property(x => x.SubjectPublicId)
			.HasColumnName("subject_public_id")
			.HasComment("public_id di booking.reservations, copiato — nessuna FK cross-schema. NULL per le notifiche di errore Fiskaly.");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(20)
			.HasComment("Numero prenotazione per il redirect al planner; nessun default fittizio.");

		builder.Property(x => x.Visibility)
			.HasColumnName("visibility")
			.HasColumnType("notification.inapp_visibility")
			.HasDefaultValueSql("'visible'")
			.IsRequired()
			.HasComment("Visibilità nel feed: visible, oppure pending_confirmation finché il pagamento non è confermato.");

		builder.Property(x => x.Payload)
			.HasColumnName("payload")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Snapshot delle sole variabili di rendering del template per questo kind, congelate alla creazione.");

		builder.Property(x => x.LegacyText)
			.HasColumnName("legacy_text")
			.HasColumnType("text")
			.HasComment("Stringa originale conservata as-is per le righe migrate il cui parsing verso kind+payload non ha riconosciuto il pattern.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema. NULL per eventi di sistema (webhook Stripe, cron Fiskaly).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data creazione dell'evento; unico ordinamento del feed.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data ultima modifica (es. transizione di visibility).");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete; retention operativa 6-12 mesi, non è un archivio.");

		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasFilter("visibility = 'visible' AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_inapp_messages_feed");

		builder.HasIndex(x => new { x.SubjectKind, x.SubjectPublicId })
			.HasDatabaseName("ix_notification_inapp_messages_subject");
	}
}
