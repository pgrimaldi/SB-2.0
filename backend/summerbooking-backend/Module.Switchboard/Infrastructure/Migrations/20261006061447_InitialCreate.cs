using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Switchboard.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "switchboard");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:switchboard.call_direction", "inbound,outbound")
                .Annotation("Npgsql:Enum:switchboard.call_direction_raw", "incoming,outgoing")
                .Annotation("Npgsql:Enum:switchboard.call_event_source", "telnyx,ivr_step")
                .Annotation("Npgsql:Enum:switchboard.call_outcome", "answered_by_ivr,forwarded_answered,forwarded_no_answer,forwarded_busy,forwarded_failed,caller_hangup,rejected_inactive,no_config,hangup_after_timeout,whatsapp_only")
                .Annotation("Npgsql:Enum:switchboard.call_routing_mode", "ivr,direct_sip")
                .Annotation("Npgsql:Enum:switchboard.forward_caller_id_mode", "caller,centralino,custom")
                .Annotation("Npgsql:Enum:switchboard.forward_result", "answered,no_answer,busy,failed,skipped_no_endpoint")
                .Annotation("Npgsql:Enum:switchboard.identity_doc_type", "passport,national_id,driving_license")
                .Annotation("Npgsql:Enum:switchboard.it_customer_type", "individual,business")
                .Annotation("Npgsql:Enum:switchboard.media_origin", "s3_private,external")
                .Annotation("Npgsql:Enum:switchboard.menu_action_type", "hangup,forward_sip,send_whatsapp")
                .Annotation("Npgsql:Enum:switchboard.number_provisioning_state", "not_started,documents_pending,submitted,active,rejected,released")
                .Annotation("Npgsql:Enum:switchboard.softphone_push_platform", "fcm_android,apns_voip,web")
                .Annotation("Npgsql:Enum:switchboard.wa_conversation_origin", "marketing,utility,service,authentication,referral_conversion,unknown")
                .Annotation("Npgsql:Enum:switchboard.wa_header_format", "none,text,image,video,document")
                .Annotation("Npgsql:Enum:switchboard.wa_inbound_message_type", "text,button,interactive,reaction,image,document,audio,video,location,sticker,contacts,unknown")
                .Annotation("Npgsql:Enum:switchboard.wa_message_status", "sent,delivered,read,failed")
                .Annotation("Npgsql:Enum:switchboard.wa_pricing_category", "marketing,utility,authentication,service,unknown")
                .Annotation("Npgsql:Enum:switchboard.wa_recipient_mode", "caller,fixed")
                .Annotation("Npgsql:Enum:switchboard.wa_template_category", "marketing,utility,authentication")
                .Annotation("Npgsql:Enum:switchboard.wa_template_status", "pending,approved,rejected,paused,disabled,in_appeal,pending_deletion")
                .Annotation("Npgsql:Enum:switchboard.wa_webhook_event_type", "status,message_in");

            migrationBuilder.CreateTable(
                name: "switchboard_configs",
                schema: "switchboard",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile usato dal pannello back-office e negli export GDPR."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Chiave di accesso di tutti gli endpoint del modulo."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Interruttore del centralino: la chiamata è accettata solo se la config esiste ed è attiva."),
                    routing_mode = table.Column<int>(type: "switchboard.call_routing_mode", nullable: false, defaultValueSql: "'direct_sip'", comment: "Modalità di servizio della chiamata in ingresso: ivr (risponde via Call Control App) o direct_sip (auto-bridge diretto)."),
                    telnyx_phone_e164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Numero PSTN geografico Telnyx dello stabilimento, in E.164; chiave di lookup della chiamata in ingresso."),
                    telnyx_connection_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id della credential connection Telnyx dell'endpoint SIP (identificatore esterno opaco)."),
                    telnyx_call_control_app_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id della Call Control Application Telnyx; presente quando routing_mode = ivr."),
                    telnyx_messaging_profile_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id del messaging profile Telnyx usato per gli SMS (es. OTP di verifica numero WhatsApp)."),
                    sip_endpoint_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id della telephony credential Telnyx dell'utenza softphone dello stabilimento."),
                    sip_username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Username SIP dell'utenza softphone, generato server-side; usato per comporre il SIP URI di inoltro."),
                    sip_password_secret_ref = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Riferimento (ARN/nome) al segreto della password SIP nel secret manager — nessun materiale segreto in tabella."),
                    sip_client_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, defaultValue: "generic", comment: "Etichetta descrittiva del client SIP dello stabilimento; nessun effetto sulla logica di chiamata."),
                    sip_client_notes = table.Column<string>(type: "text", nullable: true, comment: "Note libere sull'installazione del client SIP, solo pannello."),
                    whatsapp_phone_number_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id Meta Cloud API del numero WhatsApp Business specifico dello stabilimento, se presente ha priorità sul numero generico di piattaforma."),
                    whatsapp_phone_e164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Numero WhatsApp Business in formato E.164, per la visualizzazione nel pannello."),
                    welcome_audio_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Audio riprodotto alla risposta, base del gather DTMF di benvenuto."),
                    fallback_audio_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Audio riprodotto quando un forward_sip fallisce, prima di chiudere la chiamata."),
                    forward_caller_id_mode = table.Column<int>(type: "switchboard.forward_caller_id_mode", nullable: false, defaultValueSql: "'caller'", comment: "Da quale numero il SIP endpoint vede arrivare la chiamata inoltrata: numero del chiamante, del centralino, o custom."),
                    forward_custom_caller_id_e164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Caller ID mostrato al SIP endpoint quando forward_caller_id_mode = custom."),
                    forward_caller_id_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Display name opzionale mostrato accanto al caller ID durante l'inoltro SIP."),
                    forward_ring_timeout_seconds = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)90, comment: "Secondi di squillo del leg SIP prima di considerare fallito l'inoltro."),
                    gather_timeout_seconds = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)7, comment: "Secondi di attesa dell'input DTMF prima del timeout."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_switchboard_configs", x => x.id);
                    table.CheckConstraint("ck_switchboard_configs_forward_custom_caller_id", "forward_caller_id_mode <> 'custom' OR forward_custom_caller_id_e164 IS NOT NULL");
                    table.CheckConstraint("ck_switchboard_configs_forward_ring_timeout", "forward_ring_timeout_seconds BETWEEN 5 AND 300");
                    table.CheckConstraint("ck_switchboard_configs_gather_timeout", "gather_timeout_seconds BETWEEN 3 AND 60");
                },
                comment: "Configurazione del centralino virtuale (IVR Telnyx + inoltro SIP + WhatsApp Business) di uno stabilimento; una riga per property_id.");

            migrationBuilder.CreateTable(
                name: "switchboard_media_assets",
                schema: "switchboard",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico per il pannello e l'anteprima autenticata; non compare nell'URL pubblico servito a Telnyx."),
                    config_id = table.Column<long>(type: "bigint", nullable: false, comment: "Centralino proprietario del file audio."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping/retention."),
                    origin = table.Column<int>(type: "switchboard.media_origin", nullable: false, defaultValueSql: "'s3_private'", comment: "Codice della sorgente storage; la mappa origin → URL base vive in configurazione."),
                    path = table.Column<string>(type: "text", nullable: false, comment: "Chiave relativa dell'oggetto rispetto alla base di origin, nessuna barra iniziale."),
                    original_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Nome file caricato dall'operatore, solo visualizzazione."),
                    description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Etichetta leggibile assegnata all'upload."),
                    content_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "audio/mpeg", comment: "MIME type del file finale, in pratica sempre audio/mpeg (conversione a MP3 obbligatoria)."),
                    byte_size = table.Column<int>(type: "integer", nullable: true, comment: "Dimensione in byte dell'MP3 finale."),
                    duration_ms = table.Column<int>(type: "integer", nullable: true, comment: "Durata del prompt in millisecondi, nota alla conversione ffmpeg."),
                    checksum = table.Column<byte[]>(type: "bytea", nullable: true, comment: "SHA-256 del contenuto, calcolato all'upload, per deduplica e verifica integrità."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_switchboard_media_assets", x => x.id);
                    table.CheckConstraint("ck_switchboard_media_assets_byte_size", "byte_size IS NULL OR byte_size > 0");
                    table.ForeignKey(
                        name: "fk_switchboard_media_assets_config",
                        column: x => x.config_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Catalogo locale allo schema switchboard dei file audio (prompt IVR) di uno stabilimento.");

            migrationBuilder.CreateTable(
                name: "switchboard_messaging_wa_event_logs",
                schema: "switchboard",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    config_id = table.Column<long>(type: "bigint", nullable: true, comment: "Centralino risolto da phone_number_id/display_phone_number; NULL se il numero non è mappato a un centralino noto."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato quando risolvibile."),
                    event_type = table.Column<int>(type: "switchboard.wa_webhook_event_type", nullable: false, comment: "status (da value.statuses[]) o message_in (da value.messages[])."),
                    wamid = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Id del messaggio Meta: st.id per gli status, m.id per gli inbound."),
                    waba_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id del WhatsApp Business Account (entry.id)."),
                    phone_number_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id del numero Meta che ha inviato/ricevuto (value.metadata); chiave per risolvere config_id."),
                    display_phone_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "Numero leggibile del numero Meta (value.metadata)."),
                    counterparty_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Numero dell'utente finale (mittente per message_in, destinatario per status), pseudonimizzato alla scrittura."),
                    contact_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Nome profilo del contatto (contact.profile.name); PII, pseudonimizzabile a fine retention."),
                    status = table.Column<int>(type: "switchboard.wa_message_status", nullable: true, comment: "Stato di consegna, valorizzato solo per event_type = status."),
                    error_code = table.Column<int>(type: "integer", nullable: true, comment: "Codice errore Meta del primo elemento di st.errors[] (status = failed)."),
                    error_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Titolo errore Meta del primo elemento di st.errors[]."),
                    error_message = table.Column<string>(type: "text", nullable: true, comment: "Messaggio di dettaglio dell'errore Meta."),
                    conversation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Id della conversazione ai fini fatturazione Meta (st.conversation.id)."),
                    conversation_origin = table.Column<int>(type: "switchboard.wa_conversation_origin", nullable: true, comment: "Origine/categoria della conversazione (passthrough Meta, unknown di fallback)."),
                    pricing_billable = table.Column<bool>(type: "boolean", nullable: true, comment: "Se la conversazione è fatturabile."),
                    pricing_category = table.Column<int>(type: "switchboard.wa_pricing_category", nullable: true, comment: "Categoria di pricing Meta (passthrough, unknown di fallback)."),
                    pricing_model = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Modello di pricing Meta grezzo (CBP/PMP)."),
                    message_type = table.Column<int>(type: "switchboard.wa_inbound_message_type", nullable: true, comment: "Tipo del messaggio in arrivo, valorizzato solo per event_type = message_in."),
                    body_text = table.Column<string>(type: "text", nullable: true, comment: "Parte testuale del messaggio in arrivo (text.body, button.text, titolo reply, emoji di reaction); NULL per media/interattivi complessi."),
                    body_payload = table.Column<string>(type: "jsonb", nullable: true, comment: "Payload strutturato per messaggi non testuali (media, interactive, location, contacts), passthrough Meta."),
                    raw_payload = table.Column<string>(type: "jsonb", nullable: true, comment: "Oggetto st/m completo (non l'intero webhook), con numeri pseudonimizzati."),
                    meta_timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante dell'evento lato Meta (st.timestamp/m.timestamp)."),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Istante in cui il nostro server ha ricevuto il webhook."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_switchboard_messaging_wa_event_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_switchboard_messaging_wa_event_logs_config",
                        column: x => x.config_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Evento del webhook WhatsApp Meta Cloud API ricevuto sul numero del centralino: aggiornamento di consegna o messaggio in arrivo. Log forense write-only.");

            migrationBuilder.CreateTable(
                name: "switchboard_messaging_wa_templates",
                schema: "switchboard",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile per l'API del pannello."),
                    config_id = table.Column<long>(type: "bigint", nullable: false, comment: "Centralino proprietario del template."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping e unicità (property_id, name, language)."),
                    template_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false, comment: "Nome del template su Meta (minuscolo con underscore, unico per WABA); valore passato a template.name all'invio/cancellazione."),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "it", comment: "Codice lingua del template nel formato Meta (language.code); codice di libreria, non enum di dominio."),
                    category = table.Column<int>(type: "switchboard.wa_template_category", nullable: false, defaultValueSql: "'utility'", comment: "Categoria Meta del template, impatta pricing e regole d'invio."),
                    components = table.Column<string>(type: "jsonb", nullable: false, comment: "Array dei components del template nel formato Meta Cloud API, salvato e inviato verbatim, mai interrogato per sotto-campo."),
                    meta_status = table.Column<int>(type: "switchboard.wa_template_status", nullable: false, defaultValueSql: "'pending'", comment: "Stato di approvazione lato Meta, aggiornato dal refresh periodico."),
                    meta_status_raw = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Stringa di stato così come restituita da Meta, per gestire valori nuovi non ancora mappati nell'enum."),
                    meta_template_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id assegnato da Meta alla creazione, usato per interrogare lo stato."),
                    meta_rejection_reason = table.Column<string>(type: "text", nullable: true, comment: "Motivo di rifiuto restituito da Meta, popolato dal refresh di stato."),
                    header_format = table.Column<int>(type: "switchboard.wa_header_format", nullable: false, defaultValueSql: "'none'", comment: "Formato dell'header del template, derivato da components alla scrittura — colonna di riepilogo non autorevole."),
                    has_buttons = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se il template ha bottoni, derivato da components alla scrittura — colonna di riepilogo non autorevole."),
                    buttons_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Numero di bottoni del template, derivato da components alla scrittura."),
                    body_param_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Numero di segnaposto nel corpo del template, derivato da components alla scrittura."),
                    body_preview = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Primi ~120 caratteri del testo BODY, derivato da components per la lista del pannello senza parsare il blob."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_switchboard_messaging_wa_templates", x => x.id);
                    table.ForeignKey(
                        name: "fk_switchboard_messaging_wa_templates_config",
                        column: x => x.config_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Cache/audit locale di un template WhatsApp Business registrato su Meta per uno stabilimento; non governa l'invio, la fonte di verità resta Meta.");

            migrationBuilder.CreateTable(
                name: "switchboard_voice_call_records",
                schema: "switchboard",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile (export GDPR: la chiamata contiene numeri di telefono, dato personale)."),
                    config_id = table.Column<long>(type: "bigint", nullable: false, comment: "Centralino che ha gestito la chiamata."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping/retention/report senza join."),
                    direction = table.Column<int>(type: "switchboard.call_direction", nullable: false, comment: "inbound = chiamata in ingresso gestita dall'IVR; outbound = B-leg SIP creato dal nostro forward_sip."),
                    caller_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "Numero del chiamante PSTN originale in E.164 — dato personale, pseudonimizzabile a fine retention."),
                    callee_number = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true, comment: "Numero Telnyx del centralino chiamato (inbound), in E.164."),
                    sip_destination = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "SIP URI dell'endpoint dello stabilimento verso cui è stata inoltrata la chiamata, congelato all'istante della chiamata."),
                    outcome = table.Column<int>(type: "switchboard.call_outcome", nullable: false, comment: "Esito finale della chiamata."),
                    forward_result = table.Column<int>(type: "switchboard.forward_result", nullable: true, comment: "Esito del tentativo di inoltro SIP, valorizzato solo se l'outcome riguarda un forward."),
                    ivr_path = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'[]'::jsonb", comment: "Sequenza sintetica dei tasti premuti e delle voci attraversate, snapshot scoped alla singola chiamata."),
                    final_menu_item_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id dell'ultima voce di menù raggiunta, per analisi di dove finiscono le chiamate."),
                    telnyx_call_session_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "Id di sessione Telnyx condiviso da A-leg e B-leg, per correlare il fallimento del forward."),
                    telnyx_call_control_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "Id Telnyx per i comandi sulla chiamata."),
                    telnyx_call_leg_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "Id Telnyx del leg di chiamata."),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Istante di inizio chiamata (evento call.initiated)."),
                    answered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di risposta (evento call.answered)."),
                    ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di fine chiamata (evento call.hangup)."),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true, comment: "Durata conversazione (ended_at - answered_at) in secondi."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_switchboard_voice_call_records", x => x.id);
                    table.ForeignKey(
                        name: "fk_switchboard_voice_call_records_config",
                        column: x => x.config_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Una riga per chiamata gestita dal centralino: esito, durata, percorso IVR — tabella di dominio, non log diagnostico.");

            migrationBuilder.CreateTable(
                name: "switchboard_voice_number_registrations",
                schema: "switchboard",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    config_id = table.Column<long>(type: "bigint", nullable: false, comment: "Centralino a cui appartiene il fascicolo (1:1)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping e policy d'accesso."),
                    provisioning_state = table.Column<int>(type: "switchboard.number_provisioning_state", nullable: false, defaultValueSql: "'not_started'", comment: "Stato interno dell'ordine numero e del suo ciclo di vita (not_started → documents_pending → submitted → active/rejected/released)."),
                    telnyx_status_raw = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true, comment: "Stato del numero così come restituito da Telnyx, conservato grezzo per diagnostica."),
                    telnyx_number_order_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id del number order Telnyx, per il polling dello stato di provisioning."),
                    telnyx_phone_number_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id dell'oggetto phone number Telnyx, per PATCH/GET stato/release."),
                    reg_requirement_group_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id del requirement group Telnyx (contenitore dei requisiti regolatori compilati)."),
                    reg_address_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id dell'address Telnyx principale usato per i requisiti regolatori."),
                    reg_supplemental_address_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id dell'address Telnyx supplementare usato per i requisiti regolatori."),
                    reg_proof_of_address_doc_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id del document Telnyx caricato come prova di indirizzo."),
                    reg_id_document_copy_doc_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id del document Telnyx caricato come copia del documento d'identità."),
                    reg_company_reg_cert_doc_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id del document Telnyx caricato come certificato camerale/visura."),
                    reg_it_customer_type = table.Column<int>(type: "switchboard.it_customer_type", nullable: true, comment: "Tipo cliente IT richiesto da Telnyx per l'ordine numero locale."),
                    reg_end_user_name = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "Nome dell'utilizzatore finale del numero (persona o azienda)."),
                    reg_identity_doc_type = table.Column<int>(type: "switchboard.identity_doc_type", nullable: true, comment: "Tipo del documento d'identità del referente."),
                    reg_identity_doc_number = table.Column<byte[]>(type: "bytea", nullable: true, comment: "Numero del documento d'identità del referente — PII ad alto rischio, cifrato a riposo (KMS)."),
                    reg_identity_doc_issuer = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true, comment: "Ente emittente del documento d'identità."),
                    reg_identity_doc_expiry = table.Column<DateOnly>(type: "date", nullable: true, comment: "Data di scadenza del documento d'identità (giorno di calendario puro)."),
                    reg_date_of_birth = table.Column<byte[]>(type: "bytea", nullable: true, comment: "Data di nascita del referente — PII, cifrata a riposo (KMS)."),
                    reg_place_of_birth = table.Column<byte[]>(type: "bytea", nullable: true, comment: "Luogo di nascita del referente — PII, cifrato a riposo (KMS)."),
                    reg_personal_tax_code = table.Column<byte[]>(type: "bytea", nullable: true, comment: "Codice fiscale personale del referente — PII ad alto rischio, cifrato a riposo, mai in export/log."),
                    reg_company_tax_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true, comment: "Codice fiscale aziendale."),
                    reg_vat_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Partita IVA."),
                    submitted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di invio del requirement group a Telnyx."),
                    activated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di attivazione del numero."),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di rilascio del numero; la riga resta come registro storico."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_switchboard_voice_number_registrations", x => x.id);
                    table.CheckConstraint("ck_switchboard_voice_number_registrations_release_sequence", "released_at IS NULL OR activated_at IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_switchboard_voice_number_registrations_config",
                        column: x => x.config_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stato dell'ordine del numero PSTN italiano su Telnyx e fascicolo dei requisiti regolatori (KYC) del centralino, 1:1 con la configurazione.");

            migrationBuilder.CreateTable(
                name: "switchboard_voice_softphone_devices",
                schema: "switchboard",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    config_id = table.Column<long>(type: "bigint", nullable: false, comment: "Centralino a cui è legato il dispositivo (l'endpoint SIP è per-stabilimento)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping/retention."),
                    registered_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity.access_users, copiato — nessuna FK cross-schema; operatore che ha registrato il dispositivo dal gestionale, se applicabile."),
                    platform = table.Column<int>(type: "switchboard.softphone_push_platform", nullable: false, comment: "Piattaforma del dispositivo; determina l'adapter di invio del push wake."),
                    token = table.Column<string>(type: "text", nullable: false, comment: "Token di registrazione push del provider (FCM registration token o APNs VoIP token)."),
                    token_hash = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false, comment: "SHA-256 del token, usato per l'indice univoco (i token sono lunghi e variabili)."),
                    device_label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true, comment: "Descrizione leggibile del dispositivo (modello / user-agent abbreviato)."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Token utilizzabile; portato a false su UNREGISTERED/InvalidRegistration o rimozione del dispositivo."),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Prima registrazione del dispositivo."),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultimo refresh o ultimo push riuscito; guida il purge dei dispositivi stantii."),
                    last_error = table.Column<string>(type: "text", nullable: true, comment: "Ultimo errore del provider push per questo token."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_switchboard_voice_softphone_devices", x => x.id);
                    table.ForeignKey(
                        name: "fk_switchboard_voice_softphone_devices_config",
                        column: x => x.config_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Dispositivo softphone registrato per ricevere il push wake prima dell'inoltro di una chiamata al centralino.");

            migrationBuilder.CreateTable(
                name: "switchboard_voice_menu_items",
                schema: "switchboard",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile per l'API del pannello e i deep-link."),
                    config_id = table.Column<long>(type: "bigint", nullable: false, comment: "Centralino a cui appartiene la voce."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping multi-tenant senza join."),
                    parent_id = table.Column<long>(type: "bigint", nullable: true, comment: "Voce genitore; NULL = voce di primo livello. Il runtime naviga l'albero usando questo campo come contesto corrente."),
                    depth = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Profondità del nodo (0 = radice), mantenuta coerente in applicazione a ogni spostamento."),
                    dtmf_key = table.Column<string>(type: "character(1)", fixedLength: true, maxLength: 1, nullable: false, comment: "Tasto che il chiamante preme per raggiungere la voce nel contesto del genitore."),
                    label = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Etichetta della voce, mostrata nel pannello e riportata nei log di step."),
                    audio_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Audio associato alla voce (foglia o intro del sotto-menù); NULL se la voce non riproduce audio."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Voce abilitata; disattivata è invisibile all'IVR (input trattato come DTMF non valido)."),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Ordinamento fra voci con lo stesso genitore, solo per la resa nel pannello — l'IVR seleziona per dtmf_key, non per posizione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_switchboard_voice_menu_items", x => x.id);
                    table.CheckConstraint("ck_switchboard_voice_menu_items_depth", "depth BETWEEN 0 AND 4");
                    table.CheckConstraint("ck_switchboard_voice_menu_items_dtmf_key", "dtmf_key ~ '^[0-9*#]$'");
                    table.ForeignKey(
                        name: "fk_switchboard_voice_menu_items_audio_asset",
                        column: x => x.audio_asset_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_switchboard_voice_menu_items_config",
                        column: x => x.config_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_switchboard_voice_menu_items_parent",
                        column: x => x.parent_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_voice_menu_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Voce di menù dell'IVR ad albero del centralino, nel contesto di un genitore (parent_id auto-referenziale; NULL = primo livello).");

            migrationBuilder.CreateTable(
                name: "switchboard_voice_call_diagnostics",
                schema: "switchboard",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    config_id = table.Column<long>(type: "bigint", nullable: true, comment: "Centralino risolto dall'handler; NULL finché il contesto non è risolto (config_not_found)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; risolto dall'handler, spesso NULL sui dati reali."),
                    call_record_id = table.Column<long>(type: "bigint", nullable: true, comment: "Chiamata di dominio a cui l'evento appartiene, quando risolvibile."),
                    event_source = table.Column<int>(type: "switchboard.call_event_source", nullable: false, comment: "Origine dell'evento: webhook Telnyx o passo interno dell'IVR."),
                    event_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Nome evento grezzo (call.initiated, step.dtmf_received, ...), vocabolario aperto non tipizzato."),
                    direction = table.Column<int>(type: "switchboard.call_direction_raw", nullable: true, comment: "Verso della chiamata grezzo Telnyx (incoming/outgoing)."),
                    caller_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Numero chiamante pseudonimizzato prima della scrittura."),
                    callee_number = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Numero chiamato pseudonimizzato prima della scrittura."),
                    dtmf_digits = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Cifre DTMF raccolte all'evento call.gather.ended (stringa vuota = timeout senza input)."),
                    telnyx_call_session_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "Id di sessione Telnyx per correlare gli eventi di una stessa chiamata."),
                    telnyx_call_control_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "Id Telnyx per i comandi sulla chiamata."),
                    telnyx_call_leg_id = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true, comment: "Id Telnyx del leg di chiamata."),
                    handler_result = table.Column<string>(type: "jsonb", nullable: true, comment: "Valore di ritorno dell'handler o dato del passo IVR, passthrough diagnostico mai interrogato per sotto-campo."),
                    raw_payload = table.Column<string>(type: "jsonb", nullable: true, comment: "Corpo del webhook Telnyx come ricevuto, con numeri pseudonimizzati; vuoto per le righe ivr_step."),
                    error_message = table.Column<string>(type: "text", nullable: true, comment: "Messaggio d'errore se l'handler/il passo ha lanciato un'eccezione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_switchboard_voice_call_diagnostics", x => x.id);
                    table.ForeignKey(
                        name: "fk_switchboard_voice_call_diagnostics_call_record",
                        column: x => x.call_record_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_voice_call_records",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_switchboard_voice_call_diagnostics_config",
                        column: x => x.config_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_configs",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Evento del flusso chiamata (webhook Telnyx o passo interno IVR); log diagnostico write-only, nessun lettore applicativo.");

            migrationBuilder.CreateTable(
                name: "switchboard_voice_menu_actions",
                schema: "switchboard",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile per l'API del pannello."),
                    menu_item_id = table.Column<long>(type: "bigint", nullable: false, comment: "Voce di menù a cui è collegata l'azione."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato per scoping multi-tenant senza join."),
                    action_type = table.Column<int>(type: "switchboard.menu_action_type", nullable: false, comment: "Tipo di azione: hangup (chiude, terminale), forward_sip (inoltra al SIP endpoint con i parametri della config, terminale), send_whatsapp (invia un template, non terminale)."),
                    execution_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Ordine di esecuzione delle azioni della stessa voce, ascendente."),
                    wa_template_id = table.Column<long>(type: "bigint", nullable: true, comment: "Template WhatsApp da inviare (solo send_whatsapp); FK viva, l'azione ne segue lingua/categoria finché il template esiste."),
                    wa_template_name_snapshot = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true, comment: "Nome del template su Meta congelato all'associazione, indipendente dal ciclo di vita della FK viva."),
                    wa_language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true, defaultValue: "it", comment: "Codice lingua del template nel formato Meta; codice di libreria, non enum di dominio."),
                    wa_recipient_mode = table.Column<int>(type: "switchboard.wa_recipient_mode", nullable: true, defaultValueSql: "'caller'", comment: "A chi inviare il template: numero del chiamante corrente o wa_recipient_phone_e164 fisso."),
                    wa_recipient_phone_e164 = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Destinatario fisso in E.164 quando wa_recipient_mode = fixed."),
                    wa_body_params = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'[]'::jsonb", comment: "Parametri posizionali del corpo del template Meta, passthrough dell'API di terzi."),
                    wa_header_params = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'[]'::jsonb", comment: "Parametri di header del template Meta, passthrough dell'API di terzi."),
                    wa_button_params = table.Column<string>(type: "jsonb", nullable: true, defaultValueSql: "'[]'::jsonb", comment: "Parametri di bottone del template Meta, passthrough dell'API di terzi."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della riga."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_switchboard_voice_menu_actions", x => x.id);
                    table.CheckConstraint("ck_switchboard_voice_menu_actions_wa_recipient", "wa_recipient_mode <> 'fixed' OR wa_recipient_phone_e164 IS NOT NULL");
                    table.CheckConstraint("ck_switchboard_voice_menu_actions_wa_template", "action_type <> 'send_whatsapp' OR wa_template_id IS NOT NULL");
                    table.ForeignKey(
                        name: "fk_switchboard_voice_menu_actions_menu_item",
                        column: x => x.menu_item_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_voice_menu_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_switchboard_voice_menu_actions_wa_template",
                        column: x => x.wa_template_id,
                        principalSchema: "switchboard",
                        principalTable: "switchboard_messaging_wa_templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Azione eseguita quando il chiamante seleziona una voce di menù dell'IVR, eseguita in sequenza per execution_order.");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_configs_fallback_audio_asset_id",
                schema: "switchboard",
                table: "switchboard_configs",
                column: "fallback_audio_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_configs_welcome_audio_asset_id",
                schema: "switchboard",
                table: "switchboard_configs",
                column: "welcome_audio_asset_id");

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_configs_phone",
                schema: "switchboard",
                table: "switchboard_configs",
                column: "telnyx_phone_e164",
                unique: true,
                filter: "deleted_at IS NULL AND telnyx_phone_e164 IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_configs_property",
                schema: "switchboard",
                table: "switchboard_configs",
                column: "property_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_configs_public_id",
                schema: "switchboard",
                table: "switchboard_configs",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_configs_sip_username",
                schema: "switchboard",
                table: "switchboard_configs",
                column: "sip_username",
                unique: true,
                filter: "deleted_at IS NULL AND sip_username IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_media_assets_config",
                schema: "switchboard",
                table: "switchboard_media_assets",
                column: "config_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_media_assets_gc",
                schema: "switchboard",
                table: "switchboard_media_assets",
                column: "deleted_at",
                filter: "deleted_at IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_media_assets_checksum",
                schema: "switchboard",
                table: "switchboard_media_assets",
                columns: new[] { "config_id", "checksum" },
                unique: true,
                filter: "checksum IS NOT NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_media_assets_public_id",
                schema: "switchboard",
                table: "switchboard_media_assets",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_messaging_wa_event_logs_config_time",
                schema: "switchboard",
                table: "switchboard_messaging_wa_event_logs",
                columns: new[] { "config_id", "received_at" });

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_messaging_wa_event_logs_purge",
                schema: "switchboard",
                table: "switchboard_messaging_wa_event_logs",
                column: "received_at");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_messaging_wa_event_logs_status",
                schema: "switchboard",
                table: "switchboard_messaging_wa_event_logs",
                columns: new[] { "config_id", "event_type", "status" },
                filter: "event_type = 'status'");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_messaging_wa_event_logs_wamid",
                schema: "switchboard",
                table: "switchboard_messaging_wa_event_logs",
                column: "wamid");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_messaging_wa_templates_config",
                schema: "switchboard",
                table: "switchboard_messaging_wa_templates",
                column: "config_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_messaging_wa_templates_meta_id",
                schema: "switchboard",
                table: "switchboard_messaging_wa_templates",
                column: "meta_template_id",
                filter: "meta_template_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_messaging_wa_templates_status",
                schema: "switchboard",
                table: "switchboard_messaging_wa_templates",
                columns: new[] { "property_id", "meta_status" });

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_messaging_wa_templates_public_id",
                schema: "switchboard",
                table: "switchboard_messaging_wa_templates",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_call_diagnostics_call_record",
                schema: "switchboard",
                table: "switchboard_voice_call_diagnostics",
                column: "call_record_id",
                filter: "call_record_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_call_diagnostics_config_time",
                schema: "switchboard",
                table: "switchboard_voice_call_diagnostics",
                columns: new[] { "config_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_call_diagnostics_purge",
                schema: "switchboard",
                table: "switchboard_voice_call_diagnostics",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_call_diagnostics_session",
                schema: "switchboard",
                table: "switchboard_voice_call_diagnostics",
                columns: new[] { "telnyx_call_session_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_call_records_caller",
                schema: "switchboard",
                table: "switchboard_voice_call_records",
                columns: new[] { "property_id", "caller_number", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_call_records_history",
                schema: "switchboard",
                table: "switchboard_voice_call_records",
                columns: new[] { "config_id", "started_at" },
                filter: "direction = 'outbound'");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_call_records_retention",
                schema: "switchboard",
                table: "switchboard_voice_call_records",
                column: "started_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_call_records_session",
                schema: "switchboard",
                table: "switchboard_voice_call_records",
                column: "telnyx_call_session_id");

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_voice_call_records_public_id",
                schema: "switchboard",
                table: "switchboard_voice_call_records",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_menu_actions_item_order",
                schema: "switchboard",
                table: "switchboard_voice_menu_actions",
                columns: new[] { "menu_item_id", "execution_order" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_menu_actions_wa_template",
                schema: "switchboard",
                table: "switchboard_voice_menu_actions",
                column: "wa_template_id",
                filter: "wa_template_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_voice_menu_actions_public_id",
                schema: "switchboard",
                table: "switchboard_voice_menu_actions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_menu_items_audio",
                schema: "switchboard",
                table: "switchboard_voice_menu_items",
                column: "audio_asset_id",
                filter: "audio_asset_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_menu_items_parent_id",
                schema: "switchboard",
                table: "switchboard_voice_menu_items",
                column: "parent_id");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_menu_items_runtime",
                schema: "switchboard",
                table: "switchboard_voice_menu_items",
                columns: new[] { "config_id", "parent_id", "dtmf_key" },
                unique: true,
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_menu_items_tree",
                schema: "switchboard",
                table: "switchboard_voice_menu_items",
                columns: new[] { "config_id", "parent_id", "sort_order" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_voice_menu_items_public_id",
                schema: "switchboard",
                table: "switchboard_voice_menu_items",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_number_registrations_doc_expiry",
                schema: "switchboard",
                table: "switchboard_voice_number_registrations",
                column: "reg_identity_doc_expiry",
                filter: "reg_identity_doc_expiry IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_number_registrations_state",
                schema: "switchboard",
                table: "switchboard_voice_number_registrations",
                column: "provisioning_state",
                filter: "provisioning_state IN ('documents_pending','submitted')");

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_voice_number_registrations_config",
                schema: "switchboard",
                table: "switchboard_voice_number_registrations",
                column: "config_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_softphone_devices_config_active",
                schema: "switchboard",
                table: "switchboard_voice_softphone_devices",
                column: "config_id",
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_switchboard_voice_softphone_devices_stale",
                schema: "switchboard",
                table: "switchboard_voice_softphone_devices",
                column: "last_seen_at",
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "uq_switchboard_voice_softphone_devices_token",
                schema: "switchboard",
                table: "switchboard_voice_softphone_devices",
                column: "token_hash",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "fk_switchboard_configs_fallback_audio_asset",
                schema: "switchboard",
                table: "switchboard_configs",
                column: "fallback_audio_asset_id",
                principalSchema: "switchboard",
                principalTable: "switchboard_media_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "fk_switchboard_configs_welcome_audio_asset",
                schema: "switchboard",
                table: "switchboard_configs",
                column: "welcome_audio_asset_id",
                principalSchema: "switchboard",
                principalTable: "switchboard_media_assets",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_switchboard_configs_fallback_audio_asset",
                schema: "switchboard",
                table: "switchboard_configs");

            migrationBuilder.DropForeignKey(
                name: "fk_switchboard_configs_welcome_audio_asset",
                schema: "switchboard",
                table: "switchboard_configs");

            migrationBuilder.DropTable(
                name: "switchboard_messaging_wa_event_logs",
                schema: "switchboard");

            migrationBuilder.DropTable(
                name: "switchboard_voice_call_diagnostics",
                schema: "switchboard");

            migrationBuilder.DropTable(
                name: "switchboard_voice_menu_actions",
                schema: "switchboard");

            migrationBuilder.DropTable(
                name: "switchboard_voice_number_registrations",
                schema: "switchboard");

            migrationBuilder.DropTable(
                name: "switchboard_voice_softphone_devices",
                schema: "switchboard");

            migrationBuilder.DropTable(
                name: "switchboard_voice_call_records",
                schema: "switchboard");

            migrationBuilder.DropTable(
                name: "switchboard_voice_menu_items",
                schema: "switchboard");

            migrationBuilder.DropTable(
                name: "switchboard_messaging_wa_templates",
                schema: "switchboard");

            migrationBuilder.DropTable(
                name: "switchboard_media_assets",
                schema: "switchboard");

            migrationBuilder.DropTable(
                name: "switchboard_configs",
                schema: "switchboard");
        }
    }
}
