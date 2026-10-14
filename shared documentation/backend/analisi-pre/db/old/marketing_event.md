# Tabella `marketing_event`

> Fonti: schema e dati reali del DB dev `summer_booking_dev` (**13 righe**, `AUTO_INCREMENT=4066`,
> tutte `propertyId = 96`, dati 2025-11 / 2025-12 — quasi solo prove), model
> [`back-pre/app/models/marketingEvent.js`](../../back-pre/app/models/marketingEvent.js)
> (`sequelize.define("marketing_event", …, { freezeTableName: true, timestamps: false })`;
> `createdAt`/`updatedAt` sono colonne del model con `defaultValue: Sequelize.NOW`, a schema
> `datetime NULL`), registrato come `db.marketing_event` in
> [`models/index.js`](../../back-pre/app/models/index.js) (riga 187). **Nessuna FK, nessun indice**
> oltre la PK; nessuna associazione ORM.
>
> **Scrittori** —
> [`Service/marketingEventService.js`](../../back-pre/app/Service/marketingEventService.js) via
> [`controllers/marketingEvent.controller.js`](../../back-pre/app/controllers/marketingEvent.controller.js)
> (route in [`routes/marketing.routes.js`](../../back-pre/app/routes/marketing.routes.js), **senza
> auth**; solo `GET /api/sb/events` ha `authJwt.optionalAuth`):
> - `createEvent` (`POST /api/sb/event`) — **espande `repeatType`**: genera una lista di date
>   ricorrenti (daily/weekly/monthly/yearly/custom fino a `endDate`) e crea **una riga per
>   occorrenza**, ognuna con `startDate = endDate = <data occorrenza>`; per ognuna, se `notifications[]`
>   è nel body, crea le [`marketing_event_notification`](marketing_event_notification.md).
> - `updateEvent` (`PUT /api/sb/update-event`) — aggiorna i campi; se `notifications.length > 0`,
>   `EventNotification.destroy({ where: { eventId } })` + ricrea.
> - `deleteEvent` (`DELETE /api/sb/events/:id`) — `event.update({ status: false })` (soft-delete).
> - `duplicateEvent` (`POST /api/sb/events/duplicate`) — copia (`eventName` + `"_copy"`).
>
> **Lettori**:
> - `getEventList` (`POST /api/sb/events`) — raw SQL
>   `SELECT * FROM marketing_event WHERE propertyId = '<propertyId>' AND status = true`
>   (`propertyId` **interpolato**, non parametrizzato).
> - `getEventById` (`GET /api/sb/events/:id`) — raw SQL + `JSON_ARRAYAGG` delle notifiche, `me.status = 1`.
> - `duplicateEvent` — stessa query di `getEventById`.
>
> **Frontend**:
> - gestore: **Marketing → Eventi**
>   ([`components/Marketing/Component/Events/Events.js`](../../front-pre/src/components/Marketing/Component/Events/Events.js),
>   copia in `components/newsLetter/Marketing/…`), via
>   [`data/actions/marketingActions.js`](../../front-pre/src/data/actions/marketingActions.js)
>   (`sb/event`, `sb/events`, `sb/update-event`, `sb/events/:id`, `sb/events/duplicate`,
>   `sb/events/:id` DELETE).
> - consumer:
>   [`front-customer-pre/src/components/consumer/propertyDetails/MarketingEvents.js`](../../front-customer-pre/src/components/consumer/propertyDetails/MarketingEvents.js)
>   — carosello "eventi" nella pagina pubblica dello stabilimento; chiama `POST sb/events` con
>   `{ propertyId }` e mostra **tutti** gli eventi attivi (vedi criticità su `showOnEstablishmentPage`).

---

## Nota di contesto

**Eventi / iniziative dello stabilimento** (serate, tornei, offerte a tema) con nome, periodo, orario,
immagine di copertina e descrizione, da mostrare opzionalmente sulla pagina pubblica dello
stabilimento e da promuovere opzionalmente con notifiche pianificate a una lista di contatti.

