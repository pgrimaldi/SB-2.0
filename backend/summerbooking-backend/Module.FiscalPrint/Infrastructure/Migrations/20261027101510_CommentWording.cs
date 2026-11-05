using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.FiscalPrint.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommentWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "dept_number",
                schema: "fiscalprint",
                table: "printer_departments",
                type: "smallint",
                nullable: false,
                comment: "Numero del reparto fisico sul registratore RT (esplicito, non posizionale).",
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "Numero del reparto fisico sul registratore RT (esplicito, non più posizionale come nel legacy).");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "fiscalprint",
                table: "printer_configs",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete: valorizzato alla rimozione; is_active riportato a false in automatico, così i due flag restano coerenti.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete: valorizzato alla rimozione; is_active riportato a false in automatico (corregge il bug legacy che scollegava i due flag).");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<short>(
                name: "dept_number",
                schema: "fiscalprint",
                table: "printer_departments",
                type: "smallint",
                nullable: false,
                comment: "Numero del reparto fisico sul registratore RT (esplicito, non più posizionale come nel legacy).",
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "Numero del reparto fisico sul registratore RT (esplicito, non posizionale).");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "fiscalprint",
                table: "printer_configs",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete: valorizzato alla rimozione; is_active riportato a false in automatico (corregge il bug legacy che scollegava i due flag).",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete: valorizzato alla rimozione; is_active riportato a false in automatico, così i due flag restano coerenti.");
        }
    }
}
