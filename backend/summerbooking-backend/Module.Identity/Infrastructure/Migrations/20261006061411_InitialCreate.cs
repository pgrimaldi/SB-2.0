using System;
using System.Net;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Module.Identity.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:identity.auth_challenge_channel", "email,sms")
                .Annotation("Npgsql:Enum:identity.auth_challenge_invalidation", "superseded,too_many_attempts,manual,account_disabled")
                .Annotation("Npgsql:Enum:identity.auth_challenge_purpose", "second_factor_login,email_verification,password_reset,email_change_confirmation,invitation")
                .Annotation("Npgsql:Enum:identity.contract_acceptance_event", "accepted,revoked")
                .Annotation("Npgsql:Enum:identity.contract_document_status", "draft,published,retired")
                .Annotation("Npgsql:Enum:identity.customer_source", "contact_form,booking,service_reservation,card_transaction,import")
                .Annotation("Npgsql:Enum:identity.login_method", "normal,google,facebook,apple")
                .Annotation("Npgsql:Enum:identity.notification_sound_scope", "management_booking,online_booking,management_service_booking,online_service_booking")
                .Annotation("Npgsql:Enum:identity.refresh_revoke_reason", "logout,password_changed,account_disabled,manual,reuse_detected,superseded")
                .Annotation("Npgsql:Enum:identity.second_factor_channel", "email,sms,totp")
                .Annotation("Npgsql:Enum:identity.storage_origin", "s3_us_east_2,s3_us_east_1,cloudfront,external")
                .Annotation("Npgsql:Enum:identity.subscription_period_close_reason", "renewed,expired,cancelled,superseded")
                .Annotation("Npgsql:Enum:identity.subscription_period_kind", "initial,renewal,migration,manual_adjustment")
                .Annotation("Npgsql:Enum:identity.subscription_status", "active,suspended,expired,cancelled")
                .Annotation("Npgsql:Enum:identity.subscription_term_type", "fixed_term,legacy_day_bank")
                .Annotation("Npgsql:Enum:identity.ui_language", "it,en")
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "access_property_security_settings",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id della property (catalog) a cui si applicano le impostazioni, copiato — nessuna FK cross-schema."),
                    is_two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se true, richiede un secondo fattore al login dello staff di questo stabilimento."),
                    two_factor_channel = table.Column<int>(type: "identity.second_factor_channel", nullable: false, defaultValueSql: "'email'", comment: "Canale del secondo fattore."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_access_property_security_settings", x => x.id);
                },
                comment: "Impostazioni di sicurezza applicate a tutto lo staff di uno stabilimento (property).");

            migrationBuilder.CreateTable(
                name: "access_roles",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "Codice tecnico stabile del ruolo, usato in policy/claim del token."),
                    is_assignable_via_panel = table.Column<bool>(type: "boolean", nullable: false, comment: "Se true, un admin/superadmin può assegnare questo ruolo dal pannello gestione utenti."),
                    is_staff = table.Column<bool>(type: "boolean", nullable: false, comment: "Se true, il ruolo è operativo di stabilimento e richiede property_id sull'account."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, comment: "Ordinamento di presentazione nei menu a tendina."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_access_roles", x => x.id);
                },
                comment: "Catalogo dei ruoli applicativi (lookup gestibile da pannello, senza deploy).");

            migrationBuilder.CreateTable(
                name: "account_contract_documents",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, referenziato da catalog (property.contract_document_public_id)."),
                    version_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "Etichetta di versione dell'addendum, mostrata all'utente."),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Titolo del documento per elenchi/UI."),
                    document_origin = table.Column<int>(type: "identity.storage_origin", nullable: false, comment: "Provenienza del PDF (storage interno o provider esterno)."),
                    document_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false, comment: "Percorso relativo del PDF, o URL intero se origin = external."),
                    content_sha256 = table.Column<string>(type: "char(64)", nullable: true, comment: "Hash SHA-256 del PDF al momento della pubblicazione."),
                    status = table.Column<int>(type: "identity.contract_document_status", nullable: false, defaultValueSql: "'draft'", comment: "Stato del ciclo di vita del documento."),
                    supersedes_id = table.Column<long>(type: "bigint", nullable: true, comment: "Versione precedente che questa sostituisce."),
                    notes = table.Column<string>(type: "text", nullable: true, comment: "Note libere sul documento."),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando il documento è passato a published."),
                    superseded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando questa versione è stata superata da una successiva; NULL = versione corrente. Sostituisce il flag is_current, che era derivabile da status + published_at + supersedes_id."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_account_contract_documents", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_account_contract_documents_supersedes",
                        column: x => x.supersedes_id,
                        principalSchema: "identity",
                        principalTable: "account_contract_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Documenti contrattuali (addendum) pubblicati dalla piattaforma, con versionamento immutabile.");

            migrationBuilder.CreateTable(
                name: "account_license_plans",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "Codice tecnico del piano (basic, advanced), non tradotto."),
                    duration_months = table.Column<short>(type: "smallint", nullable: false, comment: "Durata suggerita del piano in mesi; default di comodo per precompilare le date del periodo, mai fonte di verità di ends_at."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Se true, il piano è proponibile per nuove sottoscrizioni."),
                    sort_order = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Ordinamento di presentazione."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_account_license_plans", x => x.id);
                },
                comment: "Catalogo dei piani di abbonamento della piattaforma (lookup gestibile da pannello SuperAdmin).");

            migrationBuilder.CreateTable(
                name: "access_users",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Identificativo pubblico stabile (UUIDv7), usato in URL/API e come riferimento cross-modulo."),
                    username = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Username alternativo per il login da cassa (solo staff); NULL per i consumer."),
                    email = table.Column<string>(type: "citext", maxLength: 255, nullable: false, comment: "Email di contatto e identificatore primario di login."),
                    password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Hash della password; NULL per gli account creati solo via social login."),
                    password_changed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Ultima modifica della password; usata per invalidare le sessioni al cambio."),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Nome dell'account."),
                    last_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true, comment: "Cognome dell'account."),
                    phone_country_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true, defaultValue: "+39", comment: "Prefisso telefonico internazionale (E.164) del numero di cellulare."),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Numero di cellulare, senza prefisso."),
                    profile_picture_origin = table.Column<int>(type: "identity.storage_origin", nullable: true, comment: "Provenienza della foto profilo (storage interno o provider social esterno)."),
                    profile_picture_path = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true, comment: "Percorso relativo della foto profilo, o URL intero se origin = external."),
                    login_method = table.Column<int>(type: "identity.login_method", nullable: false, defaultValueSql: "'normal'", comment: "Metodo di autenticazione usato l'ultima volta."),
                    google_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Sub dell'account Google collegato (login social)."),
                    facebook_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true, comment: "Id dell'account Facebook collegato."),
                    apple_subject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Sub Apple dell'account collegato (solo il claim, mai l'intero JWT). L'email puo' mancare su questo percorso: Apple la comunica una volta sola e l'utente puo' nasconderla - vedi le note su Account.AppleSubject."),
                    language = table.Column<int>(type: "identity.ui_language", nullable: false, defaultValueSql: "'it'", comment: "Lingua preferita per email transazionali e UI."),
                    role_id = table.Column<long>(type: "bigint", nullable: false, comment: "Ruolo applicativo dell'account (determina tipo account e permessi)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id della property (catalog) di appartenenza dello staff, copiato — nessuna FK cross-schema. NULL per i consumer."),
                    is_resource_scope_restricted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se true, l'account opera solo sulle risorse elencate in access_user_resource_scopes."),
                    news_section_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Mostra/nasconde la sezione novità nel pannello personale."),
                    enabled_notification_sounds = table.Column<int[]>(type: "identity.notification_sound_scope[]", nullable: false, defaultValueSql: "'{management_booking,online_booking,management_service_booking,online_service_booking}'", comment: "Ambiti per cui l'utente vuole l'allerta sonora. Sostituisce quattro colonne booleane parallele sullo stesso argomento; il default riproduce il comportamento precedente (tutte e quattro attive). Array vuoto = nessun suono."),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data/ora dell'ultimo login realmente autenticato."),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true, comment: "Account che ha creato questo account (staff creato da admin/superadmin); NULL per i consumer auto-registrati."),
                    resource_scopes = table.Column<string[]>(type: "text[]", nullable: false, defaultValueSql: "'{}'::text[]", comment: "Codici delle postazioni su cui l'account puo' operare quando la restrizione e' attiva. Era la tabella figlia access_user_resource_scopes, collassata in array (docs/05 §13.1)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_access_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_access_users_created_by",
                        column: x => x.created_by_user_id,
                        principalSchema: "identity",
                        principalTable: "access_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_identity_access_users_role",
                        column: x => x.role_id,
                        principalSchema: "identity",
                        principalTable: "access_roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Account di login del sistema (titolare, staff, superadmin, cliente finale consumer).");

            migrationBuilder.CreateTable(
                name: "account_license_plan_translations",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    license_plan_id = table.Column<long>(type: "bigint", nullable: false, comment: "Piano a cui la traduzione appartiene."),
                    culture = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false, comment: "Lingua (o lingua+regione) della traduzione, tag BCP-47."),
                    label = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, comment: "Nome commerciale del piano in questa lingua."),
                    description = table.Column<string>(type: "text", nullable: true, comment: "Testo descrittivo/commerciale del piano in questa lingua."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_account_license_plan_translations", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_account_license_plan_translations_plan",
                        column: x => x.license_plan_id,
                        principalSchema: "identity",
                        principalTable: "account_license_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Etichette multilingua (nome commerciale + descrizione) dei piani di abbonamento.");

            migrationBuilder.CreateTable(
                name: "access_refresh_tokens",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false, comment: "Account proprietario della sessione."),
                    token_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Hash del refresh token (il token in chiaro esiste solo nel client)."),
                    family_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Identificativo della catena di rotazione; il riuso di un token consumato revoca l'intera famiglia."),
                    replaced_by_id = table.Column<long>(type: "bigint", nullable: true, comment: "Token che ha sostituito questo alla rotazione."),
                    issued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Emissione del token."),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Scadenza naturale del refresh token (issued_at + TTL da configurazione)."),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando il token è stato usato per ottenere un nuovo access+refresh (rotazione)."),
                    revoked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando il token è stato revocato esplicitamente."),
                    revoked_reason = table.Column<int>(type: "identity.refresh_revoke_reason", nullable: true, comment: "Motivo della revoca (logout, cambio password, disabilitazione, riuso sospetto, rotazione)."),
                    client_user_agent = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true, comment: "User-agent del client all'emissione (audit sessioni attive)."),
                    client_ip = table.Column<IPAddress>(type: "inet", nullable: true, comment: "IP del client all'emissione; dato personale, retention breve."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_access_refresh_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_access_refresh_tokens_replaced_by",
                        column: x => x.replaced_by_id,
                        principalSchema: "identity",
                        principalTable: "access_refresh_tokens",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_identity_access_refresh_tokens_user",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "access_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Registro dei refresh token emessi agli account utente (rotazione, revoca, sessioni attive).");

            migrationBuilder.CreateTable(
                name: "access_user_auth_challenges",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false, comment: "Account a cui si riferisce la sfida."),
                    purpose = table.Column<int>(type: "identity.auth_challenge_purpose", nullable: false, comment: "Tipo di sfida (secondo fattore, verifica email, reset password, conferma cambio email, invito)."),
                    channel = table.Column<int>(type: "identity.auth_challenge_channel", nullable: false, defaultValueSql: "'email'", comment: "Canale su cui la sfida è stata recapitata."),
                    secret_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, comment: "Hash del segreto (OTP o token); mai salvato in chiaro."),
                    delivery_target = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Destinazione oscurata mostrabile all'utente (es. m***@dominio.it)."),
                    attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Numero di tentativi di verifica falliti su questa sfida."),
                    max_attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)5, comment: "Soglia di tentativi ammessi, oltre la quale la sfida è invalidata."),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Scadenza della sfida (created_at + TTL da configurazione per purpose)."),
                    consumed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando la sfida è stata verificata con successo (uso singolo, non riutilizzabile)."),
                    invalidated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando la sfida è stata invalidata senza essere consumata."),
                    invalidated_reason = table.Column<int>(type: "identity.auth_challenge_invalidation", nullable: true, comment: "Perché la sfida è stata invalidata."),
                    requested_ip = table.Column<IPAddress>(type: "inet", nullable: true, comment: "IP che ha richiesto l'emissione (audit anti-abuso); dato personale, retention brevissima."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_access_user_auth_challenges", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_access_user_auth_challenges_user",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "access_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Sfide di autenticazione a tempo (OTP 2FA, verifica email, reset password, inviti).");

            migrationBuilder.CreateTable(
                name: "account_contract_acceptances",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id della property (catalog) che ha eseguito l'azione, copiato — nessuna FK cross-schema."),
                    contract_document_id = table.Column<long>(type: "bigint", nullable: false, comment: "Documento/versione oggetto dell'evento."),
                    event_type = table.Column<int>(type: "identity.contract_acceptance_event", nullable: false, comment: "Tipo di evento (accettazione o revoca)."),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora dell'azione."),
                    actor_user_id = table.Column<long>(type: "bigint", nullable: false, comment: "Account (admin/superadmin) che ha eseguito l'azione."),
                    document_version_code = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false, comment: "version_code del documento copiato al momento dell'evento (snapshot leggibile)."),
                    document_sha256 = table.Column<string>(type: "char(64)", nullable: true, comment: "Hash SHA-256 del PDF accettato, come prova del contenuto esatto accettato."),
                    ip_address = table.Column<IPAddress>(type: "inet", nullable: true, comment: "IP del client al momento dell'azione (audit legale); dato personale a retention limitata."),
                    user_agent = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true, comment: "User-agent del client (rafforza il valore probatorio)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_account_contract_acceptances", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_account_contract_acceptances_actor",
                        column: x => x.actor_user_id,
                        principalSchema: "identity",
                        principalTable: "access_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_identity_account_contract_acceptances_document",
                        column: x => x.contract_document_id,
                        principalSchema: "identity",
                        principalTable: "account_contract_documents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Registro append-only delle accettazioni/revoche dei documenti contrattuali da parte degli stabilimenti.");

            migrationBuilder.CreateTable(
                name: "account_subscriptions",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id della property (catalog) titolare dell'abbonamento, copiato — nessuna FK cross-schema."),
                    owner_user_id = table.Column<long>(type: "bigint", nullable: true, comment: "Account superadmin titolare di riferimento del contratto."),
                    license_plan_id = table.Column<long>(type: "bigint", nullable: false, comment: "Piano corrente."),
                    term_type = table.Column<int>(type: "identity.subscription_term_type", nullable: false, defaultValueSql: "'fixed_term'", comment: "Tipo di abbonamento: a termine fisso con date contrattuali, o banca giorni legacy."),
                    status = table.Column<int>(type: "identity.subscription_status", nullable: false, defaultValueSql: "'active'", comment: "Stato dell'abbonamento."),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Inizio validità del periodo corrente."),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Fine validità del periodo corrente (contrattuale per fixed_term, proiettata per legacy_day_bank)."),
                    suspended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Timestamp dell'ultima transizione a suspended/expired."),
                    reactivated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Timestamp dell'ultima transizione a active da uno stato non attivo."),
                    remaining_days_balance = table.Column<int>(type: "integer", nullable: true, comment: "Banca giorni residua del vecchio modello a consumo; valorizzata solo per term_type = legacy_day_bank."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_account_subscriptions", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_account_subscriptions_owner",
                        column: x => x.owner_user_id,
                        principalSchema: "identity",
                        principalTable: "access_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_identity_account_subscriptions_plan",
                        column: x => x.license_plan_id,
                        principalSchema: "identity",
                        principalTable: "account_license_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Abbonamento corrente alla piattaforma per uno stabilimento (property).");

            migrationBuilder.CreateTable(
                name: "customer_bathhouse_likes",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false, comment: "Utente consumer che ha messo mi piace."),
                    bathhouse_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id del bathhouse (catalog) a cui è stato messo mi piace, copiato — nessuna FK cross-schema."),
                    liked_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Quando è stato messo il mi piace (aggiornato se il like viene riattivato)."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_customer_bathhouse_likes", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_customer_bathhouse_likes_user",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "access_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Registro dei \"mi piace\" delle strutture (bathhouse) da parte degli utenti consumer.");

            migrationBuilder.CreateTable(
                name: "customer_customers",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico stabile, riferimento cross-modulo (Booking/Payment/Fiskaly)."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id della property (catalog) a cui il contatto appartiene, copiato — nessuna FK cross-schema."),
                    user_id = table.Column<long>(type: "bigint", nullable: true, comment: "Account di login (ruolo consumer) collegato a questo contatto, se esiste."),
                    full_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true, comment: "Nome del cliente/contatto."),
                    phone_country_code = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true, defaultValue: "+39", comment: "Prefisso telefonico internazionale del telefono primario."),
                    phone = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Telefono primario, chiave di matching/deduplica per stabilimento."),
                    phone_secondary = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Recapito telefonico alternativo."),
                    email = table.Column<string>(type: "citext", maxLength: 255, nullable: true, comment: "Email di contatto, seconda chiave di matching."),
                    fiscal_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true, comment: "Codice fiscale, per clienti che richiedono fattura/scontrino intestato."),
                    vat_number = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true, comment: "Partita IVA per clienti business."),
                    address = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Indirizzo di residenza/fatturazione."),
                    city = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true, comment: "Città di residenza/fatturazione."),
                    postal_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true, comment: "CAP di residenza/fatturazione."),
                    date_of_birth = table.Column<DateOnly>(type: "date", nullable: true, comment: "Data di nascita."),
                    other_names = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Nominativi aggiuntivi/alias del contatto."),
                    discount_note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Nota testuale libera su sconti/condizioni particolari riservate al cliente."),
                    note = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Nota libera sul cliente."),
                    is_blacklisted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Flag di blacklist per questo stabilimento."),
                    blacklist_reason = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Motivo della blacklist."),
                    source_channel = table.Column<int>(type: "identity.customer_source", nullable: false, comment: "Come è nata la riga (form contatti, prenotazione, cassa, import)."),
                    created_by_user_id = table.Column<long>(type: "bigint", nullable: true, comment: "Operatore che ha creato/modificato il contatto dal form Contatti; NULL per le creazioni automatiche."),
                    last_activity_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data dell'ultima prenotazione/transazione che coinvolge questo cliente."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_customer_customers", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_customer_customers_created_by",
                        column: x => x.created_by_user_id,
                        principalSchema: "identity",
                        principalTable: "access_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_identity_customer_customers_user",
                        column: x => x.user_id,
                        principalSchema: "identity",
                        principalTable: "access_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Rubrica dei clienti finali per singolo stabilimento (contatti da prenotazioni/transazioni di cassa).");

            migrationBuilder.CreateTable(
                name: "account_subscription_periods",
                schema: "identity",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    subscription_id = table.Column<long>(type: "bigint", nullable: false, comment: "Abbonamento a cui il periodo appartiene."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id della property (catalog), denormalizzato per le query di reporting."),
                    license_plan_id = table.Column<long>(type: "bigint", nullable: false, comment: "Piano applicato in questo periodo."),
                    license_plan_code = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false, comment: "Code del piano copiato al momento dell'apertura del periodo (snapshot leggibile per i report)."),
                    term_type = table.Column<int>(type: "identity.subscription_term_type", nullable: false, defaultValueSql: "'fixed_term'", comment: "Tipo del periodo (a termine fisso o banca giorni legacy)."),
                    starts_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Inizio del periodo."),
                    ends_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Fine del periodo (indicativa per legacy_day_bank, contrattuale per fixed_term)."),
                    days_granted = table.Column<int>(type: "integer", nullable: true, comment: "Giorni concessi in questo periodo (solo legacy_day_bank)."),
                    amount = table.Column<decimal>(type: "numeric(12,2)", nullable: true, comment: "Importo fatturato per il periodo."),
                    currency = table.Column<string>(type: "char(3)", nullable: false, defaultValue: "EUR", comment: "Valuta dell'importo (ISO 4217)."),
                    period_kind = table.Column<int>(type: "identity.subscription_period_kind", nullable: false, comment: "Natura del periodo (iniziale, rinnovo, migrazione, rettifica manuale)."),
                    opened_by_user_id = table.Column<long>(type: "bigint", nullable: true, comment: "Account che ha stipulato o rinnovato; NULL per i periodi creati da migrazione/job."),
                    closed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando il periodo è stato chiuso; NULL = periodo corrente."),
                    close_reason = table.Column<int>(type: "identity.subscription_period_close_reason", nullable: true, comment: "Perché il periodo si è chiuso."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_account_subscription_periods", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_account_subscription_periods_opened_by",
                        column: x => x.opened_by_user_id,
                        principalSchema: "identity",
                        principalTable: "access_users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_identity_account_subscription_periods_plan",
                        column: x => x.license_plan_id,
                        principalSchema: "identity",
                        principalTable: "account_license_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_identity_account_subscription_periods_subscription",
                        column: x => x.subscription_id,
                        principalSchema: "identity",
                        principalTable: "account_subscriptions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Storico append-only dei periodi/rinnovi di abbonamento di uno stabilimento.");

            migrationBuilder.InsertData(
                schema: "identity",
                table: "access_roles",
                columns: new[] { "id", "code", "created_at", "deleted_at", "is_assignable_via_panel", "is_staff", "sort_order", "updated_at" },
                values: new object[,]
                {
                    { 1L, "systemadmin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, false, (short)0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, "superadmin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, true, (short)10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, "consumer", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, false, (short)0, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, "admin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, (short)20, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 5L, "lifeguard", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, (short)30, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 6L, "manager", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, (short)40, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 7L, "cashoperator", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, (short)50, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 8L, "websiteadmin", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, true, (short)60, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 9L, "user1", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, (short)70, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 10L, "waiter", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, (short)80, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 11L, "affiliate", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, (short)90, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 12L, "advancedlifeguard", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, (short)100, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 13L, "marketing", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, true, true, (short)110, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "account_license_plans",
                columns: new[] { "id", "code", "created_at", "deleted_at", "duration_months", "is_active", "updated_at" },
                values: new object[] { 1L, "basic", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, (short)3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "account_license_plans",
                columns: new[] { "id", "code", "created_at", "deleted_at", "duration_months", "is_active", "sort_order", "updated_at" },
                values: new object[] { 2L, "advanced", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, (short)12, true, (short)10, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                schema: "identity",
                table: "account_license_plan_translations",
                columns: new[] { "id", "created_at", "culture", "deleted_at", "description", "label", "license_plan_id", "updated_at" },
                values: new object[,]
                {
                    { 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "it", null, null, "Basic", 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "en", null, null, "Basic", 1L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 3L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "it", null, null, "Advanced", 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { 4L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "en", null, null, "Advanced", 2L, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "uq_identity_access_property_security_settings_property",
                schema: "identity",
                table: "access_property_security_settings",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "brin_identity_access_refresh_tokens_time",
                schema: "identity",
                table: "access_refresh_tokens",
                columns: new[] { "issued_at", "expires_at" })
                .Annotation("Npgsql:IndexMethod", "brin");

            migrationBuilder.CreateIndex(
                name: "ix_access_refresh_tokens_replaced_by_id",
                schema: "identity",
                table: "access_refresh_tokens",
                column: "replaced_by_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_refresh_tokens_expires_at",
                schema: "identity",
                table: "access_refresh_tokens",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_refresh_tokens_user_active",
                schema: "identity",
                table: "access_refresh_tokens",
                column: "user_id",
                filter: "consumed_at IS NULL AND revoked_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_identity_access_refresh_tokens_token_hash",
                schema: "identity",
                table: "access_refresh_tokens",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_identity_access_roles_code",
                schema: "identity",
                table: "access_roles",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_user_auth_challenges_expires_at",
                schema: "identity",
                table: "access_user_auth_challenges",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_user_auth_challenges_user_purpose_active",
                schema: "identity",
                table: "access_user_auth_challenges",
                columns: new[] { "user_id", "purpose" },
                filter: "consumed_at IS NULL AND invalidated_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_identity_access_user_auth_challenges_secret_hash",
                schema: "identity",
                table: "access_user_auth_challenges",
                column: "secret_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_access_users_created_by_user_id",
                schema: "identity",
                table: "access_users",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_users_apple_sub",
                schema: "identity",
                table: "access_users",
                column: "apple_subject",
                filter: "apple_subject IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_users_facebook_id",
                schema: "identity",
                table: "access_users",
                column: "facebook_id",
                filter: "facebook_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_users_google_id",
                schema: "identity",
                table: "access_users",
                column: "google_id",
                filter: "google_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_users_last_login",
                schema: "identity",
                table: "access_users",
                column: "last_login_at",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_users_property",
                schema: "identity",
                table: "access_users",
                column: "property_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_access_users_role",
                schema: "identity",
                table: "access_users",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "uq_identity_access_users_public_id",
                schema: "identity",
                table: "access_users",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_account_contract_acceptances_actor_user_id",
                schema: "identity",
                table: "account_contract_acceptances",
                column: "actor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_account_contract_acceptances_document",
                schema: "identity",
                table: "account_contract_acceptances",
                column: "contract_document_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_account_contract_acceptances_property_doc",
                schema: "identity",
                table: "account_contract_acceptances",
                columns: new[] { "property_id", "contract_document_id", "occurred_at" });

            migrationBuilder.CreateIndex(
                name: "ix_account_contract_documents_supersedes_id",
                schema: "identity",
                table: "account_contract_documents",
                column: "supersedes_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_account_contract_documents_current",
                schema: "identity",
                table: "account_contract_documents",
                column: "status",
                filter: "superseded_at IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_identity_account_contract_documents_public_id",
                schema: "identity",
                table: "account_contract_documents",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_identity_account_contract_documents_version",
                schema: "identity",
                table: "account_contract_documents",
                column: "version_code",
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_account_license_plan_translations_plan",
                schema: "identity",
                table: "account_license_plan_translations",
                column: "license_plan_id");

            migrationBuilder.CreateIndex(
                name: "uq_identity_account_license_plan_translations_plan_culture",
                schema: "identity",
                table: "account_license_plan_translations",
                columns: new[] { "license_plan_id", "culture" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_identity_account_license_plans_code",
                schema: "identity",
                table: "account_license_plans",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_account_subscription_periods_license_plan_id",
                schema: "identity",
                table: "account_subscription_periods",
                column: "license_plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_account_subscription_periods_opened_by_user_id",
                schema: "identity",
                table: "account_subscription_periods",
                column: "opened_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_account_subscription_periods_ends_at_open",
                schema: "identity",
                table: "account_subscription_periods",
                column: "ends_at",
                filter: "closed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_account_subscription_periods_property_time",
                schema: "identity",
                table: "account_subscription_periods",
                columns: new[] { "property_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "ix_identity_account_subscription_periods_starts_at",
                schema: "identity",
                table: "account_subscription_periods",
                column: "starts_at");

            migrationBuilder.CreateIndex(
                name: "ix_identity_account_subscription_periods_subscription",
                schema: "identity",
                table: "account_subscription_periods",
                columns: new[] { "subscription_id", "starts_at" });

            migrationBuilder.CreateIndex(
                name: "uq_identity_account_subscription_periods_current",
                schema: "identity",
                table: "account_subscription_periods",
                column: "subscription_id",
                unique: true,
                filter: "closed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_account_subscriptions_owner_user_id",
                schema: "identity",
                table: "account_subscriptions",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_account_subscriptions_expiry_scan",
                schema: "identity",
                table: "account_subscriptions",
                column: "ends_at",
                filter: "status = 'active'");

            migrationBuilder.CreateIndex(
                name: "ix_identity_account_subscriptions_plan",
                schema: "identity",
                table: "account_subscriptions",
                column: "license_plan_id");

            migrationBuilder.CreateIndex(
                name: "uq_identity_account_subscriptions_property",
                schema: "identity",
                table: "account_subscriptions",
                column: "property_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_identity_customer_bathhouse_likes_bathhouse",
                schema: "identity",
                table: "customer_bathhouse_likes",
                column: "bathhouse_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_customer_bathhouse_likes_user",
                schema: "identity",
                table: "customer_bathhouse_likes",
                column: "user_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_identity_customer_bathhouse_likes_user_bathhouse",
                schema: "identity",
                table: "customer_bathhouse_likes",
                columns: new[] { "user_id", "bathhouse_id" },
                unique: true,
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_customer_customers_created_by_user_id",
                schema: "identity",
                table: "customer_customers",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_identity_customer_customers_last_activity",
                schema: "identity",
                table: "customer_customers",
                columns: new[] { "property_id", "last_activity_at" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_customer_customers_property_alive",
                schema: "identity",
                table: "customer_customers",
                column: "property_id",
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_customer_customers_property_name",
                schema: "identity",
                table: "customer_customers",
                columns: new[] { "property_id", "full_name" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_customer_customers_property_phone",
                schema: "identity",
                table: "customer_customers",
                columns: new[] { "property_id", "phone" },
                filter: "deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_identity_customer_customers_user",
                schema: "identity",
                table: "customer_customers",
                column: "user_id",
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "uq_identity_customer_customers_public_id",
                schema: "identity",
                table: "customer_customers",
                column: "public_id",
                unique: true);

            // ─────────────────────────────────────────────────────────────────────────────
            // Oggetti che il modello EF non sa esprimere. Questa InitialCreate consolida
            // (2026-10-06) tutte le migration precedenti del modulo: qui sotto c'e' lo SQL a
            // mano che quelle contenevano e che vale ancora sullo schema finale. Gli UPDATE di
            // conversione dei dati (portavano righe esistenti da una forma all'altra) non sono
            // riportati: su un database nuovo non c'e' niente da convertire.
            // ─────────────────────────────────────────────────────────────────────────────
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            // Un abbonamento non puo' avere due periodi sovrapposti.
            migrationBuilder.Sql("""
ALTER TABLE identity.account_subscription_periods
  ADD CONSTRAINT ex_identity_account_subscription_periods_no_overlap
  EXCLUDE USING gist (subscription_id WITH =, tstzrange(starts_at, ends_at) WITH &&)
  WHERE (deleted_at IS NULL);
""");

            // Un solo documento di contratto pubblicato e corrente alla volta.
            migrationBuilder.Sql("""
CREATE UNIQUE INDEX uq_identity_account_contract_documents_single_current
    ON identity.account_contract_documents ((true))
 WHERE superseded_at IS NULL
   AND status = 'published'::identity.contract_document_status
   AND deleted_at IS NULL;
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "access_property_security_settings",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "access_refresh_tokens",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "access_user_auth_challenges",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "account_contract_acceptances",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "account_license_plan_translations",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "account_subscription_periods",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "customer_bathhouse_likes",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "customer_customers",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "account_contract_documents",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "account_subscriptions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "access_users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "account_license_plans",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "access_roles",
                schema: "identity");
        }
    }
}
