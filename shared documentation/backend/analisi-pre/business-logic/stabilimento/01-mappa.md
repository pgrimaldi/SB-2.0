# stabilimento — Mappa (spiaggia)

> **Voce di menù**: prima icona della sidebar di `front-pre` (`_goToMap` → sezione `Map` → componente
> `components/reserveArticle/ReserveArticle`, rotta `/beachmap`). È la **mappa interattiva delle
> postazioni**: da qui l'operatore vede lo stato della spiaggia per data e crea/gestisce le prenotazioni
> "da mappa".
>
> **Ruoli che la vedono**: tutti tranne `marketing` e `waiter` (il cameriere è reindirizzato a
> `services-reservation`). `lifeguard`/`advancedlifeguard`/`affiliate` la vedono con limitazioni
> **solo lato UI** (prezzi nascosti, postazioni non assegnate all'affiliato oscurate) — **nessun
> controllo server-side equivalente** (vedi [`roles.md`](../../db/roles.md) e
> [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md) §11.2, §11.10).
>
> **Metodo e profondità**: azioni ricavate dagli action file `beachmapAction.js` /
> `reservationAction.js` e dai controller `booking.controller.js` (~12.800 righe),
> `locationreservation.controller.js`, `servicereservation.controller.js`, `cardTransaction.controller.js`,
> `stripe.controller.js`, `helpers/fiskaly.helper.js`. La matematica del prezzo è nel documento
> trasversale [`_motore-di-prezzo-e-sconti.md`](_motore-di-prezzo-e-sconti.md). Gli endpoint di scrittura
> prenotazione sono montati con **`authJwt.optionalAuth`** (nessuna autorizzazione reale — §11.2).

---

## 1. Visualizzare la mappa per data e fascia

**Azioni**: l'operatore imposta data inizio, data fine e fascia (giornata intera / mattina /
pomeriggio) nell'header; la mappa carica lo stato delle postazioni.

**Endpoint**: `booking/bookingByDate`, `booking/getBookingsByLocation`, `getSharedLocationsByDate`,
`getAbsenceDates`, `getAbsenceLocationByDate`, `get-sharing-and-absence-dates`, `confImage` (icone
mappa), `availability/rows`, `check/location/booking`.

**Logiche**:

- Per ogni cella si determina lo **stato**: libera, occupata (prenotazione attiva nel periodo), in
  assenza (cliente via in quei giorni), condivisa (sharing), in hold (blocco temporaneo), stagionale.
- L'iterazione è su tutte le postazioni dei settori/file dello stabilimento
  (`sectors_rows`, `configure_maps`) incrociate con le prenotazioni che si sovrappongono all'intervallo.
- Le **date di assenza** liberano visivamente la cella in quei giorni e vengono scalate dal prezzo
  (vedi motore di prezzo §2.2).
- La ricerca per nome cliente nell'header usa `booking/getByName`.

**Calcoli**: nessuno (sola lettura). **Eventi**: nessuno.

---

## 2. Creare una prenotazione da una postazione libera

**Azione**: click su cella libera → si apre il pannello di prenotazione; l'operatore sceglie cliente
(o ospite), date, pacchetto/i, articoli aggiuntivi, servizi bar/normali/a ore, eventuale sconto/
voucher, modalità di pagamento; conferma.

**Endpoint principale**: `POST /api/sb/booking` (`booking.controller.js:booking` → `booking.helper.js:Booking`).
Endpoint di supporto: `checkdiscount`, `checkHourlyServiceAvailability`, `searchcustomerlist`,
`guest-list`, `getBalanceAndIdentifier` (conto prepagato).

**Logiche (in ordine, dal controller)**:

1. **Validazioni**: `propertyId`, `bookingBy` obbligatori; per ogni cliente serve `name` e almeno uno
   fra `email`/`mobile`; se ci sono `servicesData`, ogni oggetto deve avere `startDate`.
