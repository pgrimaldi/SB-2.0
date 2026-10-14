using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Privacy.Domain.Entities;

namespace Module.Privacy.Infrastructure.Configurations;

public sealed class LegalHoldConfiguration : IEntityTypeConfiguration<LegalHold>
{
	public void Configure(EntityTypeBuilder<LegalHold> builder)
	{
		builder.ToTable("privacy_legal_holds", "privacy", t => t.HasComment(
			"Flag esplicito che sospende cancellazione/anonimizzazione automatica di retention per un " +
			"soggetto (ed eventualmente un singolo modulo) in presenza di un contenzioso in corso. " +
			"Impostato manualmente dall'ufficio legale, mai da un job."));

		builder.HasKey(x => x.Id).HasName("pk_privacy_legal_holds");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico per il pannello legale/amministrativo.");

		builder.Property(x => x.SubjectKind)
			.HasColumnName("subject_kind")
			.HasColumnType("privacy.subject_kind")
			.HasComment("Tipo di entità sotto hold: customer o user.");

		builder.Property(x => x.SubjectId)
			.HasColumnName("subject_id")
			.HasComment("public_id copiato per valore del soggetto sotto hold (identity_customer_customers " +
				"o identity_access_users secondo subject_kind) — nessuna FK cross-schema.");

		builder.Property(x => x.ModuleCode)
			.HasColumnName("module_code")
			.HasColumnType("privacy.module_code")
			.HasComment("Se valorizzato, l'hold si applica solo a quel modulo; se NULL, a tutti i moduli " +
				"coinvolti nel trattamento del soggetto.");

		builder.Property(x => x.Reason)
			.HasColumnName("reason")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Motivo del contenzioso/hold, obbligatorio per audit Art. 5.2 accountability.");

		builder.Property(x => x.CaseReference)
			.HasColumnName("case_reference")
			.HasMaxLength(120)
			.HasComment("Riferimento pratica legale/numero causa, per rintracciabilità presso l'ufficio legale.");

		builder.Property(x => x.PlacedByUserId)
			.HasColumnName("placed_by_user_id")
			.HasComment("public_id di identity_access_users che ha impostato l'hold, copiato — sempre un " +
				"umano dell'ufficio legale/amministrativo, mai un job; nessuna FK cross-schema.");

		builder.Property(x => x.ReleasedAt)
			.HasColumnName("released_at")
			.HasComment("Data/ora di rilascio dell'hold (chiusura del contenzioso). NULL = hold attivo.");

		builder.Property(x => x.ReleasedByUserId)
			.HasColumnName("released_by_user_id")
			.HasComment("public_id di identity_access_users che ha rilasciato l'hold, copiato — valorizzato " +
				"solo insieme a released_at, sempre un umano; nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Creazione dell'hold.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica, tipicamente il rilascio.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato funzionalmente — un hold rilasciato " +
				"non si cancella, resta come prova.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_privacy_legal_holds_public_id");

		builder.HasIndex(x => new { x.SubjectKind, x.SubjectId })
			.HasFilter("released_at IS NULL AND deleted_at IS NULL")
			.HasDatabaseName("ix_privacy_legal_holds_subject_active");

		builder.HasIndex(x => x.ModuleCode)
			.HasFilter("released_at IS NULL")
			.HasDatabaseName("ix_privacy_legal_holds_module_active");
	}
}
