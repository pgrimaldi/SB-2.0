using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Fiskaly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommentWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "fiskaly",
                table: "receipts",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile (UUIDv7); usato in URL gestionale e copiato cross-modulo da Payment/Notification.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile (UUIDv7); usato in URL gestionale e copiato cross-modulo da C3/C6.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ledger_entry_public_id",
                schema: "fiskaly",
                table: "receipt_payments",
                type: "uuid",
                nullable: true,
                comment: "public_id di payment.ledger_entries, copiato — nessuna FK cross-schema. Movimento di incasso corrispondente nel libro incassi Payment.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di payment.ledger_entries, copiato — nessuna FK cross-schema. Movimento di incasso corrispondente nel libro incassi C3.");

            migrationBuilder.AlterColumn<string>(
                name: "label",
                schema: "fiskaly",
                table: "receipt_payments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "Etichetta descrittiva libera (es. per online/other) o valore originale dei dati importati, testo congelato.",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true,
                oldComment: "Etichetta descrittiva libera (es. per online/other) o valore originale del drift legacy, testo congelato.");

            migrationBuilder.AlterColumn<string>(
                name: "fiskaly_error_message",
                schema: "fiskaly",
                table: "receipt_logs",
                type: "text",
                nullable: true,
                comment: "Messaggio d'errore Fiskaly completo, non troncato.",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Messaggio d'errore Fiskaly completo, non troncato (a differenza del legacy).");

            migrationBuilder.AlterColumn<Guid>(
                name: "source_line_public_id",
                schema: "fiskaly",
                table: "receipt_lines",
                type: "uuid",
                nullable: true,
                comment: "public_id della riga prenotazione di Booking di provenienza (booking_lines / booking_service_lines / booking_line_additional_items secondo line_kind), copiato — nessuna FK cross-schema. NULL per voci sintetiche non mappabili.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id della riga prenotazione C1 di provenienza (booking_lines / booking_service_lines / booking_line_additional_items secondo line_kind), copiato — nessuna FK cross-schema. NULL per voci sintetiche non mappabili.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "fiskaly",
                table: "receipts",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile (UUIDv7); usato in URL gestionale e copiato cross-modulo da C3/C6.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile (UUIDv7); usato in URL gestionale e copiato cross-modulo da Payment/Notification.");

            migrationBuilder.AlterColumn<Guid>(
                name: "ledger_entry_public_id",
                schema: "fiskaly",
                table: "receipt_payments",
                type: "uuid",
                nullable: true,
                comment: "public_id di payment.ledger_entries, copiato — nessuna FK cross-schema. Movimento di incasso corrispondente nel libro incassi C3.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di payment.ledger_entries, copiato — nessuna FK cross-schema. Movimento di incasso corrispondente nel libro incassi Payment.");

            migrationBuilder.AlterColumn<string>(
                name: "label",
                schema: "fiskaly",
                table: "receipt_payments",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "Etichetta descrittiva libera (es. per online/other) o valore originale del drift legacy, testo congelato.",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true,
                oldComment: "Etichetta descrittiva libera (es. per online/other) o valore originale dei dati importati, testo congelato.");

            migrationBuilder.AlterColumn<string>(
                name: "fiskaly_error_message",
                schema: "fiskaly",
                table: "receipt_logs",
                type: "text",
                nullable: true,
                comment: "Messaggio d'errore Fiskaly completo, non troncato (a differenza del legacy).",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true,
                oldComment: "Messaggio d'errore Fiskaly completo, non troncato.");

            migrationBuilder.AlterColumn<Guid>(
                name: "source_line_public_id",
                schema: "fiskaly",
                table: "receipt_lines",
                type: "uuid",
                nullable: true,
                comment: "public_id della riga prenotazione C1 di provenienza (booking_lines / booking_service_lines / booking_line_additional_items secondo line_kind), copiato — nessuna FK cross-schema. NULL per voci sintetiche non mappabili.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id della riga prenotazione di Booking di provenienza (booking_lines / booking_service_lines / booking_line_additional_items secondo line_kind), copiato — nessuna FK cross-schema. NULL per voci sintetiche non mappabili.");
        }
    }
}
