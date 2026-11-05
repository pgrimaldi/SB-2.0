using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Module.Catalog.Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class DiscountsCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "requires_code",
                schema: "catalog",
                table: "pricing_discounts",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "true = si applica digitando il codice; false = automatico nei periodi, giorni e fasce di validità.");

            migrationBuilder.AddColumn<Weekday[]>(
                name: "weekdays",
                schema: "catalog",
                table: "pricing_discounts",
                type: "catalog.weekday[]",
                nullable: false,
                defaultValueSql: "'{}'",
                comment: "Giorni di validità; vuoto = tutti.");

            migrationBuilder.CreateTable(
                name: "pricing_discount_time_ranges",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    discount_id = table.Column<long>(type: "bigint", nullable: false, comment: "Sconto."),
                    time_from = table.Column<TimeOnly>(type: "time", nullable: false, comment: "Inizio della fascia."),
                    time_to = table.Column<TimeOnly>(type: "time", nullable: false, comment: "Fine della fascia."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_discount_time_ranges", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_discount_time_ranges_range", "time_to > time_from");
                    table.ForeignKey(
                        name: "fk_catalog_pricing_discount_time_ranges_discount",
                        column: x => x.discount_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_discounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Fasce orarie di validità di uno sconto.");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_discount_time_ranges_discount",
                schema: "catalog",
                table: "pricing_discount_time_ranges",
                column: "discount_id",
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "pricing_discount_time_ranges",
                schema: "catalog");

            migrationBuilder.DropColumn(
                name: "requires_code",
                schema: "catalog",
                table: "pricing_discounts");

            migrationBuilder.DropColumn(
                name: "weekdays",
                schema: "catalog",
                table: "pricing_discounts");
        }
    }
}
