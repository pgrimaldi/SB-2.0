using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PriceListSectorPrice"/> su catalog.pricing_price_list_sector_prices.</summary>
public sealed class PriceListSectorPriceConfiguration : IEntityTypeConfiguration<PriceListSectorPrice>
{
	public void Configure(EntityTypeBuilder<PriceListSectorPrice> builder)
	{
		builder.ToTable("pricing_price_list_sector_prices", "catalog", t => t.HasComment(
			"Prezzo base per settore di un pacchetto; l'esistenza della riga marca anche l'applicabilità al settore."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_sector_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Pacchetto a cui si riferisce il prezzo.");
		builder.HasOne<PriceList>()
			.WithMany(p => p.SectorPrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_sector_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Settore a cui si applica il prezzo/la disponibilità.");
		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_pricing_price_list_sector_prices_sector")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.BasePrice)
			.HasColumnName("base_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo stagionale forfettario del pacchetto nel settore (fallback rispetto ai prezzi per periodo).");

		builder.Property(x => x.HalfDayPrice)
			.HasColumnName("half_day_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo forfettario mezza giornata nel settore (allineamento con i listini hotel; spesso NULL sui price_lists puri).");

		builder.Property(x => x.SeasonalPrice)
			.HasColumnName("seasonal_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo per l'intera stagione (pass stagionale) nel settore.");

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


		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della riga.");

		builder.HasIndex(x => new { x.PriceListId, x.SectorId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_sector_prices_package_sector");

		builder.HasIndex(x => x.SectorId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_sector_prices_sector");
	}
}
