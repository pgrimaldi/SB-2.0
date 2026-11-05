using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AgreementWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "agreement_price_list_stay_prices",
                schema: "catalog",
                comment: "Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario di un listino/voucher di una convenzione.",
                oldComment: "Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario di un listino/voucher hotel.");

            migrationBuilder.AlterTable(
                name: "agreement_price_list_sector_prices",
                schema: "catalog",
                comment: "Prezzo base / mezza giornata / stagionale di un listino/voucher di una convenzione per settore.",
                oldComment: "Prezzo base / mezza giornata / stagionale di un listino/voucher hotel per settore.");

            migrationBuilder.AlterTable(
                name: "agreement_price_list_period_prices",
                schema: "catalog",
                comment: "Prezzi per periodo tariffario (etichetta libera) di un listino/voucher di una convenzione, per modalità di durata.",
                oldComment: "Prezzi per periodo tariffario (etichetta libera) di un listino/voucher hotel, per modalità di durata.");

            migrationBuilder.AlterTable(
                name: "agreement_price_list_items",
                schema: "catalog",
                comment: "Componente di un listino/voucher di una convenzione: articolo di magazzino, servizio incluso o pacchetto collegato.",
                oldComment: "Componente di un listino/voucher hotel: articolo di magazzino, servizio incluso o pacchetto collegato.");

            migrationBuilder.AlterTable(
                name: "agreement_price_list_date_prices",
                schema: "catalog",
                comment: "Override di prezzo per data di calendario (festivi, deroghe puntuali) di un listino/voucher di una convenzione.",
                oldComment: "Override di prezzo per data di calendario (festivi, deroghe puntuali) di un listino/voucher hotel.");

            migrationBuilder.AlterTable(
                name: "agreement_partners",
                schema: "catalog",
                comment: "Struttura convenzionata con cui uno stabilimento ha un accordo commerciale.",
                oldComment: "Struttura partner (\"convenzione\"/hotel) con cui uno stabilimento ha un accordo commerciale.");

            migrationBuilder.AlterColumn<decimal>(
                name: "half_day_price",
                schema: "catalog",
                table: "pricing_price_list_sector_prices",
                type: "numeric(12,2)",
                nullable: true,
                comment: "Prezzo forfettario mezza giornata nel settore (allineamento con i listini delle convenzioni; spesso NULL sui price_lists puri).",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldNullable: true,
                oldComment: "Prezzo forfettario mezza giornata nel settore (allineamento con i listini hotel; spesso NULL sui price_lists puri).");

            migrationBuilder.AlterColumn<long>(
                name: "period_price_id",
                schema: "catalog",
                table: "agreement_price_list_stay_prices",
                type: "bigint",
                nullable: false,
                comment: "Periodo tariffario (e, tramite esso, listino/voucher di una convenzione) a cui si applica il prezzo per durata.",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldComment: "Periodo tariffario (e, tramite esso, listino/voucher hotel) a cui si applica il prezzo per durata.");

            migrationBuilder.AlterColumn<int>(
                name: "price_list_id",
                schema: "catalog",
                table: "agreement_price_list_sector_prices",
                type: "integer",
                nullable: false,
                comment: "Listino/voucher della convenzione a cui appartiene il prezzo di settore.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Listino/voucher hotel a cui appartiene il prezzo di settore.");

            migrationBuilder.AlterColumn<int>(
                name: "price_list_id",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                type: "integer",
                nullable: false,
                comment: "Listino/voucher della convenzione a cui appartiene il prezzo di periodo.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Listino/voucher hotel a cui appartiene il prezzo di periodo.");

            migrationBuilder.AlterColumn<int>(
                name: "linked_price_list_id",
                schema: "catalog",
                table: "agreement_price_list_items",
                type: "integer",
                nullable: true,
                comment: "Listino della convenzione a cui questo additional è vincolato.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "Listino hotel a cui questo additional è vincolato.");

            migrationBuilder.AlterColumn<int>(
                name: "price_list_id",
                schema: "catalog",
                table: "agreement_price_list_date_prices",
                type: "integer",
                nullable: false,
                comment: "Listino/voucher della convenzione a cui si applica la deroga.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Listino/voucher hotel a cui si applica la deroga.");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "agreement_price_list_stay_prices",
                schema: "catalog",
                comment: "Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario di un listino/voucher hotel.",
                oldComment: "Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario di un listino/voucher di una convenzione.");

            migrationBuilder.AlterTable(
                name: "agreement_price_list_sector_prices",
                schema: "catalog",
                comment: "Prezzo base / mezza giornata / stagionale di un listino/voucher hotel per settore.",
                oldComment: "Prezzo base / mezza giornata / stagionale di un listino/voucher di una convenzione per settore.");

            migrationBuilder.AlterTable(
                name: "agreement_price_list_period_prices",
                schema: "catalog",
                comment: "Prezzi per periodo tariffario (etichetta libera) di un listino/voucher hotel, per modalità di durata.",
                oldComment: "Prezzi per periodo tariffario (etichetta libera) di un listino/voucher di una convenzione, per modalità di durata.");

            migrationBuilder.AlterTable(
                name: "agreement_price_list_items",
                schema: "catalog",
                comment: "Componente di un listino/voucher hotel: articolo di magazzino, servizio incluso o pacchetto collegato.",
                oldComment: "Componente di un listino/voucher di una convenzione: articolo di magazzino, servizio incluso o pacchetto collegato.");

            migrationBuilder.AlterTable(
                name: "agreement_price_list_date_prices",
                schema: "catalog",
                comment: "Override di prezzo per data di calendario (festivi, deroghe puntuali) di un listino/voucher hotel.",
                oldComment: "Override di prezzo per data di calendario (festivi, deroghe puntuali) di un listino/voucher di una convenzione.");

            migrationBuilder.AlterTable(
                name: "agreement_partners",
                schema: "catalog",
                comment: "Struttura partner (\"convenzione\"/hotel) con cui uno stabilimento ha un accordo commerciale.",
                oldComment: "Struttura convenzionata con cui uno stabilimento ha un accordo commerciale.");

            migrationBuilder.AlterColumn<decimal>(
                name: "half_day_price",
                schema: "catalog",
                table: "pricing_price_list_sector_prices",
                type: "numeric(12,2)",
                nullable: true,
                comment: "Prezzo forfettario mezza giornata nel settore (allineamento con i listini hotel; spesso NULL sui price_lists puri).",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldNullable: true,
                oldComment: "Prezzo forfettario mezza giornata nel settore (allineamento con i listini delle convenzioni; spesso NULL sui price_lists puri).");

            migrationBuilder.AlterColumn<long>(
                name: "period_price_id",
                schema: "catalog",
                table: "agreement_price_list_stay_prices",
                type: "bigint",
                nullable: false,
                comment: "Periodo tariffario (e, tramite esso, listino/voucher hotel) a cui si applica il prezzo per durata.",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldComment: "Periodo tariffario (e, tramite esso, listino/voucher di una convenzione) a cui si applica il prezzo per durata.");

            migrationBuilder.AlterColumn<int>(
                name: "price_list_id",
                schema: "catalog",
                table: "agreement_price_list_sector_prices",
                type: "integer",
                nullable: false,
                comment: "Listino/voucher hotel a cui appartiene il prezzo di settore.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Listino/voucher della convenzione a cui appartiene il prezzo di settore.");

            migrationBuilder.AlterColumn<int>(
                name: "price_list_id",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                type: "integer",
                nullable: false,
                comment: "Listino/voucher hotel a cui appartiene il prezzo di periodo.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Listino/voucher della convenzione a cui appartiene il prezzo di periodo.");

            migrationBuilder.AlterColumn<int>(
                name: "linked_price_list_id",
                schema: "catalog",
                table: "agreement_price_list_items",
                type: "integer",
                nullable: true,
                comment: "Listino hotel a cui questo additional è vincolato.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true,
                oldComment: "Listino della convenzione a cui questo additional è vincolato.");

            migrationBuilder.AlterColumn<int>(
                name: "price_list_id",
                schema: "catalog",
                table: "agreement_price_list_date_prices",
                type: "integer",
                nullable: false,
                comment: "Listino/voucher hotel a cui si applica la deroga.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldComment: "Listino/voucher della convenzione a cui si applica la deroga.");
        }
    }
}
