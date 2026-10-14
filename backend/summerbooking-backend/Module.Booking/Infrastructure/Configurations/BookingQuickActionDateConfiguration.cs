using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingQuickActionDateConfiguration : IEntityTypeConfiguration<BookingQuickActionDate>
{
	public void Configure(EntityTypeBuilder<BookingQuickActionDate> builder)
	{
		builder.ToTable("quick_action_dates", "booking", t => t.HasComment(
			"Giorno marcato dall'operatore per un'azione rapida del planner su questa prenotazione."));

		builder.HasKey(x => x.Id).HasName("pk_booking_quick_action_dates");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui la data marcata è associata.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema.");

		builder.Property(x => x.ActionDate)
			.HasColumnName("action_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Giorno marcato per un'azione rapida del planner.");

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


		builder.HasIndex(x => new { x.ReservationId, x.ActionDate })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_quick_action_dates_reservation_date");

		builder.HasIndex(x => new { x.PropertyId, x.ActionDate })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_quick_action_dates_property_date");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.QuickActionDates)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_quick_action_dates_reservation")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
