# Read-model JSON di configurazione — disegno

> **Stato: APPROVATO** (committente, 2026-09-09) — Opzione A, 6 snapshot + `search_card`, aggiornamento
> uniforme, `assetRef` + `ETag` + `payload_schema`, disponibilità in C1. Dettaglio in §11. Schede
> tabella: [`catalog_config_snapshots`](catalog/catalog_config_snapshots.md),
> [`catalog_search_bathhouses`](catalog/catalog_search_bathhouses.md). Decisione architetturale recepita anche in
> [`docs/03-gestione-dati.md` §7](../../progettazione/03-gestione-dati.md).

---

## 1. Obiettivo

Il frontend (gestionale e sito pubblico) chiede spesso la **configurazione generale** di uno stabilimento
in un colpo solo (bootstrap del gestionale, apertura scheda pubblica, apertura flusso di prenotazione).
Ricostruirla ogni volta da ~20 tabelle normalizzate con molte `JOIN` è costoso e ripetitivo.

**Doppio approccio:**

- le **tabelle relazionali** (`analisi-post/db/catalog/`) restano la **fonte di verità** — editing,
  integrità, query mirate, calcoli autoritativi;
- in aggiunta, un **payload `jsonb` derivato**, aggiornato **ad ogni modifica** delle tabelle sorgente e
  restituito così com'è quando serve "tutta la configurazione", senza ricalcolo.

È il pattern **materialized read-model / proiezione CQRS**, con storage `jsonb`.

**Invariante.** Il `jsonb` è **sempre ricostruibile** dalle tabelle: `rebuild(property_id, kind)` è una
funzione pura delle tabelle sorgente. Non è mai l'unica copia di un dato. In caso di dubbio/divergenza,
si rigenera dalle tabelle.

---

## 2. Storage — Opzione A (decisa)

Tabella dedicata unica **[`catalog_config_snapshots`](catalog/catalog_config_snapshots.md)**: una riga per
`(property_id, snapshot_kind)`.

```
catalog.config_snapshots
- id              bigint IDENTITY  PK
- property_id     <tipo tenant>    FK -> catalog_properties   (D-01)
- snapshot_kind   enum catalog.config_snapshot_kind
                  -- property_management_config | beach_map | public_showcase
                  --  | price_catalog | services_catalog | agreement_catalog
- payload         jsonb            NOT NULL
- payload_schema  smallint         NOT NULL   -- versione della FORMA del JSON (vedi §7)
- source_hash     bytea            NOT NULL   -- hash degli input; salta i rebuild no-op
- content_hash    bytea            NOT NULL   -- hash del payload; ETag verso il client (§7)
- built_at        timestamptz      NOT NULL
- stale_since     timestamptz      NULL       -- NULL = aggiornato; altrimenti DA QUANDO e' da rigenerare
- created_at / updated_at
UNIQUE (property_id, snapshot_kind)
INDEX (stale_since) WHERE stale_since IS NOT NULL
CHECK (stale_since IS NULL OR stale_since >= built_at)
```

Il read-model di **ricerca pubblica** (`search_card`) **non** sta qui: è una riga per stabilimento
interrogata con filtri e ordinamenti, quindi ha una tabella propria con colonne indicizzabili →
**[`catalog_search_bathhouses`](catalog/catalog_search_bathhouses.md)**.

Perché una tabella dedicata e non una colonna `jsonb` sulle tabelle radice: non gonfia `catalog_properties`
(tenuta apposta snella, caricata quasi ad ogni richiesta); aggiungere un `snapshot_kind` = un valore enum
+ una funzione di build, zero DDL; invalidazione/rebuild/riconciliazione uniformi; versionamento e
`stale_since`/`source_hash` per rebuild efficienti.

### Evoluzione futura (solo se serve)

Se il profiling mostra che la lettura del `jsonb` da Postgres è sul percorso critico (improbabile con
~175 stabilimenti e payload di decine di KB), si mette **Redis** come cache calda davanti:
`config:{property_id}:{kind}` con TTL + invalidazione su evento; read path = Redis → DB → rebuild.
`catalog_config_snapshots` resta lo store durevole e la fonte del rebuild. Nessun cambio di schema.

