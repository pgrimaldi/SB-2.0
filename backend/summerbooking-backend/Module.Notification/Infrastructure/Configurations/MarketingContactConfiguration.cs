using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class MarketingContactConfiguration : IEntityTypeConfiguration<MarketingContact>
{
	public void Configure(EntityTypeBuilder<MarketingContact> builder)
	{
		builder.ToTable("marketing_contacts", "notification", t => t.HasComment(
			"Rubrica marketing per stabilimento: un contatto identificato dall'email, con stato di consenso, fonte e token di disiscrizione."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_contacts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, usato in API/URL e nell'export GDPR.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_notification_marketing_contacts_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Email)
			.HasColumnName("email")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Indirizzo email del contatto (chiave naturale), normalizzato lower-case + trim.");
		builder.HasIndex(x => new { x.PropertyId, x.Email })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_marketing_contacts_property_email");

		builder.Property(x => x.FullName)
			.HasColumnName("full_name")
			.HasMaxLength(160)
			.HasComment("Nome del contatto, denormalizzato per la personalizzazione senza join a C5.");

		builder.Property(x => x.Phone)
			.HasColumnName("phone")
			.HasMaxLength(32)
			.HasComment("Telefono del contatto, denormalizzato per l'invio WhatsApp/SMS.");

		builder.Property(x => x.CustomerPublicId)
			.HasColumnName("customer_public_id")
			.HasComment("public_id di identity_customer.customers, copiato — nessuna FK cross-schema. Valorizzato quando il contatto è anche in anagrafica.");

		builder.Property(x => x.ConsentStatus)
			.HasColumnName("consent_status")
			.HasColumnType("notification.marketing_consent_status")
			.HasDefaultValueSql("'unknown'")
			.IsRequired()
			.HasComment("Stato del consenso all'invio di comunicazioni marketing.");

		builder.Property(x => x.ConsentSource)
			.HasColumnName("consent_source")
			.HasColumnType("notification.consent_source")
			.HasDefaultValueSql("'unknown'")
			.IsRequired()
			.HasComment("Come il contatto/consenso è stato acquisito.");

		builder.Property(x => x.ConsentAt)
			.HasColumnName("consent_at")
			.HasComment("Data/ora del consenso, o della revoca se withdrawn; decorrenza della retention marketing (24 mesi).");

		builder.Property(x => x.ConsentNoticeVersion)
			.HasColumnName("consent_notice_version")
			.HasMaxLength(40)
			.HasComment("Versione dell'informativa privacy accettata all'atto del consenso, prova ex art. 7 GDPR insieme a fonte e data.");

		builder.Property(x => x.UnsubscribeToken)
			.HasColumnName("unsubscribe_token")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Token opaco stabile per il link di disiscrizione nelle email marketing.");
		builder.HasIndex(x => x.UnsubscribeToken)
			.IsUnique()
			.HasDatabaseName("uq_notification_marketing_contacts_unsubscribe_token");

		builder.Property(x => x.IsSeedFromBooking)
			.HasColumnName("is_seed_from_booking")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("True se il contatto è nato dall'auto-iscrizione alla lista di default (bookings.isMarketingCampaign).");

		builder.Property(x => x.LastContactedAt)
			.HasColumnName("last_contacted_at")
			.HasComment("Ultimo invio marketing riuscito verso questo contatto; guida la retention 24 mesi dall'ultimo contatto.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione del contatto.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: retention 24 mesi senza attività (la prova di revoca resta a vita in notification_marketing_suppressions).");

		builder.HasIndex(x => x.CustomerPublicId)
			.HasFilter("customer_public_id IS NOT NULL")
			.HasDatabaseName("ix_notification_marketing_contacts_customer");

		builder.HasIndex(x => new { x.PropertyId, x.ConsentStatus })
			.HasDatabaseName("ix_notification_marketing_contacts_consent");
	}
}
