using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AgreementPriceListPeriodPrice"/> su catalog.agreement_price_list_period_prices.</summary>
public sealed class AgreementPriceListPeriodPriceConfiguration
	: IEntityTypeConfiguration<AgreementPriceListPeriodPrice>
{
	public void Configure(EntityTypeBuilder<AgreementPriceListPeriodPrice> builder)
	{
		builder.ToTable("agreement_price_list_period_prices", "catalog", t => t.HasComment(
			"Prezzi per periodo tariffario (etichetta libera) di un listino/voucher hotel, per modalità di durata."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_list_period_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.HasColumnType("integer")
			.IsRequired()
			.HasComment("Listino/voucher hotel a cui appartiene il prezzo di periodo.");

		builder.Property(x => x.PeriodLabel)
			.HasColumnName("period_label")
			.HasMaxLength(120)
			.HasComment("Etichetta libera del periodo tariffario; NULL = tariffa unica del listino (nessun collegamento a stagioni).");

		builder.Property(x => x.HalfDayPrice).HasColumnName("half_day_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo mezza giornata nel periodo.");
		builder.Property(x => x.HalfDayWeekendPrice).HasColumnName("half_day_weekend_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo mezza giornata weekend nel periodo.");
		builder.Property(x => x.DailyPrice).HasColumnName("daily_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo giornaliero nel periodo.");
		builder.Property(x => x.WeekendPrice).HasColumnName("weekend_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo weekend nel periodo.");
		builder.Property(x => x.Days7Price).HasColumnName("days_7_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo per soggiorno di 7 giorni nel periodo.");
		builder.Property(x => x.Days14Price).HasColumnName("days_14_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo per soggiorno di 14 giorni nel periodo.");
		builder.Property(x => x.MonthlyPrice).HasColumnName("monthly_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo mensile nel periodo.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete.");

		builder.HasOne(x => x.PriceList)
			.WithMany(pl => pl.PeriodPrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_agreement_price_list_period_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.DatePrices)
			.WithOne(d => d.PeriodPrice)
			.HasForeignKey(d => d.PeriodPriceId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.PriceListId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_period_prices_price_list");
	}
}
