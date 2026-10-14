using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Switchboard.Domain.Entities.Messaging;

namespace Module.Switchboard.Infrastructure.Configurations.Messaging;

public sealed class MessagingWaEventLogConfiguration : IEntityTypeConfiguration<MessagingWaEventLog>
{
	public void Configure(EntityTypeBuilder<MessagingWaEventLog> builder)
	{
		builder.ToTable("switchboard_messaging_wa_event_logs", "switchboard", t => t.HasComment(
			"Evento del webhook WhatsApp Meta Cloud API ricevuto sul numero del centralino: aggiornamento di consegna o messaggio in arrivo. Log forense write-only."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_messaging_wa_event_logs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.HasComment("Centralino risolto da phone_number_id/display_phone_number; NULL se il numero non è mappato a un centralino noto.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato quando risolvibile.");

		builder.Property(x => x.EventType)
			.HasColumnName("event_type")
			.HasColumnType("switchboard.wa_webhook_event_type")
			.IsRequired()
			.HasComment("status (da value.statuses[]) o message_in (da value.messages[]).");

		builder.Property(x => x.Wamid)
			.HasColumnName("wamid")
			.HasMaxLength(255)
			.HasComment("Id del messaggio Meta: st.id per gli status, m.id per gli inbound.");

		builder.Property(x => x.WabaId)
			.HasColumnName("waba_id")
			.HasMaxLength(64)
			.HasComment("Id del WhatsApp Business Account (entry.id).");

		builder.Property(x => x.PhoneNumberId)
			.HasColumnName("phone_number_id")
			.HasMaxLength(64)
			.HasComment("Id del numero Meta che ha inviato/ricevuto (value.metadata); chiave per risolvere config_id.");

		builder.Property(x => x.DisplayPhoneNumber)
			.HasColumnName("display_phone_number")
			.HasMaxLength(30)
			.HasComment("Numero leggibile del numero Meta (value.metadata).");

		builder.Property(x => x.CounterpartyNumber)
			.HasColumnName("counterparty_number")
			.HasMaxLength(64)
			.HasComment("Numero dell'utente finale (mittente per message_in, destinatario per status), pseudonimizzato alla scrittura.");

		builder.Property(x => x.ContactName)
			.HasColumnName("contact_name")
			.HasMaxLength(100)
			.HasComment("Nome profilo del contatto (contact.profile.name); PII, pseudonimizzabile a fine retention.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("switchboard.wa_message_status")
			.HasComment("Stato di consegna, valorizzato solo per event_type = status.");

		builder.Property(x => x.ErrorCode)
			.HasColumnName("error_code")
			.HasComment("Codice errore Meta del primo elemento di st.errors[] (status = failed).");

		builder.Property(x => x.ErrorTitle)
			.HasColumnName("error_title")
			.HasMaxLength(255)
			.HasComment("Titolo errore Meta del primo elemento di st.errors[].");

		builder.Property(x => x.ErrorMessage)
			.HasColumnName("error_message")
			.HasColumnType("text")
			.HasComment("Messaggio di dettaglio dell'errore Meta.");

		builder.Property(x => x.ConversationId)
			.HasColumnName("conversation_id")
			.HasMaxLength(100)
			.HasComment("Id della conversazione ai fini fatturazione Meta (st.conversation.id).");

		builder.Property(x => x.ConversationOrigin)
			.HasColumnName("conversation_origin")
			.HasColumnType("switchboard.wa_conversation_origin")
			.HasComment("Origine/categoria della conversazione (passthrough Meta, unknown di fallback).");

		builder.Property(x => x.PricingBillable)
			.HasColumnName("pricing_billable")
			.HasComment("Se la conversazione è fatturabile.");

		builder.Property(x => x.PricingCategory)
			.HasColumnName("pricing_category")
			.HasColumnType("switchboard.wa_pricing_category")
			.HasComment("Categoria di pricing Meta (passthrough, unknown di fallback).");

		builder.Property(x => x.PricingModel)
			.HasColumnName("pricing_model")
			.HasMaxLength(20)
			.HasComment("Modello di pricing Meta grezzo (CBP/PMP).");

		builder.Property(x => x.MessageType)
			.HasColumnName("message_type")
			.HasColumnType("switchboard.wa_inbound_message_type")
			.HasComment("Tipo del messaggio in arrivo, valorizzato solo per event_type = message_in.");

		builder.Property(x => x.BodyText)
			.HasColumnName("body_text")
			.HasColumnType("text")
			.HasComment("Parte testuale del messaggio in arrivo (text.body, button.text, titolo reply, emoji di reaction); NULL per media/interattivi complessi.");

		builder.Property(x => x.BodyPayload)
			.HasColumnName("body_payload")
			.HasColumnType("jsonb")
			.HasComment("Payload strutturato per messaggi non testuali (media, interactive, location, contacts), passthrough Meta.");

		builder.Property(x => x.RawPayload)
			.HasColumnName("raw_payload")
			.HasColumnType("jsonb")
			.HasComment("Oggetto st/m completo (non l'intero webhook), con numeri pseudonimizzati.");

		builder.Property(x => x.MetaTimestamp)
			.HasColumnName("meta_timestamp")
			.HasComment("Istante dell'evento lato Meta (st.timestamp/m.timestamp).");

		builder.Property(x => x.ReceivedAt)
			.HasColumnName("received_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Istante in cui il nostro server ha ricevuto il webhook.");

		// Idempotenza del webhook: Meta ripete gli eventi. NULLS NOT DISTINCT perché due righe con status
		// entrambi NULL (eventi message_in) devono collidere. La scheda lo progettava come
		// coalesce(status, ''), che su una colonna enum non si può scrivere; AreNullsDistinct(false) dà
		// la stessa garanzia senza SQL a mano. Fino al 2026-10-06 l'indice non esisteva.
		builder.HasIndex(x => new { x.Wamid, x.EventType, x.Status }, "uq_switchboard_messaging_wa_event_logs_event")
			.IsUnique()
			.AreNullsDistinct(false)
			.HasFilter("wamid IS NOT NULL")
			.HasDatabaseName("uq_switchboard_messaging_wa_event_logs_event");

		// bug reale osservato il 2026-09-16: created_at/updated_at non avevano default now() a
		// schema (nessuna convenzione EF globale lo applica, verificato in ModuleDbContextConventions
		// - solo converter UTC e query filter soft-delete) - un ETL che omette queste colonne
		// dall'INSERT (comportamento normale, per lasciare scattare il default) falliva NOT NULL.
		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");


		builder.HasIndex(x => new { x.ConfigId, x.ReceivedAt })
			.HasDatabaseName("ix_switchboard_messaging_wa_event_logs_config_time");

		builder.HasIndex(x => x.Wamid)
			.HasDatabaseName("ix_switchboard_messaging_wa_event_logs_wamid");

		builder.HasIndex(x => new { x.ConfigId, x.EventType, x.Status })
			.HasFilter("event_type = 'status'")
			.HasDatabaseName("ix_switchboard_messaging_wa_event_logs_status");

		builder.HasIndex(x => x.ReceivedAt)
			.HasDatabaseName("ix_switchboard_messaging_wa_event_logs_purge");

		builder.HasOne(x => x.Config)
			.WithMany()
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_messaging_wa_event_logs_config")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
