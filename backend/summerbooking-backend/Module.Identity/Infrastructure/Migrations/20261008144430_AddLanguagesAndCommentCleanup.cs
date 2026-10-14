using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Module.Identity.Domain.Enums;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Module.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLanguagesAndCommentCleanup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:identity.auth_challenge_channel", "email,sms")
                .Annotation("Npgsql:Enum:identity.auth_challenge_invalidation", "superseded,too_many_attempts,manual,account_disabled")
                .Annotation("Npgsql:Enum:identity.auth_challenge_purpose", "second_factor_login,email_verification,password_reset,email_change_confirmation,invitation")
                .Annotation("Npgsql:Enum:identity.contract_acceptance_event", "accepted,revoked")
                .Annotation("Npgsql:Enum:identity.contract_document_status", "draft,published,retired")
                .Annotation("Npgsql:Enum:identity.customer_source", "contact_form,booking,service_reservation,card_transaction,import")
                .Annotation("Npgsql:Enum:identity.login_method", "normal,google,facebook,apple")
                .Annotation("Npgsql:Enum:identity.notification_sound_scope", "management_booking,online_booking,management_service_booking,online_service_booking")
                .Annotation("Npgsql:Enum:identity.refresh_revoke_reason", "logout,password_changed,account_disabled,manual,reuse_detected,superseded")
                .Annotation("Npgsql:Enum:identity.second_factor_channel", "email,sms,totp")
                .Annotation("Npgsql:Enum:identity.storage_origin", "s3_us_east_2,s3_us_east_1,cloudfront,external")
                .Annotation("Npgsql:Enum:identity.subscription_period_close_reason", "renewed,expired,cancelled,superseded")
                .Annotation("Npgsql:Enum:identity.subscription_period_kind", "initial,renewal,migration,manual_adjustment")
                .Annotation("Npgsql:Enum:identity.subscription_status", "active,suspended,expired,cancelled")
                .Annotation("Npgsql:Enum:identity.subscription_term_type", "fixed_term,legacy_day_bank")
                .Annotation("Npgsql:Enum:identity.ui_language", "it,en,fr,es,de,zh,ar")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:identity.auth_challenge_channel", "email,sms")
                .OldAnnotation("Npgsql:Enum:identity.auth_challenge_invalidation", "superseded,too_many_attempts,manual,account_disabled")
                .OldAnnotation("Npgsql:Enum:identity.auth_challenge_purpose", "second_factor_login,email_verification,password_reset,email_change_confirmation,invitation")
                .OldAnnotation("Npgsql:Enum:identity.contract_acceptance_event", "accepted,revoked")
                .OldAnnotation("Npgsql:Enum:identity.contract_document_status", "draft,published,retired")
                .OldAnnotation("Npgsql:Enum:identity.customer_source", "contact_form,booking,service_reservation,card_transaction,import")
                .OldAnnotation("Npgsql:Enum:identity.login_method", "normal,google,facebook,apple")
                .OldAnnotation("Npgsql:Enum:identity.notification_sound_scope", "management_booking,online_booking,management_service_booking,online_service_booking")
                .OldAnnotation("Npgsql:Enum:identity.refresh_revoke_reason", "logout,password_changed,account_disabled,manual,reuse_detected,superseded")
                .OldAnnotation("Npgsql:Enum:identity.second_factor_channel", "email,sms,totp")
                .OldAnnotation("Npgsql:Enum:identity.storage_origin", "s3_us_east_2,s3_us_east_1,cloudfront,external")
                .OldAnnotation("Npgsql:Enum:identity.subscription_period_close_reason", "renewed,expired,cancelled,superseded")
                .OldAnnotation("Npgsql:Enum:identity.subscription_period_kind", "initial,renewal,migration,manual_adjustment")
                .OldAnnotation("Npgsql:Enum:identity.subscription_status", "active,suspended,expired,cancelled")
                .OldAnnotation("Npgsql:Enum:identity.subscription_term_type", "fixed_term,legacy_day_bank")
                .OldAnnotation("Npgsql:Enum:identity.ui_language", "it,en")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<SubscriptionTermType>(
                name: "term_type",
                schema: "identity",
                table: "account_subscriptions",
                type: "identity.subscription_term_type",
                nullable: false,
                defaultValueSql: "'fixed_term'",
                comment: "Tipo di abbonamento: a termine fisso con date contrattuali, o banca giorni a consumo.",
                oldClrType: typeof(SubscriptionTermType),
                oldType: "identity.subscription_term_type",
                oldDefaultValueSql: "'fixed_term'",
                oldComment: "Tipo di abbonamento: a termine fisso con date contrattuali, o banca giorni legacy.");

            migrationBuilder.AlterColumn<int>(
                name: "remaining_days_balance",
                schema: "identity",
                table: "account_subscriptions",
                type: "integer",
                nullable: true,
                comment: "Banca giorni residua del modello a consumo; valorizzata solo per term_type = legacy_day_bank.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "Banca giorni residua del vecchio modello a consumo; valorizzata solo per term_type = legacy_day_bank.");

            migrationBuilder.AlterColumn<SubscriptionTermType>(
                name: "term_type",
                schema: "identity",
                table: "account_subscription_periods",
                type: "identity.subscription_term_type",
                nullable: false,
                defaultValueSql: "'fixed_term'",
                comment: "Tipo del periodo (a termine fisso o banca giorni a consumo).",
                oldClrType: typeof(SubscriptionTermType),
                oldType: "identity.subscription_term_type",
                oldDefaultValueSql: "'fixed_term'",
                oldComment: "Tipo del periodo (a termine fisso o banca giorni legacy).");

            migrationBuilder.InsertData(
                schema: "identity",
                table: "account_license_plan_translations",
                columns: new[] { "id", "created_at", "culture", "deleted_at", "description", "label", "license_plan_id", "updated_at" },
                values: new object[,]
                {
                    { 5L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "fr", null, null, "Basic", 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "fr", null, null, "Advanced", 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "es", null, null, "Basic", 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "es", null, null, "Advanced", 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "de", null, null, "Basic", 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "de", null, null, "Advanced", 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "zh", null, null, "Basic", 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "zh", null, null, "Advanced", 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ar", null, null, "Basic", 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 14L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "ar", null, null, "Advanced", 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 5L);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 6L);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 7L);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 8L);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 9L);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 10L);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 11L);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 12L);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 13L);

            migrationBuilder.DeleteData(
                schema: "identity",
                table: "account_license_plan_translations",
                keyColumn: "id",
                keyValue: 14L);

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:identity.auth_challenge_channel", "email,sms")
                .Annotation("Npgsql:Enum:identity.auth_challenge_invalidation", "superseded,too_many_attempts,manual,account_disabled")
                .Annotation("Npgsql:Enum:identity.auth_challenge_purpose", "second_factor_login,email_verification,password_reset,email_change_confirmation,invitation")
                .Annotation("Npgsql:Enum:identity.contract_acceptance_event", "accepted,revoked")
                .Annotation("Npgsql:Enum:identity.contract_document_status", "draft,published,retired")
                .Annotation("Npgsql:Enum:identity.customer_source", "contact_form,booking,service_reservation,card_transaction,import")
                .Annotation("Npgsql:Enum:identity.login_method", "normal,google,facebook,apple")
                .Annotation("Npgsql:Enum:identity.notification_sound_scope", "management_booking,online_booking,management_service_booking,online_service_booking")
                .Annotation("Npgsql:Enum:identity.refresh_revoke_reason", "logout,password_changed,account_disabled,manual,reuse_detected,superseded")
                .Annotation("Npgsql:Enum:identity.second_factor_channel", "email,sms,totp")
                .Annotation("Npgsql:Enum:identity.storage_origin", "s3_us_east_2,s3_us_east_1,cloudfront,external")
                .Annotation("Npgsql:Enum:identity.subscription_period_close_reason", "renewed,expired,cancelled,superseded")
                .Annotation("Npgsql:Enum:identity.subscription_period_kind", "initial,renewal,migration,manual_adjustment")
                .Annotation("Npgsql:Enum:identity.subscription_status", "active,suspended,expired,cancelled")
                .Annotation("Npgsql:Enum:identity.subscription_term_type", "fixed_term,legacy_day_bank")
                .Annotation("Npgsql:Enum:identity.ui_language", "it,en")
                .Annotation("Npgsql:PostgresExtension:citext", ",,")
                .OldAnnotation("Npgsql:Enum:identity.auth_challenge_channel", "email,sms")
                .OldAnnotation("Npgsql:Enum:identity.auth_challenge_invalidation", "superseded,too_many_attempts,manual,account_disabled")
                .OldAnnotation("Npgsql:Enum:identity.auth_challenge_purpose", "second_factor_login,email_verification,password_reset,email_change_confirmation,invitation")
                .OldAnnotation("Npgsql:Enum:identity.contract_acceptance_event", "accepted,revoked")
                .OldAnnotation("Npgsql:Enum:identity.contract_document_status", "draft,published,retired")
                .OldAnnotation("Npgsql:Enum:identity.customer_source", "contact_form,booking,service_reservation,card_transaction,import")
                .OldAnnotation("Npgsql:Enum:identity.login_method", "normal,google,facebook,apple")
                .OldAnnotation("Npgsql:Enum:identity.notification_sound_scope", "management_booking,online_booking,management_service_booking,online_service_booking")
                .OldAnnotation("Npgsql:Enum:identity.refresh_revoke_reason", "logout,password_changed,account_disabled,manual,reuse_detected,superseded")
                .OldAnnotation("Npgsql:Enum:identity.second_factor_channel", "email,sms,totp")
                .OldAnnotation("Npgsql:Enum:identity.storage_origin", "s3_us_east_2,s3_us_east_1,cloudfront,external")
                .OldAnnotation("Npgsql:Enum:identity.subscription_period_close_reason", "renewed,expired,cancelled,superseded")
                .OldAnnotation("Npgsql:Enum:identity.subscription_period_kind", "initial,renewal,migration,manual_adjustment")
                .OldAnnotation("Npgsql:Enum:identity.subscription_status", "active,suspended,expired,cancelled")
                .OldAnnotation("Npgsql:Enum:identity.subscription_term_type", "fixed_term,legacy_day_bank")
                .OldAnnotation("Npgsql:Enum:identity.ui_language", "it,en,fr,es,de,zh,ar")
                .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.AlterColumn<SubscriptionTermType>(
                name: "term_type",
                schema: "identity",
                table: "account_subscriptions",
                type: "identity.subscription_term_type",
                nullable: false,
                defaultValueSql: "'fixed_term'",
                comment: "Tipo di abbonamento: a termine fisso con date contrattuali, o banca giorni legacy.",
                oldClrType: typeof(SubscriptionTermType),
                oldType: "identity.subscription_term_type",
                oldDefaultValueSql: "'fixed_term'",
                oldComment: "Tipo di abbonamento: a termine fisso con date contrattuali, o banca giorni a consumo.");

            migrationBuilder.AlterColumn<int>(
                name: "remaining_days_balance",
                schema: "identity",
                table: "account_subscriptions",
                type: "integer",
                nullable: true,
                comment: "Banca giorni residua del vecchio modello a consumo; valorizzata solo per term_type = legacy_day_bank.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "Banca giorni residua del modello a consumo; valorizzata solo per term_type = legacy_day_bank.");

            migrationBuilder.AlterColumn<SubscriptionTermType>(
                name: "term_type",
                schema: "identity",
                table: "account_subscription_periods",
                type: "identity.subscription_term_type",
                nullable: false,
                defaultValueSql: "'fixed_term'",
                comment: "Tipo del periodo (a termine fisso o banca giorni legacy).",
                oldClrType: typeof(SubscriptionTermType),
                oldType: "identity.subscription_term_type",
                oldDefaultValueSql: "'fixed_term'",
                oldComment: "Tipo del periodo (a termine fisso o banca giorni a consumo).");
        }
    }
}
