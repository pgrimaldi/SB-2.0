using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Fiskaly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingDesignedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipt_logs_property_date",
                schema: "fiskaly",
                table: "receipt_logs",
                columns: new[] { "property_id", "created_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_cash_registers_account",
                schema: "fiskaly",
                table: "cash_registers",
                column: "account_id",
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_fiskaly_receipt_logs_property_date",
                schema: "fiskaly",
                table: "receipt_logs");

            migrationBuilder.DropIndex(
                name: "ix_fiskaly_cash_registers_account",
                schema: "fiskaly",
                table: "cash_registers");
        }
    }
}
