using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Notification.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "notification");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:notification.automation_kind", "welcome,goodbye,custom")
                .Annotation("Npgsql:Enum:notification.automation_schedule_type", "before,after,custom")
                .Annotation("Npgsql:Enum:notification.automation_state", "draft,active,paused,canceled")
                .Annotation("Npgsql:Enum:notification.automation_trigger_kind", "of_booking,before_booking,after_booking,pending_payments")
                .Annotation("Npgsql:Enum:notification.campaign_state", "draft,scheduled,sending,sent,canceled")
                .Annotation("Npgsql:Enum:notification.channel", "email,whatsapp,sms,push,in_app")
                .Annotation("Npgsql:Enum:notification.channel_or_all", "email,whatsapp,sms,all")
                .Annotation("Npgsql:Enum:notification.claim_source_channel", "public_site,mobile_app")
                .Annotation("Npgsql:Enum:notification.claim_status", "new,in_review,contacted,approved,rejected,spam")
                .Annotation("Npgsql:Enum:notification.consent_source", "booking_flag,public_signup,manual_import,campaign_recipient_list,admin,unknown")
                .Annotation("Npgsql:Enum:notification.contact_list_origin", "manual,default_seed,duplicate")
                .Annotation("Npgsql:Enum:notification.delivery_kind", "booking_confirmation,reservation_summary,payment_link,payment_receipt,e_receipt,non_fiscal_receipt,agreement_report,resale_credit_earned,wallet_statement,lifeguard_attendance,forgot_password,email_verification,account_lifecycle,newsletter,marketing_campaign,marketing_automation,establishment_claim,test,other")
                .Annotation("Npgsql:Enum:notification.delivery_provider", "smtp,ses,twilio_whatsapp,twilio_sms,fcm,apns,web_push,internal")
                .Annotation("Npgsql:Enum:notification.delivery_status", "queued,sending,sent,delivered,failed,bounced,skipped")
                .Annotation("Npgsql:Enum:notification.event_reminder_offset", "one_day_before,three_days_before,one_week_before,custom")
                .Annotation("Npgsql:Enum:notification.event_repeat_type", "never,daily,weekly,monthly,yearly,custom")
                .Annotation("Npgsql:Enum:notification.event_repeat_unit", "day,week,month,year")
                .Annotation("Npgsql:Enum:notification.inapp_kind", "booking_created,service_reservation_created,payment_received,ereceipt_data_error,auto_receipt_failed,receipted_booking_move_blocked,generic")
                .Annotation("Npgsql:Enum:notification.inapp_subject_kind", "booking,service_reservation")
                .Annotation("Npgsql:Enum:notification.inapp_visibility", "visible,pending_confirmation")
                .Annotation("Npgsql:Enum:notification.list_member_source", "manual,booking_auto_enroll,import,duplicate")
                .Annotation("Npgsql:Enum:notification.marketing_consent_status", "unknown,granted,withdrawn")
                .Annotation("Npgsql:Enum:notification.marketing_parent_kind", "campaign,automation")
                .Annotation("Npgsql:Enum:notification.media_kind", "template_attachment,campaign_attachment,event_cover_image,rendered_pdf,inbound_upload,other")
                .Annotation("Npgsql:Enum:notification.media_origin", "s3_us_east_2,cloudfront,external")
                .Annotation("Npgsql:Enum:notification.newsletter_source", "public_site,mobile_app,user_signup,property_registration,admin")
                .Annotation("Npgsql:Enum:notification.newsletter_status", "pending_confirmation,subscribed,unsubscribed")
                .Annotation("Npgsql:Enum:notification.notification_kind", "booking_created,service_reservation_created,payment_received,ereceipt_data_error,auto_receipt_failed,receipted_booking_move_blocked,booking_confirmation,reservation_summary,payment_link,payment_receipt,e_receipt,non_fiscal_receipt,agreement_report,wallet_statement,lifeguard_attendance")
                .Annotation("Npgsql:Enum:notification.push_platform", "fcm_android,fcm_ios,fcm_web,apns,web_push")
                .Annotation("Npgsql:Enum:notification.recipient_skip_reason", "no_consent,suppressed,empty_address,invalid_address,duplicate")
                .Annotation("Npgsql:Enum:notification.recipient_status", "pending,sent,delivered,failed,bounced,skipped")
                .Annotation("Npgsql:Enum:notification.recipient_type", "customer,staff_user,property_contact,hotel_partner,platform_admin,external_address")
                .Annotation("Npgsql:Enum:notification.reminder_status", "pending,sent,failed,canceled")
                .Annotation("Npgsql:Enum:notification.skip_reason", "opted_out,suppressed,missing_address,non_prod_env,hard_bounce_history,consent_not_verified")
                .Annotation("Npgsql:Enum:notification.smtp_security", "none,starttls,ssl")
                .Annotation("Npgsql:Enum:notification.staff_channel", "in_app,email,whatsapp,push")
                .Annotation("Npgsql:Enum:notification.subject_type", "booking_reservation,service_reservation,payment_request_link,payment_transaction,fiskaly_receipt,agreement_report,resale_offer,wallet_transaction,marketing_campaign,marketing_automation,marketing_event,newsletter_subscription,user_account,none")
                .Annotation("Npgsql:Enum:notification.suppression_reason", "unsubscribe_link,manual_request,spam_complaint,hard_bounce,gdpr_erasure,admin")
                .Annotation("Npgsql:Enum:notification.template_channel", "email,whatsapp,sms,push,any")
                .Annotation("Npgsql:Enum:notification.template_kind", "reservation_summary,agreement_report,campaign,automation,transactional,generic");

            migrationBuilder.CreateTable(
                name: "email_channel_settings",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema."),
                    from_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "Indirizzo mittente delle email inviate per conto della property."),
                    from_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Nome visualizzato del mittente, usato nel From (\"from_name\" <from_email>)."),
                    smtp_host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Host del server SMTP."),
                    smtp_port = table.Column<int>(type: "integer", nullable: false, defaultValue: 465, comment: "Porta SMTP (465 TLS implicito, 587 STARTTLS, 25)."),
                    smtp_username = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "Utente per l'autenticazione SMTP."),
                    smtp_password_secret_ref = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Riferimento (ARN/nome) alla password SMTP nel secret manager; nessun materiale segreto in tabella, risolto a runtime."),
                    security = table.Column<int>(type: "notification.smtp_security", nullable: false, defaultValueSql: "'ssl'", comment: "Modalità di cifratura del canale SMTP."),
                    verify_certificate = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Se validare il certificato TLS del server SMTP; false ammesso solo con motivazione."),
                    test_recipients = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'", comment: "Destinatari dell'invio di prova dal pannello gestionale."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Se il canale è usato per gli invii della property; false = fallback al mittente di piattaforma."),
                    last_verification_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante dell'ultimo TENTATIVO di verifica SMTP (handshake + auth + invio di prova); NULL = mai verificata."),
                    last_verification_error = table.Column<string>(type: "text", nullable: true, comment: "Dettaglio testuale dell'errore dell'ultima verifica SMTP; NULL con last_verification_at valorizzato = verifica riuscita. Sostituisce il flag is_verified, che era derivabile da questa coppia."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione della configurazione."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete (ex deleteEmailConfig).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_email_channel_settings", x => x.id);
                },
                comment: "Configurazione SMTP in uscita per stabilimento: mittente proprio al posto di noreply@summerbooking.it.");

            migrationBuilder.CreateTable(
                name: "establishment_claim_requests",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, usato nel pannello admin."),
                    bathhouse_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.bathhouses, copiato — nessuna FK cross-schema. NULL se il form non passa un bathhouseId valido."),
                    bathhouse_name_snapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Nome dello stabilimento rivendicato, congelato al momento della richiesta."),
                    full_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false, comment: "Nome dichiarato dal richiedente."),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "Email di contatto del richiedente, validata server-side."),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Telefono del richiedente, formato libero."),
                    message = table.Column<string>(type: "text", nullable: true, comment: "Testo libero della richiesta."),
                    source_channel = table.Column<int>(type: "notification.claim_source_channel", nullable: false, defaultValueSql: "'public_site'", comment: "Canale di provenienza della richiesta (sito pubblico o app mobile)."),
                    status = table.Column<int>(type: "notification.claim_status", nullable: false, defaultValueSql: "'new'", comment: "Avanzamento della gestione del lead."),
                    handled_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema. Chi ha lavorato il lead."),
                    handled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di lavorazione del lead."),
                    resolution_note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Nota libera sull'esito della lavorazione."),
                    client_ip_snapshot = table.Column<IPAddress>(type: "inet", nullable: true, comment: "Indirizzo IP del richiedente, per anti-spam; retention breve."),
                    user_agent_snapshot = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "User-agent del richiedente, per anti-spam; retention breve."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data della richiesta."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica (cambio status, note)."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: archiviazione del lead.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_establishment_claim_requests", x => x.id);
                },
                comment: "Lead 'Richiedi la tua pagina': un titolare rivendica la gestione di uno stabilimento già elencato sul sito.");

            migrationBuilder.CreateTable(
                name: "inapp_messages",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile per il deep-link dalla campanella."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Unico criterio di targeting: visibile a tutti gli operatori della property."),
                    kind = table.Column<int>(type: "notification.inapp_kind", nullable: false, defaultValueSql: "'generic'", comment: "Tipo di evento: determina template di rendering e navigazione UI al click."),
                    subject_kind = table.Column<int>(type: "notification.inapp_subject_kind", nullable: false, defaultValueSql: "'booking'", comment: "Soggetto della notifica (rotta di navigazione): prenotazione postazioni o servizio."),
                    subject_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di booking.reservations, copiato — nessuna FK cross-schema. NULL per le notifiche di errore Fiskaly."),
                    booking_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Numero prenotazione per il redirect al planner; nessun default fittizio."),
                    visibility = table.Column<int>(type: "notification.inapp_visibility", nullable: false, defaultValueSql: "'visible'", comment: "Visibilità nel feed: visible, oppure pending_confirmation finché il pagamento non è confermato."),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'", comment: "Snapshot delle sole variabili di rendering del template per questo kind, congelate alla creazione."),
                    legacy_text = table.Column<string>(type: "text", nullable: true, comment: "Stringa originale conservata as-is per le righe migrate il cui parsing verso kind+payload non ha riconosciuto il pattern."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema. NULL per eventi di sistema (webhook Stripe, cron Fiskaly)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data creazione dell'evento; unico ordinamento del feed."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data ultima modifica (es. transizione di visibility)."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete; retention operativa 6-12 mesi, non è un archivio.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_inapp_messages", x => x.id);
                },
                comment: "Un evento operativo di uno stabilimento mostrato nel feed della campanella del gestionale; testo renderizzato alla lettura da kind+payload.");

            migrationBuilder.CreateTable(
                name: "marketing_contact_lists",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile (API/URL)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema."),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false, comment: "Nome della lista (\"Marketing\" per la default, o nome scelto dall'utente)."),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Lista predefinita della property, bersaglio dell'auto-iscrizione da prenotazione."),
                    origin = table.Column<int>(type: "notification.contact_list_origin", nullable: false, defaultValueSql: "'manual'", comment: "Come è stata creata la lista: manuale, seed di default, o duplicazione."),
                    member_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Numero di membri vivi, denormalizzato e aggiornato per evento; riconciliato da job periodico."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione della lista."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data di soft-delete; NULL se la lista è attiva.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_marketing_contact_lists", x => x.id);
                },
                comment: "Segmento di clientela di uno stabilimento, target di campagne e automazioni marketing.");

            migrationBuilder.CreateTable(
                name: "marketing_contacts",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, usato in API/URL e nell'export GDPR."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema."),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "Indirizzo email del contatto (chiave naturale), normalizzato lower-case + trim."),
                    full_name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Nome del contatto, denormalizzato per la personalizzazione senza join a C5."),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Telefono del contatto, denormalizzato per l'invio WhatsApp/SMS."),
                    customer_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_customer.customers, copiato — nessuna FK cross-schema. Valorizzato quando il contatto è anche in anagrafica."),
                    consent_status = table.Column<int>(type: "notification.marketing_consent_status", nullable: false, defaultValueSql: "'unknown'", comment: "Stato del consenso all'invio di comunicazioni marketing."),
                    consent_source = table.Column<int>(type: "notification.consent_source", nullable: false, defaultValueSql: "'unknown'", comment: "Come il contatto/consenso è stato acquisito."),
                    consent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data/ora del consenso, o della revoca se withdrawn; decorrenza della retention marketing (24 mesi)."),
                    consent_notice_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Versione dell'informativa privacy accettata all'atto del consenso, prova ex art. 7 GDPR insieme a fonte e data."),
                    unsubscribe_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Token opaco stabile per il link di disiscrizione nelle email marketing."),
                    is_seed_from_booking = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "True se il contatto è nato dall'auto-iscrizione alla lista di default (bookings.isMarketingCampaign)."),
                    last_contacted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Ultimo invio marketing riuscito verso questo contatto; guida la retention 24 mesi dall'ultimo contatto."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione del contatto."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: retention 24 mesi senza attività (la prova di revoca resta a vita in notification_marketing_suppressions).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_marketing_contacts", x => x.id);
                },
                comment: "Rubrica marketing per stabilimento: un contatto identificato dall'email, con stato di consenso, fonte e token di disiscrizione.");

            migrationBuilder.CreateTable(
                name: "marketing_newsletter_subscriptions",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile (export GDPR, pannello admin)."),
                    email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "Indirizzo iscritto, normalizzato lower-case e trim."),
                    status = table.Column<int>(type: "notification.newsletter_status", nullable: false, defaultValueSql: "'pending_confirmation'", comment: "Stato del ciclo di vita double opt-in: in attesa di conferma, iscritto, disiscritto."),
                    source = table.Column<int>(type: "notification.newsletter_source", nullable: false, defaultValueSql: "'public_site'", comment: "Punto di ingresso dell'iscrizione (sito pubblico, app, signup utente, creazione stabilimento, admin)."),
                    confirmation_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Token per confermare l'iscrizione via email; azzerato dopo la conferma."),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data di conferma del double opt-in."),
                    unsubscribe_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Token opaco stabile per il link di disiscrizione."),
                    consent_notice_version = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true, comment: "Versione dell'informativa privacy accettata al momento dell'iscrizione — prova del consenso art. 7 GDPR."),
                    customer_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access... customers (C5), copiato — nessuna FK cross-schema."),
                    unsubscribed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data della disiscrizione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di iscrizione."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data di soft-delete amministrativo.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_marketing_newsletter_subscriptions", x => x.id);
                },
                comment: "Iscrizione alla newsletter globale di piattaforma (nessuno stabilimento associato).");

            migrationBuilder.CreateTable(
                name: "marketing_suppressions",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties per cui vale la soppressione; NULL = globale su tutta la piattaforma."),
                    address = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "Email (lower-case) o numero E.164 da sopprimere."),
                    channel = table.Column<int>(type: "notification.channel_or_all", nullable: false, defaultValueSql: "'all'", comment: "Canale a cui si applica l'opt-out (email, whatsapp, sms, o tutti)."),
                    reason = table.Column<int>(type: "notification.suppression_reason", nullable: false, defaultValueSql: "'unsubscribe_link'", comment: "Motivo della soppressione (click unsubscribe, richiesta manuale, reclamo spam, hard bounce, cancellazione GDPR, admin)."),
                    source_delivery_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id della notification_deliveries da cui deriva la soppressione (link cliccato, bounce) — copiato, nessuna FK: deve sopravvivere alla purge della consegna."),
                    contact_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id del contatto marketing al momento della soppressione — copiato, nessuna FK: deve sopravvivere alla cancellazione del contatto per retention."),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Testo libero per i motivi manual_request / admin."),
                    suppressed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Quando è scattata la soppressione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità con le altre tabelle; di fatto sempre NULL — la lista di soppressione non ha retention.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_marketing_suppressions", x => x.id);
                },
                comment: "Lista di soppressione (opt-out) marketing: indirizzi da non ricontattare, prova della revoca del consenso.");

            migrationBuilder.CreateTable(
                name: "media_assets",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. NULL per file di piattaforma o upload da lead pubblici."),
                    origin = table.Column<int>(type: "notification.media_origin", nullable: false, comment: "Codice della sorgente da cui è servito il file; la mappa origin → URL base vive in configurazione."),
                    path = table.Column<string>(type: "text", nullable: false, comment: "Percorso relativo del file rispetto alla base di origin (URL completo invariato se origin = external), senza barra iniziale."),
                    kind = table.Column<int>(type: "notification.media_kind", nullable: false, defaultValueSql: "'other'", comment: "Tipo d'uso del file."),
                    filename = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Nome file originale, mostrato all'utente e usato come filename MIME in allegato."),
                    checksum = table.Column<byte[]>(type: "bytea", nullable: true, comment: "SHA-256 del contenuto, per deduplica in caricamento; NULL per i file migrati finché non riconciliati."),
                    byte_size = table.Column<int>(type: "integer", nullable: true, comment: "Dimensione del file in byte."),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "MIME type reale, validato con magic bytes in upload."),
                    uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema. Valorizzato per gli upload da gestionale."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione dell'asset."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: l'asset non si cancella finché referenziato; la GC fisica sullo storage agisce solo su asset soft-deleted e senza referenti.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_media_assets", x => x.id);
                },
                comment: "Catalogo locale al modulo dei riferimenti a file usati nelle comunicazioni: allegati, immagini di copertina, PDF renderizzati, upload da lead.");

            migrationBuilder.CreateTable(
                name: "push_device_tokens",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per scoping/retention."),
                    token = table.Column<string>(type: "text", nullable: false, comment: "Token di registrazione push del provider (FCM/APNs/web push), valore opaco."),
                    token_hash = table.Column<string>(type: "char(64)", nullable: false, comment: "SHA-256 del token, usato per l'indice UNIQUE (i token FCM sono lunghi e variabili)."),
                    platform = table.Column<int>(type: "notification.push_platform", nullable: false, defaultValueSql: "'fcm_android'", comment: "Piattaforma del dispositivo; determina l'adapter di invio."),
                    device_label = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true, comment: "Descrizione leggibile del dispositivo (modello/user-agent abbreviato)."),
                    app_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Versione dell'app mobile al momento della registrazione, per diagnostica."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Token utilizzabile; portato a false quando il provider segnala UNREGISTERED/InvalidRegistration o l'utente rimuove il dispositivo."),
                    registered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Istante di prima registrazione del token."),
                    last_seen_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Istante dell'ultimo refresh o dell'ultimo invio riuscito; guida il purge dei token stantii."),
                    last_error = table.Column<string>(type: "text", nullable: true, comment: "Ultimo errore del provider riportato per questo token."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: dispositivo rimosso. Retention: token inattivo da N mesi → hard-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_push_device_tokens", x => x.id);
                },
                comment: "Un token di dispositivo registrato da un utente C5 per ricevere notifiche push, con piattaforma, ultimo uso e stato.");

            migrationBuilder.CreateTable(
                name: "templates",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, usato in API/URL gestionale e riferito da notification_deliveries."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. NULL = template di piattaforma/seed di sistema."),
                    template_kind = table.Column<int>(type: "notification.template_kind", nullable: false, defaultValueSql: "'generic'", comment: "Scopo del template (riepilogo prenotazione, report convenzione, campagna, automazione, transazionale, generico)."),
                    channel = table.Column<int>(type: "notification.template_channel", nullable: false, defaultValueSql: "'email'", comment: "Canale per cui è pensato il contenuto; 'any' = il renderer applica l'escaping del canale d'invio effettivo."),
                    culture = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false, defaultValue: "it", comment: "Cultura del contenuto, formato BCP-47."),
                    title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false, comment: "Etichetta identificativa mostrata nella tendina di selezione (non è il contenuto)."),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Oggetto del messaggio con segnaposto {{ … }}; obbligatorio in validazione per i canali email/push."),
                    body = table.Column<string>(type: "text", nullable: false, comment: "Corpo del messaggio con segnaposto {{ … }} mustache: HTML per email, testo per gli altri canali."),
                    placeholder_keys_used = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'", comment: "Segnaposto canonici realmente usati in subject+body, derivati dal parsing e mantenuti in sync ad ogni salvataggio."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Template selezionabile dal gestore; i lettori filtrano is_active AND deleted_at IS NULL."),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Template predefinito per la combinazione (property_id, template_kind, channel, culture)."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione del template."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: ex status=0 di custom_templates / isDeleted di hotel_message_templates.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_templates", x => x.id);
                },
                comment: "Catalogo unico dei template di messaggio: oggetto + corpo con segnaposto per uno scopo, un canale e una lingua.");

            migrationBuilder.CreateTable(
                name: "user_channel_preferences",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato per scoping/retention; NULL per utenti multi-property/piattaforma."),
                    enabled_channels = table.Column<int[]>(type: "notification.staff_channel[]", nullable: false, defaultValueSql: "'{in_app}'", comment: "Canali su cui l'utente vuole ricevere le notifiche operative."),
                    is_muted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "\"Non disturbare\" globale: nessuna notifica operativa finché attivo (rispettato dal modulo, il legacy lo ignorava)."),
                    muted_kinds = table.Column<int[]>(type: "notification.notification_kind[]", nullable: false, defaultValueSql: "'{}'", comment: "Tipi di notifica silenziati singolarmente, senza spegnere tutto."),
                    quiet_hours_start = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Inizio della finestra oraria in cui non recapitare push/WhatsApp, interpretata nel fuso della property."),
                    quiet_hours_end = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Fine della finestra oraria di non recapito push/WhatsApp."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione delle preferenze."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete; di fatto non usato — si torna ai default cancellando la riga.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_user_channel_preferences", x => x.id);
                },
                comment: "Preferenze di notifica di un utente C5 (staff/gestore): canali abilitati, non disturbare, tipi silenziati.");

            migrationBuilder.CreateTable(
                name: "inapp_message_reads",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    message_id = table.Column<long>(type: "bigint", nullable: false, comment: "Notifica a cui lo stato di lettura si riferisce."),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema. Operatore che ha letto/scartato."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. Denormalizzato dal messaggio per purge e scoping multi-tenant."),
                    read_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Istante in cui l'utente ha aperto/visto la notifica."),
                    dismissed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante in cui l'utente ha rimosso la notifica dal proprio elenco, senza cancellarla per gli altri."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità: le righe sono normalmente rimosse in cascata col messaggio, non soft-deleted a mano.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_inapp_message_reads", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_inapp_message_reads_message",
                        column: x => x.message_id,
                        principalSchema: "notification",
                        principalTable: "inapp_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Stato di lettura per (messaggio × utente): l'assenza di riga significa non letta.");

            migrationBuilder.CreateTable(
                name: "marketing_campaign_recipients",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    parent_kind = table.Column<int>(type: "notification.marketing_parent_kind", nullable: false, comment: "Se il destinatario appartiene a una campagna o a un'automazione; tipizza parent_id."),
                    parent_id = table.Column<long>(type: "bigint", nullable: false, comment: "Invio (campagna o automazione) a cui la riga appartiene — integrità referenziale condizionale applicata da trigger DB, nessuna FK EF (vedi nota sopra)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, denormalizzato dal parent — nessuna FK cross-schema."),
                    contact_id = table.Column<long>(type: "bigint", nullable: true, comment: "Contatto marketing corrispondente, se risolvibile (destinatario da lista email grezza altrimenti NULL)."),
                    email_snapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false, comment: "Indirizzo email destinatario, congelato al momento dell'invio."),
                    full_name_snapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Nome del destinatario all'invio, per personalizzazione e audit."),
                    status = table.Column<int>(type: "notification.recipient_status", nullable: false, defaultValueSql: "'pending'", comment: "Esito dell'invio a questo destinatario, allineato allo status della Delivery collegata."),
                    delivery_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id della notification_deliveries che ha eseguito l'invio a questo destinatario — copiato, nessuna FK (NULL se status = skipped)."),
                    skip_reason = table.Column<int>(type: "notification.recipient_skip_reason", nullable: true, comment: "Perché il destinatario è stato saltato (consenso mancante, soppresso, indirizzo vuoto/non valido, duplicato)."),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando il messaggio è stato spedito a questo destinatario; decorrenza della retention a 6 mesi dello snapshot."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità; la retention a 6 mesi esegue una purge fisica (hard-delete), non un soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_marketing_campaign_recipients", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_marketing_campaign_recipients_contact",
                        column: x => x.contact_id,
                        principalSchema: "notification",
                        principalTable: "marketing_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Destinatario di un invio marketing (campagna o automazione): snapshot al momento dell'invio, con esito per indirizzo.");

            migrationBuilder.CreateTable(
                name: "marketing_contact_list_members",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    list_id = table.Column<long>(type: "bigint", nullable: false, comment: "Lista di appartenenza."),
                    contact_id = table.Column<long>(type: "bigint", nullable: false, comment: "Contatto membro della lista."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, denormalizzato dalla lista — nessuna FK cross-schema."),
                    added_by = table.Column<int>(type: "notification.list_member_source", nullable: false, defaultValueSql: "'manual'", comment: "Come il contatto è entrato nella lista (manuale, auto-iscrizione da prenotazione, import, duplicazione)."),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Quando il contatto è stato aggiunto alla lista."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data di rimozione del contatto dalla lista (soft-delete, storicizzata).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_marketing_contact_list_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_marketing_contact_list_members_contact",
                        column: x => x.contact_id,
                        principalSchema: "notification",
                        principalTable: "marketing_contacts",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_notification_marketing_contact_list_members_list",
                        column: x => x.list_id,
                        principalSchema: "notification",
                        principalTable: "marketing_contact_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Tabella ponte: appartenenza di un contatto marketing a una lista, con storico add/remove.");

            migrationBuilder.CreateTable(
                name: "marketing_events",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile (API/URL), copiato anche nella proiezione verso la vetrina C2."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema."),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false, comment: "Nome dell'evento."),
                    description = table.Column<string>(type: "text", nullable: true, comment: "Testo descrittivo."),
                    starts_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Primo giorno dell'evento (della prima occorrenza se ricorrente)."),
                    ends_on = table.Column<DateOnly>(type: "date", nullable: false, comment: "Ultimo giorno dell'evento."),
                    start_time = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Orario di inizio; ignorato se is_full_day."),
                    end_time = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Orario di fine; ignorato se is_full_day."),
                    is_full_day = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Evento \"tutto il giorno\": se vero, start_time/end_time sono ignorati."),
                    repeat_type = table.Column<int>(type: "notification.event_repeat_type", nullable: false, defaultValueSql: "'never'", comment: "Regola di ricorrenza dell'evento."),
                    repeat_interval = table.Column<short>(type: "smallint", nullable: true, defaultValue: (short)1, comment: "Ogni quante unità ripetere (solo repeat_type = custom)."),
                    repeat_unit = table.Column<int>(type: "notification.event_repeat_unit", nullable: true, comment: "Unità di ripetizione per repeat_type = custom (giorno/settimana/mese/anno)."),
                    repeat_until = table.Column<DateOnly>(type: "date", nullable: true, comment: "Data limite oltre cui non generare occorrenze; NULL = nessun limite / evento singolo."),
                    cover_image_asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Immagine di copertina dell'evento."),
                    contact_list_id = table.Column<long>(type: "bigint", nullable: true, comment: "Lista destinataria dei reminder dell'evento."),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se l'evento è mostrato sulla pagina pubblica dello stabilimento (filtra davvero la proiezione verso C2)."),
                    reminders_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se inviare i reminder pianificati dell'evento."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Evento visibile in gestionale/vetrina."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione dell'evento."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data di soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_marketing_events", x => x.id);
                    table.CheckConstraint("ck_notification_marketing_events_ends_on", "ends_on >= starts_on");
                    table.CheckConstraint("ck_notification_marketing_events_repeat_interval", "repeat_interval IS NULL OR repeat_interval >= 1");
                    table.ForeignKey(
                        name: "fk_notification_marketing_events_contact_list",
                        column: x => x.contact_list_id,
                        principalSchema: "notification",
                        principalTable: "marketing_contact_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notification_marketing_events_cover_image_asset",
                        column: x => x.cover_image_asset_id,
                        principalSchema: "notification",
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Evento/iniziativa di uno stabilimento (serata, torneo, offerta a tema), promuovibile in vetrina e con reminder.");

            migrationBuilder.CreateTable(
                name: "deliveries",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile usato nel gestionale e nell'export GDPR."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema. NULL solo per invii di piattaforma non legati a uno stabilimento."),
                    channel = table.Column<int>(type: "notification.channel", nullable: false, defaultValueSql: "'email'", comment: "Mezzo di consegna (email/whatsapp/sms/push/in_app); un invio è su un solo canale."),
                    kind = table.Column<int>(type: "notification.delivery_kind", nullable: false, defaultValueSql: "'other'", comment: "Scopo funzionale del messaggio: determina template, priorità, retention e regole anti-spam."),
                    subject_type = table.Column<int>(type: "notification.subject_type", nullable: false, defaultValueSql: "'none'", comment: "Tipo dell'entità che ha originato l'invio."),
                    subject_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "Id pubblico dell'entità di origine, copiato per valore — nessuna FK cross-modulo."),
                    recipient_type = table.Column<int>(type: "notification.recipient_type", nullable: false, defaultValueSql: "'customer'", comment: "Categoria del destinatario (cliente, utente gestionale, contatto stabilimento, partner, admin, indirizzo libero)."),
                    recipient_customer_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_customer.customers, copiato — nessuna FK cross-schema. Usato per export/cancellazione GDPR."),
                    recipient_user_public_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema."),
                    recipient_address_snapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true, comment: "Indirizzo di consegna congelato all'invio: email, numero E.164 o token push abbreviato."),
                    recipient_name_snapshot = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Nome del destinatario al momento dell'invio, per audit e reinvio."),
                    from_address_snapshot = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true, comment: "Mittente effettivo usato (canale email della property o noreply@summerbooking.it)."),
                    template_id = table.Column<long>(type: "bigint", nullable: true, comment: "Template di catalogo usato per il rendering, quando presente."),
                    subject_snapshot = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Oggetto renderizzato effettivamente spedito (email/push title)."),
                    rendered_body = table.Column<string>(type: "text", nullable: true, comment: "Corpo renderizzato consegnato, tenuto per audit/ricerca; troncato a NULL dopo 90 giorni dal job TruncateEmailLogBodies."),
                    has_attachments = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "True se il messaggio consegnato aveva allegati; gli allegati appartengono all'entità di origine, non a questa riga."),
                    status = table.Column<int>(type: "notification.delivery_status", nullable: false, defaultValueSql: "'queued'", comment: "Stato del ciclo di consegna: queued → sending → sent → delivered | failed | bounced | skipped."),
                    attempt_count = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Numero di tentativi di invio già eseguiti."),
                    max_attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)5, comment: "Numero massimo di tentativi previsti per questo kind+channel."),
                    next_attempt_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, defaultValueSql: "now()", comment: "Istante del prossimo tentativo del worker, calcolato con backoff esponenziale + jitter; NULL quando lo stato è terminale."),
                    provider = table.Column<int>(type: "notification.delivery_provider", nullable: false, defaultValueSql: "'smtp'", comment: "Sistema esterno usato per la consegna."),
                    provider_message_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Id restituito dal provider (SES MessageId, Twilio SID, FCM message name), per correlare i webhook di stato/bounce."),
                    idempotency_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Chiave calcolata da (subject_type, subject_public_id, kind, channel, recipient) per evitare invii duplicati."),
                    failure_reason = table.Column<string>(type: "text", nullable: true, comment: "Dettaglio testuale dell'errore terminale (failed/bounced)."),
                    skip_reason = table.Column<int>(type: "notification.skip_reason", nullable: true, comment: "Motivo dello stato skipped (opt-out, soppressione, indirizzo mancante, ambiente non-prod, consenso non verificato)."),
                    queued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Istante di creazione della riga in coda (transazione del comando di origine)."),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di accettazione da parte del provider; decorrenza della retention del corpo (90 giorni)."),
                    delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di conferma di recapito, quando il provider la fornisce."),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di transizione allo stato terminale failed/bounced."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data dell'ultima transizione di stato."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Esposto per uniformità: tabella di registro, le righe non si cancellano a mano — le rimuove la retention (12 mesi).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_deliveries", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_deliveries_template",
                        column: x => x.template_id,
                        principalSchema: "notification",
                        principalTable: "templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Outbox + audit unico per destinatario: una riga = un messaggio verso un singolo destinatario su un singolo canale.");

            migrationBuilder.CreateTable(
                name: "marketing_automations",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile (API/URL)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema."),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Nome interno dell'automazione."),
                    automation_kind = table.Column<int>(type: "notification.automation_kind", nullable: false, defaultValueSql: "'custom'", comment: "Comportamento dell'automazione: welcome, goodbye o custom."),
                    state = table.Column<int>(type: "notification.automation_state", nullable: false, defaultValueSql: "'draft'", comment: "Stato dell'automazione ricorrente."),
                    schedule_type = table.Column<int>(type: "notification.automation_schedule_type", nullable: false, defaultValueSql: "'before'", comment: "Strategia di pianificazione: prima dell'arrivo, dopo la partenza, o trigger custom."),
                    days_offset = table.Column<short>(type: "smallint", nullable: true, defaultValue: (short)1, comment: "Giorni prima dell'arrivo (before) o dopo la partenza (after); NULL per schedule_type custom."),
                    send_at_time = table.Column<TimeOnly>(type: "time", nullable: true, defaultValue: new TimeOnly(9, 0, 0), comment: "Ora locale di invio nel giorno calcolato, interpretata nel fuso di Timezone."),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Europe/Rome", comment: "Fuso IANA per il calcolo di send_at_time."),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Oggetto dell'email (e titolo della push, se also_send_push)."),
                    template_id = table.Column<long>(type: "bigint", nullable: false, comment: "Template del corpo (template_kind = automation)."),
                    contact_list_id = table.Column<long>(type: "bigint", nullable: true, comment: "Segmento destinatario, risolto all'esecuzione."),
                    also_send_push = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se inviare anche una push FCM ai destinatari utenti C5 con device token."),
                    last_run_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Ultimo tick in cui l'automazione ha prodotto invii; guida la retention."),
                    total_sent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Totale storico messaggi spediti da questa automazione."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione dell'automazione."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data di soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_marketing_automations", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_marketing_automations_contact_list",
                        column: x => x.contact_list_id,
                        principalSchema: "notification",
                        principalTable: "marketing_contact_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notification_marketing_automations_template",
                        column: x => x.template_id,
                        principalSchema: "notification",
                        principalTable: "templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Automazione marketing ricorrente: email (+ push opzionale) inviata ai contatti di una lista in relazione alle loro prenotazioni.");

            migrationBuilder.CreateTable(
                name: "marketing_email_campaigns",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, usato nell'URL del gestionale."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, copiato — nessuna FK cross-schema."),
                    name = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: true, comment: "Nome interno della campagna."),
                    subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false, comment: "Oggetto dell'email (e titolo della push se also_send_push)."),
                    template_id = table.Column<long>(type: "bigint", nullable: false, comment: "Template del corpo (template_kind = campaign); un corpo ad-hoc genera un template inline dedicato."),
                    contact_list_id = table.Column<long>(type: "bigint", nullable: true, comment: "Segmento destinatario, risolto all'invio (non snapshot)."),
                    state = table.Column<int>(type: "notification.campaign_state", nullable: false, defaultValueSql: "'draft'", comment: "Stato del ciclo di vita della campagna."),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante di invio programmato, assoluto; NULL per bozza o invio immediato."),
                    timezone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "Europe/Rome", comment: "Fuso del gestore, solo per ricostruire il form (non usato per la schedulazione, che usa scheduled_at assoluto)."),
                    also_send_push = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se, dopo l'email, inviare anche una push FCM ai destinatari utenti C5 con device token."),
                    send_copy_to_manager = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se inviare una copia del messaggio al manager della property."),
                    recipients_total = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Numero totale di destinatari risolti per questo invio."),
                    delivered_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Numero di consegne riuscite, aggiornato per evento da notification_deliveries."),
                    failed_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0, comment: "Numero di consegne fallite, aggiornato per evento da notification_deliveries."),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access.users, copiato — nessuna FK cross-schema."),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando l'invio è stato completato; decorrenza retention definizione e snapshot destinatari."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione della campagna."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data di soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_marketing_email_campaigns", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_marketing_email_campaigns_contact_list",
                        column: x => x.contact_list_id,
                        principalSchema: "notification",
                        principalTable: "marketing_contact_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_notification_marketing_email_campaigns_template",
                        column: x => x.template_id,
                        principalSchema: "notification",
                        principalTable: "templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Campagna email one-shot verso un segmento: oggetto + corpo + allegati, inviata subito o pianificata.");

            migrationBuilder.CreateTable(
                name: "template_attachments",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    template_id = table.Column<long>(type: "bigint", nullable: false, comment: "Template a cui l'allegato appartiene."),
                    media_asset_id = table.Column<long>(type: "bigint", nullable: false, comment: "File allegato."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordine di presentazione/allegato all'interno del template."),
                    filename_snapshot = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Nome file per l'header MIME Content-Disposition, congelato all'associazione; fallback sul filename dell'asset."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di associazione dell'allegato al template."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica (es. riordino)."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete: rimozione dell'allegato dal template.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_template_attachments", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_template_attachments_media_asset",
                        column: x => x.media_asset_id,
                        principalSchema: "notification",
                        principalTable: "media_assets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_notification_template_attachments_template",
                        column: x => x.template_id,
                        principalSchema: "notification",
                        principalTable: "templates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Giunzione ordinata: il template T allega il file F in posizione N, spedito con ogni messaggio renderizzato da quel template.");

            migrationBuilder.CreateTable(
                name: "marketing_event_reminders",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    event_id = table.Column<long>(type: "bigint", nullable: false, comment: "Evento a cui il reminder appartiene."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di catalog.properties, denormalizzato dall'evento — nessuna FK cross-schema."),
                    offset = table.Column<int>(type: "notification.event_reminder_offset", nullable: false, defaultValueSql: "'one_day_before'", comment: "Anticipo rispetto all'evento (1 giorno, 3 giorni, 1 settimana prima, o custom)."),
                    custom_date = table.Column<DateOnly>(type: "date", nullable: true, comment: "Data esplicita per offset = custom."),
                    custom_time = table.Column<TimeOnly>(type: "time", nullable: true, comment: "Ora esplicita per offset = custom."),
                    scheduled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Istante assoluto di invio, calcolato da offset (o custom_*) rispetto a starts_on dell'evento e al fuso della property; ricalcolato se l'evento cambia data."),
                    status = table.Column<int>(type: "notification.reminder_status", nullable: false, defaultValueSql: "'pending'", comment: "Stato di invio del reminder."),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando il reminder è stato effettivamente inviato."),
                    failure_reason = table.Column<string>(type: "text", nullable: true, comment: "Motivo del fallimento dell'invio, se status = failed."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione del reminder."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data di soft-delete.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_marketing_event_reminders", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_marketing_event_reminders_event",
                        column: x => x.event_id,
                        principalSchema: "notification",
                        principalTable: "marketing_events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Reminder pianificato di un evento marketing, inviato ai contatti della lista dell'evento allo scadere.");

            migrationBuilder.CreateTable(
                name: "marketing_automation_triggers",
                schema: "notification",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    automation_id = table.Column<long>(type: "bigint", nullable: false, comment: "Automazione a cui il trigger appartiene."),
                    trigger_kind = table.Column<int>(type: "notification.automation_trigger_kind", nullable: false, comment: "Evento della prenotazione che fa scattare l'invio (del giorno, prima/dopo l'arrivo/partenza, saldo pendente)."),
                    days_offset = table.Column<short>(type: "smallint", nullable: true, comment: "Giorni di anticipo/ritardo per before_booking / after_booking; NULL per of_booking e pending_payments."),
                    send_at_time = table.Column<TimeOnly>(type: "time", nullable: true, defaultValue: new TimeOnly(9, 0, 0), comment: "Ora locale di invio, interpretata nel fuso dell'automazione padre."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di creazione del trigger."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data di ultima modifica."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data di rimozione del trigger (soft-delete).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notification_marketing_automation_triggers", x => x.id);
                    table.ForeignKey(
                        name: "fk_notification_marketing_automation_triggers_automation",
                        column: x => x.automation_id,
                        principalSchema: "notification",
                        principalTable: "marketing_automations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Trigger custom di un'automazione marketing: evento prenotazione che fa scattare l'invio.");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_campaign",
                schema: "notification",
                table: "deliveries",
                columns: new[] { "subject_type", "subject_public_id", "status" },
                filter: "subject_type IN ('marketing_campaign','marketing_automation')");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_dedup",
                schema: "notification",
                table: "deliveries",
                column: "idempotency_key",
                unique: true,
                filter: "idempotency_key IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_pending",
                schema: "notification",
                table: "deliveries",
                column: "next_attempt_at",
                filter: "status IN ('queued','sending')");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_property_created",
                schema: "notification",
                table: "deliveries",
                columns: new[] { "property_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_recipient_customer",
                schema: "notification",
                table: "deliveries",
                columns: new[] { "recipient_customer_public_id", "created_at" },
                filter: "recipient_customer_public_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_subject",
                schema: "notification",
                table: "deliveries",
                columns: new[] { "subject_type", "subject_public_id" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_deliveries_template_id",
                schema: "notification",
                table: "deliveries",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_deliveries_public_id",
                schema: "notification",
                table: "deliveries",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_notification_email_channel_settings_property",
                schema: "notification",
                table: "email_channel_settings",
                column: "property_id",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_establishment_claim_requests_bathhouse",
                schema: "notification",
                table: "establishment_claim_requests",
                column: "bathhouse_public_id",
                filter: "bathhouse_public_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_establishment_claim_requests_status_created",
                schema: "notification",
                table: "establishment_claim_requests",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "uq_notification_establishment_claim_requests_public_id",
                schema: "notification",
                table: "establishment_claim_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_inapp_message_reads_user_recent",
                schema: "notification",
                table: "inapp_message_reads",
                columns: new[] { "user_id", "read_at" });

            migrationBuilder.CreateIndex(
                name: "uq_notification_inapp_message_reads_message_user",
                schema: "notification",
                table: "inapp_message_reads",
                columns: new[] { "message_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_inapp_messages_feed",
                schema: "notification",
                table: "inapp_messages",
                columns: new[] { "property_id", "created_at" },
                filter: "visibility = 'visible' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_inapp_messages_subject",
                schema: "notification",
                table: "inapp_messages",
                columns: new[] { "subject_kind", "subject_public_id" });

            migrationBuilder.CreateIndex(
                name: "uq_notification_inapp_messages_public_id",
                schema: "notification",
                table: "inapp_messages",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_automation_triggers_kind",
                schema: "notification",
                table: "marketing_automation_triggers",
                column: "trigger_kind",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_automation_triggers_automation_kind",
                schema: "notification",
                table: "marketing_automation_triggers",
                columns: new[] { "automation_id", "trigger_kind" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_automations_active",
                schema: "notification",
                table: "marketing_automations",
                columns: new[] { "property_id", "schedule_type" },
                filter: "state = 'active' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_automations_contact_list_id",
                schema: "notification",
                table: "marketing_automations",
                column: "contact_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_automations_template_id",
                schema: "notification",
                table: "marketing_automations",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_automations_public_id",
                schema: "notification",
                table: "marketing_automations",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_campaign_recipients_contact",
                schema: "notification",
                table: "marketing_campaign_recipients",
                column: "contact_id",
                filter: "contact_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_campaign_recipients_parent",
                schema: "notification",
                table: "marketing_campaign_recipients",
                columns: new[] { "parent_kind", "parent_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_campaign_recipients_dedup",
                schema: "notification",
                table: "marketing_campaign_recipients",
                columns: new[] { "parent_kind", "parent_id", "email_snapshot" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_contact_list_members_contact",
                schema: "notification",
                table: "marketing_contact_list_members",
                column: "contact_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_contact_list_members_list",
                schema: "notification",
                table: "marketing_contact_list_members",
                column: "list_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_contact_list_members_list_contact",
                schema: "notification",
                table: "marketing_contact_list_members",
                columns: new[] { "list_id", "contact_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_contact_lists_default",
                schema: "notification",
                table: "marketing_contact_lists",
                column: "property_id",
                unique: true,
                filter: "is_default AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_contact_lists_public_id",
                schema: "notification",
                table: "marketing_contact_lists",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_contacts_consent",
                schema: "notification",
                table: "marketing_contacts",
                columns: new[] { "property_id", "consent_status" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_contacts_customer",
                schema: "notification",
                table: "marketing_contacts",
                column: "customer_public_id",
                filter: "customer_public_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_contacts_property_email",
                schema: "notification",
                table: "marketing_contacts",
                columns: new[] { "property_id", "email" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_contacts_public_id",
                schema: "notification",
                table: "marketing_contacts",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_contacts_unsubscribe_token",
                schema: "notification",
                table: "marketing_contacts",
                column: "unsubscribe_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_email_campaigns_contact_list_id",
                schema: "notification",
                table: "marketing_email_campaigns",
                column: "contact_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_email_campaigns_due",
                schema: "notification",
                table: "marketing_email_campaigns",
                column: "scheduled_at",
                filter: "state = 'scheduled' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_email_campaigns_property_state",
                schema: "notification",
                table: "marketing_email_campaigns",
                columns: new[] { "property_id", "state", "created_at" });

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_email_campaigns_template_id",
                schema: "notification",
                table: "marketing_email_campaigns",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_email_campaigns_public_id",
                schema: "notification",
                table: "marketing_email_campaigns",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_event_reminders_due",
                schema: "notification",
                table: "marketing_event_reminders",
                column: "scheduled_at",
                filter: "status = 'pending' AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_event_reminders_event",
                schema: "notification",
                table: "marketing_event_reminders",
                column: "event_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_events_contact_list_id",
                schema: "notification",
                table: "marketing_events",
                column: "contact_list_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_events_cover_image_asset_id",
                schema: "notification",
                table: "marketing_events",
                column: "cover_image_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_events_public",
                schema: "notification",
                table: "marketing_events",
                columns: new[] { "property_id", "starts_on" },
                filter: "is_public AND is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_events_public_id",
                schema: "notification",
                table: "marketing_events",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_newsletter_subscriptions_status_created",
                schema: "notification",
                table: "marketing_newsletter_subscriptions",
                columns: new[] { "status", "created_at" });

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_newsletter_subscriptions_confirmation_token",
                schema: "notification",
                table: "marketing_newsletter_subscriptions",
                column: "confirmation_token",
                unique: true,
                filter: "confirmation_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_newsletter_subscriptions_email",
                schema: "notification",
                table: "marketing_newsletter_subscriptions",
                column: "email",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_newsletter_subscriptions_public_id",
                schema: "notification",
                table: "marketing_newsletter_subscriptions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_newsletter_subscriptions_unsubscribe_token",
                schema: "notification",
                table: "marketing_newsletter_subscriptions",
                column: "unsubscribe_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_marketing_suppressions_address",
                schema: "notification",
                table: "marketing_suppressions",
                column: "address");

            migrationBuilder.CreateIndex(
                name: "uq_notification_marketing_suppressions_scope_address_channel",
                schema: "notification",
                table: "marketing_suppressions",
                columns: new[] { "property_id", "address", "channel" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_media_assets_checksum",
                schema: "notification",
                table: "media_assets",
                column: "checksum",
                filter: "checksum IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_media_assets_origin",
                schema: "notification",
                table: "media_assets",
                column: "origin");

            migrationBuilder.CreateIndex(
                name: "ix_notification_media_assets_property_kind",
                schema: "notification",
                table: "media_assets",
                columns: new[] { "property_id", "kind" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_notification_push_device_tokens_stale",
                schema: "notification",
                table: "push_device_tokens",
                column: "last_seen_at",
                filter: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_notification_push_device_tokens_user_active",
                schema: "notification",
                table: "push_device_tokens",
                column: "user_id",
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_push_device_tokens_token",
                schema: "notification",
                table: "push_device_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_notification_template_attachments_media_asset_id",
                schema: "notification",
                table: "template_attachments",
                column: "media_asset_id");

            migrationBuilder.CreateIndex(
                name: "ix_notification_template_attachments_template_order",
                schema: "notification",
                table: "template_attachments",
                columns: new[] { "template_id", "sort_order" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_template_attachments_template_asset",
                schema: "notification",
                table: "template_attachments",
                columns: new[] { "template_id", "media_asset_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_templates_default",
                schema: "notification",
                table: "templates",
                columns: new[] { "property_id", "template_kind", "channel", "culture" },
                unique: true,
                filter: "is_default AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_notification_templates_public_id",
                schema: "notification",
                table: "templates",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_notification_user_channel_preferences_user",
                schema: "notification",
                table: "user_channel_preferences",
                column: "user_id",
                unique: true,
                filter: "deleted_at IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "deliveries",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "email_channel_settings",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "establishment_claim_requests",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "inapp_message_reads",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "marketing_automation_triggers",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "marketing_campaign_recipients",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "marketing_contact_list_members",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "marketing_email_campaigns",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "marketing_event_reminders",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "marketing_newsletter_subscriptions",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "marketing_suppressions",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "push_device_tokens",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "template_attachments",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "user_channel_preferences",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "inapp_messages",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "marketing_automations",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "marketing_contacts",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "marketing_events",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "templates",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "marketing_contact_lists",
                schema: "notification");

            migrationBuilder.DropTable(
                name: "media_assets",
                schema: "notification");
        }
    }
}
