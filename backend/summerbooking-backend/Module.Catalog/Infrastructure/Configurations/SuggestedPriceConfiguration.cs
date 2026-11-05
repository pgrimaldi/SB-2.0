using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;
using Module.Catalog.Domain.Enums;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="SuggestedPrice"/> su catalog.pricing_suggested_prices.</summary>
public sealed class SuggestedPriceConfiguration : IEntityTypeConfiguration<SuggestedPrice>
{
	public void Configure(EntityTypeBuilder<SuggestedPrice> builder)
	{
		builder.ToTable("pricing_suggested_prices", "catalog", t => t.HasComment(
			"Prezzo suggerito dall'algoritmo di Revenue Management per un pacchetto in una data specifica."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_suggested_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Pacchetto a cui si riferisce il prezzo suggerito.");
		builder.HasOne<PriceList>()
			.WithMany()
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_suggested_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento, denormalizzato per filtri/indici.");
		builder.HasOne<Property>()
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_pricing_suggested_prices_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.PriceDate)
			.HasColumnName("price_date")
			.IsRequired()
			.HasComment("Giorno a cui si riferisce il prezzo suggerito.");

		builder.Property(x => x.PeriodLabel)
			.HasColumnName("period_label")
			.HasMaxLength(120)
			.HasComment("Etichetta «fila + periodo» dei dati importati (es. \"Fila 1 Alta stagione\"), solo traccia: valgono sector_id e tariff_period_group.");

		builder.Property(x => x.ListDailyPrice)
			.HasColumnName("list_daily_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo di listino giornaliero di riferimento al momento del calcolo (snapshot).");

		builder.Property(x => x.ListWeekendPrice)
			.HasColumnName("list_weekend_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo di listino weekend di riferimento al momento del calcolo (snapshot).");

		builder.Property(x => x.SuggestedPriceValue)
			.HasColumnName("suggested_price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo suggerito dall'algoritmo per la data.");

		builder.Property(x => x.DiscountPercentage)
			.HasColumnName("discount_percentage")
			.HasColumnType("numeric(5,2)")
			.HasDefaultValue(0m)
			.IsRequired()
			.HasComment("Sconto percentuale suggerito, alternativa al prezzo assoluto.");

		builder.Property(x => x.AppliedMode)
			.HasColumnName("applied_mode")
			.HasColumnType("catalog.suggested_price_mode")
			.HasDefaultValueSql("'none'")
			.IsRequired()
			.HasComment("Cosa ha deciso il gestore per la data: nulla, prezzo suggerito, prezzo originale o sconto.");

		builder.Property(x => x.ComputedAt)
			.HasColumnName("computed_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Momento in cui l'algoritmo ha prodotto la riga.");

		builder.Property(x => x.AppliedAt)
			.HasColumnName("applied_at")
			.HasComment("Momento in cui il gestore ha applicato il prezzo/sconto suggerito.");

		builder.Property(x => x.AppliedByUserId)
			.HasColumnName("applied_by_user_id")
			.HasComment("public_id di identity_access_users, copiato — nessuna FK cross-schema.");

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
			.HasComment("Soft-delete (tabella di output algoritmico; le righe con data passata sono candidate a retention hard-delete via job).");

		// UNIQUE su (price_list_id, price_date, period_label) invece di (price_list_id, price_date):
		// i dati reali sono multi-variante per data (99%+ delle date con più di una riga
		// packages[] per lo stesso pacchetto/data, distinte per period_label).
		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.HasComment("Fila del prezzo di listino di partenza; NULL = tutte le file.");
		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_pricing_suggested_prices_sector")
			.OnDelete(DeleteBehavior.Restrict);
		builder.Property(x => x.TariffPeriodGroup)
			.HasColumnName("tariff_period_group")
			.HasComment("Periodo tariffario della data, al momento del calcolo.");

		builder.HasIndex(x => new { x.PriceListId, x.PriceDate, x.SectorId }, "uq_catalog_pricing_suggested_prices_package_date_sector")
			.IsUnique()
			.AreNullsDistinct(false)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_suggested_prices_package_date_sector");

		builder.HasIndex(x => new { x.PropertyId, x.PriceDate })
			.HasFilter("applied_mode <> 'none'")
			.HasDatabaseName("ix_catalog_pricing_suggested_prices_property_date");
	}
}
