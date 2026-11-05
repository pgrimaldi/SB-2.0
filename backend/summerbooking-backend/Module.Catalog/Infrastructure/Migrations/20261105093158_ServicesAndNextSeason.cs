using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Module.Catalog.Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ServicesAndNextSeason : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Giorni e fasce dei prezzi automatici passano dentro i periodi. Non ci sono dati da convertire (nessun prezzo
            // automatico arriva dalla migrazione): se ce ne fossero, ci si ferma invece di perderli.
            migrationBuilder.Sql(@"
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM catalog.automatic_price_time_ranges)
       OR EXISTS (SELECT 1 FROM catalog.automatic_prices WHERE cardinality(weekdays) > 0) THEN
        RAISE EXCEPTION 'automatic_prices con giorni o fasce orarie: vanno convertiti a mano nei periodi prima di questa migration';
    END IF;
END $$;");

            migrationBuilder.DropForeignKey(
                name: "fk_catalog_automatic_price_time_ranges_price",
                schema: "catalog",
                table: "automatic_price_time_ranges");

            migrationBuilder.DropIndex(
                name: "ix_catalog_automatic_price_time_ranges_price",
                schema: "catalog",
                table: "automatic_price_time_ranges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_automatic_price_periods_range",
                schema: "catalog",
                table: "automatic_price_periods");

            migrationBuilder.DropColumn(
                name: "weekdays",
                schema: "catalog",
                table: "automatic_prices");

            migrationBuilder.DropColumn(
                name: "automatic_price_id",
                schema: "catalog",
                table: "automatic_price_time_ranges");

            migrationBuilder.AlterTable(
                name: "service_sale_item_time_slots",
                schema: "catalog",
                comment: "Durata prenotabile di un servizio orario (kind = hourly) con il suo prezzo, dentro una disponibilità (service_hourly_options).",
                oldComment: "Fascia oraria interna prenotabile di un servizio a fascia oraria (kind = hourly), con eventuale prezzo dedicato.");

            migrationBuilder.AlterTable(
                name: "automatic_price_time_ranges",
                schema: "catalog",
                comment: "Fasce orarie di un periodo di validità di un prezzo automatico.",
                oldComment: "Fasce orarie di validità di un prezzo automatico.");

            migrationBuilder.AlterTable(
                name: "automatic_price_periods",
                schema: "catalog",
                comment: "Periodi di validità di un prezzo automatico, ciascuno con i suoi giorni e le sue fasce orarie.",
                oldComment: "Periodi di validità di un prezzo automatico.");

            migrationBuilder.AddColumn<bool>(
                name: "is_custom",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Durata personalizzata dal gestore, oltre a quelle standard.");

            migrationBuilder.AddColumn<long>(
                name: "option_id",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                type: "bigint",
                nullable: true,
                comment: "Disponibilità a cui appartiene la durata (service_hourly_options).");

            migrationBuilder.AddColumn<long>(
                name: "period_id",
                schema: "catalog",
                table: "automatic_price_time_ranges",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                comment: "Periodo di validità (automatic_price_periods).");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "starts_on",
                schema: "catalog",
                table: "automatic_price_periods",
                type: "date",
                nullable: true,
                comment: "Primo giorno; NULL = dall'inizio della stagione.",
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldComment: "Primo giorno.");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "ends_on",
                schema: "catalog",
                table: "automatic_price_periods",
                type: "date",
                nullable: true,
                comment: "Ultimo giorno; NULL = fino alla fine della stagione.",
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldComment: "Ultimo giorno.");

            migrationBuilder.AddColumn<Weekday[]>(
                name: "weekdays",
                schema: "catalog",
                table: "automatic_price_periods",
                type: "catalog.weekday[]",
                nullable: false,
                defaultValueSql: "'{}'",
                comment: "Giorni di validità nel periodo; vuoto = tutti.");

            migrationBuilder.CreateTable(
                name: "pricing_next_season_periods",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    season_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stagione (pricing_seasons) di cui è la stagione successiva."),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, comment: "Nome del periodo tariffario (es. Alta stagione)."),
                    period_group_key = table.Column<short>(type: "smallint", nullable: false, comment: "Chiave che raggruppa gli intervalli dello stesso periodo nella stagione successiva."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Inizio dell'intervallo."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fine dell'intervallo."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_next_season_periods", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_next_season_periods_range", "ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_pricing_next_season_periods_season",
                        column: x => x.season_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Periodi tariffari della stagione successiva, preparati in anticipo: una riga per intervallo. Separati da pricing_tariff_periods perché listini, preventivi e ricerca usano solo la stagione corrente.");

            migrationBuilder.CreateTable(
                name: "service_hourly_options",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico."),
                    sale_item_id = table.Column<long>(type: "bigint", nullable: false, comment: "Servizio orario."),
                    period_group_key = table.Column<short>(type: "smallint", nullable: true, comment: "Periodo tariffario dei prezzi (pricing_tariff_periods.period_group_key); NULL = senza periodo o legata a un'opzione di prenotazione."),
                    booking_option_id = table.Column<long>(type: "bigint", nullable: true, comment: "Opzione di prenotazione con date a cui appartiene; NULL = fa parte dei prezzi del servizio."),
                    time_from = table.Column<TimeOnly>(type: "time without time zone", nullable: true, comment: "Inizio della fascia del giorno in cui si prenota; NULL = dall'apertura."),
                    time_to = table.Column<TimeOnly>(type: "time without time zone", nullable: true, comment: "Fine della fascia del giorno; NULL = fino alla chiusura."),
                    weekdays = table.Column<Weekday[]>(type: "catalog.weekday[]", nullable: false, defaultValueSql: "'{}'", comment: "Giorni in cui vale; vuoto = tutti."),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true, comment: "Aliquota IVA in percentuale; NULL = quella del servizio."),
                    max_per_slot = table.Column<short>(type: "smallint", nullable: true, comment: "Massimo di prenotazioni per fascia; NULL = senza limite."),
                    has_fixed_time_slots = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Le prenotazioni partono a orari fissi, uno dopo l'altro, invece che a qualsiasi orario."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordine delle disponibilità del servizio."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_hourly_options", x => x.id);
                    table.CheckConstraint("ck_catalog_service_hourly_options_hours", "time_from IS NULL OR time_to IS NULL OR time_to > time_from");
                    table.CheckConstraint("ck_catalog_service_hourly_options_owner", "period_group_key IS NULL OR booking_option_id IS NULL");
                    table.ForeignKey(
                        name: "fk_catalog_service_hourly_options_booking_option",
                        column: x => x.booking_option_id,
                        principalSchema: "catalog",
                        principalTable: "service_booking_options",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_service_hourly_options_item",
                        column: x => x.sale_item_id,
                        principalSchema: "catalog",
                        principalTable: "service_sale_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Disponibilità di un servizio orario: fascia del giorno, giorni, durate con prezzo, IVA e massimo per fascia. Appartiene ai prezzi del servizio (per periodo tariffario o senza periodo) oppure a un'opzione di prenotazione con date.");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_sale_item_time_slots_option",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                column: "option_id",
                filter: "option_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_automatic_price_time_ranges_period",
                schema: "catalog",
                table: "automatic_price_time_ranges",
                column: "period_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_automatic_price_periods_range",
                schema: "catalog",
                table: "automatic_price_periods",
                sql: "starts_on IS NULL OR ends_on IS NULL OR ends_on >= starts_on");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_next_season_periods_season_group",
                schema: "catalog",
                table: "pricing_next_season_periods",
                columns: new[] { "season_id", "period_group_key" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_hourly_options_booking_option",
                schema: "catalog",
                table: "service_hourly_options",
                column: "booking_option_id",
                filter: "booking_option_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_hourly_options_item",
                schema: "catalog",
                table: "service_hourly_options",
                column: "sale_item_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_service_hourly_options_booking_option_id",
                schema: "catalog",
                table: "service_hourly_options",
                column: "booking_option_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_service_hourly_options_public_id",
                schema: "catalog",
                table: "service_hourly_options",
                column: "public_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_automatic_price_time_ranges_period",
                schema: "catalog",
                table: "automatic_price_time_ranges",
                column: "period_id",
                principalSchema: "catalog",
                principalTable: "automatic_price_periods",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_service_sale_item_time_slots_option",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                column: "option_id",
                principalSchema: "catalog",
                principalTable: "service_hourly_options",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            // Servizi orari: ogni servizio aveva una sola configurazione (orari, giorni, IVA, massimo per fascia, durate).
            // Diventa una disponibilità senza periodo, a cui si agganciano le durate; l'opzione di prenotazione senza date
            // che ne portava orari e massimo non serve più.
            migrationBuilder.Sql(@"
INSERT INTO catalog.service_hourly_options
    (public_id, sale_item_id, time_from, time_to, weekdays, vat_rate, max_per_slot, has_fixed_time_slots, sort_order, created_at, updated_at)
SELECT encode(set_bit(set_bit(overlay(uuid_send(gen_random_uuid()) PLACING substring(int8send(floor(extract(epoch FROM clock_timestamp()) * 1000)::bigint) FROM 3) FROM 1 FOR 6), 52, 1), 53, 1), 'hex')::uuid,
       s.id,
       CASE WHEN s.hour_from IS NOT NULL AND s.hour_to IS NOT NULL AND s.hour_to <= s.hour_from THEN NULL ELSE s.hour_from END,
       CASE WHEN s.hour_from IS NOT NULL AND s.hour_to IS NOT NULL AND s.hour_to <= s.hour_from THEN NULL ELSE s.hour_to END,
       s.active_weekdays, s.vat_rate, bo.max_per_slot, s.has_fixed_time_slots, 0, s.created_at, s.updated_at
FROM catalog.service_sale_items s
LEFT JOIN LATERAL (
    SELECT o.max_per_slot FROM catalog.service_booking_options o
    WHERE o.sale_item_id = s.id AND o.deleted_at IS NULL ORDER BY o.sort_order LIMIT 1) bo ON true
WHERE s.kind = 'hourly' AND s.deleted_at IS NULL
  AND NOT EXISTS (SELECT 1 FROM catalog.service_hourly_options h WHERE h.sale_item_id = s.id);

UPDATE catalog.service_sale_item_time_slots t
SET option_id = h.id
FROM catalog.service_hourly_options h
WHERE h.sale_item_id = t.sale_item_id AND h.booking_option_id IS NULL AND t.option_id IS NULL;

DELETE FROM catalog.service_booking_options o
USING catalog.service_sale_items s
WHERE s.id = o.sale_item_id AND s.kind = 'hourly' AND o.starts_on IS NULL AND o.ends_on IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_catalog_automatic_price_time_ranges_period",
                schema: "catalog",
                table: "automatic_price_time_ranges");

            migrationBuilder.DropForeignKey(
                name: "fk_catalog_service_sale_item_time_slots_option",
                schema: "catalog",
                table: "service_sale_item_time_slots");

            migrationBuilder.DropTable(
                name: "pricing_next_season_periods",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_hourly_options",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "ix_catalog_service_sale_item_time_slots_option",
                schema: "catalog",
                table: "service_sale_item_time_slots");

            migrationBuilder.DropIndex(
                name: "ix_catalog_automatic_price_time_ranges_period",
                schema: "catalog",
                table: "automatic_price_time_ranges");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_automatic_price_periods_range",
                schema: "catalog",
                table: "automatic_price_periods");

            migrationBuilder.DropColumn(
                name: "is_custom",
                schema: "catalog",
                table: "service_sale_item_time_slots");

            migrationBuilder.DropColumn(
                name: "option_id",
                schema: "catalog",
                table: "service_sale_item_time_slots");

            migrationBuilder.DropColumn(
                name: "period_id",
                schema: "catalog",
                table: "automatic_price_time_ranges");

            migrationBuilder.DropColumn(
                name: "weekdays",
                schema: "catalog",
                table: "automatic_price_periods");

            migrationBuilder.AlterTable(
                name: "service_sale_item_time_slots",
                schema: "catalog",
                comment: "Fascia oraria interna prenotabile di un servizio a fascia oraria (kind = hourly), con eventuale prezzo dedicato.",
                oldComment: "Durata prenotabile di un servizio orario (kind = hourly) con il suo prezzo, dentro una disponibilità (service_hourly_options).");

            migrationBuilder.AlterTable(
                name: "automatic_price_time_ranges",
                schema: "catalog",
                comment: "Fasce orarie di validità di un prezzo automatico.",
                oldComment: "Fasce orarie di un periodo di validità di un prezzo automatico.");

            migrationBuilder.AlterTable(
                name: "automatic_price_periods",
                schema: "catalog",
                comment: "Periodi di validità di un prezzo automatico.",
                oldComment: "Periodi di validità di un prezzo automatico, ciascuno con i suoi giorni e le sue fasce orarie.");

            migrationBuilder.AddColumn<Weekday[]>(
                name: "weekdays",
                schema: "catalog",
                table: "automatic_prices",
                type: "catalog.weekday[]",
                nullable: false,
                defaultValueSql: "'{}'",
                comment: "Giorni di validità; vuoto = tutti.");

            migrationBuilder.AddColumn<long>(
                name: "automatic_price_id",
                schema: "catalog",
                table: "automatic_price_time_ranges",
                type: "bigint",
                nullable: false,
                defaultValue: 0L,
                comment: "Prezzo automatico.");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "starts_on",
                schema: "catalog",
                table: "automatic_price_periods",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                comment: "Primo giorno.",
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true,
                oldComment: "Primo giorno; NULL = dall'inizio della stagione.");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "ends_on",
                schema: "catalog",
                table: "automatic_price_periods",
                type: "date",
                nullable: false,
                defaultValue: new DateOnly(1, 1, 1),
                comment: "Ultimo giorno.",
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true,
                oldComment: "Ultimo giorno; NULL = fino alla fine della stagione.");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_automatic_price_time_ranges_price",
                schema: "catalog",
                table: "automatic_price_time_ranges",
                column: "automatic_price_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_automatic_price_periods_range",
                schema: "catalog",
                table: "automatic_price_periods",
                sql: "ends_on >= starts_on");

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_automatic_price_time_ranges_price",
                schema: "catalog",
                table: "automatic_price_time_ranges",
                column: "automatic_price_id",
                principalSchema: "catalog",
                principalTable: "automatic_prices",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
