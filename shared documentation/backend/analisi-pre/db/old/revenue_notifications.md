# Tabella `revenue_notifications`

> Fonti: schema e dati reali (10 righe, `createdAt` 2025-11 → 2026-02), model
> [`back-pre/app/models/revenueNotification.model.js`](../../back-pre/app/models/revenueNotification.model.js)
> (`sequelize.define("revenue_notifications", ...)` → ORM `db.revenue_notifications`), controller
> [`revenue.controller.js`](../../back-pre/app/controllers/revenue.controller.js)
> (`fetchNotificationDetails` riga 786, `saveNotificationDetails` riga 832), reset giornaliero in
> [`helpers/cron.helper.js:2103`](../../back-pre/app/controllers/helpers/cron.helper.js)
> (`cronhelper.updateNotificationCount`, schedulato da
> [`cronjob/cron-job.js:290`](../../back-pre/cronjob/cron-job.js) `updateRevenueNotificationCount`),
> routing in [`app/routes/user.routes.js:2238-2255`](../../back-pre/app/routes/user.routes.js)
> (`GET /api/sb/get-revenue-notification`, `POST /api/sb/update-notification`, entrambi con solo
> `authJwt.optionalAuth`). Logica di comportamento **quasi tutta lato frontend**:
> [`front-pre/src/components/revenue/Revenue.js`](../../front-pre/src/components/revenue/Revenue.js)
> (pannello di configurazione) e
> [`front-pre/src/components/reserveArticle/ReserveArticle.js`](../../front-pre/src/components/reserveArticle/ReserveArticle.js)
> (banner sconto sulla pagina di prenotazione).
>
> **Nota di contesto** — tabella **non censita** prima di questa scheda. In
> [`_feature-tabelle.md`](_feature-tabelle.md) è citata solo di sfuggita: *"`revenue_notifications` ha
> 'notification' nel nome ma è configurazione di **Revenue Management** — non è C6"*. È la
> **configurazione per stabilimento** della feature **Revenue Management** (prezzo suggerito dinamico +
> banner sconto sulla pagina pubblica): 1 riga per `property_id`.
>
> **Dominio di destinazione proposto**: **C2 — Catalogo & configurazione**, sottoarea `pricing` (stessa
> area di [`price_lists`](price_lists.md), che *"alimenta anche la feature Revenue Management"*).
> **Non C6**: il nome è fuorviante, non c'è messaggistica — è configurazione. La logica oggi nel
> frontend (calcolo finestra oraria, incremento contatore, decisione se mostrare il banner) va portata
> nel backend nella riscrittura.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### property_id
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: `properties.id` (nessuna FK dichiarata). Chiave logica: `findOne({ where: { property_id } })`
  in lettura e in upsert.
- **Descrizione**: stabilimento a cui si riferisce la configurazione. Di fatto **1 riga per property**
  (nessun vincolo `UNIQUE` a schema → possibili duplicati, vedi criticità).
- **Esempio di valori**: `96`, `228`, `10723`
- **Deprecato**: No

### enablePriceDiscountNotifications
- **Tipo**: `tinyint(1)` (`Sequelize.BOOLEAN`), default `false`
- **Nullabile**: Sì
- **Descrizione**: interruttore generale della feature "notifiche prezzo scontato" per lo stabilimento.
  Restituito alla UI; non filtrato dal backend.
- **Esempio di valori**: `0`, `1`
- **Candidato a enumerazione**: no — booleano (`is_price_discount_enabled`).
- **Deprecato**: No

### dailyNotificationsCount
- **Tipo**: `int`, default `0`
- **Nullabile**: Sì
- **Descrizione**: quante volte al giorno il banner/notifica va mostrato. Il frontend
  ([`Revenue.js:127-142`](../../front-pre/src/components/revenue/Revenue.js)) divide la finestra
  10:00–20:00 in `dailyNotificationsCount` slot e mostra il banner se l'ora corrente cade in uno slot e
  `dailyNotificationsCount - dailyNotificationsDisplayedCount > 0`.
- **Esempio di valori**: `0`, `1`, `2`
- **Deprecato**: No (ma tutta la logica che lo usa è lato client)

### showOnEstablishmentPage
- **Tipo**: `tinyint(1)`, default `false`
- **Nullabile**: Sì
- **Descrizione**: se `1`, il banner sconto è visibile sulla pagina pubblica dello stabilimento /
  flusso di prenotazione (`Boolean(settings.showOnEstablishmentPage)` è una delle condizioni di
  `ReserveArticle.js` / `Revenue.js`). Omonimo ma **indipendente** da `marketing_event.showOnEstablishmentPage`.
- **Esempio di valori**: `0`, `1`
- **Candidato a enumerazione**: no — booleano (`is_shown_on_public_page`).
- **Deprecato**: No

### enableDiscountsForSelectedLocations
- **Tipo**: `longtext` che contiene **JSON** (array di stringhe); il model lo dichiara `Sequelize.JSON`.
- **Nullabile**: Sì
- **Descrizione**: elenco delle celle mappa/postazioni a cui applicare lo sconto quando lo sconto non è
  esteso a tutto lo stabilimento. Il backend fa `JSON.parse` solo per restituirlo; l'applicazione dello
  sconto alle postazioni è lato client (`setRevenueDisCountLoc`).
- **Esempio di valori**: `null`, `["R12C4","R12C3","R12C2","R12C1"]`
- **Deprecato**: No

