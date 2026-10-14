using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PriceListReducedPrice"/> su catalog.pricing_price_list_reduced_prices.</summary>
public sealed class PriceListReducedPriceConfiguration : IEntityTypeConfiguration<PriceListReducedPrice>
{
	public void Configure(EntityTypeBuilder<PriceListReducedPrice> builder)
	{
		builder.ToTable("pricing_price_list_reduced_prices", "catalog", t => t.HasComment(
			"Prezzo ridotto assoluto per settore, valido per una finestra di date con match esatto."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_reduced_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.WindowId)
			.HasColumnName("window_id")
			.IsRequired()
			.HasComment("Finestra di date a cui appartiene il prezzo ridotto.");
		builder.HasOne<PriceListReducedWindow>()
			.WithMany(w => w.SectorPrices)
			.HasForeignKey(x => x.WindowId)
			.HasConstraintName("fk_catalog_pricing_price_list_reduced_prices_window")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Settore per cui vale il prezzo ridotto.");
		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_pricing_price_list_reduced_prices_sector")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo assoluto del pacchetto in quel settore per l'intervallo, sostituisce il prezzo standard.");

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

		builder.HasIndex(x => new { x.WindowId, x.SectorId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_reduced_prices_window_sector");
	}
}
