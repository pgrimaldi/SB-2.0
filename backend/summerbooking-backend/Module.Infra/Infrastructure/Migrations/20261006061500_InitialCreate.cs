using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Infra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "infra");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:infra.integration_app", "of_sale,transactions,team_system");

            migrationBuilder.CreateTable(
                name: "integration_api_clients",
                schema: "infra",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile (UUIDv7) per il pannello systemadmin; non usato dal flusso di autenticazione, che chiave su client_id."),
                    app_name = table.Column<int>(type: "infra.integration_app", nullable: false, comment: "Applicazione/integrazione partner a cui il client appartiene; instrada le autorizzazioni (es. lo scambio OAuth di TeamSystem)."),
                    client_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Identificativo pubblico del client (client_id OAuth / apiKey firmata in signinApi); chiave di ricerca dell'autenticazione."),
                    client_secret_ref = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Riferimento (ARN/nome) al segreto condiviso nel secret manager, path convenzionale infra/integration-api-clients/{client_id} — il segreto vero non è mai a DB."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties a cui il client è vincolato; NULL = client non scoped a una singola property — copiato, nessuna FK cross-schema."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Interruttore attivo/revocato: un client con is_active = false non autentica più, senza essere cancellato (storico/audit preservato)."),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Scadenza effettiva del client, quando impostata; a differenza del legacy va verificata dal servizio di autenticazione."),
                    rotated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data dell'ultima rotazione del segreto (aggiornamento di client_secret_ref); unico modo di saperlo senza consultare il secret manager."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione del client."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: revoca definitiva del client, distinta da is_active (disattivazione reversibile).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_infra_integration_api_clients", x => x.id);
                },
                comment: "Registro dei client applicativi partner (POS/cassa esterni, export dati) che autenticano chiamate server-to-server verso l'API — nessuna credenziale in chiaro, solo un riferimento al secret manager.");

            migrationBuilder.CreateIndex(
                name: "ix_infra_integration_api_clients_app_active",
                schema: "infra",
                table: "integration_api_clients",
                column: "app_name",
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_infra_integration_api_clients_property",
                schema: "infra",
                table: "integration_api_clients",
                column: "property_id",
                filter: "property_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_infra_integration_api_clients_client_id",
                schema: "infra",
                table: "integration_api_clients",
                column: "client_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_infra_integration_api_clients_public_id",
                schema: "infra",
                table: "integration_api_clients",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "integration_api_clients",
                schema: "infra");
        }
    }
}
