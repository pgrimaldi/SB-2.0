using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Module.Booking.Domain.Enums;

#nullable disable

namespace Module.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SharingAlignedToFront : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<List<ResaleScope>>(
                name: "applies_to",
                schema: "booking",
                table: "resale_settings",
                type: "booking.resale_scope[]",
                nullable: false,
                defaultValueSql: "'{}'",
                comment: "A cosa si applica lo sharing: solo alle stagionali, a tutti gli articoli (non solo agli ombrelloni)",
                oldClrType: typeof(List<ResaleScope>),
                oldType: "booking.resale_scope[]",
                oldDefaultValueSql: "'{}'",
                oldComment: "Estensioni della rivendita: consentita per pass stagionali, estesa a tutte le postazioni del pacchetto");

            migrationBuilder.AddColumn<bool>(
                name: "absences_as_sharing",
                schema: "booking",
                table: "resale_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Le assenze programmate valgono come sharing: il giorno ripreso fa maturare il credito al titolare");

            migrationBuilder.AddColumn<decimal>(
                name: "card_top_up_amount",
                schema: "booking",
                table: "resale_settings",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Importo in euro della ricarica per giorno ripreso");

            migrationBuilder.AddColumn<bool>(
                name: "card_top_up_enabled",
                schema: "booking",
                table: "resale_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Ricarica della tessera prepagata del titolare per ogni giorno ripreso");

            migrationBuilder.AddColumn<decimal>(
                name: "manual_credit_amount",
                schema: "booking",
                table: "resale_settings",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Valore in euro di un credito manuale");

            migrationBuilder.AddColumn<bool>(
                name: "manual_credit_enabled",
                schema: "booking",
                table: "resale_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Crediti assegnati a mano: per ogni giorno ripreso il titolare matura manual_credit_units_per_day crediti");

            migrationBuilder.AddColumn<int>(
                name: "manual_credit_units_per_day",
                schema: "booking",
                table: "resale_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Crediti per ogni giorno ceduto e ripreso");

            migrationBuilder.AddColumn<int>(
                name: "min_booking_days",
                schema: "booking",
                table: "resale_settings",
                type: "integer",
                nullable: true,
                comment: "Giorni minimi della prenotazione per avere lo sharing; NULL = nessun minimo");

            migrationBuilder.AddColumn<decimal>(
                name: "voucher_credit_amount",
                schema: "booking",
                table: "resale_settings",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Valore in euro del voucher per giorno ripreso");

            migrationBuilder.AddColumn<bool>(
                name: "voucher_credit_enabled",
                schema: "booking",
                table: "resale_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Voucher automatico al titolare per ogni giorno ripreso");

            migrationBuilder.AddColumn<bool>(
                name: "excludes_from_price",
                schema: "booking",
                table: "resale_offers",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Assenza con «ricalcola prezzo»: il giorno non si paga");

            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                schema: "booking",
                table: "resale_offers",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "Id pubblico dell'offerta nelle API; le righe migrate lo ricevono dal default.");

            migrationBuilder.CreateIndex(
                name: "uq_booking_resale_offers_public_id",
                schema: "booking",
                table: "resale_offers",
                column: "public_id",
                unique: true);
        

            // Impostazioni di prima nelle colonne nuove: «Disabilita entrambe» spegne lo sharing, l'assenza come sharing viene da
            // mode, i crediti a importo fisso diventano crediti manuali; quelli in percentuale non hanno equivalente (0).
            migrationBuilder.Sql("""
                UPDATE booking.resale_settings SET
                    is_enabled = is_enabled AND NOT both_options_disabled,
                    absences_as_sharing = (mode = 'sharing'),
                    manual_credit_units_per_day = credit_units_per_day,
                    manual_credit_amount = CASE WHEN credit_calc = 'fixed_amount' THEN credit_amount ELSE 0 END,
                    manual_credit_enabled = credit_units_per_day > 0 AND credit_calc = 'fixed_amount' AND credit_amount > 0;
                """);

            migrationBuilder.DropColumn(
                name: "both_options_disabled",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "credit_amount",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "credit_calc",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "credit_units_per_day",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "mode",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.Sql("""
                ALTER TYPE booking.resale_scope RENAME VALUE 'seasonal_workers' TO 'seasonal_only';
                DROP TYPE booking.resale_mode;
                DROP TYPE booking.resale_credit_calc;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_booking_resale_offers_public_id",
                schema: "booking",
                table: "resale_offers");

            migrationBuilder.DropColumn(
                name: "absences_as_sharing",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "card_top_up_amount",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "card_top_up_enabled",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "manual_credit_amount",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "manual_credit_enabled",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "manual_credit_units_per_day",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "min_booking_days",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "voucher_credit_amount",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "voucher_credit_enabled",
                schema: "booking",
                table: "resale_settings");

            migrationBuilder.DropColumn(
                name: "excludes_from_price",
                schema: "booking",
                table: "resale_offers");

            migrationBuilder.DropColumn(
                name: "public_id",
                schema: "booking",
                table: "resale_offers");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:booking.additional_item_date_scope", "whole_stay,specific_dates")
                .Annotation("Npgsql:Enum:booking.additional_item_source", "price_list,inventory")
                .Annotation("Npgsql:Enum:booking.adjustment_type", "percentage,amount")
                .Annotation("Npgsql:Enum:booking.agreement_line_type", "regular,voucher")
                .Annotation("Npgsql:Enum:booking.booking_channel", "customer,manager")
                .Annotation("Npgsql:Enum:booking.cancellation_reason", "customer,manager,rejected,property_trashed,spot_removed,payment_failed,confirmation_expired")
                .Annotation("Npgsql:Enum:booking.consumption_source", "reservation_line,service_line,additional_item,manual_hold,resale")
                .Annotation("Npgsql:Enum:booking.consumption_state", "held,confirmed,released")
                .Annotation("Npgsql:Enum:booking.day_period", "full_day,morning,afternoon")
                .Annotation("Npgsql:Enum:booking.discount_channel", "any,manager_only,online_only")
                .Annotation("Npgsql:Enum:booking.discount_origin", "code_entered,auto_applied")
                .Annotation("Npgsql:Enum:booking.discount_scope", "all,custom,without_custom")
                .Annotation("Npgsql:Enum:booking.discount_type", "fixed,percentage")
                .Annotation("Npgsql:Enum:booking.hold_kind", "operational,checkout")
                .Annotation("Npgsql:Enum:booking.hold_status", "active,released,expired,converted")
                .Annotation("Npgsql:Enum:booking.listing_type", "standard,agreement_price_list,agreement_voucher")
                .Annotation("Npgsql:Enum:booking.online_confirmation_state", "not_required,pending,confirmed")
                .Annotation("Npgsql:Enum:booking.participant_role", "viewer")
                .Annotation("Npgsql:Enum:booking.payment_mode", "cash,credit_card,bank_transfer,other")
                .Annotation("Npgsql:Enum:booking.resale_credit_calc", "fixed_amount,daily_percentage")
                .Annotation("Npgsql:Enum:booking.resale_credit_entry", "earned,reversed,adjustment")
                .Annotation("Npgsql:Enum:booking.resale_mode", "absence,sharing")
                .Annotation("Npgsql:Enum:booking.resale_offer_state", "available,reassigned,withdrawn")
                .Annotation("Npgsql:Enum:booking.resale_origin", "absence,sharing,absence_as_sharing")
                .Annotation("Npgsql:Enum:booking.resale_scope", "seasonal_workers,all_articles")
                .Annotation("Npgsql:Enum:booking.reservation_kind", "spot,service")
                .Annotation("Npgsql:Enum:booking.reservation_price_flag", "suggested_price_accepted,recalculated,manually_edited")
                .Annotation("Npgsql:Enum:booking.reservation_status", "pending,confirmed,cancelled")
                .Annotation("Npgsql:Enum:booking.resource_type", "umbrella,sunbed,sunbed_xl,cabin,special_cabin,deckchair,chair,tent,parking,ticket,area_seat,bar_service,normal_service,hourly_service,inventory_item")
                .Annotation("Npgsql:Enum:booking.service_category", "bar,normal,hourly,additional")
                .OldAnnotation("Npgsql:Enum:booking.additional_item_date_scope", "whole_stay,specific_dates")
                .OldAnnotation("Npgsql:Enum:booking.additional_item_source", "price_list,inventory")
                .OldAnnotation("Npgsql:Enum:booking.adjustment_type", "percentage,amount")
                .OldAnnotation("Npgsql:Enum:booking.agreement_line_type", "regular,voucher")
                .OldAnnotation("Npgsql:Enum:booking.booking_channel", "customer,manager")
                .OldAnnotation("Npgsql:Enum:booking.cancellation_reason", "customer,manager,rejected,property_trashed,spot_removed,payment_failed,confirmation_expired")
                .OldAnnotation("Npgsql:Enum:booking.consumption_source", "reservation_line,service_line,additional_item,manual_hold,resale")
                .OldAnnotation("Npgsql:Enum:booking.consumption_state", "held,confirmed,released")
                .OldAnnotation("Npgsql:Enum:booking.day_period", "full_day,morning,afternoon")
                .OldAnnotation("Npgsql:Enum:booking.discount_channel", "any,manager_only,online_only")
                .OldAnnotation("Npgsql:Enum:booking.discount_origin", "code_entered,auto_applied")
                .OldAnnotation("Npgsql:Enum:booking.discount_scope", "all,custom,without_custom")
                .OldAnnotation("Npgsql:Enum:booking.discount_type", "fixed,percentage")
                .OldAnnotation("Npgsql:Enum:booking.hold_kind", "operational,checkout")
                .OldAnnotation("Npgsql:Enum:booking.hold_status", "active,released,expired,converted")
                .OldAnnotation("Npgsql:Enum:booking.listing_type", "standard,agreement_price_list,agreement_voucher")
                .OldAnnotation("Npgsql:Enum:booking.online_confirmation_state", "not_required,pending,confirmed")
                .OldAnnotation("Npgsql:Enum:booking.participant_role", "viewer")
                .OldAnnotation("Npgsql:Enum:booking.payment_mode", "cash,credit_card,bank_transfer,other")
                .OldAnnotation("Npgsql:Enum:booking.resale_credit_entry", "earned,reversed,adjustment")
                .OldAnnotation("Npgsql:Enum:booking.resale_offer_state", "available,reassigned,withdrawn")
                .OldAnnotation("Npgsql:Enum:booking.resale_origin", "absence,sharing,absence_as_sharing")
                .OldAnnotation("Npgsql:Enum:booking.resale_scope", "seasonal_only,all_articles")
                .OldAnnotation("Npgsql:Enum:booking.reservation_kind", "spot,service")
                .OldAnnotation("Npgsql:Enum:booking.reservation_price_flag", "suggested_price_accepted,recalculated,manually_edited")
                .OldAnnotation("Npgsql:Enum:booking.reservation_status", "pending,confirmed,cancelled")
                .OldAnnotation("Npgsql:Enum:booking.resource_type", "umbrella,sunbed,sunbed_xl,cabin,special_cabin,deckchair,chair,tent,parking,ticket,area_seat,bar_service,normal_service,hourly_service,inventory_item")
                .OldAnnotation("Npgsql:Enum:booking.service_category", "bar,normal,hourly,additional");

            migrationBuilder.AlterColumn<List<ResaleScope>>(
                name: "applies_to",
                schema: "booking",
                table: "resale_settings",
                type: "booking.resale_scope[]",
                nullable: false,
                defaultValueSql: "'{}'",
                comment: "Estensioni della rivendita: consentita per pass stagionali, estesa a tutte le postazioni del pacchetto",
                oldClrType: typeof(List<ResaleScope>),
                oldType: "booking.resale_scope[]",
                oldDefaultValueSql: "'{}'",
                oldComment: "A cosa si applica lo sharing: solo alle stagionali, a tutti gli articoli (non solo agli ombrelloni)");

            migrationBuilder.AddColumn<bool>(
                name: "both_options_disabled",
                schema: "booking",
                table: "resale_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Disabilita entrambe le modalità (semantica esatta da riverificare col committente, migrato as-is)");

            migrationBuilder.AddColumn<decimal>(
                name: "credit_amount",
                schema: "booking",
                table: "resale_settings",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Valore del credito: importo € se credit_calc = fixed_amount, percentuale 0-100 se daily_percentage");

            migrationBuilder.AddColumn<int>(
                name: "credit_calc",
                schema: "booking",
                table: "resale_settings",
                type: "booking.resale_credit_calc",
                nullable: false,
                defaultValueSql: "'fixed_amount'",
                comment: "Come si calcola il valore del credito per giorno ceduto: importo fisso o percentuale della tariffa giornaliera");

            migrationBuilder.AddColumn<int>(
                name: "credit_units_per_day",
                schema: "booking",
                table: "resale_settings",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Quanti crediti simbolici matura il titolare per ogni giorno ceduto e ripreso");

            migrationBuilder.AddColumn<int>(
                name: "mode",
                schema: "booking",
                table: "resale_settings",
                type: "booking.resale_mode",
                nullable: false,
                defaultValueSql: "'absence'",
                comment: "Come viene gestita la cessione di un giorno: solo assenza pianificata senza credito, o sharing con credito al titolare");
        }
    }
}
