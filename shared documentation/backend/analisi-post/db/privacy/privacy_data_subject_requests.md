# Tabella `privacy_data_subject_requests`

> **Dominio**: `Module.Privacy` — `privacy` · **sottoarea**: — (modulo sottile, nessuna sottoarea) ·
> **schema**: `privacy` · **nome flat**: `privacy_data_subject_requests` · **forma canonica**:
> `privacy.data_subject_requests`.
>
> Tabella **nuova** (nessuna legacy diretta: nel legacy la "cancellazione account" era un soft-delete
> anonimizzante silenzioso, senza traccia della richiesta — vedi
> [`analisi-pre/business-logic/da-chiarire.md` #47](../../../analisi-pre/business-logic/da-chiarire.md)).
> Fonti: [`docs/11-modulo-privacy.md` §2, §5](../../../progettazione/11-modulo-privacy.md) (aggregati
> `ErasureRequest`/`ExportRequest`, flusso Art. 15/17/20),
> [`docs/03-gestione-dati.md` §3.1ter (id pubblico su endpoint GDPR), §3.6 (snapshot)](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §7 ("Registro richieste GDPR")](../../../progettazione/80-retention.md).
>
> **Nota sulla voce "Legacy"**: `— (nuovo)` = campo/tabella senza corrispondente legacy diretto.

---

## Descrizione

**Testata di una richiesta dell'interessato** (Art. 15/17/20 GDPR): cancellazione (`erasure`) o
accesso/portabilità (`export`). È l'aggregato radice del **process manager/saga in-process** descritto in
[`docs/11-modulo-privacy.md` §5](../../../progettazione/11-modulo-privacy.md): alla creazione pubblica un evento di
dominio (`CustomerErasureRequested`/`CustomerExportRequested`), ogni modulo coinvolto (Booking, Payment,
Notification, Switchboard, Identity, …) applica la propria regola e risponde con un esito registrato in
[`privacy_data_subject_request_module_outcomes`](privacy_data_subject_request_module_outcomes.md); quando
tutti i moduli hanno risposto, questa riga si consolida in `completed`/`partially_completed`/`rejected`.

**Regola cardine del modulo** (doc §2): questa tabella **non contiene mai un dato personale** del
soggetto (niente nome/email/telefono) — solo identificativi e metadati di workflow. Chi è l'interessato è
un **riferimento per valore** (`subject_kind` + `subject_id`, il `public_id` copiato dall'entità nello
schema che la possiede), mai una copia dei suoi dati.

**Scostamento dal documento di indirizzo — testata unica invece di due aggregati.** Il documento elenca
`ErasureRequest` ed `ExportRequest` come due aggregati distinti, ma nota esplicitamente che condividono
**lo stesso meccanismo** ("Lo stesso meccanismo copre l'export", doc §5). Coerente con il precedente già
adottato nel progetto per casi identici — testata unica + discriminatore invece di due tabelle
quasi-gemelle (`booking_reservations.reservation_kind`, `fiskaly_receipts.document_kind` — D-38) — qui si
usa **una tabella con `request_kind`** invece di due. Vedi [D-59](../_domande-committente.md). Correzione
da riportare in `docs/11-modulo-privacy.md` §2.

## Verifica di esclusività

- **`request_kind`**: colonna singola (enum), non due tabelle parallele — la mutua esclusività ("una
  richiesta è o cancellazione o export, mai entrambe") è garantita per costruzione dal tipo, non da una
  convenzione applicativa.
- **`subject_kind`**: colonna singola (enum) accanto a `subject_id` — stesso pattern già adottato per
  `resource_type`/`resource_id` in C1 (`booking_resource_consumptions`, `booking_spot_holds`,
  [`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md)): un soggetto è di un solo tipo,
  l'enum lo garantisce senza bisogno di due colonne FK nullable alternative.
- **`status`**: colonna singola (enum) — nessun gruppo di flag booleani paralleli.

## Snapshot

**Non applicabile per scelta esplicita del modulo** (doc §2: "Non contiene mai una riga di dato personale
di un cliente/utente"). A differenza di altre schede di dominio (es. `booking_lines.*_snapshot`), qui
**nessun campo congela nome/email/telefono** del soggetto: la tabella porta solo `subject_id` (un
riferimento vivo per valore) e metadati di processo. Il contenuto congelato della richiesta di export
(l'insieme effettivo dei dati restituiti) non vive a DB come snapshot ma come **file** (vedi
`export_artifact_*` sotto e [D-61](../_domande-committente.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_privacy_data_subject_requests` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | id esposto nell'URL (`GET /api/customers/{id}/erasure-requests/{requestId}`) |
| `ix_..._subject` | `(subject_kind, subject_id)` | — | "tutte le richieste di un soggetto" (storico, verifica duplicati) |
| `ix_..._property` | `(property_id) WHERE property_id IS NOT NULL` | parziale | dashboard/filtro staff per stabilimento |
| `ix_..._open` | `(status) WHERE status IN ('pending','in_progress')` | parziale | coda di lavorazione del saga/orchestratore |
| `ix_..._due` | `(due_at) WHERE status IN ('pending','in_progress')` | parziale | job di alert sulla scadenza Art. 12.3 GDPR (1 mese, estendibile a 3) |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica interna allo schema `privacy`.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `request_id` di
  [`privacy_data_subject_request_module_outcomes`](privacy_data_subject_request_module_outcomes.md) e
  (opzionale) `data_subject_request_id` di
  [`privacy_compliance_audit_logs`](privacy_compliance_audit_logs.md).

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico esposto nell'URL della richiesta (endpoint GDPR:
  `.../erasure-requests/{requestId}`, `.../personal-data-export`) e nella UI self-service del cliente.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: [`docs/03-gestione-dati.md` §3.1ter](../../../progettazione/03-gestione-dati.md) elenca
  esplicitamente "gli endpoint GDPR" tra i casi che richiedono `public_id`.

### request_kind
- **Legacy**: — (nuovo)
- **Descrizione**: tipo di richiesta.
- **Tipo**: `enum privacy.request_kind` — valori: `erasure` (Art. 17), `export` (Art. 15/20).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: vedi "Scostamento dal documento di indirizzo" sopra.

### subject_kind
- **Legacy**: — (nuovo)
- **Descrizione**: tipo di entità interessata dalla richiesta.
- **Tipo**: `enum privacy.subject_kind` — valori: `customer` (cliente finale, rubrica C5), `user` (account
  di login C5 — titolare/staff).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: discrimina il tipo referenziato da `subject_id`.
- **Note**: tutti gli esempi del documento di indirizzo (doc §5, endpoint) riguardano solo `customer`.
  `user` è seminato fin da ora (stesso pattern di [D-55]/[D-57]) per il caso — non ancora coperto da
  requisiti — di uno staff/titolare che richieda i propri dati. Vedi [D-60](../_domande-committente.md).

### subject_id
- **Legacy**: — (nuovo)
- **Descrizione**: l'interessato. **`public_id` copiato per valore** dall'entità nello schema che la
  possiede — mai una FK cross-schema, mai una copia dei suoi dati personali.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: se `subject_kind = customer` → `public_id` di
  [`identity_customer_customers`](../identity/identity_customer_customers.md); se `subject_kind = user` →
  `public_id` di [`identity_access_users`](../identity/identity_access_users.md). Riferimento per valore,
  coerente con [`docs/03-gestione-dati.md` §3.1ter](../../../progettazione/03-gestione-dati.md).

### property_id
- **Legacy**: — (nuovo)
- **Descrizione**: stabilimento del soggetto al momento della richiesta (copiato da
  `identity_customer_customers.property_id`, o dallo staff se `subject_kind = user`), usato per lo scoping
  dell'accesso staff alla richiesta nel pannello e per gli indici di filtro.
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)).
- **Default**: `NULL`.
- **Nullabile**: Sì — `NULL` per account `user` di livello piattaforma (`superadmin`, non legati a una
  property).
- **Relazioni**: riferimento cross-modulo a C2 per valore.

### status
- **Legacy**: — (nuovo)
- **Descrizione**: stato di avanzamento della richiesta.
- **Tipo**: `enum privacy.request_status` — valori: `pending`, `in_progress` (almeno un modulo ha
  risposto, altri in attesa), `completed`, `partially_completed` (alcuni moduli hanno trattenuto il dato
  per obbligo legale/legal hold), `rejected`.
- **Default**: `pending`.
- **Nullabile**: No.
- **Relazioni**: consolidato dal saga leggendo
  [`privacy_data_subject_request_module_outcomes`](privacy_data_subject_request_module_outcomes.md) —
  vedi [`docs/11-modulo-privacy.md` §5 punto 3](../../../progettazione/11-modulo-privacy.md).

### requested_by_kind
- **Legacy**: — (nuovo)
- **Descrizione**: chi ha originato la richiesta.
- **Tipo**: `enum privacy.requester_kind` — valori: `data_subject` (self-service, l'interessato ha un
  account e usa l'endpoint pubblico), `staff` (personale dello stabilimento inserisce a nome del cliente,
  es. richiesta telefonica/email), `system_admin`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### requested_by_user_id
- **Legacy**: — (nuovo)
- **Descrizione**: account che ha materialmente inserito la richiesta.
- **Tipo**: `uuid` (= `public_id` di [`identity_access_users`](../identity/identity_access_users.md)).
- **Default**: `NULL`.
- **Nullabile**: Sì — `NULL` se la richiesta arriva da un canale senza account (es. import da form legale
  esterno).
- **Relazioni**: riferimento cross-modulo a C5 per valore. Se `requested_by_kind = data_subject`, coincide
  con l'account collegato al `subject_id` (self-service); se `staff`/`system_admin`, è un account
  **diverso** dal soggetto.

### requested_at
- **Legacy**: — (nuovo)
- **Descrizione**: data/ora di creazione della richiesta — decorrenza del termine Art. 12.3.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### due_at
- **Legacy**: — (nuovo)
- **Descrizione**: scadenza per rispondere alla richiesta (Art. 12.3 GDPR: un mese, estendibile a tre per
  richieste complesse — questo caso, cross-modulo, lo è).
- **Tipo**: `timestamptz`.
- **Default**: nessuno (valorizzato dall'applicazione all'`INSERT`: `requested_at + 30 giorni`; estendibile
  a `+90 giorni` con motivazione, da un'azione esplicita, non dal job).
- **Nullabile**: No.
- **Relazioni**: —.

### completed_at
- **Legacy**: — (nuovo)
- **Descrizione**: data/ora in cui `status` è diventato `completed`/`partially_completed`/`rejected`.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### rejection_reason
- **Legacy**: — (nuovo)
- **Descrizione**: motivazione quando `status = rejected` (es. identità del richiedente non verificabile).
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì — valorizzato solo per `status = rejected`.
- **Relazioni**: —.

### export_artifact_origin
- **Legacy**: — (nuovo)
- **Descrizione**: sorgente di storage del file di export generato (solo `request_kind = export`,
  `status = completed`). Stesso pattern di `<dominio>_media_assets.origin`
  ([`docs/03-gestione-dati.md` §3.1quater.A](../../../progettazione/03-gestione-dati.md)), qui inline (un solo
  "tipo di file" in questo modulo, non serve una tabella dedicata).
- **Tipo**: `enum privacy.export_artifact_origin` — valori: `s3private` (bucket privato, mai pubblico:
  l'accesso è solo via presigned URL a TTL breve generato a runtime, mai salvato a DB).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: vedi [D-61](../_domande-committente.md) (file su object storage vs `jsonb` inline).

### export_artifact_path
- **Legacy**: — (nuovo)
- **Descrizione**: percorso del file di export nel bucket privato.
- **Tipo**: `varchar(1024)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### export_artifact_expires_at
- **Legacy**: — (nuovo)
- **Descrizione**: scadenza del file fisico sul bucket (lifecycle policy S3) — il bundle di export non
  vive a tempo indeterminato, a differenza della riga di richiesta (che resta come prova, §7 di
  [`docs/80-retention.md`](../../../progettazione/80-retention.md)).
- **Tipo**: `timestamptz`.
- **Default**: `NULL` (valorizzato alla generazione: proposta 30 giorni).
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: dopo la scadenza il file va rimosso dal bucket (lifecycle rule nativa S3) ma la riga resta
  come prova che l'export è stato prodotto e consegnato.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione della riga (coincide con `requested_at`) / ultima transizione di stato.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: presente per uniformità di schema, **non usato** funzionalmente.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: come [`identity_account_contract_acceptances`](../identity/identity_account_contract_acceptances.md):
  è un registro di conformità (prova, [`docs/80-retention.md` §7](../../../progettazione/80-retention.md): "Registro
  richieste GDPR — permanente, ~10 anni"), non si soft-cancella nell'uso normale. La rimozione avviene solo
  per hard-delete di retention a scadenza del termine, tramite la stessa
  [`privacy_retention_policies`](privacy_retention_policies.md) del modulo (auto-riferimento: il modulo
  applica a sé stesso lo stesso meccanismo dichiarativo che offre agli altri).

## Valutazione JSON

Nessun JSON. Il contenuto effettivo dell'export (l'aggregazione dei dati dai vari moduli) **non** è un
`jsonb` a DB — vive come file (`export_artifact_*`), per evitare una colonna di dimensione imprevedibile
(un cliente con anni di prenotazioni/pagamenti può generare un export non piccolo) e per riusare il
pattern "file esterno, riferimento tipizzato a DB" già adottato ovunque nel progetto (§1 di
[`docs/03-gestione-dati.md`](../../../progettazione/03-gestione-dati.md)). Vedi [D-61](../_domande-committente.md).

## Suggerimenti per le prestazioni

- Volume atteso basso: richieste Art. 15/17/20 sono un evento raro per soggetto anche a piena scala
  (~500 stabilimenti); ordine di grandezza decine/centinaia l'anno, non migliaia.
- L'indice parziale su `status IN ('pending','in_progress')` mantiene piccola la working set su cui
  l'orchestratore e il job di alert scadenze lavorano, anche quando la tabella cresce nel tempo (righe
  `completed` restano per anni come prova, §7 retention).
- Nessun bisogno di partizionamento al volume atteso.

## Implementazione .NET

**Aggregate root** `DataSubjectRequest`, modulo `Module.Privacy`, schema `privacy`. Testata del
process manager/saga in-process descritto in `docs/11-modulo-privacy.md` §5: alla creazione (factory
statica `RequestErasure`/`RequestExport`) solleva `CustomerErasureRequested`/`CustomerExportRequested`
(`AggregateRoot`/`Raise`, §1 del pattern comune); riceve `ErasureAppliedInModule`/`ExportProducedInModule`
da ogni modulo coinvolto e aggiorna gli esiti figli in
[`privacy_data_subject_request_module_outcomes`](privacy_data_subject_request_module_outcomes.md);
consolida `Status` quando tutti gli esiti attesi sono arrivati.

`IAuditable` (`created_at`/`updated_at` gestiti da `AuditInterceptor`). **Non** implementa
`ISoftDeletable`: `deleted_at` è presente in colonna per uniformità di schema ma non è il meccanismo di
cancellazione di questa tabella (la riga è prova di conformità e resta anche dopo `completed` — la sua
eventuale rimozione è un **hard-delete** a scadenza retention, guidato da
[`privacy_retention_policies`](privacy_retention_policies.md), non un `UPDATE deleted_at`). Non essendo
`ISoftDeletable`, l'entità non entra nel query filter globale riflessivo (§1) né nell'`SoftDeleteInterceptor`.

### Enum propri

```csharp
// Domain/Enums/RequestKind.cs
public enum RequestKind { Erasure = 0, Export = 1 }

// Domain/Enums/SubjectKind.cs
public enum SubjectKind { Customer = 0, User = 1 }

// Domain/Enums/RequestStatus.cs
public enum RequestStatus { Pending = 0, InProgress = 1, Completed = 2, PartiallyCompleted = 3, Rejected = 4 }

// Domain/Enums/RequesterKind.cs
public enum RequesterKind { DataSubject = 0, Staff = 1, SystemAdmin = 2 }

// Domain/Enums/ExportArtifactOrigin.cs
public enum ExportArtifactOrigin { S3Private = 0 }
```

```csharp
// PrivacyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<RequestKind>("privacy", "request_kind");
modelBuilder.HasPostgresEnum<SubjectKind>("privacy", "subject_kind");
modelBuilder.HasPostgresEnum<RequestStatus>("privacy", "request_status");
modelBuilder.HasPostgresEnum<RequesterKind>("privacy", "requester_kind");
modelBuilder.HasPostgresEnum<ExportArtifactOrigin>("privacy", "export_artifact_origin");
```

```csharp
// Registrazione data source (Module.cs / Infrastructure DI)
dataSourceBuilder.MapEnum<RequestKind>("privacy.request_kind");
dataSourceBuilder.MapEnum<SubjectKind>("privacy.subject_kind");
dataSourceBuilder.MapEnum<RequestStatus>("privacy.request_status");
dataSourceBuilder.MapEnum<RequesterKind>("privacy.requester_kind");
dataSourceBuilder.MapEnum<ExportArtifactOrigin>("privacy.export_artifact_origin");
```

### Configuration

```csharp
namespace Module.Privacy.Infrastructure.Configurations;

public sealed class DataSubjectRequestConfiguration : IEntityTypeConfiguration<DataSubjectRequest>
{
	public void Configure(EntityTypeBuilder<DataSubjectRequest> builder)
	{
		builder.ToTable("privacy_data_subject_requests", "privacy", t => t.HasComment(
			"Testata di una richiesta dell'interessato (Art. 15/17/20 GDPR): cancellazione o export. " +
			"Non contiene mai un dato personale del soggetto, solo riferimenti e metadati di workflow."));

		builder.HasKey(x => x.Id).HasName("pk_privacy_data_subject_requests");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico esposto nell'URL della richiesta e nella UI self-service del cliente.");

		builder.Property(x => x.RequestKind)
			.HasColumnName("request_kind")
			.HasComment("Tipo di richiesta: erasure (Art. 17) o export (Art. 15/20).");

		builder.Property(x => x.SubjectKind)
			.HasColumnName("subject_kind")
			.HasComment("Tipo di entità interessata: customer (rubrica C5) o user (account di login C5).");

		builder.Property(x => x.SubjectId)
			.HasColumnName("subject_id")
			.HasComment("public_id copiato per valore dell'interessato (identity_customer_customers o " +
				"identity_access_users secondo subject_kind) — nessuna FK cross-schema, nessun dato personale.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties al momento della richiesta, copiato — NULL per " +
				"account user di livello piattaforma; nessuna FK cross-schema.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasDefaultValue(RequestStatus.Pending)
			.HasComment("Stato di avanzamento, consolidato dal saga leggendo gli esiti per modulo.");

		builder.Property(x => x.RequestedByKind)
			.HasColumnName("requested_by_kind")
			.HasComment("Chi ha originato la richiesta: data_subject (self-service), staff o systemadmin.");

		builder.Property(x => x.RequestedByUserId)
			.HasColumnName("requested_by_user_id")
			.HasComment("public_id di identity_access_users che ha inserito la richiesta, copiato — NULL se " +
				"da canale senza account; nessuna FK cross-schema.");

		builder.Property(x => x.RequestedAt)
			.HasColumnName("requested_at")
			.HasDefaultValueSql("now()")
			.HasComment("Data/ora di creazione della richiesta — decorrenza del termine Art. 12.3.");

		builder.Property(x => x.DueAt)
			.HasColumnName("due_at")
			.HasComment("Scadenza per rispondere (Art. 12.3: 30 giorni, estendibile a 90 con motivazione " +
				"esplicita), valorizzata dall'applicazione all'inserimento.");

		builder.Property(x => x.CompletedAt)
			.HasColumnName("completed_at")
			.HasComment("Data/ora in cui lo stato è diventato completed, partially_completed o rejected.");

		builder.Property(x => x.RejectionReason)
			.HasColumnName("rejection_reason")
			.HasColumnType("text")
			.HasComment("Motivazione quando status = rejected (es. identità del richiedente non verificabile).");

		builder.Property(x => x.ExportArtifactOrigin)
			.HasColumnName("export_artifact_origin")
			.HasComment("Sorgente di storage del file di export generato (solo request_kind = export, " +
				"status = completed); oggi solo s3private, accesso via presigned URL mai persistito.");

		builder.Property(x => x.ExportArtifactPath)
			.HasColumnName("export_artifact_path")
			.HasMaxLength(1024)
			.HasComment("Percorso del file di export nel bucket privato.");

		builder.Property(x => x.ExportArtifactExpiresAt)
			.HasColumnName("export_artifact_expires_at")
			.HasComment("Scadenza del file fisico sul bucket (lifecycle policy S3); la riga resta come " +
				"prova anche dopo la rimozione del file.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga, coincide con requested_at.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultima transizione di stato.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Presente per uniformità di schema, non usato funzionalmente: la riga è un " +
				"registro di conformità permanente; l'unica rimozione è un hard-delete a scadenza retention.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_privacy_data_subject_requests_public_id");

		builder.HasIndex(x => new { x.SubjectKind, x.SubjectId })
			.HasDatabaseName("ix_privacy_data_subject_requests_subject");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("property_id IS NOT NULL")
			.HasDatabaseName("ix_privacy_data_subject_requests_property");

		builder.HasIndex(x => x.Status)
			.HasFilter("status IN ('pending','in_progress')")
			.HasDatabaseName("ix_privacy_data_subject_requests_open");

		builder.HasIndex(x => x.DueAt)
			.HasFilter("status IN ('pending','in_progress')")
			.HasDatabaseName("ix_privacy_data_subject_requests_due");

		// Collezione ModuleOutcomes: FK reale, configurata lato figlio
		// (privacy_data_subject_request_module_outcomes.request_id) — vedi quella scheda.
	}
}
```

### Relazioni

- `subject_id`, `property_id`, `requested_by_user_id`: riferimenti cross-modulo per valore (`Guid` copiato
  da identity/catalog), solo `.HasComment`, nessuna FK EF.
- `ModuleOutcomes` (figli in [`privacy_data_subject_request_module_outcomes`](privacy_data_subject_request_module_outcomes.md)):
  FK reale interna allo schema `privacy`, configurata lato figlio con `HasOne(x => x.Request).WithMany(r =>
  r.ModuleOutcomes).HasForeignKey(x => x.RequestId)`.
- Riferimento opzionale da [`privacy_compliance_audit_logs`](privacy_compliance_audit_logs.md) via
  `data_subject_request_id` — FK reale interna allo schema, configurata lato log.

### Note di migrazione

Tabella **nuova**: nessun dato legacy da migrare (0 righe all'avvio). Il comportamento legacy che questa
entità sostituisce — soft-delete anonimizzante silenzioso in fase di cancellazione account, senza traccia
della richiesta — non lasciava alcun dato migrabile (`analisi-pre/business-logic/da-chiarire.md` #47).
`request_kind` unifica in una sola tabella quanto il documento di indirizzo descriveva come due aggregati
(`ErasureRequest`/`ExportRequest`): nessuna migration di dati, solo la definizione dello schema.
