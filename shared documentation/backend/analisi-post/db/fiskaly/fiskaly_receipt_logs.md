# Tabella `fiskaly_receipt_logs`

> **Dominio**: C4 — `fiskaly` · **sottoarea**: — · **schema**: `fiskaly` · **nome flat**:
> `fiskaly_receipt_logs` · **forma canonica**: `fiskaly.receipt_logs`.
>
> Sostituisce [`fiskaly_receipt_log`](../../../analisi-pre/db/fiskaly_receipt_log.md) (8.063 righe;
> ~3 per scontrino). Fonti:
> [`analisi-pre/db/fiskaly_receipt_log.md`](../../../analisi-pre/db/fiskaly_receipt_log.md),
> [`docs/03-gestione-dati.md` §2.4ter, §3.5 (ultimo capoverso — log senza `deleted_at`), §3.6 punto 5](../../../progettazione/03-gestione-dati.md),
> [`docs/15-logs.md`](../../../progettazione/15-logs.md), [`docs/80-retention.md`](../../../progettazione/80-retention.md).

---

## Descrizione

**Tabella di puro log append-only** del flusso di emissione/annullamento dello scontrino elettronico:
N righe per [`fiskaly_receipts`](fiskaly_receipts.md), una per transizione di stato o errore nel
dialogo con l'API SIGN IT. Serve a diagnosticare i fallimenti (l'API ha molti stati intermedi e casi
di *resume*). Nessun dato economico né di business: solo audit tecnico.

Correzioni rispetto al legacy:

- **`fiskaly_error_message varchar(50)`** (troncato — spesso insufficiente) → `text`.
- **`updatedAt` inutile** (sempre = `createdAt` su tabella append-only) → **non migrato**; resta un
  `updated_at`/`deleted_at` **esposti per uniformità ma non usati** ([`docs/03 §3.5`](../../../progettazione/03-gestione-dati.md)).