### discounts
- **Tipo**: `longtext` / `Sequelize.JSON`
- **Nullabile**: Sì
- **Descrizione**: dovrebbe contenere gli sconti configurati per la feature (percentuali/valori). In
  realtà è una **colonna morta**. Verifica incrociata completa:
  - **Backend**: `revenue.controller.js` (`fetchNotificationDetails`, `saveNotificationDetails`) **non
    legge né scrive `discounts`**; `saveNotificationDetails` fa `record.update(req.body)` sull'intero
    body, ma nessun client mette `discounts` nel body.
  - **Job/cron**: `cron.helper.updateNotificationCount` tocca solo `dailyNotificationsDisplayedCount`;
    nessun altro job la referenzia.
  - **Frontend** (`front-pre` + `front-customer-pre`): l'oggetto impostazioni costruito e inviato a
    `POST /api/sb/update-notification` — `SettingsModal.js` (`settingData`), `Revenue.js`
    (`isNotificationData = { ...settings, … }`), `ReserveArticle.js` (`{ ...revenueSettings, … }`) —
    contiene solo `dailyNotificationsCount`, `enableDiscountsForSelectedLocations`,
    `enablePriceDiscountNotifications`, `property_id`, `showOnEstablishmentPage`,
    `enableDiscountsLocFlag`, `dailyNotificationsDisplayedCount`. `discounts` **non è** nello state
    iniziale, **non è** nel mapping di risposta di `_getRevenueNotification`, **non è** nel payload di
    salvataggio. `front-app-pre` / `front-modern-pre`: nessun uso della feature notifiche revenue.
  - **Dati** (`local_sb`): `discounts` è `NULL` in **10/10** righe; unico valore distinto = `NULL`.
  Lo scoping "sconto solo su alcune postazioni" è realizzato da `enableDiscountsForSelectedLocations`
  + `enableDiscountsLocFlag`, non da questo campo.
- **Esempio di valori**: `null` (sempre)
- **Deprecato**: **Sì** — mai scritta, mai letta, mai inviata da alcun client, `NULL` ovunque.
  Da **non migrare**.
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### dailyNotificationsDisplayedCount
- **Tipo**: `int`, default `0`
- **Nullabile**: Sì
- **Descrizione**: quante volte il banner è **già stato mostrato oggi**. Incrementato **dal frontend**
  (`ReserveArticle.js:1116-1117`: `dailyNotificationsDisplayedCount: s.dailyNotificationsDisplayedCount + 1`,
  poi rimandato al backend con `POST /api/sb/update-notification`). **Azzerato ogni notte** dal cron
  `updateNotificationCount` (`UPDATE ... SET dailyNotificationsDisplayedCount = 0 WHERE ... <> 0`).
- **Esempio di valori**: `0`, `1`, `2`
- **Deprecato**: No

### enableDiscountsLocFlag
- **Tipo**: `tinyint(1)`, default `false`
- **Nullabile**: Sì
- **Descrizione**: flag che indica se lo sconto è ristretto alle postazioni di
  `enableDiscountsForSelectedLocations` (vs. tutto lo stabilimento). Solo restituito alla UI.
- **Esempio di valori**: `0`, `1`
- **Candidato a enumerazione**: no — booleano (`is_discount_limited_to_locations`).
- **Deprecato**: No

### createdAt / updatedAt
- **Tipo**: `datetime`, default `Sequelize.NOW`.
- **Nullabile**: Sì
- **Esempio di valori**: `2025-11-12 05:41:04`, `2026-04-19 20:00:00`

---

## Osservazioni verificate nel codice (criticità)

- **`saveNotificationDetails` — bug nell'`INSERT`**: sul ramo "record non esistente" fa
  `RevenueNotifications.create({ property_id, updateData })` — annida l'intero body in una colonna
  inesistente `updateData` invece di fare lo spread. La prima configurazione di una property o fallisce
  o crea una riga incompleta; funziona in pratica solo il ramo `record.update(updateData)`.
- **`saveNotificationDetails` — risposta sempre vuota**: sul ramo update `newRecord` resta `undefined`,
  quindi la risposta è `{ message: "...", data: undefined }` anche quando il salvataggio riesce.
- **Upsert non atomico e nessun `UNIQUE(property_id)`**: `findOne` + `create`/`update` in due passi →
  richieste concorrenti possono creare righe duplicate per la stessa property.
- **Body salvato tale e quale**: `record.update(req.body)` senza whitelist dei campi → un client può
  scrivere qualunque colonna del model (incluso `dailyNotificationsDisplayedCount`, `id`).
- **Logica di business nel frontend**: finestra oraria 10–20, suddivisione in slot, incremento del
  contatore "mostrato", decisione di visibilità del banner sono tutte in `Revenue.js` /
  `ReserveArticle.js`. Nella riscrittura vanno server-side (il client non deve poter falsare il conteggio
  scrivendo `dailyNotificationsDisplayedCount`).
- **`discounts` è colonna morta** (verificato su backend, cron e tutti i frontend — vedi campo sopra):
  nessuno la scrive/legge, `NULL` in tutte le righe. **Da non migrare.**
- **Naming fuorviante**: `*_notifications` / `showOnEstablishmentPage` fanno pensare a C6, ma non c'è
  invio di nulla — è configurazione di pricing/Revenue (C2).
- **Endpoint senza autorizzazione**: `get-revenue-notification` e `update-notification` montano solo
  `authJwt.optionalAuth`; `fetchNotificationDetails` maschera i valori di disponibilità solo se
  `req.user == null` (`maskDecimal`), ma la scrittura è aperta.
