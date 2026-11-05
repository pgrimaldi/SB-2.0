using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CatalogLookups : Migration
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

            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "service_type_icons",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "Id pubblico (UUIDv7) usato dalle API.");

            migrationBuilder.AddColumn<string>(
                name: "code",
                schema: "catalog",
                table: "service_amenities",
                type: "character varying(60)",
                maxLength: 60,
                nullable: false,
                defaultValue: "",
                comment: "Codice stabile della dotazione usato dalle API (es. wifi, pets_allowed).");

            migrationBuilder.CreateTable(
                name: "country_vat_rates",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    country_code = table.Column<string>(type: "char(2)", nullable: false, comment: "Codice ISO del Paese (catalog.countries.code), senza FK: le aliquote arrivano con le migration, i paesi con l'ETL."),
                    rate = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false, comment: "Aliquota in percentuale (22.00 = 22%)."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordine nella tendina: la prima è l'aliquota proposta.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_country_vat_rates", x => x.id);
                    table.CheckConstraint("ck_catalog_country_vat_rates_rate", "rate > 0 AND rate < 100");
                },
                comment: "Aliquote IVA selezionabili per Paese dello stabilimento (pacchetti, articoli aggiuntivi, servizi); lookup di piattaforma caricata con le migration.");

            // UUIDv7 dal momento di creazione (PostgreSQL 17 non ha uuidv7()); codice provvisorio, quello vero arriva dall'ETL.
            migrationBuilder.Sql("""
                UPDATE catalog.service_type_icons SET public_id = encode(set_bit(set_bit(overlay(uuid_send(gen_random_uuid())
                    placing substring(int8send((extract(epoch from created_at) * 1000)::bigint) from 3) from 1 for 6),
                    52, 1), 53, 1), 'hex')::uuid;
                UPDATE catalog.service_amenities SET code = 'amenity_' || id;
                """);

            // Aliquote ordinarie e ridotte per Paese; per l'Italia senza il 5%.
            migrationBuilder.Sql("""
                INSERT INTO catalog.country_vat_rates (country_code, rate, sort_order) VALUES
                ('IT', 22, 0),
                ('IT', 10, 1),
                ('IT', 4, 2),
                ('ME', 21, 0),
                ('ME', 7, 1),
                ('AL', 20, 0),
                ('AL', 6, 1),
                ('ES', 21, 0),
                ('ES', 10, 1),
                ('ES', 4, 2),
                ('PT', 23, 0),
                ('PT', 13, 1),
                ('PT', 6, 2),
                ('HR', 25, 0),
                ('HR', 13, 1),
                ('HR', 5, 2),
                ('GR', 24, 0),
                ('GR', 13, 1),
                ('GR', 6, 2),
                ('BA', 17, 0),
                ('TR', 20, 0),
                ('TR', 10, 1),
                ('TR', 1, 2),
                ('FR', 20, 0),
                ('FR', 10, 1),
                ('FR', 5.5, 2),
                ('SI', 22, 0),
                ('SI', 9.5, 1),
                ('SI', 5, 2);
                """);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_service_type_icons_public_id",
                schema: "catalog",
                table: "service_type_icons",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_service_amenities_code",
                schema: "catalog",
                table: "service_amenities",
                column: "code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_country_vat_rates_country_rate",
                schema: "catalog",
                table: "country_vat_rates",
                columns: new[] { "country_code", "rate" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "country_vat_rates",
                schema: "catalog");

            migrationBuilder.DropIndex(
                name: "uq_catalog_service_type_icons_public_id",
                schema: "catalog",
                table: "service_type_icons");

            migrationBuilder.DropIndex(
                name: "uq_catalog_service_amenities_code",
                schema: "catalog",
                table: "service_amenities");

            migrationBuilder.DropColumn(
                name: "public_id",
                schema: "catalog",
                table: "service_type_icons");

            migrationBuilder.DropColumn(
                name: "code",
                schema: "catalog",
                table: "service_amenities");

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
                .OldAnnotation("Npgsql:Enum:catalog.vat_nature", "n1,n2,n3,n4,n5,n6")
                .OldAnnotation("Npgsql:Enum:catalog.weekday", "monday,tuesday,wednesday,thursday,friday,saturday,sunday")
                .OldAnnotation("Npgsql:Enum:catalog.weekend_setting", "saturday_sunday,sunday_only")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");
        }
    }
}
