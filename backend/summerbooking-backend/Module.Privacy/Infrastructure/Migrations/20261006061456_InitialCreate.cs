using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Module.Privacy.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "privacy");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:Enum:privacy.actor_kind", "system_job,staff_user")
                .Annotation("Npgsql:Enum:privacy.archive_object_kind", "row_archive,linked_asset")
                .Annotation("Npgsql:Enum:privacy.audit_action", "delete_row,null_fields,anonymize,archive_cold,exported,delete_object,archive_object")
                .Annotation("Npgsql:Enum:privacy.export_artifact_origin", "s3private")
                .Annotation("Npgsql:Enum:privacy.module_code", "identity,catalog,booking,payment,fiskaly,fiscal_print,notification,switchboard,chatbot,infra")
                .Annotation("Npgsql:Enum:privacy.module_outcome_result", "pending,not_applicable,erased,anonymized,retained_legal_hold,retained_fiscal_obligation,exported,failed")
                .Annotation("Npgsql:Enum:privacy.request_kind", "erasure,export")
                .Annotation("Npgsql:Enum:privacy.request_status", "pending,in_progress,completed,partially_completed,rejected")
                .Annotation("Npgsql:Enum:privacy.requester_kind", "data_subject,staff,system_admin")
                .Annotation("Npgsql:Enum:privacy.retention_action", "delete_row,null_fields,anonymize,archive_cold,delete_object,archive_object")
                .Annotation("Npgsql:Enum:privacy.retention_basis_source", "law,recommended_policy,disputed")
                .Annotation("Npgsql:Enum:privacy.retention_duration_unit", "days,months,years,seasons")
                .Annotation("Npgsql:Enum:privacy.retention_reference_event", "property_ceased,asset_unreferenced,asset_replaced")
                .Annotation("Npgsql:Enum:privacy.retention_run_status", "succeeded,skipped,failed")
                .Annotation("Npgsql:Enum:privacy.retention_storage_date_source", "object_last_modified")
                .Annotation("Npgsql:Enum:privacy.subject_kind", "customer,user");

            migrationBuilder.CreateTable(
                name: "privacy_data_subject_requests",
                schema: "privacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico esposto nell'URL della richiesta e nella UI self-service del cliente."),
                    request_kind = table.Column<int>(type: "privacy.request_kind", nullable: false, comment: "Tipo di richiesta: erasure (Art. 17) o export (Art. 15/20)."),
                    subject_kind = table.Column<int>(type: "privacy.subject_kind", nullable: false, comment: "Tipo di entità interessata: customer (rubrica C5) o user (account di login C5)."),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id copiato per valore dell'interessato (identity_customer_customers o identity_access_users secondo subject_kind) — nessuna FK cross-schema, nessun dato personale."),
                    property_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di catalog.properties al momento della richiesta, copiato — NULL per account user di livello piattaforma; nessuna FK cross-schema."),
                    status = table.Column<int>(type: "privacy.request_status", nullable: false, defaultValueSql: "'pending'", comment: "Stato di avanzamento, consolidato dal saga leggendo gli esiti per modulo."),
                    requested_by_kind = table.Column<int>(type: "privacy.requester_kind", nullable: false, comment: "Chi ha originato la richiesta: data_subject (self-service), staff o systemadmin."),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access_users che ha inserito la richiesta, copiato — NULL se da canale senza account; nessuna FK cross-schema."),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora di creazione della richiesta — decorrenza del termine Art. 12.3."),
                    due_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Scadenza per rispondere (Art. 12.3: 30 giorni, estendibile a 90 con motivazione esplicita), valorizzata dall'applicazione all'inserimento."),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data/ora in cui lo stato è diventato completed, partially_completed o rejected."),
                    rejection_reason = table.Column<string>(type: "text", nullable: true, comment: "Motivazione quando status = rejected (es. identità del richiedente non verificabile)."),
                    export_artifact_origin = table.Column<int>(type: "privacy.export_artifact_origin", nullable: true, comment: "Sorgente di storage del file di export generato (solo request_kind = export, status = completed); oggi solo s3_private, accesso via presigned URL mai persistito."),
                    export_artifact_path = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true, comment: "Percorso del file di export nel bucket privato."),
                    export_artifact_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Scadenza del file fisico sul bucket (lifecycle policy S3); la riga resta come prova anche dopo la rimozione del file."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga, coincide con requested_at."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima transizione di stato."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Presente per uniformità di schema, non usato funzionalmente: la riga è un registro di conformità permanente; l'unica rimozione è un hard-delete a scadenza retention.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_data_subject_requests", x => x.id);
                },
                comment: "Testata di una richiesta dell'interessato (Art. 15/17/20 GDPR): cancellazione o export. Non contiene mai un dato personale del soggetto, solo riferimenti e metadati di workflow.");

            migrationBuilder.CreateTable(
                name: "privacy_legal_holds",
                schema: "privacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico per il pannello legale/amministrativo."),
                    subject_kind = table.Column<int>(type: "privacy.subject_kind", nullable: false, comment: "Tipo di entità sotto hold: customer o user."),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id copiato per valore del soggetto sotto hold (identity_customer_customers o identity_access_users secondo subject_kind) — nessuna FK cross-schema."),
                    module_code = table.Column<int>(type: "privacy.module_code", nullable: true, comment: "Se valorizzato, l'hold si applica solo a quel modulo; se NULL, a tutti i moduli coinvolti nel trattamento del soggetto."),
                    reason = table.Column<string>(type: "text", nullable: false, comment: "Motivo del contenzioso/hold, obbligatorio per audit Art. 5.2 accountability."),
                    case_reference = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true, comment: "Riferimento pratica legale/numero causa, per rintracciabilità presso l'ufficio legale."),
                    placed_by_user_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "public_id di identity_access_users che ha impostato l'hold, copiato — sempre un umano dell'ufficio legale/amministrativo, mai un job; nessuna FK cross-schema."),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data/ora di rilascio dell'hold (chiusura del contenzioso). NULL = hold attivo."),
                    released_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access_users che ha rilasciato l'hold, copiato — valorizzato solo insieme a released_at, sempre un umano; nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione dell'hold."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica, tipicamente il rilascio."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Presente per uniformità di schema, non usato funzionalmente — un hold rilasciato non si cancella, resta come prova.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_legal_holds", x => x.id);
                },
                comment: "Flag esplicito che sospende cancellazione/anonimizzazione automatica di retention per un soggetto (ed eventualmente un singolo modulo) in presenza di un contenzioso in corso. Impostato manualmente dall'ufficio legale, mai da un job.");

            migrationBuilder.CreateTable(
                name: "privacy_retention_policies",
                schema: "privacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico per il pannello di revisione SystemAdmin/DPO."),
                    schema_name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false, comment: "Schema PostgreSQL del modulo proprietario della tabella target — riferimento testuale, non FK (punta fuori da privacy per definizione), validato contro module_code."),
                    table_name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false, comment: "Tabella target nella forma canonica relativa allo schema (senza prefisso modulo)."),
                    field_names = table.Column<string[]>(type: "varchar(120)[]", nullable: false, defaultValueSql: "'{}'", comment: "Campo/i a cui si applica l'azione; array vuoto = l'intera riga (rilevante per delete_row/archive_cold)."),
                    reference_date_column = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false, comment: "Colonna sorgente della data di riferimento da cui decorre la durata. Stringa vuota = nessuna colonna locale la porta: la decorrenza la danno reference_event (un fatto accaduto altrove, come la cessazione dello stabilimento) oppure storage_date_source (la data dell'oggetto su storage, per le policy su prefisso)."),
                    duration_value = table.Column<short>(type: "smallint", nullable: false, comment: "Durata numerica della conservazione, validata positiva in applicazione."),
                    duration_unit = table.Column<int>(type: "privacy.retention_duration_unit", nullable: false, comment: "Unità della durata: days, months, years o seasons (stagioni, risolte dal job annuale sui confini di catalog.pricing_seasons — non equivalenti a 12 mesi)."),
                    action = table.Column<int>(type: "privacy.retention_action", nullable: false, comment: "Azione da applicare al superamento della durata: delete_row, null_fields, anonymize, archive_cold, delete_object (rimuove l'oggetto dallo storage) o archive_object (lo sposta su classe fredda)."),
                    only_if_unreferenced = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false, comment: "Se true, l'azione su storage si esegue solo quando nessun'altra riga nomina quel file: la stessa foto puo' stare su due vetrine e lo stesso allegato su due campagne, e la scadenza della prima non lo rende inutile. Ignorato dalle azioni su riga."),
                    reference_event = table.Column<int>(type: "privacy.retention_reference_event", nullable: true, comment: "Evento da cui decorre la durata, in alternativa alla colonna data: property_ceased (derivato da identity.account_subscriptions, a database quello stato non esiste), asset_unreferenced o asset_replaced. Fa parte della chiave naturale, perche' due policy sulla stessa tabella con la stessa azione si distinguono per l'evento che le fa scattare."),
                    storage_prefix = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true, comment: "Prefisso di storage su cui agisce la policy quando nessuna riga nomina i file (QR delle prenotazioni, file orfani). ATTENZIONE: non e' il prefisso a decidere, e' l'assenza di riferimento; il prefisso limita solo DOVE guardare. Il job deve accettare solo prefissi da elenco chiuso e rifiutare qualunque prefisso antenato di un altro noto."),
                    storage_date_source = table.Column<int>(type: "privacy.retention_storage_date_source", nullable: true, comment: "Da dove prende la data una policy su prefisso: oggi solo object_last_modified, il LastModified dell'oggetto su S3 usato come data approssimata del fatto che lo ha prodotto. Valorizzato se e solo se storage_prefix lo e'."),
                    target_filter = table.Column<string>(type: "jsonb", nullable: true, comment: "QUALI righe della tabella riguarda la policy, quando non sono tutte. NULL = non dichiarato. Due forme: {\"scope\": \"whole_table\"} per dire DELIBERATAMENTE tutte, oppure {\"column\": \"kind\", \"op\": \"=\", \"value\": \"rendered_pdf\"} per un filtro (op ammessi: = <> in 'not in' 'is null' 'is not null'). ⚠️ Su un'azione su storage questa colonna o only_if_unreferenced DEVONO essere valorizzate, altrimenti il motore salta la policy: su un file un perimetro indovinato non si annulla. I valori sono limitati a token identificativi, vedi RetentionEngine.BuildTargetFilter."),
                    linked_asset = table.Column<string>(type: "jsonb", nullable: true, comment: "Il file che deve SEGUIRE le righe archiviate da questa policy, nella forma {\"column\": \"pdf_asset_id\", \"table\": \"media_assets\", \"action\": \"archive_object\"}. Riguarda le sole policy archive_cold. ⚠️ Serve perche' archiviare una riga e lasciare il suo file dov'era lascia il file sullo storage senza nessuna riga calda che lo nomini, e con la data da cui scade uscita dal database insieme alla riga: non resta niente che possa decidere quando cancellarlo. Il file viene anche inventariato in privacy_archive_manifests con object_kind = 'linked_asset', cosi' la policy decennale su quell'inventario lo cancella insieme all'archivio che accompagnava."),
                    legal_basis = table.Column<string>(type: "text", nullable: false, comment: "Riferimento normativo o motivazione di policy aziendale del termine scelto."),
                    basis_source = table.Column<int>(type: "privacy.retention_basis_source", nullable: false, comment: "Qualifica la fonte del termine: law (obbligo verificato), recommended_policy (proposta da approvare) o disputed (qualificazione non risolta, non attivare)."),
                    is_dry_run_only = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Se true, il job produce solo un report senza eseguire l'azione — obbligatorio prima di abilitare l'esecuzione reale."),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true, comment: "Policy attiva/sospesa: permette di sospendere una regola senza cancellarla."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della regola."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica della regola."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Soft-delete della policy (regola ritirata) — Global Query Filter EF standard.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_retention_policies", x => x.id);
                    table.CheckConstraint("ck_privacy_retention_policies_has_decorrenza", "reference_date_column <> '' OR reference_event IS NOT NULL OR storage_date_source IS NOT NULL");
                    table.CheckConstraint("ck_privacy_retention_policies_linked_asset_action", "linked_asset IS NULL OR action = 'archive_cold'");
                    table.CheckConstraint("ck_privacy_retention_policies_linked_asset_shape", "linked_asset IS NULL OR (jsonb_typeof(linked_asset) = 'object' AND linked_asset ? 'column' AND linked_asset ? 'table' AND linked_asset ? 'action')");
                    table.CheckConstraint("ck_privacy_retention_policies_prefix_action", "storage_prefix IS NULL OR action IN ('delete_object', 'archive_object')");
                    table.CheckConstraint("ck_privacy_retention_policies_prefix_date", "(storage_prefix IS NULL) = (storage_date_source IS NULL)");
                    table.CheckConstraint("ck_privacy_retention_policies_target_filter_shape", "target_filter IS NULL OR (jsonb_typeof(target_filter) = 'object' AND (target_filter ? 'scope' OR (target_filter ? 'column' AND target_filter ? 'op')))");
                    table.CheckConstraint("ck_privacy_retention_policies_target_filter_table", "target_filter IS NULL OR table_name <> ''");
                    table.CheckConstraint("ck_privacy_retention_policies_target_xor", "(table_name <> '' AND storage_prefix IS NULL) OR (storage_prefix IS NOT NULL AND storage_prefix <> '' AND table_name = '')");
                },
                comment: "Configurazione dichiarativa di retention: su schema.tabella (campo/i X) oppure su un prefisso di storage, calcolato dalla data Y, oltre la durata Z, esegui l'azione W. Letta dal job del modulo proprietario, mai eseguita da qui. Ambito piattaforma, valida per tutti gli stabilimenti.");

            migrationBuilder.CreateTable(
                name: "privacy_data_subject_request_module_outcomes",
                schema: "privacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    request_id = table.Column<long>(type: "bigint", nullable: false, comment: "Richiesta a cui l'esito appartiene."),
                    module_code = table.Column<int>(type: "privacy.module_code", nullable: false, comment: "Modulo/dominio proprietario dei dati che ha risposto (vocabolario chiuso dei domini applicativi del progetto)."),
                    outcome = table.Column<int>(type: "privacy.module_outcome_result", nullable: false, defaultValueSql: "'pending'", comment: "Esito applicato da quel modulo: pending, not_applicable, erased, anonymized, retained_legal_hold, retained_fiscal_obligation, exported o failed."),
                    outcome_detail = table.Column<string>(type: "text", nullable: true, comment: "Motivazione leggibile mostrata al richiedente nella risposta parzialmente accolta."),
                    retained_until = table.Column<DateOnly>(type: "date", nullable: true, comment: "Data fino a cui il vincolo di conservazione è previsto durare, quando calcolabile (tipicamente retained_fiscal_obligation); abilita il job di ri-tentativo automatico."),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data/ora in cui il modulo ha applicato l'esito e pubblicato l'evento di risposta; NULL finché outcome = pending."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga, alla pubblicazione dell'evento verso il modulo."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultima modifica: la risposta del modulo, o un ri-tentativo su un esito trattenuto."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Presente per uniformità di schema, non usato funzionalmente — stesso ciclo di vita da registro di conformità della testata.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_data_subject_request_module_outcomes", x => x.id);
                    table.ForeignKey(
                        name: "fk_privacy_data_subject_request_module_outcomes_request",
                        column: x => x.request_id,
                        principalSchema: "privacy",
                        principalTable: "privacy_data_subject_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                },
                comment: "Esito applicato da un singolo modulo a una richiesta dell'interessato: una riga per ogni modulo coinvolto, dettaglio che la testata riassume in status.");

            migrationBuilder.CreateTable(
                name: "privacy_archive_manifests",
                schema: "privacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico della riga di inventario."),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Corsa che ha prodotto l'archivio. Riferimento LOGICO e non FK: le righe di privacy_retention_runs scadono a 24 mesi, questa tabella vive molto piu' a lungo, e una chiave esterna impedirebbe al verbale di scadere."),
                    policy_id = table.Column<long>(type: "bigint", nullable: false, comment: "Policy che ha deciso l'archiviazione."),
                    source_schema = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false, comment: "Schema della tabella le cui righe sono dentro l'archivio."),
                    source_table = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false, comment: "Nome FISICO della tabella le cui righe sono dentro l'archivio."),
                    storage_origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Host dello storage dell'archivio. ⚠️ Deve essere un'etichetta DISTINTA da quella dei file applicativi: l'archivio sta su un bucket dedicato, e il job che svuota la coda delle cancellazioni risolve il bucket proprio da questa etichetta."),
                    storage_path = table.Column<string>(type: "text", nullable: false, comment: "Chiave dell'oggetto, nella forma archivio/<schema>/<tabella>/<anno>/<mese>/<istante>-<policy>-<lotto>.jsonl.gz."),
                    byte_size = table.Column<long>(type: "bigint", nullable: true, comment: "Byte dell'oggetto; per un archivio di righe, compressi. NULL ammesso solo su un file collegato di cui la dimensione non era registrata: byte_size e' annullabile su tutte le tabelle di asset, e inventare uno zero sarebbe peggio del non saperlo."),
                    object_kind = table.Column<int>(type: "privacy.archive_object_kind", nullable: false, defaultValueSql: "'row_archive'::privacy.archive_object_kind", comment: "Che cosa e' l'oggetto: 'row_archive' = l'archivio JSON Lines delle righe uscite dal database; 'linked_asset' = un file che ha SEGUITO quelle righe, per esempio il PDF di uno scontrino archiviato con la sua ricevuta. Stanno nello stesso inventario perche' devono avere la stessa scadenza, e una sola policy decennale li cancella entrambi. Il default e' row_archive perche' e' quello che erano tutte le righe prima del 2026-10-06."),
                    row_count = table.Column<long>(type: "bigint", nullable: true, comment: "Quante righe di database sono dentro questo oggetto. Serve a verificare che l'archivio sia completo senza aprirlo. NULL quando object_kind = 'linked_asset': un file che ha seguito le righe non contiene righe, e 'zero' sarebbe un'altra cosa."),
                    cutoff_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Data di taglio della policy che ha prodotto l'archivio: dice che cosa c'e' dentro."),
                    archived_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Quando l'archivio e' stato scritto. E' la DECORRENZA della policy che lo cancellera'."),
                    purged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando l'oggetto e' stato rimosso dallo storage. NULL = c'e' ancora. La riga resta come prova di averlo rimosso nei termini."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Inserimento della riga."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultimo aggiornamento (in pratica: la valorizzazione di purged_at).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_archive_manifests", x => x.id);
                    table.CheckConstraint("ck_privacy_archive_manifests_counts", "(object_kind = 'row_archive' AND row_count IS NOT NULL AND row_count > 0                              AND byte_size IS NOT NULL AND byte_size > 0) OR (object_kind = 'linked_asset' AND row_count IS NULL                               AND (byte_size IS NULL OR byte_size > 0))");
                    table.CheckConstraint("ck_privacy_archive_manifests_purged", "purged_at IS NULL OR purged_at >= archived_at");
                    table.ForeignKey(
                        name: "fk_privacy_archive_manifests_policy",
                        column: x => x.policy_id,
                        principalSchema: "privacy",
                        principalTable: "privacy_retention_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Inventario degli archivi prodotti dalle policy archive_cold: una riga per oggetto scritto su storage. Separata dal verbale delle corse perche' deve vivere quanto l'oggetto che indirizza - dieci anni per gli scontrini fiscali - mentre il verbale e' operativo e scade a 24 mesi. E' anche la fonte da cui parte la policy che cancella l'archivio alla scadenza.");

            migrationBuilder.CreateTable(
                name: "privacy_compliance_audit_logs",
                schema: "privacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    occurred_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Data/ora dell'azione."),
                    module_code = table.Column<int>(type: "privacy.module_code", nullable: false, comment: "Modulo che ha eseguito l'azione sui propri dati."),
                    schema_name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false, comment: "Schema toccato — riferimento testuale, non FK."),
                    table_name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false, comment: "Tabella toccata — riferimento testuale, non FK."),
                    record_id = table.Column<long>(type: "bigint", nullable: false, comment: "Id interno (bigint) della riga toccata nello schema di origine — puramente informativo, mai una FK cross-schema; la riga sorgente può non esistere più alla lettura."),
                    field_names = table.Column<List<string>>(type: "varchar(120)[]", nullable: false, defaultValueSql: "'{}'", comment: "Campi toccati dall'azione (null_fields/anonymize); vuoto per delete_row/archive_cold."),
                    action = table.Column<int>(type: "privacy.audit_action", nullable: false, comment: "Azione eseguita: delete_row, null_fields, anonymize, archive_cold o exported."),
                    retention_policy_id = table.Column<long>(type: "bigint", nullable: true, comment: "Policy di retention che ha generato l'azione, se schedulata."),
                    data_subject_request_id = table.Column<long>(type: "bigint", nullable: true, comment: "Richiesta GDPR che ha generato l'azione, se esplicita."),
                    reason = table.Column<string>(type: "text", nullable: false, comment: "Motivazione dell'azione, ereditata dalla policy/richiesta o inserita a mano."),
                    performed_by = table.Column<int>(type: "privacy.actor_kind", nullable: false, comment: "Chi ha eseguito materialmente l'azione: system_job o staff_user."),
                    performed_by_user_id = table.Column<Guid>(type: "uuid", nullable: true, comment: "public_id di identity_access_users, copiato — valorizzato solo se performed_by = staff_user; nessuna FK cross-schema."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga, coincide con occurred_at."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Presente per uniformità di schema, non usato: registro append-only, non cambia mai dopo l'inserimento."),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Presente per uniformità di schema, non usato dall'applicazione — la sola rimozione è un hard-delete a scadenza della propria retention (1-2 anni).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_compliance_audit_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_privacy_compliance_audit_logs_data_subject_request",
                        column: x => x.data_subject_request_id,
                        principalSchema: "privacy",
                        principalTable: "privacy_data_subject_requests",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_privacy_compliance_audit_logs_retention_policy",
                        column: x => x.retention_policy_id,
                        principalSchema: "privacy",
                        principalTable: "privacy_retention_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                },
                comment: "Registro append-only di ogni azione di cancellazione/anonimizzazione/export eseguita da un modulo proprietario sui propri dati — prova di conformità Art. 5.2 GDPR (chi/cosa/quando/perché).");

            migrationBuilder.CreateTable(
                name: "privacy_file_deletions",
                schema: "privacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico per il pannello SystemAdmin/DPO e per correlare il report di dry-run con l'esecuzione."),
                    asset_schema = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: true, comment: "Schema del modulo proprietario della riga che nominava il file (catalog, fiskaly, notification, switchboard). NULL per gli oggetti che nessuna riga nomina: QR e orfani."),
                    asset_table = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: true, comment: "Tabella di asset nella forma canonica relativa allo schema (media_assets), come in privacy_retention_policies.table_name. NULL insieme alle altre due."),
                    asset_id = table.Column<long>(type: "bigint", nullable: true, comment: "Chiave della riga di asset. NULL insieme alle altre due."),
                    storage_origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, comment: "Host dello storage come era al momento della decisione. Testo e non enum nativo: i quattro moduli di asset hanno enum media_origin con etichette diverse fra loro, e questa coda li raccoglie tutti."),
                    storage_path = table.Column<string>(type: "text", nullable: false, comment: "Chiave dell'oggetto congelata al momento della decisione: se il path sulla riga di asset cambia dopo, si cancella comunque l'oggetto che era stato valutato."),
                    byte_size = table.Column<int>(type: "integer", nullable: true, comment: "Dimensione nota dell'oggetto, per rendicontare lo spazio liberato. NULL se il metadato non era stato calcolato."),
                    action = table.Column<int>(type: "privacy.retention_action", nullable: false, defaultValueSql: "'delete_object'::privacy.retention_action", comment: "Che cosa va fatto all'oggetto: delete_object lo rimuove, archive_object lo sposta su una classe di storage piu' fredda lasciandolo dov'e'. ⚠️ Senza questa colonna le due azioni erano indistinguibili in coda, e il job avrebbe CANCELLATO i PDF degli scontrini fiscali che la policy a 36 mesi vuole soltanto raffreddare. Il default e' delete_object perche' e' quello che erano tutte le righe prima del 2026-10-05. La classe di destinazione NON sta qui: sta in Retention:Deletion:Targets[].ArchiveStorageClass, perche' e' una decisione di costo e dipende dall'ambiente."),
                    policy_id = table.Column<long>(type: "bigint", nullable: true, comment: "Policy che ha deciso la cancellazione. NULL = cancellazione manuale di un amministratore."),
                    reason = table.Column<string>(type: "text", nullable: false, comment: "Perche' l'oggetto e' in coda, in forma leggibile da chi apre il pannello. Si regge da solo: non rimanda a documenti interni ne' a chi ha deciso il termine."),
                    decided_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Quando la policy (o la persona) ha deciso la cancellazione."),
                    executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando l'oggetto e' stato davvero rimosso dallo storage. NULL = ancora da fare: e' il filtro della coda."),
                    attempts = table.Column<short>(type: "smallint", nullable: false, defaultValue: (short)0, comment: "Tentativi di rimozione gia' fatti. E' l'unica cosa che distingue un lavoro non ancora iniziato da uno che non riesce."),
                    last_error = table.Column<string>(type: "text", nullable: true, comment: "Ultimo errore riportato dallo storage, testuale."),
                    last_error_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Quando si e' verificato l'ultimo errore."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Creazione della riga di coda."),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Ultimo aggiornamento (tentativo, errore, esecuzione).")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_file_deletions", x => x.id);
                    table.CheckConstraint("ck_privacy_file_deletions_action", "action IN ('delete_object', 'archive_object')");
                    table.CheckConstraint("ck_privacy_file_deletions_asset", "(asset_schema IS NULL AND asset_table IS NULL AND asset_id IS NULL) OR (asset_schema IS NOT NULL AND asset_table IS NOT NULL AND asset_id IS NOT NULL)");
                    table.CheckConstraint("ck_privacy_file_deletions_last_error", "(last_error IS NULL) = (last_error_at IS NULL)");
                    table.ForeignKey(
                        name: "fk_privacy_file_deletions_policy",
                        column: x => x.policy_id,
                        principalSchema: "privacy",
                        principalTable: "privacy_retention_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Coda delle AZIONI su storage decise da una policy di retention o a mano: cancellare un oggetto (delete_object) oppure spostarlo su una classe piu' fredda (archive_object). Serve perche' un'operazione su S3 non partecipa alla transazione del database: senza coda un errore di rete lascia il database e lo storage in disaccordo. Agisce solo sulla copia AWS; la copia locale di sicurezza non viene toccata. ⚠️ IL NOME DICE MENO DI QUELLO CHE FA: nasce il 2026-10-03 quando l'unica azione era la cancellazione, e dal 2026-10-05 porta anche archive_object, che non cancella niente. Guardare la colonna action.");

            migrationBuilder.CreateTable(
                name: "privacy_retention_runs",
                schema: "privacy",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    public_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Id pubblico della riga di verbale."),
                    run_id = table.Column<Guid>(type: "uuid", nullable: false, comment: "Raggruppa tutte le righe prodotte dalla stessa esecuzione, su tutti gli schemi. E' la chiave con cui si legge un report di dry-run per intero."),
                    policy_id = table.Column<long>(type: "bigint", nullable: false, comment: "Policy valutata."),
                    schema_name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false, comment: "Schema del modulo proprietario, copiato dalla policy."),
                    table_name = table.Column<string>(type: "character varying(63)", maxLength: 63, nullable: false, comment: "Nome FISICO della tabella, come risolto dal motore: in privacy e switchboard ha il prefisso di modulo, mentre la policy porta la forma canonica senza prefisso."),
                    action = table.Column<int>(type: "privacy.retention_action", nullable: false, comment: "Azione della policy, copiata: il verbale deve restare leggibile anche se la policy viene poi modificata."),
                    cutoff_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true, comment: "Data di taglio calcolata. NULL quando la policy e' stata saltata prima di calcolarla."),
                    is_dry_run = table.Column<bool>(type: "boolean", nullable: false, comment: "La corsa non ha scritto niente: o era una prova, o la policy e' is_dry_run_only."),
                    matched_rows = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L, comment: "Righe oltre la data di taglio. E' il numero che si legge nel report di dry-run."),
                    affected_rows = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L, comment: "Righe davvero cancellate, azzerate o anonimizzate. Zero in prova, anche con matched_rows alto."),
                    archive_origin = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true, comment: "Host dello storage di archivio. Testo e non enum: e' lo stesso motivo di privacy_file_deletions.storage_origin."),
                    archive_path = table.Column<string>(type: "text", nullable: true, comment: "Chiave dell'oggetto di archivio, o piu' chiavi separate da spazio se l'export e' andato in piu' lotti. ⚠️ ECO OPERATIVO, NON L'INVENTARIO: questa riga scade a 24 mesi, mentre gli archivi vivono fino a dieci anni. L'inventario autorevole di dove sono gli archivi e' privacy_archive_manifests, una riga per oggetto, che vive quanto l'oggetto che indirizza. Qui la chiave c'e' solo per leggere il verbale di una corsa senza fare una join, e dopo 24 mesi non c'e' piu'."),
                    archive_byte_size = table.Column<long>(type: "bigint", nullable: true, comment: "Byte scritti sull'archivio, compressi. Come archive_path: eco operativo, il dato autorevole per oggetto sta su privacy_archive_manifests.byte_size."),
                    status = table.Column<int>(type: "privacy.retention_run_status", nullable: false, defaultValueSql: "'succeeded'", comment: "succeeded, skipped (ragione dichiarata e legittima, non un errore) o failed."),
                    detail = table.Column<string>(type: "text", nullable: true, comment: "Il motivo del salto, o il testo dell'errore con le sue cause. NULL se e' andata liscia."),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, comment: "Istante di valutazione della corsa, lo stesso per tutte le policy della corsa: due policy della stessa esecuzione non devono vedere due 'adesso' diversi."),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Fine della valutazione di questa policy."),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L, comment: "Quanto e' durata questa policy. Serve a capire quali non staranno in una finestra notturna."),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()", comment: "Inserimento della riga.")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_privacy_retention_runs", x => x.id);
                    table.CheckConstraint("ck_privacy_retention_runs_counts", "matched_rows >= 0 AND affected_rows >= 0 AND affected_rows <= matched_rows");
                    table.CheckConstraint("ck_privacy_retention_runs_detail", "(status = 'failed' AND detail IS NOT NULL) OR status <> 'failed'");
                    table.CheckConstraint("ck_privacy_retention_runs_dry_run", "NOT is_dry_run OR affected_rows = 0");
                    table.CheckConstraint("ck_privacy_retention_runs_times", "finished_at >= started_at");
                    table.ForeignKey(
                        name: "fk_privacy_retention_runs_policy",
                        column: x => x.policy_id,
                        principalSchema: "privacy",
                        principalTable: "privacy_retention_policies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                },
                comment: "Verbale di una policy di retention in una corsa: righe trovate, righe toccate, dove ha archiviato, e se e' fallita perche'. Una riga per (corsa, policy). Serve a tre cose che nessun'altra tabella copre: ritrovare le righe archiviate su storage, raccogliere gli errori senza fermare il motore, e leggere il report di dry-run prima di autorizzare l'esecuzione reale.");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_archive_manifests_da_rimuovere",
                schema: "privacy",
                table: "privacy_archive_manifests",
                column: "archived_at",
                filter: "purged_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_archive_manifests_policy_id",
                schema: "privacy",
                table: "privacy_archive_manifests",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_archive_manifests_sorgente",
                schema: "privacy",
                table: "privacy_archive_manifests",
                columns: new[] { "source_schema", "source_table", "cutoff_at" });

            migrationBuilder.CreateIndex(
                name: "uq_privacy_archive_manifests_path",
                schema: "privacy",
                table: "privacy_archive_manifests",
                columns: new[] { "storage_origin", "storage_path" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uq_privacy_archive_manifests_public_id",
                schema: "privacy",
                table: "privacy_archive_manifests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "brin_privacy_compliance_audit_logs_occurred_at",
                schema: "privacy",
                table: "privacy_compliance_audit_logs",
                column: "occurred_at")
                .Annotation("Npgsql:IndexMethod", "brin");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_compliance_audit_logs_module_time",
                schema: "privacy",
                table: "privacy_compliance_audit_logs",
                columns: new[] { "module_code", "occurred_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_privacy_compliance_audit_logs_policy",
                schema: "privacy",
                table: "privacy_compliance_audit_logs",
                column: "retention_policy_id",
                filter: "retention_policy_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_compliance_audit_logs_request",
                schema: "privacy",
                table: "privacy_compliance_audit_logs",
                column: "data_subject_request_id",
                filter: "data_subject_request_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_compliance_audit_logs_target_row",
                schema: "privacy",
                table: "privacy_compliance_audit_logs",
                columns: new[] { "schema_name", "table_name", "record_id" });

            migrationBuilder.CreateIndex(
                name: "ix_privacy_data_subject_request_module_outcomes_module",
                schema: "privacy",
                table: "privacy_data_subject_request_module_outcomes",
                columns: new[] { "module_code", "outcome" });

            migrationBuilder.CreateIndex(
                name: "ix_privacy_data_subject_request_module_outcomes_pending",
                schema: "privacy",
                table: "privacy_data_subject_request_module_outcomes",
                column: "outcome",
                filter: "outcome = 'pending'");

            migrationBuilder.CreateIndex(
                name: "uq_privacy_data_subject_request_module_outcomes_request_module",
                schema: "privacy",
                table: "privacy_data_subject_request_module_outcomes",
                columns: new[] { "request_id", "module_code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_privacy_data_subject_requests_due",
                schema: "privacy",
                table: "privacy_data_subject_requests",
                column: "due_at",
                filter: "status IN ('pending','in_progress')");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_data_subject_requests_open",
                schema: "privacy",
                table: "privacy_data_subject_requests",
                column: "status",
                filter: "status IN ('pending','in_progress')");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_data_subject_requests_property",
                schema: "privacy",
                table: "privacy_data_subject_requests",
                column: "property_id",
                filter: "property_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_data_subject_requests_subject",
                schema: "privacy",
                table: "privacy_data_subject_requests",
                columns: new[] { "subject_kind", "subject_id" });

            migrationBuilder.CreateIndex(
                name: "uq_privacy_data_subject_requests_public_id",
                schema: "privacy",
                table: "privacy_data_subject_requests",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_privacy_file_deletions_pending",
                schema: "privacy",
                table: "privacy_file_deletions",
                column: "decided_at",
                filter: "executed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_file_deletions_policy_id",
                schema: "privacy",
                table: "privacy_file_deletions",
                column: "policy_id");

            migrationBuilder.CreateIndex(
                name: "uq_privacy_file_deletions_pending_path",
                schema: "privacy",
                table: "privacy_file_deletions",
                columns: new[] { "storage_origin", "storage_path" },
                unique: true,
                filter: "executed_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_privacy_file_deletions_public_id",
                schema: "privacy",
                table: "privacy_file_deletions",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_privacy_legal_holds_module_active",
                schema: "privacy",
                table: "privacy_legal_holds",
                column: "module_code",
                filter: "released_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_legal_holds_subject_active",
                schema: "privacy",
                table: "privacy_legal_holds",
                columns: new[] { "subject_kind", "subject_id" },
                filter: "released_at IS NULL AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_privacy_legal_holds_public_id",
                schema: "privacy",
                table: "privacy_legal_holds",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_privacy_retention_policies_lookup",
                schema: "privacy",
                table: "privacy_retention_policies",
                columns: new[] { "schema_name", "table_name" },
                filter: "is_active AND deleted_at IS NULL");

            migrationBuilder.CreateIndex(
                name: "uq_privacy_retention_policies_public_id",
                schema: "privacy",
                table: "privacy_retention_policies",
                column: "public_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_privacy_retention_runs_failed",
                schema: "privacy",
                table: "privacy_retention_runs",
                column: "finished_at",
                filter: "status = 'failed'");

            migrationBuilder.CreateIndex(
                name: "ix_privacy_retention_runs_policy",
                schema: "privacy",
                table: "privacy_retention_runs",
                columns: new[] { "policy_id", "finished_at" });

            migrationBuilder.CreateIndex(
                name: "ix_privacy_retention_runs_run",
                schema: "privacy",
                table: "privacy_retention_runs",
                columns: new[] { "run_id", "schema_name" });

            migrationBuilder.CreateIndex(
                name: "uq_privacy_retention_runs_public_id",
                schema: "privacy",
                table: "privacy_retention_runs",
                column: "public_id",
                unique: true);

            // ─────────────────────────────────────────────────────────────────────────────
            // Oggetti che il modello EF non sa esprimere. Questa InitialCreate consolida
            // (2026-10-06) tutte le migration precedenti del modulo: qui sotto c'e' lo SQL a
            // mano che quelle contenevano e che vale ancora sullo schema finale. Gli UPDATE di
            // conversione dei dati (portavano righe esistenti da una forma all'altra) non sono
            // riportati: su un database nuovo non c'e' niente da convertire.
            // ─────────────────────────────────────────────────────────────────────────────

            // Una sola policy viva per bersaglio. NULLS NOT DISTINCT: reference_event e
            // storage_prefix sono NULL sulla maggior parte delle righe, e senza questa clausola due
            // righe con NULL non collidono.
            migrationBuilder.Sql("""
CREATE UNIQUE INDEX uq_privacy_retention_policies_target
  ON privacy.privacy_retention_policies
     (schema_name, table_name, field_names, action, reference_event, storage_prefix)
  NULLS NOT DISTINCT
  WHERE deleted_at IS NULL;
""");

            // Seed delle policy di retention nella forma finale. Consolida i seed e le correzioni delle
            // migration SeedRetentionPolicies, ResolveDisputedSwitchboardRetentionBasis,
            // SeedFileRetentionPolicies, SeedRetentionRunsRetentionPolicy,
            // SeedArchiveManifestRetentionPolicy, AddRetentionTargetFilter,
            // RaiseComplianceProofRetentionToTenYears e AddLinkedAssetArchiving.
            //
            // Tutte nascono is_dry_run_only = true (default di colonna): si accendono con il verbo
            // retention-enable, mai con una migration, che le farebbe nascere accese anche in
            // produzione. Le due su fiskaly.media_assets nascono
            // is_active = false: sono ritirate, il PDF di uno scontrino segue la sua ricevuta
            // (linked_asset sulla policy archive_cold di fiskaly.receipts).
            migrationBuilder.Sql("""
INSERT INTO privacy.privacy_retention_policies
  (public_id, schema_name, table_name, field_names, reference_date_column,
   duration_value, duration_unit, action, only_if_unreferenced, reference_event,
   storage_prefix, storage_date_source, target_filter, linked_asset, is_active,
   basis_source, legal_basis)
VALUES
  ('01a0a783-6000-7710-ae2c-c3f18f440d34', 'booking', 'reservations', '{}', 'stay_to', 2, 'seasons', 'archive_cold', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Nessun obbligo di mantenere online la testata: resta consultabile per la stagione corrente e le due precedenti (rinnovi, confronti di ricavo, riassegnazione della stessa postazione), poi archiviazione su storage freddo con i dati identificativi pseudonimizzati.'),
  ('01a0a783-6000-7c04-8fbf-b6c0f05e9946', 'booking', 'reservations', '{}', 'stay_to', 10, 'years', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'law', 'Art. 2220 c.c.: conservazione decennale del riepilogo economico collegato al fiscale. Cancellazione dell''archivio freddo alla scadenza del termine.'),
  ('01a0a783-6000-799d-82c7-10a553761cf6', 'booking', 'reservations', '{note,customer_tags}', 'stay_to', 1, 'seasons', 'null_fields', false, NULL, NULL, NULL, NULL, NULL, true, 'law', 'Art. 5.1.e e Art. 9 GDPR: campo libero che puo'' contenere dati relativi alla salute, privo di base giuridica per una conservazione prolungata. Azzerato una stagione prima della testata che lo contiene.'),
  ('01a0a783-6000-7370-b376-59d2fcad548c', 'booking', 'lines', '{}', 'end_date', 2, 'seasons', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Nessun obbligo: il dettaglio e'' ricostruibile e non concorre al fiscale. Conservato quanto la testata per contestazioni e riemissione, poi cancellato.'),
  ('01a0a783-6000-7810-9f76-1bb86fc24b03', 'booking', 'service_lines', '{}', 'service_date', 1, 'seasons', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.c GDPR (minimizzazione): il dettaglio dei servizi non concorre al fiscale. Resta la testata economica quando esiste uno scontrino.'),
  ('01a0a783-6000-7ed9-8dc4-699ef4e2b174', 'booking', 'resource_consumptions', '{}', 'consumption_date', 1, 'seasons', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Nessun obbligo di conservazione. VINCOLO OPERATIVO: la cancellazione e'' subordinata alla produzione dell''aggregato mensile, che sostituisce il dettaglio nella reportistica storica; il job deve interrompersi se l''aggregato del periodo non esiste, non cancellare comunque.'),
  ('01a0a783-6000-73de-941a-3906a4e9e20a', 'booking', 'resale_offers', '{}', 'offer_date', 2, 'seasons', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Nessun obbligo di conservazione; finestra scelta per consentire confronti di occupazione su piu'' stagioni.'),
  ('01a0a783-6000-7700-8cf4-432a2fee9430', 'booking', 'spot_holds', '{}', 'end_date', 2, 'seasons', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Nessun obbligo: blocchi scaduti privi di valore fiscale. Da non confondere con il TTL di pochi minuti degli hold di checkout, liberati da un job dedicato.'),
  ('01a0a783-6000-7d62-891d-c59f49bd854e', 'booking', 'groups', '{}', 'created_at', 2, 'seasons', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Nessun obbligo di conservazione: segue la finestra della testata a cui i raggruppamenti si riferiscono.'),
  ('01a0a783-6000-7ec9-bd3f-e670b154754e', 'booking', 'resale_credit_ledger', '{}', 'occurred_at', 10, 'years', 'archive_cold', false, NULL, NULL, NULL, NULL, NULL, true, 'law', 'Art. 2220 c.c.: il credito maturato e'' una scrittura contabile del wallet cliente. Il dettaglio giornaliero della condivisione scade prima, con le offerte di rivendita.'),
  ('01a0a783-6000-7b01-8e68-60a0c59c3814', 'payment', 'transaction_charges', '{}', 'occurred_at', 24, 'months', 'archive_cold', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 2220 c.c. per la conservazione decennale. Finestra online di 24 mesi scelta per coprire riconciliazione contabile e termini di contestazione dei pagamenti con margine, poi archiviazione.'),
  ('01a0a783-6000-7aee-a9ca-f00d8d11149e', 'payment', 'transaction_refunds', '{}', 'occurred_at', 24, 'months', 'archive_cold', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 2220 c.c.: scrittura contabile. Stessa finestra online delle transazioni a cui i rimborsi si riferiscono.'),
  ('01a0a783-6000-75c6-b224-8200e6b66056', 'payment', 'wallet_transactions', '{}', 'occurred_at', 24, 'months', 'archive_cold', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 2220 c.c.: i movimenti del wallet cliente sono scritture contabili. Finestra online di 24 mesi, poi archiviazione.'),
  ('01a0a783-6000-7b6b-b615-2ceb9dabb5d2', 'payment', 'ledger_entries', '{}', 'occurred_at', 24, 'months', 'archive_cold', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 2220 c.c. Include gli snapshot di nome, email e telefono congelati sulla riga, che seguono la sorte del movimento contabile.'),
  ('01a0a783-6000-7365-85c2-0cff06c87200', 'payment', 'ledger_entries', '{}', 'occurred_at', 10, 'years', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'law', 'Art. 2220 c.c.: cancellazione dell''archivio alla scadenza del termine decennale.'),
  ('01a0a783-6000-7eba-81b1-8cc18d7f22ea', 'payment', 'pos_charges', '{}', 'charged_at', 24, 'months', 'archive_cold', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 2220 c.c.: gli addebiti registrati su POS esterni concorrono al conto del cliente. Finestra online di 24 mesi, poi archiviazione.'),
  ('01a0a783-6000-7305-be91-2af599475961', 'payment', 'pos_charges', '{}', 'charged_at', 10, 'years', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'law', 'Art. 2220 c.c.: obbligo fiscale decennale pieno, trattandosi di addebiti che concorrono al conto del cliente. Nessuna cancellazione anticipata dell''archivio.'),
  ('01a0a783-6000-7fc8-82ba-3d4dbcb6aa8f', 'payment', 'request_links', '{stripe_payment_link_id}', 'created_at', 12, 'months', 'null_fields', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.c GDPR (minimizzazione): il riferimento al link di pagamento non ha valore una volta esaurita l''operazione. Il fatto che il pagamento sia stato richiesto, con importo ed esito, resta sulla transazione per il termine fiscale.'),
  ('01a0a783-6000-7929-9590-3dfaf0da14e6', 'payment', 'customer_gateway_profiles', '{}', 'last_used_at', 24, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: la mappatura verso il gestore di pagamento non serve oltre un''inattivita'' prolungata. La chiusura dell''account non anticipa la cancellazione. Nessun numero di carta o codice di sicurezza e'' conservato, in nessun caso (PCI-DSS).'),
  ('01a0a783-6000-72a9-997a-96409b37b871', 'fiskaly', 'receipts', '{}', 'signed_at', 36, 'months', 'archive_cold', false, NULL, NULL, NULL, NULL, '{"table": "media_assets", "action": "archive_object", "column": "pdf_asset_id"}', true, 'recommended_policy', 'Art. 2220 c.c. e art. 39 DPR 633/1972 per la conservazione decennale. Finestra online di 36 mesi per la consultazione corrente; oltre, metadati su database freddo e documento su storage a lungo termine.'),
  ('01a0a783-6000-7086-95a6-6d51f3b830b6', 'fiskaly', 'receipts', '{}', 'signed_at', 10, 'years', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'law', 'Art. 2220 c.c. e art. 39 DPR 633/1972: nessuna cancellazione prima di dieci anni, nemmeno a fronte di una richiesta dell''interessato (Art. 17.3.b GDPR, obbligo legale di conservazione).'),
  ('01a0a783-6000-733c-9f81-0086af1e4467', 'fiskaly', 'receipt_logs', '{}', 'created_at', 12, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Nessun obbligo di conservazione: e'' la diagnostica del dialogo con il servizio di firma, non il documento commerciale.'),
  ('01a0a783-6000-77d6-9ade-92cb36fe91ac', 'fiskaly', 'cash_registers', '{}', 'deleted_at', 10, 'years', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'law', 'Art. 2220 c.c.: si conserva il minimo necessario a identificare il dispositivo per i documenti che lo referenziano. LIMITE: la decorrenza corretta e'' la data dell''ultimo documento collegato, non la dismissione del dispositivo; la data va calcolata dal job.'),
  ('01a0a783-6000-746b-b7a9-dcc4bf665558', 'fiscalprint', 'receipts', '{}', 'printed_at', 12, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Nessun obbligo di conservazione: il documento fiscale e'' il giornale di fondo del registratore telematico, non questo log di stampa.'),
  ('01a0a783-6000-78db-94ed-6940dc6d04e2', 'identity', 'customer_customers', '{full_name,phone,phone_secondary,email,address,city,postal_code,date_of_birth,other_names}', 'last_activity_at', 48, 'months', 'anonymize', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR (limitazione della conservazione), per analogia con il termine di prescrizione ordinaria dell''art. 2946 c.c. Restano identificativo, codice fiscale e importi necessari agli obblighi fiscali.'),
  ('01a0a783-6000-7274-8766-66779b60527d', 'identity', 'customer_customers', '{note,discount_note}', 'last_activity_at', 1, 'seasons', 'null_fields', false, NULL, NULL, NULL, NULL, NULL, true, 'law', 'Art. 5.1.e e Art. 9 GDPR: campi liberi che possono contenere dati relativi alla salute. Scadono prima del resto dell''anagrafica che li contiene.'),
  ('01a0a783-6000-7806-9187-52bbfcd2d149', 'identity', 'customer_customers', '{}', 'last_activity_at', 10, 'years', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'law', 'Art. 2220 c.c.: cancellazione completa alla scadenza del termine decennale, quando il cliente e'' collegato a documenti fiscali.'),
  ('01a0a783-6000-7542-a0bb-34af3cc35e37', 'identity', 'access_users', '{}', 'deleted_at', 12, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: revocato l''accesso, l''identita'' dell''operatore non e'' piu'' necessaria; per l''audit fiscale e contabile e'' sufficiente il riferimento allo stabilimento. LIMITE: la decorrenza usa la cancellazione logica, valida finche'' la revoca dell''accesso coincide con essa.'),
  ('01a0a783-6000-7f7a-a9a8-fb716b59a2e0', 'identity', 'account_contract_acceptances', '{ip_address}', 'occurred_at', 24, 'months', 'anonymize', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.c GDPR: l''indirizzo IP prova l''accettazione e non e'' necessario oltre il periodo utile a fini probatori. Troncamento dell''ultimo ottetto; la riga resta per la durata del contratto piu'' dieci anni.'),
  ('01a0a783-6000-73e3-9cbd-b6287d3152ec', 'identity', 'customer_bathhouse_likes', '{}', 'liked_at', 24, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: preferenza dell''utente priva di utilita'' oltre un''inattivita'' prolungata o la chiusura dell''account.'),
  ('01a0a783-6000-7e34-8cd4-cbad0ff95deb', 'notification', 'deliveries', '{rendered_body}', 'sent_at', 150, 'days', 'null_fields', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.c GDPR: il corpo del messaggio contiene di norma dati identificativi nel testo. Finestra scelta per coprire l''intera stagione in cui l''invio e'' avvenuto, orizzonte entro cui puo'' arrivare una contestazione sulla mancata ricezione.'),
  ('01a0a783-6000-7a97-92ff-6f782be4d6da', 'notification', 'deliveries', '{}', 'sent_at', 12, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: i metadati d''invio (esito, data) non sono necessari oltre l''anno.'),
  ('01a0a783-6000-7899-a1d3-3c3708309384', 'notification', 'inapp_messages', '{}', 'created_at', 6, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: e'' un feed operativo del gestionale, non un archivio. Le letture collegate seguono per integrita'' referenziale.'),
  ('01a0a783-6000-74f0-8d17-05f5b7e68878', 'notification', 'marketing_campaign_recipients', '{}', 'sent_at', 6, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.c GDPR: elenco di destinatari congelato al momento dell''invio e privo di uso successivo. Il consenso va verificato all''invio, non su questo elenco.'),
  ('01a0a783-6000-7297-9cbe-d7dedc845e7a', 'notification', 'marketing_email_campaigns', '{}', 'sent_at', 24, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 7 GDPR e linee guida in materia di marketing: la definizione della campagna non e'' necessaria oltre ventiquattro mesi dall''ultimo utilizzo effettivo.'),
  ('01a0a783-6000-7a7a-9043-ea597be01906', 'notification', 'marketing_automations', '{}', 'last_run_at', 24, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 7 GDPR e linee guida in materia di marketing: decorrenza dall''ultima esecuzione effettiva, non dalla creazione dell''automazione.'),
  ('01a0a783-6000-7d89-8c94-20a9aaef65fe', 'notification', 'marketing_contact_lists', '{}', 'updated_at', 24, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 7 GDPR e linee guida in materia di marketing: la decorrenza e'' l''attivita'' della lista, non quella del singolo membro; i membri seguono per integrita'' referenziale. La lista di soppressione degli opt-out e'' esclusa da questa regola e si conserva a tempo indeterminato come prova della revoca.'),
  ('01a0a783-6000-7fb4-b164-09668ea8158d', 'notification', 'establishment_claim_requests', '{}', 'created_at', 12, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: contatto commerciale non trasformato in rapporto, privo di base per una conservazione prolungata.'),
  ('01a0a783-6000-74cd-8d6c-57c878b45e6a', 'switchboard', 'voice_call_records', '{}', 'started_at', 6, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR, minimizzazione dei dati di traffico. Termine scelto dal titolare (docs/82-retention-data.md 3.7). L''art. 132 del Codice Privacy (24 mesi telefonico / 12 telematico) e l''art. 24 della L. 167/2017 (72 mesi in deroga, per i reati degli artt. 51 c.3-quater e 407 c.2 lett. a c.p.p.) vincolano il fornitore di servizi di comunicazione elettronica accessibili al pubblico: SummerBooking non lo e'' - il traffico e'' fornito da Telnyx e non esiste autorizzazione generale AGCOM (verificato il 2026-09-30).'),
  ('01a0a783-6000-71c4-ab89-43209a6d2d14', 'switchboard', 'voice_call_diagnostics', '{}', 'created_at', 30, 'days', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Nessun obbligo di conservazione: diagnostica tecnica del flusso di chiamata, con finestra ridotta al minimo utile ad analizzare un malfunzionamento.'),
  ('01a0a783-6000-7e96-bbd8-16156b5b645b', 'switchboard', 'messaging_wa_event_logs', '{}', 'received_at', 6, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR, minimizzazione dei dati di traffico. Termine scelto dal titolare (docs/82-retention-data.md 3.7). L''art. 132 del Codice Privacy (24 mesi telefonico / 12 telematico) e l''art. 24 della L. 167/2017 (72 mesi in deroga, per i reati degli artt. 51 c.3-quater e 407 c.2 lett. a c.p.p.) vincolano il fornitore di servizi di comunicazione elettronica accessibili al pubblico: SummerBooking non lo e'' - il traffico e'' fornito da Telnyx e non esiste autorizzazione generale AGCOM (verificato il 2026-09-30).'),
  ('01a0a783-6000-79c3-8bce-6702c47a7307', 'chatbot', 'conversation_turns', '{}', 'created_at', 12, 'months', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: testo libero che puo'' contenere dati personali di terzi. Conservazione limitata al periodo utile all''analisi d''uso del servizio.'),
  ('01a0a783-6000-7600-b7c6-2139d40c9ff6', 'privacy', 'compliance_audit_logs', '{}', 'occurred_at', 10, 'years', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.2 GDPR (responsabilizzazione): registro delle azioni di retention eseguite, conservato per il tempo utile a dimostrare la conformita''. Il modulo applica a se'' stesso il meccanismo che offre agli altri.'),
  ('01a0a783-6000-7b63-9b27-671fbb942c7c', 'privacy', 'data_subject_requests', '{}', 'requested_at', 10, 'years', 'archive_cold', false, NULL, NULL, NULL, NULL, NULL, true, 'law', 'Art. 5.2 GDPR (responsabilizzazione): e'' la prova di aver dato seguito alle richieste degli interessati e non va minimizzato a zero.'),
  ('01a0ff0f-7c00-798c-9651-2349a0d61fd0', 'notification', 'media_assets', '{}', 'deleted_at', 2, 'days', 'delete_object', true, 'asset_unreferenced', NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR (limitazione della conservazione): un allegato non ha piu'' nessuna finalita'' quando nessun modello di messaggio e nessuna campagna lo usa. Le 48 ore fra l''ultimo riferimento rimosso e la rimozione dell''oggetto sono il margine per accorgersi di un errore, perche'' su storage la cancellazione non si annulla.'),
  ('01a0ff0f-7c00-7e41-a793-44d404da249d', 'catalog', 'media_assets', '{}', 'deleted_at', 2, 'days', 'delete_object', true, 'asset_unreferenced', NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR (limitazione della conservazione): foto rimosse dalla galleria, listini e menu'' sostituiti non hanno piu'' nessuna finalita'' quando nessuna riga li nomina. Le 48 ore fra l''ultimo riferimento rimosso e la rimozione dell''oggetto sono il margine per accorgersi di un errore, perche'' su storage la cancellazione non si annulla.'),
  ('01a0ff0f-7c00-7b2b-b48e-c3cc901fec74', 'fiskaly', 'media_assets', '{}', 'signed_at', 36, 'months', 'archive_object', false, NULL, NULL, NULL, '{"op": "in", "column": "kind", "values": ["fiscal_receipt_pdf", "non_fiscal_receipt_pdf"]}', NULL, false, 'recommended_policy', 'RITIRATA il 2026-10-06. Dichiarava signed_at come decorrenza, ma quella colonna sta su fiskaly.receipts e non su fiskaly.media_assets: la policy falliva a ogni corsa. Il PDF di uno scontrino segue ora la sua ricevuta - la policy archive_cold di fiskaly.receipts lo accoda e lo inventaria con linked_asset - e la policy decennale su privacy.archive_manifests cancella archivio e PDF insieme. Non e'' stata sostituita da una policy only_if_unreferenced di proposito: dopo l''archiviazione il PDF non e'' nominato da nessuno, e quella regola lo cancellerebbe al mese 36 invece che all''anno 10.'),
  ('01a0ff0f-7c00-7aaa-8f19-b213e077fc18', 'fiskaly', 'media_assets', '{}', 'signed_at', 10, 'years', 'delete_object', false, NULL, NULL, NULL, '{"op": "in", "column": "kind", "values": ["fiscal_receipt_pdf", "non_fiscal_receipt_pdf"]}', NULL, false, 'law', 'RITIRATA il 2026-10-06. Dichiarava signed_at come decorrenza, ma quella colonna sta su fiskaly.receipts e non su fiskaly.media_assets: la policy falliva a ogni corsa. Il PDF di uno scontrino segue ora la sua ricevuta - la policy archive_cold di fiskaly.receipts lo accoda e lo inventaria con linked_asset - e la policy decennale su privacy.archive_manifests cancella archivio e PDF insieme. Non e'' stata sostituita da una policy only_if_unreferenced di proposito: dopo l''archiviazione il PDF non e'' nominato da nessuno, e quella regola lo cancellerebbe al mese 36 invece che all''anno 10.'),
  ('01a0ff0f-7c00-74ce-9164-b43d9c90ee42', 'notification', 'media_assets', '{}', 'created_at', 24, 'months', 'delete_object', false, NULL, NULL, NULL, '{"op": "=", "value": "rendered_pdf", "column": "kind"}', NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: il PDF del report periodico inviato a una struttura partner contiene nomi di ospiti e non e'' necessario oltre ventiquattro mesi; il dato grezzo da cui e'' ricostruibile resta nelle prenotazioni per il proprio termine. VINCOLO OPERATIVO: si applica ai soli asset con kind = rendered_pdf, e la decorrenza e'' created_at perche'' il PDF viene archiviato nello stesso momento in cui il report e'' generato.'),
  ('01a0ff0f-7c00-7aa1-8d67-5cf79a9d7325', 'switchboard', 'media_assets', '{}', '', 0, 'days', 'delete_object', false, 'property_ceased', NULL, NULL, '{"scope": "whole_table"}', NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: i messaggi vocali del centralino sono configurazione dello stabilimento e non hanno nessuna finalita'' quando lo stabilimento cessa. Cessano con la configurazione, senza attesa. VINCOLO OPERATIVO: la cessazione non e'' uno stato a database, si deriva da identity.account_subscriptions (cancelled, oppure expired senza rinnovo successivo) e la data e'' closed_at dell''ultimo periodo.'),
  ('01a0ff0f-7c00-7458-a66d-67207436a34b', 'catalog', 'media_assets', '{}', '', 24, 'months', 'delete_object', false, 'property_ceased', NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: alla cessazione dello stabilimento la scheda pubblica resta in forma ridotta (copertina e prime tre foto per sort_order, conservate senza termine), mentre le altre foto e gli sfondi di mappa seguono lo snapshot di configurazione e si rimuovono due anni dopo, finestra scelta per consentire il rientro del cliente. VINCOLO OPERATIVO: la cessazione non e'' uno stato a database, si deriva da identity.account_subscriptions e la data e'' closed_at dell''ultimo periodo.'),
  ('01a0ff0f-7c00-742e-b851-9e7200a3b5f3', 'notification', '', '{}', '', 2, 'seasons', 'delete_object', true, NULL, 'images/qrcodescan/', 'object_last_modified', NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: i QR delle prenotazioni seguono la retention delle prenotazioni a cui si riferiscono e sono rigenerabili dal codice di prenotazione, quindi la rimozione non perde nessuna informazione. VINCOLO OPERATIVO: la soglia e'' il 1 gennaio dell''anno N-2, ricalcolata a ogni esecuzione, e la data dell''oggetto e'' il suo LastModified perche'' nessuna riga del database nomina questi file.'),
  ('01a0ff0f-7c00-769a-b9a8-493c0c1c71cf', 'catalog', '', '{}', '', 0, 'days', 'delete_object', true, NULL, 'images/bathouse/', 'object_last_modified', NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: oggetti che nessuna riga nomina piu''. Non sono materiale che qualcuno ha scelto di conservare, sono cancellazioni che il gestionale precedente ha tentato e non ha portato a termine, perche'' chiedeva la rimozione senza attenderne l''esito e ricostruendo male la chiave. VINCOLO OPERATIVO: da attivare solo dopo il recupero delle foto di properties.photos, altrimenti rimuove file che il recupero avrebbe rimesso in gioco.'),
  ('01a0ff0f-7c00-7e08-8a68-b4d9abbfef78', 'catalog', '', '{}', '', 0, 'days', 'delete_object', true, NULL, 'images/configure/', 'object_last_modified', NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: sfondi di mappa e icone di ombrellone sostituiti dai gestori, che nessuna riga nomina piu''. VINCOLO OPERATIVO: in questo prefisso convivono due cose diverse - il catalogo globale delle icone (configure_images), che non si cancella mai, e i caricamenti del singolo stabilimento. Il job deve escludere sia cio'' che e'' referenziato sia cio'' che e'' nel catalogo, comprese le voci con status = false, che un flag puo'' riportare in uso.'),
  ('01a0ff0f-7c00-7fc5-aa31-3ed4304c2d04', 'catalog', '', '{}', '', 0, 'days', 'delete_object', true, NULL, 'images/property/', 'object_last_modified', NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: foto di stabilimento caricate sul percorso della property e non piu'' nominate da nessuna riga. Stessa natura dei file di images/bathouse/: cancellazioni tentate e non portate a termine dal gestionale precedente. VINCOLO OPERATIVO: da attivare solo dopo il recupero delle foto di properties.photos.'),
  ('01a0ff0f-7c00-7bc4-8f67-e9f29da09da7', 'catalog', '', '{}', '', 0, 'days', 'delete_object', true, NULL, 'pdf/', 'object_last_modified', NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: listini e menu'' sostituiti con una versione nuova, che nessuna riga nomina piu''. Il prezzo applicato a una prenotazione e'' congelato dentro la prenotazione, quindi il documento sostituito non serve a ricostruire nulla.'),
  ('01a0ff0f-7c00-79ea-b880-203fdabd8b63', 'notification', '', '{}', '', 0, 'days', 'delete_object', true, NULL, 'file/', 'object_last_modified', NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: allegati di campagne e automazioni che non esistono piu''. Le righe che li nominavano sono state cancellate negli anni e l''oggetto e'' rimasto.'),
  ('01a0ff0f-7c00-7677-8866-b74a7aa85a18', 'notification', '', '{}', '', 0, 'days', 'delete_object', true, NULL, 'files[]/', 'object_last_modified', NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: allegati di campagne che non esistono piu''. Il nome del prefisso e'' un difetto del gestionale precedente, che ha scritto in chiaro il nome di una variabile di array; va trattato come prefisso letterale.'),
  ('01a0ff0f-7c00-7600-a624-f21217963a34', 'notification', '', '{}', '', 0, 'days', 'delete_object', true, NULL, 'files/newsletter/', 'object_last_modified', NULL, NULL, true, 'recommended_policy', 'Art. 5.1.e GDPR: allegati di newsletter cancellate, che nessuna riga nomina piu''.'),
  ('01a1095c-3400-7d5d-94fd-296a00f6d98c', 'privacy', 'retention_runs', '{}', 'finished_at', 10, 'years', 'delete_row', false, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.2 GDPR (responsabilizzazione): verbale delle corse di retention, una riga per policy per esecuzione, con le righe trovate, quelle toccate, la destinazione dell''archivio e l''eventuale errore. Conservato DIECI ANNI, quanto il piu'' lungo dei termini che deve poter provare (conservazione decennale dei documenti fiscali): una prova che scade prima dell''obbligo che la motiva non prova niente. Non contiene dati personali, solo conteggi e nomi di tabella, quindi il termine lungo non allunga la vita di nessun dato.'),
  ('01a11f2e-7b00-7a1c-9f3d-4e5b6a7c8d90', 'privacy', 'archive_manifests', '{}', 'archived_at', 10, 'years', 'delete_object', true, NULL, NULL, NULL, NULL, NULL, true, 'recommended_policy', 'Art. 5.1(e) GDPR (limitazione della conservazione) applicato agli ARCHIVI: archiviare non e'' cancellare, i dati personali dentro un archivio a oggetti sono ancora dati personali e devono avere un termine. Dieci anni dalla scrittura dell''archivio, il piu'' lungo fra gli obblighi che motivano un''archiviazione in questo progetto (conservazione decennale dei documenti fiscali: scontrini e corrispettivi, art. 2220 c.c. e normativa IVA). L''azione rimuove l''OGGETTO dallo storage e NON la riga di inventario, che resta con purged_at valorizzato come prova di averlo rimosso nei termini.');
""");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "privacy_archive_manifests",
                schema: "privacy");

            migrationBuilder.DropTable(
                name: "privacy_compliance_audit_logs",
                schema: "privacy");

            migrationBuilder.DropTable(
                name: "privacy_data_subject_request_module_outcomes",
                schema: "privacy");

            migrationBuilder.DropTable(
                name: "privacy_file_deletions",
                schema: "privacy");

            migrationBuilder.DropTable(
                name: "privacy_legal_holds",
                schema: "privacy");

            migrationBuilder.DropTable(
                name: "privacy_retention_runs",
                schema: "privacy");

            migrationBuilder.DropTable(
                name: "privacy_data_subject_requests",
                schema: "privacy");

            migrationBuilder.DropTable(
                name: "privacy_retention_policies",
                schema: "privacy");
        }
    }
}
