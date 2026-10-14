using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Payment.Domain.Entities;

namespace Module.Payment.Infrastructure.Configurations;

public sealed class WebhookEventConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
	public void Configure(EntityTypeBuilder<WebhookEvent> builder)
	{
		builder.ToTable("webhook_events", "payment", t => t.HasComment(
			"Registro degli eventi webhook ricevuti dai gateway di pagamento. La chiave di " +
			"idempotenza (provider, external_event_id) e' un vincolo di schema: l'inserimento e' un " +
			"INSERT ... ON CONFLICT DO NOTHING, la corsa fra due webhook concorrenti la arbitra " +
			"PostgreSQL e non l'applicazione."));

		builder.HasKey(x => x.Id).HasName("pk_payment_webhook_events");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Provider)
			.HasColumnName("provider")
			.HasColumnType("payment.webhook_provider")
			.HasComment("Gateway che ha inviato l'evento; oggi solo stripe.");

		builder.Property(x => x.ExternalEventId)
			.HasColumnName("external_event_id")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Id dell'evento presso il gateway (per Stripe: evt_...). Meta' della chiave di idempotenza.");

		builder.Property(x => x.EventType)
			.HasColumnName("event_type")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Tipo di evento come dichiarato dal gateway, testo libero e non enum: il " +
				"vocabolario lo decide Stripe e cambia senza il nostro deploy. Oggi ne sono gestiti " +
				"due (payment_intent.succeeded, checkout.session.completed), ma ogni altro evento " +
				"va comunque registrato.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, se l'evento e' attribuibile a uno " +
				"stabilimento — nullable perche' l'attribuzione avviene leggendo il payload e puo' " +
				"non riuscire (es. eventi di piattaforma).");

		builder.Property(x => x.Payload)
			.HasColumnName("payload")
			.HasColumnType("jsonb")
			.IsRequired()
			.HasComment("Corpo dell'evento come ricevuto, per poterlo rielaborare senza richiederlo " +
				"di nuovo al gateway.");

		builder.Property(x => x.ProcessingStatus)
			.HasColumnName("processing_status")
			.HasColumnType("payment.webhook_processing_status")
			.HasComment("received (registrato), processed (effetti applicati), ignored (riconosciuto " +
				"e volutamente non gestito), failed (da riprovare o esaminare).");

		builder.Property(x => x.AttemptCount)
			.HasColumnName("attempt_count")
			.HasColumnType("smallint")
			.HasDefaultValue((short)0)
			.HasComment("Numero di tentativi di elaborazione: distingue un fallimento isolato da uno " +
				"ricorrente senza dover leggere i log.");

		builder.Property(x => x.FailureReason)
			.HasColumnName("failure_reason")
			.HasColumnType("text")
			.HasComment("Motivo dell'ultimo fallimento; NULL quando processing_status non e' failed.");

		builder.Property(x => x.ReceivedAt)
			.HasColumnName("received_at")
			.HasDefaultValueSql("now()")
			.HasComment("Istante di ricezione del webhook. Decorrenza della retention.");

		builder.Property(x => x.ProcessedAt)
			.HasColumnName("processed_at")
			.HasComment("Istante in cui l'evento e' stato elaborato; NULL finche' e' received.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()")
			.HasComment("Ultima modifica (cambio di stato dell'elaborazione).");

		// LA ragione d'essere della tabella: senza questo indice l'idempotenza tornerebbe a essere
		// un SELECT + INSERT applicativo, con la finestra di corsa che aveva il legacy.
		builder.HasIndex(x => new { x.Provider, x.ExternalEventId })
			.IsUnique()
			.HasDatabaseName("uq_payment_webhook_events_external");

		// Coda di lavoro: eventi da elaborare o da riprovare, in ordine di arrivo. Parziale, cosi'
		// non indicizza gli eventi gia' chiusi, che sono la quasi totalita' delle righe.
		builder.HasIndex(x => x.ReceivedAt)
			.HasFilter("processing_status IN ('received', 'failed')")
			.HasDatabaseName("ix_payment_webhook_events_pending");

		builder.HasIndex(x => new { x.PropertyId, x.ReceivedAt })
			.HasFilter("property_id IS NOT NULL")
			.HasDatabaseName("ix_payment_webhook_events_property");

		// BRIN per la retention: tabella in solo append.
		builder.HasIndex(x => x.CreatedAt)
			.HasMethod("brin")
			.HasDatabaseName("brin_payment_webhook_events_created_at");
	}
}
