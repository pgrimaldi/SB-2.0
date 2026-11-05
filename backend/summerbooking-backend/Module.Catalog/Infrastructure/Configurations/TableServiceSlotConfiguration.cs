using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="TableServiceSlot"/> su catalog.table_service_slots.</summary>
public sealed class TableServiceSlotConfiguration : IEntityTypeConfiguration<TableServiceSlot>
{
	public void Configure(EntityTypeBuilder<TableServiceSlot> builder)
	{
		builder.ToTable("table_service_slots", "catalog", t =>
		{
			t.HasComment("Fasce orarie prenotabili di un periodo del servizio tavoli; senza giorno valgono per tutti i giorni.");
			t.HasCheckConstraint("ck_catalog_table_service_slots_range", "time_to > time_from");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_table_service_slots");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.AvailabilityId)
			.HasColumnName("availability_id")
			.IsRequired()
			.HasComment("Periodo di disponibilità.");

		builder.Property(x => x.Weekday)
			.HasColumnName("weekday")
			.HasColumnType("catalog.weekday")
			.HasComment("Giorno; NULL = tutti i giorni del periodo.");

		builder.Property(x => x.From)
			.HasColumnName("time_from")
			.IsRequired()
			.HasColumnType("time")
			.HasComment("Inizio della fascia.");

		builder.Property(x => x.To)
			.HasColumnName("time_to")
			.IsRequired()
			.HasColumnType("time")
			.HasComment("Fine della fascia.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.AvailabilityId, "ix_catalog_table_service_slots_availability")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_table_service_slots_availability");

		builder.HasOne<TableServiceAvailability>()
			.WithMany(p => p.Slots)
			.HasForeignKey(x => x.AvailabilityId)
			.HasConstraintName("fk_catalog_table_service_slots_availability")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
