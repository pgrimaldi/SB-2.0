# 81 — Retention: il motore, le decisioni e i difetti

> Era `docs/_temp6.md`; spostato qui il 2026-10-06.
>
> Stato al **2026-10-06**. Il motore di retention esiste, è collaudato e le 61 policy sono accese.
>
> Questo documento è il **resoconto di come ci si è arrivati**: che cosa è andato storto, che cosa è
> stato deciso e perché, che cosa resta da fare. Il «come funziona» in forma di specifica sta in
> [`20-jobs.md` §1.5](20-jobs.md); le regole e i tempi in [`80-retention.md`](80-retention.md); il
> verbale delle scelte del titolare in [`82-retention-data.md`](82-retention-data.md); la retention dei
> file in [`83-retention-file.md`](83-retention-file.md).

---

## 1. Stato in una pagina

| | |
|---|---|
| **Regole** | **61 righe** di policy, 59 attive, tutte **accese** (`is_dry_run_only = false`) |
| **Tabelle** | 199 su 11 schemi |
| **Codice** | un **motore generico** in `BuildingBlocks.Infrastructure/Retention/`, non 19 job scritti a mano; **un** job con il perimetro nel codice, per l'unico caso che una riga di configurazione non esprime |
| **Test** | **165 verdi**: 90 `BuildingBlocks.Infrastructure.Tests` + 7 `Module.Catalog.Tests` + 68 `ArchitectureTests`. Build a 0 warning |
| **Ultimo ciclo completo** | 2026-10-05: 199 tabelle ri-migrate → ETL da zero (**6.163.412 righe, 0 errori**, identico alla corsa buona del 2026-10-03) → 2 corse di retention: **321.791 righe toccate, poi 0** |
| **Difetti incontrati** | **17**, tutti corretti (§6). Nessuno si vedeva leggendo il codice; **cinque** li hanno trovati le verifiche, non la lettura |
| **Policy** | **59 attive**, tutte accese. Due ritirate il 2026-10-06, sostituite da un meccanismo migliore (§7.9) |
| **Policy che non girano** | **9 su 59**: 8 su prefisso (serve un job che elenchi il bucket) + 1 affidata a un job dedicato. **Nessuna per un difetto** |
| **Verificato** | corsa reale, convergenza a zero, archivi riletti, **catena delle migration da zero: schema identico** (§8) |

---

## 2. Come funziona, in breve

Le regole non stanno nel codice: stanno in
[`privacy.privacy_retention_policies`](../analisi-post/db/privacy/privacy_retention_policies.md), una
riga per regola. Cambiare una durata è un `UPDATE`, non una release. Il motore legge quella tabella e
la applica.

Una corsa ha **tre passi**, in quest'ordine:

1. **il motore**, una volta per schema, sulla connessione del modulo proprietario;
2. **i job con il perimetro nel codice**, uno per ogni policy che il motore non sa esprimere;
3. **la riconciliazione della coda** su storage: rimuove gli oggetti e cambia classe a quelli da
   raffreddare.

### 2.1 Le sei azioni

| Azione | Che cosa fa | Su che cosa |
|---|---|---|
| `null_fields` | mette a NULL i campi elencati in `field_names` | riga |
| `anonymize` | sostituisce i campi con valori non riconoscibili, **lasciando la riga usabile** | riga |
| `archive_cold` | **esporta la riga fuori dal database** e poi la rimuove dal caldo | riga |
| `archive_object` | sposta un **file** su una classe di storage più fredda | oggetto |
| `delete_object` | rimuove un **file** dallo storage | oggetto |
| `delete_row` | cancella la riga | riga |

⚠️ **Quell'ordine è anche l'ordine di esecuzione fra policy della stessa tabella**, e non è casuale:
prima si **minimizza**, poi si **archivia**, poi si **cancella**. `booking.reservations` ha tre policy
— azzera le note a 1 stagione, archivia a 2, cancella a 10 anni — e se l'archiviazione girasse prima
dell'azzeramento l'archivio conterrebbe le note dei clienti, che possono contenere dati sulla salute
(art. 9 GDPR). **L'archivio non si riscrive.** Vedi il difetto §6.4.

### 2.2 `archive_cold`, per esteso

Alcuni dati non si possono cancellare ma non serve tenerli in linea. Uno scontrino fiscale va
conservato dieci anni, ma nessuno consulta quello di tre anni fa: tenerlo nel database caldo costa in
spazio, in backup e in tempo di query su ogni tabella che lo attraversa.

Quattro passi, e **l'ordine è la cosa importante**:

1. `SELECT row_to_json(...)` delle righe oltre la data di taglio;
2. compressione in **JSON Lines gzip**, una riga di database per riga di file, a lotti di 20.000;
3. caricamento su storage a oggetti, un oggetto per lotto, sotto una chiave che dice da sola cosa
   contiene: `archivio/<schema>/<tabella>/<anno>/<mese>/<istante>-<policy>-<lotto>.jsonl.gz`;
4. scrittura dell'**inventario** (§2.6) e **solo dopo** la `DELETE` dal database caldo.

> **Perché JSON Lines e non un dump SQL**: l'archivio deve restare leggibile anche quando lo schema che
> l'ha prodotto non esiste più. Un dump SQL presuppone quelle tabelle; un file di oggetti JSON no.
>
> **Perché l'ordine non è negoziabile**: se la scrittura dell'archivio fallisce, non si cancella niente
> e la policy risulta fallita. Se fallisce la cancellazione, resta un oggetto in più sull'archivio — il
> verso innocuo. **È già servito due volte**: i difetti §6.1 e §6.5 sono entrambi esplosi fra il punto
> 3 e il punto 4, e in entrambi i casi le righe erano ancora tutte nel database.

Prodotto nella corsa del 2026-10-03: 3 oggetti, 334.370 byte compressi, per `booking.reservations`
(650 righe), `payment.transaction_charges` (4.308) e `payment.wallet_transactions` (82).

### 2.3 Il perimetro: *quali* righe riguarda una policy

Una riga di policy dice *quando* un dato scade con grande precisione — durata, unità, colonna di
decorrenza. Sul *quali* sapeva dire molto meno, e questo è stato il nodo più lungo da sciogliere.

Per le policy **sulle righe** basta il nome della tabella, perché una tabella contiene una cosa sola:
tutte le righe di `notification.deliveries` sono invii, e «tutti gli invii oltre 12 mesi» è una frase
completa.

Per le policy **sui file** non basta, perché **una tabella di asset contiene file di natura diversa**.
`notification.media_assets` ha sei `kind`: allegati di modelli, allegati di campagne, immagini di
copertina degli eventi, PDF generati, caricamenti in entrata, altro. La policy, letta alla lettera,
diceva «cancella tutti i file di notification oltre 24 mesi»; la regola decisa dal titolare riguarda
**solo** i PDF dei report alle strutture convenzionate. Un job che l'avesse eseguita avrebbe cancellato
anche gli allegati delle campagne — non per un errore di programmazione, ma perché la policy glielo
stava dicendo.

Oggi il perimetro si dichiara, in due colonne:

| Colonna | Che cosa dice |
|---|---|
| `only_if_unreferenced` | «solo i file che nessuna riga nomina più». Si risolve leggendo `pg_constraint`, una `NOT EXISTS` per ogni chiave esterna in entrata — non un elenco cablato, che si disallineerebbe alla prima tabella nuova **in silenzio** |
| `target_filter` (`jsonb`) | `{"scope": "whole_table"}` oppure `{"column": "kind", "op": "=", "value": "rendered_pdf"}`. Operatori: `=` `<>` `in` `not in` `is null` `is not null` |

**Il cancello del motore**: un'azione su storage si esegue **solo** se la policy dichiara
`only_if_unreferenced` **oppure** un `target_filter`. Senza, viene saltata con il motivo scritto nel
verbale.

> ### `{"scope": "whole_table"}` non è ridondante
>
> Produce lo stesso SQL di «nessun filtro» — nessuna condizione aggiunta — e serve comunque: trasforma
> **«nessuno ha detto quali»** in **«qualcuno ha detto tutte»**. È la differenza fra una policy che il
> motore esegue e una che salta, e su un file un perimetro indovinato non si annulla.
>
> Senza di esso le tabelle di asset omogenee — `fiskaly.media_assets`, dove ogni riga è uno scontrino —
> non avrebbero potuto girare.

> ### ⚠️ Perché su fiskaly c'è un elenco esplicito e non `whole_table`
>
> Il perimetro seminato è `{"column":"kind","op":"in","values":["fiscal_receipt_pdf","non_fiscal_receipt_pdf"]}`,
> che oggi equivale a «tutta la tabella»: l'enum ha due sole etichette, entrambe scontrini.
>
> Ma se un domani qualcuno aggiunge un'etichetta a `fiskaly.media_kind`, con `whole_table` la policy
> comincerebbe **da sola** a cancellare anche quella. **Un perimetro che si allarga quando si allarga
> un enum è un perimetro che nessuno ha deciso.**

