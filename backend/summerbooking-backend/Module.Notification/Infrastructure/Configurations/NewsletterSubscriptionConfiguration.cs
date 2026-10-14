using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class NewsletterSubscriptionConfiguration : IEntityTypeConfiguration<NewsletterSubscription>
{
	public void Configure(EntityTypeBuilder<NewsletterSubscription> builder)
	{
		builder.ToTable("marketing_newsletter_subscriptions", "notification", t => t.HasComment(
			"Iscrizione alla newsletter globale di piattaforma (nessuno stabilimento associato)."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_newsletter_subscriptions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile (export GDPR, pannello admin).");
		builder.HasIndex(x => x.PublicId).IsUnique()
			.HasDatabaseName("uq_notification_marketing_newsletter_subscriptions_public_id");

		builder.Property(x => x.Email)
			.HasColumnName("email")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Indirizzo iscritto, normalizzato lower-case e trim.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("notification.newsletter_status")
			.IsRequired()
			.HasDefaultValueSql("'pending_confirmation'")
			.HasComment("Stato del ciclo di vita double opt-in: in attesa di conferma, iscritto, disiscritto.");

		builder.Property(x => x.Source)
			.HasColumnName("source")
			.HasColumnType("notification.newsletter_source")
			.IsRequired()
			.HasDefaultValueSql("'public_site'")
			.HasComment("Punto di ingresso dell'iscrizione (sito pubblico, app, signup utente, creazione stabilimento, admin).");

		builder.Property(x => x.ConfirmationToken)
			.HasColumnName("confirmation_token")
			.HasMaxLength(64)
			.HasComment("Token per confermare l'iscrizione via email; azzerato dopo la conferma.");

		builder.Property(x => x.ConfirmedAt)
			.HasColumnName("confirmed_at")
			.HasComment("Data di conferma del double opt-in.");

		builder.Property(x => x.UnsubscribeToken)
			.HasColumnName("unsubscribe_token")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Token opaco stabile per il link di disiscrizione.");
		builder.HasIndex(x => x.UnsubscribeToken).IsUnique()
			.HasDatabaseName("uq_notification_marketing_newsletter_subscriptions_unsubscribe_token");

		builder.Property(x => x.ConsentNoticeVersion)
			.HasColumnName("consent_notice_version")
			.HasMaxLength(40)
			.HasComment("Versione dell'informativa privacy accettata al momento dell'iscrizione — prova del consenso art. 7 GDPR.");

		builder.Property(x => x.CustomerPublicId)
			.HasColumnName("customer_public_id")
			.HasComment("public_id di identity_access... customers (C5), copiato — nessuna FK cross-schema.");

		builder.Property(x => x.UnsubscribedAt)
			.HasColumnName("unsubscribed_at")
			.HasComment("Data della disiscrizione.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di iscrizione.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete amministrativo.");

		// Nota: l'indice UNIQUE è pensato su lower(email); Email è normalizzata a lower-case già a
		// livello di dominio, quindi un indice scalare è equivalente e non richiede un indice su espressione.
		builder.HasIndex(x => x.Email)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_marketing_newsletter_subscriptions_email");

		builder.HasIndex(x => x.ConfirmationToken)
			.HasFilter("confirmation_token IS NOT NULL")
			.IsUnique()
			.HasDatabaseName("uq_notification_marketing_newsletter_subscriptions_confirmation_token");

		builder.HasIndex(x => new { x.Status, x.CreatedAt })
			.HasDatabaseName("ix_notification_marketing_newsletter_subscriptions_status_created");
	}
}
