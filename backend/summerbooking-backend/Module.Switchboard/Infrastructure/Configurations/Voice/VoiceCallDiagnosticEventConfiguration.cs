using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Switchboard.Domain.Entities.Voice;

namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceCallDiagnosticEventConfiguration : IEntityTypeConfiguration<VoiceCallDiagnosticEvent>
{
	public void Configure(EntityTypeBuilder<VoiceCallDiagnosticEvent> builder)
	{
		builder.ToTable("switchboard_voice_call_diagnostics", "switchboard", t => t.HasComment(
			"Evento del flusso chiamata (webhook Telnyx o passo interno IVR); log diagnostico write-only, nessun lettore applicativo."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_call_diagnostics");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.HasComment("Centralino risolto dall'handler; NULL finché il contesto non è risolto (config_not_found).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; risolto dall'handler, spesso NULL sui dati reali.");

		builder.Property(x => x.CallRecordId)
			.HasColumnName("call_record_id")
			.HasComment("Chiamata di dominio a cui l'evento appartiene, quando risolvibile.");

		builder.Property(x => x.EventSource)
			.HasColumnName("event_source")
			.HasColumnType("switchboard.call_event_source")
			.IsRequired()
			.HasComment("Origine dell'evento: webhook Telnyx o passo interno dell'IVR.");

		builder.Property(x => x.EventName)
			.HasColumnName("event_name")
			.HasMaxLength(64)
			.IsRequired()
			.HasComment("Nome evento grezzo (call.initiated, step.dtmf_received, ...), vocabolario aperto non tipizzato.");

		builder.Property(x => x.Direction)
			.HasColumnName("direction")
			.HasColumnType("switchboard.call_direction_raw")
			.HasComment("Verso della chiamata grezzo Telnyx (incoming/outgoing).");

		builder.Property(x => x.CallerNumber)
			.HasColumnName("caller_number")
			.HasMaxLength(64)
			.HasComment("Numero chiamante pseudonimizzato prima della scrittura.");

		builder.Property(x => x.CalleeNumber)
			.HasColumnName("callee_number")
			.HasMaxLength(64)
			.HasComment("Numero chiamato pseudonimizzato prima della scrittura.");

		builder.Property(x => x.DtmfDigits)
			.HasColumnName("dtmf_digits")
			.HasMaxLength(20)
			.HasComment("Cifre DTMF raccolte all'evento call.gather.ended (stringa vuota = timeout senza input).");

		builder.Property(x => x.TelnyxCallSessionId)
			.HasColumnName("telnyx_call_session_id")
			.HasMaxLength(150)
			.HasComment("Id di sessione Telnyx per correlare gli eventi di una stessa chiamata.");

		builder.Property(x => x.TelnyxCallControlId)
			.HasColumnName("telnyx_call_control_id")
			.HasMaxLength(150)
			.HasComment("Id Telnyx per i comandi sulla chiamata.");

		builder.Property(x => x.TelnyxCallLegId)
			.HasColumnName("telnyx_call_leg_id")
			.HasMaxLength(150)
			.HasComment("Id Telnyx del leg di chiamata.");

		builder.Property(x => x.HandlerResult)
			.HasColumnName("handler_result")
			.HasColumnType("jsonb")
			.HasComment("Valore di ritorno dell'handler o dato del passo IVR, passthrough diagnostico mai interrogato per sotto-campo.");

		builder.Property(x => x.RawPayload)
			.HasColumnName("raw_payload")
			.HasColumnType("jsonb")
			.HasComment("Corpo del webhook Telnyx come ricevuto, con numeri pseudonimizzati; vuoto per le righe ivr_step.");

		builder.Property(x => x.ErrorMessage)
			.HasColumnName("error_message")
			.HasColumnType("text")
			.HasComment("Messaggio d'errore se l'handler/il passo ha lanciato un'eccezione.");

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

		builder.HasIndex(x => new { x.TelnyxCallSessionId, x.CreatedAt })
			.HasDatabaseName("ix_switchboard_voice_call_diagnostics_session");

		builder.HasIndex(x => x.CallRecordId)
			.HasFilter("call_record_id IS NOT NULL")
			.HasDatabaseName("ix_switchboard_voice_call_diagnostics_call_record");

		builder.HasIndex(x => new { x.ConfigId, x.CreatedAt })
			.HasDatabaseName("ix_switchboard_voice_call_diagnostics_config_time");

		builder.HasIndex(x => x.CreatedAt)
			.HasDatabaseName("ix_switchboard_voice_call_diagnostics_purge");

		builder.HasOne(x => x.Config)
			.WithMany()
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_voice_call_diagnostics_config")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasOne(x => x.CallRecord)
			.WithMany(r => r.DiagnosticEvents)
			.HasForeignKey(x => x.CallRecordId)
			.HasConstraintName("fk_switchboard_voice_call_diagnostics_call_record")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
