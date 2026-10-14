using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="BathhousePhoto"/> su catalog.bathhouse_photos.</summary>
public sealed class BathhousePhotoConfiguration : IEntityTypeConfiguration<BathhousePhoto>
{
	public void Configure(EntityTypeBuilder<BathhousePhoto> builder)
	{
		builder.ToTable("bathhouse_photos", "catalog", t => t.HasComment(
			"Elenco ordinato delle foto della galleria pubblica di una vetrina. Sostituisce l'array JSON bathouses.photos."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_bathhouse_photos");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.BathhouseId)
			.HasColumnName("bathhouse_id")
			.IsRequired()
			.HasComment("Vetrina a cui appartiene la foto.");

		builder.Property(x => x.MediaAssetId)
			.HasColumnName("media_asset_id")
			.IsRequired()
			.HasComment("Il file immagine (catalog.media_assets, kind = photo).");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasComment("Ordine di visualizzazione in galleria (0-based); il riordino aggiorna solo questa colonna.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete: foto rimossa dalla galleria.");

		builder.HasIndex(x => new { x.BathhouseId, x.SortOrder })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_bathhouse_photos_bathhouse_sort");

		builder.HasIndex(x => x.BathhouseId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_bathhouse_photos_bathhouse");

		builder.HasIndex(x => x.MediaAssetId)
			.HasDatabaseName("ix_catalog_bathhouse_photos_media_asset");

		builder.HasOne(x => x.Bathhouse)
			.WithMany(b => b.Photos)
			.HasForeignKey(x => x.BathhouseId)
			.HasConstraintName("fk_catalog_bathhouse_photos_bathhouse")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.MediaAsset)
			.WithMany()
			.HasForeignKey(x => x.MediaAssetId)
			.HasConstraintName("fk_catalog_bathhouse_photos_media_asset")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
