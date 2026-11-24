using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OnlineBooking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                .Annotation("Npgsql:Enum:booking.online_confirmation_state", "not_required,pending,confirmed,rejected,expired")
                .Annotation("Npgsql:Enum:booking.participant_role", "viewer")
                .Annotation("Npgsql:Enum:booking.payment_mode", "cash,credit_card,bank_transfer,other")
                .Annotation("Npgsql:Enum:booking.resale_credit_entry", "earned,reversed,adjustment")
                .Annotation("Npgsql:Enum:booking.resale_offer_state", "available,reassigned,withdrawn")
                .Annotation("Npgsql:Enum:booking.resale_origin", "absence,sharing,absence_as_sharing")
                .Annotation("Npgsql:Enum:booking.resale_scope", "seasonal_only,all_articles")
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

            migrationBuilder.AddColumn<DateTime>(
                name: "payment_hold_until",
                schema: "booking",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Prenotazione online in attesa del pagamento: postazioni tenute fino a quest'ora.");

            migrationBuilder.CreateIndex(
                name: "ix_booking_reservations_payment_hold",
                schema: "booking",
                table: "reservations",
                column: "payment_hold_until",
                filter: "payment_hold_until IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_booking_reservations_payment_hold",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "payment_hold_until",
                schema: "booking",
                table: "reservations");

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
                .Annotation("Npgsql:Enum:booking.resale_credit_entry", "earned,reversed,adjustment")
                .Annotation("Npgsql:Enum:booking.resale_offer_state", "available,reassigned,withdrawn")
                .Annotation("Npgsql:Enum:booking.resale_origin", "absence,sharing,absence_as_sharing")
                .Annotation("Npgsql:Enum:booking.resale_scope", "seasonal_only,all_articles")
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
                .OldAnnotation("Npgsql:Enum:booking.online_confirmation_state", "not_required,pending,confirmed,rejected,expired")
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
        }
    }
}