> ### ⚠️ I valori di un filtro sono limitati a token, e non è pigrizia
>
> `^[A-Za-z0-9_.:-]{1,64}$`. Un valore con uno spazio, un apice o una barra — un content type come
> `image/png` — **non si può dichiarare**, e la policy fallisce con un messaggio chiaro.
>
> Il predicato di una policy viene composto in **sette punti diversi** del motore: i conteggi, la
> `DELETE`, i due `UPDATE`, l'export dell'archivio, l'accodamento. Ognuno ha la propria lista di
> parametri. Passare il valore come parametro avrebbe richiesto di propagarne uno attraverso tutti e
> sette, e **l'errore che conta davvero è dimenticarsene in uno**: quel punto avrebbe eseguito la policy
> *senza* il perimetro — cancellando più del dovuto — e in silenzio, perché un `WHERE` più largo non dà
> errore.
>
> Con i valori ridotti a token l'iniezione è impossibile per costruzione su tre fronti: il nome della
> colonna è verificato contro il catalogo di PostgreSQL, l'operatore viene da un elenco chiuso di sei
> voci, e il carattere con cui si uscirebbe dal letterale è rifiutato prima di arrivare all'SQL. È una
> restrizione reale e va saputa; se un giorno servirà di più, si propagano i parametri nei sette punti.
>
> Il codice sta in un file suo — `BuildingBlocks.Infrastructure/Retention/TargetFilter.cs` — perché è l'unico pezzo che prende
> un dato scritto in una tabella e lo trasforma in SQL: è la parte da leggere per prima se ci si chiede
> se una policy possa far danni.

**Perché non si è riusato `field_names`.** Quella colonna dice su quali colonne agisce `null_fields`:
usarla anche per il perimetro le avrebbe dato due significati a seconda dell'azione. E non basterebbe,
perché i perimetri non sono tutti della stessa forma — vedi il caso `catalog` al §5.

### 2.4 Il file che **segue** le righe archiviate

Alcune righe nominano un file che è **parte del dato**, non un accessorio: la riga di
`fiskaly.receipts` nomina il PDF dello scontrino. Archiviare la riga e lasciare il PDF dov'era produce
due guai insieme:

- nessuna riga calda nomina più quel PDF;
- la data da cui scade — `signed_at` — è uscita dal database insieme alla riga.

Non resta **niente** che possa decidere quando cancellarlo.

> ### ⚠️⚠️ E non si risolve con «cancella i file che nessuno nomina più»
>
> È la soluzione che viene in mente per prima, ed è un disastro: dopo l'archiviazione il PDF di uno
> scontrino **è** per definizione «non nominato da nessuno», quindi quella policy lo cancellerebbe al
> **mese 36** invece che all'**anno 10**. Distruggerebbe esattamente il documento che l'obbligo fiscale
> impone di conservare.
>
> È il motivo per cui `fiskaly.media_assets` **non ha, e non deve avere**, una policy
> `only_if_unreferenced`.

La policy dichiara quindi il file che la segue, in `linked_asset`:

```jsonc
{"column": "pdf_asset_id", "table": "media_assets", "action": "archive_object"}
```

Solo su `archive_cold` — su una cancellazione non servirebbe, su un azzeramento non vorrebbe dire
niente (vincolo `ck_..._linked_asset_action`). Il motore, **dopo** aver scritto l'archivio e **prima**
di cancellare le righe, fa due cose:

| | |
|---|---|
| accoda l'azione dichiarata sul file | `archive_object` lo raffredda, `delete_object` lo rimuove. ⚠️ Con l'origine **dell'asset**, non quella dell'archivio: il PDF sta sul bucket dei file |
| **inventaria** il file in `privacy_archive_manifests` con `object_kind = 'linked_asset'` | così la policy decennale su quell'inventario cancella il PDF **insieme** all'archivio delle righe che accompagnava |

**Una sola regola per i due pezzi della coppia** — ed è il punto: scadere in momenti diversi era il
difetto.

> L'inventario del file porta `cutoff_at` e `archived_at` **della corsa**, non del file: la sua
> scadenza decorre da quando ha lasciato il database caldo, come per l'archivio delle righe. Serve che
> sia la stessa data, o i due pezzi tornerebbero a scadere separati.

### 2.5 La cessazione dello stabilimento

Due policy hanno come decorrenza un **evento**, `property_ceased`, non una colonna data. «Cessazione»
non corrisponde a nessuno stato a database — `catalog.property_status` è
`pending | approved | rejected | imported` e non dice niente sull'abbonamento — quindi si **deriva**
(decisione del titolare del 2026-10-02, D-65 punto 1): una colonna di stato che qualcuno deve
ricordarsi di aggiornare fa scadere i dati di uno stabilimento ancora attivo.

`BuildingBlocks.Infrastructure/Retention/PropertyCeasedPredicate.cs` la costruisce in SQL, in **due metà che servono
entrambe**:

1. **ha chiuso** — esiste un abbonamento `cancelled` o `expired`, e `max(closed_at)` dei suoi periodi è
   anteriore al taglio;
2. **e non è ripartito** — *nessun* abbonamento è `active` o `suspended`.

> ⚠️ **La seconda metà è quella che conta.** Senza, uno stabilimento che ha disdetto il piano vecchio e
> ne ha sottoscritto uno nuovo risulterebbe cessato, e la retention gli cancellerebbe le foto mentre
> sta lavorando. Un abbonamento **sospeso** non è una cessazione: era l'obiezione che aveva bloccato
> D-65, e sta scritta lì.

### 2.6 Il verbale della corsa e l'inventario degli archivi

Sono **due tabelle**, e la differenza è la ragione per cui sono due.

