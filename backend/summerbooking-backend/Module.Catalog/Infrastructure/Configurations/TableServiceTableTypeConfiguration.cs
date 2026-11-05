using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="TableServiceTableType"/> su catalog.table_service_table_types.</summary>
public sealed class TableServiceTableTypeConfiguration : IEntityTypeConfiguration<TableServiceTableType>
{
	public void Configure(EntityTypeBuilder<TableServiceTableType> builder)
	{
		builder.ToTable("table_service_table_types", "catalog", t =>
		{
			t.HasComment("Tavoli di un servizio tavoli con lo stesso numero di posti (es. 5 tavoli da 4).");
			t.HasCheckConstraint("ck_catalog_table_service_table_types_values", "seats > 0 AND count > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_table_service_table_types");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.TableServiceId)
			.HasColumnName("table_service_id")
			.IsRequired()
			.HasComment("Servizio tavoli.");

		builder.Property(x => x.Seats)
			.HasColumnName("seats")
			.IsRequired()
			.HasComment("Posti per tavolo.");

		builder.Property(x => x.Count)
			.HasColumnName("count")
			.IsRequired()
			.HasComment("Numero di tavoli.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.TableServiceId, x.Seats }, "uq_catalog_table_service_table_types_seats")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_table_service_table_types_seats");

		builder.HasOne<TableService>()
			.WithMany(p => p.TableTypes)
			.HasForeignKey(x => x.TableServiceId)
			.HasConstraintName("fk_catalog_table_service_table_types_service")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
