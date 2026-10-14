# Tabella `location_reservations`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/locationReservation.model.js`](../../back-pre/app/models/locationReservation.model.js)
> (`freezeTableName: true`, `timestamps: false` nel commento ma **il 3° argomento è `{ indexes }`**,
> quindi Sequelize **aggiunge comunque** `createdAt`/`updatedAt` — vedi criticità; ORM
> `db.locationReservation`), controller
> [`locationreservation.controller.js`](../../back-pre/app/controllers/locationreservation.controller.js)
> (`locationReservation` → `POST /api/sb/location/reservation`, crea **una riga per giorno**
> dell'intervallo; `freeLocationReservation` → `POST /api/sb/freelocation/reservation`, cancella per
> `(locationId, DATE(startDate)>=, DATE(endDate)<=)`), lettura in
> [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js) `checkBookingDetails`
> (query SQL grezza `SELECT ... FROM location_reservations WHERE propertyId = :propertyId AND endDate
> >= :fromDate AND startDate <= :toDate`) e in
> [`helpers/common.helper.js`](../../back-pre/app/controllers/helpers/common.helper.js)
> `checkLocationReservation` (`SELECT id FROM location_reservations WHERE locationId IN (:locations)
> AND propertyId=:property_id AND (startDate<=:booking_end_date AND endDate>=:booking_start_date)`).
> Frontend `front-pre`/`front-customer-pre`: `src/services/beachmapService.js`,
> `src/components/reserveArticle/**` (marcare una postazione come "riservata" sulla mappa senza creare
> una prenotazione).
>
> **Nota di contesto** — segna una postazione come **"riservata" (blocco morbido) per un intervallo di
> date**, senza generare una prenotazione vera (nessun cliente, nessun pagamento). Usata dal
> gestionale per tenere fuori dalla vendita una postazione (manutenzione, ospite VIP, ecc.) e nel
> flusso mappa. **Una riga per (postazione × giorno)**.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### locationId
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: identificativo/etichetta postazione riservata. Confrontato per stringa con
  `booking_locations[*].Location` (`checkLocationReservation` usa `locationId IN (:locations)`).
- **Esempio di valori**: `"30"`, `"6"`

### propertyId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK). Indicizzato.
- **Esempio di valori**: 10812, 10591

### agent
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì (validato come obbligatorio in create)
- **Relazioni**: `users.id` operatore che ha creato la riserva.
- **Esempio di valori**: `"3479"`, `"1471"`

### startDate
- **Tipo**: `datetime` — inizio giornata
- **Nullabile**: Sì. Indicizzato.
- **Descrizione**: `moment(date).startOf("day")` per il singolo giorno.
- **Esempio di valori**: `2026-07-27 00:00:00`

### endDate
- **Tipo**: `datetime` — fine giornata
- **Nullabile**: Sì. Indicizzato.
- **Descrizione**: `startOf("day").endOf("day")` dello **stesso** giorno (una riga = un giorno). Un
  intervallo di N giorni produce N righe.
- **Esempio di valori**: `2026-07-27 23:59:59`

### locationData
- **Tipo**: `longtext` — oggetto JSON (metadati completi della postazione)
- **Nullabile**: Sì
- **Descrizione**: snapshot dei dati mappa della postazione (`empty_item_image`, `name`, `id` cella
  (`"R4C7"`), `items` con `id`/`name`/`it` e URL immagini S3/CloudFront, `currentRow`, `incrementId`,
  `isBooked`, `isReserved`, `isAffiliated`, `multipleBookindex`, ...). Stesse strutture di
  `bookings.packageSelectedData[].PkgData[].data`.
- **Esempio di valori** (troncato):
  `{"empty_item_image":"https://d26kxhigi9o4u2.cloudfront.net/images/configure/Umbrella/umbrella-empty.png","name":"30","id":"R4C7","items":{"id":20,"name":"Umbrella","it":"Ombrellone",...},"currentRow":"Dalla 3ª Fila in poi","incrementId":7,"isBooked":false,"isAffiliated":false,"multipleBookindex":0}`

### reservedObject
- **Tipo**: `longtext` — oggetto JSON (piccolo)
- **Nullabile**: Sì
- **Descrizione**: stato "riservato" applicato alla postazione.
- **Campi**: `Location` (`number | string`), `isBooked` (`boolean`), `isReserved` (`boolean`).
- **Esempio di valori**: `{"Location":"30","isBooked":false,"isReserved":true}`

### created_at / updated_at
- **Tipo**: `datetime` — default `NOW`
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-27 08:12:37`

### createdAt / updatedAt
- **Tipo**: `datetime` — `NOT NULL` a schema (timestamp Sequelize automatici — il model non li
  disattiva davvero, vedi criticità)
- **Nullabile**: No
- **Esempio di valori**: `2026-07-27 08:12:37`

---

## Osservazioni verificate nel codice (criticità)

- **`timestamps: false` non applicato**: nel model il 3° argomento a `sequelize.define` è
  `{ indexes: [...] }` e il 4° `{ freezeTableName, timestamps }` è **ignorato** → Sequelize aggiunge
  `createdAt`/`updatedAt` **in più** rispetto a `created_at`/`updated_at` dichiarati. Quattro colonne
  data.
- **Esplosione righe**: una riserva di N giorni → N righe (`getAllDatesInRange`). Nessun raggruppamento;
  `checkBookingDetails` poi le rilegge tutte per l'intervallo.
- **`endDate` ridondante**: essendo sempre "stesso giorno di `startDate`", non aggiunge informazione;
  serve solo a far combaciare le query per intervallo.
- **Come funziona `freeLocationReservation`** (endpoint `POST /api/sb/freelocation/reservation`):
  richiede `startDate`, `endDate`, `locationId` (validati come obbligatori) e fa una **`DELETE`
  fisica**:
  ```
  LocReservation.destroy({ where: {
    locationId,
    [Op.and]: [ DATE(startDate) >= :startDate, DATE(endDate) <= :endDate ]
  }})
  ```
  cioè cancella **tutte** le righe di quella `locationId` il cui intervallo `[startDate, endDate]`
  cade **dentro** il range passato; se `deletedCount === 0` risponde `status: false` "No record
  found", altrimenti `status: true`. Lato frontend (`ReserveArticle.js` / `NewPlannerParent.js`
  `_freeReservedLocation`) è l'azione "libera postazione riservata" sulla mappa: l'operatore clicca
  una postazione marcata "riservata" e la sblocca. Limiti: la `DELETE` **non filtra per
  `propertyId`** (usa la sola `locationId`, che è una stringa tipo `"30"` non univoca fra
  stabilimenti) → rischio di cancellare la riserva di un'altra property con stessa `locationId` nello
  stesso range; non gestisce sovrapposizioni parziali (una riserva che sborda il range non viene
  toccata); c'era una versione precedente (commentata) che cancellava per `id` — più sicura ma
  abbandonata.
- **Nessun campo di stato/scadenza**: la riserva si rimuove solo con `freeLocationReservation`
  (cancellazione fisica). Nessun soft-delete, nessun `hold_until`, nessun cron di pulizia →
  una postazione può restare "riservata" indefinitamente se nessuno la libera.
- **Confronto postazione per stringa** (`locationId` vs `Location`) — fragile con id numerici vs
  codici; `locationId` è `varchar` mentre `Location` a volte è numero.
- **`locationData` duplica metadati mappa** già presenti in `configure_maps`/`configurations` (decine
  di KB per riga).
- **Nessuna FK.** Tabella legacy `location_reservations_old` (0 righe) da ignorare.
