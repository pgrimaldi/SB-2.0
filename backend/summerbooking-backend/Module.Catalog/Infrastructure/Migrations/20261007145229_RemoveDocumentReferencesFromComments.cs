using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDocumentReferencesFromComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "latitude",
                schema: "catalog",
                table: "search_bathhouses",
                type: "double precision",
                nullable: true,
                comment: "Latitudine della vetrina, per la ricerca \"vicino a me\"/entro raggio (al posto di una colonna geography(Point,4326): nessuna dipendenza PostGIS/NetTopologySuite nella solution).",
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true,
                oldComment: "Latitudine della vetrina, per la ricerca \"vicino a me\"/entro raggio (sostituisce la colonna geography(Point,4326) della scheda — nessuna dipendenza PostGIS/NetTopologySuite nella solution).");

            migrationBuilder.AlterColumn<string>(
                name: "period_label",
                schema: "catalog",
                table: "pricing_suggested_prices",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "Etichetta libera del periodo/settore legacy (es. \"Fila 1 Alta stagione\"); NULL quando il calcolo non distingue varianti per la data.",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true,
                oldComment: "Etichetta libera del periodo/settore legacy (es. \"Fila 1 Alta stagione\"); NULL quando il calcolo non distingue varianti per la data. Aggiunta per D-63/D-64.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<double>(
                name: "latitude",
                schema: "catalog",
                table: "search_bathhouses",
                type: "double precision",
                nullable: true,
                comment: "Latitudine della vetrina, per la ricerca \"vicino a me\"/entro raggio (sostituisce la colonna geography(Point,4326) della scheda — nessuna dipendenza PostGIS/NetTopologySuite nella solution).",
                oldClrType: typeof(double),
                oldType: "double precision",
                oldNullable: true,
                oldComment: "Latitudine della vetrina, per la ricerca \"vicino a me\"/entro raggio (al posto di una colonna geography(Point,4326): nessuna dipendenza PostGIS/NetTopologySuite nella solution).");

            migrationBuilder.AlterColumn<string>(
                name: "period_label",
                schema: "catalog",
                table: "pricing_suggested_prices",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "Etichetta libera del periodo/settore legacy (es. \"Fila 1 Alta stagione\"); NULL quando il calcolo non distingue varianti per la data. Aggiunta per D-63/D-64.",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true,
                oldComment: "Etichetta libera del periodo/settore legacy (es. \"Fila 1 Alta stagione\"); NULL quando il calcolo non distingue varianti per la data.");
        }
    }
}
