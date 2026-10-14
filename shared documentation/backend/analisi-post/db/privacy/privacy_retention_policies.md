# Tabella `privacy_retention_policies`

> **Dominio**: `Module.Privacy` — `privacy` · **sottoarea**: — · **schema**: `privacy` · **nome flat**:
> `privacy_retention_policies` · **forma canonica**: `privacy.retention_policies`.
>
> Tabella **nuova** (nessuna legacy: il legacy non ha mai avuto retention dichiarativa — dato di fatto
> alla base di [`01-criticita-e-indicazioni-backend.md`](../../../progettazione/01-criticita-e-indicazioni-backend.md)
> e di [`11-modulo-privacy.md` §3](../../../progettazione/11-modulo-privacy.md), "violato sistematicamente"). Fonti:
> [`docs/11-modulo-privacy.md` §2 (`RetentionPolicy`), §6](../../../progettazione/11-modulo-privacy.md) (policy
> dichiarativa + esecuzione decentrata, dry-run obbligatorio, tabella delle durate),
> [`docs/80-retention.md`](../../../progettazione/80-retention.md) (tabellone §3, job §6, cheat sheet §7),
> [`docs/82-retention-data.md`](../../../progettazione/82-retention-data.md) (**verbale delle scelte del
> titolare** — fonte autorevole dei valori esatti).
>
> **Aggiornamento 2026-09-21 — la tabella ha finalmente i suoi dati.** La politica di retention è stata
> **validata dal titolare** (2026-09-17): le durate non sono più una proposta da far approvare, sono
> decisioni. Da qui due conseguenze su questa scheda:
> 1. è stato aggiunto il **§ Seed** in fondo, con le ~40 righe concrete da inserire — è il "seed
>    applicativo" che le *Note di migrazione* si limitavano a promettere;
> 2. l'enum `privacy.retention_duration_unit` guadagna il valore **`seasons`** (vedi campo `duration_unit`): senza
>    di esso l'intero blocco C1, che il titolare esprime in **stagioni** (N−1/N−2), non era
>    rappresentabile in questa tabella se non con un'approssimazione in mesi che **cancella troppo
>    presto** (vedi § Seed, "Perché `seasons` è un'unità e non una conversione in mesi").

---

## Descrizione

**Configurazione dichiarativa**: una riga = "su `schema.tabella`, il campo/i X, calcolato dalla colonna
data Y, oltre la durata Z, esegui l'azione W". **Letta** dal job di retention di ciascun modulo — mai
eseguita da qui, coerente con "ogni modulo scrive nella propria transazione/possiede il proprio schema"
([`docs/11-modulo-privacy.md` §6](../../../progettazione/11-modulo-privacy.md)): `Module.Privacy` **non** esegue
mai una `DELETE`/`UPDATE` cross-schema, offre solo la regola che il job del modulo proprietario applica
sulla propria tabella tramite il proprio `DbContext`.

Popola a schema la "tabella delle durate" di
[`docs/11-modulo-privacy.md` §6](../../../progettazione/11-modulo-privacy.md) e la cheat sheet di
[`docs/80-retention.md` §7](../../../progettazione/80-retention.md) — oggi documentazione, qui dati interrogabili
da un job e da un pannello di revisione (lo stesso spirito dell'utility Python descritta in doc §6, che
legge/scrive questa tabella invece di un file JSON separato).

**Ambito: piattaforma, non per-stabilimento.** Le righe sono regole normative/di policy aziendale (Art.
5.1.e GDPR, obblighi fiscali) valide per **tutti** gli stabilimenti allo stesso modo — non una
configurazione che uno stabilimento può personalizzare. Nessuna colonna `property_id`: stesso trattamento
riservato ai cataloghi master globali del progetto (es.
[`catalog_map_icons`](../catalog/catalog_map_icons.md)). Vedi [D-62](../_domande-committente.md).

## Verifica di esclusività

- **`is_dry_run_only`** e **`is_active`**: due booleani indipendenti, nessun conflitto — una policy può
  essere attiva-in-dry-run (produce solo report) o attiva-in-esecuzione-reale; disattivarla
  (`is_active = false`) la sospende del tutto senza cancellarla. Non sono un gruppo esclusivo, restano
  colonne separate.

## Snapshot

Non applicabile — non esiste un valore "congelato" da un'entità mutabile: questa tabella **è** la fonte
di verità della regola, non una copia di qualcos'altro.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_privacy_retention_policies` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | id pubblico per il pannello di revisione (SystemAdmin/DPO) |
| `uq_..._target` | `(schema_name, table_name, field_names, action, reference_event, storage_prefix, target_filter) NULLS NOT DISTINCT WHERE deleted_at IS NULL` | UNIQUE parziale | evita due policy **con la stessa azione** sullo stesso target; ammette più righe con `field_names` diversi (es. `customers` generale vs `customers.note`) **e** le policy a **due fasi** (archivia a N, cancella a 10 anni) sulla stessa tabella — vedi § Seed, *"Perché `action` fa parte della chiave naturale"*. 🔄 **Rifatto il 2026-10-03** (`AddRetentionFileTargetingColumns`) con due colonne in più e `NULLS NOT DISTINCT`: vedi il riquadro sotto la tabella. 🔄 **2026-10-13** (`AddAuditRetentionPolicies`): `+ target_filter` |
| `ix_..._lookup` | `(schema_name, table_name) WHERE is_active AND deleted_at IS NULL` | parziale | query del job di retention di ciascun modulo: "le mie policy attive" |

> ### ✅ Deciso il 2026-10-07, fatto il 2026-10-13: `target_filter` entra in `uq_..._target`
>
> **Il caso.** [`audit_trail_entries`](../audit/audit_trail_entries.md) tiene in una sola tabella tre
> categorie con durate diverse: `security` 6 mesi, `platform` 12 mesi, `operator` 12 mesi (D-66).
> Sono tre policy sulla stessa tabella, con la stessa azione (`delete_row`), che si distinguono **solo**
> per `target_filter` (`{"column": "category", "op": "=", "value": "security"}`, …). Con la chiave
> attuale la seconda e la terza violano l'indice: per il database sono «la stessa regola con durate
> diverse».
>
> **La modifica.** `uq_..._target` diventa
> `(schema_name, table_name, field_names, action, reference_event, storage_prefix, target_filter) NULLS NOT DISTINCT WHERE deleted_at IS NULL`.
> - Regole con filtri **diversi** sulla stessa tabella diventano ammesse; due regole con lo **stesso**
>   filtro restano vietate, come oggi. `NULLS NOT DISTINCT` tratta come uguali due filtri `NULL`.
> - `target_filter` è `jsonb`, che si confronta per valore: `{"a":1,"b":2}` e `{"b":2,"a":1}` sono lo
>   stesso filtro.
> - **Seeder**: la clausola `ON CONFLICT` usa la nuova chiave; l'hash che genera il `public_id`
>   deterministico include `target_filter` **solo se non è NULL**, così gli id delle policy già seminate
>   non cambiano.
> - **Sovrapposizioni**: il seeder rifiuta, sulla stessa tabella e con la stessa azione, una regola senza
>   filtro accanto a regole con filtro. Altrimenti le righe filtrate ricadrebbero in due regole e
>   vincerebbe la durata più corta senza che nessuno l'abbia deciso.
>
> **Fatto** con la migration `AddAuditRetentionPolicies` di `Module.Privacy` (2026-10-13): nuovo indice e le tre
> policy di `audit.trail_entries`, nate in prova. Le policy entrano solo da migration (non esiste un seeder
> applicativo), quindi il rifiuto delle sovrapposizioni descritto sopra resta da scrivere
> ([`docs/85-promemoria.md`](../../../progettazione/85-promemoria.md), PROMEMORIA-85-3).

> ### 🔄 `uq_..._target`: perché è stato rifatto, e perché non è dichiarato in EF
>
> Le policy sui file hanno aggiunto due casi che la forma a 4 colonne non sapeva rappresentare:
>
> - **`reference_event` nella chiave**: le foto scadono *sia* quando nessuno le usa più *sia* alla
>   cessazione dello stabilimento. Stessa tabella, stessa azione, due regole diverse — la differenza
>   **è** l'evento che le fa scattare, quindi è giusto che stia nella chiave naturale;
> - **`storage_prefix` nella chiave**: le policy su prefisso hanno `table_name` vuoto e
>   collidere­bbero tutte fra loro.
>
> **`NULLS NOT DISTINCT` non è un dettaglio.** In PostgreSQL i NULL sono distinti in un indice unico,
> quindi su colonne nullable semplici due righe identiche con `reference_event` e `storage_prefix`
> entrambi NULL — **il caso di tutte e 44 le policy preesistenti** — non collidere­bbero, e la
> protezione sparirebbe proprio sulle righe che l'indice proteggeva prima. PostgreSQL è alla 17.6 e lo
> supporta dalla 15; l'alternativa era un indice su espressioni `COALESCE`, che fa la stessa cosa ma
> si legge peggio.
>
> ⚠️ **Nessuna delle due forme è esprimibile con la Fluent API**, quindi questo indice **non è
> dichiarato** in `RetentionPolicyConfiguration` ed è creato in SQL dentro la migration (eccezione
> prevista da [`docs/42-migration.md` §7](../../../progettazione/42-migration.md)). Nel punto della
> configurazione dove era dichiarato c'è un commento che dice dov'è finito e perché: chi aggiunge un
> indice a questa tabella lo dichiari in EF, chi tocca **questo** lo faccia in SQL, consapevolmente.

