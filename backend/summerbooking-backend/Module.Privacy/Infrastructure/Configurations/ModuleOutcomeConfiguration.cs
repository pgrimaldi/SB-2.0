using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Privacy.Domain.Entities;

namespace Module.Privacy.Infrastructure.Configurations;

public sealed class ModuleOutcomeConfiguration : IEntityTypeConfiguration<ModuleOutcome>
{
	public void Configure(EntityTypeBuilder<ModuleOutcome> builder)
	{
		builder.ToTable("privacy_data_subject_request_module_outcomes", "privacy", t => t.HasComment(
			"Esito applicato da un singolo modulo a una richiesta dell'interessato: una riga per ogni " +
			"modulo coinvolto, dettaglio che la testata riassume in status."));

		builder.HasKey(x => x.Id).HasName("pk_privacy_data_subject_request_module_outcomes");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.RequestId)
			.HasColumnName("request_id")
			.HasComment("Richiesta a cui l'esito appartiene.");

		builder.Property(x => x.ModuleCode)
			.HasColumnName("module_code")
			.HasColumnType("privacy.module_code")
			.HasComment("Modulo/dominio proprietario dei dati che ha risposto (vocabolario chiuso dei " +
				"domini applicativi del progetto).");

		builder.Property(x => x.Outcome)
			.HasColumnName("outcome")
			.HasColumnType("privacy.module_outcome_result")
			.HasDefaultValueSql("'pending'")
			.HasComment("Esito applicato da quel modulo: pending, not_applicable, erased, anonymized, " +
				"retained_legal_hold, retained_fiscal_obligation, exported o failed.");

		builder.Property(x => x.OutcomeDetail)
			.HasColumnName("outcome_detail")
			.HasColumnType("text")
			.HasComment("Motivazione leggibile mostrata al richiedente nella risposta parzialmente accolta.");

		builder.Property(x => x.RetainedUntil)
			.HasColumnName("retained_until")
			.HasColumnType("date")
			.HasComment("Data fino a cui il vincolo di conservazione è previsto durare, quando calcolabile " +
				"(tipicamente retained_fiscal_obligation); abilita il job di ri-tentativo automatico.");

		builder.Property(x => x.RespondedAt)
			.HasColumnName("responded_at")
			.HasComment("Data/ora in cui il modulo ha applicato l'esito e pubblicato l'evento di risposta; " +
				"NULL finché outcome = pending.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga, alla pubblicazione dell'evento verso il modulo.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica: la risposta del modulo, o un ri-tentativo su un esito trattenuto.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato funzionalmente — stesso ciclo di " +
				"vita da registro di conformità della testata.");

		builder.HasIndex(x => new { x.RequestId, x.ModuleCode })
			.IsUnique()
			.HasDatabaseName("uq_privacy_data_subject_request_module_outcomes_request_module");

		builder.HasIndex(x => x.Outcome)
			.HasFilter("outcome = 'pending'")
			.HasDatabaseName("ix_privacy_data_subject_request_module_outcomes_pending");

		builder.HasIndex(x => new { x.ModuleCode, x.Outcome })
			.HasDatabaseName("ix_privacy_data_subject_request_module_outcomes_module");

		builder.HasOne(x => x.Request)
			.WithMany(r => r.ModuleOutcomes)
			.HasForeignKey(x => x.RequestId)
			.HasConstraintName("fk_privacy_data_subject_request_module_outcomes_request")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