- **Vocabolari come `varchar`** (`state`, `result`, `fiskaly_record_state`, `fiskaly_error_code`) →
  `enum` PG; la costante `FiskalyReceiptState` (disallineata) è da rifare
  ([`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)).
- **`fiskaly_log_messages` JSON** → `jsonb` passthrough (schema di terzi, §3.6 punto 5).
- **`createdBy int` senza FK** → `actor_user_id uuid` ([`docs/03 §3.2`](../../../progettazione/03-gestione-dati.md)).
- **Nessuna retention** (`AUTO_INCREMENT` in crescita illimitata) → pruning a **12 mesi** ✅
  (valore validato dal titolare — estremo inferiore del range 12–24 proposto,
  [`docs/82-retention-data.md` §3.3](../../../progettazione/82-retention-data.md)) + partizionamento per mese.
- **`booking_number` + `property_id` ridondanti** (copiati dallo scontrino) → mantenuti come
  denormalizzazione **documentata** per filtrare i log senza join.

## Verifica di esclusività

| Gruppo | Campi | Esito |
|---|---|---|
| Passo del flusso | `step` | **enum** esclusivo (un solo passo per riga di log). |
| Esito del passo | `result` | **enum** esclusivo: `ok` / `ko` (equivalente a un `bool is_ok`; enum per leggibilità in pgAdmin). |
| Stato Fiskaly grezzo del passo | `fiskaly_record_state` | **enum** esclusivo: `accepted` / `rejected` / `completed` / `failed`. |

## Snapshot

Nessun `_snapshot`: `property_id` e `booking_number` sono **denormalizzazioni per query** (business
key, regola C3-8), non "fotografie" di un'entità mutabile. `fiskaly_log_payload` è `jsonb` grezzo di
sistema esterno (§3.6 punto 5), niente suffisso.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiskaly_receipt_logs` | `id` | PK | — |
| `ix_fiskaly_receipt_logs_receipt` | `(receipt_id, created_at)` | btree | timeline dei log di uno scontrino (dettaglio gestionale) |
| `ix_fiskaly_receipt_logs_property_date` | `(property_id, created_at DESC)` | btree | log per stabilimento ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): a DB mancava perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo. |
| `ix_fiskaly_receipt_logs_errors` | `(property_id, created_at DESC) WHERE result = 'ko'` | btree parziale | monitoraggio errori SIGN IT (~2% dei tentativi) |


> ### ✅ Applicato nel backend (2026-09-21)
>
> Indice **BRIN** su `created_at` ([`docs/05-ottimizzazioni-db.md` §3](../../../progettazione/05-ottimizzazioni-db.md)), migration
> `AddBrinTimeIndexes`. Serve alla scansione del job di retention (12 mesi flat): nessuna query applicativa filtra su questa colonna da sola.
>
> BRIN e non B-tree perché la tabella è in **solo append** e l'ordine fisico dei blocchi coincide già
> con l'ordine temporale: l'indice memorizza un intervallo per blocco invece di una voce per riga, e
> costa una frazione in spazio e in manutenzione. Non sostituisce gli indici B-tree esistenti, che
> servono query diverse (tutte con un'altra colonna come prefisso).

## Campi

### id
- **Legacy**: `fiskaly_receipt_log.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### receipt_id
- **Legacy**: `fiskaly_receipt_log.receipt_id` (`int`, FK `fiskaly_receipt.id` `ON DELETE SET NULL`)
- **Descrizione**: scontrino a cui il log si riferisce.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (a schema; nel codice sempre valorizzato).
- **Relazioni**: **FK interna** → [`fiskaly_receipts.id`](fiskaly_receipts.md), `ON DELETE SET NULL`
  (di fatto mai: gli scontrini non si cancellano).
- **Stato dopo la migrazione (2026-10-01)**: **non è più vuota** dal 2026-10-01: gli id della fetta fiscale sono preservati 1:1, quindi il valore è lo stesso del legacy e si aggancia con `fk_bigint_if_registered` (7.840 log su 8.125). Restano NULL i 285 log che puntano a 78 ricevute scartate dall'ETL — un `copy` cieco avrebbe violato la chiave esterna. Voce M7 di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### property_id
- **Legacy**: `fiskaly_receipt_log.property_id` (`int`, FK `properties.id`)
- **Descrizione**: stabilimento (ridondante con lo scontrino, comodo per filtrare i log).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: Sì (come nel legacy).
- **Relazioni**: **`public_id` (uuid) copiato** da C2; [D-01](../_domande-committente.md).

### booking_number
- **Legacy**: `fiskaly_receipt_log.booking_number` (`varchar(50)`, troncato a 50 da `createLog`)
- **Descrizione**: numero prenotazione dello scontrino (denormalizzazione per query dirette).
- **Tipo**: `varchar(32)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `booking_reservations.booking_number` (per valore).

### step
- **Legacy**: `fiskaly_receipt_log.state` (`varchar(50)`)
- **Descrizione**: passo/stato del flusso a cui il log si riferisce.
- **Tipo**: `enum fiskaly.receipt_log_step` — valori: `intention_created`, `transaction_created`,
  `transaction_failed`, `pdf_generation_failed`, `s3upload_failed`, `completed`, `creation_failed`,
  `cancellation_intention_created`, `cancellation_intention_failed`, `cancellation_transaction_failed`,
  `cancelled`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: sovrainsieme di `fiskaly.receipt_record_state` con gli step di sola diagnostica
  (`pdf_generation_failed`, `creation_failed`).

### result
- **Legacy**: `fiskaly_receipt_log.result` (`varchar(2)`, `'OK'` / `'KO'`)
- **Descrizione**: esito del passo. In prod i `ko` esistono (~160 con `fiskaly_error_code`, ~95 con
  status HTTP 502/405/400/500 — AdE non disponibile, credenziali Fisconline scadute, timeout 30s).
- **Tipo**: `enum fiskaly.log_result` — valori: `ok`, `ko`.
- **Default**: `ok`.
- **Nullabile**: No.
- **Relazioni**: —.

### internal_message
- **Legacy**: `fiskaly_receipt_log.internal_error_messages` (`varchar(500)`)
- **Descrizione**: messaggio d'errore interno (`error.message`) o nota (es. `"Receipt cancelled by
  user"` sul log `cancelled`). `NULL` sui log OK.
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### fiskaly_record_state
- **Legacy**: `fiskaly_receipt_log.fiskaly_record_state` (`varchar(50)`)
- **Descrizione**: stato **grezzo** del record restituito da Fiskaly per quel passo
  (`ACCEPTED` sugli INTENTION, `COMPLETED` su TRANSACTION). Distribuzione prod: `COMPLETED` 5.062,
  `ACCEPTED` 2.742, `REJECTED` 70, `FAILED` 14.
- **Tipo**: `enum fiskaly.fiskaly_record_state` — valori: `accepted`, `rejected`, `completed`,
  `failed`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` sui log di sola diagnostica interna).
- **Relazioni**: costante `FiskalyRecordState` portata a enum.

### fiskaly_error_status
- **Legacy**: `fiskaly_receipt_log.fiskaly_error_status` (`int`, HTTP status)
- **Descrizione**: status HTTP dell'errore Fiskaly/axios. `NULL` sui log OK.
- **Tipo**: `smallint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### fiskaly_error_code
- **Legacy**: `fiskaly_receipt_log.fiskaly_error_code` (`varchar(50)`)
- **Descrizione**: codice d'errore applicativo/Fiskaly (`SIGN_IT_TRANSACTION_ERROR`,
  `SIGN_IT_TRANSACTION_FAILED`, `SIGN_IT_INTENTION_ERROR`, `NOTHING_TO_BILL`, …). Vocabolario aperto
  (SIGN IT può introdurne di nuovi) → `varchar`, non enum.
- **Tipo**: `varchar(60)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### fiskaly_error_category
- **Legacy**: `fiskaly_receipt_log.fiskaly_error_error` (`varchar(50)` — campo `error.error`)
- **Descrizione**: breve categoria/descrizione dell'errore Fiskaly (es. `"TRANSACTION creation
  failed"`).
- **Tipo**: `varchar(80)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### fiskaly_error_message
- **Legacy**: `fiskaly_receipt_log.fiskaly_error_message` (`varchar(50)` — **troncato**, spesso
  insufficiente)
- **Descrizione**: messaggio d'errore Fiskaly completo (`error.message`).
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### fiskaly_log_payload
- **Legacy**: `fiskaly_receipt_log.fiskaly_log_messages` (`longtext` JSON — `CHECK (json_valid(...))`)
- **Descrizione**: array/oggetto "logs" dell'errore Fiskaly (`error.logs`), quando presente. `NULL`
  sui log OK. Passthrough di sistema esterno (§3.6 punto 5).
- **Tipo**: `jsonb`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### actor_user_id
- **Legacy**: `fiskaly_receipt_log.createdBy` (`int`, `users.id`, nessuna FK; `NULL` per gli
  automatici)
- **Descrizione**: operatore che ha innescato l'operazione.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.

### created_at
- **Legacy**: `fiskaly_receipt_log.createdAt` (`datetime`, `timestamps: false`)
- **Descrizione**: istante del log. Chiave di ordinamento cronologico dei log di uno scontrino.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: decorrenza per la retention.

### updated_at / deleted_at
- **Legacy**: `fiskaly_receipt_log.updatedAt` (ridondante, = `createdAt`) / —
- **Descrizione**: esposti per uniformità ma **non usati**: tabella di **puro log append-only**, non
  implementa `ISoftDeletable` ([`docs/03 §3.5` ultimo capoverso](../../../progettazione/03-gestione-dati.md)).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `NULL`.
- **Nullabile**: No / Sì.
- **Relazioni**: —.

## Valutazione JSON

`fiskaly_log_messages` (array/oggetto) → **resta `jsonb`** (`fiskaly_log_payload`): è il log grezzo
dell'errore di un'API di terzi, mai interrogato per campo (§3.6 punto 5). Tutto il resto è scalare
tipizzato.

## Suggerimenti per le prestazioni

- Tabella ad alto volume relativo (~3 righe per scontrino, crescita illimitata nel legacy):
  **retention** — hard-delete o partizionamento per mese di `created_at`; conservare i `ko` più a
  lungo dei `ok` (i `ok` di flusso normale sono rumore diagnostico —
  [`docs/15-logs.md`](../../../progettazione/15-logs.md), [`docs/80-retention.md`](../../../progettazione/80-retention.md)).
- Indice parziale sui soli `result = 'ko'` per il cruscotto errori.
- Nessun `updated_at` trigger (append-only): `INSERT` puro.
- Valutare una tabella `UNLOGGED` + flush periodico se il volume di scrittura diventa un collo di
  bottiglia (perdita accettabile: è diagnostica).

## Implementazione .NET

`ReceiptLog` è un'entità di **puro log append-only** del modulo `Module.Fiskaly` (schema `fiskaly`):
una riga per transizione di stato o errore nel dialogo con l'API SIGN IT. **Non** è owned
dell'aggregato `Receipt` — deve poter scrivere anche quando l'aggregato fa rollback, esattamente come
`createLog`/`createErrorLog` legacy, che "non lancia mai". Referenzia `Receipt` con FK reale
(`ON DELETE SET NULL`, di fatto mai percorsa: gli scontrini non si cancellano).

### Enum propri

```csharp
// Domain/Enums/ReceiptLogStep.cs
public enum ReceiptLogStep
{
	IntentionCreated = 0,
	TransactionCreated = 1,
	TransactionFailed = 2,
	PdfGenerationFailed = 3,
	S3UploadFailed = 4,
	Completed = 5,
	CreationFailed = 6,
	CancellationIntentionCreated = 7,
	CancellationIntentionFailed = 8,
	CancellationTransactionFailed = 9,
	Cancelled = 10
}

// Domain/Enums/FiskalyRecordState.cs — stato grezzo del record Fiskaly per il passo di log
public enum FiskalyRecordState { Accepted = 0, Rejected = 1, Completed = 2, Failed = 3 }
```

`LogResult` (`ok`/`ko`) è il vocabolario **SharedKernel** (riusato anche da altri moduli di log) — non
ridefinito qui, solo mappato come `fiskaly.log_result`.

```csharp
// FiskalyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ReceiptLogStep>("fiskaly", "receipt_log_step");
modelBuilder.HasPostgresEnum<LogResult>("fiskaly", "log_result");
modelBuilder.HasPostgresEnum<FiskalyRecordState>("fiskaly", "fiskaly_record_state");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<ReceiptLogStep>("fiskaly.receipt_log_step");
dataSourceBuilder.MapEnum<LogResult>("fiskaly.log_result");
dataSourceBuilder.MapEnum<FiskalyRecordState>("fiskaly.fiskaly_record_state");
```

### Configuration

```csharp
namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class ReceiptLogConfiguration : IEntityTypeConfiguration<ReceiptLog>
{
	public void Configure(EntityTypeBuilder<ReceiptLog> builder)
	{
		builder.ToTable("fiskaly_receipt_logs", "fiskaly", t => t.HasComment(
			"Log append-only del flusso di emissione/annullamento dello scontrino elettronico: una riga per transizione di stato o errore SIGN IT."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_receipt_logs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReceiptId)
			.HasColumnName("receipt_id")
			.HasComment("Scontrino a cui il log si riferisce.");
		builder.HasOne<Receipt>()
			.WithMany()
			.HasForeignKey(x => x.ReceiptId)
			.HasConstraintName("fk_fiskaly_receipt_logs_receipt")
			.OnDelete(DeleteBehavior.SetNull);
		builder.HasIndex(x => new { x.ReceiptId, x.CreatedAt })
			.HasDatabaseName("ix_fiskaly_receipt_logs_receipt");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Ridondante con lo scontrino, comodo per filtrare i log.");
		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasDatabaseName("ix_fiskaly_receipt_logs_property_date");
		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasFilter("result = 'ko'")
			.HasDatabaseName("ix_fiskaly_receipt_logs_errors");

		builder.Property(x => x.BookingNumber)
			.HasColumnName("booking_number")
			.HasMaxLength(32)
			.HasComment("Numero prenotazione dello scontrino — denormalizzazione per filtrare i log senza join.");

		builder.Property(x => x.Step)
			.HasColumnName("step")
			.HasColumnType("fiskaly.receipt_log_step")
			.IsRequired()
			.HasComment("Passo/stato del flusso a cui il log si riferisce.");

		builder.Property(x => x.Result)
			.HasColumnName("result")
			.HasColumnType("fiskaly.log_result")
			.IsRequired()
			.HasDefaultValue(LogResult.Ok)
			.HasComment("Esito del passo.");

		builder.Property(x => x.InternalMessage)
			.HasColumnName("internal_message")
			.HasComment("Messaggio d'errore interno o nota diagnostica; NULL sui log OK.");

		builder.Property(x => x.FiskalyRecordState)
			.HasColumnName("fiskaly_record_state")
			.HasColumnType("fiskaly.fiskaly_record_state")
			.HasComment("Stato grezzo del record restituito da Fiskaly per quel passo.");

		builder.Property(x => x.FiskalyErrorStatus)
			.HasColumnName("fiskaly_error_status")
			.HasColumnType("smallint")
			.HasComment("Status HTTP dell'errore Fiskaly/axios; NULL sui log OK.");

		builder.Property(x => x.FiskalyErrorCode)
			.HasColumnName("fiskaly_error_code")
			.HasMaxLength(60)
			.HasComment("Codice d'errore applicativo/Fiskaly — vocabolario aperto (SIGN IT può introdurne di nuovi), non enum.");

		builder.Property(x => x.FiskalyErrorCategory)
			.HasColumnName("fiskaly_error_category")
			.HasMaxLength(80)
			.HasComment("Breve categoria/descrizione dell'errore Fiskaly.");

		builder.Property(x => x.FiskalyErrorMessage)
			.HasColumnName("fiskaly_error_message")
			.HasComment("Messaggio d'errore Fiskaly completo, non troncato (a differenza del legacy).");

		builder.Property(x => x.FiskalyLogPayload)
			.HasColumnName("fiskaly_log_payload")
			.HasColumnType("jsonb")
			.HasComment("Array/oggetto grezzo dei log d'errore Fiskaly — passthrough di sistema esterno, solo diagnostica.");

		builder.Property(x => x.ActorUserId)
			.HasColumnName("actor_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha innescato l'operazione.");

		// created_at (IAuditable, solo creazione) — plumbing comune; updated_at/deleted_at esposti per uniformità, mai usati (append-only, non ISoftDeletable)
	}
}
```

`ReceiptLog` implementa **solo** `CreatedAt` (nessun `UpdatedAt` reale: `updated_at` esposto per
uniformità ma sempre = `created_at`); **non** implementa `ISoftDeletable` — è un log append-only puro
([`docs/03 §3.5` ultimo capoverso](../../../progettazione/03-gestione-dati.md)), niente Global Query Filter.

### Note di migrazione

- `state` → `step`; `result` → `enum log_result`; `fiskaly_record_state` → enum (costante
  `FiskalyRecordState` legacy, disallineata, da rifare pulita).
- `fiskaly_error_message` (`varchar(50)`, troncato) → `text`, nessuna perdita per i messaggi lunghi.
- `updatedAt` (ridondante, sempre = `createdAt`) **scartato** in migrazione, non riportato come valore
  significativo.
- `createdBy` (`int`, senza FK) → `actor_user_id` (`uuid`) via mappa C5.
- Job di retention (`PurgeDiagnosticLogs`, [`docs/20-jobs.md` §1.5](../../../progettazione/20-jobs.md)):
  **12 mesi** validati dal titolare. La differenziazione `ok` vs `ko` resta una possibilità, ma va
  espressa come **due righe di policy** con `field_names`/filtri distinti, non come un parametro del
  job: la durata vive in [`privacy_retention_policies`](../privacy/privacy_retention_policies.md), non
  in `appsettings`. Impostazione storica (giorni di conservazione differenziati per `ok` vs `ko`, i `ko`
  tenuti più a lungo) — hard-delete o partizionamento per mese di `created_at`, vedi
  [`docs/80-retention.md`](../../../progettazione/80-retention.md).
