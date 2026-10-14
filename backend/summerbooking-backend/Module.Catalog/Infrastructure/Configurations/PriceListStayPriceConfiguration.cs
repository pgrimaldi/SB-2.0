using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="PriceListStayPrice"/> su catalog.pricing_price_list_stay_prices.</summary>
public sealed class PriceListStayPriceConfiguration : IEntityTypeConfiguration<PriceListStayPrice>
{
	public void Configure(EntityTypeBuilder<PriceListStayPrice> builder)
	{
		builder.ToTable("pricing_price_list_stay_prices", "catalog", t =>
		{
			t.HasComment("Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario.");
			t.HasCheckConstraint("ck_catalog_pricing_price_list_stay_prices_stay_days", "stay_days BETWEEN 1 AND 90");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_stay_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PeriodPriceId)
			.HasColumnName("period_price_id")
			.IsRequired()
			.HasComment("Periodo tariffario (e, tramite esso, voce di listino) a cui si applica il prezzo per durata.");
		builder.HasOne<PriceListPeriodPrice>()
			.WithMany(p => p.StayPriceOverrides)
			.HasForeignKey(x => x.PeriodPriceId)
			.HasConstraintName("fk_catalog_pricing_price_list_stay_prices_period_price")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.HasComment("Settore a cui si applica il prezzo per durata (NULL = valido per tutti i settori del periodo, da confermare in migrazione).");
		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_pricing_price_list_stay_prices_sector")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.StayDays)
			.HasColumnName("stay_days")
			.IsRequired()
			.HasComment("Numero di giorni consecutivi di soggiorno a cui si applica il prezzo (1-90).");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo impostato manualmente dal gestore per la durata indicata.");

		builder.Property(x => x.IsManualOverride)
			.HasColumnName("is_manual_override")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Sempre true per le righe presenti: si persistono solo gli override manuali (isPriceChange legacy).");

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
			.HasComment("Soft-delete dell'override.");

		builder.HasIndex(x => new { x.PeriodPriceId, x.SectorId, x.StayDays })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_stay_prices_period_sector_days");

		builder.HasIndex(x => new { x.PeriodPriceId, x.StayDays })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_stay_prices_period_days");
	}
}
