using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Privacy.Domain.Entities;

namespace Module.Privacy.Infrastructure.Configurations;

public sealed class FileDeletionConfiguration : IEntityTypeConfiguration<FileDeletion>
{
	public void Configure(EntityTypeBuilder<FileDeletion> builder)
	{
		builder.ToTable("privacy_file_deletions", "privacy", t =>
		{
			t.HasComment(
				"Coda delle AZIONI su storage decise da una policy di retention o a mano: cancellare un " +
				"oggetto (delete_object) oppure spostarlo su una classe piu' fredda (archive_object). " +
				"Serve perche' un'operazione su S3 non partecipa alla transazione del database: senza coda " +
				"un errore di rete lascia il database e lo storage in disaccordo. Agisce solo sulla copia " +
				"AWS; la copia locale di sicurezza non viene toccata. ⚠️ IL NOME DICE MENO DI QUELLO CHE " +
				"FA: nasce il 2026-10-03 quando l'unica azione era la cancellazione, e dal 2026-10-05 " +
				"porta anche archive_object, che non cancella niente. Guardare la colonna action.");

			// Delle sei etichette di retention_action solo due agiscono su un oggetto di storage: le
			// altre quattro (delete_row, null_fields, anonymize, archive_cold) agiscono sulle righe e qui
			// non vogliono dire niente. Il vincolo evita che un INSERT sbagliato metta in coda una riga
			// che il job non saprebbe eseguire.
			t.HasCheckConstraint("ck_privacy_file_deletions_action",
				"action IN ('delete_object', 'archive_object')");

			// O si sa quale riga usava il file, o non si sa: mai mezza informazione. I QR e gli orfani non
			// hanno nessuna riga, ed e' il motivo per cui le tre colonne sono nullable.
			t.HasCheckConstraint("ck_privacy_file_deletions_asset",
				"(asset_schema IS NULL AND asset_table IS NULL AND asset_id IS NULL) " +
				"OR (asset_schema IS NOT NULL AND asset_table IS NOT NULL AND asset_id IS NOT NULL)");

			// Un tentativo fallito deve lasciare traccia di quando: le due colonne vivono insieme.
			t.HasCheckConstraint("ck_privacy_file_deletions_last_error",
				"(last_error IS NULL) = (last_error_at IS NULL)");
		});

		builder.HasKey(x => x.Id).HasName("pk_privacy_file_deletions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico per il pannello SystemAdmin/DPO e per correlare il report di dry-run con l'esecuzione.");

		builder.Property(x => x.AssetSchema)
			.HasColumnName("asset_schema")
			.HasMaxLength(63)
			.HasComment("Schema del modulo proprietario della riga che nominava il file (catalog, fiskaly, " +
				"notification, switchboard). NULL per gli oggetti che nessuna riga nomina: QR e orfani.");

		builder.Property(x => x.AssetTable)
			.HasColumnName("asset_table")
			.HasMaxLength(63)
			.HasComment("Tabella di asset nella forma canonica relativa allo schema (media_assets), come in " +
				"privacy_retention_policies.table_name. NULL insieme alle altre due.");

		builder.Property(x => x.AssetId)
			.HasColumnName("asset_id")
			.HasComment("Chiave della riga di asset. NULL insieme alle altre due.");

		builder.Property(x => x.StorageOrigin)
			.HasColumnName("storage_origin")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Host dello storage come era al momento della decisione. Testo e non enum nativo: i " +
				"quattro moduli di asset hanno enum media_origin con etichette diverse fra loro, e questa " +
				"coda li raccoglie tutti.");

