using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class OfflineSync : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cursore di sincronizzazione dei dispositivi: una sequenza per tutto il modulo.
            migrationBuilder.Sql("CREATE SEQUENCE IF NOT EXISTS booking.sync_seq;");

            migrationBuilder.AddColumn<long>(
                name: "sync_seq",
                schema: "booking",
                table: "spot_holds",
                type: "bigint",
                nullable: false,
                defaultValueSql: "nextval('booking.sync_seq')",
                comment: "Cursore di sincronizzazione dei dispositivi: nuovo numero a ogni modifica (trigger).");

            migrationBuilder.AddColumn<long>(
                name: "sync_seq",
                schema: "booking",
                table: "reservations",
                type: "bigint",
                nullable: false,
                defaultValueSql: "nextval('booking.sync_seq')",
                comment: "Cursore di sincronizzazione dei dispositivi: nuovo numero a ogni modifica (trigger).");

            migrationBuilder.CreateTable(
                name: "processed_commands",
                schema: "booking",
                columns: table => new
                {
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema"),
                    idempotency_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Chiave mandata dal client nell'header Idempotency-Key."),
                    method = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, comment: "Metodo HTTP del comando."),
                    path = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false, comment: "Percorso del comando."),
                    status_code = table.Column<int>(type: "integer", nullable: false, comment: "Stato HTTP della risposta."),
                    response_body = table.Column<string>(type: "jsonb", nullable: true, comment: "Corpo della risposta."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Quando è stato eseguito.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_processed_commands", x => new { x.property_id, x.idempotency_key });
                },
                comment: "Comandi già eseguiti con la chiave di idempotenza: chi li rimanda (offline) riceve la stessa risposta. Cancellati dopo 7 giorni.");

            migrationBuilder.CreateIndex(
                name: "ix_booking_spot_holds_sync",
                schema: "booking",
                table: "spot_holds",
                columns: new[] { "property_id", "sync_seq" });

            migrationBuilder.CreateIndex(
                name: "ix_booking_reservations_sync",
                schema: "booking",
                table: "reservations",
                columns: new[] { "property_id", "sync_seq" });

            migrationBuilder.CreateIndex(
                name: "ix_booking_processed_commands_created",
                schema: "booking",
                table: "processed_commands",
                column: "created_at");

            // A ogni modifica la testata e la riserva prendono un numero nuovo del cursore (le righe toccano la testata).
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION booking.next_sync_seq() RETURNS trigger LANGUAGE plpgsql AS $$
                BEGIN
                    NEW.sync_seq := nextval('booking.sync_seq');
                    RETURN NEW;
                END $$;
                CREATE TRIGGER trg_booking_reservations_sync BEFORE UPDATE ON booking.reservations
                    FOR EACH ROW EXECUTE FUNCTION booking.next_sync_seq();
                CREATE TRIGGER trg_booking_spot_holds_sync BEFORE UPDATE ON booking.spot_holds
                    FOR EACH ROW EXECUTE FUNCTION booking.next_sync_seq();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP TRIGGER IF EXISTS trg_booking_reservations_sync ON booking.reservations;
                DROP TRIGGER IF EXISTS trg_booking_spot_holds_sync ON booking.spot_holds;
                DROP FUNCTION IF EXISTS booking.next_sync_seq();
                """);

            migrationBuilder.DropTable(
                name: "processed_commands",
                schema: "booking");

            migrationBuilder.DropIndex(
                name: "ix_booking_spot_holds_sync",
                schema: "booking",
                table: "spot_holds");

            migrationBuilder.DropIndex(
                name: "ix_booking_reservations_sync",
                schema: "booking",
                table: "reservations");

            migrationBuilder.DropColumn(
                name: "sync_seq",
                schema: "booking",
                table: "spot_holds");

            migrationBuilder.DropColumn(
                name: "sync_seq",
                schema: "booking",
                table: "reservations");

            migrationBuilder.Sql("DROP SEQUENCE IF EXISTS booking.sync_seq;");
        }
    }
}
