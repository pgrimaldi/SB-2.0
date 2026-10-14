using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Privacy.Domain.Entities;

namespace Module.Privacy.Infrastructure.Configurations;

public sealed class ComplianceAuditEntryConfiguration : IEntityTypeConfiguration<ComplianceAuditEntry>
{
	public void Configure(EntityTypeBuilder<ComplianceAuditEntry> builder)
	{
		builder.ToTable("privacy_compliance_audit_logs", "privacy", t => t.HasComment(
			"Registro append-only di ogni azione di cancellazione/anonimizzazione/export eseguita da un " +
			"modulo proprietario sui propri dati — prova di conformità Art. 5.2 GDPR (chi/cosa/quando/perché)."));

		builder.HasKey(x => x.Id).HasName("pk_privacy_compliance_audit_logs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora dell'azione.");

		builder.Property(x => x.ModuleCode)
			.HasColumnName("module_code")
			.HasColumnType("privacy.module_code")
			.HasComment("Modulo che ha eseguito l'azione sui propri dati.");

		builder.Property(x => x.SchemaName)
			.HasColumnName("schema_name")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Schema toccato — riferimento testuale, non FK.");

		builder.Property(x => x.TableName)
			.HasColumnName("table_name")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Tabella toccata — riferimento testuale, non FK.");

		builder.Property(x => x.RecordId)
			.HasColumnName("record_id")
			.HasComment("Id interno (bigint) della riga toccata nello schema di origine — puramente " +
				"informativo, mai una FK cross-schema; la riga sorgente può non esistere più alla lettura.");

		builder.Property(x => x.FieldNames)
			.HasColumnName("field_names")
			.HasColumnType("varchar(120)[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Campi toccati dall'azione (null_fields/anonymize); vuoto per delete_row/archive_cold.");

		builder.Property(x => x.Action)
			.HasColumnName("action")
			.HasColumnType("privacy.audit_action")
			.HasComment("Azione eseguita: delete_row, null_fields, anonymize, archive_cold o exported.");

		builder.Property(x => x.RetentionPolicyId)
			.HasColumnName("retention_policy_id")
			.HasComment("Policy di retention che ha generato l'azione, se schedulata.");

		builder.Property(x => x.DataSubjectRequestId)
			.HasColumnName("data_subject_request_id")
			.HasComment("Richiesta GDPR che ha generato l'azione, se esplicita.");

		builder.Property(x => x.Reason)
			.HasColumnName("reason")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Motivazione dell'azione, ereditata dalla policy/richiesta o inserita a mano.");

		builder.Property(x => x.PerformedBy)
			.HasColumnName("performed_by")
			.HasColumnType("privacy.actor_kind")
			.HasComment("Chi ha eseguito materialmente l'azione: system_job o staff_user.");

		builder.Property(x => x.PerformedByUserId)
			.HasColumnName("performed_by_user_id")
			.HasComment("public_id di identity_access_users, copiato — valorizzato solo se performed_by = " +
				"staff_user; nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga, coincide con occurred_at.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Presente per uniformità di schema, non usato: registro append-only, non cambia " +
				"mai dopo l'inserimento.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato dall'applicazione — la sola " +
				"rimozione è un hard-delete a scadenza della propria retention (1-2 anni).");

		builder.HasIndex(x => new { x.ModuleCode, x.OccurredAt })
			.HasDatabaseName("ix_privacy_compliance_audit_logs_module_time")
			.IsDescending(false, true);

		builder.HasIndex(x => new { x.SchemaName, x.TableName, x.RecordId })
			.HasDatabaseName("ix_privacy_compliance_audit_logs_target_row");

		builder.HasIndex(x => x.DataSubjectRequestId)
			.HasFilter("data_subject_request_id IS NOT NULL")
			.HasDatabaseName("ix_privacy_compliance_audit_logs_request");

		builder.HasIndex(x => x.RetentionPolicyId)
			.HasFilter("retention_policy_id IS NOT NULL")
			.HasDatabaseName("ix_privacy_compliance_audit_logs_policy");

		// BRIN su occurred_at: registro in solo append. La scheda
		// esclude il partizionamento ("non necessario al lancio"); il BRIN e' l'opzione a costo
		// pressoche' nullo che serve subito al job di retention del modulo (24 mesi, policy che
		// privacy applica a se' stesso) e alle estrazioni per periodo in caso di ispezione.
		builder.HasIndex(x => x.OccurredAt)
			.HasMethod("brin")
			.HasDatabaseName("brin_privacy_compliance_audit_logs_occurred_at");

		builder.HasOne(x => x.RetentionPolicy)
			.WithMany()
			.HasForeignKey(x => x.RetentionPolicyId)
			.HasConstraintName("fk_privacy_compliance_audit_logs_retention_policy")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.DataSubjectRequest)
			.WithMany()
			.HasForeignKey(x => x.DataSubjectRequestId)
			.HasConstraintName("fk_privacy_compliance_audit_logs_data_subject_request")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