| | [`privacy_retention_runs`](../analisi-post/db/privacy/privacy_retention_runs.md) | [`privacy_archive_manifests`](../analisi-post/db/privacy/privacy_archive_manifests.md) |
|---|---|---|
| granularità | una riga per **(corsa, policy)** | una riga per **oggetto di archivio** |
| natura | **operativa**: com'è andata quella notte | **di inventario**: dove sta un oggetto |
| contiene | taglio, righe trovate, righe toccate, esito, errore, tempi | schema e tabella d'origine, `row_count`, `cutoff_at`, `archived_at`, `purged_at` |
| termine | **10 anni** | **10 anni** (vive quanto l'oggetto che indirizza) |

Il verbale serve a tre cose che nessun'altra tabella copre: **raccogliere gli errori** — il motore non
si ferma alla prima policy che fallisce, perché una tabella trattenuta da una chiave esterna non deve
impedire alle altre 60 di scadere, e il log di un processo notturno non si ritrova; **leggere il report
di prova** prima di autorizzare l'esecuzione reale; e **documentare che la regola è stata applicata**,
che è la prova richiesta dall'art. 5.2 GDPR.

Si legge così:

```sql
SELECT table_name, action, cutoff_at, matched_rows, affected_rows, status, detail
  FROM privacy.privacy_retention_runs
 WHERE run_id = '...'
 ORDER BY schema_name, table_name;
```

⚠️ Il verbale conserva ancora `archive_origin`, `archive_path` e `archive_byte_size`, ma sono un **eco
operativo, non l'inventario**: servono a leggere il verbale di una corsa senza fare una join. L'elenco
autorevole di dove sono gli archivi è `privacy_archive_manifests`. I commenti delle colonne lo dicono,
perché una colonna che resta è esattamente il tipo di cosa che qualcuno riusa credendola completa.

### 2.7 La coda delle azioni su storage

Un'operazione su S3 **non partecipa alla transazione del database**. Quindi le azioni su storage non si
eseguono in linea: si **accodano** in
[`privacy.privacy_file_deletions`](../analisi-post/db/privacy/privacy_file_deletions.md), e un job a
parte la svuota.

⚠️ **Il nome della tabella dice meno di quello che fa.** Nasce il 2026-10-03, quando l'unica azione era
la cancellazione; dal 2026-10-05 la colonna `action` porta anche `archive_object`, che **non cancella
niente**. La colonna da guardare è `action`: un job che la ignorasse rimuoverebbe i PDF degli scontrini
fiscali che la policy vuole soltanto raffreddare.

| Azione in coda | Che cosa fa il job |
|---|---|
| `delete_object` | `DeleteObjects`, e valorizza `purged_at` sulla riga di asset se quella colonna esiste |
| `archive_object` | `CopyObject` **dell'oggetto su se stesso** con la `StorageClass` nuova e `MetadataDirective = COPY` |

**Le due azioni si eseguono separatamente anche dentro lo stesso lotto**, raggruppate per azione e non
per chiave: non esiste nessun percorso in cui una riga `archive_object` finisca in una
`DeleteObjectsRequest`. Una richiesta di cancellazione di massa sbagliata non si annulla.

Due protezioni deliberate nel job che svuota la coda:

| Protezione | Che cosa impedisce |
|---|---|
| un `storage_origin` **senza destinazione configurata non viene toccato** | che un ambiente di sviluppo cancelli dal bucket di produzione perché nessuno ha letto la configurazione. Il job riporta «*n* oggetti non toccati» invece di indovinare un bucket |
| in **prova** non parte nessuna richiesta | che una verifica diventi un'esecuzione |

---

## 3. Dove sta il codice

### Il motore — `BuildingBlocks.Infrastructure/Retention/` (non dipende da nessun modulo)

| File | Cosa c'è |
|---|---|
| `RetentionEngine.cs` | **il cuore**: legge le policy del proprio schema, risolve il nome fisico della tabella, calcola il taglio, applica l'azione, accoda le azioni su storage, scrive il verbale. Non si ferma al primo errore |
| `TargetFilter.cs` | traduce il perimetro dichiarato in un frammento di `WHERE`. L'unico pezzo che trasforma un dato di tabella in SQL |
| `LinkedAssetSpec.cs` | la dichiarazione «questo file segue le righe». Come `TargetFilter`: un dato di tabella che diventa SQL |
| `PropertyCeasedPredicate.cs` | la cessazione di uno stabilimento, derivata da `identity.account_subscriptions` |
| `SeasonBoundary.cs` | **l'unica** funzione del confine stagionale: 1 gennaio dell'anno N−k |
| `S3ArchiveStore.cs` / `IArchiveStore.cs` | l'archivio delle policy `archive_cold`: JSON Lines compresso, un oggetto per lotto |
| `StorageDeletionReconciler.cs` | svuota la coda: rimuove gli oggetti, cambia classe a quelli da raffreddare, segna `purged_at` |
| `RetentionRunLog.cs` | l'`INSERT` del verbale, **condiviso** fra il motore e i job con il perimetro nel codice |
| `IModuleRetentionJob.cs` | il job di **un** modulo, più le registrazioni DI |
| `IBespokeRetentionJob.cs` | un job che applica **una** policy il cui perimetro non è dichiarabile |
| `RetentionModel.cs` | `RetentionPolicyRecord`, `PolicyOutcome`, `SchemaRetentionReport`, `RetentionRunOptions` |

> **Perché in `Shared` e non in un modulo**: deve servirli tutti, e `Shared` è l'unico posto da cui può
> farlo senza dipendere da nessuno — regola verificata da `SharedArchitectureTests`. Per la stessa
> ragione non conosce nessuna entità: lavora su `DbConnection` e nomi di tabella.

### Le tabelle — `Module.Privacy/`

| File | Che cos'è |
|---|---|
| `Domain/Entities/RetentionRun.cs` + configurazione | il **verbale** di una corsa |
| `Domain/Entities/ArchiveManifest.cs` + configurazione | l'**inventario** degli archivi |
| `Domain/Entities/FileDeletion.cs` + configurazione | la **coda** delle azioni su storage |

### Il job con il perimetro nel codice — `Module.Catalog/`

`Infrastructure/Infras/CeasedPropertyPhotoRetentionJob.cs`. Sta in Catalog e non in `Shared` perché usa
la connessione del **proprio** modulo; che la sua SQL legga anche `identity` è un fatto del database —
gli schemi stanno nello stesso — non una dipendenza di codice verso un altro modulo.

### La registrazione — una riga per modulo

`Module.<Nome>/Infrastructure/Infras/ModulePersistenceRegistration.cs`, **tutti e 11**:

```csharp
services.AddModuleRetention<DbContext>(DbContext.Schema);
```

E **solo** in `Module.Privacy`, perché la coda è una e non una per modulo:

```csharp
services.AddFileDeletionQueue<DbContext>(deletion);
```

E **solo** in `Module.Catalog`, per il job dedicato:

```csharp
services.AddBespokeRetention<CeasedPropertyPhotoRetentionJob, DbContext>();
```

### Le migration

> **Dal 2026-10-06 queste migration non esistono più come file**: ogni modulo ha una sola
> `InitialCreate` che le consolida, e il seed delle policy è un solo `INSERT` con i valori finali.
> L'elenco resta come storia di come ci si è arrivati. Dettagli e verifica in
> [`42-migration.md` §10](42-migration.md).

| Modulo | Migration (storiche) |
|---|---|
| Privacy, 2026-10-03 | `AddRetentionObjectActionsAndFileEventEnums` · `AddRetentionFileTargetingColumns` · `AddPrivacyFileDeletionsQueue` · `AddRetentionDecorrenzaCheck` · `SeedFileRetentionPolicies` · `AddPrivacyRetentionRuns` |
| Privacy, 2026-10-05 | `SeedRetentionRunsRetentionPolicy` · `AddPrivacyArchiveManifests` · `SeedArchiveManifestRetentionPolicy` · `AddRetentionTargetFilter` · `AddFileDeletionAction` · `RaiseComplianceProofRetentionToTenYears` |
| Privacy, 2026-10-06 | `AddLinkedAssetArchiving`: enum `archive_object_kind`, colonne `linked_asset` e `object_kind`, seed del collegamento e ritiro delle due policy di fiskaly |
| Catalog | `AddMediaAssetPropertyId` · `AddAgreementReports` (2026-10-03) · `FixMediaAssetPropertyIdType` (2026-10-05) |

---

## 4. Come si esegue, e come si accende una policy

### Eseguire

```bash
dotnet run --project Boot -- retention [--apply] [--schema NOME] [--no-seasonal]
```

Senza `--apply` non scrive niente. ⚠️ Una policy con `is_dry_run_only = true` resta in prova **anche
con `--apply`**: quel flag è la decisione di una persona e il motore non lo scavalca.

Configurazione in `Boot/appsettings.Development.json`, sezione `Retention`: `Archive` (bucket
dell'archivio) e `Deletion:Targets` (destinazioni della coda, una per `storage_origin`).

### Accendere

```bash
dotnet run --project Boot -- retention-enable --list        # chi è accesa e chi è in prova
dotnet run --project Boot -- retention-enable <public_id>   # accende una policy
dotnet run --project Boot -- retention-enable --all         # tutte, con conferma da tastiera
dotnet run --project Boot -- retention-enable --all --dry-run-again   # torna indietro
```

`--all` chiede di scrivere `confermo`; `--yes` salta la domanda per gli usi non interattivi.

> ### Perché accendere è un comando, e non le due alternative
>
> | Alternativa | Perché no |
> |---|---|
> | una **migration** | una policy nasce `is_dry_run_only = true` e non tocca niente finché una persona non la accende dopo aver letto il report: è la protezione che regge tutto l'impianto. Una migration la farebbe nascere **accesa** in ogni ambiente, produzione compresa |
> | un **`UPDATE` a mano** | non lascia traccia, non si rivede, e si perde al primo ricaricamento del database |
>
> **La seconda non è un'ipotesi: è accaduta il 2026-10-05.** Un drop e rimigrazione ha riportato tutte
> le 61 policy in prova, perché l'accensione viveva solo come `UPDATE` manuale. Le corse successive non
> avrebbero archiviato niente, e **niente lo avrebbe detto**: un report di prova ha esattamente l'aria
> di un report riuscito, con `matched_rows` pieno e `affected_rows` a zero.
>
> È anche il motivo per cui esiste `--dry-run-again`: accendere deve essere reversibile con la stessa
> facilità.

Il verbo è **separato** da `retention` di proposito: accendere non esegue niente, modifica una riga di
configurazione. Tenerli distinti evita che un `--apply` di troppo accenda qualcosa oltre a eseguirlo.

> ⚠️ **In sviluppo serve `DOTNET_ENVIRONMENT=Development`** su entrambi i verbi, altrimenti la stringa
> di connessione non si risolve: `Host.CreateApplicationBuilder` legge quella variabile e **non**
> `ASPNETCORE_ENVIRONMENT`.

---

## 5. Che cosa gira, e che cosa no

**50 policy su 59 girano, e la corsa del 2026-10-06 ha chiuso con 0 fallite.** Le altre nove non
girano per un motivo dichiarato, e **nessuna per un difetto**.

| Quante | Non gira perché | Che cosa servirebbe |
|---|---|---|
| **8** | sono su **prefisso di storage**: nominano percorsi, non righe. Per sapere quali oggetti sono scaduti serve chiedere l'elenco delle chiavi allo storage, e il database non sa che esistono | un job che elenchi il bucket. ⚠️ È la **forma più pericolosa** di tutta la politica: cancellare per prefisso senza una riga che trattenga i file. Vedi §7.1 |
| **1** | `catalog.media_assets` · cessazione: il perimetro non è dichiarabile | è già fatto — la applica un **job dedicato**, e il motore la salta *in silenzio* per non scrivere due righe di verbale per la stessa policy |

### Il caso `catalog`, che mostra il limite di una regola dichiarativa

La regola è: «le prime 3 foto per `sort_order` più la copertina restano pubblicate **per sempre**, le
altre seguono la cessazione». Sono due condizioni che nessuna sintassi di filtro ragionevole porta:

- «escludi le prime N secondo un **ordinamento**»;
- «escludi quella puntata da `bathhouses.main_photo_id`, che sta su **un'altra tabella**».

`target_filter` resta **NULL di proposito** su quella riga. ⚠️⚠️ Se qualcuno gliene dichiarasse uno, il
motore comincerebbe a eseguirla **oltre** al job, con un perimetro più largo.

Il perimetro del job, nelle quattro condizioni che lo compongono:

| Condizione | Perché |
|---|---|
| `posizione > 3`, con `row_number()` su `sort_order, id` | le prime tre restano. L'ordinamento secondario su `id` rende la posizione deterministica quando due foto hanno lo stesso `sort_order`: senza, due corse sceglierebbero due terzine diverse |
| non è `bathhouses.main_photo_id` | la copertina **può non essere fra le prime tre**, quindi la condizione sopra non la copre |
| lo stabilimento ha cessato prima del taglio | `PropertyCeasedPredicate`, §2.5 |
| non è già in coda per la stessa azione | l'indice unico della coda protegge solo le righe pendenti: dopo l'esecuzione sparisce, e senza questa condizione il job riaccoderebbe le stesse foto a ogni corsa |

⚠️ `bathhouse_photos.deleted_at IS NULL` sta **dentro** la finestra che calcola la posizione, non
fuori: altrimenti basterebbe cancellare la prima foto per far scadere la quarta. C'è un test apposta.

**Il job non caba nessun tempo**: legge la sua policy per `public_id` e ne prende durata, unità e
`is_dry_run_only`. Nel codice sta **solo il perimetro**. Se la policy non c'è, non è attiva o è
cancellata logicamente, il job non fa niente e lo scrive nel verbale.

### ⚠️ Due policy girano ma non troveranno niente, e non è un difetto

Le due con decorrenza `property_ceased` (`catalog` e `switchboard`) si appoggiano a
`identity.account_subscriptions`, che è **vuota**: nel legacy gli abbonamenti erano gestiti **fuori dal
portale** e attivati a mano, quindi non c'è niente da migrare (risposta del titolare del 2026-10-02,
Q10 — vedi [`_colonne-vuote.md`](../analisi-pre/db-no-copied/_colonne-vuote.md), Parte 3).

Troveranno zero righe **per costruzione**, e continueranno a trovarne zero finché gli abbonamenti non
saranno gestiti dal nuovo sistema. Il meccanismo è scritto e verificato con **dati sintetici**, perché
altrimenti sarebbe codice mai eseguito pronto a sbagliare — e a sbagliare *cancellando foto* — la notte
in cui i primi abbonamenti ci saranno.

### ⚠️ La seconda policy di una coppia riporta sempre «0 righe»

Cinque tabelle hanno **due** policy: archivia a N, cancella a 10 anni — `fiskaly.receipts`,
`payment.ledger_entries`, `payment.pos_charges`, `booking.reservations`,
`booking.resale_credit_ledger`. Siccome `archive_cold` **sposta**, la seconda fase non trova più
niente: le righe hanno lasciato il database alla prima.

Non è un errore, e gli archivi **non restano senza scadenza** — ci pensa la policy decennale
sull'inventario (§7.3). Ma il report della seconda policy ha l'aria di una regola che non ha niente da
fare, e va letto sapendolo.

---

## 6. I difetti incontrati, e cosa insegnano

Diciassette, tutti corretti. Li elenco perché **nessuno si vedeva leggendo il codice**, e metà avrebbe rotto
qualcosa in silenzio.

### Trovati dai test, prima di toccare dati

**6.1 `DisablePayloadSigning` funziona solo su HTTPS.** Impostato per «agevolare» un endpoint non-AWS.
L'SDK v4 lo rifiuta su HTTP, e LocalStack parla HTTP: **ogni** policy `archive_cold` sarebbe fallita
alla prima corsa.

**6.2 `RegionEndpoint` assegnato dopo `ServiceURL` azzera l'endpoint.** Le richieste partono verso
**AWS vera** invece che verso l'endpoint configurato. Con un archivio di righe di produzione
finirebbero sul bucket sbagliato. L'errore che si legge è «The AWS Access Key Id you provided does not
exist in our records», che sembra un problema di credenziali e invece è di indirizzo — il tipo di
errore su cui si perde mezza giornata. Corretto con un avviso esplicito in **entrambi** i punti che
costruiscono un client S3.

**6.3 `asset_schema` nella coda è annullabile, e il job ci andava a sbattere.** Le tre colonne
dell'asset sono annullabili **per progetto**, perché una riga in coda può venire da una policy su
**prefisso**, che indirizza oggetti senza nessuna riga di asset dietro. Due test del riconciliatore
sono diventati rossi: la cancellazione dell'oggetto — il lavoro — sarebbe fallita per non poter
scrivere una data accessoria.

### Trovati da una corsa reale, cioè nel posto peggiore

**6.4 L'ordine fra policy della stessa tabella non era deliberato.** Funzionava per caso, perché
`duration_value` cresceva nell'ordine giusto. Ora il motore ordina esplicitamente **minimizza →
archivia → cancella** (§2.1). Verificato rileggendo l'archivio: la colonna `note` è NULL su tutte e 650
le righe archiviate.

**6.5 Lo stream compresso veniva chiuso due volte.** Tutte e tre le policy `archive_cold` con righe da
archiviare sono fallite con «Cannot access a disposed object: GZipStream», **dopo** aver caricato
l'oggetto su S3.

> ⚠️ **Qui si è visto che l'ordine a prova di perdita funziona.** L'errore è arrivato fra l'archivio e
> la cancellazione, quindi i tre archivi erano scritti e le **650 + 4.308 + 82 = 5.040 righe erano
> ancora tutte nel database**. Il danno sono stati tre oggetti in più sul bucket, non righe perdute. Con
> l'ordine inverso quella corsa avrebbe cancellato 5.040 righe e poi fallito l'archiviazione.

**6.6 Il timeout predefinito di Npgsql è sbagliato per un job batch.** Misurato con `EXPLAIN ANALYZE`:
cancellare **650** righe di `booking.reservations` richiede **42,7 secondi**. Non per il volume — quelle
righe non hanno nemmeno una figlia — ma per i **trigger di verifica delle 14 chiavi esterne** in
entrata, uno per riga per vincolo. Il default di Npgsql è **30 secondi**, e la policy è fallita *dopo*
aver scritto l'archivio.

Corretto con `StatementTimeoutSeconds` (1800), applicato da un'**unica fabbrica di comandi**:
dimenticarlo su uno solo, quello della cancellazione, è esattamente l'errore che è costato la corsa.

> ⚠️ **Il numero da ricordare**: 650 righe in 43 secondi significa che su volumi veri il collo di
> bottiglia non è la cancellazione, è la verifica delle chiavi esterne. Vedi §10.1.

**6.7 `null_fields` e `anonymize` non erano idempotenti.** La seconda corsa ha riscritto **41.943**
righe di `booking.reservations`, **17.257** di `customer_customers` e **3.575** di `deliveries` **già
azzerate**, perché la condizione guardava solo la data: ~63.000 versioni di riga al giorno da far
ripulire al vacuum, per niente.

Corretto con `IS NOT NULL` (azzeramento) e `IS DISTINCT FROM <espressione>` (anonimizzazione), **e lo
stesso filtro vale per il conteggio** — così il verbale dice quanto *resta* da fare, che è il numero su
cui si decide. Verificato: la terza corsa trova 0 righe.

### Trovati ragionando sul progetto

**6.8 L'inventario dell'archivio scadeva prima dell'archivio.** Il manifesto stava in
`privacy_retention_runs.archive_path`, e la policy nuova sul verbale lo cancellava a 24 mesi, mentre
gli archivi vivono dieci anni. Fra il mese 24 e l'anno 10 ci sarebbero stati sullo storage oggetti **di
cui il database non sapeva più niente** — e la policy che li deve cancellare non avrebbe avuto nessuna
fonte da cui partire. Risolto spostando l'inventario in una tabella sua (§2.6).

**6.9 L'etichetta dello storage dell'archivio era la stessa dei file applicativi.**
`Retention:Archive:Origin` valeva `s3_localstack`, che in `Retention:Deletion:Targets` punta al bucket
dei file. La policy decennale avrebbe cercato le chiavi `archivio/...` **nel bucket sbagliato**: nel
migliore dei casi non trovando niente, e non cancellando mai l'archivio. Ora è `s3_archivio_local`, con
la sua destinazione sul bucket dedicato.

⚠️ Un difetto che un test non avrebbe preso: la configurazione era sintatticamente valida e il codice
corretto.

**6.10 Un oggetto già cancellato veniva riaccodato a ogni corsa.** L'indice unico della coda protegge
solo finché la cancellazione è **pendente** — il suo filtro è `executed_at IS NULL` — quindi appena il
job la esegue la protezione sparisce, e la riga di asset, che esiste ancora, torna a cadere nel
perimetro. Aggiunta una guardia `NOT EXISTS` su **(asset, azione)**: non sul percorso, perché un
percorso riusato è un oggetto nuovo; e con l'azione, perché un oggetto raffreddato a 36 mesi deve poter
essere cancellato a 10 anni.

**6.11 La clausola su `property_id` si applicava a tabelle a cui non doveva.** Il motore escludeva
`AND property_id IS NULL` su **qualunque** tabella avesse quella colonna. La regola nasce per
`catalog.media_assets`, dove `property_id` marca le 272 foto di stabilimenti senza vetrina — file senza
riga di aggancio che **non** sono orfani.

Ma `notification.media_assets` ha anche lei una colonna `property_id`, **valorizzata**: un report
appartiene a uno stabilimento. Quindi la policy dei PDF dei report avrebbe trovato **zero righe** e
scritto nel verbale «niente da fare», mentre i file c'erano. È il verso peggiore di un errore:
silenzioso, e con l'aria di un lavoro fatto. Ora la clausola si applica **solo** alle policy
`only_if_unreferenced`, di cui è un raffinamento.

**6.12 `archive_object` sarebbe diventata una cancellazione.** La coda sapeva fare una cosa sola —
cancellare — e non c'era nessuna colonna che distinguesse «rimuovi» da «cambia classe». Allentando il
cancello sul perimetro, le due policy `archive_object` di `fiskaly` avrebbero accodato **alla
cancellazione** i PDF degli scontrini fiscali che devono vivere dieci anni. Risolto con la colonna
`action` (§2.7).

### Trovati dalla verifica finale, il 2026-10-05

**6.13 Due policy di fiskaly puntano a una colonna che la loro tabella non ha.**
`fiskaly.media_assets` ha entrambe le policy su storage con `reference_date_column = 'signed_at'`, ma
`signed_at` sta su `fiskaly.receipts`, non sull'asset. Le due policy **falliscono**, e il motore lo
dice invece di indovinare una data.

Era un difetto **mascherato**: prima del 2026-10-05 quelle due policy venivano saltate dal cancello
sul perimetro *prima* che il controllo sulle colonne venisse eseguito. Dichiarare il perimetro le ha
portate oltre quel cancello, e il problema vero è emerso.

⚠️ **Non è solo una colonna sbagliata nel seed, ed è il motivo per cui non l'ho corretta da solo.** Il
legame è `fiskaly.receipts.pdf_asset_id → media_assets.id`, quindi la data del PDF si ricava dalla
ricevuta. Ma `fiskaly.receipts` ha una policy `archive_cold` a 36 mesi: **a quel punto la riga della
ricevuta lascia il database caldo**, portandosi via `signed_at` e il collegamento al PDF. L'asset
resta senza data e senza nessuno che lo nomini. Le tre strade:

| Strada | Cosa comporta |
|---|---|
| decorrenza **`created_at` dell'asset** | il PDF viene creato nell'istante della firma, quindi in pratica coincide. Costa un `UPDATE` su due righe di policy. ⚠️ Ma è un'approssimazione su un termine di legge |
| un **job dedicato** come quello di catalog | giunge `receipts` su `pdf_asset_id` e usa `signed_at` esatto. ⚠️ Smette di funzionare dopo i 36 mesi, quando la ricevuta è archiviata |
| far **archiviare insieme** ricevuta e PDF | la più corretta e la più grossa: `archive_cold` dovrebbe trattare anche l'oggetto collegato |

✅ **Deciso il 2026-10-06: far archiviare insieme ricevuta e PDF** — la terza strada, quella più
corretta. Implementata: vedi §2.4 per il meccanismo e §7.9 per le conseguenze sulle due policy.

**6.14 Il job dedicato non scriveva il suo verbale nel caso più comune.** I `return` anticipati —
«policy assente», «zero righe», «prova» — scavalcavano la scrittura del verbale, che stava dopo il
`try`. Risultato: con zero righe il job era **muto**, e il verbale di una corsa aveva **60 righe
invece di 61** senza che niente lo segnalasse. Un job muto è indistinguibile da un job che non è
partito.

Trovato **contando le righe del verbale**, non leggendo il codice. Corretto spostando il lavoro in un
metodo a parte, così il verbale si scrive sempre.

**6.15 L'ETL rimetteva due chiavi esterne senza `ON DELETE CASCADE`.** Trovato confrontando lo schema
di un database costruito **da zero** con quello di sviluppo dopo l'ETL: due righe di `pg_dump` su
9.600.

`custom_transforms/catalog_map_spots.py` toglie le chiavi esterne in entrata su `catalog.map_spots`
per poter ricaricare la tabella, e le rimetteva da un **elenco cablato** in cui il comportamento in
cancellazione era `None` per entrambe, mentre la migration `InitialCreate` le dichiara `Cascade`.

⚠️ **Non dava nessun errore**: il vincolo veniva creato, validato, e tutto sembrava riuscito. Lo
schema del database restava semplicemente **diverso** da quello che le migration descrivono — e la
differenza si sarebbe scoperta il giorno in cui una cancellazione che doveva propagarsi non si
propagava.

Corretto alla radice, non nei valori: `custom_transforms/_fk_roundtrip.py` legge la definizione vera
dal catalogo (`pg_get_constraintdef`) **prima** di togliere un vincolo e rimette quella. Gli elenchi
dei tre file che fanno questo giro — `catalog_map_spots`, `catalog_media_assets`,
`fiskaly_media_assets` — si riducono ai **nomi**: niente di cablato può più essere sbagliato. Gli
altri undici vincoli, verificati uno per uno contro le migration, erano giusti.

### Trovati scrivendo il test del meccanismo nuovo, il 2026-10-06

**6.16 `now()` dentro una transazione è congelato, e faceva saltare il verbale.** Il verbale scriveva
`finished_at = now()`. In una transazione esplicita `now()` vale l'**istante di apertura della
transazione**, quindi risulta *anteriore* a `started_at` — che il motore passa come parametro, letto
dall'orologio del processo — e il vincolo `ck_privacy_retention_runs_times` salta.

In autocommit, che è come il motore gira oggi, i due sono indistinguibili e il difetto **non si vede**.
Si è visto scrivendo un test che avvolge la corsa in una transazione per poterla annullare.

⚠️ **La conseguenza era peggiore del sintomo.** L'errore veniva **inghiottito** — un verbale che non si
scrive non deve far fallire la corsa, ed è giusto così — ma lasciava la **transazione interrotta**, e
*tutte* le policy successive fallivano con `25P02 transazione corrente interrotta` senza che il motivo
vero comparisse da nessuna parte. Un errore inghiottito che ne genera tre illeggibili è peggio di un
errore che si vede.

Corretto con `clock_timestamp()`, che è l'ora vera anche dentro una transazione.

**6.17 L'asserzione del test diceva il conteggio e non i messaggi.** `Assert.Equal(0, report.Failed)`
produce «atteso 0, trovato 3» e costringe a rieseguire con un debugger. Ora l'asserzione confronta la
**stringa degli errori**, che li stampa. Non è un difetto del prodotto, ma è il motivo per cui il
difetto §6.16 è costato tre tentativi invece di uno.

### Un'imprecisione, non un difetto

Il motivo del salto sulle policy a evento diceva «azione su storage senza colonna di decorrenza», e
mandava a cercare una colonna che **non deve esistere**: quelle policy prendono la data da un evento.
Ora nomina l'evento e dice dove si deriva.

---

## 7. Le decisioni prese

### 7.1 I file che nessuna riga nomina: la bonifica non è un lavoro di retention

Le 8 policy su prefisso riguardano i **743.576 QR** (1,2 GB) e i **~12.000 orfani** (3,6 GB) sul bucket
legacy.

Il bucket nuovo si popola con `utility/02-migrazione-file`, e quella utility **non travasa i QR né gli
orfani**: sono classificati in `EXPECTED_ORPHANS` con il motivo per esteso. Il bucket nuovo quindi **non
li conterrà mai**. Ne segue:

| | |
|---|---|
| sul **bucket nuovo** | le 8 policy non hanno niente da cancellare, oggi e dopo il travaso. Non sono inutili: servono come **igiene continua**, per il giorno in cui il gestionale nuovo producesse un orfano |
| sul **bucket legacy** | i 4,8 GB restano dove sono. ⚠️ Non si toccano |
| la «bonifica» dei 4,8 GB | **non è retention**: è la dismissione del bucket legacy dopo il cutover. Non serve una policy, serve decidere quando quel bucket si spegne — e quel momento verrà a suo tempo |

### 7.2 `archive_cold` **sposta**

Esporta su storage e cancella dal database caldo. Deciso il 2026-10-05.

L'alternativa era «copia e lascia la riga, che la cancellazione a 10 anni rimuoverà», ma allora la riga
resterebbe **calda per dieci anni** e «archiviazione» non significherebbe più niente, in contraddizione
con «resta consultabile per la stagione corrente e le due precedenti».

### 7.3 Gli archivi scadono a 10 anni, e la fonte è un inventario nel database

```
privacy · archive_manifests · archived_at · 10 anni · delete_object · only_if_unreferenced
```

**Chiude il buco più grosso dell'impianto**: fino al 2026-10-05 nessuna policy toccava gli archivi, e
archiviare non è cancellare — i dati personali dentro un archivio sono ancora dati personali.

Non è una policy su prefisso, ed era l'opzione che sembrava necessaria. Un prefisso avrebbe richiesto
di elencare il bucket e **dedurre la scadenza di un oggetto dalla data scritta nel nome della chiave**,
cioè dedurre un termine di legge da una convenzione di naming. Al suo posto c'è
`privacy_archive_manifests`, e sopra di esso una policy ordinaria.

| Scelta | Perché |
|---|---|
| **10 anni** | il più lungo fra gli obblighi che motivano un'archiviazione: conservazione decennale dei documenti fiscali |
| decorrenza **`archived_at`** e non `cutoff_at` | `cutoff_at` dice quanto erano vecchie le righe, `archived_at` da quando l'**oggetto** esiste. È l'oggetto che questa policy cancella |
| azione **`delete_object`** e non `delete_row` | la riga di inventario **non si cancella**: è la prova di aver rimosso l'oggetto nei termini, e sopravvive all'oggetto con `purged_at` valorizzato |

⚠️ Il termine è **prudente, non preciso**: un oggetto contiene righe di una tabella sola, ma tabelle
diverse hanno termini diversi e la policy li appiattisce sul più lungo. Policy più specifiche per
`source_table` si aggiungono senza toccare il codice.

### 7.4 `archive_object` si implementa come azione della coda, non come lifecycle rule del bucket

Deciso il 2026-10-05. ⚠️ Su S3 la classe di un oggetto **non si modifica**: si ricopia l'oggetto su se
stesso dichiarando la classe nuova. E senza `MetadataDirective = COPY` i metadati si perdono: un PDF
torna indietro come `application/octet-stream`. C'è un test che lo verifica, perché è il genere di
dettaglio che non dà errore e si scopre aprendo un file sei mesi dopo.

La **classe** sta nella configurazione (`Retention:Deletion:Targets[].ArchiveStorageClass`, default
`GLACIER`) e non sulla policy: «quale classe» è una decisione di costo che dipende dall'ambiente —
LocalStack la ignora, AWS la fattura — mentre la policy decide *che cosa* succede al dato.

### 7.5 Il perimetro si dichiara in una colonna nuova, non riusando `field_names`

Deciso il 2026-10-05: colonna `target_filter`. Il dettaglio è al §2.3.

### 7.6 La retention di massa si documenta **per policy**, non per riga

Il registro **per riga** `privacy_compliance_audit_logs` resta per gli **atti verso le persone** — le
richieste di cancellazione — dove l'unità è l'interessato e `record_id NOT NULL` ha senso.

Il ragionamento, articolo per articolo:

| Norma | Che cosa chiede | Unità |
|---|---|---|
| **Art. 5.2** (responsabilizzazione) | il titolare deve poter **dimostrare** di rispettare i principi | la **regola** e la prova che è stata applicata. Non l'elenco delle righe |
| **Art. 30** (registro dei trattamenti) | fra l'altro «i termini ultimi previsti per la cancellazione» | per **trattamento**, non per record |
| **Art. 17 + 19** (cancellazione su richiesta) | cancellare e informare i destinatari | ⚠️ qui l'unità **è l'interessato**: serve la tracciabilità per riga |
| **Art. 15** (accesso) | dire che dati si detengono | se il dato è cancellato non si detiene niente |

> ### Come è stata verificata questa analisi — e come non lo era la prima volta
>
> ⚠️ **La prima stesura non controllava nessuna fonte**: ragionava sul testo degli articoli a memoria.
> Verificato il 2026-10-05:
>
> | Fonte | Che cosa dice, verificato |
> |---|---|
> | Testo dell'**art. 5** | par. 1(e): i dati vanno conservati «per un arco di tempo non superiore al conseguimento delle finalità»; par. 2: il titolare «è competente per il rispetto … e in grado di dimostrarlo». **Nessuna forma di registro è imposta**, e nessuna documentazione per record |
> | Testo dell'**art. 30**, par. 1 | elenca cosa deve contenere il registro: finalità, categorie di interessati e di dati, destinatari, trasferimenti, **«i termini ultimi previsti per la cancellazione»**, misure di sicurezza. È tenuto **per attività di trattamento** e **non chiede di registrare le cancellazioni avvenute** |
> | Prassi e orientamenti | gli obblighi di «log della cancellazione» che si trovano in letteratura riguardano le **richieste degli interessati**. Il rapporto EDPB del *Coordinated Enforcement Framework* 2026 sul diritto alla cancellazione insiste su **procedure documentate**, motivazione ragionata e mappatura nel registro dei trattamenti — non su un registro per record delle scadenze programmate |
>
> Il punto decisivo — «la legge chiede un registro per riga delle cancellazioni di retention?» — è
> verificato sul **testo** degli articoli. La parte su prassi e orientamenti viene da fonti secondarie e
> vale meno.
>
> ⚠️ **Che cosa NON è stato verificato**: il D.Lgs. 196/2003 come modificato dal D.Lgs. 101/2018,
> provvedimenti del Garante su casi specifici, e le norme di settore (fiscali, sanitarie) che possono
> aggiungere obblighi documentali propri. **Non è un parere legale**: la qualificazione finale resta al
> DPO. Quello che si può dire è che nel testo del GDPR l'obbligo non c'è, e che la distinzione fra
> retention di massa e richiesta dell'interessato è netta: la prima è una **regola applicata a un
> insieme**, la seconda un **atto verso una persona**.

> ⚠️ **Il costo di questa scelta, detto chiaro.** Dopo una cancellazione per policy **non si può più
> rispondere «la riga 1234 è stata cancellata il giorno X»**: si può dire «tutte le righe di quella
> tabella oltre quel taglio sono state cancellate in quella corsa, ed erano *n*». Per una contestazione
> individuale è una risposta più debole. Dove il dato è stato **archiviato** invece che cancellato,
> l'archivio stesso risponde alla domanda.

### 7.7 Le due tabelle che provano la conformità si conservano **10 anni**

```
privacy.retention_runs         24 mesi → 10 anni
privacy.compliance_audit_logs  24 mesi → 10 anni
```

I 24 mesi erano un'**analogia, non una misura**: il verbale li aveva presi da
`compliance_audit_logs`, che li aveva presi da una finestra «1-2 anni» indicata quando si temeva che il
registro per riga crescesse più dei dati che protegge. Il risultato era che

> la prova di aver cancellato nei termini durava **meno** dell'obbligo che la motiva.

Per i dati con termine fiscale decennale, fra il mese 24 e l'anno 10 non si poteva più documentare di
aver cancellato quando si doveva — l'opposto di quello che l'art. 5.2 chiede.

Le tre ragioni per cui si è potuto alzare, ciascuna verificata:

| | |
|---|---|
| **volume del verbale** | 61 policy × una corsa al giorno = ~22.300 righe l'anno, **~223.000 in dieci anni**. Non è un volume che richieda una decisione |
| **volume del registro per riga** | era la preoccupazione vera (una corsa su `booking.resource_consumptions` avrebbe prodotto milioni di righe), ma presupponeva che la retention scrivesse per riga. Con la decisione §7.6 non lo fa: **verificato nel codice, nessun writer di `compliance_audit_logs` in tutto il backend**. Restano le richieste degli interessati, che il titolare ha dichiarato irrilevanti come numero |
| **nessun costo in dati conservati** | nessuna delle due tabelle contiene dati personali del soggetto: descrivono l'**azione** (dove, cosa, quando, perché), mai il contenuto prima o dopo |

**Perché 10 anni**: è il più lungo fra i termini che queste tabelle devono poter provare, ed è lo stesso
di `privacy.data_subject_requests` — quindi si chiude anche la discrepanza fra la testata di una
richiesta (10 anni) e il dettaglio di che cosa quella richiesta ha cancellato (24 mesi), che rendeva
possibile dimostrare *di aver* risposto ma non più *che cosa* si era cancellato.

⚠️ I due termini restano **legati fra loro**: provano la stessa cosa a due livelli di dettaglio, quindi
se un giorno si riapre uno va riaperto anche l'altro.

### 7.8 Lo scheduler si scrive in un secondo momento

I nomi dei 19 job in [`20-jobs.md` §1.5](20-jobs.md) restano la mappa di come andranno raggruppati: il
motore li esegue già tutti, in un colpo solo, da riga di comando.

Non è solo una questione di tempi. Scegliere **dove** vivono i job schedulati di un monolite modulare è
una decisione di architettura che va oltre la retention: se li ospita il processo web, un deploy li
interrompe a metà; se è un processo separato, serve decidere come viene avviato e come si evita che due
istanze girino insieme.

### 7.9 Il PDF di uno scontrino segue la sua ricevuta, e le due policy su `fiskaly.media_assets` si ritirano

Deciso il 2026-10-06, come conseguenza del §6.13. Il meccanismo è al §2.4.

Le due policy su `fiskaly.media_assets` — 36 mesi `archive_object` e 10 anni `delete_object` —
**si disattivano**. Erano quelle che fallivano a ogni corsa, e il loro lavoro lo fa ora la policy di
`fiskaly.receipts` tramite `linked_asset`, più la policy decennale sull'inventario.

Si **disattivano e non si cancellano**: la riga resta come traccia di una regola che è esistita, con il
motivo scritto nella base giuridica. Una policy cancellata non lascia detto perché non c'è più.

> ### ⚠️ Una conseguenza da conoscere
>
> Un PDF di fiskaly che **non** è nominato da nessuna ricevuta non è coperto da nessuna policy. È
> **deliberato**: il segnale «nessuno lo nomina» è esattamente quello che l'archiviazione produce, e
> agire su quel segnale cancellerebbe documenti fiscali al mese 36 invece che all'anno 10 (§2.4).

---

## 8. Test

### 165 automatici, verdi

| Progetto | Test | Cosa provano |
|---|---:|---|
| `BuildingBlocks.Infrastructure.Tests/SeasonBoundaryTests.cs` | 10 *(+11 casi)* | il confine è il **1 gennaio** e non il 1 aprile; non dipende dal giorno di esecuzione; «2 stagioni» ≠ «24 mesi»; la durata zero è ammessa; un'unità non riconosciuta **non** viene indovinata |
| `BuildingBlocks.Infrastructure.Tests/LinkedAssetSpecTests.cs` | 27 | la dichiarazione del file che segue le righe, e **soprattutto i rifiuti**: nome di colonna o tabella che non è un identificatore (compreso `pdf_asset_id"; DROP TABLE …`), azione che non agisce su un oggetto, JSON incompleto. ⚠️ Una dichiarazione malformata deve **far fallire** la policy, non farla girare ignorando il file |
| `BuildingBlocks.Infrastructure.Tests/LinkedAssetArchivingTests.cs` | 1 | ⚠️ il meccanismo **contro un PostgreSQL e un S3 veri**, con dati sintetici: una ricevuta firmata cinque anni fa viene archiviata, il suo PDF finisce in coda come `archive_object` con l'origine **dell'asset**, è inventariato come `linked_asset` con `row_count` NULL, la riga della ricevuta lascia il database e la riga dell'asset **resta**. Dati sintetici per necessità: la ricevuta più vecchia è del 2026-06, quindi su dati reali il meccanismo non si eserciterebbe per anni |
| `BuildingBlocks.Infrastructure.Tests/TargetFilterTests.cs` | 36 | la forma esatta dell'SQL prodotto (il cast `::text` non è un dettaglio) e, per **metà** dei casi, che un filtro **venga rifiutato**: colonna inesistente, operatore fuori elenco, e i sei tentativi classici di uscire dal letterale (`pdf' OR '1'='1`, il punto e virgola con la `DROP TABLE`, l'apice sfuggito, lo spazio, il ritorno a capo) |
| `BuildingBlocks.Infrastructure.Tests/S3ArchiveStoreTests.cs` | 4 | crea il bucket ed è idempotente; **l'archivio scritto si rilegge** e la dimensione riportata è quella vera; la chiave porta schema, tabella, data e policy; due lotti non collidono |
| `BuildingBlocks.Infrastructure.Tests/StorageDeletionReconcilerTests.cs` | 4 | rimuove e chiude la riga di coda; **un'origine non configurata non viene toccata**; su errore conta il tentativo e scrive `last_error` lasciando `executed_at` vuoto; una riga `archive_object` **sposta di classe e non cancella**, conservando il content type, anche quando sta nello stesso lotto di una cancellazione |
| `Module.Catalog.Tests/CeasedPropertyPhotoPerimeterTests.cs` | 7 | il perimetro delle foto: prime tre protette · copertina protetta **anche se è la quinta** · `active` e `suspended` non sono cessazioni · una cessazione di un mese fa non è scaduta · `closed_at` NULL non fa scadere niente · una foto rimossa logicamente non occupa un posto protetto |
| `ArchitectureTests` | 68 | le regole del monolite modulare, fra cui che `Shared` non dipenda da nessun modulo |

