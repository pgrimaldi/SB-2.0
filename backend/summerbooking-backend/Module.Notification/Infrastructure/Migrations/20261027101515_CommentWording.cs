using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommentWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "user_channel_preferences",
                schema: "notification",
                comment: "Preferenze di notifica di un utente di Identity (staff/gestore): canali abilitati, non disturbare, tipi silenziati.",
                oldComment: "Preferenze di notifica di un utente C5 (staff/gestore): canali abilitati, non disturbare, tipi silenziati.");

            migrationBuilder.AlterTable(
                name: "push_device_tokens",
                schema: "notification",
                comment: "Un token di dispositivo registrato da un utente di Identity per ricevere notifiche push, con piattaforma, ultimo uso e stato.",
                oldComment: "Un token di dispositivo registrato da un utente C5 per ricevere notifiche push, con piattaforma, ultimo uso e stato.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_muted",
                schema: "notification",
                table: "user_channel_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "\"Non disturbare\" globale: nessuna notifica operativa finché attivo (rispettato dal modulo).",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "\"Non disturbare\" globale: nessuna notifica operativa finché attivo (rispettato dal modulo, il legacy lo ignorava).");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "notification",
                table: "templates",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete: ex status=0 di custom_templates / isDeleted di hotel_message_templates.");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_public_id",
                schema: "notification",
                table: "marketing_newsletter_subscriptions",
                type: "uuid",
                nullable: true,
                comment: "public_id di identity_access... customers (Identity), copiato — nessuna FK cross-schema.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di identity_access... customers (C5), copiato — nessuna FK cross-schema.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "notification",
                table: "marketing_events",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile (API/URL), copiato anche nella proiezione verso la vetrina Catalog.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile (API/URL), copiato anche nella proiezione verso la vetrina C2.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_public",
                schema: "notification",
                table: "marketing_events",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Se l'evento è mostrato sulla pagina pubblica dello stabilimento (filtra davvero la proiezione verso Catalog).",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Se l'evento è mostrato sulla pagina pubblica dello stabilimento (filtra davvero la proiezione verso C2).");

            migrationBuilder.AlterColumn<bool>(
                name: "also_send_push",
                schema: "notification",
                table: "marketing_email_campaigns",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Se, dopo l'email, inviare anche una push FCM ai destinatari utenti di Identity con device token.",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Se, dopo l'email, inviare anche una push FCM ai destinatari utenti C5 con device token.");

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                schema: "notification",
                table: "marketing_contacts",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true,
                comment: "Nome del contatto, denormalizzato per la personalizzazione senza join a Identity.",
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160,
                oldNullable: true,
                oldComment: "Nome del contatto, denormalizzato per la personalizzazione senza join a C5.");

            migrationBuilder.AlterColumn<bool>(
                name: "also_send_push",
                schema: "notification",
                table: "marketing_automations",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Se inviare anche una push FCM ai destinatari utenti di Identity con device token.",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Se inviare anche una push FCM ai destinatari utenti C5 con device token.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "user_channel_preferences",
                schema: "notification",
                comment: "Preferenze di notifica di un utente C5 (staff/gestore): canali abilitati, non disturbare, tipi silenziati.",
                oldComment: "Preferenze di notifica di un utente di Identity (staff/gestore): canali abilitati, non disturbare, tipi silenziati.");

            migrationBuilder.AlterTable(
                name: "push_device_tokens",
                schema: "notification",
                comment: "Un token di dispositivo registrato da un utente C5 per ricevere notifiche push, con piattaforma, ultimo uso e stato.",
                oldComment: "Un token di dispositivo registrato da un utente di Identity per ricevere notifiche push, con piattaforma, ultimo uso e stato.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_muted",
                schema: "notification",
                table: "user_channel_preferences",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "\"Non disturbare\" globale: nessuna notifica operativa finché attivo (rispettato dal modulo, il legacy lo ignorava).",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "\"Non disturbare\" globale: nessuna notifica operativa finché attivo (rispettato dal modulo).");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "notification",
                table: "templates",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete: ex status=0 di custom_templates / isDeleted di hotel_message_templates.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete.");

            migrationBuilder.AlterColumn<Guid>(
                name: "customer_public_id",
                schema: "notification",
                table: "marketing_newsletter_subscriptions",
                type: "uuid",
                nullable: true,
                comment: "public_id di identity_access... customers (C5), copiato — nessuna FK cross-schema.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di identity_access... customers (Identity), copiato — nessuna FK cross-schema.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "notification",
                table: "marketing_events",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile (API/URL), copiato anche nella proiezione verso la vetrina C2.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile (API/URL), copiato anche nella proiezione verso la vetrina Catalog.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_public",
                schema: "notification",
                table: "marketing_events",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Se l'evento è mostrato sulla pagina pubblica dello stabilimento (filtra davvero la proiezione verso C2).",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Se l'evento è mostrato sulla pagina pubblica dello stabilimento (filtra davvero la proiezione verso Catalog).");

            migrationBuilder.AlterColumn<bool>(
                name: "also_send_push",
                schema: "notification",
                table: "marketing_email_campaigns",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Se, dopo l'email, inviare anche una push FCM ai destinatari utenti C5 con device token.",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Se, dopo l'email, inviare anche una push FCM ai destinatari utenti di Identity con device token.");

            migrationBuilder.AlterColumn<string>(
                name: "full_name",
                schema: "notification",
                table: "marketing_contacts",
                type: "character varying(160)",
                maxLength: 160,
                nullable: true,
                comment: "Nome del contatto, denormalizzato per la personalizzazione senza join a C5.",
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160,
                oldNullable: true,
                oldComment: "Nome del contatto, denormalizzato per la personalizzazione senza join a Identity.");

            migrationBuilder.AlterColumn<bool>(
                name: "also_send_push",
                schema: "notification",
                table: "marketing_automations",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Se inviare anche una push FCM ai destinatari utenti C5 con device token.",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Se inviare anche una push FCM ai destinatari utenti di Identity con device token.");
        }
    }
}
