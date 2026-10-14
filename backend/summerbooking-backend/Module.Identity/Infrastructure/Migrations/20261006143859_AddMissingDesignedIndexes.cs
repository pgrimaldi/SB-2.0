using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingDesignedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_identity_customer_customers_property_email",
                schema: "identity",
                table: "customer_customers",
                columns: new[] { "property_id", "email" },
                filter: "deleted_at IS NULL AND email IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_identity_customer_customers_property_email",
                schema: "identity",
                table: "customer_customers");
        }
    }
}
