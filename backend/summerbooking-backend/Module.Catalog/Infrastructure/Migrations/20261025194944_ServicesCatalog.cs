using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Module.Catalog.Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Shared.Localization;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ServicesCatalog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                .OldAnnotation("Npgsql:Enum:catalog.config_snapshot_kind", "property_management_config,beach_map,public_showcase,price_catalog,services_catalog,agreement_catalog")
                .OldAnnotation("Npgsql:Enum:catalog.date_price_kind", "override,holiday")
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
                .OldAnnotation("Npgsql:Enum:catalog.tariff_tier", "low,mid,high,peak,custom")
                .OldAnnotation("Npgsql:Enum:catalog.vat_nature", "n1,n2,n3,n4,n5,n6")
                .OldAnnotation("Npgsql:Enum:catalog.weekday", "monday,tuesday,wednesday,thursday,friday,saturday,sunday")
                .OldAnnotation("Npgsql:Enum:catalog.weekend_setting", "saturday_sunday,sunday_only")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AddColumn<Allergen[]>(
                name: "allergens",
                schema: "catalog",
                table: "service_sale_items",
                type: "catalog.allergen[]",
                nullable: false,
                defaultValueSql: "'{}'",
                comment: "Allergeni dichiarati sui prodotti del bar (i 14 del Reg. UE 1169/2011).");

            migrationBuilder.AddColumn<BarCategory>(
                name: "category",
                schema: "catalog",
                table: "service_sale_items",
                type: "catalog.bar_category",
                nullable: true,
                comment: "Categoria dei servizi bar (bar, pizzeria, bibite, ...); NULL per gli altri tipi.");

            migrationBuilder.AddColumn<string>(
                name: "description",
                schema: "catalog",
                table: "service_sale_items",
                type: "text",
                nullable: true,
                comment: "Descrizione mostrata al cliente.");

            migrationBuilder.AddColumn<bool>(
                name: "has_fixed_time_slots",
                schema: "catalog",
                table: "service_sale_items",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Servizi orari: fasce orarie fisse (inizio e fine in service_sale_item_time_slots).");

            migrationBuilder.AddColumn<bool>(
                name: "has_period_prices",
                schema: "catalog",
                table: "service_sale_items",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Variazioni di prezzo per periodo attive: valgono i prezzi di service_sale_item_period_prices.");

            migrationBuilder.AddColumn<bool>(
                name: "is_highlighted",
                schema: "catalog",
                table: "service_sale_items",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Metti in evidenza: proposto al cliente quando prenota la spiaggia.");

            migrationBuilder.AddColumn<bool>(
                name: "is_sector_restricted",
                schema: "catalog",
                table: "service_sale_items",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Acquistabile solo nelle file/settori di service_sale_item_sectors.");

            migrationBuilder.AddColumn<VatNature>(
                name: "vat_nature",
                schema: "catalog",
                table: "service_sale_items",
                type: "catalog.vat_nature",
                nullable: true,
                comment: "Natura dell'esenzione IVA (n1..n6), solo senza aliquota.");

            migrationBuilder.AddColumn<decimal>(
                name: "vat_rate",
                schema: "catalog",
                table: "service_sale_items",
                type: "numeric(5,2)",
                precision: 5,
                scale: 2,
                nullable: true,
                comment: "Aliquota IVA in percentuale; NULL = esente, con la natura in vat_nature.");

            migrationBuilder.CreateTable(
                name: "automatic_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico (UUIDv7) usato dalle API."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome mostrato al cliente."),
                    description = table.Column<string>(type: "text", nullable: true, comment: "Descrizione."),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, comment: "Prezzo aggiunto."),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: true, comment: "Aliquota IVA in percentuale; NULL = esente (vat_nature)."),
                    vat_nature = table.Column<VatNature>(type: "catalog.vat_nature", nullable: true, comment: "Natura dell'esenzione IVA."),
                    icon_id = table.Column<int>(type: "integer", nullable: true, comment: "Icona del catalogo (service_type_icons)."),
                    icon_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Immagine caricata dal gestore."),
                    is_purchasable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Vale anche per gli ordini online."),
                    applies_per_service = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "true = per ogni servizio dell'ordine; false = una volta per ordine."),
                    applies_to_all_services = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "true = tutti i servizi; false = solo quelli di automatic_price_services."),
                    is_sector_restricted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Vale solo nelle file/settori di automatic_price_sectors."),
                    weekdays = table.Column<Weekday[]>(type: "catalog.weekday[]", nullable: false, defaultValueSql: "'{}'", comment: "Giorni di validità; vuoto = tutti."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_automatic_prices", x => x.id);
                    table.CheckConstraint("ck_catalog_automatic_prices_price", "price >= 0");
                    table.CheckConstraint("ck_catalog_automatic_prices_vat", "(vat_rate IS NULL) <> (vat_nature IS NULL)");
                    table.ForeignKey(
                        name: "fk_catalog_automatic_prices_icon",
                        column: x => x.icon_id,
                        principalSchema: "catalog",
                        principalTable: "service_type_icons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_catalog_automatic_prices_icon_asset",
                        column: x => x.icon_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_automatic_prices_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Prezzi automatici (es. Coperto): aggiunti da soli agli ordini dei servizi indicati nei periodi, giorni e fasce di validità.");

            migrationBuilder.CreateTable(
                name: "service_booking_options",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    sale_item_id = table.Column<long>(type: "bigint", nullable: false, comment: "Servizio."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: true, comment: "Inizio del periodo in cui vale l'opzione; NULL = tutta la stagione."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fine del periodo."),
                    weekdays = table.Column<Weekday[]>(type: "catalog.weekday[]", nullable: false, defaultValueSql: "'{}'", comment: "Giorni in cui vale l'opzione; vuoto = tutti."),
                    management_from = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Orari prenotabili dal gestionale: inizio."),
                    management_to = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Orari prenotabili dal gestionale: fine."),
                    online_from = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Orari prenotabili online: inizio."),
                    online_to = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Orari prenotabili online: fine."),
                    max_per_hour = table.Column<short>(type: "smallint", nullable: true, comment: "Massimo di prenotazioni all'ora."),
                    max_per_day = table.Column<short>(type: "smallint", nullable: true, comment: "Massimo di prenotazioni al giorno."),
                    max_per_slot = table.Column<short>(type: "smallint", nullable: true, comment: "Servizi orari: massimo di prenotazioni per fascia."),
                    interval_minutes = table.Column<short>(type: "smallint", nullable: true, comment: "Tempo minimo fra due ordini dello stesso cliente."),
                    min_items = table.Column<short>(type: "smallint", nullable: true, comment: "Minimo di articoli per prenotazione."),
                    max_items = table.Column<short>(type: "smallint", nullable: true, comment: "Massimo di articoli per prenotazione."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordine delle opzioni."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_booking_options", x => x.id);
                    table.CheckConstraint("ck_catalog_service_booking_options_items", "min_items IS NULL OR max_items IS NULL OR max_items >= min_items");
                    table.CheckConstraint("ck_catalog_service_booking_options_management", "management_from IS NULL OR management_to IS NULL OR management_to > management_from");
                    table.CheckConstraint("ck_catalog_service_booking_options_online", "online_from IS NULL OR online_to IS NULL OR online_to > online_from");
                    table.CheckConstraint("ck_catalog_service_booking_options_period", "starts_on IS NULL OR ends_on IS NULL OR ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_service_booking_options_item",
                        column: x => x.sale_item_id,
                        principalSchema: "catalog",
                        principalTable: "service_sale_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Opzioni di prenotazione di un servizio: periodo, giorni, orari da gestionale e online, capienza e limiti per prenotazione.");

            migrationBuilder.CreateTable(
                name: "service_sale_item_period_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    sale_item_id = table.Column<long>(type: "bigint", nullable: false, comment: "Servizio."),
                    tariff_period_group = table.Column<short>(type: "smallint", nullable: false, comment: "Chiave del periodo nella stagione (pricing_tariff_periods.period_group_key)."),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, comment: "Prezzo del servizio nel periodo."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_sale_item_period_prices", x => x.id);
                    table.CheckConstraint("ck_catalog_service_sale_item_period_prices_price", "price >= 0");
                    table.ForeignKey(
                        name: "fk_catalog_service_sale_item_period_prices_item",
                        column: x => x.sale_item_id,
                        principalSchema: "catalog",
                        principalTable: "service_sale_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Prezzo di un servizio per periodo tariffario della stagione (variazioni di prezzo per periodo).");

            migrationBuilder.CreateTable(
                name: "service_sale_item_sectors",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    sale_item_id = table.Column<long>(type: "bigint", nullable: false, comment: "Servizio."),
                    sector_id = table.Column<long>(type: "bigint", nullable: false, comment: "Fila/settore."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_sale_item_sectors", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_service_sale_item_sectors_item",
                        column: x => x.sale_item_id,
                        principalSchema: "catalog",
                        principalTable: "service_sale_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_service_sale_item_sectors_sector",
                        column: x => x.sector_id,
                        principalSchema: "catalog",
                        principalTable: "map_sectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "File/settori in cui un servizio è acquistabile, quando non vale per tutte.");

            migrationBuilder.CreateTable(
                name: "service_sale_item_translations",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    sale_item_id = table.Column<long>(type: "bigint", nullable: false, comment: "Riferimento a un servizio."),
                    language = table.Column<AppLanguage>(type: "catalog.content_language", nullable: false, comment: "Lingua della traduzione."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome tradotto."),
                    description = table.Column<string>(type: "text", nullable: true, comment: "Descrizione tradotta."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_sale_item_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_service_sale_item_translations_parent",
                        column: x => x.sale_item_id,
                        principalSchema: "catalog",
                        principalTable: "service_sale_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Nome e descrizione di un servizio nelle altre lingue.");

            migrationBuilder.CreateTable(
                name: "service_sale_item_variants",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    sale_item_id = table.Column<long>(type: "bigint", nullable: false, comment: "Servizio."),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Nome della variante."),
                    price = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false, comment: "Prezzo aggiunto al servizio."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordine di visualizzazione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_sale_item_variants", x => x.id);
                    table.CheckConstraint("ck_catalog_service_sale_item_variants_price", "price >= 0");
                    table.ForeignKey(
                        name: "fk_catalog_service_sale_item_variants_item",
                        column: x => x.sale_item_id,
                        principalSchema: "catalog",
                        principalTable: "service_sale_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Varianti di un servizio bar (es. Crema +0,50 €): il prezzo si somma a quello del servizio.");

            migrationBuilder.CreateTable(
                name: "table_services",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico (UUIDv7) usato dalle API."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome del servizio (es. Pranzo)."),
                    description = table.Column<string>(type: "text", nullable: true, comment: "Descrizione."),
                    is_online_bookable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Prenotabile online."),
                    is_highlighted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Proposto al cliente insieme alla prenotazione dell'ombrellone."),
                    capacity_mode = table.Column<TableCapacityMode>(type: "catalog.table_capacity_mode", nullable: false, comment: "Disponibilita per numero di tavoli o per posti totali."),
                    total_seats = table.Column<short>(type: "smallint", nullable: true, comment: "Posti totali, con capacity_mode = seats."),
                    average_meal_minutes = table.Column<short>(type: "smallint", nullable: false, comment: "Durata media del pasto in minuti: libera il tavolo per la fascia successiva."),
                    max_bookings_per_slot = table.Column<short>(type: "smallint", nullable: true, comment: "Massimo di prenotazioni per fascia oraria."),
                    min_seats_per_booking = table.Column<short>(type: "smallint", nullable: true, comment: "Minimo di posti prenotabili in una volta."),
                    max_seats_per_booking = table.Column<short>(type: "smallint", nullable: true, comment: "Massimo di posti prenotabili in una volta."),
                    deposit_mode = table.Column<DepositMode>(type: "catalog.deposit_mode", nullable: false, comment: "Caparra: nessuna, totale o a persona."),
                    deposit_type = table.Column<DepositType>(type: "catalog.deposit_type", nullable: true, comment: "Caparra in percentuale o importo."),
                    deposit_value = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: true, comment: "Percentuale o importo della caparra."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_table_services", x => x.id);
                    table.CheckConstraint("ck_catalog_table_services_booking_seats", "min_seats_per_booking IS NULL OR max_seats_per_booking IS NULL OR max_seats_per_booking >= min_seats_per_booking");
                    table.CheckConstraint("ck_catalog_table_services_deposit", "(deposit_mode = 'none') = (deposit_type IS NULL AND deposit_value IS NULL)");
                    table.CheckConstraint("ck_catalog_table_services_meal", "average_meal_minutes > 0");
                    table.CheckConstraint("ck_catalog_table_services_seats", "capacity_mode <> 'seats' OR total_seats > 0");
                    table.ForeignKey(
                        name: "fk_catalog_table_services_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Servizio tavoli (pranzo, cena): disponibilità, capienza per tavoli o posti, durata del pasto, limiti e caparra.");

            migrationBuilder.CreateTable(
                name: "automatic_price_periods",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    automatic_price_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prezzo automatico."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Primo giorno."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Ultimo giorno."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_automatic_price_periods", x => x.id);
                    table.CheckConstraint("ck_catalog_automatic_price_periods_range", "ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_automatic_price_periods_price",
                        column: x => x.automatic_price_id,
                        principalSchema: "catalog",
                        principalTable: "automatic_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Periodi di validità di un prezzo automatico.");

            migrationBuilder.CreateTable(
                name: "automatic_price_sectors",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    automatic_price_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prezzo automatico."),
                    sector_id = table.Column<long>(type: "bigint", nullable: false, comment: "Fila/settore."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_automatic_price_sectors", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_automatic_price_sectors_price",
                        column: x => x.automatic_price_id,
                        principalSchema: "catalog",
                        principalTable: "automatic_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_automatic_price_sectors_sector",
                        column: x => x.sector_id,
                        principalSchema: "catalog",
                        principalTable: "map_sectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "File/settori in cui vale un prezzo automatico.");

            migrationBuilder.CreateTable(
                name: "automatic_price_services",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    automatic_price_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prezzo automatico."),
                    sale_item_id = table.Column<long>(type: "bigint", nullable: false, comment: "Servizio."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_automatic_price_services", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_automatic_price_services_item",
                        column: x => x.sale_item_id,
                        principalSchema: "catalog",
                        principalTable: "service_sale_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_automatic_price_services_price",
                        column: x => x.automatic_price_id,
                        principalSchema: "catalog",
                        principalTable: "automatic_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Servizi a cui si applica un prezzo automatico, quando non vale per tutti.");

            migrationBuilder.CreateTable(
                name: "automatic_price_time_ranges",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    automatic_price_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prezzo automatico."),
                    time_from = table.Column<TimeOnly>(type: "time", nullable: false, comment: "Inizio della fascia."),
                    time_to = table.Column<TimeOnly>(type: "time", nullable: false, comment: "Fine della fascia."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_automatic_price_time_ranges", x => x.id);
                    table.CheckConstraint("ck_catalog_automatic_price_time_ranges_range", "time_to > time_from");
                    table.ForeignKey(
                        name: "fk_catalog_automatic_price_time_ranges_price",
                        column: x => x.automatic_price_id,
                        principalSchema: "catalog",
                        principalTable: "automatic_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Fasce orarie di validità di un prezzo automatico.");

            migrationBuilder.CreateTable(
                name: "automatic_price_translations",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    automatic_price_id = table.Column<long>(type: "bigint", nullable: false, comment: "Riferimento a un prezzo automatico."),
                    language = table.Column<AppLanguage>(type: "catalog.content_language", nullable: false, comment: "Lingua della traduzione."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome tradotto."),
                    description = table.Column<string>(type: "text", nullable: true, comment: "Descrizione tradotta."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_automatic_price_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_automatic_price_translations_parent",
                        column: x => x.automatic_price_id,
                        principalSchema: "catalog",
                        principalTable: "automatic_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Nome e descrizione di un prezzo automatico nelle altre lingue.");

            migrationBuilder.CreateTable(
                name: "table_service_availabilities",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    table_service_id = table.Column<long>(type: "bigint", nullable: false, comment: "Servizio tavoli."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Primo giorno."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Ultimo giorno."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_table_service_availabilities", x => x.id);
                    table.CheckConstraint("ck_catalog_table_service_availabilities_range", "ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_table_service_availabilities_service",
                        column: x => x.table_service_id,
                        principalSchema: "catalog",
                        principalTable: "table_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Periodi di disponibilità di un servizio tavoli.");

            migrationBuilder.CreateTable(
                name: "table_service_table_types",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    table_service_id = table.Column<long>(type: "bigint", nullable: false, comment: "Servizio tavoli."),
                    seats = table.Column<short>(type: "smallint", nullable: false, comment: "Posti per tavolo."),
                    count = table.Column<short>(type: "smallint", nullable: false, comment: "Numero di tavoli."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_table_service_table_types", x => x.id);
                    table.CheckConstraint("ck_catalog_table_service_table_types_values", "seats > 0 AND count > 0");
                    table.ForeignKey(
                        name: "fk_catalog_table_service_table_types_service",
                        column: x => x.table_service_id,
                        principalSchema: "catalog",
                        principalTable: "table_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tavoli di un servizio tavoli con lo stesso numero di posti (es. 5 tavoli da 4).");

            migrationBuilder.CreateTable(
                name: "table_service_translations",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    table_service_id = table.Column<long>(type: "bigint", nullable: false, comment: "Riferimento a un servizio tavoli."),
                    language = table.Column<AppLanguage>(type: "catalog.content_language", nullable: false, comment: "Lingua della traduzione."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome tradotto."),
                    description = table.Column<string>(type: "text", nullable: true, comment: "Descrizione tradotta."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_table_service_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_table_service_translations_parent",
                        column: x => x.table_service_id,
                        principalSchema: "catalog",
                        principalTable: "table_services",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Nome e descrizione di un servizio tavoli nelle altre lingue.");

            migrationBuilder.CreateTable(
                name: "table_service_slots",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    availability_id = table.Column<long>(type: "bigint", nullable: false, comment: "Periodo di disponibilità."),
                    weekday = table.Column<Weekday>(type: "catalog.weekday", nullable: true, comment: "Giorno; NULL = tutti i giorni del periodo."),
                    time_from = table.Column<TimeOnly>(type: "time", nullable: false, comment: "Inizio della fascia."),
                    time_to = table.Column<TimeOnly>(type: "time", nullable: false, comment: "Fine della fascia."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_table_service_slots", x => x.id);
                    table.CheckConstraint("ck_catalog_table_service_slots_range", "time_to > time_from");
                    table.ForeignKey(
                        name: "fk_catalog_table_service_slots_availability",
                        column: x => x.availability_id,
                        principalSchema: "catalog",
                        principalTable: "table_service_availabilities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Fasce orarie prenotabili di un periodo del servizio tavoli; senza giorno valgono per tutti i giorni.");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_service_sale_items_category",
                schema: "catalog",
                table: "service_sale_items",
                sql: "category IS NULL OR kind = 'bar'");

            // Il legacy non migrava l'IVA dei servizi: 22% fino al prossimo caricamento dell'ETL.
            migrationBuilder.Sql("UPDATE catalog.service_sale_items SET vat_rate = 22 WHERE vat_rate IS NULL AND vat_nature IS NULL;");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_service_sale_items_vat",
                schema: "catalog",
                table: "service_sale_items",
                sql: "(vat_rate IS NULL) <> (vat_nature IS NULL)");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_automatic_price_periods_price",
                schema: "catalog",
                table: "automatic_price_periods",
                column: "automatic_price_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_automatic_price_sectors_sector",
                schema: "catalog",
                table: "automatic_price_sectors",
                column: "sector_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_automatic_price_sectors_sector",
                schema: "catalog",
                table: "automatic_price_sectors",
                columns: new[] { "automatic_price_id", "sector_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_automatic_price_services_sale_item",
                schema: "catalog",
                table: "automatic_price_services",
                column: "sale_item_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_automatic_price_services_item",
                schema: "catalog",
                table: "automatic_price_services",
                columns: new[] { "automatic_price_id", "sale_item_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_automatic_price_time_ranges_price",
                schema: "catalog",
                table: "automatic_price_time_ranges",
                column: "automatic_price_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_automatic_price_translations_language",
                schema: "catalog",
                table: "automatic_price_translations",
                columns: new[] { "automatic_price_id", "language" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_automatic_prices_icon_asset_id",
                schema: "catalog",
                table: "automatic_prices",
                column: "icon_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_automatic_prices_icon_id",
                schema: "catalog",
                table: "automatic_prices",
                column: "icon_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_automatic_prices_property",
                schema: "catalog",
                table: "automatic_prices",
                column: "property_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_automatic_prices_public_id",
                schema: "catalog",
                table: "automatic_prices",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_booking_options_item",
                schema: "catalog",
                table: "service_booking_options",
                column: "sale_item_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_service_sale_item_period_prices_period",
                schema: "catalog",
                table: "service_sale_item_period_prices",
                columns: new[] { "sale_item_id", "tariff_period_group" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_sale_item_sectors_sector",
                schema: "catalog",
                table: "service_sale_item_sectors",
                column: "sector_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_service_sale_item_sectors_sector",
                schema: "catalog",
                table: "service_sale_item_sectors",
                columns: new[] { "sale_item_id", "sector_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_service_sale_item_translations_language",
                schema: "catalog",
                table: "service_sale_item_translations",
                columns: new[] { "sale_item_id", "language" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_sale_item_variants_item",
                schema: "catalog",
                table: "service_sale_item_variants",
                column: "sale_item_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_table_service_availabilities_service",
                schema: "catalog",
                table: "table_service_availabilities",
                column: "table_service_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_table_service_slots_availability",
                schema: "catalog",
                table: "table_service_slots",
                column: "availability_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_table_service_table_types_seats",
                schema: "catalog",
                table: "table_service_table_types",
                columns: new[] { "table_service_id", "seats" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_table_service_translations_language",
                schema: "catalog",
                table: "table_service_translations",
                columns: new[] { "table_service_id", "language" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_table_services_property",
                schema: "catalog",
                table: "table_services",
                column: "property_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_table_services_public_id",
                schema: "catalog",
                table: "table_services",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "automatic_price_periods",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "automatic_price_sectors",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "automatic_price_services",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "automatic_price_time_ranges",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "automatic_price_translations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_booking_options",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_sale_item_period_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_sale_item_sectors",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_sale_item_translations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_sale_item_variants",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "table_service_slots",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "table_service_table_types",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "table_service_translations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "automatic_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "table_service_availabilities",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "table_services",
                schema: "catalog");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_service_sale_items_category",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_service_sale_items_vat",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.DropColumn(
                name: "allergens",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.DropColumn(
                name: "category",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.DropColumn(
                name: "description",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.DropColumn(
                name: "has_fixed_time_slots",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.DropColumn(
                name: "has_period_prices",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.DropColumn(
                name: "is_highlighted",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.DropColumn(
                name: "is_sector_restricted",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.DropColumn(
                name: "vat_nature",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.DropColumn(
                name: "vat_rate",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.agreement_list_type", "regular,voucher")
                .Annotation("Npgsql:Enum:catalog.agreement_report_search_type", "check_in,check_out")
                .Annotation("Npgsql:Enum:catalog.agreement_report_status", "pending,created,cancelled")
                .Annotation("Npgsql:Enum:catalog.config_snapshot_kind", "property_management_config,beach_map,public_showcase,price_catalog,services_catalog,agreement_catalog")
                .Annotation("Npgsql:Enum:catalog.date_price_kind", "override,holiday")
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
        }
    }
}