> ### Vincoli di coerenza aggiunti il 2026-10-03
>
> | Vincolo | Regola | Perché |
> |---|---|---|
> | `ck_..._target_xor` | `(table_name <> '' AND storage_prefix IS NULL) OR (storage_prefix IS NOT NULL AND storage_prefix <> '' AND table_name = '')` | una policy ha **o** una tabella **o** un prefisso, mai entrambi, mai nessuno dei due |
> | `ck_..._prefix_action` | `storage_prefix IS NULL OR action IN ('delete_object', 'archive_object')` | su un prefisso si può solo agire sull'oggetto: non c'è nessuna riga da cancellare |
> | `ck_..._prefix_date` | `(storage_prefix IS NULL) = (storage_date_source IS NULL)` | una policy su prefisso deve dire da dove prende la data, e una su tabella non deve |
> | `ck_..._has_decorrenza` 🆕 | `reference_date_column <> '' OR reference_event IS NOT NULL OR storage_date_source IS NOT NULL` | **ogni** policy deve avere una decorrenza da qualche parte. Non era nella specifica: è stato aggiunto applicandola, perché la convenzione «stringa vuota = nessuna colonna locale» apriva la possibilità di scrivere una regola che non scade mai, indistinguibile da una scritta male |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (opzionale) da `retention_policy_id` di
  [`privacy_compliance_audit_logs`](privacy_compliance_audit_logs.md).

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico per il pannello di revisione (CRUD policy da parte di SystemAdmin/DPO, come
  previsto dall'utility descritta in [`docs/11-modulo-privacy.md` §6](../../../progettazione/11-modulo-privacy.md)).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: stesso criterio di [`infra_integration_api_clients.public_id`](../infra/infra_integration_api_clients.md)
  ("id pubblico stabile per il pannello systemadmin").

### schema_name
- **Legacy**: — (nuovo)
- **Descrizione**: schema PostgreSQL del modulo proprietario della tabella target (es. `booking`).
- **Tipo**: `varchar(63)` (limite identificatore PostgreSQL).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento **testuale**, non FK — punta fuori dallo schema `privacy` per definizione
  (violerebbe "zero FK cross-schema" altrimenti); validato in applicazione contro l'elenco dei moduli noti
  (`privacy.module_code`, stesso vocabolario di
  [`privacy_data_subject_request_module_outcomes`](privacy_data_subject_request_module_outcomes.md)).

### table_name
- **Legacy**: — (nuovo)
- **Descrizione**: tabella target nella **forma canonica relativa allo schema** (senza il prefisso del
  modulo — es. `reservations`, non `booking_reservations`; cfr. "forma canonica" `schema.tabella` di ogni
  scheda di dominio).
- **Tipo**: `varchar(63)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento testuale, vedi `schema_name`.

### field_names
- **Legacy**: — (nuovo)
- **Descrizione**: campo o gruppo di campi a cui si applica l'azione (vuoto/omesso concettualmente =
  l'intera riga, rilevante per `action = delete_row`/`archive_cold`).
- **Tipo**: `varchar(120)[]`.
- **Default**: `'{}'` (array vuoto = azione sull'intera riga).
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: array e non una tabella figlia per-campo: è metadato di configurazione letto per intero dal
  job (mai filtrato/joinato per singolo elemento), a bassa cardinalità e stabile — non un caso EAV
  ([`docs/03-gestione-dati.md` §2.5](../../../progettazione/03-gestione-dati.md)).

### reference_date_column
- **Legacy**: — (nuovo)
- **Descrizione**: colonna sorgente della data di riferimento da cui decorre la durata (es. `created_at`
  per policy standard, `last_activity_at` per "clienti inattivi da N anni" —
  [`docs/11-modulo-privacy.md` §6 "Quando serve una data di riferimento dedicata"](../../../progettazione/11-modulo-privacy.md)).
- **Tipo**: `varchar(63)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: riferimento testuale a una colonna della tabella target.
- **Nota (2026-10-03)**: 🔄 la **stringa vuota** significa «nessuna colonna locale porta la data»: la
  decorrenza la danno `reference_event` (un fatto accaduto altrove, come la cessazione dello
  stabilimento) oppure `storage_date_source` (la data dell'oggetto su storage, per le policy su
  prefisso). È la stessa convenzione che `table_name` usa per «nessuna tabella», e il vincolo
  `ck_..._has_decorrenza` garantisce che ce ne sia sempre una delle tre.

### only_if_unreferenced 🆕
- **Legacy**: — (nuovo, 2026-10-03)
- **Descrizione**: cancella o archivia l'oggetto **solo se nessun'altra riga lo usa**.
- **Tipo**: `boolean`, default `false`. **Nullabile**: No.
- **Nota**: riguarda le sole azioni su storage. Un file può essere referenziato da più righe — la
  stessa foto su due vetrine, lo stesso allegato su due campagne — e la scadenza della prima non lo
  rende inutile. Si risolve con una query di risalita sulle 19 chiavi esterne al momento della
  valutazione, senza vista materializzata né trigger: è un job giornaliero, non un percorso caldo.

### target_filter 🆕
- **Legacy**: — (nuovo, 2026-10-05)
- **Descrizione**: **quali** righe della tabella riguarda la policy, quando non sono tutte.
- **Tipo**: `jsonb`. **Nullabile**: Sì — NULL significa «perimetro non dichiarato».
- **Vincoli**: `ck_..._target_filter_shape` (oggetto JSON con `scope`, oppure con `column` **e** `op`)
  e `ck_..._target_filter_table` (una policy su prefisso non ha righe da filtrare).

**Perché esiste.** Tutte le altre colonne dicono *quando* un dato scade, con grande precisione. Sul
*quali*, la tabella sapeva dire solo due cose: «le righe di `schema_name.table_name`» e
`only_if_unreferenced`, che è un sì/no e non una condizione. Basta per le policy sulle **righe**,
perché una tabella contiene una cosa sola — tutte le righe di `notification.deliveries` sono invii — e
**non** basta per i **file**, perché una tabella di asset mescola nature diverse.

Il caso che l'ha motivata: `notification.media_assets` ha sei `kind` (allegati di modelli, di
campagne, copertine di eventi, PDF generati, caricamenti, altro) e la regola decisa dal titolare
riguarda **solo** i PDF dei report alle strutture convenzionate. La policy, letta alla lettera, diceva
«cancella tutti i file di notification oltre 24 mesi»: un job che l'avesse eseguita avrebbe cancellato
anche gli allegati delle campagne — non per un errore di programmazione, ma perché la policy glielo
stava dicendo. Per questo il motore la **saltava**.

**Le forme ammesse**, tutte oggetti JSON:

```jsonc
{"scope": "whole_table"}                                 // tutte le righe, DICHIARATO
{"column": "kind", "op": "=", "value": "rendered_pdf"}
{"column": "kind", "op": "in", "values": ["a", "b"]}
{"column": "purged_at", "op": "is null"}
```

Operatori ammessi: `=` · `<>` (o `!=`) · `in` · `not in` · `is null` · `is not null`.

> ### `{"scope": "whole_table"}` non è ridondante
>
> Produce lo stesso SQL di «nessun filtro» — nessuna condizione aggiunta — e sono comunque due cose
> diverse: trasforma **«nessuno ha detto quali»** in **«qualcuno ha detto tutte»**. Sulle azioni su
> storage è la differenza fra una policy che il motore esegue e una che salta, perché su un file un
> perimetro indovinato non si annulla.

> ### ⚠️ I valori sono limitati a token identificativi, e il motivo va saputo
>
> `^[A-Za-z0-9_.:-]{1,64}$`. Un valore con uno spazio, un apice o una barra — per esempio un content
> type come `image/png` — **non si può dichiarare**, e la policy fallisce con un messaggio chiaro.
>
> Il perché sta in `BuildingBlocks.Infrastructure/Retention/TargetFilter.cs`: il predicato viene composto in **sette punti
> diversi** del motore, ognuno con la propria lista di parametri, e passare il valore come parametro
> avrebbe richiesto di propagarlo in tutti e sette. L'errore che conta davvero è dimenticarsene in
> uno: quel punto avrebbe eseguito la policy **senza** il perimetro, cioè cancellando più del dovuto,
> in silenzio. Con i valori ridotti a token, l'iniezione è impossibile per costruzione — il carattere
> con cui si uscirebbe dal letterale è rifiutato prima — e il perimetro non può sparire per
> distrazione. Se un giorno servirà di più, si propagano i parametri nei sette punti.

**Perché non si è riusato `field_names`.** Quella colonna dice su quali colonne agisce `null_fields`:
usarla anche per il perimetro le avrebbe dato due significati a seconda dell'azione. E non basterebbe,
perché i perimetri non sono tutti della stessa forma — vedi il caso `catalog` qui sotto.

**I valori seminati** (migration `20261005111230_AddRetentionTargetFilter`):

| Policy | `target_filter` | Esito |
|---|---|---|
| `notification.media_assets` · 24 mesi da `created_at` · `delete_object` | `{"column":"kind","op":"=","value":"rendered_pdf"}` | ✅ **la policy ora gira** |
| `fiskaly.media_assets` · 10 anni da `signed_at` · `delete_object` | `{"column":"kind","op":"in","values":["fiscal_receipt_pdf","non_fiscal_receipt_pdf"]}` | ✅ gira |
| `fiskaly.media_assets` · 36 mesi · `archive_object` | lo stesso elenco | ✅ gira dal 2026-10-05, da quando la coda ha la colonna [`action`](privacy_file_deletions.md) e sa raffreddare invece di cancellare |
| `switchboard.media_assets` · `property_ceased` · `delete_object` | `{"scope":"whole_table"}` — la tabella non ha nessuna colonna di tipo | ✅ gira dal 2026-10-05: il motore deriva `property_ceased` da `identity.account_subscriptions`. ⚠️ Troverà **zero righe** finché gli abbonamenti non sono nel nuovo sistema, vedi sotto |
| `catalog.media_assets` · 24 mesi da `property_ceased` · `delete_object` | **NULL, di proposito** | ⚠️ il motore la **salta in silenzio**: la applica un job scritto a mano, `CeasedPropertyPhotoRetentionJob` |

