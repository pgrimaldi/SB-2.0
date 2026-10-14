using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="MapLayoutSpecialIcon"/> su catalog.map_layout_special_icons.</summary>
public sealed class MapLayoutSpecialIconConfiguration : IEntityTypeConfiguration<MapLayoutSpecialIcon>
{
	public void Configure(EntityTypeBuilder<MapLayoutSpecialIcon> builder)
	{
		builder.ToTable("map_layout_special_icons", "catalog", t => t.HasComment(
			"Icone delle varianti speciali di ombrellone (1-5) per stato occupato/libero. Sostituisce le colonne specialUmbrella1..5 di configure_maps."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_layout_special_icons");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.LayoutId)
			.HasColumnName("layout_id")
			.IsRequired()
			.HasComment("Layout a cui appartiene la variante.");

		builder.Property(x => x.Variant)
			.HasColumnName("variant")
			.IsRequired()
			.HasComment("Indice della variante speciale (1-5, corrispondente a SpecialUmbrella1..5 nel legacy); estendibile.");

		builder.Property(x => x.State)
			.HasColumnName("state")
			.HasColumnType("catalog.map_icon_state")
			.IsRequired()
			.HasComment("Stato rappresentato dall'icona (occupied/free).");

		builder.Property(x => x.MediaAssetId)
			.HasColumnName("media_asset_id")
			.IsRequired()
			.HasComment("Icona della variante (catalog.media_assets).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete della variante.");

		builder.HasIndex(x => new { x.LayoutId, x.Variant, x.State })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_map_layout_special_icons_layout_variant_state");

		builder.HasOne(x => x.Layout)
			.WithMany(l => l.SpecialIcons)
			.HasForeignKey(x => x.LayoutId)
			.HasConstraintName("fk_catalog_map_layout_special_icons_layout")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.MediaAsset)
			.WithMany()
			.HasForeignKey(x => x.MediaAssetId)
			.HasConstraintName("fk_catalog_map_layout_special_icons_media_asset")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
