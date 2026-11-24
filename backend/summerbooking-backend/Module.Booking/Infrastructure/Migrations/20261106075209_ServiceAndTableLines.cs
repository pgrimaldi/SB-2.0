using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ServiceAndTableLines : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "table_lines",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Id pubblico della riga nelle API."),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione a cui appartiene il tavolo."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema"),
                    table_service_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.table_services (pranzo, cena), copiato — nessuna FK cross-schema"),
                    table_service_name_snapshot = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome del servizio tavoli al momento della prenotazione."),
                    service_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Giorno del pasto."),
                    slot_start = table.Column<TimeOnly>(type: "time", nullable: false, comment: "Inizio della fascia."),
                    slot_end = table.Column<TimeOnly>(type: "time", nullable: false, comment: "Fine della fascia."),
                    covers = table.Column<short>(type: "smallint", nullable: false, comment: "Coperti."),
                    table_seats = table.Column<short[]>(type: "smallint[]", nullable: false, defaultValueSql: "'{}'", comment: "Posti di ciascun tavolo assegnato; vuoto con la capienza a posti o senza tavolo."),
                    deposit_due = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Caparra richiesta dal servizio tavoli (si incassa con Payment)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_table_lines", x => x.id);
                    table.CheckConstraint("ck_booking_table_lines_covers", "covers > 0");
                    table.CheckConstraint("ck_booking_table_lines_slot", "slot_end > slot_start");
                    table.ForeignKey(
                        name: "fk_booking_table_lines_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tavolo prenotato: servizio tavoli, giorno, fascia, coperti e tavoli assegnati.");

            migrationBuilder.CreateIndex(
                name: "ix_booking_table_lines_service_day",
                schema: "booking",
                table: "table_lines",
                columns: new[] { "property_id", "table_service_id", "service_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_table_lines_reservation_id",
                schema: "booking",
                table: "table_lines",
                column: "reservation_id");

            migrationBuilder.CreateIndex(
                name: "uq_booking_table_lines_public_id",
                schema: "booking",
                table: "table_lines",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "table_lines",
                schema: "booking");
        }
    }
}
