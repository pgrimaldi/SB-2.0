using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OnlineBookingOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "property_online_stay_limits",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                comment: "Id pubblico (UUIDv7) usato dalle API.");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "afternoon_conversion_time",
                schema: "catalog",
                table: "property_online_booking_settings",
                type: "time",
                nullable: true,
                comment: "Le prenotazioni del giorno fatte dopo questo orario diventano pomeridiane; NULL = nessuna conversione.");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "afternoon_from",
                schema: "catalog",
                table: "property_online_booking_settings",
                type: "time",
                nullable: true,
                comment: "Mezza giornata: inizio del pomeriggio.");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "afternoon_to",
                schema: "catalog",
                table: "property_online_booking_settings",
                type: "time",
                nullable: true,
                comment: "Mezza giornata: fine del pomeriggio.");

            migrationBuilder.AddColumn<short>(
                name: "booking_horizon_days",
                schema: "catalog",
                table: "property_online_booking_settings",
                type: "smallint",
                nullable: true,
                comment: "Blocca le prenotazioni online oltre questo numero di giorni dal giorno corrente; NULL = nessun blocco.");

            migrationBuilder.AddColumn<short>(
                name: "max_bookings_per_day",
                schema: "catalog",
                table: "property_online_booking_settings",
                type: "smallint",
                nullable: true,
                comment: "Numero massimo di prenotazioni online al giorno; NULL = nessun limite.");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "morning_from",
                schema: "catalog",
                table: "property_online_booking_settings",
                type: "time",
                nullable: true,
                comment: "Mezza giornata: inizio della mattina.");

            migrationBuilder.AddColumn<TimeOnly>(
                name: "morning_to",
                schema: "catalog",
                table: "property_online_booking_settings",
                type: "time",
                nullable: true,
                comment: "Mezza giornata: fine della mattina.");

            // UUIDv7 dal momento di creazione (PostgreSQL 17 non ha uuidv7()).
            migrationBuilder.Sql("""
                UPDATE catalog.property_online_stay_limits SET public_id = encode(set_bit(set_bit(overlay(uuid_send(gen_random_uuid())
                    placing substring(int8send((extract(epoch from created_at) * 1000)::bigint) from 3) from 1 for 6),
                    52, 1), 53, 1), 'hex')::uuid;
                """);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_property_online_stay_limits_public_id",
                schema: "catalog",
                table: "property_online_stay_limits",
                column: "public_id",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_property_online_stay_limits_bound",
                schema: "catalog",
                table: "property_online_stay_limits",
                sql: "min_days IS NOT NULL OR max_days IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_property_online_booking_settings_afternoon",
                schema: "catalog",
                table: "property_online_booking_settings",
                sql: "afternoon_from IS NULL OR afternoon_to IS NULL OR afternoon_to > afternoon_from");

            migrationBuilder.AddCheckConstraint(
                name: "ck_catalog_property_online_booking_settings_morning",
                schema: "catalog",
                table: "property_online_booking_settings",
                sql: "morning_from IS NULL OR morning_to IS NULL OR morning_to > morning_from");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_catalog_property_online_stay_limits_public_id",
                schema: "catalog",
                table: "property_online_stay_limits");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_property_online_stay_limits_bound",
                schema: "catalog",
                table: "property_online_stay_limits");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_property_online_booking_settings_afternoon",
                schema: "catalog",
                table: "property_online_booking_settings");

            migrationBuilder.DropCheckConstraint(
                name: "ck_catalog_property_online_booking_settings_morning",
                schema: "catalog",
                table: "property_online_booking_settings");

            migrationBuilder.DropColumn(
                name: "public_id",
                schema: "catalog",
                table: "property_online_stay_limits");

            migrationBuilder.DropColumn(
                name: "afternoon_conversion_time",
                schema: "catalog",
                table: "property_online_booking_settings");

            migrationBuilder.DropColumn(
                name: "afternoon_from",
                schema: "catalog",
                table: "property_online_booking_settings");

            migrationBuilder.DropColumn(
                name: "afternoon_to",
                schema: "catalog",
                table: "property_online_booking_settings");

            migrationBuilder.DropColumn(
                name: "booking_horizon_days",
                schema: "catalog",
                table: "property_online_booking_settings");

            migrationBuilder.DropColumn(
                name: "max_bookings_per_day",
                schema: "catalog",
                table: "property_online_booking_settings");

            migrationBuilder.DropColumn(
                name: "morning_from",
                schema: "catalog",
                table: "property_online_booking_settings");

            migrationBuilder.DropColumn(
                name: "morning_to",
                schema: "catalog",
                table: "property_online_booking_settings");
        }
    }
}
