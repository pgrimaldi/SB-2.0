using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AutomaticPriceService"/> su catalog.automatic_price_services.</summary>
public sealed class AutomaticPriceServiceConfiguration : IEntityTypeConfiguration<AutomaticPriceService>
{
	public void Configure(EntityTypeBuilder<AutomaticPriceService> builder)
	{
		builder.ToTable("automatic_price_services", "catalog", t => t.HasComment(
			"Servizi a cui si applica un prezzo automatico, quando non vale per tutti."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_automatic_price_services");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.AutomaticPriceId)
			.HasColumnName("automatic_price_id")
			.IsRequired()
			.HasComment("Prezzo automatico.");

		builder.Property(x => x.SaleItemId)
			.HasColumnName("sale_item_id")
			.IsRequired()
			.HasComment("Servizio.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.AutomaticPriceId, x.SaleItemId }, "uq_catalog_automatic_price_services_item")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_automatic_price_services_item");

		builder.HasIndex(x => x.SaleItemId, "ix_catalog_automatic_price_services_sale_item")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_automatic_price_services_sale_item");

		builder.HasOne<AutomaticPrice>()
			.WithMany(p => p.Services)
			.HasForeignKey(x => x.AutomaticPriceId)
			.HasConstraintName("fk_catalog_automatic_price_services_price")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne<ServiceSaleItem>()
			.WithMany()
			.HasForeignKey(x => x.SaleItemId)
			.HasConstraintName("fk_catalog_automatic_price_services_item")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
