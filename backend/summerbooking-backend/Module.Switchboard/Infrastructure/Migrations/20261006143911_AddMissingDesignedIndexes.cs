using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Switchboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingDesignedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_switchboard_voice_menu_items_runtime",
                schema: "switchboard",
                table: "switchboard_voice_menu_items");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_menu_items_runtime",
                schema: "switchboard",
                table: "switchboard_voice_menu_items",
                columns: new[] { "config_id", "parent_id", "dtmf_key" },
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_voice_menu_items_dtmf",
                schema: "switchboard",
                table: "switchboard_voice_menu_items",
                columns: new[] { "config_id", "parent_id", "dtmf_key" },
                unique: true,
                filter: "deleted_at IS NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_call_records_config_started",
                schema: "switchboard",
                table: "switchboard_voice_call_records",
                columns: new[] { "config_id", "started_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_messaging_wa_event_logs_event",
                schema: "switchboard",
                table: "switchboard_messaging_wa_event_logs",
                columns: new[] { "wamid", "event_type", "status" },
                unique: true,
                filter: "wamid IS NOT NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            // Oggetti che il modello EF non sa esprimere (indice su espressione): da riportare a mano in
            // un eventuale nuovo consolidamento delle migration.
            // Unicità imposta da Meta sul WABA: un nome di template per lingua, senza distinzione di maiuscole.
            migrationBuilder.Sql("""
CREATE UNIQUE INDEX uq_switchboard_messaging_wa_templates_name_lang
    ON switchboard.switchboard_messaging_wa_templates (property_id, lower(template_name), language)
 WHERE deleted_at IS NULL;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS switchboard.uq_switchboard_messaging_wa_templates_name_lang;");

            migrationBuilder.DropIndex(
                name: "ix_switchboard_voice_menu_items_runtime",
                schema: "switchboard",
                table: "switchboard_voice_menu_items");

            migrationBuilder.DropIndex(
                name: "uq_switchboard_voice_menu_items_dtmf",
                schema: "switchboard",
                table: "switchboard_voice_menu_items");

            migrationBuilder.DropIndex(
                name: "ix_switchboard_voice_call_records_config_started",
                schema: "switchboard",
                table: "switchboard_voice_call_records");

            migrationBuilder.DropIndex(
                name: "uq_switchboard_messaging_wa_event_logs_event",
                schema: "switchboard",
                table: "switchboard_messaging_wa_event_logs");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_menu_items_runtime",
                schema: "switchboard",
                table: "switchboard_voice_menu_items",
                columns: new[] { "config_id", "parent_id", "dtmf_key" },
                unique: true,
                filter: "is_active AND deleted_at IS NULL");
        }
    }
}
