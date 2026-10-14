using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="AmenityCategory"/> su catalog.service_amenity_categories.</summary>
public sealed class AmenityCategoryConfiguration : IEntityTypeConfiguration<AmenityCategory>
{
	public void Configure(EntityTypeBuilder<AmenityCategory> builder)
	{
		builder.ToTable("service_amenity_categories", "catalog", t => t.HasComment(
			"Categoria di raggruppamento delle dotazioni (catalog_service_amenities) nella ricerca pubblica."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_amenity_categories");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(48)
			.IsRequired()
			.HasComment("Codice stabile della categoria (slug: popular, accessibility, restaurant, ...); l'etichetta mostrata sta nelle risorse di localizzazione.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Categoria visibile nella ricerca pubblica.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasComment("Ordine di visualizzazione nella ricerca pubblica.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della categoria.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della categoria.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.Code)
			.IsUnique()
			.HasDatabaseName("uq_catalog_service_amenity_categories_code");
	}
}
