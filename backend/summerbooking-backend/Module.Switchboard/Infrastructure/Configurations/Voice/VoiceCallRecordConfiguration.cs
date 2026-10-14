using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Switchboard.Domain.Entities.Voice;

namespace Module.Switchboard.Infrastructure.Configurations.Voice;

public sealed class VoiceCallRecordConfiguration : IEntityTypeConfiguration<VoiceCallRecord>
{
	public void Configure(EntityTypeBuilder<VoiceCallRecord> builder)
	{
		builder.ToTable("switchboard_voice_call_records", "switchboard", t => t.HasComment(
			"Una riga per chiamata gestita dal centralino: esito, durata, percorso IVR — tabella di dominio, non log diagnostico."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_voice_call_records");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (export GDPR: la chiamata contiene numeri di telefono, dato personale).");

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.IsRequired()
			.HasComment("Centralino che ha gestito la chiamata.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping/retention/report senza join.");

		builder.Property(x => x.Direction)
			.HasColumnName("direction")
			.HasColumnType("switchboard.call_direction")
			.IsRequired()
			.HasComment("inbound = chiamata in ingresso gestita dall'IVR; outbound = B-leg SIP creato dal nostro forward_sip.");

		builder.Property(x => x.CallerNumber)
			.HasColumnName("caller_number")
			.HasMaxLength(30)
			.HasComment("Numero del chiamante PSTN originale in E.164 — dato personale, pseudonimizzabile a fine retention.");

		builder.Property(x => x.CalleeNumber)
			.HasColumnName("callee_number")
			.HasMaxLength(30)
			.HasComment("Numero Telnyx del centralino chiamato (inbound), in E.164.");

		builder.Property(x => x.SipDestination)
			.HasColumnName("sip_destination")
			.HasMaxLength(255)
			.HasComment("SIP URI dell'endpoint dello stabilimento verso cui è stata inoltrata la chiamata, congelato all'istante della chiamata.");

		builder.Property(x => x.Outcome)
			.HasColumnName("outcome")
			.HasColumnType("switchboard.call_outcome")
			.IsRequired()
			.HasComment("Esito finale della chiamata.");

		builder.Property(x => x.ForwardResult)
			.HasColumnName("forward_result")
			.HasColumnType("switchboard.forward_result")
			.HasComment("Esito del tentativo di inoltro SIP, valorizzato solo se l'outcome riguarda un forward.");

		builder.Property(x => x.IvrPath)
			.HasColumnName("ivr_path")
			.HasColumnType("jsonb")
			.HasDefaultValueSql("'[]'::jsonb")
			.HasComment("Sequenza sintetica dei tasti premuti e delle voci attraversate, snapshot scoped alla singola chiamata.");

		builder.Property(x => x.FinalMenuItemPublicId)
			.HasColumnName("final_menu_item_public_id")
			.HasComment("public_id dell'ultima voce di menù raggiunta, per analisi di dove finiscono le chiamate.");

		builder.Property(x => x.TelnyxCallSessionId)
			.HasColumnName("telnyx_call_session_id")
			.HasMaxLength(150)
			.HasComment("Id di sessione Telnyx condiviso da A-leg e B-leg, per correlare il fallimento del forward.");

		builder.Property(x => x.TelnyxCallControlId)
			.HasColumnName("telnyx_call_control_id")
			.HasMaxLength(150)
			.HasComment("Id Telnyx per i comandi sulla chiamata.");

		builder.Property(x => x.TelnyxCallLegId)
			.HasColumnName("telnyx_call_leg_id")
			.HasMaxLength(150)
			.HasComment("Id Telnyx del leg di chiamata.");

		builder.Property(x => x.StartedAt)
			.HasColumnName("started_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Istante di inizio chiamata (evento call.initiated).");

		builder.Property(x => x.AnsweredAt)
			.HasColumnName("answered_at")
			.HasComment("Istante di risposta (evento call.answered).");

		builder.Property(x => x.EndedAt)
			.HasColumnName("ended_at")
			.HasComment("Istante di fine chiamata (evento call.hangup).");

		builder.Property(x => x.DurationSeconds)
			.HasColumnName("duration_seconds")
			.HasComment("Durata conversazione (ended_at - answered_at) in secondi.");

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


		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_switchboard_voice_call_records_public_id");

		// Overload con nome: due HasIndex sulle stesse colonne senza nome sono LO STESSO indice per EF,
		// e il secondo ne sovrascrive nome, filtro e unicità. Fino al 2026-10-06 a DB ne esisteva uno solo.
		builder.HasIndex(x => new { x.ConfigId, x.StartedAt }, "ix_switchboard_voice_call_records_config_started")
			.IsDescending(false, true)
			.HasDatabaseName("ix_switchboard_voice_call_records_config_started");

		builder.HasIndex(x => new { x.ConfigId, x.StartedAt }, "ix_switchboard_voice_call_records_history")
			.HasFilter("direction = 'outbound'")
			.HasDatabaseName("ix_switchboard_voice_call_records_history");

		builder.HasIndex(x => new { x.PropertyId, x.CallerNumber, x.StartedAt })
			.HasDatabaseName("ix_switchboard_voice_call_records_caller");

		builder.HasIndex(x => x.TelnyxCallSessionId)
			.HasDatabaseName("ix_switchboard_voice_call_records_session");

		builder.HasIndex(x => x.StartedAt)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_switchboard_voice_call_records_retention");

		builder.HasOne(x => x.Config)
			.WithMany(c => c.CallRecords)
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_voice_call_records_config")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
