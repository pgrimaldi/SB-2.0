using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Payment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommentWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "payment",
                table: "wallet_transactions",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato da Booking (riga servizio pagata col borsellino) e da Fiskaly (pagamento VOUCHER/OUTSTANDING dello scontrino).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato da C1 (riga servizio pagata col borsellino) e da C4 (pagamento VOUCHER/OUTSTANDING dello scontrino).");

            migrationBuilder.AlterColumn<string>(
                name: "description_snapshot",
                schema: "payment",
                table: "wallet_transaction_items",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                comment: "Nome dell'articolo congelato al momento del consumo; il nome vivo si legge da Catalog quando sale_item_public_id è valorizzato.",
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160,
                oldComment: "Nome dell'articolo congelato al momento del consumo; il nome vivo si legge da C2 quando sale_item_public_id è valorizzato.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "payment",
                table: "wallet_accounts",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, esposto al cliente e copiato da Booking sulla riga servizio pagata col borsellino.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, esposto al cliente e copiato da C1 sulla riga servizio pagata col borsellino.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "payment",
                table: "transaction_refunds",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, per il gestionale e per eventuale riferimento da Fiskaly/Notification.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, per il gestionale e per eventuale riferimento da C4/C6.");

            migrationBuilder.AlterColumn<string>(
                name: "stripe_payment_intent_id",
                schema: "payment",
                table: "transaction_charges",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "Id del PaymentIntent Stripe; NULL per paga-in-struttura. 255 caratteri perche' puo' contenere anche un Checkout Session id (\"cs_live_...\", 66+ caratteri) invece di un PaymentIntent id (\"pi_...\").",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "Id del PaymentIntent Stripe; NULL per paga-in-struttura. 255 (non 40): il legacy ha dimostrato che questo campo puo' contenere anche un Checkout Session id (\"cs_live_...\", fino a 66+ caratteri) invece di un vero PaymentIntent id (\"pi_...\") - bug reale osservato il 2026-09-15, 190 righe scartate a NULL dalla migrazione perche' il varchar(40) le rifiutava.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "payment",
                table: "transaction_charges",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo (Fiskaly autoreceipt; URL gestionale).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo (C4 autoreceipt; URL gestionale).");

            migrationBuilder.AlterColumn<string>(
                name: "booking_number",
                schema: "payment",
                table: "pos_charges",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                comment: "Numero prenotazione, denormalizzato per la correlazione con la cassa.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "Numero prenotazione, denormalizzato per correlazione col legacy.");

            migrationBuilder.AlterColumn<string>(
                name: "description_snapshot",
                schema: "payment",
                table: "ledger_entry_lines",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                comment: "Descrizione dell'articolo congelata al momento del movimento; il nome vivo si legge da Booking quando reservation_line_public_id è valorizzato.",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldComment: "Descrizione dell'articolo congelata al momento del movimento; il nome vivo si legge da C1 quando reservation_line_public_id è valorizzato.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "payment",
                table: "ledger_entries",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo (Fiskaly, righe payments[] dello scontrino; export contabile).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo (C4 righe payments[] dello scontrino; export contabile).");

            migrationBuilder.AlterColumn<int>(
                name: "payment_mode",
                schema: "payment",
                table: "ledger_entries",
                type: "payment.payment_mode",
                nullable: true,
                defaultValueSql: "'other'",
                comment: "Modalità con cui è avvenuto il movimento; NULL sui movimenti importati non normalizzabili.",
                oldClrType: typeof(int),
                oldType: "payment.payment_mode",
                oldNullable: true,
                oldDefaultValueSql: "'other'",
                oldComment: "Modalità con cui è avvenuto il movimento; NULL sui movimenti legacy non normalizzabili.");

            migrationBuilder.AlterColumn<decimal>(
                name: "line_total",
                schema: "payment",
                table: "wallet_transaction_items",
                type: "numeric(12,2)",
                nullable: false,
                computedColumnSql: "quantity * unit_price + variants_surcharge",
                stored: true,
                comment: "Totale della riga — colonna generata (quantity * unit_price + variants_surcharge). Sola lettura: le rettifiche sono un nuovo movimento con source = 'adjustment'. Il totale non puo' mai essere null o incoerente con le sue componenti.",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldComputedColumnSql: "quantity * unit_price + variants_surcharge",
                oldStored: true,
                oldComment: "Totale della riga — colonna generata (quantity * unit_price + variants_surcharge). Sola lettura: le rettifiche sono un nuovo movimento con source = 'adjustment'. Chiude anche un bug storico del legacy, dove il totale arrivava spesso null o incoerente con le sue componenti.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "payment",
                table: "wallet_transactions",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato da C1 (riga servizio pagata col borsellino) e da C4 (pagamento VOUCHER/OUTSTANDING dello scontrino).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato da Booking (riga servizio pagata col borsellino) e da Fiskaly (pagamento VOUCHER/OUTSTANDING dello scontrino).");

            migrationBuilder.AlterColumn<string>(
                name: "description_snapshot",
                schema: "payment",
                table: "wallet_transaction_items",
                type: "character varying(160)",
                maxLength: 160,
                nullable: false,
                comment: "Nome dell'articolo congelato al momento del consumo; il nome vivo si legge da C2 quando sale_item_public_id è valorizzato.",
                oldClrType: typeof(string),
                oldType: "character varying(160)",
                oldMaxLength: 160,
                oldComment: "Nome dell'articolo congelato al momento del consumo; il nome vivo si legge da Catalog quando sale_item_public_id è valorizzato.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "payment",
                table: "wallet_accounts",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, esposto al cliente e copiato da C1 sulla riga servizio pagata col borsellino.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, esposto al cliente e copiato da Booking sulla riga servizio pagata col borsellino.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "payment",
                table: "transaction_refunds",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, per il gestionale e per eventuale riferimento da C4/C6.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, per il gestionale e per eventuale riferimento da Fiskaly/Notification.");

            migrationBuilder.AlterColumn<string>(
                name: "stripe_payment_intent_id",
                schema: "payment",
                table: "transaction_charges",
                type: "character varying(255)",
                maxLength: 255,
                nullable: true,
                comment: "Id del PaymentIntent Stripe; NULL per paga-in-struttura. 255 (non 40): il legacy ha dimostrato che questo campo puo' contenere anche un Checkout Session id (\"cs_live_...\", fino a 66+ caratteri) invece di un vero PaymentIntent id (\"pi_...\") - bug reale osservato il 2026-09-15, 190 righe scartate a NULL dalla migrazione perche' il varchar(40) le rifiutava.",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: true,
                oldComment: "Id del PaymentIntent Stripe; NULL per paga-in-struttura. 255 caratteri perche' puo' contenere anche un Checkout Session id (\"cs_live_...\", 66+ caratteri) invece di un PaymentIntent id (\"pi_...\").");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "payment",
                table: "transaction_charges",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo (C4 autoreceipt; URL gestionale).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo (Fiskaly autoreceipt; URL gestionale).");

            migrationBuilder.AlterColumn<string>(
                name: "booking_number",
                schema: "payment",
                table: "pos_charges",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                comment: "Numero prenotazione, denormalizzato per correlazione col legacy.",
                oldClrType: typeof(string),
                oldType: "character varying(32)",
                oldMaxLength: 32,
                oldNullable: true,
                oldComment: "Numero prenotazione, denormalizzato per la correlazione con la cassa.");

            migrationBuilder.AlterColumn<string>(
                name: "description_snapshot",
                schema: "payment",
                table: "ledger_entry_lines",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                comment: "Descrizione dell'articolo congelata al momento del movimento; il nome vivo si legge da C1 quando reservation_line_public_id è valorizzato.",
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldComment: "Descrizione dell'articolo congelata al momento del movimento; il nome vivo si legge da Booking quando reservation_line_public_id è valorizzato.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "payment",
                table: "ledger_entries",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo (C4 righe payments[] dello scontrino; export contabile).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo (Fiskaly, righe payments[] dello scontrino; export contabile).");

            migrationBuilder.AlterColumn<int>(
                name: "payment_mode",
                schema: "payment",
                table: "ledger_entries",
                type: "payment.payment_mode",
                nullable: true,
                defaultValueSql: "'other'",
                comment: "Modalità con cui è avvenuto il movimento; NULL sui movimenti legacy non normalizzabili.",
                oldClrType: typeof(int),
                oldType: "payment.payment_mode",
                oldNullable: true,
                oldDefaultValueSql: "'other'",
                oldComment: "Modalità con cui è avvenuto il movimento; NULL sui movimenti importati non normalizzabili.");

            migrationBuilder.AlterColumn<decimal>(
                name: "line_total",
                schema: "payment",
                table: "wallet_transaction_items",
                type: "numeric(12,2)",
                nullable: false,
                computedColumnSql: "quantity * unit_price + variants_surcharge",
                stored: true,
                comment: "Totale della riga — colonna generata (quantity * unit_price + variants_surcharge). Sola lettura: le rettifiche sono un nuovo movimento con source = 'adjustment'. Chiude anche un bug storico del legacy, dove il totale arrivava spesso null o incoerente con le sue componenti.",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldComputedColumnSql: "quantity * unit_price + variants_surcharge",
                oldStored: true,
                oldComment: "Totale della riga — colonna generata (quantity * unit_price + variants_surcharge). Sola lettura: le rettifiche sono un nuovo movimento con source = 'adjustment'. Il totale non puo' mai essere null o incoerente con le sue componenti.");
        }
    }
}
