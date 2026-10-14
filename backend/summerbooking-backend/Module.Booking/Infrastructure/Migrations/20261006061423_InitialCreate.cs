using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Booking.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "booking");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:booking.additional_item_date_scope", "whole_stay,specific_dates")
                .Annotation("Npgsql:Enum:booking.additional_item_source", "price_list,inventory")
                .Annotation("Npgsql:Enum:booking.agreement_line_type", "regular,voucher")
                .Annotation("Npgsql:Enum:booking.booking_channel", "customer,manager")
                .Annotation("Npgsql:Enum:booking.consumption_source", "reservation_line,service_line,additional_item,manual_hold,resale")
                .Annotation("Npgsql:Enum:booking.consumption_state", "held,confirmed,released")
                .Annotation("Npgsql:Enum:booking.day_period", "full_day,morning,afternoon")
                .Annotation("Npgsql:Enum:booking.discount_channel", "any,manager_only,online_only")
                .Annotation("Npgsql:Enum:booking.discount_origin", "code_entered,auto_applied")
                .Annotation("Npgsql:Enum:booking.discount_scope", "all,custom,without_custom")
                .Annotation("Npgsql:Enum:booking.discount_type", "fixed,percentage")
                .Annotation("Npgsql:Enum:booking.hold_kind", "operational,checkout")
                .Annotation("Npgsql:Enum:booking.hold_status", "active,released,expired,converted")
                .Annotation("Npgsql:Enum:booking.listing_type", "standard,agreement_price_list,agreement_voucher")
                .Annotation("Npgsql:Enum:booking.online_confirmation_state", "not_required,pending,confirmed")
                .Annotation("Npgsql:Enum:booking.participant_role", "viewer")
                .Annotation("Npgsql:Enum:booking.payment_mode", "cash,credit_card,bank_transfer,other")
                .Annotation("Npgsql:Enum:booking.resale_credit_calc", "fixed_amount,daily_percentage")
                .Annotation("Npgsql:Enum:booking.resale_credit_entry", "earned,reversed,adjustment")
                .Annotation("Npgsql:Enum:booking.resale_mode", "absence,sharing")
                .Annotation("Npgsql:Enum:booking.resale_offer_state", "available,reassigned,withdrawn")
                .Annotation("Npgsql:Enum:booking.resale_origin", "absence,sharing,absence_as_sharing")
                .Annotation("Npgsql:Enum:booking.resale_scope", "seasonal_workers,all_articles")
                .Annotation("Npgsql:Enum:booking.reservation_kind", "spot,service")
                .Annotation("Npgsql:Enum:booking.reservation_price_flag", "suggested_price_accepted,recalculated,manually_edited")
                .Annotation("Npgsql:Enum:booking.reservation_status", "pending,confirmed,cancelled,no_show")
                .Annotation("Npgsql:Enum:booking.resource_type", "umbrella,sunbed,sunbed_xl,cabin,special_cabin,deckchair,chair,tent,parking,ticket,area_seat,bar_service,normal_service,hourly_service,inventory_item")
                .Annotation("Npgsql:Enum:booking.service_category", "bar,normal,hourly,additional");

            migrationBuilder.CreateTable(
                name: "groups",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties (stabilimento proprietario del gruppo), copiato — nessuna FK cross-schema"),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Nome del gruppo mostrato in UI"),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users (operatore che ha creato il gruppo, dal token), copiato — nessuna FK cross-schema"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione del gruppo"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica del gruppo"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete del gruppo (cancellando il gruppo si soft-eliminano anche i membri)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_groups", x => x.id);
                },
                comment: "Raggruppamento manuale di prenotazioni distinte sotto un'etichetta scelta dall'operatore (comitiva, famiglia su più ombrelloni); non è la prenotazione multipla.");

            migrationBuilder.CreateTable(
                name: "number_sequences",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema"),
                    number_prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, comment: "Prefisso stabile e immutabile del numero prenotazione per la property (di norma il suo id legacy)"),
                    last_sequence = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L, comment: "Ultimo progressivo assegnato per la property; il prossimo numero è last_sequence + 1, incrementato sotto lock FOR UPDATE"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Presente per uniformità di schema, non usato: tabella di registro/contatore, non soggetta a soft-delete")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_number_sequences", x => x.id);
                },
                comment: "Contatore progressivo del numero prenotazione per stabilimento (1 riga per property); booking_number si compone come number_prefix || '-' || lpad(last_sequence, 10, '0').");

            migrationBuilder.CreateTable(
                name: "resale_settings",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema"),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "La feature rivendita/sharing è attiva per lo stabilimento"),
                    mode = table.Column<int>(type: "booking.resale_mode", nullable: false, defaultValueSql: "'absence'", comment: "Come viene gestita la cessione di un giorno: solo assenza pianificata senza credito, o sharing con credito al titolare"),
                    applies_to = table.Column<int[]>(type: "booking.resale_scope[]", nullable: false, defaultValueSql: "'{}'", comment: "Estensioni della rivendita: consentita per pass stagionali, estesa a tutte le postazioni del pacchetto"),
                    both_options_disabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Disabilita entrambe le modalità (semantica esatta da riverificare col committente, migrato as-is)"),
                    credit_units_per_day = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Quanti crediti simbolici matura il titolare per ogni giorno ceduto e ripreso"),
                    credit_calc = table.Column<int>(type: "booking.resale_credit_calc", nullable: false, defaultValueSql: "'fixed_amount'", comment: "Come si calcola il valore del credito per giorno ceduto: importo fisso o percentuale della tariffa giornaliera"),
                    credit_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Valore del credito: importo € se credit_calc = fixed_amount, percentuale 0-100 se daily_percentage"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della configurazione"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della configurazione"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della configurazione")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_resale_settings", x => x.id);
                },
                comment: "Configurazione per stabilimento della feature rivendita/sharing del posto: guida il comportamento di booking_resale_offers e booking_resale_credit_ledger.");

            migrationBuilder.CreateTable(
                name: "reservations",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, copiato cross-modulo da C3/C4/C6."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; discriminatore tenant."),
                    bathhouse_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.bathhouses, copiato — NULL = vetrina di default della property."),
                    booking_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "Numero prenotazione generato server-side da booking_number_sequences, univoco fra righe vive."),
                    reservation_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, comment: "Codice pubblico (URL/QR) generato con RNG sicuro, univoco fra righe vive."),
                    reservation_kind = table.Column<int>(type: "booking.reservation_kind", nullable: false, comment: "spot (con postazioni) o service (soli servizi)."),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.customers, copiato — riferimento vivo, nessuna FK cross-schema."),
                    booking_channel = table.Column<int>(type: "booking.booking_channel", nullable: false, comment: "Origine della prenotazione: customer (online) o manager (gestionale). Immutabile dopo la creazione."),
                    reservation_status = table.Column<int>(type: "booking.reservation_status", nullable: false, comment: "Stato del ciclo di vita, separato da soft-delete e da pagamento."),
                    online_confirmation_state = table.Column<int>(type: "booking.online_confirmation_state", nullable: false, defaultValueSql: "'not_required'", comment: "Stato del flusso di conferma online via reservation_code."),
                    operator_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato dal token JWT — operatore che ha creato la prenotazione, NULL se online."),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — ultimo operatore che ha modificato la prenotazione."),
                    agreement_partner_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.agreement_partners, copiato — valorizzato solo per reservation_kind = spot."),
                    listing_type = table.Column<int>(type: "booking.listing_type", nullable: false, defaultValueSql: "'standard'", comment: "Tipo di listino applicato: standard, listino convenzione o voucher convenzione."),
                    has_seasonal_pass = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Prenotazione stagionale / abbonamento; false per reservation_kind = service."),
                    is_split = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Prenotazione split-location (derivabile dalle righe, tenuto per compatibilità di lettura)."),
                    price_flags = table.Column<int[]>(type: "booking.reservation_price_flag[]", nullable: false, defaultValueSql: "'{}'", comment: "Come si è formato il prezzo finale (accettato suggerito / ricalcolato / forzato a mano) — flag non esclusivi."),
                    pay_now = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Il cliente ha scelto di pagare subito online al checkout (canale customer)."),
                    is_order_filled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Riepilogo evasione ordini bar/servizio collegati; dettaglio per riga su booking_service_lines."),
                    note = table.Column<string>(type: "text", nullable: true, comment: "Nota libera dell'operatore sulla prenotazione."),
                    customer_tags = table.Column<List<string>>(type: "text[]", nullable: false, defaultValueSql: "'{}'", comment: "Tag/icone cliente selezionati nel riepilogo (presentazione, vocabolario chiuso lato UI)."),
                    stay_from = table.Column<DateOnly>(type: "date", nullable: true, comment: "Data minima dell'intervallo complessivo, denormalizzata dalle righe per i filtri di agenda."),
                    stay_to = table.Column<DateOnly>(type: "date", nullable: true, comment: "Data massima dell'intervallo complessivo, denormalizzata dalle righe per i filtri di agenda."),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "EUR", comment: "Valuta ISO 4217 degli importi della prenotazione."),
                    subtotal = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Imponibile prima di sconti e supplementi, somma delle righe."),
                    discount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Totale sconti/voucher applicati (valore assoluto negativo o 0); dettaglio su booking_discounts."),
                    surcharge = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Supplemento/sovrapprezzo (commissione di servizio, extra manuale)."),
                    total = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Totale dovuto = subtotal + surcharge + discount; calcolato dal motore prezzo server-side."),
                    price_override = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo finale forzato manualmente dall'operatore; NULL = nessun override."),
                    deposit = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Importo acconto/caparra corrente (riepilogo); i singoli acconti sono movimenti C3."),
                    paid = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Totale già incassato, proiezione dagli eventi di pagamento di C3."),
                    balance_due = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Residuo da incassare = total - paid; può essere negativo per storni/incassi in eccesso."),
                    prevalent_payment_mode = table.Column<int>(type: "booking.payment_mode", nullable: true, comment: "Modalità di pagamento prevalente (riepilogo); NULL finché non ancora incassata/determinata."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_reservations", x => x.id);
                },
                comment: "Testata unica di una prenotazione logica (un booking_number), spot o solo servizi.");

            migrationBuilder.CreateTable(
                name: "resource_availability_daily",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    consumption_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Giorno della grana di disponibilità"),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema"),
                    resource_type = table.Column<int>(type: "booking.resource_type", nullable: false, comment: "Tipo di risorsa (stesso enum di booking_resource_consumptions.resource_type)"),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id C2 della risorsa (proiezione fine); NULL per la proiezione grossolana per tipo"),
                    sector_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.map_sectors, denormalizzato al momento del rebuild — nessuna FK cross-schema"),
                    booking_period = table.Column<int>(type: "booking.day_period", nullable: false, defaultValueSql: "'full_day'", comment: "Fascia della grana (full_day aggrega mattina+pomeriggio in lettura)"),
                    booked_qty = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Unità occupate (state <> released) per la grana, mantenuto atomico dal trigger"),
                    held_qty = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Quota di booked_qty in stato held (blocco temporaneo non ancora confermato)"),
                    total_qty = table.Column<int>(type: "integer", nullable: true, comment: "Capienza totale della grana (1 per postazione, giacenza magazzino, capienza area, maximum_reservations servizio); NULL = nessun limite"),
                    available_qty = table.Column<int>(type: "integer", nullable: true, computedColumnSql: "total_qty - booked_qty", stored: true, comment: "total_qty - booked_qty quando total_qty è valorizzato, altrimenti NULL (illimitato) — colonna generata"),
                    rebuilt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultimo ricalcolo completo dal job di riconciliazione"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga di proiezione"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultimo UPSERT della riga di proiezione"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Presente per uniformità, non usato: la proiezione si ricostruisce, non si soft-elimina; le righe di date passate le rimuove la retention (hard-delete)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_resource_availability_daily", x => new { x.id, x.consumption_date });
                },
                comment: "Proiezione di disponibilità per (risorsa/tipo × giorno), mantenuta dal DB via trigger su booking_resource_consumptions: sostituisce il GROUP BY a runtime del legacy.");

            migrationBuilder.CreateTable(
                name: "resource_consumptions",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    consumption_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Il giorno occupato: una riga per giorno, mai un intervallo"),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; prima colonna di ogni indice e dell'exclusion constraint"),
                    resource_type = table.Column<int>(type: "booking.resource_type", nullable: false, comment: "Tipo di risorsa consumata (postazione, articolo, servizio, slot orario)"),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id C2 della risorsa (map_spots / service_sale_items / inventory_items secondo resource_type), copiato — nessuna FK cross-schema; NULL per articolo personalizzato non a catalogo"),
                    resource_name_snapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Nome della risorsa congelato al momento del consumo, per reportistica anche se rinominata/non a catalogo"),
                    booking_period = table.Column<int>(type: "booking.day_period", nullable: false, defaultValueSql: "'full_day'", comment: "Fascia occupata; alimenta la colonna generata period_mask usata dall'exclusion constraint"),
                    slot_start = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Inizio fascia oraria interna, valorizzato solo per resource_type = hourly_service"),
                    slot_end = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Fine fascia oraria interna, valorizzato solo per resource_type = hourly_service"),
                    time_slot_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.service_sale_item_time_slots (fascia di catalogo prenotata), copiato — nessuna FK cross-schema; solo hourly_service"),
                    quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 1, comment: "Unità consumate in quel giorno (1 per postazione puntuale, N per magazzino/area)"),
                    state = table.Column<int>(type: "booking.consumption_state", nullable: false, defaultValueSql: "'confirmed'", comment: "Stato del consumo, indipendente dal pagamento: bloccato temporaneamente, occupazione effettiva, liberato"),
                    held_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Scadenza del blocco temporaneo (state = held), calcolata server-side (now() + Booking:HoldTtl), mai dal body"),
                    source_kind = table.Column<int>(type: "booking.consumption_source", nullable: false, comment: "Cosa ha generato il consumo: riga postazione, riga servizio, articolo aggiuntivo, blocco manuale, ripresa di rivendita"),
                    source_id = table.Column<long>(type: "bigint", nullable: true, comment: "Id (interno C1) dell'entità di origine indicata da source_kind — nessuna FK polimorfa DB, coerenza gestita in applicazione; NULL per manual_hold senza entità a monte"),
                    booking_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Numero prenotazione copiato per query dirette; NULL per manual_hold"),
                    booking_channel = table.Column<int>(type: "booking.booking_channel", nullable: true, comment: "Canale della prenotazione di origine, per statistiche sui consumi; NULL per manual_hold"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_resource_consumptions", x => new { x.id, x.consumption_date });
                    table.CheckConstraint("ck_booking_resource_consumptions_quantity_positive", "quantity > 0");
                },
                comment: "Una riga per (risorsa × giorno occupato × sorgente): unica fonte di verità della disponibilità di postazioni, articoli, servizi e slot orari.");

            migrationBuilder.CreateTable(
                name: "group_members",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    group_id = table.Column<long>(type: "bigint", nullable: false, comment: "Gruppo di appartenenza"),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione membro del gruppo (l'intera prenotazione, non una singola riga postazione come nel legacy)"),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, denormalizzato dal gruppo — nessuna FK cross-schema"),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora in cui la prenotazione è stata aggiunta al gruppo"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga di giunzione"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga di giunzione"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: prenotazione rimossa dal gruppo")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_group_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_group_members_group",
                        column: x => x.group_id,
                        principalSchema: "booking",
                        principalTable: "groups",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_group_members_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Giunzione gruppo × prenotazione: una prenotazione appartiene ad al più un gruppo.");

            migrationBuilder.CreateTable(
                name: "lines",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile della riga, copiato cross-modulo da C3 (righe economiche) e C4 (line item scontrino)."),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione (testata) a cui appartiene la riga."),
                    booking_line_number = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false, comment: "Numero riga leggibile, subordinato al booking_number della testata (es. 10795-0000012345-01)."),
                    booking_number = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "Numero prenotazione copiato dalla testata, per correlazioni dirette con C3/C4 senza join."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema."),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.map_spots, copiato — la postazione prenotata; NULL per le righe ad area."),
                    spot_label_snapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Etichetta della postazione congelata al momento della prenotazione."),
                    sector_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.map_sectors, copiato — settore contro cui è stata calcolata la tariffa."),
                    sector_label_snapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Etichetta del settore/fila congelata al momento della prenotazione."),
                    price_list_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.pricing_price_lists, copiato — voce di listino (pacchetto) applicata alla riga."),
                    package_name_snapshot = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Nome del pacchetto congelato al momento della prenotazione."),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Inizio dell'intervallo contiguo coperto dalla riga (incluso)."),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fine dell'intervallo contiguo coperto dalla riga (incluso)."),
                    booking_period = table.Column<int>(type: "booking.day_period", nullable: false, defaultValueSql: "'full_day'", comment: "Fascia oraria della riga."),
                    quantity = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1, comment: "Numero di postazioni coperte dalla riga; > 1 solo per riga ad area."),
                    line_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Prezzo bloccato della riga (pacchetto), al netto degli articoli aggiuntivi."),
                    is_split = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "La riga fa parte di una prenotazione split-location."),
                    is_area = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Prenotazione ad area a capienza (senza postazione puntuale); se true resource_id è NULL."),
                    area_resource_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.map_spots (spot_type d'area), copiato — l'area occupata quando is_area = true."),
                    area_seat_count = table.Column<short>(type: "smallint", nullable: true, comment: "Posti occupati nell'area da questa riga (solo se is_area)."),
                    checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando il cliente si è presentato per questa postazione; NULL = non arrivato."),
                    checked_out_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando la postazione è stata liberata / cliente uscito."),
                    checkout_effective_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Fine occupazione effettiva in caso di check-out anticipato; limite superiore per la disponibilità al posto di end_date."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_lines", x => x.id);
                    table.CheckConstraint("ck_booking_lines_dates", "end_date >= start_date");
                    table.CheckConstraint("ck_booking_lines_quantity", "quantity >= 1");
                    table.ForeignKey(
                        name: "fk_booking_lines_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Riga dell'aggregato Prenotazione: una postazione per un intervallo di date contiguo e una fascia oraria.");

            migrationBuilder.CreateTable(
                name: "participants",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione (testata) resa visibile al partecipante."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di identity.access_users, copiato — account consumer che ha aperto la prenotazione col codice."),
                    role = table.Column<int>(type: "booking.participant_role", nullable: false, defaultValueSql: "'viewer'", comment: "Ruolo del partecipante rispetto alla prenotazione."),
                    linked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Quando l'aggancio è avvenuto."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_participants", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_participants_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Concessione di visibilità: account consumer che ha agganciato la prenotazione col reservation_code.");

            migrationBuilder.CreateTable(
                name: "quick_action_dates",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione a cui la data marcata è associata."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema."),
                    action_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Giorno marcato per un'azione rapida del planner."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_quick_action_dates", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_quick_action_dates_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Giorno marcato dall'operatore per un'azione rapida del planner su questa prenotazione.");

            migrationBuilder.CreateTable(
                name: "spot_holds",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato dal token — nessuna FK cross-schema"),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.map_spots (postazione bloccata), copiato — nessuna FK cross-schema"),
                    spot_label_snapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Etichetta della postazione congelata al momento del blocco"),
                    hold_kind = table.Column<int>(type: "booking.hold_kind", nullable: false, defaultValueSql: "'operational'", comment: "Motivo del blocco: operativo (manutenzione/VIP/planner) o checkout temporaneo durante il pagamento online"),
                    hold_status = table.Column<int>(type: "booking.hold_status", nullable: false, defaultValueSql: "'active'", comment: "Stato del blocco: attivo, liberato manualmente, scaduto, convertito in prenotazione"),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Inizio dell'intervallo bloccato (estremi inclusi)"),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fine dell'intervallo bloccato (estremi inclusi); l'espansione per-giorno è in booking_resource_consumptions"),
                    booking_period = table.Column<int>(type: "booking.day_period", nullable: false, defaultValueSql: "'full_day'", comment: "Fascia bloccata"),
                    held_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Scadenza del blocco di checkout (now() + Booking:CheckoutHoldTtl, calcolato server-side); NULL per hold operational"),
                    reason_note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Nota libera opzionale, motivo della riserva operativa"),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users (operatore che ha creato il blocco, dal token), copiato — nessuna FK cross-schema; NULL per hold di checkout creati dal flusso online"),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data/ora di rilascio del blocco (soft-release, per audit)"),
                    released_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users (operatore che ha liberato il blocco), copiato — nessuna FK cross-schema"),
                    converted_reservation_id = table.Column<long>(type: "bigint", nullable: true, comment: "Prenotazione nata da questo blocco di checkout (hold_status = converted)"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione del blocco"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica del blocco"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete del blocco")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_spot_holds", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_spot_holds_converted_reservation",
                        column: x => x.converted_reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Blocco di una postazione senza prenotazione (manutenzione, VIP, planner, o blocco temporaneo di checkout online); nessun cliente, nessun pagamento.");

            migrationBuilder.CreateTable(
                name: "agreement_lines",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico, copiato cross-modulo verso C3 che tiene importo/stato pagamento della voce."),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione a cui la voce appartiene."),
                    booking_line_id = table.Column<long>(type: "bigint", nullable: true, comment: "Riga postazione che ha consumato la voce di convenzione; NULL = voce a livello prenotazione."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema."),
                    agreement_partner_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.agreement_partners, copiato — struttura partner della convenzione."),
                    agreement_price_list_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.agreement_price_lists, copiato — voce di listino/voucher di convenzione usata."),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.customer_customers, copiato dalla testata — per il conteggio utilizzi voucher per cliente/stagione senza join."),
                    list_type = table.Column<int>(type: "booking.agreement_line_type", nullable: false, defaultValueSql: "'regular'", comment: "Natura della voce: voce di listino ordinaria o voucher."),
                    quantity = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1, comment: "Quantità di voci di listino consumate."),
                    price_calculation = table.Column<short>(type: "smallint", nullable: true, comment: "Id copiato della lookup catalog.pricing_calculation_methods (metodo di calcolo prezzo congelato) — nessuna FK cross-schema."),
                    applied_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Prezzo bloccato della voce per la riga, già risolto dal motore prezzo (non l'intero listino)."),
                    is_used_as_discount = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "La voce è applicata come sconto sulla prenotazione invece che come addebito."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_agreement_lines", x => x.id);
                    table.CheckConstraint("ck_booking_agreement_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_booking_agreement_lines_line",
                        column: x => x.booking_line_id,
                        principalSchema: "booking",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_agreement_lines_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Voce di listino/voucher di una convenzione consumata da una riga prenotazione, con snapshot del prezzo applicato.");

            migrationBuilder.CreateTable(
                name: "discounts",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione a cui si applica lo sconto."),
                    booking_line_id = table.Column<long>(type: "bigint", nullable: true, comment: "Riga specifica a cui lo sconto è limitato; NULL = sconto sull'intera prenotazione."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema."),
                    discount_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.pricing_discounts, copiato — riferimento vivo per l'audit del contatore utilizzi; NULL per sconto manuale non a catalogo."),
                    origin = table.Column<int>(type: "booking.discount_origin", nullable: false, defaultValueSql: "'code_entered'", comment: "Come lo sconto è entrato nella prenotazione: codice digitato o regola automatica."),
                    code_snapshot = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, comment: "Codice/nome del voucher congelato al momento dell'applicazione."),
                    discount_type = table.Column<int>(type: "booking.discount_type", nullable: false, comment: "Natura del valore: importo fisso o percentuale."),
                    value = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Valore dello sconto congelato (importo € se fixed, percentuale se percentage)."),
                    scope = table.Column<int>(type: "booking.discount_scope", nullable: false, defaultValueSql: "'all'", comment: "Ambito di validità del voucher, congelato dalla regola di C2."),
                    applied_channel = table.Column<int>(type: "booking.discount_channel", nullable: false, defaultValueSql: "'any'", comment: "Canale su cui il voucher era ammesso, congelato dalla regola di C2."),
                    applies_to_categories = table.Column<int[]>(type: "booking.service_category[]", nullable: false, defaultValueSql: "'{}'", comment: "Categorie di servizio a cui il voucher è limitato, congelate; vuoto = nessun limite."),
                    is_roundup = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Arrotondamento del prezzo dopo lo sconto, congelato dalla regola."),
                    applied_amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Importo effettivamente scontato su questa prenotazione, calcolato dal motore prezzo server-side."),
                    consumed_usage = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "True se questa prenotazione ha consumato un utilizzo del voucher; l'incremento/decremento del contatore è atomico su C2."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_discounts", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_discounts_line",
                        column: x => x.booking_line_id,
                        principalSchema: "booking",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_discounts_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Snapshot di un voucher/sconto applicato a una prenotazione o a una sua riga.");

            migrationBuilder.CreateTable(
                name: "guests",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione a cui l'ospite appartiene."),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.customer_customers, copiato — anagrafica cliente dell'ospite; NULL per ospite senza record cliente (solo nome)."),
                    full_name_snapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false, comment: "Nome dell'ospite congelato, per elenco presenze/check-in anche se l'anagrafica cambia in seguito."),
                    booking_line_id = table.Column<long>(type: "bigint", nullable: true, comment: "Postazione a cui l'ospite è associato (prenotazioni multi-postazione); NULL = ospite della prenotazione nel complesso."),
                    checked_in_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Check-in per nome dell'ospite, opzionale."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_guests", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_guests_line",
                        column: x => x.booking_line_id,
                        principalSchema: "booking",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_booking_guests_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Ospite di una prenotazione: persona presente ma non intestataria, senza addebito/comunicazioni a suo carico.");

            migrationBuilder.CreateTable(
                name: "line_additional_items",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione a cui appartiene l'articolo."),
                    booking_line_id = table.Column<long>(type: "bigint", nullable: true, comment: "Riga postazione a cui l'articolo è agganciato; NULL = articolo sulla prenotazione nel complesso."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema."),
                    item_source = table.Column<int>(type: "booking.additional_item_source", nullable: false, comment: "Origine dell'anagrafica dell'articolo: voce di listino o articolo di magazzino."),
                    catalog_item_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.pricing_price_lists o catalog.inventory_items (secondo item_source), copiato — NULL per articolo personalizzato non a catalogo."),
                    name_snapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false, comment: "Nome dell'articolo congelato al momento della prenotazione."),
                    date_scope = table.Column<int>(type: "booking.additional_item_date_scope", nullable: false, defaultValueSql: "'whole_stay'", comment: "Copertura temporale: tutta la durata della riga o solo date puntuali."),
                    quantity = table.Column<short>(type: "smallint", nullable: false, comment: "Unità acquistate (per giorno se specific_dates, per l'intera durata se whole_stay)."),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Prezzo unitario bloccato dell'articolo."),
                    line_total = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Totale bloccato dell'articolo (quantità × prezzo unitario, eventualmente × giorni); concorre al subtotal della testata."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_line_additional_items", x => x.id);
                    table.CheckConstraint("ck_booking_line_additional_items_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_booking_line_additional_items_line",
                        column: x => x.booking_line_id,
                        principalSchema: "booking",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_line_additional_items_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Articolo aggiuntivo (lettino extra, cabina, tassa di soggiorno, ...) acquistato su una riga o sull'intera prenotazione.");

            migrationBuilder.CreateTable(
                name: "resale_offers",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema"),
                    origin = table.Column<int>(type: "booking.resale_origin", nullable: false, comment: "Come è nata l'offerta: assenza pianificata, sharing con credito, o assenza gestita come sharing"),
                    source_reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione del titolare che cede il giorno"),
                    booking_line_id = table.Column<long>(type: "bigint", nullable: true, comment: "Riga postazione ceduta (nullable: offerta a livello prenotazione, raro)"),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.map_spots (postazione ceduta), copiato — nessuna FK cross-schema"),
                    offer_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Giorno ceduto"),
                    offered_period = table.Column<int>(type: "booking.day_period", nullable: false, defaultValueSql: "'full_day'", comment: "Fascia ceduta (full day, mattina, pomeriggio)"),
                    remaining_period = table.Column<int>(type: "booking.day_period", nullable: true, comment: "Fascia ancora disponibile dopo una ripresa parziale; NULL se completamente ripresa o se offered_period non è full_day"),
                    offer_state = table.Column<int>(type: "booking.resale_offer_state", nullable: false, defaultValueSql: "'available'", comment: "Stato dell'offerta: disponibile, ripreso da un'altra prenotazione, ritirato dal titolare"),
                    taken_by_reservation_id = table.Column<long>(type: "bigint", nullable: true, comment: "Prenotazione che ha ripreso il giorno ceduto (valorizzato quando offer_state = reassigned)"),
                    taken_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data/ora della ripresa del giorno"),
                    credit_settled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "True se il credito per questa ripresa è stato accreditato al titolare (solo origin di tipo sharing)"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione dell'offerta"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica dell'offerta"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete dell'offerta")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_resale_offers", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_resale_offers_booking_line",
                        column: x => x.booking_line_id,
                        principalSchema: "booking",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_resale_offers_source_reservation",
                        column: x => x.source_reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_resale_offers_taken_by_reservation",
                        column: x => x.taken_by_reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Riga (riga prenotazione di origine × postazione × giorno ceduto): il titolare dichiara che non userà la postazione quel giorno, che torna prenotabile (assenza pianificata o sharing).");

            migrationBuilder.CreateTable(
                name: "service_lines",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico della riga servizio, copiato cross-modulo da C4 (line item) e C3 (riga economica)."),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione a cui il servizio appartiene."),
                    booking_line_id = table.Column<long>(type: "bigint", nullable: true, comment: "Riga postazione a cui il servizio è agganciato; NULL = servizio sulla prenotazione nel complesso."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema."),
                    kind = table.Column<int>(type: "booking.service_category", nullable: false, comment: "Categoria del servizio venduto: bar, normale o orario."),
                    sale_item_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.service_sale_items, copiato — NULL per servizio ad hoc non a catalogo."),
                    name_snapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false, comment: "Nome del servizio congelato al momento della vendita."),
                    variant_label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true, comment: "Variante scelta del prodotto (es. \"Macchiato\"); NULL = nessuna variante."),
                    variant_surcharge = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Sovrapprezzo unitario della variante."),
                    unit_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Prezzo unitario bloccato, senza variante."),
                    quantity = table.Column<int>(type: "integer", nullable: false, comment: "Unità vendute (totale della riga); il dettaglio per giorno è in booking_resource_consumptions."),
                    vat_rate = table.Column<decimal>(type: "numeric(5,2)", nullable: true, comment: "Aliquota IVA % congelata per lo scontrino."),
                    line_total = table.Column<decimal>(type: "numeric(12,2)", nullable: false, computedColumnSql: "(unit_price + variant_surcharge) * quantity", stored: true, comment: "Totale bloccato della riga — colonna generata: (unit_price + variant_surcharge) * quantity. Concorre al subtotal della testata; sola lettura."),
                    service_date = table.Column<DateOnly>(type: "date", nullable: true, comment: "Data di riferimento del servizio quando è una sola; per servizi multi-data le date sono in booking_resource_consumptions."),
                    booking_channel = table.Column<int>(type: "booking.booking_channel", nullable: false, comment: "Canale della prenotazione di origine, denormalizzato dalla testata."),
                    is_order_filled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "La riga è stata evasa (bar/cucina); il riepilogo a livello prenotazione è su booking_reservations."),
                    is_paid_with_wallet = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "La riga è stata pagata col borsellino/tessera prepagata; il movimento wallet è in C3."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_service_lines", x => x.id);
                    table.CheckConstraint("ck_booking_service_lines_quantity", "quantity > 0");
                    table.ForeignKey(
                        name: "fk_booking_service_lines_line",
                        column: x => x.booking_line_id,
                        principalSchema: "booking",
                        principalTable: "lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_booking_service_lines_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Riga d'ordine per un servizio (bar/normale/orario) venduto su una prenotazione, agganciata o meno a una postazione.");

            migrationBuilder.CreateTable(
                name: "resale_credit_ledger",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema"),
                    customer_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di identity.customer_customers (cliente titolare a cui il credito è maturato), copiato — nessuna FK cross-schema"),
                    resale_offer_id = table.Column<long>(type: "bigint", nullable: true, comment: "Offerta/ripresa che ha generato il credito (nullable: aggiustamenti manuali senza offerta)"),
                    source_reservation_id = table.Column<long>(type: "bigint", nullable: true, comment: "Prenotazione del titolare di cui è stato ceduto il giorno"),
                    entry_type = table.Column<int>(type: "booking.resale_credit_entry", nullable: false, defaultValueSql: "'earned'", comment: "Tipo di movimento: accredito per ripresa, storno di una ripresa annullata, correzione manuale"),
                    credit_units = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Numero di crediti (unità simboliche) maturati per il movimento"),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Valore economico del credito: segno positivo per earned, negativo per reversed"),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora in cui il movimento è avvenuto"),
                    wallet_event_emitted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "True quando l'evento ResaleCreditEarned è stato consegnato a Payment (C3) per la riconciliazione col borsellino"),
                    note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Nota libera, valorizzata per i movimenti di tipo adjustment"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga di ledger"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga di ledger"),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Presente per uniformità di schema, non usato: il ledger è append-only, le correzioni sono righe reversed/adjustment")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_resale_credit_ledger", x => x.id);
                    table.ForeignKey(
                        name: "fk_booking_resale_credit_ledger_resale_offer",
                        column: x => x.resale_offer_id,
                        principalSchema: "booking",
                        principalTable: "resale_offers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_booking_resale_credit_ledger_source_reservation",
                        column: x => x.source_reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Ledger append-only dei crediti sharing maturati dal titolare quando un giorno ceduto viene ripreso da un altro cliente; il saldo è la somma delle righe per cliente.");

            migrationBuilder.CreateTable(
                name: "service_time_slots",
                schema: "booking",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    service_line_id = table.Column<long>(type: "bigint", nullable: false, comment: "Riga servizio orario a cui lo slot appartiene."),
                    reservation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Prenotazione, denormalizzata dalla riga servizio per query dirette."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema."),
                    time_slot_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.service_sale_item_time_slots, copiato — NULL per slot manuale non a catalogo."),
                    slot_start = table.Column<TimeOnly>(type: "time", nullable: false, comment: "Orario di inizio dello slot, congelato al momento della prenotazione."),
                    slot_end = table.Column<TimeOnly>(type: "time", nullable: false, comment: "Orario di fine dello slot, congelato al momento della prenotazione."),
                    slot_label_snapshot = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Etichetta dello slot congelata (es. \"9-11\")."),
                    service_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Giorno dello slot."),
                    seats = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1, comment: "Posti/unità prenotati per lo slot; alimenta il consumo di booking_resource_consumptions."),
                    slot_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo dello slot congelato alla conferma, se ha tariffa dedicata; altrimenti quello del servizio."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_booking_service_time_slots", x => x.id);
                    table.CheckConstraint("ck_booking_service_time_slots_range", "slot_end > slot_start");
                    table.CheckConstraint("ck_booking_service_time_slots_seats", "seats > 0");
                    table.ForeignKey(
                        name: "fk_booking_service_time_slots_reservation",
                        column: x => x.reservation_id,
                        principalSchema: "booking",
                        principalTable: "reservations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_booking_service_time_slots_service_line",
                        column: x => x.service_line_id,
                        principalSchema: "booking",
                        principalTable: "service_lines",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Slot orario prenotato per una riga servizio hourly: giorno, fascia e posti.");

            migrationBuilder.CreateIndex(
                name: "ix_booking_agreement_lines_line",
                schema: "booking",
                table: "agreement_lines",
                column: "booking_line_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_agreement_lines_partner",
                schema: "booking",
                table: "agreement_lines",
                column: "agreement_partner_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_agreement_lines_reservation",
                schema: "booking",
                table: "agreement_lines",
                column: "reservation_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_agreement_lines_voucher_limit",
                schema: "booking",
                table: "agreement_lines",
                columns: new[] { "property_id", "agreement_price_list_id", "customer_id" },
                filter: "list_type = 'voucher' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_agreement_lines_public_id",
                schema: "booking",
                table: "agreement_lines",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_discounts_discount",
                schema: "booking",
                table: "discounts",
                column: "discount_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_discounts_property_code",
                schema: "booking",
                table: "discounts",
                columns: new[] { "property_id", "code_snapshot" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_discounts_reservation",
                schema: "booking",
                table: "discounts",
                column: "reservation_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_discounts_booking_line_id",
                schema: "booking",
                table: "discounts",
                column: "booking_line_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_group_members_group",
                schema: "booking",
                table: "group_members",
                column: "group_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_group_members_reservation",
                schema: "booking",
                table: "group_members",
                column: "reservation_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_groups_property",
                schema: "booking",
                table: "groups",
                column: "property_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_groups_property_name",
                schema: "booking",
                table: "groups",
                columns: new[] { "property_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_guests_customer",
                schema: "booking",
                table: "guests",
                column: "customer_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_guests_booking_line_id",
                schema: "booking",
                table: "guests",
                column: "booking_line_id");

            migrationBuilder.CreateIndex(
                name: "uq_booking_guests_reservation_customer",
                schema: "booking",
                table: "guests",
                columns: new[] { "reservation_id", "customer_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_line_additional_items_catalog_item",
                schema: "booking",
                table: "line_additional_items",
                column: "catalog_item_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_line_additional_items_line",
                schema: "booking",
                table: "line_additional_items",
                column: "booking_line_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_line_additional_items_reservation",
                schema: "booking",
                table: "line_additional_items",
                column: "reservation_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_lines_booking_number",
                schema: "booking",
                table: "lines",
                column: "booking_number");

            migrationBuilder.CreateIndex(
                name: "ix_booking_lines_checkin_open",
                schema: "booking",
                table: "lines",
                columns: new[] { "property_id", "start_date" },
                filter: "checked_in_at IS NOT NULL AND checked_out_at IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_lines_price_list",
                schema: "booking",
                table: "lines",
                column: "price_list_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_lines_property_resource_dates",
                schema: "booking",
                table: "lines",
                columns: new[] { "property_id", "resource_id", "start_date", "end_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_lines_reservation",
                schema: "booking",
                table: "lines",
                column: "reservation_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_lines_number",
                schema: "booking",
                table: "lines",
                column: "booking_line_number",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_lines_public_id",
                schema: "booking",
                table: "lines",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_booking_number_sequences_property",
                schema: "booking",
                table: "number_sequences",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_participants_user",
                schema: "booking",
                table: "participants",
                column: "user_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_participants_reservation_user",
                schema: "booking",
                table: "participants",
                columns: new[] { "reservation_id", "user_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_quick_action_dates_property_date",
                schema: "booking",
                table: "quick_action_dates",
                columns: new[] { "property_id", "action_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_quick_action_dates_reservation_date",
                schema: "booking",
                table: "quick_action_dates",
                columns: new[] { "reservation_id", "action_date" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resale_credit_ledger_customer",
                schema: "booking",
                table: "resale_credit_ledger",
                columns: new[] { "property_id", "customer_id", "occurred_at" },
                descending: new[] { false, false, true });

            migrationBuilder.CreateIndex(
                name: "ix_resale_credit_ledger_source_reservation_id",
                schema: "booking",
                table: "resale_credit_ledger",
                column: "source_reservation_id");

            migrationBuilder.CreateIndex(
                name: "uq_booking_resale_credit_ledger_offer_entry",
                schema: "booking",
                table: "resale_credit_ledger",
                column: "resale_offer_id",
                unique: true,
                filter: "entry_type = 'earned'");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resale_offers_availability",
                schema: "booking",
                table: "resale_offers",
                columns: new[] { "property_id", "resource_id", "offer_date" },
                filter: "offer_state = 'available' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resale_offers_credit_pending",
                schema: "booking",
                table: "resale_offers",
                column: "property_id",
                filter: "offer_state = 'reassigned' AND credit_settled = false AND origin = 'sharing' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resale_offers_origin_line",
                schema: "booking",
                table: "resale_offers",
                column: "booking_line_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resale_offers_taken_by",
                schema: "booking",
                table: "resale_offers",
                column: "taken_by_reservation_id",
                filter: "taken_by_reservation_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_resale_offers_source_reservation_id",
                schema: "booking",
                table: "resale_offers",
                column: "source_reservation_id");

            migrationBuilder.CreateIndex(
                name: "uq_booking_resale_settings_property",
                schema: "booking",
                table: "resale_settings",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_reservations_agreement",
                schema: "booking",
                table: "reservations",
                column: "agreement_partner_id",
                filter: "agreement_partner_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_reservations_customer",
                schema: "booking",
                table: "reservations",
                column: "customer_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_reservations_online_pending",
                schema: "booking",
                table: "reservations",
                column: "property_id",
                filter: "online_confirmation_state = 'pending' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_reservations_property_channel",
                schema: "booking",
                table: "reservations",
                columns: new[] { "property_id", "booking_channel" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_reservations_property_status",
                schema: "booking",
                table: "reservations",
                columns: new[] { "property_id", "reservation_status" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_reservations_unpaid",
                schema: "booking",
                table: "reservations",
                columns: new[] { "property_id", "stay_from" },
                filter: "balance_due > 0 AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_reservations_number",
                schema: "booking",
                table: "reservations",
                column: "booking_number",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_reservations_public_id",
                schema: "booking",
                table: "reservations",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_booking_reservations_reservation_code",
                schema: "booking",
                table: "reservations",
                column: "reservation_code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resource_availability_daily_search",
                schema: "booking",
                table: "resource_availability_daily",
                columns: new[] { "property_id", "consumption_date", "resource_type" },
                filter: "available_qty > 0");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resource_availability_daily_sector",
                schema: "booking",
                table: "resource_availability_daily",
                columns: new[] { "sector_id", "consumption_date" },
                filter: "sector_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resource_availability_daily_spot_day",
                schema: "booking",
                table: "resource_availability_daily",
                columns: new[] { "property_id", "consumption_date" },
                filter: "resource_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_booking_resource_availability_daily_grain",
                schema: "booking",
                table: "resource_availability_daily",
                columns: new[] { "property_id", "resource_type", "resource_id", "consumption_date", "booking_period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_resource_consumptions_availability",
                schema: "booking",
                table: "resource_consumptions",
                columns: new[] { "property_id", "resource_id", "consumption_date" },
                filter: "state <> 'released'");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resource_consumptions_booking_number",
                schema: "booking",
                table: "resource_consumptions",
                column: "booking_number");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resource_consumptions_holds_expiry",
                schema: "booking",
                table: "resource_consumptions",
                column: "held_until",
                filter: "state = 'held' AND held_until IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_resource_consumptions_property_date_state",
                schema: "booking",
                table: "resource_consumptions",
                columns: new[] { "property_id", "consumption_date", "state" });

            migrationBuilder.CreateIndex(
                name: "ix_booking_resource_consumptions_source",
                schema: "booking",
                table: "resource_consumptions",
                columns: new[] { "source_kind", "source_id" });

            migrationBuilder.CreateIndex(
                name: "ix_booking_resource_consumptions_type_date",
                schema: "booking",
                table: "resource_consumptions",
                columns: new[] { "property_id", "resource_type", "consumption_date" },
                filter: "state <> 'released'");

            migrationBuilder.CreateIndex(
                name: "ix_booking_service_lines_order_open",
                schema: "booking",
                table: "service_lines",
                column: "property_id",
                filter: "is_order_filled = false AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_service_lines_property_kind",
                schema: "booking",
                table: "service_lines",
                columns: new[] { "property_id", "kind" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_service_lines_reservation",
                schema: "booking",
                table: "service_lines",
                column: "reservation_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_service_lines_sale_item",
                schema: "booking",
                table: "service_lines",
                column: "sale_item_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_service_lines_booking_line_id",
                schema: "booking",
                table: "service_lines",
                column: "booking_line_id");

            migrationBuilder.CreateIndex(
                name: "uq_booking_service_lines_public_id",
                schema: "booking",
                table: "service_lines",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_booking_service_time_slots_property_slot_date",
                schema: "booking",
                table: "service_time_slots",
                columns: new[] { "property_id", "time_slot_id", "service_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_service_time_slots_service_line",
                schema: "booking",
                table: "service_time_slots",
                column: "service_line_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_service_time_slots_reservation_id",
                schema: "booking",
                table: "service_time_slots",
                column: "reservation_id");

            migrationBuilder.CreateIndex(
                name: "ix_booking_spot_holds_expiry",
                schema: "booking",
                table: "spot_holds",
                column: "held_until",
                filter: "hold_status = 'active' AND held_until IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_spot_holds_property_active",
                schema: "booking",
                table: "spot_holds",
                column: "property_id",
                filter: "hold_status = 'active' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_booking_spot_holds_property_resource_dates",
                schema: "booking",
                table: "spot_holds",
                columns: new[] { "property_id", "resource_id", "start_date", "end_date" },
                filter: "hold_status = 'active'");

            migrationBuilder.CreateIndex(
                name: "ix_spot_holds_converted_reservation_id",
                schema: "booking",
                table: "spot_holds",
                column: "converted_reservation_id");

            // ─────────────────────────────────────────────────────────────────────────────
            // Oggetti che il modello EF non sa esprimere. Questa InitialCreate consolida
            // (2026-10-06) tutte le migration precedenti del modulo: qui sotto c'e' lo SQL a
            // mano che quelle contenevano e che vale ancora sullo schema finale. Gli UPDATE di
            // conversione dei dati (portavano righe esistenti da una forma all'altra) non sono
            // riportati: su un database nuovo non c'e' niente da convertire.
            // ─────────────────────────────────────────────────────────────────────────────

            // Partizionamento per anno solare di resource_consumptions e resource_availability_daily.
            //
            // EF crea le due tabelle come tabelle normali; qui si sostituiscono con una gemella
            // partizionata (LIKE ... INCLUDING ...: stesse colonne, default, colonne generate e
            // commenti). Gli indici si ricreano dopo sul padre, perche' su una tabella partizionata
            // ogni indice UNIQUE deve contenere la chiave di partizione. Nessuna FK punta a queste
            // tabelle, quindi il DROP e' sicuro.
            //
            // Partizioni dal 2021 al 2035 piu' una di default come rete di sicurezza. La default
            // non deve riempirsi: finche' contiene righe di un anno, la partizione di quell'anno non
            // e' piu' creabile. Prima del 2035 vanno aggiunte le successive.
            migrationBuilder.Sql($"""
CREATE TABLE booking.resource_consumptions_new (
  LIKE booking.resource_consumptions
  INCLUDING DEFAULTS INCLUDING GENERATED INCLUDING COMMENTS INCLUDING STATISTICS INCLUDING IDENTITY
) PARTITION BY RANGE (consumption_date);
{PartizioniAnnuali("resource_consumptions_new", "resource_consumptions")}
DROP TABLE booking.resource_consumptions;
ALTER TABLE booking.resource_consumptions_new RENAME TO resource_consumptions;
ALTER TABLE booking.resource_consumptions ADD CONSTRAINT pk_booking_resource_consumptions PRIMARY KEY (id, consumption_date);
CREATE INDEX ix_booking_resource_consumptions_availability ON booking.resource_consumptions
  (property_id, resource_id, consumption_date) WHERE (state <> 'released');
CREATE INDEX ix_booking_resource_consumptions_booking_number ON booking.resource_consumptions (booking_number);
CREATE INDEX ix_booking_resource_consumptions_holds_expiry ON booking.resource_consumptions
  (held_until) WHERE (state = 'held' AND held_until IS NOT NULL);
CREATE INDEX ix_booking_resource_consumptions_property_date_state ON booking.resource_consumptions
  (property_id, consumption_date, state);
CREATE INDEX ix_booking_resource_consumptions_source ON booking.resource_consumptions (source_kind, source_id);
CREATE INDEX ix_booking_resource_consumptions_type_date ON booking.resource_consumptions
  (property_id, resource_type, consumption_date) WHERE (state <> 'released');
""");
            migrationBuilder.Sql($"""
CREATE TABLE booking.resource_availability_daily_new (
  LIKE booking.resource_availability_daily
  INCLUDING DEFAULTS INCLUDING GENERATED INCLUDING COMMENTS INCLUDING STATISTICS INCLUDING IDENTITY
) PARTITION BY RANGE (consumption_date);
{PartizioniAnnuali("resource_availability_daily_new", "resource_availability_daily")}
DROP TABLE booking.resource_availability_daily;
ALTER TABLE booking.resource_availability_daily_new RENAME TO resource_availability_daily;
ALTER TABLE booking.resource_availability_daily ADD CONSTRAINT pk_booking_resource_availability_daily PRIMARY KEY (id, consumption_date);
CREATE UNIQUE INDEX uq_booking_resource_availability_daily_grain ON booking.resource_availability_daily
  (property_id, resource_type, resource_id, consumption_date, booking_period);
CREATE INDEX ix_booking_resource_availability_daily_search ON booking.resource_availability_daily
  (property_id, consumption_date, resource_type) WHERE (available_qty > 0 OR available_qty IS NULL);
CREATE INDEX ix_booking_resource_availability_daily_sector ON booking.resource_availability_daily
  (sector_id, consumption_date) WHERE (sector_id IS NOT NULL);
CREATE INDEX ix_booking_resource_availability_daily_spot_day ON booking.resource_availability_daily
  (property_id, consumption_date) WHERE (resource_id IS NOT NULL);
""");

            // Offerte di rivendita: una sola offerta per linea di prenotazione, giorno e fascia.
            // La chiave e' la linea e non la risorsa: sotto un ombrellone ci sono piu' linee (i
            // lettini), ognuna rilasciabile per conto proprio (confermato dal committente il
            // 2026-09-30). Le offerte senza booking_line_id restano fuori dal vincolo: in un EXCLUDE
            // la colonna NULL non conflitta con niente, e non va raccontato come copertura totale.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");
            migrationBuilder.Sql("""
ALTER TABLE booking.resale_offers
  ADD CONSTRAINT ex_booking_resale_offers_no_overlap
  EXCLUDE USING gist (booking_line_id WITH =, offer_date WITH =,
                      int4range(CASE offered_period WHEN 'afternoon' THEN 2 ELSE 1 END,
                                CASE offered_period WHEN 'morning'   THEN 2 ELSE 3 END) WITH &&)
  WHERE (deleted_at IS NULL AND offer_state <> 'withdrawn' AND booking_line_id IS NOT NULL);
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agreement_lines",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "discounts",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "group_members",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "guests",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "line_additional_items",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "number_sequences",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "participants",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "quick_action_dates",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "resale_credit_ledger",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "resale_settings",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "resource_availability_daily",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "resource_consumptions",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "service_time_slots",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "spot_holds",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "groups",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "resale_offers",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "service_lines",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "lines",
                schema: "booking");

            migrationBuilder.DropTable(
                name: "reservations",
                schema: "booking");
        }

        /// <summary>
        /// Una partizione per anno solare dal 2021 al 2035, piu' quella di default.
        /// <paramref name="padre"/> e' il nome che la tabella partizionata ha al momento della
        /// creazione; <paramref name="nomeFinale"/> quello da cui derivano i nomi delle partizioni.
        /// </summary>
        private static string PartizioniAnnuali(string padre, string nomeFinale) =>
            string.Join("\n", Enumerable.Range(2021, 15).Select(anno =>
                $"CREATE TABLE booking.{nomeFinale}_y{anno} PARTITION OF booking.{padre} " +
                $"FOR VALUES FROM ('{anno}-01-01') TO ('{anno + 1}-01-01');"))
            + $"\nCREATE TABLE booking.{nomeFinale}_default PARTITION OF booking.{padre} DEFAULT;";
    }
}
