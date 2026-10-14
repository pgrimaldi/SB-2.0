using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Chatbot.Domain.Entities;
using Module.Chatbot.Domain.Enums;

namespace Module.Chatbot.Infrastructure.Configurations;

public sealed class ChatbotConversationTurnConfiguration : IEntityTypeConfiguration<ChatbotConversationTurn>
{
	public void Configure(EntityTypeBuilder<ChatbotConversationTurn> builder)
	{
		builder.ToTable("conversation_turns", "chatbot", t =>
		{
			t.HasComment("Uno scambio completo domanda/risposta tra un utente (operatore o, in futuro, cliente finale) e l'assistente LLM.");
			t.HasCheckConstraint("ck_chatbot_conversation_turns_channel_actor", "channel <> 'manager' OR actor_user_id IS NOT NULL");
			t.HasCheckConstraint("ck_chatbot_conversation_turns_correlator", "actor_user_id IS NOT NULL OR actor_session_id IS NOT NULL OR channel = 'manager'");
			t.HasCheckConstraint("ck_chatbot_conversation_turns_status_output", "status <> 'failed' OR output_text IS NULL");
			t.HasCheckConstraint("ck_chatbot_conversation_turns_input_length", "char_length(input_text) <= 5000");
		});

		builder.HasKey(x => x.Id).HasName("pk_chatbot_conversation_turns");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7) restituito dalle API (/chat → historyId, /history → id di ogni voce) al posto dell'id interno sequenziale.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_chatbot_conversation_turns_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties (stabilimento del turno), copiato — nessuna FK cross-schema; sempre dal claim del token.");

		builder.Property(x => x.Channel)
			.HasColumnName("channel")
			.HasColumnType("chatbot.channel_kind")
			.IsRequired()
			.HasDefaultValueSql("'manager'")
			.HasComment("Canale applicativo da cui parte il turno: manager (gestionale, autenticato) o customer (futuro assistente lato cliente finale, anche anonimo).");

		builder.Property(x => x.ActorUserId)
			.HasColumnName("actor_user_id")
			.HasComment("public_id di identity.access_users, operatore autenticato che ha inviato il messaggio; obbligatorio per channel = manager — copiato, nessuna FK cross-schema.");

		builder.Property(x => x.ActorSessionId)
			.HasColumnName("actor_session_id")
			.HasMaxLength(64)
			.HasComment("Correlatore opaco generato lato client per turni anonimi del canale customer (actor_user_id NULL); non è un dato personale.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("chatbot.turn_status")
			.IsRequired()
			.HasDefaultValueSql("'answered'")
			.HasComment("Esito del turno: answered, oppure failed quando il provider non ha prodotto una risposta valida (mai dedotto da una stringa vuota).");

		builder.Property(x => x.LlmProviderId)
			.HasColumnName("llm_provider_id")
			.HasComment("Provider/modello (chatbot.llm_providers) che ha effettivamente generato la risposta di questo turno; assente sui turni failed prima della selezione del provider.");

		builder.Property(x => x.LlmModelSnapshot)
			.HasColumnName("llm_model_snapshot")
			.HasMaxLength(64)
			.HasComment("Identificativo del modello riportato dal provider al momento della generazione — congelato, non riflette una rinomina/aggiornamento successivo di llm_providers.");

		builder.Property(x => x.ResponseLanguage)
			.HasColumnName("response_language")
			.HasMaxLength(10)
			.HasComment("Lingua della risposta generata (codice cultura, es. it-IT), da preferenza utente nota — non da un'euristica sul singolo messaggio.");

		builder.Property(x => x.InputText)
			.HasColumnName("input_text")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Messaggio inviato dall'utente — testo libero, può contenere dati personali di terzi.");

		builder.Property(x => x.OutputText)
			.HasColumnName("output_text")
			.HasColumnType("text")
			.HasComment("Risposta generata dal provider LLM; NULL quando status = failed, mai stringa vuota — stessa cautela GDPR di input_text.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Istante del messaggio; colonna di riferimento per il conteggio giornaliero del rate limit (non updated_at, a differenza del legacy).");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica; righe di fatto immutabili dopo la creazione.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: retention o cancellazione su richiesta.");

		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_chatbot_conversation_turns_property_time");

		builder.HasIndex(x => new { x.PropertyId, x.Channel, x.CreatedAt })
			.HasDatabaseName("ix_chatbot_conversation_turns_rate_limit");

		builder.HasIndex(x => new { x.ActorUserId, x.CreatedAt })
			.HasFilter("actor_user_id IS NOT NULL")
			.HasDatabaseName("ix_chatbot_conversation_turns_actor_time");

		builder.HasIndex(x => new { x.ActorSessionId, x.CreatedAt })
			.HasFilter("actor_session_id IS NOT NULL")
			.HasDatabaseName("ix_chatbot_conversation_turns_session");

		builder.HasIndex(x => x.LlmProviderId)
			.HasFilter("llm_provider_id IS NOT NULL")
			.HasDatabaseName("ix_chatbot_conversation_turns_provider");

		// BRIN su created_at: storico in solo append. Gli indici
		// B-tree qui sopra hanno tutti property_id come prima colonna e non servono al job di
		// retention, che scansiona per sola data (12 mesi); il BRIN copre quel caso a costo
		// pressoche' nullo ed e' gia' pronto ora che la finestra e' stata fissata.
		builder.HasIndex(x => x.CreatedAt)
			.HasMethod("brin")
			.HasDatabaseName("brin_chatbot_conversation_turns_created_at");

		builder.HasOne(x => x.LlmProvider)
			.WithMany()
			.HasForeignKey(x => x.LlmProviderId)
			.HasConstraintName("fk_chatbot_conversation_turns_llm_provider")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
