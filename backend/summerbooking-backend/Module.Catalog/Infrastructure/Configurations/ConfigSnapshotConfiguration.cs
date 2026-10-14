using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Catalog.Domain.Entities;

namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="ConfigSnapshot"/> su catalog.config_snapshots.</summary>
public sealed class ConfigSnapshotConfiguration : IEntityTypeConfiguration<ConfigSnapshot>
{
	public void Configure(EntityTypeBuilder<ConfigSnapshot> builder)
	{
		builder.ToTable("config_snapshots", "catalog", t => t.HasComment(
			"Read-model materializzato in jsonb della configurazione di uno stabilimento, servito wholesale al frontend; sempre ricostruibile dalle tabelle catalog_* normalizzate."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_config_snapshots");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento a cui si riferisce lo snapshot.");

		builder.Property(x => x.SnapshotKind)
			.HasColumnName("snapshot_kind")
			.HasColumnType("catalog.config_snapshot_kind")
			.IsRequired()
			.HasComment("Vista di configurazione contenuta nella riga (property_management_config, beach_map, public_showcase, price_catalog, services_catalog, agreement_catalog).");

		builder.Property(x => x.Payload)
			.HasColumnName("payload")
			.HasColumnType("jsonb")
			.IsRequired()
			.HasComment("Documento JSON servito al frontend; chiavi camelCase, importi a 2 decimali, file come assetRef {origin,path} — mai URL completi.");

		builder.Property(x => x.PayloadSchema)
			.HasColumnName("payload_schema")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)1)
			.HasComment("Versione della forma del payload (contratto verso il client); incrementata solo da un cambio non retrocompatibile del serializzatore.");

		builder.Property(x => x.SourceHash)
			.HasColumnName("source_hash")
			.HasColumnType("bytea")
			.IsRequired()
			.HasComment("SHA-256 delle righe sorgente rilevanti; invariato ⇒ il rebuild salta la riscrittura del payload.");

		builder.Property(x => x.ContentHash)
			.HasColumnName("content_hash")
			.HasColumnType("bytea")
			.IsRequired()
			.HasComment("SHA-256 del payload corrente, usato come ETag HTTP per le richieste condizionali.");

		builder.Property(x => x.BuiltAt)
			.HasColumnName("built_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Quando il payload corrente è stato generato.");

		builder.Property(x => x.StaleSince)
			.HasColumnName("stale_since")
			.HasComment("Da quando un evento ha segnato lo snapshot da rigenerare, con il rebuild non ancora avvenuto; NULL = aggiornato. Sostituisce il flag is_stale: la soglia di avviso/rifiuto della lettura dipende da quanto è vecchia l'invalidazione, non dal fatto che esista (rilevante solo per kind ad aggiornamento asincrono).");

		builder.ToTable(t => t.HasCheckConstraint(
			"ck_catalog_config_snapshots_stale_after_build",
			"stale_since IS NULL OR stale_since >= built_at"));

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga (coincide con built_at a ogni rebuild che cambia qualcosa).");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete presente per uniformità di progetto; non usato funzionalmente su questa tabella di puro read-model.");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_config_snapshots_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.PropertyId, x.SnapshotKind })
			.IsUnique()
			.HasDatabaseName("uq_catalog_config_snapshots_property_kind");

		builder.HasIndex(x => x.StaleSince)
			.HasFilter("stale_since IS NOT NULL")
			.HasDatabaseName("ix_catalog_config_snapshots_stale");

		builder.HasIndex(x => x.BuiltAt)
			.HasDatabaseName("ix_catalog_config_snapshots_built_at");
	}
}
