using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="TableService"/> su catalog.table_services.</summary>
public sealed class TableServiceConfiguration : IEntityTypeConfiguration<TableService>
{
	public void Configure(EntityTypeBuilder<TableService> builder)
	{
		builder.ToTable("table_services", "catalog", t =>
		{
			t.HasComment("Servizio tavoli (pranzo, cena): disponibilità, capienza per tavoli o posti, durata del pasto, limiti e caparra.");
			t.HasCheckConstraint("ck_catalog_table_services_seats", "capacity_mode <> 'seats' OR total_seats > 0");
			t.HasCheckConstraint("ck_catalog_table_services_booking_seats", "min_seats_per_booking IS NULL OR max_seats_per_booking IS NULL OR max_seats_per_booking >= min_seats_per_booking");
			t.HasCheckConstraint("ck_catalog_table_services_deposit", "(deposit_mode = 'none') = (deposit_type IS NULL AND deposit_value IS NULL)");
			t.HasCheckConstraint("ck_catalog_table_services_meal", "average_meal_minutes > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_table_services");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7) usato dalle API.");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_table_services_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasMaxLength(255)
			.HasComment("Nome del servizio (es. Pranzo).");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasColumnType("text")
			.HasComment("Descrizione.");

		builder.Property(x => x.IsOnlineBookable)
			.HasColumnName("is_online_bookable")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Prenotabile online.");

		builder.Property(x => x.IsHighlighted)
			.HasColumnName("is_highlighted")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Proposto al cliente insieme alla prenotazione dell'ombrellone.");

		builder.Property(x => x.CapacityMode)
			.HasColumnName("capacity_mode")
			.IsRequired()
			.HasColumnType("catalog.table_capacity_mode")
			.HasComment("Disponibilita per numero di tavoli o per posti totali.");

		builder.Property(x => x.TotalSeats)
			.HasColumnName("total_seats")
			.HasComment("Posti totali, con capacity_mode = seats.");

		builder.Property(x => x.AverageMealMinutes)
			.HasColumnName("average_meal_minutes")
			.IsRequired()
			.HasComment("Durata media del pasto in minuti: libera il tavolo per la fascia successiva.");

		builder.Property(x => x.MaxBookingsPerSlot)
			.HasColumnName("max_bookings_per_slot")
			.HasComment("Massimo di prenotazioni per fascia oraria.");

		builder.Property(x => x.MinSeatsPerBooking)
			.HasColumnName("min_seats_per_booking")
			.HasComment("Minimo di posti prenotabili in una volta.");

		builder.Property(x => x.MaxSeatsPerBooking)
			.HasColumnName("max_seats_per_booking")
			.HasComment("Massimo di posti prenotabili in una volta.");

		builder.Property(x => x.DepositMode)
			.HasColumnName("deposit_mode")
			.IsRequired()
			.HasColumnType("catalog.deposit_mode")
			.HasComment("Caparra: nessuna, totale o a persona.");

		builder.Property(x => x.DepositType)
			.HasColumnName("deposit_type")
			.HasColumnType("catalog.deposit_type")
			.HasComment("Caparra in percentuale o importo.");

		builder.Property(x => x.DepositValue)
			.HasColumnName("deposit_value")
			.HasPrecision(12, 2)
			.HasComment("Percentuale o importo della caparra.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.PropertyId, "ix_catalog_table_services_property")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_table_services_property");

		builder.HasOne<Property>()
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_table_services_property")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
