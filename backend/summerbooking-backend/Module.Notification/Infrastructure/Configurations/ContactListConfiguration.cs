using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class ContactListConfiguration : IEntityTypeConfiguration<ContactList>
{
	public void Configure(EntityTypeBuilder<ContactList> builder)
	{
		builder.ToTable("marketing_contact_lists", "notification", t => t.HasComment(
			"Segmento di clientela di uno stabilimento, target di campagne e automazioni marketing."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_contact_lists");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile (API/URL).");
		builder.HasIndex(x => x.PublicId).IsUnique()
			.HasDatabaseName("uq_notification_marketing_contact_lists_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome della lista (\"Marketing\" per la default, o nome scelto dall'utente).");

		builder.Property(x => x.IsDefault)
			.HasColumnName("is_default")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Lista predefinita della property, bersaglio dell'auto-iscrizione da prenotazione.");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.HasColumnType("notification.contact_list_origin")
			.IsRequired()
			.HasDefaultValueSql("'manual'")
			.HasComment("Come è stata creata la lista: manuale, seed di default, o duplicazione.");

		builder.Property(x => x.MemberCount)
			.HasColumnName("member_count")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Numero di membri vivi, denormalizzato e aggiornato per evento; riconciliato da job periodico.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione della lista.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete; NULL se la lista è attiva.");

		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => x.PropertyId, "ix_notification_marketing_contact_lists_property")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_contact_lists_property");

		builder.HasIndex(x => x.PropertyId, "uq_notification_marketing_contact_lists_default")
			.IsUnique()
			.HasFilter("is_default AND deleted_at IS NULL")
			.HasDatabaseName("uq_notification_marketing_contact_lists_default");
	}
}
