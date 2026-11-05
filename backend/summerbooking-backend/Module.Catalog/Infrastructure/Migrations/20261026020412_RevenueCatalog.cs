using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Module.Catalog.Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RevenueCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_catalog_pricing_suggested_prices_package_date_period",
                schema: "catalog",
                table: "pricing_suggested_prices");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.agreement_list_type", "regular,voucher")
                .Annotation("Npgsql:Enum:catalog.agreement_report_search_type", "check_in,check_out")
                .Annotation("Npgsql:Enum:catalog.agreement_report_status", "pending,created,cancelled")
                .Annotation("Npgsql:Enum:catalog.allergen", "gluten,crustaceans,eggs,fish,peanuts,soybeans,milk,nuts,celery,mustard,sesame,sulphites,lupin,molluscs")
                .Annotation("Npgsql:Enum:catalog.bar_category", "bar,pizzeria,restaurant,fry_shop,sandwich_shop,appetizers,first_courses,second_courses,desserts,fruit,ice_cream_shop,drinks")
                .Annotation("Npgsql:Enum:catalog.config_snapshot_kind", "property_management_config,beach_map,public_showcase,price_catalog,services_catalog,agreement_catalog")
                .Annotation("Npgsql:Enum:catalog.content_language", "it,en,fr,es,de,zh,ar")
                .Annotation("Npgsql:Enum:catalog.date_price_kind", "override,holiday")
                .Annotation("Npgsql:Enum:catalog.deposit_mode", "none,total,per_person")
                .Annotation("Npgsql:Enum:catalog.deposit_type", "percentage,amount")
                .Annotation("Npgsql:Enum:catalog.discount_applicable_channel", "manager_and_online,manager_only,online_only")
                .Annotation("Npgsql:Enum:catalog.discount_scope", "all,custom,without_custom")
                .Annotation("Npgsql:Enum:catalog.discount_service_category", "bar,normal,hourly,additional")
                .Annotation("Npgsql:Enum:catalog.discount_type", "percentage,fixed")
                .Annotation("Npgsql:Enum:catalog.inventory_article", "umbrella,sunbed,sunbed_xl,sunbed_maxi,deckchair,directors_chair,tent,gazebo,cabin,custom_cabin,parking,ticket,sunbed_area,umbrella_area,parking_area,cabin_area,custom_article1,custom_article2,custom_article3,custom_article4,custom_article5,custom_article6,custom_article7,custom_article8")
                .Annotation("Npgsql:Enum:catalog.map_icon_category", "template,umbrella,tent,sunbed,cabin")
                .Annotation("Npgsql:Enum:catalog.map_icon_state", "occupied,free")
                .Annotation("Npgsql:Enum:catalog.media_kind", "photo,pdf,icon")
                .Annotation("Npgsql:Enum:catalog.media_origin", "s3us_east2,s3us_east1,cloudfront,external")
                .Annotation("Npgsql:Enum:catalog.monthly_setting", "effective_month_duration,actual_month_duration,fixed30days")
                .Annotation("Npgsql:Enum:catalog.online_booking_scope", "all,contacts_only")
                .Annotation("Npgsql:Enum:catalog.online_payment_method", "pay_now,pay_at_property,pay_at_property_no_card,deposit_half,pay_at_property_prepaid")
                .Annotation("Npgsql:Enum:catalog.package_item_kind", "inventory_item,area_item,included_service,linked_package")
                .Annotation("Npgsql:Enum:catalog.pets_policy", "allowed,not_allowed,on_request")
                .Annotation("Npgsql:Enum:catalog.price_list_kind", "package,additional")
                .Annotation("Npgsql:Enum:catalog.property_status", "pending,approved,rejected,imported")
                .Annotation("Npgsql:Enum:catalog.revenue_season_tier", "low,high")
                .Annotation("Npgsql:Enum:catalog.sales_channel", "manager_only,manager_and_online,online_only")
                .Annotation("Npgsql:Enum:catalog.service_sale_kind", "bar,normal,hourly")
                .Annotation("Npgsql:Enum:catalog.spot_type", "umbrella,sunbed,sunbed_xl,cabin,custom_cabin,custom_article1,custom_article2,custom_article3,custom_article4,custom_article5,tent,parking,ticket,umbrella_area,cabin_area,sunbed_area,parking_area,custom_article6,custom_article7,custom_article8,beach_element")
                .Annotation("Npgsql:Enum:catalog.suggested_price_mode", "none,suggested,original,discount")
                .Annotation("Npgsql:Enum:catalog.surcharge_type", "disable,percentage,fixed")
                .Annotation("Npgsql:Enum:catalog.table_capacity_mode", "tables,seats")
                .Annotation("Npgsql:Enum:catalog.tariff_tier", "low,mid,high,peak,custom")
                .Annotation("Npgsql:Enum:catalog.vat_nature", "n1,n2,n3,n4,n5,n6")
                .Annotation("Npgsql:Enum:catalog.weekday", "monday,tuesday,wednesday,thursday,friday,saturday,sunday")
                .Annotation("Npgsql:Enum:catalog.weekend_setting", "saturday_sunday,sunday_only")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:catalog.agreement_list_type", "regular,voucher")
                .OldAnnotation("Npgsql:Enum:catalog.agreement_report_search_type", "check_in,check_out")
                .OldAnnotation("Npgsql:Enum:catalog.agreement_report_status", "pending,created,cancelled")
                .OldAnnotation("Npgsql:Enum:catalog.allergen", "gluten,crustaceans,eggs,fish,peanuts,soybeans,milk,nuts,celery,mustard,sesame,sulphites,lupin,molluscs")
                .OldAnnotation("Npgsql:Enum:catalog.bar_category", "bar,pizzeria,restaurant,fry_shop,sandwich_shop,appetizers,first_courses,second_courses,desserts,fruit,ice_cream_shop,drinks")
                .OldAnnotation("Npgsql:Enum:catalog.config_snapshot_kind", "property_management_config,beach_map,public_showcase,price_catalog,services_catalog,agreement_catalog")
                .OldAnnotation("Npgsql:Enum:catalog.content_language", "it,en,fr,es,de,zh,ar")
                .OldAnnotation("Npgsql:Enum:catalog.date_price_kind", "override,holiday")
                .OldAnnotation("Npgsql:Enum:catalog.deposit_mode", "none,total,per_person")
                .OldAnnotation("Npgsql:Enum:catalog.deposit_type", "percentage,amount")
                .OldAnnotation("Npgsql:Enum:catalog.discount_applicable_channel", "manager_and_online,manager_only,online_only")
                .OldAnnotation("Npgsql:Enum:catalog.discount_scope", "all,custom,without_custom")
                .OldAnnotation("Npgsql:Enum:catalog.discount_service_category", "bar,normal,hourly,additional")
                .OldAnnotation("Npgsql:Enum:catalog.discount_type", "percentage,fixed")
                .OldAnnotation("Npgsql:Enum:catalog.inventory_article", "umbrella,sunbed,sunbed_xl,sunbed_maxi,deckchair,directors_chair,tent,gazebo,cabin,custom_cabin,parking,ticket,sunbed_area,umbrella_area,parking_area,cabin_area,custom_article1,custom_article2,custom_article3,custom_article4,custom_article5,custom_article6,custom_article7,custom_article8")
                .OldAnnotation("Npgsql:Enum:catalog.map_icon_category", "template,umbrella,tent,sunbed,cabin")
                .OldAnnotation("Npgsql:Enum:catalog.map_icon_state", "occupied,free")
                .OldAnnotation("Npgsql:Enum:catalog.media_kind", "photo,pdf,icon")
                .OldAnnotation("Npgsql:Enum:catalog.media_origin", "s3us_east2,s3us_east1,cloudfront,external")
                .OldAnnotation("Npgsql:Enum:catalog.monthly_setting", "effective_month_duration,actual_month_duration,fixed30days")
                .OldAnnotation("Npgsql:Enum:catalog.online_booking_scope", "all,contacts_only")
                .OldAnnotation("Npgsql:Enum:catalog.online_payment_method", "pay_now,pay_at_property,pay_at_property_no_card,deposit_half,pay_at_property_prepaid")
                .OldAnnotation("Npgsql:Enum:catalog.package_item_kind", "inventory_item,area_item,included_service,linked_package")
                .OldAnnotation("Npgsql:Enum:catalog.pets_policy", "allowed,not_allowed,on_request")
                .OldAnnotation("Npgsql:Enum:catalog.price_list_kind", "package,additional")
                .OldAnnotation("Npgsql:Enum:catalog.property_status", "pending,approved,rejected,imported")
                .OldAnnotation("Npgsql:Enum:catalog.sales_channel", "manager_only,manager_and_online,online_only")
                .OldAnnotation("Npgsql:Enum:catalog.service_sale_kind", "bar,normal,hourly")
                .OldAnnotation("Npgsql:Enum:catalog.spot_type", "umbrella,sunbed,sunbed_xl,cabin,custom_cabin,custom_article1,custom_article2,custom_article3,custom_article4,custom_article5,tent,parking,ticket,umbrella_area,cabin_area,sunbed_area,parking_area,custom_article6,custom_article7,custom_article8,beach_element")
                .OldAnnotation("Npgsql:Enum:catalog.suggested_price_mode", "none,suggested,original,discount")
                .OldAnnotation("Npgsql:Enum:catalog.surcharge_type", "disable,percentage,fixed")
                .OldAnnotation("Npgsql:Enum:catalog.table_capacity_mode", "tables,seats")
                .OldAnnotation("Npgsql:Enum:catalog.tariff_tier", "low,mid,high,peak,custom")
                .OldAnnotation("Npgsql:Enum:catalog.vat_nature", "n1,n2,n3,n4,n5,n6")
                .OldAnnotation("Npgsql:Enum:catalog.weekday", "monday,tuesday,wednesday,thursday,friday,saturday,sunday")
                .OldAnnotation("Npgsql:Enum:catalog.weekend_setting", "saturday_sunday,sunday_only")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AddColumn<long>(
                name: "sector_id",
                schema: "catalog",
                table: "pricing_suggested_prices",
                type: "bigint",
                nullable: true,
                comment: "Fila del prezzo di listino di partenza; NULL = tutte le file.");

            migrationBuilder.AddColumn<short>(
                name: "tariff_period_group",
                schema: "catalog",
                table: "pricing_suggested_prices",
                type: "smallint",
                nullable: true,
                comment: "Periodo tariffario della data, al momento del calcolo.");

            migrationBuilder.AddColumn<bool>(
                name: "is_suggested_price_enabled",
                schema: "catalog",
                table: "pricing_revenue_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Prezzo suggerito attivo (legacy revenue_notifications.isRevenuePricesEnabled).");

            migrationBuilder.CreateTable(
                name: "pricing_revenue_standard_periods",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Nome del periodo."),
                    start_month = table.Column<short>(type: "smallint", nullable: false, comment: "Mese di inizio."),
                    start_day = table.Column<short>(type: "smallint", nullable: false, comment: "Giorno di inizio."),
                    end_month = table.Column<short>(type: "smallint", nullable: false, comment: "Mese di fine."),
                    end_day = table.Column<short>(type: "smallint", nullable: false, comment: "Giorno di fine (può essere prima dell'inizio: il periodo scavalca il capodanno)."),
                    tier = table.Column<RevenueSeasonTier>(type: "catalog.revenue_season_tier", nullable: false, comment: "Bassa o alta stagione."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordine di visualizzazione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_revenue_standard_periods", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_revenue_standard_periods_dates", "start_month BETWEEN 1 AND 12 AND end_month BETWEEN 1 AND 12 AND start_day BETWEEN 1 AND 31 AND end_day BETWEEN 1 AND 31");
                },
                comment: "Periodi standard del prezzo suggerito (systemadmin), uguali per tutti e ripetuti ogni anno; fuori dai periodi vale la bassa stagione.");

            migrationBuilder.CreateTable(
                name: "pricing_revenue_weights",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false, comment: "Sempre 1."),
                    rain_low_season_factor = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false, comment: "Moltiplicatore con pioggia in bassa stagione."),
                    rain_high_season_factor = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false, comment: "Moltiplicatore con pioggia in alta stagione."),
                    before_weekend_factor = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false, comment: "Moltiplicatore del giorno prima del weekend."),
                    high_season_factor = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false, comment: "Moltiplicatore di alta stagione (se non è il giorno prima del weekend)."),
                    free_spots_threshold_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, comment: "Soglia dei posti liberi in percentuale."),
                    free_spots_above_factor = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false, comment: "Moltiplicatore con posti liberi dalla soglia in su."),
                    free_spots_below_factor = table.Column<decimal>(type: "numeric(5,3)", precision: 5, scale: 3, nullable: false, comment: "Moltiplicatore con posti liberi sotto la soglia."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_revenue_weights", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_revenue_weights_factors", "rain_low_season_factor > 0 AND rain_high_season_factor > 0 AND before_weekend_factor > 0 AND high_season_factor > 0 AND free_spots_above_factor > 0 AND free_spots_below_factor > 0 AND free_spots_threshold_percent BETWEEN 0 AND 100");
                    table.CheckConstraint("ck_catalog_pricing_revenue_weights_singleton", "id = 1");
                },
                comment: "Pesi del prezzo suggerito (systemadmin), uguali per tutti gli stabilimenti: riga unica id = 1.");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_suggested_prices_sector_id",
                schema: "catalog",
                table: "pricing_suggested_prices",
                column: "sector_id");

            // Valori legacy: alta stagione fissa 15/07-26/08, pesi del calcolo con la soglia dei posti liberi al 30%.
            migrationBuilder.Sql("""
                INSERT INTO catalog.pricing_revenue_standard_periods (name, start_month, start_day, end_month, end_day, tier, sort_order)
                VALUES ('Alta stagione', 7, 15, 8, 26, 'high', 0);
                INSERT INTO catalog.pricing_revenue_weights (id, rain_low_season_factor, rain_high_season_factor, before_weekend_factor,
                    high_season_factor, free_spots_threshold_percent, free_spots_above_factor, free_spots_below_factor)
                VALUES (1, 0.70, 0.80, 1.25, 1.20, 30, 0.70, 1.10);
                """);

            // Fila e periodo dall'etichetta legacy «<fila> <periodo>», come per i prezzi dei listini. Il legacy calcolava un
            // suggerimento per ogni periodo in ogni data: resta quello del periodo che vale nel giorno (il più breve fra
            // quelli che lo coprono), preferendo una riga già scelta dal gestore; le altre si chiudono.
            migrationBuilder.Sql("""
                WITH labelled AS (
                    SELECT sp.id, lower(trim(sp.period_label)) AS label, sp.property_id
                    FROM catalog.pricing_suggested_prices sp
                    WHERE sp.period_label IS NOT NULL),
                with_sector AS (
                    SELECT l.id, l.property_id, s.id AS sector_id,
                           CASE WHEN s.id IS NULL THEN l.label ELSE trim(substr(l.label, length(s.name) + 1)) END AS period_name
                    FROM labelled l
                    LEFT JOIN LATERAL (
                        SELECT ms.id, lower(trim(ms.name)) AS name FROM catalog.map_sectors ms
                        WHERE ms.property_id = l.property_id AND ms.deleted_at IS NULL
                          AND (l.label = lower(trim(ms.name)) OR l.label LIKE lower(trim(ms.name)) || ' %')
                        ORDER BY length(ms.name) DESC LIMIT 1) s ON true)
                UPDATE catalog.pricing_suggested_prices sp
                SET sector_id = w.sector_id,
                    tariff_period_group = (
                        SELECT tp.period_group_key FROM catalog.pricing_tariff_periods tp
                        JOIN catalog.pricing_seasons ps ON ps.id = tp.season_id
                        WHERE ps.property_id = w.property_id AND lower(trim(tp.name)) = w.period_name AND tp.deleted_at IS NULL
                        LIMIT 1)
                FROM with_sector w
                WHERE sp.id = w.id;

                WITH ranked AS (
                    SELECT sp.id, row_number() OVER (
                        PARTITION BY sp.price_list_id, sp.price_date, sp.sector_id
                        ORDER BY (sp.tariff_period_group IS NOT DISTINCT FROM (
                                     SELECT tp.period_group_key FROM catalog.pricing_tariff_periods tp
                                     JOIN catalog.pricing_seasons ps ON ps.id = tp.season_id
                                     WHERE ps.property_id = sp.property_id AND tp.deleted_at IS NULL
                                       AND tp.starts_on <= sp.price_date AND sp.price_date <= tp.ends_on
                                     ORDER BY tp.ends_on - tp.starts_on, tp.id LIMIT 1)) DESC,
                                 (sp.applied_mode <> 'none') DESC, sp.id) AS n
                    FROM catalog.pricing_suggested_prices sp
                    WHERE sp.deleted_at IS NULL)
                UPDATE catalog.pricing_suggested_prices SET deleted_at = now() WHERE id IN (SELECT id FROM ranked WHERE n > 1);
                """);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_suggested_prices_package_date_sector",
                schema: "catalog",
                table: "pricing_suggested_prices",
                columns: new[] { "price_list_id", "price_date", "sector_id" },
                unique: true,
                filter: "deleted_at IS NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_pricing_suggested_prices_sector",
                schema: "catalog",
                table: "pricing_suggested_prices",
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
                name: "fk_catalog_pricing_suggested_prices_sector",
                schema: "catalog",
                table: "pricing_suggested_prices");

            migrationBuilder.DropTable(
                name: "pricing_revenue_standard_periods",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_revenue_weights",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "ix_pricing_suggested_prices_sector_id",
                schema: "catalog",
                table: "pricing_suggested_prices");

            migrationBuilder.DropIndex(
                name: "uq_catalog_pricing_suggested_prices_package_date_sector",
                schema: "catalog",
                table: "pricing_suggested_prices");

            migrationBuilder.DropColumn(
                name: "sector_id",
                schema: "catalog",
                table: "pricing_suggested_prices");

            migrationBuilder.DropColumn(
                name: "tariff_period_group",
                schema: "catalog",
                table: "pricing_suggested_prices");

            migrationBuilder.DropColumn(
                name: "is_suggested_price_enabled",
                schema: "catalog",
                table: "pricing_revenue_settings");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.agreement_list_type", "regular,voucher")
                .Annotation("Npgsql:Enum:catalog.agreement_report_search_type", "check_in,check_out")
                .Annotation("Npgsql:Enum:catalog.agreement_report_status", "pending,created,cancelled")
                .Annotation("Npgsql:Enum:catalog.allergen", "gluten,crustaceans,eggs,fish,peanuts,soybeans,milk,nuts,celery,mustard,sesame,sulphites,lupin,molluscs")
                .Annotation("Npgsql:Enum:catalog.bar_category", "bar,pizzeria,restaurant,fry_shop,sandwich_shop,appetizers,first_courses,second_courses,desserts,fruit,ice_cream_shop,drinks")
                .Annotation("Npgsql:Enum:catalog.config_snapshot_kind", "property_management_config,beach_map,public_showcase,price_catalog,services_catalog,agreement_catalog")
                .Annotation("Npgsql:Enum:catalog.content_language", "it,en,fr,es,de,zh,ar")
                .Annotation("Npgsql:Enum:catalog.date_price_kind", "override,holiday")
                .Annotation("Npgsql:Enum:catalog.deposit_mode", "none,total,per_person")
                .Annotation("Npgsql:Enum:catalog.deposit_type", "percentage,amount")
                .Annotation("Npgsql:Enum:catalog.discount_applicable_channel", "manager_and_online,manager_only,online_only")
                .Annotation("Npgsql:Enum:catalog.discount_scope", "all,custom,without_custom")
                .Annotation("Npgsql:Enum:catalog.discount_service_category", "bar,normal,hourly,additional")
                .Annotation("Npgsql:Enum:catalog.discount_type", "percentage,fixed")
                .Annotation("Npgsql:Enum:catalog.inventory_article", "umbrella,sunbed,sunbed_xl,sunbed_maxi,deckchair,directors_chair,tent,gazebo,cabin,custom_cabin,parking,ticket,sunbed_area,umbrella_area,parking_area,cabin_area,custom_article1,custom_article2,custom_article3,custom_article4,custom_article5,custom_article6,custom_article7,custom_article8")
                .Annotation("Npgsql:Enum:catalog.map_icon_category", "template,umbrella,tent,sunbed,cabin")
                .Annotation("Npgsql:Enum:catalog.map_icon_state", "occupied,free")
                .Annotation("Npgsql:Enum:catalog.media_kind", "photo,pdf,icon")
                .Annotation("Npgsql:Enum:catalog.media_origin", "s3us_east2,s3us_east1,cloudfront,external")
                .Annotation("Npgsql:Enum:catalog.monthly_setting", "effective_month_duration,actual_month_duration,fixed30days")
                .Annotation("Npgsql:Enum:catalog.online_booking_scope", "all,contacts_only")
                .Annotation("Npgsql:Enum:catalog.online_payment_method", "pay_now,pay_at_property,pay_at_property_no_card,deposit_half,pay_at_property_prepaid")
                .Annotation("Npgsql:Enum:catalog.package_item_kind", "inventory_item,area_item,included_service,linked_package")
                .Annotation("Npgsql:Enum:catalog.pets_policy", "allowed,not_allowed,on_request")
                .Annotation("Npgsql:Enum:catalog.price_list_kind", "package,additional")
                .Annotation("Npgsql:Enum:catalog.property_status", "pending,approved,rejected,imported")
                .Annotation("Npgsql:Enum:catalog.sales_channel", "manager_only,manager_and_online,online_only")
                .Annotation("Npgsql:Enum:catalog.service_sale_kind", "bar,normal,hourly")
                .Annotation("Npgsql:Enum:catalog.spot_type", "umbrella,sunbed,sunbed_xl,cabin,custom_cabin,custom_article1,custom_article2,custom_article3,custom_article4,custom_article5,tent,parking,ticket,umbrella_area,cabin_area,sunbed_area,parking_area,custom_article6,custom_article7,custom_article8,beach_element")
                .Annotation("Npgsql:Enum:catalog.suggested_price_mode", "none,suggested,original,discount")
                .Annotation("Npgsql:Enum:catalog.surcharge_type", "disable,percentage,fixed")
                .Annotation("Npgsql:Enum:catalog.table_capacity_mode", "tables,seats")
                .Annotation("Npgsql:Enum:catalog.tariff_tier", "low,mid,high,peak,custom")
                .Annotation("Npgsql:Enum:catalog.vat_nature", "n1,n2,n3,n4,n5,n6")
                .Annotation("Npgsql:Enum:catalog.weekday", "monday,tuesday,wednesday,thursday,friday,saturday,sunday")
                .Annotation("Npgsql:Enum:catalog.weekend_setting", "saturday_sunday,sunday_only")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:catalog.agreement_list_type", "regular,voucher")
                .OldAnnotation("Npgsql:Enum:catalog.agreement_report_search_type", "check_in,check_out")
                .OldAnnotation("Npgsql:Enum:catalog.agreement_report_status", "pending,created,cancelled")
                .OldAnnotation("Npgsql:Enum:catalog.allergen", "gluten,crustaceans,eggs,fish,peanuts,soybeans,milk,nuts,celery,mustard,sesame,sulphites,lupin,molluscs")
                .OldAnnotation("Npgsql:Enum:catalog.bar_category", "bar,pizzeria,restaurant,fry_shop,sandwich_shop,appetizers,first_courses,second_courses,desserts,fruit,ice_cream_shop,drinks")
                .OldAnnotation("Npgsql:Enum:catalog.config_snapshot_kind", "property_management_config,beach_map,public_showcase,price_catalog,services_catalog,agreement_catalog")
                .OldAnnotation("Npgsql:Enum:catalog.content_language", "it,en,fr,es,de,zh,ar")
                .OldAnnotation("Npgsql:Enum:catalog.date_price_kind", "override,holiday")
                .OldAnnotation("Npgsql:Enum:catalog.deposit_mode", "none,total,per_person")
                .OldAnnotation("Npgsql:Enum:catalog.deposit_type", "percentage,amount")
                .OldAnnotation("Npgsql:Enum:catalog.discount_applicable_channel", "manager_and_online,manager_only,online_only")
                .OldAnnotation("Npgsql:Enum:catalog.discount_scope", "all,custom,without_custom")
                .OldAnnotation("Npgsql:Enum:catalog.discount_service_category", "bar,normal,hourly,additional")
                .OldAnnotation("Npgsql:Enum:catalog.discount_type", "percentage,fixed")
                .OldAnnotation("Npgsql:Enum:catalog.inventory_article", "umbrella,sunbed,sunbed_xl,sunbed_maxi,deckchair,directors_chair,tent,gazebo,cabin,custom_cabin,parking,ticket,sunbed_area,umbrella_area,parking_area,cabin_area,custom_article1,custom_article2,custom_article3,custom_article4,custom_article5,custom_article6,custom_article7,custom_article8")
                .OldAnnotation("Npgsql:Enum:catalog.map_icon_category", "template,umbrella,tent,sunbed,cabin")
                .OldAnnotation("Npgsql:Enum:catalog.map_icon_state", "occupied,free")
                .OldAnnotation("Npgsql:Enum:catalog.media_kind", "photo,pdf,icon")
                .OldAnnotation("Npgsql:Enum:catalog.media_origin", "s3us_east2,s3us_east1,cloudfront,external")
                .OldAnnotation("Npgsql:Enum:catalog.monthly_setting", "effective_month_duration,actual_month_duration,fixed30days")
                .OldAnnotation("Npgsql:Enum:catalog.online_booking_scope", "all,contacts_only")
                .OldAnnotation("Npgsql:Enum:catalog.online_payment_method", "pay_now,pay_at_property,pay_at_property_no_card,deposit_half,pay_at_property_prepaid")
                .OldAnnotation("Npgsql:Enum:catalog.package_item_kind", "inventory_item,area_item,included_service,linked_package")
                .OldAnnotation("Npgsql:Enum:catalog.pets_policy", "allowed,not_allowed,on_request")
                .OldAnnotation("Npgsql:Enum:catalog.price_list_kind", "package,additional")
                .OldAnnotation("Npgsql:Enum:catalog.property_status", "pending,approved,rejected,imported")
                .OldAnnotation("Npgsql:Enum:catalog.revenue_season_tier", "low,high")
                .OldAnnotation("Npgsql:Enum:catalog.sales_channel", "manager_only,manager_and_online,online_only")
                .OldAnnotation("Npgsql:Enum:catalog.service_sale_kind", "bar,normal,hourly")
                .OldAnnotation("Npgsql:Enum:catalog.spot_type", "umbrella,sunbed,sunbed_xl,cabin,custom_cabin,custom_article1,custom_article2,custom_article3,custom_article4,custom_article5,tent,parking,ticket,umbrella_area,cabin_area,sunbed_area,parking_area,custom_article6,custom_article7,custom_article8,beach_element")
                .OldAnnotation("Npgsql:Enum:catalog.suggested_price_mode", "none,suggested,original,discount")
                .OldAnnotation("Npgsql:Enum:catalog.surcharge_type", "disable,percentage,fixed")
                .OldAnnotation("Npgsql:Enum:catalog.table_capacity_mode", "tables,seats")
                .OldAnnotation("Npgsql:Enum:catalog.tariff_tier", "low,mid,high,peak,custom")
                .OldAnnotation("Npgsql:Enum:catalog.vat_nature", "n1,n2,n3,n4,n5,n6")
                .OldAnnotation("Npgsql:Enum:catalog.weekday", "monday,tuesday,wednesday,thursday,friday,saturday,sunday")
                .OldAnnotation("Npgsql:Enum:catalog.weekend_setting", "saturday_sunday,sunday_only")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_suggested_prices_package_date_period",
                schema: "catalog",
                table: "pricing_suggested_prices",
                columns: new[] { "price_list_id", "price_date", "period_label" },
                unique: true);
        }
    }
}
