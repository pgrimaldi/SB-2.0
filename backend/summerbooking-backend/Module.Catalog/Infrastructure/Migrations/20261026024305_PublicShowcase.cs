using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PublicShowcase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_contact_only",
                schema: "catalog",
                table: "search_bathhouses",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Struttura importata: solo contatti.");

            migrationBuilder.AddColumn<bool>(
                name: "is_online_bookable",
                schema: "catalog",
                table: "search_bathhouses",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Prenotabile online adesso (stato, vendite, abbonamento, pacchetti online).");

            migrationBuilder.AddColumn<long>(
                name: "region_id",
                schema: "catalog",
                table: "search_bathhouses",
                type: "bigint",
                nullable: true,
                comment: "Regione della vetrina (copiata da catalog.bathhouses.region_id, nessuna FK).");

            migrationBuilder.AddColumn<DateOnly>(
                name: "subscribed_since",
                schema: "catalog",
                table: "search_bathhouses",
                type: "date",
                nullable: true,
                comment: "Inizio dell'abbonamento al gestionale: secondo criterio della popolarità.");

            migrationBuilder.AddColumn<DateOnly>(
                name: "subscribed_since",
                schema: "catalog",
                table: "properties",
                type: "date",
                nullable: true,
                comment: "Primo abbonamento attivo al gestionale (da Identity): ordina la popolarità a parità di «mi piace».");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_search_bathhouses_published_region",
                schema: "catalog",
                table: "search_bathhouses",
                columns: new[] { "region_id", "like_count" },
                filter: "is_published");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_catalog_search_bathhouses_published_region",
                schema: "catalog",
                table: "search_bathhouses");

            migrationBuilder.DropColumn(
                name: "is_contact_only",
                schema: "catalog",
                table: "search_bathhouses");

            migrationBuilder.DropColumn(
                name: "is_online_bookable",
                schema: "catalog",
                table: "search_bathhouses");

            migrationBuilder.DropColumn(
                name: "region_id",
                schema: "catalog",
                table: "search_bathhouses");

            migrationBuilder.DropColumn(
                name: "subscribed_since",
                schema: "catalog",
                table: "search_bathhouses");

            migrationBuilder.DropColumn(
                name: "subscribed_since",
                schema: "catalog",
                table: "properties");
        }
    }
}
