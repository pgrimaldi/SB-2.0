using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Audit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDocumentReferencesFromComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "trail_entries",
                schema: "audit",
                comment: "Registro trasversale append-only di chi ha fatto cosa (operatori, eventi di sicurezza, systemadmin), alimentato dagli eventi AuditEntryRecorded dei moduli. Partizionata per mese su occurred_at. I log tecnici di dominio restano nei moduli proprietari.",
                oldComment: "Registro trasversale append-only di chi ha fatto cosa (operatori, eventi di sicurezza, systemadmin), alimentato dagli eventi AuditEntryRecorded dei moduli. Partizionata per mese su occurred_at. I log tecnici di dominio restano nei moduli proprietari (docs/86-modulo-audit.md).");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "trail_entries",
                schema: "audit",
                comment: "Registro trasversale append-only di chi ha fatto cosa (operatori, eventi di sicurezza, systemadmin), alimentato dagli eventi AuditEntryRecorded dei moduli. Partizionata per mese su occurred_at. I log tecnici di dominio restano nei moduli proprietari (docs/86-modulo-audit.md).",
                oldComment: "Registro trasversale append-only di chi ha fatto cosa (operatori, eventi di sicurezza, systemadmin), alimentato dagli eventi AuditEntryRecorded dei moduli. Partizionata per mese su occurred_at. I log tecnici di dominio restano nei moduli proprietari.");
        }
    }
}
