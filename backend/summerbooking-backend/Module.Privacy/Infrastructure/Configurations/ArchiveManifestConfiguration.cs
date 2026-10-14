using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Privacy.Domain.Entities;

namespace Module.Privacy.Infrastructure.Configurations;

public sealed class ArchiveManifestConfiguration : IEntityTypeConfiguration<ArchiveManifest>
{
	public void Configure(EntityTypeBuilder<ArchiveManifest> builder)
	{
		builder.ToTable("privacy_archive_manifests", "privacy", t =>
		{
			t.HasComment(
				"Inventario degli archivi prodotti dalle policy archive_cold: una riga per oggetto scritto " +
				"su storage. Separata dal verbale delle corse perche' deve vivere quanto l'oggetto che " +
				"indirizza - dieci anni per gli scontrini fiscali - mentre il verbale e' operativo e scade " +
				"a 24 mesi. E' anche la fonte da cui parte la policy che cancella l'archivio alla scadenza.");

			// Un oggetto non puo' pesare zero byte, e un archivio di RIGHE deve contenerne almeno una.
			// Un file che ha seguito le righe (object_kind = 'linked_asset') non contiene righe di
			// database: la' row_count e' NULL, e dire "zero" sarebbe una cosa diversa da "non si
			// applica".
			t.HasCheckConstraint("ck_privacy_archive_manifests_counts",
				"(object_kind = 'row_archive' AND row_count IS NOT NULL AND row_count > 0 " +
				"                             AND byte_size IS NOT NULL AND byte_size > 0) OR " +
				"(object_kind = 'linked_asset' AND row_count IS NULL " +
				"                              AND (byte_size IS NULL OR byte_size > 0))");

			// L'oggetto non puo' essere stato rimosso prima di essere stato scritto.
			t.HasCheckConstraint("ck_privacy_archive_manifests_purged",
				"purged_at IS NULL OR purged_at >= archived_at");
		});

		builder.HasKey(x => x.Id).HasName("pk_privacy_archive_manifests");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico della riga di inventario.");

		builder.Property(x => x.RunId)
			.HasColumnName("run_id")
			.HasComment("Corsa che ha prodotto l'archivio. Riferimento LOGICO e non FK: le righe di " +
				"privacy_retention_runs scadono a 24 mesi, questa tabella vive molto piu' a lungo, e una " +
				"chiave esterna impedirebbe al verbale di scadere.");

		builder.Property(x => x.PolicyId)
			.HasColumnName("policy_id")
			.HasComment("Policy che ha deciso l'archiviazione.");

		builder.Property(x => x.SourceSchema)
			.HasColumnName("source_schema")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Schema della tabella le cui righe sono dentro l'archivio.");

		builder.Property(x => x.SourceTable)
			.HasColumnName("source_table")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Nome FISICO della tabella le cui righe sono dentro l'archivio.");

		builder.Property(x => x.StorageOrigin)
			.HasColumnName("storage_origin")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Host dello storage dell'archivio. ⚠️ Deve essere un'etichetta DISTINTA da quella " +
				"dei file applicativi: l'archivio sta su un bucket dedicato, e il job che svuota la coda " +
				"delle cancellazioni risolve il bucket proprio da questa etichetta.");

		builder.Property(x => x.StoragePath)
			.HasColumnName("storage_path")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Chiave dell'oggetto, nella forma " +
				"archivio/<schema>/<tabella>/<anno>/<mese>/<istante>-<policy>-<lotto>.jsonl.gz.");

		builder.Property(x => x.ByteSize)
			.HasColumnName("byte_size")
			.HasComment("Byte dell'oggetto; per un archivio di righe, compressi. NULL ammesso solo su un " +
				"file collegato di cui la dimensione non era registrata: byte_size e' annullabile su " +
				"tutte le tabelle di asset, e inventare uno zero sarebbe peggio del non saperlo.");

		builder.Property(x => x.ObjectKind)
			.HasColumnName("object_kind")
			// HasColumnType obbligatorio sulle colonne enum native, HasDefaultValueSql mai
			// HasDefaultValue:.
			.HasColumnType("privacy.archive_object_kind")
			.HasDefaultValueSql("'row_archive'::privacy.archive_object_kind")
			.HasComment("Che cosa e' l'oggetto: 'row_archive' = l'archivio JSON Lines delle righe uscite " +
				"dal database; 'linked_asset' = un file che ha SEGUITO quelle righe, per esempio il PDF " +
				"di uno scontrino archiviato con la sua ricevuta. Stanno nello stesso inventario perche' " +
				"devono avere la stessa scadenza, e una sola policy decennale li cancella entrambi. Il " +
				"default e' row_archive perche' e' quello che erano tutte le righe prima del 2026-10-06.");

		builder.Property(x => x.RowCount)
			.HasColumnName("row_count")
			.HasComment("Quante righe di database sono dentro questo oggetto. Serve a verificare che " +
				"l'archivio sia completo senza aprirlo. NULL quando object_kind = 'linked_asset': un " +
				"file che ha seguito le righe non contiene righe, e 'zero' sarebbe un'altra cosa.");

		builder.Property(x => x.CutoffAt)
			.HasColumnName("cutoff_at")
			.IsRequired()
			.HasComment("Data di taglio della policy che ha prodotto l'archivio: dice che cosa c'e' dentro.");

		builder.Property(x => x.ArchivedAt)
			.HasColumnName("archived_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Quando l'archivio e' stato scritto. E' la DECORRENZA della policy che lo cancellera'.");

		builder.Property(x => x.PurgedAt)
			.HasColumnName("purged_at")
			.HasComment("Quando l'oggetto e' stato rimosso dallo storage. NULL = c'e' ancora. La riga resta " +
				"come prova di averlo rimosso nei termini.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Inserimento della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultimo aggiornamento (in pratica: la valorizzazione di purged_at).");

		// Restrict: ritirare una policy non deve portarsi via l'inventario di cio' che ha archiviato.
		builder.HasOne(x => x.Policy)
			.WithMany()
			.HasForeignKey(x => x.PolicyId)
			.OnDelete(DeleteBehavior.Restrict)
			.HasConstraintName("fk_privacy_archive_manifests_policy");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_privacy_archive_manifests_public_id");

		// Un oggetto non si inventaria due volte.
		builder.HasIndex(x => new { x.StorageOrigin, x.StoragePath })
			.IsUnique()
			.HasDatabaseName("uq_privacy_archive_manifests_path");

		// "Quali archivi sono scaduti e ancora da rimuovere": e' la query della policy che li cancella.
		builder.HasIndex(x => x.ArchivedAt)
			.HasFilter("purged_at IS NULL")
			.HasDatabaseName("ix_privacy_archive_manifests_da_rimuovere");

		// "Dove sono finite le righe di questa tabella": la domanda di chi cerca un dato archiviato.
		builder.HasIndex(x => new { x.SourceSchema, x.SourceTable, x.CutoffAt })
			.HasDatabaseName("ix_privacy_archive_manifests_sorgente");
	}
}
