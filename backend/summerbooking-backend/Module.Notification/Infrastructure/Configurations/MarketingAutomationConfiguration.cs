using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class MarketingAutomationConfiguration : IEntityTypeConfiguration<MarketingAutomation>
{
	public void Configure(EntityTypeBuilder<MarketingAutomation> builder)
	{
		builder.ToTable("marketing_automations", "notification", t => t.HasComment(
			"Automazione marketing ricorrente: email (+ push opzionale) inviata ai contatti di una lista in relazione alle loro prenotazioni."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_automations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile (API/URL).");
		builder.HasIndex(x => x.PublicId).IsUnique()
			.HasDatabaseName("uq_notification_marketing_automations_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(160)
			.HasComment("Nome interno dell'automazione.");

		builder.Property(x => x.AutomationKind)
			.HasColumnName("automation_kind")
			.HasColumnType("notification.automation_kind")
			.IsRequired()
			.HasDefaultValueSql("'custom'")
			.HasComment("Comportamento dell'automazione: welcome, goodbye o custom.");

		builder.Property(x => x.State)
			.HasColumnName("state")
			.HasColumnType("notification.automation_state")
			.IsRequired()
			.HasDefaultValueSql("'draft'")
			.HasComment("Stato dell'automazione ricorrente.");

		builder.Property(x => x.ScheduleType)
			.HasColumnName("schedule_type")
			.HasColumnType("notification.automation_schedule_type")
			.IsRequired()
			.HasDefaultValueSql("'before'")
			.HasComment("Strategia di pianificazione: prima dell'arrivo, dopo la partenza, o trigger custom.");

		builder.Property(x => x.DaysOffset)
			.HasColumnName("days_offset")
			.HasDefaultValue((short)1)
			.HasComment("Giorni prima dell'arrivo (before) o dopo la partenza (after); NULL per schedule_type custom.");

		builder.Property(x => x.SendAtTime)
			.HasColumnName("send_at_time")
			.HasColumnType("time")
			.HasDefaultValue(new TimeOnly(9, 0))
			.HasComment("Ora locale di invio nel giorno calcolato, interpretata nel fuso di Timezone.");

		builder.Property(x => x.Timezone)
			.HasColumnName("timezone")
			.HasMaxLength(64)
			.IsRequired()
			.HasDefaultValue("Europe/Rome")
			.HasComment("Fuso IANA per il calcolo di send_at_time.");

		builder.Property(x => x.Subject)
			.HasColumnName("subject")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Oggetto dell'email (e titolo della push, se also_send_push).");

		builder.Property(x => x.TemplateId)
			.HasColumnName("template_id")
			.IsRequired()
			.HasComment("Template del corpo (template_kind = automation).");

		builder.Property(x => x.ContactListId)
			.HasColumnName("contact_list_id")
			.HasComment("Segmento destinatario, risolto all'esecuzione.");

		builder.Property(x => x.AlsoSendPush)
			.HasColumnName("also_send_push")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se inviare anche una push FCM ai destinatari utenti C5 con device token.");

		builder.Property(x => x.LastRunAt)
			.HasColumnName("last_run_at")
			.HasComment("Ultimo tick in cui l'automazione ha prodotto invii; guida la retention.");

		builder.Property(x => x.TotalSent)
			.HasColumnName("total_sent")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Totale storico messaggi spediti da questa automazione.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione dell'automazione.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete.");

		builder.HasIndex(x => new { x.PropertyId, x.ScheduleType })
			.HasFilter("state = 'active' AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_automations_active");

		// Indici di supporto alle FK: nome esplicito per la convenzione del progetto (prefisso flat
		// ix_notification_*), che EF altrimenti deriva dal nome tabella canonico.
		builder.HasIndex(x => x.TemplateId)
			.HasDatabaseName("ix_notification_marketing_automations_template_id");

		builder.HasIndex(x => x.ContactListId)
			.HasDatabaseName("ix_notification_marketing_automations_contact_list_id");

		builder.HasOne(x => x.Template)
			.WithMany()
			.HasForeignKey(x => x.TemplateId)
			.HasConstraintName("fk_notification_marketing_automations_template")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.ContactList)
			.WithMany()
			.HasForeignKey(x => x.ContactListId)
			.HasConstraintName("fk_notification_marketing_automations_contact_list")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
