# Uso offline del gestionale (voce Mappa)

> Requisito del titolare: *«la possibilità di utilizzare le funzioni base quando non c'è rete: fare
> prenotazioni, visualizzarle, mettere pagamenti. Direi le cose che si possono fare da mappa»* — con
> un'autonomia attesa di **circa 1 ora**.
>
> Questo documento definisce **cosa deve fare il backend .NET 10** per rendere sicuro il lavoro offline
> e la successiva risincronizzazione, e **il contratto che il frontend deve rispettare** per coordinarsi.
> Lo scope è la **voce Mappa** ([../analisi-pre/business-logic/stabilimento/01-mappa.md](../analisi-pre/business-logic/stabilimento/01-mappa.md)):
> visualizzare la mappa, creare/spostare/annullare prenotazioni, check‑in/out, registrare incassi
> manuali, date di assenza. **Fuori scope offline**: addebiti carta/Stripe e scontrino elettronico
> Fiskaly (restano operazioni online — vedi [../analisi-pre/business-logic/stabilimento/03-cassa.md](../analisi-pre/business-logic/stabilimento/03-cassa.md)).
>
> Complemento a: [pre/linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md)
> (§6 eventi + outbox, §7 shared), [pre/standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md)
> (Problem Details, catalogo `code`), [pre/identificatori-e-uuid.md](pre/identificatori-e-uuid.md)
> (UUIDv7 generato app‑side), [pre/autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md)
> (claim, access token breve + refresh), [20-jobs.md](20-jobs.md) (outbox/worker),
> [35-integrazioni-esterne.md §2–§3](35-integrazioni-esterne.md) (Idempotency‑Key, resilienza),
> [34-multitenant.md](34-multitenant.md) (isolamento tenant),
> [01-criticita-e-indicazioni-backend.md](01-criticita-e-indicazioni-backend.md) (§3.2 IDOR, §11.2
> endpoint `optionalAuth`, §11.9 importi decisi dal client).
>
> **Documento vivo**: da aggiornare quando le decisioni aperte (§10) vengono chiuse.

---

## 0. Principio guida

**Il client lavora su una copia (working set) di una giornata di uno stabilimento e accoda i comandi;
il server resta l'unica autorità.** Da qui tutto il resto:

| Principio | Conseguenza |
|---|---|
| L'offline è **"working set in cache + coda di comandi da rigiocare"**, non una replica del DB | Nessun motore di merge/CRDT, nessun database locale completo, nessun servizio di sync dedicato |
| Il **server rivalida tutto** al momento del replay | Il legacy si fida degli importi e dei permessi del client (§11.2, §11.9): l'offline **non** eredita questa fiducia |
| Il **replay usa gli endpoint REST normali**, un comando alla volta | Nessuna API di batch/bulk; ciò che rende sicuro il rigioco è `Idempotency-Key` + `If-Match` |
| Le operazioni verso sistemi esterni (Stripe, Fiskaly) **non vanno offline** | Offline "mettere pagamenti" = registrare un **incasso manuale/contante**; carta e scontrino restano online |
| Ogni entità creabile offline ha **ID generato dal client (UUIDv7)** | Il `bookingNumber` progressivo resta assegnato dal **server** al replay |
| Gli effetti collaterali (email, notifiche, refresh mappa) passano già per l'**outbox** | Una prenotazione riapplicata in ritardo li genera comunque, senza codice nuovo |

---

## 1. Scope: cosa funziona offline

Operazioni della voce Mappa e loro trattamento (numeri di sezione = [01-mappa.md](../analisi-pre/business-logic/stabilimento/01-mappa.md)):

| Operazione | Offline | Note |
|---|---|---|
| Vedere la mappa per data/fascia (§1) | **Lettura da cache** | Solo le date incluse nella finestra pre‑scaricata (§9, decisione C) |
| Aprire scheda prenotazione + azioni rapide (§3) | **Lettura da cache** | |
| Stampa mappa (§12) | **Solo client** | Nessuna chiamata |
| Creare prenotazione da cella libera (§2) | **Scrittura in coda** | Il grosso della complessità (§4.4, §4.6) |
| Spostare / drag&drop (§6) | **Scrittura in coda** | Conflitto sul posto di destinazione (§4.5) |
| Check‑in / check‑out (§4) | **Scrittura in coda** | Banale, nessun evento |
| Annulla / elimina (§5) | **Scrittura in coda** | Rimborso Stripe **differito** al rientro (§4.9), o bloccato (decisione D) |
| Aggiungi acconto/saldo = **incasso manuale** (§7) | **Scrittura in coda** | Questo è "mettere pagamenti"; nessun sistema esterno |
| Date di assenza (§10) | **Scrittura in coda** | Nessun evento |
| Hold temporaneo (§11) | **Solo UI locale** | Un hold offline non è visibile agli altri operatori; **non** viene sincronizzato |
| Invio riepilogo / email (§8) | **Accodato** | Parte via outbox al rientro; l'UI lo dichiara ("verrà inviata alla riconnessione") |
| Sharing (§9) | **Escluso dal primo rilascio** | Validazione date + email, bassa priorità (decisione aperta) |
| Link pagamento Stripe / cattura / rimborso (§7) | **Bloccato** offline | Richiede Stripe online; l'UI disabilita il pulsante |
| Scontrino elettronico Fiskaly (Cassa) | **Fuori scope** | Resta online |

**Regola UI**: tutto ciò che è "Bloccato" o "Fuori scope" va **disabilitato in modo esplicito** quando
il client è offline, con spiegazione ("richiede connessione"), non lasciato cliccabile e poi in errore.

