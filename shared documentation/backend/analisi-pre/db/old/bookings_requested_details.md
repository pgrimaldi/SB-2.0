# Tabella `bookings_requested_details`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/bookingRequested.model.js`](../../back-pre/app/models/bookingRequested.model.js)
> (`sequelize.define("bookings_requested_details", ...)`; ORM `db.bookingsRequestedDetails`), scrittura
> in [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js)
> `getBookingDetailsByReservationCode` (`bookingRequestedDetails.findOrCreate({ where: { booking_id,
> booking_request_by } })`, righe ~7024-7045) e `confirmBookingByReservationCode`; lettura via
> `LEFT JOIN bookings_requested_details ON bookings.id = bookings_requested_details.booking_id` in
> `getBookingDetailsByReservationCode` e via `INNER JOIN ... brd` in
> [`user.controller.js`](../../back-pre/app/controllers/user.controller.js) (`myBooking`,
> `mybookingsByUserId`). Frontend `front-customer-pre`: apertura di una prenotazione tramite
> `reservation_code` dal link/QR (schermate `components/consumer/**`).
>
> **Nota di contesto** — traccia **quali account consumer hanno "agganciato" una prenotazione tramite
> il codice `reservation_code`** (link o QR condiviso). Il nome è fuorviante: non sono "richieste di
> prenotazione" ma **concessioni di visibilità** ("questo utente può vedere questa prenotazione nella
> sua area personale"). **Una riga = (prenotazione × account consumer che l'ha aperta)**.

---

## Flusso

1. Una prenotazione (fatta da gestionale o online) riceve un `reservation_code` casuale (generato dal
   frontend, vedi [`bookings.md` → `reservation_code`](bookings.md)). Il gestore/cliente condivide il
   link `{basePath}/onlinebooking/consumer/{it|en}/{reservation_code}` (anche come QR generato in
   `bookingDetailsById`).
2. Un utente **loggato come consumer** apre il link. Il frontend `front-customer-pre` chiama
   `POST /api/sb/getBookingByReservationCode` (`getBookingDetailsByReservationCode`) con
   `reservation_code` + `userId` (l'id `users` del consumer autenticato).
3. Il server, per **ogni riga `bookings`** con quel `reservation_code` e `status = 1`, esegue
   `bookingRequestedDetails.findOrCreate({ where: { booking_id, booking_request_by: userId } })` →
   registra (idempotente) che quel consumer ha aperto quella prenotazione.
4. Da quel momento la prenotazione compare nell'area personale del consumer:
   `user.controller.myBooking` / `mybookingsByUserId` fanno
   `... INNER JOIN bookings_requested_details brd ON brd.booking_id = bookings.id
   WHERE ... OR brd.booking_request_by = <userId>` → l'utente vede sia le prenotazioni intestate a lui
   (`customerId` / `guest_ids`) sia quelle "agganciate" via codice.
5. Flusso **separato ma collegato** — conferma: se lo stabilimento ha
   `properties.onlineBookingOptions.locationOptionObj.requestBookingConfirmation = "true"`, la
   prenotazione online nasce con `is_booking_confirmed = 1` ("da confermare"); il consumer, dalla
   stessa pagina, chiama `POST /api/sb/confirmBookingByReservationCode` che fa
   `Booking.update({ is_booking_confirmed: 1 }, { where: { reservation_code } })` — **lo stato resta
   1** (vedi [`bookings.md` → `is_booking_confirmed`](bookings.md): la conferma non fa avanzare il
   flag, invia solo l'email "Your booking is confirmed" al cliente e allo stabilimento). La conferma
   **non** scrive su `bookings_requested_details`.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK)
- **Esempio di valori**: 172414, 169128, 193201

### booking_request_by
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `users.id` (nessuna FK) — l'utente (account consumer) che ha aperto la prenotazione
  con il codice. In `getBookingDetailsByReservationCode` è `req.body.userId`.
- **Esempio di valori**: 5983, 609, 5932

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-27 06:47:12`

---

## Osservazioni verificate nel codice (criticità)

- **`findOrCreate` senza vincolo di unicità**: la de-duplicazione su `(booking_id, booking_request_by)`
  è solo applicativa; nessun `UNIQUE` a DB → in concorrenza si possono creare doppioni.
- **Nessuna FK** verso `bookings`/`users`; `booking_request_by` può restare orfano.
- **Semantica implicita**: la sola presenza della riga = "questo utente può vedere questa
  prenotazione". Nessun campo di stato/permesso, nessuna scadenza. In riscrittura andrebbe modellata
  come relazione esplicita "partecipante alla prenotazione" con ruolo (titolare / ospite invitato /
  ecc.).
- Tabella piccola e semplice, ma centrale per la visibilità cliente delle prenotazioni condivise.
