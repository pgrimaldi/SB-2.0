using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Chatbot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommentWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "chatbot",
                table: "conversation_turns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                comment: "Istante del messaggio; colonna di riferimento per il conteggio giornaliero del rate limit (non updated_at).",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()",
                oldComment: "Istante del messaggio; colonna di riferimento per il conteggio giornaliero del rate limit (non updated_at, a differenza del legacy).");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "created_at",
                schema: "chatbot",
                table: "conversation_turns",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "now()",
                comment: "Istante del messaggio; colonna di riferimento per il conteggio giornaliero del rate limit (non updated_at, a differenza del legacy).",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldDefaultValueSql: "now()",
                oldComment: "Istante del messaggio; colonna di riferimento per il conteggio giornaliero del rate limit (non updated_at).");
        }
    }
}
