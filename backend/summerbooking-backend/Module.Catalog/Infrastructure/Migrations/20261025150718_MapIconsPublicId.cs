using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MapIconsPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "map_layout_special_icons",
                schema: "catalog",
                comment: "Icone degli articoli personalizzati (1-8) per stato occupato/libero. Sostituisce le colonne specialUmbrella1..5 di configure_maps.",
                oldComment: "Icone delle varianti speciali di ombrellone (1-5) per stato occupato/libero. Sostituisce le colonne specialUmbrella1..5 di configure_maps.");

            migrationBuilder.AlterColumn<short>(
                name: "variant",
                schema: "catalog",
                table: "map_layout_special_icons",
                type: "smallint",
                nullable: false,
                comment: "Numero dell'articolo personalizzato (1-8, i primi 5 corrispondono a SpecialUmbrella1..5 del legacy).",
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "Indice della variante speciale (1-5, corrispondente a SpecialUmbrella1..5 nel legacy); estendibile.");

            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "map_icons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "Id pubblico (UUIDv7) usato dalle API.");

            // UUIDv7 dal momento di creazione dell'icona (PostgreSQL 17 non ha uuidv7()).
            migrationBuilder.Sql("""
                UPDATE catalog.map_icons SET public_id = encode(set_bit(set_bit(overlay(uuid_send(gen_random_uuid())
                    placing substring(int8send((extract(epoch from created_at) * 1000)::bigint) from 3) from 1 for 6),
                    52, 1), 53, 1), 'hex')::uuid;
                """);

            // numero della postazione unico senza distinguere maiuscole e minuscole
            migrationBuilder.Sql("DROP INDEX catalog.uq_catalog_map_spots_property_label;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX uq_catalog_map_spots_property_label ON catalog.map_spots (property_id, lower(label)) WHERE deleted_at IS NULL;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_map_layout_special_icons_variant",
                schema: "catalog",
                table: "map_layout_special_icons",
                sql: "variant BETWEEN 1 AND 8");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_map_icons_public_id",
                schema: "catalog",
                table: "map_icons",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX catalog.uq_catalog_map_spots_property_label;");
            migrationBuilder.Sql("CREATE UNIQUE INDEX uq_catalog_map_spots_property_label ON catalog.map_spots (property_id, label) WHERE deleted_at IS NULL;");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_map_layout_special_icons_variant",
                schema: "catalog",
                table: "map_layout_special_icons");

            migrationBuilder.DropIndex(
                name: "uq_catalog_map_icons_public_id",
                schema: "catalog",
                table: "map_icons");

            migrationBuilder.DropColumn(
                name: "public_id",
                schema: "catalog",
                table: "map_icons");

            migrationBuilder.AlterTable(
                name: "map_layout_special_icons",
                schema: "catalog",
                comment: "Icone delle varianti speciali di ombrellone (1-5) per stato occupato/libero. Sostituisce le colonne specialUmbrella1..5 di configure_maps.",
                oldComment: "Icone degli articoli personalizzati (1-8) per stato occupato/libero. Sostituisce le colonne specialUmbrella1..5 di configure_maps.");

            migrationBuilder.AlterColumn<short>(
                name: "variant",
                schema: "catalog",
                table: "map_layout_special_icons",
                type: "smallint",
                nullable: false,
                comment: "Indice della variante speciale (1-5, corrispondente a SpecialUmbrella1..5 nel legacy); estendibile.",
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "Numero dell'articolo personalizzato (1-8, i primi 5 corrispondono a SpecialUmbrella1..5 del legacy).");
        }
    }
}
