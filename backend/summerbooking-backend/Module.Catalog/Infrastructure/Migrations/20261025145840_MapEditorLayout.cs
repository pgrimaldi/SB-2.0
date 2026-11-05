using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class MapEditorLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'special_cabin' TO 'custom_cabin';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'special_umbrella1' TO 'custom_article1';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'special_umbrella2' TO 'custom_article2';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'special_umbrella3' TO 'custom_article3';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'special_umbrella4' TO 'custom_article4';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'special_umbrella5' TO 'custom_article5';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type ADD VALUE IF NOT EXISTS 'custom_article6';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type ADD VALUE IF NOT EXISTS 'custom_article7';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type ADD VALUE IF NOT EXISTS 'custom_article8';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type ADD VALUE IF NOT EXISTS 'beach_element';");

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
                .OldAnnotation("Npgsql:Enum:catalog.weekday", "monday,tuesday,wednesday,thursday,friday,saturday,sunday")
                .OldAnnotation("Npgsql:Enum:catalog.weekend_setting", "saturday_sunday,sunday_only")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterTable(
                name: "map_spots",
                schema: "catalog",
                comment: "Elementi del reticolo della mappa: postazioni prenotabili (ombrelloni, lettini, cabine, aree, ...) ed elementi spiaggia solo grafici. Sostituisce il blob JSON configurations.beachMapData.",
                oldComment: "Anagrafica delle postazioni (ombrelloni, lettini, cabine, parcheggi, ...) di uno stabilimento: una riga per postazione. Sostituisce il blob JSON configurations.beachMapData.");

            migrationBuilder.AlterColumn<string>(
                name: "label",
                schema: "catalog",
                table: "map_spots",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                comment: "Etichetta/numero della postazione visibile al cliente e all'operatore (es. A12); univoca per stabilimento. NULL solo per gli elementi spiaggia.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldComment: "Etichetta/numero della postazione visibile al cliente e all'operatore (es. A12); univoca per stabilimento.");

            migrationBuilder.AddColumn<long>(
                name: "beach_element_id",
                schema: "catalog",
                table: "map_spots",
                type: "bigint",
                nullable: true,
                comment: "Elemento spiaggia del catalogo di piattaforma; valorizzato solo per spot_type = beach_element.");

            migrationBuilder.AddColumn<short>(
                name: "column_span",
                schema: "catalog",
                table: "map_spots",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1,
                comment: "Colonne occupate dall'elemento (più di una solo per gli elementi spiaggia).");

            migrationBuilder.AddColumn<bool>(
                name: "is_agreement_only",
                schema: "catalog",
                table: "map_spots",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Riservata alle strutture convenzionate: non prenotabile online.");

            migrationBuilder.AddColumn<short>(
                name: "offset_x",
                schema: "catalog",
                table: "map_spots",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                comment: "Spostamento orizzontale dentro la casella, in quinti di casella (da -2 a 2; 0 = centro).");

            migrationBuilder.AddColumn<short>(
                name: "offset_y",
                schema: "catalog",
                table: "map_spots",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                comment: "Spostamento verticale dentro la casella, in quinti di casella (da -2 a 2; 0 = centro).");

            migrationBuilder.AddColumn<short>(
                name: "rotation_degrees",
                schema: "catalog",
                table: "map_spots",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                comment: "Rotazione dell'articolo sul proprio asse, a passi di 45 gradi.");

            migrationBuilder.AddColumn<short>(
                name: "row_span",
                schema: "catalog",
                table: "map_spots",
                type: "smallint",
                nullable: false,
                defaultValue: (short)1,
                comment: "Righe occupate dall'elemento (più di una solo per gli elementi spiaggia).");

            migrationBuilder.AddColumn<bool>(
                name: "reset_deletes_bookings",
                schema: "catalog",
                table: "map_layouts",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Impostazione del gestore: false = «Reimposta mappa» e l'eliminazione di righe o colonne mantengono le prenotazioni e le ricollegano per numero; true = le cancellano.");

            migrationBuilder.AddColumn<short>(
                name: "tilt_degrees",
                schema: "catalog",
                table: "map_layouts",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                comment: "Inclinazione del reticolo in gradi (da -45 a 45), applicata anche sul sito.");

            migrationBuilder.AddColumn<bool>(
                name: "is_default",
                schema: "catalog",
                table: "map_icons",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Icona predefinita della categoria e dello stato: sostituisce quella eliminata dal systemadmin nelle mappe che la usavano.");

            migrationBuilder.CreateTable(
                name: "map_beach_elements",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico (UUIDv7) usato dalle API."),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, comment: "Nome dell'elemento per il pannello di amministrazione; sulla mappa compare solo l'icona."),
                    media_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Icona dell'elemento; NULL finché il systemadmin non la carica."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordine nell'elenco mostrato al gestore."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Elemento selezionabile dal gestore."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_map_beach_elements", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_map_beach_elements_media_asset",
                        column: x => x.media_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catalogo di piattaforma degli elementi spiaggia solo grafici (passerella, doccia, bar, torretta, ...); il systemadmin ne aggiunge di nuovi.");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_map_spots_beach_element",
                schema: "catalog",
                table: "map_spots",
                column: "beach_element_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_map_spots_beach_element",
                schema: "catalog",
                table: "map_spots",
                sql: "(spot_type::text = 'beach_element') = (beach_element_id IS NOT NULL) AND (spot_type::text = 'beach_element' OR label IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_map_spots_offset",
                schema: "catalog",
                table: "map_spots",
                sql: "offset_x BETWEEN -2 AND 2 AND offset_y BETWEEN -2 AND 2");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_map_spots_rotation",
                schema: "catalog",
                table: "map_spots",
                sql: "rotation_degrees >= 0 AND rotation_degrees < 360 AND rotation_degrees % 45 = 0");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_map_spots_span",
                schema: "catalog",
                table: "map_spots",
                sql: "row_span >= 1 AND column_span >= 1");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_map_layouts_tilt",
                schema: "catalog",
                table: "map_layouts",
                sql: "tilt_degrees BETWEEN -45 AND 45");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_map_icons_default",
                schema: "catalog",
                table: "map_icons",
                columns: new[] { "category_id", "type" },
                unique: true,
                filter: "is_default AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_map_beach_elements_media_asset",
                schema: "catalog",
                table: "map_beach_elements",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_map_beach_elements_name",
                schema: "catalog",
                table: "map_beach_elements",
                column: "name",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_map_beach_elements_public_id",
                schema: "catalog",
                table: "map_beach_elements",
                column: "public_id",
                unique: true);

            migrationBuilder.Sql("""
                INSERT INTO catalog.map_beach_elements (public_id, name, sort_order) VALUES
                ('01a17392-d440-7929-9abe-c52721203dd2', 'Passerella 1', 1),
                ('01a17392-d441-7048-bf11-741faa331234', 'Passerella 2', 2),
                ('01a17392-d442-7b9b-a612-c54e649a0a8d', 'Passerella 3', 3),
                ('01a17392-d443-763c-a6b7-8008fa712a80', 'Prato', 4),
                ('01a17392-d444-7752-9853-8785b7e8b1da', 'Doccia', 5),
                ('01a17392-d445-7cc9-8374-cdd51bc8ca55', 'Guarda spiaggia', 6),
                ('01a17392-d446-7271-a099-4ccaf7a95501', 'Pianta', 7),
                ('01a17392-d447-71a1-984e-23bb0fb57f9e', 'Bar', 8),
                ('01a17392-d448-7820-bccc-02eab8772550', 'Ingresso', 9),
                ('01a17392-d449-7ab8-a310-ad4e53a644b5', 'Torretta', 10);
                """);

            migrationBuilder.Sql("""
                UPDATE catalog.map_icons i SET is_default = true
                WHERE i.id IN (SELECT DISTINCT ON (category_id, type) id FROM catalog.map_icons
                               WHERE is_active AND deleted_at IS NULL ORDER BY category_id, type, id);
                """);

            migrationBuilder.AddForeignKey(
                name: "fk_catalog_map_spots_beach_element",
                schema: "catalog",
                table: "map_spots",
                column: "beach_element_id",
                principalSchema: "catalog",
                principalTable: "map_beach_elements",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL non elimina le etichette di un enum: custom_article6..8 e beach_element restano.
            migrationBuilder.Sql("DELETE FROM catalog.map_spots WHERE spot_type::text = 'beach_element';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'custom_cabin' TO 'special_cabin';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'custom_article1' TO 'special_umbrella1';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'custom_article2' TO 'special_umbrella2';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'custom_article3' TO 'special_umbrella3';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'custom_article4' TO 'special_umbrella4';");
            migrationBuilder.Sql("ALTER TYPE catalog.spot_type RENAME VALUE 'custom_article5' TO 'special_umbrella5';");

            migrationBuilder.DropForeignKey(
                name: "fk_catalog_map_spots_beach_element",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropTable(
                name: "map_beach_elements",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "ix_catalog_map_spots_beach_element",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_map_spots_beach_element",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_map_spots_offset",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_map_spots_rotation",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_map_spots_span",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_map_layouts_tilt",
                schema: "catalog",
                table: "map_layouts");

            migrationBuilder.DropIndex(
                name: "uq_catalog_map_icons_default",
                schema: "catalog",
                table: "map_icons");

            migrationBuilder.DropColumn(
                name: "beach_element_id",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropColumn(
                name: "column_span",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropColumn(
                name: "is_agreement_only",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropColumn(
                name: "offset_x",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropColumn(
                name: "offset_y",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropColumn(
                name: "rotation_degrees",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropColumn(
                name: "row_span",
                schema: "catalog",
                table: "map_spots");

            migrationBuilder.DropColumn(
                name: "reset_deletes_bookings",
                schema: "catalog",
                table: "map_layouts");

            migrationBuilder.DropColumn(
                name: "tilt_degrees",
                schema: "catalog",
                table: "map_layouts");

            migrationBuilder.DropColumn(
                name: "is_default",
                schema: "catalog",
                table: "map_icons");

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
                .OldAnnotation("Npgsql:Enum:catalog.weekday", "monday,tuesday,wednesday,thursday,friday,saturday,sunday")
                .OldAnnotation("Npgsql:Enum:catalog.weekend_setting", "saturday_sunday,sunday_only")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterTable(
                name: "map_spots",
                schema: "catalog",
                comment: "Anagrafica delle postazioni (ombrelloni, lettini, cabine, parcheggi, ...) di uno stabilimento: una riga per postazione. Sostituisce il blob JSON configurations.beachMapData.",
                oldComment: "Elementi del reticolo della mappa: postazioni prenotabili (ombrelloni, lettini, cabine, aree, ...) ed elementi spiaggia solo grafici. Sostituisce il blob JSON configurations.beachMapData.");

            migrationBuilder.AlterColumn<string>(
                name: "label",
                schema: "catalog",
                table: "map_spots",
                type: "character varying(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "",
                comment: "Etichetta/numero della postazione visibile al cliente e all'operatore (es. A12); univoca per stabilimento.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "Etichetta/numero della postazione visibile al cliente e all'operatore (es. A12); univoca per stabilimento. NULL solo per gli elementi spiaggia.");
        }
    }
}
