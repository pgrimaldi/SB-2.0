using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="MapBeachElement"/> su catalog.map_beach_elements.</summary>
public sealed class MapBeachElementConfiguration : IEntityTypeConfiguration<MapBeachElement>
{
	public void Configure(EntityTypeBuilder<MapBeachElement> builder)
	{
		builder.ToTable("map_beach_elements", "catalog", t => t.HasComment(
			"Catalogo di piattaforma degli elementi spiaggia solo grafici (passerella, doccia, bar, torretta, ...); il systemadmin ne aggiunge di nuovi."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_beach_elements");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId).HasColumnName("public_id").ValueGeneratedNever().IsRequired()
			.HasComment("Id pubblico (UUIDv7) usato dalle API.");
		builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(MapBeachElement.NameMaxLength).IsRequired()
			.HasComment("Nome dell'elemento per il pannello di amministrazione; sulla mappa compare solo l'icona.");
		builder.Property(x => x.MediaAssetId).HasColumnName("media_asset_id")
			.HasComment("Icona dell'elemento; NULL finché il systemadmin non la carica.");
		builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired().HasDefaultValue((short)0)
			.HasComment("Ordine nell'elenco mostrato al gestore.");
		builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true)
			.HasComment("Elemento selezionabile dal gestore.");
		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasOne(x => x.MediaAsset)
			.WithMany()
			.HasForeignKey(x => x.MediaAssetId)
			.HasConstraintName("fk_catalog_map_beach_elements_media_asset")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(x => x.MediaAssetId).HasDatabaseName("ix_catalog_map_beach_elements_media_asset");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_map_beach_elements_public_id");
		builder.HasIndex(x => x.Name).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("uq_catalog_map_beach_elements_name");
	}
}
