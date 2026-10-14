using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Module.Audit.Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Audit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit.actor_kind", "user,system,external_client,anonymous")
                .Annotation("Npgsql:Enum:audit.entry_category", "operator,security,platform")
                .Annotation("Npgsql:Enum:audit.entry_outcome", "succeeded,failed")
                .Annotation("Npgsql:Enum:audit.source_module", "identity,catalog,booking,payment,fiskaly,fiscalprint,notification,switchboard,chatbot,privacy,infra");

            migrationBuilder.CreateTable(
                name: "trail_entries",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Istante dell'azione nel modulo di origine. Colonna di partizione."),
                    source_event_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id dell'evento AuditEntryRecorded: rende idempotente la consegna at-least-once dell'outbox."),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Istante di scrittura qui; la differenza con occurred_at misura il ritardo dell'outbox."),
                    category = table.Column<EntryCategory>(type: "audit.entry_category", nullable: false, comment: "operator, security o platform: decide chi legge la voce e la sua retention."),
                    source_module = table.Column<SourceModule>(type: "audit.source_module", nullable: false, comment: "Modulo che ha pubblicato l'evento."),
                    action_code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Codice stabile <modulo>.<azione_al_passato>; non enum perché il catalogo cresce con le API dei moduli."),
                    outcome = table.Column<EntryOutcome>(type: "audit.entry_outcome", nullable: false, defaultValueSql: "'succeeded'", comment: "Esito dell'azione (login fallito, OTP errato)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id della property (catalog), copiato; NULL per azioni senza stabilimento."),
                    actor_kind = table.Column<ActorKind>(type: "audit.actor_kind", nullable: false, comment: "user, system, external_client o anonymous."),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato; nessuna FK cross-modulo."),
                    actor_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Nome dell'operatore al momento dell'azione."),
                    actor_role_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Codice ruolo dell'operatore al momento dell'azione."),
                    impersonator_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id del systemadmin che agiva in impersonazione, se c'era."),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Tipo dell'oggetto principale (booking, customer, user, receipt, ...)."),
                    target_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id dell'oggetto principale; riferimento polimorfo senza FK."),
                    booking_number_snapshot = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Numero di prenotazione al momento dell'azione (colonna «ID prenotazione» della pagina)."),
                    spot_label_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Postazione al momento dell'azione."),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb", comment: "Dettagli specifici dell'azione; mai password, token, OTP o dati di carta."),
                    client_ip = table.Column<IPAddress>(type: "inet", nullable: true, comment: "IP della richiesta (eventi security e platform); dato personale, segue la retention della categoria."),
                    client_user_agent = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true, comment: "User-agent della richiesta."),
                    trace_id = table.Column<string>(type: "character varying(55)", maxLength: 55, nullable: true, comment: "traceId della richiesta, per risalire ai log diagnostici."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga, coincide con recorded_at."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Presente per uniformità di schema: registro append-only, non cambia mai."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Presente per uniformità di schema, non usato: la sola rimozione è l'hard-delete della retention.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_audit_trail_entries", x => new { x.id, x.occurred_at });
                },
                comment: "Registro trasversale append-only di chi ha fatto cosa (operatori, eventi di sicurezza, systemadmin), alimentato dagli eventi AuditEntryRecorded dei moduli. Partizionata per mese su occurred_at. I log tecnici di dominio restano nei moduli proprietari (docs/86-modulo-audit.md).");

            migrationBuilder.CreateIndex(
                name: "uq_audit_trail_entries_source_event",
                schema: "audit",
                table: "trail_entries",
                columns: new[] { "source_event_id", "occurred_at" },
                unique: true);

            // Partizionamento mensile su occurred_at. Come per Booking: EF crea la tabella normale, qui la si
            // sostituisce con una gemella partizionata (LIKE ... INCLUDING ...) e si ricreano sul padre PK,
            // UNIQUE e indici, che PostgreSQL propaga alle partizioni. Nessuna FK punta a questa tabella.
            //
            // Partizioni da ottobre 2026 a dicembre 2028 piu' una di default come rete di sicurezza: la
            // default non deve riempirsi, perche' finche' contiene righe di un mese la partizione di quel
            // mese non e' piu' creabile. Prima del 2029 vanno aggiunte le successive.
            migrationBuilder.Sql($"""
CREATE TABLE audit.trail_entries_new (
  LIKE audit.trail_entries
  INCLUDING DEFAULTS INCLUDING GENERATED INCLUDING COMMENTS INCLUDING STATISTICS INCLUDING IDENTITY
) PARTITION BY RANGE (occurred_at);
{PartizioniMensili("trail_entries_new", "trail_entries", new DateOnly(2026, 10, 1), new DateOnly(2029, 1, 1))}
DROP TABLE audit.trail_entries;
ALTER TABLE audit.trail_entries_new RENAME TO trail_entries;
COMMENT ON TABLE audit.trail_entries IS 'Registro trasversale append-only di chi ha fatto cosa (operatori, eventi di sicurezza, systemadmin), alimentato dagli eventi AuditEntryRecorded dei moduli. Partizionata per mese su occurred_at. I log tecnici di dominio restano nei moduli proprietari (docs/86-modulo-audit.md).';
ALTER TABLE audit.trail_entries ADD CONSTRAINT pk_audit_trail_entries PRIMARY KEY (id, occurred_at);
CREATE UNIQUE INDEX uq_audit_trail_entries_source_event ON audit.trail_entries (source_event_id, occurred_at);
CREATE INDEX ix_audit_trail_entries_property_time ON audit.trail_entries
  (property_id, occurred_at DESC, id DESC) WHERE (property_id IS NOT NULL);
CREATE INDEX ix_audit_trail_entries_property_actor_time ON audit.trail_entries
  (property_id, actor_user_id, occurred_at DESC) WHERE (property_id IS NOT NULL);
CREATE INDEX ix_audit_trail_entries_actor_time ON audit.trail_entries
  (actor_user_id, occurred_at DESC) WHERE (actor_user_id IS NOT NULL);
CREATE INDEX ix_audit_trail_entries_category_time ON audit.trail_entries (category, occurred_at DESC);
CREATE INDEX brin_audit_trail_entries_occurred_at ON audit.trail_entries USING brin (occurred_at);
""");
        }

        /// <summary>Una partizione per mese in [da, a) piu' la default, con nome finale gia' giusto.</summary>
        private static string PartizioniMensili(string padre, string nomeFinale, DateOnly da, DateOnly a)
        {
            var sql = new System.Text.StringBuilder();
            for (var mese = da; mese < a; mese = mese.AddMonths(1))
            {
                var fine = mese.AddMonths(1);
                sql.AppendLine(
                    $"CREATE TABLE audit.{nomeFinale}_y{mese.Year}m{mese.Month:00} PARTITION OF audit.{padre} " +
                    $"FOR VALUES FROM ('{mese:yyyy-MM-dd}') TO ('{fine:yyyy-MM-dd}');");
            }
            sql.Append($"CREATE TABLE audit.{nomeFinale}_default PARTITION OF audit.{padre} DEFAULT;");
            return sql.ToString();
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "trail_entries",
                schema: "audit");
        }
    }
}
