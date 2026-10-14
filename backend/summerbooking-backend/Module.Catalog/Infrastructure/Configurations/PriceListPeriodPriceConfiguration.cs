using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PriceListPeriodPrice"/> su catalog.pricing_price_list_period_prices.</summary>
public sealed class PriceListPeriodPriceConfiguration : IEntityTypeConfiguration<PriceListPeriodPrice>
{
	public void Configure(EntityTypeBuilder<PriceListPeriodPrice> builder)
	{
		builder.ToTable("pricing_price_list_period_prices", "catalog", t => t.HasComment(
			"Tariffe base (giornaliera/weekend/mezza giornata/7-14gg/mensile) di un pacchetto per periodo tariffario."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_period_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Pacchetto a cui si riferiscono i prezzi.");
		builder.HasOne<PriceList>()
			.WithMany(p => p.PeriodPrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_period_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.TariffPeriodGroup)
			.HasColumnName("tariff_period_group")
			.HasComment("Gruppo di periodo tariffario (period_group_key di catalog_pricing_tariff_periods per la stagione della property); NULL = tariffa senza periodo.");

		builder.Property(x => x.HalfDayPrice)
			.HasColumnName("half_day_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo mezza giornata nel periodo.");

		builder.Property(x => x.HalfDayWeekendPrice)
			.HasColumnName("half_day_weekend_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo mezza giornata weekend nel periodo.");

		builder.Property(x => x.DailyPrice)
			.HasColumnName("daily_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo giornaliero nel periodo.");

		builder.Property(x => x.WeekendPrice)
			.HasColumnName("weekend_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo giornaliero weekend nel periodo.");

		builder.Property(x => x.Days7Price)
			.HasColumnName("days_7_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo per soggiorno di 7 giorni nel periodo.");

		builder.Property(x => x.Days14Price)
			.HasColumnName("days_14_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo per soggiorno di 14 giorni nel periodo.");

		builder.Property(x => x.MonthlyPrice)
			.HasColumnName("monthly_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo mensile nel periodo.");

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
			.HasComment("Soft-delete della riga tariffaria.");

		builder.HasIndex(x => new { x.PriceListId, x.TariffPeriodGroup })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_period_prices_package_period");

		builder.HasIndex(x => x.TariffPeriodGroup)
			.HasDatabaseName("ix_catalog_pricing_price_list_period_prices_period");

		builder.HasMany(x => x.StayPriceOverrides).WithOne().HasForeignKey(s => s.PeriodPriceId);
	}
}
