using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Audit.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditSourceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit.actor_kind", "user,system,external_client,anonymous")
                .Annotation("Npgsql:Enum:audit.entry_category", "operator,security,platform")
                .Annotation("Npgsql:Enum:audit.entry_outcome", "succeeded,failed")
                .Annotation("Npgsql:Enum:audit.source_module", "identity,catalog,booking,payment,fiskaly,fiscalprint,notification,switchboard,chatbot,privacy,infra,audit")
                .OldAnnotation("Npgsql:Enum:audit.actor_kind", "user,system,external_client,anonymous")
                .OldAnnotation("Npgsql:Enum:audit.entry_category", "operator,security,platform")
                .OldAnnotation("Npgsql:Enum:audit.entry_outcome", "succeeded,failed")
                .OldAnnotation("Npgsql:Enum:audit.source_module", "identity,catalog,booking,payment,fiskaly,fiscalprint,notification,switchboard,chatbot,privacy,infra");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:audit.actor_kind", "user,system,external_client,anonymous")
                .Annotation("Npgsql:Enum:audit.entry_category", "operator,security,platform")
                .Annotation("Npgsql:Enum:audit.entry_outcome", "succeeded,failed")
                .Annotation("Npgsql:Enum:audit.source_module", "identity,catalog,booking,payment,fiskaly,fiscalprint,notification,switchboard,chatbot,privacy,infra")
                .OldAnnotation("Npgsql:Enum:audit.actor_kind", "user,system,external_client,anonymous")
                .OldAnnotation("Npgsql:Enum:audit.entry_category", "operator,security,platform")
                .OldAnnotation("Npgsql:Enum:audit.entry_outcome", "succeeded,failed")
                .OldAnnotation("Npgsql:Enum:audit.source_module", "identity,catalog,booking,payment,fiskaly,fiscalprint,notification,switchboard,chatbot,privacy,infra,audit");
        }
    }
}
