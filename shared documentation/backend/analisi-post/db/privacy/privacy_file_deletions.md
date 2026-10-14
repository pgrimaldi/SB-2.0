# Tabella `privacy_file_deletions`

> **Dominio**: C8 — `privacy` · **sottoarea**: `retention` · **schema**: `privacy` · **nome flat**:
> `privacy_file_deletions` · **forma canonica**: `privacy.privacy_file_deletions`.
>
> **Tabella nuova**: il legacy non ha nessuna coda di cancellazione, ed è esattamente il motivo per cui
> sul bucket di produzione ci sono ~12.000 file orfani. Specifica:
> [`docs/83-retention-file.md` §5.3](../../../progettazione/83-retention-file.md); analisi:
> [`docs/83-retention-file.md` §5.3](../../../progettazione/83-retention-file.md); regole:
> [`docs/80-retention.md` §3.11](../../../progettazione/80-retention.md).
>
> **Applicata** il 2026-10-03 con la migration `20261003075631_AddPrivacyFileDeletionsQueue`. Nasce
> vuota: i job che la riempiono **non sono stati scritti**, su richiesta.

---

## Descrizione

Coda delle **azioni su storage** decise da una policy di retention (o a mano da un amministratore):
cancellare un oggetto, oppure spostarlo su una classe di storage più fredda. È **indispensabile prima
di toccare qualunque file**, per una ragione che non si può aggirare: un'operazione su S3 **non
partecipa alla transazione del database**. Senza una coda, un errore di rete lascia il database che
dice una cosa e lo storage che ne dice un'altra, e nessuno sa quale delle due sia vera.

> ### ⚠️ Il nome della tabella dice meno di quello che fa
>
> `privacy_file_deletions` nasce il 2026-10-03, quando l'unica azione possibile era la cancellazione.
> Dal **2026-10-05** la colonna `action` porta anche `archive_object`, che **non cancella niente**: fa
> cambiare classe di storage all'oggetto, lasciandolo dov'è.
>
> Il nome è rimasto per non rinominare una tabella già documentata in cinque posti. **La colonna da
> guardare è `action`**, e un job che la ignorasse rimuoverebbe file che la policy vuole soltanto
> raffreddare — i PDF degli scontrini fiscali, che devono vivere dieci anni.

La riga nasce quando la decisione è presa (`decided_at`) e si chiude quando l'oggetto è stato davvero
rimosso (`executed_at`). Fra i due momenti ci sono `attempts`, `last_error` e `last_error_at`, che
sono l'unica cosa che distingue «non ancora fatto» da «non si riesce».

> ### Perché serve: il legacy ha già fatto questo errore, e si vede
>
> `_multipleDeleteFileFromS3` in `back-pre/app/global/globalConstants.js` ha tre difetti che si
> sommano: lancia `deleteObject` senza attenderne la callback e fa `return true` subito; ingoia gli
> errori in un `console.log`; e ricostruisce la chiave dal solo nome del file concatenandolo a una
> cartella cablata dal chiamante. Poi il chiamante riscrive l'array delle foto dando per riuscita
> un'operazione che non è ancora cominciata. **Il riferimento sparisce, il file resta.**
>
> Il risultato misurato sul bucket di produzione: ~12.000 oggetti orfani, 3,6 GB. Non sono materiale
> che qualcuno ha scelto di conservare, sono cancellazioni decise e non portate a termine. Questa
> tabella esiste perché il nuovo gestionale non possa ripetere lo stesso errore in silenzio.

**Non c'è una colonna per la copia locale**, di proposito: la cancellazione per policy agisce **solo
sulla copia AWS** (scelta esplicita del titolare). Se un giorno si decidesse di propagarla anche alla
copia locale si aggiunge `local_copy_removed_at`, ma oggi una colonna sempre `NULL` suggerirebbe un
lavoro che nessuno ha deciso di fare.

## Chi la riempie e chi la svuota — ✅ entrambi scritti il 2026-10-03

