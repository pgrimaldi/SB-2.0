using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AgreementPriceListSectorPrice"/> su catalog.agreement_price_list_sector_prices.</summary>
public sealed class AgreementPriceListSectorPriceConfiguration
	: IEntityTypeConfiguration<AgreementPriceListSectorPrice>
{
	public void Configure(EntityTypeBuilder<AgreementPriceListSectorPrice> builder)
	{
		builder.ToTable("agreement_price_list_sector_prices", "catalog", t => t.HasComment(
			"Prezzo base / mezza giornata / stagionale di un listino/voucher hotel per settore."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_list_sector_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.HasColumnType("integer")
			.IsRequired()
			.HasComment("Listino/voucher hotel a cui appartiene il prezzo di settore.");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Settore (della property del partner) a cui si applica il prezzo.");

		builder.Property(x => x.BasePrice).HasColumnName("base_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo giornaliero base nel settore.");
		builder.Property(x => x.HalfDayPrice).HasColumnName("half_day_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo mezza giornata nel settore.");
		builder.Property(x => x.SeasonalPrice).HasColumnName("seasonal_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo stagionale nel settore.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete.");

		builder.HasOne(x => x.PriceList)
			.WithMany(pl => pl.SectorPrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_agreement_price_list_sector_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Sector)
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_agreement_price_list_sector_prices_sector")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(x => new { x.PriceListId, x.SectorId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_agreement_price_list_sector_prices_pricelist_sector");

		builder.HasIndex(x => x.SectorId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_sector_prices_sector");
	}
}
