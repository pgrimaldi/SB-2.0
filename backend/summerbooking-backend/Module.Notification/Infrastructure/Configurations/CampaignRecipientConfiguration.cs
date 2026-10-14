using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class CampaignRecipientConfiguration : IEntityTypeConfiguration<CampaignRecipient>
{
	public void Configure(EntityTypeBuilder<CampaignRecipient> builder)
	{
		builder.ToTable("marketing_campaign_recipients", "notification", t => t.HasComment(
			"Destinatario di un invio marketing (campagna o automazione): snapshot al momento dell'invio, con esito per indirizzo."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_campaign_recipients");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ParentKind)
			.HasColumnName("parent_kind")
			.HasColumnType("notification.marketing_parent_kind")
			.IsRequired()
			.HasComment("Se il destinatario appartiene a una campagna o a un'automazione; tipizza parent_id.");

		builder.Property(x => x.ParentId)
			.HasColumnName("parent_id")
			.IsRequired()
			.HasComment("Invio (campagna o automazione) a cui la riga appartiene — integrità referenziale condizionale applicata da trigger DB, nessuna FK EF (vedi nota sopra).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato dal parent — nessuna FK cross-schema.");

		builder.Property(x => x.ContactId)
			.HasColumnName("contact_id")
			.HasComment("Contatto marketing corrispondente, se risolvibile (destinatario da lista email grezza altrimenti NULL).");

		builder.Property(x => x.EmailSnapshot)
			.HasColumnName("email_snapshot")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Indirizzo email destinatario, congelato al momento dell'invio.");

		builder.Property(x => x.FullNameSnapshot)
			.HasColumnName("full_name_snapshot")
			.HasMaxLength(160)
			.HasComment("Nome del destinatario all'invio, per personalizzazione e audit.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("notification.recipient_status")
			.IsRequired()
			.HasDefaultValueSql("'pending'")
			.HasComment("Esito dell'invio a questo destinatario, allineato allo status della Delivery collegata.");

		builder.Property(x => x.DeliveryPublicId)
			.HasColumnName("delivery_public_id")
			.HasComment("public_id della notification_deliveries che ha eseguito l'invio a questo destinatario — copiato, nessuna FK (NULL se status = skipped).");

		builder.Property(x => x.SkipReason)
			.HasColumnName("skip_reason")
			.HasColumnType("notification.recipient_skip_reason")
			.HasComment("Perché il destinatario è stato saltato (consenso mancante, soppresso, indirizzo vuoto/non valido, duplicato).");

		builder.Property(x => x.SentAt)
			.HasColumnName("sent_at")
			.HasComment("Quando il messaggio è stato spedito a questo destinatario; decorrenza della retention a 6 mesi dello snapshot.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità; la retention a 6 mesi esegue una purge fisica (hard-delete), non un soft-delete.");

		builder.HasIndex(x => new { x.ParentKind, x.ParentId, x.Status })
			.HasDatabaseName("ix_notification_marketing_campaign_recipients_parent");

		builder.HasIndex(x => x.ContactId)
			.HasFilter("contact_id IS NOT NULL")
			.HasDatabaseName("ix_notification_marketing_campaign_recipients_contact");

		builder.HasIndex(x => new { x.ParentKind, x.ParentId, x.EmailSnapshot })
			.IsUnique()
			.HasDatabaseName("uq_notification_marketing_campaign_recipients_dedup");

		builder.HasOne(x => x.Contact)
			.WithMany()
			.HasForeignKey(x => x.ContactId)
			.HasConstraintName("fk_notification_marketing_campaign_recipients_contact")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
