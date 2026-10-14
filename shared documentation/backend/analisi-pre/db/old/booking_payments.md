# Tabella `booking_payments`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/bookingPayments.model.js`](../../back-pre/app/models/bookingPayments.model.js)
> (definito con 4 argomenti a `sequelize.define` → il 4° `{ freezeTableName, timestamps }` è
> **ignorato**: il nome `booking_payments` regge solo perché già plurale, e `timestamps` **non** viene
> disattivato — da qui il doppio set di colonne data, vedi criticità), scrittura in
> [`helpers/booking.helper.js`](../../back-pre/app/controllers/helpers/booking.helper.js)
> (`bookingPayments(response, id, bookingNumber)`, righe ~1629-1660), lettura in
> [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js) `creditDetails`
> (query SQL grezza con `LEFT JOIN bookings ON bp.bookingNumber = b.bookingNumber`, righe ~2851-2933),
> `getPaidAndUnpaidBookings`, `getUnpaidBookings`, `checkCredit` (helper); associazione
> `db.bookingPayments.belongsTo(db.booking, { foreignKey: "booking_id" })` in
> [`models/index.js`](../../back-pre/app/models/index.js); frontend `front-pre`/`front-customer-pre`
> `src/services/ReservationServices.js`, schermate credito/incassi
> (`components/statistics/modules/UnpaidBooModal.js`, `AllBookings.js`).
>
> **Nota di contesto** — riga di **riepilogo economico "una per prenotazione"** (`booking_id`), derivata
> da `bookings.booking_payments` (JSON) al momento della creazione/modifica. Non è il libro-cassa dei
> singoli movimenti (quelli stanno dentro il JSON `bookings.booking_payments.paidAmountData` /
> `depositList` e, per gli stabilimenti abilitati, in [`transactions_payments`](transactions_payments.md)).
> Serve soprattutto alle viste "crediti / incassi / non pagati" per intervallo di date.
>
> **Dominio nella riscrittura**: è un dato di **stato riassuntivo della prenotazione** → resta in **C1**
> come parte della testata (chiave `booking_number`); i **movimenti** dentro `paidAmountData`/`depositList`
> vanno estratti in **C3 (Pagamenti)** come `payment_transactions`. Analisi completa in
> [`docs/03-gestione-dati.md` §4](../../docs/03-gestione-dati.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### booking_id
- **Tipo**: `int`, `NOT NULL` — default a schema `0`
- **Nullabile**: No
- **Relazioni**: `bookings.id` (associazione ORM `belongsTo`, nessuna FK a DB). Indicizzato.
- **Descrizione**: prenotazione a cui si riferisce il riepilogo. In fase di **update** la riga viene
  però aggiornata con `where: { bookingNumber }` (non per `booking_id`) — vedi criticità.
- **Esempio di valori**: 269344, 269343

### bookingNumber
- **Tipo**: `varchar(255)` — default model `0`, default a schema assente (`NULL`)
- **Nullabile**: Sì
- **Relazioni**: `bookings.bookingNumber`
- **Descrizione**: numero prenotazione condiviso; chiave usata da `creditDetails` per fare il join con
  `bookings` e da `bookingPayments()` in update.
- **Esempio di valori**: `"1079511235"`, `"1074911835"`

### property_id
- **Tipo**: `int` — `NOT NULL` nel model, `NULL` ammesso a schema
- **Nullabile**: Sì (a schema)
- **Relazioni**: `properties.id` (nessuna FK). Indicizzato.
- **Esempio di valori**: 10795, 10749

### consumer_name
- **Tipo**: `varchar(255)`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: `bookings.booking_payments`… no: valorizzato da `response.booking_consumer.name`
  (snapshot nome cliente principale).
- **Esempio di valori**: `"Oliviero "`, `"Unknown"`

### consumer_email
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: da `response.booking_consumer.email` (può essere `""` se il cliente è stato
  registrato solo con il telefono).
- **Esempio di valori**: `"ptaurelli@gkmail.com"`, `"oliviero.l@example.it"`, `""`

### total_amount
- **Tipo**: `float`
- **Nullabile**: Sì
- **Descrizione**: `= bookings.booking_payments.payableAmount` (totale dovuto). Tipo `float` → rischio di
  imprecisione su importi monetari.
- **Esempio di valori**: `110.0`, `25.0`, `14.5`

### paid_amount
- **Tipo**: `float`
- **Nullabile**: Sì
- **Descrizione**: `= payableAmount - toPay` (quanto già incassato).
- **Esempio di valori**: `0.0`, `25.0`

### pending_amount
- **Tipo**: `float`
- **Nullabile**: Sì
- **Descrizione**: `= bookings.booking_payments.toPay` (residuo da incassare).
- **Esempio di valori**: `110.0`, `0.0`

### payment_mode
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: modalità di pagamento principale, `= bookings.booking_payments.paymentMode`. Valori
  reali osservati: `NULL` (71), `"Credit card"` (21), `"Cash"` (7), `"Other"` (1).
- **Candidato a enumerazione**: **Sì** (`PaymentMode`: `Cash` / `CreditCard` / `Other`), con
  normalizzazione: in [`service_reservation_payments`](service_reservation_payments.md) lo stesso campo
  ha anche il valore drift `""` (stringa vuota). Vedi
  [`docs/03-gestione-dati.md` §2](../../docs/03-gestione-dati.md).
- **Esempio di valori**: `null`, `"Credit card"`, `"Cash"`, `"Other"`

### type
- **Tipo**: `int` — default `1` — commento DB: *"1:Booking, 2:Service"*
- **Nullabile**: Sì
- **Descrizione**: soggetto del pagamento. Nella tabella `booking_payments` è **sempre `1`** (le righe
  di tipo servizio stanno in `service_reservation_payments`, dove `type` default è `2`). Usato in
  `creditDetails` per etichettare `type: 1 //booking`.
- **Candidato a enumerazione**: **Sì** (`PaymentSubjectType`: `Booking`=1 / `ServiceReservation`=2) —
  condiviso con `service_reservation_payments.type`.
- **Esempio di valori**: `1`

### status
- **Tipo**: `tinyint(1)` — default `1` (`true`)
- **Nullabile**: Sì
- **Descrizione**: riga attiva. `creditDetails` filtra `bp.status = 1`. Valori reali: solo `1`.
- **Esempio di valori**: `1`

### startDate
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: `= bookings.booking_start_date` (parse). Usato nei filtri per intervallo di
  `creditDetails` (`bp.startDate <= :endDate`).
- **Esempio di valori**: `2026-08-20 00:00:00`

### endDate
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: `= moment(booking_end_date).endOf('day')`. Usato in `creditDetails`
  (`bp.endDate >= :startDate`).
- **Esempio di valori**: `2026-08-24 23:59:59`

### locationData
- **Tipo**: `longtext` — array JSON
- **Nullabile**: Sì
- **Descrizione**: **copia integrale** di `bookings.booking_locations` (stessa struttura: `Location`,
  `isBooked`, `isAreaBooked`, `itemName`, `type`, `packageDetails`, `booking_period`). Duplicazione di
  dati già presenti in `bookings`.
- **Esempio di valori**: vedi [`bookings.md` → `booking_locations`](bookings.md)

### created_at / updated_at
- **Tipo**: `datetime` — default `NOW`
- **Nullabile**: Sì
- **Descrizione**: coppia "logica" gestita dal model.
- **Esempio di valori**: `2026-07-27 08:19:29`

### createdAt / updatedAt
- **Tipo**: `datetime` — `NOT NULL` a schema
- **Nullabile**: No (a schema)
- **Descrizione**: coppia timestamp aggiunta automaticamente da Sequelize (il model non disabilita
  `timestamps`), **in parallelo** a `created_at`/`updated_at`. Ridondanza.
- **Esempio di valori**: `2026-07-27 08:19:29`

---

## Osservazioni verificate nel codice (criticità)

- **Doppio set di timestamp**: coesistono `created_at`/`updated_at` (dichiarati nel model) e
  `createdAt`/`updatedAt` (aggiunti da Sequelize perché `timestamps` non è disattivato). Quattro colonne
  data per lo stesso scopo.
- **Update per chiave sbagliata**: `bookingPayments(response, id, bookingNumber)` in update esegue
  `BookingPayments.update(data, { where: { bookingNumber } })` — se la prenotazione è multi-riga
  (stesso `bookingNumber`, più `booking_id`) **aggiorna tutte le righe con lo stesso riepilogo**,
  perdendo il dettaglio per singola data/postazione. In creazione invece la riga è per `booking_id`.
- **Importi in `float`**: `total_amount`/`paid_amount`/`pending_amount` sono `float` → usare
  `DECIMAL(10,2)` in riscrittura.
- **Dato derivato e potenzialmente stale**: la riga è uno snapshot di `bookings.booking_payments` al
  momento della create/update; se `bookings.booking_payments` cambia per altra via (es. webhook Stripe,
  OfSale, incassi da gestionale che riscrivono solo il JSON), `booking_payments` resta indietro.
- **`locationData` duplica `bookings.booking_locations`**: dato ridondante (decine di KB) senza
  necessità evidente.
- **Nessuna FK, nessun `ENUM`**: `payment_mode` è stringa libera con già un valore drift (`""`) nella
  tabella gemella dei servizi.
