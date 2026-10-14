using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class InAppMessageReadConfiguration : IEntityTypeConfiguration<InAppMessageRead>
{
	public void Configure(EntityTypeBuilder<InAppMessageRead> builder)
	{
		builder.ToTable("inapp_message_reads", "notification", t => t.HasComment(
			"Stato di lettura per (messaggio × utente): l'assenza di riga significa non letta."));

		builder.HasKey(x => x.Id).HasName("pk_notification_inapp_message_reads");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.MessageId)
			.HasColumnName("message_id")
			.IsRequired()
			.HasComment("Notifica a cui lo stato di lettura si riferisce.");

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema. Operatore che ha letto/scartato.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato dal messaggio per purge e scoping multi-tenant.");

		builder.Property(x => x.ReadAt)
			.HasColumnName("read_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Istante in cui l'utente ha aperto/visto la notifica.");

		builder.Property(x => x.DismissedAt)
			.HasColumnName("dismissed_at")
			.HasComment("Istante in cui l'utente ha rimosso la notifica dal proprio elenco, senza cancellarla per gli altri.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: le righe sono normalmente rimosse in cascata col messaggio, non soft-deleted a mano.");

		builder.HasOne(x => x.Message)
			.WithMany(m => m.Reads)
			.HasForeignKey(x => x.MessageId)
			.HasConstraintName("fk_notification_inapp_message_reads_message")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.MessageId, x.UserId })
			.IsUnique()
			.HasDatabaseName("uq_notification_inapp_message_reads_message_user");

		builder.HasIndex(x => new { x.UserId, x.ReadAt })
			.HasDatabaseName("ix_notification_inapp_message_reads_user_recent");
	}
}
