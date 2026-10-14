# Tabella `notification_details`

> Fonti: schema e dati reali del DB dev `summer_booking_dev` (**16 578 righe**, `AUTO_INCREMENT=16661`,
> dati dal 2022-09-06 al 2026-09-01), model
> [`back-pre/app/models/notification.model.js`](../../back-pre/app/models/notification.model.js)
> (`sequelize.define("notification_details", …)`, senza `timestamps: false` → Sequelize gestisce
> `createdAt`/`updatedAt`, che però a schema sono `datetime NULL`), registrato come `db.notification` in
> [`models/index.js`](../../back-pre/app/models/index.js) (riga 144). **Nessuna associazione ORM**,
> nessuna FK; unico indice `property_id_status (property_id, status)`.
>
> **Scrittori** — tutti passano dalla funzione interna `createNotification(data)` di
> [`notification.controller.js`](../../back-pre/app/controllers/notification.controller.js):
> - [`booking.controller.js:340`](../../back-pre/app/controllers/booking.controller.js) — avviso
>   bloccante "stai spostando una prenotazione già scontrinata" (`type 1`, `status 1`, testo = HTML
>   `<b>Attenzione</b>…`, uguale in `notification_text` e `notification_text_it`).
> - [`booking.controller.js:305/531-572`](../../back-pre/app/controllers/booking.controller.js) —
>   **prenotazione ombrellone creata** (`type 1`). `status = 1` se `response.status == 1`, altrimenti
>   `2`. Testo (gestionale, `bookingBy == 1`):
>   `"<nome consumer>, <inizio> - <fine> - <postazioni> - <accountName>"`; (online):
>   `"<nome consumer> - Online, <inizio> - <fine> - <postazioni>"`.
> - [`servicereservation.controller.js:190/273-306`](../../back-pre/app/controllers/servicereservation.controller.js) —
>   **prenotazione servizi creata** (`type 2`). `status` 1/2 come sopra da `serviceResponse.status`.
>   Testo (gestionale): `"<firstName>, <data> - <accountName>"`; (online):
>   `"<firstName> - Online, <data>"` / IT `"<firstName> - In linea, <data>"`.
> - [`stripe.controller.js:3186`](../../back-pre/app/controllers/stripe.controller.js) e
>   [`:4344`](../../back-pre/app/controllers/stripe.controller.js) — **pagamento ricevuto via link di
>   pagamento** (`type 1`, `status 1`, `reference_id`/`bookingNumber` valorizzati). Testo EN in
>   `notification_text` (`"Received payment:  amount  € <importo> , <nome> <inizio> - <fine>"`), IT in
>   `notification_text_it` (`"Ricevuto pagamento:  amount  € …"`).
> - [`helpers/fiskaly.helper.js:1640`](../../back-pre/app/controllers/helpers/fiskaly.helper.js)
>   (`checkAndNotifyPaidItemsOnUpdate`) — in aggiornamento prenotazione/servizio, quando le righe non
>   quadrano con gli importi già pagati e lo scontrino elettronico non è generabile (`type 1`,
>   `status 1`, **senza `reference_id` né `bookingNumber`**). Testo:
>   `"<baseText> - Electronic receipt data error: <errore>"` / IT
>   `"<baseText> - Errore nei dati per la generazione dello scontrino elettronico: <errore>"`.
> - [`Service/fiskalyReceiptManagerService.js:2028`](../../back-pre/app/Service/fiskalyReceiptManagerService.js)
>   (`sendFailureNotification`) — **creazione automatica della ricevuta non riuscita** (`type 1`,
>   `status 1`, **senza `reference_id` né `bookingNumber`**); invia anche una email al recapito
>   [`bathouses`](bathouses.md) della property. Testo (uguale in entrambe le colonne):
>   `"Creazione automatica della ricevuta non riuscita (<motivo>) - <nome>, <date> - <postazioni> - <accountName>"`
>   (o variante servizio, o fallback `"… - Stripe: <stripeTransactionId>"`).
>
> **Aggiornatori di `status`** (rendono visibile la notifica dopo la conferma del pagamento):
> `Notification.update({ status: 1 }, { where: { reference_id } })` in
> [`stripe.controller.js:289`](../../back-pre/app/controllers/stripe.controller.js) (`updateBooking`),
> [`:374`](../../back-pre/app/controllers/stripe.controller.js) (`updateReservation`),
> [`stripeWebhookHandler.js:616`](../../back-pre/app/controllers/stripeWebhookHandler.js) /
> [`:680`](../../back-pre/app/controllers/stripeWebhookHandler.js).
>
> **Lettori** — unico: `getNotification` (`POST /api/sb/getNotification?page=N`, `authJwt.optionalAuth`),
> body `{ property_id }` → `Notification.findAll({ where: { property_id, status: 1 }, order:
> [["createdAt","DESC"]] })`, poi **paginazione lato JS** con `slice((page-1)*10, page*10)` (se `page`
> è `undefined` restituisce l'elenco intero). Nessun'altra query legge la tabella.
>
> **Endpoint di scrittura HTTP senza chiamanti noti**: `newNotification`
> (`POST /api/sb/createNotification`, `authJwt.optionalAuth`) — scrive `notification_text`, `type`,
> `user_id`, `reference_id`, `property_id`, `bookingNumber` dal body (niente `status`, niente
> `notification_text_it`). Nessun frontend del repo lo chiama.
> `updateNotification` (`POST /api/sb/updateNotification`, `authJwt.optionalAuth`) — imposta
> `read: true`: con `id` nel body su quella riga, **senza `id` su tutte le righe della property**.
>
> **Frontend** (`front-pre` e la copia quasi identica `front-customer-pre`; le action Redux
> `getNotification`/`updateNotification` stanno in
> [`data/actions/beachmapAction.js`](../../front-pre/src/data/actions/beachmapAction.js), non esiste
> una action `createNotification`):
> - la **campanella notifiche** in
>   [`components/sub_parts/AdminHeader.js`](../../front-pre/src/components/sub_parts/AdminHeader.js)
>   (+ `AdminHeaderOld.js`) e nella variante realtime
>   [`layout/header/AdminHeader/AdminHeader.js`](../../front-pre/src/layout/header/AdminHeader/AdminHeader.js):
>   solo per `role !== "consumer"` (staff/gestore). Polling ogni **30 s** (`startInterval`); la
>   variante realtime ricarica anche sull'evento socket `sendNotification`. Il badge conta le righe
>   con `read == 0` nella pagina restituita. Per il `superadmin` viene riprodotto un suono all'arrivo
>   di nuove righe, in base al fatto che il testo contenga `"Online"` e ai flag
>   `sessionUser.enableOnlineBookingSound` / `enableManagementBookingSound`.
> - la tendina in
>   [`components/account/UserNoticeficationData.js`](../../front-pre/src/components/account/UserNoticeficationData.js):
>   mostra `notification_text_it` se `languageReducer.Language === "it"`, altrimenti `notification_text`
>   (**senza fallback**). Al click: `updateNotification({ property_id, id })` (segna quella riga letta)
>   e poi naviga — `type === 1` → `/beachmap` (`state.id = reference_id`, imposta
>   `{ booking_id: reference_id, bookingNumber }`); `type === 2` →
>   `/edit-services-reservation/<reference_id>`.
> - "segna tutte come lette" (`_resetCount`) → `updateNotification({ property_id })` senza `id`.
> - **Non** consumata da `front-modern-pre` (che ha un proprio `NotificationApi` per toast antd /
>   web-push, scollegato da questa tabella) né dall'app mobile `front-app-pre` (che ha solo
>   l'interruttore on/off `users.disableNotification`, senza mai leggere questo feed).
>
> **Realtime**: il backend emette l'evento socket `sendNotification`
> (`process.env.NOTIFIATION_TRIGGER_NAME`, con "NOTIFICATION" scritto male) nella room
> `property_<propertyId>` da
> [`booking.controller.js:913`](../../back-pre/app/controllers/booking.controller.js),
> [`servicereservation.controller.js:401`](../../back-pre/app/controllers/servicereservation.controller.js),
> [`stripe.controller.js:3153/4077/4092/4311`](../../back-pre/app/controllers/stripe.controller.js)
> alla creazione di prenotazioni/servizi e all'incasso del link di pagamento. Il payload
> (`{ bookingId|reservationId, propertyId, status, message }`) **non contiene la riga notifica**:
> serve solo a far rifare la `getNotification` al client. Il client sottoscrive anche
> `notification:new`, che **nessun** punto del backend emette.

---

## Nota di contesto

`notification_details` è il **centro notifiche in-app per stabilimento** ("campanella" del pannello
gestionale): un feed di eventi operativi — *prenotazione creata*, *pagamento ricevuto*, *errore nella
generazione dello scontrino elettronico* — mostrato agli operatori di una property. **Non** è il canale
email/WhatsApp (quello è `email.helper.js` / marketing) e **non** è la notifica push del browser
(`front-modern-pre`).

Punti chiave emersi dal codice e dai dati:

- La notifica è **per property, non per destinatario**: `getNotification` filtra solo per `property_id`
  (+ `status`), mai per `user_id`. Il campo `user_id` è **scritto ad ogni riga ma mai riletto**, e
  contiene un `customers.id` (default `0`), non un `users.id` — verificato incrociando i valori
  (`7210 → customers."Unknown"`, `7320 → customers."doe johns"`, …), che non esistono in `users`.
- `status` **non è lo stato della notifica** ma un flag di **visibilità**: `1` = mostrata, `2` =
  nascosta finché la prenotazione collegata non è confermata/pagata. `getNotification` scarta `2`;
  i flussi Stripe fanno `status → 1` (`Notification.update … where reference_id`).
- `type` distingue il **soggetto**: `1` = prenotazione ombrellone/postazione ([`bookings`](bookings.md)),
  `2` = prenotazione di soli servizi ([`service_reservation`](service_reservation.md)). Nei dati
  esistono **solo** `1` (14 842) e `2` (1 736). La UI usa `type` per decidere la rotta di navigazione.
- `reference_id` è l'**id della singola riga** `bookings` / `service_reservation` (non il
  `bookingNumber`); `bookingNumber` è copiato a parte, ma solo dai percorsi più recenti (11 448 righe
  su 16 578 hanno ancora il default `'0'`).
