using Microsoft.EntityFrameworkCore.Migrations;
using Module.Catalog.Domain.Enums;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgreementsCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_catalog_agreement_price_list_items_included_service",
                schema: "catalog",
                table: "agreement_price_list_items");

            migrationBuilder.DropIndex(
                name: "ix_agreement_price_list_items_included_service_id",
                schema: "catalog",
                table: "agreement_price_list_items");

            migrationBuilder.DropColumn(
                name: "included_service_id",
                schema: "catalog",
                table: "agreement_price_list_items");

            migrationBuilder.AddColumn<bool>(
                name: "is_cumulative",
                schema: "catalog",
                table: "agreement_price_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Prezzo cumulativo: prezzo dei giorni totali su più postazioni.");

            migrationBuilder.AddColumn<VatNature>(
                name: "vat_nature",
                schema: "catalog",
                table: "agreement_price_lists",
                type: "catalog.vat_nature",
                nullable: true,
                comment: "Natura dell'esenzione IVA (n1..n6), solo senza aliquota.");

            migrationBuilder.AddColumn<decimal>(
                name: "vat_rate",
                schema: "catalog",
                table: "agreement_price_lists",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                comment: "Aliquota IVA in percentuale; NULL = esente, con la natura in vat_nature.");

            migrationBuilder.AlterColumn<string>(
                name: "period_label",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "Etichetta legacy «fila + periodo», solo traccia della migrazione: valgono sector_id e tariff_period_group.",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true,
                oldComment: "Etichetta libera del periodo tariffario; NULL = tariffa unica del listino (nessun collegamento a stagioni).");

            migrationBuilder.AddColumn<decimal>(
                name: "half_day_14_price",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true,
                comment: "Mezza giornata per 14 giorni nel periodo.");

            migrationBuilder.AddColumn<decimal>(
                name: "half_day_7_price",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true,
                comment: "Mezza giornata per 7 giorni nel periodo.");

            migrationBuilder.AddColumn<decimal>(
                name: "half_day_monthly_price",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true,
                comment: "Mezza giornata per il mese nel periodo.");

            migrationBuilder.AddColumn<long>(
                name: "sector_id",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                type: "bigint",
                nullable: true,
                comment: "Fila/settore; NULL = tutte le file.");

            migrationBuilder.AddColumn<short>(
                name: "tariff_period_group",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                type: "smallint",
                nullable: true,
                comment: "Periodo tariffario della stagione (pricing_tariff_periods.period_group_key); NULL = senza periodo.");

            migrationBuilder.AddColumn<long>(
                name: "included_sale_item_id",
                schema: "catalog",
                table: "agreement_price_list_items",
                type: "bigint",
                nullable: true,
                comment: "Servizio venduto incluso nel pacchetto.");

            migrationBuilder.Sql("UPDATE catalog.agreement_price_lists SET vat_rate = 22 WHERE vat_rate IS NULL AND vat_nature IS NULL;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_agreement_price_lists_vat",
                schema: "catalog",
                table: "agreement_price_lists",
                sql: "(vat_rate IS NULL) <> (vat_nature IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_agreement_price_list_period_prices_sector_id",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                column: "sector_id");

            // Fila e periodo dall'etichetta legacy «<fila> <periodo>»: la fila è il nome di settore più lungo che fa da
            // prefisso, il resto il nome del periodo della stagione dello stabilimento. Poi i doppioni si chiudono.
            migrationBuilder.Sql("""
                WITH labelled AS (
                    SELECT pp.id, lower(trim(pp.period_label)) AS label, partner.property_id
                    FROM catalog.agreement_price_list_period_prices pp
                    JOIN catalog.agreement_price_lists pl ON pl.id = pp.price_list_id
                    JOIN catalog.agreement_partners partner ON partner.id = pl.partner_id
                    WHERE pp.period_label IS NOT NULL),
                with_sector AS (
                    SELECT l.id, l.property_id, l.label, s.id AS sector_id,
                           CASE WHEN s.id IS NULL THEN l.label ELSE trim(substr(l.label, length(s.name) + 1)) END AS period_name
                    FROM labelled l
                    LEFT JOIN LATERAL (
                        SELECT ms.id, lower(trim(ms.name)) AS name FROM catalog.map_sectors ms
                        WHERE ms.property_id = l.property_id AND ms.deleted_at IS NULL
                          AND (l.label = lower(trim(ms.name)) OR l.label LIKE lower(trim(ms.name)) || ' %')
                        ORDER BY length(ms.name) DESC LIMIT 1) s ON true)
                UPDATE catalog.agreement_price_list_period_prices pp
                SET sector_id = w.sector_id,
                    tariff_period_group = (
                        SELECT tp.period_group_key FROM catalog.pricing_tariff_periods tp
                        JOIN catalog.pricing_seasons ps ON ps.id = tp.season_id
                        WHERE ps.property_id = w.property_id AND lower(trim(tp.name)) = w.period_name AND tp.deleted_at IS NULL
                        LIMIT 1)
                FROM with_sector w
                WHERE pp.id = w.id;

                WITH dup AS (
                    SELECT id FROM (
                        SELECT id, row_number() OVER (PARTITION BY price_list_id, sector_id, tariff_period_group ORDER BY id) AS n
                        FROM catalog.agreement_price_list_period_prices WHERE deleted_at IS NULL) x
                    WHERE n > 1)
                UPDATE catalog.agreement_price_list_period_prices SET deleted_at = now() WHERE id IN (SELECT id FROM dup);
                """);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_agreement_price_list_period_prices_period",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                columns: new[] { "price_list_id", "sector_id", "tariff_period_group" },
                unique: true,
                filter: "deleted_at IS NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_agreement_price_list_items_included_sale_item_id",
                schema: "catalog",
                table: "agreement_price_list_items",
                column: "included_sale_item_id");

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_agreement_price_list_items_included_sale_item",
                schema: "catalog",
                table: "agreement_price_list_items",
                column: "included_sale_item_id",
                principalSchema: "catalog",
                principalTable: "service_sale_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_agreement_price_list_period_prices_sector",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                column: "sector_id",
                principalSchema: "catalog",
                principalTable: "map_sectors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_catalog_agreement_price_list_items_included_sale_item",
                schema: "catalog",
                table: "agreement_price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_catalog_agreement_price_list_period_prices_sector",
                schema: "catalog",
                table: "agreement_price_list_period_prices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_agreement_price_lists_vat",
                schema: "catalog",
                table: "agreement_price_lists");

            migrationBuilder.DropIndex(
                name: "ix_agreement_price_list_period_prices_sector_id",
                schema: "catalog",
                table: "agreement_price_list_period_prices");

            migrationBuilder.DropIndex(
                name: "uq_catalog_agreement_price_list_period_prices_period",
                schema: "catalog",
                table: "agreement_price_list_period_prices");

            migrationBuilder.DropIndex(
                name: "ix_agreement_price_list_items_included_sale_item_id",
                schema: "catalog",
                table: "agreement_price_list_items");

            migrationBuilder.DropColumn(
                name: "is_cumulative",
                schema: "catalog",
                table: "agreement_price_lists");

            migrationBuilder.DropColumn(
                name: "vat_nature",
                schema: "catalog",
                table: "agreement_price_lists");

            migrationBuilder.DropColumn(
                name: "vat_rate",
                schema: "catalog",
                table: "agreement_price_lists");

            migrationBuilder.DropColumn(
                name: "half_day_14_price",
                schema: "catalog",
                table: "agreement_price_list_period_prices");

            migrationBuilder.DropColumn(
                name: "half_day_7_price",
                schema: "catalog",
                table: "agreement_price_list_period_prices");

            migrationBuilder.DropColumn(
                name: "half_day_monthly_price",
                schema: "catalog",
                table: "agreement_price_list_period_prices");

            migrationBuilder.DropColumn(
                name: "sector_id",
                schema: "catalog",
                table: "agreement_price_list_period_prices");

            migrationBuilder.DropColumn(
                name: "tariff_period_group",
                schema: "catalog",
                table: "agreement_price_list_period_prices");

            migrationBuilder.DropColumn(
                name: "included_sale_item_id",
                schema: "catalog",
                table: "agreement_price_list_items");

            migrationBuilder.AlterColumn<string>(
                name: "period_label",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "Etichetta libera del periodo tariffario; NULL = tariffa unica del listino (nessun collegamento a stagioni).",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true,
                oldComment: "Etichetta legacy «fila + periodo», solo traccia della migrazione: valgono sector_id e tariff_period_group.");

            migrationBuilder.AddColumn<int>(
                name: "included_service_id",
                schema: "catalog",
                table: "agreement_price_list_items",
                type: "integer",
                nullable: true,
                comment: "Dotazione/servizio incluso.");

            migrationBuilder.CreateIndex(
                name: "ix_agreement_price_list_items_included_service_id",
                schema: "catalog",
                table: "agreement_price_list_items",
                column: "included_service_id");

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_agreement_price_list_items_included_service",
                schema: "catalog",
                table: "agreement_price_list_items",
                column: "included_service_id",
                principalSchema: "catalog",
                principalTable: "service_amenities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
