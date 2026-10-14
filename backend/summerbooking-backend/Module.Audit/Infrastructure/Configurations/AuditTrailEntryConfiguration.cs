using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Module.Audit.Domain.Entities;

namespace Module.Audit.Infrastructure.Configurations;

/// <summary>
/// Registro trasversale delle azioni. La tabella è partizionata per mese su occurred_at: il
/// partizionamento e gli indici con DESC/parziali li crea la migration in SQL, qui c'è il modello.
/// </summary>
public sealed class AuditTrailEntryConfiguration : IEntityTypeConfiguration<AuditTrailEntry>
{
	public void Configure(EntityTypeBuilder<AuditTrailEntry> builder)
	{
		builder.ToTable("trail_entries", "audit", t => t.HasComment(
			"Registro trasversale append-only di chi ha fatto cosa (operatori, eventi di sicurezza, " +
			"systemadmin), alimentato dagli eventi AuditEntryRecorded dei moduli. Partizionata per mese " +
			"su occurred_at. I log tecnici di dominio restano nei moduli proprietari."));

		// Requisito del partizionamento: ogni PK/UNIQUE contiene la colonna di partizione.
		builder.HasKey(x => new { x.Id, x.OccurredAt }).HasName("pk_audit_trail_entries");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SourceEventId)
			.HasColumnName("source_event_id")
			.HasComment("Id dell'evento AuditEntryRecorded: rende idempotente la consegna at-least-once dell'outbox.");

		builder.Property(x => x.OccurredAt)
			.HasColumnName("occurred_at")
			.HasComment("Istante dell'azione nel modulo di origine. Colonna di partizione.");

		builder.Property(x => x.RecordedAt)
			.HasColumnName("recorded_at")
			.HasDefaultValueSql("now()")
			.HasComment("Istante di scrittura qui; la differenza con occurred_at misura il ritardo dell'outbox.");

		builder.Property(x => x.Category)
			.HasColumnName("category")
			.HasColumnType("audit.entry_category")
			.HasComment("operator, security o platform: decide chi legge la voce e la sua retention.");

		builder.Property(x => x.SourceModule)
			.HasColumnName("source_module")
			.HasColumnType("audit.source_module")
			.HasComment("Modulo che ha pubblicato l'evento.");

		builder.Property(x => x.ActionCode)
			.HasColumnName("action_code")
			.HasMaxLength(100)
			.IsRequired()
			.HasComment("Codice stabile <modulo>.<azione_al_passato>; non enum perché il catalogo cresce con le API dei moduli.");

		builder.Property(x => x.Outcome)
			.HasColumnName("outcome")
			.HasColumnType("audit.entry_outcome")
			.HasDefaultValueSql("'succeeded'")
			.HasComment("Esito dell'azione (login fallito, OTP errato).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id della property (catalog), copiato; NULL per azioni senza stabilimento.");

		builder.Property(x => x.ActorKind)
			.HasColumnName("actor_kind")
			.HasColumnType("audit.actor_kind")
			.HasComment("user, system, external_client o anonymous.");

		builder.Property(x => x.ActorUserId)
			.HasColumnName("actor_user_id")
			.HasComment("public_id di identity.access_users, copiato; nessuna FK cross-modulo.");

		builder.Property(x => x.ActorNameSnapshot)
			.HasColumnName("actor_name_snapshot")
			.HasMaxLength(200)
			.HasComment("Nome dell'operatore al momento dell'azione.");

		builder.Property(x => x.ActorRoleSnapshot)
			.HasColumnName("actor_role_snapshot")
			.HasMaxLength(50)
			.HasComment("Codice ruolo dell'operatore al momento dell'azione.");

		builder.Property(x => x.ImpersonatorUserId)
			.HasColumnName("impersonator_user_id")
			.HasComment("public_id del systemadmin che agiva in impersonazione, se c'era.");

		builder.Property(x => x.TargetType)
			.HasColumnName("target_type")
			.HasMaxLength(50)
			.HasComment("Tipo dell'oggetto principale (booking, customer, user, receipt, ...).");

		builder.Property(x => x.TargetId)
			.HasColumnName("target_id")
			.HasComment("public_id dell'oggetto principale; riferimento polimorfo senza FK.");

		builder.Property(x => x.BookingNumberSnapshot)
			.HasColumnName("booking_number_snapshot")
			.HasMaxLength(50)
			.HasComment("Numero di prenotazione al momento dell'azione (colonna «ID prenotazione» della pagina).");

		builder.Property(x => x.SpotLabelSnapshot)
			.HasColumnName("spot_label_snapshot")
			.HasMaxLength(100)
			.HasComment("Postazione al momento dell'azione.");

		builder.Property(x => x.Metadata)
			.HasColumnName("metadata")
			.HasColumnType("jsonb")
			.IsRequired()
			.HasDefaultValueSql("'{}'::jsonb")
			.HasComment("Dettagli specifici dell'azione; mai password, token, OTP o dati di carta.");

		builder.Property(x => x.ClientIp)
			.HasColumnName("client_ip")
			.HasComment("IP della richiesta (eventi security e platform); dato personale, segue la retention della categoria.");

		builder.Property(x => x.ClientUserAgent)
			.HasColumnName("client_user_agent")
			.HasMaxLength(400)
			.HasComment("User-agent della richiesta.");

		builder.Property(x => x.TraceId)
			.HasColumnName("trace_id")
			.HasMaxLength(55)
			.HasComment("traceId della richiesta, per risalire ai log diagnostici.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()")
			.HasComment("Creazione della riga, coincide con recorded_at.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()")
			.HasComment("Presente per uniformità di schema: registro append-only, non cambia mai.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato: la sola rimozione è l'hard-delete della retention.");

		builder.HasIndex(x => new { x.SourceEventId, x.OccurredAt })
			.IsUnique()
			.HasDatabaseName("uq_audit_trail_entries_source_event");
	}
}
