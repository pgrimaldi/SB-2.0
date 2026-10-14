using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class EventReminderConfiguration : IEntityTypeConfiguration<EventReminder>
{
	public void Configure(EntityTypeBuilder<EventReminder> builder)
	{
		builder.ToTable("marketing_event_reminders", "notification", t => t.HasComment(
			"Reminder pianificato di un evento marketing, inviato ai contatti della lista dell'evento allo scadere."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_event_reminders");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.EventId)
			.HasColumnName("event_id")
			.IsRequired()
			.HasComment("Evento a cui il reminder appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato dall'evento — nessuna FK cross-schema.");

		builder.Property(x => x.Offset)
			.HasColumnName("offset")
			.HasColumnType("notification.event_reminder_offset")
			.IsRequired()
			.HasDefaultValueSql("'one_day_before'")
			.HasComment("Anticipo rispetto all'evento (1 giorno, 3 giorni, 1 settimana prima, o custom).");

		builder.Property(x => x.CustomDate)
			.HasColumnName("custom_date")
			.HasComment("Data esplicita per offset = custom.");

		builder.Property(x => x.CustomTime)
			.HasColumnName("custom_time")
			.HasColumnType("time")
			.HasComment("Ora esplicita per offset = custom.");

		builder.Property(x => x.ScheduledAt)
			.HasColumnName("scheduled_at")
			.HasComment("Istante assoluto di invio, calcolato da offset (o custom_*) rispetto a starts_on dell'evento e al fuso della property; ricalcolato se l'evento cambia data.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("notification.reminder_status")
			.IsRequired()
			.HasDefaultValueSql("'pending'")
			.HasComment("Stato di invio del reminder.");

		builder.Property(x => x.SentAt)
			.HasColumnName("sent_at")
			.HasComment("Quando il reminder è stato effettivamente inviato.");

		builder.Property(x => x.FailureReason)
			.HasColumnName("failure_reason")
			.HasColumnType("text")
			.HasComment("Motivo del fallimento dell'invio, se status = failed.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione del reminder.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete.");

		builder.HasIndex(x => x.ScheduledAt)
			.HasFilter("status = 'pending' AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_event_reminders_due");

		builder.HasIndex(x => x.EventId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_event_reminders_event");

		builder.HasOne(x => x.Event)
			.WithMany(e => e.Reminders)
			.HasForeignKey(x => x.EventId)
			.HasConstraintName("fk_notification_marketing_event_reminders_event")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
