using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingDesignedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_notification_templates_lookup",
                schema: "notification",
                table: "templates",
                columns: new[] { "property_id", "template_kind", "channel", "culture" },
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_events_property_active",
                schema: "notification",
                table: "marketing_events",
                columns: new[] { "property_id", "starts_on" },
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_contact_lists_property",
                schema: "notification",
                table: "marketing_contact_lists",
                column: "property_id",
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_notification_templates_lookup",
                schema: "notification",
                table: "templates");

            migrationBuilder.DropIndex(
                name: "ix_notification_marketing_events_property_active",
                schema: "notification",
                table: "marketing_events");

            migrationBuilder.DropIndex(
                name: "ix_notification_marketing_contact_lists_property",
                schema: "notification",
                table: "marketing_contact_lists");
        }
    }
}
