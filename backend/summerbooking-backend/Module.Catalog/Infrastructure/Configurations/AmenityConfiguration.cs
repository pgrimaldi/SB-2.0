using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="Amenity"/> su catalog.service_amenities.</summary>
public sealed class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
	public void Configure(EntityTypeBuilder<Amenity> builder)
	{
		builder.ToTable("service_amenities", "catalog", t => t.HasComment(
			"Catalogo master delle dotazioni/servizi che uno stabilimento può dichiarare di offrire (es. Wi-Fi, Doccia, Bar)."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_service_amenities");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Nome della dotazione nella lingua di default (le traduzioni vivono in service_amenity_translations).");

		builder.Property(x => x.CategoryId)
			.HasColumnName("category_id")
			.HasComment("Categoria a cui appartiene la dotazione; NULL = non categorizzata, esclusa dai filtri di ricerca.");

		builder.Property(x => x.IconAssetId)
			.HasColumnName("icon_asset_id")
			.HasComment("Icona della dotazione, mostrata nel selettore gestionale e nella scheda pubblica.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.IsRequired()
			.HasComment("Dotazione selezionabile/mostrata (il catalogo pubblico restituisce solo le attive).");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.HasDefaultValue((short)0)
			.IsRequired()
			.HasComment("Ordine di visualizzazione dentro la categoria.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Creazione della dotazione.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Ultima modifica della dotazione.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasIndex(x => x.CategoryId)
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_service_amenities_category");

		builder.HasIndex(x => x.IconAssetId)
			.HasDatabaseName("ix_catalog_service_amenities_media_asset");

		builder.HasOne<AmenityCategory>()
			.WithMany()
			.HasForeignKey(x => x.CategoryId)
			.HasConstraintName("fk_catalog_service_amenities_category")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne<MediaAsset>()
			.WithMany()
			.HasForeignKey(x => x.IconAssetId)
			.HasConstraintName("fk_catalog_service_amenities_icon_asset")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasMany(x => x.Translations)
			.WithOne()
			.HasForeignKey(x => x.AmenityId)
			.HasConstraintName("fk_catalog_service_amenity_translations_amenity")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