---

## 3. URL negli snapshot: riferimento scomposto, non URL completo

Gli snapshot **non** contengono URL completi (re-introdurrebbe il prefisso hardcoded che
[`catalog_media_assets`](catalog/catalog_media_assets.md) elimina). Ogni file è un **`assetRef`**:

```jsonc
{ "origin": "cloudfront", "path": "images/configure/2yja5bgukmxic259.png" }
// se origin == "external": path è l'URL completo, invariato
```

La **mappa `origin → base URL`** è servita da un endpoint dedicato e cacheabile, es.
`GET /config/media-origins` → `{ "cloudfront": "https://d26kxhigi9o4u2.cloudfront.net",
"s3_us_east_2": "https://summerbooking.s3.us-east-2.amazonaws.com", "external": "" }` (stessa mappa
`Storage:Origins` usata server-side, [`03-gestione-dati.md` §1.2](../../progettazione/03-gestione-dati.md)). Ha un
proprio `ETag` e cambia rarissimamente.

Il client compone: `origin === "external" ? path : origins[origin] + "/" + path`.

**Vantaggi:** un cambio di bucket/CDN è una modifica di configurazione (l'endpoint origins), **non**
richiede di rigenerare gli snapshot; il payload è più compatto (prefisso non ripetuto); il `content_hash`
dello snapshot resta stabile a un cambio CDN, quindi gli `ETag` dei client non si invalidano inutilmente.

*(Alternativa scartata: comporre gli URL nell'API al momento della risposta — renderebbe la risposta
diversa dal blob salvato, complicando `content_hash`/ETag e legandoli alla versione della config CDN.)*

---

## 4. Gli snapshot: contenuto, quando li chiama l'utente, esempi

Chiavi camelCase per il frontend; importi = numero JSON a 2 decimali
([§3.7](../../progettazione/03-gestione-dati.md)); file = `assetRef` (§3).

### 4.1 `property_management_config` — bootstrap del **gestionale**

**Cosa contiene:** l'ossatura di configurazione dello stabilimento, in forma di **riepiloghi** (i
dettagli stanno negli altri snapshot): impostazioni canale online postazioni + servizi, opzioni di
pagamento, stagione e periodi tariffari, settori, config Revenue Management, stato abbonamento
(proiezione da C5), e i **conteggi** delle entità (quanti pacchetti, sconti, servizi, articoli di
magazzino) per popolare i badge dei menù.
**Interazione utente:** il gestore fa login e apre il pannello gestionale → il client chiama
`GET /manage/properties/{id}/config` (una volta, con `If-None-Match`) → riceve questo blob e con esso
abilita/disabilita le voci di menù, precompila i form delle Impostazioni, mostra lo stato "vendite online
sospese", ecc. Senza questo: 10–15 chiamate separate + join al bootstrap.
**Non contiene:** il listino completo (`price_catalog`), il catalogo servizi dettagliato
(`services_catalog`), la mappa (`beach_map`), segreti/credenziali.

### 4.2 `beach_map` — rendering della **mappa spiaggia** (gestionale e sito pubblico)

**Cosa contiene:** il layout della griglia (righe/colonne, colore, template come `assetRef`, icone per
tipo di elemento nei due stati), i settori, la legenda, e **tutte le postazioni** (`id` + `publicId`,
`label` = il "numero", `type`, `sectorId`, posizione `gridRow`/`gridColumn`, `isArea`/`areaSeatCount`,
`isBookable`). **Senza** occupazione: quella si sovrappone a runtime per la data scelta (§6).
**Interazione utente:**
- *Gestionale*: il gestore apre "Mappa" o "Configurazione mappa" → `GET /manage/properties/{id}/beach-map`
  → il client disegna la griglia con le postazioni; per la vista operativa aggiunge una seconda chiamata
  leggera "occupazione del {data}".
- *Sito pubblico*: il cliente in fase di prenotazione sceglie "seleziona sulla mappa" →
  `GET /public/bathhouses/{slug}/beach-map` → stesso blob (filtrato ai campi pubblici) + overlay
  disponibilità per la data richiesta.
