# Tabella `privacy_retention_runs`

> **Dominio**: C8 — `privacy` · **sottoarea**: `retention` · **schema**: `privacy` · **nome flat**:
> `privacy_retention_runs` · **forma canonica**: `privacy.privacy_retention_runs`.
>
> **Tabella nuova**, aggiunta il 2026-10-03 con la migration `20261003101352_AddPrivacyRetentionRuns`
> insieme ai job di retention. Il legacy non ha mai cancellato né archiviato nulla, quindi non ha
> nessun verbale da migrare.
>
> Job: [`docs/20-jobs.md` §1.5](../../../progettazione/20-jobs.md) · politica:
> [`docs/80-retention.md` §6](../../../progettazione/80-retention.md) · verbale delle scelte del titolare:
> [`docs/82-retention-data.md`](../../../progettazione/82-retention-data.md).

---

## Descrizione

Verbale di una policy di retention in una corsa: che cosa ha trovato, che cosa ha toccato, dove ha
archiviato, e se è andata male perché. **Una riga per `(corsa, policy)`**, scritta dal motore di
retention (`BuildingBlocks.Infrastructure/Retention/RetentionEngine.cs`) subito dopo aver valutato la policy — anche quando
la valutazione è fallita.

Esiste per tre ragioni che nessun'altra tabella copre.

**1. Com'è andata la corsa.** Quante righe ha trovato ogni policy, quante ne ha toccate, quanto è
durata. ⚠️ **Non** «dove sono finite le righe archiviate»: quella era la prima ragione di questa
tabella fino al 2026-10-05, e oggi non lo è più — l'inventario degli archivi è
[`privacy_archive_manifests`](privacy_archive_manifests.md). Vedi *Le colonne `archive_*` sono un eco*,
sotto.

**2. Gli errori.** Il motore **non si ferma** alla prima policy che fallisce: una tabella trattenuta
da una chiave esterna non deve impedire alle altre 58 di scadere. Gli errori vanno quindi raccolti da
qualche parte, e il log del processo non basta — va letto da chi decide se accendere una policy, non
da chi guarda la console di un job notturno.

**3. La prova di cosa farebbe.** Tutte le policy nascono con `is_dry_run_only = true`, e il primo giro
serve a leggere `matched_rows` prima di autorizzare l'esecuzione reale. È *il* report di dry-run che la
politica richiede, e senza una tabella sarebbe un file di log che nessuno ritrova.

