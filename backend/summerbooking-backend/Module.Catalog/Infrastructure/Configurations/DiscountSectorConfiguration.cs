using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="DiscountSector"/> su catalog.pricing_discount_sectors.</summary>
public sealed class DiscountSectorConfiguration : IEntityTypeConfiguration<DiscountSector>
{
	public void Configure(EntityTypeBuilder<DiscountSector> builder)
	{
		builder.ToTable("pricing_discount_sectors", "catalog", t => t.HasComment(
			"Settori a cui un voucher (discount) è ristretto; assenza di righe = nessuna restrizione."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_discount_sectors");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.DiscountId)
			.HasColumnName("discount_id")
			.IsRequired()
			.HasComment("Voucher a cui si applica la restrizione di settore.");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Settore a cui il voucher è limitato.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: valorizzata quando la restrizione viene rimossa.");

		builder.HasOne(x => x.Discount)
			.WithMany(d => d.RestrictedSectors)
			.HasForeignKey(x => x.DiscountId)
			.HasConstraintName("fk_catalog_pricing_discount_sectors_discount")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Sector)
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_pricing_discount_sectors_sector")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.DiscountId, x.SectorId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_discount_sectors_discount_sector");

		builder.HasIndex(x => x.SectorId)
			.HasDatabaseName("ix_catalog_pricing_discount_sectors_sector");
	}
}
