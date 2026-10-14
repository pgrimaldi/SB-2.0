# cliente finale — Prenotazione servizi e listino

> **Ambito**: prenotazione online di **servizi** (bar, prestazioni normali, prestazioni a ore) e di
> **articoli di listino** non legati a una postazione; scelta modalità di pagamento; conferma.
>
> **Metodo**: `front-customer-pre/src/components/serviceReservation/*`, `onlineReservation/*` +
> `servicereservation.controller.js` (`consumerServiceReservation`, `getServiceBookingCount`,
> `serviceReservationDeatailsByBookingNumber`, `confirmServiceBookingByBookingNumber`,
> `cancelServiceBooking`) + `booking.controller.js` (`checkHourlyServiceAvailability`,
> `getHourlyServiceBooking`) + `discounts.controller.js` (`checkdiscount`). Motore di prezzo/servizi:
> [`../stabilimento/_motore-di-prezzo-e-sconti.md`](../stabilimento/_motore-di-prezzo-e-sconti.md) §4–§5.

---

## 1. Composizione e calcolo (client)

- **Servizi bar**: `Σ (prezzo × quantità) + somma varianti selezionate`.
- **Servizi normali**: `Σ (prezzo × quantità)`.
- **Servizi a ore**: `Σ totalPrice` di ogni slot; gli slot (fascia oraria, capienza
  `maximumReservation`, sovrapposizione) sono validati **solo lato client**
  (`checkAndAddTimeSlot`); il backend **non** ricontrolla (vedi
  [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md) §11.8). `checkHourlyServiceAvailability`
  è un pre‑check informativo.
- **Sconto/voucher**: `checkdiscount` valida il codice; per i servizi l'importo scontato percentuale è
  `totale − (totale × valore / 100)`; fisso = `valore`. Calcolo lato client.

**Calcoli**: sopra. **Eventi**: nessuno in fase di composizione.

---

## 2. Creazione della prenotazione servizio

**Azione** — `POST /api/sb/service/reservation/consumer` (`consumerServiceReservation`).

**Logiche**:

- Validazioni: `propertyId`, `bathHouseId`, `bookingBy` obbligatori; per il cliente serve
  `customerData.firstName` **e `customerData.email`** (qui l'email è obbligatoria, non alternativa al
  telefono come nel flusso operatore).
- **Blacklist**: se il cliente risulta `isBlackList = 1` → blocco "User is blacklisted" (nessuna
  deroga per i servizi).
- Cerca/crea il cliente; crea la riga `service_reservation` con `servicesData` e gli importi
  **calcolati dal client** (`payments`); genera il numero prenotazione (`booking_number_informations`
  + `booking_logs`); inserisce i conteggi servizi e, se presenti, i dati servizi a ore.
- **Stripe (opzionale)**: se il body contiene un `token` di carta, crea un **customer Stripe**
  (`commonHelper.createCustomerStripe`) per addebiti futuri.

**Calcoli**: nessun ricalcolo. **Eventi**:

- **Nessuna email, nessuna notifica in‑app, nessun WhatsApp** in questo percorso consumer (a
  differenza del percorso operatore `serviceReservation`, che invia email+WhatsApp+notifica). È una
  **divergenza** da chiarire (vedi [`da-chiarire.md`](../da-chiarire.md)).
- Eventuale creazione customer Stripe.
- La conferma/riepilogo passa poi da `confirmServiceBookingByBookingNumber` /
  `serviceReservationDeatailsByBookingNumber` (pagina `/servicebooking/consumer/:language/:code`).

---

## 3. Conferma prenotazione servizio

**Azione** — `POST confirmServiceBookingByBookingNumber` (`confirmServiceBookingByBookingNumber`):
marca la prenotazione servizio confermata a partire dal numero.

**Eventi**: (analogamente al flusso postazioni) può inviare l'**email** di conferma al cliente + allo
stabilimento con `email_logs`.

---

## 4. Annullamento da parte del cliente

**Azione** — `cancelServiceBooking` (regole di cancellazione dello stabilimento).

**Logiche**: verifica la finestra temporale (`bathouses.cancellation`: "Free within 48/24/12/3
hours"); se consentito annulla e, se c'era pagamento online, avvia il **rimborso Stripe**.

**Calcoli**: differenza in ore fra adesso e l'inizio prenotazione vs soglia. **Eventi**: email di
annullamento + `email_logs`; **rimborso Stripe** se applicabile.

---

## 5. Riepilogo "calcoli / logiche / eventi"

- **Calcoli**: totali servizi bar/normali/orari; sconto servizi (`totale − totale×valore/100` o
  fisso); ore alla cancellazione vs soglia.
- **Logiche**: email cliente **obbligatoria** per i servizi consumer; blacklist senza deroga; slot
  orari validati solo client; regole di cancellazione per soglia oraria.
- **Eventi**: alla **creazione consumer**: nessuna email/notifica/WhatsApp (divergenza dal percorso
  operatore); eventuale creazione customer Stripe. Alla **conferma**: email di conferma + `email_logs`.
  Alla **cancellazione**: email annullamento + eventuale rimborso Stripe.