- Nessun endpoint di cancellazione, nessun `deleted_at`, nessuna retention: la tabella cresce senza
  limiti. I dati sono concentrati su 3 stabilimenti (`property_id` 96, 496, 179 = ~99% delle righe).

**Dominio nella riscrittura**: **C6 — Comunicazione** (modulo `Notification`, vedi
[`docs/01-criticita-e-indicazioni-backend.md` §8](../../docs/01-criticita-e-indicazioni-backend.md)):
è l'**unica** tabella della feature "Notifiche in-app". Nel target va separata dal soggetto (evento di
dominio `BookingCreated` / `PaymentReceived` / `EReceiptFailed` consumato dal modulo Notification, che
materializza una notifica per destinatario), con testo reso da template localizzati (non concatenato a
mano in 6 punti), stato di lettura **per utente**, e una retention.

---

## Campi

### id
- **Tipo**: `int(11)`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### type
- **Tipo**: `int(11)` — codice del soggetto della notifica
- **Nullabile**: Sì (a schema; sempre valorizzato dai flussi reali; da `req.body.type` nell'endpoint
  `newNotification` senza chiamanti)
- **Descrizione**: `1` = prenotazione ombrellone/postazione — `reference_id` è un
  [`bookings`](bookings.md)`.id`; `2` = prenotazione di soli servizi — `reference_id` è un
  [`service_reservation`](service_reservation.md)`.id`. Determina la navigazione della UI al click
  (`/beachmap` vs `/edit-services-reservation/<id>`). Nessun altro valore nel codice né nei dati.