		builder.Property(x => x.StoragePath)
			.HasColumnName("storage_path")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Chiave dell'oggetto congelata al momento della decisione: se il path sulla riga di " +
				"asset cambia dopo, si cancella comunque l'oggetto che era stato valutato.");

		builder.Property(x => x.ByteSize)
			.HasColumnName("byte_size")
			.HasComment("Dimensione nota dell'oggetto, per rendicontare lo spazio liberato. NULL se il metadato non era stato calcolato.");

		builder.Property(x => x.Action)
			.HasColumnName("action")
			// HasColumnType obbligatorio sulle colonne enum native, e HasDefaultValueSql mai
			// HasDefaultValue:.
			.HasColumnType("privacy.retention_action")
			.HasDefaultValueSql("'delete_object'::privacy.retention_action")
			.HasComment("Che cosa va fatto all'oggetto: delete_object lo rimuove, archive_object lo sposta " +
				"su una classe di storage piu' fredda lasciandolo dov'e'. ⚠️ Senza questa colonna le due " +
				"azioni erano indistinguibili in coda, e il job avrebbe CANCELLATO i PDF degli scontrini " +
				"fiscali che la policy a 36 mesi vuole soltanto raffreddare. Il default e' delete_object " +
				"perche' e' quello che erano tutte le righe prima del 2026-10-05. La classe di " +
				"destinazione NON sta qui: sta in Retention:Deletion:Targets[].ArchiveStorageClass, " +
				"perche' e' una decisione di costo e dipende dall'ambiente.");

		builder.Property(x => x.PolicyId)
			.HasColumnName("policy_id")
			.HasComment("Policy che ha deciso la cancellazione. NULL = cancellazione manuale di un amministratore.");

		builder.Property(x => x.Reason)
			.HasColumnName("reason")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Perche' l'oggetto e' in coda, in forma leggibile da chi apre il pannello. Si regge " +
				"da solo: non rimanda a documenti interni ne' a chi ha deciso il termine.");

		builder.Property(x => x.DecidedAt)
			.HasColumnName("decided_at")
			.HasDefaultValueSql("now()")
			.HasComment("Quando la policy (o la persona) ha deciso la cancellazione.");

		builder.Property(x => x.ExecutedAt)
			.HasColumnName("executed_at")
			.HasComment("Quando l'oggetto e' stato davvero rimosso dallo storage. NULL = ancora da fare: e' il filtro della coda.");

		builder.Property(x => x.Attempts)
			.HasColumnName("attempts")
			.HasDefaultValue((short)0)
			.HasComment("Tentativi di rimozione gia' fatti. E' l'unica cosa che distingue un lavoro non ancora iniziato da uno che non riesce.");

		builder.Property(x => x.LastError)
			.HasColumnName("last_error")
			.HasColumnType("text")
			.HasComment("Ultimo errore riportato dallo storage, testuale.");

		builder.Property(x => x.LastErrorAt)
			.HasColumnName("last_error_at")
			.HasComment("Quando si e' verificato l'ultimo errore.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga di coda.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultimo aggiornamento (tentativo, errore, esecuzione).");

		// FK verso la policy che ha deciso, con Restrict: una policy ritirata non deve portarsi via la
		// prova di cosa ha cancellato.
		builder.HasOne<RetentionPolicy>()
			.WithMany()
			.HasForeignKey(x => x.PolicyId)
			.OnDelete(DeleteBehavior.Restrict)
			.HasConstraintName("fk_privacy_file_deletions_policy");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_privacy_file_deletions_public_id");

		// La coda da lavorare: solo le righe non ancora eseguite.
		builder.HasIndex(x => x.DecidedAt)
			.HasFilter("executed_at IS NULL")
			.HasDatabaseName("ix_privacy_file_deletions_pending");

		// Lo stesso oggetto non va messo in coda due volte mentre e' ancora da fare. Dopo l'esecuzione
		// invece puo' tornare: un path riusato e' un oggetto nuovo.
		builder.HasIndex(x => new { x.StorageOrigin, x.StoragePath })
			.IsUnique()
			.HasFilter("executed_at IS NULL")
			.HasDatabaseName("uq_privacy_file_deletions_pending_path");
	}
}
