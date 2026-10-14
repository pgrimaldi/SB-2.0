using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalog");

            // pg_trgm: indici GIN di ricerca testuale (gin_trgm_ops) sui nomi.
            // btree_gist: vincoli EXCLUDE di non sovrapposizione, in fondo a questa migration.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:catalog.agreement_list_type", "regular,voucher")
                .Annotation("Npgsql:Enum:catalog.agreement_report_search_type", "check_in,check_out")
                .Annotation("Npgsql:Enum:catalog.agreement_report_status", "pending,created,cancelled")
                .Annotation("Npgsql:Enum:catalog.config_snapshot_kind", "property_management_config,beach_map,public_showcase,price_catalog,services_catalog,agreement_catalog")
                .Annotation("Npgsql:Enum:catalog.date_price_kind", "override,holiday")
                .Annotation("Npgsql:Enum:catalog.discount_applicable_channel", "manager_and_online,manager_only,online_only")
                .Annotation("Npgsql:Enum:catalog.discount_scope", "all,custom,without_custom")
                .Annotation("Npgsql:Enum:catalog.discount_service_category", "bar,normal,hourly,additional")
                .Annotation("Npgsql:Enum:catalog.discount_type", "percentage,fixed")
                .Annotation("Npgsql:Enum:catalog.map_icon_category", "template,umbrella,tent,sunbed,cabin")
                .Annotation("Npgsql:Enum:catalog.map_icon_state", "occupied,free")
                .Annotation("Npgsql:Enum:catalog.media_kind", "photo,pdf,icon")
                .Annotation("Npgsql:Enum:catalog.media_origin", "s3us_east2,s3us_east1,cloudfront,external")
                .Annotation("Npgsql:Enum:catalog.monthly_setting", "effective_month_duration,actual_month_duration,fixed30days")
                .Annotation("Npgsql:Enum:catalog.online_booking_scope", "all,contacts_only")
                .Annotation("Npgsql:Enum:catalog.online_payment_method", "pay_now,pay_at_property,pay_at_property_no_card,deposit_half,pay_at_property_prepaid")
                .Annotation("Npgsql:Enum:catalog.package_item_kind", "inventory_item,area_item,included_service,linked_package")
                .Annotation("Npgsql:Enum:catalog.pets_policy", "allowed,not_allowed,on_request")
                .Annotation("Npgsql:Enum:catalog.price_list_kind", "package,additional")
                .Annotation("Npgsql:Enum:catalog.property_status", "pending,approved,rejected,imported")
                .Annotation("Npgsql:Enum:catalog.sales_channel", "manager_only,manager_and_online,online_only")
                .Annotation("Npgsql:Enum:catalog.service_sale_kind", "bar,normal,hourly")
                .Annotation("Npgsql:Enum:catalog.spot_type", "umbrella,sunbed,sunbed_xl,cabin,special_cabin,special_umbrella1,special_umbrella2,special_umbrella3,special_umbrella4,special_umbrella5,tent,parking,ticket,umbrella_area,cabin_area,sunbed_area,parking_area")
                .Annotation("Npgsql:Enum:catalog.suggested_price_mode", "none,suggested,original,discount")
                .Annotation("Npgsql:Enum:catalog.surcharge_type", "disable,percentage,fixed")
                .Annotation("Npgsql:Enum:catalog.tariff_tier", "low,mid,high,peak,custom")
                .Annotation("Npgsql:Enum:catalog.weekday", "monday,tuesday,wednesday,thursday,friday,saturday,sunday")
                .Annotation("Npgsql:Enum:catalog.weekend_setting", "saturday_sunday,sunday_only")
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "countries",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "char(2)", nullable: false, comment: "Codice ISO 3166-1 alpha-2 (IT, ES, HR, ...), valore copiato dai moduli che referenziano un paese — mai FK cross-schema."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Paese selezionabile nei form."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordine nelle tendine (es. Italia in cima)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: un paese referenziato da property/bathhouse/cliente non si cancella fisicamente.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_countries", x => x.id);
                },
                comment: "Elenco (non esaustivo) delle nazioni selezionabili nei form; lookup di dominio referenziata dai moduli tramite il code ISO copiato.");

            migrationBuilder.CreateTable(
                name: "map_icon_categories",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<int>(type: "catalog.map_icon_category", nullable: false, comment: "Codice stabile della categoria, usato in codice e come chiave di risorsa di localizzazione (nessuna colonna etichetta)."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Categoria selezionabile; consente di ritirarla senza cancellarla."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete; praticamente immutabile, presente per uniformità.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_map_icon_categories", x => x.id);
                },
                comment: "Lookup globale/di piattaforma: le categorie in cui sono raggruppate le icone della mappa spiaggia.");

            migrationBuilder.CreateTable(
                name: "pricing_calculation_methods",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "Codice canonico del metodo (proportional, daily, daily_with_weekend, logarithmic)."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, comment: "Metodo selezionabile nell'editor prezzi."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_calculation_methods", x => x.id);
                },
                comment: "Lookup dei metodi di calcolo del prezzo multi-giorno di una voce di listino.");

            migrationBuilder.CreateTable(
                name: "search_bathhouses",
                schema: "catalog",
                columns: table => new
                {
                    bathhouse_public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Identità della vetrina = catalog_bathhouses.public_id, copiato (no FK: read-model ricostruibile)."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento proprietario, copiato — per invalidazione e raggruppamento; no FK."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome commerciale, per il match testuale e il sort."),
                    city = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Città, per i filtri per area."),
                    region = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Regione, per i filtri per area."),
                    province = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Provincia, per i filtri per area."),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Per costruire il link alla scheda pubblica direttamente dalla card."),
                    latitude = table.Column<double>(type: "double precision", nullable: true, comment: "Latitudine della vetrina, per la ricerca \"vicino a me\"/entro raggio (sostituisce la colonna geography(Point,4326) della scheda — nessuna dipendenza PostGIS/NetTopologySuite nella solution)."),
                    longitude = table.Column<double>(type: "double precision", nullable: true, comment: "Longitudine della vetrina, per la ricerca \"vicino a me\"/entro raggio."),
                    min_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo minimo corrente prenotabile online, materializzato dal rebuild (evita la query-per-risultato del legacy)."),
                    amenity_ids = table.Column<short[]>(type: "smallint[]", nullable: false, defaultValueSql: "'{}'", comment: "Id delle dotazioni offerte dalla vetrina, per il filtro \"con questi servizi\"."),
                    like_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Numero di \"mi piace\" della vetrina, per il sort \"più popolari\" e la card."),
                    is_published = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "La vetrina è visibile nella ricerca pubblica."),
                    card = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb", comment: "Dati non filtrabili per renderizzare la card di risultato (foto, snippet, orari, servizi principali) — JSON derivato, eccezione consentita."),
                    content_hash = table.Column<byte[]>(type: "bytea", nullable: false, comment: "Hash della riga (colonne + card), per ETag sulle risposte di ricerca."),
                    built_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Timestamp dell'ultimo rebuild della riga."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete per uniformità; non usato funzionalmente — la visibilità è is_published.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_search_bathhouses", x => x.bathhouse_public_id);
                },
                comment: "Read-model di ricerca pubblica: una riga per vetrina pubblicata, ricostruibile da catalog_bathhouses + catalog_pricing_* + catalog_bathhouse_amenities + identity_customer_bathhouse_likes.");

            migrationBuilder.CreateTable(
                name: "service_amenity_categories",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false, comment: "Codice stabile della categoria (slug: popular, accessibility, restaurant, ...); l'etichetta mostrata sta nelle risorse di localizzazione."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Categoria visibile nella ricerca pubblica."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, comment: "Ordine di visualizzazione nella ricerca pubblica."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della categoria."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della categoria."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_amenity_categories", x => x.id);
                },
                comment: "Categoria di raggruppamento delle dotazioni (catalog_service_amenities) nella ricerca pubblica.");

            migrationBuilder.CreateTable(
                name: "country_translations",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<short>(type: "smallint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    country_id = table.Column<short>(type: "smallint", nullable: false, comment: "Paese a cui si riferisce la traduzione."),
                    culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, comment: "Codice cultura BCP-47 (es. it-IT, en-GB)."),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Nome del paese nella lingua indicata."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete; praticamente immutabile, presente per uniformità.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_country_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_country_translations_country",
                        column: x => x.country_id,
                        principalSchema: "catalog",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Nome di un paese in una culture. Una riga per (paese × lingua); sostituisce le colonne country_english/country_italian.");

            migrationBuilder.CreateTable(
                name: "properties",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile (UUIDv7) per URL di back-office / endpoint admin; non è il discriminatore tenant."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome amministrativo dello stabilimento (il nome commerciale pubblico vive su catalog.bathhouses)."),
                    status = table.Column<int>(type: "catalog.property_status", nullable: false, defaultValueSql: "'pending'", comment: "Stato del ciclo di vita amministrativo dello stabilimento."),
                    admin_comment = table.Column<string>(type: "text", nullable: true, comment: "Nota del systemadmin all'approvazione/rifiuto/cancellazione, mostrata anche al titolare nelle email automatiche."),
                    owner_user_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Titolare storico della registrazione."),
                    contact_full_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Nome e cognome della persona di riferimento (contatto interno)."),
                    contact_email = table.Column<string>(type: "citext", nullable: true, comment: "Email di contatto amministrativo; destinataria delle comunicazioni di benvenuto/approvazione/rifiuto/cancellazione."),
                    contact_phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Telefono di contatto amministrativo."),
                    contact_phone_country_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true, comment: "Prefisso internazionale del telefono di contatto."),
                    city = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Città indicata in registrazione (dato amministrativo; la città pubblica è su catalog.bathhouses)."),
                    country_id = table.Column<short>(type: "smallint", nullable: true, comment: "Paese dello stabilimento."),
                    is_online_sales_suspended = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Kill-switch di piattaforma che sospende le vendite online dello stabilimento (es. abbonamento scaduto), a prescindere dalle impostazioni del gestore."),
                    contract_document_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.account_contract_documents, copiato — nessuna FK cross-schema. NULL = feature contratto B2B disattivata per la property."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della property (evento raro e ad alto impatto: trascina vetrine, listini, mappe).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_properties", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_properties_country",
                        column: x => x.country_id,
                        principalSchema: "catalog",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Anagrafica gestionale/di piattaforma dello stabilimento (tenant): stato di approvazione, contatti amministrativi, titolare.");

            migrationBuilder.CreateTable(
                name: "agreement_partners",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico (UUIDv7), copiato cross-modulo da C1 (booking_reservations.agreement_partner_id)."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento che ha registrato la struttura partner (risolto sempre server-side, mai dal body)."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome della struttura partner."),
                    city = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Città della struttura partner."),
                    postal_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "CAP della struttura partner."),
                    province = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Provincia della struttura partner (testo libero, dati storicamente sporchi)."),
                    street_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Via della struttura partner."),
                    street_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Numero civico della struttura partner."),
                    phone = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Telefono di contatto della struttura partner."),
                    email = table.Column<string>(type: "citext", nullable: true, comment: "Email di contatto della struttura partner."),
                    note = table.Column<string>(type: "text", nullable: true, comment: "Annotazioni libere sulla struttura partner."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete; propaga a cascata (applicativa) ai listini/voucher figli.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_agreement_partners", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_partners_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Struttura partner (\"convenzione\"/hotel) con cui uno stabilimento ha un accordo commerciale.");

            migrationBuilder.CreateTable(
                name: "config_snapshots",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento a cui si riferisce lo snapshot."),
                    snapshot_kind = table.Column<int>(type: "catalog.config_snapshot_kind", nullable: false, comment: "Vista di configurazione contenuta nella riga (property_management_config, beach_map, public_showcase, price_catalog, services_catalog, agreement_catalog)."),
                    payload = table.Column<string>(type: "jsonb", nullable: false, comment: "Documento JSON servito al frontend; chiavi camelCase, importi a 2 decimali, file come assetRef {origin,path} — mai URL completi."),
                    payload_schema = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1, comment: "Versione della forma del payload (contratto verso il client); incrementata solo da un cambio non retrocompatibile del serializzatore."),
                    source_hash = table.Column<byte[]>(type: "bytea", nullable: false, comment: "SHA-256 delle righe sorgente rilevanti; invariato ⇒ il rebuild salta la riscrittura del payload."),
                    content_hash = table.Column<byte[]>(type: "bytea", nullable: false, comment: "SHA-256 del payload corrente, usato come ETag HTTP per le richieste condizionali."),
                    built_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Quando il payload corrente è stato generato."),
                    stale_since = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Da quando un evento ha segnato lo snapshot da rigenerare, con il rebuild non ancora avvenuto; NULL = aggiornato. Sostituisce il flag is_stale: la soglia di avviso/rifiuto della lettura dipende da quanto è vecchia l'invalidazione, non dal fatto che esista (rilevante solo per kind ad aggiornamento asincrono)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga (coincide con built_at a ogni rebuild che cambia qualcosa)."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete presente per uniformità di progetto; non usato funzionalmente su questa tabella di puro read-model.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_config_snapshots", x => x.id);
                    table.CheckConstraint("ck_catalog_config_snapshots_stale_after_build", "stale_since IS NULL OR stale_since >= built_at");
                    table.ForeignKey(
                        name: "fk_catalog_config_snapshots_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Read-model materializzato in jsonb della configurazione di uno stabilimento, servito wholesale al frontend; sempre ricostruibile dalle tabelle catalog_* normalizzate.");

            migrationBuilder.CreateTable(
                name: "inventory_items",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico (UUIDv7), copiato — C1 lo referenzia come resource_id nei consumi di magazzino."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento proprietario dell'articolo."),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Nome dell'articolo, nella lingua del manager."),
                    total_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Quantità totale disponibile dell'articolo; la disponibilità per data è total_quantity meno i consumi (C1)."),
                    low_stock_threshold = table.Column<int>(type: "integer", nullable: true, comment: "Soglia minima sotto la quale segnalare scorta bassa."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Articolo attivo/visibile."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access_users (C5), copiato — operatore che ha configurato l'articolo, nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete; un articolo con consumi storici (C1) non si cancella fisicamente.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_inventory_items", x => x.id);
                    table.CheckConstraint("ck_catalog_inventory_items_total_quantity", "total_quantity >= 0");
                    table.ForeignKey(
                        name: "fk_catalog_inventory_items_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Articolo fisico a corredo (ombrellone, lettino, tenda, sdraio, …) di cui uno stabilimento tiene la quantità disponibile.");

            migrationBuilder.CreateTable(
                name: "map_sectors",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, copiato cross-modulo da C1 (booking_lines.sector_id, resource_availability_daily.sector_id)."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento proprietario dei settori."),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false, comment: "Nome del settore, mostrato in tutta l'app; univoco per stabilimento (case-insensitive)."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, comment: "Ordine di visualizzazione dei settori."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha creato/modificato i settori."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete del settore (bloccato/gestito esplicitamente se ancora referenziato da spot/prezzi/sconti).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_map_sectors", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_map_sectors_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Settori/file in cui è divisa la spiaggia di uno stabilimento. Sostituisce l'array JSON sectors_rows.sectors.");

            migrationBuilder.CreateTable(
                name: "media_assets",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    origin = table.Column<int>(type: "catalog.media_origin", nullable: false, comment: "Sorgente da cui viene servito il file; la mappa origin -> URL base vive in configurazione, non a DB."),
                    path = table.Column<string>(type: "text", nullable: false, comment: "Percorso relativo rispetto alla base di origin; URL completo se origin = external."),
                    kind = table.Column<int>(type: "catalog.media_kind", nullable: false, comment: "Tipo d'uso del file (photo, pdf, icon)."),
                    checksum = table.Column<byte[]>(type: "bytea", nullable: true, comment: "Hash SHA-256 del contenuto, calcolato all'upload, per deduplica e verifica integrità. NULL per i file migrati dal legacy."),
                    byte_size = table.Column<int>(type: "integer", nullable: true, comment: "Dimensione in byte del file, nota all'upload."),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "MIME type (image/jpeg, image/png, image/svg+xml, application/pdf)."),
                    property_id = table.Column<long>(type: "bigint", nullable: true, comment: "Stabilimento a cui il file appartiene quando non esiste (ancora) una riga di aggancio: foto di stabilimenti registrati o approvati che non hanno aperto la vetrina. NULL per tutti gli asset con un utilizzatore vero, dove la riga di aggancio resta la fonte dell'appartenenza. Serve alle policy di retention per non trattare come orfano un file che non lo e'. ⚠️ bigint verso catalog.properties.ID, con VERA chiave esterna: e' la convenzione dello schema, che le altre 16 tabelle di catalog seguono. Nata come uuid verso public_id il 2026-10-03 (riferimento logico, senza FK) e corretta il 2026-10-05: l'uuid e' la forma dei riferimenti che ATTRAVERSANO i moduli, dove una FK non si puo' dichiarare, e qui non si attraversa niente."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: l'asset non è cancellato finché una riga lo referenzia.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_media_assets", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_media_assets_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catalogo locale al modulo catalog di tutti i riferimenti a file (foto, PDF, icone) usati dalle entità C2.");

            migrationBuilder.CreateTable(
                name: "pricing_discounts",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico, copiato negli snapshot sconto di C1 (booking_discount, discountData)."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento proprietario del voucher."),
                    code = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false, comment: "Codice che il cliente digita per riscattare lo sconto."),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true, comment: "Nome descrittivo del voucher, uso interno/gestore."),
                    type = table.Column<int>(type: "catalog.discount_type", nullable: false, comment: "Tipo di sconto: percentuale o importo fisso."),
                    value = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Entità dello sconto: 0-100 per percentage, euro per fixed."),
                    scope = table.Column<int>(type: "catalog.discount_scope", nullable: false, defaultValueSql: "'all'", comment: "Ambito di applicabilità del voucher (all / custom / without_custom)."),
                    applies_to_service_categories = table.Column<int[]>(type: "catalog.discount_service_category[]", nullable: false, defaultValueSql: "'{}'", comment: "Categorie di servizio a cui è limitato il voucher (scope = custom); vuoto = nessuna restrizione."),
                    applicable_channel = table.Column<int>(type: "catalog.discount_applicable_channel", nullable: false, defaultValueSql: "'manager_and_online'", comment: "Canale/i di prenotazione su cui il voucher è valido."),
                    max_uses = table.Column<int>(type: "integer", nullable: true, comment: "Numero massimo di utilizzi consentiti, aggregato (NULL = illimitato)."),
                    used_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Utilizzi già effettuati, incrementato atomicamente da C1 alla conferma prenotazione."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Voucher attivo/riscattabile."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access_users, copiato — nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete del voucher."),
                    code_normalized = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true, computedColumnSql: "upper(code)", stored: true, comment: "Proiezione upper(code) per l'indice univoco case-insensitive.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_discounts", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_discounts_max_uses", "max_uses IS NULL OR max_uses > 0");
                    table.CheckConstraint("ck_catalog_pricing_discounts_used_count", "max_uses IS NULL OR used_count <= max_uses");
                    table.CheckConstraint("ck_catalog_pricing_discounts_value", "value >= 0");
                    table.ForeignKey(
                        name: "fk_catalog_pricing_discounts_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Voucher/codice sconto configurato da uno stabilimento.");

            migrationBuilder.CreateTable(
                name: "pricing_price_lists",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, copiato in C1 booking_lines.price_list_package_id."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento proprietario della voce di listino."),
                    kind = table.Column<int>(type: "catalog.price_list_kind", nullable: false, comment: "Tipo di voce: pacchetto o articolo aggiuntivo."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome della voce di listino, testo libero digitato dal gestore."),
                    max_people = table.Column<short>(type: "smallint", nullable: true, comment: "Numero massimo di persone coperte dal pacchetto."),
                    calculation_method_id = table.Column<short>(type: "smallint", nullable: true, comment: "Metodo di calcolo del prezzo multi-giorno (NULL = non impostato)."),
                    monthly_setting = table.Column<int>(type: "catalog.monthly_setting", nullable: true, defaultValueSql: "'effective_month_duration'", comment: "Come conteggiare la durata 'mese' nel calcolo mensile."),
                    weekend_setting = table.Column<int>(type: "catalog.weekend_setting", nullable: true, defaultValueSql: "'saturday_sunday'", comment: "Quali giorni contano come weekend per la tariffa weekend."),
                    sales_channel = table.Column<int>(type: "catalog.sales_channel", nullable: false, defaultValueSql: "'manager_only'", comment: "Canali su cui la voce è vendibile (gestionale / online / entrambi)."),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Pacchetto predefinito dello stabilimento (base per prezzo minimo e Revenue Mgmt)."),
                    is_default_online = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Pacchetto predefinito per il contesto online (semantica da confermare col committente)."),
                    is_area_package = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Il pacchetto copre un'area con più posti anziché una singola postazione."),
                    is_cumulative = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Pacchetto cumulabile con altri articoli/sconti (semantica esatta da chiarire)."),
                    allows_public_holidays = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "La voce ha una tariffa dedicata per le date festive."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access_users, copiato — nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_price_lists", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_lists_calculation_method",
                        column: x => x.calculation_method_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_calculation_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_lists_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Voce di listino di uno stabilimento: un pacchetto o un articolo aggiuntivo.");

            migrationBuilder.CreateTable(
                name: "pricing_revenue_settings",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento a cui si applica la configurazione Revenue Management."),
                    is_price_discount_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Interruttore generale della feature \"notifiche prezzo scontato\"."),
                    is_shown_on_public_page = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Il banner sconto è visibile sulla pagina pubblica dello stabilimento / flusso di prenotazione (indipendente da marketing_event.showOnEstablishmentPage di C6)."),
                    daily_banner_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Numero di volte al giorno in cui mostrare il banner (finestra 10:00–20:00 divisa in altrettanti slot)."),
                    daily_banner_shown_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Numero di volte in cui il banner è già stato mostrato oggi; incrementata server-side, azzerata ogni notte."),
                    is_discount_limited_to_spots = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se true, lo sconto è ristretto alle postazioni elencate in catalog_pricing_revenue_discounted_spots invece che a tutto lo stabilimento."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_revenue_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_revenue_settings_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Configurazione per stabilimento della feature Revenue Management (prezzo suggerito e banner sconto in pagina pubblica).");

            migrationBuilder.CreateTable(
                name: "pricing_seasons",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento proprietario (relazione 1:1)."),
                    season_starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Primo giorno della stagione corrente."),
                    season_ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Ultimo giorno della stagione corrente."),
                    next_season_starts_on = table.Column<DateOnly>(type: "date", nullable: true, comment: "Primo giorno pre-impostato della stagione dell'anno successivo (rinnovo pass stagionali)."),
                    next_season_ends_on = table.Column<DateOnly>(type: "date", nullable: true, comment: "Ultimo giorno pre-impostato della stagione dell'anno successivo."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete (praticamente non usato, 1:1 con la property; presente per uniformità).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_seasons", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_seasons_next_season_range", "next_season_ends_on IS NULL OR next_season_starts_on IS NULL OR next_season_ends_on >= next_season_starts_on");
                    table.CheckConstraint("ck_catalog_pricing_seasons_season_range", "season_ends_on >= season_starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_pricing_seasons_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Confini della stagione corrente (e, opzionale, della successiva) di uno stabilimento.");

            migrationBuilder.CreateTable(
                name: "property_online_booking_settings",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento a cui si applica la configurazione."),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Canale prenotazione online postazioni attivo per lo stabilimento."),
                    is_map_pick = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Il cliente sceglie la postazione dalla mappa interattiva (vs. lista/richiesta); indipendente dagli altri due flag di restrizione."),
                    is_location_restricted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "La prenotazione online è limitata al sottoinsieme di postazioni in allowed_locations."),
                    is_period_restricted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "La prenotazione online è consentita solo nelle finestre di date in allowed_periods."),
                    surcharge_type = table.Column<int>(type: "catalog.surcharge_type", nullable: false, defaultValueSql: "'disable'", comment: "Come interpretare surcharge_value: disabilitato, percentuale o importo fisso."),
                    surcharge_value = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Importo o percentuale del sovrapprezzo sulle prenotazioni online (calcolato server-side dal motore prezzo)."),
                    is_online_payment_blocked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se true, il cliente non può pagare online: override che blocca il pagamento a prescindere da enabled_payment_methods."),
                    requires_manual_confirmation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Le prenotazioni online restano \"da confermare\" e richiedono approvazione manuale del gestore."),
                    allows_same_day_booking = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Il cliente può prenotare online per il giorno stesso; se false, solo da domani in poi."),
                    allows_half_day = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Abilita la prenotazione online per mezza giornata (mattina/pomeriggio)."),
                    allows_seasonal = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Abilita la prenotazione online \"stagionale\" (pass multi-settimana/mese)."),
                    allows_sharing = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Espone al cliente la funzione di sharing durante la prenotazione online (i parametri operativi vivono in C1)."),
                    max_spots_per_booking = table.Column<short>(type: "smallint", nullable: true, comment: "Numero massimo di postazioni prenotabili in una singola prenotazione online; NULL = nessun limite."),
                    allow_online_booking_scope = table.Column<int>(type: "catalog.online_booking_scope", nullable: false, defaultValueSql: "'all'", comment: "Chi può prenotare online: tutti, o solo clienti già in rubrica (contacts_only)."),
                    useful_info = table.Column<string>(type: "text", nullable: true, comment: "Testo libero \"informazioni utili\" mostrato al cliente nel flusso online."),
                    good_to_know = table.Column<string>(type: "text", nullable: true, comment: "Testo libero aggiuntivo \"da sapere\" mostrato al cliente."),
                    enabled_payment_methods = table.Column<int[]>(type: "catalog.online_payment_method[]", nullable: false, defaultValueSql: "'{}'", comment: "Insieme dei metodi di pagamento offerti al cliente per la prenotazione online postazioni."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della configurazione."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della configurazione."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete (non usato in pratica: per disattivare il canale si usa is_enabled = false).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_property_online_booking_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_property_online_booking_settings_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Configurazione del canale di prenotazione online delle postazioni per uno stabilimento (1:1).");

            migrationBuilder.CreateTable(
                name: "property_service_booking_settings",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento a cui si applica la configurazione."),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Canale prenotazione online servizi attivo."),
                    surcharge_type = table.Column<int>(type: "catalog.surcharge_type", nullable: false, defaultValueSql: "'disable'", comment: "Come interpretare surcharge_value: disabilitato, percentuale o importo fisso."),
                    surcharge_value = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Importo o percentuale del sovrapprezzo sulle prenotazioni online di servizi."),
                    requires_manual_confirmation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Le prenotazioni servizi online restano \"da confermare\" e richiedono approvazione manuale del gestore."),
                    section_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Titolo personalizzato della sezione \"prenota i servizi online\" sul sito pubblico."),
                    useful_info = table.Column<string>(type: "text", nullable: true, comment: "Testo informativo mostrato durante la prenotazione online di un servizio."),
                    enabled_payment_methods = table.Column<int[]>(type: "catalog.online_payment_method[]", nullable: false, defaultValueSql: "'{}'", comment: "Insieme dei metodi di pagamento offerti per la prenotazione online di servizi."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della configurazione."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della configurazione."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_property_service_booking_settings", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_property_service_booking_settings_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Configurazione del canale di prenotazione online dei servizi (bar/normali/a ore) per uno stabilimento (1:1).");

            migrationBuilder.CreateTable(
                name: "agreement_price_lists",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico (UUIDv7), copiato — C1 booking_agreement_lines referenzia il listino/voucher consumato."),
                    partner_id = table.Column<int>(type: "integer", nullable: false, comment: "Struttura partner a cui appartiene il listino/voucher."),
                    list_type = table.Column<int>(type: "catalog.agreement_list_type", nullable: false, defaultValueSql: "'regular'", comment: "regular = listino ordinario; voucher = buono riscattabile presso la struttura partner, con vincoli d'uso."),
                    kind = table.Column<int>(type: "catalog.price_list_kind", nullable: false, defaultValueSql: "'package'", comment: "Tipo di voce: pacchetto o articolo aggiuntivo (enum condiviso con catalog_pricing_price_lists)."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome del listino/voucher (testo libero digitato dal manager)."),
                    max_people = table.Column<short>(type: "smallint", nullable: true, comment: "Numero massimo di persone coperte."),
                    calculation_method_id = table.Column<short>(type: "smallint", nullable: true, comment: "Metodo di calcolo prezzo multi-giorno (lookup condiviso catalog_pricing_calculation_methods)."),
                    monthly_setting = table.Column<int>(type: "catalog.monthly_setting", nullable: true, defaultValueSql: "'effective_month_duration'", comment: "Impostazione durata mensile (enum condiviso con catalog_pricing_price_lists)."),
                    weekend_setting = table.Column<int>(type: "catalog.weekend_setting", nullable: true, defaultValueSql: "'saturday_sunday'", comment: "Impostazione giorni weekend (enum condiviso con catalog_pricing_price_lists)."),
                    allows_public_holidays = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Indica se il listino/voucher prevede una tariffa festiva dedicata."),
                    min_booking_days = table.Column<short>(type: "smallint", nullable: true, comment: "Durata minima di soggiorno perché il voucher sia applicabile (solo list_type = voucher)."),
                    max_voucher_quantity = table.Column<short>(type: "smallint", nullable: true, comment: "Numero massimo di utilizzi del voucher per cliente per stagione; NULL = nessun limite (solo list_type = voucher)."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access_users (C5), copiato — operatore che ha creato il listino, nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete, anche a cascata dalla cancellazione della struttura partner.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_agreement_price_lists", x => x.id);
                    table.CheckConstraint("ck_catalog_agreement_price_lists_max_voucher_quantity", "list_type = 'voucher' OR max_voucher_quantity IS NULL");
                    table.CheckConstraint("ck_catalog_agreement_price_lists_min_booking_days", "list_type = 'voucher' OR min_booking_days IS NULL");
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_lists_calculation_method",
                        column: x => x.calculation_method_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_calculation_methods",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_lists_partner",
                        column: x => x.partner_id,
                        principalSchema: "catalog",
                        principalTable: "agreement_partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Listino ordinario o voucher venduto da una struttura partner (convenzione).");

            migrationBuilder.CreateTable(
                name: "agreement_reports",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico (UUIDv7) del report, per il pannello e per i link di invio."),
                    partner_id = table.Column<int>(type: "integer", nullable: false, comment: "Struttura partner destinataria del report."),
                    search_type = table.Column<int>(type: "catalog.agreement_report_search_type", nullable: false, defaultValueSql: "'check_in'", comment: "Se l'intervallo di date filtra le prenotazioni per arrivo (check_in) o per partenza (check_out)."),
                    period_start = table.Column<DateOnly>(type: "date", nullable: false, comment: "Primo giorno del periodo coperto dal report. E' un giorno, non un istante."),
                    period_end = table.Column<DateOnly>(type: "date", nullable: false, comment: "Ultimo giorno del periodo coperto dal report, incluso."),
                    status = table.Column<int>(type: "catalog.agreement_report_status", nullable: false, defaultValueSql: "'pending'", comment: "Stato del report: pending, created o cancelled. Distinto da deleted_at, che e' il soft-delete della riga: nel legacy i due erano scritti sempre insieme."),
                    report_data = table.Column<string>(type: "jsonb", nullable: true, comment: "Cache dei dati del report, NON la fonte: il dato grezzo resta in booking.* e il documento e' ricostruibile da la'. Serve a ristampare esattamente cio' che e' stato inviato."),
                    total_reservations = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Numero di prenotazioni comprese nel periodo, al momento del calcolo."),
                    total_revenue = table.Column<decimal>(type: "numeric(12,2)", nullable: false, defaultValue: 0m, comment: "Ricavo totale del periodo, al momento del calcolo."),
                    generated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Utente che ha generato il report: riferimento logico a identity.access_users.public_id, non FK (altro schema)."),
                    notes = table.Column<string>(type: "text", nullable: true, comment: "Note libere di chi gestisce il report."),
                    pdf_media_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "PDF archiviato all'invio: riferimento logico a notification.media_assets (kind = 'rendered_pdf'), NON una FK perche' attraversa lo schema di un altro modulo. NULL finche' il report non e' stato inviato. ATTENZIONE: sostituire il PDF richiede di accodare il vecchio oggetto in privacy.privacy_file_deletions, altrimenti si ripete il difetto del legacy che ha lasciato 24 oggetti orfani su 28."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della riga (Global Query Filter EF standard).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_agreement_reports", x => x.id);
                    table.CheckConstraint("ck_catalog_agreement_reports_period", "period_end >= period_start");
                    table.CheckConstraint("ck_catalog_agreement_reports_totals", "total_reservations >= 0 AND total_revenue >= 0");
                    table.ForeignKey(
                        name: "fk_catalog_agreement_reports_partner",
                        column: x => x.partner_id,
                        principalSchema: "catalog",
                        principalTable: "agreement_partners",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Report periodico (arrivi/partenze/ricavi) che uno stabilimento invia a una struttura partner. report_data e' una cache: il dato grezzo resta in booking.*. Il PDF archiviato all'invio sta in notification.media_assets (kind = 'rendered_pdf').");

            migrationBuilder.CreateTable(
                name: "map_spots",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, copiato cross-modulo (C1 resource_id, snapshot mappa)."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento proprietario della postazione."),
                    sector_id = table.Column<long>(type: "bigint", nullable: true, comment: "Settore a cui appartiene la postazione."),
                    label = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Etichetta/numero della postazione visibile al cliente e all'operatore (es. A12); univoca per stabilimento."),
                    spot_type = table.Column<int>(type: "catalog.spot_type", nullable: false, comment: "Tipo di elemento posizionato nella cella."),
                    is_area = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "True se la postazione rappresenta un'area con più posti, non una singola postazione."),
                    area_seat_count = table.Column<short>(type: "smallint", nullable: true, comment: "Numero di posti dell'area, rilevante solo se is_area = true."),
                    grid_row = table.Column<short>(type: "smallint", nullable: true, comment: "Riga della postazione nella griglia del layout."),
                    grid_column = table.Column<short>(type: "smallint", nullable: true, comment: "Colonna della postazione nella griglia del layout."),
                    is_bookable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "La postazione è offerta alla prenotazione (esclude celle vuote/decorative della griglia)."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha configurato la mappa."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: la postazione non si cancella fisicamente se ha prenotazioni storiche in C1.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_map_spots", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_map_spots_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_map_spots_sector",
                        column: x => x.sector_id,
                        principalSchema: "catalog",
                        principalTable: "map_sectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Anagrafica delle postazioni (ombrelloni, lettini, cabine, parcheggi, ...) di uno stabilimento: una riga per postazione. Sostituisce il blob JSON configurations.beachMapData.");

            migrationBuilder.CreateTable(
                name: "bathhouses",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, copiato cross-modulo (C5 identity_customer_bathhouse_likes.bathhouse_id) e usato in URL/API pubbliche."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Property (tenant) proprietaria della vetrina."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Nome commerciale pubblico; sorgente dello slug."),
                    slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Identificatore leggibile nell'URL pubblico, generato una sola volta alla prima pubblicazione."),
                    description = table.Column<string>(type: "text", nullable: true, comment: "Descrizione libera (HTML sanificato)."),
                    regulation = table.Column<string>(type: "text", nullable: true, comment: "Regolamento interno (HTML sanificato)."),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Indirizzo pubblico."),
                    city = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Città della vetrina."),
                    cap = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true, comment: "CAP."),
                    province = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Provincia."),
                    region = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Regione, usata dalla ricerca pubblica per area."),
                    country_id = table.Column<short>(type: "smallint", nullable: true, comment: "Paese della vetrina."),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true, comment: "Latitudine GPS per mappa e ricerca geografica."),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true, comment: "Longitudine GPS per mappa e ricerca geografica."),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Telefono pubblico."),
                    email = table.Column<string>(type: "citext", nullable: true, comment: "Email pubblica."),
                    website = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Sito web."),
                    facebook = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Link Facebook."),
                    instagram = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Link Instagram."),
                    whatsapp = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Numero WhatsApp pubblico."),
                    open_hours_from = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Orario di apertura giornaliero."),
                    open_hours_to = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Orario di chiusura giornaliero."),
                    is_free_cancellation = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Cancellazione gratuita entro un preavviso."),
                    cancellation_notice_hours = table.Column<short>(type: "smallint", nullable: true, comment: "Ore di preavviso richieste, valorizzato solo se is_free_cancellation = true."),
                    pets_policy = table.Column<int>(type: "catalog.pets_policy", nullable: false, defaultValueSql: "'not_allowed'", comment: "Politica animali della vetrina."),
                    main_photo_id = table.Column<long>(type: "bigint", nullable: true, comment: "Foto di copertina (kind = photo)."),
                    menu_pdf_id = table.Column<long>(type: "bigint", nullable: true, comment: "PDF menu bar (kind = pdf)."),
                    price_list_pdf_id = table.Column<long>(type: "bigint", nullable: true, comment: "PDF listino cartaceo (kind = pdf)."),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando la vetrina è stata resa pubblica per la prima volta; NULL = bozza/non pubblicata."),
                    updated_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Account che ha salvato per ultimo la vetrina."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della vetrina.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_bathhouses", x => x.id);
                    table.CheckConstraint("ck_catalog_bathhouses_cancellation_notice", "is_free_cancellation OR cancellation_notice_hours IS NULL");
                    table.ForeignKey(
                        name: "fk_catalog_bathhouses_country",
                        column: x => x.country_id,
                        principalSchema: "catalog",
                        principalTable: "countries",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_bathhouses_main_photo",
                        column: x => x.main_photo_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_catalog_bathhouses_menu_pdf",
                        column: x => x.menu_pdf_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_catalog_bathhouses_price_list_pdf",
                        column: x => x.price_list_pdf_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_catalog_bathhouses_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Vetrina pubblica dello stabilimento: nome commerciale, indirizzo, contatti, descrizione, foto, geolocalizzazione, orari, politiche.");

            migrationBuilder.CreateTable(
                name: "map_icons",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    category_id = table.Column<short>(type: "smallint", nullable: false, comment: "Categoria dell'icona (catalog.map_icon_categories)."),
                    type = table.Column<int>(type: "catalog.map_icon_state", nullable: false, comment: "Stato rappresentato dall'icona; ignorato (ma valorizzato) per category = template."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome descrittivo mostrato nel selettore del gestionale."),
                    media_asset_id = table.Column<long>(type: "bigint", nullable: false, comment: "Il file icona (catalog.media_assets, kind = icon)."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Icona selezionabile nel pannello SuperAdmin."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete; la rimozione del file su storage è demandata a un job asincrono.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_map_icons", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_map_icons_category",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "map_icon_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_map_icons_media_asset",
                        column: x => x.media_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catalogo globale/master delle icone/immagini per comporre la mappa spiaggia: sfondi, varianti ombrellone/cabina/tenda/lettino nei due stati.");

            migrationBuilder.CreateTable(
                name: "map_layouts",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento proprietario del layout (1:1)."),
                    grid_rows = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Numero di righe della griglia."),
                    grid_columns = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Numero di colonne della griglia (legacy: configure_maps.items, rinominato per eliminare l'ambiguità)."),
                    is_custom_template = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "True se lo sfondo è un'immagine caricata dal gestore; false se è un template predefinito."),
                    template_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Immagine di sfondo della griglia (catalog.media_assets, kind = icon)."),
                    background_color = table.Column<string>(type: "char(7)", nullable: true, comment: "Colore di sfondo/tema della griglia, hex #RRGGBB."),
                    umbrella_occupied_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Icona dell'ombrellone standard nello stato occupato."),
                    umbrella_free_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Icona dell'ombrellone standard nello stato libero."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha configurato il layout."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete del layout.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_map_layouts", x => x.id);
                    table.CheckConstraint("ck_catalog_map_layouts_background_color", "background_color IS NULL OR background_color ~ '^#[0-9A-Fa-f]{6}$'");
                    table.ForeignKey(
                        name: "fk_catalog_map_layouts_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_map_layouts_template_asset",
                        column: x => x.template_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_catalog_map_layouts_umbrella_free_asset",
                        column: x => x.umbrella_free_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_catalog_map_layouts_umbrella_occupied_asset",
                        column: x => x.umbrella_occupied_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id");
                },
                comment: "Layout grafico della mappa spiaggia di uno stabilimento: griglia, sfondo, colore, icone per tipo elemento nei due stati.");

            migrationBuilder.CreateTable(
                name: "map_legend_items",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento a cui appartiene la legenda."),
                    label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Testo della voce di legenda (es. \"Ombrellone libero\")."),
                    icon_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Icona della voce di legenda (catalog.media_assets, kind = icon)."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, comment: "Ordine di visualizzazione della voce."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della voce di legenda.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_map_legend_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_map_legend_items_icon_asset",
                        column: x => x.icon_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_catalog_map_legend_items_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Voci della legenda mostrata sulla mappa di prenotazione online del sito pubblico. Sostituisce l'array JSON properties.onlineBookingOptions.locationOptionObj.legendList.");

            migrationBuilder.CreateTable(
                name: "service_amenities",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Nome della dotazione nella lingua di default (le traduzioni vivono in service_amenity_translations)."),
                    category_id = table.Column<short>(type: "smallint", nullable: true, comment: "Categoria a cui appartiene la dotazione; NULL = non categorizzata, esclusa dai filtri di ricerca."),
                    icon_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Icona della dotazione, mostrata nel selettore gestionale e nella scheda pubblica."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Dotazione selezionabile/mostrata (il catalogo pubblico restituisce solo le attive)."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordine di visualizzazione dentro la categoria."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della dotazione."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della dotazione."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_amenities", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_service_amenities_category",
                        column: x => x.category_id,
                        principalSchema: "catalog",
                        principalTable: "service_amenity_categories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_catalog_service_amenities_icon_asset",
                        column: x => x.icon_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catalogo master delle dotazioni/servizi che uno stabilimento può dichiarare di offrire (es. Wi-Fi, Doccia, Bar).");

            migrationBuilder.CreateTable(
                name: "service_type_icons",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    type = table.Column<int>(type: "catalog.service_sale_kind", nullable: false, comment: "Tipo di servizio a cui l'icona è destinata."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome descrittivo dell'icona (etichetta/tooltip nel selettore)."),
                    icon_asset_id = table.Column<long>(type: "bigint", nullable: false, comment: "Il file icona."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Icona selezionabile."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione dell'icona."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica dell'icona."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_type_icons", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_service_type_icons_icon_asset",
                        column: x => x.icon_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Catalogo master delle icone disponibili per i tre tipi di servizio venduto (bar/normale/a ore).");

            migrationBuilder.CreateTable(
                name: "pricing_discount_date_ranges",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    discount_id = table.Column<long>(type: "bigint", nullable: false, comment: "Voucher a cui appartiene la finestra di validità."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Inizio dell'intervallo in cui il voucher è riscattabile."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fine dell'intervallo in cui il voucher è riscattabile."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della finestra di validità.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_discount_date_ranges", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_discount_date_ranges_range", "ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_pricing_discount_date_ranges_discount",
                        column: x => x.discount_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_discounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Finestra di validità (una fra eventualmente più) di un voucher sconto.");

            migrationBuilder.CreateTable(
                name: "pricing_discount_sectors",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    discount_id = table.Column<long>(type: "bigint", nullable: false, comment: "Voucher a cui si applica la restrizione di settore."),
                    sector_id = table.Column<long>(type: "bigint", nullable: false, comment: "Settore a cui il voucher è limitato."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: valorizzata quando la restrizione viene rimossa.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_discount_sectors", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_discount_sectors_discount",
                        column: x => x.discount_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_discounts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_discount_sectors_sector",
                        column: x => x.sector_id,
                        principalSchema: "catalog",
                        principalTable: "map_sectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Settori a cui un voucher (discount) è ristretto; assenza di righe = nessuna restrizione.");

            migrationBuilder.CreateTable(
                name: "pricing_price_list_period_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    price_list_id = table.Column<long>(type: "bigint", nullable: false, comment: "Pacchetto a cui si riferiscono i prezzi."),
                    tariff_period_group = table.Column<short>(type: "smallint", nullable: true, comment: "Gruppo di periodo tariffario (period_group_key di catalog_pricing_tariff_periods per la stagione della property); NULL = tariffa senza periodo."),
                    half_day_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo mezza giornata nel periodo."),
                    half_day_weekend_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo mezza giornata weekend nel periodo."),
                    daily_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo giornaliero nel periodo."),
                    weekend_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo giornaliero weekend nel periodo."),
                    days_7_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo per soggiorno di 7 giorni nel periodo."),
                    days_14_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo per soggiorno di 14 giorni nel periodo."),
                    monthly_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo mensile nel periodo."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della riga tariffaria.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_price_list_period_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_period_prices_price_list",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tariffe base (giornaliera/weekend/mezza giornata/7-14gg/mensile) di un pacchetto per periodo tariffario.");

            migrationBuilder.CreateTable(
                name: "pricing_price_list_reduced_windows",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    price_list_id = table.Column<long>(type: "bigint", nullable: false, comment: "Pacchetto a cui si applica la finestra di prezzo ridotto."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Inizio dell'intervallo che deve coincidere esattamente con quello della prenotazione."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fine dell'intervallo che deve coincidere esattamente con quello della prenotazione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della finestra.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_price_list_reduced_windows", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_price_list_reduced_windows_range", "ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_reduced_windows_price_list",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Finestra di date che, se coincide esattamente con quella prenotata, attiva i prezzi ridotti per settore.");

            migrationBuilder.CreateTable(
                name: "pricing_price_list_sector_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    price_list_id = table.Column<long>(type: "bigint", nullable: false, comment: "Pacchetto a cui si riferisce il prezzo."),
                    sector_id = table.Column<long>(type: "bigint", nullable: false, comment: "Settore a cui si applica il prezzo/la disponibilità."),
                    base_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo stagionale forfettario del pacchetto nel settore (fallback rispetto ai prezzi per periodo)."),
                    half_day_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo forfettario mezza giornata nel settore (allineamento con i listini hotel; spesso NULL sui price_lists puri)."),
                    seasonal_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo per l'intera stagione (pass stagionale) nel settore."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della riga.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_price_list_sector_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_sector_prices_price_list",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_sector_prices_sector",
                        column: x => x.sector_id,
                        principalSchema: "catalog",
                        principalTable: "map_sectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Prezzo base per settore di un pacchetto; l'esistenza della riga marca anche l'applicabilità al settore.");

            migrationBuilder.CreateTable(
                name: "pricing_suggested_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    price_list_id = table.Column<long>(type: "bigint", nullable: false, comment: "Pacchetto a cui si riferisce il prezzo suggerito."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento, denormalizzato per filtri/indici."),
                    price_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Giorno a cui si riferisce il prezzo suggerito."),
                    period_label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true, comment: "Etichetta libera del periodo/settore legacy (es. \"Fila 1 Alta stagione\"); NULL quando il calcolo non distingue varianti per la data. Aggiunta per D-63/D-64."),
                    list_daily_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo di listino giornaliero di riferimento al momento del calcolo (snapshot)."),
                    list_weekend_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo di listino weekend di riferimento al momento del calcolo (snapshot)."),
                    suggested_price = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Prezzo suggerito dall'algoritmo per la data."),
                    discount_percentage = table.Column<decimal>(type: "numeric(5,2)", nullable: false, defaultValue: 0m, comment: "Sconto percentuale suggerito, alternativa al prezzo assoluto."),
                    applied_mode = table.Column<int>(type: "catalog.suggested_price_mode", nullable: false, defaultValueSql: "'none'", comment: "Cosa ha deciso il gestore per la data: nulla, prezzo suggerito, prezzo originale o sconto."),
                    computed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Momento in cui l'algoritmo ha prodotto la riga."),
                    applied_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Momento in cui il gestore ha applicato il prezzo/sconto suggerito."),
                    applied_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access_users, copiato — nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete (tabella di output algoritmico; le righe con data passata sono candidate a retention hard-delete via job).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_suggested_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_suggested_prices_price_list",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_suggested_prices_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Prezzo suggerito dall'algoritmo di Revenue Management per un pacchetto in una data specifica.");

            migrationBuilder.CreateTable(
                name: "pricing_tariff_periods",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    season_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stagione a cui appartiene il periodo."),
                    name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, comment: "Nome del periodo tariffario (es. Alta stagione); solo etichetta, le correlazioni sono per id."),
                    period_group_key = table.Column<short>(type: "smallint", nullable: false, comment: "Chiave che raggruppa gli intervalli non contigui dello stesso periodo tariffario."),
                    tier = table.Column<int>(type: "catalog.tariff_tier", nullable: false, defaultValueSql: "'custom'", comment: "Livello tariffario normalizzato, indipendente dal nome/lingua."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Inizio dell'intervallo di date coperto dalla riga."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Fine dell'intervallo di date coperto dalla riga."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete dell'intervallo.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_tariff_periods", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_tariff_periods_range", "ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_pricing_tariff_periods_season",
                        column: x => x.season_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_seasons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Intervallo di date di un periodo tariffario (bassa/media/alta stagione), eventualmente non contiguo.");

            migrationBuilder.CreateTable(
                name: "property_online_allowed_periods",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    settings_id = table.Column<long>(type: "bigint", nullable: false, comment: "Configurazione canale online a cui appartiene la finestra."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Primo giorno della finestra (incluso)."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Ultimo giorno della finestra (incluso)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_property_online_allowed_periods", x => x.id);
                    table.CheckConstraint("ck_catalog_property_online_allowed_periods_range", "ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_property_online_allowed_periods_settings",
                        column: x => x.settings_id,
                        principalSchema: "catalog",
                        principalTable: "property_online_booking_settings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Finestra di date in cui è consentita la prenotazione online quando is_period_restricted è true.");

            migrationBuilder.CreateTable(
                name: "property_online_stay_limits",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    settings_id = table.Column<long>(type: "bigint", nullable: false, comment: "Configurazione canale online a cui appartiene il limite."),
                    sector_id = table.Column<long>(type: "bigint", nullable: false, comment: "Settore a cui si applica il limite."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Primo giorno di validità del limite (incluso)."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Ultimo giorno di validità del limite (incluso)."),
                    min_days = table.Column<short>(type: "smallint", nullable: true, comment: "Numero minimo di giorni consecutivi prenotabili online per il settore in quella finestra; NULL = nessun minimo."),
                    max_days = table.Column<short>(type: "smallint", nullable: true, comment: "Numero massimo di giorni consecutivi prenotabili online per il settore in quella finestra; NULL = nessun massimo."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_property_online_stay_limits", x => x.id);
                    table.CheckConstraint("ck_catalog_property_online_stay_limits_days", "max_days IS NULL OR min_days IS NULL OR max_days >= min_days");
                    table.CheckConstraint("ck_catalog_property_online_stay_limits_range", "ends_on >= starts_on");
                    table.ForeignKey(
                        name: "fk_catalog_property_online_stay_limits_sector",
                        column: x => x.sector_id,
                        principalSchema: "catalog",
                        principalTable: "map_sectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_property_online_stay_limits_settings",
                        column: x => x.settings_id,
                        principalSchema: "catalog",
                        principalTable: "property_online_booking_settings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Limite minimo/massimo di giorni consecutivi prenotabili online, per settore e finestra di date.");

            migrationBuilder.CreateTable(
                name: "agreement_price_list_period_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    price_list_id = table.Column<int>(type: "integer", nullable: false, comment: "Listino/voucher hotel a cui appartiene il prezzo di periodo."),
                    period_label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true, comment: "Etichetta libera del periodo tariffario; NULL = tariffa unica del listino (nessun collegamento a stagioni)."),
                    half_day_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo mezza giornata nel periodo."),
                    half_day_weekend_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo mezza giornata weekend nel periodo."),
                    daily_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo giornaliero nel periodo."),
                    weekend_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo weekend nel periodo."),
                    days_7_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo per soggiorno di 7 giorni nel periodo."),
                    days_14_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo per soggiorno di 14 giorni nel periodo."),
                    monthly_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo mensile nel periodo."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_agreement_price_list_period_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_list_period_prices_price_list",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "agreement_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Prezzi per periodo tariffario (etichetta libera) di un listino/voucher hotel, per modalità di durata.");

            migrationBuilder.CreateTable(
                name: "agreement_price_list_sector_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    price_list_id = table.Column<int>(type: "integer", nullable: false, comment: "Listino/voucher hotel a cui appartiene il prezzo di settore."),
                    sector_id = table.Column<long>(type: "bigint", nullable: false, comment: "Settore (della property del partner) a cui si applica il prezzo."),
                    base_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo giornaliero base nel settore."),
                    half_day_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo mezza giornata nel settore."),
                    seasonal_price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo stagionale nel settore."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_agreement_price_list_sector_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_list_sector_prices_price_list",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "agreement_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_list_sector_prices_sector",
                        column: x => x.sector_id,
                        principalSchema: "catalog",
                        principalTable: "map_sectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Prezzo base / mezza giornata / stagionale di un listino/voucher hotel per settore.");

            migrationBuilder.CreateTable(
                name: "pricing_revenue_discounted_spots",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    settings_id = table.Column<long>(type: "bigint", nullable: false, comment: "Configurazione Revenue Management a cui appartiene la restrizione."),
                    spot_id = table.Column<long>(type: "bigint", nullable: false, comment: "Postazione a cui si applica lo sconto."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: valorizzata quando la restrizione viene rimossa.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_revenue_discounted_spots", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_revenue_discounted_spots_settings",
                        column: x => x.settings_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_revenue_settings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_revenue_discounted_spots_spot",
                        column: x => x.spot_id,
                        principalSchema: "catalog",
                        principalTable: "map_spots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Postazioni a cui si applica lo sconto Revenue Management quando la configurazione è limitata a postazioni specifiche.");

            migrationBuilder.CreateTable(
                name: "property_online_allowed_locations",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    settings_id = table.Column<long>(type: "bigint", nullable: false, comment: "Configurazione canale online a cui appartiene la restrizione."),
                    spot_id = table.Column<long>(type: "bigint", nullable: false, comment: "La postazione ammessa alla prenotazione online."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_property_online_allowed_locations", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_property_online_allowed_locations_settings",
                        column: x => x.settings_id,
                        principalSchema: "catalog",
                        principalTable: "property_online_booking_settings",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_property_online_allowed_locations_spot",
                        column: x => x.spot_id,
                        principalSchema: "catalog",
                        principalTable: "map_spots",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Postazione ammessa alla prenotazione online quando is_location_restricted è true.");

            migrationBuilder.CreateTable(
                name: "bathhouse_photos",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    bathhouse_id = table.Column<long>(type: "bigint", nullable: false, comment: "Vetrina a cui appartiene la foto."),
                    media_asset_id = table.Column<long>(type: "bigint", nullable: false, comment: "Il file immagine (catalog.media_assets, kind = photo)."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, comment: "Ordine di visualizzazione in galleria (0-based); il riordino aggiorna solo questa colonna."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: foto rimossa dalla galleria.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_bathhouse_photos", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_bathhouse_photos_bathhouse",
                        column: x => x.bathhouse_id,
                        principalSchema: "catalog",
                        principalTable: "bathhouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_bathhouse_photos_media_asset",
                        column: x => x.media_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Elenco ordinato delle foto della galleria pubblica di una vetrina. Sostituisce l'array JSON bathouses.photos.");

            migrationBuilder.CreateTable(
                name: "map_layout_special_icons",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    layout_id = table.Column<long>(type: "bigint", nullable: false, comment: "Layout a cui appartiene la variante."),
                    variant = table.Column<short>(type: "smallint", nullable: false, comment: "Indice della variante speciale (1-5, corrispondente a SpecialUmbrella1..5 nel legacy); estendibile."),
                    state = table.Column<int>(type: "catalog.map_icon_state", nullable: false, comment: "Stato rappresentato dall'icona (occupied/free)."),
                    media_asset_id = table.Column<long>(type: "bigint", nullable: false, comment: "Icona della variante (catalog.media_assets)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della variante.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_map_layout_special_icons", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_map_layout_special_icons_layout",
                        column: x => x.layout_id,
                        principalSchema: "catalog",
                        principalTable: "map_layouts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_map_layout_special_icons_media_asset",
                        column: x => x.media_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Icone delle varianti speciali di ombrellone (1-5) per stato occupato/libero. Sostituisce le colonne specialUmbrella1..5 di configure_maps.");

            migrationBuilder.CreateTable(
                name: "agreement_price_list_items",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    price_list_id = table.Column<int>(type: "integer", nullable: false, comment: "Listino/voucher a cui appartiene la componente."),
                    item_kind = table.Column<int>(type: "catalog.package_item_kind", nullable: false, comment: "Natura della componente: articolo di magazzino, articolo d'area, servizio incluso o pacchetto collegato."),
                    inventory_item_id = table.Column<long>(type: "bigint", nullable: true, comment: "Articolo di magazzino incluso (valorizzato solo per item_kind inventory_item/area_item)."),
                    included_service_id = table.Column<int>(type: "integer", nullable: true, comment: "Dotazione/servizio incluso."),
                    linked_price_list_id = table.Column<int>(type: "integer", nullable: true, comment: "Listino hotel a cui questo additional è vincolato."),
                    quantity = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1, comment: "Quantità dell'articolo di magazzino inclusa; 1 per servizi inclusi/pacchetti collegati."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_agreement_price_list_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_list_items_included_service",
                        column: x => x.included_service_id,
                        principalSchema: "catalog",
                        principalTable: "service_amenities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_list_items_inventory_item",
                        column: x => x.inventory_item_id,
                        principalSchema: "catalog",
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_list_items_linked_price_list",
                        column: x => x.linked_price_list_id,
                        principalSchema: "catalog",
                        principalTable: "agreement_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_list_items_price_list",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "agreement_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Componente di un listino/voucher hotel: articolo di magazzino, servizio incluso o pacchetto collegato.");

            migrationBuilder.CreateTable(
                name: "bathhouse_amenities",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    bathhouse_id = table.Column<long>(type: "bigint", nullable: false, comment: "Vetrina che offre l'amenity."),
                    amenity_id = table.Column<int>(type: "integer", nullable: false, comment: "La dotazione/servizio offerto (catalog.service_amenities)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga (selezione dell'amenity)."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: amenity deselezionata dalla vetrina.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_bathhouse_amenities", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_bathhouse_amenities_amenity",
                        column: x => x.amenity_id,
                        principalSchema: "catalog",
                        principalTable: "service_amenities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_bathhouse_amenities_bathhouse",
                        column: x => x.bathhouse_id,
                        principalSchema: "catalog",
                        principalTable: "bathhouses",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Giunzione N:N: quali dotazioni/servizi (amenity) offre una vetrina. Sostituisce la copia JSON integrale bathouses.services.");

            migrationBuilder.CreateTable(
                name: "pricing_price_list_items",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    price_list_id = table.Column<long>(type: "bigint", nullable: false, comment: "Voce di listino a cui appartiene la componente."),
                    item_kind = table.Column<int>(type: "catalog.package_item_kind", nullable: false, comment: "Natura della componente: determina quale colonna di riferimento è valorizzata."),
                    inventory_item_id = table.Column<long>(type: "bigint", nullable: true, comment: "Articolo di magazzino incluso (per inventory_item / area_item)."),
                    included_service_id = table.Column<int>(type: "integer", nullable: true, comment: "Servizio/dotazione incluso nel pacchetto (per included_service)."),
                    linked_price_list_id = table.Column<long>(type: "bigint", nullable: true, comment: "Pacchetto a cui questo additional è vincolato (per linked_package)."),
                    quantity = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)1, comment: "Quantità dell'articolo di magazzino inclusa nel pacchetto (1 per service/linked_package)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della componente.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_price_list_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_items_included_service",
                        column: x => x.included_service_id,
                        principalSchema: "catalog",
                        principalTable: "service_amenities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_items_inventory_item",
                        column: x => x.inventory_item_id,
                        principalSchema: "catalog",
                        principalTable: "inventory_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_items_linked_price_list",
                        column: x => x.linked_price_list_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_items_price_list",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Componente di una voce di listino: articolo incluso, servizio incluso o pacchetto vincolato.");

            migrationBuilder.CreateTable(
                name: "service_amenity_translations",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    amenity_id = table.Column<int>(type: "integer", nullable: false, comment: "Dotazione a cui si riferisce la traduzione."),
                    culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, comment: "Codice cultura BCP-47 (es. it-IT, en-GB)."),
                    name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Nome della dotazione nella lingua indicata."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della traduzione."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della traduzione."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_amenity_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_service_amenity_translations_amenity",
                        column: x => x.amenity_id,
                        principalSchema: "catalog",
                        principalTable: "service_amenities",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Nome di una dotazione (catalog_service_amenities) in una specifica culture.");

            migrationBuilder.CreateTable(
                name: "service_sale_items",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico, copiato cross-modulo: C1 lo usa per contatori/consumi del servizio."),
                    property_id = table.Column<long>(type: "bigint", nullable: false, comment: "Stabilimento che vende il servizio."),
                    kind = table.Column<int>(type: "catalog.service_sale_kind", nullable: false, comment: "Tipo di servizio: bar, normale, a fascia oraria."),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Nome del servizio mostrato al cliente."),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Prezzo unitario del servizio (sovrascrivibile dalla fascia oraria per kind = hourly)."),
                    icon_id = table.Column<int>(type: "integer", nullable: true, comment: "Icona scelta dal catalogo master; alternativa a icon_asset_id."),
                    icon_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Icona caricata ad hoc; alternativa a icon_id."),
                    is_purchasable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Il servizio è acquistabile (vs. solo elencato/disattivato)."),
                    active_weekdays = table.Column<int[]>(type: "catalog.weekday[]", nullable: false, defaultValueSql: "'{}'", comment: "Giorni della settimana in cui il servizio e' attivo. Sostituisce la tabella figlia a sette righe con flag: insieme chiuso, piccolo, sempre letto e scritto insieme al padre. Array vuoto = servizio mai prenotabile."),
                    hour_from = table.Column<TimeOnly>(type: "time without time zone", nullable: true, comment: "Inizio della fascia oraria complessiva di disponibilità (solo kind = hourly)."),
                    hour_to = table.Column<TimeOnly>(type: "time without time zone", nullable: true, comment: "Fine della fascia oraria complessiva di disponibilità (solo kind = hourly)."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access_users (C5), copiato — nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione del servizio."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica del servizio."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete; un servizio con consumi storici (C1) non si cancella fisicamente.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_sale_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_service_sale_items_icon",
                        column: x => x.icon_id,
                        principalSchema: "catalog",
                        principalTable: "service_type_icons",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_catalog_service_sale_items_icon_asset",
                        column: x => x.icon_asset_id,
                        principalSchema: "catalog",
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_catalog_service_sale_items_property",
                        column: x => x.property_id,
                        principalSchema: "catalog",
                        principalTable: "properties",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Servizio (bar/normale/a fascia oraria) venduto da uno stabilimento a corredo della prenotazione.");

            migrationBuilder.CreateTable(
                name: "pricing_price_list_date_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    price_list_id = table.Column<long>(type: "bigint", nullable: false, comment: "Pacchetto a cui si applica la deroga."),
                    period_price_id = table.Column<long>(type: "bigint", nullable: true, comment: "Periodo tariffario a cui la deroga è agganciata (NULL per le date festive globali del pacchetto)."),
                    kind = table.Column<int>(type: "catalog.date_price_kind", nullable: false, comment: "Natura della deroga: override puntuale o data festiva."),
                    price_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Data a cui si applica il prezzo/la deroga."),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo giornaliero per la data (NULL per holiday quando si applica la regola festiva del pacchetto)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della deroga.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_price_list_date_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_date_prices_period_price",
                        column: x => x.period_price_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_list_period_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_date_prices_price_list",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Deroga di prezzo per data di calendario: data festiva con tariffa dedicata o override puntuale.");

            migrationBuilder.CreateTable(
                name: "pricing_price_list_stay_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    period_price_id = table.Column<long>(type: "bigint", nullable: false, comment: "Periodo tariffario (e, tramite esso, voce di listino) a cui si applica il prezzo per durata."),
                    sector_id = table.Column<long>(type: "bigint", nullable: true, comment: "Settore a cui si applica il prezzo per durata (NULL = valido per tutti i settori del periodo, da confermare in migrazione)."),
                    stay_days = table.Column<short>(type: "smallint", nullable: false, comment: "Numero di giorni consecutivi di soggiorno a cui si applica il prezzo (1-90)."),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Prezzo impostato manualmente dal gestore per la durata indicata."),
                    is_manual_override = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Sempre true per le righe presenti: si persistono solo gli override manuali (isPriceChange legacy)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete dell'override.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_price_list_stay_prices", x => x.id);
                    table.CheckConstraint("ck_catalog_pricing_price_list_stay_prices_stay_days", "stay_days BETWEEN 1 AND 90");
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_stay_prices_period_price",
                        column: x => x.period_price_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_list_period_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_stay_prices_sector",
                        column: x => x.sector_id,
                        principalSchema: "catalog",
                        principalTable: "map_sectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario.");

            migrationBuilder.CreateTable(
                name: "pricing_price_list_reduced_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    window_id = table.Column<long>(type: "bigint", nullable: false, comment: "Finestra di date a cui appartiene il prezzo ridotto."),
                    sector_id = table.Column<long>(type: "bigint", nullable: false, comment: "Settore per cui vale il prezzo ridotto."),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Prezzo assoluto del pacchetto in quel settore per l'intervallo, sostituisce il prezzo standard."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della riga.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_pricing_price_list_reduced_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_reduced_prices_sector",
                        column: x => x.sector_id,
                        principalSchema: "catalog",
                        principalTable: "map_sectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_pricing_price_list_reduced_prices_window",
                        column: x => x.window_id,
                        principalSchema: "catalog",
                        principalTable: "pricing_price_list_reduced_windows",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Prezzo ridotto assoluto per settore, valido per una finestra di date con match esatto.");

            migrationBuilder.CreateTable(
                name: "agreement_price_list_date_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    price_list_id = table.Column<int>(type: "integer", nullable: false, comment: "Listino/voucher hotel a cui si applica la deroga."),
                    period_price_id = table.Column<long>(type: "bigint", nullable: true, comment: "Periodo tariffario a cui la deroga è agganciata; NULL per le date festive globali del listino."),
                    kind = table.Column<int>(type: "catalog.date_price_kind", nullable: false, comment: "Natura della deroga: override puntuale o data festiva."),
                    price_date = table.Column<DateOnly>(type: "date", nullable: false, comment: "Data a cui si applica la deroga."),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo giornaliero per la data; valorizzato per kind = override, può essere NULL per kind = holiday."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_agreement_price_list_date_prices", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_list_date_prices_period_price",
                        column: x => x.period_price_id,
                        principalSchema: "catalog",
                        principalTable: "agreement_price_list_period_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_list_date_prices_price_list",
                        column: x => x.price_list_id,
                        principalSchema: "catalog",
                        principalTable: "agreement_price_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Override di prezzo per data di calendario (festivi, deroghe puntuali) di un listino/voucher hotel.");

            migrationBuilder.CreateTable(
                name: "agreement_price_list_stay_prices",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    period_price_id = table.Column<long>(type: "bigint", nullable: false, comment: "Periodo tariffario (e, tramite esso, listino/voucher hotel) a cui si applica il prezzo per durata."),
                    sector_id = table.Column<long>(type: "bigint", nullable: true, comment: "Settore a cui si applica il prezzo per durata (NULL = valido per tutti i settori del periodo)."),
                    stay_days = table.Column<short>(type: "smallint", nullable: false, comment: "Numero di giorni consecutivi di soggiorno a cui si applica il prezzo (1-90)."),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: false, comment: "Prezzo impostato manualmente dal gestore per la durata indicata."),
                    is_manual_override = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Sempre true per le righe presenti: si persistono solo gli override manuali (isPriceChange legacy)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete dell'override.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_agreement_price_list_stay_prices", x => x.id);
                    table.CheckConstraint("ck_catalog_agreement_price_list_stay_prices_stay_days", "stay_days BETWEEN 1 AND 90");
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_list_stay_prices_period_price",
                        column: x => x.period_price_id,
                        principalSchema: "catalog",
                        principalTable: "agreement_price_list_period_prices",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_catalog_agreement_price_list_stay_prices_sector",
                        column: x => x.sector_id,
                        principalSchema: "catalog",
                        principalTable: "map_sectors",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Override manuale del prezzo per una durata specifica (1-90 giorni) di un periodo tariffario di un listino/voucher hotel.");

            migrationBuilder.CreateTable(
                name: "service_sale_item_time_slots",
                schema: "catalog",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico, copiato cross-modulo: C1 lo usa come time_slot_id in booking_service_time_slots."),
                    sale_item_id = table.Column<long>(type: "bigint", nullable: false, comment: "Servizio a fascia oraria a cui appartiene la fascia."),
                    label = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "Etichetta della fascia mostrata al cliente. Nei dati legacy è sempre una durata (\"30 min\", \"1 hr 30 min\"), mai un intervallo: vedi duration_minutes."),
                    slot_start = table.Column<TimeOnly>(type: "time without time zone", nullable: true, comment: "Orario di inizio della fascia."),
                    slot_end = table.Column<TimeOnly>(type: "time without time zone", nullable: true, comment: "Orario di fine della fascia."),
                    duration_minutes = table.Column<short>(type: "smallint", nullable: true, comment: "Durata della fascia in minuti, quando il servizio è venduto a durata invece che a orario fisso. I dati legacy sono tutti di questa forma (undici valori: 10/15/30/45/60/90/120/180/240/300/360 minuti), quindi per lo storico migrato è questa la colonna valorizzata e slot_start/slot_end restano NULL. Per una fascia a orario assoluto vale il contrario: la durata si calcola e questa resta NULL."),
                    price = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Prezzo dedicato della fascia; se NULL si usa il prezzo del servizio padre."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "La fascia è prenotabile."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, comment: "Ordine di visualizzazione delle fasce del servizio."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della fascia."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della fascia."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_catalog_service_sale_item_time_slots", x => x.id);
                    table.ForeignKey(
                        name: "fk_catalog_service_sale_item_time_slots_item",
                        column: x => x.sale_item_id,
                        principalSchema: "catalog",
                        principalTable: "service_sale_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Fascia oraria interna prenotabile di un servizio a fascia oraria (kind = hourly), con eventuale prezzo dedicato.");

            migrationBuilder.InsertData(
                schema: "catalog",
                table: "pricing_calculation_methods",
                columns: new[] { "id", "code", "created_at", "deleted_at", "is_active", "updated_at" },
                values: new object[,]
                {
                    { (short)1, "proportional", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { (short)2, "daily", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { (short)3, "daily_with_weekend", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { (short)4, "logarithmic", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_partners_name_trgm",
                schema: "catalog",
                table: "agreement_partners",
                column: "name")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_partners_property",
                schema: "catalog",
                table: "agreement_partners",
                column: "property_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_agreement_partners_public_id",
                schema: "catalog",
                table: "agreement_partners",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_agreement_price_list_date_prices_period_price_id",
                schema: "catalog",
                table: "agreement_price_list_date_prices",
                column: "period_price_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_price_list_date_prices_pricelist_date",
                schema: "catalog",
                table: "agreement_price_list_date_prices",
                columns: new[] { "price_list_id", "price_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_agreement_price_list_date_prices_pricelist_date_kind",
                schema: "catalog",
                table: "agreement_price_list_date_prices",
                columns: new[] { "price_list_id", "price_date", "kind" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agreement_price_list_items_included_service_id",
                schema: "catalog",
                table: "agreement_price_list_items",
                column: "included_service_id");

            migrationBuilder.CreateIndex(
                name: "ix_agreement_price_list_items_linked_price_list_id",
                schema: "catalog",
                table: "agreement_price_list_items",
                column: "linked_price_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_price_list_items_inventory_item",
                schema: "catalog",
                table: "agreement_price_list_items",
                column: "inventory_item_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_price_list_items_price_list",
                schema: "catalog",
                table: "agreement_price_list_items",
                column: "price_list_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_price_list_period_prices_price_list",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                column: "price_list_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_price_list_sector_prices_sector",
                schema: "catalog",
                table: "agreement_price_list_sector_prices",
                column: "sector_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_agreement_price_list_sector_prices_pricelist_sector",
                schema: "catalog",
                table: "agreement_price_list_sector_prices",
                columns: new[] { "price_list_id", "sector_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agreement_price_list_stay_prices_sector_id",
                schema: "catalog",
                table: "agreement_price_list_stay_prices",
                column: "sector_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_price_list_stay_prices_period_days",
                schema: "catalog",
                table: "agreement_price_list_stay_prices",
                columns: new[] { "period_price_id", "stay_days" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_agreement_price_list_stay_prices_period_sector_days",
                schema: "catalog",
                table: "agreement_price_list_stay_prices",
                columns: new[] { "period_price_id", "sector_id", "stay_days" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agreement_price_lists_calculation_method_id",
                schema: "catalog",
                table: "agreement_price_lists",
                column: "calculation_method_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_price_lists_partner_kind",
                schema: "catalog",
                table: "agreement_price_lists",
                columns: new[] { "partner_id", "list_type" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_agreement_price_lists_public_id",
                schema: "catalog",
                table: "agreement_price_lists",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_reports_partner",
                schema: "catalog",
                table: "agreement_reports",
                column: "partner_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_reports_pdf_media_asset",
                schema: "catalog",
                table: "agreement_reports",
                column: "pdf_media_asset_id",
                filter: "pdf_media_asset_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_agreement_reports_period",
                schema: "catalog",
                table: "agreement_reports",
                columns: new[] { "period_start", "period_end" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_agreement_reports_public_id",
                schema: "catalog",
                table: "agreement_reports",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_bathhouse_amenities_amenity",
                schema: "catalog",
                table: "bathhouse_amenities",
                column: "amenity_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_bathhouse_amenities_bathhouse",
                schema: "catalog",
                table: "bathhouse_amenities",
                column: "bathhouse_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_bathhouse_amenities_bathhouse_amenity",
                schema: "catalog",
                table: "bathhouse_amenities",
                columns: new[] { "bathhouse_id", "amenity_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_bathhouse_photos_bathhouse",
                schema: "catalog",
                table: "bathhouse_photos",
                column: "bathhouse_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_bathhouse_photos_media_asset",
                schema: "catalog",
                table: "bathhouse_photos",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_bathhouse_photos_bathhouse_sort",
                schema: "catalog",
                table: "bathhouse_photos",
                columns: new[] { "bathhouse_id", "sort_order" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_bathhouses_country_id",
                schema: "catalog",
                table: "bathhouses",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_bathhouses_main_photo_id",
                schema: "catalog",
                table: "bathhouses",
                column: "main_photo_id");

            migrationBuilder.CreateIndex(
                name: "ix_bathhouses_menu_pdf_id",
                schema: "catalog",
                table: "bathhouses",
                column: "menu_pdf_id");

            migrationBuilder.CreateIndex(
                name: "ix_bathhouses_price_list_pdf_id",
                schema: "catalog",
                table: "bathhouses",
                column: "price_list_pdf_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_bathhouses_geo",
                schema: "catalog",
                table: "bathhouses",
                columns: new[] { "latitude", "longitude" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_bathhouses_property",
                schema: "catalog",
                table: "bathhouses",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_bathhouses_published",
                schema: "catalog",
                table: "bathhouses",
                column: "published_at",
                filter: "published_at IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_bathhouses_region",
                schema: "catalog",
                table: "bathhouses",
                columns: new[] { "region", "city" });

            migrationBuilder.CreateIndex(
                name: "uq_catalog_bathhouses_public_id",
                schema: "catalog",
                table: "bathhouses",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_bathhouses_slug",
                schema: "catalog",
                table: "bathhouses",
                column: "slug",
                unique: true,
                filter: "slug IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_config_snapshots_built_at",
                schema: "catalog",
                table: "config_snapshots",
                column: "built_at");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_config_snapshots_stale",
                schema: "catalog",
                table: "config_snapshots",
                column: "stale_since",
                filter: "stale_since IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_config_snapshots_property_kind",
                schema: "catalog",
                table: "config_snapshots",
                columns: new[] { "property_id", "snapshot_kind" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_countries_code",
                schema: "catalog",
                table: "countries",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_country_translations_culture",
                schema: "catalog",
                table: "country_translations",
                column: "culture",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_country_translations_country_culture",
                schema: "catalog",
                table: "country_translations",
                columns: new[] { "country_id", "culture" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_inventory_items_property_active",
                schema: "catalog",
                table: "inventory_items",
                column: "property_id",
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_inventory_items_property_name",
                schema: "catalog",
                table: "inventory_items",
                columns: new[] { "property_id", "name" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_inventory_items_public_id",
                schema: "catalog",
                table: "inventory_items",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_map_icon_categories_code",
                schema: "catalog",
                table: "map_icon_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_map_icons_category_type",
                schema: "catalog",
                table: "map_icons",
                columns: new[] { "category_id", "type" },
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_map_icons_media_asset",
                schema: "catalog",
                table: "map_icons",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_map_layout_special_icons_media_asset_id",
                schema: "catalog",
                table: "map_layout_special_icons",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_map_layout_special_icons_layout_variant_state",
                schema: "catalog",
                table: "map_layout_special_icons",
                columns: new[] { "layout_id", "variant", "state" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_map_layouts_template_asset_id",
                schema: "catalog",
                table: "map_layouts",
                column: "template_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_map_layouts_umbrella_free_asset_id",
                schema: "catalog",
                table: "map_layouts",
                column: "umbrella_free_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_map_layouts_umbrella_occupied_asset_id",
                schema: "catalog",
                table: "map_layouts",
                column: "umbrella_occupied_asset_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_map_layouts_property",
                schema: "catalog",
                table: "map_layouts",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_map_legend_items_icon_asset",
                schema: "catalog",
                table: "map_legend_items",
                column: "icon_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_map_legend_items_property",
                schema: "catalog",
                table: "map_legend_items",
                columns: new[] { "property_id", "sort_order" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_map_sectors_property",
                schema: "catalog",
                table: "map_sectors",
                columns: new[] { "property_id", "sort_order" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_map_sectors_public_id",
                schema: "catalog",
                table: "map_sectors",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_map_spots_grid",
                schema: "catalog",
                table: "map_spots",
                columns: new[] { "property_id", "grid_row", "grid_column" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_map_spots_property_type",
                schema: "catalog",
                table: "map_spots",
                columns: new[] { "property_id", "spot_type" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_map_spots_sector",
                schema: "catalog",
                table: "map_spots",
                column: "sector_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_map_spots_property_label",
                schema: "catalog",
                table: "map_spots",
                columns: new[] { "property_id", "label" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_map_spots_public_id",
                schema: "catalog",
                table: "map_spots",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_media_assets_kind",
                schema: "catalog",
                table: "media_assets",
                column: "kind",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_media_assets_origin",
                schema: "catalog",
                table: "media_assets",
                column: "origin");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_media_assets_property",
                schema: "catalog",
                table: "media_assets",
                column: "property_id",
                filter: "property_id IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_calculation_methods_code",
                schema: "catalog",
                table: "pricing_calculation_methods",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_discount_date_ranges_discount_range",
                schema: "catalog",
                table: "pricing_discount_date_ranges",
                columns: new[] { "discount_id", "starts_on", "ends_on" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_discount_sectors_sector",
                schema: "catalog",
                table: "pricing_discount_sectors",
                column: "sector_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_discount_sectors_discount_sector",
                schema: "catalog",
                table: "pricing_discount_sectors",
                columns: new[] { "discount_id", "sector_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_discounts_property_active",
                schema: "catalog",
                table: "pricing_discounts",
                column: "property_id",
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_discounts_property_code",
                schema: "catalog",
                table: "pricing_discounts",
                columns: new[] { "property_id", "code_normalized" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_discounts_public_id",
                schema: "catalog",
                table: "pricing_discounts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_list_date_prices_package_date",
                schema: "catalog",
                table: "pricing_price_list_date_prices",
                columns: new[] { "price_list_id", "price_date" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_price_list_date_prices_period_price_id",
                schema: "catalog",
                table: "pricing_price_list_date_prices",
                column: "period_price_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_date_prices_package_date_kind",
                schema: "catalog",
                table: "pricing_price_list_date_prices",
                columns: new[] { "price_list_id", "price_date", "kind" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_list_items_inventory_item",
                schema: "catalog",
                table: "pricing_price_list_items",
                column: "inventory_item_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_list_items_linked_package",
                schema: "catalog",
                table: "pricing_price_list_items",
                column: "linked_price_list_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_list_items_package",
                schema: "catalog",
                table: "pricing_price_list_items",
                column: "price_list_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_price_list_items_included_service_id",
                schema: "catalog",
                table: "pricing_price_list_items",
                column: "included_service_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_list_period_prices_period",
                schema: "catalog",
                table: "pricing_price_list_period_prices",
                column: "tariff_period_group");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_period_prices_package_period",
                schema: "catalog",
                table: "pricing_price_list_period_prices",
                columns: new[] { "price_list_id", "tariff_period_group" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_price_list_reduced_prices_sector_id",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                column: "sector_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_reduced_prices_window_sector",
                schema: "catalog",
                table: "pricing_price_list_reduced_prices",
                columns: new[] { "window_id", "sector_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_reduced_windows_package_range",
                schema: "catalog",
                table: "pricing_price_list_reduced_windows",
                columns: new[] { "price_list_id", "starts_on", "ends_on" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_list_sector_prices_sector",
                schema: "catalog",
                table: "pricing_price_list_sector_prices",
                column: "sector_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_sector_prices_package_sector",
                schema: "catalog",
                table: "pricing_price_list_sector_prices",
                columns: new[] { "price_list_id", "sector_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_list_stay_prices_period_days",
                schema: "catalog",
                table: "pricing_price_list_stay_prices",
                columns: new[] { "period_price_id", "stay_days" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_price_list_stay_prices_sector_id",
                schema: "catalog",
                table: "pricing_price_list_stay_prices",
                column: "sector_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_list_stay_prices_period_sector_days",
                schema: "catalog",
                table: "pricing_price_list_stay_prices",
                columns: new[] { "period_price_id", "sector_id", "stay_days" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_lists_online",
                schema: "catalog",
                table: "pricing_price_lists",
                column: "property_id",
                filter: "sales_channel <> 'manager_only' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_price_lists_property_kind",
                schema: "catalog",
                table: "pricing_price_lists",
                columns: new[] { "property_id", "kind" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_pricing_price_lists_calculation_method_id",
                schema: "catalog",
                table: "pricing_price_lists",
                column: "calculation_method_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_price_lists_public_id",
                schema: "catalog",
                table: "pricing_price_lists",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_revenue_discounted_spots_spot",
                schema: "catalog",
                table: "pricing_revenue_discounted_spots",
                column: "spot_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_revenue_discounted_spots_settings_spot",
                schema: "catalog",
                table: "pricing_revenue_discounted_spots",
                columns: new[] { "settings_id", "spot_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_revenue_settings_property",
                schema: "catalog",
                table: "pricing_revenue_settings",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_seasons_property",
                schema: "catalog",
                table: "pricing_seasons",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_suggested_prices_property_date",
                schema: "catalog",
                table: "pricing_suggested_prices",
                columns: new[] { "property_id", "price_date" },
                filter: "applied_mode <> 'none'");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_pricing_suggested_prices_package_date_period",
                schema: "catalog",
                table: "pricing_suggested_prices",
                columns: new[] { "price_list_id", "price_date", "period_label" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_tariff_periods_range",
                schema: "catalog",
                table: "pricing_tariff_periods",
                columns: new[] { "season_id", "starts_on", "ends_on" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_tariff_periods_season",
                schema: "catalog",
                table: "pricing_tariff_periods",
                column: "season_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_pricing_tariff_periods_season_group",
                schema: "catalog",
                table: "pricing_tariff_periods",
                columns: new[] { "season_id", "period_group_key" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_properties_country",
                schema: "catalog",
                table: "properties",
                column: "country_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_properties_owner",
                schema: "catalog",
                table: "properties",
                column: "owner_user_public_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_properties_status",
                schema: "catalog",
                table: "properties",
                column: "status",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_properties_public_id",
                schema: "catalog",
                table: "properties",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_property_online_allowed_locations_settings",
                schema: "catalog",
                table: "property_online_allowed_locations",
                column: "settings_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_property_online_allowed_locations_spot",
                schema: "catalog",
                table: "property_online_allowed_locations",
                column: "spot_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_property_online_allowed_locations_settings_spot",
                schema: "catalog",
                table: "property_online_allowed_locations",
                columns: new[] { "settings_id", "spot_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_property_online_allowed_periods_range",
                schema: "catalog",
                table: "property_online_allowed_periods",
                columns: new[] { "settings_id", "starts_on", "ends_on" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_property_online_allowed_periods_settings",
                schema: "catalog",
                table: "property_online_allowed_periods",
                column: "settings_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_property_online_booking_settings_property",
                schema: "catalog",
                table: "property_online_booking_settings",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_property_online_stay_limits_sector_range",
                schema: "catalog",
                table: "property_online_stay_limits",
                columns: new[] { "sector_id", "starts_on", "ends_on" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_property_online_stay_limits_settings",
                schema: "catalog",
                table: "property_online_stay_limits",
                column: "settings_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_property_service_booking_settings_property",
                schema: "catalog",
                table: "property_service_booking_settings",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_search_bathhouses_amenities",
                schema: "catalog",
                table: "search_bathhouses",
                column: "amenity_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_search_bathhouses_property",
                schema: "catalog",
                table: "search_bathhouses",
                column: "property_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_search_bathhouses_published_name_trgm",
                schema: "catalog",
                table: "search_bathhouses",
                column: "name",
                filter: "is_published")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_catalog_search_bathhouses_published_price",
                schema: "catalog",
                table: "search_bathhouses",
                column: "min_price",
                filter: "is_published");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_search_bathhouses_published_region_city",
                schema: "catalog",
                table: "search_bathhouses",
                columns: new[] { "region", "city" },
                filter: "is_published");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_amenities_category",
                schema: "catalog",
                table: "service_amenities",
                column: "category_id",
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_amenities_media_asset",
                schema: "catalog",
                table: "service_amenities",
                column: "icon_asset_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_service_amenity_categories_code",
                schema: "catalog",
                table: "service_amenity_categories",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_amenity_translations_culture",
                schema: "catalog",
                table: "service_amenity_translations",
                column: "culture",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_service_amenity_translations_amenity_culture",
                schema: "catalog",
                table: "service_amenity_translations",
                columns: new[] { "amenity_id", "culture" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_sale_item_time_slots_item",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                columns: new[] { "sale_item_id", "sort_order" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_service_sale_item_time_slots_public_id",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_sale_items_property_purchasable",
                schema: "catalog",
                table: "service_sale_items",
                columns: new[] { "property_id", "kind" },
                filter: "is_purchasable AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_service_sale_items_icon_asset_id",
                schema: "catalog",
                table: "service_sale_items",
                column: "icon_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_service_sale_items_icon_id",
                schema: "catalog",
                table: "service_sale_items",
                column: "icon_id");

            migrationBuilder.CreateIndex(
                name: "uq_catalog_service_sale_items_public_id",
                schema: "catalog",
                table: "service_sale_items",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_type_icons_media_asset",
                schema: "catalog",
                table: "service_type_icons",
                column: "icon_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_catalog_service_type_icons_type",
                schema: "catalog",
                table: "service_type_icons",
                column: "type",
                filter: "is_active AND deleted_at IS NULL");

            // ─────────────────────────────────────────────────────────────────────────────
            // Oggetti che il modello EF non sa esprimere. Questa InitialCreate consolida
            // (2026-10-06) tutte le migration precedenti del modulo: qui sotto c'e' lo SQL a
            // mano che quelle contenevano e che vale ancora sullo schema finale. Gli UPDATE di
            // conversione dei dati (portavano righe esistenti da una forma all'altra) non sono
            // riportati: su un database nuovo non c'e' niente da convertire.
            // ─────────────────────────────────────────────────────────────────────────────

            // Nome di settore unico per stabilimento, senza distinzione di maiuscole.
            migrationBuilder.Sql("""
CREATE UNIQUE INDEX uq_catalog_map_sectors_property_name
    ON catalog.map_sectors (property_id, lower(name))
 WHERE deleted_at IS NULL;
""");

            // Vincoli di non sovrapposizione fra periodi e fasce orarie.
            migrationBuilder.Sql("""
ALTER TABLE catalog.property_online_stay_limits
  ADD CONSTRAINT ex_catalog_property_online_stay_limits_no_overlap
  EXCLUDE USING gist (settings_id WITH =, sector_id WITH =,
                      daterange(starts_on, ends_on, '[]') WITH &&)
  WHERE (deleted_at IS NULL);
""");
            migrationBuilder.Sql("""
ALTER TABLE catalog.service_sale_item_time_slots
  ADD CONSTRAINT ex_catalog_service_sale_item_time_slots_no_overlap
  EXCLUDE USING gist (sale_item_id WITH =,
                      tsrange(DATE '2000-01-01' + slot_start, DATE '2000-01-01' + slot_end) WITH &&)
  WHERE (deleted_at IS NULL AND slot_start IS NOT NULL AND slot_end IS NOT NULL
         AND slot_end > slot_start);
""");
            migrationBuilder.Sql("""
ALTER TABLE catalog.pricing_tariff_periods
  ADD CONSTRAINT ex_catalog_pricing_tariff_periods_no_overlap
  EXCLUDE USING gist (season_id WITH =,
                      COALESCE(period_group_key, -1) WITH =,
                      daterange(starts_on, ends_on, '[]') WITH &&)
  WHERE (deleted_at IS NULL);
""");
            migrationBuilder.Sql("""
ALTER TABLE catalog.property_online_allowed_periods
  ADD CONSTRAINT ex_catalog_property_online_allowed_periods_no_overlap
  EXCLUDE USING gist (settings_id WITH =,
                      daterange(starts_on, ends_on, '[]') WITH &&)
  WHERE (deleted_at IS NULL);
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "agreement_price_list_date_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "agreement_price_list_items",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "agreement_price_list_sector_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "agreement_price_list_stay_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "agreement_reports",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "bathhouse_amenities",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "bathhouse_photos",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "config_snapshots",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "country_translations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "map_icons",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "map_layout_special_icons",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "map_legend_items",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_discount_date_ranges",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_discount_sectors",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_price_list_date_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_price_list_items",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_price_list_reduced_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_price_list_sector_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_price_list_stay_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_revenue_discounted_spots",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_suggested_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_tariff_periods",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "property_online_allowed_locations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "property_online_allowed_periods",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "property_online_stay_limits",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "property_service_booking_settings",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "search_bathhouses",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_amenity_translations",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_sale_item_time_slots",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "agreement_price_list_period_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "bathhouses",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "map_icon_categories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "map_layouts",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_discounts",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "inventory_items",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_price_list_reduced_windows",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_price_list_period_prices",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_revenue_settings",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_seasons",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "map_spots",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "property_online_booking_settings",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_amenities",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_sale_items",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "agreement_price_lists",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_price_lists",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "map_sectors",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_amenity_categories",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "service_type_icons",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "agreement_partners",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "pricing_calculation_methods",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "media_assets",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "properties",
                schema: "catalog");

            migrationBuilder.DropTable(
                name: "countries",
                schema: "catalog");
        }
    }
}
