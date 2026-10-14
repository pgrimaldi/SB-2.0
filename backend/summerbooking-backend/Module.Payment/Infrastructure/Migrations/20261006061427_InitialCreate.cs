using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Payment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "payment");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:payment.charge_status", "pending,authorized,succeeded,failed,canceled,refunded,partially_refunded")
                .Annotation("Npgsql:Enum:payment.commission_exemption", "none,iframe_only,always")
                .Annotation("Npgsql:Enum:payment.connect_onboarding_state", "pending,restricted,active,rejected,disabled")
                .Annotation("Npgsql:Enum:payment.ledger_direction", "in,out")
                .Annotation("Npgsql:Enum:payment.ledger_entry_type", "deposit,payment,expense,refund,adjustment")
                .Annotation("Npgsql:Enum:payment.ledger_line_kind", "package,additional_item,service,agreement_voucher,surcharge,discount,unspecified")
                .Annotation("Npgsql:Enum:payment.ledger_source", "manual,online_gateway,payment_link,pos_ofsale,pos_teamsystem,wallet,agreement")
                .Annotation("Npgsql:Enum:payment.online_payment_type", "pay_now,advance_balance,pay_at_property")
                .Annotation("Npgsql:Enum:payment.payment_mode", "cash,credit_card,bank_transfer,other")
                .Annotation("Npgsql:Enum:payment.payment_request_origin", "booking_detail,booking_creation,unknown")
                .Annotation("Npgsql:Enum:payment.payment_request_status", "pending,paid,canceled,expired,failed")
                .Annotation("Npgsql:Enum:payment.pos_charge_status", "recorded,voided")
                .Annotation("Npgsql:Enum:payment.pos_system", "ofsale,teamsystem")
                .Annotation("Npgsql:Enum:payment.refund_reason", "requested_by_customer,booking_canceled,duplicate,fraudulent,other")
                .Annotation("Npgsql:Enum:payment.refund_status", "pending,succeeded,failed,canceled")
                .Annotation("Npgsql:Enum:payment.sales_channel", "redirect_checkout,iframe")
                .Annotation("Npgsql:Enum:payment.transaction_subject", "reservation,service_reservation")
                .Annotation("Npgsql:Enum:payment.wallet_kind", "prepaid_card,resale_credit")
                .Annotation("Npgsql:Enum:payment.wallet_movement_source", "top_up,service_consumption,resale_credit_earned,reservation_settlement,reversal,adjustment,expiry")
                .Annotation("Npgsql:Enum:payment.wallet_movement_type", "credit,debit")
                .Annotation("Npgsql:Enum:payment.wallet_status", "active,blocked,closed")
                .Annotation("Npgsql:Enum:payment.webhook_processing_status", "received,processed,ignored,failed")
                .Annotation("Npgsql:Enum:payment.webhook_provider", "stripe");

            migrationBuilder.CreateTable(
                name: "gateway_commission_settings",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento a cui si applica la commissione."),
                    online_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 6.00m, comment: "Percentuale trattenuta sulle prenotazioni concluse sul sito pubblico (fuori iframe)."),
                    iframe_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 6.00m, comment: "Percentuale trattenuta sulle prenotazioni concluse nel widget iframe su sito terzo."),
                    fixed_fee_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0.50m, comment: "Fee fissa in euro aggiunta alla commissione percentuale su ogni transazione online."),
                    exemption = table.Column<int>(type: "payment.commission_exemption", nullable: false, defaultValueSql: "'none'", comment: "Motivo di esenzione dalla commissione, esclusivo per stabilimento."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete alla cessazione dello stabilimento.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_gateway_commission_settings", x => x.id);
                },
                comment: "Tasso di commissione trattenuto dalla piattaforma sugli incassi online, per stabilimento e canale.");

            migrationBuilder.CreateTable(
                name: "gateway_connected_accounts",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento proprietario dell'account."),
                    stripe_account_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Id del connected account Stripe (acct_…), identificativo esterno opaco."),
                    onboarding_state = table.Column<int>(type: "payment.connect_onboarding_state", nullable: false, defaultValueSql: "'pending'", comment: "Stato di completamento dell'onboarding Connect lato Stripe, aggiornato dal webhook account.updated."),
                    charges_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Copia del flag Stripe account.charges_enabled."),
                    payouts_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Copia del flag Stripe account.payouts_enabled."),
                    connected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di collegamento dell'account."),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data di revoca del collegamento Connect (cessazione contratto o ricollegamento); distinta da deleted_at."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete alla cessazione dello stabilimento.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_gateway_connected_accounts", x => x.id);
                },
                comment: "Account Stripe Connect (connected account) del venditore, uno per stabilimento.");

            migrationBuilder.CreateTable(
                name: "ledger_entries",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, copiato cross-modulo (C4 righe payments[] dello scontrino; export contabile)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento che incassa."),
                    reservation_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione del movimento."),
                    agreement_line_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.booking_agreement_lines, copiato — nessuna FK cross-schema; valorizzato per addebiti reali su voucher hotel."),
                    entry_type = table.Column<int>(type: "payment.ledger_entry_type", nullable: false, defaultValueSql: "'payment'", comment: "Natura del movimento: acconto, incasso, spesa cliente, rimborso o rettifica."),
                    direction = table.Column<int>(type: "payment.ledger_direction", nullable: false, defaultValueSql: "'in'", comment: "Verso del denaro rispetto allo stabilimento; gli importi restano sempre positivi."),
                    payment_mode = table.Column<int>(type: "payment.payment_mode", nullable: true, defaultValueSql: "'other'", comment: "Modalità con cui è avvenuto il movimento; NULL sui movimenti legacy non normalizzabili."),
                    source = table.Column<int>(type: "payment.ledger_source", nullable: false, defaultValueSql: "'manual'", comment: "Da dove nasce il movimento (cassa manuale, gateway online, link, POS, borsellino, convenzione)."),
                    source_ref_id = table.Column<long>(type: "bigint", nullable: true, comment: "Id interno della riga di origine nello schema payment (charge, request link, pos charge, wallet transaction), tipo determinato da source; NULL se source = manual."),
                    gross_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, computedColumnSql: "net_amount + vat_amount", stored: true, comment: "Importo lordo del movimento, IVA inclusa — colonna generata (net_amount + vat_amount). Sola lettura: le rettifiche contabili sono un nuovo movimento con entry_kind = 'adjustment', mai un UPDATE dell'importo esistente."),
                    net_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Imponibile del movimento."),
                    vat_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Quota IVA del movimento."),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, comment: "Aliquota IVA % applicata; se il movimento copre aliquote miste è la prevalente, il dettaglio sta nelle Lines."),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "EUR", comment: "Valuta ISO 4217."),
                    customer_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente del movimento (riferimento vivo)."),
                    customer_name_snapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Nome cliente congelato al momento del movimento (fatto fiscale), accanto al riferimento vivo customer_public_id."),
                    customer_email_snapshot = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: true, comment: "Email cliente congelata al momento del movimento."),
                    customer_phone_snapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Telefono cliente congelato al momento del movimento."),
                    recorded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha registrato il movimento manuale (audit fiscale)."),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora contabile del movimento, decorrenza retention."),
                    stay_from = table.Column<DateOnly>(type: "date", nullable: true, comment: "Inizio del soggiorno/servizio, denormalizzato per export contabile e filtri stagionali."),
                    stay_to = table.Column<DateOnly>(type: "date", nullable: true, comment: "Fine del soggiorno/servizio, denormalizzato per export contabile e filtri stagionali."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità: append-only (una correzione è una nuova riga adjustment, sempre NULL).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_ledger_entries", x => x.id);
                    table.CheckConstraint("ck_payment_ledger_entries_gross_amount", "gross_amount > 0");
                },
                comment: "Movimento di incasso/acconto/spesa/rimborso/rettifica relativo a una prenotazione; append-only.");

            migrationBuilder.CreateTable(
                name: "pos_charges",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile restituito al sistema POS come identificativo della spesa; su un retry con lo stesso external_ref si restituisce lo stesso valore (idempotenza)."),
                    pos_system = table.Column<int>(type: "payment.pos_system", nullable: false, comment: "Sistema POS di origine, esclusivo per riga."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento a cui la spesa è addebitata."),
                    reservation_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione al cui conto la spesa confluisce."),
                    booking_number = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Numero prenotazione, denormalizzato per correlazione col legacy."),
                    external_ref = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Id del documento/scontrino sul POS di origine, chiave di deduplicazione."),
                    description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Nome del punto vendita / descrizione dello scontrino, testo libero come inviato dal POS (nessuna entità nostra corrispondente)."),
                    total_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Totale dello scontrino, IVA inclusa; deve coincidere con la somma delle righe entro 1 cent."),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "EUR", comment: "Valuta ISO 4217."),
                    status = table.Column<int>(type: "payment.pos_charge_status", nullable: false, defaultValueSql: "'recorded'", comment: "Stato dello scontrino POS nel gestionale: acquisito o annullato dal POS con un documento di storno."),
                    ledger_entry_ref = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di payment.ledger_entries — correlazione debole (non FK EF) verso la riga contabile generata dall'addebito, valorizzata a posteriori dall'handler dell'evento."),
                    charged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora dello scontrino sul POS, decorrenza retention."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità: append-only (concorre al fiscale, retention 10 anni), sempre NULL.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_pos_charges", x => x.id);
                    table.CheckConstraint("ck_payment_pos_charges_total_amount", "total_amount > 0");
                },
                comment: "Scontrino/addebito trasmesso da un POS esterno (OfSale o TeamSystem) e associato a una prenotazione dello stabilimento.");

            migrationBuilder.CreateTable(
                name: "wallet_accounts",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, esposto al cliente e copiato da C1 sulla riga servizio pagata col borsellino."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento presso cui vale il borsellino."),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente titolare del conto."),
                    wallet_kind = table.Column<int>(type: "payment.wallet_kind", nullable: false, defaultValueSql: "'prepaid_card'", comment: "Tipo di borsellino: tessera fisica ricaricabile o crediti da cessione posto."),
                    card_number = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: true, comment: "Numero stampato sulla tessera fisica, generato server-side; NULL per wallet_kind = ResaleCredit."),
                    balance = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Saldo corrente del conto, materializzato e mantenuto dai movimenti; può andare negativo per storni/rettifiche."),
                    lifetime_credited = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Totale storico accreditato, mai decrementato; distingue quanto maturato da quanto residuo."),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "EUR", comment: "Valuta ISO 4217 del saldo."),
                    status = table.Column<int>(type: "payment.wallet_status", nullable: false, defaultValueSql: "'active'", comment: "Stato operativo del conto; blocked/closed impediscono nuovi debiti."),
                    opened_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Apertura del conto."),
                    last_movement_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Ultimo movimento registrato, guida la retention per inattività (24 mesi)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete alla chiusura del conto.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_wallet_accounts", x => x.id);
                },
                comment: "Conto prepagato del cliente presso uno stabilimento: tessera fisica ricaricabile o crediti da cessione posto.");

            migrationBuilder.CreateTable(
                name: "webhook_events",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    provider = table.Column<int>(type: "payment.webhook_provider", nullable: false, comment: "Gateway che ha inviato l'evento; oggi solo stripe."),
                    external_event_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Id dell'evento presso il gateway (per Stripe: evt_...). Meta' della chiave di idempotenza."),
                    event_type = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Tipo di evento come dichiarato dal gateway, testo libero e non enum: il vocabolario lo decide Stripe e cambia senza il nostro deploy. Oggi ne sono gestiti due (payment_intent.succeeded, checkout.session.completed), ma ogni altro evento va comunque registrato."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties, se l'evento e' attribuibile a uno stabilimento — nullable perche' l'attribuzione avviene leggendo il payload e puo' non riuscire (es. eventi di piattaforma)."),
                    payload = table.Column<string>(type: "jsonb", nullable: false, comment: "Corpo dell'evento come ricevuto, per poterlo rielaborare senza richiederlo di nuovo al gateway."),
                    processing_status = table.Column<int>(type: "payment.webhook_processing_status", nullable: false, comment: "received (registrato), processed (effetti applicati), ignored (riconosciuto e volutamente non gestito), failed (da riprovare o esaminare)."),
                    attempt_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Numero di tentativi di elaborazione: distingue un fallimento isolato da uno ricorrente senza dover leggere i log."),
                    failure_reason = table.Column<string>(type: "text", nullable: true, comment: "Motivo dell'ultimo fallimento; NULL quando processing_status non e' failed."),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Istante di ricezione del webhook. Decorrenza della retention."),
                    processed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante in cui l'evento e' stato elaborato; NULL finche' e' received."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica (cambio di stato dell'elaborazione).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_webhook_events", x => x.id);
                },
                comment: "Registro degli eventi webhook ricevuti dai gateway di pagamento. La chiave di idempotenza (provider, external_event_id) e' un vincolo di schema: l'inserimento e' un INSERT ... ON CONFLICT DO NOTHING, la corsa fra due webhook concorrenti la arbitra PostgreSQL e non l'applicazione.");

            migrationBuilder.CreateTable(
                name: "customer_gateway_profiles",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    customer_public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente titolare del Customer Stripe."),
                    connected_account_id = table.Column<long>(type: "bigint", nullable: false, comment: "Connected account su cui vive il Customer Stripe (il cliente ha un cus_… per stabilimento)."),
                    stripe_customer_id = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Id del Customer Stripe (cus_…) creato sul connected account, identificativo esterno opaco."),
                    default_payment_method_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Id del PaymentMethod Stripe (pm_…) marcato come predefinito dal cliente, per la pre-selezione al checkout."),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Ultimo utilizzo di una carta salvata di questo profilo, guida la retention per inattività."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete alla chiusura account o alla retention (24 mesi di inattività).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_customer_gateway_profiles", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_customer_gateway_profiles_connected_account",
                        column: x => x.connected_account_id,
                        principalSchema: "payment",
                        principalTable: "gateway_connected_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Mappatura cliente finale → Stripe Customer, per connected account: consente di riusare le carte salvate presso quello stabilimento.");

            migrationBuilder.CreateTable(
                name: "transaction_charges",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, copiato cross-modulo (C4 autoreceipt; URL gestionale)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento che incassa."),
                    connected_account_id = table.Column<long>(type: "bigint", nullable: true, comment: "Connected account su cui è avvenuto l'incasso; NULL per le righe paga-in-struttura."),
                    stripe_account_id_snapshot = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Id acct_… congelato al momento dell'incasso, accanto alla FK viva connected_account_id (snapshot su entità mutabile)."),
                    reservation_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione pagata."),
                    subject_kind = table.Column<int>(type: "payment.transaction_subject", nullable: true, defaultValueSql: "'reservation'", comment: "Cosa si sta pagando: prenotazione con postazioni o di soli servizi."),
                    payment_type = table.Column<int>(type: "payment.online_payment_type", nullable: false, defaultValueSql: "'pay_now'", comment: "Modalità di pagamento online scelta: tutto subito, acconto+saldo, o paga in struttura."),
                    sales_channel = table.Column<int>(type: "payment.sales_channel", nullable: false, defaultValueSql: "'redirect_checkout'", comment: "Canale di partenza del pagamento, determina il tasso di commissione applicato."),
                    status = table.Column<int>(type: "payment.charge_status", nullable: false, defaultValueSql: "'pending'", comment: "Stato interno della transazione, derivato dallo stato Stripe."),
                    stripe_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Stato grezzo del PaymentIntent Stripe conservato verbatim, snapshot per diagnostica/riconciliazione."),
                    stripe_payment_intent_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Id del PaymentIntent Stripe; NULL per paga-in-struttura. 255 (non 40): il legacy ha dimostrato che questo campo puo' contenere anche un Checkout Session id (\"cs_live_...\", fino a 66+ caratteri) invece di un vero PaymentIntent id (\"pi_...\") - bug reale osservato il 2026-09-15, 190 righe scartate a NULL dalla migrazione perche' il varchar(40) le rifiutava."),
                    stripe_charge_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Id del Charge Stripe (ch_…) associato al PaymentIntent, usato per i rimborsi. 255: stessa famiglia di id Stripe di StripePaymentIntentId sopra, stesso margine per coerenza."),
                    gross_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, computedColumnSql: "paid_amount + pending_amount + hold_amount", stored: true, comment: "Importo lordo dell'operazione (quanto paga il cliente) — colonna generata (paid_amount + pending_amount + hold_amount). Sola lettura: la tabella e' append/stato e i rimborsi sono righe di payment_transaction_refunds, non modifiche di questa riga."),
                    paid_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Importo effettivamente incassato (catturato)."),
                    hold_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Importo autorizzato con cattura manuale, fondi bloccati non ancora incassati."),
                    pending_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Residuo ancora da pagare, senza fondi bloccati."),
                    application_fee_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Commissione di piattaforma trattenuta (Stripe Connect), calcolata server-side."),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "EUR", comment: "Valuta ISO 4217 dell'operazione."),
                    customer_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.customer_customers, copiato — nessuna FK cross-schema; cliente pagante."),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora dell'evento gateway (creazione PaymentIntent); decorrenza retention."),
                    captured_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando l'hold è stato catturato."),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando la transazione è stata annullata."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità: la tabella è append/stato, non si soft-elimina (scrittura contabile, retention 10 anni).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_transaction_charges", x => x.id);
                    table.ForeignKey(
                        name: "fk_payment_transaction_charges_connected_account",
                        column: x => x.connected_account_id,
                        principalSchema: "payment",
                        principalTable: "gateway_connected_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Livello gateway dell'incasso online: un PaymentIntent Stripe (o una registrazione paga-in-struttura).");

            migrationBuilder.CreateTable(
                name: "ledger_entry_lines",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    entry_id = table.Column<long>(type: "bigint", nullable: false, comment: "Movimento di pagamento a cui la riga appartiene."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'export contabile e i report IVA per riga senza join (docs/05-ottimizzazioni-db.md 10)."),
                    line_kind = table.Column<int>(type: "payment.ledger_line_kind", nullable: false, defaultValueSql: "'unspecified'", comment: "Tipo di articolo a cui è attribuita la quota."),
                    description_snapshot = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Descrizione dell'articolo congelata al momento del movimento; il nome vivo si legge da C1 quando reservation_line_public_id è valorizzato."),
                    reservation_line_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.booking_lines, copiato — nessuna FK cross-schema; riga prenotazione a cui la quota si riferisce, quando risolvibile."),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Quantità dell'articolo (pacchetti 1; servizi count)."),
                    net_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Imponibile della quota di movimento attribuita alla riga."),
                    vat_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Quota IVA della riga."),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, comment: "Aliquota IVA % della riga."),
                    is_allocated = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "True = la quota è una ripartizione proporzionale del movimento (stima), non un importo pagato distintamente per l'articolo."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità: append-only con la testata, sempre NULL.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_ledger_entry_lines", x => x.id);
                    table.ForeignKey(
                        name: "fk_ledger_entry_lines_ledger_entries_entry_id",
                        column: x => x.entry_id,
                        principalSchema: "payment",
                        principalTable: "ledger_entries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Quota di un movimento di pagamento attribuita a un articolo (pacchetto, servizio, voucher, ...), per l'export contabile e i report IVA per riga.");

            migrationBuilder.CreateTable(
                name: "pos_charge_items",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    charge_id = table.Column<long>(type: "bigint", nullable: false, comment: "Scontrino POS a cui la riga appartiene."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per i report IVA degli addebiti POS e il report per articolo senza join (docs/05-ottimizzazioni-db.md 10)."),
                    external_line_ref = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id della riga sul POS di origine, quando fornito (solo OfSale)."),
                    sale_item_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.service_sale_items, copiato — nessuna FK cross-schema; articolo di catalogo corrispondente, quando mappabile."),
                    description_snapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false, comment: "Descrizione del prodotto congelata al momento della trasmissione dal POS; il POS non modifica retroattivamente le righe trasmesse."),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Quantità."),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Prezzo unitario, IVA inclusa."),
                    line_total = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Totale della riga, IVA inclusa; la somma delle righe deve coincidere con il totale scontrino entro 1 cent."),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: false, comment: "Aliquota IVA % della riga."),
                    net_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, computedColumnSql: "round(line_total / (1 + vat_rate / 100), 2)", stored: true, comment: "Imponibile scorporato — colonna generata: round(line_total / (1 + vat_rate/100), 2). Sola lettura: lo storno di un addebito POS porta status = 'voided' e un documento di storno a parte, mai una modifica di questi importi."),
                    vat_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, computedColumnSql: "line_total - round(line_total / (1 + vat_rate / 100), 2)", stored: true, comment: "Quota IVA scorporata — colonna generata (line_total - net_amount). La formula ripete lo scorporo invece di referenziare net_amount: PostgreSQL non ammette che una colonna generata ne referenzi un'altra generata."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità: append-only con lo scontrino, sempre NULL."),
                    pos_charge_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_pos_charge_items", x => x.id);
                    table.CheckConstraint("ck_payment_pos_charge_items_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_pos_charge_items_pos_charges_pos_charge_id",
                        column: x => x.pos_charge_id,
                        principalSchema: "payment",
                        principalTable: "pos_charges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Prodotto/riga di uno scontrino POS: descrizione, quantità, prezzo, aliquota IVA, totale, congelati al momento della trasmissione.");

            migrationBuilder.CreateTable(
                name: "wallet_transactions",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, copiato da C1 (riga servizio pagata col borsellino) e da C4 (pagamento VOUCHER/OUTSTANDING dello scontrino)."),
                    wallet_id = table.Column<long>(type: "bigint", nullable: false, comment: "Conto borsellino movimentato."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato dal conto — nessuna FK cross-schema; denormalizzato per gli indici."),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di identity.customer_customers, copiato dal conto — nessuna FK cross-schema; denormalizzato per gli indici."),
                    movement_type = table.Column<int>(type: "payment.wallet_movement_type", nullable: false, comment: "Verso del movimento: credit incrementa balance/lifetime_credited, debit li decrementa."),
                    source = table.Column<int>(type: "payment.wallet_movement_source", nullable: false, defaultValueSql: "'top_up'", comment: "Causale del movimento (ricarica, consumo, accredito sharing, saldo prenotazione, storno, rettifica, scadenza)."),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Importo del movimento, sempre positivo; il verso lo dà movement_type."),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "EUR", comment: "Valuta ISO 4217, allineata al conto."),
                    balance_after = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Saldo del conto dopo questo movimento (running balance), per l'estratto conto e la verifica di integrità."),
                    reservation_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione a cui il movimento è collegato."),
                    resale_offer_ref = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.booking_resale_offers, copiato — nessuna FK cross-schema; valorizzato per source = ResaleCreditEarned."),
                    reverses_transaction_id = table.Column<long>(type: "bigint", nullable: true, comment: "Movimento stornato, per source = Reversal."),
                    ledger_entry_ref = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di payment.ledger_entries — correlazione debole (non FK EF) verso la riga contabile generata quando il movimento salda una prenotazione: valorizzata a posteriori dall'handler di PaymentRecorded, non in un'unica transazione con l'inserimento del movimento."),
                    operated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha registrato il movimento (audit)."),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora del movimento, decorrenza retention."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità: append-only (una correzione è un movimento reversal, sempre NULL).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_wallet_transactions", x => x.id);
                    table.CheckConstraint("ck_payment_wallet_transactions_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payment_wallet_transactions_reverses_transaction",
                        column: x => x.reverses_transaction_id,
                        principalSchema: "payment",
                        principalTable: "wallet_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_payment_wallet_transactions_wallet",
                        column: x => x.wallet_id,
                        principalSchema: "payment",
                        principalTable: "wallet_accounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Movimento del borsellino (ricarica o consumo); append-only, il saldo del conto è la somma di queste righe.");

            migrationBuilder.CreateTable(
                name: "request_links",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, esposto nell'URL post-pagamento e nel messaggio al cliente."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; stabilimento."),
                    reservation_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; il link può precedere la prenotazione."),
                    status = table.Column<int>(type: "payment.payment_request_status", nullable: false, defaultValueSql: "'pending'", comment: "Stato del link: creato, pagato, revocato, scaduto o fallito."),
                    origin = table.Column<int>(type: "payment.payment_request_origin", nullable: false, defaultValueSql: "'booking_detail'", comment: "Da dove è stato generato il link, cambia testi/redirect."),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Importo richiesto dal link."),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "EUR", comment: "Valuta ISO 4217."),
                    cancel_reservation_on_expiry = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se true, alla scadenza/annullamento del link va cancellata anche la prenotazione associata."),
                    stripe_payment_link_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Id del Payment Link / Checkout Session Stripe, valorizzato appena Stripe risponde."),
                    charge_id = table.Column<long>(type: "bigint", nullable: true, comment: "Transazione generata dal pagamento del link, valorizzata a status = paid."),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Scadenza del link; NULL se senza scadenza."),
                    paid_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando il link è stato pagato."),
                    canceled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando il link è stato revocato."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha generato il link."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità: il fatto segue la transazione (retention 10 anni).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_request_links", x => x.id);
                    table.CheckConstraint("ck_payment_request_links_amount", "amount > 0");
                    table.ForeignKey(
                        name: "fk_payment_request_links_charge",
                        column: x => x.charge_id,
                        principalSchema: "payment",
                        principalTable: "transaction_charges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Link di pagamento Stripe inviato al cliente per saldare o confermare una prenotazione.");

            migrationBuilder.CreateTable(
                name: "transaction_refunds",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, per il gestionale e per eventuale riferimento da C4/C6."),
                    charge_id = table.Column<long>(type: "bigint", nullable: false, comment: "Transazione online rimborsata."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato dalla transazione — nessuna FK cross-schema; denormalizzato per indici ed export."),
                    reservation_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.booking_reservations, copiato — nessuna FK cross-schema; prenotazione a cui il rimborso si riferisce."),
                    status = table.Column<int>(type: "payment.refund_status", nullable: false, defaultValueSql: "'pending'", comment: "Esito del rimborso."),
                    reason = table.Column<int>(type: "payment.refund_reason", nullable: false, defaultValueSql: "'booking_canceled'", comment: "Motivo del rimborso, per report e per il parametro reason di Stripe."),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Importo rimborsato in euro; può essere inferiore a original_amount (rimborso parziale)."),
                    original_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Importo pieno della transazione originaria al momento del rimborso (snapshot)."),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "EUR", comment: "Valuta ISO 4217, allineata alla transazione."),
                    stripe_refund_id = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Id dell'oggetto Refund Stripe (re_…), verbatim; correla al webhook charge.refunded."),
                    failure_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Messaggio/codice di fallimento Stripe quando status = failed."),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha disposto il rimborso (audit fiscale); NULL se automatico."),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora del rimborso, decorrenza retention."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità: append-only (scrittura contabile), sempre NULL.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_transaction_refunds", x => x.id);
                    table.CheckConstraint("ck_payment_transaction_refunds_amount", "amount > 0 AND amount <= original_amount");
                    table.ForeignKey(
                        name: "fk_payment_transaction_refunds_charge",
                        column: x => x.charge_id,
                        principalSchema: "payment",
                        principalTable: "transaction_charges",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Un rimborso Stripe su una transazione online, con supporto al rimborso parziale.");

            migrationBuilder.CreateTable(
                name: "wallet_transaction_items",
                schema: "payment",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    transaction_id = table.Column<long>(type: "bigint", nullable: false, comment: "Movimento di debito a cui l'articolo appartiene."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema. Denormalizzato per l'eventuale report per stabilimento senza join (docs/05-ottimizzazioni-db.md 10)."),
                    sale_item_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.service_sale_items, copiato — nessuna FK cross-schema; articolo di catalogo consumato, quando risolvibile."),
                    description_snapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false, comment: "Nome dell'articolo congelato al momento del consumo; il nome vivo si legge da C2 quando sale_item_public_id è valorizzato."),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Quantità consumata."),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Prezzo unitario dell'articolo al momento del consumo."),
                    variants_description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Varianti scelte in forma leggibile congelata; testo libero della riga-scontrino, non uno snapshot di un'entità modellata."),
                    variants_surcharge = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Somma dei sovrapprezzi delle varianti su questa riga, inclusa in line_total."),
                    line_total = table.Column<decimal>(type: "numeric(12,2)", nullable: false, computedColumnSql: "quantity * unit_price + variants_surcharge", stored: true, comment: "Totale della riga — colonna generata (quantity * unit_price + variants_surcharge). Sola lettura: le rettifiche sono un nuovo movimento con source = 'adjustment'. Chiude anche un bug storico del legacy, dove il totale arrivava spesso null o incoerente con le sue componenti."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità: append-only con il movimento, sempre NULL."),
                    wallet_transaction_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_payment_wallet_transaction_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_wallet_transaction_items_wallet_transactions_wallet_transac",
                        column: x => x.wallet_transaction_id,
                        principalSchema: "payment",
                        principalTable: "wallet_transactions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Articolo consumato pagato con un movimento di debito del borsellino (es. una consumazione al bar), congelato al momento del consumo.");

            migrationBuilder.CreateIndex(
                name: "ix_payment_customer_gateway_profiles_account",
                schema: "payment",
                table: "customer_gateway_profiles",
                column: "connected_account_id");

            migrationBuilder.CreateIndex(
                name: "uq_payment_customer_gateway_profiles_customer_account",
                schema: "payment",
                table: "customer_gateway_profiles",
                columns: new[] { "customer_public_id", "connected_account_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_payment_customer_gateway_profiles_stripe_customer",
                schema: "payment",
                table: "customer_gateway_profiles",
                column: "stripe_customer_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_payment_gateway_commission_settings_property",
                schema: "payment",
                table: "gateway_commission_settings",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_gateway_connected_accounts_onboarding",
                schema: "payment",
                table: "gateway_connected_accounts",
                column: "onboarding_state",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_payment_gateway_connected_accounts_property",
                schema: "payment",
                table: "gateway_connected_accounts",
                column: "property_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_payment_gateway_connected_accounts_stripe_account",
                schema: "payment",
                table: "gateway_connected_accounts",
                column: "stripe_account_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_ledger_entries_agreement_line",
                schema: "payment",
                table: "ledger_entries",
                column: "agreement_line_public_id",
                filter: "agreement_line_public_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_ledger_entries_property_date",
                schema: "payment",
                table: "ledger_entries",
                columns: new[] { "property_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_ledger_entries_property_mode_date",
                schema: "payment",
                table: "ledger_entries",
                columns: new[] { "property_id", "payment_mode", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_ledger_entries_reservation",
                schema: "payment",
                table: "ledger_entries",
                columns: new[] { "reservation_public_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_ledger_entries_source_ref",
                schema: "payment",
                table: "ledger_entries",
                columns: new[] { "source", "source_ref_id" });

            migrationBuilder.CreateIndex(
                name: "uq_payment_ledger_entries_public_id",
                schema: "payment",
                table: "ledger_entries",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_ledger_entry_lines_entry",
                schema: "payment",
                table: "ledger_entry_lines",
                column: "entry_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_ledger_entry_lines_property",
                schema: "payment",
                table: "ledger_entry_lines",
                columns: new[] { "property_id", "vat_rate" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_ledger_entry_lines_reservation_line",
                schema: "payment",
                table: "ledger_entry_lines",
                column: "reservation_line_public_id",
                filter: "reservation_line_public_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_ledger_entry_lines_vat",
                schema: "payment",
                table: "ledger_entry_lines",
                column: "vat_rate");

            migrationBuilder.CreateIndex(
                name: "ix_payment_pos_charge_items_charge",
                schema: "payment",
                table: "pos_charge_items",
                column: "charge_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_pos_charge_items_property",
                schema: "payment",
                table: "pos_charge_items",
                columns: new[] { "property_id", "vat_rate" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_pos_charge_items_sale_item",
                schema: "payment",
                table: "pos_charge_items",
                column: "sale_item_public_id",
                filter: "sale_item_public_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_pos_charge_items_vat",
                schema: "payment",
                table: "pos_charge_items",
                column: "vat_rate");

            migrationBuilder.CreateIndex(
                name: "ix_pos_charge_items_pos_charge_id",
                schema: "payment",
                table: "pos_charge_items",
                column: "pos_charge_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_pos_charges_booking_number",
                schema: "payment",
                table: "pos_charges",
                column: "booking_number");

            migrationBuilder.CreateIndex(
                name: "ix_payment_pos_charges_property_date",
                schema: "payment",
                table: "pos_charges",
                columns: new[] { "property_id", "charged_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_pos_charges_reservation",
                schema: "payment",
                table: "pos_charges",
                column: "reservation_public_id");

            migrationBuilder.CreateIndex(
                name: "uq_payment_pos_charges_external_ref",
                schema: "payment",
                table: "pos_charges",
                columns: new[] { "pos_system", "property_id", "external_ref" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_payment_pos_charges_public_id",
                schema: "payment",
                table: "pos_charges",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_request_links_expiry",
                schema: "payment",
                table: "request_links",
                column: "expires_at",
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "ix_payment_request_links_property_status",
                schema: "payment",
                table: "request_links",
                columns: new[] { "property_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_request_links_reservation",
                schema: "payment",
                table: "request_links",
                column: "reservation_public_id");

            migrationBuilder.CreateIndex(
                name: "ix_request_links_charge_id",
                schema: "payment",
                table: "request_links",
                column: "charge_id");

            migrationBuilder.CreateIndex(
                name: "uq_payment_request_links_public_id",
                schema: "payment",
                table: "request_links",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_payment_request_links_stripe_link",
                schema: "payment",
                table: "request_links",
                column: "stripe_payment_link_id",
                unique: true,
                filter: "stripe_payment_link_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_transaction_charges_connected_account",
                schema: "payment",
                table: "transaction_charges",
                column: "connected_account_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_transaction_charges_property_date",
                schema: "payment",
                table: "transaction_charges",
                columns: new[] { "property_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_transaction_charges_reservation",
                schema: "payment",
                table: "transaction_charges",
                column: "reservation_public_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_transaction_charges_status",
                schema: "payment",
                table: "transaction_charges",
                columns: new[] { "property_id", "status" },
                filter: "status IN ('pending','authorized')");

            migrationBuilder.CreateIndex(
                name: "uq_payment_transaction_charges_payment_intent",
                schema: "payment",
                table: "transaction_charges",
                column: "stripe_payment_intent_id",
                unique: true,
                filter: "stripe_payment_intent_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_payment_transaction_charges_public_id",
                schema: "payment",
                table: "transaction_charges",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_payment_transaction_refunds_charge",
                schema: "payment",
                table: "transaction_refunds",
                column: "charge_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_transaction_refunds_pending",
                schema: "payment",
                table: "transaction_refunds",
                column: "status",
                filter: "status = 'pending'");

            migrationBuilder.CreateIndex(
                name: "ix_payment_transaction_refunds_property_date",
                schema: "payment",
                table: "transaction_refunds",
                columns: new[] { "property_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "uq_payment_transaction_refunds_public_id",
                schema: "payment",
                table: "transaction_refunds",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_payment_transaction_refunds_stripe_refund",
                schema: "payment",
                table: "transaction_refunds",
                column: "stripe_refund_id",
                unique: true,
                filter: "stripe_refund_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_wallet_accounts_customer",
                schema: "payment",
                table: "wallet_accounts",
                column: "customer_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_wallet_accounts_nonzero",
                schema: "payment",
                table: "wallet_accounts",
                column: "property_id",
                filter: "balance <> 0 AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_payment_wallet_accounts_card_number",
                schema: "payment",
                table: "wallet_accounts",
                columns: new[] { "property_id", "card_number" },
                unique: true,
                filter: "card_number IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_payment_wallet_accounts_public_id",
                schema: "payment",
                table: "wallet_accounts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_payment_wallet_accounts_scope",
                schema: "payment",
                table: "wallet_accounts",
                columns: new[] { "property_id", "customer_id", "wallet_kind" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_wallet_transaction_items_property",
                schema: "payment",
                table: "wallet_transaction_items",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_wallet_transaction_items_sale_item",
                schema: "payment",
                table: "wallet_transaction_items",
                column: "sale_item_public_id",
                filter: "sale_item_public_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_wallet_transaction_items_transaction",
                schema: "payment",
                table: "wallet_transaction_items",
                column: "transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_wallet_transaction_items_wallet_transaction_id",
                schema: "payment",
                table: "wallet_transaction_items",
                column: "wallet_transaction_id");

            migrationBuilder.CreateIndex(
                name: "ix_payment_wallet_transactions_property_date",
                schema: "payment",
                table: "wallet_transactions",
                columns: new[] { "property_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_payment_wallet_transactions_reservation",
                schema: "payment",
                table: "wallet_transactions",
                column: "reservation_public_id",
                filter: "reservation_public_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_payment_wallet_transactions_wallet_date",
                schema: "payment",
                table: "wallet_transactions",
                columns: new[] { "wallet_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_wallet_transactions_reverses_transaction_id",
                schema: "payment",
                table: "wallet_transactions",
                column: "reverses_transaction_id");

            migrationBuilder.CreateIndex(
                name: "uq_payment_wallet_transactions_public_id",
                schema: "payment",
                table: "wallet_transactions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "brin_payment_webhook_events_created_at",
                schema: "payment",
                table: "webhook_events",
                column: "created_at")
                .Annotation("Npgsql:IndexMethod", "brin");

            migrationBuilder.CreateIndex(
                name: "ix_payment_webhook_events_pending",
                schema: "payment",
                table: "webhook_events",
                column: "received_at",
                filter: "processing_status IN ('received', 'failed')");

            migrationBuilder.CreateIndex(
                name: "ix_payment_webhook_events_property",
                schema: "payment",
                table: "webhook_events",
                columns: new[] { "property_id", "received_at" },
                filter: "property_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_payment_webhook_events_external",
                schema: "payment",
                table: "webhook_events",
                columns: new[] { "provider", "external_event_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "customer_gateway_profiles",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "gateway_commission_settings",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "ledger_entry_lines",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "pos_charge_items",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "request_links",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "transaction_refunds",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "wallet_transaction_items",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "webhook_events",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "ledger_entries",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "pos_charges",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "transaction_charges",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "wallet_transactions",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "gateway_connected_accounts",
                schema: "payment");

            migrationBuilder.DropTable(
                name: "wallet_accounts",
                schema: "payment");
        }
    }
}
