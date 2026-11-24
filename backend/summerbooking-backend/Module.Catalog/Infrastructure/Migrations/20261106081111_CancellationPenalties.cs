using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CancellationPenalties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short[]>(
                name: "cancellation_penalty_hours",
                schema: "catalog",
                table: "bathhouses",
                type: "smallint[]",
                nullable: false,
                defaultValueSql: "'{}'",
                comment: "Trattenute sull'annullamento online: ore di preavviso sotto cui vale ciascuna (al massimo due).");

            migrationBuilder.AddColumn<short[]>(
                name: "cancellation_penalty_percents",
                schema: "catalog",
                table: "bathhouses",
                type: "smallint[]",
                nullable: false,
                defaultValueSql: "'{}'",
                comment: "Percentuale trattenuta per ciascuna soglia di cancellation_penalty_hours, nello stesso ordine.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cancellation_penalty_hours",
                schema: "catalog",
                table: "bathhouses");

            migrationBuilder.DropColumn(
                name: "cancellation_penalty_percents",
                schema: "catalog",
                table: "bathhouses");
        }
    }
}
