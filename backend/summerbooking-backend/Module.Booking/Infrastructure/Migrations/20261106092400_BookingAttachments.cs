using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "media_assets",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()", comment: "Id pubblico dell'allegato nelle API."),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione a cui è allegato."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema"),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome del file; resta anche dopo la cancellazione del file."),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Tipo del file (PDF, JPEG, PNG)."),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false, comment: "Dimensione in byte (al massimo 2 MB)."),
                    storage_origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Archivio di file in cui sta."),
                    storage_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Percorso relativo nell'archivio; NULL dopo la cancellazione del file."),
                    sha256 = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Impronta del contenuto."),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users (chi l'ha caricato), copiato — nessuna FK cross-schema"),
                    file_deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando il file è stato cancellato dall'archivio."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Caricamento."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: allegato tolto dal gestore.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_media_assets", x => x.id);
                    table.CheckConstraint("ck_booking_media_assets_size", "size_bytes > 0");
                    table.ForeignKey(
                        name: "fk_booking_media_assets_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "File allegati alle prenotazioni (PDF, JPEG, PNG): al 31 dicembre si cancella il file, resta il nome.");

            migrationBuilder.CreateIndex(
                name: "ix_booking_media_assets_file_alive",
                schema: "booking",
                table: "media_assets",
                column: "created_at",
                filter: "storage_path IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_media_assets_reservation",
                schema: "booking",
                table: "media_assets",
                column: "reservation_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_media_assets_public_id",
                schema: "booking",
                table: "media_assets",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "media_assets",
                schema: "booking");
        }
    }
}
