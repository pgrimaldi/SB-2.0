using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Privacy.Domain.Entities;

namespace Module.Privacy.Infrastructure.Configurations;

public sealed class RetentionRunConfiguration : IEntityTypeConfiguration<RetentionRun>
{
	public void Configure(EntityTypeBuilder<RetentionRun> builder)
	{
		builder.ToTable("privacy_retention_runs", "privacy", t =>
		{
			t.HasComment(
				"Verbale di una policy di retention in una corsa: righe trovate, righe toccate, dove ha " +
				"archiviato, e se e' fallita perche'. Una riga per (corsa, policy). Serve a tre cose che " +
				"nessun'altra tabella copre: ritrovare le righe archiviate su storage, raccogliere gli " +
				"errori senza fermare il motore, e leggere il report di dry-run prima di autorizzare " +
				"l'esecuzione reale.");

			// Una corsa finisce dopo che e' cominciata. Uguale ammesso: una policy saltata dura zero.
			t.HasCheckConstraint("ck_privacy_retention_runs_times", "finished_at >= started_at");

			// I conteggi non sono negativi, e non si possono toccare piu' righe di quante se ne trovino.
			t.HasCheckConstraint("ck_privacy_retention_runs_counts",
				"matched_rows >= 0 AND affected_rows >= 0 AND affected_rows <= matched_rows");

			// In prova non si scrive niente, per definizione.
			t.HasCheckConstraint("ck_privacy_retention_runs_dry_run",
				"NOT is_dry_run OR affected_rows = 0");

			// Un fallimento o un salto devono dire perche'. Un successo non deve.
			t.HasCheckConstraint("ck_privacy_retention_runs_detail",
				"(status = 'failed' AND detail IS NOT NULL) OR status <> 'failed'");
		});

		builder.HasKey(x => x.Id).HasName("pk_privacy_retention_runs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico della riga di verbale.");

		builder.Property(x => x.RunId)
			.HasColumnName("run_id")
			.HasComment("Raggruppa tutte le righe prodotte dalla stessa esecuzione, su tutti gli schemi. " +
				"E' la chiave con cui si legge un report di dry-run per intero.");

		builder.Property(x => x.PolicyId)
			.HasColumnName("policy_id")
			.HasComment("Policy valutata.");

		builder.Property(x => x.SchemaName)
			.HasColumnName("schema_name")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Schema del modulo proprietario, copiato dalla policy.");

		builder.Property(x => x.TableName)
			.HasColumnName("table_name")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Nome FISICO della tabella, come risolto dal motore: in privacy e switchboard ha " +
				"il prefisso di modulo, mentre la policy porta la forma canonica senza prefisso.");

		builder.Property(x => x.Action)
			.HasColumnName("action")
			.HasColumnType("privacy.retention_action")
			.IsRequired()
			.HasComment("Azione della policy, copiata: il verbale deve restare leggibile anche se la " +
				"policy viene poi modificata.");

		builder.Property(x => x.CutoffAt)
			.HasColumnName("cutoff_at")
			.HasComment("Data di taglio calcolata. NULL quando la policy e' stata saltata prima di calcolarla.");

		builder.Property(x => x.IsDryRun)
			.HasColumnName("is_dry_run")
			.IsRequired()
			.HasComment("La corsa non ha scritto niente: o era una prova, o la policy e' is_dry_run_only.");

		builder.Property(x => x.MatchedRows)
			.HasColumnName("matched_rows")
			.IsRequired()
			.HasDefaultValue(0L)
			.HasComment("Righe oltre la data di taglio. E' il numero che si legge nel report di dry-run.");

		builder.Property(x => x.AffectedRows)
			.HasColumnName("affected_rows")
			.IsRequired()
			.HasDefaultValue(0L)
			.HasComment("Righe davvero cancellate, azzerate o anonimizzate. Zero in prova, anche con matched_rows alto.");

		builder.Property(x => x.ArchiveOrigin)
			.HasColumnName("archive_origin")
			.HasMaxLength(32)
			.HasComment("Host dello storage di archivio. Testo e non enum: e' lo stesso motivo di " +
				"privacy_file_deletions.storage_origin.");

		builder.Property(x => x.ArchivePath)
			.HasColumnName("archive_path")
			.HasColumnType("text")
			.HasComment("Chiave dell'oggetto di archivio, o piu' chiavi separate da spazio se l'export e' " +
				"andato in piu' lotti. ⚠️ ECO OPERATIVO, NON L'INVENTARIO: questa riga scade a 24 mesi, " +
				"mentre gli archivi vivono fino a dieci anni. L'inventario autorevole di dove sono gli " +
				"archivi e' privacy_archive_manifests, una riga per oggetto, che vive quanto l'oggetto " +
				"che indirizza. Qui la chiave c'e' solo per leggere il verbale di una corsa senza fare " +
				"una join, e dopo 24 mesi non c'e' piu'.");

		builder.Property(x => x.ArchiveByteSize)
			.HasColumnName("archive_byte_size")
			.HasComment("Byte scritti sull'archivio, compressi. Come archive_path: eco operativo, il dato " +
				"autorevole per oggetto sta su privacy_archive_manifests.byte_size.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("privacy.retention_run_status")
			.IsRequired()
			.HasDefaultValueSql("'succeeded'")
			.HasComment("succeeded, skipped (ragione dichiarata e legittima, non un errore) o failed.");

		builder.Property(x => x.Detail)
			.HasColumnName("detail")
			.HasColumnType("text")
			.HasComment("Il motivo del salto, o il testo dell'errore con le sue cause. NULL se e' andata liscia.");

		builder.Property(x => x.StartedAt)
			.HasColumnName("started_at")
			.IsRequired()
			.HasComment("Istante di valutazione della corsa, lo stesso per tutte le policy della corsa: " +
				"due policy della stessa esecuzione non devono vedere due 'adesso' diversi.");

		builder.Property(x => x.FinishedAt)
			.HasColumnName("finished_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Fine della valutazione di questa policy.");

		builder.Property(x => x.DurationMs)
			.HasColumnName("duration_ms")
			.IsRequired()
			.HasDefaultValue(0L)
			.HasComment("Quanto e' durata questa policy. Serve a capire quali non staranno in una finestra notturna.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Inserimento della riga.");

		// Restrict: ritirare una policy non deve portarsi via la prova di cosa ha fatto.
		builder.HasOne(x => x.Policy)
			.WithMany()
			.HasForeignKey(x => x.PolicyId)
			.OnDelete(DeleteBehavior.Restrict)
			.HasConstraintName("fk_privacy_retention_runs_policy");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_privacy_retention_runs_public_id");

		// "Leggimi il report di questa corsa": e' la query del pannello e di chi autorizza.
		builder.HasIndex(x => new { x.RunId, x.SchemaName })
			.HasDatabaseName("ix_privacy_retention_runs_run");

		// "Che cosa e' andato male": l'indice parziale tiene fuori le righe riuscite, che sono la
		// maggioranza, e rende immediata la domanda per cui questa tabella esiste.
		builder.HasIndex(x => x.FinishedAt)
			.HasFilter("status = 'failed'")
			.HasDatabaseName("ix_privacy_retention_runs_failed");

		// "Com'e' andata questa policy nel tempo": serve a vedere se una regola ha smesso di trovare righe.
		builder.HasIndex(x => new { x.PolicyId, x.FinishedAt })
			.HasDatabaseName("ix_privacy_retention_runs_policy");
	}
}
