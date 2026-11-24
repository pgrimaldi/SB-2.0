using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingEntranceConfiguration : IEntityTypeConfiguration<BookingEntrance>
{
	public void Configure(EntityTypeBuilder<BookingEntrance> builder)
	{
		builder.ToTable("entrances", "booking", t => t.HasComment(
			"«Segnala ingresso»: il cliente è entrato su questa postazione in questo giorno. Una riga per riga di prenotazione e giorno."));

		builder.HasKey(x => x.Id).HasName("pk_booking_entrances");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; discriminatore tenant.");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.IsRequired()
			.HasComment("Riga (postazione) della prenotazione su cui è segnato l'ingresso.");

		builder.Property(x => x.EntryDate)
			.HasColumnName("entry_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Giorno dell'ingresso (ora italiana): il giorno dopo l'ingresso non risulta più segnato.");

		builder.Property(x => x.EnteredAt)
			.HasColumnName("entered_at")
			.IsRequired()
			.HasComment("Quando l'operatore ha segnato l'ingresso.");

		builder.Property(x => x.OperatorUserId)
			.HasColumnName("operator_user_id")
			.HasComment("public_id di identity.access_users, copiato dal token — chi ha segnato l'ingresso.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");

		builder.HasOne(x => x.BookingLine)
			.WithMany(l => l.Entrances)
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_entrances_line")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.BookingLineId, x.EntryDate })
			.IsUnique()
			.HasDatabaseName("uq_booking_entrances_line_date");

		builder.HasIndex(x => new { x.PropertyId, x.EntryDate })
			.HasDatabaseName("ix_booking_entrances_property_date");
	}
}
