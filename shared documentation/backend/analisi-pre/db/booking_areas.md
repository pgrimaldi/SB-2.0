# Tabella `booking_areas`

> Fonti: schema; **0 righe sia nel DB dev sia nel dump di produzione `sb_prod_dump_2026_09_08`**
> (tabella esportata solo come schema, nessun `INSERT`) → conferma definitiva che la tabella non è mai
> stata usata in produzione. Model
> [`back-pre/app/models/bookingArea.js`](../../../../../../back-pre/app/models/bookingArea.js)
> (`sequelize.define("booking_area", ...)` → tabella `booking_areas`; ORM `db.bookingArea`), unico
> writer [`helpers/booking.helper.js`](../../../../../../back-pre/app/controllers/helpers/booking.helper.js)
> `createBookingArea` (righe ~1610-1627) — **funzione mai invocata** (nessun `createBookingArea(...)`
> in tutto `back-pre`). Le letture "area" (`getBookingAreaList`, `getBookingAreaListCount` in
> [`Service/bookingService.js`](../../../../../../back-pre/app/Service/bookingService.js)) leggono invece
> [`bookings.areaDetails`](bookings.md) (colonna JSON) con `JSON_CONTAINS(areaDetails, ...)`, **non**
> questa tabella.
>
> **Nota di contesto** — doveva contenere il **conteggio dei posti prenotati per "area"** (aree a
> capienza, es. "Prato", dove si prenota un numero di posti senza posizione puntuale). La feature è
> stata implementata usando il campo JSON `bookings.areaDetails` (`areaName`, `selectedAreaCount`,
> `totalAreaCount`) e la tabella è rimasta **inutilizzata**.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, .. (nessun dato reale)

### area_name
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: nome dell'area (`areaItemsCountDetails.areaName`). Corrisponde a
  `bookings.areaDetails[].areaName`.
- **Esempio di valori**: (nessun dato) — atteso tipo `"Prato"`, `"Gazebo"`

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK)

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK)

### start_date / end_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: intervallo della prenotazione ad area.

### total_count
- **Tipo**: `int`
- **Nullabile**: Sì
- **Descrizione**: capienza totale dell'area (`areaItemsCountDetails.totalAreaCount`).

### booked_count
- **Tipo**: `int`
- **Nullabile**: Sì
- **Descrizione**: posti prenotati da questa prenotazione (`areaItemsCountDetails.selectedAreaCount`).

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard
- **Nullabile**: Sì

---

## Osservazioni verificate nel codice (criticità)

- **Deprecato / non utilizzato** — *riverificato*: `grep -rn "createBookingArea" back-pre/` restituisce
  **solo la definizione** (`helpers/booking.helper.js:1610`), **zero invocazioni**;
  `grep -rn "BookingArea\.|booking_areas|FROM booking_areas" back-pre/` non trova alcun
  `create`/`findAll`/`SELECT` sulla tabella (i match su `bookingAreaList` in `Service/bookingService.js`
  sono variabili locali che contengono il risultato di query su **`bookings`**, non su
  `booking_areas`). **`0` righe anche in produzione** (`sb_prod_dump_2026_09_08`). Da **non migrare**.
- La feature "prenotazione ad area" vive interamente nel JSON `bookings.areaDetails` + query
  `JSON_CONTAINS` in `Service/bookingService.js` (`getBookingAreaList` / `getBookingAreaListCount`) —
  con i noti limiti dei campi JSON non normalizzati (nessun indice, `Number(area.totalAreaCount)` per
  gestire valori-stringa).
- In riscrittura: se serve, modellare una vera tabella `booking_area_allocations`
  `(area_id, booking_id, date, seats)` con FK verso un'anagrafica aree del modulo Catalogo; altrimenti
  rimuovere.
