using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingGroupConfiguration : IEntityTypeConfiguration<BookingGroup>
{
	public void Configure(EntityTypeBuilder<BookingGroup> builder)
	{
		builder.ToTable("groups", "booking", t => t.HasComment(
			"Raggruppamento manuale di prenotazioni distinte sotto un'etichetta scelta dall'operatore " +
			"(comitiva, famiglia su più ombrelloni); non è la prenotazione multipla."));

		builder.HasKey(x => x.Id).HasName("pk_booking_groups");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties (stabilimento proprietario del gruppo), copiato — nessuna FK cross-schema");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Nome del gruppo mostrato in UI");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users (operatore che ha creato il gruppo, dal token), copiato — nessuna FK cross-schema");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione del gruppo");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica del gruppo");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete del gruppo (cancellando il gruppo si soft-eliminano anche i membri)");

		// indice unique effettivo su (property_id, lower(name)) — espressione non esprimibile in Fluent
		// API pura, rifinita in migration con SQL raw
		builder.HasIndex(x => new { x.PropertyId, x.Name })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_groups_property_name");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_groups_property");

		builder.HasMany(x => x.Members)
			.WithOne(m => m.Group)
			.HasForeignKey(m => m.GroupId)
			.HasConstraintName("fk_booking_group_members_group")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