I test dell'archivio, della coda e del perimetro parlano con **LocalStack e PostgreSQL veri**, non con
doppi: è il codice che cancella, e un doppio non proverebbe la cosa che conta. Si saltano da sé se
l'ambiente non risponde, e quelli di `Module.Catalog.Tests` girano dentro una transazione che non viene
mai confermata.

> ### ⚠️ Perché il perimetro delle foto **deve** essere provato con dati sintetici
>
> Sui dati migrati trova zero righe per costruzione (§5): `identity.account_subscriptions` è vuota.
> Senza quei sette test, quel job resterebbe codice mai eseguito, pronto a sbagliare — e a sbagliare
> *cancellando foto* — la notte in cui i primi abbonamenti ci saranno.

### ✅ La verifica del 2026-10-05, in dettaglio

| Che cosa | Esito |
|---|---|
| **ETL da zero** | 6.163.412 righe migrate, 4.694.180 scartate, **0 errori** — totali **identici** alla corsa buona del 2026-10-03 |
| **1ª corsa di retention** (reale) | 61 policy: 50 ok, 9 saltate, 2 fallite. **321.791 righe trovate, 321.791 toccate** |
| **2ª corsa** | **0 righe trovate, 0 toccate**: la retention converge |
| **archivi** | 3 oggetti, 335.383 byte compressi, su `summerbooking-archivio-retention` |
| **inventario** | 3 righe in `privacy_archive_manifests`, con `row_count` 651 + 4.308 + 82 = **5.041**, e l'etichetta dedicata `s3_archivio_local` |
| **archivio riletto** | 651 righe (= `row_count`), 37 colonne, `note` **null**, `stay_to` del 2021 — prova dell'ordine «minimizza → archivia → cancella» |
| **verbale** | 1ª corsa 60 righe *(difetto §6.14)*, 2ª corsa **61 righe su 61 policy distinte**: una per policy, nessun doppione per quella delegata al job |
| **job dedicato** | eseguito, 0 righe, taglio 2024-10-05 — atteso, perché gli abbonamenti non ci sono (§5) |
| **policy sbloccate** | `notification` (PDF dei report) e `switchboard` (audio) **girano** e trovano 0, come previsto |
| **catena delle migration da zero** | tutte e 11 applicate su un database usa-e-getta, e `pg_dump --schema-only` **identico** a quello di sviluppo (dopo la riparazione del §6.15) |
| **un database nuovo** | nasce con **61 policy, 61 in prova**: le migration non accendono niente, com'è voluto |

