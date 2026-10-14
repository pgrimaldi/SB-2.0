using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Payment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDocumentReferencesFromComments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "property_id",
                schema: "payment",
                table: "wallet_transaction_items",
                type: "uuid",
                nullable: false,
                comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'eventuale report per stabilimento senza join.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'eventuale report per stabilimento senza join (docs/05-ottimizzazioni-db.md 10).");

            migrationBuilder.AlterColumn<Guid>(
                name: "property_id",
                schema: "payment",
                table: "pos_charge_items",
                type: "uuid",
                nullable: false,
                comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per i report IVA degli addebiti POS e il report per articolo senza join.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per i report IVA degli addebiti POS e il report per articolo senza join (docs/05-ottimizzazioni-db.md 10).");

            migrationBuilder.AlterColumn<Guid>(
                name: "property_id",
                schema: "payment",
                table: "ledger_entry_lines",
                type: "uuid",
                nullable: false,
                comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'export contabile e i report IVA per riga senza join.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'export contabile e i report IVA per riga senza join (docs/05-ottimizzazioni-db.md 10).");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "property_id",
                schema: "payment",
                table: "wallet_transaction_items",
                type: "uuid",
                nullable: false,
                comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'eventuale report per stabilimento senza join (docs/05-ottimizzazioni-db.md 10).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'eventuale report per stabilimento senza join.");

            migrationBuilder.AlterColumn<Guid>(
                name: "property_id",
                schema: "payment",
                table: "pos_charge_items",
                type: "uuid",
                nullable: false,
                comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per i report IVA degli addebiti POS e il report per articolo senza join (docs/05-ottimizzazioni-db.md 10).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per i report IVA degli addebiti POS e il report per articolo senza join.");

            migrationBuilder.AlterColumn<Guid>(
                name: "property_id",
                schema: "payment",
                table: "ledger_entry_lines",
                type: "uuid",
                nullable: false,
                comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'export contabile e i report IVA per riga senza join (docs/05-ottimizzazioni-db.md 10).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'export contabile e i report IVA per riga senza join.");
        }
    }
}
