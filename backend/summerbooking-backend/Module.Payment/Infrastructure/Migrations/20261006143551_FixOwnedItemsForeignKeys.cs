using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Payment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FixOwnedItemsForeignKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Le colonne duplicate (pos_charge_id, wallet_transaction_id) erano shadow property di EF per
            // le relazioni OwnsMany: portavano la FK reale, mentre charge_id / transaction_id, le colonne
            // che il codice e l'ETL usano, restavano senza vincolo. Ora la FK passa sulle colonne giuste
            // (WithOwner().HasForeignKey nelle configurazioni) e le duplicate spariscono.
            //
            // Guardia: si elimina una colonna NOT NULL, quindi si verifica prima che non porti
            // informazione propria. Se su un database le due colonne divergono, la migration si ferma.
            migrationBuilder.Sql("""
DO $$
BEGIN
    IF EXISTS (SELECT 1 FROM payment.pos_charge_items WHERE charge_id <> pos_charge_id) THEN
        RAISE EXCEPTION 'payment.pos_charge_items: charge_id e pos_charge_id divergono, riallinearle prima della migration';
    END IF;
    IF EXISTS (SELECT 1 FROM payment.wallet_transaction_items WHERE transaction_id <> wallet_transaction_id) THEN
        RAISE EXCEPTION 'payment.wallet_transaction_items: transaction_id e wallet_transaction_id divergono, riallinearle prima della migration';
    END IF;
END $$;
""");

            migrationBuilder.DropForeignKey(
                name: "fk_pos_charge_items_pos_charges_pos_charge_id",
                schema: "payment",
                table: "pos_charge_items");

            migrationBuilder.DropForeignKey(
                name: "fk_wallet_transaction_items_wallet_transactions_wallet_transac",
                schema: "payment",
                table: "wallet_transaction_items");

            migrationBuilder.DropIndex(
                name: "ix_wallet_transaction_items_wallet_transaction_id",
                schema: "payment",
                table: "wallet_transaction_items");

            migrationBuilder.DropIndex(
                name: "ix_pos_charge_items_pos_charge_id",
                schema: "payment",
                table: "pos_charge_items");

            migrationBuilder.DropColumn(
                name: "wallet_transaction_id",
                schema: "payment",
                table: "wallet_transaction_items");

            migrationBuilder.DropColumn(
                name: "pos_charge_id",
                schema: "payment",
                table: "pos_charge_items");

            migrationBuilder.AddForeignKey(
                name: "fk_pos_charge_items_pos_charges_charge_id",
                schema: "payment",
                table: "pos_charge_items",
                column: "charge_id",
                principalSchema: "payment",
                principalTable: "pos_charges",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_wallet_transaction_items_wallet_transactions_transaction_id",
                schema: "payment",
                table: "wallet_transaction_items",
                column: "transaction_id",
                principalSchema: "payment",
                principalTable: "wallet_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_pos_charge_items_pos_charges_charge_id",
                schema: "payment",
                table: "pos_charge_items");

            migrationBuilder.DropForeignKey(
                name: "fk_wallet_transaction_items_wallet_transactions_transaction_id",
                schema: "payment",
                table: "wallet_transaction_items");

            migrationBuilder.AddColumn<long>(
                name: "wallet_transaction_id",
                schema: "payment",
                table: "wallet_transaction_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<long>(
                name: "pos_charge_id",
                schema: "payment",
                table: "pos_charge_items",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            // Le colonne tornano NOT NULL con default 0 (necessario per aggiungerle a una tabella piena):
            // senza travaso la FK qui sotto fallirebbe su ogni riga esistente.
            migrationBuilder.Sql("""
UPDATE payment.wallet_transaction_items SET wallet_transaction_id = transaction_id;
UPDATE payment.pos_charge_items SET pos_charge_id = charge_id;
ALTER TABLE payment.wallet_transaction_items ALTER COLUMN wallet_transaction_id DROP DEFAULT;
ALTER TABLE payment.pos_charge_items ALTER COLUMN pos_charge_id DROP DEFAULT;
""");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_transaction_items_wallet_transaction_id",
                schema: "payment",
                table: "wallet_transaction_items",
                column: "wallet_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_pos_charge_items_pos_charge_id",
                schema: "payment",
                table: "pos_charge_items",
                column: "pos_charge_id");

            migrationBuilder.AddForeignKey(
                name: "fk_pos_charge_items_pos_charges_pos_charge_id",
                schema: "payment",
                table: "pos_charge_items",
                column: "pos_charge_id",
                principalSchema: "payment",
                principalTable: "pos_charges",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "fk_wallet_transaction_items_wallet_transactions_wallet_transac",
                schema: "payment",
                table: "wallet_transaction_items",
                column: "wallet_transaction_id",
                principalSchema: "payment",
                principalTable: "wallet_transactions",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
