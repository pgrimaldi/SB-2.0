# Tabella `hold_locations`

> Fonti: schema (DB dev: **0 righe**), model
> [`back-pre/app/models/holdBookingLocation.model.js`](../../back-pre/app/models/holdBookingLocation.model.js)
> (`sequelize.define("hold_location", ...)` → tabella `hold_locations`; ORM `db.holdBookingLocation`),
> CRUD in [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js):
> `holdLocationBooking` (`POST /api/sb/hold-booking-locations`, righe ~8770-8811),
> `deleteHoldLocationBooking` (`POST /api/sb/delete-hold-booking-locations`),
> `getHoldLocationBooking` (`POST /api/sb/get-hold-booking-locations`).
>
> **Ricerca completa**: nessun frontend (`front-pre`, `front-customer-pre`, `front-modern-pre`,
> `front-app-pre`) chiama questi endpoint; la tabella **non** è consultata da `isLocationAvailable` né
> da `checkBookingDetails` né da alcuna logica di disponibilità. Non esiste cron di pulizia degli
> hold scaduti.
>
> **Nota di contesto** — doveva implementare il **blocco temporaneo di una postazione durante il
> checkout** (per evitare che due utenti prenotino lo stesso ombrellone mentre uno sta pagando). La
> feature è **implementata solo a metà**: esistono modello + endpoint CRUD, ma nessun consumatore e
> nessuna enforcement in fase di verifica disponibilità.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, .. (nessun dato reale)

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK)

### location
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: identificativo/etichetta della postazione da bloccare (stessa convenzione di
  `bookings.booking_locations[*].Location`).

### status
- **Tipo**: `tinyint` — default `0`
- **Nullabile**: Sì
- **Descrizione**: valorizzato da `req.body.status` (client), senza semantica documentata; presumibile
  `0` = hold attivo / `1` = rilasciato. **Mai letto.**
- **Candidato a enumerazione**: irrilevante finché la feature non è completata.

### hold_until
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: scadenza del blocco, fornita **dal client** (`req.body.hold_until`) — nessun
  ricalcolo server-side, nessun cron che la usi.

### start_date / end_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: intervallo di date per cui la postazione è bloccata.

### createdAt / updatedAt
- **Tipo**: `datetime`
- **Nullabile**: Sì (timestamp Sequelize standard)

---

## Osservazioni verificate nel codice (criticità)

- **Feature incompleta / effettivamente inattiva** — *riverificato*: `grep -rn "hold_location|
  holdBookingLocation|HoldBookingLocation|hold-booking-location"` su `back-pre` + tutti i frontend
  restituisce **solo**: il model, la registrazione in `models/index.js`, i 3 handler CRUD in
  `booking.controller.js` (`holdLocationBooking` / `deleteHoldLocationBooking` /
  `getHoldLocationBooking`) e le 3 route in `user.routes.js`. **Nessun frontend** (`front-pre`,
  `front-customer-pre`, `front-modern-pre`, `front-app-pre`) chiama quelle route; **nessuna** logica
  di disponibilità (`isLocationAvailable`, `checkBookingDetails`, `common.helper.checkLocationReservation`
  — quest'ultima a sua volta mai invocata) consulta `hold_locations`. `0` righe in DB. Da **non
  migrare** così com'è; per il blocco in checkout vedi
  [`docs/03-gestione-dati.md` §3.9](../../docs/03-gestione-dati.md).
- **`hold_until` e `status` fidati dal client**: nessuna validazione, nessun cron di scadenza →
  anche se popolata, non garantirebbe nulla.
- **`getHoldLocationBooking` cerca per `(property_id, start_date, end_date)` esatti** con `findOne` →
  non gestisce sovrapposizioni parziali di intervallo.
- **Nessuna FK, nessun `ENUM`.**
