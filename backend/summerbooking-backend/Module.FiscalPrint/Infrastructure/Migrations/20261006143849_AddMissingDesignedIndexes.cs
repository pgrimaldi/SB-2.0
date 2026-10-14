using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.FiscalPrint.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingDesignedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "ix_fiscalprint_printer_departments_resolver",
                schema: "fiscalprint",
                table: "printer_departments",
                newName: "uq_fiscalprint_printer_departments_rate");

            migrationBuilder.CreateIndex(
                name: "ix_fiscalprint_printer_departments_resolver",
                schema: "fiscalprint",
                table: "printer_departments",
                columns: new[] { "printer_id", "vat_rate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_fiscalprint_printer_departments_resolver",
                schema: "fiscalprint",
                table: "printer_departments");

            migrationBuilder.RenameIndex(
                name: "uq_fiscalprint_printer_departments_rate",
                schema: "fiscalprint",
                table: "printer_departments",
                newName: "ix_fiscalprint_printer_departments_resolver");
        }
    }
}
