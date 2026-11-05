using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Module.Catalog.Domain.Enums;

#nullable disable

namespace Module.Catalog.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CommentWording : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "media_assets",
                schema: "catalog",
                comment: "Catalogo locale al modulo catalog di tutti i riferimenti a file (foto, PDF, icone) usati dalle entità Catalog.",
                oldComment: "Catalogo locale al modulo catalog di tutti i riferimenti a file (foto, PDF, icone) usati dalle entità C2.");

            migrationBuilder.AlterTable(
                name: "map_sectors",
                schema: "catalog",
                comment: "Settori/file in cui è divisa la spiaggia di uno stabilimento.",
                oldComment: "Settori/file in cui è divisa la spiaggia di uno stabilimento. Sostituisce l'array JSON sectors_rows.sectors.");

            migrationBuilder.AlterTable(
                name: "map_layout_special_icons",
                schema: "catalog",
                comment: "Icone degli articoli personalizzati (1-8) per stato occupato/libero.",
                oldComment: "Icone degli articoli personalizzati (1-8) per stato occupato/libero. Sostituisce le colonne specialUmbrella1..5 di configure_maps.");

            migrationBuilder.AlterTable(
                name: "bathhouse_photos",
                schema: "catalog",
                comment: "Elenco ordinato delle foto della galleria pubblica di una vetrina.",
                oldComment: "Elenco ordinato delle foto della galleria pubblica di una vetrina. Sostituisce l'array JSON bathouses.photos.");

            migrationBuilder.AlterTable(
                name: "bathhouse_amenities",
                schema: "catalog",
                comment: "Giunzione N:N: quali dotazioni/servizi (amenity) offre una vetrina.",
                oldComment: "Giunzione N:N: quali dotazioni/servizi (amenity) offre una vetrina. Sostituisce la copia JSON integrale bathouses.services.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "service_sale_items",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico, copiato cross-modulo: Booking lo usa per contatori/consumi del servizio.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico, copiato cross-modulo: C1 lo usa per contatori/consumi del servizio.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "catalog",
                table: "service_sale_items",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete; un servizio con consumi storici (Booking) non si cancella fisicamente.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete; un servizio con consumi storici (C1) non si cancella fisicamente.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_user_id",
                schema: "catalog",
                table: "service_sale_items",
                type: "uuid",
                nullable: true,
                comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di identity_access_users (C5), copiato — nessuna FK cross-schema.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico, copiato cross-modulo: Booking lo usa come time_slot_id in booking_service_time_slots.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico, copiato cross-modulo: C1 lo usa come time_slot_id in booking_service_time_slots.");

            migrationBuilder.AlterColumn<string>(
                name: "label",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "Etichetta della fascia mostrata al cliente (es. \"30 min\", \"1 hr 30 min\").",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "Etichetta della fascia mostrata al cliente. Nei dati legacy è sempre una durata (\"30 min\", \"1 hr 30 min\"), mai un intervallo: vedi duration_minutes.");

            migrationBuilder.AlterColumn<short>(
                name: "duration_minutes",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                type: "smallint",
                nullable: true,
                comment: "Durata della fascia in minuti, quando il servizio è venduto a durata invece che a orario fisso (slot_start/slot_end NULL). Per una fascia a orario assoluto vale il contrario: la durata si calcola e questa resta NULL.",
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true,
                oldComment: "Durata della fascia in minuti, quando il servizio è venduto a durata invece che a orario fisso. I dati legacy sono tutti di questa forma (undici valori: 10/15/30/45/60/90/120/180/240/300/360 minuti), quindi per lo storico migrato è questa la colonna valorizzata e slot_start/slot_end restano NULL. Per una fascia a orario assoluto vale il contrario: la durata si calcola e questa resta NULL.");

            migrationBuilder.AlterColumn<decimal>(
                name: "min_price",
                schema: "catalog",
                table: "search_bathhouses",
                type: "numeric(12,2)",
                nullable: true,
                comment: "Prezzo minimo corrente prenotabile online, materializzato dal rebuild (nessuna query per ogni risultato).",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldNullable: true,
                oldComment: "Prezzo minimo corrente prenotabile online, materializzato dal rebuild (evita la query-per-risultato del legacy).");

            migrationBuilder.AlterColumn<bool>(
                name: "allows_sharing",
                schema: "catalog",
                table: "property_online_booking_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Espone al cliente la funzione di sharing durante la prenotazione online (i parametri operativi vivono in Booking).",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Espone al cliente la funzione di sharing durante la prenotazione online (i parametri operativi vivono in C1).");

            migrationBuilder.AlterColumn<string>(
                name: "period_label",
                schema: "catalog",
                table: "pricing_suggested_prices",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "Etichetta «fila + periodo» dei dati importati (es. \"Fila 1 Alta stagione\"), solo traccia: valgono sector_id e tariff_period_group.",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true,
                oldComment: "Etichetta libera del periodo/settore legacy (es. \"Fila 1 Alta stagione\"); NULL quando il calcolo non distingue varianti per la data.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_suggested_price_enabled",
                schema: "catalog",
                table: "pricing_revenue_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Prezzo suggerito attivo: il motore di prezzo usa la scelta del gestore.",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Prezzo suggerito attivo (legacy revenue_notifications.isRevenuePricesEnabled).");

            migrationBuilder.AlterColumn<bool>(
                name: "is_shown_on_public_page",
                schema: "catalog",
                table: "pricing_revenue_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Il banner sconto è visibile sulla pagina pubblica dello stabilimento / flusso di prenotazione (indipendente dalle campagne marketing).",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Il banner sconto è visibile sulla pagina pubblica dello stabilimento / flusso di prenotazione (indipendente da marketing_event.showOnEstablishmentPage di C6).");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "pricing_price_lists",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato in Booking booking_lines.price_list_package_id.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato in C1 booking_lines.price_list_package_id.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_manual_override",
                schema: "catalog",
                table: "pricing_price_list_stay_prices",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Sempre true per le righe presenti: si persistono solo le celle corrette a mano dal gestore.",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true,
                oldComment: "Sempre true per le righe presenti: si persistono solo gli override manuali (isPriceChange legacy).");

            migrationBuilder.AlterColumn<decimal>(
                name: "half_day_price",
                schema: "catalog",
                table: "pricing_price_list_sector_prices",
                type: "numeric(12,2)",
                nullable: true,
                comment: "Prezzo forfettario mezza giornata nel settore (come nei listini delle convenzioni; spesso NULL).",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldNullable: true,
                oldComment: "Prezzo forfettario mezza giornata nel settore (allineamento con i listini delle convenzioni; spesso NULL sui price_lists puri).");

            migrationBuilder.AlterColumn<int>(
                name: "used_count",
                schema: "catalog",
                table: "pricing_discounts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Utilizzi già effettuati, incrementato atomicamente da Booking alla conferma prenotazione.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "Utilizzi già effettuati, incrementato atomicamente da C1 alla conferma prenotazione.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "pricing_discounts",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico, copiato negli snapshot sconto di Booking (booking_discount, discountData).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico, copiato negli snapshot sconto di C1 (booking_discount, discountData).");

            migrationBuilder.AlterColumn<byte[]>(
                name: "checksum",
                schema: "catalog",
                table: "media_assets",
                type: "bytea",
                nullable: true,
                comment: "Hash SHA-256 del contenuto, calcolato all'upload, per deduplica e verifica integrità. NULL per i file importati finché non se ne calcolano i metadati.",
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true,
                oldComment: "Hash SHA-256 del contenuto, calcolato all'upload, per deduplica e verifica integrità. NULL per i file migrati dal legacy.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "map_spots",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo (Booking resource_id, snapshot mappa).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo (C1 resource_id, snapshot mappa).");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "catalog",
                table: "map_spots",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete: la postazione non si cancella fisicamente se ha prenotazioni storiche in Booking.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete: la postazione non si cancella fisicamente se ha prenotazioni storiche in C1.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "map_sectors",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo da Booking (booking_lines.sector_id, resource_availability_daily.sector_id).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo da C1 (booking_lines.sector_id, resource_availability_daily.sector_id).");

            migrationBuilder.AlterColumn<short>(
                name: "grid_columns",
                schema: "catalog",
                table: "map_layouts",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                comment: "Numero di colonne della griglia (articoli per fila).",
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)0,
                oldComment: "Numero di colonne della griglia (legacy: configure_maps.items, rinominato per eliminare l'ambiguità).");

            migrationBuilder.AlterColumn<short>(
                name: "variant",
                schema: "catalog",
                table: "map_layout_special_icons",
                type: "smallint",
                nullable: false,
                comment: "Numero dell'articolo personalizzato (1-8).",
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "Numero dell'articolo personalizzato (1-8, i primi 5 corrispondono a SpecialUmbrella1..5 del legacy).");

            migrationBuilder.AlterColumn<int>(
                name: "total_quantity",
                schema: "catalog",
                table: "inventory_items",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Quantità totale disponibile dell'articolo; la disponibilità per data è total_quantity meno i consumi (Booking).",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "Quantità totale disponibile dell'articolo; la disponibilità per data è total_quantity meno i consumi (C1).");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "inventory_items",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico (UUIDv7), copiato — Booking lo referenzia come resource_id nei consumi di magazzino.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico (UUIDv7), copiato — C1 lo referenzia come resource_id nei consumi di magazzino.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "catalog",
                table: "inventory_items",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete; un articolo con consumi storici (Booking) non si cancella fisicamente.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete; un articolo con consumi storici (C1) non si cancella fisicamente.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_user_id",
                schema: "catalog",
                table: "inventory_items",
                type: "uuid",
                nullable: true,
                comment: "public_id di identity.access_users, copiato — operatore che ha configurato l'articolo, nessuna FK cross-schema.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di identity_access_users (C5), copiato — operatore che ha configurato l'articolo, nessuna FK cross-schema.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "bathhouses",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo (identity_customer_bathhouse_likes.bathhouse_id) e usato in URL/API pubbliche.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo (C5 identity_customer_bathhouse_likes.bathhouse_id) e usato in URL/API pubbliche.");

            migrationBuilder.AlterColumn<AgreementReportStatus>(
                name: "status",
                schema: "catalog",
                table: "agreement_reports",
                type: "catalog.agreement_report_status",
                nullable: false,
                defaultValueSql: "'pending'",
                comment: "Stato del report: pending, created o cancelled. Distinto da deleted_at, che e' il soft-delete della riga.",
                oldClrType: typeof(AgreementReportStatus),
                oldType: "catalog.agreement_report_status",
                oldDefaultValueSql: "'pending'",
                oldComment: "Stato del report: pending, created o cancelled. Distinto da deleted_at, che e' il soft-delete della riga: nel legacy i due erano scritti sempre insieme.");

            migrationBuilder.AlterColumn<long>(
                name: "pdf_media_asset_id",
                schema: "catalog",
                table: "agreement_reports",
                type: "bigint",
                nullable: true,
                comment: "PDF archiviato all'invio: riferimento logico a notification.media_assets (kind = 'rendered_pdf'), NON una FK perche' attraversa lo schema di un altro modulo. NULL finche' il report non e' stato inviato. ATTENZIONE: sostituire il PDF richiede di accodare il vecchio oggetto in privacy.privacy_file_deletions, altrimenti resta sul bucket senza riga che lo nomini.",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true,
                oldComment: "PDF archiviato all'invio: riferimento logico a notification.media_assets (kind = 'rendered_pdf'), NON una FK perche' attraversa lo schema di un altro modulo. NULL finche' il report non e' stato inviato. ATTENZIONE: sostituire il PDF richiede di accodare il vecchio oggetto in privacy.privacy_file_deletions, altrimenti si ripete il difetto del legacy che ha lasciato 24 oggetti orfani su 28.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "agreement_price_lists",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico (UUIDv7), copiato — booking_agreement_lines (Booking) referenzia il listino/voucher consumato.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico (UUIDv7), copiato — C1 booking_agreement_lines referenzia il listino/voucher consumato.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_user_id",
                schema: "catalog",
                table: "agreement_price_lists",
                type: "uuid",
                nullable: true,
                comment: "public_id di identity.access_users, copiato — operatore che ha creato il listino, nessuna FK cross-schema.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di identity_access_users (C5), copiato — operatore che ha creato il listino, nessuna FK cross-schema.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_manual_override",
                schema: "catalog",
                table: "agreement_price_list_stay_prices",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Sempre true per le righe presenti: si persistono solo le celle corrette a mano dal gestore.",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true,
                oldComment: "Sempre true per le righe presenti: si persistono solo gli override manuali (isPriceChange legacy).");

            migrationBuilder.AlterColumn<string>(
                name: "period_label",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "Etichetta «fila + periodo» dei dati importati, solo traccia: valgono sector_id e tariff_period_group.",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true,
                oldComment: "Etichetta legacy «fila + periodo», solo traccia della migrazione: valgono sector_id e tariff_period_group.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "agreement_partners",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico (UUIDv7), copiato cross-modulo da Booking (booking_reservations.agreement_partner_id).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico (UUIDv7), copiato cross-modulo da C1 (booking_reservations.agreement_partner_id).");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterTable(
                name: "media_assets",
                schema: "catalog",
                comment: "Catalogo locale al modulo catalog di tutti i riferimenti a file (foto, PDF, icone) usati dalle entità C2.",
                oldComment: "Catalogo locale al modulo catalog di tutti i riferimenti a file (foto, PDF, icone) usati dalle entità Catalog.");

            migrationBuilder.AlterTable(
                name: "map_sectors",
                schema: "catalog",
                comment: "Settori/file in cui è divisa la spiaggia di uno stabilimento. Sostituisce l'array JSON sectors_rows.sectors.",
                oldComment: "Settori/file in cui è divisa la spiaggia di uno stabilimento.");

            migrationBuilder.AlterTable(
                name: "map_layout_special_icons",
                schema: "catalog",
                comment: "Icone degli articoli personalizzati (1-8) per stato occupato/libero. Sostituisce le colonne specialUmbrella1..5 di configure_maps.",
                oldComment: "Icone degli articoli personalizzati (1-8) per stato occupato/libero.");

            migrationBuilder.AlterTable(
                name: "bathhouse_photos",
                schema: "catalog",
                comment: "Elenco ordinato delle foto della galleria pubblica di una vetrina. Sostituisce l'array JSON bathouses.photos.",
                oldComment: "Elenco ordinato delle foto della galleria pubblica di una vetrina.");

            migrationBuilder.AlterTable(
                name: "bathhouse_amenities",
                schema: "catalog",
                comment: "Giunzione N:N: quali dotazioni/servizi (amenity) offre una vetrina. Sostituisce la copia JSON integrale bathouses.services.",
                oldComment: "Giunzione N:N: quali dotazioni/servizi (amenity) offre una vetrina.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "service_sale_items",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico, copiato cross-modulo: C1 lo usa per contatori/consumi del servizio.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico, copiato cross-modulo: Booking lo usa per contatori/consumi del servizio.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "catalog",
                table: "service_sale_items",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete; un servizio con consumi storici (C1) non si cancella fisicamente.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete; un servizio con consumi storici (Booking) non si cancella fisicamente.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_user_id",
                schema: "catalog",
                table: "service_sale_items",
                type: "uuid",
                nullable: true,
                comment: "public_id di identity_access_users (C5), copiato — nessuna FK cross-schema.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di identity.access_users, copiato — nessuna FK cross-schema.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico, copiato cross-modulo: C1 lo usa come time_slot_id in booking_service_time_slots.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico, copiato cross-modulo: Booking lo usa come time_slot_id in booking_service_time_slots.");

            migrationBuilder.AlterColumn<string>(
                name: "label",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                comment: "Etichetta della fascia mostrata al cliente. Nei dati legacy è sempre una durata (\"30 min\", \"1 hr 30 min\"), mai un intervallo: vedi duration_minutes.",
                oldClrType: typeof(string),
                oldType: "character varying(40)",
                oldMaxLength: 40,
                oldComment: "Etichetta della fascia mostrata al cliente (es. \"30 min\", \"1 hr 30 min\").");

            migrationBuilder.AlterColumn<short>(
                name: "duration_minutes",
                schema: "catalog",
                table: "service_sale_item_time_slots",
                type: "smallint",
                nullable: true,
                comment: "Durata della fascia in minuti, quando il servizio è venduto a durata invece che a orario fisso. I dati legacy sono tutti di questa forma (undici valori: 10/15/30/45/60/90/120/180/240/300/360 minuti), quindi per lo storico migrato è questa la colonna valorizzata e slot_start/slot_end restano NULL. Per una fascia a orario assoluto vale il contrario: la durata si calcola e questa resta NULL.",
                oldClrType: typeof(short),
                oldType: "smallint",
                oldNullable: true,
                oldComment: "Durata della fascia in minuti, quando il servizio è venduto a durata invece che a orario fisso (slot_start/slot_end NULL). Per una fascia a orario assoluto vale il contrario: la durata si calcola e questa resta NULL.");

            migrationBuilder.AlterColumn<decimal>(
                name: "min_price",
                schema: "catalog",
                table: "search_bathhouses",
                type: "numeric(12,2)",
                nullable: true,
                comment: "Prezzo minimo corrente prenotabile online, materializzato dal rebuild (evita la query-per-risultato del legacy).",
                oldClrType: typeof(decimal),
                oldType: "numeric(12,2)",
                oldNullable: true,
                oldComment: "Prezzo minimo corrente prenotabile online, materializzato dal rebuild (nessuna query per ogni risultato).");

            migrationBuilder.AlterColumn<bool>(
                name: "allows_sharing",
                schema: "catalog",
                table: "property_online_booking_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Espone al cliente la funzione di sharing durante la prenotazione online (i parametri operativi vivono in C1).",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Espone al cliente la funzione di sharing durante la prenotazione online (i parametri operativi vivono in Booking).");

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
                oldComment: "Etichetta «fila + periodo» dei dati importati (es. \"Fila 1 Alta stagione\"), solo traccia: valgono sector_id e tariff_period_group.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_suggested_price_enabled",
                schema: "catalog",
                table: "pricing_revenue_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Prezzo suggerito attivo (legacy revenue_notifications.isRevenuePricesEnabled).",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Prezzo suggerito attivo: il motore di prezzo usa la scelta del gestore.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_shown_on_public_page",
                schema: "catalog",
                table: "pricing_revenue_settings",
                type: "boolean",
                nullable: false,
                defaultValue: false,
                comment: "Il banner sconto è visibile sulla pagina pubblica dello stabilimento / flusso di prenotazione (indipendente da marketing_event.showOnEstablishmentPage di C6).",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: false,
                oldComment: "Il banner sconto è visibile sulla pagina pubblica dello stabilimento / flusso di prenotazione (indipendente dalle campagne marketing).");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "pricing_price_lists",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato in C1 booking_lines.price_list_package_id.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato in Booking booking_lines.price_list_package_id.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_manual_override",
                schema: "catalog",
                table: "pricing_price_list_stay_prices",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Sempre true per le righe presenti: si persistono solo gli override manuali (isPriceChange legacy).",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true,
                oldComment: "Sempre true per le righe presenti: si persistono solo le celle corrette a mano dal gestore.");

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
                oldComment: "Prezzo forfettario mezza giornata nel settore (come nei listini delle convenzioni; spesso NULL).");

            migrationBuilder.AlterColumn<int>(
                name: "used_count",
                schema: "catalog",
                table: "pricing_discounts",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Utilizzi già effettuati, incrementato atomicamente da C1 alla conferma prenotazione.",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "Utilizzi già effettuati, incrementato atomicamente da Booking alla conferma prenotazione.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "pricing_discounts",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico, copiato negli snapshot sconto di C1 (booking_discount, discountData).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico, copiato negli snapshot sconto di Booking (booking_discount, discountData).");

            migrationBuilder.AlterColumn<byte[]>(
                name: "checksum",
                schema: "catalog",
                table: "media_assets",
                type: "bytea",
                nullable: true,
                comment: "Hash SHA-256 del contenuto, calcolato all'upload, per deduplica e verifica integrità. NULL per i file migrati dal legacy.",
                oldClrType: typeof(byte[]),
                oldType: "bytea",
                oldNullable: true,
                oldComment: "Hash SHA-256 del contenuto, calcolato all'upload, per deduplica e verifica integrità. NULL per i file importati finché non se ne calcolano i metadati.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "map_spots",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo (C1 resource_id, snapshot mappa).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo (Booking resource_id, snapshot mappa).");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "catalog",
                table: "map_spots",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete: la postazione non si cancella fisicamente se ha prenotazioni storiche in C1.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete: la postazione non si cancella fisicamente se ha prenotazioni storiche in Booking.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "map_sectors",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo da C1 (booking_lines.sector_id, resource_availability_daily.sector_id).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo da Booking (booking_lines.sector_id, resource_availability_daily.sector_id).");

            migrationBuilder.AlterColumn<short>(
                name: "grid_columns",
                schema: "catalog",
                table: "map_layouts",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0,
                comment: "Numero di colonne della griglia (legacy: configure_maps.items, rinominato per eliminare l'ambiguità).",
                oldClrType: typeof(short),
                oldType: "smallint",
                oldDefaultValue: (short)0,
                oldComment: "Numero di colonne della griglia (articoli per fila).");

            migrationBuilder.AlterColumn<short>(
                name: "variant",
                schema: "catalog",
                table: "map_layout_special_icons",
                type: "smallint",
                nullable: false,
                comment: "Numero dell'articolo personalizzato (1-8, i primi 5 corrispondono a SpecialUmbrella1..5 del legacy).",
                oldClrType: typeof(short),
                oldType: "smallint",
                oldComment: "Numero dell'articolo personalizzato (1-8).");

            migrationBuilder.AlterColumn<int>(
                name: "total_quantity",
                schema: "catalog",
                table: "inventory_items",
                type: "integer",
                nullable: false,
                defaultValue: 0,
                comment: "Quantità totale disponibile dell'articolo; la disponibilità per data è total_quantity meno i consumi (C1).",
                oldClrType: typeof(int),
                oldType: "integer",
                oldDefaultValue: 0,
                oldComment: "Quantità totale disponibile dell'articolo; la disponibilità per data è total_quantity meno i consumi (Booking).");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "inventory_items",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico (UUIDv7), copiato — C1 lo referenzia come resource_id nei consumi di magazzino.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico (UUIDv7), copiato — Booking lo referenzia come resource_id nei consumi di magazzino.");

            migrationBuilder.AlterColumn<DateTime>(
                name: "deleted_at",
                schema: "catalog",
                table: "inventory_items",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Soft-delete; un articolo con consumi storici (C1) non si cancella fisicamente.",
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true,
                oldComment: "Soft-delete; un articolo con consumi storici (Booking) non si cancella fisicamente.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_user_id",
                schema: "catalog",
                table: "inventory_items",
                type: "uuid",
                nullable: true,
                comment: "public_id di identity_access_users (C5), copiato — operatore che ha configurato l'articolo, nessuna FK cross-schema.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di identity.access_users, copiato — operatore che ha configurato l'articolo, nessuna FK cross-schema.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "bathhouses",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico stabile, copiato cross-modulo (C5 identity_customer_bathhouse_likes.bathhouse_id) e usato in URL/API pubbliche.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico stabile, copiato cross-modulo (identity_customer_bathhouse_likes.bathhouse_id) e usato in URL/API pubbliche.");

            migrationBuilder.AlterColumn<AgreementReportStatus>(
                name: "status",
                schema: "catalog",
                table: "agreement_reports",
                type: "catalog.agreement_report_status",
                nullable: false,
                defaultValueSql: "'pending'",
                comment: "Stato del report: pending, created o cancelled. Distinto da deleted_at, che e' il soft-delete della riga: nel legacy i due erano scritti sempre insieme.",
                oldClrType: typeof(AgreementReportStatus),
                oldType: "catalog.agreement_report_status",
                oldDefaultValueSql: "'pending'",
                oldComment: "Stato del report: pending, created o cancelled. Distinto da deleted_at, che e' il soft-delete della riga.");

            migrationBuilder.AlterColumn<long>(
                name: "pdf_media_asset_id",
                schema: "catalog",
                table: "agreement_reports",
                type: "bigint",
                nullable: true,
                comment: "PDF archiviato all'invio: riferimento logico a notification.media_assets (kind = 'rendered_pdf'), NON una FK perche' attraversa lo schema di un altro modulo. NULL finche' il report non e' stato inviato. ATTENZIONE: sostituire il PDF richiede di accodare il vecchio oggetto in privacy.privacy_file_deletions, altrimenti si ripete il difetto del legacy che ha lasciato 24 oggetti orfani su 28.",
                oldClrType: typeof(long),
                oldType: "bigint",
                oldNullable: true,
                oldComment: "PDF archiviato all'invio: riferimento logico a notification.media_assets (kind = 'rendered_pdf'), NON una FK perche' attraversa lo schema di un altro modulo. NULL finche' il report non e' stato inviato. ATTENZIONE: sostituire il PDF richiede di accodare il vecchio oggetto in privacy.privacy_file_deletions, altrimenti resta sul bucket senza riga che lo nomini.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "agreement_price_lists",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico (UUIDv7), copiato — C1 booking_agreement_lines referenzia il listino/voucher consumato.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico (UUIDv7), copiato — booking_agreement_lines (Booking) referenzia il listino/voucher consumato.");

            migrationBuilder.AlterColumn<Guid>(
                name: "created_by_user_id",
                schema: "catalog",
                table: "agreement_price_lists",
                type: "uuid",
                nullable: true,
                comment: "public_id di identity_access_users (C5), copiato — operatore che ha creato il listino, nessuna FK cross-schema.",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true,
                oldComment: "public_id di identity.access_users, copiato — operatore che ha creato il listino, nessuna FK cross-schema.");

            migrationBuilder.AlterColumn<bool>(
                name: "is_manual_override",
                schema: "catalog",
                table: "agreement_price_list_stay_prices",
                type: "boolean",
                nullable: false,
                defaultValue: true,
                comment: "Sempre true per le righe presenti: si persistono solo gli override manuali (isPriceChange legacy).",
                oldClrType: typeof(bool),
                oldType: "boolean",
                oldDefaultValue: true,
                oldComment: "Sempre true per le righe presenti: si persistono solo le celle corrette a mano dal gestore.");

            migrationBuilder.AlterColumn<string>(
                name: "period_label",
                schema: "catalog",
                table: "agreement_price_list_period_prices",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true,
                comment: "Etichetta legacy «fila + periodo», solo traccia della migrazione: valgono sector_id e tariff_period_group.",
                oldClrType: typeof(string),
                oldType: "character varying(120)",
                oldMaxLength: 120,
                oldNullable: true,
                oldComment: "Etichetta «fila + periodo» dei dati importati, solo traccia: valgono sector_id e tariff_period_group.");

            migrationBuilder.AlterColumn<Guid>(
                name: "public_id",
                schema: "catalog",
                table: "agreement_partners",
                type: "uuid",
                nullable: false,
                comment: "Id pubblico (UUIDv7), copiato cross-modulo da C1 (booking_reservations.agreement_partner_id).",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldComment: "Id pubblico (UUIDv7), copiato cross-modulo da Booking (booking_reservations.agreement_partner_id).");
        }
    }
}
