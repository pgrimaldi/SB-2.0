using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Chatbot.Domain.Entities;

namespace Module.Chatbot.Infrastructure.Configurations;

public sealed class LlmProviderConfiguration : IEntityTypeConfiguration<LlmProvider>
{
	public void Configure(EntityTypeBuilder<LlmProvider> builder)
	{
		builder.ToTable("llm_providers", "chatbot", t => t.HasComment(
			"Catalogo dei modelli/provider LLM utilizzabili dal modulo Chatbot per generare le risposte — una riga per modello selezionabile, non per vendor."));

		builder.HasKey(x => x.Id).HasName("pk_chatbot_llm_providers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Identificativo tecnico del modello passato al SDK del vendor (es. \"gemini-3-flash-preview\"); chiave applicativa, non solo la PK interna.");

		builder.Property(x => x.Vendor)
			.HasColumnName("vendor")
			.HasMaxLength(32)
			.IsRequired()
			.HasComment("Fornitore del modello (es. \"google\"); stringa libera, vocabolario aperto e in crescita — non enum.");

		builder.Property(x => x.DisplayName)
			.HasColumnName("display_name")
			.HasMaxLength(120)
			.HasComment("Etichetta leggibile per il pannello di configurazione; solo presentazione.");

		builder.Property(x => x.IsSelectable)
			.HasColumnName("is_selectable")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Se false, il modello non è proponibile per nuove selezioni; le righe storiche che lo referenziano restano valide.");

		builder.Property(x => x.RetiresOn)
			.HasColumnName("retires_on")
			.HasComment("Data oltre la quale il modello è considerato ritirato dal vendor; puramente informativa — l'interruttore applicato è is_selectable.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione del catalogo provider.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: rimozione di un provider mai usato. Un provider già usato si ritira con is_selectable = false, non si cancella.");

		builder.HasIndex(x => x.Code)
			.IsUnique()
			.HasDatabaseName("uq_chatbot_llm_providers_code");

		builder.HasIndex(x => x.Vendor)
			.HasFilter("is_selectable AND deleted_at IS NULL")
			.HasDatabaseName("ix_chatbot_llm_providers_selectable");
	}
}
