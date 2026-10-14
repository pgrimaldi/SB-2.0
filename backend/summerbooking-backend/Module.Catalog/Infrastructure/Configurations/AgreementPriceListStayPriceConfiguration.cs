using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>
/// Mapping EF Core di <see cref="AgreementPriceListStayPrice"/> su catalog.agreement_price_list_stay_prices
/// — gemella di <see cref="PriceListStayPriceConfiguration"/> lato pricing.
/// </summary>
public sealed class AgreementPriceListStayPriceConfiguration : IEntityTypeConfiguration<AgreementPriceListStayPrice>
{
	public void Configure(EntityTypeBuilder<AgreementPriceListStayPrice> builder)
	{
		builder.ToTable("agreement_price_list_stay_prices", "catalog", t =>
		{
			t.HasComment("Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario di un listino/voucher hotel.");
			t.HasCheckConstraint("ck_catalog_agreement_price_list_stay_prices_stay_days", "stay_days BETWEEN 1 AND 90");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_list_stay_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PeriodPriceId)
			.HasColumnName("period_price_id")
			.IsRequired()
			.HasComment("Periodo tariffario (e, tramite esso, listino/voucher hotel) a cui si applica il prezzo per durata.");
		builder.HasOne<AgreementPriceListPeriodPrice>()
			.WithMany(p => p.StayPriceOverrides)
			.HasForeignKey(x => x.PeriodPriceId)
			.HasConstraintName("fk_catalog_agreement_price_list_stay_prices_period_price")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.HasComment("Settore a cui si applica il prezzo per durata (NULL = valido per tutti i settori del periodo).");
		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_agreement_price_list_stay_prices_sector")
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
			.HasDatabaseName("uq_catalog_agreement_price_list_stay_prices_period_sector_days");

		builder.HasIndex(x => new { x.PeriodPriceId, x.StayDays })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_stay_prices_period_days");
	}
}
