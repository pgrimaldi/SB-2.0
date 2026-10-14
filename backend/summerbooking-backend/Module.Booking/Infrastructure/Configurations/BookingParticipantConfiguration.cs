using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingParticipantConfiguration : IEntityTypeConfiguration<BookingParticipant>
{
	public void Configure(EntityTypeBuilder<BookingParticipant> builder)
	{
		builder.ToTable("participants", "booking", t => t.HasComment(
			"Concessione di visibilità: account consumer che ha agganciato la prenotazione col reservation_code."));

		builder.HasKey(x => x.Id).HasName("pk_booking_participants");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione (testata) resa visibile al partecipante.");

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("public_id di identity.access_users, copiato — account consumer che ha aperto la prenotazione col codice.");

		builder.Property(x => x.Role)
			.HasColumnName("role")
			.HasColumnType("booking.participant_role")
			.IsRequired()
			.HasDefaultValueSql("'viewer'")
			.HasComment("Ruolo del partecipante rispetto alla prenotazione.");

		builder.Property(x => x.LinkedAt)
			.HasColumnName("linked_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Quando l'aggancio è avvenuto.");

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


		builder.HasIndex(x => new { x.ReservationId, x.UserId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_participants_reservation_user");

		builder.HasIndex(x => x.UserId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_participants_user");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.Participants)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_participants_reservation")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
