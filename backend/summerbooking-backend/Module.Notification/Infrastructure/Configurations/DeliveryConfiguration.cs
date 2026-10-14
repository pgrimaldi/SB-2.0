using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
	public void Configure(EntityTypeBuilder<Delivery> builder)
	{
		builder.ToTable("deliveries", "notification", t => t.HasComment(
			"Outbox + audit unico per destinatario: una riga = un messaggio verso un singolo destinatario su un singolo canale."));

		builder.HasKey(x => x.Id).HasName("pk_notification_deliveries");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile usato nel gestionale e nell'export GDPR.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_notification_deliveries_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. NULL solo per invii di piattaforma non legati a uno stabilimento.");

		builder.Property(x => x.Channel)
			.HasColumnName("channel")
			.HasColumnType("notification.channel")
			.HasDefaultValueSql("'email'")
			.IsRequired()
			.HasComment("Mezzo di consegna (email/whatsapp/sms/push/in_app); un invio è su un solo canale.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("notification.delivery_kind")
			.HasDefaultValueSql("'other'")
			.IsRequired()
			.HasComment("Scopo funzionale del messaggio: determina template, priorità, retention e regole anti-spam.");

		builder.Property(x => x.SubjectType)
			.HasColumnName("subject_type")
			.HasColumnType("notification.subject_type")
			.HasDefaultValueSql("'none'")
			.IsRequired()
			.HasComment("Tipo dell'entità che ha originato l'invio.");

		builder.Property(x => x.SubjectPublicId)
			.HasColumnName("subject_public_id")
			.HasComment("Id pubblico dell'entità di origine, copiato per valore — nessuna FK cross-modulo.");

		builder.Property(x => x.RecipientType)
			.HasColumnName("recipient_type")
			.HasColumnType("notification.recipient_type")
			.HasDefaultValueSql("'customer'")
			.IsRequired()
			.HasComment("Categoria del destinatario (cliente, utente gestionale, contatto stabilimento, partner, admin, indirizzo libero).");

		builder.Property(x => x.RecipientCustomerPublicId)
			.HasColumnName("recipient_customer_public_id")
			.HasComment("public_id di identity_customer.customers, copiato — nessuna FK cross-schema. Usato per export/cancellazione GDPR.");

		builder.Property(x => x.RecipientUserPublicId)
			.HasColumnName("recipient_user_public_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.RecipientAddressSnapshot)
			.HasColumnName("recipient_address_snapshot")
			.HasMaxLength(320)
			.HasComment("Indirizzo di consegna congelato all'invio: email, numero E.164 o token push abbreviato.");

		builder.Property(x => x.RecipientNameSnapshot)
			.HasColumnName("recipient_name_snapshot")
			.HasMaxLength(160)
			.HasComment("Nome del destinatario al momento dell'invio, per audit e reinvio.");

		builder.Property(x => x.FromAddressSnapshot)
			.HasColumnName("from_address_snapshot")
			.HasMaxLength(320)
			.HasComment("Mittente effettivo usato (canale email della property o noreply@summerbooking.it).");

		builder.Property(x => x.TemplateId)
			.HasColumnName("template_id")
			.HasComment("Template di catalogo usato per il rendering, quando presente.");

		builder.Property(x => x.SubjectSnapshot)
			.HasColumnName("subject_snapshot")
			.HasMaxLength(255)
			.HasComment("Oggetto renderizzato effettivamente spedito (email/push title).");

		builder.Property(x => x.RenderedBody)
			.HasColumnName("rendered_body")
			.HasColumnType("text")
			.HasComment("Corpo renderizzato consegnato, tenuto per audit/ricerca; troncato a NULL dopo 90 giorni dal job TruncateEmailLogBodies.");

		builder.Property(x => x.HasAttachments)
			.HasColumnName("has_attachments")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("True se il messaggio consegnato aveva allegati; gli allegati appartengono all'entità di origine, non a questa riga.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("notification.delivery_status")
			.HasDefaultValueSql("'queued'")
			.IsRequired()
			.HasComment("Stato del ciclo di consegna: queued → sending → sent → delivered | failed | bounced | skipped.");

		builder.Property(x => x.AttemptCount)
			.HasColumnName("attempt_count")
			.HasColumnType("smallint")
			.HasDefaultValue((short)0)
			.IsRequired()
			.HasComment("Numero di tentativi di invio già eseguiti.");

		builder.Property(x => x.MaxAttempts)
			.HasColumnName("max_attempts")
			.HasColumnType("smallint")
			.HasDefaultValue((short)5)
			.IsRequired()
			.HasComment("Numero massimo di tentativi previsti per questo kind+channel.");

		builder.Property(x => x.NextAttemptAt)
			.HasColumnName("next_attempt_at")
			.HasDefaultValueSql("now()")
			.HasComment("Istante del prossimo tentativo del worker, calcolato con backoff esponenziale + jitter; NULL quando lo stato è terminale.");

		builder.Property(x => x.Provider)
			.HasColumnName("provider")
			.HasColumnType("notification.delivery_provider")
			.HasDefaultValueSql("'smtp'")
			.IsRequired()
			.HasComment("Sistema esterno usato per la consegna.");

		builder.Property(x => x.ProviderMessageId)
			.HasColumnName("provider_message_id")
			.HasMaxLength(255)
			.HasComment("Id restituito dal provider (SES MessageId, Twilio SID, FCM message name), per correlare i webhook di stato/bounce.");

		builder.Property(x => x.IdempotencyKey)
			.HasColumnName("idempotency_key")
			.HasMaxLength(200)
			.HasComment("Chiave calcolata da (subject_type, subject_public_id, kind, channel, recipient) per evitare invii duplicati.");

		builder.Property(x => x.FailureReason)
			.HasColumnName("failure_reason")
			.HasColumnType("text")
			.HasComment("Dettaglio testuale dell'errore terminale (failed/bounced).");

		builder.Property(x => x.SkipReason)
			.HasColumnName("skip_reason")
			.HasColumnType("notification.skip_reason")
			.HasComment("Motivo dello stato skipped (opt-out, soppressione, indirizzo mancante, ambiente non-prod, consenso non verificato).");

		builder.Property(x => x.QueuedAt)
			.HasColumnName("queued_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Istante di creazione della riga in coda (transazione del comando di origine).");

		builder.Property(x => x.SentAt)
			.HasColumnName("sent_at")
			.HasComment("Istante di accettazione da parte del provider; decorrenza della retention del corpo (90 giorni).");

		builder.Property(x => x.DeliveredAt)
			.HasColumnName("delivered_at")
			.HasComment("Istante di conferma di recapito, quando il provider la fornisce.");

		builder.Property(x => x.FailedAt)
			.HasColumnName("failed_at")
			.HasComment("Istante di transizione allo stato terminale failed/bounced.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data dell'ultima transizione di stato.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: tabella di registro, le righe non si cancellano a mano — le rimuove la retention (12 mesi).");

		builder.HasOne(x => x.Template)
			.WithMany()
			.HasForeignKey(x => x.TemplateId)
			.HasConstraintName("fk_notification_deliveries_template")
			.OnDelete(DeleteBehavior.SetNull);

		// Indice di supporto alla FK: nome esplicito perche' la convenzione del progetto vuole il
		// prefisso flat (ix_notification_*), che EF altrimenti deriva dal nome tabella canonico.
		builder.HasIndex(x => x.TemplateId)
			.HasDatabaseName("ix_notification_deliveries_template_id");

		builder.HasIndex(x => x.NextAttemptAt)
			.HasFilter("status IN ('queued','sending')")
			.HasDatabaseName("ix_notification_deliveries_pending");

		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasDatabaseName("ix_notification_deliveries_property_created");

		builder.HasIndex(x => new { x.SubjectType, x.SubjectPublicId })
			.HasDatabaseName("ix_notification_deliveries_subject");

		builder.HasIndex(x => new { x.RecipientCustomerPublicId, x.CreatedAt })
			.HasFilter("recipient_customer_public_id IS NOT NULL")
			.HasDatabaseName("ix_notification_deliveries_recipient_customer");

		builder.HasIndex(x => new { x.SubjectType, x.SubjectPublicId, x.Status })
			.HasFilter("subject_type IN ('marketing_campaign','marketing_automation')")
			.HasDatabaseName("ix_notification_deliveries_campaign");

		builder.HasIndex(x => x.IdempotencyKey)
			.IsUnique()
			.HasFilter("idempotency_key IS NOT NULL")
			.HasDatabaseName("ix_notification_deliveries_dedup");
	}
}