2. **Blacklist cliente**: si cercano i `customers` per email/mobile su quella property; se uno ha
   `isBlackList = 1` **e** la richiesta non ha `allowBookingForBlacklistedCustomer = true` → blocco
   "User is blacklisted".
3. **Contatto registrato con la property**: se la prenotazione **non** è fatta dall'operatore
   (`bookingBy != 1`) e la property ammette prenotazioni solo da contatti censiti dal gestore, e il
   contatto non è registrato → blocco "Only contacts registered by the property can make a booking".
4. **Anagrafica cliente/ospiti**: se il cliente non esiste viene creato in `customers`
   (`customerId`); gli ospiti aggiuntivi diventano `guest_ids`.
5. **`payment_status`**: `1` = pagato / `2` → convertito internamente a `3` (per evitare doppia
   prenotazione stessa cella/giorno).
6. **Controllo disponibilità magazzino** (`checkItemsAvailability`): per i soli **nuovi** inserimenti,
   somma le quantità richieste degli articoli inclusi nei pacchetti e verifica contro `warehouses`;
   se insufficiente → blocco "Insufficient inventory".
7. **Fiskaly (solo update)**: se si sta spostando una prenotazione **già scontrinata** su un'altra
   postazione → blocco + **notifica in‑app** "annullare prima lo scontrino"; controllo articoli già
   pagati (`checkAndNotifyPaidItemsOnUpdate`).
8. **Persistenza** (`booking.helper.js:Booking`): crea 1..N righe `bookings` (una per postazione),
   `booking_consumer`, `booking_payments`, `booking_locations`, `booking_agent` — **salvando gli importi
   calcolati dal client** (vedi motore di prezzo §7). Genera/riusa `bookingNumber`, popola
   `booking_number_informations` (`payments_new`, `details_payments_new` = line item Fiskaly,
   `quick_actions_data`).
9. **Conteggi servizi** (`serviceReservationCounts`) e **dati servizi a ore**
   (`hourlyBookingServiceData`) inseriti una sola volta (per la prima riga).
10. **Consumi magazzino**: registrazione dell'uso degli articoli (`booking_items_counts_details` /
    `additional_items_counts_details`).
11. **Pagamenti hotel/convenzione** collegati (se `hotelBooking` presente) →
    `hotelPaymentService.processHotelBookingPayments`.

**Calcoli**: tutti lato client prima dell'invio (motore di prezzo). Il server non ricalcola.

**Eventi**:

- **Notifica in‑app** (`createNotification`, `type = 1`, tabella `notification_details`): per ogni
  nuova prenotazione con `status = 1` (o `status = 2` "in attesa"), testo tipo
  `"<nome> - Online, <dd-mm-yyyy> - <dd-mm-yyyy> - <postazioni>"` (variante "da operatore" con
  `- <accountName>` invece di `- Online`). Serve a far comparire il badge notifiche nell'header e
  aggiornare la mappa in tempo reale (socket `req.io`).
- **Link di pagamento** (opzionale, `paymentSummaryId`): se l'operatore aveva predisposto un invio
  riepilogo pagamento, `updatePaymentSummary` collega la prenotazione e invia il link (email/SMS —
  vedi §4 e [05-prenotazioni](05-prenotazioni.md)).
- **Scontrino Fiskaly**: non emesso automaticamente qui; i line item vengono preparati e salvati per
  l'emissione successiva dalla Cassa (vedi [03-cassa](03-cassa.md)).
- **Nessuna email** di conferma al cliente dal percorso "da mappa" dell'operatore (l'email di conferma
  è del flusso di prenotazione online cliente).

---

## 3. Aprire una postazione occupata: scheda prenotazione e azioni rapide

**Azione**: click su cella occupata → `BookingDetailsCard` con i dati e una lista di azioni rapide.