- **Enumeratore**: **sì** — `enum NotificationSubjectType { Booking, ServiceReservation }`; stesso
  vocabolario concettuale di [`booking_payments`](booking_payments.md)`.type` /
  [`service_reservation_payments`](service_reservation_payments.md)`.type` (`1`/`2`): da consolidare.
- **Esempio di valori**: `1` (14 842 righe), `2` (1 736 righe)

### notification_text
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci` — testo mostrato (lingua non-IT)
- **Nullabile**: Sì
- **Descrizione**: messaggio già formattato, costruito per **concatenazione** in ogni scrittore
  (nessun campo strutturato dietro). Formati osservati: `"<nome>, <inizio> - <fine> - <postazioni> - <agente>"`
  (prenotazione da gestionale), `"<nome> - Online, <inizio> - <fine> - <postazioni>"` (online),
  `"<nome>, <data> - <agente>"` (servizi), `"Received payment:  amount  € <importo> , <nome> <date>"`
  (incasso link — con la parola `amount` lasciata letterale), `"<base> - Electronic receipt data error: <dettaglio>"`
  (errore scontrino), `"Creazione automatica della ricevuta non riuscita (<motivo>) - …"`. Righe 2022:
  testo generico non tradotto. Tronca a 255 caratteri sulle righe reali (errori Fiskaly lunghi).
- **Esempio di valori**: `"doe johns - Online, 01-09-2026 - 01-09-2026 - 17"`,
  `"Test Mario Rossi 2, 19-08-2026 - fierarimini"`,
  `"Received payment:  amount  € 350 , Unknown 23-07-2026 - 29-07-2026"`,
  `"undefined, 17-08-2026 - 17-08-2026 -  - fierarimini - Electronic receipt data error: Item 7fc5732b-…: unitPrice * quantity (8) is lower than the DB paid amount (10)"`,
  `"Booking done successfully"` (righe 2022)

### notification_text_it
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci` — testo mostrato quando la lingua UI è `it`
- **Nullabile**: Sì (model `defaultValue: null`)
- **Descrizione**: nella maggior parte degli scrittori è riempito con la **stessa stringa** di
  `notification_text` (spesso già in italiano); differenze reali minime (spaziatura, trattini,
  `"Online"` → `"In linea"`). Solo gli scrittori Stripe e Fiskaly scrivono davvero una variante IT
  dedicata. La UI (`UserNoticeficationData`) mostra questo campo **senza fallback** su
  `notification_text`: sulle **11 329 righe** con valore `NULL` (tutte fino al 2025-09-26; dalle righe
  successive il campo è sempre valorizzato) un utente con lingua IT non vede alcun testo.
