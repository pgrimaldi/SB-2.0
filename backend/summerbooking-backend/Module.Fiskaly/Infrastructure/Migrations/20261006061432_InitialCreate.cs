using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Fiskaly.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "fiskaly");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:fiskaly.card_kind", "credit,debit")
                .Annotation("Npgsql:Enum:fiskaly.entity_mode", "inactive,operative,suspended,degraded")
                .Annotation("Npgsql:Enum:fiskaly.entity_state", "acquired,commissioned,decommissioned")
                .Annotation("Npgsql:Enum:fiskaly.entry_category", "service,goods")
                .Annotation("Npgsql:Enum:fiskaly.entry_type", "sale,return")
                .Annotation("Npgsql:Enum:fiskaly.fiscal_setup_level", "not_configured,vat_only,fiskaly_active")
                .Annotation("Npgsql:Enum:fiskaly.fiskaly_record_state", "accepted,rejected,completed,failed")
                .Annotation("Npgsql:Enum:fiskaly.language_code", "it,en")
                .Annotation("Npgsql:Enum:fiskaly.line_item_type", "package,additional_item,bar_service,normal_service,hourly_service,agreement_voucher_package,standard_voucher_fixed,standard_voucher_percentage,surcharge_fixed,surcharge_percentage,pos_charge")
                .Annotation("Npgsql:Enum:fiskaly.log_result", "ok,ko")
                .Annotation("Npgsql:Enum:fiskaly.media_kind", "fiscal_receipt_pdf,non_fiscal_receipt_pdf")
                .Annotation("Npgsql:Enum:fiskaly.media_origin", "s3_us_east_2,cloudfront,external")
                .Annotation("Npgsql:Enum:fiskaly.org_state", "enabled,decommissioned")
                .Annotation("Npgsql:Enum:fiskaly.outstanding_concept", "service")
                .Annotation("Npgsql:Enum:fiskaly.payment_mode", "cash,credit_card,bank_transfer,other")
                .Annotation("Npgsql:Enum:fiskaly.payment_type", "cash,card,online,other,voucher,outstanding")
                .Annotation("Npgsql:Enum:fiskaly.receipt_kind", "fiscal,non_fiscal")
                .Annotation("Npgsql:Enum:fiskaly.receipt_log_step", "intention_created,transaction_created,transaction_failed,pdf_generation_failed,s3upload_failed,completed,creation_failed,cancellation_intention_created,cancellation_intention_failed,cancellation_transaction_failed,cancelled")
                .Annotation("Npgsql:Enum:fiskaly.receipt_record_state", "intention_created,transaction_created,transaction_failed,ereceipt_failed,s3upload_failed,completed,cancellation_intention_created,cancellation_transaction_failed,cancelled,generated,generation_failed")
                .Annotation("Npgsql:Enum:fiskaly.receipt_source", "manual,autoreceipt")
                .Annotation("Npgsql:Enum:fiskaly.record_mode", "finished")
                .Annotation("Npgsql:Enum:fiskaly.system_type", "fiscal_device")
                .Annotation("Npgsql:Enum:fiskaly.vat_kind", "vat_rate,vat_exemption")
                .Annotation("Npgsql:Enum:fiskaly.voucher_kind", "single");

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "fiskaly",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile (UUIDv7); esposto all'export GDPR e al pannello Fiscalization."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Stabilimento proprietario dell'account."),
                    fiscal_setup_level = table.Column<int>(type: "fiskaly.fiscal_setup_level", nullable: false, defaultValueSql: "'not_configured'", comment: "Stato di configurazione fiscale: NotConfigured / VatOnly / FiskalyActive. FiskalyActive è derivato e ricalcolato dal dominio, non liberamente scrivibile."),
                    terms_accepted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante in cui il gestore ha accettato termini e condizioni Fiskaly; NULL = non accettati. Precondizione bloccante per il setup (sostituisce il flag terms_accepted)."),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Interruttore master della fiscalità elettronica, gestito da SuperAdmin."),
                    autoreceipt_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Emissione automatica dello scontrino dopo un pagamento Stripe andato a buon fine."),
                    organization_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true, comment: "Id opaco dell'Organization su Fiskaly SIGN IT."),
                    subject_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true, comment: "Id opaco del Subject API_KEY su Fiskaly SIGN IT."),
                    taxpayer_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true, comment: "Id opaco del Taxpayer COMPANY su Fiskaly SIGN IT."),
                    location_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true, comment: "Id opaco della Location BRANCH su Fiskaly SIGN IT."),
                    organization_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Nome assegnato all'Organization su Fiskaly."),
                    subject_api_key_name = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Nome del Subject API_KEY su Fiskaly."),
                    organization_state = table.Column<int>(type: "fiskaly.org_state", nullable: true, comment: "Stato dell'Organization su Fiskaly (enabled/decommissioned); può divergere dal remoto dopo closeAccount."),
                    subject_state = table.Column<int>(type: "fiskaly.org_state", nullable: true, comment: "Stato del Subject su Fiskaly (enabled/decommissioned); può divergere dal remoto dopo closeAccount."),
                    taxpayer_state = table.Column<int>(type: "fiskaly.entity_state", nullable: true, comment: "Ciclo di vita del Taxpayer su Fiskaly (acquired/commissioned/decommissioned)."),
                    location_state = table.Column<int>(type: "fiskaly.entity_state", nullable: true, comment: "Ciclo di vita della Location su Fiskaly (acquired/commissioned/decommissioned)."),
                    taxpayer_mode = table.Column<int>(type: "fiskaly.entity_mode", nullable: true, comment: "Modalità operativa del Taxpayer su Fiskaly (inactive/operative/suspended/degraded)."),
                    location_mode = table.Column<int>(type: "fiskaly.entity_mode", nullable: true, comment: "Modalità operativa della Location su Fiskaly (inactive/operative/suspended/degraded)."),
                    subject_credentials_secret_ref = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Riferimento (ARN/nome) al segreto {key,secret} del Subject in secret manager — nessun materiale segreto in colonna."),
                    taxpayer_legal_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Ragione sociale legale del titolare, inviata a Fiskaly come name.legal."),
                    taxpayer_trade_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Nome commerciale / insegna, preferito nel PDF scontrino e nei messaggi."),
                    taxpayer_vat_number = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: true, comment: "Partita IVA del titolare (dato fiscale)."),
                    taxpayer_fiscal_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true, comment: "Codice fiscale / partita IVA del soggetto passivo (dato fiscale)."),
                    taxpayer_address_street = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Via dell'indirizzo del titolare inviato a Fiskaly e stampato sul PDF."),
                    taxpayer_address_street_number = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true, comment: "Numero civico dell'indirizzo del titolare (default applicativo 'SNC' quando inviato)."),
                    taxpayer_address_postal_code = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: true, comment: "CAP dell'indirizzo del titolare."),
                    taxpayer_address_city = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Città dell'indirizzo del titolare."),
                    taxpayer_address_country_code = table.Column<string>(type: "char(2)", nullable: false, defaultValue: "IT", comment: "Nazione ISO 3166-1 alpha-2 dell'indirizzo del titolare. Vocabolario locale al modulo (non un tipo SharedKernel cross-schema in questa fase)."),
                    fisconline_holder_fiscal_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true, comment: "Codice fiscale della persona fisica abilitata a Fisconline — dato personale, rilevante per l'export GDPR."),
                    fisconline_credentials_secret_ref = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Riferimento (ARN/nome) al segreto {pin,password} Fisconline in secret manager — nessun materiale segreto in colonna."),
                    fisconline_updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data dell'ultima modifica di PIN o password Fisconline."),
                    software_cash_register_is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Se true, la cassa software auto-creata è quella di default e le casse utente sono forzate a is_default=false."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha creato la riga."),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Ultimo operatore che ha modificato la configurazione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete = chiusura account (closeAccount); un successivo updateAccount lo azzera e ricrea da zero tutte le entità Fiskaly.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiskaly_accounts", x => x.id);
                    table.CheckConstraint("ck_fiskaly_accounts_activation_chain", "autoreceipt_enabled = false OR is_enabled = true");
                },
                comment: "Account fiscale di uno stabilimento presso Fiskaly SIGN IT: identità, credenziali (per riferimento) e stato di configurazione fiscale.");

            migrationBuilder.CreateTable(
                name: "media_assets",
                schema: "fiskaly",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    origin = table.Column<int>(type: "fiskaly.media_origin", nullable: false, comment: "Codice della sorgente da cui è servito il file; la mappa origin → URL base vive in configurazione."),
                    path = table.Column<string>(type: "text", nullable: false, comment: "Percorso relativo del file rispetto alla base di origin (nessuna barra iniziale); se origin=external, URL completo invariato."),
                    kind = table.Column<int>(type: "fiskaly.media_kind", nullable: false, comment: "Tipo d'uso del file: PDF scontrino fiscale o pre-conto."),
                    checksum = table.Column<byte[]>(type: "bytea", nullable: true, comment: "Hash SHA-256 del contenuto per deduplica/integrità; NULL per i file migrati finché non rigenerati."),
                    byte_size = table.Column<int>(type: "integer", nullable: true, comment: "Dimensione del file in byte."),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "MIME type del file (es. application/pdf)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: un asset non è cancellato finché lo referenzia uno scontrino; per i PDF fiscali di fatto non si cancella (retention 10 anni).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiskaly_media_assets", x => x.id);
                },
                comment: "Catalogo locale al modulo fiskaly dei riferimenti a file — oggi solo i PDF degli scontrini fiscali e pre-conto.");

            migrationBuilder.CreateTable(
                name: "vat_codes",
                schema: "fiskaly",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Codice SIGN IT, chiave di business inviata a Fiskaly verbatim; chiave delle risorse di localizzazione."),
                    kind = table.Column<int>(type: "fiskaly.vat_kind", nullable: false, comment: "Se il codice è un'aliquota (VatRate) o un caso di esenzione (VatExemption)."),
                    rate = table.Column<decimal>(type: "numeric(5,2)", nullable: true, comment: "Aliquota IVA in percentuale; NULL per i codici di esenzione."),
                    nature_code = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true, comment: "Codice Natura (N1-N6) per fatturazione elettronica / SdI; NULL per le aliquote."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Se il codice è proponibile nelle UI; i codici storici restano per gli scontrini già emessi con is_active=false."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordinamento nelle select di aliquote/esenzioni."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga di lookup."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga di lookup."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposta per uniformità ma non usata: un codice IVA non si cancella, si disattiva con is_active.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiskaly_vat_codes", x => x.id);
                    table.CheckConstraint("ck_fiskaly_vat_codes_kind_coherence", "(kind = 'vat_rate' AND rate IS NOT NULL AND nature_code IS NULL) OR (kind = 'vat_exemption' AND nature_code IS NOT NULL AND rate IS NULL)");
                },
                comment: "Catalogo dei codici IVA e di esenzione ammessi da Fiskaly SIGN IT per le righe del documento commerciale.");

            migrationBuilder.CreateTable(
                name: "cash_registers",
                schema: "fiskaly",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    account_id = table.Column<long>(type: "bigint", nullable: false, comment: "Account Fiskaly (stabilimento) a cui la cassa appartiene."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per lo scoping/indici senza join."),
                    fiskaly_system_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true, comment: "Id del System su Fiskaly; chiave con cui scontrini e sequenze giornaliere si legano alla cassa."),
                    system_type = table.Column<int>(type: "fiskaly.system_type", nullable: false, defaultValueSql: "'fiscal_device'", comment: "Tipo di System su Fiskaly (di fatto costante: FISCAL_DEVICE)."),
                    producer_number = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Numero produttore del dispositivo (producer.number); non modificabile dopo la creazione."),
                    producer_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true, comment: "Nome/descrizione del produttore — campo descrittivo, non chiave di riconoscimento della cassa software."),
                    software_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Nome del software gestionale (software.name verso Fiskaly)."),
                    software_version = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Versione del software gestionale (software.version verso Fiskaly); un cambio forza decommission e ricreazione del System."),
                    system_state = table.Column<int>(type: "fiskaly.entity_state", nullable: true, comment: "Stato del System su Fiskaly (acquired/commissioned/decommissioned)."),
                    system_mode = table.Column<int>(type: "fiskaly.entity_mode", nullable: true, comment: "Modalità operativa del System su Fiskaly (inactive/operative/suspended/degraded)."),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "True = cassa di default dello stabilimento, usata quando l'emissione non specifica un system id."),
                    is_software = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "True = registratore software auto-creato dal backend; colonna autorevole, non modificabile via API."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha creato la cassa."),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Ultimo operatore che ha modificato la cassa."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete, valorizzato da deleteEntities, closeAccount, dalle cascate di decommission.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiskaly_cash_registers", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiskaly_cash_registers_account",
                        column: x => x.account_id,
                        principalSchema: "fiskaly",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Registratore di cassa / System FISCAL_DEVICE registrato su Fiskaly SIGN IT, con cui si firmano i documenti commerciali.");

            migrationBuilder.CreateTable(
                name: "receipt_sequences",
                schema: "fiskaly",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per lo scoping/indici."),
                    cash_register_id = table.Column<long>(type: "bigint", nullable: false, comment: "Registratore di cassa a cui la sequenza appartiene."),
                    fiscal_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Giorno fiscale del contatore, sempre calcolato in Europe/Rome."),
                    last_number = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Ultimo numero progressivo di documento effettivamente emesso quel giorno su quella cassa; monotòno, incrementato solo su firma riuscita."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultimo incremento del contatore (aggiornato dall'ON CONFLICT DO UPDATE)."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità ma non usato: la sequenza non si cancella.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiskaly_receipt_sequences", x => x.id);
                    table.CheckConstraint("ck_fiskaly_receipt_sequences_last_number", "last_number >= 0");
                    table.ForeignKey(
                        name: "fk_fiskaly_receipt_sequences_cash_register",
                        column: x => x.cash_register_id,
                        principalSchema: "fiskaly",
                        principalTable: "cash_registers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Contatore progressivo giornaliero del numero documento commerciale per registratore di cassa.");

            migrationBuilder.CreateTable(
                name: "receipts",
                schema: "fiskaly",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile (UUIDv7); usato in URL gestionale e copiato cross-modulo da C3/C6."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Stabilimento emittente."),
                    document_kind = table.Column<int>(type: "fiskaly.receipt_kind", nullable: false, comment: "Discriminatore di testata: fiscal (firmato SIGN IT) oppure non_fiscal (pre-conto/proforma)."),
                    source = table.Column<int>(type: "fiskaly.receipt_source", nullable: false, defaultValueSql: "'manual'", comment: "Origine dell'emissione: manual (operatore) oppure autoreceipt (post-pagamento Stripe)."),
                    fiskaly_account_id = table.Column<long>(type: "bigint", nullable: true, comment: "Account fiscale sotto cui è stato emesso il documento."),
                    cash_register_id = table.Column<long>(type: "bigint", nullable: true, comment: "Registratore di cassa che ha firmato il documento."),
                    fiskaly_system_id_snapshot = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true, comment: "Id System Fiskaly congelato al momento della firma — snapshot accanto alla FK viva cash_register_id."),
                    document_number = table.Column<int>(type: "integer", nullable: true, comment: "Numero progressivo fiscale assegnato dalla sequenza giornaliera della cassa; non univoco (riparte ogni giorno/cassa)."),
                    record_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true, comment: "Id del record TRANSACTION::RECEIPT su Fiskaly, referenziato dalla CANCELLATION."),
                    intention_record_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true, comment: "Id del record INTENTION consumato dalla TRANSACTION di emissione."),
                    intention_cancellation_record_id = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true, comment: "Id del record INTENTION consumato dalla TRANSACTION di annullamento."),
                    record_state = table.Column<int>(type: "fiskaly.receipt_record_state", nullable: false, comment: "Stato interno del flusso di emissione/annullamento/generazione."),
                    fiskaly_state_raw = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Stato grezzo del record TRANSACTION restituito da SIGN IT, verbatim, per diagnostica."),
                    record_mode = table.Column<int>(type: "fiskaly.record_mode", nullable: true, comment: "Mode del record TRANSACTION Fiskaly."),
                    signed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di firma; determina il giorno fiscale della sequenza numerica e la data documento sul PDF."),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "EUR", comment: "Valuta ISO 4217 del documento."),
                    language_code = table.Column<int>(type: "fiskaly.language_code", nullable: false, defaultValueSql: "'it'", comment: "Lingua del PDF / eReceipt. Vocabolario concettualmente SharedKernel, registrato localmente in fiskaly finché un secondo modulo non lo userà davvero (vedi Domain/Enums/LanguageCode.cs)."),
                    total_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Totale del documento IVA inclusa, sconto già applicato — quanto il cliente paga con questo scontrino."),
                    taxable_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Imponibile del documento, al netto dello sconto."),
                    vat_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Totale IVA del documento."),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Sconto totale applicato al documento, già scontato da total_amount."),
                    invoiced_payment_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Importo dell'incasso specifico che questo scontrino ha fatturato."),
                    invoiced_payment_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data dell'incasso fatturato da questo scontrino."),
                    invoiced_payment_mode = table.Column<int>(type: "fiskaly.payment_mode", nullable: true, comment: "Forma dell'incasso fatturato da questo scontrino (vocabolario condiviso con booking/payment)."),
                    seller_legal_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Ragione sociale del venditore congelata al momento dell'emissione."),
                    seller_trade_name_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Nome commerciale del venditore congelato al momento dell'emissione."),
                    seller_vat_number_snapshot = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true, comment: "Partita IVA del venditore congelata al momento dell'emissione."),
                    seller_fiscal_code_snapshot = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true, comment: "Codice fiscale del venditore congelato al momento dell'emissione."),
                    seller_address_snapshot = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true, comment: "Indirizzo del venditore congelato e già formattato per il documento al momento dell'emissione."),
                    reservation_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.reservations, copiato — nessuna FK cross-schema. Prenotazione fatturata."),
                    booking_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Numero prenotazione — business key denormalizzata per autorizzazioni e invio senza join."),
                    pdf_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Riferimento al PDF eReceipt del documento."),
                    e_receipt_uuid = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: true, comment: "Id del PDF eReceipt, usato come nome file su storage."),
                    fiskaly_response = table.Column<string>(type: "jsonb", nullable: true, comment: "Risposta grezza completa di Fiskaly per il record TRANSACTION — passthrough di sistema esterno, solo audit/diagnostica."),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di annullamento dello scontrino."),
                    cancelled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Chi ha annullato lo scontrino."),
                    cancellation_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Motivo dell'annullamento."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha emesso il documento; NULL per source=autoreceipt."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione riga (inizio flusso) / ultimo step. getReceipts filtra per intervallo su questa colonna."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultimo step del flusso di emissione/annullamento."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità ma non usato: fatto fiscale, conservazione 10 anni; l'annullamento è record_state = cancelled, non un soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiskaly_receipts", x => x.id);
                    table.CheckConstraint("ck_fiskaly_receipts_amounts", "round(taxable_amount + vat_amount, 2) = total_amount");
                    table.CheckConstraint("ck_fiskaly_receipts_autoreceipt_no_user", "source = 'manual' OR created_by_user_id IS NULL");
                    table.CheckConstraint("ck_fiskaly_receipts_fiscal_account", "document_kind = 'non_fiscal' OR fiskaly_account_id IS NOT NULL");
                    table.CheckConstraint("ck_fiskaly_receipts_fiscal_cash_register", "document_kind = 'non_fiscal' OR cash_register_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_fiskaly_receipts_account",
                        column: x => x.fiskaly_account_id,
                        principalSchema: "fiskaly",
                        principalTable: "accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fiskaly_receipts_cash_register",
                        column: x => x.cash_register_id,
                        principalSchema: "fiskaly",
                        principalTable: "cash_registers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_fiskaly_receipts_pdf_asset",
                        column: x => x.pdf_asset_id,
                        principalSchema: "fiskaly",
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Documento commerciale emesso (fiscale o non fiscale) per una prenotazione — testata unica, fatto fiscale storico immutabile.");

            migrationBuilder.CreateTable(
                name: "receipt_lines",
                schema: "fiskaly",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    receipt_id = table.Column<long>(type: "bigint", nullable: false, comment: "Documento a cui la voce appartiene."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per i riepiloghi IVA senza join."),
                    line_no = table.Column<short>(type: "smallint", nullable: false, comment: "Ordine della voce nel documento (1-based)."),
                    line_kind = table.Column<int>(type: "fiskaly.line_item_type", nullable: false, comment: "Tipo di voce-prenotazione sottostante (pacchetto/servizio/articolo/sovrapprezzo/…), vocabolario condiviso con booking."),
                    fiskaly_entry_type = table.Column<int>(type: "fiskaly.entry_type", nullable: false, defaultValueSql: "'sale'", comment: "Vendita o reso."),
                    category = table.Column<int>(type: "fiskaly.entry_category", nullable: false, defaultValueSql: "'service'", comment: "Natura fiscale della voce: servizio o bene."),
                    source_line_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id della riga prenotazione C1 di provenienza (booking_lines / booking_service_lines / booking_line_additional_items secondo line_kind), copiato — nessuna FK cross-schema. NULL per voci sintetiche non mappabili."),
                    description_snapshot = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Descrizione articolo congelata all'emissione (nome pacchetto/servizio al momento del documento)."),
                    quantity = table.Column<decimal>(type: "numeric(12,3)", nullable: false, defaultValue: 1m, comment: "Quantità della voce (ammette frazioni per servizi a peso/ore)."),
                    unit_price_gross = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Prezzo unitario IVA inclusa."),
                    unit_price_net = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Prezzo unitario IVA esclusa."),
                    vat_code_id = table.Column<short>(type: "smallint", nullable: false, comment: "Codice IVA/esenzione della voce."),
                    vat_kind = table.Column<int>(type: "fiskaly.vat_kind", nullable: false, defaultValueSql: "'vat_rate'", comment: "Tipo IVA denormalizzato dal codice IVA e congelato sulla riga al momento dell'emissione."),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, comment: "Aliquota percentuale applicata (0 per le esenzioni) — dato proprio del documento, non uno snapshot."),
                    vat_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "IVA della riga."),
                    line_gross = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Totale riga IVA inclusa (quantity × unit_price_gross), materializzato per i riepiloghi."),
                    line_net = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Imponibile riga, materializzato per i riepiloghi."),
                    paid_allocation = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Quota di incasso che questo scontrino ha attribuito a questa voce."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità ma non usato: append-only con la testata (fatto fiscale, retention 10 anni).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiskaly_receipt_lines", x => x.id);
                    table.CheckConstraint("ck_fiskaly_receipt_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_fiskaly_receipt_lines_receipt",
                        column: x => x.receipt_id,
                        principalSchema: "fiskaly",
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_fiskaly_receipt_lines_vat_code",
                        column: x => x.vat_code_id,
                        principalSchema: "fiskaly",
                        principalTable: "vat_codes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Voce del documento commerciale (pacchetto, articolo, servizio, sovrapprezzo) congelata al momento dell'emissione.");

            migrationBuilder.CreateTable(
                name: "receipt_logs",
                schema: "fiskaly",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    receipt_id = table.Column<long>(type: "bigint", nullable: true, comment: "Scontrino a cui il log si riferisce."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Ridondante con lo scontrino, comodo per filtrare i log."),
                    booking_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Numero prenotazione dello scontrino — denormalizzazione per filtrare i log senza join."),
                    step = table.Column<int>(type: "fiskaly.receipt_log_step", nullable: false, comment: "Passo/stato del flusso a cui il log si riferisce."),
                    result = table.Column<int>(type: "fiskaly.log_result", nullable: false, defaultValueSql: "'ok'", comment: "Esito del passo."),
                    internal_message = table.Column<string>(type: "text", nullable: true, comment: "Messaggio d'errore interno o nota diagnostica; NULL sui log OK."),
                    fiskaly_record_state = table.Column<int>(type: "fiskaly.fiskaly_record_state", nullable: true, comment: "Stato grezzo del record restituito da Fiskaly per quel passo."),
                    fiskaly_error_status = table.Column<short>(type: "smallint", nullable: true, comment: "Status HTTP dell'errore Fiskaly/axios; NULL sui log OK."),
                    fiskaly_error_code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true, comment: "Codice d'errore applicativo/Fiskaly — vocabolario aperto (SIGN IT può introdurne di nuovi), non enum."),
                    fiskaly_error_category = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true, comment: "Breve categoria/descrizione dell'errore Fiskaly."),
                    fiskaly_error_message = table.Column<string>(type: "text", nullable: true, comment: "Messaggio d'errore Fiskaly completo, non troncato (a differenza del legacy)."),
                    fiskaly_log_payload = table.Column<string>(type: "jsonb", nullable: true, comment: "Array/oggetto grezzo dei log d'errore Fiskaly — passthrough di sistema esterno, solo diagnostica."),
                    actor_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha innescato l'operazione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Istante del log. Chiave di ordinamento cronologico dei log di uno scontrino."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Esposto per uniformità ma non usato: tabella di puro log append-only, sempre uguale a created_at."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità ma non usato: log append-only, nessun soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiskaly_receipt_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_fiskaly_receipt_logs_receipt",
                        column: x => x.receipt_id,
                        principalSchema: "fiskaly",
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Log append-only del flusso di emissione/annullamento dello scontrino elettronico: una riga per transizione di stato o errore SIGN IT.");

            migrationBuilder.CreateTable(
                name: "receipt_payments",
                schema: "fiskaly",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    receipt_id = table.Column<long>(type: "bigint", nullable: false, comment: "Documento a cui la riga pagamento appartiene."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per i riepiloghi senza join."),
                    payment_no = table.Column<short>(type: "smallint", nullable: false, comment: "Ordine della riga pagamento nel documento (1-based)."),
                    payment_type = table.Column<int>(type: "fiskaly.payment_type", nullable: false, comment: "Forma di pagamento normalizzata a un tipo valido Fiskaly."),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Importo lordo incassato con questa forma di pagamento."),
                    discount_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Sconto applicato su questa forma di pagamento."),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true, comment: "Etichetta descrittiva libera (es. per online/other) o valore originale del drift legacy, testo congelato."),
                    card_kind = table.Column<int>(type: "fiskaly.card_kind", nullable: true, comment: "Tipo carta, valorizzato solo se payment_type = card."),
                    card_number_masked = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Ultime cifre / maschera della carta — mai il PAN completo."),
                    voucher_kind = table.Column<int>(type: "fiskaly.voucher_kind", nullable: true, comment: "Tipo voucher, valorizzato solo se payment_type = voucher."),
                    outstanding_concept = table.Column<int>(type: "fiskaly.outstanding_concept", nullable: true, comment: "Concetto dell'insoluto, valorizzato solo se payment_type = outstanding."),
                    ledger_entry_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di payment.ledger_entries, copiato — nessuna FK cross-schema. Movimento di incasso corrispondente nel libro incassi C3."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità ma non usato: append-only con la testata.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fiskaly_receipt_payments", x => x.id);
                    table.CheckConstraint("ck_fiskaly_receipt_payments_card_kind", "payment_type = 'card' OR card_kind IS NULL");
                    table.CheckConstraint("ck_fiskaly_receipt_payments_outstanding_concept", "payment_type = 'outstanding' OR outstanding_concept IS NULL");
                    table.CheckConstraint("ck_fiskaly_receipt_payments_voucher_kind", "payment_type = 'voucher' OR voucher_kind IS NULL");
                    table.ForeignKey(
                        name: "fk_fiskaly_receipt_payments_receipt",
                        column: x => x.receipt_id,
                        principalSchema: "fiskaly",
                        principalTable: "receipts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Modalità di pagamento con cui è stato incassato l'importo del documento commerciale, congelata all'emissione.");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_accounts_setup_level",
                schema: "fiskaly",
                table: "accounts",
                column: "fiscal_setup_level",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_accounts_location",
                schema: "fiskaly",
                table: "accounts",
                column: "location_id",
                unique: true,
                filter: "location_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_accounts_organization",
                schema: "fiskaly",
                table: "accounts",
                column: "organization_id",
                unique: true,
                filter: "organization_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_accounts_property",
                schema: "fiskaly",
                table: "accounts",
                column: "property_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_accounts_public_id",
                schema: "fiskaly",
                table: "accounts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_accounts_subject",
                schema: "fiskaly",
                table: "accounts",
                column: "subject_id",
                unique: true,
                filter: "subject_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_accounts_taxpayer",
                schema: "fiskaly",
                table: "accounts",
                column: "taxpayer_id",
                unique: true,
                filter: "taxpayer_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_cash_registers_property_active",
                schema: "fiskaly",
                table: "cash_registers",
                columns: new[] { "property_id", "system_state", "system_mode" },
                filter: "deleted_at IS NULL AND system_state = 'commissioned' AND system_mode = 'operative'");

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_cash_registers_default",
                schema: "fiskaly",
                table: "cash_registers",
                column: "account_id",
                unique: true,
                filter: "is_default AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_cash_registers_system",
                schema: "fiskaly",
                table: "cash_registers",
                column: "fiskaly_system_id",
                unique: true,
                filter: "fiskaly_system_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_media_assets_kind",
                schema: "fiskaly",
                table: "media_assets",
                column: "kind",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_media_assets_origin",
                schema: "fiskaly",
                table: "media_assets",
                column: "origin");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipt_lines_receipt",
                schema: "fiskaly",
                table: "receipt_lines",
                columns: new[] { "receipt_id", "line_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipt_lines_source",
                schema: "fiskaly",
                table: "receipt_lines",
                column: "source_line_public_id",
                filter: "source_line_public_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipt_lines_vat",
                schema: "fiskaly",
                table: "receipt_lines",
                columns: new[] { "property_id", "vat_code_id" });

            migrationBuilder.CreateIndex(
                name: "ix_receipt_lines_vat_code_id",
                schema: "fiskaly",
                table: "receipt_lines",
                column: "vat_code_id");

            migrationBuilder.CreateIndex(
                name: "brin_fiskaly_receipt_logs_created_at",
                schema: "fiskaly",
                table: "receipt_logs",
                column: "created_at")
                .Annotation("Npgsql:IndexMethod", "brin");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipt_logs_errors",
                schema: "fiskaly",
                table: "receipt_logs",
                columns: new[] { "property_id", "created_at" },
                filter: "result = 'ko'");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipt_logs_receipt",
                schema: "fiskaly",
                table: "receipt_logs",
                columns: new[] { "receipt_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipt_payments_ledger",
                schema: "fiskaly",
                table: "receipt_payments",
                column: "ledger_entry_public_id",
                filter: "ledger_entry_public_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipt_payments_receipt",
                schema: "fiskaly",
                table: "receipt_payments",
                columns: new[] { "receipt_id", "payment_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipt_payments_type",
                schema: "fiskaly",
                table: "receipt_payments",
                columns: new[] { "property_id", "payment_type" });

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipt_sequences_property_date",
                schema: "fiskaly",
                table: "receipt_sequences",
                columns: new[] { "property_id", "fiscal_date" });

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_receipt_sequences_key",
                schema: "fiskaly",
                table: "receipt_sequences",
                columns: new[] { "cash_register_id", "fiscal_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipts_booking_number",
                schema: "fiskaly",
                table: "receipts",
                column: "booking_number");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipts_cash_register",
                schema: "fiskaly",
                table: "receipts",
                columns: new[] { "cash_register_id", "signed_at" },
                filter: "document_kind = 'fiscal'");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipts_completed",
                schema: "fiskaly",
                table: "receipts",
                columns: new[] { "property_id", "signed_at" },
                filter: "record_state = 'completed' AND record_mode = 'finished'");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipts_flow",
                schema: "fiskaly",
                table: "receipts",
                columns: new[] { "property_id", "document_kind", "record_state" });

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipts_property_date",
                schema: "fiskaly",
                table: "receipts",
                columns: new[] { "property_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_receipts_reservation",
                schema: "fiskaly",
                table: "receipts",
                column: "reservation_public_id");

            migrationBuilder.CreateIndex(
                name: "ix_receipts_fiskaly_account_id",
                schema: "fiskaly",
                table: "receipts",
                column: "fiskaly_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_receipts_pdf_asset_id",
                schema: "fiskaly",
                table: "receipts",
                column: "pdf_asset_id");

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_receipts_ereceipt_uuid",
                schema: "fiskaly",
                table: "receipts",
                column: "e_receipt_uuid",
                unique: true,
                filter: "e_receipt_uuid IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_receipts_public_id",
                schema: "fiskaly",
                table: "receipts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_receipts_record",
                schema: "fiskaly",
                table: "receipts",
                column: "record_id",
                unique: true,
                filter: "record_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_fiskaly_vat_codes_active",
                schema: "fiskaly",
                table: "vat_codes",
                columns: new[] { "kind", "sort_order" },
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "uq_fiskaly_vat_codes_code",
                schema: "fiskaly",
                table: "vat_codes",
                column: "code",
                unique: true);

            // ─────────────────────────────────────────────────────────────────────────────
            // Oggetti che il modello EF non sa esprimere. Questa InitialCreate consolida
            // (2026-10-06) tutte le migration precedenti del modulo: qui sotto c'e' lo SQL a
            // mano che quelle contenevano e che vale ancora sullo schema finale. Gli UPDATE di
            // conversione dei dati (portavano righe esistenti da una forma all'altra) non sono
            // riportati: su un database nuovo non c'e' niente da convertire.
            // ─────────────────────────────────────────────────────────────────────────────

            // Chiusura cassa giornaliera (normativa sui corrispettivi telematici).
            migrationBuilder.Sql("""
CREATE VIEW fiskaly.v_daily_cash_closure AS
-- (1) incassato per forma di pagamento
SELECT r.property_id,
       r.cash_register_id,
       (r.signed_at AT TIME ZONE 'Europe/Rome')::date          AS fiscal_date,
       'payment'::text                                         AS breakdown_kind,
       p.payment_type::text                                    AS breakdown_key,
       NULL::numeric(5,2)                                      AS vat_rate,
       NULL::numeric(12,2)                                     AS taxable_amount,
       NULL::numeric(12,2)                                     AS vat_amount,
       sum(p.amount)                                           AS total_amount,
       count(DISTINCT r.id)                                    AS receipt_count
FROM fiskaly.receipts r
JOIN fiskaly.receipt_payments p ON p.receipt_id = r.id AND p.deleted_at IS NULL
WHERE r.deleted_at IS NULL
  AND r.record_state = 'completed'
  AND r.signed_at IS NOT NULL
GROUP BY r.property_id, r.cash_register_id, 3, p.payment_type

UNION ALL

-- (2) imponibile e imposta per aliquota
SELECT r.property_id,
       r.cash_register_id,
       (r.signed_at AT TIME ZONE 'Europe/Rome')::date,
       'vat'::text,
       COALESCE(v.code, l.vat_kind::text),
       l.vat_rate,
       sum(l.line_net),
       sum(l.vat_amount),
       sum(l.line_gross),
       count(DISTINCT r.id)
FROM fiskaly.receipts r
JOIN fiskaly.receipt_lines l ON l.receipt_id = r.id AND l.deleted_at IS NULL
LEFT JOIN fiskaly.vat_codes v ON v.id = l.vat_code_id
WHERE r.deleted_at IS NULL
  AND r.record_state = 'completed'
  AND r.signed_at IS NOT NULL
GROUP BY r.property_id, r.cash_register_id, 3, 5, l.vat_rate;
""");
            migrationBuilder.Sql("""
COMMENT ON VIEW fiskaly.v_daily_cash_closure IS
  'Chiusura cassa giornaliera per la normativa sui corrispettivi telematici. Due ripartizioni dello '
  'stesso incasso, distinte da breakdown_kind: payment (totale per forma di pagamento) e vat '
  '(imponibile e imposta per aliquota). Non sommare le due famiglie fra loro: sono due viste dello '
  'stesso importo, non due addendi. Data fiscale in fuso Europe/Rome.';
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // La vista dipende da receipts, receipt_payments, receipt_lines e vat_codes: va tolta prima.
            migrationBuilder.Sql("DROP VIEW IF EXISTS fiskaly.v_daily_cash_closure;");

            migrationBuilder.DropTable(
                name: "receipt_lines",
                schema: "fiskaly");

            migrationBuilder.DropTable(
                name: "receipt_logs",
                schema: "fiskaly");

            migrationBuilder.DropTable(
                name: "receipt_payments",
                schema: "fiskaly");

            migrationBuilder.DropTable(
                name: "receipt_sequences",
                schema: "fiskaly");

            migrationBuilder.DropTable(
                name: "vat_codes",
                schema: "fiskaly");

            migrationBuilder.DropTable(
                name: "receipts",
                schema: "fiskaly");

            migrationBuilder.DropTable(
                name: "cash_registers",
                schema: "fiskaly");

            migrationBuilder.DropTable(
                name: "media_assets",
                schema: "fiskaly");

            migrationBuilder.DropTable(
                name: "accounts",
                schema: "fiskaly");
        }
    }
}
