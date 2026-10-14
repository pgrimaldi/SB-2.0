using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingGroupMemberConfiguration : IEntityTypeConfiguration<BookingGroupMember>
{
	public void Configure(EntityTypeBuilder<BookingGroupMember> builder)
	{
		builder.ToTable("group_members", "booking", t => t.HasComment(
			"Giunzione gruppo × prenotazione: una prenotazione appartiene ad al più un gruppo."));

		builder.HasKey(x => x.Id).HasName("pk_booking_group_members");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.GroupId)
			.HasColumnName("group_id")
			.IsRequired()
			.HasComment("Gruppo di appartenenza");

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione membro del gruppo (l'intera prenotazione, non una singola riga postazione come nel legacy)");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato dal gruppo — nessuna FK cross-schema");

		builder.Property(x => x.AddedAt)
			.HasColumnName("added_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora in cui la prenotazione è stata aggiunta al gruppo");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione della riga di giunzione");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica della riga di giunzione");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: prenotazione rimossa dal gruppo");

		builder.HasIndex(x => x.ReservationId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_group_members_reservation");

		builder.HasIndex(x => x.GroupId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_group_members_group");

		builder.HasOne(x => x.Group)
			.WithMany(g => g.Members)
			.HasForeignKey(x => x.GroupId)
			.HasConstraintName("fk_booking_group_members_group")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Reservation)
			.WithMany()
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_group_members_reservation")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