**Dimensione:** ~30 KB per uno stabilimento piccolo, ~150–250 KB per uno con ~300 postazioni (le icone
sono sul layout, **non** ripetute per postazione).

### 4.3 `public_showcase` — la **scheda pubblica** di uno stabilimento

**Cosa contiene** (tutto ciò che serve alla pagina di dettaglio pubblica, in **una** risposta):

| Gruppo | Campi |
|---|---|
| Identità | `publicId`, `slug`, `name` |
| Indirizzo | `street`, `city`, `cap`, `province`, `region`, `country` (nome risolto da `catalog_countries` per la lingua) |
| Geo | `lat`, `lng` |
| Contatti | `phone`, `email`, `website`, `facebook`, `instagram`, `whatsapp` |
| Orari | `openHours.from` / `.to` |
| Contenuti | `descriptionHtml`, `regulationHtml` (sanificati) |
| Politiche | `cancellationPolicy`, `petsPolicy` (enum) |
| Media | `photos[]` (assetRef), `mainPhoto` (assetRef), `menuPdf`, `priceListPdf`, `tourVirtual` (assetRef o `null`) |
| Servizi | `amenities[]` = `{code, name (lingua), icon (assetRef), filter}` — risolti da `catalog_bathhouse_amenities` + `catalog_service_amenities` |
| Prezzo | `minPrice` (prezzo minimo corrente, materializzato) |
| Prenotabilità pubblica | `onlineBooking` = `{isEnabled, mode:["map","location"], allowsHalfDay, requiresManualConfirmation}` (solo i campi che il consumer deve vedere) |
| Social proof | `likeCount` (da C5, per `bathhouse`) |

