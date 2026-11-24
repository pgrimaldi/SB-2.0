using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingTableLineConfiguration : IEntityTypeConfiguration<BookingTableLine>
{
	public void Configure(EntityTypeBuilder<BookingTableLine> builder)
	{
		builder.ToTable("table_lines", "booking", t =>
		{
			t.HasComment("Tavolo prenotato: servizio tavoli, giorno, fascia, coperti e tavoli assegnati.");
			t.HasCheckConstraint("ck_booking_table_lines_covers", "covers > 0");
			t.HasCheckConstraint("ck_booking_table_lines_slot", "slot_end > slot_start");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_table_lines");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId).HasColumnName("public_id").ValueGeneratedNever().IsRequired()
			.HasDefaultValueSql("gen_random_uuid()").HasComment("Id pubblico della riga nelle API.");
		builder.Property(x => x.ReservationId).HasColumnName("reservation_id").IsRequired().HasComment("Prenotazione a cui appartiene il tavolo.");
		builder.Property(x => x.PropertyId).HasColumnName("property_id").IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema");
		builder.Property(x => x.TableServiceId).HasColumnName("table_service_id").IsRequired()
			.HasComment("public_id di catalog.table_services (pranzo, cena), copiato — nessuna FK cross-schema");
		builder.Property(x => x.TableServiceNameSnapshot).HasColumnName("table_service_name_snapshot").HasMaxLength(255).IsRequired()
			.HasComment("Nome del servizio tavoli al momento della prenotazione.");
		builder.Property(x => x.ServiceDate).HasColumnName("service_date").HasColumnType("date").IsRequired().HasComment("Giorno del pasto.");
		builder.Property(x => x.SlotStart).HasColumnName("slot_start").HasColumnType("time").IsRequired().HasComment("Inizio della fascia.");
		builder.Property(x => x.SlotEnd).HasColumnName("slot_end").HasColumnType("time").IsRequired().HasComment("Fine della fascia.");
		builder.Property(x => x.Covers).HasColumnName("covers").IsRequired().HasComment("Coperti.");
		builder.Property(x => x.TableSeats).HasColumnName("table_seats").HasColumnType("smallint[]").HasDefaultValueSql("'{}'").IsRequired()
			.HasComment("Posti di ciascun tavolo assegnato; vuoto con la capienza a posti o senza tavolo.");
		builder.Property(x => x.DepositDue).HasColumnName("deposit_due").HasColumnType("numeric(12,2)").HasDefaultValue(0m).IsRequired()
			.HasComment("Caparra richiesta dal servizio tavoli (si incassa con Payment).");
		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired().HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired().HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_booking_table_lines_public_id");
		builder.HasIndex(x => new { x.PropertyId, x.TableServiceId, x.ServiceDate })
			.HasFilter("deleted_at IS NULL").HasDatabaseName("ix_booking_table_lines_service_day");

		builder.HasOne(x => x.Reservation).WithMany(r => r.TableLines).HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_table_lines_reservation").OnDelete(DeleteBehavior.Cascade);
	}
}
