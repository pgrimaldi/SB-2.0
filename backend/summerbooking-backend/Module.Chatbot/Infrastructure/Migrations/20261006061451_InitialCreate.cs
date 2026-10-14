using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Chatbot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "chatbot");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:chatbot.channel_kind", "manager,customer")
                .Annotation("Npgsql:Enum:chatbot.turn_status", "answered,failed");

            migrationBuilder.CreateTable(
                name: "llm_providers",
                schema: "chatbot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Identificativo tecnico del modello passato al SDK del vendor (es. \"gemini-3-flash-preview\"); chiave applicativa, non solo la PK interna."),
                    vendor = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Fornitore del modello (es. \"google\"); stringa libera, vocabolario aperto e in crescita — non enum."),
                    display_name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true, comment: "Etichetta leggibile per il pannello di configurazione; solo presentazione."),
                    is_selectable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Se false, il modello non è proponibile per nuove selezioni; le righe storiche che lo referenziano restano valide."),
                    retires_on = table.Column<DateOnly>(type: "date", nullable: true, comment: "Data oltre la quale il modello è considerato ritirato dal vendor; puramente informativa — l'interruttore applicato è is_selectable."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione del catalogo provider."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: rimozione di un provider mai usato. Un provider già usato si ritira con is_selectable = false, non si cancella.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chatbot_llm_providers", x => x.id);
                },
                comment: "Catalogo dei modelli/provider LLM utilizzabili dal modulo Chatbot per generare le risposte — una riga per modello selezionabile, non per vendor.");

            migrationBuilder.CreateTable(
                name: "conversation_turns",
                schema: "chatbot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile (UUIDv7) restituito dalle API (/chat → historyId, /history → id di ogni voce) al posto dell'id interno sequenziale."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties (stabilimento del turno), copiato — nessuna FK cross-schema; sempre dal claim del token."),
                    channel = table.Column<int>(type: "chatbot.channel_kind", nullable: false, defaultValueSql: "'manager'", comment: "Canale applicativo da cui parte il turno: manager (gestionale, autenticato) o customer (futuro assistente lato cliente finale, anche anonimo)."),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, operatore autenticato che ha inviato il messaggio; obbligatorio per channel = manager — copiato, nessuna FK cross-schema."),
                    actor_session_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Correlatore opaco generato lato client per turni anonimi del canale customer (actor_user_id NULL); non è un dato personale."),
                    status = table.Column<int>(type: "chatbot.turn_status", nullable: false, defaultValueSql: "'answered'", comment: "Esito del turno: answered, oppure failed quando il provider non ha prodotto una risposta valida (mai dedotto da una stringa vuota)."),
                    llm_provider_id = table.Column<long>(type: "bigint", nullable: true, comment: "Provider/modello (chatbot.llm_providers) che ha effettivamente generato la risposta di questo turno; assente sui turni failed prima della selezione del provider."),
                    llm_model_snapshot = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Identificativo del modello riportato dal provider al momento della generazione — congelato, non riflette una rinomina/aggiornamento successivo di llm_providers."),
                    response_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, comment: "Lingua della risposta generata (codice cultura, es. it-IT), da preferenza utente nota — non da un'euristica sul singolo messaggio."),
                    input_text = table.Column<string>(type: "text", nullable: false, comment: "Messaggio inviato dall'utente — testo libero, può contenere dati personali di terzi (vedi D-54)."),
                    output_text = table.Column<string>(type: "text", nullable: true, comment: "Risposta generata dal provider LLM; NULL quando status = failed, mai stringa vuota — stessa cautela GDPR di input_text."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Istante del messaggio; colonna di riferimento per il conteggio giornaliero del rate limit (non updated_at, a differenza del legacy)."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica; righe di fatto immutabili dopo la creazione."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: retention o cancellazione su richiesta.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chatbot_conversation_turns", x => x.id);
                    table.CheckConstraint("ck_chatbot_conversation_turns_channel_actor", "channel <> 'manager' OR actor_user_id IS NOT NULL");
                    table.CheckConstraint("ck_chatbot_conversation_turns_correlator", "actor_user_id IS NOT NULL OR actor_session_id IS NOT NULL OR channel = 'manager'");
                    table.CheckConstraint("ck_chatbot_conversation_turns_input_length", "char_length(input_text) <= 5000");
                    table.CheckConstraint("ck_chatbot_conversation_turns_status_output", "status <> 'failed' OR output_text IS NULL");
                    table.ForeignKey(
                        name: "fk_chatbot_conversation_turns_llm_provider",
                        column: x => x.llm_provider_id,
                        principalSchema: "chatbot",
                        principalTable: "llm_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Uno scambio completo domanda/risposta tra un utente (operatore o, in futuro, cliente finale) e l'assistente LLM.");

            migrationBuilder.CreateTable(
                name: "manager_configs",
                schema: "chatbot",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties (stabilimento a cui si applica la configurazione), copiato — nessuna FK cross-schema; sempre dal claim del token, mai dal body."),
                    daily_request_limit = table.Column<int>(type: "integer", nullable: false, defaultValue: 10, comment: "Numero massimo di richieste al chatbot consentite per giorno solare (Europe/Rome), per stabilimento — limite anti-abuso/controllo costi."),
                    history_page_size_limit = table.Column<int>(type: "integer", nullable: false, defaultValue: 10, comment: "Tetto massimo di voci di chatbot.conversation_turns restituite da /history per stabilimento, anche se il chiamante ne richiede di più."),
                    llm_provider_id = table.Column<long>(type: "bigint", nullable: true, comment: "Override del provider/modello LLM per questo stabilimento; NULL = usa il default di piattaforma."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, operatore che ha creato/modificato manualmente la configurazione; NULL se provisionata automaticamente dal sistema — copiato, nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della configurazione."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: disattivazione del modulo chatbot per lo stabilimento.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_chatbot_manager_configs", x => x.id);
                    table.CheckConstraint("ck_chatbot_manager_configs_daily_limit", "daily_request_limit > 0");
                    table.CheckConstraint("ck_chatbot_manager_configs_history_limit", "history_page_size_limit > 0");
                    table.ForeignKey(
                        name: "fk_chatbot_manager_configs_llm_provider",
                        column: x => x.llm_provider_id,
                        principalSchema: "chatbot",
                        principalTable: "llm_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Limiti di utilizzo e preferenza di provider LLM del chatbot per stabilimento — una riga per property_id.");

            migrationBuilder.CreateIndex(
                name: "brin_chatbot_conversation_turns_created_at",
                schema: "chatbot",
                table: "conversation_turns",
                column: "created_at")
                .Annotation("Npgsql:IndexMethod", "brin");

            migrationBuilder.CreateIndex(
                name: "ix_chatbot_conversation_turns_actor_time",
                schema: "chatbot",
                table: "conversation_turns",
                columns: new[] { "actor_user_id", "created_at" },
                filter: "actor_user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_chatbot_conversation_turns_property_time",
                schema: "chatbot",
                table: "conversation_turns",
                columns: new[] { "property_id", "created_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_chatbot_conversation_turns_provider",
                schema: "chatbot",
                table: "conversation_turns",
                column: "llm_provider_id",
                filter: "llm_provider_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_chatbot_conversation_turns_rate_limit",
                schema: "chatbot",
                table: "conversation_turns",
                columns: new[] { "property_id", "channel", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_chatbot_conversation_turns_session",
                schema: "chatbot",
                table: "conversation_turns",
                columns: new[] { "actor_session_id", "created_at" },
                filter: "actor_session_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_chatbot_conversation_turns_public_id",
                schema: "chatbot",
                table: "conversation_turns",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chatbot_llm_providers_selectable",
                schema: "chatbot",
                table: "llm_providers",
                column: "vendor",
                filter: "is_selectable AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_chatbot_llm_providers_code",
                schema: "chatbot",
                table: "llm_providers",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_chatbot_manager_configs_llm_provider",
                schema: "chatbot",
                table: "manager_configs",
                column: "llm_provider_id",
                filter: "llm_provider_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_chatbot_manager_configs_property",
                schema: "chatbot",
                table: "manager_configs",
                column: "property_id",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conversation_turns",
                schema: "chatbot");

            migrationBuilder.DropTable(
                name: "manager_configs",
                schema: "chatbot");

            migrationBuilder.DropTable(
                name: "llm_providers",
                schema: "chatbot");
        }
    }
}
