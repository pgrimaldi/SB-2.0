# Tabella `booking_logs`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **232.526 righe** —
> ordini di grandezza più delle ~10 mila attese "1 per stabilimento": conferma su vasta scala del bug
> della riga-extra-per-prenotazione, vedi criticità), model
> [`back-pre/app/models/bookingLogs.model.js`](../../../../../../back-pre/app/models/bookingLogs.model.js)
> (definito con 4 argomenti → il 4° `{ freezeTableName, timestamps }` è ignorato; nome `booking_logs`
> già plurale; `createdAt`/`updatedAt` dichiarati come attributi espliciti; ORM `db.bookingLog`), uso in
> [`helpers/common.helper.js`](../../../../../../back-pre/app/controllers/helpers/common.helper.js)
> `createBookingNumber(propertyId)` (righe ~40-68) e in
> [`helpers/booking.helper.js`](../../../../../../back-pre/app/controllers/helpers/booking.helper.js)
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
- **Esempio di valori** (prod): `1` … `232.527` (praticamente contiguo — la tabella non viene mai
  ripulita).

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK). Indicizzato.
- **Descrizione**: stabilimento a cui si riferisce il contatore. **Non unico**: in produzione lo stesso
  `property_id` compare in migliaia di righe (una per prenotazione creata via `createNewBooking`).
- **Esempio di valori** (prod): `1`, `2`, `35`, `67` — range `1` … `10856`.

### booking_number
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: ultimo numero prenotazione assegnato per quello stabilimento. Valore iniziale:
  `propertyId` **concatenato** alla stringa `"10000"` (es. property `10795` → `"1079510000"`), poi
  `parseInt(booking_number) + 1` ad ogni nuova prenotazione. `createBookingNumber` lo aggiorna dentro
  una transazione con `lock: transaction.LOCK.UPDATE` sulla riga. È l'origine di
  [`bookings.bookingNumber`](bookings.md) e
  [`service_reservation.bookingNumber`](service_reservation.md).
- **Esempio di valori** (prod): `"210033"`, `"110017"`, `"3510451"`, `"6710134"` (property a 1–2 cifre
  producono numeri a 6–7 cifre, quelle a 4–5 cifre ne producono 9–10 → conferma il rischio di
  avvicinamento dei range fra property adiacenti).

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW` (`timestamps: false`, gestiti come attributi)
- **Nullabile**: Sì
- **Esempio di valori** (prod): range `2021-05-25 07:21:21` … `2026-09-08 09:02:09` (attiva fino
  all'export).

---

## Osservazioni verificate nel codice (criticità)

- **Righe duplicate per property**: `createBookingNumber` (helper `common`) fa `findOne` per
  `property_id`, incrementa e `save` sulla **stessa** riga se esiste — comportamento corretto. Ma
  `createNewBooking` (helper `booking`) esegue **in aggiunta** un `BookingLogs.update(...)` **oppure**
  `BookingLogs.create({ property_id, booking_number })` a seconda di `bookingNumResponse.response`: il
  nuovo `createBookingNumber` **non restituisce più** il campo `response` (ritorna solo
  `{ booking_number }`), quindi il ramo va **sempre** su `BookingLogs.create` → **una riga extra per
  ogni prenotazione**. **Confermato in produzione: 232.526 righe** a fronte di ~10 mila stabilimenti →
  la tabella è di fatto un log di creazione prenotazioni non voluto. Codice residuo da rimuovere; in
  migrazione tenere solo l'ultimo `booking_number` per `property_id`.
- **`booking_number` generato per concatenazione di stringhe** (`propertyId.toString() + 10000`) poi
  `parseInt + 1`: per property con conteggi molto alti i range di property adiacenti possono
  avvicinarsi; il numero non è realmente "globalmente unico per costruzione".
- **Nome fuorviante**: `booking_logs` suggerisce audit trail. In riscrittura: rinominare in
  `booking_number_sequences` (o usare una `SEQUENCE`/`IDENTITY` per-property) e separare nettamente
  l'eventuale audit log.
- **Nessun vincolo di unicità su `property_id`** (che invece dovrebbe essere la chiave logica).
