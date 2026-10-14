using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class ContactListMembershipConfiguration : IEntityTypeConfiguration<ContactListMembership>
{
	public void Configure(EntityTypeBuilder<ContactListMembership> builder)
	{
		builder.ToTable("marketing_contact_list_members", "notification", t => t.HasComment(
			"Tabella ponte: appartenenza di un contatto marketing a una lista, con storico add/remove."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_contact_list_members");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ListId)
			.HasColumnName("list_id")
			.IsRequired()
			.HasComment("Lista di appartenenza.");

		builder.Property(x => x.ContactId)
			.HasColumnName("contact_id")
			.IsRequired()
			.HasComment("Contatto membro della lista.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato dalla lista — nessuna FK cross-schema.");

		builder.Property(x => x.AddedBy)
			.HasColumnName("added_by")
			.HasColumnType("notification.list_member_source")
			.IsRequired()
			.HasDefaultValueSql("'manual'")
			.HasComment("Come il contatto è entrato nella lista (manuale, auto-iscrizione da prenotazione, import, duplicazione).");

		builder.Property(x => x.AddedAt)
			.HasColumnName("added_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Quando il contatto è stato aggiunto alla lista.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di rimozione del contatto dalla lista (soft-delete, storicizzata).");

		builder.HasIndex(x => new { x.ListId, x.ContactId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_marketing_contact_list_members_list_contact");

		builder.HasIndex(x => x.ListId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_contact_list_members_list");

		builder.HasIndex(x => x.ContactId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_contact_list_members_contact");

		builder.HasOne(x => x.List)
			.WithMany(l => l.Members)
			.HasForeignKey(x => x.ListId)
			.HasConstraintName("fk_notification_marketing_contact_list_members_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Contact)
			.WithMany()
			.HasForeignKey(x => x.ContactId)
			.HasConstraintName("fk_notification_marketing_contact_list_members_contact")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
