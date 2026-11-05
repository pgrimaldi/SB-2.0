using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PriceListReducedPrice"/> su catalog.pricing_price_list_reduced_prices.</summary>
public sealed class PriceListReducedPriceConfiguration : IEntityTypeConfiguration<PriceListReducedPrice>
{
	public void Configure(EntityTypeBuilder<PriceListReducedPrice> builder)
	{
		builder.ToTable("pricing_price_list_reduced_prices", "catalog", t =>
		{
			t.HasComment("Prezzo di un pacchetto per il prezzo stagionale ridotto, per fila/settore.");
			t.HasCheckConstraint("ck_catalog_pricing_price_list_reduced_prices_price", "price >= 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_reduced_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Pacchetto.");

		builder.Property(x => x.ReducedSeasonId)
			.HasColumnName("reduced_season_id")
			.IsRequired()
			.HasComment("Prezzo stagionale ridotto (pricing_reduced_seasons).");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Fila/settore.");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.IsRequired()
			.HasPrecision(12, 2)
			.HasComment("Prezzo per l'intero periodo ridotto.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.PriceListId, x.ReducedSeasonId, x.SectorId }, "uq_catalog_pricing_price_list_reduced_prices_season_sector")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_reduced_prices_season_sector");

		builder.HasIndex(x => x.ReducedSeasonId, "ix_catalog_pricing_price_list_reduced_prices_season")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_reduced_prices_season");

		builder.HasOne<PriceList>()
			.WithMany(p => p.ReducedPrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_reduced_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<ReducedSeason>()
			.WithMany()
			.HasForeignKey(x => x.ReducedSeasonId)
			.HasConstraintName("fk_catalog_pricing_price_list_reduced_prices_season")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_pricing_price_list_reduced_prices_sector")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
