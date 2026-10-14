using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDocumentReferencesFromComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string[]>(
                name: "resource_scopes",
                schema: "identity",
                table: "access_users",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]",
                comment: "Codici delle postazioni su cui l'account puo' operare quando la restrizione e' attiva. Era la tabella figlia access_user_resource_scopes, collassata in array.",
                oldClrType: typeof(string[]),
                oldType: "text[]",
                oldDefaultValueSql: "'{}'::text[]",
                oldComment: "Codici delle postazioni su cui l'account puo' operare quando la restrizione e' attiva. Era la tabella figlia access_user_resource_scopes, collassata in array (docs/05 §13.1).");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string[]>(
                name: "resource_scopes",
                schema: "identity",
                table: "access_users",
                type: "text[]",
                nullable: false,
                defaultValueSql: "'{}'::text[]",
                comment: "Codici delle postazioni su cui l'account puo' operare quando la restrizione e' attiva. Era la tabella figlia access_user_resource_scopes, collassata in array (docs/05 §13.1).",
                oldClrType: typeof(string[]),
                oldType: "text[]",
                oldDefaultValueSql: "'{}'::text[]",
                oldComment: "Codici delle postazioni su cui l'account puo' operare quando la restrizione e' attiva. Era la tabella figlia access_user_resource_scopes, collassata in array.");
        }
    }
}
