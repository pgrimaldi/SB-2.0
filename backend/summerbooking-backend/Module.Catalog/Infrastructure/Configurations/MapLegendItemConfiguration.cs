using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="MapLegendItem"/> su catalog.map_legend_items.</summary>
public sealed class MapLegendItemConfiguration : IEntityTypeConfiguration<MapLegendItem>
{
	public void Configure(EntityTypeBuilder<MapLegendItem> builder)
	{
		builder.ToTable("map_legend_items", "catalog", t => t.HasComment(
			"Voci della legenda mostrata sulla mappa di prenotazione online del sito pubblico. Sostituisce l'array JSON properties.onlineBookingOptions.locationOptionObj.legendList."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_legend_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento a cui appartiene la legenda.");

		builder.Property(x => x.Label)
			.HasColumnName("label")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Testo della voce di legenda (es. \"Ombrellone libero\").");

		builder.Property(x => x.IconAssetId)
			.HasColumnName("icon_asset_id")
			.HasComment("Icona della voce di legenda (catalog.media_assets, kind = icon).");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasComment("Ordine di visualizzazione della voce.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete della voce di legenda.");

		builder.HasIndex(x => new { x.PropertyId, x.SortOrder })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_map_legend_items_property");

		builder.HasIndex(x => x.IconAssetId)
			.HasDatabaseName("ix_catalog_map_legend_items_icon_asset");

		builder.HasOne(x => x.Property)
			.WithMany(p => p.MapLegendItems)
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_map_legend_items_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.IconAsset)
			.WithMany()
			.HasForeignKey(x => x.IconAssetId)
			.HasConstraintName("fk_catalog_map_legend_items_icon_asset");
	}
}
