using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class ContractAcceptanceEventConfiguration : IEntityTypeConfiguration<ContractAcceptanceEvent>
{
	public void Configure(EntityTypeBuilder<ContractAcceptanceEvent> builder)
	{
		builder.ToTable("account_contract_acceptances", "identity", t => t.HasComment(
			"Registro append-only delle accettazioni/revoche dei documenti contrattuali da parte degli stabilimenti."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_contract_acceptances");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id della property (catalog) che ha eseguito l'azione, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.ContractDocumentId)
			.HasColumnName("contract_document_id")
			.IsRequired()
			.HasComment("Documento/versione oggetto dell'evento.");

		builder.Property(x => x.EventType)
			.HasColumnName("event_type")
			.HasColumnType("identity.contract_acceptance_event")
			.IsRequired()
			.HasComment("Tipo di evento (accettazione o revoca).");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora dell'azione.");

		builder.Property(x => x.ActorUserId)
			.HasColumnName("actor_user_id")
			.IsRequired()
			.HasComment("Account (admin/superadmin) che ha eseguito l'azione.");

		builder.Property(x => x.DocumentVersionCode)
			.HasColumnName("document_version_code")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("version_code del documento copiato al momento dell'evento (snapshot leggibile).");

		builder.Property(x => x.DocumentSha256)
			.HasColumnName("document_sha256")
			.HasColumnType("char(64)")
			.HasComment("Hash SHA-256 del PDF accettato, come prova del contenuto esatto accettato.");

		builder.Property(x => x.IpAddress)
			.HasColumnName("ip_address")
			.HasColumnType("inet")
			.HasComment("IP del client al momento dell'azione (audit legale); dato personale a retention limitata.");

		builder.Property(x => x.UserAgent)
			.HasColumnName("user_agent")
			.HasMaxLength(400)
			.HasComment("User-agent del client (rafforza il valore probatorio).");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune; deleted_at presente per uniformità, non usato funzionalmente (registro legale).

		builder.HasIndex(x => new { x.PropertyId, x.ContractDocumentId, x.OccurredAt })
			.HasDatabaseName("ix_identity_account_contract_acceptances_property_doc");

		builder.HasIndex(x => x.ContractDocumentId)
			.HasDatabaseName("ix_identity_account_contract_acceptances_document");

		builder.HasOne(x => x.ContractDocument)
			.WithMany()
			.HasForeignKey(x => x.ContractDocumentId)
			.HasConstraintName("fk_identity_account_contract_acceptances_document")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.ActorUser)
			.WithMany()
			.HasForeignKey(x => x.ActorUserId)
			.HasConstraintName("fk_identity_account_contract_acceptances_actor")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
