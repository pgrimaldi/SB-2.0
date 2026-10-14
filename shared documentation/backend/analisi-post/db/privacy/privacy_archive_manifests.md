# Tabella `privacy_archive_manifests`

> **Dominio**: C8 — `privacy` · **sottoarea**: `retention` · **schema**: `privacy` · **nome flat**:
> `privacy_archive_manifests` · **forma canonica**: `privacy.privacy_archive_manifests`.
>
> **Tabella nuova**, aggiunta il 2026-10-05 con la migration `20261005084130_AddPrivacyArchiveManifests`.
> Il legacy non ha mai archiviato nulla, quindi non ha nessun inventario da migrare.
>
> Job: [`docs/20-jobs.md` §1.5](../../../progettazione/20-jobs.md) · politica:
> [`docs/80-retention.md` §6](../../../progettazione/80-retention.md) · verbale delle scelte del titolare:
> [`docs/82-retention-data.md`](../../../progettazione/82-retention-data.md).

---

## Descrizione

Dove sta un archivio prodotto da una policy `archive_cold`: **una riga per oggetto scritto su
storage**, con quante righe contiene, da quale taglio provengono e quando è stato scritto. La scrive
il motore di retention (`BuildingBlocks.Infrastructure/Retention/RetentionEngine.cs`, `RecordManifestAsync`) subito dopo aver
caricato l'oggetto e **prima** di cancellare le righe dal database caldo.

È la risposta a due domande che nessun'altra tabella può più dare:

**1. «Dove sono finite le prenotazioni archiviate nell'ottobre 2026?»** Le righe non sono più nel
database. Senza un inventario la risposta si ottiene solo elencando il bucket e aprendo gli oggetti
uno a uno per vedere cosa contengono.

**2. «Quali archivi sono scaduti?»** È la query della policy che li cancella — quella decennale
seminata con `20261005084928_SeedArchiveManifestRetentionPolicy`. Senza questa tabella quella policy
non ha nessuna fonte da cui partire.

### 🆕 Due nature nello stesso inventario (2026-10-06)

Dal 2026-10-06 qui finiscono **due cose diverse**, che però devono avere la **stessa scadenza**:

| `object_kind` | Che cos'è | `row_count` |
|---|---|---|
| `row_archive` | l'archivio JSON Lines prodotto da una policy `archive_cold` | quante righe contiene |
| `linked_asset` | un **file che ha seguito** le righe archiviate — il PDF di uno scontrino | **NULL** |

Il caso che l'ha richiesto: la riga di `fiskaly.receipts` viene archiviata a 36 mesi, e il suo PDF deve
andarsene con lei. Lasciandolo dov'era, il PDF resterebbe sullo storage **senza nessuna riga calda che
lo nomini**, e con la data da cui scade — `signed_at` — uscita dal database insieme alla riga: non
resterebbe niente che possa decidere quando cancellarlo.

Inventariando anche il PDF, **l'unica policy decennale su questa tabella li cancella entrambi** al
momento giusto. Una sola regola per i due pezzi della coppia: scadere in momenti diversi era il
difetto.

> ⚠️⚠️ **E non si risolve con una policy «cancella i file che nessuno nomina più».** Dopo
> l'archiviazione il PDF di uno scontrino **è** per definizione «non nominato da nessuno», quindi
> quella regola lo cancellerebbe al **mese 36** invece che all'**anno 10**. È il motivo per cui
> `fiskaly.media_assets` non ha, e non deve avere, una policy `only_if_unreferenced`.

> ⚠️ `object_kind` è **un'etichetta e non un `row_count IS NULL`**: dedurre la natura di una riga
> dall'assenza di un valore è il genere di implicito che qualcuno, fra due anni, legge al contrario.

La riga di inventario di un file collegato porta `cutoff_at` e `archived_at` **della corsa**, non del
file: la sua scadenza decorre da quando ha lasciato il database caldo, come per l'archivio delle righe.

### Perché è una tabella sua e non una colonna di `privacy_retention_runs`