| Passo | Dove | Cosa fa |
|---|---|---|
| **riempie** | `BuildingBlocks.Infrastructure/Retention/RetentionEngine.cs` (`EnqueueStorageAsync`) | per una policy `delete_object`/`archive_object` riferita a una tabella, accoda i file scaduti **che nessuna riga nomina più**. Il predicato «nessun riferimento» si costruisce leggendo `pg_constraint`, non da un elenco cablato di chiavi esterne: il progetto ne ha diciannove e cambiano con lo schema, e un elenco scritto a mano si disallineerebbe **in silenzio** — una FK dimenticata non dà errore, fa cancellare un file ancora in uso. Esclude gli asset con `property_id` valorizzato, che hanno un proprietario dichiarato e non sono orfani |
| **svuota** | `BuildingBlocks.Infrastructure/Retention/StorageDeletionReconciler.cs` | rimuove gli oggetti e segna `executed_at`. Registrato **solo** da `Module.Privacy`: la coda è una. ⚠️ Un `storage_origin` senza destinazione configurata **non viene toccato** e finisce nel report: è ciò che impedisce a un ambiente di sviluppo di cancellare dal bucket di produzione |

⚠️ **Non tutte le policy sui file si accodano**, e il motore dice perché: quelle su **prefisso** (QR,
orfani) guardano il bucket e servono un job che lo elenchi; quelle **senza `only_if_unreferenced`** (i
PDF dei report, gli scontrini) hanno un perimetro che la policy non sa esprimere — `field_names` è vuoto
su quelle righe — e cancellare per sola data rimuoverebbe file ancora in uso.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_privacy_file_deletions` | `id` | PK | — |
| `uq_privacy_file_deletions_public_id` | `(public_id)` | UNIQUE | pannello SystemAdmin/DPO; correla il report di dry-run con l'esecuzione |
| `ix_privacy_file_deletions_pending` | `(decided_at) WHERE executed_at IS NULL` | parziale | **la coda da lavorare**: il job legge solo le righe non ancora eseguite, in ordine di decisione |
| `uq_privacy_file_deletions_pending_path` | `(storage_origin, storage_path) WHERE executed_at IS NULL` | UNIQUE parziale | lo stesso oggetto non va accodato due volte mentre è ancora da fare. Dopo l'esecuzione invece può tornare: un path riusato è un oggetto nuovo |
| `ix_privacy_file_deletions_policy_id` | `(policy_id)` | FK | generato da EF per la chiave esterna |

## Vincoli

| Vincolo | Regola | Perché |
|---|---|---|
| `ck_privacy_file_deletions_asset` | le tre colonne `asset_schema`/`asset_table`/`asset_id` sono tutte NULL o tutte valorizzate | o si sa quale riga usava il file, o non si sa: mai mezza informazione. I QR e gli orfani non hanno nessuna riga, ed è il motivo per cui sono nullable |
| `ck_privacy_file_deletions_last_error` | `(last_error IS NULL) = (last_error_at IS NULL)` | un errore senza la sua data non dice quando è successo, e la coda si giudica sulla distanza fra l'ultimo tentativo e adesso |
| `fk_privacy_file_deletions_policy` | FK verso `privacy_retention_policies(id)`, `ON DELETE RESTRICT` | una policy ritirata non deve portarsi via la prova di cosa ha cancellato |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Nullabile**: No.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico per il pannello SystemAdmin/DPO e per correlare il report di dry-run
  con l'esecuzione reale.
- **Tipo**: `uuid` (UUIDv7), `ValueGeneratedNever`.
- **Nullabile**: No.

### asset_schema / asset_table / asset_id
- **Legacy**: — (nuovo)
- **Descrizione**: quale riga di asset nominava il file. `asset_table` è nella **forma canonica
  relativa allo schema** (`media_assets`), come `privacy_retention_policies.table_name`.
- **Tipo**: `varchar(63)` / `varchar(63)` / `bigint`.
- **Nullabile**: Sì, **tutte e tre insieme** (vincolo `ck_..._asset`): sono NULL per gli oggetti che
  nessuna riga nomina — i 743.576 QR e i ~12.000 orfani.
- **Relazioni**: riferimento **testuale**, non FK: punta fuori dallo schema `privacy` per
  definizione, e nessun modulo vede le tabelle di un altro.

### storage_origin
- **Legacy**: — (nuovo)
- **Descrizione**: host dello storage **come era al momento della decisione**.
- **Tipo**: `varchar(32)`.
- **Nullabile**: No.
- **Nota**: testo e **non** un enum nativo, di proposito: i quattro moduli di asset hanno enum
  `media_origin` con etichette diverse fra loro (`catalog` usa `s3us_east2`, `fiskaly`
  `s3_us_east_2`, `switchboard` `s3_private`), e questa coda li raccoglie tutti. Un enum qui
  costringerebbe a tenere allineate cinque liste.

### storage_path
- **Legacy**: — (nuovo)
- **Descrizione**: chiave dell'oggetto **congelata** al momento della decisione.
- **Tipo**: `text`.
- **Nullabile**: No.
- **Nota**: congelata e non letta dalla riga di asset al momento dell'esecuzione, perché se il `path`
  cambia nel frattempo si deve cancellare **l'oggetto che era stato valutato**, non quello nuovo.

### byte_size
- **Descrizione**: dimensione nota dell'oggetto, per rendicontare lo spazio liberato.
- **Tipo**: `integer`. **Nullabile**: Sì — NULL se il metadato non era stato calcolato.

### action 🆕
- **Legacy**: — (nuova, 2026-10-05, migration `20261005113821_AddFileDeletionAction`)
- **Descrizione**: che cosa va fatto all'oggetto.
- **Tipo**: `privacy.retention_action`, default `delete_object`. **Nullabile**: No.
- **Vincolo**: `ck_privacy_file_deletions_action` — solo `delete_object` e `archive_object`. Delle sei
  etichette dell'enum, le altre quattro (`delete_row`, `null_fields`, `anonymize`, `archive_cold`)
  agiscono sulle **righe** e qui non vorrebbero dire niente.

| Valore | Che cosa fa il job |
|---|---|
| `delete_object` | rimuove l'oggetto (`DeleteObjects`), e valorizza `purged_at` sulla riga di asset se quella colonna esiste |
| `archive_object` | **non rimuove niente**: ricopia l'oggetto su se stesso con una `StorageClass` più fredda (`CopyObject` + `MetadataDirective = COPY`), e lo lascia dov'è |

> ### ⚠️ Perché questa colonna esiste, e che cosa succedeva senza
>
> Due policy su `fiskaly.media_assets` chiedono di **raffreddare** i PDF degli scontrini a 36 mesi, non
> di cancellarli: devono restare leggibili per dieci anni. Senza una colonna che distingua le due
> azioni, in coda erano indistinguibili, e il job avrebbe **cancellato** quei PDF.
>
> È il motivo per cui, fino al 2026-10-05, il motore **saltava** le policy `archive_object` con questo
> motivo scritto nel verbale. Il salto non era dovuto al perimetro: dichiararlo non avrebbe reso
> l'azione eseguibile.

> ### La classe di destinazione non sta qui
>
> Sta nella configurazione della destinazione: `Retention:Deletion:Targets[].ArchiveStorageClass`,
> default `GLACIER`. Il motivo è che «quale classe» è una decisione di **costo** e dipende
> dall'ambiente — LocalStack la accetta e la ignora, AWS la fattura — mentre la policy decide *che
> cosa* succede al dato, non *dove* finisce.
>
> ⚠️ `GLACIER` ha un **minimo di 90 giorni** di fatturazione: un oggetto raffreddato e poi cancellato
> prima dei 90 giorni si paga comunque per 90. Per gli scontrini — raffreddati a 36 mesi e cancellati a
> 10 anni — non è un problema.

> ### ⚠️ L'indice unico non include `action`, ed è voluto
>
> `uq_...` è su `(storage_origin, storage_path) WHERE executed_at IS NULL`: **una sola azione pendente
> per oggetto**. Includere `action` permetterebbe di accodare contemporaneamente il raffreddamento e la
> cancellazione dello stesso oggetto, senza nessuna garanzia sull'ordine di esecuzione — e raffreddare
> un oggetto già cancellato fallisce.
>
> La conseguenza è che, se un raffreddamento è ancora pendente, l'`ON CONFLICT DO NOTHING` scarta in
> silenzio la cancellazione: la corsa successiva la riaccoda, perché a quel punto la prima è eseguita.
> Si sistema da sé, ma va saputo leggendo un report che riporta meno righe di quelle attese.

### policy_id
- **Descrizione**: quale regola ha deciso l'azione.
- **Tipo**: `bigint`. **Nullabile**: Sì — `NULL` significa **cancellazione manuale** di un
  amministratore, che è un caso legittimo e va distinguibile da una decisione automatica.
- **Relazioni**: FK verso `privacy_retention_policies(id)`, `RESTRICT`.

### reason
- **Descrizione**: perché l'oggetto è in coda, in forma leggibile da chi apre il pannello.
- **Tipo**: `text`. **Nullabile**: No.
- **Nota**: si regge da solo, come `privacy_retention_policies.legal_basis`: non rimanda a documenti
  interni né a chi ha deciso il termine.

### decided_at / executed_at
- **Descrizione**: quando la decisione è stata presa; quando l'oggetto è stato davvero rimosso.
- **Tipo**: `timestamptz` (`decided_at` default `now()`).
- **Nullabile**: `decided_at` No, `executed_at` Sì — **`executed_at IS NULL` è il filtro della
  coda**, ed è la colonna su cui poggiano due dei tre indici parziali.

### attempts / last_error / last_error_at
- **Descrizione**: tentativi già fatti, ultimo errore riportato dallo storage e quando.
- **Tipo**: `smallint` (default `0`) / `text` / `timestamptz`.
- **Nota**: la forma è la stessa del checkpoint di `utility/02-migrazione-file`, e per la stessa
  ragione: le operazioni su storage falliscono indipendentemente dal database, e il numero di
  tentativi è l'unica cosa che distingue «non ancora fatto» da «non si riesce».

### created_at / updated_at
- **Tipo**: `timestamptz`, default `now()`. **Nullabile**: No.
- **Nota**: `IAuditable`, scritte dall'`AuditInterceptor`. **Non** `ISoftDeletable`: una riga di coda
  non si cancella logicamente — o è da fare, o è fatta.

## Valutazione JSON

Nessuna colonna JSON. Non serve: la forma di una cancellazione è chiusa e nota.

## Suggerimenti per le prestazioni

- l'indice parziale su `executed_at IS NULL` tiene la coda piccola anche quando la tabella è grossa:
  dopo la bonifica iniziale dei QR conterrà ~750.000 righe eseguite e qualche decina da fare;
- `uq_..._pending_path` è anche la protezione contro il doppio accodamento da parte di due job che
  valutano la stessa policy in parallelo: il secondo prende un errore di unicità invece di
  cancellare due volte;
- la bonifica iniziale (743.576 QR + ~12.000 orfani) va inserita a lotti, non in un'unica
  transazione: è lo stesso motivo per cui `utility/02-migrazione-file` committa ogni 20.000
  statement.

## Implementazione .NET

- entità: `Module.Privacy/Domain/Entities/FileDeletion.cs` (`IAuditable`, **non** `ISoftDeletable`);
- configurazione: `Module.Privacy/Infrastructure/Configurations/FileDeletionConfiguration.cs`;
- `DbSet<FileDeletion> FileDeletions` in `Module.Privacy/Infrastructure/DbContext.cs`;
- **nessun enum nuovo**: `storage_origin` è `varchar(32)` per la ragione detta sopra;
- migration: `20261003075631_AddPrivacyFileDeletionsQueue`.
