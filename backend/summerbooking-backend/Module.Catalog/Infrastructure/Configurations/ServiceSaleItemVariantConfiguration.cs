using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ServiceSaleItemVariant"/> su catalog.service_sale_item_variants.</summary>
public sealed class ServiceSaleItemVariantConfiguration : IEntityTypeConfiguration<ServiceSaleItemVariant>
{
	public void Configure(EntityTypeBuilder<ServiceSaleItemVariant> builder)
	{
		builder.ToTable("service_sale_item_variants", "catalog", t =>
		{
			t.HasComment("Varianti di un servizio bar (es. Crema +0,50 €): il prezzo si somma a quello del servizio.");
			t.HasCheckConstraint("ck_catalog_service_sale_item_variants_price", "price >= 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_sale_item_variants");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SaleItemId)
			.HasColumnName("sale_item_id")
			.IsRequired()
			.HasComment("Servizio.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasMaxLength(120)
			.HasComment("Nome della variante.");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.IsRequired()
			.HasPrecision(12, 2)
			.HasComment("Prezzo aggiunto al servizio.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Ordine di visualizzazione.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.SaleItemId, "ix_catalog_service_sale_item_variants_item")
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_service_sale_item_variants_item");

		builder.HasOne<ServiceSaleItem>()
			.WithMany(p => p.Variants)
			.HasForeignKey(x => x.SaleItemId)
			.HasConstraintName("fk_catalog_service_sale_item_variants_item")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
