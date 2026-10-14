using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingDesignedIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_sale_items_property_kind",
                schema: "catalog",
                table: "service_sale_items",
                columns: new[] { "property_id", "kind" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_lists_property_default",
                schema: "catalog",
                table: "pricing_price_lists",
                column: "property_id",
                filter: "is_default AND deleted_at IS NULL");

            // Oggetti che il modello EF non sa esprimere (indice su espressione): da riportare a mano in
            // un eventuale nuovo consolidamento delle migration.
            // Ricerca amministrativa per nome (sostituisce i LIKE full-scan del legacy). Richiede pg_trgm.
            migrationBuilder.Sql("""
CREATE INDEX ix_catalog_properties_name_trgm
    ON catalog.properties USING gin (lower(name) gin_trgm_ops);
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS catalog.ix_catalog_properties_name_trgm;");

            migrationBuilder.DropIndex(
                name: "ix_catalog_service_sale_items_property_kind",
                schema: "catalog",
                table: "service_sale_items");

            migrationBuilder.DropIndex(
                name: "ix_catalog_pricing_price_lists_property_default",
                schema: "catalog",
                table: "pricing_price_lists");
        }
    }
}
