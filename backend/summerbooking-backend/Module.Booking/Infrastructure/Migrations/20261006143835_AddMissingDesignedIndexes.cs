using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingDesignedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_booking_reservations_property_stay",
                schema: "booking",
                table: "reservations",
                columns: new[] { "property_id", "stay_from" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resale_credit_ledger_offer",
                schema: "booking",
                table: "resale_credit_ledger",
                column: "resale_offer_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_lines_property_stay",
                schema: "booking",
                table: "lines",
                columns: new[] { "property_id", "start_date" },
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_booking_reservations_property_stay",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropIndex(
                name: "ix_booking_resale_credit_ledger_offer",
                schema: "booking",
                table: "resale_credit_ledger");

            migrationBuilder.DropIndex(
                name: "ix_booking_lines_property_stay",
                schema: "booking",
                table: "lines");
        }
    }
}
