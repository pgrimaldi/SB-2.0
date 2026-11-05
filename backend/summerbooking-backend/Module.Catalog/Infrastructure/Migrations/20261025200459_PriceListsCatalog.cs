using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Module.Catalog.Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Shared.Localization;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PriceListsCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // I prezzi ridotti erano appesi a una finestra per listino: le finestre uguali dello stesso stabilimento
            // diventano una voce «stagionale ridotto» unica. La corrispondenza si salva prima di eliminarle.
            migrationBuilder.Sql("""
                CREATE TEMP TABLE reduced_move ON COMMIT DROP AS
                SELECT rp.id AS reduced_price_id, w.price_list_id, pl.property_id, w.starts_on, w.ends_on
                FROM catalog.pricing_price_list_reduced_prices rp
                JOIN catalog.pricing_price_list_reduced_windows w ON w.id = rp.window_id
                JOIN catalog.pricing_price_lists pl ON pl.id = w.price_list_id;
                """);

            migrationBuilder.DropForeignKey(
                name: "fk_catalog_pricing_price_list_items_included_service",
                schema: "catalog",
                table: "pricing_price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_catalog_pricing_price_list_reduced_prices_window",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices");

            migrationBuilder.DropTable(
                name: "pricing_price_list_reduced_windows",
                schema: "catalog");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_pricing_price_list_stay_prices_stay_days",
                schema: "catalog",
                table: "pricing_price_list_stay_prices");

            migrationBuilder.DropIndex(
                name: "uq_catalog_pricing_price_list_reduced_prices_window_sector",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices");

            migrationBuilder.DropIndex(
                name: "uq_catalog_pricing_price_list_period_prices_package_period",
                schema: "catalog",
                table: "pricing_price_list_period_prices");

            migrationBuilder.DropIndex(
                name: "ix_pricing_price_list_items_included_service_id",
                schema: "catalog",
                table: "pricing_price_list_items");

            migrationBuilder.DropColumn(
                name: "window_id",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices");

            migrationBuilder.DropColumn(
                name: "included_service_id",
                schema: "catalog",
                table: "pricing_price_list_items");

            migrationBuilder.AlterTable(
                name: "pricing_price_list_reduced_prices",
                schema: "catalog",
                comment: "Prezzo di un pacchetto per il prezzo stagionale ridotto, per fila/settore.",
                oldComment: "Prezzo ridotto assoluto per settore, valido per una finestra di date con match esatto.");

            migrationBuilder.AddColumn<bool>(
                name: "is_map_assignable",
                schema: "catalog",
                table: "pricing_price_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Articoli aggiuntivi: assegnabile su mappa.");

            migrationBuilder.AddColumn<bool>(
                name: "is_sector_restricted",
                schema: "catalog",
                table: "pricing_price_lists",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Aree abilitate: solo le file/settori con un prezzo in pricing_price_list_sector_prices; false = tutte.");

            migrationBuilder.AddColumn<short>(
                name: "max_additional_items",
                schema: "catalog",
                table: "pricing_price_lists",
                type: "smallint",
                nullable: true,
                comment: "Pacchetti: numero massimo di articoli aggiuntivi; NULL = nessun limite.");

            migrationBuilder.AddColumn<VatNature>(
                name: "vat_nature",
                schema: "catalog",
                table: "pricing_price_lists",
                type: "catalog.vat_nature",
                nullable: true,
                comment: "Natura dell'esenzione IVA (n1..n6), solo senza aliquota.");

            migrationBuilder.AddColumn<decimal>(
                name: "vat_rate",
                schema: "catalog",
                table: "pricing_price_lists",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                comment: "Aliquota IVA in percentuale; NULL = esente, con la natura in vat_nature.");

            migrationBuilder.AlterColumn<long>(
                name: "sector_id",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                type: "bigint",
                nullable: false,
                comment: "Fila/settore.",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldComment: "Settore per cui vale il prezzo ridotto.");

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: false,
                comment: "Prezzo per l'intero periodo ridotto.",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldComment: "Prezzo assoluto del pacchetto in quel settore per l'intervallo, sostituisce il prezzo standard.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete della riga.");

            migrationBuilder.AddColumn<long>(
                name: "price_list_id",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                comment: "Pacchetto.");

            migrationBuilder.AddColumn<long>(
                name: "reduced_season_id",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                comment: "Prezzo stagionale ridotto (pricing_reduced_seasons).");

            migrationBuilder.AddColumn<decimal>(
                name: "half_day_14_price",
                schema: "catalog",
                table: "pricing_price_list_period_prices",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true,
                comment: "Mezza giornata per 14 giorni nel periodo.");

            migrationBuilder.AddColumn<decimal>(
                name: "half_day_7_price",
                schema: "catalog",
                table: "pricing_price_list_period_prices",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true,
                comment: "Mezza giornata per 7 giorni nel periodo: riferimento del calcolo; NULL = prezzo mezza giornata x giorni.");

            migrationBuilder.AddColumn<decimal>(
                name: "half_day_monthly_price",
                schema: "catalog",
                table: "pricing_price_list_period_prices",
                type: "numeric(12,2)",
                precision: 12,
                scale: 2,
                nullable: true,
                comment: "Mezza giornata per il mese nel periodo.");

            migrationBuilder.AddColumn<long>(
                name: "sector_id",
                schema: "catalog",
                table: "pricing_price_list_period_prices",
                type: "bigint",
                nullable: true,
                comment: "Fila/settore a cui si applicano i prezzi; NULL = tutte le file.");

            migrationBuilder.AddColumn<long>(
                name: "included_sale_item_id",
                schema: "catalog",
                table: "pricing_price_list_items",
                type: "bigint",
                nullable: true,
                comment: "Servizio venduto incluso nel pacchetto (per included_service).");

            migrationBuilder.CreateTable(
                name: "pricing_price_list_translations",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    price_list_id = table.Column<long>(type: "bigint", nullable: false, comment: "Voce di listino."),
                    language = table.Column<AppLanguage>(type: "catalog.content_language", nullable: false, comment: "Lingua della traduzione."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome tradotto."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_price_list_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_translations_parent",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Nome di pacchetti e articoli aggiuntivi nelle altre lingue.");

            migrationBuilder.CreateTable(
                name: "pricing_reduced_seasons",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico (UUIDv7) usato dalle API."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento."),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Nome della voce (es. Luglio e agosto)."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Primo giorno del periodo."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Ultimo giorno del periodo."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_reduced_seasons", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_reduced_seasons_range", "ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_pricing_reduced_seasons_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Prezzo stagionale ridotto: periodo più breve della stagione, voce dello stabilimento valida per tutti i pacchetti.");

            // Pacchetti e aggiuntivi partono al 22% (decisione D1).
            migrationBuilder.Sql("UPDATE catalog.pricing_price_lists SET vat_rate = 22 WHERE vat_rate IS NULL AND vat_nature IS NULL;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_pricing_price_lists_vat",
                schema: "catalog",
                table: "pricing_price_lists",
                sql: "(vat_rate IS NULL) <> (vat_nature IS NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_pricing_price_list_stay_prices_stay_days",
                schema: "catalog",
                table: "pricing_price_list_stay_prices",
                sql: "stay_days BETWEEN 1 AND 366");

            migrationBuilder.Sql("""
                INSERT INTO catalog.pricing_reduced_seasons (public_id, property_id, name, starts_on, ends_on)
                SELECT encode(set_bit(set_bit(overlay(uuid_send(gen_random_uuid())
                           placing substring(int8send((extract(epoch from now()) * 1000)::bigint) from 3) from 1 for 6),
                           52, 1), 53, 1), 'hex')::uuid,
                       property_id, 'Stagionale ridotto ' || to_char(starts_on, 'DD/MM') || ' - ' || to_char(ends_on, 'DD/MM'), starts_on, ends_on
                FROM (SELECT DISTINCT property_id, starts_on, ends_on FROM reduced_move) d;
                UPDATE catalog.pricing_price_list_reduced_prices rp
                SET price_list_id = m.price_list_id, reduced_season_id = s.id
                FROM reduced_move m
                JOIN catalog.pricing_reduced_seasons s
                  ON s.property_id = m.property_id AND s.starts_on = m.starts_on AND s.ends_on = m.ends_on
                WHERE rp.id = m.reduced_price_id;
                DELETE FROM catalog.pricing_price_list_reduced_prices WHERE price_list_id = 0 OR reduced_season_id = 0;
                """);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_list_reduced_prices_season",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                column: "reduced_season_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_reduced_prices_season_sector",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                columns: new[] { "price_list_id", "reduced_season_id", "sector_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_pricing_price_list_reduced_prices_price",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                sql: "price >= 0");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_list_period_prices_sector",
                schema: "catalog",
                table: "pricing_price_list_period_prices",
                column: "sector_id",
                filter: "deleted_at IS NULL");

            // Con i NULL non più distinti, i doppioni «senza periodo» dello stesso listino collidono: resta il primo.
            migrationBuilder.Sql("""
                WITH dup AS (
                    SELECT id FROM (
                        SELECT id, row_number() OVER (PARTITION BY price_list_id, sector_id, tariff_period_group ORDER BY id) AS n
                        FROM catalog.pricing_price_list_period_prices WHERE deleted_at IS NULL) x
                    WHERE n > 1)
                UPDATE catalog.pricing_price_list_period_prices SET deleted_at = now() WHERE id IN (SELECT id FROM dup);
                """);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_period_prices_sector_period",
                schema: "catalog",
                table: "pricing_price_list_period_prices",
                columns: new[] { "price_list_id", "sector_id", "tariff_period_group" },
                unique: true,
                filter: "deleted_at IS NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "ix_pricing_price_list_items_included_sale_item_id",
                schema: "catalog",
                table: "pricing_price_list_items",
                column: "included_sale_item_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_translations_language",
                schema: "catalog",
                table: "pricing_price_list_translations",
                columns: new[] { "price_list_id", "language" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_reduced_seasons_property",
                schema: "catalog",
                table: "pricing_reduced_seasons",
                column: "property_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_reduced_seasons_public_id",
                schema: "catalog",
                table: "pricing_reduced_seasons",
                column: "public_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_pricing_price_list_items_included_sale_item",
                schema: "catalog",
                table: "pricing_price_list_items",
                column: "included_sale_item_id",
                principalSchema: "catalog",
                principalTable: "service_sale_items",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_pricing_price_list_period_prices_sector",
                schema: "catalog",
                table: "pricing_price_list_period_prices",
                column: "sector_id",
                principalSchema: "catalog",
                principalTable: "map_sectors",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_pricing_price_list_reduced_prices_price_list",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                column: "price_list_id",
                principalSchema: "catalog",
                principalTable: "pricing_price_lists",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_pricing_price_list_reduced_prices_season",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                column: "reduced_season_id",
                principalSchema: "catalog",
                principalTable: "pricing_reduced_seasons",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_catalog_pricing_price_list_items_included_sale_item",
                schema: "catalog",
                table: "pricing_price_list_items");

            migrationBuilder.DropForeignKey(
                name: "fk_catalog_pricing_price_list_period_prices_sector",
                schema: "catalog",
                table: "pricing_price_list_period_prices");

            migrationBuilder.DropForeignKey(
                name: "fk_catalog_pricing_price_list_reduced_prices_price_list",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices");

            migrationBuilder.DropForeignKey(
                name: "fk_catalog_pricing_price_list_reduced_prices_season",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices");

            migrationBuilder.DropTable(
                name: "pricing_price_list_translations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_reduced_seasons",
                schema: "catalog");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_pricing_price_lists_vat",
                schema: "catalog",
                table: "pricing_price_lists");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_pricing_price_list_stay_prices_stay_days",
                schema: "catalog",
                table: "pricing_price_list_stay_prices");

            migrationBuilder.DropIndex(
                name: "ix_catalog_pricing_price_list_reduced_prices_season",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices");

            migrationBuilder.DropIndex(
                name: "uq_catalog_pricing_price_list_reduced_prices_season_sector",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_pricing_price_list_reduced_prices_price",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices");

            migrationBuilder.DropIndex(
                name: "ix_catalog_pricing_price_list_period_prices_sector",
                schema: "catalog",
                table: "pricing_price_list_period_prices");

            migrationBuilder.DropIndex(
                name: "uq_catalog_pricing_price_list_period_prices_sector_period",
                schema: "catalog",
                table: "pricing_price_list_period_prices");

            migrationBuilder.DropIndex(
                name: "ix_pricing_price_list_items_included_sale_item_id",
                schema: "catalog",
                table: "pricing_price_list_items");

            migrationBuilder.DropColumn(
                name: "is_map_assignable",
                schema: "catalog",
                table: "pricing_price_lists");

            migrationBuilder.DropColumn(
                name: "is_sector_restricted",
                schema: "catalog",
                table: "pricing_price_lists");

            migrationBuilder.DropColumn(
                name: "max_additional_items",
                schema: "catalog",
                table: "pricing_price_lists");

            migrationBuilder.DropColumn(
                name: "vat_nature",
                schema: "catalog",
                table: "pricing_price_lists");

            migrationBuilder.DropColumn(
                name: "vat_rate",
                schema: "catalog",
                table: "pricing_price_lists");

            migrationBuilder.DropColumn(
                name: "price_list_id",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices");

            migrationBuilder.DropColumn(
                name: "reduced_season_id",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices");

            migrationBuilder.DropColumn(
                name: "half_day_14_price",
                schema: "catalog",
                table: "pricing_price_list_period_prices");

            migrationBuilder.DropColumn(
                name: "half_day_7_price",
                schema: "catalog",
                table: "pricing_price_list_period_prices");

            migrationBuilder.DropColumn(
                name: "half_day_monthly_price",
                schema: "catalog",
                table: "pricing_price_list_period_prices");

            migrationBuilder.DropColumn(
                name: "sector_id",
                schema: "catalog",
                table: "pricing_price_list_period_prices");

            migrationBuilder.DropColumn(
                name: "included_sale_item_id",
                schema: "catalog",
                table: "pricing_price_list_items");

            migrationBuilder.AlterTable(
                name: "pricing_price_list_reduced_prices",
                schema: "catalog",
                comment: "Prezzo ridotto assoluto per settore, valido per una finestra di date con match esatto.",
                oldComment: "Prezzo di un pacchetto per il prezzo stagionale ridotto, per fila/settore.");

            migrationBuilder.AlterColumn<long>(
                name: "sector_id",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                type: "bigint",
                nullable: false,
                comment: "Settore per cui vale il prezzo ridotto.",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldComment: "Fila/settore.");

            migrationBuilder.AlterColumn<decimal>(
                name: "price",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                type: "numeric(12,2)",
                nullable: false,
                comment: "Prezzo assoluto del pacchetto in quel settore per l'intervallo, sostituisce il prezzo standard.",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldPrecision: 12,
                oldScale: 2,
                oldComment: "Prezzo per l'intero periodo ridotto.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete della riga.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete.");

            migrationBuilder.AddColumn<long>(
                name: "window_id",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                comment: "Finestra di date a cui appartiene il prezzo ridotto.");

            migrationBuilder.AddColumn<int>(
                name: "included_service_id",
                schema: "catalog",
                table: "pricing_price_list_items",
                type: "integer",
                nullable: true,
                comment: "Servizio/dotazione incluso nel pacchetto (per included_service).");

            migrationBuilder.CreateTable(
                name: "pricing_price_list_reduced_windows",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della finestra."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fine dell'intervallo che deve coincidere esattamente con quello della prenotazione."),
                    price_list_id = table.Column<long>(type: "bigint", nullable: false, comment: "Pacchetto a cui si applica la finestra di prezzo ridotto."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Inizio dell'intervallo che deve coincidere esattamente con quello della prenotazione."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_price_list_reduced_windows", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_price_list_reduced_windows_range", "ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_reduced_windows_price_list",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Finestra di date che, se coincide esattamente con quella prenotata, attiva i prezzi ridotti per settore.");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_pricing_price_list_stay_prices_stay_days",
                schema: "catalog",
                table: "pricing_price_list_stay_prices",
                sql: "stay_days BETWEEN 1 AND 90");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_reduced_prices_window_sector",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                columns: new[] { "window_id", "sector_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_period_prices_package_period",
                schema: "catalog",
                table: "pricing_price_list_period_prices",
                columns: new[] { "price_list_id", "tariff_period_group" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_price_list_items_included_service_id",
                schema: "catalog",
                table: "pricing_price_list_items",
                column: "included_service_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_reduced_windows_package_range",
                schema: "catalog",
                table: "pricing_price_list_reduced_windows",
                columns: new[] { "price_list_id", "starts_on", "ends_on" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_pricing_price_list_items_included_service",
                schema: "catalog",
                table: "pricing_price_list_items",
                column: "included_service_id",
                principalSchema: "catalog",
                principalTable: "service_amenities",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_pricing_price_list_reduced_prices_window",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                column: "window_id",
                principalSchema: "catalog",
                principalTable: "pricing_price_list_reduced_windows",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
