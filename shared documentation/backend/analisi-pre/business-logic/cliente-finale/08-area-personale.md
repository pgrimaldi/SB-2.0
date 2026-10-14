# cliente finale — Area personale

> **Ambito**: l'area riservata del cliente autenticato: le mie prenotazioni (postazioni e servizi),
> dettaglio e stato, riepilogo pagamenti, crediti maturati, annullamento, i miei preferiti, i miei
> dati.
>
> **Metodo**: `front-customer-pre/src/components/account/*`, `consumer/*` +
> `user.controller.js` (`myBookingDetails`, `mybookingsByUserId`), `booking.controller.js`
> (`bookingDetailsByCustomerId`, `customerBookings`, `getBookingDetailsByReservationCode`,
> `getCreditsEarnedByBookingId`, `cancelPackageBooking`, `deleteBooking`),
> `servicereservation.controller.js` (`reservationDetailsByCustomerId`, `cancelServiceBooking`),
> `propertylikelogs.controller.js` (`myfavourite`). Schede DB: [`bookings.md`](../../db/bookings.md),
> [`service_reservation.md`](../../db/service_reservation.md),
> [`property_like_logs.md`](../../db/property_like_logs.md).

---

## 1. Le mie prenotazioni

**Azioni** — `POST myBookingDetails` / `mybookingsByUserId`, `booking/detailsByCustomerId`,
`service/reservation/detailsByCustomerId`, `getBookingByReservationCode`.

**Logiche**:

- Recupera le prenotazioni dove il cliente è `customerId` **oppure** compare in `guest_ids`
  (`JSON_CONTAINS(guest_ids, ...)`), con `status = true`.
- **Raggruppa per `bookingNumber`**: per ciascun gruppo aggrega date, postazioni (`locationData`),
  pagamenti (`booking_payments`), servizi, stato check‑in, pass stagionale.
- Ordina per data decrescente.

**Calcoli**: aggregazione date/postazioni per prenotazione; `residuo = totale − incassato` (dagli
importi salvati). **Eventi**: nessuno.

---

## 2. Dettaglio prenotazione e stato

**Azioni** — `booking/getBookingDetailsByBookingId`, `bookingSummaryById`,
`getBookingDetailsByReservationCode` (pagina pubblica `/onlinebooking/consumer/:language/:code`),
`serviceReservationDeatailsByBookingNumber`.

**Logiche**: ricompone il riepilogo (pacchetti, servizi, prezzi, sconto, pagamenti, stato
`is_booking_confirmed` / `status`). Mostra il **codice prenotazione** e l'eventuale QR.

**Calcoli**: nessun ricalcolo. **Eventi**: nessuno.

---

## 3. Crediti maturati

**Azione** — `getCreditsEarnedByBookingId` / `getCreditLedgerDetails`.

**Logiche**: per una prenotazione mostra l'eventuale **credito** maturato dal cliente (es. giorni non
utilizzati / date di assenza che generano credito) e i movimenti collegati.

**Calcoli**: `credito da giorni non usati = giorni_non_usati × tariffa_giornaliera`; somma algebrica
dei movimenti. **Eventi**: nessuno.

---

## 4. Annullare una prenotazione

**Azioni** — `booking/cancelBooking` (`cancelPackageBooking`), `booking/deleteBooking`,
`cancelServiceBooking`.

**Logiche**:

- Verifica le **regole di cancellazione** dello stabilimento (`bathouses.cancellation`: "Free within
  48/24/12/3 hours"): `ore_a_inizio = inizio − adesso`; se `ore_a_inizio ≥ soglia` la cancellazione
  è gratuita/ammessa, altrimenti no (o con penale).
- Se ammessa: marca la prenotazione annullata; se c'era pagamento online → avvia il **rimborso
  Stripe** (`refundPayment` → `refund_transaction_details`); può ripristinare disponibilità magazzino
  e liberare le date su mappa.

**Calcoli**: differenza in ore vs soglia; importo rimborso. **Eventi**: **email di annullamento** al
cliente (IT/EN, varianti per sovrapprezzo %/fisso) + `email_logs`; **API Stripe** (refund);
aggiornamento stato prenotazione/notifica.

---

## 5. I miei preferiti

**Azione** — `myfavourite` (`propertylikelogs.controller.js`): elenco degli stabilimenti a cui il
cliente ha messo "mi piace" (`property_like_logs`). La gestione del like/unlike è in
[09-preferiti-e-condivisione](09-preferiti-e-condivisione.md).

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 6. I miei dati

Vedi [01-account-e-registrazione](01-account-e-registrazione.md) §6–§7: `getpersonalDetails`,
`updatePersonalDetails`, `deleteAccount`.

---

## 7. Riepilogo "calcoli / logiche / eventi"

- **Calcoli**: aggregazione date/postazioni per `bookingNumber`; `residuo = totale − incassato`;
  credito da giorni non usati; ore alla cancellazione vs soglia; importo rimborso.
- **Logiche**: prenotazioni dove il cliente è titolare o ospite; raggruppamento per `bookingNumber`;
  regole di cancellazione per soglia oraria.
- **Eventi**: alla **cancellazione**: email di annullamento + `email_logs`, **rimborso Stripe**,
  aggiornamento stato. Tutte le altre azioni sono di sola lettura e **non generano eventi**.
