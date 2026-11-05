using Microsoft.EntityFrameworkCore.Migrations;
using Module.Catalog.Domain.Enums;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InventoryArticlesAndLowStockAlert : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                .Annotation("Npgsql:Enum:catalog.spot_type", "umbrella,sunbed,sunbed_xl,cabin,special_cabin,special_umbrella1,special_umbrella2,special_umbrella3,special_umbrella4,special_umbrella5,tent,parking,ticket,umbrella_area,cabin_area,sunbed_area,parking_area")
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
                .OldAnnotation("Npgsql:Enum:catalog.spot_type", "umbrella,sunbed,sunbed_xl,cabin,special_cabin,special_umbrella1,special_umbrella2,special_umbrella3,special_umbrella4,special_umbrella5,tent,parking,ticket,umbrella_area,cabin_area,sunbed_area,parking_area")
                .OldAnnotation("Npgsql:Enum:catalog.suggested_price_mode", "none,suggested,original,discount")
                .OldAnnotation("Npgsql:Enum:catalog.surcharge_type", "disable,percentage,fixed")
                .OldAnnotation("Npgsql:Enum:catalog.tariff_tier", "low,mid,high,peak,custom")
                .OldAnnotation("Npgsql:Enum:catalog.weekday", "monday,tuesday,wednesday,thursday,friday,saturday,sunday")
                .OldAnnotation("Npgsql:Enum:catalog.weekend_setting", "saturday_sunday,sunday_only")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AddColumn<InventoryArticle>(
                name: "article_code",
                schema: "catalog",
                table: "inventory_items",
                type: "catalog.inventory_article",
                nullable: true,
                comment: "Articolo dell'elenco fisso; NULL per gli articoli inseriti con «Altro».");

            migrationBuilder.AddColumn<bool>(
                name: "low_stock_alert_enabled",
                schema: "catalog",
                table: "inventory_items",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Avviso nella campanella e nella sezione News quando la disponibilità del giorno scende sotto la soglia.");

            // L'interruttore «Abilita» del legacy (is_active) era l'avviso soglia.
            migrationBuilder.Sql("UPDATE catalog.inventory_items SET low_stock_alert_enabled = is_active;");

            // Articoli dell'elenco fisso riconosciuti dal nome (italiano o inglese del legacy).
            migrationBuilder.Sql(@"
UPDATE catalog.inventory_items SET article_code = (CASE lower(trim(name))
    WHEN 'ombrellone' THEN 'umbrella' WHEN 'umbrella' THEN 'umbrella'
    WHEN 'lettino' THEN 'sunbed' WHEN 'sunbed' THEN 'sunbed'
    WHEN 'lettino xl' THEN 'sunbed_xl' WHEN 'xlsunbed' THEN 'sunbed_xl' WHEN 'xl sunbed' THEN 'sunbed_xl'
    WHEN 'lettino maxi' THEN 'sunbed_maxi'
    WHEN 'sdraio' THEN 'deckchair' WHEN 'deckchair' THEN 'deckchair'
    WHEN 'sedia regista' THEN 'directors_chair' WHEN 'directors chair' THEN 'directors_chair'
    WHEN 'tenda' THEN 'tent' WHEN 'tent' THEN 'tent'
    WHEN 'gazebo' THEN 'gazebo'
    WHEN 'cabina' THEN 'cabin' WHEN 'cabin' THEN 'cabin'
    WHEN 'cabina speciale' THEN 'custom_cabin' WHEN 'specialcabin' THEN 'custom_cabin' WHEN 'special cabin' THEN 'custom_cabin'
    WHEN 'parcheggio' THEN 'parking' WHEN 'parking' THEN 'parking'
    WHEN 'ticket' THEN 'ticket'
    WHEN 'area lettini' THEN 'sunbed_area' WHEN 'sundbedarea' THEN 'sunbed_area'
    WHEN 'area ombrelloni' THEN 'umbrella_area' WHEN 'umbrellaarea' THEN 'umbrella_area'
    WHEN 'area parcheggi' THEN 'parking_area' WHEN 'parkingarea' THEN 'parking_area'
    WHEN 'area cabine' THEN 'cabin_area' WHEN 'cabinarea' THEN 'cabin_area'
    ELSE CASE WHEN lower(trim(name)) ~ '^(ombrellone speciale|specialumbrella) ?[1-5]$'
        THEN 'custom_article' || right(trim(name), 1) END
END)::catalog.inventory_article;");

            migrationBuilder.DropIndex(
                name: "ix_catalog_inventory_items_property_active",
                schema: "catalog",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "is_active",
                schema: "catalog",
                table: "inventory_items");

            // Univocità del nome senza distinguere maiuscole e minuscole.
            migrationBuilder.Sql(@"
DROP INDEX catalog.uq_catalog_inventory_items_property_name;
CREATE UNIQUE INDEX uq_catalog_inventory_items_property_name
    ON catalog.inventory_items (property_id, lower(name)) WHERE deleted_at IS NULL;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DROP INDEX catalog.uq_catalog_inventory_items_property_name;
CREATE UNIQUE INDEX uq_catalog_inventory_items_property_name
    ON catalog.inventory_items (property_id, name) WHERE deleted_at IS NULL;");

            migrationBuilder.AddColumn<bool>(
                name: "is_active",
                schema: "catalog",
                table: "inventory_items",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Articolo attivo/visibile.");

            migrationBuilder.Sql("UPDATE catalog.inventory_items SET is_active = low_stock_alert_enabled;");

            migrationBuilder.DropColumn(
                name: "article_code",
                schema: "catalog",
                table: "inventory_items");

            migrationBuilder.DropColumn(
                name: "low_stock_alert_enabled",
                schema: "catalog",
                table: "inventory_items");

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
                .Annotation("Npgsql:Enum:catalog.spot_type", "umbrella,sunbed,sunbed_xl,cabin,special_cabin,special_umbrella1,special_umbrella2,special_umbrella3,special_umbrella4,special_umbrella5,tent,parking,ticket,umbrella_area,cabin_area,sunbed_area,parking_area")
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
                .OldAnnotation("Npgsql:Enum:catalog.spot_type", "umbrella,sunbed,sunbed_xl,cabin,special_cabin,special_umbrella1,special_umbrella2,special_umbrella3,special_umbrella4,special_umbrella5,tent,parking,ticket,umbrella_area,cabin_area,sunbed_area,parking_area")
                .OldAnnotation("Npgsql:Enum:catalog.suggested_price_mode", "none,suggested,original,discount")
                .OldAnnotation("Npgsql:Enum:catalog.surcharge_type", "disable,percentage,fixed")
                .OldAnnotation("Npgsql:Enum:catalog.tariff_tier", "low,mid,high,peak,custom")
                .OldAnnotation("Npgsql:Enum:catalog.weekday", "monday,tuesday,wednesday,thursday,friday,saturday,sunday")
                .OldAnnotation("Npgsql:Enum:catalog.weekend_setting", "saturday_sunday,sunday_only")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_inventory_items_property_active",
                schema: "catalog",
                table: "inventory_items",
                column: "property_id",
                filter: "is_active AND deleted_at IS NULL");
        }
    }
}
