using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AgreementPriceListDatePrice"/> su catalog.agreement_price_list_date_prices.</summary>
public sealed class AgreementPriceListDatePriceConfiguration
	: IEntityTypeConfiguration<AgreementPriceListDatePrice>
{
	public void Configure(EntityTypeBuilder<AgreementPriceListDatePrice> builder)
	{
		builder.ToTable("agreement_price_list_date_prices", "catalog", t => t.HasComment(
			"Override di prezzo per data di calendario (festivi, deroghe puntuali) di un listino/voucher hotel."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_list_date_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.HasColumnType("integer")
			.IsRequired()
			.HasComment("Listino/voucher hotel a cui si applica la deroga.");

		builder.Property(x => x.PeriodPriceId)
			.HasColumnName("period_price_id")
			.HasComment("Periodo tariffario a cui la deroga è agganciata; NULL per le date festive globali del listino.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("catalog.date_price_kind")
			.IsRequired()
			.HasComment("Natura della deroga: override puntuale o data festiva.");

		builder.Property(x => x.PriceDate)
			.HasColumnName("price_date")
			.IsRequired()
			.HasComment("Data a cui si applica la deroga.");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo giornaliero per la data; valorizzato per kind = override, può essere NULL per kind = holiday.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete.");

		builder.HasOne(x => x.PriceList)
			.WithMany(pl => pl.DatePrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_agreement_price_list_date_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.PeriodPrice)
			.WithMany(p => p.DatePrices)
			.HasForeignKey(x => x.PeriodPriceId)
			.HasConstraintName("fk_catalog_agreement_price_list_date_prices_period_price")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.PriceListId, x.PriceDate, x.Kind })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_agreement_price_list_date_prices_pricelist_date_kind");

		builder.HasIndex(x => new { x.PriceListId, x.PriceDate })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_date_prices_pricelist_date");
	}
}
