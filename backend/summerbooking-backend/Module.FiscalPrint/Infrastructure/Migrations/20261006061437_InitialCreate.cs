using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.FiscalPrint.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fiscalprint");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:fiscalprint.connection_kind", "tcp,usb")
                .Annotation("Npgsql:Enum:fiscalprint.printer_mode", "fiscal,non_fiscal,orders")
                .Annotation("Npgsql:Enum:fiscalprint.printer_model", "fp_81,fp_90,rch,tm_t88,tm_t20,online");

            migrationBuilder.CreateTable(
                name: "printer_configs",
                schema: "fiscalprint",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties (stabilimento proprietario), copiato — nessuna FK cross-schema."),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Nome libero della stampante assegnato dall'operatore (etichetta in UI)."),
                    model = table.Column<int>(type: "fiscalprint.printer_model", nullable: false, comment: "Modello stampante: determina l'adapter di stampa lato client (createPrinterAdapter)."),
                    mode = table.Column<int>(type: "fiscalprint.printer_mode", nullable: false, comment: "Modalità d'uso: scontrino fiscale RT, documento non fiscale/preconto, comanda."),
                    connection_kind = table.Column<int>(type: "fiscalprint.connection_kind", nullable: false, defaultValueSql: "'tcp'", comment: "Tipo di connessione al dispositivo: tcp (host+port) o usb (usb_device)."),
                    host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Indirizzo di rete della stampante (IP LAN o 127.0.0.1), valorizzato solo per connection_kind = tcp."),
                    port = table.Column<int>(type: "integer", nullable: true, comment: "Porta TCP della stampante, valorizzata solo per connection_kind = tcp."),
                    usb_device = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identificatore del device USB locale, valorizzato solo per connection_kind = usb."),
                    use_tls = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "True se la comunicazione con la stampante avviene via HTTPS/TLS anziché HTTP in chiaro."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Stampante attualmente selezionata/in uso per la sua modalità d'uso."),
                    heading_line_1 = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Prima riga di intestazione stampata in testa allo scontrino/comanda."),
                    heading_line_2 = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Seconda riga di intestazione stampata in testa allo scontrino/comanda."),
                    footer_line = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Riga di piè di pagina stampata in coda allo scontrino/comanda."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, operatore che ha configurato la stampante — copiato, nessuna FK cross-schema."),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, operatore che ha modificato per ultimo la stampante — copiato, nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della configurazione."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della configurazione."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: valorizzato alla rimozione; is_active riportato a false in automatico (corregge il bug legacy che scollegava i due flag).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiscalprint_printer_configs", x => x.id);
                    table.CheckConstraint("ck_fiscalprint_printer_configs_connection", "(connection_kind = 'tcp' AND host IS NOT NULL AND port IS NOT NULL) OR (connection_kind = 'usb' AND usb_device IS NOT NULL)");
                    table.CheckConstraint("ck_fiscalprint_printer_configs_port_range", "port IS NULL OR port BETWEEN 1 AND 65535");
                },
                comment: "Configurazione di una stampante (termica/fiscale RT) di uno stabilimento: modello, connessione, modalità d'uso, intestazioni scontrino.");

            migrationBuilder.CreateTable(
                name: "printer_departments",
                schema: "fiscalprint",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    printer_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stampante (fiscalprint.printer_configs) a cui il reparto appartiene."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, denormalizzato per lo scoping — copiato, nessuna FK cross-schema."),
                    dept_number = table.Column<short>(type: "smallint", nullable: false, comment: "Numero del reparto fisico sul registratore RT (esplicito, non più posizionale come nel legacy)."),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, comment: "Aliquota IVA con cui il reparto è pre-programmato sulla stampante."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della mappatura."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della mappatura."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete, in cascata dalla cancellazione della stampante proprietaria.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiscalprint_printer_departments", x => x.id);
                    table.CheckConstraint("ck_fiscalprint_printer_departments_number", "dept_number > 0");
                    table.CheckConstraint("ck_fiscalprint_printer_departments_rate", "vat_rate >= 0");
                    table.ForeignKey(
                        name: "fk_fiscalprint_printer_departments_printer",
                        column: x => x.printer_id,
                        principalSchema: "fiscalprint",
                        principalTable: "printer_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Mappatura reparto fisico del registratore di cassa RT → aliquota IVA con cui il reparto è pre-programmato.");

            migrationBuilder.CreateTable(
                name: "receipts",
                schema: "fiscalprint",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile (UUIDv7) per l'URL del dettaglio gestionale (getReceiptDetails)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties (stabilimento che ha stampato), copiato — nessuna FK cross-schema."),
                    printer_id = table.Column<long>(type: "bigint", nullable: true, comment: "Stampante (fiscalprint.printer_configs) usata per la stampa."),
                    reservation_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.reservations, valorizzato solo se lo scontrino è stampato dal dettaglio di una prenotazione — copiato, nessuna FK cross-schema."),
                    booking_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Numero prenotazione (business key denormalizzata da booking.reservations, per comodità del gestionale)."),
                    receipt_number = table.Column<int>(type: "integer", nullable: true, comment: "Numero progressivo dello scontrino RT per (printer_id, giorno fiscale), assegnato da fiscalprint.serial_numbers."),
                    rt_fiscal_receipt_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Numero scontrino restituito dal registratore RT — proiezione read-only non autorevole di device_response."),
                    rt_fiscal_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Importo dello scontrino RT — proiezione derivata da device_response (virgola decimale del dispositivo convertita a numeric)."),
                    rt_z_report_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Numero della chiusura Z in cui rientra lo scontrino — proiezione read-only di device_response."),
                    rt_serial_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Matricola del registratore telematico che ha stampato — proiezione read-only di device_response."),
                    printed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora della stampa; determina il giorno fiscale usato dalla sequenza dei numeri progressivi."),
                    device_response = table.Column<string>(type: "jsonb", nullable: true, comment: "Risposta grezza del registratore telematico (lastCommand, printerStatus, fiscalReceiptNumber/Amount/Date/Time, zRepNumber, serialNumber, …) — passthrough di un sistema esterno, mai interrogata per campo."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, operatore che ha lanciato la stampa — copiato, nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete esposto per uniformità con le altre entità del modulo; mai valorizzato in pratica — log di stampe / fatto fiscale, retention da obbligo di conservazione.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiscalprint_receipts", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiscalprint_receipts_printer",
                        column: x => x.printer_id,
                        principalSchema: "fiscalprint",
                        principalTable: "printer_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Scontrino/ricevuta stampato su registratore di cassa RT fisico, con la risposta grezza del dispositivo e le proiezioni derivate per le liste.");

            migrationBuilder.CreateTable(
                name: "serial_numbers",
                schema: "fiscalprint",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    printer_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stampante RT (dispositivo fiscale) a cui la serie appartiene — il progressivo è per dispositivo, non per stabilimento."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, denormalizzato da fiscalprint.printer_configs per lo scoping/indici — copiato, nessuna FK cross-schema."),
                    fiscal_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Giorno a cui si riferisce il contatore, fuso Europe/Rome."),
                    last_number = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Ultimo numero progressivo scontrino emesso quel giorno su quella stampante; il prossimo scontrino usa last_number + 1."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga (primo scontrino del giorno su quella stampante)."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultimo incremento, aggiornata a ogni upsert."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Colonna esposta solo per uniformità con le altre tabelle del modulo; non implementa ISoftDeletable e non è mai scritta — tabella di sequenza/contatore, non entità di dominio cancellabile.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiscalprint_serial_numbers", x => x.id);
                    table.CheckConstraint("ck_fiscalprint_serial_numbers_last_number", "last_number >= 0");
                    table.ForeignKey(
                        name: "fk_fiscalprint_serial_numbers_printer",
                        column: x => x.printer_id,
                        principalSchema: "fiscalprint",
                        principalTable: "printer_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Contatore del numero progressivo scontrino RT, una riga per (printer_id, fiscal_date); incrementato atomicamente a ogni stampa.");

            migrationBuilder.CreateIndex(
                name: "ix_fiscalprint_printer_configs_property",
                schema: "fiscalprint",
                table: "printer_configs",
                column: "property_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_fiscalprint_printer_configs_active_mode",
                schema: "fiscalprint",
                table: "printer_configs",
                columns: new[] { "property_id", "mode" },
                unique: true,
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_fiscalprint_printer_departments_resolver",
                schema: "fiscalprint",
                table: "printer_departments",
                columns: new[] { "printer_id", "vat_rate" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_fiscalprint_printer_departments_number",
                schema: "fiscalprint",
                table: "printer_departments",
                columns: new[] { "printer_id", "dept_number" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_fiscalprint_receipts_printer",
                schema: "fiscalprint",
                table: "receipts",
                column: "printer_id");

            migrationBuilder.CreateIndex(
                name: "ix_fiscalprint_receipts_property_date",
                schema: "fiscalprint",
                table: "receipts",
                columns: new[] { "property_id", "printed_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fiscalprint_receipts_reservation",
                schema: "fiscalprint",
                table: "receipts",
                column: "reservation_public_id",
                filter: "reservation_public_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_fiscalprint_receipts_public_id",
                schema: "fiscalprint",
                table: "receipts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fiscalprint_serial_numbers_property_date",
                schema: "fiscalprint",
                table: "serial_numbers",
                columns: new[] { "property_id", "fiscal_date" });

            migrationBuilder.CreateIndex(
                name: "uq_fiscalprint_serial_numbers_key",
                schema: "fiscalprint",
                table: "serial_numbers",
                columns: new[] { "printer_id", "fiscal_date" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "printer_departments",
                schema: "fiscalprint");

            migrationBuilder.DropTable(
                name: "receipts",
                schema: "fiscalprint");

            migrationBuilder.DropTable(
                name: "serial_numbers",
                schema: "fiscalprint");

            migrationBuilder.DropTable(
                name: "printer_configs",
                schema: "fiscalprint");
        }
    }
}
