using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Notification.Domain.Entities;
using Module.Notification.Domain.Enums;

namespace Module.Notification.Infrastructure.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
	public void Configure(EntityTypeBuilder<MediaAsset> builder)
	{
		builder.ToTable("media_assets", "notification", t => t.HasComment(
			"Catalogo locale al modulo dei riferimenti a file usati nelle comunicazioni: allegati, immagini di copertina, PDF renderizzati, upload da lead."));

		builder.HasKey(x => x.Id).HasName("pk_notification_media_assets");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. NULL per file di piattaforma o upload da lead pubblici.");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.HasColumnType("notification.media_origin")
			.IsRequired()
			.HasComment("Codice della sorgente da cui è servito il file; la mappa origin → URL base vive in configurazione.");

		builder.Property(x => x.Path)
			.HasColumnName("path")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Percorso relativo del file rispetto alla base di origin (URL completo invariato se origin = external), senza barra iniziale.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("notification.media_kind")
			.HasDefaultValueSql("'other'")
			.IsRequired()
			.HasComment("Tipo d'uso del file.");

		builder.Property(x => x.Filename)
			.HasColumnName("filename")
			.HasMaxLength(255)
			.HasComment("Nome file originale, mostrato all'utente e usato come filename MIME in allegato.");

		builder.Property(x => x.Checksum)
			.HasColumnName("checksum")
			.HasColumnType("bytea")
			.HasComment("SHA-256 del contenuto, per deduplica in caricamento; NULL per i file migrati finché non riconciliati.");

		builder.Property(x => x.ByteSize)
			.HasColumnName("byte_size")
			.HasComment("Dimensione del file in byte.");

		builder.Property(x => x.ContentType)
			.HasColumnName("content_type")
			.HasMaxLength(100)
			.HasComment("MIME type reale, validato con magic bytes in upload.");

		builder.Property(x => x.UploadedByUserId)
			.HasColumnName("uploaded_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema. Valorizzato per gli upload da gestionale.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione dell'asset.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: l'asset non si cancella finché referenziato; la GC fisica sullo storage agisce solo su asset soft-deleted e senza referenti.");

		builder.HasIndex(x => new { x.PropertyId, x.Kind })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_media_assets_property_kind");

		builder.HasIndex(x => x.Checksum)
			.HasFilter("checksum IS NOT NULL")
			.HasDatabaseName("ix_notification_media_assets_checksum");

		builder.HasIndex(x => x.Origin)
			.HasDatabaseName("ix_notification_media_assets_origin");
	}
}