---

## 2. Modello di sincronizzazione (vocabolario condiviso BE/FE)

Tre oggetti, un algoritmo.

- **Working set** — snapshot di **uno** stabilimento (dal claim del token) per una **finestra di date**,
  salvato in IndexedDB dal client. Contiene layout mappa, prenotazioni, pagamenti, anagrafica clienti
  della property, catalogo/listino e le **regole** necessarie a validare offline. Ogni entità porta un
  campo **`version`** e il working set porta un **`cursor`** di sincronizzazione.
- **Coda comandi (client outbox)** — lista **ordinata** delle mutazioni fatte offline. Ogni comando ha
  `clientCommandId` (UUIDv7), `type`, `payload`, `occurredAt` (orologio client corretto, §5 lato FE),
  `baseVersion` (per gli update → header `If-Match`), `dependsOn` (id di altri comandi), `status`.
- **Cursor** — token opaco monotòno emesso dal server che rappresenta "fin dove il client è allineato".
  Serve a chiedere solo il **delta** al rientro.

**Algoritmo di riconnessione** (dettaglio FE in §6):

1. Rinnova l'access token (refresh).
2. `GET …/changes?since={cursor}` → applica il delta alla cache (**il server vince** sui campi di sua
   competenza), ottiene il nuovo `cursor`. Se il cursor è troppo vecchio/scaduto → ricarica lo snapshot
   completo.
3. **Replay** della coda, un comando alla volta in ordine `occurredAt`, sugli endpoint REST normali con
   `Idempotency-Key` + `If-Match`. Su conflitto bloccante: ferma la **catena dipendente**, marca il
   comando `conflict`, prosegue con i comandi indipendenti.
4. Nuovo `GET …/changes` per riconciliare, svuota i comandi `applied`.
5. Presenta all'operatore la **lista conflitti** (posto perso, prezzo non valido, magazzino esaurito,
   permesso revocato) con le azioni per risolverli.

---

## 3. Fase 1 — Lettura offline: cosa fare in backend

Obiettivo: servire in modo efficiente tutto ciò che va messo in cache e permettere un refresh
incrementale. **Nessuna modifica al modello di scrittura.**

### 3.1 Endpoint snapshot del working set

```
GET /api/hub/map/workset?from=2026-08-01&to=2026-08-14
Authorization: Bearer <UserBearer>
→ 200
{
  "serverTime": "2026-08-01T09:12:33.481Z",
  "cursor": "eyJzZXEiOjkwNDh9",
  "window": { "from": "2026-08-01", "to": "2026-08-14" },
  "property": { "id": "…", "timezone": "Europe/Rome" },
  "layout":    { "sectors": [...], "rows": [...], "cells": [...], "icons": [...] },
  "bookings":  [ { "id":"…","version":7,"bookingNumber":"2026-0004512","status":1,
                   "paymentStatus":2,"isFiskalized":false,"consumers":[…],
                   "locations":[…],"startDate":"…","endDate":"…","amounts":{…} } ],
  "payments":  [ { "id":"…","version":2,"bookingId":"…","kind":"manual","amount":…,"method":"cash" } ],
  "customers": [ { "id":"…","version":1,"name":"…","email":"…","mobile":"…","isBlackList":false } ],
  "catalog":   { "packages":[…], "articles":[…], "services":[…], "priceRules":[…] },
  "rules":     { "cancellation":{…}, "warehouse":{…}, "paymentConfig":{…}, "bookingConstraints":{…} },
  "sharing":   [...], "absences": [...], "holds": [...], "seasonal": [...]
}
```

Requisiti:

- **Scoping rigido al tenant**: `propertyId` **sempre** dal claim `property_id` del token, **mai**
  da querystring/body. Se il client passa un `propertyId` diverso da quello del token → **403**.
  Chiude l'IDOR sistemico e la montatura `optionalAuth` degli endpoint mappa legacy
  ([01-criticita §3.2, §11.2](01-criticita-e-indicazioni-backend.md), [34-multitenant.md](34-multitenant.md)).
- **Un solo round‑trip** per il bootstrap: layout + prenotazioni sovrapposte all'intervallo + relativi
  `booking_consumer`/`booking_locations`/righe pagamento + clienti della property con `isBlackList` +
  catalogo attivo + **le regole che servono a validare offline** (regole di cancellazione, conteggi
  `warehouses`, configurazione pagamento, vincoli prenotazione).
