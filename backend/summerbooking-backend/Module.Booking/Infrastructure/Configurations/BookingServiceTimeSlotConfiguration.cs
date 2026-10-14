using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Booking.Domain.Entities;

namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingServiceTimeSlotConfiguration : IEntityTypeConfiguration<BookingServiceTimeSlot>
{
	public void Configure(EntityTypeBuilder<BookingServiceTimeSlot> builder)
	{
		builder.ToTable("service_time_slots", "booking", t =>
		{
			t.HasComment("Slot orario prenotato per una riga servizio hourly: giorno, fascia e posti.");
			t.HasCheckConstraint("ck_booking_service_time_slots_range", "slot_end > slot_start");
			t.HasCheckConstraint("ck_booking_service_time_slots_seats", "seats > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_booking_service_time_slots");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ServiceLineId)
			.HasColumnName("service_line_id")
			.IsRequired()
			.HasComment("Riga servizio orario a cui lo slot appartiene.");

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione, denormalizzata dalla riga servizio per query dirette.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.TimeSlotId)
			.HasColumnName("time_slot_id")
			.HasComment("public_id di catalog.service_sale_item_time_slots, copiato — NULL per slot manuale non a catalogo.");

		builder.Property(x => x.SlotStart)
			.HasColumnName("slot_start")
			.HasColumnType("time")
			.IsRequired()
			.HasComment("Orario di inizio dello slot, congelato al momento della prenotazione.");

		builder.Property(x => x.SlotEnd)
			.HasColumnName("slot_end")
			.HasColumnType("time")
			.IsRequired()
			.HasComment("Orario di fine dello slot, congelato al momento della prenotazione.");

		builder.Property(x => x.SlotLabelSnapshot)
			.HasColumnName("slot_label_snapshot")
			.HasMaxLength(40)
			.HasComment("Etichetta dello slot congelata (es. \"9-11\").");

		builder.Property(x => x.ServiceDate)
			.HasColumnName("service_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Giorno dello slot.");

		builder.Property(x => x.Seats)
			.HasColumnName("seats")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)1)
			.HasComment("Posti/unità prenotati per lo slot; alimenta il consumo di booking_resource_consumptions.");

		builder.Property(x => x.SlotPrice)
			.HasColumnName("slot_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo dello slot congelato alla conferma, se ha tariffa dedicata; altrimenti quello del servizio.");

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


		builder.HasIndex(x => x.ServiceLineId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_time_slots_service_line");

		builder.HasIndex(x => new { x.PropertyId, x.TimeSlotId, x.ServiceDate })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_service_time_slots_property_slot_date");

		builder.HasOne(x => x.ServiceLine)
			.WithMany(l => l.TimeSlots)
			.HasForeignKey(x => x.ServiceLineId)
			.HasConstraintName("fk_booking_service_time_slots_service_line")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Reservation)
			.WithMany()
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_service_time_slots_reservation")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
