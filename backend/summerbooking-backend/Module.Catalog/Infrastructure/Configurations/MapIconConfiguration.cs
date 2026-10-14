using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="MapIcon"/> su catalog.map_icons.</summary>
public sealed class MapIconConfiguration : IEntityTypeConfiguration<MapIcon>
{
	public void Configure(EntityTypeBuilder<MapIcon> builder)
	{
		builder.ToTable("map_icons", "catalog", t => t.HasComment(
			"Catalogo globale/master delle icone/immagini per comporre la mappa spiaggia: sfondi, varianti ombrellone/cabina/tenda/lettino nei due stati."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_icons");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.CategoryId)
			.HasColumnName("category_id")
			.IsRequired()
			.HasComment("Categoria dell'icona (catalog.map_icon_categories).");

		builder.Property(x => x.Type)
			.HasColumnName("type")
			.HasColumnType("catalog.map_icon_state")
			.IsRequired()
			.HasComment("Stato rappresentato dall'icona; ignorato (ma valorizzato) per category = template.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome descrittivo mostrato nel selettore del gestionale.");

		builder.Property(x => x.MediaAssetId)
			.HasColumnName("media_asset_id")
			.IsRequired()
			.HasComment("Il file icona (catalog.media_assets, kind = icon).");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Icona selezionabile nel pannello SuperAdmin.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete; la rimozione del file su storage è demandata a un job asincrono.");

		builder.HasIndex(x => new { x.CategoryId, x.Type })
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_map_icons_category_type");

		builder.HasIndex(x => x.MediaAssetId)
			.HasDatabaseName("ix_catalog_map_icons_media_asset");

		builder.HasOne(x => x.Category)
			.WithMany()
			.HasForeignKey(x => x.CategoryId)
			.HasConstraintName("fk_catalog_map_icons_category")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.MediaAsset)
			.WithMany()
			.HasForeignKey(x => x.MediaAssetId)
			.HasConstraintName("fk_catalog_map_icons_media_asset")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
