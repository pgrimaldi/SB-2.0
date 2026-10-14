using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="MediaAsset"/> su catalog.media_assets.</summary>
public sealed class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
	public void Configure(EntityTypeBuilder<MediaAsset> builder)
	{
		builder.ToTable("media_assets", "catalog", t => t.HasComment(
			"Catalogo locale al modulo catalog di tutti i riferimenti a file (foto, PDF, icone) usati dalle entità C2."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_media_assets");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.OwnsOne(x => x.Ref, r =>
		{
			r.Property(p => p.Origin)
				.HasColumnName("origin")
				.HasColumnType("catalog.media_origin")
				.IsRequired()
				.HasComment("Sorgente da cui viene servito il file; la mappa origin -> URL base vive in configurazione, non a DB.");
			r.Property(p => p.Path)
				.HasColumnName("path")
				.HasColumnType("text")
				.IsRequired()
				.HasComment("Percorso relativo rispetto alla base di origin; URL completo se origin = external.");

			r.HasIndex(p => p.Origin).HasDatabaseName("ix_catalog_media_assets_origin");
		});

		builder.Navigation(x => x.Ref).IsRequired();

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("catalog.media_kind")
			.IsRequired()
			.HasComment("Tipo d'uso del file (photo, pdf, icon).");

		builder.Property(x => x.Checksum)
			.HasColumnName("checksum")
			.HasColumnType("bytea")
			.HasComment("Hash SHA-256 del contenuto, calcolato all'upload, per deduplica e verifica integrità. NULL per i file migrati dal legacy.");

		builder.Property(x => x.ByteSize)
			.HasColumnName("byte_size")
			.HasComment("Dimensione in byte del file, nota all'upload.");

		builder.Property(x => x.ContentType)
			.HasColumnName("content_type")
			.HasMaxLength(100)
			.HasComment("MIME type (image/jpeg, image/png, image/svg+xml, application/pdf).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("Stabilimento a cui il file appartiene quando non esiste (ancora) una riga di " +
				"aggancio: foto di stabilimenti registrati o approvati che non hanno aperto la vetrina. " +
				"NULL per tutti gli asset con un utilizzatore vero, dove la riga di aggancio resta la " +
				"fonte dell'appartenenza. Serve alle policy di retention per non trattare come orfano un " +
				"file che non lo e'. ⚠️ bigint verso catalog.properties.ID, con VERA chiave esterna: e' " +
				"la convenzione dello schema, che le altre 16 tabelle di catalog seguono. Nata come uuid " +
				"verso public_id il 2026-10-03 (riferimento logico, senza FK) e corretta il 2026-10-05: " +
				"l'uuid e' la forma dei riferimenti che ATTRAVERSANO i moduli, dove una FK non si puo' " +
				"dichiarare, e qui non si attraversa niente.");

		// FK vera, non logica: la tabella e lo stabilimento stanno nello stesso schema, quindi non c'e'
		// nessun motivo per rinunciare all'integrita' referenziale.
		//
		// Restrict e non Cascade: cancellare uno stabilimento non deve portarsi via le righe di asset,
		// perche' gli OGGETTI su storage resterebbero senza nessuna riga che li nomini - cioe'
		// diventerebbero orfani, che e' il problema che questa colonna esiste per evitare. Chi cancella
		// uno stabilimento deve prima decidere che fare dei suoi file.
		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.OnDelete(DeleteBehavior.Restrict)
			.HasConstraintName("fk_catalog_media_assets_property");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete: l'asset non è cancellato finché una riga lo referenzia.");

		builder.HasIndex(x => x.Kind)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_media_assets_kind");

		// "Gli asset di questa property che nessuna riga aggancia": e' la query delle policy di retention
		// e l'unico uso previsto della colonna, quindi indice parziale sui soli valorizzati.
		builder.HasIndex(x => x.PropertyId)
			.HasFilter("property_id IS NOT NULL AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_media_assets_property");
	}
}
