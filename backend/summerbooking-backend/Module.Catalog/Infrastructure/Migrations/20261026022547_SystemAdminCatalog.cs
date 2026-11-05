using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Module.Catalog.Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Shared.Localization;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SystemAdminCatalog : Migration
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
                .Annotation("Npgsql:Enum:catalog.property_status", "pending,approved,rejected,imported,hidden")
                .Annotation("Npgsql:Enum:catalog.revenue_season_tier", "low,high")
                .Annotation("Npgsql:Enum:catalog.sales_channel", "manager_only,manager_and_online,online_only")
                .Annotation("Npgsql:Enum:catalog.service_sale_kind", "bar,normal,hourly")
                .Annotation("Npgsql:Enum:catalog.showcase_section", "featured,popular")
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

            migrationBuilder.AddColumn<bool>(
                name: "is_subscription_active",
                schema: "catalog",
                table: "properties",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Abbonamento al gestionale attivo (evento di Identity): senza, niente vendite online.");

            migrationBuilder.AddColumn<DateTime>(
                name: "purged_at",
                schema: "catalog",
                table: "properties",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Eliminazione definitiva dal cestino (deleted_at = spostamento nel cestino): da qui partono i tempi di conservazione.");

            migrationBuilder.AddColumn<bool>(
                name: "is_region_manual",
                schema: "catalog",
                table: "bathhouses",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Regione scelta a mano dal systemadmin: non si ricalcola dall'indirizzo.");

            migrationBuilder.AddColumn<int>(
                name: "like_count",
                schema: "catalog",
                table: "bathhouses",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "«Mi piace» dei clienti, aggiornati dagli eventi di Identity.");

            migrationBuilder.AddColumn<long>(
                name: "region_id",
                schema: "catalog",
                table: "bathhouses",
                type: "bigint",
                nullable: true,
                comment: "Regione della vetrina: automatica dalla colonna region, o scelta a mano.");

            migrationBuilder.CreateTable(
                name: "showcase_badges",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico (UUIDv7) usato dalle API."),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, comment: "Nome in italiano."),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true, comment: "Colore esadecimale #RRGGBB."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordine di visualizzazione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_showcase_badges", x => x.id);
                    table.CheckConstraint("ck_catalog_showcase_badges_color", "color IS NULL OR color ~ '^#[0-9A-F]{6}$'");
                },
                comment: "Badge della vetrina creati dal systemadmin.");

            migrationBuilder.CreateTable(
                name: "showcase_entries",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    section = table.Column<ShowcaseSection>(type: "catalog.showcase_section", nullable: false, comment: "Sezione della vetrina."),
                    bathhouse_id = table.Column<long>(type: "bigint", nullable: false, comment: "Struttura."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, comment: "Posizione nella sezione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_showcase_entries", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_showcase_entries_bathhouse",
                        column: x => x.bathhouse_id,
                        principalSchema: "catalog",
                        principalTable: "bathhouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Strutture nelle sezioni della vetrina («In evidenza», «Più frequentati»), scelte e ordinate dal systemadmin.");

            migrationBuilder.CreateTable(
                name: "showcase_regions",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico (UUIDv7) usato dalle API."),
                    country_id = table.Column<short>(type: "smallint", nullable: true, comment: "Paese della regione."),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Nome in italiano."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordine di visualizzazione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_showcase_regions", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_showcase_regions_country",
                        column: x => x.country_id,
                        principalSchema: "catalog",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Regioni della vetrina gestite dal systemadmin; assegnate alle strutture dalla regione dell'indirizzo, modificabili a mano.");

            migrationBuilder.CreateTable(
                name: "bathhouse_badges",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    bathhouse_id = table.Column<long>(type: "bigint", nullable: false, comment: "Struttura."),
                    badge_id = table.Column<long>(type: "bigint", nullable: false, comment: "Badge."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_bathhouse_badges", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_bathhouse_badges_badge",
                        column: x => x.badge_id,
                        principalSchema: "catalog",
                        principalTable: "showcase_badges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_bathhouse_badges_bathhouse",
                        column: x => x.bathhouse_id,
                        principalSchema: "catalog",
                        principalTable: "bathhouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Badge assegnati alle strutture dal systemadmin.");

            migrationBuilder.CreateTable(
                name: "showcase_badge_translations",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    badge_id = table.Column<long>(type: "bigint", nullable: false, comment: "Badge."),
                    language = table.Column<AppLanguage>(type: "catalog.content_language", nullable: false, comment: "Lingua della traduzione."),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, comment: "Nome tradotto."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_showcase_badge_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_showcase_badge_translations_parent",
                        column: x => x.badge_id,
                        principalSchema: "catalog",
                        principalTable: "showcase_badges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Nome dei badge della vetrina nelle altre lingue.");

            migrationBuilder.CreateTable(
                name: "showcase_region_translations",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    region_id = table.Column<long>(type: "bigint", nullable: false, comment: "Regione."),
                    language = table.Column<AppLanguage>(type: "catalog.content_language", nullable: false, comment: "Lingua della traduzione."),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Nome tradotto."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_showcase_region_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_showcase_region_translations_parent",
                        column: x => x.region_id,
                        principalSchema: "catalog",
                        principalTable: "showcase_regions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Nome delle regioni della vetrina nelle altre lingue.");

            migrationBuilder.CreateIndex(
                name: "ix_bathhouses_region_id",
                schema: "catalog",
                table: "bathhouses",
                column: "region_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_bathhouse_badges_badge",
                schema: "catalog",
                table: "bathhouse_badges",
                column: "badge_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_bathhouse_badges_badge",
                schema: "catalog",
                table: "bathhouse_badges",
                columns: new[] { "bathhouse_id", "badge_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_showcase_badge_translations_language",
                schema: "catalog",
                table: "showcase_badge_translations",
                columns: new[] { "badge_id", "language" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_showcase_badges_public_id",
                schema: "catalog",
                table: "showcase_badges",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_showcase_entries_bathhouse_id",
                schema: "catalog",
                table: "showcase_entries",
                column: "bathhouse_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_showcase_entries_bathhouse",
                schema: "catalog",
                table: "showcase_entries",
                columns: new[] { "section", "bathhouse_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_showcase_region_translations_language",
                schema: "catalog",
                table: "showcase_region_translations",
                columns: new[] { "region_id", "language" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_showcase_regions_name",
                schema: "catalog",
                table: "showcase_regions",
                columns: new[] { "country_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL")
                .Annotation("Npgsql:NullsDistinct", false);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_showcase_regions_public_id",
                schema: "catalog",
                table: "showcase_regions",
                column: "public_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_bathhouses_showcase_region",
                schema: "catalog",
                table: "bathhouses",
                column: "region_id",
                principalSchema: "catalog",
                principalTable: "showcase_regions",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // Le 20 regioni italiane (nome inglese come traduzione); la regione delle strutture si assegna dalla colonna region,
            // confrontando solo le lettere (così «Emilia Romagna» trova «Emilia-Romagna»).
            migrationBuilder.Sql("""
                WITH seed(name, name_en, sort_order) AS (VALUES
                    ('Abruzzo', 'Abruzzo', 0),
                    ('Basilicata', 'Basilicata', 1),
                    ('Calabria', 'Calabria', 2),
                    ('Campania', 'Campania', 3),
                    ('Emilia-Romagna', 'Emilia-Romagna', 4),
                    ('Friuli-Venezia Giulia', 'Friuli-Venezia Giulia', 5),
                    ('Lazio', 'Lazio', 6),
                    ('Liguria', 'Liguria', 7),
                    ('Lombardia', 'Lombardy', 8),
                    ('Marche', 'Marche', 9),
                    ('Molise', 'Molise', 10),
                    ('Piemonte', 'Piedmont', 11),
                    ('Puglia', 'Apulia', 12),
                    ('Sardegna', 'Sardinia', 13),
                    ('Sicilia', 'Sicily', 14),
                    ('Toscana', 'Tuscany', 15),
                    ('Trentino-Alto Adige', 'Trentino-South Tyrol', 16),
                    ('Umbria', 'Umbria', 17),
                    ('Valle d''Aosta', 'Aosta Valley', 18),
                    ('Veneto', 'Veneto', 19)),
                inserted AS (
                    INSERT INTO catalog.showcase_regions (public_id, country_id, name, sort_order)
                    SELECT encode(set_bit(set_bit(overlay(uuid_send(gen_random_uuid()) placing substring(int8send((extract(epoch from clock_timestamp())*1000)::bigint) from 3) from 1 for 6),52,1),53,1),'hex')::uuid, (SELECT id FROM catalog.countries WHERE code = 'IT'), seed.name, seed.sort_order
                    FROM seed ORDER BY seed.sort_order
                    RETURNING id, name)
                INSERT INTO catalog.showcase_region_translations (region_id, language, name)
                SELECT inserted.id, 'en', seed.name_en FROM inserted JOIN seed ON seed.name = inserted.name;

                UPDATE catalog.bathhouses b SET region_id = r.id
                FROM catalog.showcase_regions r
                WHERE regexp_replace(lower(b.region), '[^a-z]', '', 'g') = regexp_replace(lower(r.name), '[^a-z]', '', 'g');
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_catalog_bathhouses_showcase_region",
                schema: "catalog",
                table: "bathhouses");

            migrationBuilder.DropTable(
                name: "bathhouse_badges",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "showcase_badge_translations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "showcase_entries",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "showcase_region_translations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "showcase_badges",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "showcase_regions",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "ix_bathhouses_region_id",
                schema: "catalog",
                table: "bathhouses");

            migrationBuilder.DropColumn(
                name: "is_subscription_active",
                schema: "catalog",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "purged_at",
                schema: "catalog",
                table: "properties");

            migrationBuilder.DropColumn(
                name: "is_region_manual",
                schema: "catalog",
                table: "bathhouses");

            migrationBuilder.DropColumn(
                name: "like_count",
                schema: "catalog",
                table: "bathhouses");

            migrationBuilder.DropColumn(
                name: "region_id",
                schema: "catalog",
                table: "bathhouses");

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
                .OldAnnotation("Npgsql:Enum:catalog.property_status", "pending,approved,rejected,imported,hidden")
                .OldAnnotation("Npgsql:Enum:catalog.revenue_season_tier", "low,high")
                .OldAnnotation("Npgsql:Enum:catalog.sales_channel", "manager_only,manager_and_online,online_only")
                .OldAnnotation("Npgsql:Enum:catalog.service_sale_kind", "bar,normal,hourly")
                .OldAnnotation("Npgsql:Enum:catalog.showcase_section", "featured,popular")
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
