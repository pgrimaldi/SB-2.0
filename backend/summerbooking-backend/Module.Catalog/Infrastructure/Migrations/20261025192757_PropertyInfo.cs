using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PropertyInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "virtual_tour_url",
                schema: "catalog",
                table: "bathhouses",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true,
                comment: "Link al tour virtuale (Matterport, 360°), mostrato sulla pagina pubblica se valorizzato.");

            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "bathhouse_photos",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "Id pubblico (UUIDv7) usato dalle API.");

            // UUIDv7 dal momento di creazione (PostgreSQL 17 non ha uuidv7()).
            migrationBuilder.Sql("""
                UPDATE catalog.bathhouse_photos SET public_id = encode(set_bit(set_bit(overlay(uuid_send(gen_random_uuid())
                    placing substring(int8send((extract(epoch from created_at) * 1000)::bigint) from 3) from 1 for 6),
                    52, 1), 53, 1), 'hex')::uuid;
                """);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_bathhouse_photos_public_id",
                schema: "catalog",
                table: "bathhouse_photos",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_catalog_bathhouse_photos_public_id",
                schema: "catalog",
                table: "bathhouse_photos");

            migrationBuilder.DropColumn(
                name: "virtual_tour_url",
                schema: "catalog",
                table: "bathhouses");

            migrationBuilder.DropColumn(
                name: "public_id",
                schema: "catalog",
                table: "bathhouse_photos");
        }
    }
}
