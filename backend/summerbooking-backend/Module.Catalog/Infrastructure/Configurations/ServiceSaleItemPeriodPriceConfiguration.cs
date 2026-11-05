using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ServiceSaleItemPeriodPrice"/> su catalog.service_sale_item_period_prices.</summary>
public sealed class ServiceSaleItemPeriodPriceConfiguration : IEntityTypeConfiguration<ServiceSaleItemPeriodPrice>
{
	public void Configure(EntityTypeBuilder<ServiceSaleItemPeriodPrice> builder)
	{
		builder.ToTable("service_sale_item_period_prices", "catalog", t =>
		{
			t.HasComment("Prezzo di un servizio per periodo tariffario della stagione (variazioni di prezzo per periodo).");
			t.HasCheckConstraint("ck_catalog_service_sale_item_period_prices_price", "price >= 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_sale_item_period_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SaleItemId)
			.HasColumnName("sale_item_id")
			.IsRequired()
			.HasComment("Servizio.");

		builder.Property(x => x.TariffPeriodGroup)
			.HasColumnName("tariff_period_group")
			.IsRequired()
			.HasComment("Chiave del periodo nella stagione (pricing_tariff_periods.period_group_key).");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.IsRequired()
			.HasPrecision(12, 2)
			.HasComment("Prezzo del servizio nel periodo.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.SaleItemId, x.TariffPeriodGroup }, "uq_catalog_service_sale_item_period_prices_period")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_service_sale_item_period_prices_period");

		builder.HasOne<ServiceSaleItem>()
			.WithMany(p => p.PeriodPrices)
			.HasForeignKey(x => x.SaleItemId)
			.HasConstraintName("fk_catalog_service_sale_item_period_prices_item")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
