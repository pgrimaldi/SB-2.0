using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ServiceSaleItemTranslation"/> su catalog.service_sale_item_translations.</summary>
public sealed class ServiceSaleItemTranslationConfiguration : IEntityTypeConfiguration<ServiceSaleItemTranslation>
{
	public void Configure(EntityTypeBuilder<ServiceSaleItemTranslation> builder)
	{
		builder.ToTable("service_sale_item_translations", "catalog", t => t.HasComment(
			"Nome e descrizione di un servizio nelle altre lingue."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_sale_item_translations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SaleItemId)
			.HasColumnName("sale_item_id")
			.IsRequired()
			.HasComment("Riferimento a un servizio.");

		builder.Property(x => x.Language)
			.HasColumnName("language")
			.IsRequired()
			.HasColumnType("catalog.content_language")
			.HasComment("Lingua della traduzione.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.IsRequired()
			.HasMaxLength(255)
			.HasComment("Nome tradotto.");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasColumnType("text")
			.HasComment("Descrizione tradotta.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => new { x.SaleItemId, x.Language }, "uq_catalog_service_sale_item_translations_language")
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_service_sale_item_translations_language");

		builder.HasOne<ServiceSaleItem>()
			.WithMany(p => p.Translations)
			.HasForeignKey(x => x.SaleItemId)
			.HasConstraintName("fk_catalog_service_sale_item_translations_parent")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