La chiave dell'oggetto **era** in `privacy_retention_runs.archive_path`, ed è stata spostata qui il
2026-10-05. Il motivo è un difetto scoperto seminando la retention del verbale stesso:

| | verbale della corsa | inventario degli archivi |
|---|---|---|
| natura del dato | **operativo**: com'è andata quella notte | **di inventario**: dove sta un oggetto |
| a chi serve | a chi autorizza una policy, per qualche mese | a chi deve ritrovare un dato archiviato, e alla policy che lo cancella |
| termine | **24 mesi** | **quanto l'oggetto che indirizza**: dieci anni per gli scontrini fiscali |

Tenerli nella stessa tabella significava che la retention del primo cancellava il secondo. Fra il mese
24 e l'anno 10 ci sarebbero stati sullo storage oggetti **di cui il database non sapeva più niente**:
né che esistevano, né quale policy li aveva prodotti, né quando scadevano.

> ⚠️ `privacy_retention_runs.archive_path` **esiste ancora** e non è stato rimosso: serve a leggere il
> verbale di una corsa senza fare una join. Ma è un **eco operativo**, non l'inventario, e scade a 24
> mesi. I commenti delle due colonne lo dicono a chiare lettere, perché una colonna che resta è
> esattamente il tipo di cosa che qualcuno riusa credendola completa.

### Perché su questa tabella una policy non ha bisogno di un perimetro

Ogni riga nomina **esattamente un oggetto**, e tutte le righe sono della stessa natura. Non c'è nessun
sottoinsieme da distinguere.

È il contrario di quello che succede sulle tabelle di asset: `notification.media_assets` mescola
allegati di modelli, allegati di campagne, copertine di eventi e PDF generati, e una policy che dica
solo «i file oltre 24 mesi» cancellerebbe anche quelli che non c'entrano. Quel problema — dichiarare
il perimetro di una policy su file — **resta aperto** per `notification` e `catalog`, e questa tabella
non lo risolve: lo **evita**, perché non ce l'ha.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_privacy_archive_manifests` | `id` | PK | — |
| `uq_privacy_archive_manifests_public_id` | `(public_id)` | UNIQUE | id pubblico della riga |
| `uq_privacy_archive_manifests_path` | `(storage_origin, storage_path)` | UNIQUE | **un oggetto non si inventaria due volte.** È anche il bersaglio dell'`ON CONFLICT DO NOTHING` che rende idempotente la scrittura del motore se una corsa viene interrotta fra il caricamento e l'`INSERT` |
| `ix_privacy_archive_manifests_da_rimuovere` | `(archived_at) WHERE purged_at IS NULL` | parziale | «quali archivi sono scaduti e ancora da rimuovere»: *la* query della policy decennale. Parziale perché gli oggetti già rimossi restano in tabella per sempre come prova, e col tempo diventano la maggioranza |
| `ix_privacy_archive_manifests_sorgente` | `(source_schema, source_table, cutoff_at)` | — | «dove sono finite le righe di questa tabella»: la domanda di chi cerca un dato archiviato |

## Vincoli

| Vincolo | Regola | Perché |
|---|---|---|
| `ck_privacy_archive_manifests_counts` | per `row_archive`: `row_count` e `byte_size` non nulli e maggiori di zero · per `linked_asset`: `row_count` NULL e `byte_size` NULL oppure maggiore di zero | un archivio di **righe** non può contenerne zero né pesare zero byte: se succede, l'oggetto non è un archivio e inventariarlo mentirebbe. Un **file collegato** non contiene righe, e di lui la dimensione può non essere registrata — inventare uno zero sarebbe peggio del non saperlo |
| `ck_privacy_archive_manifests_purged` | `purged_at IS NULL OR purged_at >= archived_at` | un oggetto non può essere stato rimosso prima di essere stato scritto |
| `fk_privacy_archive_manifests_policy` | FK verso `privacy_retention_policies(id)`, `RESTRICT` | ritirare una policy non deve portarsi via l'inventario di ciò che ha archiviato |