> ### Il perimetro di `catalog` è in un job, e la policy resta la fonte dei tempi
>
> `Module.Catalog/Infrastructure/Infras/CeasedPropertyPhotoRetentionJob.cs`. Il job legge **questa
> policy** per `public_id` e ne prende durata, unità e `is_dry_run_only`: nel codice sta **solo il
> perimetro**, che è la parte che non si può dichiarare. Se la policy non c'è, non è attiva o è
> cancellata logicamente, il job non fa niente e lo scrive nel verbale.
>
> ⚠️ Il motore salta questa policy **senza scrivere nel verbale**
> (`RetentionRunOptions.PoliciesHandledElsewhere`): altrimenti una corsa produrrebbe **due righe per la
> stessa policy**, una «saltata» dal motore e una dal job, e chi apre il verbale per decidere se
> accendere la regola leggerebbe due esiti diversi dello stesso identificativo.
>
> ⚠️⚠️ **Se un domani qualcuno dichiarasse un `target_filter` su questa riga, il motore comincerebbe a
> eseguirla *oltre* al job**, con un perimetro più largo. La colonna va lasciata NULL.

> ### ⚠️ `property_ceased` non è calcolabile sui dati migrati, e non è un difetto del codice
>
> La cessazione si deriva da `identity.account_subscriptions` + `account_subscription_periods`. Quelle
> tabelle sono **vuote**: nel legacy gli abbonamenti erano gestiti **fuori dal portale** e attivati a
> mano, quindi non c'è niente da migrare (risposta del titolare del 2026-10-02, Q10 — vedi
> `utility/01-migrazione-test` → `scripts_perche_vuote_documento.py`).
>
> Conseguenza: le **due** policy con `property_ceased` trovano zero righe per costruzione, e
> continueranno a trovarne zero finché gli abbonamenti non saranno gestiti dal nuovo sistema. Il
> meccanismo è scritto e verificato con dati sintetici (`Module.Catalog.Tests`, 7 test), perché
> altrimenti sarebbe codice mai eseguito pronto a sbagliare — cancellando foto — la notte in cui i
> primi abbonamenti ci saranno.

> **Perché su fiskaly un elenco esplicito e non `whole_table`.** Oggi sono equivalenti: l'enum ha due
> sole etichette, entrambe scontrini. Ma se un domani qualcuno aggiunge un'etichetta a
> `fiskaly.media_kind`, con `whole_table` la policy comincerebbe **da sola** a cancellare anche quella.
> Un perimetro che si allarga quando si allarga un enum è un perimetro che nessuno ha deciso.
>
> **Perché su catalog non si dichiara niente.** La regola è «le prime 3 foto per `sort_order` più la
> copertina restano per sempre, le altre seguono la cessazione». Non è un confronto su una colonna: è
> «escludi le prime N secondo un **ordinamento**» più «escludi quella puntata da `main_photo_id` su
> **un'altra tabella**». `target_filter` non la esprime, e inventare qui un perimetro approssimato
> vorrebbe dire cancellare la scheda pubblica che il titolare ha chiesto esplicitamente di conservare.
> **Serve un job scritto a mano**, ed è l'unico caso rimasto.

### linked_asset 🆕
- **Legacy**: — (nuovo, 2026-10-06, migration `20261006054652_AddLinkedAssetArchiving`)
- **Descrizione**: il file che deve **seguire** le righe archiviate da questa policy.
- **Tipo**: `jsonb`. **Nullabile**: Sì — NULL quando non c'è nessun file collegato.
- **Vincoli**: `ck_..._linked_asset_action` (solo su `action = 'archive_cold'`) e
  `ck_..._linked_asset_shape` (oggetto JSON con `column`, `table` e `action`).

```jsonc
{"column": "pdf_asset_id", "table": "media_assets", "action": "archive_object"}
```

| Chiave | Che cosa dice |
|---|---|
| `column` | la colonna di **questa** tabella che porta l'id dell'asset |
| `table` | la tabella di asset, forma **canonica**, nello **stesso schema** (il motore risolve il nome fisico da sé) |
| `action` | `archive_object` (raffredda il file) oppure `delete_object` (lo rimuove) |

**Perché esiste.** Alcune righe nominano un file che è **parte del dato**, non un accessorio: la riga
di `fiskaly.receipts` nomina il PDF dello scontrino. Archiviare la riga e lasciare il PDF dov'era
produce due guai insieme — nessuna riga calda nomina più quel PDF, e la data da cui scade
(`signed_at`) è uscita dal database insieme alla riga. Non resta **niente** che possa decidere quando
cancellarlo.

Il motore, **dopo** aver scritto l'archivio e **prima** di cancellare le righe, accoda l'azione
dichiarata sul file **e** lo inventaria in
[`privacy_archive_manifests`](privacy_archive_manifests.md) con `object_kind = 'linked_asset'`: così la
policy decennale su quell'inventario cancella il file insieme all'archivio che accompagnava.

> ⚠️⚠️ **Non si risolve con una policy «cancella i file che nessuno nomina più»**, che è la soluzione
> che viene in mente per prima: dopo l'archiviazione il PDF di uno scontrino **è** per definizione
> «non nominato da nessuno», quindi quella regola lo cancellerebbe al **mese 36** invece che
> all'**anno 10**. È il motivo per cui `fiskaly.media_assets` non ha, e non deve avere, una policy
> `only_if_unreferenced`.

> **Solo su `archive_cold`**, per vincolo: su una cancellazione non servirebbe — la policy sul file
> agisce per conto suo — e su un azzeramento non vorrebbe dire niente.

**Seminato su**: la policy `fiskaly · receipts · 36 mesi da signed_at · archive_cold`. Le due policy su
`fiskaly.media_assets` sono state **ritirate** (`is_active = false`) nella stessa migration: erano
quelle che fallivano a ogni corsa, perché dichiaravano `signed_at` come decorrenza e quella colonna
sta su `receipts`, non sull'asset.

### reference_event 🆕
- **Legacy**: — (nuovo, 2026-10-03)
- **Descrizione**: evento da cui decorre la durata, in alternativa alla colonna data.
- **Tipo**: `enum privacy.retention_reference_event` — valori: `property_ceased`,
  `asset_unreferenced`, `asset_replaced`. **Nullabile**: Sì.
- **Nota**: fa parte della **chiave naturale** (vedi `uq_..._target`). ⚠️ `property_ceased` non
  corrisponde a nessuno stato a database: `catalog.property_status` è
  `pending | approved | rejected | imported`, e la cessazione si **deriva** da
  `identity.account_subscriptions.status` (`cancelled` = esplicita, `expired` senza rinnovo = di
  fatto, `suspended` = nessuna delle due), con la data da `closed_at` dell'ultimo periodo in
  `identity.account_subscription_periods`. Decisione del titolare del 2026-10-02, che chiude anche
  **D-65 punto 1**: derivarla invece di aggiungere uno stato che qualcuno deve ricordarsi di scrivere.

### storage_prefix 🆕
- **Legacy**: — (nuovo, 2026-10-03)
- **Descrizione**: prefisso di storage su cui agisce la policy quando **nessuna riga nomina i file**
  (i 743.576 QR delle prenotazioni, i ~12.000 orfani).
- **Tipo**: `varchar(255)`. **Nullabile**: Sì — alternativo a `table_name` (`ck_..._target_xor`).
- **Nota**: ⚠️⚠️ **non è il prefisso a decidere, è l'assenza di riferimento**: il prefisso limita solo
  *dove* guardare. Se un job implementasse queste policy come «cancella tutto sotto questo prefisso
  oltre N», `images/bathouse/` cancellerebbe **tutte** le foto degli stabilimenti. Difesa
  obbligatoria, come già fa `uploader.clean_test_prefix` di `utility/02-migrazione-file`: elenco
  chiuso dei prefissi ammessi, rifiuto di tutto il resto, e rifiuto di qualunque prefisso che sia
  antenato di un altro prefisso noto.

### storage_date_source 🆕
- **Legacy**: — (nuovo, 2026-10-03)
- **Descrizione**: da dove prende la data una policy su prefisso.
- **Tipo**: `enum privacy.retention_storage_date_source` — un solo valore: `object_last_modified`.
  **Nullabile**: Sì — valorizzato **se e solo se** `storage_prefix` lo è (`ck_..._prefix_date`).
- **Nota**: un'etichetta sola di proposito. Oggi l'unica data disponibile per un oggetto senza riga è
  il suo `LastModified` su S3; l'enum esiste comunque perché la colonna deve **dire** da dove viene la
  data invece di lasciarlo implicito nel codice del job.