**Interazione utente:** il consumer tocca un risultato di ricerca, oppure apre un link
`summerbooking.it/{slug}` (o l'app apre una vetrina) → `GET /public/bathhouses/{slug}` → il client
renderizza la pagina di dettaglio (hero + galleria, chip servizi, orari, mini-mappa, politica di
cancellazione, CTA "Prenota"). Una richiesta, zero join, zero N+1.
**Non contiene:** listini dettagliati (arrivano con `price_catalog` quando l'utente clicca "Prenota"),
la griglia mappa completa (`beach_map`), campi gestionali/amministrativi.

**Esempio (abbreviato):**
```jsonc
{
  "schema": 1, "publicId": "018f…", "slug": "bagno-medusa", "name": "Bagno Medusa",
  "address": { "street":"Viale Roma 34","city":"Rimini","cap":"47921","province":"RN","region":"Emilia-Romagna","country":"Italia" },
  "geo": { "lat": 44.0503, "lng": 12.5608 },
  "contacts": { "phone":"+39 0541 123456","email":"info@bagnomedusa.it","website":"https://bagnomedusa.it","whatsapp":"+39 333 1234567" },
  "openHours": { "from": "08:00", "to": "20:00" },
  "descriptionHtml": "<p>Stabilimento familiare…</p>",
  "cancellationPolicy": "free_within_24h", "petsPolicy": "not_allowed",
  "mainPhoto": { "origin":"cloudfront", "path":"images/bathouse/decui1nwkmk9ib9k.jpeg" },
  "photos": [ {"origin":"cloudfront","path":"images/bathouse/2yjaja1dknhqmxl5.jpeg"}, {"origin":"cloudfront","path":"images/bathouse/3br3w805slif0a1m8.jpeg"} ],
  "tourVirtual": null, "menuPdf": null, "priceListPdf": null,
  "amenities": [
    { "code":"wifi", "name":"Wi-Fi", "icon":{"origin":"cloudfront","path":"icons/wifi.svg"}, "filter":"popular" },
    { "code":"bar",  "name":"Bar",   "icon":{"origin":"cloudfront","path":"icons/bar.svg"},  "filter":"popular" }
  ],
  "minPrice": 25.00,
  "onlineBooking": { "isEnabled": true, "mode": ["map","location"], "allowsHalfDay": true, "requiresManualConfirmation": false },
  "likeCount": 42,
  "builtAt": "2026-09-09T10:00:00Z"
}
```

### 4.4 `price_catalog` — listino **proprio** dello stabilimento (flusso di prenotazione)

**Cosa contiene:** **solo** il listino dello stabilimento stesso — `catalog_pricing_*`: periodi
tariffari; per ogni pacchetto — nome, `kind`, `maxPeople`, metodo di calcolo, flag
(online/default/area/festivi), articoli inclusi, prezzi per settore, prezzi per periodo (giornaliero/
weekend/½ giornata/7-14 gg/mensile), override per data + date festive, finestre di prezzo ridotto, e le
sole righe di prezzo suggerito **già applicate** dal gestore; articoli aggiuntivi con eventuale vincolo a
pacchetti; la parte pubblica dei voucher (codice, tipo, valore, validità, settori).
**Non contiene** i listini/voucher delle **convenzioni** (`catalog_agreement_*`) → snapshot separato
`agreement_catalog` (§4.7).
**Interazione utente:** il consumer clicca "Prenota" sulla scheda o sulla mappa → `GET
/public/bathhouses/{slug}/price-catalog` → il client mostra i pacchetti e fa una **stima ottimistica**
del totale mentre l'utente compone la prenotazione. Il **totale autoritativo** resta `POST
/bookings/quote` server-side ([`03-gestione-dati.md` §3.7](../../progettazione/03-gestione-dati.md)) — questo blob
serve solo a evitare un round-trip a ogni click.

### 4.5 `services_catalog` — catalogo servizi (gestionale e prenotazione)

**Cosa contiene:** i servizi venduti divisi in `bar` / `normal` / `hourly`; per ognuno nome, prezzo,
`isPurchasable`, icona (assetRef); per gli `hourly` anche `hourFrom`/`hourTo`, i giorni attivi, e le
fasce orarie con prezzo dedicato.
**Interazione utente:** il gestore apre "Servizi" nel gestionale, oppure il consumer apre la sezione
"Aggiungi servizi" nel flusso di prenotazione → `GET /…/services-catalog` → lista pronta da renderizzare,
niente join sulle 3 liste + icone.

### 4.6 `search_card` — riga per la **ricerca pubblica** (tabella [`catalog_search_bathhouses`](catalog/catalog_search_bathhouses.md))

**Cosa contiene:** i campi necessari a (a) **filtrare/ordinare** — colonne vere indicizzate: `name`,
`city`, `region`, `min_price numeric`, `amenity_ids smallint[]` (GIN), `location geography` (GiST),
`like_count`, `is_published`; e (b) **renderizzare la card** — `payload jsonb` con `slug`, `mainPhoto`
(assetRef), `shortDescription`, `openHours`, il nome amenities principali.
**Interazione utente:** il consumer sulla home cerca per città/regione/geo e applica filtri (servizi,
prezzo, "aperto ora", distanza) → `GET /public/search?city=Rimini&amenities=wifi,bar&near=44.05,12.56&radius=5000&maxPrice=40&sort=price`
→ il backend interroga **solo** `catalog_search_bathhouses` (GIN su `amenity_ids`, GiST su `location`,
B-tree su `min_price`), pagina i risultati, restituisce un array di card. Tocca una card → poi si carica
`public_showcase`.
**Perché una tabella a sé:** è interrogata con `WHERE`/`ORDER BY`/`LIMIT` su più dimensioni, non letta
per chiave `(property_id, kind)`. Nel legacy questa ricerca faceva `findAll` senza paginazione + una
query `getLowestPrice` **per ogni** risultato + filtro servizi in JavaScript — questa tabella la rende
una singola query indicizzata.

**Esempio (una card):**
```jsonc
{
  "bathhousePublicId": "018f…", "propertyId": "…",
  "name": "Bagno Medusa", "city": "Rimini", "region": "Emilia-Romagna", "slug": "bagno-medusa",
  "geo": { "lat": 44.0503, "lng": 12.5608 },
  "minPrice": 25.00, "amenityCodes": ["wifi","bar","shower","parking"], "likeCount": 42,
  "isPublished": true,
  "card": {
    "mainPhoto": { "origin":"cloudfront", "path":"images/bathouse/decui1nwkmk9ib9k.jpeg" },
    "shortDescription": "Stabilimento familiare a 50 m dal centro",
    "openHours": { "from":"08:00","to":"20:00" },
    "topAmenities": ["Wi-Fi","Bar","Doccia calda"]
  },
  "builtAt": "2026-09-09T10:00:00Z"
}
```

### 4.7 `agreement_catalog` — listini/voucher delle **convenzioni** (Hotel)

**Cosa contiene:** le strutture partner della property (`catalog_agreement_partners`) e, per ciascuna, i
suoi listini/voucher (`catalog_agreement_price_lists` + figlie): nome, `list_type`, `minBookingDays`,
`maxVoucherQuantity`, articoli inclusi, prezzi per settore e per periodo (`period_label` testuale).
**Perché separato da `price_catalog`:** il flusso Convenzioni è **opt-in** (si attiva solo quando la
prenotazione ha un `hotelId`); è usato da **~17 stabilimenti** e uno di essi ha **47 partner** — includere
tutti i loro listini nel `price_catalog` caricato da *ogni* consumer lo gonfierebbe inutilmente.
**Interazione utente:** nel flusso di prenotazione il cliente sceglie "Ho una convenzione con un hotel" e
seleziona la struttura → `GET /public/bathhouses/{slug}/agreement-catalog` (una volta) → il client mostra
i partner e i relativi voucher/listini; la parte transazionale (quale voucher è stato consumato) è **C1**.
Per gli stabilimenti **senza** convenzioni la riga snapshot **non esiste**.

---

## 5. Strategia di aggiornamento — **uniforme per tutti**

Un solo modello per i 6 `snapshot_kind` + `catalog_search_bathhouses`:

1. **Durante il giorno — incrementale, event-driven, rebuild SINCRONO.** Ogni comando che scrive una
   tabella sorgente emette un evento di dominio; un handler mappa evento → `{property_id, kind[]}` e
   **rigenera l'intero payload nella stessa transazione** del comando. Payload piccoli (decine di KB),
   ~175 stabilimenti, poche scritture/giorno per stabilimento → costo di pochi ms, **zero finestra di
   incoerenza**, nessuna coda da operare.
2. **Di notte — ricostruzione totale.** Un job ricostruisce **tutti** gli snapshot da zero (`rebuild`
   per ogni `(property, kind)`), confronta `content_hash`, aggiorna solo quelli cambiati. Rete di
   sicurezza per: eventi persi, bug nella mappa evento→kind, modifiche fatte **fuori** dai comandi
   standard (fix diretti a DB, import, migrazioni).
3. **Manuale — utility di rigenerazione** (vedi §8): per supporto/debug/post-migrazione.

**Escape hatch (non ora).** Se un `kind` supera una soglia di durata del rebuild sincrono (candidato:
`price_catalog` per stabilimenti con centinaia di pacchetti × periodi; soglia indicativa **250 ms p95**),
**quel solo `kind`** passa ad **asincrono** (evento → coda → job idempotente; `stale_since` valorizzata
nel frattempo; il read serve il payload marcato stantio con un header di avviso, e sa **da quanto** lo è). Gli altri restano
sincroni. Si progetta tutto sincrono e si misura.

`source_hash` (hash deterministico degli input) permette di **saltare** il rebuild quando un evento non
tocca davvero lo snapshot → il rebuild sincrono che non cambia nulla costa una `SELECT` e un confronto.

Rispondendo alla domanda: **sì**, "aggiornare durante il giorno + ricostruire la notte" è esattamente il
modello. **Uniforme** è preferibile a "caso per caso" perché è più semplice da operare e da ragionare;
la differenziazione (async per `price_catalog`) è un'ottimizzazione da introdurre **solo se** una misura
la richiede.

---

## 6. Disponibilità / occupazione — **non** in questi snapshot (analisi preliminare, disegno in C1)

La "disponibilità per data" (postazioni libere il giorno D, slot orari residui, quantità di magazzino
non consumate) è **transazionale**: cambia a ogni prenotazione/cancellazione/assenza. Metterla negli
snapshot li invaliderebbe di continuo. Vive in **C1**, dove sta `resource_consumptions`
([`03-gestione-dati.md` §3.8](../../progettazione/03-gestione-dati.md)).

**Cosa C2 già fornisce** perché C1 possa costruire una vista disponibilità efficiente: l'anagrafica
**stabile** delle risorse — `catalog_map_spots` (con `public_id`, `label`, `type`, `sectorId`),
`catalog_service_sale_items` (+ slot), `catalog_inventory_items` (+ `total_quantity`) — e lo snapshot
`beach_map` (griglia + postazioni senza stato). Il client compone: `beach_map` (statico, cache lunga) +
un overlay leggero "stato del {data}".

**Analisi preliminare delle opzioni per l'overlay (da decidere in C1):**
- **(i)** vista/proiezione `booking_availability_daily(property_id, date, resource_type, total, occupied)`
  — conteggi grossolani per la ricerca "disponibile il {data}" e per i badge; aggiornata a eventi di
  prenotazione.
- **(ii)** proiezione fine `spot_availability(spot_id, date, state)` — stato per singola postazione/giorno
  per il rendering mappa; più righe ma query diretta O(postazioni della property).
- **(iii)** calcolo on-the-fly da `resource_consumptions` con indice `(property_id, consumption_date,
  resource_id)` + cache a breve TTL per (property, data).
- Tipicamente: **(iii)** come base + **(i)** materializzata per i filtri di ricerca; **(ii)** solo se il
  rendering mappa a volume lo richiede.

**Raccomandazione:** **non** progettarlo ora. È input di disegno per C1; qui si annota il requisito e i
punti di aggancio. → [D-32](_domande-committente.md).

---

## 7. `payload_schema` ed `ETag`/`If-None-Match`

### `payload_schema` — versione della **forma** del JSON

Intero sulla riga snapshot. Dice "questo payload segue la struttura N". Cambia **solo quando cambia il
codice di serializzazione** in modo non retrocompatibile (aggiunta/rinomina/ristrutturazione di campi) —
**non** quando cambiano i dati.

A cosa serve:
- Il client fa `switch (payload.schema)` invece di controlli difensivi sparsi (`if (x !== undefined)`).
- Durante un rilascio graduale il client può supportare N **e** N-1: righe con schema diverso coesistono
  finché il **rebuild di massa** (job) non ha portato tutte le righe alla forma nuova.
- Un cambio di schema è un'operazione tracciata: `payload_schema` da 1 a 2 ⇒ job che rigenera tutto.

Distinto da `content_hash` (cambia a ogni modifica dei **dati**); `payload_schema` cambia a ogni modifica
del **contratto**.

### `ETag` / `If-None-Match` — richiesta HTTP condizionale (standard)

Meccanismo per **non ritrasferire** una risorsa che il client ha già.

1. Nella risposta il server mette l'header `ETag: "…"` = impronta della versione corrente della risorsa.
   Qui è il `content_hash` dello snapshot (es. i primi 16 hex dello SHA-256 del payload) — **già
   precalcolato** sulla riga, quindi generare l'ETag costa zero.
2. Il client memorizza `payload` + `ETag`.
3. Alla richiesta successiva il client manda `If-None-Match: "…"` con l'ETag che ha.
4. Il server confronta:
   - **invariato** → risponde **`304 Not Modified`** senza corpo → il client riusa la copia in cache.
     Traffico ≈ poche centinaia di byte.
   - **cambiato** → **`200 OK`** con il nuovo payload e il nuovo `ETag`.

Effetto concreto: il gestionale che al bootstrap ricarica `property_management_config` di norma riceve un
`304` invece di 20–80 KB; la mappa (`beach_map`, fino a 250 KB) si ritrasferisce solo dopo una modifica
reale. Poiché lo snapshot ha già `content_hash`, non serve calcolare hash a ogni richiesta.

*(Con l'approccio `assetRef` + endpoint origins separato (§3), il payload restituito è identico al blob
salvato → `ETag == content_hash` puro, nessuna complicazione. L'endpoint `/config/media-origins` ha un
suo `ETag` indipendente.)*

---

## 8. Utility di rigenerazione manuale *(da realizzare — annotato in [`docs/note.md`](../../progettazione/note.md))*

CLI + endpoint amministrativo per rigenerare gli snapshot su richiesta:

- `snapshots rebuild --property <id> --kind <kind>` — una riga.
- `snapshots rebuild --property <id> --all` — tutti i kind di uno stabilimento.
- `snapshots rebuild --all [--kind <kind>]` — ricostruzione totale (o di un kind) su tutti gli
  stabilimenti (stessa logica del job notturno, eseguibile a mano).
- `snapshots verify [--property <id>]` — rigenera in memoria e confronta `content_hash` con la riga
  salvata, **senza** scrivere; elenca le divergenze (diagnostica).

Usi: dopo la migrazione iniziale; dopo un fix diretto a DB; dopo un cambio di `payload_schema`; su
segnalazione di dato "vecchio" nel frontend; in CI/staging per validare il rebuild.

---

## 9. Cosa **non** mettere negli snapshot

- **Occupazione / disponibilità per data** → C1 (§6).
- **Prezzo suggerito non ancora "applicato"** (`catalog_pricing_suggested_prices` con `applied_mode =
  none`) → output di un job, non configurazione; nello snapshot solo le righe applicate dal gestore.
- **Dati per-utente / per-viewer** (preferiti, carrello, sessione).
- **Segreti / credenziali** (Stripe, SMTP, Fiskaly).
- **Il totale autoritativo di una prenotazione** → sempre `POST /bookings/quote`.
- **URL completi** → sempre `assetRef` (§3).

---

## 10. Rischi e mitigazioni

| Rischio | Mitigazione |
|---|---|
| Divergenza blob ↔ tabelle | rebuild-from-source (mai patch incrementali); job notturno di riconciliazione; `content_hash` di controllo; `snapshots verify` |
| Blob troppo grande | snapshot **separati** per responsabilità; `beach_map` con 300 postazioni ≈ 150–250 KB (icone sul layout, non per postazione); soglia di allarme ~1 MB |
| Evento perso → blob stantio | `stale_since` + job notturno; l'`ETag` mostra comunque al client l'ultima versione nota |
| Cambio schema payload | `payload_schema` + rebuild di massa versionato; client tollera N e N-1 |
| Costo del rebuild sincrono | payload piccoli + poche scritture/giorno; `source_hash` salta i no-op; escape hatch async per singolo kind |
| Chi edita vede "vecchio" | l'**editor** legge dalle tabelle normalizzate (o update ottimistico), non dallo snapshot; lo snapshot serve i percorsi "carica tutta la config" |
| Cambio CDN | `assetRef` + endpoint origins → modifica di configurazione, nessun rebuild |

---

## 11. Decisioni (committente, 2026-09-09)

- **[D-28]** ✅ Storage → **Opzione A** (`catalog_config_snapshots` + `catalog_search_bathhouses`). Opzione
  D (A + Redis) solo se il profiling lo richiede.
- **[D-29]** ✅ Elenco dei 6 snapshot + `search_card` e i contenuti di §4: **confermati**; eventuali
  aggiunte di campo a `property_management_config` in corso d'opera.
- **[D-30]** ✅ Strategia di aggiornamento → **uniforme**: rebuild sincrono a evento durante il giorno +
  ricostruzione totale notturna + utility manuale (§5, §8). Escape hatch async per singolo `kind` sopra
  soglia (~250 ms p95).
- **[D-31]** ✅ `assetRef` + endpoint `/config/media-origins`; `ETag`/`If-None-Match`; `payload_schema` con
  supporto N/N-1: **approvati**.
- **[D-32]** ✅ Vista disponibilità/occupazione: **disegno di dettaglio in C1** (§6); qui si annotano
  requisito e punti di aggancio.
- **[D-33]** 🟨 Snapshot anche per **C5** (es. `account_config`): da valutare quando si tocca C5.
