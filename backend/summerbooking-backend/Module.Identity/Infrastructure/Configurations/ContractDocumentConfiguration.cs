using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Identity.Domain.Entities;

namespace Module.Identity.Infrastructure.Configurations;

public sealed class ContractDocumentConfiguration : IEntityTypeConfiguration<ContractDocument>
{
	public void Configure(EntityTypeBuilder<ContractDocument> builder)
	{
		builder.ToTable("account_contract_documents", "identity", t => t.HasComment(
			"Documenti contrattuali (addendum) pubblicati dalla piattaforma, con versionamento immutabile."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_contract_documents");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, referenziato da catalog (property.contract_document_public_id).");

		builder.Property(x => x.VersionCode)
			.HasColumnName("version_code")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Etichetta di versione dell'addendum, mostrata all'utente.");

		builder.Property(x => x.Title)
			.HasColumnName("title")
			.HasMaxLength(200)
			.HasComment("Titolo del documento per elenchi/UI.");

		builder.Property(x => x.DocumentOrigin)
			.HasColumnName("document_origin")
			.HasColumnType("identity.storage_origin")
			.IsRequired()
			.HasComment("Provenienza del PDF (storage interno o provider esterno).");

		builder.Property(x => x.DocumentPath)
			.HasColumnName("document_path")
			.HasMaxLength(500)
			.IsRequired()
			.HasComment("Percorso relativo del PDF, o URL intero se origin = external.");

		builder.Property(x => x.ContentSha256)
			.HasColumnName("content_sha256")
			.HasColumnType("char(64)")
			.HasComment("Hash SHA-256 del PDF al momento della pubblicazione.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("identity.contract_document_status")
			.IsRequired()
			.HasDefaultValueSql("'draft'")
			.HasComment("Stato del ciclo di vita del documento.");

		builder.Property(x => x.SupersedesId)
			.HasColumnName("supersedes_id")
			.HasComment("Versione precedente che questa sostituisce.");

		builder.Property(x => x.Notes)
			.HasColumnName("notes")
			.HasComment("Note libere sul documento.");

		builder.Property(x => x.PublishedAt)
			.HasColumnName("published_at")
			.HasComment("Quando il documento è passato a published.");

		builder.Property(x => x.SupersededAt)
			.HasColumnName("superseded_at")
			.HasComment("Quando questa versione è stata superata da una successiva; NULL = versione corrente. Sostituisce il flag is_current, che era derivabile da status + published_at + supersedes_id.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune.

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_identity_account_contract_documents_public_id");

		builder.HasIndex(x => x.VersionCode)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_identity_account_contract_documents_version");

		// "documento corrente da proporre": le righe non superate, filtrate poi per status = published.
		// L'unicità "al più un documento corrente pubblicato" è un indice UNIQUE su espressione costante,
		// non esprimibile in Fluent API: aggiunto a mano nella migration
		// (uq_identity_account_contract_documents_single_current).
		builder.HasIndex(x => x.Status)
			.HasFilter("superseded_at IS NULL AND deleted_at IS NULL")
			.HasDatabaseName("ix_identity_account_contract_documents_current");

		builder.HasOne(x => x.Supersedes)
			.WithMany()
			.HasForeignKey(x => x.SupersedesId)
			.HasConstraintName("fk_identity_account_contract_documents_supersedes")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
