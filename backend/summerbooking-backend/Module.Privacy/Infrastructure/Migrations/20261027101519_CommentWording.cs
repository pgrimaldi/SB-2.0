using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Privacy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommentWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "subject_kind",
                schema: "privacy",
                table: "privacy_data_subject_requests",
                type: "privacy.subject_kind",
                nullable: false,
                comment: "Tipo di entità interessata: customer (rubrica di Identity) o user (account di login di Identity).",
                oldClrType: typeof(int),
                oldType: "privacy.subject_kind",
                oldComment: "Tipo di entità interessata: customer (rubrica C5) o user (account di login C5).");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "subject_kind",
                schema: "privacy",
                table: "privacy_data_subject_requests",
                type: "privacy.subject_kind",
                nullable: false,
                comment: "Tipo di entità interessata: customer (rubrica C5) o user (account di login C5).",
                oldClrType: typeof(int),
                oldType: "privacy.subject_kind",
                oldComment: "Tipo di entità interessata: customer (rubrica di Identity) o user (account di login di Identity).");
        }
    }
}
