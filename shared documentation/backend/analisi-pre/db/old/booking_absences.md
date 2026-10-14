# Tabella `booking_absences`

> Fonti: schema e dati reali (100 righe, ultimo `createdAt` reale **2024-07-30**), model
> [`back-pre/app/models/booking-absences.model.js`](../../back-pre/app/models/booking-absences.model.js)
> (`sequelize.define("booking_absences", ...)` → ORM `db.bookinAbscences`). Ricerca in `back-pre`:
> il model è importato in
> [`booking.controller.js:31`](../../back-pre/app/controllers/booking.controller.js)
> (`const BookingAbsences = db.bookinAbscences;`) ma **ogni riferimento a `BookingAbsences.*` è dentro
> blocchi di codice commentati** (es. `// await BookingAbsences.destroy({...})`). Nessuna scrittura né
> lettura attiva. Nessun riferimento nei frontend.
>
> **Nota di contesto** — versione **precedente e minimale** della feature "assenza pianificata",
> sostituita da [`booking_absence_details`](booking_absence_details.md) (che aggiunge
> `property_id`, `location`, `isBooked`, `booked_by`, `book_period_allowed`, `status`, `bookingNumber`).
> Conservata come storico.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### booking_id
- **Tipo**: `varchar(255)` (stringa, non `int`)
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` della prenotazione di origine (salvato come stringa).
- **Esempio di valori**: `"74845"`, `"58543"`

### absence_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: giorno dichiarato di non utilizzo della postazione.
- **Esempio di valori**: `2024-07-31 00:00:00`, `2024-08-06 00:00:00`

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard (il model non disattiva `timestamps`)
- **Nullabile**: Sì
- **Esempio di valori**: `2024-07-30 15:05:08`

---

## Osservazioni verificate nel codice (criticità)

- **Deprecato**: **Sì** — model registrato ma usato solo in codice commentato; nessun `INSERT`/`SELECT`
  attivo; ultimi dati reali metà 2024. Da **non migrare** (o migrare come storico read-only). La
  funzionalità è oggi interamente su [`booking_absence_details`](booking_absence_details.md).
- `booking_id` come `varchar` invece di `int` (nessuna FK).
- Non confondere con la tabella **`bookin-absences`** (con trattino, senza model, 91 righe, ultimi dati
  2022) — anch'essa legacy orfana; vedi
  [nota tabelle legacy in `_feature-tabelle.md`](_feature-tabelle.md).
