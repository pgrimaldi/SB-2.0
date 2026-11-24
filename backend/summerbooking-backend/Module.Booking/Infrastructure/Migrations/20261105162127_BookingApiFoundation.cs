using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Module.Booking.Domain.Enums;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Booking.Infrastructure.Migrations
{
    /// <summary>
    /// Schema per le API di Booking: check-in e check-out sulla testata, motivo dell'annullamento, conferma del gestore con
    /// scadenza, sconto manuale e incremento con etichetta, rinnovo e dividi conto, autore della nota, spunta «Pagato»;
    /// sulle righe fila, colonna e tipo della postazione e prezzo del pacchetto scritto a mano; date degli articoli
    /// aggiuntivi; id pubblico delle riserve; ingressi per postazione e giorno. Tolti lo stato «non presentato», le azioni
    /// rapide del planner e is_split della testata.
    /// </summary>
    public partial class BookingApiFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "quick_action_dates",
                schema: "booking");

            migrationBuilder.DropIndex(
                name: "ix_booking_lines_checkin_open",
                schema: "booking",
                table: "lines");

            migrationBuilder.DropColumn(
                name: "is_split",
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
                .OldAnnotation("Npgsql:Enum:booking.agreement_line_type", "regular,voucher")
                .OldAnnotation("Npgsql:Enum:booking.booking_channel", "customer,manager")
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
                .OldAnnotation("Npgsql:Enum:booking.resale_credit_calc", "fixed_amount,daily_percentage")
                .OldAnnotation("Npgsql:Enum:booking.resale_credit_entry", "earned,reversed,adjustment")
                .OldAnnotation("Npgsql:Enum:booking.resale_mode", "absence,sharing")
                .OldAnnotation("Npgsql:Enum:booking.resale_offer_state", "available,reassigned,withdrawn")
                .OldAnnotation("Npgsql:Enum:booking.resale_origin", "absence,sharing,absence_as_sharing")
                .OldAnnotation("Npgsql:Enum:booking.resale_scope", "seasonal_workers,all_articles")
                .OldAnnotation("Npgsql:Enum:booking.reservation_kind", "spot,service")
                .OldAnnotation("Npgsql:Enum:booking.reservation_price_flag", "suggested_price_accepted,recalculated,manually_edited")
                .OldAnnotation("Npgsql:Enum:booking.reservation_status", "pending,confirmed,cancelled")
                .OldAnnotation("Npgsql:Enum:booking.resource_type", "umbrella,sunbed,sunbed_xl,cabin,special_cabin,deckchair,chair,tent,parking,ticket,area_seat,bar_service,normal_service,hourly_service,inventory_item")
                .OldAnnotation("Npgsql:Enum:booking.service_category", "bar,normal,hourly,additional");

            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                schema: "booking",
                table: "spot_holds",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "Id pubblico della riserva nelle API; le righe migrate lo ricevono dal default.");

            migrationBuilder.AlterColumn<OnlineConfirmationState>(
                name: "online_confirmation_state",
                schema: "booking",
                table: "reservations",
                type: "booking.online_confirmation_state",
                nullable: false,
                defaultValueSql: "'not_required'",
                comment: "Conferma del gestore delle prenotazioni online che la richiedono: not_required, pending, confirmed.",
                oldClrType: typeof(int),
                oldType: "booking.online_confirmation_state",
                oldDefaultValueSql: "'not_required'",
                oldComment: "Stato del flusso di conferma online via reservation_code.");

            migrationBuilder.AlterColumn<Guid>(
                name: "agreement_partner_id",
                schema: "booking",
                table: "reservations",
                type: "uuid",
                nullable: true,
                comment: "public_id di catalog.agreement_partners, copiato — riepilogo: struttura della prima riga con una convenzione; il dettaglio per riga è in booking.agreement_lines.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di catalog.agreement_partners, copiato — valorizzato solo per reservation_kind = spot.");

            migrationBuilder.AddColumn<CancellationReason>(
                name: "cancellation_reason",
                schema: "booking",
                table: "reservations",
                type: "booking.cancellation_reason",
                nullable: true,
                comment: "Motivo dell'annullamento: customer, manager, rejected, property_trashed, spot_removed, payment_failed, confirmation_expired.");

            migrationBuilder.AddColumn<DateTime>(
                name: "cancelled_at",
                schema: "booking",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Quando la prenotazione è stata annullata.");

            migrationBuilder.AddColumn<DateTime>(
                name: "checked_in_at",
                schema: "booking",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Check-in dell'intera prenotazione; NULL = cliente non arrivato.");

            migrationBuilder.AddColumn<DateTime>(
                name: "checked_out_at",
                schema: "booking",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Check-out dell'intera prenotazione, anche anticipato.");

            migrationBuilder.AddColumn<DateTime>(
                name: "checkout_effective_at",
                schema: "booking",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Fine effettiva dell'occupazione con un check-out anticipato: i giorni da lì in poi tornano liberi.");

            migrationBuilder.AddColumn<DateTime>(
                name: "confirmation_expires_at",
                schema: "booking",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Scadenza della richiesta online da confermare: la prima fra 48 ore, le 23:00 della vigilia e, per il giorno stesso, 1 ora.");

            migrationBuilder.AddColumn<DateTime>(
                name: "confirmed_at",
                schema: "booking",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Quando il gestore ha confermato la richiesta online.");

            migrationBuilder.AddColumn<Guid>(
                name: "confirmed_by_user_id",
                schema: "booking",
                table: "reservations",
                type: "uuid",
                nullable: true,
                comment: "public_id di identity.access_users, copiato — chi ha confermato la richiesta online.");

            migrationBuilder.AddColumn<decimal>(
                name: "manual_discount_amount",
                schema: "booking",
                table: "reservations",
                type: "numeric(12,2)",
                nullable: false,
                defaultValue: 0m,
                comment: "Importo in euro dello sconto manuale, compreso in discount.");

            migrationBuilder.AddColumn<AdjustmentType>(
                name: "manual_discount_type",
                schema: "booking",
                table: "reservations",
                type: "booking.adjustment_type",
                nullable: true,
                comment: "Sconto manuale dell'operatore: percentage o amount; NULL = nessuno. Si somma al codice sconto, entrambi sul subtotale.");

            migrationBuilder.AddColumn<decimal>(
                name: "manual_discount_value",
                schema: "booking",
                table: "reservations",
                type: "numeric(12,2)",
                nullable: true,
                comment: "Valore dello sconto manuale: percentuale (0-100) o euro.");

            migrationBuilder.AddColumn<DateTime>(
                name: "note_updated_at",
                schema: "booking",
                table: "reservations",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Ultima modifica del testo della nota.");

            migrationBuilder.AddColumn<Guid>(
                name: "note_updated_by_user_id",
                schema: "booking",
                table: "reservations",
                type: "uuid",
                nullable: true,
                comment: "public_id di identity.access_users, copiato — chi ha scritto per ultimo la nota.");

            migrationBuilder.AddColumn<Guid>(
                name: "paid_mark_entry_id",
                schema: "booking",
                table: "reservations",
                type: "uuid",
                nullable: true,
                comment: "public_id di payment.ledger_entries, copiato — l'incasso creato con la spunta «Pagato», annullato togliendola.");

            migrationBuilder.AddColumn<long>(
                name: "renewed_from_reservation_id",
                schema: "booking",
                table: "reservations",
                type: "bigint",
                nullable: true,
                comment: "Prenotazione da cui nasce il rinnovo; impedisce di rinnovarla due volte.");

            migrationBuilder.AddColumn<long>(
                name: "split_from_reservation_id",
                schema: "booking",
                table: "reservations",
                type: "bigint",
                nullable: true,
                comment: "Prenotazione da cui nasce con «Dividi conto».");

            migrationBuilder.AddColumn<string>(
                name: "surcharge_label",
                schema: "booking",
                table: "reservations",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "Etichetta libera dell'unico incremento, riga a sé sullo scontrino.");

            migrationBuilder.AddColumn<AdjustmentType>(
                name: "surcharge_type",
                schema: "booking",
                table: "reservations",
                type: "booking.adjustment_type",
                nullable: true,
                comment: "Incremento: percentage o amount; NULL = nessuno.");

            migrationBuilder.AddColumn<decimal>(
                name: "surcharge_value",
                schema: "booking",
                table: "reservations",
                type: "numeric(12,2)",
                nullable: true,
                comment: "Valore dell'incremento: percentuale (0-100) o euro.");

            migrationBuilder.AddColumn<decimal>(
                name: "manual_price",
                schema: "booking",
                table: "lines",
                type: "numeric(12,2)",
                nullable: true,
                comment: "Prezzo del pacchetto scritto a mano; si perde cambiando le date della riga.");

            migrationBuilder.AddColumn<short>(
                name: "spot_column_snapshot",
                schema: "booking",
                table: "lines",
                type: "smallint",
                nullable: true,
                comment: "Colonna della postazione nel reticolo della mappa, per il ricollegamento per posizione.");

            migrationBuilder.AddColumn<short>(
                name: "spot_row_snapshot",
                schema: "booking",
                table: "lines",
                type: "smallint",
                nullable: true,
                comment: "Fila della postazione nel reticolo della mappa, per il ricollegamento per posizione.");

            migrationBuilder.AddColumn<string>(
                name: "spot_type_snapshot",
                schema: "booking",
                table: "lines",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true,
                comment: "Tipo della postazione al momento della prenotazione (codice di catalog.map_spots): il ricollegamento dopo «Reimposta mappa» vale solo con lo stesso tipo.");

            migrationBuilder.AddColumn<List<DateOnly>>(
                name: "dates",
                schema: "booking",
                table: "line_additional_items",
                type: "date[]",
                nullable: false,
                defaultValueSql: "'{}'",
                comment: "Giorni a cui vale l'articolo quando date_scope = specific_dates; vuoto = tutto il soggiorno della riga.");

            // Check-in e check-out valgono per l'intera prenotazione: si portano sulla testata (il più vecchio check-in,
            // l'ultimo check-out delle righe) prima di togliere le colonne dalle righe.
            migrationBuilder.Sql(@"
UPDATE booking.reservations r
SET checked_in_at = l.checked_in_at, checked_out_at = l.checked_out_at, checkout_effective_at = l.checkout_effective_at
FROM (SELECT reservation_id, min(checked_in_at) AS checked_in_at, max(checked_out_at) AS checked_out_at,
             max(checkout_effective_at) AS checkout_effective_at
      FROM booking.lines
      WHERE checked_in_at IS NOT NULL OR checked_out_at IS NOT NULL
      GROUP BY reservation_id) l
WHERE l.reservation_id = r.id;");

            migrationBuilder.DropColumn(
                name: "checked_in_at",
                schema: "booking",
                table: "lines");

            migrationBuilder.DropColumn(
                name: "checked_out_at",
                schema: "booking",
                table: "lines");

            migrationBuilder.DropColumn(
                name: "checkout_effective_at",
                schema: "booking",
                table: "lines");

            // «Non presentato» non esiste più: PostgreSQL non toglie un'etichetta da un enum, il tipo si ricrea.
            migrationBuilder.Sql(@"
UPDATE booking.reservations SET reservation_status = 'cancelled' WHERE reservation_status = 'no_show';
ALTER TYPE booking.reservation_status RENAME TO reservation_status_old;
CREATE TYPE booking.reservation_status AS ENUM ('pending', 'confirmed', 'cancelled');
ALTER TABLE booking.reservations
    ALTER COLUMN reservation_status TYPE booking.reservation_status USING reservation_status::text::booking.reservation_status;
DROP TYPE booking.reservation_status_old;");

            migrationBuilder.CreateTable(
                name: "entrances",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; discriminatore tenant."),
                    booking_line_id = table.Column<long>(type: "bigint", nullable: false, comment: "Riga (postazione) della prenotazione su cui è segnato l'ingresso."),
                    entry_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Giorno dell'ingresso (ora italiana): il giorno dopo l'ingresso non risulta più segnato."),
                    entered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Quando l'operatore ha segnato l'ingresso."),
                    operator_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato dal token — chi ha segnato l'ingresso."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_entrances", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_entrances_line",
                        column: x => x.booking_line_id,
                        principalSchema: "booking",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "«Segnala ingresso»: il cliente è entrato su questa postazione in questo giorno. Una riga per riga di prenotazione e giorno.");

            migrationBuilder.CreateIndex(
                name: "uq_booking_spot_holds_public_id",
                schema: "booking",
                table: "spot_holds",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_reservations_split_from",
                schema: "booking",
                table: "reservations",
                column: "split_from_reservation_id",
                filter: "split_from_reservation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_reservations_trash",
                schema: "booking",
                table: "reservations",
                column: "deleted_at",
                filter: "deleted_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_reservations_renewed_from",
                schema: "booking",
                table: "reservations",
                column: "renewed_from_reservation_id",
                unique: true,
                filter: "renewed_from_reservation_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_entrances_property_date",
                schema: "booking",
                table: "entrances",
                columns: new[] { "property_id", "entry_date" });

            migrationBuilder.CreateIndex(
                name: "uq_booking_entrances_line_date",
                schema: "booking",
                table: "entrances",
                columns: new[] { "booking_line_id", "entry_date" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_reservations_renewed_from",
                schema: "booking",
                table: "reservations",
                column: "renewed_from_reservation_id",
                principalSchema: "booking",
                principalTable: "reservations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_booking_reservations_split_from",
                schema: "booking",
                table: "reservations",
                column: "split_from_reservation_id",
                principalSchema: "booking",
                principalTable: "reservations",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_booking_reservations_renewed_from",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropForeignKey(
                name: "fk_booking_reservations_split_from",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropTable(
                name: "entrances",
                schema: "booking");

            migrationBuilder.DropIndex(
                name: "uq_booking_spot_holds_public_id",
                schema: "booking",
                table: "spot_holds");

            migrationBuilder.DropIndex(
                name: "ix_booking_reservations_split_from",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropIndex(
                name: "ix_booking_reservations_trash",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropIndex(
                name: "uq_booking_reservations_renewed_from",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "public_id",
                schema: "booking",
                table: "spot_holds");

            migrationBuilder.DropColumn(
                name: "cancellation_reason",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "cancelled_at",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "checked_in_at",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "checked_out_at",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "checkout_effective_at",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "confirmation_expires_at",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "confirmed_at",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "confirmed_by_user_id",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "manual_discount_amount",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "manual_discount_type",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "manual_discount_value",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "note_updated_at",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "note_updated_by_user_id",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "paid_mark_entry_id",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "renewed_from_reservation_id",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "split_from_reservation_id",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "surcharge_label",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "surcharge_type",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "surcharge_value",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "manual_price",
                schema: "booking",
                table: "lines");

            migrationBuilder.DropColumn(
                name: "spot_column_snapshot",
                schema: "booking",
                table: "lines");

            migrationBuilder.DropColumn(
                name: "spot_row_snapshot",
                schema: "booking",
                table: "lines");

            migrationBuilder.DropColumn(
                name: "spot_type_snapshot",
                schema: "booking",
                table: "lines");

            migrationBuilder.DropColumn(
                name: "dates",
                schema: "booking",
                table: "line_additional_items");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:booking.additional_item_date_scope", "whole_stay,specific_dates")
                .Annotation("Npgsql:Enum:booking.additional_item_source", "price_list,inventory")
                .Annotation("Npgsql:Enum:booking.agreement_line_type", "regular,voucher")
                .Annotation("Npgsql:Enum:booking.booking_channel", "customer,manager")
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
                .Annotation("Npgsql:Enum:booking.reservation_status", "pending,confirmed,cancelled,no_show")
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
                .OldAnnotation("Npgsql:Enum:booking.resale_credit_calc", "fixed_amount,daily_percentage")
                .OldAnnotation("Npgsql:Enum:booking.resale_credit_entry", "earned,reversed,adjustment")
                .OldAnnotation("Npgsql:Enum:booking.resale_mode", "absence,sharing")
                .OldAnnotation("Npgsql:Enum:booking.resale_offer_state", "available,reassigned,withdrawn")
                .OldAnnotation("Npgsql:Enum:booking.resale_origin", "absence,sharing,absence_as_sharing")
                .OldAnnotation("Npgsql:Enum:booking.resale_scope", "seasonal_workers,all_articles")
                .OldAnnotation("Npgsql:Enum:booking.reservation_kind", "spot,service")
                .OldAnnotation("Npgsql:Enum:booking.reservation_price_flag", "suggested_price_accepted,recalculated,manually_edited")
                .OldAnnotation("Npgsql:Enum:booking.reservation_status", "pending,confirmed,cancelled")
                .OldAnnotation("Npgsql:Enum:booking.resource_type", "umbrella,sunbed,sunbed_xl,cabin,special_cabin,deckchair,chair,tent,parking,ticket,area_seat,bar_service,normal_service,hourly_service,inventory_item")
                .OldAnnotation("Npgsql:Enum:booking.service_category", "bar,normal,hourly,additional");

            migrationBuilder.AlterColumn<int>(
                name: "online_confirmation_state",
                schema: "booking",
                table: "reservations",
                type: "booking.online_confirmation_state",
                nullable: false,
                defaultValueSql: "'not_required'",
                comment: "Stato del flusso di conferma online via reservation_code.",
                oldClrType: typeof(OnlineConfirmationState),
                oldType: "booking.online_confirmation_state",
                oldDefaultValueSql: "'not_required'",
                oldComment: "Conferma del gestore delle prenotazioni online che la richiedono: not_required, pending, confirmed.");

            migrationBuilder.AlterColumn<Guid>(
                name: "agreement_partner_id",
                schema: "booking",
                table: "reservations",
                type: "uuid",
                nullable: true,
                comment: "public_id di catalog.agreement_partners, copiato — valorizzato solo per reservation_kind = spot.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di catalog.agreement_partners, copiato — riepilogo: struttura della prima riga con una convenzione; il dettaglio per riga è in booking.agreement_lines.");

            migrationBuilder.AddColumn<bool>(
                name: "is_split",
                schema: "booking",
                table: "reservations",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Prenotazione split-location (derivabile dalle righe, tenuto per compatibilità di lettura).");

            migrationBuilder.AddColumn<DateTime>(
                name: "checked_in_at",
                schema: "booking",
                table: "lines",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Quando il cliente si è presentato per questa postazione; NULL = non arrivato.");

            migrationBuilder.AddColumn<DateTime>(
                name: "checked_out_at",
                schema: "booking",
                table: "lines",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Quando la postazione è stata liberata / cliente uscito.");

            migrationBuilder.AddColumn<DateTime>(
                name: "checkout_effective_at",
                schema: "booking",
                table: "lines",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Fine occupazione effettiva in caso di check-out anticipato; limite superiore per la disponibilità al posto di end_date.");

            migrationBuilder.CreateTable(
                name: "quick_action_dates",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione a cui la data marcata è associata."),
                    action_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Giorno marcato per un'azione rapida del planner."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_quick_action_dates", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_quick_action_dates_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Giorno marcato dall'operatore per un'azione rapida del planner su questa prenotazione.");

            migrationBuilder.CreateIndex(
                name: "ix_booking_lines_checkin_open",
                schema: "booking",
                table: "lines",
                columns: new[] { "property_id", "start_date" },
                filter: "checked_in_at IS NOT NULL AND checked_out_at IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_quick_action_dates_property_date",
                schema: "booking",
                table: "quick_action_dates",
                columns: new[] { "property_id", "action_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_quick_action_dates_reservation_date",
                schema: "booking",
                table: "quick_action_dates",
                columns: new[] { "reservation_id", "action_date" },
                unique: true,
                filter: "deleted_at IS NULL");
        }
    }
}