- **Esempio di valori**: `"doe johns - Online, 01-09-2026 - 01-09-2026 - 17"`,
  `"Ricevuto pagamento:  amount  € 12 , …"`, `"abc xyz, 26-09-2025  - 26-09-2025-34"`, `null`
- **Deprecato**: No, ma **da rifare** — la duplicazione del testo e l'assenza di fallback vanno
  sostituite da un rendering a template localizzato lato modulo Notification.

### reference_id
- **Tipo**: `int(11)` — id della riga soggetto
- **Nullabile**: Sì
- **Relazioni**: se `type = 1` → [`bookings`](bookings.md)`.id` (una singola riga, **non** il
  `bookingNumber`); se `type = 2` → [`service_reservation`](service_reservation.md)`.id`. **Nessuna FK**,
  nessuna associazione ORM. Gli spazi id delle due tabelle si **sovrappongono** (vedi criticità).
- **Descrizione**: usato dalla UI per navigare alla prenotazione/servizio e dai flussi Stripe come
  chiave dell'`update` di `status`. `NULL` (119 righe) nelle notifiche di errore Fiskaly
  (`checkAndNotifyPaidItemsOnUpdate`, `fiskalyReceiptManagerService`) che non lo passano.
- **Esempio di valori**: `26769`, `26768` (booking id, `type 1`); `2303`, `2302` (service reservation
  id, `type 2`); `null`

