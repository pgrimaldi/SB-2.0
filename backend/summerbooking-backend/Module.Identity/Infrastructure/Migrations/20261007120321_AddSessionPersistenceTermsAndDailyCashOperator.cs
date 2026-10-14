using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionPersistenceTermsAndDailyCashOperator : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "terms_accepted_at",
                schema: "identity",
                table: "access_users",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Accettazione di privacy e termini alla registrazione pubblica; NULL per lo storico e per lo staff.");

            migrationBuilder.AddColumn<string>(
                name: "terms_version",
                schema: "identity",
                table: "access_users",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true,
                comment: "Versione del testo di privacy e termini accettato.");

            migrationBuilder.AddColumn<bool>(
                name: "is_persistent",
                schema: "identity",
                table: "access_refresh_tokens",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Ricordami: cookie persistente invece che di sessione; ereditato a ogni rotazione.");

            migrationBuilder.InsertData(
                schema: "identity",
                table: "access_roles",
                columns: new[] { "id", "code", "created_at", "deleted_at", "is_assignable_via_panel", "is_staff", "sort_order", "updated_at" },
                values: new object[] { 14L, "dailycashoperator", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, (short)55, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "identity",
                table: "access_roles",
                keyColumn: "id",
                keyValue: 14L);

            migrationBuilder.DropColumn(
                name: "terms_accepted_at",
                schema: "identity",
                table: "access_users");

            migrationBuilder.DropColumn(
                name: "terms_version",
                schema: "identity",
                table: "access_users");

            migrationBuilder.DropColumn(
                name: "is_persistent",
                schema: "identity",
                table: "access_refresh_tokens");
        }
    }
}
