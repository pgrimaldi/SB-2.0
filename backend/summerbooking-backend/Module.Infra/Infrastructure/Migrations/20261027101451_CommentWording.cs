using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Infra.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommentWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                schema: "infra",
                table: "integration_api_clients",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Scadenza effettiva del client, quando impostata; la verifica il servizio di autenticazione.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Scadenza effettiva del client, quando impostata; a differenza del legacy va verificata dal servizio di autenticazione.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "expires_at",
                schema: "infra",
                table: "integration_api_clients",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Scadenza effettiva del client, quando impostata; a differenza del legacy va verificata dal servizio di autenticazione.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Scadenza effettiva del client, quando impostata; la verifica il servizio di autenticazione.");
        }
    }
}
