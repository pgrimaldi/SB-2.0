using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="MapLayout"/> su catalog.map_layouts.</summary>
public sealed class MapLayoutConfiguration : IEntityTypeConfiguration<MapLayout>
{
	public void Configure(EntityTypeBuilder<MapLayout> builder)
	{
		builder.ToTable("map_layouts", "catalog", t =>
		{
			t.HasComment("Layout grafico della mappa spiaggia di uno stabilimento: griglia, sfondo, colore, icone per tipo elemento nei due stati.");
			t.HasCheckConstraint("ck_catalog_map_layouts_background_color", "background_color IS NULL OR background_color ~ '^#[0-9A-Fa-f]{6}$'");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_layouts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario del layout (1:1).");

		builder.Property(x => x.GridRows)
			.HasColumnName("grid_rows")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di righe della griglia.");

		builder.Property(x => x.GridColumns)
			.HasColumnName("grid_columns")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di colonne della griglia (legacy: configure_maps.items, rinominato per eliminare l'ambiguità).");

		builder.Property(x => x.IsCustomTemplate)
			.HasColumnName("is_custom_template")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True se lo sfondo è un'immagine caricata dal gestore; false se è un template predefinito.");

		builder.Property(x => x.TemplateAssetId)
			.HasColumnName("template_asset_id")
			.HasComment("Immagine di sfondo della griglia (catalog.media_assets, kind = icon).");

		builder.Property(x => x.BackgroundColor)
			.HasColumnName("background_color")
			.HasColumnType("char(7)")
			.HasComment("Colore di sfondo/tema della griglia, hex #RRGGBB.");

		builder.Property(x => x.UmbrellaOccupiedAssetId)
			.HasColumnName("umbrella_occupied_asset_id")
			.HasComment("Icona dell'ombrellone standard nello stato occupato.");
		builder.Property(x => x.UmbrellaFreeAssetId)
			.HasColumnName("umbrella_free_asset_id")
			.HasComment("Icona dell'ombrellone standard nello stato libero.");

		builder.Property(x => x.CreatedByUserPublicId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha configurato il layout.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete del layout.");

		builder.HasIndex(x => x.PropertyId).IsUnique().HasDatabaseName("uq_catalog_map_layouts_property");

		builder.HasOne(x => x.Property)
			.WithOne()
			.HasForeignKey<MapLayout>(x => x.PropertyId)
			.HasConstraintName("fk_catalog_map_layouts_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.TemplateAsset).WithMany().HasForeignKey(x => x.TemplateAssetId).HasConstraintName("fk_catalog_map_layouts_template_asset");
		builder.HasOne(x => x.UmbrellaOccupiedAsset).WithMany().HasForeignKey(x => x.UmbrellaOccupiedAssetId).HasConstraintName("fk_catalog_map_layouts_umbrella_occupied_asset");
		builder.HasOne(x => x.UmbrellaFreeAsset).WithMany().HasForeignKey(x => x.UmbrellaFreeAssetId).HasConstraintName("fk_catalog_map_layouts_umbrella_free_asset");
	}
}
