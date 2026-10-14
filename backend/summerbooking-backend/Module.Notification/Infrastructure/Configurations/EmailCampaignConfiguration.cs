using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class EmailCampaignConfiguration : IEntityTypeConfiguration<EmailCampaign>
{
	public void Configure(EntityTypeBuilder<EmailCampaign> builder)
	{
		builder.ToTable("marketing_email_campaigns", "notification", t => t.HasComment(
			"Campagna email one-shot verso un segmento: oggetto + corpo + allegati, inviata subito o pianificata."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_email_campaigns");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile, usato nell'URL del gestionale.");
		builder.HasIndex(x => x.PublicId).IsUnique()
			.HasDatabaseName("uq_notification_marketing_email_campaigns_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(160)
			.HasComment("Nome interno della campagna.");

		builder.Property(x => x.Subject)
			.HasColumnName("subject")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Oggetto dell'email (e titolo della push se also_send_push).");

		builder.Property(x => x.TemplateId)
			.HasColumnName("template_id")
			.IsRequired()
			.HasComment("Template del corpo (template_kind = campaign); un corpo ad-hoc genera un template inline dedicato.");

		builder.Property(x => x.ContactListId)
			.HasColumnName("contact_list_id")
			.HasComment("Segmento destinatario, risolto all'invio (non snapshot).");

		builder.Property(x => x.State)
			.HasColumnName("state")
			.HasColumnType("notification.campaign_state")
			.IsRequired()
			.HasDefaultValueSql("'draft'")
			.HasComment("Stato del ciclo di vita della campagna.");

		builder.Property(x => x.ScheduledAt)
			.HasColumnName("scheduled_at")
			.HasComment("Istante di invio programmato, assoluto; NULL per bozza o invio immediato.");

		builder.Property(x => x.Timezone)
			.HasColumnName("timezone")
			.HasMaxLength(64)
			.IsRequired()
			.HasDefaultValue("Europe/Rome")
			.HasComment("Fuso del gestore, solo per ricostruire il form (non usato per la schedulazione, che usa scheduled_at assoluto).");

		builder.Property(x => x.AlsoSendPush)
			.HasColumnName("also_send_push")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se, dopo l'email, inviare anche una push FCM ai destinatari utenti C5 con device token.");

		builder.Property(x => x.SendCopyToManager)
			.HasColumnName("send_copy_to_manager")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se inviare una copia del messaggio al manager della property.");

		builder.Property(x => x.RecipientsTotal)
			.HasColumnName("recipients_total")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Numero totale di destinatari risolti per questo invio.");

		builder.Property(x => x.DeliveredCount)
			.HasColumnName("delivered_count")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Numero di consegne riuscite, aggiornato per evento da notification_deliveries.");

		builder.Property(x => x.FailedCount)
			.HasColumnName("failed_count")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Numero di consegne fallite, aggiornato per evento da notification_deliveries.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.SentAt)
			.HasColumnName("sent_at")
			.HasComment("Quando l'invio è stato completato; decorrenza retention definizione e snapshot destinatari.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione della campagna.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete.");

		builder.HasIndex(x => x.ScheduledAt)
			.HasFilter("state = 'scheduled' AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_email_campaigns_due");

		builder.HasIndex(x => new { x.PropertyId, x.State, x.CreatedAt })
			.HasDatabaseName("ix_notification_marketing_email_campaigns_property_state");

		// Indici di supporto alle FK: nome esplicito per la convenzione del progetto (prefisso flat
		// ix_notification_*), che EF altrimenti deriva dal nome tabella canonico.
		builder.HasIndex(x => x.TemplateId)
			.HasDatabaseName("ix_notification_marketing_email_campaigns_template_id");

		builder.HasIndex(x => x.ContactListId)
			.HasDatabaseName("ix_notification_marketing_email_campaigns_contact_list_id");

		builder.HasOne(x => x.Template)
			.WithMany()
			.HasForeignKey(x => x.TemplateId)
			.HasConstraintName("fk_notification_marketing_email_campaigns_template")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.ContactList)
			.WithMany()
			.HasForeignKey(x => x.ContactListId)
			.HasConstraintName("fk_notification_marketing_email_campaigns_contact_list")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
