using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

/// <summary>
/// L'UNIQUE (property_id, address, channel) previsto richiede coalesce(property_id, '0000...')
/// perche' property_id e' nullable (soppressione globale) e Postgres tratta NULL <> NULL — non
/// esprimibile con HasIndex su colonne scalari. L'indice sotto e' un fallback scalare che NON
/// impedisce doppioni fra righe globali; va sostituito nella migration generata con
/// migrationBuilder.Sql(...) sull'espressione coalesce.
/// </summary>
public sealed class MarketingSuppressionConfiguration : IEntityTypeConfiguration<MarketingSuppression>
{
	public void Configure(EntityTypeBuilder<MarketingSuppression> builder)
	{
		builder.ToTable("marketing_suppressions", "notification", t => t.HasComment(
			"Lista di soppressione (opt-out) marketing: indirizzi da non ricontattare, prova della revoca del consenso."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_suppressions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties per cui vale la soppressione; NULL = globale su tutta la piattaforma.");

		builder.Property(x => x.Address)
			.HasColumnName("address")
			.HasMaxLength(320)
			.IsRequired()
			.HasComment("Email (lower-case) o numero E.164 da sopprimere.");

		builder.Property(x => x.Channel)
			.HasColumnName("channel")
			.HasColumnType("notification.channel_or_all")
			.IsRequired()
			.HasDefaultValueSql("'all'")
			.HasComment("Canale a cui si applica l'opt-out (email, whatsapp, sms, o tutti).");

		builder.Property(x => x.Reason)
			.HasColumnName("reason")
			.HasColumnType("notification.suppression_reason")
			.IsRequired()
			.HasDefaultValueSql("'unsubscribe_link'")
			.HasComment("Motivo della soppressione (click unsubscribe, richiesta manuale, reclamo spam, hard bounce, cancellazione GDPR, admin).");

		builder.Property(x => x.SourceDeliveryPublicId)
			.HasColumnName("source_delivery_public_id")
			.HasComment("public_id della notification_deliveries da cui deriva la soppressione (link cliccato, bounce) — copiato, nessuna FK: deve sopravvivere alla purge della consegna.");

		builder.Property(x => x.ContactPublicId)
			.HasColumnName("contact_public_id")
			.HasComment("public_id del contatto marketing al momento della soppressione — copiato, nessuna FK: deve sopravvivere alla cancellazione del contatto per retention.");

		builder.Property(x => x.Note)
			.HasColumnName("note")
			.HasMaxLength(500)
			.HasComment("Testo libero per i motivi manual_request / admin.");

		builder.Property(x => x.SuppressedAt)
			.HasColumnName("suppressed_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Quando è scattata la soppressione.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità con le altre tabelle; di fatto sempre NULL — la lista di soppressione non ha retention.");

		builder.HasIndex(x => new { x.PropertyId, x.Address, x.Channel })
			.IsUnique()
			.HasDatabaseName("uq_notification_marketing_suppressions_scope_address_channel");

		builder.HasIndex(x => x.Address)
			.HasDatabaseName("ix_notification_marketing_suppressions_address");
	}
}
