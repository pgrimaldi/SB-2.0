using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Chatbot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDocumentReferencesFromComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "input_text",
                schema: "chatbot",
                table: "conversation_turns",
                type: "text",
                nullable: false,
                comment: "Messaggio inviato dall'utente — testo libero, può contenere dati personali di terzi.",
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Messaggio inviato dall'utente — testo libero, può contenere dati personali di terzi (vedi D-54).");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "input_text",
                schema: "chatbot",
                table: "conversation_turns",
                type: "text",
                nullable: false,
                comment: "Messaggio inviato dall'utente — testo libero, può contenere dati personali di terzi (vedi D-54).",
                oldClrType: typeof(string),
                oldType: "text",
                oldComment: "Messaggio inviato dall'utente — testo libero, può contenere dati personali di terzi.");
        }
    }
}
