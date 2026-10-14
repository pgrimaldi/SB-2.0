using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Switchboard.Domain.Entities;
using Module.Switchboard.Domain.Enums;

namespace Module.Switchboard.Infrastructure.Configurations;

public sealed class SwitchboardMediaAssetConfiguration : IEntityTypeConfiguration<SwitchboardMediaAsset>
{
	public void Configure(EntityTypeBuilder<SwitchboardMediaAsset> builder)
	{
		builder.ToTable("switchboard_media_assets", "switchboard", t => t.HasComment(
			"Catalogo locale allo schema switchboard dei file audio (prompt IVR) di uno stabilimento."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_media_assets");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico per il pannello e l'anteprima autenticata; non compare nell'URL pubblico servito a Telnyx.");

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino proprietario del file audio.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping/retention.");

		builder.Property(x => x.Origin)
			.HasColumnName("origin")
			.HasColumnType("switchboard.media_origin")
			.IsRequired()
			.HasDefaultValueSql("'s3_private'")
			.HasComment("Codice della sorgente storage; la mappa origin → URL base vive in configurazione.");

		builder.Property(x => x.Path)
			.HasColumnName("path")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Chiave relativa dell'oggetto rispetto alla base di origin, nessuna barra iniziale.");

		builder.Property(x => x.OriginalName)
			.HasColumnName("original_name")
			.HasMaxLength(255)
			.HasComment("Nome file caricato dall'operatore, solo visualizzazione.");

		builder.Property(x => x.Description)
			.HasColumnName("description")
			.HasMaxLength(255)
			.HasComment("Etichetta leggibile assegnata all'upload.");

		builder.Property(x => x.ContentType)
			.HasColumnName("content_type")
			.HasMaxLength(50)
			.IsRequired()
			.HasDefaultValue("audio/mpeg")
			.HasComment("MIME type del file finale, in pratica sempre audio/mpeg (conversione a MP3 obbligatoria).");

		builder.Property(x => x.ByteSize)
			.HasColumnName("byte_size")
			.HasComment("Dimensione in byte dell'MP3 finale.");

		builder.Property(x => x.DurationMs)
			.HasColumnName("duration_ms")
			.HasComment("Durata del prompt in millisecondi, nota alla conversione ffmpeg.");

		builder.Property(x => x.Checksum)
			.HasColumnName("checksum")
			.HasColumnType("bytea")
			.HasComment("SHA-256 del contenuto, calcolato all'upload, per deduplica e verifica integrità.");

		builder.ToTable(t => t.HasCheckConstraint(
			"ck_switchboard_media_assets_byte_size", "byte_size IS NULL OR byte_size > 0"));

		// bug reale osservato il 2026-09-16: created_at/updated_at non avevano default now() a
		// schema (nessuna convenzione EF globale lo applica, verificato in ModuleDbContextConventions
		// - solo converter UTC e query filter soft-delete) - un ETL che omette queste colonne
		// dall'INSERT (comportamento normale, per lasciare scattare il default) falliva NOT NULL.
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


		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_media_assets_public_id");

		builder.HasIndex(x => x.ConfigId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_media_assets_config");

		builder.HasIndex(x => x.DeletedAt)
			.HasFilter("deleted_at IS NOT NULL")
			.HasDatabaseName("ix_switchboard_media_assets_gc");

		builder.HasIndex(x => new { x.ConfigId, x.Checksum })
			.IsUnique()
			.HasFilter("checksum IS NOT NULL AND deleted_at IS NULL")
			.HasDatabaseName("uq_switchboard_media_assets_checksum");

		builder.HasOne(x => x.Config)
			.WithMany(c => c.MediaAssets)
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_media_assets_config")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
