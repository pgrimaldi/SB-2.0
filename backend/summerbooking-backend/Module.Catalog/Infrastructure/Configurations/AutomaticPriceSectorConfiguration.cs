using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AutomaticPriceSector"/> su catalog.automatic_price_sectors.</summary>
public sealed class AutomaticPriceSectorConfiguration : IEntityTypeConfiguration<AutomaticPriceSector>
{
	public void Configure(EntityTypeBuilder<AutomaticPriceSector> builder)
	{
		builder.ToTable("automatic_price_sectors", "catalog", t => t.HasComment(
			"File/settori in cui vale un prezzo automatico."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_automatic_price_sectors");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.AutomaticPriceId)
			.HasColumnName("automatic_price_id")
			.IsRequired()
			.HasComment("Prezzo automatico.");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Fila/settore.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.AutomaticPriceId, x.SectorId }, "uq_catalog_automatic_price_sectors_sector")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_automatic_price_sectors_sector");

		builder.HasIndex(x => x.SectorId, "ix_catalog_automatic_price_sectors_sector")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_automatic_price_sectors_sector");

		builder.HasOne<AutomaticPrice>()
			.WithMany(p => p.Sectors)
			.HasForeignKey(x => x.AutomaticPriceId)
			.HasConstraintName("fk_catalog_automatic_price_sectors_price")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_automatic_price_sectors_sector")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
