using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class TranslateLicensePlanLabels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 1L,
                column: "label",
                value: "Base");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 3L,
                column: "label",
                value: "Avanzato");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 5L,
                column: "label",
                value: "Essentiel");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 6L,
                column: "label",
                value: "Avancé");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 7L,
                column: "label",
                value: "Básico");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 8L,
                column: "label",
                value: "Avanzado");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 9L,
                column: "label",
                value: "Basis");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 10L,
                column: "label",
                value: "Erweitert");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 11L,
                column: "label",
                value: "基础版");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 12L,
                column: "label",
                value: "高级版");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 13L,
                column: "label",
                value: "الأساسية");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 14L,
                column: "label",
                value: "المتقدمة");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 1L,
                column: "label",
                value: "Basic");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 3L,
                column: "label",
                value: "Advanced");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 5L,
                column: "label",
                value: "Basic");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 6L,
                column: "label",
                value: "Advanced");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 7L,
                column: "label",
                value: "Basic");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 8L,
                column: "label",
                value: "Advanced");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 9L,
                column: "label",
                value: "Basic");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 10L,
                column: "label",
                value: "Advanced");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 11L,
                column: "label",
                value: "Basic");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 12L,
                column: "label",
                value: "Advanced");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 13L,
                column: "label",
                value: "Basic");

            migrationBuilder.UpdateData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 14L,
                column: "label",
                value: "Advanced");
        }
    }
}
