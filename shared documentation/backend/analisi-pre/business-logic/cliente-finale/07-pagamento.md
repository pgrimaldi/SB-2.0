# cliente finale — Pagamento

> **Ambito**: il checkout della prenotazione online (postazioni e servizi): scelta modalità
> ("paga ora tutto", "acconto/metà", "paga in struttura"), addebito Stripe, conferma via webhook,
> pagamento tramite link, "paga in struttura" con validazione, rimborsi.
>
> **Metodo**: `front-customer-pre/src/components/consumer/payment/*` (`CheckoutForm.js`) +
> `stripe.controller.js` (`payment`, `confirmPayment`, `capturePayment`, `createPyamentLink`,
> `createCustomerStripeDetails`, `refundPayment`) + `stripeWebhookHandler.js` +
> `onlineTransaction.controller.js` (`TransactionEntryForPayAtProperty`) +
> `Service/transactionApiService.js`. Schede DB: [`online_transaction_details.md`](../../db/online_transaction_details.md),
> [`card_transactions.md`](../../db/card_transactions.md), [`transactions_payments.md`](../../db/transactions_payments.md),
> [`bookings_send_payment_summaries.md`](../../db/bookings_send_payment_summaries.md),
> [`refund_transaction_details.md`](../../db/refund_transaction_details.md),
> [`stripe_configrations.md`](../../db/stripe_configrations.md).

---

## 1. Importo da pagare (client)

Il cliente sceglie la modalità; l'importo è già calcolato dal motore di prezzo
([04-prenotazione-postazioni](04-prenotazione-postazioni.md) §1):

- **paga ora tutto**: `toPay = totale + commissione` (commissione = `totale × % / 100 + 0,50`).
- **acconto/metà**: `toPay = totale/2 + commissione` (commissione sulla metà `+ 0,50`).
- **paga in struttura**: `toPay = 0` a Stripe; l'incasso avviene al banco.

Tutti questi valori sono composti nel browser (`CheckoutForm.js`, `PropertySignIn.js`) e inviati.

**Calcoli**: sopra. **Eventi**: nessuno in questa fase.

---

## 2. Addebito Stripe

**Azione** — `POST /api/sb/payment` (`stripe.controller.js:payment`).

**Logiche**:

- **Ricontrollo disponibilità postazione**: `isLocationStillAvailable(bookingId)` — se la postazione
  scelta non è più libera → errore "Location is no longer available" (è **l'unico ricontrollo
  server‑side** di disponibilità nel flusso di pagamento).
- Recupera la **configurazione Stripe della property** (`stripe_configrations`); se assente → errore
  "NoConfigrationFound".
- Crea un **PaymentIntent / Charge** sull'account Stripe (Connect) dello stabilimento per l'importo
  indicato dal client; l'importo **non** è ricalcolato server‑side (vedi
  [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md) §11.3).
- `confirmPayment` / `capturePayment`: confermano/catturano l'autorizzazione.
- `createCustomerStripeDetails` / `createCustomerStripe`: salvano un customer Stripe (carta) per
  addebiti futuri.

**Calcoli**: nessun ricalcolo. **Eventi**: **chiamate API Stripe**; salvataggio su
`online_transaction_details` / `card_transactions`.

---

## 3. Conferma via webhook Stripe

**Evento in ingresso** — `stripeWebhookHandler.js` su `payment_intent.succeeded` /
`checkout.session.completed`:

- **Prenotazione postazione** (`updateBooking`): `bookings.status = 1` (attiva);
  `notification_details.status = 1` per `reference_id = booking_id` (rende **visibile** la notifica
  in‑app allo stabilimento, prima creata con `status = 2`); `booking_number_informations`/count a
  `status = 1`; `updateAbsenceDetails` / `updateSharingDetails` marcano `isBooked = 1` le date di
  assenza/sharing collegate.
