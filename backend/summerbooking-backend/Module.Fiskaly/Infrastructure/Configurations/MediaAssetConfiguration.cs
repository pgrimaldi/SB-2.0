using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Fiskaly.Domain.Entities;

namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
	public void Configure(EntityTypeBuilder<MediaAsset> builder)
	{
		builder.ToTable("media_assets", "fiskaly", t => t.HasComment(
			"Catalogo locale al modulo fiskaly dei riferimenti a file — oggi solo i PDF degli scontrini fiscali e pre-conto."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_media_assets");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.OwnsOne(x => x.Ref, r =>
		{
			r.Property(p => p.Origin)
				.HasColumnName("origin")
				.HasColumnType("fiskaly.media_origin")
				.IsRequired()
				.HasComment("Codice della sorgente da cui è servito il file; la mappa origin → URL base vive in configurazione.");
			r.HasIndex(p => p.Origin).HasDatabaseName("ix_fiskaly_media_assets_origin");

			r.Property(p => p.Path)
				.HasColumnName("path")
				.HasColumnType("text")
				.IsRequired()
				.HasComment("Percorso relativo del file rispetto alla base di origin (nessuna barra iniziale); se origin=external, URL completo invariato.");
		});

		builder.Navigation(x => x.Ref).IsRequired();

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("fiskaly.media_kind")
			.IsRequired()
			.HasComment("Tipo d'uso del file: PDF scontrino fiscale o pre-conto.");
		builder.HasIndex(x => x.Kind)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_fiskaly_media_assets_kind");

		builder.Property(x => x.Checksum)
			.HasColumnName("checksum")
			.HasColumnType("bytea")
			.HasComment("Hash SHA-256 del contenuto per deduplica/integrità; NULL per i file migrati finché non rigenerati.");

		builder.Property(x => x.ByteSize)
			.HasColumnName("byte_size")
			.HasComment("Dimensione del file in byte.");

		builder.Property(x => x.ContentType)
			.HasColumnName("content_type")
			.HasMaxLength(100)
			.HasComment("MIME type del file (es. application/pdf).");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: un asset non è cancellato finché lo referenzia uno scontrino; per i PDF fiscali di fatto non si cancella (retention 10 anni).");
	}
}