### bookingNumber
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`, `DEFAULT '0'` — numero prenotazione
- **Nullabile**: Sì (model `allowNull: true`, `defaultValue: 0` → salvato come stringa `'0'`)
- **Relazioni**: [`bookings`](bookings.md)`.bookingNumber` / [`service_reservation`](service_reservation.md)`.bookingNumber`
  (correlazione per valore, nessuna FK).
- **Descrizione**: passato alla UI per il redirect a `/beachmap` (`type 1`). Valorizzato solo dai
  percorsi più recenti (creazione prenotazione/servizio, incasso link); assente negli scrittori
  Fiskaly. **11 448 righe su 16 578** hanno ancora il default `'0'`.
- **Esempio di valori**: `"9624193"`, `"9624200"`, `"0"` (default)

### property_id
- **Tipo**: `int(11)` — stabilimento destinatario. Parte dell'indice `property_id_status`.
- **Nullabile**: Sì (a schema; **sempre** valorizzato: nessuna riga con `property_id` nullo)
- **Relazioni**: [`properties`](properties.md)`.id` — **nessuna FK**, nessuna associazione ORM. È
  l'**unico** criterio di targeting: tutte le notifiche di una property sono visibili a tutti i suoi
  operatori.
- **Descrizione**: filtro di `getNotification` e di `updateNotification` ("segna tutte come lette").
- **Esempio di valori**: `96` (12 949 righe), `496` (1 766), `179` (1 732), `501`, `554`, `157`, `64`

### user_id
- **Tipo**: `int(11)`
- **Nullabile**: Sì (33 righe `NULL`; default applicativo `0` quando non c'è cliente)
- **Relazioni**: **nome fuorviante** — contiene un [`customers`](customers.md)`.id` (l'intestatario
  della prenotazione, `customerId` nei controller), **non** un [`users`](users.md)`.id`. Nessuna FK.
- **Descrizione**: `req.body.user_id` nell'endpoint `newNotification`; `customerId` in tutti gli altri
  scrittori. **Mai usato in lettura**: nessuna query filtra o proietta `user_id`.
- **Esempio di valori**: `7210`, `7320`, `10346` (`customers.id`); `0`; `null`
- **Deprecato**: **parzialmente** — scritto ad ogni notifica ma mai riletto; da **non** migrare come
  colonna (il destinatario nel target sarà l'operatore, non il cliente).

### read
- **Tipo**: `tinyint(1)`, `DEFAULT 0` — flag "letta"
- **Nullabile**: Sì
- **Descrizione**: impostato a `1` da `updateNotification`. È un flag **per property, non per
  utente/sessione**: con `id` nel body segna la singola riga, senza `id` segna **tutte** le righe
  della property (usato da "segna tutte come lette" e all'apertura della campanella). Il badge della
  campanella conta `read == 0` nella pagina restituita.
- **Enumeratore**: no — booleano (`bool is_read`, meglio spostato su una tabella di stato-lettura per
  utente).
- **Esempio di valori**: `0` (3 333 righe), `1` (13 245 righe)

### status
- **Tipo**: `int(11)`, `DEFAULT 1` — flag di **visibilità** della notifica
- **Nullabile**: Sì
- **Descrizione**: `1` = mostrata da `getNotification`; `2` = **nascosta** finché la prenotazione
  collegata non è confermata/pagata (impostato dagli scrittori quando `response.status != 1`). I
  flussi Stripe (`updateBooking`/`updateReservation`, webhook incluso) fanno
  `Notification.update({ status: 1 }, { where: { reference_id } })` alla conferma del pagamento.
  Naming che collide con [`bookings`](bookings.md)`.status` / [`service_reservation`](service_reservation.md)`.status`.
  L'endpoint `newNotification` non lo imposta → default `1` (salterebbe il gating).
- **Enumeratore**: **sì (di ciclo di vita, non di dominio)** — `enum NotificationState { Visible,
  PendingConfirmation }` oppure derivarlo dallo stato della prenotazione referenziata; allinearlo al
  concetto "in attesa pagamento" di `bookings.status` / `service_reservation.status`.
- **Esempio di valori**: `1` (14 969 righe), `2` (1 609 righe)

### createdAt
- **Tipo**: `datetime`, `DEFAULT NULL` (a schema nullabile; gestito da Sequelize, sempre popolato)
- **Nullabile**: Sì (a schema) — nei dati sempre valorizzato
- **Descrizione**: momento di creazione della notifica; **unico** criterio di ordinamento di
  `getNotification` (`ORDER BY createdAt DESC`).
- **Esempio di valori**: `2022-09-06 14:22:09`, `2026-09-01 14:55:28`

### updatedAt
- **Tipo**: `datetime`, `DEFAULT NULL` (gestito da Sequelize)
- **Nullabile**: Sì (a schema) — nei dati sempre valorizzato
- **Descrizione**: ultimo aggiornamento (tipicamente il passaggio `read → 1` o `status → 1`).
- **Esempio di valori**: `2026-09-01 14:40:17`

---

## Osservazioni verificate nel codice (criticità)

- **Notifica per stabilimento, non per destinatario**: `getNotification` filtra solo `property_id`
  (+ `status`); `user_id` è **scritto ma mai letto** e per giunta contiene un `customers.id` (default
  `0`), non un `users.id`. Non esiste il concetto di "notifica indirizzata all'operatore X".
- **`read` globale per property**: `updateNotification` senza `id` fa `update({ read: true }, { where:
  { property_id } })` → il primo operatore che apre la campanella azzera il contatore per **tutti**;
  nessuno scoping per utente/sessione.
- **`status` è un flag di visibilità mascherato**: significati `1`/`2` non documentati a schema,
  naming in collisione con `bookings.status`. Reso `1` da un `update … where reference_id` **che non
  filtra `type`**: `reference_id` è un `bookings.id` per `type 1` e un `service_reservation.id` per
  `type 2` e i due spazi id **si sovrappongono** (es. esistono sia `bookings.id = 2303` sia
  `service_reservation.id = 2303`) → la conferma di un pagamento può rendere visibile in anticipo una
  notifica dell'altro tipo con lo stesso `reference_id`.
- **Notifiche di errore Fiskaly senza `reference_id` e con `bookingNumber = '0'`** (~119 righe): al
  click, `UserNoticeficationData` naviga a `/beachmap` con `state.id = undefined` (sono `type 1`) →
  pagina senza contesto. Manca un `type` dedicato per gli errori.
- **`undefined` letterale nel testo**: quando il nome consumer non è disponibile,
  `checkAndNotifyPaidItemsOnUpdate` salva a DB stringhe tipo
  `"undefined, 17-08-2026 - 17-08-2026 -  - fierarimini - Electronic receipt data error: …"`.
- **`notification_text` / `notification_text_it` quasi sempre identici**: entrambi riempiti con la
  stessa stringa dalla maggior parte degli scrittori; **11 329 righe** hanno `notification_text_it =
  NULL` e la UI non fa fallback → utenti IT senza testo. Gli scrittori Stripe scrivono EN/IT ma con la
  parola `amount` lasciata letterale (`"Received payment:  amount  € 12"`).
- **`varchar(255)` che tronca** e testo come unico dato "leggibile", costruito per concatenazione in
  **6+ punti** con format leggermente diversi (spaziature e trattini non allineati tra gestionale IT,
  online, servizi).
- **`getNotification` inefficiente**: `findAll` **senza `limit`** carica tutte le righe `status = 1`
  della property (per la property 96: ~13 000) ad ogni chiamata; la paginazione è un `slice` in JS,
  `RecordsperPage` è calcolato dopo lo slice, e con `page` `undefined` la response contiene l'intero
  elenco. Il polling è ogni 30 s per ogni scheda admin aperta.
- **`POST /api/sb/createNotification` (`newNotification`)**: nessun chiamante noto in alcun frontend
  del repo; `authJwt.optionalAuth` (nessun login richiesto) e `type`/`property_id`/`notification_text`
  presi dal body → iniezione arbitraria di notifiche in qualsiasi property. Da rimuovere.
- **`POST /api/sb/updateNotification`**: `authJwt.optionalAuth`, nessun controllo che il chiamante
  appartenga alla property indicata → `{ property_id }` basta per segnare lette tutte le sue notifiche.
- **Crescita illimitata**: nessun endpoint di delete, nessun `deleted_at`, nessuna retention;
  16 578 righe dal 2022, ~99% su 3 property.
- **Evento socket con nome scritto male**: `process.env.NOTIFIATION_TRIGGER_NAME` (manca la "C");
  il client sottoscrive anche `notification:new`, che **nessun** punto del backend emette.
- **Nessuna FK / nessuna associazione ORM**: `property_id`, `user_id`, `reference_id`, `bookingNumber`
  liberi. Unico indice `property_id_status (property_id, status)` (coerente con l'unica query).
- **Feature non consumata dal client moderno né dal mobile**: `front-modern-pre` ha un `NotificationApi`
  proprio (toast/web-push) scollegato da questa tabella; `front-app-pre` ha solo l'interruttore
  `users.disableNotification` (che il backend **non** legge mai per filtrare questo feed).
- **Righe legacy 2022** con testo generico non localizzato (`"Booking done successfully"`,
  `"Service reservation done successfully"`), `notification_text_it = NULL`, `bookingNumber = '0'`.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `type` | `int` `1`/`2` | `enum NotificationSubjectType { Booking, ServiceReservation }` — unificare con `booking_payments.type` / `service_reservation_payments.type` |
| `status` | `int` `1`/`2` | `enum NotificationState { Visible, PendingConfirmation }` (o derivato dallo stato della prenotazione) — allineare al "in attesa pagamento" di `bookings.status` / `service_reservation.status` |

`read` è un booleano (`tinyint(1)`), non un enumeratore.

---

## Ottimizzazione di `notification_text` / `notification_text_it`

**Problema.** Sono due colonne `varchar(255)` che contengono il **messaggio già renderizzato**,
costruito per concatenazione di stringhe in **6+ punti** del backend
(`booking.controller`, `servicereservation.controller`, `stripe.controller` ×2, `fiskaly.helper`,
`fiskalyReceiptManagerService`), ciascuno con un format leggermente diverso. Conseguenze già misurate:
`notification_text_it` è quasi sempre una **copia** di `notification_text` (spesso già in italiano),
`NULL` su 11 329 righe (la UI non fa fallback → utente IT senza testo), la parola `amount` resta
letterale, compare `undefined` quando manca il nome cliente, e i messaggi d'errore Fiskaly vengono
**troncati a 255 caratteri**. È i18n solo a parole: due lingue cablate, nessuna terza possibile.

**Ottimizzazione — non memorizzare il testo, memorizzare l'evento.** Sostituire le due colonne con:

| Nuova colonna | Contenuto |
|---|---|
| `kind` (`enum`) | tipo di notifica: `booking_created`, `payment_received`, `ereceipt_data_error`, `auto_receipt_failed`, `receipted_booking_move_blocked`, … (un valore per ogni punto che oggi concatena una stringa) |
| `payload` (JSON) | **solo** le variabili che servono al template, come snapshot: `{ customerName, channel, dateFrom, dateTo, locations, operator, amount, errorSummary }` — niente HTML, niente testo |

Il testo viene **renderizzato al momento della lettura** da template localizzati del modulo
`Notification` (risorse `.resx`/`.json` per lingua, §8 di
[`docs/01-…backend.md`](../../docs/01-criticita-e-indicazioni-backend.md)), con catena di fallback
`lingua richiesta → lingua default → chiave`. Vantaggi:

- **−2 colonne** e **−6 concatenazioni divergenti**: un solo template per `kind`, un solo punto di
  formattazione;
- **i18n reale**: qualunque lingua, non solo `it`/`en`; sparisce il caso `NULL` → testo vuoto;
- spariscono `undefined` letterale, `"amount"` letterale, e il **troncamento `varchar(255)`**
  (il dettaglio d'errore sta nel `payload`, il template decide come mostrarlo);
- il **wording cambia senza migrazione dati** (si cambia il template, non 16 000 righe);
- la notifica resta **auto-consistente** anche se la prenotazione referenziata cambia o è cancellata
  (lo snapshot è nel `payload`), senza dover rileggere `bookings`.

**Migrazione.** Parsing best-effort delle stringhe esistenti verso `kind` + `payload` per i pattern
noti (sono pochi); per lo storico non riconoscibile, conservare la stringa originale in una colonna
`legacy_text` mostrata as-is (sola lettura). **Se** il team vuole comunque un testo denormalizzato
(ricerca/audit): **una** sola colonna `rendered_text`, popolata al *write* dall'unico renderer di
template (non due, non concatenata a mano), nella lingua di consegna.
