using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="TableServiceAvailability"/> su catalog.table_service_availabilities.</summary>
public sealed class TableServiceAvailabilityConfiguration : IEntityTypeConfiguration<TableServiceAvailability>
{
	public void Configure(EntityTypeBuilder<TableServiceAvailability> builder)
	{
		builder.ToTable("table_service_availabilities", "catalog", t =>
		{
			t.HasComment("Periodi di disponibilità di un servizio tavoli.");
			t.HasCheckConstraint("ck_catalog_table_service_availabilities_range", "ends_on >= starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_table_service_availabilities");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.TableServiceId)
			.HasColumnName("table_service_id")
			.IsRequired()
			.HasComment("Servizio tavoli.");

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.IsRequired()
			.HasComment("Primo giorno.");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.IsRequired()
			.HasComment("Ultimo giorno.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.TableServiceId, "ix_catalog_table_service_availabilities_service")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_table_service_availabilities_service");

		builder.HasOne<TableService>()
			.WithMany(p => p.Availabilities)
			.HasForeignKey(x => x.TableServiceId)
			.HasConstraintName("fk_catalog_table_service_availabilities_service")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
