using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class BookingGroupsPublicId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "public_id",
                schema: "booking",
                table: "groups",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                comment: "Id pubblico del gruppo nelle API; le righe migrate lo ricevono dal default.");

            migrationBuilder.CreateIndex(
                name: "uq_booking_groups_public_id",
                schema: "booking",
                table: "groups",
                column: "public_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "uq_booking_groups_public_id",
                schema: "booking",
                table: "groups");

            migrationBuilder.DropColumn(
                name: "public_id",
                schema: "booking",
                table: "groups");
        }
    }
}