- **Prenotazione servizio** (`updateReservation`): `service_reservation.status = 1` +
  `notification_details.status = 1` + conteggi servizi a `status = 1`.
- **Pagamento tramite link** (`checkout.session.completed` con `payment_link`): trova il record
  `bookings_send_payment_summaries` per `stripe_session_id` e lo marca pagato
  (`updatePaymentSummaryInWebhook`).
- Possibile **notifica in‑app** dedicata (`stripe.controller.js` `createNotification`) e, se la
  property ha `fiskalyAutoreceipt`, emissione **automatica dello scontrino Fiskaly**.

**Calcoli**: nessuno. **Eventi**: aggiornamenti di stato prenotazione/notifica/conteggi;
eventuale notifica in‑app; eventuale scontrino Fiskaly automatico.

---

## 4. Pagamento tramite link (inviato dallo stabilimento)

**Azione**: il cliente riceve un link (email/SMS) creato da `createPyamentLink`
(`stripe.controller.js`), apre la pagina Stripe e paga.

**Logiche**: `createPyamentLink` controlla solo `!amount` (**non** che l'importo sia coerente col
saldo residuo — vedi [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md) §11.9); la
conferma passa dal webhook (§3).

**Calcoli**: importo deciso dallo stabilimento. **Eventi**: API Stripe; webhook → aggiornamento
`bookings_send_payment_summaries` + prenotazione.

---

## 5. "Paga in struttura" con validazione

**Azione** — `POST TransactionEntryForPayAtProperty` (`onlineTransaction.controller.js`).

**Logiche**:

- Registra la prenotazione come "da pagare in struttura" senza addebito Stripe.
- **Ricalcolo server‑side dello sconto** (uno dei pochi presenti): se `pending_amount == 0`, verifica
  che sia legittimo ricalcolando il totale scontato:
  - percentuale → `amountDifference = bookingTotal − (bookingTotal × valore / 100)`;
  - fisso → `amountDifference = bookingTotal − valore`;
  - se `amountDifference == 0` accetta il "pending 0"; altrimenti applica la validazione standard sul
    residuo.
- `bookingTotal` è letto da `booking_payments.bookingTotalPrice` (postazioni) o `payments.subTotal`
  (servizi).

**Calcoli**: ricalcolo del totale scontato server‑side (verifica). **Eventi**: scrittura su
`online_transaction_details`; nessuna chiamata Stripe.

---

## 6. Rimborsi

**Azione** — `refundPayment` / `refundPaymentController` (invocati dall'annullamento prenotazione,
lato cliente o operatore).

**Logiche**: emette il rimborso sull'account Stripe della property; registra
`refund_transaction_details`. L'entità del rimborso dipende dalle **regole di cancellazione**
(`bathouses.cancellation`: "Free within 48/24/12/3 hours") — se fuori finestra, niente rimborso.

**Calcoli**: differenza in ore fra adesso e inizio prenotazione vs soglia. **Eventi**: **API Stripe**
(refund); scrittura `refund_transaction_details`; email di annullamento (dal flusso di
cancellazione).

---

## 7. Riepilogo "calcoli / logiche / eventi"

- **Calcoli**: importo `toPay` (totale/metà + commissione + 0,50) lato client; ricalcolo del totale
  scontato server‑side solo nel "paga in struttura"; ore vs soglia di cancellazione per il rimborso.
- **Logiche**: ricontrollo disponibilità postazione in `payment`; config Stripe per property
  obbligatoria; importo **non** ricalcolato per gli addebiti Stripe; tetto del link non verificato.
- **Eventi**: **API Stripe** (PaymentIntent/Charge, capture, customer, refund, payment link);
  **webhook Stripe** → `bookings.status = 1`, `notification_details.status = 1`, conteggi, assenze/
  sharing, payment summary; eventuale **notifica in‑app** e **scontrino Fiskaly automatico**;
  scritture su `online_transaction_details` / `card_transactions` / `refund_transaction_details`.
