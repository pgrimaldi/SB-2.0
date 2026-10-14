using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PriceListDatePrice"/> su catalog.pricing_price_list_date_prices.</summary>
public sealed class PriceListDatePriceConfiguration : IEntityTypeConfiguration<PriceListDatePrice>
{
	public void Configure(EntityTypeBuilder<PriceListDatePrice> builder)
	{
		builder.ToTable("pricing_price_list_date_prices", "catalog", t => t.HasComment(
			"Deroga di prezzo per data di calendario: data festiva con tariffa dedicata o override puntuale."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_date_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Pacchetto a cui si applica la deroga.");
		builder.HasOne<PriceList>()
			.WithMany(p => p.DatePrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_date_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.PeriodPriceId)
			.HasColumnName("period_price_id")
			.HasComment("Periodo tariffario a cui la deroga è agganciata (NULL per le date festive globali del pacchetto).");
		builder.HasOne<PriceListPeriodPrice>()
			.WithMany()
			.HasForeignKey(x => x.PeriodPriceId)
			.HasConstraintName("fk_catalog_pricing_price_list_date_prices_period_price")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("catalog.date_price_kind")
			.IsRequired()
			.HasComment("Natura della deroga: override puntuale o data festiva.");

		builder.Property(x => x.PriceDate)
			.HasColumnName("price_date")
			.IsRequired()
			.HasComment("Data a cui si applica il prezzo/la deroga.");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo giornaliero per la data (NULL per holiday quando si applica la regola festiva del pacchetto).");

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
			.HasComment("Soft-delete della deroga.");

		builder.HasIndex(x => new { x.PriceListId, x.PriceDate, x.Kind })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_date_prices_package_date_kind");

		builder.HasIndex(x => new { x.PriceListId, x.PriceDate })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_date_prices_package_date");
	}
}
