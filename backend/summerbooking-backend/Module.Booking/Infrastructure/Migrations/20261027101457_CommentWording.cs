using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommentWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "resource_availability_daily",
                schema: "booking",
                comment: "Proiezione di disponibilità per (risorsa/tipo × giorno), mantenuta dal DB via trigger su booking_resource_consumptions: evita il GROUP BY a ogni lettura.",
                oldComment: "Proiezione di disponibilità per (risorsa/tipo × giorno), mantenuta dal DB via trigger su booking_resource_consumptions: sostituisce il GROUP BY a runtime del legacy.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "booking",
                table: "service_lines",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico della riga servizio, copiato cross-modulo da Fiskaly (line item) e Payment (riga economica).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico della riga servizio, copiato cross-modulo da C4 (line item) e C3 (riga economica).");

            migrationBuilder.AlterColumn<bool>(
                name: "is_paid_with_wallet",
                schema: "booking",
                table: "service_lines",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "La riga è stata pagata col borsellino/tessera prepagata; il movimento wallet è in Payment.",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "La riga è stata pagata col borsellino/tessera prepagata; il movimento wallet è in C3.");

            migrationBuilder.AlterColumn<long>(
                name: "source_id",
                schema: "booking",
                table: "resource_consumptions",
                type: "bigint",
                nullable: true,
                comment: "Id (interno di Booking) dell'entità di origine indicata da source_kind — nessuna FK polimorfa DB, coerenza gestita in applicazione; NULL per manual_hold senza entità a monte",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true,
                oldComment: "Id (interno C1) dell'entità di origine indicata da source_kind — nessuna FK polimorfa DB, coerenza gestita in applicazione; NULL per manual_hold senza entità a monte");

            migrationBuilder.AlterColumn<Guid>(
                name: "resource_id",
                schema: "booking",
                table: "resource_consumptions",
                type: "uuid",
                nullable: true,
                comment: "public_id (Catalog) della risorsa (map_spots / service_sale_items / inventory_items secondo resource_type), copiato — nessuna FK cross-schema; NULL per articolo personalizzato non a catalogo",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id C2 della risorsa (map_spots / service_sale_items / inventory_items secondo resource_type), copiato — nessuna FK cross-schema; NULL per articolo personalizzato non a catalogo");

            migrationBuilder.AlterColumn<Guid>(
                name: "resource_id",
                schema: "booking",
                table: "resource_availability_daily",
                type: "uuid",
                nullable: true,
                comment: "public_id (Catalog) della risorsa (proiezione fine); NULL per la proiezione grossolana per tipo",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id C2 della risorsa (proiezione fine); NULL per la proiezione grossolana per tipo");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "booking",
                table: "reservations",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo da Payment/Fiskaly/Notification.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo da C3/C4/C6.");

            migrationBuilder.AlterColumn<decimal>(
                name: "paid",
                schema: "booking",
                table: "reservations",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Totale già incassato, proiezione dagli eventi di pagamento di Payment.",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldDefaultValue: 0m,
                oldComment: "Totale già incassato, proiezione dagli eventi di pagamento di C3.");

            migrationBuilder.AlterColumn<decimal>(
                name: "deposit",
                schema: "booking",
                table: "reservations",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Importo acconto/caparra corrente (riepilogo); i singoli acconti sono movimenti Payment.",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldDefaultValue: 0m,
                oldComment: "Importo acconto/caparra corrente (riepilogo); i singoli acconti sono movimenti C3.");

            migrationBuilder.AlterColumn<bool>(
                name: "wallet_event_emitted",
                schema: "booking",
                table: "resale_credit_ledger",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "True quando l'evento ResaleCreditEarned è stato consegnato a Payment (Payment) per la riconciliazione col borsellino",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "True quando l'evento ResaleCreditEarned è stato consegnato a Payment (C3) per la riconciliazione col borsellino");

            migrationBuilder.AlterColumn<string>(
                name: "number_prefix",
                schema: "booking",
                table: "number_sequences",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "Prefisso stabile e immutabile del numero prenotazione per la property (di norma il suo id interno)",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Prefisso stabile e immutabile del numero prenotazione per la property (di norma il suo id legacy)");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "booking",
                table: "lines",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile della riga, copiato cross-modulo da Payment (righe economiche) e Fiskaly (line item scontrino).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile della riga, copiato cross-modulo da C3 (righe economiche) e C4 (line item scontrino).");

            migrationBuilder.AlterColumn<string>(
                name: "booking_number",
                schema: "booking",
                table: "lines",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "Numero prenotazione copiato dalla testata, per correlazioni dirette con Payment/Fiskaly senza join.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "Numero prenotazione copiato dalla testata, per correlazioni dirette con C3/C4 senza join.");

            migrationBuilder.AlterColumn<long>(
                name: "reservation_id",
                schema: "booking",
                table: "group_members",
                type: "bigint",
                nullable: false,
                comment: "Prenotazione membro del gruppo (l'intera prenotazione, non una singola riga postazione)",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldComment: "Prenotazione membro del gruppo (l'intera prenotazione, non una singola riga postazione come nel legacy)");

            migrationBuilder.AlterColumn<int>(
                name: "scope",
                schema: "booking",
                table: "discounts",
                type: "booking.discount_scope",
                nullable: false,
                defaultValueSql: "'all'",
                comment: "Ambito di validità del voucher, congelato dalla regola di Catalog.",
                oldClrType: typeof(int),
                oldType: "booking.discount_scope",
                oldDefaultValueSql: "'all'",
                oldComment: "Ambito di validità del voucher, congelato dalla regola di C2.");

            migrationBuilder.AlterColumn<bool>(
                name: "consumed_usage",
                schema: "booking",
                table: "discounts",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "True se questa prenotazione ha consumato un utilizzo del voucher; l'incremento/decremento del contatore è atomico su Catalog.",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "True se questa prenotazione ha consumato un utilizzo del voucher; l'incremento/decremento del contatore è atomico su C2.");

            migrationBuilder.AlterColumn<int>(
                name: "applied_channel",
                schema: "booking",
                table: "discounts",
                type: "booking.discount_channel",
                nullable: false,
                defaultValueSql: "'any'",
                comment: "Canale su cui il voucher era ammesso, congelato dalla regola di Catalog.",
                oldClrType: typeof(int),
                oldType: "booking.discount_channel",
                oldDefaultValueSql: "'any'",
                oldComment: "Canale su cui il voucher era ammesso, congelato dalla regola di C2.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "booking",
                table: "agreement_lines",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico, copiato cross-modulo verso Payment che tiene importo/stato pagamento della voce.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico, copiato cross-modulo verso C3 che tiene importo/stato pagamento della voce.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "resource_availability_daily",
                schema: "booking",
                comment: "Proiezione di disponibilità per (risorsa/tipo × giorno), mantenuta dal DB via trigger su booking_resource_consumptions: sostituisce il GROUP BY a runtime del legacy.",
                oldComment: "Proiezione di disponibilità per (risorsa/tipo × giorno), mantenuta dal DB via trigger su booking_resource_consumptions: evita il GROUP BY a ogni lettura.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "booking",
                table: "service_lines",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico della riga servizio, copiato cross-modulo da C4 (line item) e C3 (riga economica).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico della riga servizio, copiato cross-modulo da Fiskaly (line item) e Payment (riga economica).");

            migrationBuilder.AlterColumn<bool>(
                name: "is_paid_with_wallet",
                schema: "booking",
                table: "service_lines",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "La riga è stata pagata col borsellino/tessera prepagata; il movimento wallet è in C3.",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "La riga è stata pagata col borsellino/tessera prepagata; il movimento wallet è in Payment.");

            migrationBuilder.AlterColumn<long>(
                name: "source_id",
                schema: "booking",
                table: "resource_consumptions",
                type: "bigint",
                nullable: true,
                comment: "Id (interno C1) dell'entità di origine indicata da source_kind — nessuna FK polimorfa DB, coerenza gestita in applicazione; NULL per manual_hold senza entità a monte",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true,
                oldComment: "Id (interno di Booking) dell'entità di origine indicata da source_kind — nessuna FK polimorfa DB, coerenza gestita in applicazione; NULL per manual_hold senza entità a monte");

            migrationBuilder.AlterColumn<Guid>(
                name: "resource_id",
                schema: "booking",
                table: "resource_consumptions",
                type: "uuid",
                nullable: true,
                comment: "public_id C2 della risorsa (map_spots / service_sale_items / inventory_items secondo resource_type), copiato — nessuna FK cross-schema; NULL per articolo personalizzato non a catalogo",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id (Catalog) della risorsa (map_spots / service_sale_items / inventory_items secondo resource_type), copiato — nessuna FK cross-schema; NULL per articolo personalizzato non a catalogo");

            migrationBuilder.AlterColumn<Guid>(
                name: "resource_id",
                schema: "booking",
                table: "resource_availability_daily",
                type: "uuid",
                nullable: true,
                comment: "public_id C2 della risorsa (proiezione fine); NULL per la proiezione grossolana per tipo",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id (Catalog) della risorsa (proiezione fine); NULL per la proiezione grossolana per tipo");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "booking",
                table: "reservations",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo da C3/C4/C6.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo da Payment/Fiskaly/Notification.");

            migrationBuilder.AlterColumn<decimal>(
                name: "paid",
                schema: "booking",
                table: "reservations",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Totale già incassato, proiezione dagli eventi di pagamento di C3.",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldDefaultValue: 0m,
                oldComment: "Totale già incassato, proiezione dagli eventi di pagamento di Payment.");

            migrationBuilder.AlterColumn<decimal>(
                name: "deposit",
                schema: "booking",
                table: "reservations",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Importo acconto/caparra corrente (riepilogo); i singoli acconti sono movimenti C3.",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldDefaultValue: 0m,
                oldComment: "Importo acconto/caparra corrente (riepilogo); i singoli acconti sono movimenti Payment.");

            migrationBuilder.AlterColumn<bool>(
                name: "wallet_event_emitted",
                schema: "booking",
                table: "resale_credit_ledger",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "True quando l'evento ResaleCreditEarned è stato consegnato a Payment (C3) per la riconciliazione col borsellino",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "True quando l'evento ResaleCreditEarned è stato consegnato a Payment (Payment) per la riconciliazione col borsellino");

            migrationBuilder.AlterColumn<string>(
                name: "number_prefix",
                schema: "booking",
                table: "number_sequences",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                comment: "Prefisso stabile e immutabile del numero prenotazione per la property (di norma il suo id legacy)",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldComment: "Prefisso stabile e immutabile del numero prenotazione per la property (di norma il suo id interno)");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "booking",
                table: "lines",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile della riga, copiato cross-modulo da C3 (righe economiche) e C4 (line item scontrino).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile della riga, copiato cross-modulo da Payment (righe economiche) e Fiskaly (line item scontrino).");

            migrationBuilder.AlterColumn<string>(
                name: "booking_number",
                schema: "booking",
                table: "lines",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "Numero prenotazione copiato dalla testata, per correlazioni dirette con C3/C4 senza join.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "Numero prenotazione copiato dalla testata, per correlazioni dirette con Payment/Fiskaly senza join.");

            migrationBuilder.AlterColumn<long>(
                name: "reservation_id",
                schema: "booking",
                table: "group_members",
                type: "bigint",
                nullable: false,
                comment: "Prenotazione membro del gruppo (l'intera prenotazione, non una singola riga postazione come nel legacy)",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldComment: "Prenotazione membro del gruppo (l'intera prenotazione, non una singola riga postazione)");

            migrationBuilder.AlterColumn<int>(
                name: "scope",
                schema: "booking",
                table: "discounts",
                type: "booking.discount_scope",
                nullable: false,
                defaultValueSql: "'all'",
                comment: "Ambito di validità del voucher, congelato dalla regola di C2.",
                oldClrType: typeof(int),
                oldType: "booking.discount_scope",
                oldDefaultValueSql: "'all'",
                oldComment: "Ambito di validità del voucher, congelato dalla regola di Catalog.");

            migrationBuilder.AlterColumn<bool>(
                name: "consumed_usage",
                schema: "booking",
                table: "discounts",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "True se questa prenotazione ha consumato un utilizzo del voucher; l'incremento/decremento del contatore è atomico su C2.",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "True se questa prenotazione ha consumato un utilizzo del voucher; l'incremento/decremento del contatore è atomico su Catalog.");

            migrationBuilder.AlterColumn<int>(
                name: "applied_channel",
                schema: "booking",
                table: "discounts",
                type: "booking.discount_channel",
                nullable: false,
                defaultValueSql: "'any'",
                comment: "Canale su cui il voucher era ammesso, congelato dalla regola di C2.",
                oldClrType: typeof(int),
                oldType: "booking.discount_channel",
                oldDefaultValueSql: "'any'",
                oldComment: "Canale su cui il voucher era ammesso, congelato dalla regola di Catalog.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "booking",
                table: "agreement_lines",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico, copiato cross-modulo verso C3 che tiene importo/stato pagamento della voce.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico, copiato cross-modulo verso Payment che tiene importo/stato pagamento della voce.");
        }
    }
}
