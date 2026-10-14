using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class AutomationTriggerConfiguration : IEntityTypeConfiguration<AutomationTrigger>
{
	public void Configure(EntityTypeBuilder<AutomationTrigger> builder)
	{
		builder.ToTable("marketing_automation_triggers", "notification", t => t.HasComment(
			"Trigger custom di un'automazione marketing: evento prenotazione che fa scattare l'invio."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_automation_triggers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.AutomationId)
			.HasColumnName("automation_id")
			.IsRequired()
			.HasComment("Automazione a cui il trigger appartiene.");

		builder.Property(x => x.TriggerKind)
			.HasColumnName("trigger_kind")
			.HasColumnType("notification.automation_trigger_kind")
			.IsRequired()
			.HasComment("Evento della prenotazione che fa scattare l'invio (del giorno, prima/dopo l'arrivo/partenza, saldo pendente).");

		builder.Property(x => x.DaysOffset)
			.HasColumnName("days_offset")
			.HasComment("Giorni di anticipo/ritardo per before_booking / after_booking; NULL per of_booking e pending_payments.");

		builder.Property(x => x.SendAtTime)
			.HasColumnName("send_at_time")
			.HasColumnType("time")
			.HasDefaultValue(new TimeOnly(9, 0))
			.HasComment("Ora locale di invio, interpretata nel fuso dell'automazione padre.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione del trigger.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di rimozione del trigger (soft-delete).");

		builder.HasIndex(x => new { x.AutomationId, x.TriggerKind })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_marketing_automation_triggers_automation_kind");

		builder.HasIndex(x => x.TriggerKind)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_automation_triggers_kind");

		builder.HasOne(x => x.Automation)
			.WithMany(a => a.Triggers)
			.HasForeignKey(x => x.AutomationId)
			.HasConstraintName("fk_notification_marketing_automation_triggers_automation")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