- **`serverTime`** in ogni risposta (il client stima l'offset di orologio — §5 lato FE).
- **`ETag`** sull'intera risposta + **`version`** per singola entità (numero intero o rowversion). La
  `version` è la stessa che il client rimanda in `If-Match` negli update (§4.4).
- **Compressione** `br`/`gzip`; misurare il **caso peggiore** (stabilimento grande, alta stagione,
  finestra ampia) e decidere la finestra massima (decisione C).
- **Deleted**: le entità cancellate compaiono come tombstone (`deletedAt` valorizzato), coerente con il
  soft‑delete + Global Query Filter di [03-gestione-dati.md §3.5](03-gestione-dati.md).

### 3.2 Endpoint delta (refresh incrementale)

```
GET /api/hub/map/changes?since=eyJzZXEiOjkwNDh9
→ 200
{
  "serverTime": "…",
  "cursor": "eyJzZXEiOjkxMDR9",
  "changed": { "bookings":[…], "payments":[…], "customers":[…], "sharing":[…], "absences":[…] },
  "deleted": { "bookings":["id1","id2"], "payments":[…] },
  "windowStillValid": true
}
```

- **Meccanismo del cursore** (decisione F): colonna monotòna `sync_seq bigint` per‑property, valorizzata
  dal **percorso applicativo** a ogni insert/update delle entità sincronizzabili (non da trigger, per
  restare dentro il modulo), indicizzata; query `WHERE property_id = :p AND sync_seq > :cursor ORDER BY
  sync_seq`. In alternativa `updated_at timestamptz` + tie‑breaker sull'id, ma il `bigint` monotòno è
  più robusto (niente ambiguità su timestamp uguali, niente problemi di clock del DB).
- Se `since` è più vecchio della retention del feed di modifiche → **410 Gone** `sync.cursor_expired`;
  il client ricarica lo snapshot completo.
- `windowStillValid = false` quando la data corrente è uscita dalla finestra pre‑scaricata → il client
  deve richiedere una nuova finestra.

### 3.3 Endpoint di heartbeat

```
GET /api/hub/ping  → 200 { "serverTime": "…" }
```

Endpoint autenticato, minimale, senza accesso al DB (o quasi): il client lo usa per distinguere
"davvero offline" da "una singola chiamata fallita" (§6 lato FE). Non è `navigator.onLine`.

### 3.4 Autenticazione tollerante alla disconnessione

- Access token breve (15‑60 min, [pre/autenticazione-autorizzazione-dotnet.md §2](pre/autenticazione-autorizzazione-dotnet.md)) + **refresh token** con vita ≥ finestra offline massima + margine (decisione: es. 24h).
- Al rientro: il client rinnova prima di sincronizzare. Se il refresh è scaduto → sync bloccata,
  l'operatore rientra con login e **la coda resta in IndexedDB** (non si perde).
- Nessuna sessione "offline" a permessi estesi: offline si può fare **solo** ciò che il ruolo già
  permette; la verifica reale avviene comunque al replay (§4.8).

---

## 4. Fase 2 — Scrittura offline in coda: cosa fare in backend

Obiettivo: rendere **sicuro il replay** dei comandi accumulati (creazione/spostamento prenotazione,
check‑in/out, annullo/elimina, incasso manuale, date di assenza).

### 4.1 ID generati dal client (UUIDv7)

- Il **client genera l'`id` (UUIDv7)** per: prenotazione, righe `booking_consumer`, riga incasso
  (`booking_payments`/`transactions_payments`), **cliente nuovo** creato contestualmente. Coerente con
  [pre/identificatori-e-uuid.md §2](pre/identificatori-e-uuid.md) (ID assegnato nella factory
  dell'aggregato, `ValueGeneratedNever()`), semplicemente l'ID arriva nel body invece che dalla factory
  server.
- Il server **valida** che l'`id` ricevuto sia un UUIDv7 ben formato e non già presente (un replay con
  lo stesso id → gestito da idempotenza, §4.3, non da errore di chiave duplicata).
- Il **`bookingNumber` progressivo** (numero umano, usato anche per i line item Fiskaly e i log) **non**
  è generato offline: lo assegna il server al replay. La risposta del create lo restituisce; il client
  aggiorna il record locale. Finché non c'è, l'UI mostra **"numero in assegnazione"**.
- Vale lo stesso per il `bookingNumber` dei servizi/cassa se un giorno rientrasse nello scope.

### 4.2 Endpoint di scrittura: gli stessi di sempre

Il replay chiama gli endpoint REST già esistenti della Mappa, **uno per comando**:

| Comando offline | Endpoint (nuovo backend) | Metodo |
|---|---|---|
| Crea prenotazione | `/api/hub/bookings` | POST |
| Sposta / modifica prenotazione | `/api/hub/bookings/{id}` | PUT/PATCH |
| Check‑in | `/api/hub/bookings/{id}/check-in` | POST |
| Check‑out | `/api/hub/bookings/{id}/check-out` | POST |
| Annulla | `/api/hub/bookings/{id}/cancel` | POST |
| Elimina | `/api/hub/bookings/{id}` | DELETE |
| Registra incasso manuale | `/api/hub/bookings/{id}/payments` | POST |
| Date di assenza | `/api/hub/bookings/{id}/absences` | POST/DELETE |

Nessun endpoint "applica batch". Ciò che rende sicuro il rigioco sono i due meccanismi seguenti.

### 4.3 Idempotency‑Key su tutti gli endpoint mutating

- Header **`Idempotency-Key: <clientCommandId>`** obbligatorio su ogni POST/PUT/PATCH/DELETE della
  Mappa. Priorità assoluta su **creazione prenotazione** e **incasso** (registrare due volte un
  pagamento è il danno peggiore).
- Store `idempotency_keys`: `{ key, user_id, property_id, request_hash, response_status, response_body,
  created_at }`. Chiave unica su `(key)`. TTL ≥ finestra offline + finestra di retry (es. 7–30 giorni;
  retention in [80-retention.md](80-retention.md)).
- Comportamento:
  - Stessa `key` + stesso `request_hash` → **restituisce la risposta memorizzata** senza rieseguire
    (anche se era un 409/422: così il client non ritenta all'infinito).
  - Stessa `key` + `request_hash` diverso → **422** `idempotency.key_reuse_mismatch`.
  - `key` assente → **400** `idempotency.key_required` sugli endpoint che la esigono.
- Implementazione: filtro/middleware condiviso (BuildingBlocks.Infrastructure,
  [pre/linee-guida-implementative-dotnet.md §7](pre/linee-guida-implementative-dotnet.md)); lo store
  può stare su Postgres (una tabella) o Redis se già attivo ([16-redis.md](16-redis.md)) — l'astrazione
  è `IIdempotencyStore`, il modulo non la conosce. Generalizza l'`Idempotency-Key` oggi previsto solo
  verso Stripe ([35-integrazioni-esterne.md §3](35-integrazioni-esterne.md)).

### 4.4 Concorrenza ottimistica (update) — `If-Match`

- Sugli update di prenotazione il client invia **`If-Match: "<version>"`** (la `version` letta dallo
  snapshot/delta al momento in cui l'operatore ha fatto la modifica offline).
- Se la `version` corrente sul server è diversa → **409** `booking.version_conflict`, con nel corpo
  l'entità attuale e `currentVersion`. Il client mostra il diff e fa **rideterminare** l'azione
  all'operatore (§6 lato FE).
- EF Core: proprietà di concorrenza (`IsRowVersion()` / token `xmin` di Npgsql). La `version` esposta
  in API è derivata da quel token.

### 4.5 Vincolo DB anti‑doppia‑prenotazione sul posto

Difesa indipendente dal codice applicativo: anche se due comandi offline (due operatori, o operatore +
cliente online) prendono lo **stesso ombrellone** nello stesso periodo, il **DB rifiuta il secondo** in
modo deterministico.

```sql
-- vincolo di esclusione sulla sovrapposizione posto × periodo, per prenotazioni attive
ALTER TABLE booking_locations
  ADD CONSTRAINT no_location_overlap
  EXCLUDE USING gist (
    location_id WITH =,
    tstzrange(start_date, end_date, '[]') WITH &&
  ) WHERE (status = 'active' AND deleted_at IS NULL);
```

- Violazione → intercettata nell'Infrastructure e tradotta in **409** `booking.location_overlap`
  (riusa il codice già presente nell'esempio di [pre/standard-risposta-api-dotnet.md §3](pre/standard-risposta-api-dotnet.md)).
- Il comando "perdente" torna al client come **conflitto da riposizionare** (non un 500).
- La fascia (giornata/mattina/pomeriggio) entra nel range o in una colonna dedicata a seconda del
  modello dati definitivo; il principio non cambia.

### 4.6 Rivalidazione server‑side di TUTTE le regole al replay

Il legacy calcola e verifica lato client e il server si fida ([01-mappa §2.8, §13](../analisi-pre/business-logic/stabilimento/01-mappa.md),
[01-criticita §11.2, §11.9](01-criticita-e-indicazioni-backend.md)). Nel nuovo backend il server
**riesegue** tutte le regole, offline o no; il replay è solo il caso in cui è più probabile che una
regola sia nel frattempo cambiata:

| Regola | Esito se violata al replay |
|---|---|
| Blacklist cliente (`isBlackList` sulla property; deroga `allowBookingForBlacklistedCustomer`) | **422** `booking.customer_blacklisted` |
| Disponibilità magazzino (`checkItemsAvailability` sui soli nuovi inserimenti) | **422** `booking.insufficient_inventory` (`args`: articolo, richiesto, disponibile) |
| Spostamento di prenotazione **già scontrinata** | **422** `booking.fiscalized_locked` |
| Regole di cancellazione (finestra temporale, penale) su annulla/elimina | **422** `booking.cancellation_not_allowed` |
| Contatto registrato con la property (solo se `bookingBy` ≠ operatore) | **422** `booking.contact_not_registered` |
| **Validazione importo**: il server ricalcola il prezzo (`IPricingApi`, [pre/linee-guida-implementative-dotnet.md §6](pre/linee-guida-implementative-dotnet.md)) e lo confronta con quello inviato dal client | **422** `booking.price_mismatch` **oppure** accetta e segnala per revisione — **decisione A** |

- La validazione di input (campi obbligatori, `startDate` sui servizi, ecc.) resta **400** con
  `errors[]` (FluentValidation → `IExceptionHandler` globale).
- Tutti questi `code` sono nel catalogo condiviso (§8) e il frontend li traduce/gestisce.

### 4.7 Timestamp dell'azione offline (`occurred_at`)

Sulle entità **create offline** (prenotazioni, incassi, assenze) una sola colonna nuova:

- **`occurred_at timestamptz`** — quando l'operatore ha eseguito l'azione (dal comando; orologio client
  corretto). Il server la **valida**: non nel futuro oltre uno skew tollerato (es. 5 min), non più
  vecchia della finestra offline massima + margine → altrimenti **422** `sync.occurred_at_out_of_range`.
- Il momento in cui il server ha scritto la riga è già dato da **`created_at`** (ora di INSERT lato
  server, impostata da interceptor/trigger — [50-standard-di-codifica.md §3](50-standard-di-codifica.md)):
  non serve una colonna dedicata. Su una riga entrata offline `created_at` ≠ `occurred_at`; la
  differenza è il ritardo di sincronizzazione.
- Le notifiche/email che citano "quando" e l'ordinamento cronologico (prenotazioni entrate offline vs
  online nel frattempo) usano **`occurred_at`**; l'audit usa entrambe.

Per le entità **modificate offline** non serve nulla di nuovo sulla riga: `updated_at` = ora del replay;
l'accoppiata "quando l'ha fatto l'operatore" ↔ "quando è stata applicata" vive sulla **riga di audit
del comando rigiocato** (§4.11), non sulla prenotazione.

### 4.8 Ri‑autorizzazione per comando al replay

Ogni comando rigiocato passa dalla pipeline di autorizzazione normale, valutata sul token **corrente**
(rinnovato al rientro):

- Ruolo/permesso/scoping‑stabilimento non più validi → **403** `auth.forbidden_on_replay`.
- Il client **scarta** il comando e avvisa l'operatore (non è un errore ritentabile).
- Copre il caso "l'operatore è stato disabilitato o spostato di stabilimento mentre era offline".

### 4.9 Rimborso Stripe su annullo offline

- Il comando "annulla" persiste la cancellazione nella transazione del modulo Booking ed emette
  l'evento `BookingCancelled`.
- Il modulo Payment, in sottoscrizione, **accoda il rimborso via outbox** ([20-jobs.md](20-jobs.md));
  il rimborso Stripe parte quando la connettività è tornata. **Mai in linea** durante il replay.
- Se **decisione D** = bloccare l'annullo offline di prenotazioni pagate online: il server risponde
  **422** `booking.cancel_blocked_paid_online` e il frontend disabilita l'azione offline a monte.

### 4.10 Effetti collaterali via outbox (già a favore)

Email (annullamento, riepilogo, sharing), notifiche in‑app, preparazione line item Fiskaly, refresh
mappa via socket: sono già gestiti come effetti asincroni post‑commit
([pre/linee-guida-implementative-dotnet.md §6](pre/linee-guida-implementative-dotnet.md),
[20-jobs.md](20-jobs.md)). Una prenotazione riapplicata in ritardo li produce comunque. **Unica
verifica**: i template che stampano una data devono usare `occurred_at`, non `DateTime.Now`.

### 4.11 Audit del replay

Log strutturato ([15-logs.md](15-logs.md), con `traceId` di correlazione) di ogni comando rigiocato:
`clientCommandId`, `type`, `occurredAt`, `appliedAt` (ora del replay), esito (`applied` /
`conflict:<code>` / `rejected:<code>`), id + `version` dell'entità risultante. È la traccia per il
supporto e per spiegare all'operatore cosa è successo alla sua coda; `appliedAt − occurredAt` misura il
ritardo di sincronizzazione. Retention breve (1–2 anni, [80-retention.md](80-retention.md)).

### 4.12 Test di integrazione richiesti

- Replay **duplicato** dello stesso `Idempotency-Key` → **una sola** scrittura, stessa risposta.
- Due create offline sullo **stesso posto/periodo** → uno **409** `booking.location_overlap`, l'altro ok.
- Update con `If-Match` **stale** → **409** `booking.version_conflict` con `currentVersion`.
- Create con importo diverso dal ricalcolo server → **422** `booking.price_mismatch` (o flag, per
  decisione A).
- Magazzino esaurito tra offline e replay → **422** `booking.insufficient_inventory`.
- Spostamento di prenotazione scontrinata → **422** `booking.fiscalized_locked`.
- Token con ruolo revocato → **403** `auth.forbidden_on_replay`, comando scartato.
- Incasso manuale rigiocato due volte → **un solo** movimento.
- `occurred_at` nel futuro / troppo vecchio → **422** `sync.occurred_at_out_of_range`.
- `since` scaduto → **410** `sync.cursor_expired`, il client ricarica lo snapshot.

---

## 5. Contratto lato frontend — cosa deve fare per coordinarsi

Il frontend è React ([front-modern-pre](../../../../../front-modern-pre)). Serve un livello offline (Service
Worker + IndexedDB) che rispetti quanto segue.

### 5.1 Cosa mettere in cache (working set)

- Tutto il payload di `GET /api/hub/map/workset` (§3.1): layout, prenotazioni, pagamenti, clienti della
  property, catalogo, regole, sharing/assenze/hold/stagionali.
- **Finestra di date**: oggi + N giorni (decisione C, proposta 7–14). Offline si lavora **solo** su
  quelle date; l'UI deve impedire di navigare fuori finestra quando è offline.
- **Solo lo stabilimento corrente** (quello del token). Al cambio stabilimento: nuovo snapshot, la
  cache precedente si può scartare.
- Persistenza in **IndexedDB** (non `localStorage`). Il Service Worker cache‑a l'app‑shell.

### 5.2 Refresh quando è online

- All'avvio, al rientro dal background, al cambio stabilimento, al cambio finestra date → `workset`
  completo se non c'è cursor valido, altrimenti `changes?since=cursor`.
- A regime (più operatori sulla stessa mappa) → polling `changes?since=cursor` ogni X secondi/minuti
  (o via socket se disponibile). Applica il delta: **il server vince** sui campi di sua competenza; i
  record con un comando `pending` locale mantengono l'**overlay ottimistico** finché il comando non è
  `applied` o `conflict`.
- `windowStillValid=false` o **410** → ricarica `workset`.

### 5.3 Rilevamento connettività

- **Non** fidarsi solo di `navigator.onLine`. Stato "offline" = **N chiamate consecutive** fallite/andate
  in timeout, oppure `ping` fallito. Stato "online" = `GET /api/hub/ping` ok **e** `changes` ok.
- Mostrare sempre un **banner persistente** con lo stato e da quanto tempo ("Offline da 14 min").

### 5.4 Generazione degli ID

- Generare **UUIDv7** lato client per: `id` prenotazione, righe consumer, `id` incasso, `id` cliente
  nuovo. Usare una libreria UUIDv7 (o l'API nativa quando disponibile).
- **Mai** inventare il `bookingNumber`: mostrare "numero in assegnazione"; dopo il replay, leggere il
  `bookingNumber` dalla risposta del server e aggiornare il record locale (e ogni riferimento in UI).

### 5.5 Formato del comando in coda (client outbox)

```jsonc
{
  "clientCommandId": "018f...-7...-...",   // UUIDv7; è anche l'Idempotency-Key
  "type": "booking.create",                // booking.create | booking.move | booking.checkIn |
                                           // booking.cancel | booking.delete | payment.addManual | absence.set ...
  "endpoint": "/api/hub/bookings",
  "method": "POST",
  "payload": { /* corpo della richiesta, con gli id UUIDv7 già dentro */ },
  "occurredAt": "2026-08-01T13:47:05.123Z",// clientNow + offset server (§5.7)
  "baseVersion": 7,                        // solo per update → header If-Match; assente per i create
  "dependsOn": ["<clientCommandId>", ...], // es. un incasso dipende dal create della sua prenotazione
  "status": "pending",                     // pending | sent | applied | conflict | rejected
  "attempts": 0,
  "lastError": null
}
```

- **`Idempotency-Key` = `clientCommandId`**, stabile per tutta la vita del comando (anche fra retry).
- L'ordine della coda è per **`occurredAt`**.

### 5.6 Algoritmo di riconnessione (dettaglio)

1. **Refresh token**. Se scade → ferma la sync, chiedi login, **conserva la coda**.
2. **Pull delta** `changes?since=cursor` → aggiorna la cache (server vince; preserva gli overlay dei
   comandi `pending`). Se **410** → ricarica `workset`.
3. **Replay** in ordine `occurredAt`, un comando alla volta:
   - Salta i comandi il cui `dependsOn` contiene un comando `conflict`/`rejected` → marcali
     `rejected` a cascata (localmente, senza inviarli).
   - Invia con header `Idempotency-Key` e, se `baseVersion` presente, `If-Match: "<baseVersion>"`.
   - **2xx** → `applied`; salva dalla risposta la mappatura id, il `bookingNumber`, la nuova `version`.
   - **409 / 422** → `conflict`; **non ritentare**; ferma la catena dipendente; accumula per la
     schermata conflitti.
   - **403** `auth.forbidden_on_replay` → `rejected`; scarta; avvisa.
   - **5xx / rete** → resta `pending`; retry con backoff esponenziale + jitter (max tentativi, poi
     lascia `pending` e segnala).
4. **Pull delta** di nuovo → riconcilia; rimuovi i comandi `applied` dalla coda.
5. Mostra la **schermata conflitti** con azioni: *riposiziona* (posto perso), *ricalcola/aggiorna*
   (prezzo o versione), *scarta*.

### 5.7 Orologio

- Il **server time è autoritativo**. A ogni risposta leggi `serverTime`, mantieni una stima di
  `offset = serverTime - clientNow`.
- `occurredAt` di ogni comando = `clientNow + offset` al momento dell'azione.
- Aspettati un **422** `sync.occurred_at_out_of_range` se l'orologio del dispositivo è troppo sballato:
  mostra un messaggio esplicito ("l'orologio del dispositivo non è corretto").

### 5.8 Versioni e `If-Match`

- Ogni entità in cache porta `version`. Quando l'operatore modifica offline una prenotazione, il
  comando cattura la `version` corrente come `baseVersion`.
- Su **409** `booking.version_conflict` il server restituisce l'entità aggiornata + `currentVersion`:
  mostra il **confronto** e fai riconfermare l'azione (che genera un nuovo comando con la nuova
  `baseVersion`).

### 5.9 Codici di errore da gestire

Vedi il catalogo in §8. Per ognuno il FE deve avere: traduzione utente (via `code` + `args`), e una
**azione** (ritenta / apri conflitto / scarta / mostra messaggio). Prevedere il fallback "errore
sconosciuto" per `code` non ancora mappati ([pre/standard-risposta-api-dotnet.md §4](pre/standard-risposta-api-dotnet.md)).

### 5.10 Cosa disabilitare in UI quando offline

- Invio link di pagamento Stripe, cattura, rimborso (a meno che decisione D non consenta l'annullo con
  rimborso differito — in quel caso l'annullo è permesso ma segnala "rimborso alla riconnessione").
- Emissione scontrino Fiskaly.
- Sharing (se escluso dal primo rilascio).
- Navigazione fuori dalla finestra di date in cache.
- Cambio stabilimento (se non c'è snapshot dell'altro).
- Invio email immediato → consentito ma etichettato "verrà inviata alla riconnessione".

### 5.11 Cosa mostrare all'operatore

- Banner offline persistente + durata.
- **Badge di stato per riga**: *in coda* / *sincronizzata* / *in conflitto*.
- "Numero in assegnazione" finché il `bookingNumber` non arriva dal server.
- Contatore comandi in coda; avviso se la coda cresce troppo o lo storage IndexedDB è vicino al limite.
- Schermata di **risoluzione conflitti** al termine della sync.

### 5.12 Limiti e sicurezza

- **Finestra offline massima** allineata a: TTL degli `idempotency_keys` e tolleranza `occurred_at`
  (proposta 24h). Oltre, l'UI avvisa che il lavoro offline non è più garantito.
- Il working set contiene **PII** (contatti cliente, `isBlackList`, note): risiede in IndexedDB sul
  dispositivo. **Cancellare** la cache e la coda al logout; purga per TTL; nessun dato carta/fiscale è
  in cache (non ce n'è). Valutare cifratura a riposo se il dispositivo è condiviso (decisione aperta) —
  vedi [11-modulo-privacy.md](11-modulo-privacy.md), [80-retention.md](80-retention.md).
- Gestire il caso **quota IndexedDB piena** / storage non disponibile (finestra privata): degradare a
  "solo online" con messaggio, non fallire in modo opaco.

---

## 6. Riepilogo endpoint

| Endpoint | Metodo | Fase | Scopo |
|---|---|---|---|
| `/api/hub/map/workset?from&to` | GET | 1 | Snapshot completo del working set (una property, una finestra) |
| `/api/hub/map/changes?since={cursor}` | GET | 1 | Delta dal cursore + tombstone dei cancellati |
| `/api/hub/ping` | GET | 1 | Heartbeat per il rilevamento connettività |
| `/api/identity/auth/refresh` | POST | 1 | Rinnovo access token al rientro |
| `/api/hub/bookings` | POST | 2 | Crea prenotazione (id UUIDv7 dal client, `Idempotency-Key`) |
| `/api/hub/bookings/{id}` | PUT/PATCH | 2 | Sposta/modifica (`If-Match`, `Idempotency-Key`) |
| `/api/hub/bookings/{id}` | DELETE | 2 | Elimina (`If-Match`, `Idempotency-Key`) |
| `/api/hub/bookings/{id}/check-in` · `/check-out` | POST | 2 | Check‑in / check‑out (`Idempotency-Key`) |
| `/api/hub/bookings/{id}/cancel` | POST | 2 | Annulla (rimborso Stripe via outbox, `Idempotency-Key`) |
| `/api/hub/bookings/{id}/payments` | POST | 2 | Registra incasso manuale (`Idempotency-Key`) |
| `/api/hub/bookings/{id}/absences` | POST/DELETE | 2 | Date di assenza (`Idempotency-Key`) |

Header trasversali sugli endpoint di scrittura: `Idempotency-Key` (obbligatorio), `If-Match`
(obbligatorio sugli update), corpo con `occurredAt`.

---

## 7. Modifiche al modello dati (sintesi)

| Dove | Modifica | Motivo |
|---|---|---|
| Entità sincronizzabili (bookings, payments, customers, …) | Colonna **`version`** (rowversion/`xmin`) | Concorrenza ottimistica `If-Match` (§4.4) |
| Entità sincronizzabili | Colonna **`sync_seq bigint`** monotòna per‑property, indicizzata | Cursore del delta (§3.2) |
| bookings, payments, absences | Colonna **`occurred_at timestamptz`** (l'ora di scrittura server è già `created_at`) | Timestamp dell'azione offline (§4.7) |
| `booking_locations` | Vincolo **`EXCLUDE`** posto × periodo su prenotazioni attive | Anti‑doppia‑prenotazione a livello DB (§4.5) |
| Nuova tabella `idempotency_keys` | `key, user_id, property_id, request_hash, response_status, response_body, created_at` | Idempotenza del replay (§4.3) |
| PK delle entità create offline | Confermare **UUIDv7** (già standard) e accettare l'id nel body | ID generati dal client (§4.1) |
| Feed modifiche | Retention del `sync_seq`/change‑log ≥ finestra offline massima | Evitare `410` frequenti (§3.2) |

---

## 8. Catalogo dei codici di errore (offline/replay)

Formato Problem Details di [pre/standard-risposta-api-dotnet.md §3](pre/standard-risposta-api-dotnet.md).
`code` stabile, `args` per l'interpolazione, `traceId` per il log.

| `code` | HTTP | Quando | Azione frontend |
|---|---|---|---|
| `idempotency.key_required` | 400 | Manca l'header su endpoint che lo esige | Bug FE: aggiungere la chiave, non ritentare a vuoto |
| `idempotency.key_reuse_mismatch` | 422 | Stessa chiave, payload diverso | Bug FE: nuova chiave per un nuovo comando |
| `booking.version_conflict` | 409 | `If-Match` stale | Mostra diff con `currentVersion`, fai riconfermare |
| `booking.location_overlap` | 409 | Posto già occupato nel periodo | Conflitto: riposiziona la prenotazione |
| `booking.customer_blacklisted` | 422 | Cliente in blacklist senza deroga | Messaggio; eventuale deroga esplicita |
| `booking.insufficient_inventory` | 422 | Magazzino esaurito tra offline e replay | Conflitto: rimuovi/riduci articoli |
| `booking.fiscalized_locked` | 422 | Spostamento di prenotazione scontrinata | Messaggio: annullare prima lo scontrino (online) |
| `booking.cancellation_not_allowed` | 422 | Fuori finestra / regole di cancellazione | Messaggio con la regola applicata |
| `booking.contact_not_registered` | 422 | Regola "solo contatti registrati" | Messaggio |
| `booking.price_mismatch` | 422 | Importo client ≠ ricalcolo server (decisione A) | Conflitto: ricalcola e riconferma |
| `booking.cancel_blocked_paid_online` | 422 | Annullo offline di prenotazione pagata online (decisione D) | Disabilita offline; rimanda al rientro |
| `auth.forbidden_on_replay` | 403 | Permesso/scoping revocato mentre offline | Scarta il comando; avvisa l'operatore |
| `sync.occurred_at_out_of_range` | 422 | Orologio dispositivo sballato | Messaggio "orologio non corretto" |
| `sync.cursor_expired` | 410 | `since` più vecchio della retention del feed | Ricarica lo snapshot completo |
| `validation.*` | 400 | Input non valido (`errors[]`) | Evidenzia i campi |
| `server.unexpected` | 500 | Non gestito | Resta `pending`, retry con backoff |

---

## 9. Decisioni aperte (chiudere prima di implementare)

- **A — Motore di prezzo.** Oggi il prezzo è calcolato **interamente nel client** e il server non
  ricalcola ([01-mappa §2.8, §13](../analisi-pre/business-logic/stabilimento/01-mappa.md),
  [_motore-di-prezzo-e-sconti.md](../analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md)).
  Se il refactoring sposta il calcolo lato server (fix di sicurezza), per creare prenotazioni da mappa
  **offline** serve una copia del motore di prezzo eseguibile nel client, e il server lo **rivalida** al
  replay (`booking.price_mismatch`, può rifiutare). Se resta client‑side, offline è più semplice ma il
  server deve comunque validare l'importo. **È il bivio architetturale principale.**
- **B — Perimetro pagamenti offline.** Confermare che carta (Stripe) e scontrino (Fiskaly) restano
  online‑only e che offline "mettere pagamenti" = **solo incasso manuale/contante**. Se sì, non serve
  altro lavoro su Stripe/Fiskaly per l'offline.
- **C — Ampiezza del working set.** Numero di giorni della finestra (proposta 7–14) e conferma "solo
  stabilimento corrente". Determina il peso del payload e cosa è possibile fare offline.
- **D — Annullo offline di prenotazioni pagate online.** Consentito con rimborso Stripe **differito**
  via outbox, oppure **bloccato** (`booking.cancel_blocked_paid_online`).
- **E — Politica di conflitto sul posto.** Confermare "**primo commit vince**; il perdente torna in
  coda conflitti da riposizionare a mano" (nessuna risoluzione automatica).
- **F — Meccanismo del cursore.** `sync_seq bigint` monotòno per‑property (consigliato) vs `updated_at`
  + tie‑breaker; retention del feed di modifiche.
- **G — Cifratura del working set in IndexedDB** su dispositivi condivisi (sì/no; come gestire la
  chiave).

---

## 10. Checklist implementativa

**Backend — Fase 1**

- [ ] `GET /api/hub/map/workset` con scoping `propertyId` dal claim, `ETag`, `version` per entità,
      `serverTime`, compressione.
- [ ] Colonna `sync_seq` monotòna per‑property sulle entità sincronizzabili + `GET …/map/changes`.
- [ ] Tombstone dei cancellati nel delta (soft‑delete già presente, §3.5 di 03-gestione-dati).
- [ ] `GET /api/hub/ping`.
- [ ] Refresh token con vita ≥ finestra offline + margine.
- [ ] Chiusura degli endpoint mappa legacy montati `optionalAuth`.

**Backend — Fase 2**

- [ ] Accettare l'`id` UUIDv7 nel body dei create; `bookingNumber` assegnato dal server al replay.
- [ ] Middleware `Idempotency-Key` + tabella `idempotency_keys` + `IIdempotencyStore`.
- [ ] `If-Match` / colonna `version` (rowversion `xmin`) sugli update di prenotazione.
- [ ] Vincolo `EXCLUDE` posto × periodo su `booking_locations`.
- [ ] Rivalidazione server‑side: blacklist, magazzino, scontrinata, regole di cancellazione, contatto
      registrato, importo (`IPricingApi`).
- [ ] Colonna `occurred_at` sulle entità create offline + validazione range (`created_at` resta l'ora di scrittura server).
- [ ] Ri‑autorizzazione per comando al replay (`auth.forbidden_on_replay`).
- [ ] Annullo → evento `BookingCancelled` → rimborso Stripe **via outbox** (o blocco per decisione D).
- [ ] Verifica che i template email/notifica usino `occurred_at`.
- [ ] Audit strutturato del replay con `traceId`.
- [ ] Suite di test di integrazione (§4.12).
- [ ] Catalogo `code` (§8) nel catalogo condiviso di [pre/standard-risposta-api-dotnet.md §9](pre/standard-risposta-api-dotnet.md).

**Frontend**

- [ ] Service Worker (app‑shell) + IndexedDB (working set + coda comandi).
- [ ] Bootstrap da `workset`, refresh incrementale da `changes`, ricarica su `410` / fuori finestra.
- [ ] Rilevamento connettività basato su `ping` + N fallimenti, banner persistente.
- [ ] Generazione UUIDv7 lato client; "numero in assegnazione" fino al replay.
- [ ] Coda comandi nel formato §5.5, `Idempotency-Key = clientCommandId`, ordinamento per `occurredAt`,
      `dependsOn`.
- [ ] Algoritmo di riconnessione §5.6 (pull delta → replay → riconcilia → schermata conflitti).
- [ ] Stima offset di orologio da `serverTime`; gestione `sync.occurred_at_out_of_range`.
- [ ] Gestione `If-Match` / `booking.version_conflict` con confronto e riconferma.
- [ ] Mappatura di tutti i `code` di §8 → messaggio + azione; fallback "errore sconosciuto".
- [ ] Disabilitazione esplicita offline delle operazioni fuori scope (§5.10).
- [ ] Badge di stato per riga, contatore coda, schermata risoluzione conflitti.
- [ ] Pulizia cache+coda al logout; gestione quota IndexedDB piena; TTL finestra offline.

---

## 11. Ordine di adozione

1. **Fase 1 (lettura offline)** — solo frontend + endpoint `workset`/`changes`/`ping`. Copre già "il
   gestore continua a vedere la mappa e le prenotazioni" durante un blackout di rete.
2. **Fase 2 (scrittura offline)** — le operazioni senza sistemi esterni (prenotazioni, spostamenti,
   check‑in/out, annullo, incasso manuale, assenze). Richiede tutte le voci "Backend — Fase 2".
3. **Rifiniture** — sharing offline, eventuale cifratura del working set, tuning della finestra e delle
   retention in base ai dati d'uso reali.