**Endpoint**: `booking/getBookingDetailsByBookingId`, `booking/bookingSummaryById`,
`bookingPayments/detailsPayments/:bookingNumber`, `getCreditsEarnedByBookingId`,
`serviceReservationDeatailsByBookingNumber`.

Azioni disponibili dalla scheda (dettaglio nelle sezioni seguenti e in
[03-cassa](03-cassa.md)/[05-prenotazioni](05-prenotazioni.md)): **Check‑in**, **Check‑out**,
**Annulla**, **Elimina**, **Sposta**, **Aggiungi acconto/pagamento**, **Invia link di pagamento**,
**Invia riepilogo prenotazione**, **Aggiungi spese cliente**, **Sconto**, **Movimenti conto/carta**,
**Storico prenotazione**, **Note**, **Icone cliente** (`updateCustomerIcons`), **Stampa**.

---

## 4. Check‑in / Check‑out

- **Check‑in** — `POST booking/checkInBooking` (input `bookingNumber`): imposta `checkIn = true` sulla
  riga con `booking_start_date` più vicina (`limit 1`, ordine crescente). Nessun calcolo, nessun evento.
- **Check‑out** — `POST checkout-booking` (`checkOutBooking`) / `getCheckOutLocationDetails`: chiude la
  presenza; usato dalle statistiche presenze/partenze. Nessuna email; può aggiornare contatori.

---

## 5. Annullare / eliminare una prenotazione

- **Annulla** — `POST booking/cancelBooking` (`cancelPackageBooking`): applica le **regole di
  cancellazione** dello stabilimento (finestra temporale, penale) prima di procedere; se ammesso,
  marca la prenotazione annullata e, se c'era un pagamento online, avvia il **rimborso Stripe**
  (`refundPayment` → `refund_transaction_details`). **Eventi**: email di annullamento al cliente
  (IT/EN, 4 varianti a seconda di sovrapprezzo %/fisso — vedi [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md)
  §menzione template) con riga in `email_logs`; possibile ripristino disponibilità magazzino.
- **Elimina** — `POST booking/deleteBooking` (`deleteBookingByBookingId`): eliminazione secondo le
  regole di cancellazione (`deleteByManager` bypassa alcune regole); se non consentito risponde
  "You can not delete booking as per cancellation rules". Nessuna email dedicata.

**Calcoli**: eventuale penale/rimborso parziale secondo le regole di cancellazione della property.

---

## 6. Spostare una prenotazione

**Azione**: trascinare/assegnare la prenotazione a un'altra postazione (o cambiarne le date). Passa di
nuovo da `POST /api/sb/booking` in modalità update (`data[0].id` valorizzato).

**Logiche**: se la prenotazione è **già scontrinata** e si cambia postazione → blocco con notifica
in‑app (vedi §2.7). Ricalcolo prezzo lato client se cambiano date/settore. **Eventi**: notifica
in‑app; ricalcolo line item Fiskaly.

---

## 7. Pagamenti dalla mappa

Dalla scheda prenotazione:

- **Aggiungi acconto / saldo** (`AddDeposit`, `PaymentModal`): registra un incasso su
  `booking_payments` / `transactions_payments`. Il tetto del pagamento parziale è vincolato **solo da
  `sessionStorage`** lato client (§11.9): il backend controlla solo che l'importo esista.
- **Invia link di pagamento** — `POST send-link` (`stripe.controller.js:createPyamentLink`): crea una
  sessione/link Stripe per l'importo indicato e lo invia al cliente. **Eventi**: chiamata **API Stripe**;
  invio link via email/SMS; alla conferma webhook Stripe → aggiornamento pagamento + eventuale
  **notifica in‑app** (`stripe.controller.js:3186/4344`).
- **Cattura pagamento** — `capturePayment` (`reservationAction.js`): cattura un'autorizzazione Stripe
  precedente.