### Perché `run_id` **non** è una chiave esterna

Punta a `privacy_retention_runs.run_id`, ma come riferimento **logico**. Una FK impedirebbe al verbale
di scadere: le sue righe vivono 24 mesi e queste fino a dieci anni, quindi il vincolo trasformerebbe
l'inventario in una catena che trattiene il verbale — esattamente il contrario del motivo per cui le
due tabelle sono separate.

## Campi

| Colonna | Tipo | Nota |
|---|---|---|
| `id` | `bigint` identity always | — |
| `public_id` | `uuid` (UUIDv7) | id pubblico. Generato dal motore, non dal database: su una tabella append-only l'ordinamento per `public_id` coincide con quello temporale |
| `run_id` | `uuid` | corsa che ha prodotto l'archivio. ⚠️ riferimento **logico**, non FK (sopra) |
| `policy_id` | `bigint` | FK alla policy che ha deciso l'archiviazione |
| `source_schema` | `varchar(63)` | schema della tabella le cui righe sono dentro l'archivio |
| `source_table` | `varchar(63)` | ⚠️ nome **fisico** risolto dal motore: in `privacy` e `switchboard` ha il prefisso di modulo |
| `storage_origin` | `varchar(32)` | host dello storage. ⚠️ **deve essere un'etichetta distinta da quella dei file applicativi** (vedi sotto) |
| `storage_path` | `text` | chiave dell'oggetto, nella forma `archivio/<schema>/<tabella>/<anno>/<mese>/<istante>-<policy>-<lotto>.jsonl.gz` |
| `object_kind` 🆕 | `privacy.archive_object_kind` | `row_archive` = l'archivio JSON Lines delle righe; `linked_asset` = un **file che ha seguito** quelle righe. Default `row_archive` |
| `byte_size` | `bigint?` | byte dell'oggetto; per un archivio di righe, compressi. NULL ammesso solo su un file collegato di cui la dimensione non era registrata |
| `row_count` | `bigint?` | quante righe di database sono dentro questo oggetto. **Serve a verificare che l'archivio sia completo senza aprirlo.** NULL su `linked_asset`: un file non contiene righe, e «zero» sarebbe un'altra cosa |
| `cutoff_at` | `timestamptz` | data di taglio della policy: dice **che cosa** c'è dentro |
| `archived_at` | `timestamptz` | quando l'archivio è stato scritto. È la **decorrenza** della policy che lo cancellerà |
| `purged_at` | `timestamptz?` | quando l'oggetto è stato rimosso dallo storage. NULL = c'è ancora |
| `created_at` / `updated_at` | `timestamptz` | `updated_at` cambia in pratica solo quando si valorizza `purged_at` |

### `purged_at`: chi lo scrive, e perché la riga non si cancella

Lo scrive `BuildingBlocks.Infrastructure/Retention/StorageDeletionReconciler.cs` (`MarkAssetsPurgedAsync`), il job che svuota
la coda `privacy_file_deletions`: dopo aver rimosso gli oggetti di un lotto, aggiorna le righe di asset
corrispondenti **se la loro tabella ha una colonna `purged_at`**. La regola è generica e dichiarata
dallo schema, non un caso speciale per Privacy: le tabelle di media non hanno quella colonna — usano
la cancellazione logica — e vengono semplicemente saltate.

> La riga di inventario **non si cancella mai**, e per questo la tabella non implementa
> `ISoftDeletable`. È la prova di aver rimosso l'oggetto nei termini: cancellarla insieme all'oggetto
> significherebbe non poter più dimostrare di aver fatto la cosa giusta. È anche il motivo per cui la
> policy decennale usa l'azione `delete_object` e non `delete_row`.
>
> ⚠️ Un errore nell'aggiornamento di `purged_at` **non** fa fallire il lotto: l'oggetto è già rimosso e
> la coda è già aggiornata, che sono i due fatti che contano. Si registra un avviso. Ritentare la
> cancellazione di un oggetto per non aver potuto scrivere una data sarebbe il rimedio peggiore del
> male.

