# Criticità del backend legacy (Node.js) e indicazioni per il nuovo backend .NET

> Analisi del codice sorgente esistente (`back-pre`, con incrocio su `front-pre` dove rilevante) in vista della
> riscrittura completa in **.NET 10**. Documento prodotto leggendo il codice sorgente reale — ogni finding
> riporta un riferimento `file:riga` verificabile. Complementare a [00-struttura.md](00-struttura.md) e ai
> documenti già presenti in [docs/pre/](pre/) (in particolare
> [refactoring-analisi-A-vs-B.md](pre/refactoring-analisi-A-vs-B.md), che fotografa la codebase a livello
> aggregato — 54 controller, ~98.000 LOC, 458 endpoint, 633 `JSON.parse`, 281 query SQL raw — e da cui questo
> documento riparte per scendere nel dettaglio puntuale).
>
> Tutti i percorsi sono relativi alla root del repository. Dove non specificato, i file sono sotto `back-pre/`.

---

## Indice

1. [Executive summary](#1-executive-summary)
2. [Segreti compromessi — azione immediata indipendente dalla riscrittura](#2-segreti-compromessi--azione-immediata-indipendente-dalla-riscrittura)
3. [Pattern trasversali (bug/anti-pattern che attraversano tutto il codebase)](#3-pattern-trasversali)
4. [Dominio C1 — Prenotazioni (Booking)](#4-dominio-c1--prenotazioni-booking)
5. [Dominio C2 — Catalogo & configurazione](#5-dominio-c2--catalogo--configurazione)
6. [Dominio C3/C4 — Pagamenti & Fiscale (Stripe, Fiskaly)](#6-dominio-c3c4--pagamenti--fiscale-stripe-fiskaly)
7. [Dominio C5 — Autenticazione, autorizzazione, utenti, middleware, config](#7-dominio-c5--autenticazione-autorizzazione-utenti-middleware-config)
8. [Dominio C6 — Comunicazione & marketing](#8-dominio-c6--comunicazione--marketing)
9. [Dominio C7 — Switchboard/Centralino](#9-dominio-c7--switchboardcentralino)
10. [INFRA — Upload, cron, configurazione, integrazioni esterne](#10-infra--upload-cron-configurazione-integrazioni-esterne)
11. [Frontend: codice/logica da spostare o duplicare nel backend](#11-frontend-codicelogica-da-spostare-o-duplicare-nel-backend)
12. [Problemi di modellazione dati trasversali](#12-problemi-di-modellazione-dati-trasversali)
13. [Checklist di sintesi per la riscrittura .NET](#13-checklist-di-sintesi-per-la-riscrittura-net)

---

## 1. Executive summary

Il codice legacy non ha una singola vulnerabilità isolata da correggere: ha **pattern sistemici** ripetuti
decine di volte in controller diversi, scritti da mani diverse in ~4,5 anni, senza test, senza code review
strutturata, senza un livello di autorizzazione centralizzato. I 12 punti seguenti sono quelli che più
condizionano il disegno del nuovo backend .NET (non semplici bug da correggere uno a uno, ma ragioni per cui
certe scelte architetturali — descritte nei documenti in `docs/pre/` — sono necessarie e non opzionali).

1. **Segreti in chiaro nel repository**, non solo in `.env`: chiave Stripe **live**, credenziali RDS di
   produzione, chiavi AWS S3 con bucket `public-read-write`, credenziali SMTP, Twilio, WhatsApp, Fiskaly, e
   persino una password SMTP + chiave AES **nel bundle del frontend pubblico**. Il pannello systemadmin
   `front-admin/` (in repo dal 2026-09-09) aggiunge altre 3 chiavi hardcoded nel proprio bundle (chiave
   AES-GCM di decifratura risposte, chiave `crypto-js` `"SB_USER_DATA"`, chiave `cryptr`
   `"myTotalySecretKey"` — vedi §11.12 e [17-secrets.md §4](17-secrets.md)). Vanno ruotate a prescindere
   dalla riscrittura (§2).
2. **JWT secret debole e hardcoded** (`"summer-booking-secret-key"`, stringa letterale nel sorgente
   versionato), token validi **365 giorni**, nessuna revoca, nessuna restrizione di algoritmo in `jwt.verify`.
   Chiunque abbia mai avuto accesso al repository può forgiare un token valido per qualunque utente.
3. **SQL injection sistemica**: decine di query costruite per concatenazione di stringa invece che con
   parametri/`replacements`, ripetute identiche in quasi ogni controller (booking, catalogo, pagamenti, auth,
   comunicazione). Il progetto conosce il pattern corretto (lo usa altrove nello stesso file) — è un problema
   di disciplina, non di strumenti: nella riscrittura va reso strutturalmente impossibile (EF Core/query
   parametrizzate come unica via, mai SQL raw a mano libera).
4. **IDOR sistemico**: quasi nessun controller verifica che la risorsa (prenotazione, pagamento, utente,
   listino) appartenga davvero al chiamante autenticato prima di leggerla/modificarla/cancellarla. Il
   `propertyId`/`userId`/`bookingId` è quasi sempre preso dal body e usato as-is nel `WHERE`.
5. **`authJwt.optionalAuth` usato su endpoint che toccano soldi e dati personali**: questo middleware non
   blocca mai la richiesta (token assente o invalido → procede comunque con `req.user = null`). È applicato a
   refund, capture pagamento, trasferimenti Stripe, cancellazione account, dettaglio pagamenti prenotazione.
6. **Importi finanziari calcolati lato client e mai ricalcolati dal server**: prezzo pacchetto, sconti/voucher,
   importo del `PaymentIntent` Stripe, importo residuo da pagare. Il backend salva e addebita ciò che il
   client dichiara.
7. **Reset/cambio password e login by-email completamente forgeable**: chiave di cifratura statica per il
   link di reset, endpoint di cambio password e "invia token" privi di autenticazione, OTP a 4 cifre generato
   con `Math.random()` senza rate limit.
8. **Middleware di sicurezza presenti nel codice ma sistematicamente commentati**: rate limiting, whitelist di
   dominio, verifica app mobile, controllo di ruolo su intere famiglie di endpoint (es. tutte le rotte hotel).
   È un falso senso di sicurezza per chi legge il codice: le protezioni "ci sono" ma non sono mai attive.
9. **Dati di pagamento (PAN, CVC) salvati in chiaro e restituiti dalle API** in almeno un flusso
   (`onlinereservation.controller.js`) — violazione diretta PCI-DSS.
10. **Webhook in ingresso non verificati**: Telnyx e WhatsApp/Meta non validano la firma del provider (Stripe
    invece la valida correttamente, ma con idempotenza solo parziale sul flusso "payment link").
11. **Race condition diffuse**: `.map(async ...)` usati come se fossero sincroni (nessun `Promise.all`/`await`),
    chiavi duplicate in oggetti `where`/letterali che si sovrascrivono silenziosamente, check-poi-scrivi non
    transazionali su stock di magazzino, contatore utilizzo voucher, numerazione scontrini fiscali. Effetto:
    overbooking, doppio utilizzo sconti, doppia emissione di scontrino fiscale.
12. **God object e duplicazione**: `booking.controller.js` (12.775 righe) e `booking.helper.js` (3.908 righe)
    concentrano gran parte della logica di dominio critica; pattern CRUD quasi identici copiati in 6+
    controller del catalogo, spesso con bug divergenti introdotti in una copia e non nelle altre.

Il frontend, dal canto suo (§11), non è un consumer passivo: contiene un **motore di pricing completo**
(tariffe stagionali, weekend, sconti a scaglioni, voucher) il cui risultato il backend accetta senza
ricalcolo, oltre a controlli di ruolo che esistono solo come styling condizionale dei componenti React.

Questi 12 punti, più che i singoli finding, sono la ragione per cui la riscrittura in .NET (§13) deve
imporre **per costruzione** ciò che nel codice Node è oggi opzionale: autorizzazione secure-by-default,
query parametrizzate come unica via, ricalcolo server-side di ogni importo, transazioni atomiche sulle
operazioni multi-step, secret management esterno al codice.

---

## 2. Segreti compromessi — azione immediata indipendente dalla riscrittura

Questi valori sono presenti in chiaro nel codice sorgente/filesystem del progetto **oggi**, a prescindere da
quando partirà la riscrittura. Vanno considerati compromessi e ruotati appena possibile, perché chiunque
abbia mai avuto accesso al repository (collaboratori passati, backup, fork, un clone su una macchina non più
controllata) li possiede:

| Segreto | Dove | Nota |
|---|---|---|
| Stripe **secret key live** (`sk_live_...`) | `back-pre/.env:10` | Accesso completo all'account Stripe di produzione (charge, refund, payout) |
| Stripe webhook secret | `back-pre/.env:95` | Permette di forgiare eventi webhook se non altrimenti verificato |
| Credenziali DB di produzione (host, user, password) | `back-pre/.env:11-14` **e duplicate** in `back-pre/app/config/db.config.js:18-27` | Il secondo punto è ancora più grave: è hardcoded nel codice, non in `.env` |
| Twilio Account SID + Auth Token | `back-pre/.env:21-22` | |
| SendGrid API key | `back-pre/.env:61` | |
| SMTP produzione (user/password) | `back-pre/.env:24-31` | |
| Telnyx API key | `back-pre/.env:64` | |
| WhatsApp (Meta) access token, app secret, webhook verify token | `back-pre/.env:74-83` | |
| AWS S3 access key + secret key | `back-pre/.env:89-91` **e duplicate** in `back-pre/app/global/globalConstants.js:6-7` e `back-pre/app/utils/audioUpload.js:17-18` | Il bucket è inoltre configurato con ACL `public-read-write` di default (`globalConstants.js:15`) |
| Fiskaly API key/secret (ambiente test, ma pattern replicabile su live) | `back-pre/.env:48-49` | |
| Chiave JWT applicativa | `back-pre/app/config/auth.config.js:2` (`"summer-booking-secret-key"`) | Firma **tutti** i token di sessione dell'app |
| Chiave di cifratura del link di reset password | `back-pre/app/controllers/password.controller.js:5` (`"myTotalySecretKey"`) | |
| Password SMTP + chiave AES applicativa | `front-pre/src/constants/envrionment.js:7,49-57` | **Nel bundle JavaScript pubblico**, non solo nel repository: chiunque apra i DevTools del sito le ottiene oggi, senza bisogno di accesso al codice sorgente |

**Indicazione per il nuovo backend**: nessun segreto in codice o in file versionati. Configurazione da
secret manager (AWS Secrets Manager / SSM Parameter Store, coerente con [infrastruttura-aws.md](../../../../../infra/infrastruttura-aws.md)),
mai raggiungibile dal client. Vedi anche §13.

---

## 3. Pattern trasversali

Raggruppati qui perché attraversano più domini; le sezioni per dominio (§4-§10) rimandano a questi pattern
invece di ripeterne la spiegazione, e riportano solo le occorrenze specifiche.

### 3.1 SQL injection per concatenazione di stringhe in query raw

Il progetto usa sia Sequelize ORM sia `sequelize.query(...)` con SQL scritto a mano. Nel secondo caso, il
pattern corretto (`replacements: { param }`) convive, nello **stesso file e talvolta nella stessa funzione**,
con l'interpolazione diretta di valori provenienti da `req.body`/`req.query`/`req.params` nella stringa SQL.
Occorrenze verificate (lista non esaustiva, solo quelle lette direttamente nel codice):

- `booking.controller.js`: 616-622, 2472 (`Sequelize.literal` + `JSON_CONTAINS`), 2851-2866, 3777-3838,
  4316-4339, 4819-4855, 6027-6044 (parametrizzazione *parziale*: un parametro bindato, altri due no, nello
  stesso statement), 6105-6146, 6670-6678, 6842, 6855, 6869, 7229-7324, 7626-7632, 8056-8078, 9539-9547
  (nemmeno quoted, valore numerico nudo), 10224-10265, 11262-11266, 11530, 11679-11706
- `booking.helper.js`: 3541-3553, 3627-3631, 3645-3649 (il valore `location` proviene dal body della
  richiesta di prenotazione)
- `servicereservation.controller.js`: 603, 638, 1428-1431, 1763-1765, 1791-1796
- `property.controller.js`: 547-549, 611-613, 677-679, 712-714, 783-785, 817-819, 886-888, 914-916
- `discounts.controller.js`: 173-174, 209-210, 381-386
- `helpers/property.helper.js`: 129-130, 137-138
- `serviceFilters.controller.js`: 62-67 (campo testuale libero `hourly_service_name`)
- `cardTransaction.controller.js`: 132-137, 145-150, 162-168 (endpoint pubblico, solo `optionalAuth`)
- `user.controller.js`: 87-92, 272-278
- `stripe.controller.js`: 3700-3725
- `email.helper.js`: 104, 311, 357, 369, 1406, 1655, 1668, 1677
- `marketingAutomation.controller.js:190`, `marketingEmailCampaign.controller.js:241`,
  `marketingContactList.controller.js:291`
- `helpers/cron.helper.js`: 1392, 1483

**Rischio concreto**: ogni occorrenza è un vettore di lettura/alterazione dati cross-tenant (un cliente/gestore
di uno stabilimento può leggere o modificare dati di un altro), in molti casi su tabelle con dati personali
(`booking_consumer`) o di pagamento (`booking_payments`).

**Indicazione .NET**: EF Core con LINQ (parametrizzazione automatica) come via primaria; se serve SQL raw,
`FromSqlInterpolated`/parametri nominati **mai** concatenazione manuale. Vietare per policy di code review
(e idealmente con un analyzer Roslyn) la costruzione di stringhe SQL da input esterno.

### 3.2 IDOR — nessun controllo di ownership sulla risorsa

Pattern ricorrente: l'handler riceve un id (`bookingId`, `serviceReservationId`, `propertyId`, `userId`) dal
body/query, lo usa direttamente in un `WHERE`/`findOne`/`update`/`destroy`, senza mai confrontarlo con
l'identità o lo scoping (property/tenant) dell'utente autenticato. Occorrenze verificate:

- Booking: `deleteBooking` (booking.controller.js:2427-2455), `updateBooking` (2623-2656, riscrive persino
  `property_id`), `cancelPackageBooking1`/`cancelPackageBooking` (3692-3742), `myBookings` (4569-4581),
  `updateCustomerIcons` (11457-11498), `updateReservationCode` (12782-12792, aggravato dall'`await` mancante)
- Service/location reservation: `singleServiceReservationList` (servicereservation.controller.js:860-868),
  `deleteSingleServiceReservationList` (906-926), `cancelServiceBooking1`/`cancelServiceBooking` (1376-1421)
- Pagamenti: `getDetailsPaymentsByBookingNumber` (bookingPayments.controller.js + payment.helper.js:7-92,
  endpoint pubblico che espone PII e importi dato solo il `bookingNumber`)
- Catalogo: **l'intero dominio catalogo** (§5.1) — `pricelist`, `discounts`, `sectors`, `seasons`,
  `servicetypes`, `bathouse`, `confimaps`, `warehouse` si fidano tutti del `propertyId` nel body
- Auth/utenti: `edituser`/`updateuser` (user.routes.js:194-213), `deleteAccount` (protetto solo da
  `optionalAuth`)

**Rischio concreto**: un gestore autenticato di uno stabilimento può leggere, modificare o cancellare dati
(prenotazioni, listini, sconti, utenti) di un **altro** stabilimento semplicemente cambiando un id nel body
della richiesta — riproducibile con una singola chiamata Postman, non richiede alcuna competenza offensiva
avanzata.

**Indicazione .NET**: l'id della risorsa non deve mai bastare da solo. Ogni comando/query che opera su una
risorsa scoped-per-tenant deve verificare `resource.PropertyId == currentUser.PropertyId` (o equivalente),
idealmente centralizzato in un `IAuthorizationHandler`/policy basata su risorsa, non ripetuto (e dimenticato)
in ogni controller.

### 3.3 `authJwt.optionalAuth` su endpoint sensibili

`authJwt.js:310-331`: se il token è assente, `"null"`, `"undefined"` o non valido, il middleware imposta
`req.user = null` e chiama comunque `next()` — **non blocca mai** la richiesta. È montato su:

- `/api/sb/refund`, `/api/sb/capturePayment`, `/api/sb/updateCaptureCharge`, `/api/sb/TransferTOAdminAccount`,
  `/api/sb/send-link` (pagamenti Stripe reali)
- `/api/sb/booking`, `/api/sb/update/booking`, `/api/sb/booking/deleteBooking` (scrittura prenotazioni)
- `/api/sb/deleteAccount`
- `/api/sb/propertydetails`, `/api/sb/getPropertyWithBathHouseDetails` (qui con un commento nel codice che
  mostra esplicitamente la protezione originale `[verifyToken, isSystemAdmin]` sostituita da `optionalAuth`:
  `user.routes.js:604-622` — una regressione di sicurezza documentata dal codice stesso)
- `bookingPayments` (dettaglio pagamenti per `bookingNumber`)
- `cardTransaction` → `getMovementList` (oltretutto SQL injection, §3.1)

**Indicazione .NET**: secure-by-default (`FallbackPolicy` che richiede autenticazione), apertura **esplicita**
e mirata con `[AllowAnonymous]` solo dove realmente necessario (login, ricerca pubblica, webhook — questi
ultimi autenticati via firma, non via assenza di controllo). Vedi [autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md).

### 3.4 Importi finanziari non ricalcolati server-side

Il backend salva/usa numeri che arrivano dal client come fonte di verità, invece di ricalcolarli dal proprio
stato autorevole:

- `booking.controller.js:2634,2640` e `booking.helper.js:1483-1489,1629-1646`: `booking_payments`,
  `packageSelectedData`, `total_amount`/`paid_amount`/`pending_amount` salvati as-is da `req.body`
- `stripe.controller.js:690,1569,1975,2074,2898,2907,4402,4444`: `Math.round(req.body.amount * 100)` usato
  come importo del `PaymentIntent`/price Stripe
- `stripe.controller.js:3979,4048-4053` (`confirmPayment`): `application_fee_amount`/`amount` da `req.body`
  usati per calcolare l'incasso registrato, pur avendo disponibile `paymentIntent.amount_received` (autorevole,
  da Stripe) nello stesso scope
- `serviceReservation.helper.js:23-39,64-86`: stesso pattern per le prenotazioni di servizio
- `onlineTransaction.controller.js:77-111`: `pending_payment` salvato da `req.body.pending_amount` senza
  confronto col saldo residuo reale
- Lato frontend (§11.3-11.5): l'intero motore di pricing/sconti gira solo lato client

**Indicazione .NET**: il prezzo/importo autorevole è **sempre** un ricalcolo server-side (servizio di dominio
`IPricingEngine`) a partire da tariffario, date, regole sconto/voucher lette dal DB; l'input del client è al
più un "importo atteso" da confrontare e rifiutare in caso di mismatch, mai il valore salvato/addebitato.

### 3.5 Operazioni asincrone non attese / race condition

Pattern JavaScript ricorrente: `array.map(async item => {...})` usato come se fosse sincrono — nessun
`Promise.all`/`await` lo racchiude, quindi la funzione chiamante prosegue (e spesso risponde al client) prima
che le operazioni interne siano completate; eventuali errori interni diventano *unhandled promise rejection*
silenziose. Occorrenze verificate:

- `booking.controller.js:504-505,907-911,1075-1081` (creazione booking: risposta di successo inviata prima
  del completamento, rischio di doppia `res.send`)
- `booking.controller.js:8216-8228` (chiave `where` duplicata nello stesso update — la seconda sovrascrive la
  prima, l'update per-id non ha mai effetto reale)
- `booking.controller.js:12406-12421` (chiave `sharing_date`/`absence_date` duplicata in un `where`: il
  filtro sulla data di inizio viene silenziosamente perso, con impatto sul calcolo disponibilità/overbooking)
- `booking.helper.js:735-916` (`updateSharingDetails`: `resolve()`/`reject()` dentro loop annidati non
  coordinati, la Promise esterna si stabilizza dopo la prima iterazione, non dopo tutte)
- `helpers/property.helper.js:61-70` (chiave `startDate` duplicata: il limite superiore del range date viene
  perso, fatturato riportato gonfiato)
- `newsletter.controller.js:60-149` (`.map(async...)`, condizione `index === lastIndex` che può scattare più
  volte o mai → `ERR_HTTP_HEADERS_SENT` o richieste senza risposta)
- `cron-cancel-booking.js:29,42,71` (variabile globale implicita `sellerstripeconnectId` condivisa tra
  iterazioni concorrenti)
- `serviceReservation.helper.js:141-184` (conteggi servizio persi silenziosamente, stesso pattern)

**Indicazione .NET**: `await Task.WhenAll(...)` esplicito su collezioni di operazioni indipendenti, mai
`.Select(async x => ...)` "fire and forget"; la risposta HTTP va costruita solo dopo che tutte le operazioni
sono realmente completate, con eccezioni propagate (mai inghiottite).

### 3.6 Gestione errori assente o che espone dettagli interni

- **Catch vuoti** che inghiottono l'errore senza log: `email.helper.js:90,202,1466,1542,1544-1557`
  (`sendWhatsappTo`), `serviceReservation.helper.js:957-959` (refund Stripe rotto — `stripe` non è nemmeno
  importato nel file — fallisce sempre e silenziosamente)
- **Errori restituiti al client con `err.message`/oggetto `Error` completo**, spesso con `status(200)`:
  pattern ricorrente in `payment.controller.js`, `cardTransaction.controller.js`, `stripe.controller.js`,
  praticamente tutto `password.controller.js`/`auth.controller.js`/`subscribe.controller.js`
- **Endpoint che non rispondono mai** (nessun `res.send` in un ramo): `warehouse.controller.js:114-156`,
  `printerConfig.controller.js:250-282` (variabile non definita → eccezione non gestita dal secondo scontrino
  del giorno in poi — feature C4bis "stampa fiscale su hardware RT")
- **`ReferenceError` da variabili non dichiarate/mai definite**: `booking.controller.js:8297-8313`,
  `stripe.controller.js:4240-4376` (`updatePaymentSummaryInWebhook` usa `req.io`/`req.body` non definiti nel
  closure — l'intero flusso "pagamento tramite link" fallisce silenziosamente ad ogni occorrenza)

**Indicazione .NET**: `IExceptionHandler` globale unico che produce sempre una risposta (Problem Details,
vedi [standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md)), logga il dettaglio solo
server-side, non lo espone mai al client; nessun percorso di codice deve poter "non rispondere".

### 3.7 Denaro come `FLOAT`/`STRING` invece di `DECIMAL`

- `bookingPayments.model.js:43-51`: `total_amount`, `paid_amount`, `pending_amount` come `Sequelize.FLOAT`
- `onlineTaransactionDetails.js:17-25,32-37,50-52`: importi **e persino le foreign key** `booking_id`/
  `reservation_id` come `Sequelize.STRING`
- `booking-absences.model.js:3-5`, `bookingItemsCount.model.js:9-11`: `booking_id` come `STRING` mentre la PK
  referenziata (`booking.id`) è `INTEGER`

**Indicazione .NET**: `decimal`/`DECIMAL(18,2)` per ogni importo, FK tipizzate correttamente con vincoli
referenziali reali — coerente con l'abbandono del modello a colonne JSON già deciso in
[refactoring-analisi-A-vs-B.md](pre/refactoring-analisi-A-vs-B.md).

### 3.8 Middleware di sicurezza presenti ma disattivati

`user.routes.js` e `hotel.routes.js` mostrano, per centinaia di endpoint, righe come:

```js
/*authJwt.publicLimiter*/
//authJwt.verifyMobileApp,
//authJwt.checkAllowedDomain,
[authJwt.verifyToken/*, authJwt.isManager*/],
```

Rate limiting, whitelist di dominio, verifica app mobile e — su tutte le rotte hotel — controllo di ruolo,
sono presenti nel sorgente ma commentati sistematicamente: **nessuna di queste protezioni è mai attiva**,
mentre il codice dà l'impressione visiva del contrario a chi lo legge rapidamente.

**Indicazione .NET**: autorizzazione/rate limiting espressi come policy dichiarative applicate per default
(non middleware opt-in per singola route), con test di integrazione in CI che falliscono la build se un
endpoint sensibile non ha la policy attesa (§13).

---

## 4. Dominio C1 — Prenotazioni (Booking)

`booking.controller.js` (12.775 righe) e `booking.helper.js` (3.908 righe) sono il cuore del sistema e la
componente a rischio più alto per la riscrittura (coerente con la valutazione già fatta in
[refactoring-analisi-A-vs-B.md §5.2](pre/refactoring-analisi-A-vs-B.md)). Oltre ai pattern trasversali già
citati in §3 (di cui questo dominio è la fonte principale — la maggior parte delle occorrenze SQLi e IDOR di
§3.1/§3.2 sono qui), i finding seguenti sono specifici e non riconducibili a un pattern già descritto.

**Sicurezza**

- **Verifica di disponibilità che copre solo la prima location** — `booking.helper.js:3529-3589`
  (`isLocationAvailable`): il `for (const result of locationData)` ha un `return` incondizionato dentro al
  primo giro di loop in entrambi i rami `if`/`else`. Per una prenotazione multi-location (più ombrelloni/
  lettini in un'unica richiesta), solo la prima location viene realmente verificata: tutte le altre sono
  considerate libere senza controllo. **Rischio**: doppie prenotazioni sistematiche su tutte le location
  successive alla prima.
- **Dati di pagamento (PAN, CVC) salvati in chiaro e restituiti dalle API** —
  `onlinereservation.controller.js:36-40,84-88,108-112,156-160`: `cardNumber`/`cvc` scritti su tabella e
  ririnviati integralmente nella response JSON. Violazione PCI-DSS diretta (il CVC in particolare non va mai
  persistito).
- **Creazione prenotazione location senza controllo sovrapposizioni** — `locationreservation.controller.js:113-188`:
  insert diretto per ogni data/location richiesta, nessuna query di conflitto né lock. Due richieste
  concorrenti sulla stessa location/data producono doppia prenotazione.
- **Lost update sul contatore di utilizzo voucher/sconto** — `booking.controller.js:976-1067`: lettura,
  incremento in memoria e riscrittura di `totalNoOfUsage` senza transazione/lock (il secondo blocco, voucher
  automatici, lo fa oltretutto dentro un `.map(async...)` non atteso, §3.5). Due prenotazioni concorrenti con
  lo stesso codice sconto possono entrambe superare il `maximunNoUse` configurato.
- **Race condition check-then-insert sullo stock di magazzino hotel** — `booking.helper.js:3393-3420`: la
  `SELECT SUM(quantity)` di verifica disponibilità e la successiva `INSERT` non sono nella stessa transazione
  né protette da lock — overbooking di item fisici (cabine, lettini) sotto concorrenza.
- **Credenziali SMTP loggate in chiaro** — `booking.controller.js:5267-5288`: l'oggetto transporter
  (contenente `auth.user`/`auth.pass`) viene passato a `console.log`.
- **Query di endpoint statistiche con `logging: console.log` attivo** — `booking.controller.js:1994`:
  logga in produzione l'intera query con parametri (date, propertyId, id) ad ogni chiamata.

**Performance**

- **N+1 sistemico**: generazione+upload di 2 QR code su S3 ad ogni singola lettura dei dettagli booking
  invece che una volta alla creazione (`booking.controller.js:1426-1466`); `validateBookings` con una
  `findAll` per ogni prenotazione nel loop (10683,10705); `createSharing`/`getSharedLocationsByDate` con una
  query per ogni id/ogni giorno dell'intervallo, fino a migliaia di round-trip per una singola chiamata
  (6670-6678, 9316-9513); `updateBookingNumber` con una query per record anche dentro un batch già paginato
  (9779-9799); calcolo item hotel con tripla iterazione annidata e una query per elemento
  (`booking.helper.js:3245-3269`); `locationreservation.controller.js:161-173` con `INSERT` singole in loop
  invece di `bulkCreate` (fino a 300 insert sequenziali per una prenotazione stagionale multi-location).
- **Divisione importi per split-booking senza riconciliazione dei centesimi** — `booking.helper.js:3662-3813`:
  ogni quota arrotondata indipendentemente con `toFixed(2)`, nessuna verifica che la somma delle parti
  torni al totale originale (es. 100.00€/3 → 33.33+33.33+33.33 = 99.99€, un centesimo perso senza
  riassegnazione) — discrepanza contabile silenziosa. La stessa logica è duplicata in due funzioni
  (`convertIntoSplit`/`convertMultipleBookingIntoSplit`) con **divisori diversi e incoerenti** tra loro
  (3689 vs 3790 vs 3862).
- **Divisione per `length` non validato**: se `length` è `0` (array vuoto dal client), la divisione produce
  `NaN`/`Infinity` che viene comunque persistito nel JSON `booking_payments` senza errore visibile
  (`booking.helper.js:3665-3689`).

**Dati / Best practice**

- **Bug di assegnazione invece di confronto**: `sharing.isBooked = 1 ? "Completed" : "Waiting"` a
  `booking.controller.js:11285` — usa `=` invece di `==`/`===`: lo stato è **sempre** "Completed",
  indipendentemente dal valore reale, mutando anche l'oggetto originale.
- **`await` mancante su update Sequelize** — `updateReservationCode` (`booking.controller.js:12782-12792`):
  la funzione non è `async`, `Booking.update()` non è atteso, `if(updateData)` è sempre truthy (è una
  Promise): il client riceve "aggiornato con successo" anche quando l'update fallisce o non trova righe.
- **Bug analogo su `updatesectors`/`updateseasonsperiods`** nel dominio catalogo (§5) — stesso pattern,
  copiato.
- **Refund Stripe strutturalmente rotto**: `serviceReservation.helper.js:933-960` — `stripe` non è mai
  importato nel file; ogni cancellazione gestita da questo percorso tenta un rimborso che fallisce sempre con
  `ReferenceError`, catturato da un `catch` vuoto (§3.6): nessun rimborso viene mai eseguito, nessuno se ne
  accorge.
- **Stati/periodi come literal sparsi** (status `1`/`2`/`3`, periodo `'Morning'`/`'Afternoon'`/`'FullDay'`)
  ripetuti in decine di punti di `booking.helper.js` senza un'unica fonte di verità (es. righe 3544, 2686,
  3399, 477): un refactor che introduce un nuovo stato richiede una caccia manuale a tutte le occorrenze.
- **Duplicazione di funzioni "morte"**: `cancelServiceBooking1` e `cancelServiceBooking`
  (`servicereservation.controller.js:1376-1421`) sono quasi identiche, la prima è codice morto mai rimosso ma
  espone comunque la stessa falla IDOR.

**Indicazione .NET per l'intero dominio**: questo è il modulo `Booking` core descritto in
[architettura-evolutiva-microservizi.md §2.2](pre/architettura-evolutiva-microservizi.md) — richiede
disponibilità/conflitti verificati **nella stessa transazione** dell'insert (constraint DB o lock esplicito,
mai un pre-check separato), allocazione di importi con algoritmo "largest remainder" per garantire
`Sum(parti) == Totale`, e un dominio ricco (`Booking.Confirm()`, `Booking.Cancel()`) invece di controller che
manipolano direttamente righe JSON.

---

## 5. Dominio C2 — Catalogo & configurazione

### 5.1 IDOR sistemico su tutto il dominio (il finding più grave del dominio)

Ogni controller del catalogo — `pricelist` (addpricelist, pricelistdetail, updatepricelist, deletepricelist,
seasonalprice: 65-379), `discounts` (adddiscounts: 16-77), `sectors` (addsectors, updatesectors: 8-140),
`seasons` (addseasonsperiods, updateseasonsperiods: 13-182), `servicetypes` (addservicetypes: 14-83),
`bathouse` (updatebathouse: 388-523), `confimaps` (addconfimaps, resetBeachMap: 19-438), `warehouse`
(addwarehouse: 10-71) — legge `req.body.propertyId` e lo usa direttamente nel `WHERE`, **senza mai
verificarlo contro l'utente autenticato**. Il ruolo applicato via route (`user.routes.js:375-427,430-439`) è
`isSuperAdmin`, ma `isSuperAdmin` (`authJwt.js:~83`) controlla solo che l'utente abbia un ruolo chiamato
"superadmin" — assegnato **per singola property** in fase di registrazione
(`auth.controller.js:174,211,226,235`), quindi tecnicamente indistinguibile da un vero super-admin di
piattaforma.

**Rischio concreto, riproducibile**: il gestore dello stabilimento A, autenticato col proprio token valido,
può chiamare `POST /api/sb/deletepricelist` o `/api/sb/addsectors` passando l'`id`/`propertyId` dello
stabilimento B e alterare/cancellare listini, sconti, settori, stagioni, mappa spiaggia o magazzino di un
concorrente.

Aggravante specifica: su **tutte le rotte hotel** (`hotel.routes.js:34-158` e oltre) il controllo di ruolo è
esplicitamente commentato — `[authJwt.verifyToken/*, authJwt.isManager*/]` — quindi qualunque utente
autenticato, anche un semplice cliente, può scrivere sul catalogo hotel di terzi (nessun controllo di ruolo
minimo, non solo di ownership).

### Altri finding — Sicurezza

- **Mass assignment** su `UpdateOnlineBookingSettings` (`property.controller.js:964-978`): l'intero
  `req.body` passato a `Property.update()` senza whitelist, combinato col punto 5.1 — un chiamante può
  alterare `status`, `commissionAmount`, `fiskalyFlag` di qualunque property.
- **Mascheramento PII disattivato per bug**: `serchProperty` (`property.controller.js:313-322`) importa le
  funzioni `maskEmail`/`maskName`/`maskPhone` (usate correttamente altrove, es. `warehouse.controller.js:87-91`)
  ma le applica con un ternario che restituisce lo stesso valore in entrambi i rami — no-op. L'endpoint
  pubblico di ricerca proprietà espone email/cellulare/indirizzo di ogni stabilimento senza autenticazione.
- **Upload immagini/mappe senza validazione** (`bathouse.controller.js:34-91,401-476`,
  `confimages.controller.js:90-122`, `confimaps.controller.js:60-78,120-129`): nessun controllo di
  content-type reale, dimensione, numero file prima dell'upload su S3 pubblico.

### Performance

- **Ricerca proprietà (`serchProperty`) senza paginazione** (`property.controller.js:252-467`): `findAll`
  senza `limit`/`offset`, poi una query aggiuntiva (`getLowestPrice`, con `JSON_TABLE`) **per ogni** risultato
  trovato, e filtro per servizi applicato in JavaScript dopo aver caricato tutto in memoria. Codice morto
  (righe 400-467) mostra che una versione paginata esisteva ed è stata abbandonata.
- **Full-table scan multi-tenant sugli sconti**: `getpropertyDiscount`/`getPropertyDiscountDetails`
  (`discounts.controller.js:250-360`) caricano gli sconti di **ogni** property del sistema, poi filtrano in
  loop JS — il costo cresce con la crescita della piattaforma, non con la richiesta specifica.
- **`searchRegion`**: tre query sequenziali con JOIN invece di una (`searchRegion.controller.js:70-212`),
  con un array `searchResult` calcolato e mai usato nella risposta (codice morto).
- **Cinque endpoint statistiche** (`packageStatisticsQuantity`, `additionalListQuantity`,
  `barServicesQuantity`, `normalServicesQuantity`, `hourlyServicesQuantity`,
  `property.controller.js:533-962`) senza `LIMIT` sul range di date e senza alcun `try/catch` (a differenza
  di `statistics` nello stesso file) — un range di più anni genera parsing JSON riga-per-riga proporzionale
  allo storico totale, con rischio OOM/timeout e nessuna risposta di errore strutturata.
- **Upload S3 ridondanti**: `updatebathouse` (`bathouse.controller.js:420-476`) carica la stessa immagine
  fino a 2-3 volte nello stesso handler, con risultati intermedi scartati/mai referenziati.

### Dati / Best practice

- **Bug finanziario nel calcolo fatturato**: `property.helper.js:61-70` — chiave `startDate` duplicata nello
  stesso oggetto `where` (`Op.lte` sovrascritto da `Op.gte`): il filtro superiore del range viene perso, i
  report di fatturato per periodo sono sistematicamente gonfiati.
- **`.reduce()` senza valore iniziale** su array potenzialmente vuoti (`property.helper.js:93-95,167-169`):
  eccezione catturata ma il chiamante interpreta l'errore come "fatturato zero" invece di segnalare
  un'anomalia — impossibile distinguere "davvero zero incassi" da "bug".
- **Endpoint che non risponde mai**: `warehouse.controller.js:114-156` (`bookingdetails`) — nessun ramo del
  `.then()` chiama `res.send`.
- **`ReferenceError` nella registrazione property senza foto**: `property.controller.js:81-98` — `imgurls`
  usato fuori dal blocco `if` che lo assegna; una registrazione senza foto genera un record DB "fantasma" con
  un errore JS in risposta.
- **Cache Redis invalidata con la chiave sbagliata**: `updatepricelist` (`pricelist.controller.js:262-296`)
  invalida `pricelist:group:${req.body.propertyId}` (valore client, spesso assente nel payload di update),
  mentre `deletepricelist` usa correttamente il valore letto dal record — prezzi stantii serviti da cache
  fino a 1800s dopo una modifica.
- **Condizione sempre falsa (dead branch)**: `if (photos.length && photos.length === 0)` in
  `bathouse.controller.js:415,429` — logicamente impossibile, il ramo non viene mai eseguito.
- **Doppio `JSON.parse` incoerente** sulla stessa colonna `discounts.items` in 3 punti su 4
  (`discounts.controller.js:94,130,229,301`) — segno di un cambio di codifica mai migrato.
- **Bug di precedenza operatori**: `(res?.selectedVariant ?? [].length == 0)` invece di
  `(res?.selectedVariant ?? []).length == 0` (`property.controller.js:699,733`) — la condizione non verifica
  mai realmente la lunghezza dell'array, con impatto sui report vendite bar.
- **Migrazione dati mascherata da endpoint live**: `updateVoucherDateRange`
  (`discounts.controller.js:417-447`) — `findAll()` su tutta la tabella `discounts` di ogni property,
  salvataggio riga per riga senza transazione/batching, un solo JSON malformato interrompe il ciclo a metà.
- **Variabili globali per i messaggi di risposta**: `globalVariables.js` popola `global.xxx = "..."`, ogni
  controller vi fa riferimento come variabile libera non dichiarata — nessun errore a compile-time su
  refactor/typo, solo `ReferenceError` a runtime (probabile causa del punto precedente su property senza
  foto).
- **Pattern CRUD duplicato in 6+ controller** con bug divergenti introdotti in copie diverse: `updatesectors`
  e `updateseasonsperiods` non fanno `await` sulla `.update()` (mentre le versioni "add" nello stesso file lo
  fanno), quindi rispondono "successo" prima che la scrittura sia confermata.
- **Homepage "in evidenza" hardcoded nel sorgente**: `utils/propertyList.js` (338 righe, di cui ~290 di dati
  morti commentati) — ogni modifica alla vetrina richiede un redeploy del backend.
- **Business logic complessa nel controller**: l'algoritmo di resize della griglia mappa spiaggia (~130
  righe) vive interamente dentro `confimaps.controller.js:159-290`, non testabile in isolamento.

**Indicazione .NET**: il modulo `Configuration/Catalog` (C2) deve derivare `propertyId` dal claim del token
(mai dal body) tramite una policy di autorizzazione basata su risorsa condivisa da tutti gli endpoint del
modulo — non un controllo ripetuto (e dimenticabile) in ogni handler; vedi
[autenticazione-autorizzazione-dotnet.md §2](pre/autenticazione-autorizzazione-dotnet.md).

---

## 6. Dominio C3/C4 — Pagamenti & Fiscale (Stripe, Fiskaly)

Dominio a rischio massimo per definizione (soldi reali, obbligo di legge sullo scontrino fiscale). Oltre ai
pattern §3.1-§3.4 (di cui questo dominio è anch'esso una fonte primaria), i finding specifici:

> **Nota** — l'analisi tabella-per-tabella successiva (`analisi-pre/db/`, `docs/03-gestione-dati.md`
> §2.4ter/§4) separa questo dominio in **C3** (`Payment` — Stripe/incassi/transazioni/POS) e **C4**
> (`Fiskaly` — scontrino fiscale **elettronico** SIGN IT), e isola una feature a sé **C4bis —
> "Stampa fiscale su hardware RT"** (percorso legacy `printerConfig.controller.js` + adapter di
> stampa lato client: tabelle `printer_configs`, `booking_receipts`, `receipt_serial_numbers`;
> si mantiene come componente separato). I finding qui sotto restano validi; la ripartizione modulare .NET
> segue quella suddivisione più fine.

**Sicurezza — Critica**

- **`optionalAuth` su endpoint di pagamento reali**: `/api/sb/refund`, `/api/sb/capturePayment`,
  `/api/sb/updateCaptureCharge`, `/api/sb/TransferTOAdminAccount`, `/api/sb/send-link` (§3.3) — nessuna
  verifica di ruolo/ownership nei controller stessi.
- **PAN e CVC ricevuti dal proprio backend e inoltrati a Stripe** —
  `payment.controller.js:134-178` (`createStripeCrustomerDataByCardData`),
  `common.helper.js:477-487`: il numero di carta transita in chiaro nel body HTTP verso il server Express
  invece di essere tokenizzato client-side con Stripe.js/Elements. Porta l'infrastruttura in scope PCI-DSS
  SAQ D con rischio concreto di logging accidentale (pattern diffuso nel codebase, §3.6).
- **`updateCaptureCharge` opera su un charge Stripe hardcoded** (`stripe.controller.js:2383-2419`): codice di
  debug rimasto attivo, aggiorna sempre lo stesso charge reale (`"ch_3KkgW92eZvKYlo2C1EIICclm"`) e scrive il
  suo importo sulla riga identificata da uno `stripe_transaction_id` arbitrario passato dal chiamante —
  chiunque può marcare come "succeeded" qualunque transazione con un importo a piacere.
- **Refund funzionalmente rotto**: `refundPaymentController`/`refundPayment`
  (`stripe.controller.js:2420-2470`) — legge `req.body.bookingid` (il campo reale è `booking_id`, quasi
  sempre `undefined`), passa un id di `PaymentIntent` dove Stripe si aspetta un id di `Charge`, e referenzia
  `refundData` mai definito nello scope di successo. Lo staff può credere che un rimborso sia avvenuto quando
  non lo è.
- **`transferToSeller` ignora il parametro e trasferisce sempre allo stesso account hardcoded**
  (`stripe.controller.js:91-103`): `destination: "acct_1J7L2L2S34EthQ0F"` letterale, `sellerStripeId`
  ricevuto ma mai usato.
- **Endpoint di test con carta Stripe di test esposto in produzione**: `TransferTOAdminAccount`
  (`stripe.controller.js:3656-3695`, route pubblica `user.routes.js:1939-1945`) — commento nel codice stesso
  ("test function for check stripe payment"), carta `4242...` e account destinazione hardcoded.
- **Bug critico nel webhook "payment link"**: `updatePaymentSummaryInWebhook`
  (`stripe.controller.js:4240-4376`) referenzia `req.io`/`req.body` non definiti nella firma della funzione
  (`async (id) => {...}`), chiamata **senza try/catch** dal webhook handler
  (`stripeWebhookHandler.js:574-599`): ogni pagamento completato via link fallisce silenziosamente — nessuna
  conferma, nessuna email, Stripe ritenta contro lo stesso errore all'infinito.
- **SQL injection nel ledger carta prepagata**: `cardTransaction.controller.js:132-168` (`getMovementList`,
  endpoint `GET` pubblico con solo `optionalAuth`) — anche su `req.query.email`.

**Sicurezza — Alta**

- **Segreti Fiskaly cifrati in modo incoerente**: `fiskalyAccount.model.js:50-57,103-106` —
  `subject_credential_key`/`subject_credential_secret`/`taxpayer_fisconline_pin` salvati in chiaro, mentre
  `taxpayer_fisconline_password` è correttamente cifrato AES-256-GCM (`fiskalyEncrypt.js`) — il codice
  dimostra di conoscere il problema ma applica la soluzione solo a un campo. `subject_credential_secret` è la
  credenziale che autentica ogni emissione di scontrino verso l'Agenzia delle Entrate.
- **Nessun lock sulla creazione scontrino fiscale**: `fiskalyReceiptManagerService.js:1890-1941` — check
  "esiste già una ricevuta aperta" e creazione non sono in transazione/lock; un fallimento del salvataggio
  del flag "già fatturato" **dopo** una chiamata Fiskaly riuscita può portare a doppia emissione di scontrino
  fiscale reale — problema di compliance non risolvibile con una semplice cancellazione.
- **Update di configurazione Stripe con `WHERE` potenzialmente degenere**:
  `payment.controller.js:203-249` (`getCustomerIdByAuthCode`) — `req.body.userId` mai validato, usato in
  `where: { id: req.body.userId }`: nel caso peggiore un update di `sellerStripeConnectId` non filtrato
  correttamente instrada incassi reali all'account Connect sbagliato.
- **Idempotenza webhook solo parziale**: `stripeWebhookHandler.js:406-568` — il check di idempotenza legge lo
  stato della transazione **prima** di eseguire conferma booking + notifiche; un crash tra l'update di stato
  e queste operazioni lascia un pagamento incassato ma mai confermato nel gestionale, e un redelivery Stripe
  successivo non rieseguirà nulla (lo stato risulta già "succeeded"). Il branch `checkout.session.completed`
  (574-599) non ha **nessun** controllo di idempotenza.
- **Confirm-payment duplicato client+webhook**: `confirmPayment` (chiamato dal frontend dopo Stripe.js) e il
  webhook eseguono entrambi conferma booking/notifiche in modo indipendente — race condition tipica, doppia
  email di conferma.

**Performance**

- **Nessun indice** su `stripe_transaction_id`/`booking_id`/`reservation_id` in
  `onlineTaransactionDetails.js` (nessun blocco `indexes`), colonne lette ad ogni webhook/refund — degrado a
  full scan sotto carico stagionale, con rischio di aggravare i problemi di idempotenza (retry Stripe su
  timeout).
- **`calculateSuggestedPrice`**: loop giorno-per-giorno con chiamata a un'API meteo esterna **e** query DB
  per ciascun giorno del periodo (`revenue.controller.js:248-331`) — su un range stagionale (120-150 giorni)
  centinaia di round-trip sequenziali, rischio di superare i rate limit dell'API esterna.
- **Saldo carta prepagata ricalcolato con `SUM()` su tutto lo storico** ad ogni lettura
  (`cardTransaction.controller.js:312-327`), nessun indice composito `(user_id, property_id, isDeleted)`.

**Dati / Best practice**

- **Nessuna transazione DB atomica sui flussi multi-step**: `updatePaymentDetailsofBooking`
  (`stripe.controller.js:3595-3647`) esegue due `.update()` separati (Booking + BookingPayments); il servizio
  revenue (`transactionsPaymentsService.js:204-208,510-514`) fa `destroy()` + `bulkCreate()` non transazionali
  — un crash a metà lascia un buco nel report IVA/revenue per quella prenotazione.
- **Errori sul percorso fiscale/revenue loggati e ignorati**: `transactionsPaymentsService.js:213-219,519-524`
  — commento esplicito nel codice "should not break the booking flow": la prenotazione procede, il dato
  fiscale/di revenue per quella riga risulta semplicemente mancante, senza alert.
- **Importi/FK come `STRING`** in `onlineTaransactionDetails.js` (§3.7).
- **`sync({ alter: true })` eseguito ad ogni avvio** su una tabella di transazioni di produzione
  (`onlineTaransactionDetails.js:59`) — auto-alter dello schema invece di migration versionate.
- **Errori con oggetto `Error` completo esposto al client**, spesso con `status(200)` — pattern diffuso
  (§3.6), qui su dati finanziari.

**Indicazione .NET**: modulo `Payment` isolato dietro un'interfaccia C# (SDK `Stripe.net` ufficiale, coerente
con [architettura-evolutiva-microservizi.md §2.2](pre/architettura-evolutiva-microservizi.md)); ogni sequenza
verifica→chiamata esterna→persistenza va in un'unica transazione con outbox per gli effetti collaterali
(email/notifiche); idempotenza dei webhook basata su `event.id` Stripe persistito, non sullo stato derivato
dell'entità applicativa; nessun segreto Fiskaly/Stripe in chiaro, tokenizzazione carte esclusivamente
client-side.

---

## 7. Dominio C5 — Autenticazione, autorizzazione, utenti, middleware, config

Il dominio con la più alta concentrazione di criticità **critiche** isolate (non solo pattern ripetuti):
diversi endpoint permettono, singolarmente e senza bisogno di combinare più bug, un account takeover
completo.

**Sicurezza — Critica**

- **Reset password forgeable**: `password.controller.js:5` cifra l'email con `Cryptr("myTotalySecretKey")`
  (chiave statica, hardcoded, non un token random) invece di generare un token opaco con scadenza;
  `/api/auth/resetpassword` (`updatepasswordByEmail`, 246-314) non ha middleware di autenticazione. Chiunque
  conosca la chiave (nel repository) può forgiare una "conferma" valida per qualsiasi email.
- **`/api/auth/updatepassword` non autenticato**: `password.controller.js:316-353` — l'id utente da
  modificare arriva dal body (`req.body.userid`), non dal token. `POST` con solo `{userid, password}`, nessun
  header, cambia la password di qualunque account.
- **`/api/auth/verifypassword` non autenticato, nessun rate limit**: `password.controller.js:208-244` —
  oracolo di verifica password per qualunque `userid`, sfruttabile per credential stuffing offline-friendly.
- **`/api/auth/send-token` emette un JWT valido conoscendo solo l'email**: `auth.controller.js:896-1033`
  (`sendToken`) — nessuna verifica di password o OTP nello stesso flusso, solo controllo che l'utente esista
  e sia attivo. Account takeover conoscendo solo un indirizzo email pubblico. **Nota**: il pannello
  systemadmin `front-admin/` (in repo dal 2026-09-09) usa proprio questo endpoint per la funzione
  "accesso diretto" al gestionale come titolare (`PropertyManagement/ApproverdProperties.js` →
  URL hardcoded verso un tunnel di sviluppo, payload ricifrato con chiave AES `"SB_USER_DATA"` nel
  bundle, apertura di `http://localhost:5001/...`): l'impersonation va riprogettata con token monouso
  server-side a scadenza breve, URL da configurazione e audit dell'accesso.
- **JWT secret hardcoded e debole**: `auth.config.js:2` (§2), nessuna restrizione di algoritmo in
  `jwt.verify` (`authJwt.js:15,30,42,320` — niente `{ algorithms: ["HS256"] }`).
- **SQL injection in `userlist`/`edituser`**: `user.controller.js:87-92,272-278` — anche su `propertyId`
  (dato multi-tenant).

**Sicurezza — Alta**

- **JWT validi 365 giorni, nessuna revoca**: `auth.controller.js:334-336,531-533,613-615` — `logOut`
  (1035) fa solo `res.clearCookie`, ma l'app usa `x-access-token` in header, non cookie: il logout non
  invalida nulla lato server. Un token rubato resta utilizzabile per un anno anche dopo cambio password.
- **OTP a 4 cifre, `Math.random()`, nessun rate limit, non invalidato dopo l'uso**:
  `common.helper.js:778-781`, `auth.controller.js:833-864` — 9.000 combinazioni, PRNG non crittografico,
  30 minuti di validità, endpoint pubblico senza lockout.
- **Rate limiting definito ma mai applicato**: `authJwt.js:333-347` definisce `publicLimiter`, ma è
  commentato ovunque nel routing (§3.8) — login, reset password, OTP completamente esposti a brute force.
- **IDOR su `edituser`/`updateuser`**: `user.routes.js:194-213` — solo `verifyToken`, nessun controllo di
  ruolo (a differenza di `userlist`/`deleteuser` che richiedono `isSuperAdmin`), id utente da modificare dal
  body.
- **`deleteAccount` protetto solo da `optionalAuth`**: `user.routes.js:956-963` — un chiamante anonimo che
  indovina uno `userid` può disattivare qualunque account, incluso un admin.
- **Downgrade di autorizzazione documentato dal codice stesso**: `user.routes.js:604-622` — commento che
  mostra la protezione originale `[verifyToken, isSystemAdmin]` sostituita da `optionalAuth` per
  `/api/sb/propertydetails` e `/api/sb/getPropertyWithBathHouseDetails`.
- **Endpoint di pagamento con codice di test hardcoded esposto pubblicamente**: `TransferTOAdminAccount`
  (già in §6, qui rilevante anche come problema di routing/autorizzazione: `user.routes.js:1939-1946`).

**Sicurezza — Media**

- **CORS completamente aperto**: `server.js:87,106-114` — `cors()` di default + header manuali
  `Access-Control-Allow-Origin: *`/`Methods: *` su tutta l'API. Il middleware `checkAllowedDomain`
  (`authJwt.js:365-400`) pensato per restringere non è mai collegato a nessuna route, e comunque userebbe un
  confronto `startsWith` bypassabile con un dominio tipo `https://summerbooking.it.attacker.com`.
- **Bcrypt cost factor 8**: `auth.controller.js:45,202`, `password.controller.js:274,327` — sotto lo standard
  raccomandato (≥10-12).
- **User enumeration**: `signin` (messaggi distinti utente-non-trovato/password-errata) e
  `checkemail.controller.js:6-31` (conferma esplicita se un'email è registrata), nessun rate limit su
  entrambi.
- **Credenziali DB di produzione hardcoded** + **TLS con verifica certificato disabilitata**:
  `db.config.js:18-27` (`rejectUnauthorized: false`) — già in §2, qui si aggiunge che il ramo "produzione"
  della configurazione è interamente commentato: se `NODE_ENV` non è esattamente `"development"`,
  `module.exports` risulta `undefined` e l'app non si avvia.
- **Swagger pubblico**: `server.js:101` — documentazione completa dei 375+ endpoint raggiungibile senza
  autenticazione, facilita la ricognizione per un attaccante.
- **Socket.IO senza autenticazione**: `server.js:124-144` — chiunque si connette e si iscrive a
  `property_<id>` per qualsiasi id, ricevendo eventi realtime (verosimilmente notifiche prenotazioni) di
  strutture altrui; CORS del socket anch'esso `*`.

**Best practice / Robustezza**

- **12+ funzioni di controllo ruolo duplicate senza gestione errori**: `authJwt.js:52-308` (`isWebsiteAdmin`,
  `isSystemAdmin`, `isSuperAdmin`, `isAdmin`, `isManager`, `isLifeguard`, `isCashOperator`, `isConsumer`,
  `checkRole`, ecc.) — ciascuna ripete `User.findByPk(...).then(...)` senza `.catch()`: se l'utente del
  token è stato cancellato (plausibile, dato lo scadere a 365 giorni) la richiesta resta appesa fino a
  timeout invece di rispondere 401.
- **Bug di case-mismatch su un messaggio di errore**: `authJwt.js:164` referenzia `requirelifeguardRole`
  (minuscolo) mentre `globalVariables.js` definisce `requireLifeguardRole` — variabile non definita,
  `ReferenceError` non gestito ogni volta che un utente senza ruolo lifeguard viene respinto.
- **Race condition su variabili di modulo condivise**: `auth.controller.js:451,469-471`
  (`newGoogleClientId`, riassegnata ad ogni richiesta `social` e letta dopo un `await` — due richieste
  concorrenti mobile/web possono usare l'audience Google sbagliata); `password.controller.js:40`
  (`authorities` senza `var/let/const`, globale implicita).
- **Limite body 100MB globale + upload multipart senza `limits.fileSize`, montato su ogni richiesta**:
  `server.js:48-49,88,98-99` — vettore DoS a basso costo; `server.setTimeout(5000000)` (~83 minuti,
  `server.js:189`) aggrava l'esaurimento connessioni.
- **Strato di cache Redis "middleware" morto, uso ad hoc incoerente**: `redisCache.middleware.js:1-34`
  interamente commentato, ma `getCache`/`setCache`/`clearGroupCache` usati manualmente e in modo non uniforme
  in ~10 controller diversi, ciascuno con propria logica di chiave/TTL (coerente con il bug di invalidazione
  già visto in §5).

**Indicazione .NET**: vedi [autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md)
(già scritto in previsione di questi problemi): secure-by-default, ruolo+scoping nel token, due schemi JWT
distinti utente/M2M, access token a vita breve + refresh revocabile, rate limiting nativo ASP.NET Core
applicato per policy e non per singola route "opt-in".

---

## 8. Dominio C6 — Comunicazione & marketing

**Sicurezza**

- **SQL injection** negli stessi file già citati in §3.1 (`email.helper.js`, `marketingAutomation`,
  `marketingEmailCampaign`, `marketingContactList`, `cron.helper.js`).
- **Password SMTP e chiave AES nel bundle frontend** — già coperto in §2, qui rilevante perché
  `DecryptionService.js` del frontend la usa per "proteggere" payload che chiunque può comunque decifrare
  (security theatre).
- **Validazione TLS disabilitata su più transport SMTP**: `rejectUnauthorized: false` ripetuto identico in
  `emailConfig.controller.js:133-135`, `news-letter.js:134`, `switchboard.controller.js:2757` — MITM
  possibile su connessioni che trasportano le credenziali SMTP stesse.
- **Password SMTP restituita in chiaro dall'API**: `emailConfig.controller.js:76-91` (`getEmailConfig`) —
  nessuna evidenza di controllo ownership sul `propertyId`.
- **Dati utente interpolati senza escaping in template email HTML**: `email.helper.js:871-877,2013-2031,275`
  — uno strip-regex naive dei tag (facilmente aggirabile) è l'unica "sanitizzazione" presente.

**Performance**

- **Invio email sincrono e sequenziale**: `cron.helper.js:1330-1384` (`sendCampaignEmail`),
  `news-letter.js:116-170` — un `await` per destinatario, re-render del template EJS da disco ad ogni
  iterazione (nessuna cache del template compilato); una campagna con migliaia di destinatari blocca il tick
  del cron per minuti.
- **N+1 nell'elaborazione automazioni**: `cron.helper.js:1463-1498` (`processAutomation` →
  `processMemberAutomation`) — una query SQL raw **per destinatario** per ogni automazione attiva, ogni 5
  minuti.
- **Nessuna strategia di retry/backoff**: `cron.helper.js:2051-2082` — un invio fallito lascia `status: 0`,
  ripescato e ritentato **ad ogni tick, per sempre**, anche per errori permanenti (es. indirizzo inesistente),
  con rischio concreto di blacklisting del dominio mittente.

**Dati / Best practice**

- **Duplicazione massiccia della logica di generazione HTML**: `email.helper.js:406-729` — blocco quasi
  identico ripetuto 4 volte (EN/IT × sovrapprezzo percentuale/fisso), già divergente nei dettagli di
  formattazione tra le copie.
- **Catch vuoti** che inghiottono errori, incluse le notifiche WhatsApp (`email.helper.js:90,202,1466,1542-1557`,
  `sendWhatsappTo`) — nessun log, nessun alert su un fallimento sistemico (es. credenziali Twilio scadute).
- **Race condition nel controller newsletter**: `newsletter.controller.js:60-149` (§3.5).
- **Setup SMTP duplicato ad-hoc**: `switchboard.controller.js:2745-2775` crea un proprio transporter invece
  di riusare `mailtemplates/mailsetup.js`, con gestione TLS non allineata alle altre due istanze (di nuovo
  `rejectUnauthorized: false`, ma configurato indipendentemente).

**Indicazione .NET**: modulo `Notification` con motore di rendering basato su Razor (auto-escaping by
default, coerente con [linee-guida-implementative-dotnet.md §8](pre/linee-guida-implementative-dotnet.md)),
invio disaccoppiato via coda/outbox con retry a backoff esponenziale e stato terminale "failed" dopo N
tentativi (pattern Polly).

---

## 9. Dominio C7 — Switchboard/Centralino

**Sicurezza**

- **Webhook Telnyx privi di verifica firma**: `switchboard.controller.js:2639-2716`
  (`handleCallWebhook`/`handleSmsWebhook`, sezione commentata esplicitamente `// NO AUTH`) — nessuna verifica
  dell'header `Telnyx-Signature-Ed25519`. Un attaccante può forgiare eventi (`call.gather.ended` con
  `dtmfDigits` a piacere) per far eseguire azioni IVR come `send_whatsapp` a spese dell'account business, o
  interferire con chiamate reali in corso.
- **Webhook WhatsApp/Meta: verifica firma solo sull'handshake GET, non sul POST reale**:
  `switchboard.controller.js:2471-2481` (GET, corretto) vs `2489-2578` (`handleWaWebhook`, POST — non valida
  `X-Hub-Signature-256` pur avendo `WHATSAPP_APP_SECRET` disponibile).
- **Credenziali SIP in chiaro**: generate random (corretto) ma poi salvate in chiaro
  (`switchboardConfig.sipPassword`), usate come chiave di lookup diretta senza hashing
  (`switchboard.controller.js:2373-2413,2424-2460`), restituite in chiaro nella response di creazione
  (1872-1900) e spedite via email HTML senza scadenza (2745-2775).
- **Endpoint pubblici autenticati solo da confronto diretto di credenziali SIP, nessun rate limiting**:
  `getSipCallHistory`/`registerPushToken` (2373-2460) — `findOne({ where: { sipUsername, sipPassword } })`,
  nessun throttling né lockout.
- **Payload di errore dei provider esterni restituiti tal quali al client**: pattern ricorrente
  (660-662,997-999,1992-1999) — dettagli su configurazione interna (ID connessioni Telnyx, struttura account
  WhatsApp) esposti a chiunque abbia accesso alle chiamate API.

**Indicazione .NET**: modulo `Switchboard` isolato (già raccomandato come candidato n.1 all'estrazione in
[architettura-evolutiva-microservizi.md §2.1](pre/architettura-evolutiva-microservizi.md), anche per il
runtime real-time diverso), con un `IWebhookSignatureValidator` condiviso per Telnyx/Meta/Stripe e segreti
mai loggati/restituiti dopo la creazione iniziale (pattern "mostra una sola volta", come una API key).

---

## 10. INFRA — Upload, cron, configurazione, integrazioni esterne

**Sicurezza**

- **Upload file senza validazione, S3 key da input utente, ACL pubblica**:
  `uploadFile.controller.js:10-34` + `s3Upload` (`globalConstants.js:365-393`) — nessun controllo su
  estensione/MIME reale/dimensione, `file.originalname` (controllabile dal client) diventa parte della chiave
  S3, oggetto reso `public-read`. Da confrontare con `audioUpload.js`, che nello stesso progetto implementa
  correttamente whitelist estensioni, limite dimensione e nome generato server-side — l'incoerenza tra
  endpoint è essa stessa il problema.
- **Bug bloccante nella numerazione ricevute fiscali** *(feature C4bis — stampa fiscale su hardware RT)*:
  `printerConfig.controller.js:250-282` (`saveReceiptdata`) — `receipt_created_date` mai dichiarata nello
  scope, `ReferenceError` garantito dal secondo scontrino del giorno in poi; nessun `.catch()` sulla catena,
  la richiesta resta appesa. Impatta direttamente stampa e tracciabilità fiscale. Scheda:
  `analisi-pre/db/receipt_serial_numbers.md`.

**Performance**

- **Cron job eseguiti in-process nel web server, alcuni ogni 5 secondi**: `cronjob/cron-job.js` (es. righe
  90-195) — condividono pool di connessioni DB ed event loop col traffico utenti; nessuna protezione da
  sovrapposizione tranne un job su tutti quelli definiti (`automationEmail`, con flag `isRunning`) — gli
  altri possono sovrapporsi se l'esecuzione supera l'intervallo, causando invii duplicati/race condition su
  aggiornamenti concorrenti.

**Best practice**

- **Variabile globale implicita + async non atteso**: `cron-cancel-booking.js:29,42,71` (§3.5).
- **Duplicazione di setup SMTP/servizi esterni** già coperta in §8.

**Indicazione .NET**: coerente con [infrastruttura-aws.md §3.7](../../../../../infra/infrastruttura-aws.md) — job
schedulati **non** nel processo web ma in EventBridge Scheduler → ECS RunTask (esecuzione singola garantita),
elaborazioni event-driven su SQS → worker dedicato; upload validati (magic bytes, whitelist estensioni,
`RequestSizeLimit`), chiavi oggetto generate server-side, mai ACL pubblica di default.

---

## 11. Frontend: codice/logica da spostare o duplicare nel backend

Analisi di `front-pre/src` incrociata con `back-pre` per verificare, dove rilevante, se il backend accetta
realmente ciò che il client calcola. Per il dettaglio completo dei controlli incrociati vedi il metodo
descritto in premessa; qui i finding verificati. **§11.12** copre a parte il pannello systemadmin
`front-admin/`, aggiunto al repo il 2026-09-09.

### 11.1 Segreti nel bundle pubblico (già in §2)

`front-pre/src/constants/envrionment.js:7,49-57` — password SMTP e chiave AES `ENC_KEY` nel codice sorgente
del frontend, quindi nel bundle JS pubblico. `ENC_KEY` è poi usata da `DecryptionService.js` per decifrare
risposte del backend: chiunque ispezioni i DevTools ottiene la stessa chiave, rendendo la cifratura verso il
client pura *security theatre*.

### 11.2 Endpoint di scrittura booking senza autorizzazione reale — verificato end-to-end

`user.routes.js:717-892` monta `/api/sb/booking`, `/api/sb/update/booking`, `/api/sb/booking/deleteBooking`
con `authJwt.optionalAuth` (§3.3). Nel frontend (`ThePlannerPage.js`, 20+ occorrenze tra le righe
4301-5952) i controlli per ruolo `lifeguard`/`advancedlifeguard` **nascondono solo elementi UI**: il backend
possiede middleware di ruolo granulari (`isLifeguard`, `checkRole`, ecc.) ma non li applica a nessuna di
queste tre operazioni. Un utente lifeguard (o chiunque conosca l'URL) può chiamare direttamente l'API e
bypassare completamente la restrizione grafica.

### 11.3 Il backend salva e riutilizza ciecamente prezzo/importi calcolati lato client — verificato

`front-pre/src/services/BookingCalcService.js` calcola l'intero totale (tariffe stagionali/weekend/festivi,
scaglioni 7-14-30gg, prezzi cumulativi multi-location); il risultato (`payableAmount`, `toPay`,
`TotalSumItem`) viene inviato e il backend lo salva/usa as-is (booking.controller.js:2634,2640,
booking.helper.js:1483-1489,1629-1646, stripe.controller.js: multiple righe — dettaglio in §3.4). **Nessun
ricalcolo server-side esiste in nessun punto della catena.** Un utente che intercetta/modifica la richiesta
può alterare l'importo salvato sia in contabilità sia nell'addebito Stripe reale.

### 11.4 Motore di pricing interamente client-side

`front-pre/src/services/BookingCalcService.js` (~1650 righe): `getPackagePriceCalc`, `calcHDWPrice`,
`getWeekendsCount`, `getHolidayDateDays`, `getRemainingDays`, `applyCumulativePricingToFinalLocData` — un
algoritmo di pricing complesso che esiste solo nel browser. Va portato come servizio di dominio
(`IPricingEngine`) nel backend, condiviso da booking admin, prenotazione online cliente e API mobile; il
frontend può mantenere una copia "ottimistica" per UX reattiva, ma il prezzo autorevole deve provenire solo
dal server.

### 11.5 Calcolo sconto/voucher lato client, backend valida solo l'esistenza del codice

`front-pre/src/services/voucherServices.js:19-26,98-118` calcola l'importo scontato (fisso/percentuale);
l'API backend sembra validare solo che il voucher esista e restituire i suoi parametri — il calcolo
dell'importo applicato avviene nel browser e finisce direttamente nel totale salvato (§11.3). Il calcolo
tipo/valore sconto deve avvenire server-side a partire dal solo *codice* voucher inviato dal client.

### 11.6 Limiti di soggiorno per settore: validati solo client-side, l'endpoint server esiste ma è orfano

`front-pre/src/services/configureServices.js:232-280` (`_allowBookingLimitCustomer`) applica il vincolo
min/max giorni prenotabili solo lato client. Il backend possiede un endpoint equivalente
(`booking.controller.js:9526`, `checkBookingLimit`, montato su `/api/sb/check-bookinglimit`) — **verificato
con ricerca esaustiva**: nessuna chiamata a questo endpoint esiste nel frontend, né viene richiamato
internamente da `exports.booking`. Il vincolo non è enforced da nessuna parte lato server.

### 11.7 Conflitto disponibilità/double booking non atomico — coerente con §4

`checkAvailabilityRow` (booking.controller.js:4233) è un endpoint di pre-check separato, mai richiamato
internamente da `exports.booking` prima dell'`INSERT`: nessuna garanzia atomica contro prenotazioni
concorrenti sulla stessa location/data (si somma al bug di §4 che copre solo la prima location).

### 11.8 Capienza/sovrapposizione slot orari servizi verificata solo in frontend

`front-pre/src/services/BookingCalcService.js:1615-1656` (`checkAndAddTimeSlot`) — verificato: nessun
riscontro di `maximumReservation`/controllo sovrapposizione in `servicereservation.controller.js`. La
creazione della prenotazione servizio non ricontrolla capienza/sovrapposizione oraria.

### 11.9 Tetto massimo di un pagamento parziale vincolato solo da `sessionStorage`

`front-pre/src/components/reserveArticle/Modal/PaymentModal.js:122-166` — `paymentModalAmount` letto da
`sessionStorage` (alterabile via DevTools). Verificato: `createPyamentLink`
(`stripe.controller.js:2802-2920`) controlla solo `if (!req.body.amount)`, non la coerenza con il saldo
residuo reale della prenotazione.

### 11.10 Altri controlli di autorizzazione solo estetici

Pattern identico al §11.2 ripetuto in `ThePlannerNew`/`ThePlannerold`: azioni condizionate solo da
`userRole?.role !== "lifeguard"` in JSX, nessuna policy server-side equivalente.

### 11.11 Minori

- Importo scontato tenuto in `localStorage` (`voucherServices.js:8-12,21,105`) — editabile da DevTools.
- Enumerazioni di dominio (tipi sconto) hardcoded nel frontend (`Common/DiscountConstant.js`) invece di
  derivate da un'unica fonte backend.
- Selezione ambiente Stripe (dev/prod) con flag booleano hardcoded nel sorgente
  (`constants/envrionment.js:32`, `let dev = true`) invece che via variabili di build.

### 11.12 Pannello systemadmin `front-admin/` (aggiunto al repo il 2026-09-09)

SPA React 16 / Metronic 7.2.2 (progetto CRA `demo3`), **work-in-progress**. È il back-office del ruolo
`systemadmin` (login `Auth/pages/Login.js` che ammette solo `roles === "systemadmin"`). Moduli reali:
`PropertyManagement`, `ConfigurationSettings`, `SubscriptionDetails`, `Auth`, `UserProfile` (il resto
sono demo Metronic non rimosse).

> **Decisione**: il nuovo back-office **verrà riscritto da zero**. `front-admin/` non va evoluto né
> migrato — vale solo come riferimento dei requisiti (azioni + contratti API, documentati in
> [`analisi-pre/azioni/systemadmin/`](../analisi-pre/azioni/systemadmin/)). I findings qui sotto sono
> quindi **vincoli sul nuovo backend**, non fix da applicare a `front-admin/`.

Findings da recepire lato backend nella riscrittura:

- **Segreti nel bundle** (stesso pattern §11.1): `src/DecryptionService.js` — chiave AES-256-GCM
  hardcoded per decifrare **tutte** le risposte di `config.js#Api` (*security theatre*: sostituire con
  solo TLS); `ApproverdProperties.js` — seconda chiave AES `crypto-js` `"SB_USER_DATA"` per cifrare il
  payload di "accesso diretto"; `ResetPassword.js` — chiave `cryptr` `"myTotalySecretKey"` per
  decifrare l'email dal token in URL.
- **Base URL hardcoded e incoerenti**: `config.js` → `https://api.summerbooking.it/api`;
  `Auth/_redux/authCrud.js` login → `https://development.summerbooking.it/api/...`;
  `ConfigSettingApicalls.js` ricostruisce un terzo `API_PATH` hardcoded; "accesso diretto" →
  `https://<...>.devtunnels.ms/...` + apertura di `http://localhost:5001/home`. Tutto da portare a
  configurazione d'ambiente.
- **Nuova scadenza abbonamento calcolata lato client** (variante amministrativa di §11.3/§11.4):
  `PropertyManagement/PropertyDetails.js` calcola nel browser i giorni residui (`datediff`, gestione
  `subscriptionStatus`/`subscriptionDisableDate`) e invia `additionalDays` (`"+N"`/`"-N"`) a
  `sb/updateproperty`; il backend applica `endDate ± N` **senza ricalcolo** (vedi
  [`analisi-pre/business-logic/systemadmin/01-gestione-stabilimenti.md`](../analisi-pre/business-logic/systemadmin/01-gestione-stabilimenti.md) §4.2).
  Anche `licenseMonths` arriva dal client (tendina popolata da `public/licensetype`), non verificato
  contro `licenses.months`.
- **Impersonation via `auth/send-token`**: vedi §7 (Sicurezza — Critica) — la funzione "accesso diretto"
  del pannello si appoggia a un endpoint che emette un JWT conoscendo solo l'email. Da riprogettare
  (token monouso server-side, scadenza breve, audit).
- **Autorizzazione parzialmente solo client**: `Login.js` filtra il ruolo lato browser; il backend
  applica `isSystemAdmin` sulla maggior parte delle rotte `sb/*` **tranne** `propertydetails`
  (controllo di ruolo commentato) — da chiudere.
- **Contratto disallineato**: `deleteproperty` dal client invia solo `{ id }` mentre il controller
  legge anche `adminComment`/`isDelete`; `SubscriptionDetails` legge il flag di esito col nome `sucess`
  (typo del backend, da normalizzare nel nuovo contratto).
- **`sb/getSubscribe` / `sb/deleteSubscribe`** montano solo `optionalAuth`: la voce "Subscription
  Details" li usa con token systemadmin ma non c'è un gate `isSystemAdmin` reale — da aggiungere.

**Nota positiva**: il backend possiede già middleware di ruolo granulari (`authJwt.js`) e un endpoint
dedicato ai limiti di prenotazione — il problema non è l'assenza di building block server-side, ma la loro
**mancata applicazione sistematica** ai percorsi critici (booking write, pricing, pagamenti). Nella
riscrittura .NET conviene partire da un "authorization/pricing gate" centralizzato che nessun endpoint di
scrittura possa bypassare, invece di controlli opt-in per singola route come nell'attuale Express app.

---

## 12. Problemi di modellazione dati trasversali

Da consolidare nel documento di modello dati (`analisi-pre/db/`, PROMPT 02) ma segnalati qui perché emersi
durante l'analisi di criticità:

- **Denaro come `FLOAT`/`STRING`** invece di `DECIMAL` (§3.7).
- **FK tipizzate in modo incoerente con la PK referenziata**: `booking_absences.booking_id` e
  `bookingItemsCount.booking_id` come `STRING`, mentre `booking.id` è `INTEGER` — join impliciti con cast,
  indice PK non sfruttato.
- **Vincoli di unicità mancanti dove servirebbero come garanzia di integrità**:
  `booking_number_informations.bookingNumber` (indice non-unique, usato con `findOne` senza `ORDER BY`),
  `bookings_send_payment_summary.stripe_session_id` (nessun indice, usato come chiave di idempotenza webhook),
  `bookingReceipt.receipt_number`/`document_no` (nessun `unique`, nonostante l'import — mai usato — di
  `sequelize-slugify`).
- **Date salvate come `STRING`**: `bookingReceipt.receipt_print_date` — impedisce range query native su un
  dato fiscale.
- **Default incoerenti col tipo dichiarato**: `bookingNumber` come `STRING` con `defaultValue: 0` (numero) e
  `allowNull: true` insieme, in tre model diversi.
- **Indici mancanti su colonne di lookup ad alta frequenza**: `booking_payments.bookingNumber` (usato in
  tutto il dominio pagamenti come chiave pubblica di ricerca, nessun indice su questa tabella).
- **`sync({ alter: true })` invece di migration versionate** su tabelle di produzione (§6).

---

## 13. Checklist di sintesi per la riscrittura .NET

Le indicazioni puntuali sono già distribuite nelle sezioni sopra; qui il riepilogo trasversale, coerente con
[linee-guida-implementative-dotnet.md](pre/linee-guida-implementative-dotnet.md),
[autenticazione-autorizzazione-dotnet.md](pre/autenticazione-autorizzazione-dotnet.md) e
[standard-risposta-api-dotnet.md](pre/standard-risposta-api-dotnet.md), che già anticipano la maggior parte
di questi punti:

1. **Nessun segreto in codice**: configurazione da secret manager, mai in `appsettings` versionato. Ruotare
   subito tutti i segreti elencati in §2, indipendentemente dai tempi della riscrittura.
2. **Secure-by-default**: `FallbackPolicy` che richiede autenticazione; nessun equivalente di
   `optionalAuth` su percorsi che toccano denaro o PII.
3. **Autorizzazione basata su risorsa, non su fiducia nell'id**: ogni comando/query su un'entità scoped
   (property/booking/utente) verifica l'ownership tramite claim del token, mai un id preso dal body senza
   controllo.
4. **Query parametrizzate come unica via**: EF Core/LINQ di default; SQL raw solo con parametri, mai
   concatenazione di stringhe — da imporre anche con analyzer in CI, non solo con disciplina.
5. **Importi finanziari sempre ricalcolati server-side**: il client propone, il server dispone; `decimal`
   ovunque, mai `float`/`string` per il denaro.
6. **Operazioni multi-step in transazione**: nessuna sequenza verifica→scrittura (disponibilità, stock,
   contatore voucher, stato pagamento→booking→scontrino) senza atomicità (lock/transazione/vincolo DB).
7. **Webhook autenticati per firma** (Stripe, Fiskaly, Telnyx, Meta), con idempotenza basata sull'id evento
   del provider, mai sullo stato derivato dell'entità applicativa.
8. **`IExceptionHandler` globale unico**: nessun percorso di codice che non risponde o che espone stack
   trace/dettagli interni al client.
9. **Async coerente**: `Task.WhenAll` esplicito su operazioni indipendenti, mai un equivalente del
   `.map(async ...)` "fire and forget"; risposta HTTP solo dopo il completamento reale.
10. **Test di architettura in CI** (NetArchTest, vedi
    [linee-guida-implementative-dotnet.md §11](pre/linee-guida-implementative-dotnet.md)) e test di
    autorizzazione per endpoint sensibile, per evitare la classe di regressione vista in §3.8 (middleware
    presenti nel codice ma silenziosamente disattivati) e in §7 (downgrade di autorizzazione documentato dal
    codice stesso).
11. **Frontend come consumer, non come fonte di verità**: pricing, sconti, limiti di prenotazione,
    disponibilità/capienza e permessi vanno rivalidati/ricalcolati lato server ad ogni scrittura, anche se
    già "controllati" in UI (§11).
12. **Un modulo per dominio** (Booking, Configuration/Catalog, Payment, Fiskaly, Identity, Notification,
    Switchboard — vedi [architettura-evolutiva-microservizi.md §2.2](pre/architettura-evolutiva-microservizi.md)),
    ciascuno col proprio schema/`DbContext`, per rendere strutturalmente più difficile la ricomparsa dei
    pattern IDOR/mass-assignment visti qui (nessun controller che scrive a piacere su tabelle di un altro
    dominio).