> ### Da non confondere con `privacy_compliance_audit_logs`
>
> Quello è il registro **per riga** previsto dall'art. 5.2 GDPR: `record_id` è una colonna
> obbligatoria. Questo è **per policy**: dice «la policy X ha cancellato 83.796 righe di
> `notification.deliveries` con taglio al 2025-10-03».
>
> ⚠️ **I due non si sostituiscono, e il registro per riga ha un problema di scala da affrontare prima
> di attivare le policy sulle tabelle grandi**: una sola corsa su `booking.resource_consumptions`
> produrrebbe milioni di righe di audit. La questione non è risolta e non va scoperta la notte in cui
> il job gira per la prima volta.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_privacy_retention_runs` | `id` | PK | — |
| `uq_privacy_retention_runs_public_id` | `(public_id)` | UNIQUE | id pubblico della riga di verbale |
| `ix_privacy_retention_runs_run` | `(run_id, schema_name)` | — | «leggimi il report di questa corsa»: la query del pannello e di chi autorizza l'esecuzione reale |
| `ix_privacy_retention_runs_failed` | `(finished_at) WHERE status = 'failed'` | parziale | «che cosa è andato male»: tiene fuori le righe riuscite, che sono la maggioranza, e rende immediata la domanda per cui la tabella esiste |
| `ix_privacy_retention_runs_policy` | `(policy_id, finished_at)` | — | «com'è andata questa policy nel tempo»: serve a vedere se una regola ha smesso di trovare righe |

## Vincoli

| Vincolo | Regola | Perché |
|---|---|---|
| `ck_privacy_retention_runs_times` | `finished_at >= started_at` | una corsa finisce dopo che è cominciata. Uguale ammesso: una policy saltata dura zero |
| `ck_privacy_retention_runs_counts` | `matched_rows >= 0 AND affected_rows >= 0 AND affected_rows <= matched_rows` | non si possono toccare più righe di quante se ne trovino |
| `ck_privacy_retention_runs_dry_run` | `NOT is_dry_run OR affected_rows = 0` | **in prova non si scrive niente, per definizione.** È il vincolo che rende la prova una prova anche se il codice sbagliasse |
| `ck_privacy_retention_runs_detail` | un `failed` deve avere `detail` | un errore senza il suo testo è inutile |
| `fk_privacy_retention_runs_policy` | FK verso `privacy_retention_policies(id)`, `RESTRICT` | ritirare una policy non deve portarsi via la prova di cosa ha fatto |

## Campi

| Colonna | Tipo | Nota |
|---|---|---|
| `id` | `bigint` identity always | — |
| `public_id` | `uuid` (UUIDv7) | id pubblico |
| `run_id` | `uuid` | raggruppa tutte le righe della stessa esecuzione, **su tutti gli schemi**: è la chiave con cui si legge un report per intero |
| `policy_id` | `bigint` | FK alla policy valutata |
| `schema_name` | `varchar(63)` | copiato dalla policy |
| `table_name` | `varchar(63)` | ⚠️ nome **fisico** risolto dal motore: in `privacy` e `switchboard` ha il prefisso di modulo, mentre la policy porta la forma canonica senza prefisso |
| `action` | `privacy.retention_action` | **copiata** dalla policy: il verbale deve restare leggibile anche se la policy viene poi modificata |
| `cutoff_at` | `timestamptz` | data di taglio calcolata. NULL quando la policy è stata saltata prima di calcolarla |
| `is_dry_run` | `boolean` | la corsa non ha scritto niente: o era una prova, o la policy è `is_dry_run_only` |
| `matched_rows` | `bigint` | righe oltre la data di taglio. **È il numero che si legge nel report di dry-run** |
| `affected_rows` | `bigint` | righe davvero cancellate, azzerate o anonimizzate. Zero in prova, anche con `matched_rows` alto |
| `archive_origin` | `varchar(32)` | host dello storage. Testo e non enum, per lo stesso motivo di `privacy_file_deletions.storage_origin`. ⚠️ eco, vedi sotto |
| `archive_path` | `text` | chiave dell'oggetto, o più chiavi separate da spazio se l'export è andato in più lotti. ⚠️ eco, vedi sotto |
| `archive_byte_size` | `bigint` | byte scritti, compressi. ⚠️ eco, vedi sotto |
| `status` | `privacy.retention_run_status` | `succeeded` · `skipped` · `failed` |
| `detail` | `text` | il motivo del salto, o il testo dell'errore con le sue cause |
| `started_at` | `timestamptz` | istante di valutazione della corsa, **lo stesso per tutte le policy**: due policy della stessa esecuzione non devono vedere due «adesso» diversi |
| `finished_at` / `duration_ms` | `timestamptz` / `bigint` | quanto è durata questa policy: serve a capire quali non staranno in una finestra notturna |

### ⚠️ Le colonne `archive_*` sono un eco, non l'inventario

Dal 2026-10-05 l'inventario autorevole degli archivi è
[`privacy_archive_manifests`](privacy_archive_manifests.md): **una riga per oggetto**, con il numero di
righe che contiene, e con un termine di conservazione pari a quello dell'oggetto che indirizza.

Il motivo dello spostamento è che questa tabella **scade a 24 mesi** e gli archivi vivono **fino a
dieci anni**. Finché la chiave dell'oggetto stava solo qui, fra il mese 24 e l'anno 10 ci sarebbero
stati sullo storage oggetti di cui il database non sapeva più niente, e la policy che deve cancellarli
alla scadenza non avrebbe avuto nessuna fonte da cui partire.

Le tre colonne restano perché permettono di leggere il verbale di una corsa senza fare una join. **Non
vanno usate come inventario**: dopo 24 mesi non ci sono più, e una riga `archive_path` con più chiavi
separate da spazio non dice quante righe stanno in ciascuna.

### `skipped` non è un errore, e tenerlo distinto serve

Un salto ha sempre una ragione dichiarata, scritta in `detail`. Le quattro che il motore produce:

| Ragione | Quando |
|---|---|
| azione su storage | `delete_object`/`archive_object` non si eseguono in linea: vanno accodate in [`privacy_file_deletions`](privacy_file_deletions.md), perché una `DELETE` su S3 non partecipa alla transazione del database |
| policy su prefisso | nessuna riga di database da valutare |
| durata in stagioni | applicabile **solo** dal job annuale: un job mensile che la leggesse non saprebbe tradurla in una data di taglio, e deve dirlo invece di improvvisare |
| decorrenza da evento | `property_ceased` va derivata da `identity.account_subscriptions`, che è fuori dallo schema del modulo |

## Valutazione JSON

Nessuna colonna JSON. La forma di un esito è chiusa, e `detail` è testo libero perché contiene il
messaggio di un errore, non una struttura.

## Un avviso sulla configurazione dell'archivio

`ArchiveStoreOptions.StorageClass` è vuota in sviluppo, perché LocalStack la ignora. **In produzione va
impostata** (`GLACIER` o `DEEP_ARCHIVE`): senza, gli oggetti finiscono su classe `STANDARD` e si pagano
come se fossero caldi — che è l'opposto del motivo per cui si archivia. Il bucket va inoltre **dedicato
all'archivio**: qui finiscono righe di database, non i file applicativi, e mescolarli renderebbe la
policy sugli orfani dei file pericolosa per l'archivio.

## Suggerimenti per le prestazioni

- la tabella cresce di una riga per policy per corsa — oggi 61 — e con una corsa giornaliera sono
  ~22.300 righe l'anno. Non serve partizionarla. **La sua retention è dichiarata** dal 2026-10-05:
  24 mesi da `finished_at`, `delete_row`, migration `20261005073850_SeedRetentionRunsRetentionPolicy`;
- `ix_..._failed` è parziale perché le righe `failed` sono (si spera) una minoranza: l'indice resta
  piccolo anche quando la tabella non lo è.

## Implementazione .NET

- entità: `Module.Privacy/Domain/Entities/RetentionRun.cs`. **Né `IAuditable` né `ISoftDeletable`**:
  è un registro append-only, `updated_at` non cambierebbe mai e una riga di verbale non si cancella
  logicamente;
- enum nuovo: `Domain/Enums/RetentionRunStatus.cs`, registrato nei tre posti di rito;
- configurazione: `Infrastructure/Configurations/RetentionRunConfiguration.cs`;
- **la scrive il motore in `Shared`, non EF Core.** `BuildingBlocks.Infrastructure/Retention/RetentionEngine.cs` fa l'`INSERT`
  in SQL con i cast espliciti `::privacy.retention_action` e `::privacy.retention_run_status`: la
  scrittura parte dalla connessione del modulo proprietario dello schema valutato — quella di Booking,
  di Payment — e su quelle connessioni gli enum di `privacy` non sono mappati. Il cast da testo è ciò
  che rende la scrittura possibile da qualunque modulo senza che nessuno conosca gli enum di un altro.