Nei dati: 13 righe, tutte `propertyId = 96`, quasi tutte prove ("evt 3", "test event 1", "winter
event"); `repeatInterval = 0` su tutte (il frontend invia `0`, il model ha default `1`); `repeatUnit`
sempre `NULL`; `notificationsEnabled` sempre `0`; `contactListId` sempre `NULL`;
`showOnEstablishmentPage = 1` su 7 righe; `status = 0` su 3 righe (soft-deleted). `AUTO_INCREMENT =
4066` per 13 righe → forte churn dovuto all'espansione delle ricorrenze in righe multiple.

**Dominio nella riscrittura**: **C6 — Comunicazione / Marketing** (l'evento è un contenuto
promozionale). La ricorrenza va tenuta come **regola** (un evento + una regola di ripetizione), non
materializzata in centinaia di righe. La visibilità sulla pagina pubblica è una proiezione verso il
modulo Catalog/Public.

---

## Campi

### id
- **Tipo**: `int(11)`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### eventName
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: nome dell'evento. Il duplicato aggiunge `"_copy"`.
- **Esempio di valori**: `"winter event"`, `"test event 1"`, `"Test - 2"`

### startDate
- **Tipo**: `datetime`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: inizio evento. **Sulle righe generate da una ricorrenza**, `startDate` è la data
  della singola occorrenza e coincide con `endDate`.
- **Esempio di valori**: `2025-12-23 18:30:00`, `2025-11-18 06:31:27`

### endDate
- **Tipo**: `datetime`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: fine evento; per le occorrenze generate è uguale a `startDate` (stesso giorno).
- **Esempio di valori**: `2025-12-23 18:30:00`

### startHour / endHour
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: orario `"HH:mm"` come stringa; `""` quando `isFullDay = 1`.
- **Esempio di valori**: `"20:16"`, `"21:19"`, `""`

### isFullDay
- **Tipo**: `tinyint(1)`, `DEFAULT 0`
- **Nullabile**: Sì
- **Descrizione**: evento "tutto il giorno" (ignora `startHour`/`endHour`).
- **Esempio di valori**: `0`, `1`

### repeatType
- **Tipo**: `enum('never','daily','weekly','monthly','yearly','custom')` `COLLATE utf8mb4_unicode_ci`,
  `DEFAULT 'never'` — **già ENUM nativo MySQL**
- **Nullabile**: Sì
- **Descrizione**: tipo di ricorrenza; `createEvent` la usa per generare le date/righe. Con
  `'custom'` servono `repeatInterval` + `repeatUnit`.
- **Enumeratore**: **sì** — `enum EventRepeatType { Never, Daily, Weekly, Monthly, Yearly, Custom }`
  (mappatura diretta).
- **Esempio di valori**: `"daily"`, `"weekly"`, `"never"`

### repeatInterval
- **Tipo**: `int(11)`, `DEFAULT 1`
- **Nullabile**: Sì
- **Descrizione**: ogni quante unità ripetere (solo con `repeatType = 'custom'`). **Nei dati reali
  sempre `0`** (il frontend invia `0` anche per i tipi non-custom) → con `custom` un valore `0`
  manderebbe `createEvent` in loop infinito (`current` non avanza).
- **Esempio di valori**: `0` (tutte le righe), `1` (default teorico), `2`

### repeatUnit
- **Tipo**: `enum('day','week','month','year')` `COLLATE utf8mb4_unicode_ci` — **già ENUM**
- **Nullabile**: Sì
- **Descrizione**: unità per `repeatType = 'custom'`. **Sempre `NULL`** nei dati.
- **Enumeratore**: **sì** — `enum EventRepeatUnit { Day, Week, Month, Year }`.
- **Esempio di valori**: `null`

### description
- **Tipo**: `text` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Esempio di valori**: `"test the test"`, `"ggghjjj"`

### coverImage
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: URL S3 dell'immagine di copertina. **`updateEvent` la aggiorna** ma il ramo
  commentato nel service originale non la gestiva; oggi passa da `req.body.coverImage`.
- **Esempio di valori**: `"https://summerbooking.s3.us-east-2.amazonaws.com/file/summer.png"`

### showOnEstablishmentPage
- **Tipo**: `tinyint(1)`, `DEFAULT 0`
- **Nullabile**: Sì
- **Descrizione**: intento "mostra questo evento sulla pagina pubblica". **Scritto dal form gestore ma
  mai usato come filtro**: `getEventList` (che alimenta il carosello consumer) restituisce **tutti**
  gli eventi con `status = true`, senza guardare questo flag; nessun frontend lo filtra.
- **Enumeratore**: no — booleano.
- **Esempio di valori**: `0`, `1`
- **Deprecato**: **Sì** — nessun percorso di lettura lo applica.

### notificationsEnabled
- **Tipo**: `tinyint(1)`, `DEFAULT 0`
- **Nullabile**: Sì
- **Descrizione**: intento "invia notifiche pianificate per questo evento". Le
  [`marketing_event_notification`](marketing_event_notification.md) vengono create ma **nessun
  processo le invia**. Sempre `0` nei dati.
- **Enumeratore**: no — booleano.
- **Esempio di valori**: `0`
- **Deprecato**: **Sì** — feature di notifica evento incompleta (vedi
  [`marketing_event_notification`](marketing_event_notification.md)).

### contactListId
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Relazioni**: [`contact_list`](contact_list.md)`.id` — **nessuna FK**. Destinatari delle notifiche
  evento. Sempre `NULL` nei dati.
- **Esempio di valori**: `null`
- **Deprecato**: **Sì** — collegato alla feature notifiche evento non implementata.

### createdAt / updatedAt
- **Tipo**: `datetime`, `DEFAULT NULL` (model `defaultValue: Sequelize.NOW`, `timestamps: false`)
- **Nullabile**: Sì (a schema)
- **Esempio di valori**: `2025-11-18 06:31:27`

### propertyId
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Relazioni**: [`properties`](properties.md)`.id` — **nessuna FK**. Scoping (interpolato in raw SQL).
- **Esempio di valori**: `96`

### status
- **Tipo**: `tinyint(1)`, `DEFAULT 1`
- **Nullabile**: Sì
- **Descrizione**: `1` = attivo (i lettori filtrano `status = true`/`1`); `0` = soft-deleted
  (`deleteEvent`). Nei dati: `1` (10) / `0` (3).
- **Enumeratore**: no — booleano/`enum ActiveState`.
- **Esempio di valori**: `1`, `0`

---

## Osservazioni verificate nel codice (criticità)

- **Route senza auth** (`marketing.routes.js`): `POST /api/sb/event`, `PUT /api/sb/update-event`,
  `DELETE /api/sb/events/:id`, `POST /api/sb/events/duplicate` non hanno alcun middleware; `propertyId`
  dal body.
- **SQL injection**: `getEventList`/`getEventById`/`duplicateEvent` interpolano `propertyId`/`id`
  nella query raw.
- **Ricorrenza materializzata in righe**: `createEvent` crea N righe (una per occorrenza), tutte con
  lo stesso contenuto; `AUTO_INCREMENT` 4066 su 13 righe. Modificare "l'evento" ne modifica solo una.
- **`repeatInterval` sempre `0`**: con `repeatType = 'custom'` il `while (true)` di `createEvent` non
  terminerebbe (nessun avanzamento di `current`).
- **`showOnEstablishmentPage` senza effetto**: il carosello pubblico mostra tutti gli eventi attivi.
- **`notificationsEnabled` + `contactListId` + [`marketing_event_notification`](marketing_event_notification.md)**:
  la catena di notifica evento non ha un mittente → feature incompleta.
- **`updateEvent` richiede `notifications` sempre presente** (`if (notifications.length > 0)` senza
  guardia su `undefined` → `TypeError` se il client non lo manda).
- Nessuna FK verso `properties` / `contact_list`.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `repeatType` | `ENUM('never','daily','weekly','monthly','yearly','custom')` | `enum EventRepeatType { Never, Daily, Weekly, Monthly, Yearly, Custom }` |
| `repeatUnit` | `ENUM('day','week','month','year')` | `enum EventRepeatUnit { Day, Week, Month, Year }` |

`isFullDay`, `showOnEstablishmentPage`, `notificationsEnabled`, `status` sono booleani (`tinyint`).
