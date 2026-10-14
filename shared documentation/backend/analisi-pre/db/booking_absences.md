# Tabella `booking_absences`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **3.208 righe**;
> `createdAt` da 2022-07-20 all'ultimo reale **2024-07-30**, `absence_date` fino a 2024-09-20 — nessun
> dato dopo l'estate 2024, conferma che la tabella è stata dismessa), model
> [`back-pre/app/models/booking-absences.model.js`](../../../../../../back-pre/app/models/booking-absences.model.js)
> (`sequelize.define("booking_absences", ...)` → ORM `db.bookinAbscences`). Ricerca in `back-pre`:
> il model è importato in
> [`booking.controller.js:31`](../../../../../../back-pre/app/controllers/booking.controller.js)
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
- **Esempio di valori** (prod): interi `1` … `10985` per 3.208 righe (molti id mancanti: la tabella
  cancellava le assenze non riprese).

### booking_id
- **Tipo**: `varchar(255)` (stringa, non `int`)
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` della prenotazione di origine (salvato come stringa).
- **Esempio di valori** (prod): `"10829"`, `"14290"`, `"6766"`, `"6729"`.

### absence_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: giorno dichiarato di non utilizzo della postazione.
- **Esempio di valori** (prod): sempre a mezzanotte, range `2022-05-11` … `2024-09-20`.

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard (il model non disattiva `timestamps`)
- **Nullabile**: Sì
- **Esempio di valori** (prod): range `2022-07-20 18:25:57` … `2024-07-30 15:05:08` (nessuna riga
  successiva).

---

## Osservazioni verificate nel codice (criticità)

- **Deprecato**: **Sì** — model registrato ma usato solo in codice commentato; nessun `INSERT`/`SELECT`
  attivo; **confermato in produzione: 3.208 righe, nessuna dopo il 2024-07-30**. Da **non migrare**
  (o migrare come storico read-only). La funzionalità è oggi interamente su
  [`booking_absence_details`](booking_absence_details.md).
- `booking_id` come `varchar` invece di `int` (nessuna FK).
- Non confondere con la tabella **`bookin-absences`** (con trattino, senza model, 91 righe, ultimi dati
  2022) — anch'essa legacy orfana; vedi
  [nota tabelle legacy in `_feature-tabelle.md`](_feature-tabelle.md).
