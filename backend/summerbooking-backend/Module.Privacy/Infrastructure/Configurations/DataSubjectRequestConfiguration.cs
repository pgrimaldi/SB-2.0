using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Privacy.Domain.Entities;

namespace Module.Privacy.Infrastructure.Configurations;

public sealed class DataSubjectRequestConfiguration : IEntityTypeConfiguration<DataSubjectRequest>
{
	public void Configure(EntityTypeBuilder<DataSubjectRequest> builder)
	{
		builder.ToTable("privacy_data_subject_requests", "privacy", t => t.HasComment(
			"Testata di una richiesta dell'interessato (Art. 15/17/20 GDPR): cancellazione o export. " +
			"Non contiene mai un dato personale del soggetto, solo riferimenti e metadati di workflow."));

		builder.HasKey(x => x.Id).HasName("pk_privacy_data_subject_requests");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico esposto nell'URL della richiesta e nella UI self-service del cliente.");

		builder.Property(x => x.RequestKind)
			.HasColumnName("request_kind")
			.HasColumnType("privacy.request_kind")
			.HasComment("Tipo di richiesta: erasure (Art. 17) o export (Art. 15/20).");

		builder.Property(x => x.SubjectKind)
			.HasColumnName("subject_kind")
			.HasColumnType("privacy.subject_kind")
			.HasComment("Tipo di entità interessata: customer (rubrica C5) o user (account di login C5).");

		builder.Property(x => x.SubjectId)
			.HasColumnName("subject_id")
			.HasComment("public_id copiato per valore dell'interessato (identity_customer_customers o " +
				"identity_access_users secondo subject_kind) — nessuna FK cross-schema, nessun dato personale.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties al momento della richiesta, copiato — NULL per " +
				"account user di livello piattaforma; nessuna FK cross-schema.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("privacy.request_status")
			.HasDefaultValueSql("'pending'")
			.HasComment("Stato di avanzamento, consolidato dal saga leggendo gli esiti per modulo.");

		builder.Property(x => x.RequestedByKind)
			.HasColumnName("requested_by_kind")
			.HasColumnType("privacy.requester_kind")
			.HasComment("Chi ha originato la richiesta: data_subject (self-service), staff o systemadmin.");

		builder.Property(x => x.RequestedByUserId)
			.HasColumnName("requested_by_user_id")
			.HasComment("public_id di identity_access_users che ha inserito la richiesta, copiato — NULL se " +
				"da canale senza account; nessuna FK cross-schema.");

		builder.Property(x => x.RequestedAt)
			.HasColumnName("requested_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della richiesta — decorrenza del termine Art. 12.3.");

		builder.Property(x => x.DueAt)
			.HasColumnName("due_at")
			.HasComment("Scadenza per rispondere (Art. 12.3: 30 giorni, estendibile a 90 con motivazione " +
				"esplicita), valorizzata dall'applicazione all'inserimento.");

		builder.Property(x => x.CompletedAt)
			.HasColumnName("completed_at")
			.HasComment("Data/ora in cui lo stato è diventato completed, partially_completed o rejected.");

		builder.Property(x => x.RejectionReason)
			.HasColumnName("rejection_reason")
			.HasColumnType("text")
			.HasComment("Motivazione quando status = rejected (es. identità del richiedente non verificabile).");

		builder.Property(x => x.ExportArtifactOrigin)
			.HasColumnName("export_artifact_origin")
			.HasColumnType("privacy.export_artifact_origin")
			.HasComment("Sorgente di storage del file di export generato (solo request_kind = export, " +
				"status = completed); oggi solo s3_private, accesso via presigned URL mai persistito.");

		builder.Property(x => x.ExportArtifactPath)
			.HasColumnName("export_artifact_path")
			.HasMaxLength(1024)
			.HasComment("Percorso del file di export nel bucket privato.");

		builder.Property(x => x.ExportArtifactExpiresAt)
			.HasColumnName("export_artifact_expires_at")
			.HasComment("Scadenza del file fisico sul bucket (lifecycle policy S3); la riga resta come " +
				"prova anche dopo la rimozione del file.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga, coincide con requested_at.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultima transizione di stato.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato funzionalmente: la riga è un " +
				"registro di conformità permanente; l'unica rimozione è un hard-delete a scadenza retention.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_privacy_data_subject_requests_public_id");

		builder.HasIndex(x => new { x.SubjectKind, x.SubjectId })
			.HasDatabaseName("ix_privacy_data_subject_requests_subject");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("property_id IS NOT NULL")
			.HasDatabaseName("ix_privacy_data_subject_requests_property");

		builder.HasIndex(x => x.Status)
			.HasFilter("status IN ('pending','in_progress')")
			.HasDatabaseName("ix_privacy_data_subject_requests_open");

		builder.HasIndex(x => x.DueAt)
			.HasFilter("status IN ('pending','in_progress')")
			.HasDatabaseName("ix_privacy_data_subject_requests_due");

		// Collezione ModuleOutcomes: FK reale, configurata lato figlio
		// (privacy_data_subject_request_module_outcomes.request_id).
	}
}
