using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingGuestConfiguration : IEntityTypeConfiguration<BookingGuest>
{
	public void Configure(EntityTypeBuilder<BookingGuest> builder)
	{
		builder.ToTable("guests", "booking", t => t.HasComment(
			"Ospite di una prenotazione: persona presente ma non intestataria, senza addebito/comunicazioni a suo carico."));

		builder.HasKey(x => x.Id).HasName("pk_booking_guests");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui l'ospite appartiene.");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.HasComment("public_id di identity.customer_customers, copiato — anagrafica cliente dell'ospite; NULL per ospite senza record cliente (solo nome).");

		builder.Property(x => x.FullNameSnapshot)
			.HasColumnName("full_name_snapshot")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome dell'ospite congelato, per elenco presenze/check-in anche se l'anagrafica cambia in seguito.");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Postazione a cui l'ospite è associato (prenotazioni multi-postazione); NULL = ospite della prenotazione nel complesso.");

		builder.Property(x => x.CheckedInAt)
			.HasColumnName("checked_in_at")
			.HasComment("Check-in per nome dell'ospite, opzionale.");

		// bug reale osservato il 2026-09-16: created_at/updated_at non avevano default now() a
		// schema (nessuna convenzione EF globale lo applica, verificato in ModuleDbContextConventions
		// - solo converter UTC e query filter soft-delete) - un ETL che omette queste colonne
		// dall'INSERT (comportamento normale, per lasciare scattare il default) falliva NOT NULL.
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


		builder.HasIndex(x => new { x.ReservationId, x.CustomerId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_guests_reservation_customer");

		builder.HasIndex(x => x.CustomerId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_guests_customer");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.Guests)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_guests_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany()
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_guests_line")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
