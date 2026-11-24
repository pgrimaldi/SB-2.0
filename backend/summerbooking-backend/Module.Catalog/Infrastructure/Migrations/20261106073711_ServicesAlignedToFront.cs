using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ServicesAlignedToFront : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_service_hourly_options_booking_option_id",
                schema: "catalog",
                table: "service_hourly_options");

            // «Si applica a tutti i servizi» non c'è più: i prezzi che lo avevano si legano a tutti i servizi di oggi.
            migrationBuilder.Sql("""
                INSERT INTO catalog.automatic_price_services (automatic_price_id, sale_item_id)
                SELECT a.id, s.id
                FROM catalog.automatic_prices a
                JOIN catalog.service_sale_items s ON s.property_id = a.property_id AND s.deleted_at IS NULL
                WHERE a.applies_to_all_services AND a.deleted_at IS NULL
                  AND NOT EXISTS (SELECT 1 FROM catalog.automatic_price_services x
                                  WHERE x.automatic_price_id = a.id AND x.sale_item_id = s.id AND x.deleted_at IS NULL);
                """);

            migrationBuilder.DropColumn(
                name: "applies_to_all_services",
                schema: "catalog",
                table: "automatic_prices");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "applies_to_all_services",
                schema: "catalog",
                table: "automatic_prices",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "true = tutti i servizi; false = solo quelli di automatic_price_services.");

            migrationBuilder.CreateIndex(
                name: "ix_service_hourly_options_booking_option_id",
                schema: "catalog",
                table: "service_hourly_options",
                column: "booking_option_id",
                unique: true);
        }
    }
}