- **Movimenti conto/carta prepagata** — `cardTransaction`, `getMovementList`, `getBalanceAndIdentifier`,
  `deleteUserCard`, `send-card-detail` (`cardTransaction.controller.js`): gestione del "conto" cliente
  (ricariche/consumi su `card_transactions`), con invio dettagli carta al cliente (`sendCardDetails`).

**Calcoli**: importi presi dal client; saldo residuo = totale − incassato (calcolato in UI).

---

## 8. Invio riepilogo prenotazione

**Azione** — `POST sendReservationSummery` (`booking.controller.js:sendReservationSummery`): invia al
cliente il riepilogo della prenotazione via **email**.

**Logiche**: sceglie il trasportatore SMTP — se la property ha una configurazione email dedicata
(`bathhouse_email_configrations`) usa quella, altrimenti il trasportatore di default. Compone il
template riepilogo (IT/EN in base a `languageCode`).

**Eventi**: invio **email** + riga `email_logs`. Nessuna notifica in‑app.

---

## 9. Sharing (condivisione di una postazione)

**Azione** — `POST createSharing` (`booking.controller.js:createSharing`): due o più clienti si
alternano sulla stessa postazione in giorni diversi (es. uno lun‑mer‑ven, l'altro mar‑gio).

**Logiche**: valida che le date assegnate non si sovrappongano; crea/aggiorna le righe di
prenotazione con i rispettivi intervalli e registra il legame in `sharing_details`; le date di sharing
alimentano `getSharingDates`/`getSharedLocationsByDate` per la mappa.

**Calcoli**: il prezzo di ciascun partecipante è calcolato lato client sui suoi soli giorni.

**Eventi**: email di riepilogo/condivisione ai partecipanti (template dedicato) + `email_logs`.

---

## 10. Date di assenza

**Azione**: marcare i giorni in cui il cliente non sarà presente (`getAbsenceDates`,
`removeAbsenceDate`, `booking_absences` / `booking_absence_details`).

**Logiche**: le date di assenza (a) liberano la cella in quei giorni sulla mappa e la rendono
ri‑vendibile, (b) vengono **scalate dal conteggio giorni** nel ricalcolo prezzo (motore di prezzo
§2.2). L'assenza può essere legata a un "hold location" per bloccare temporaneamente la cella.

**Eventi**: nessuno (solo effetti su mappa e su prezzo al successivo ricalcolo).

---

## 11. Hold (blocco temporaneo di una postazione)

**Azione** — `holdLocationBooking` / `deleteHoldLocationBooking` / `getHoldLocationBooking`
(`hold_locations`): blocca una cella per un breve periodo mentre si completa una prenotazione, per
evitare che un altro operatore/cliente la prenda.

**Logiche**: crea una riga hold con scadenza; la mappa mostra la cella come non disponibile finché
l'hold è valido o non viene rimosso.

**Eventi**: nessuno.

---

## 12. Stampa mappa

**Azione**: `MapPrintComponent` genera la versione stampabile della mappa con lo stato del giorno.
Nessuna chiamata di scrittura, nessun evento.

---

## 13. Riepilogo "calcoli / logiche / eventi" della voce Mappa

- **Calcoli**: tutti delegati al motore di prezzo lato client (prezzo pacchetto/postazione,
  scaglioni, weekend/festivi, cumulativo multi‑postazione, servizi, sconto); saldo residuo; penale di
  cancellazione; rimborso.
- **Logiche**: stato cella per data; blacklist; contatto registrato con property; disponibilità
  magazzino; blocco spostamento di prenotazioni scontrinate; regole di cancellazione; non‑
  sovrapposizione date sharing; scadenza hold.
- **Eventi**: notifica in‑app alla creazione/spostamento; email (riepilogo prenotazione, annullamento,
  sharing) con log su `email_logs`; **API Stripe** (link/cattura/rimborso) + notifica in‑app da webhook;
  preparazione line item **Fiskaly**; aggiornamento consumi **magazzino**; socket.io per refresh mappa.