## La policy che cancella gli archivi

Seminata il 2026-10-05 con `20261005084928_SeedArchiveManifestRetentionPolicy`:

```
privacy · archive_manifests · archived_at · 10 anni · delete_object · only_if_unreferenced = true
```

| Scelta | Perché |
|---|---|
| **10 anni** | il più lungo fra gli obblighi che motivano un'archiviazione in questo progetto: conservazione decennale dei documenti fiscali (scontrini e corrispettivi). Termini più corti differenziati per tabella sorgente si potranno aggiungere in seguito con policy più specifiche; per ora una regola unica e prudente è meglio di nessuna regola |
| decorrenza **`archived_at`** e non `cutoff_at` | `cutoff_at` dice quanto erano vecchie le righe quando sono state archiviate, `archived_at` da quando l'**oggetto** esiste. È l'oggetto che questa policy cancella |
| azione **`delete_object`** | rimuove l'oggetto dallo storage e **non** la riga di inventario |
| `only_if_unreferenced = true` | è il requisito che il motore impone a **ogni** azione su storage. Qui è soddisfatto per costruzione: nessuna tabella ha una FK verso questa, quindi il predicato generato è vuoto e non restringe niente |

> **Nasce in prova** (`is_dry_run_only = true`), come tutte. In ogni caso oggi non trova niente: il
> primo archivio è del 2026 e il taglio è il 2016.
>
> ⚠️ **Che cosa questa policy NON copre.** Archiviare non è cancellare: i dati personali dentro un
> archivio sono ancora dati personali, e finora **nessuna policy li cancellava**. Questo era il buco
> più grosso dell'impianto di retention e si chiude qui. Resta però il fatto che un archivio contiene
> righe di tabelle diverse con termini diversi, appiattiti su dieci anni: è prudente, non preciso.

## Valutazione JSON

Nessuna colonna JSON. La forma di una riga di inventario è chiusa.

## ⚠️ `storage_origin` deve essere un'etichetta dedicata

Il job che svuota la coda delle cancellazioni risolve il **bucket** proprio da questa etichetta, e un
`storage_origin` non elencato fra le destinazioni configurate non viene toccato affatto.

Alla creazione della tabella l'archivio era configurato con `Origin = "s3_localstack"`, **la stessa
etichetta dei file applicativi**, che punta al bucket `summerbooking-nuovo`. Con quella
configurazione la policy decennale avrebbe cercato le chiavi `archivio/...` nel bucket dei file: nel
migliore dei casi non avrebbe trovato niente, e l'archivio non sarebbe stato cancellato mai. È stata
cambiata in `s3_archivio_local`, con la sua destinazione in `Retention:Deletion:Targets`.

Il limite di lunghezza è **32 caratteri**: un'etichetta più lunga fa fallire l'`INSERT`.

## Suggerimenti per le prestazioni

- la tabella cresce di **una riga per lotto di archivio**, non per corsa: con lotti da 20.000 righe,
  le corse attuali ne producono tre in tutto. Non serve nessun accorgimento;
- `ix_..._da_rimuovere` è parziale su `purged_at IS NULL` perché le righe degli oggetti già rimossi
  restano per sempre e col tempo diventano la maggioranza: l'indice della policy resta piccolo.

## Implementazione .NET

- entità: `Module.Privacy/Domain/Entities/ArchiveManifest.cs`. **`IAuditable` ma non
  `ISoftDeletable`**: un registro di inventario non si cancella logicamente;
- configurazione: `Infrastructure/Configurations/ArchiveManifestConfiguration.cs`;
- **la scrive il motore in `Shared`, non EF Core**, in SQL: `Shared` non conosce i `DbContext` dei
  moduli, e l'`INSERT` parte dalla connessione del modulo proprietario dello schema archiviato —
  quella di Booking, di Payment — non da quella di Privacy. Nessun enum da castare: tutte le colonne
  sono tipi nativi, che è un vantaggio accidentale ma reale rispetto al verbale delle corse.