### duration_value
- **Legacy**: — (nuovo)
- **Descrizione**: durata numerica della conservazione.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: validato positivo in applicazione (doc §6, utility Python: "validare che 'Durata' sia un
  numero positivo").

### duration_unit
- **Legacy**: — (nuovo)
- **Descrizione**: unità della durata.
- **Tipo**: `enum privacy.retention_duration_unit` — valori: `days`, `months`, `years`, **`seasons`**.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `seasons` si risolve contro
  [`catalog_pricing_seasons`](../catalog/catalog_pricing_seasons.md) — riferimento **logico**, non FK
  (schema diverso): è il job `SeasonalArchive` del modulo Booking a tradurre "2 stagioni" in una data di
  taglio, leggendo i confini della stagione corrente.
- **Note**: `seasons` è stato aggiunto il 2026-09-21 applicando i valori validati dal titolare. Non è
  zucchero sintattico per "12 mesi": una durata stagionale scade **all'apertura della stagione
  successiva** (~1 aprile), non a 365 giorni dalla data di riferimento — vedi § Seed. Una policy con
  `duration_unit = seasons` è applicabile **solo** dal job annuale, non da un job mensile: è un vincolo
  applicativo, non un `CHECK` a schema.

### action
- **Legacy**: — (nuovo)
- **Descrizione**: azione da applicare al superamento della durata.
- **Tipo**: `enum privacy.retention_action` — valori: `delete_row`, `null_fields`, `anonymize`,
  `archive_cold`, 🆕 `delete_object`, 🆕 `archive_object`.
- **Nota (2026-10-03)**: le due etichette nuove agiscono sull'**oggetto su storage**, non sulla riga —
  le quattro precedenti agiscono tutte su una riga, e `archive_cold` sposta *la riga*, non il file. Le
  stesse due sono state aggiunte a `privacy.audit_action`: senza, la cancellazione di un file non
  sarebbe tracciabile in `privacy_compliance_audit_logs` e un file rimosso per policy non si
  distinguerebbe da un file mai esistito. ⚠️ **Vincolo applicativo**: `delete_object` non va eseguita
  in linea ma accodata in [`privacy_file_deletions`](privacy_file_deletions.md), perché una `DELETE`
  su S3 non partecipa alla transazione del database.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: vocabolario esatto di
  [`docs/11-modulo-privacy.md` §6](../../../progettazione/11-modulo-privacy.md).

### legal_basis
- **Legacy**: — (nuovo)
- **Descrizione**: riferimento normativo o motivazione di policy aziendale (es. "Art. 2220 c.c.: 10 anni
  dall'ultima registrazione" o "Policy interna: analogia prescrizione ordinaria art. 2946 c.c.").
- **Tipo**: `text`.
- **Regola di redazione**: contiene **la motivazione e nient'altro**. Niente percorsi di documenti
  interni (`docs/…`, schede `.md`), niente riferimenti a chi ha deciso il termine o a quale fosse la
  proposta precedente. Il testo viene letto da un pannello o da una query — spesso da chi non ha il
  repository sotto mano — e deve reggersi da solo: un rimando a un file che nel frattempo è stato
  rinominato non spiega più niente, e *chi* ha scelto un termine non è una base giuridica. La
  tracciabilità della decisione (verbale, data, proposta superata) vive **qui nella scheda**, che è il
  posto giusto per la storia; il dato porta la sostanza.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### basis_source
- **Legacy**: — (nuovo)
- **Descrizione**: qualifica la fonte del termine — distingue un obbligo verificato da una proposta.
- **Tipo**: `enum privacy.retention_basis_source` — valori: `law` (obbligo verificato su testo normativo),
  `recommended_policy` (proposta ragionevole, da approvare DPO/legale), `disputed` (qualificazione
  giuridica non risolta, non attivare senza parere specialistico).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: legenda identica a quella di
  [`_domande-committente.md`](../_domande-committente.md) ("Legge" / "Policy consigliata" / "Dubbio") e di
  [`docs/11-modulo-privacy.md` §6 "Tabella delle durate"](../../../progettazione/11-modulo-privacy.md).
- **Note**: una policy con `basis_source = disputed` dovrebbe restare `is_dry_run_only = true` finché non
  passa a `law`/`recommended_policy` con parere ricevuto — vincolo applicativo, non un `CHECK` a schema
  (il parere arriva fuori banda).

### is_dry_run_only
- **Legacy**: — (nuovo)
- **Descrizione**: se `true`, il job produce solo un report (righe che verrebbero toccate) **senza**
  eseguire alcuna azione — obbligatorio prima di abilitare l'esecuzione reale in produzione
  ([`docs/11-modulo-privacy.md` §6](../../../progettazione/11-modulo-privacy.md)).
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: disattivato esplicitamente da un umano (SystemAdmin/DPO) dopo revisione del report di
  dry-run, mai di default alla creazione della policy.

### is_active
- **Legacy**: — (nuovo)
- **Descrizione**: policy attiva/sospesa. Permette di sospendere una regola (es. in attesa di un parere
  legale) senza cancellarla.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica della regola.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete della policy (regola ritirata).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: Global Query Filter EF standard — a differenza delle altre tabelle di questo modulo
  (registri di prova), questa **è** una tabella di dominio applicativo normale (configurazione viva).

## Valutazione JSON

`field_names` è un **array tipizzato**, non un `jsonb`: è una lista piatta di stringhe (nomi colonna), mai
annidata, letta per intero — un array PostgreSQL nativo è indicizzabile e sufficiente, un `jsonb` non
aggiungerebbe nulla ([`docs/03-gestione-dati.md` §2.5](../../../progettazione/03-gestione-dati.md)).

## Suggerimenti per le prestazioni

- Tabella minuscola (poche decine di righe, una per combinazione tabella/campo/policy — la "tabella delle
  durate" di doc §6 ne enumera già una decina).
- Letta raramente e in blocco da ogni job di modulo all'avvio del proprio ciclo di retention (non in hot
  path applicativo) → nessuna preoccupazione di prestazioni; cache in-memory del job con refresh periodico
  possibile ma non necessaria a questo volume.
- Nessun bisogno di partizionamento o indici aggiuntivi.

## Implementazione .NET

**Entità di configurazione indipendente** `RetentionPolicy`, modulo `Module.Privacy`, schema `privacy`.
Aggregate root leggero: vita propria via CRUD dal pannello SystemAdmin/DPO (creazione, revisione dry-run,
attivazione), non solleva eventi di dominio verso altri moduli — è **letta**, mai eseguita da qui: ogni
modulo applica la regola sulla propria tabella tramite il proprio `IRetentionJob`/`DbContext`
([`docs/11-modulo-privacy.md` §6](../../../progettazione/11-modulo-privacy.md)).

`ISoftDeletable` + `IAuditable`, a differenza delle altre tabelle di questo modulo (registri di prova con
`deleted_at` inerte): questa **è** configurazione viva, il Global Query Filter riflessivo standard (§1 del
pattern comune) si applica normalmente.

### Enum propri

```csharp
// Domain/Enums/RetentionDurationUnit.cs
// Seasons: durata espressa in stagioni, risolta dal job annuale sui confini di catalog.pricing_seasons.
public enum RetentionDurationUnit { Days = 0, Months = 1, Years = 2, Seasons = 3 }

// Domain/Enums/RetentionAction.cs
public enum RetentionAction { DeleteRow = 0, NullFields = 1, Anonymize = 2, ArchiveCold = 3 }

// Domain/Enums/RetentionBasisSource.cs
public enum RetentionBasisSource { Law = 0, RecommendedPolicy = 1, Disputed = 2 }
```

```csharp
// PrivacyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<RetentionDurationUnit>("privacy", "retention_duration_unit");
modelBuilder.HasPostgresEnum<RetentionAction>("privacy", "retention_action");
modelBuilder.HasPostgresEnum<RetentionBasisSource>("privacy", "retention_basis_source");
```

```csharp
// Registrazione data source (Module.cs / Infrastructure DI)
dataSourceBuilder.MapEnum<RetentionDurationUnit>("privacy.retention_duration_unit");
dataSourceBuilder.MapEnum<RetentionAction>("privacy.retention_action");
dataSourceBuilder.MapEnum<RetentionBasisSource>("privacy.retention_basis_source");
```

### Configuration

```csharp
namespace Module.Privacy.Infrastructure.Configurations;

public sealed class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
	public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
	{
		builder.ToTable("privacy_retention_policies", "privacy", t => t.HasComment(
			"Configurazione dichiarativa di retention: su schema.tabella (campo/i X), calcolato dalla " +
			"colonna data Y, oltre la durata Z, esegui l'azione W. Letta dal job del modulo proprietario, " +
			"mai eseguita da qui. Ambito piattaforma, valida per tutti gli stabilimenti."));

		builder.HasKey(x => x.Id).HasName("pk_privacy_retention_policies");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico per il pannello di revisione SystemAdmin/DPO.");

		builder.Property(x => x.SchemaName)
			.HasColumnName("schema_name")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Schema PostgreSQL del modulo proprietario della tabella target — riferimento " +
				"testuale, non FK (punta fuori da privacy per definizione), validato contro module_code.");

		builder.Property(x => x.TableName)
			.HasColumnName("table_name")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Tabella target nella forma canonica relativa allo schema (senza prefisso modulo).");

		builder.Property(x => x.FieldNames)
			.HasColumnName("field_names")
			.HasColumnType("varchar(120)[]")
			.HasDefaultValueSql("'{}'")
			.IsRequired()
			.HasComment("Campo/i a cui si applica l'azione; array vuoto = l'intera riga (rilevante per " +
				"delete_row/archive_cold).");

		builder.Property(x => x.ReferenceDateColumn)
			.HasColumnName("reference_date_column")
			.HasMaxLength(63)
			.IsRequired()
			.HasComment("Colonna sorgente della data di riferimento da cui decorre la durata.");

		builder.Property(x => x.DurationValue)
			.HasColumnName("duration_value")
			.HasComment("Durata numerica della conservazione, validata positiva in applicazione.");

		builder.Property(x => x.DurationUnit)
			.HasColumnName("duration_unit")
			.HasComment("Unità della durata: days, months, years o seasons (stagioni, risolte dal job " +
				"annuale sui confini di catalog.pricing_seasons — non equivalenti a 12 mesi).");

		builder.Property(x => x.Action)
			.HasColumnName("action")
			.HasComment("Azione da applicare al superamento della durata: delete_row, null_fields, " +
				"anonymize o archive_cold.");

		builder.Property(x => x.LegalBasis)
			.HasColumnName("legal_basis")
			.HasColumnType("text")
			.IsRequired()
			.HasComment("Riferimento normativo o motivazione di policy aziendale del termine scelto.");

		builder.Property(x => x.BasisSource)
			.HasColumnName("basis_source")
			.HasComment("Qualifica la fonte del termine: law (obbligo verificato), recommended_policy " +
				"(proposta da approvare) o disputed (qualificazione non risolta, non attivare).");

		builder.Property(x => x.IsDryRunOnly)
			.HasColumnName("is_dry_run_only")
			.HasDefaultValue(true)
			.HasComment("Se true, il job produce solo un report senza eseguire l'azione — obbligatorio " +
				"prima di abilitare l'esecuzione reale.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.HasDefaultValue(true)
			.HasComment("Policy attiva/sospesa: permette di sospendere una regola senza cancellarla.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della regola.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della regola.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della policy (regola ritirata) — Global Query Filter EF standard.");

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_privacy_retention_policies_public_id");

		// Action fa parte della chiave naturale: la politica validata è spesso a DUE FASI sullo
		// stesso target (archivia a N, cancella a 10 anni). Senza Action qui, la seconda fase
		// violerebbe l'indice e la politica non sarebbe rappresentabile.
		builder.HasIndex(x => new { x.SchemaName, x.TableName, x.FieldNames, x.Action })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_privacy_retention_policies_target");

		builder.HasIndex(x => new { x.SchemaName, x.TableName })
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_privacy_retention_policies_lookup");
	}
}
```

### Relazioni

- `schema_name`/`table_name`/`reference_date_column`: riferimenti **testuali**, non FK — puntano per
  definizione fuori dallo schema `privacy` (violerebbero "zero FK cross-schema" altrimenti); validati in
  applicazione contro il vocabolario `ModuleCode` (stesso enum di
  [`privacy_data_subject_request_module_outcomes`](privacy_data_subject_request_module_outcomes.md)).
- Referenziata (opzionale, per valore/FK interna) da `retention_policy_id` di
  [`privacy_compliance_audit_logs`](privacy_compliance_audit_logs.md) — FK configurata lato log.
- Nessuna relazione in ingresso da altri moduli: la lettura da parte dei job di modulo avviene per
  `schema_name`/`table_name`, non per FK (i moduli non hanno visibilità sullo schema `privacy` come
  dipendenza di dominio).

### Note di migrazione

Tabella **nuova**: nessun dato legacy (il legacy non ha mai avuto retention dichiarativa — "violata
sistematicamente", `docs/01-criticita-e-indicazioni-backend.md`). Da popolare con un **seed applicativo**
(non migration dati): le righe sono elencate per intero nel **§ Seed** qui sotto, derivate dai valori
**validati dal titolare** in [`docs/82-retention-data.md`](../../../progettazione/82-retention-data.md) e
rielaborate in [`docs/80-retention.md` §3/§6/§7](../../../progettazione/80-retention.md).

Tutte le righe seed nascono con `is_dry_run_only = true` e `basis_source` coerente (in prevalenza
`recommended_policy`, alcune `law` per i termini fiscali, tre `disputed` — traffico telefonico/WhatsApp,
D.L. 7/2015 — da **non** attivare senza parere legale), incluso l'auto-riferimento
`(schema_name='privacy', table_name='compliance_audit_logs', …)` con cui il modulo applica a sé stesso lo
stesso meccanismo dichiarativo che offre agli altri.

**Il seed è idempotente e ri-eseguibile**: la chiave naturale è
`(schema_name, table_name, field_names, action, reference_event, storage_prefix)` — lo stesso
`uq_..._target` 🔄 — quindi l'upsert non duplica nulla e non sovrascrive
`is_dry_run_only`/`is_active` già cambiati a mano da un umano. Una durata che cambia dopo una nuova
validazione del titolare è un `UPDATE` su una riga, **non** una release del backend: questo è
esattamente il punto della tabella.

> ⚠️ **L'`ON CONFLICT` di una migration di seed cita la forma dell'indice del suo momento.** La
> migration del 2026-09-21 usa `ON CONFLICT (schema_name, table_name, field_names, action)` e resta
> valida perché viene applicata **prima** di quella che rifà l'indice: su un database vuoto l'ordine
> cronologico delle migration la salva. Quella del 2026-10-03 cita la forma a sei colonne. Chi
> toccherà ancora l'indice deve ricordarsi che qui ci sono due `ON CONFLICT` diversi, e che sono
> giusti entrambi.
>
> **Perché un `DO UPDATE` e non un `DO NOTHING`**: una motivazione riscritta o una durata rivalidata
> devono aggiornare la riga. Ma l'`UPDATE` ha una clausola `WHERE … IS DISTINCT FROM …` che lo
> esegue solo se qualcosa è davvero cambiato, così un ri-seed non sposta `updated_at` di 59 righe e
> non fa sembrare modificato ciò che non lo è. E **non tocca mai** `is_dry_run_only` né `is_active`:
> sono decisioni operative di una persona, e un ri-seed non deve riaccendere una policy che qualcuno
> ha spento.

> ### ✅ Implementato il 2026-09-21
>
> Non è più solo un progetto. Nel backend `backend/summerbooking-backend/Module.Privacy`:
>
> | Migration | Cosa fa |
> |---|---|
> | `20260921074734_AddRetentionSeasonsUnitAndTwoPhaseTargetIndex` | aggiunge `seasons` a `privacy.retention_duration_unit` e ricrea `uq_..._target` includendo `action` |
> | `20260921074959_SeedRetentionPolicies` | semina le **44 righe** con un `INSERT … ON CONFLICT DO UPDATE` |
>
> Applicate su `summer_booking_dev`: 44 righe, **tutte in dry-run**, 2 con `basis_source = disputed`.
> `dotnet build` 0 warning/error, `ArchitectureTests` 56/56.


> ### ✅ Implementato il 2026-10-03 — le policy sui **file**
>
> Cinque migration su questa tabella e sui suoi enum, più il seed. Il *perché* di ogni scelta è in
> [`docs/83-retention-file.md`](../../../progettazione/83-retention-file.md); qui c'è il *come*.
>
> | Migration | Cosa fa |
> |---|---|
> | `20261003074912_AddRetentionObjectActionsAndFileEventEnums` | `delete_object` e `archive_object` su `privacy.retention_action` **e** su `privacy.audit_action`; crea `privacy.retention_reference_event` e `privacy.retention_storage_date_source` |
> | `20261003075313_AddRetentionFileTargetingColumns` | le 4 colonne nuove, i 3 `CHECK`, l'indice unico rifatto |
> | `20261003080319_AddRetentionDecorrenzaCheck` | 🆕 `ck_..._has_decorrenza`, **non previsto in specifica** |
> | `20261003080717_SeedFileRetentionPolicies` | **15 righe** (sezione FILE del seed) |
>
> Fuori da questa tabella, nello stesso giro: `privacy.privacy_file_deletions`
> ([scheda](privacy_file_deletions.md)), `catalog.media_assets.property_id`
> ([scheda](../catalog/catalog_media_assets.md)) e `catalog.agreement_reports`
> ([scheda](../catalog/catalog_agreement_reports.md)).
>
> Stato: **20 colonne**, **59 righe** (44 + 15), tutte in dry-run. `dotnet build` 0 warning/error,
> `ArchitectureTests` 68/68, seed verificato idempotente rieseguendolo a mano.
>
> #### Perché quattro colonne qui e non una tabella `privacy_file_retention_policies` separata
>
> Una tabella a sé sarebbe più espressiva — le quattro colonne sono `NULL` su 44 righe su 59 — ma
> spezzerebbe in due il posto dove si leggono le regole: il pannello systemadmin, l'audit e la *dry
> run* dovrebbero interrogare due modelli, e **due modelli divergono**. Con quattro colonne il
> modello resta uno, e il costo è qualche `NULL`.
>
> #### Aggiungere un'etichetta a un enum: il precedente da replicare
>
> `ALTER TYPE … ADD VALUE` non si può eseguire dentro una transazione nelle versioni di PostgreSQL
> precedenti alla 12, ed EF Core mette ogni migration in transazione. Nel progetto il precedente è
> già risolto: `20260921074734_AddRetentionSeasonsUnitAndTwoPhaseTargetIndex` ha aggiunto `seasons` a
> `retention_duration_unit` con `AlterDatabase().Annotation("Npgsql:Enum:…")`, lasciando a Npgsql il
> compito di emettere l'SQL giusto. **Replicare quell'approccio**, non inventarne un altro.
>
> ⚠️ Una nuova etichetta **non si può usare nella stessa transazione che l'ha aggiunta**. È il motivo
> per cui le etichette e i `CHECK` che le citano (`ck_..._prefix_action` nomina `delete_object`) sono
> in **due migration separate**: EF applica ogni migration in una transazione sua, e metterle insieme
> avrebbe fatto fallire la seconda metà.
>
> ⚠️ Lato .NET: `HasColumnType` è obbligatorio su ogni property enum, e il default va con
> `HasDefaultValueSql`, **mai** `HasDefaultValue(Enum.Membro)` — vedi
> [`docs/43-migration-procedure.md` §4](../../../progettazione/43-migration-procedure.md). E la lista degli
> enum va tenuta allineata in **tre** file: `DbContext.OnModelCreating`,
> `ModulePersistenceRegistration` e `DesignTimeDbContextFactory`. Dimenticare il terzo non rompe
> `dotnet ef`, rompe il runtime.
>
> #### Quello che si poteva pensare di aggiungere, e che **non** serve
>
> | Cosa | Perché non serve |
> |---|---|
> | Una vista materializzata o un trigger per contare i riferimenti | `only_if_unreferenced` si risolve con una query di risalita sulle 19 chiavi esterne al momento della valutazione. È un job giornaliero, non un percorso caldo. Si rivaluta se i tempi lo richiedono |
> | Un flag «foto pubblica» su `catalog.bathhouse_photos` | la regola è «le prime 3 per `sort_order` più `main_photo_id`», e `sort_order` c'è già. Un flag andrebbe mantenuto in sincrono con l'ordinamento: due verità per la stessa cosa |
> | Una colonna per il soft-delete delle foto | `catalog.bathhouse_photos.deleted_at` **esiste già**, come su `media_assets` e sulle altre utilizzatrici |
> | Modifiche al seed delle 9 policy stagionali | **nessuna**: il seed scrive `duration_value` + `duration_unit = 'seasons'` e non contiene nessuna data di confine. «Il confine è il 1 gennaio» è l'interpretazione di *stagione* e vive nel job |
> | Uno stato `ceased` su `catalog.property_status` | la cessazione si **deriva**: vedi [`docs/80-retention.md` §5](../../../progettazione/80-retention.md) |
>
> #### ⚠️ Il controllo da fare su ogni riga di seed, prima di seminarla
>
> **Una policy non vale niente se la colonna in `reference_date_column` è vuota sui dati reali.** Non
> basta che la colonna esista. Verificato il 2026-10-02 sulle 44 righe preesistenti:
>
> | Colonna | Righe | Verdetto |
> |---|---|---|
> | `identity.customer_customers.last_activity_at` | 224.524, **tutte NULL** | ❌ **dato mancante**: tre policy (anonimizza a 48 mesi, azzera `note` a 1 stagione, cancella a 10 anni) non toccavano **nessun** cliente legacy. L'ETL ora la popola — vedi [`_colonne-vuote.md` §3.4 (Q14)](../../../analisi-pre/db-no-copied/_colonne-vuote.md) |
> | `identity.access_users.deleted_at` | 6.674, tutte NULL | ✅ **corretto**: significa «nessun account revocato» |
> | `fiskaly.cash_registers.deleted_at` | 5, tutte NULL | ✅ **corretto**: significa «nessuna cassa dismessa» |
>
> Distinguere i due casi è tutto il punto del controllo: una colonna vuota perché il fatto non è
> ancora accaduto è sana, una vuota perché nessuno la scrive è una policy che non scatterà mai. Il
> vincolo `ck_..._has_decorrenza` copre il caso più grossolano (nessuna decorrenza dichiarata), non
> questo: questo si vede solo guardando i dati.

---

## Seed — policy validate dal titolare (2026-09-17)

> Le ~40 righe con cui la tabella nasce. **Fonte dei valori**: colonna *"Considerazioni / scelta del
> proprietario"* di [`docs/82-retention-data.md`](../../../progettazione/82-retention-data.md) (+ chiarimenti
> Q1–Q6 del 2026-09-17), rielaborata in [`docs/80-retention.md` §3](../../../progettazione/80-retention.md). Dove
> il titolare ha scostato dalla proposta iniziale la colonna "Nota" lo dice.
>
> `table_name` è nella **forma canonica relativa allo schema** (senza prefisso di modulo):
> `booking.reservations`, non `booking_reservations`. `field_names` vuoto (`{}`) = azione sull'**intera
> riga**. Tutte le righe nascono `is_dry_run_only = true`, `is_active = true`.

### Perché `seasons` è un'unità e non una conversione in mesi

Il blocco C1 è espresso dal titolare in **stagioni**, e la conversione in mesi non è innocua.

Una prenotazione con `stay_to = 2024-09-15`, policy **N−2** (online: stagione corrente + le due
precedenti). Al 2026-09 la stagione corrente è la 2026 e le stagioni online sono 2026/2025/**2024**: la
prenotazione **deve ancora essere online**. Ma "24 mesi da `stay_to`" scade proprio il 2026-09-15 e la
archivierebbe **in piena stagione**, con un anno di anticipo sul dovuto. Il taglio corretto è l'apertura
della stagione 2027 (~1 aprile 2027), cioè ~30 mesi — ma "30 mesi" è altrettanto sbagliato per una
prenotazione di giugno.

Non esiste un numero di mesi che valga per tutte le prenotazioni della stessa stagione, perché la
decorrenza reale è il **confine di stagione**, non la data della singola riga. Da qui `seasons` come
unità di prima classe: la traduzione in una data di taglio la fa il job annuale leggendo
[`catalog_pricing_seasons`](../catalog/catalog_pricing_seasons.md), una volta per tutte le righe.

### C1 — `booking`

| `schema.table_name` | `field_names` | `reference_date_column` | Durata | `action` | `basis_source` | Nota |
|---|---|---|---|---|---|---|
| `booking.reservations` | `{}` | `stay_to` | 2 `seasons` | `archive_cold` | `recommended_policy` | Testata: **N−2** *(proposta: N−1)*. L'archivio si cancella a 10 anni se la prenotazione è fiscalizzata → riga successiva |
| `booking.reservations` | `{}` | `stay_to` | 10 `years` | `delete_row` | `law` | Cancellazione dell'archivio — art. 2220 c.c. Applicata **sull'archivio freddo**, non sul DB caldo |
| `booking.reservations` | `{note, customer_tags}` | `stay_to` | 1 `seasons` | `null_fields` | `law` | Note libere: **N−1**, una stagione **prima** della testata che le contiene. Possibili dati art. 9 GDPR ([D-25](../_domande-committente.md)) |
| `booking.lines` | `{}` | `end_date` | 2 `seasons` | `delete_row` | `recommended_policy` | Dettaglio postazione allineato alla testata *(proposta: fine S + 1 S)* |
| `booking.service_lines` | `{}` | `service_date` | 1 `seasons` | `delete_row` | `recommended_policy` | Prenotazioni di soli servizi: **N−1** *(proposta: fine S)*; la testata economica resta se c'è scontrino |
| `booking.resource_consumptions` | `{}` | `consumption_date` | 1 `seasons` | `delete_row` | `recommended_policy` | **Solo dopo l'aggregato mensile** (scelta esplicita del titolare) — vincolo applicativo del job, non esprimibile a schema |
| `booking.resale_offers` | `{}` | `offer_date` | 2 `seasons` | `delete_row` | `recommended_policy` | Assenze / rivendita del posto: **N−2** *(proposta: N−1)* |
| `booking.spot_holds` | `{}` | `end_date` | 2 `seasons` | `delete_row` | `recommended_policy` | Riserve morbide: **N−2** *(proposta: fine S)*. **Non** confonderla col TTL di minuti in checkout, che è il job giornaliero `PurgeExpiredMapHolds` |
| `booking.groups` | `{}` | `created_at` | 2 `seasons` | `delete_row` | `recommended_policy` | Segue la testata |
| `booking.resale_credit_ledger` | `{}` | `occurred_at` | 10 `years` | `archive_cold` | `law` | Il credito maturato è contabilità wallet (C3), non dettaglio stagionale |

### C3 — `payment`

| `schema.table_name` | `field_names` | `reference_date_column` | Durata | `action` | `basis_source` | Nota |
|---|---|---|---|---|---|---|
| `payment.transaction_charges` | `{}` | `occurred_at` | 24 `months` | `archive_cold` | `recommended_policy` | Copre riconciliazione + finestra chargeback |
| `payment.transaction_refunds` | `{}` | `occurred_at` | 24 `months` | `archive_cold` | `recommended_policy` | Come sopra |
| `payment.wallet_transactions` | `{}` | `occurred_at` | 24 `months` | `archive_cold` | `recommended_policy` | Movimenti wallet |
| `payment.ledger_entries` | `{}` | `occurred_at` | 24 `months` | `archive_cold` | `recommended_policy` | Include gli snapshot PII `customer_name/email/phone_snapshot` |
| `payment.ledger_entries` | `{}` | `occurred_at` | 10 `years` | `delete_row` | `law` | Cancellazione dell'archivio — art. 2220 c.c. |
| `payment.pos_charges` | `{}` | `charged_at` | 24 `months` | `archive_cold` | `recommended_policy` | Spese/addebiti da POS esterni |
| `payment.pos_charges` | `{}` | `charged_at` | **10 `years`** | `delete_row` | `law` | ⚠️ **Correzione del titolare** (chiarimento Q4): l'ipotesi precedente era "cancella archivio a 5 anni". Concorrono al conto → obbligo fiscale pieno |
| `payment.request_links` | `{stripe_payment_link_id}` | `created_at` | 12 `months` | `null_fields` | `recommended_policy` | Il *payload d'invio* scade a 12 mesi; il *fatto* "pagamento richiesto/esito" segue la transazione |
| `payment.customer_gateway_profiles` | `{}` | `last_used_at` | 24 `months` | `delete_row` | `recommended_policy` | ⚠️ Decorrenza = **ultima attività**, **non** la chiusura account: il titolare ha deciso che la mappatura **permane** anche se l'account chiude *(proposta: cancellazione immediata alla chiusura)* |

### C4 / C4bis — `fiskaly`, `fiscalprint`

| `schema.table_name` | `field_names` | `reference_date_column` | Durata | `action` | `basis_source` | Nota |
|---|---|---|---|---|---|---|
| `fiskaly.receipts` | `{}` | `signed_at` | **36 `months`** | `archive_cold` | `recommended_policy` | Online **36 mesi** *(proposta: ~24)* — il valore della riga specifica batte il cheat sheet C4 generico |
| `fiskaly.receipts` | `{}` | `signed_at` | 10 `years` | `delete_row` | `law` | **Mai** prima dei 10 anni, nemmeno su richiesta dell'interessato (art. 17.3.b GDPR) |
| `fiskaly.receipt_logs` | `{}` | `created_at` | **12 `months`** | `delete_row` | `recommended_policy` | Diagnostica, non il documento. Estremo inferiore del range 12–24 |
| `fiskaly.cash_registers` | `{}` | `deleted_at` | 10 `years` | `delete_row` | `law` | Azione **cancella** (scelta del titolare), non archivia: il device non è un documento fiscale. Decorrenza approssimata con `deleted_at` — vedi *Limiti noti* |
| `fiscalprint.receipts` | `{}` | `printed_at` | 12 `months` | `delete_row` | `recommended_policy` | Log stampe RT; il documento fiscale è il journal del dispositivo, non questa tabella |

### C5 — `identity`

| `schema.table_name` | `field_names` | `reference_date_column` | Durata | `action` | `basis_source` | Nota |
|---|---|---|---|---|---|---|
| `identity.customer_customers` | `{full_name, phone, phone_secondary, email, address, city, postal_code, date_of_birth, other_names}` | `last_activity_at` | **48 `months`** | `anonymize` | `recommended_policy` | *(proposta: 24–36 mesi)*. Restano id, codice fiscale e importi per il fiscale |
| `identity.customer_customers` | `{note, discount_note}` | `last_activity_at` | 1 `seasons` | `null_fields` | `law` | Le note scadono **prima** del resto dell'anagrafica: possibili dati art. 9 |
| `identity.customer_customers` | `{}` | `last_activity_at` | 10 `years` | `delete_row` | `law` | Cancellazione completa se legata a documenti fiscali |
| `identity.access_users` | `{}` | `deleted_at` | **12 `months`** | `delete_row` | `recommended_policy` | ⚠️ **Cancellazione**, non pseudonimizzazione *(proposta: 🔒 a 24 mesi)*: per l'audit fiscale basta il riferimento allo **stabilimento**, non l'identità dell'utente (chiarimento Q5) |
| `identity.account_contract_acceptances` | `{ip_address}` | `occurred_at` | 24 `months` | `anonymize` | `recommended_policy` | Troncamento dell'ultimo ottetto; la riga vive contratto + 10 anni |
| `identity.customer_bathhouse_likes` | `{}` | `liked_at` | 24 `months` | `delete_row` | `recommended_policy` | Preferiti consumer |

### C6 — `notification`

| `schema.table_name` | `field_names` | `reference_date_column` | Durata | `action` | `basis_source` | Nota |
|---|---|---|---|---|---|---|
| `notification.deliveries` | `{rendered_body}` | `sent_at` | **150 `days`** | `null_fields` | `recommended_policy` | *(proposta: 90 gg)* |
| `notification.deliveries` | `{}` | `sent_at` | 12 `months` | `delete_row` | `recommended_policy` | Metadati d'invio |
| `notification.inapp_messages` | `{}` | `created_at` | **6 `months`** | `delete_row` | `recommended_policy` | Estremo inferiore del range 6–12. `notification.inapp_message_reads` segue per FK cascade |
| `notification.marketing_campaign_recipients` | `{}` | `sent_at` | 6 `months` | `delete_row` | `recommended_policy` | Snapshot destinatari: array di email congelati, nessun uso dopo l'invio |
| `notification.marketing_email_campaigns` | `{}` | `sent_at` | 24 `months` | `delete_row` | `recommended_policy` | Clock sull'**ultimo uso** |
| `notification.marketing_automations` | `{}` | `last_run_at` | 24 `months` | `delete_row` | `recommended_policy` | Idem — `last_run_at`, non `created_at` |
| `notification.marketing_contact_lists` | `{}` | `updated_at` | 24 `months` | `delete_row` | `recommended_policy` | Clock = attività **della lista**, non del singolo membro; i membri seguono per FK cascade |
| `notification.establishment_claim_requests` | `{}` | `created_at` | 12 `months` | `delete_row` | `recommended_policy` | Lead "richiedi la tua pagina" |

### C7 — `switchboard`

| `schema.table_name` | `field_names` | `reference_date_column` | Durata | `action` | `basis_source` | Nota |
|---|---|---|---|---|---|---|
| `switchboard.voice_call_records` | `{}` | `started_at` | **6 `months`** | `delete_row` | **`disputed`** | *(proposta: 12 mesi con pseudonimizzazione)*. Il titolare ha deciso 6 mesi + cancellazione, ma il dubbio **D.L. 7/2015 (72 mesi)** non è risolto: la riga **resta in dry-run** finché non arriva un parere legale ([D-51](../_domande-committente.md)) |
| `switchboard.voice_call_diagnostics` | `{}` | `created_at` | **30 `days`** | `delete_row` | `recommended_policy` | *(proposta: 30–90 gg)*. Pura diagnostica, oggi disabilitata di default |
| `switchboard.messaging_wa_event_logs` | `{}` | `received_at` | **6 `months`** | `delete_row` | **`disputed`** | *(proposta: 6–12 mesi con pseudonimizzazione del payload)*. Stesso dubbio D.L. 7/2015 → dry-run |

### C8 — `chatbot`

| `schema.table_name` | `field_names` | `reference_date_column` | Durata | `action` | `basis_source` | Nota |
|---|---|---|---|---|---|---|
| `chatbot.conversation_turns` | `{}` | `created_at` | 12 `months` | `delete_row` | `recommended_policy` | Confermato. Testo libero con possibili dati di terzi ([D-54](../_domande-committente.md)) |

### FILE — oggetti su storage 🆕 *(seminate il 2026-10-03)*

**15 righe**, aggiunte dalla migration `20261003080717_SeedFileRetentionPolicies`. Politica validata dal
titolare il 2026-10-01 e completata il 2026-10-02; regole in
[`docs/80-retention.md` §3.11](../../../progettazione/80-retention.md), analisi e numeri misurati in
[`docs/83-retention-file.md`](../../../progettazione/83-retention-file.md), specifica a database in
[`docs/80-retention.md` §3.11](../../../progettazione/80-retention.md).

Sono 15 e non 11 perché la voce F11 della specifica elenca **cinque** prefissi diversi, e
`storage_prefix` ne contiene uno per riga.

#### Policy su riga

| # | `schema.table_name` | `reference_date_column` | `reference_event` | Durata | `action` | `only_if_unref.` |
|---|---|---|---|---|---|---|
| F1 | `notification.media_assets` | `deleted_at` | `asset_unreferenced` | 2 `days` | `delete_object` | **sì** |
| F2 | `catalog.media_assets` | `deleted_at` | `asset_unreferenced` | 2 `days` | `delete_object` | **sì** |
| F3 | `fiskaly.media_assets` | `signed_at` | — | 36 `months` | `archive_object` | no |
| F4 | `fiskaly.media_assets` | `signed_at` | — | 10 `years` | `delete_object` | no |
| F5 | `notification.media_assets` | `created_at` | — | 24 `months` | `delete_object` | no |
| F6 | `switchboard.media_assets` | *(vuoto)* | `property_ceased` | 0 `days` | `delete_object` | no |
| F7 | `catalog.media_assets` | *(vuoto)* | `property_ceased` | 24 `months` | `delete_object` | no |

#### Policy su prefisso — nessuna riga a database

| # | `schema_name` | `storage_prefix` | Durata | `action` | `only_if_unref.` |
|---|---|---|---|---|---|
| F8 | `notification` | `images/qrcodescan/` | 2 `seasons` | `delete_object` | **sì** |
| F9 | `catalog` | `images/bathouse/` | 0 `days` | `delete_object` | **sì** |
| F10 | `catalog` | `images/configure/` | 0 `days` | `delete_object` | **sì** |
| F11a | `catalog` | `images/property/` | 0 `days` | `delete_object` | **sì** |
| F11b | `catalog` | `pdf/` | 0 `days` | `delete_object` | **sì** |
| F11c | `notification` | `file/` | 0 `days` | `delete_object` | **sì** |
| F11d | `notification` | `files[]/` | 0 `days` | `delete_object` | **sì** |
| F11e | `notification` | `files/newsletter/` | 0 `days` | `delete_object` | **sì** |

Tutte hanno `table_name = ''`, `field_names = '{}'`, `reference_date_column = ''` e
`storage_date_source = 'object_last_modified'`.

#### Le convenzioni di queste righe, e perché

| Convenzione | Perché |
|---|---|
| `table_name = ''` sulle policy su prefisso | non hanno una tabella, e `ck_..._target_xor` impone che una delle due sia vuota |
| `reference_date_column = ''` su F6, F7 e su tutte quelle su prefisso | nessuna colonna locale porta la data: la dà `reference_event` o `storage_date_source` |
| `reference_date_column = 'deleted_at'` su F1 e F2 | su `media_assets` il soft-delete **è** la materializzazione di «nessuna riga lo usa più»: la colonna giusta esiste, e usare il segnaposto qui sarebbe stato pigro |
| `schema_name` sulle policy su prefisso | è il modulo che **possiede** quei file (`catalog` per foto e listini, `notification` per allegati e QR). È una scelta: una policy su prefisso non ha una tabella da cui ricavarlo, e serve a far sì che `ix_..._lookup` continui a rispondere alla domanda «quali sono le policy del mio modulo» |
| `only_if_unreferenced = true` su **tutte** e otto quelle su prefisso | ⚠️⚠️ non è il prefisso a decidere, è l'assenza di riferimento. I QR compresi: oggi nessuna riga li nomina, quindi il controllo risponde sempre «non referenziato», ma scritto così la regola resta vera anche se un domani qualcuno li referenziasse |
| `duration_value = 0` su F6 e sulle sette policy sugli orfani | «senza attesa». ⚠️ La validazione applicativa di `duration_value` **deve ammettere lo zero**: il commento della colonna dice «validata positiva in applicazione», e preso alla lettera scarterebbe otto righe legittime |

#### Tre tipi di file per cui **non** c'è nessuna policy, di proposito

| Tipo | Perché |
|---|---|
| **Icone del catalogo** (`images/services/`, `images/servicetypes/`, le 89 di `configure_images`) | permanenti senza eccezioni. «Del gestionale» significa **utilizzabili da tutti i gestori**, e comprende le voci non ancora messe in tavolozza e quelle nascoste con `status = false`, che un flag può riportare in uso |
| **Foto della scheda pubblica** (prime 3 per `sort_order` + copertina) | permanenti. È una conservazione **decisa**, non l'assenza di una regola: va scritta nella documentazione anche se non produce una riga |
| **Le 272 foto senza vetrina** | recuperate da `properties.photos` per property in attesa o approvate, restano senza riga di aggancio **per costruzione**. Non sono orfane: si distinguono con `catalog.media_assets.property_id`, aggiunto lo stesso giorno |

#### Stato

Tutte e 15 nascono con `is_dry_run_only = true` e `is_active = true`, come le 44 precedenti. **I job
non sono stati scritti**, su richiesta esplicita: F5 e il suo job nasceranno anche *a vuoto*, perché
`catalog.agreement_reports` è appena stata creata e non contiene ancora nessun report.

### `privacy` — auto-riferimento

| `schema.table_name` | `field_names` | `reference_date_column` | Durata | `action` | `basis_source` | Nota |
|---|---|---|---|---|---|---|
| `privacy.compliance_audit_logs` | `{}` | `occurred_at` | 24 `months` | `delete_row` | `recommended_policy` | Il modulo applica a sé stesso il meccanismo che offre agli altri ([`docs/11-modulo-privacy.md` §6](../../../progettazione/11-modulo-privacy.md): "retention breve, 1–2 anni") |
| `privacy.data_subject_requests` | `{}` | `requested_at` | 10 `years` | `archive_cold` | `law` | È la **prova di conformità** (accountability, art. 5.2): non si minimizza a zero |

### Cosa **non** viene seminato, e perché

Non ogni regola del §3 di `80-retention.md` diventa una riga qui. Le omissioni sono deliberate:

| Regola | Perché non è una riga di policy |
|---|---|
| **Lista di soppressione marketing** (`notification.marketing_suppressions`) | Si conserva **a vita**: è la prova che quel contatto ha chiesto di non essere ricontattato. Una policy con durata sarebbe un errore, non una dimenticanza |
| **Sequenze e contatori** (`booking.number_sequences`, `fiskaly.receipt_sequences`, `fiscalprint.serial_numbers`) | Sono **stato corrente**, non storico: vivono quanto lo stabilimento |
| **Cataloghi master globali** (`catalog.countries`, `catalog.map_icons`, …) | Permanenti se servono al portale; quelli specifici di un solo stabilimento seguono la cessazione, non una durata |
| **Configurazione viva** (listini, mappa, settori, stagioni, servizi) | Nessuna retention a tempo: scade alla **cessazione dello stabilimento**, gestita dalla cascata (riga successiva) |
| **Cascata "stabilimento cessato"** (§5 di `80-retention.md`) | Non è una durata da una colonna data della tabella stessa, ma da un **evento su un'altra tabella** (fine contratto). La esegue il job `DeprovisionClosedProperty`, non questa tabella — vedi *Limiti noti* |
| **Revisione annuale di `is_blacklisted`** | Il titolare l'ha assegnata al **gestore dello stabilimento**: è un promemoria nel gestionale, non un job che cancella un flag |
| **TTL degli hold di checkout** (minuti) | È logica di dominio del Booking (`PurgeExpiredMapHolds`), non retention privacy: libera una postazione, non minimizza un dato personale |
| **Tabelle figlie con FK `ON DELETE CASCADE`** (`booking.line_additional_items`, `notification.inapp_message_reads`, `notification.marketing_contact_list_members`, righe di scontrino, …) | Seguono il padre per integrità referenziale. Una policy propria sarebbe ridondante e rischierebbe di cancellare il figlio **prima** del padre |
| **Log diagnostici applicativi (P1–P5)** | Vivono in CloudWatch, non in una tabella: 90 gg + lifecycle S3 ([`docs/15-logs.md` §2](../../../progettazione/15-logs.md)) |
| **Backup PITR / export logici** | Rotazione automatica dello storage ([`docs/40-backup.md`](../../../progettazione/40-backup.md)), non policy applicativa |

### Limiti noti emersi seminando i dati

Tre punti dove il modello dichiarativo **non arriva** e serve logica nel job (o una colonna che oggi non
esiste). Vanno detti, non nascosti dietro una riga di seed approssimativa:

1. **Non c'è una data di cessazione dello stabilimento.**
   [`catalog_properties`](../catalog/catalog_properties.md) ha `status` ma nessun `terminated_at`; la
   data reale vive in [`identity_account_subscriptions.ends_at`/`suspended_at`](../identity/identity_account_subscriptions.md),
   **in un altro schema**. La cascata del §5 (subito / +3 mesi / +12 mesi / +2 anni / +10 anni) non è
   quindi esprimibile come `reference_date_column` di una riga di policy: la esegue
   `DeprovisionClosedProperty` leggendo la subscription via interfaccia pubblica del modulo Identity.
   *Se* si volesse renderla dichiarativa, servirebbe un `terminated_at` denormalizzato su
   `catalog.properties` — decisione di disegno, non una svista del seed.
2. **`identity.access_users` non ha una data di revoca.** La riga di seed usa `deleted_at` (soft-delete
   = disabilitazione), che è l'approssimazione corretta *se* la revoca dell'accesso si materializza
   sempre come soft-delete. Se invece esisterà uno stato "attivo/sospeso" distinto dal soft-delete,
   servirà una colonna dedicata: altrimenti un account **sospeso ma non cancellato** non scadrebbe mai.
3. **`fiskaly.cash_registers` dovrebbe decorrere dall'ultimo documento che lo referenzia**, non dal
   proprio `deleted_at`. È una data **calcolata con una query sui figli**, che il modello
   "colonna data + durata" non esprime: il job deve fare `MAX(receipts.signed_at) WHERE cash_register_id
   = …` prima di cancellare. La riga di seed vale come promemoria e guard-rail, non come regola
   eseguibile alla lettera.

### Perché `action` fa parte della chiave naturale

Scoperto **scrivendo** il seed, non progettandolo. L'indice unico originale era
`(schema_name, table_name, field_names)`, cioè **una sola regola per target** — ma la politica validata
dal titolare è quasi sempre a **due fasi** sullo stesso dato:

| Target | Fase 1 | Fase 2 |
|---|---|---|
| `booking.reservations` | 📦 archivia oltre **N−2** | 🗑️ cancella l'archivio a **10 anni** |
| `payment.ledger_entries` | 📦 archivia a **24 mesi** | 🗑️ cancella a **10 anni** |
| `payment.pos_charges` | 📦 archivia a **24 mesi** | 🗑️ cancella a **10 anni** |
| `fiskaly.receipts` | 📦 archivia a **36 mesi** | 🗑️ cancella a **10 anni** |

Con l'indice originale la seconda riga di ogni coppia era **impossibile da inserire**: quattro delle
politiche più importanti — proprio quelle fiscali — non erano rappresentabili nella tabella che avrebbe
dovuto rappresentarle.

La correzione minima è includere `action` nella chiave: permette archiviazione + cancellazione sullo
stesso target, e continua a impedire ciò che deve restare impedito, cioè due regole **in conflitto fra
loro** (stessa azione, stesso target, durate diverse). L'alternativa — un modello a due colonne
`archive_after` / `delete_after` — direbbe la stessa cosa costringendo però ogni policy a essere a due
fasi anche quando non lo è (la gran parte).

### Forma del seeder — implementata

Il seed vive in una **migration EF Core**, `SeedRetentionPolicies`, come singola istruzione SQL. È un
**upsert**, non un insert:

```sql
INSERT INTO privacy.privacy_retention_policies
  (public_id, schema_name, table_name, field_names, reference_date_column,
   duration_value, duration_unit, action, basis_source, legal_basis)
VALUES
  ('01a0a783-…', 'booking', 'reservations', '{}', 'stay_to', 2, 'seasons', 'archive_cold',
   'recommended_policy',
   'Nessun obbligo di mantenere online la testata: resta consultabile per la stagione corrente e le
    due precedenti (rinnovi, confronti di ricavo, riassegnazione della stessa postazione), poi
    archiviazione su storage freddo con i dati identificativi pseudonimizzati.'),
  …  -- 44 righe
ON CONFLICT (schema_name, table_name, field_names, action) WHERE deleted_at IS NULL
DO UPDATE SET
  reference_date_column = EXCLUDED.reference_date_column,
  duration_value        = EXCLUDED.duration_value,
  duration_unit         = EXCLUDED.duration_unit,
  basis_source          = EXCLUDED.basis_source,
  legal_basis           = EXCLUDED.legal_basis,
  updated_at            = now()
WHERE (privacy_retention_policies.reference_date_column, …)
   IS DISTINCT FROM (EXCLUDED.reference_date_column, …);
```

Quattro scelte, e il motivo di ognuna:

1. **`is_dry_run_only` e `is_active` non sono nel `SET`.** Sono decisioni operative di un umano (il DPO
   che, letto il report di dry-run, abilita l'esecuzione reale). Un ri-seed che le riportasse al default
   riaccenderebbe in dry-run una policy appena messa in produzione, o riattiverebbe una regola sospesa in
   attesa di un parere legale. *Verificato*: spenta a mano una policy e ri-eseguito il seed, le due
   restano come le aveva lasciate l'umano.
2. **`WHERE … IS DISTINCT FROM`** sul `DO UPDATE`: senza, ogni ri-esecuzione riscriverebbe 44 righe
   identiche e sposterebbe `updated_at`, rendendo inutilizzabile "cosa è cambiato nella policy e quando".
   *Verificato*: il secondo giro non tocca `updated_at`.
3. **`public_id` è un UUIDv7 deterministico**, derivato per hash dalla chiave naturale della riga. La
   stessa policy ha lo stesso id pubblico in ogni ambiente — utile al supporto ("la policy `01a0a783-…`
   è ancora in dry-run in produzione?") e al seed, che non ha bisogno di una tabella di corrispondenza.
   Non è nel `SET`: una volta assegnato non cambia.
4. **SQL e non `migrationBuilder.InsertData`** (che pure è lo stile della casa, cfr.
   `SeedPricingCalculationMethods` in `Module.Catalog`): `InsertData` non sa fare `ON CONFLICT`, quindi
   fallirebbe su un database dove qualche policy esiste già, e non potrebbe esprimere i punti 1 e 2.
5. **`legal_basis` non cita questa scheda né chi ha deciso** (vedi la *Regola di redazione* del campo):
   nel database finisce solo la norma o la ragione operativa. Il collegamento fra le due cose è la
   tabella qui sopra, non una stringa dentro la riga.

Un test di generazione rifiuta il seed se una motivazione contiene un percorso di documento, un'estensione
`.md` o una menzione del decisore: è la stessa lista di termini vietati controllata sia sul file della
migration sia, a valle, sui dati letti dal database.

Il `Down` cancella **solo** le 44 righe seminate, riconosciute per `public_id`: una policy creata a mano
dal pannello SystemAdmin sopravvive al rollback.

**Nota sul perché è una migration e non un seeder applicativo** (come ipotizzato nella prima stesura di
questa scheda): `Boot` oggi registra **solo** `Module.Identity`, quindi un seeder invocato all'avvio
dell'host non girerebbe affatto. La migration invece è applicata dal meccanismo che costruisce davvero il
database in questa fase (`dotnet ef database update`). Quando `Boot` cablerà anche Privacy, l'SQL qui
sopra si sposta in un seeder applicativo **senza riscritture**: è già idempotente, ed è già SQL.
