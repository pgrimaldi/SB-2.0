using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ServiceSaleItemSector"/> su catalog.service_sale_item_sectors.</summary>
public sealed class ServiceSaleItemSectorConfiguration : IEntityTypeConfiguration<ServiceSaleItemSector>
{
	public void Configure(EntityTypeBuilder<ServiceSaleItemSector> builder)
	{
		builder.ToTable("service_sale_item_sectors", "catalog", t => t.HasComment(
			"File/settori in cui un servizio è acquistabile, quando non vale per tutte."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_sale_item_sectors");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SaleItemId)
			.HasColumnName("sale_item_id")
			.IsRequired()
			.HasComment("Servizio.");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Fila/settore.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.SaleItemId, x.SectorId }, "uq_catalog_service_sale_item_sectors_sector")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_service_sale_item_sectors_sector");

		builder.HasIndex(x => x.SectorId, "ix_catalog_service_sale_item_sectors_sector")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_service_sale_item_sectors_sector");

		builder.HasOne<ServiceSaleItem>()
			.WithMany(p => p.Sectors)
			.HasForeignKey(x => x.SaleItemId)
			.HasConstraintName("fk_catalog_service_sale_item_sectors_item")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_service_sale_item_sectors_sector")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
