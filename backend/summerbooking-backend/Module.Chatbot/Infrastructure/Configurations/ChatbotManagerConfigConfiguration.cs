using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Chatbot.Domain.Entities;

namespace Module.Chatbot.Infrastructure.Configurations;

public sealed class ChatbotManagerConfigConfiguration : IEntityTypeConfiguration<ChatbotManagerConfig>
{
	public void Configure(EntityTypeBuilder<ChatbotManagerConfig> builder)
	{
		builder.ToTable("manager_configs", "chatbot", t =>
		{
			t.HasComment("Limiti di utilizzo e preferenza di provider LLM del chatbot per stabilimento — una riga per property_id.");
			t.HasCheckConstraint("ck_chatbot_manager_configs_daily_limit", "daily_request_limit > 0");
			t.HasCheckConstraint("ck_chatbot_manager_configs_history_limit", "history_page_size_limit > 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_chatbot_manager_configs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties (stabilimento a cui si applica la configurazione), copiato — nessuna FK cross-schema; sempre dal claim del token, mai dal body.");

		builder.Property(x => x.DailyRequestLimit)
			.HasColumnName("daily_request_limit")
			.IsRequired()
			.HasDefaultValue(10)
			.HasComment("Numero massimo di richieste al chatbot consentite per giorno solare (Europe/Rome), per stabilimento — limite anti-abuso/controllo costi.");

		builder.Property(x => x.HistoryPageSizeLimit)
			.HasColumnName("history_page_size_limit")
			.IsRequired()
			.HasDefaultValue(10)
			.HasComment("Tetto massimo di voci di chatbot.conversation_turns restituite da /history per stabilimento, anche se il chiamante ne richiede di più.");

		builder.Property(x => x.LlmProviderId)
			.HasColumnName("llm_provider_id")
			.HasComment("Override del provider/modello LLM per questo stabilimento; NULL = usa il default di piattaforma.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, operatore che ha creato/modificato manualmente la configurazione; NULL se provisionata automaticamente dal sistema — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della configurazione.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di ultima modifica.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: disattivazione del modulo chatbot per lo stabilimento.");

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_chatbot_manager_configs_property");

		builder.HasIndex(x => x.LlmProviderId)
			.HasFilter("llm_provider_id IS NOT NULL")
			.HasDatabaseName("ix_chatbot_manager_configs_llm_provider");

		builder.HasOne(x => x.LlmProvider)
			.WithMany()
			.HasForeignKey(x => x.LlmProviderId)
			.HasConstraintName("fk_chatbot_manager_configs_llm_provider")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