### Verifiche fatte a mano, su dati reali

- **tre corse complete** su un database ricostruito da zero (6,1 milioni di righe): 426.442 righe
  toccate, poi 63.518, poi **0** — che è la prova della convergenza;
- **archivio riletto**: 650 righe, 37 colonne, `note` già NULL (prova dell'ordine corretto);
- **anonimizzazione su dati sintetici**, per coprire il ramo che la corsa reale non ha esercitato: IP
  troncato all'ultimo ottetto (`192.168.1.57` → `192.168.1.0/24`), troncamento che rispetta
  `varchar(20)`, importi e chiavi intatti;
- **accodamento su dati sintetici**: di 4 asset scaduti entra in coda solo l'orfano — non il
  referenziato, non quello con `property_id`, non quello troppo recente — e riaccodare non duplica;
- **durata della cancellazione** misurata con `EXPLAIN ANALYZE` (difetto §6.6);
- **chiavi esterne senza `CASCADE`** censite e verificate riga per riga.

### ⚠️ Il buco che resta: `RetentionEngine.cs` non ha test automatici

È il file più grosso, provato **solo** dalle corse reali. Tre dei difetti (§6.4, §6.5, §6.7) sono stati
trovati da una corsa su dati veri, cioè nel posto peggiore: con l'archivio già scritto e le righe già
toccate.

La situazione è migliorata — i due pezzi con logica pura, `SeasonBoundary` e `TargetFilter`, hanno 46
test fra loro, e il perimetro di `catalog` ne ha 7 — ma non è coperto: l'ordine fra le azioni,
l'idempotenza, la risoluzione del nome fisico, il predicato «nessun riferimento», la scrittura del
verbale, il rispetto di `is_dry_run_only`. Vedi §10.2.

---

## 9. Scelte tecniche, spiegate

### 9.1 Perché `RetentionEngine` usa SQL e non EF Core

Tre ragioni, in ordine di forza. La prima da sola decide.

**1. I nomi delle tabelle e delle colonne sono *dati*, non tipi.** È tutto il punto della tabella
dichiarativa: una policy dice «su `booking.reservations`, azzera `{note, customer_tags}`, oltre 1
stagione da `stay_to`». Quei nomi arrivano da una riga di database e cambiano con un `UPDATE`, senza
una release. EF Core è tipizzato in compilazione: `db.Reservations.Where(r => r.StayTo < cutoff)`
richiede di conoscere `Reservation` e `StayTo` **mentre si scrive il codice**. Non esiste un modo onesto
di scrivere in LINQ «aggiorna la colonna il cui nome è in questa stringa». Con EF si finirebbe comunque
a comporre SQL, solo passando per `ExecuteSqlRaw` — la stessa cosa con un giro in più.

**2. Il motore vive in `Shared`, che non può dipendere da nessun modulo.** Regola verificata da
`SharedArchitectureTests`, e serve: deve funzionare per undici schemi, e se dipendesse dalle entità di
Booking non potrebbe stare nello stesso posto che serve Payment. Senza accesso alle entità non c'è nulla
su cui fare LINQ. Il motore riceve un `DbConnection`, che gli passa il job del modulo — quindi la
connessione **del modulo proprietario dello schema**, non una connessione centrale.

**3. Gli enum nativi di `privacy` non sono mappati sulle connessioni degli altri moduli.** Ogni modulo
costruisce il proprio `NpgsqlDataSource` con le proprie mappature. Leggere
`privacy_retention_policies` dalla connessione di Booking con EF farebbe esplodere il materializzatore
su `action`, `duration_unit`, `basis_source`. Il motore le proietta con `::text` — una riga di SQL
contro una ri-mappatura degli enum di `privacy` su undici data source.

**Che cosa si perde, e come è coperto.** Senza EF non c'è il controllo del compilatore sui nomi: una
tabella rinominata non dà errore di build, dà un errore a runtime. Per questo il motore
**introspeziona prima di agire** — `information_schema.tables` per il nome fisico,
`information_schema.columns` per le colonne, `pg_constraint` per le chiavi esterne — e un bersaglio che
non esiste diventa una policy `failed` con il nome esatto che non ha trovato, non un'eccezione che ferma
la corsa. Tutti i valori passano da parametri, mai per concatenazione; i nomi di tabella e colonna, che
parametro non possono essere, sono quotati e vengono dall'introspezione, non dall'esterno.

**Dove EF c'è, invece**: le migration, le entità e le configurazioni. Le **scritture** del verbale però
passano da SQL per la ragione 3: avvengono dalla connessione del modulo valutato, dove gli enum di
`privacy` non sono mappati, e i cast espliciti `::privacy.retention_action` sono ciò che le rende
possibili da qualunque modulo.

### 9.2 Perché il verbale lo scrive un pezzo condiviso

`RetentionRunLog` esiste come file a sé perché il verbale lo scrivono **due** esecutori: il motore
generico e i job con il perimetro nel codice. Duplicare l'`INSERT` nei due posti sarebbe la ricetta per
farli divergere alla prima colonna aggiunta — e il verbale è l'unica cosa che una persona legge prima di
autorizzare una cancellazione.

Per la stessa ragione il motore **non scrive** il verbale per le policy affidate a un job dedicato
(`RetentionRunOptions.PoliciesHandledElsewhere`): altrimenti una corsa produrrebbe **due righe per la
stessa policy**, una «saltata» dal motore e una dal job, e chi apre la tabella per decidere se accendere
una regola leggerebbe due esiti diversi dello stesso identificativo — con il più visibile che è quello
sbagliato.

### 9.3 `run_id` sull'inventario non è una chiave esterna

Punta a `privacy_retention_runs.run_id`, ma come riferimento **logico**. Una FK impedirebbe al verbale
di scadere, trasformando l'inventario in una catena che lo trattiene — esattamente il contrario del
motivo per cui le due tabelle sono separate.

### 9.4 `property_id`: due riferimenti con lo stesso nome, e la convenzione

Verificato su tutte le **129** colonne chiamate `property_id`:

| Dove | Tipo | Verso |
|---|---|---|
| **dentro `catalog`**, che possiede `properties` | `bigint` | `properties.id`, con **vera chiave esterna** — 18 tabelle, 17 con FK (l'unica senza è `search_bathhouses`, denormalizzata) |
| **fuori da `catalog`** | `uuid` | `properties.public_id`, riferimento **logico**: una FK non attraversa i moduli |

`catalog.media_assets.property_id` era l'unica fuori convenzione — `uuid` senza FK dentro `catalog` — ed
è stata corretta il 2026-10-05 (`FixMediaAssetPropertyIdType`). ⚠️ Chi implementerà il recupero delle
272 foto deve scrivere `properties.id`, non `public_id`.

✅ Una cosa che **non** è un rischio: scambiare i due riferimenti non è un errore silenzioso.
PostgreSQL rifiuta `uuid = bigint` con «l'operatore non esiste», perché fra i due tipi non c'è
conversione implicita. La differenza di tipo **protegge**. Il controllo nel motore — che rifiuta una
policy `property_ceased` su una tabella il cui `property_id` non è `uuid` — resta utile per un altro
motivo: fa fallire la policy *prima* e con un messaggio che dice qual è il problema, invece di un
«operatore non esiste» a metà corsa dopo che altre policy hanno già scritto.

---

## 10. Quello che resta da fare

### 10.1 Cancellare e aggiornare **a lotti**

Il motore fa oggi una sola `DELETE`/`UPDATE` per policy. Misurato: **650 righe in 42,7 secondi**, per i
trigger di verifica delle 14 chiavi esterne in entrata su `booking.reservations`. Il tempo cresce
**linearmente con le righe**.

Oggi il tetto è 30 minuti, quindi la corsa non fallisce. Ma **una `DELETE` unica tiene un lock per tutta
la sua durata**: su una tabella viva non è accettabile, e la prima corsa su un database di produzione
non ancora ripulito troverebbe molte più righe.

Le quattro tabelle con il problema:

| Tabella | FK in entrata | Note |
|---|---:|---|
| `booking.reservations` | 14 | tutte con indice sulla figlia, quindi non è un indice mancante |
| `fiskaly.cash_registers` | 2 | entrambe **senza** `CASCADE` |
| `identity.access_users` | 1 | **senza** `CASCADE` |
| `payment.transaction_charges` | 2 | **senza** `CASCADE`; 1.390 righe in `request_links` |

⚠️ Sulle tre con FK senza `CASCADE` nessuna riga in scadenza è oggi davvero trattenuta — verificato riga
per riga. Ma `payment.request_links` ha 1.390 righe e 4.308 addebiti scadono: il giorno in cui un link
punta a un addebito scaduto, quella policy fallisce.

La forma è un ciclo a lotti (`DELETE … WHERE id IN (SELECT id … LIMIT n)`) con `n` configurabile. Tre
effetti desiderabili: il lock dura il tempo di un lotto; una policy interrotta a metà lascia uno stato
coerente e la corsa successiva riprende da dove era (le guardie di idempotenza del §6.7 lo rendono
sicuro); e il progresso diventa visibile.

### 10.2 Test automatici del motore

Creare uno schema di prova, metterci tabelle e righe finte, inserire righe di policy che puntano a
quello schema, eseguire il motore e verificare l'esito. Coprirebbe proprio i punti dove si è sbagliato:
l'ordine fra le azioni, l'idempotenza, la risoluzione del nome fisico della tabella, il predicato
«nessun riferimento», e il fatto che `is_dry_run_only` non si scavalca.

### 10.3 ~~La decorrenza delle due policy di fiskaly~~ — ✅ chiusa il 2026-10-06

Era il §6.13: da quale data decorrono i dieci anni del PDF di uno scontrino, dato che la riga della
ricevuta — che porta `signed_at` — lascia il database caldo a 36 mesi. Deciso di far archiviare
insieme ricevuta e PDF (`linked_asset`, §2.4); le due policy su `fiskaly.media_assets` sono
ritirate (§7.9) e non compaiono più come `failed` nel verbale.

### 10.4 Il raffreddamento non è ancora stato esercitato su dati veri

`archive_object` è provata dai test (con LocalStack, verificando classe e content type, più
`LinkedAssetArchivingTests` per il percorso `linked_asset`), ma nessuna corsa reale ha ancora
raffreddato un oggetto. Dopo il §7.9 il raffreddamento arriva solo dai PDF che seguono una ricevuta
archiviata a 36 mesi, e la ricevuta più vecchia è del 2026-06: su dati reali succederà fra anni.

### 10.5 Lavori di altri ambiti che la retention aspetta

- il **recupero delle 272 foto** di stabilimenti senza vetrina (`utility/01-migrazione-test`): finché
  non gira, `catalog.media_assets.property_id` resta vuota;
- la **dismissione del bucket legacy** (§7.1);
- gli **abbonamenti nel nuovo sistema**: senza, le due policy con `property_ceased` restano inerti
  (§5).

---

## 11. Note operative da sapere

**Il confine stagionale è il 1 gennaio**, e dieci policy dipendono da una funzione sola
(`SeasonBoundary`) con dieci test addosso. La documentazione del progetto ha avuto il 1 aprile per
settimane: se qualcuno ricalcola la soglia dentro un job, quell'errore rientra in silenzio.

**La data di migrazione di `last_activity_at` deve essere una costante.** L'ETL lo segnala da sé:
`migration_cutover_date` non impostata ⇒ usa la data della corsa, e **a ogni rerun l'orologio della
retention si azzera** per i 117.614 clienti senza prenotazioni. In produzione va impostata la data di
cutover, altrimenti quelle anagrafiche non scadono mai.

**In produzione va impostata la classe di storage dell'archivio.**
`ArchiveStoreOptions.StorageClass` è vuota in sviluppo perché LocalStack la ignora. Senza, gli oggetti
finiscono su classe `STANDARD` e si pagano come se fossero caldi — l'opposto del motivo per cui si
archivia. Il bucket va inoltre **dedicato**: qui finiscono righe di database, non file applicativi, e
mescolarli renderebbe la policy sugli orfani pericolosa per l'archivio.

⚠️ **`GLACIER` ha un minimo di 90 giorni di fatturazione** e il recupero non è immediato: un oggetto
raffreddato e poi cancellato prima dei 90 giorni si paga comunque per 90. Per gli scontrini —
raffreddati a 36 mesi e cancellati a 10 anni — non è un problema.

⚠️ **L'indice unico della coda non include `action`, ed è voluto**: una sola azione pendente per
oggetto. Includerla permetterebbe di accodare insieme il raffreddamento e la cancellazione dello stesso
oggetto senza garanzie sull'ordine, e raffreddare un oggetto già cancellato fallisce. La conseguenza è
che, se un raffreddamento è ancora pendente, l'`ON CONFLICT DO NOTHING` scarta in silenzio la
cancellazione e la corsa successiva la riaccoda. Si sistema da sé, ma va saputo leggendo un report con
meno righe di quelle attese.

> ### ⚠️ La persistenza di LocalStack non si può abilitare con la licenza attuale
>
> Provata il 2026-10-05. Il volume c'era già
> (`c:\D\docker\localstack\localstack-data → /var/lib/localstack`) e mancava solo `PERSISTENCE=1`.
> Aggiunto in `docker-compose.yml` e provato **due volte**, la seconda attendendo il flush schedulato di
> 15 secondi:
>
> | Indizio | Cosa dice |
> |---|---|
> | `/_localstack/health` | `persistence: initialized` — la configurazione è accettata |
> | `./localstack-data/state/` | creata, **vuota**: nessuno snapshot è mai stato scritto |
> | dopo un `docker compose restart` | bucket e oggetti **non ci sono più** |
> | log di avvio | `Successfully activated cached license …:freemium` |
> | `license.json` → `products` | **non contiene la persistenza** |
>
> **È una funzione Pro a pagamento**, e la licenza è `freemium` (scade il 2026-10-19).
> `/_localstack/info` dice `edition: pro` solo perché l'immagine è quella Pro: la licenza è un'altra
> cosa. La riga `PERSISTENCE=1` resta nel compose, con l'esito della prova nel commento, perché è la
> configurazione giusta e con un token Pro vero funziona da sé.
>
> **Conseguenza pratica**: gli archivi su LocalStack non sopravvivono a un riavvio e vanno rigenerati
> rilanciando la retention. Non è grave — sono riproducibili — e l'**inventario** invece sopravvive,
> perché sta su PostgreSQL.
