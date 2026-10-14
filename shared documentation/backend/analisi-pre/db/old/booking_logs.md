# Tabella `booking_logs`

> Fonti: schema e dati reali (100 righe, 33 `property_id` distinti — quindi **~3 righe per property**,
> vedi criticità), model
> [`back-pre/app/models/bookingLogs.model.js`](../../back-pre/app/models/bookingLogs.model.js)
> (definito con 4 argomenti → il 4° `{ freezeTableName, timestamps }` è ignorato; nome `booking_logs`
> già plurale; `createdAt`/`updatedAt` dichiarati come attributi espliciti; ORM `db.bookingLog`), uso in
> [`helpers/common.helper.js`](../../back-pre/app/controllers/helpers/common.helper.js)
> `createBookingNumber(propertyId)` (righe ~40-68) e in
> [`helpers/booking.helper.js`](../../back-pre/app/controllers/helpers/booking.helper.js)
> `createNewBooking` (righe ~1594-1604). Nessun frontend la interroga direttamente.
>
> **Nota di contesto** — a dispetto del nome, **non è un log di audit** delle modifiche alle
> prenotazioni: è il **contatore progressivo del numero prenotazione per stabilimento**. Ogni
> stabilimento dovrebbe avere **una sola riga** il cui `booking_number` viene incrementato ad ogni
> nuova prenotazione. In pratica il codice ne crea di più (bug).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK). Indicizzato.
- **Descrizione**: stabilimento a cui si riferisce il contatore.
- **Esempio di valori**: 10611, 10795, 10749

### booking_number
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: ultimo numero prenotazione assegnato per quello stabilimento. Valore iniziale:
  `propertyId` **concatenato** alla stringa `"10000"` (es. property `10795` → `"1079510000"`), poi
  `parseInt(booking_number) + 1` ad ogni nuova prenotazione. `createBookingNumber` lo aggiorna dentro
  una transazione con `lock: transaction.LOCK.UPDATE` sulla riga. È l'origine di
  [`bookings.bookingNumber`](bookings.md) e
  [`service_reservation.bookingNumber`](service_reservation.md).
- **Esempio di valori**: `"1061129866"`, `"1079511235"` (nel campione, `MIN`..`MAX` per property
  10611 = `1061129860`..`1061129866`)

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW` (`timestamps: false`, gestiti come attributi)
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-27 08:19:28`

---

## Osservazioni verificate nel codice (criticità)

- **Righe duplicate per property**: `createBookingNumber` (helper `common`) fa `findOne` per
  `property_id`, incrementa e `save` sulla **stessa** riga se esiste — comportamento corretto. Ma
  `createNewBooking` (helper `booking`) esegue **in aggiunta** un `BookingLogs.update(...)` **oppure**
  `BookingLogs.create({ property_id, booking_number })` a seconda di `bookingNumResponse.response`: il
  nuovo `createBookingNumber` **non restituisce più** il campo `response` (ritorna solo
  `{ booking_number }`), quindi il ramo va **sempre** su `BookingLogs.create` → **una riga extra per
  ogni prenotazione**. Nel campione: 100 righe per 33 property. Codice residuo da rimuovere.
- **`booking_number` generato per concatenazione di stringhe** (`propertyId.toString() + 10000`) poi
  `parseInt + 1`: per property con conteggi molto alti i range di property adiacenti possono
  avvicinarsi; il numero non è realmente "globalmente unico per costruzione".
- **Nome fuorviante**: `booking_logs` suggerisce audit trail. In riscrittura: rinominare in
  `booking_number_sequences` (o usare una `SEQUENCE`/`IDENTITY` per-property) e separare nettamente
  l'eventuale audit log.
- **Nessun vincolo di unicità su `property_id`** (che invece dovrebbe essere la chiave logica).
