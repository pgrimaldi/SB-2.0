# cliente-finale — Pagamento

> Area funzionale: checkout della prenotazione (postazioni / servizi / eventi) con pagamento online
> tramite Stripe (Connect verso l'account dello stabilimento), gestione carte salvate, acconto/saldo,
> pagamento in struttura, ricevuta e conferma.
> **Schermate**: **non disegnate nei mockup**. Nel mockup si vede solo la CTA "Prenota ora" e la
> struttura del prezzo ("A partire da 20,00 €"). Tutte le firme qui sono **ipotizzate**, basate sugli
> endpoint oggi usati da `front-customer-pre` (`sb/payment`, `sb/capturePayment`, `sb/confirm-payment`,
> `sb/cardTransaction`, `sb/getCardDetails`, `sb/deleteUserCard`, `sb/find-connected-account`,
> `sb/getPaymentOptions`, `sb/TransactionEntryForPayAtProperty`, `sb/checkout-booking`).
>
> Logica di business correlata: [`business-logic/systemadmin/01-gestione-stabilimenti.md`](../../business-logic/systemadmin/01-gestione-stabilimenti.md)
> §4.1 (commissione: totale se paghi tutto, metà se acconto, 0 se in struttura) e
> [`db/properties.md#commissionamount`](../../db/properties.md).

---

## 1. Apertura del riepilogo di checkout

**Descrizione**: dopo aver creato la prenotazione in bozza (`sb/booking`) l'utente vede il riepilogo:
voci, subtotale, commissione, sconto, totale, e le modalità di pagamento ammesse.
**Schermata**: non nei mockup → firma ipotizzata.
**Ruolo**: ospite (previo login/registrazione) o cliente autenticato.

**Input → API** (`sb/bookingSummaryById` / `sb/checkout-booking`)
- `bookingId` (int) **o** `reservationCode` (string)
- `userId` (int, opzionale)

**Output ← API**
- `summary` ({ propertyName, items[], checkIn, checkOut, seasonal, guestContact })
- `pricing` ({ subtotal, discountAmount, commissionPercent, commissionAmount, total, currency })
- `paymentOptions` (array) — `[ { key: "full"|"deposit"|"atProperty", label, amountDue,
  commissionAmount } ]`
- `depositPercent` (number)
- `holdExpiresAt` (datetime)
- `stripeAccountId` (string) — account Connect dello stabilimento (da `sb/find-connected-account`)

---

## 2. Scelta della modalità di pagamento

**Descrizione**: l'utente sceglie fra pagamento **intero**, **acconto** o **in struttura**; l'importo
dovuto e la commissione si aggiornano.
**Schermata**: non nei mockup → firma ipotizzata.
**Ruolo**: ospite o cliente autenticato.

**Input → API** (`sb/getPaymentOptions`)
- `bookingId` (int)
- `paymentOption` (string) — `"full"|"deposit"|"atProperty"`

**Output ← API**
- `amountDueNow` (number) — importo da pagare adesso (0 se "atProperty")
- `amountDueAtProperty` (number) — saldo da pagare in loco
- `commissionAmount` (number) — calcolata sul totale (full), sulla metà (deposit) o 0 (atProperty)
- `currency` (string)

---

## 3. Elenco delle carte salvate

**Descrizione**: il cliente loggato vede le proprie carte già memorizzate per pagare più in fretta.
**Schermata**: non nei mockup → firma ipotizzata (endpoint `getCardDetails`).
**Ruolo**: cliente autenticato.

**Input → API** (`sb/getCardDetails`)
- `userId` (int)

**Output ← API**
- `cards` (array) — `{ id, brand, last4, expMonth, expYear, isDefault }`

---

## 4. Rimozione di una carta salvata

**Descrizione**: il cliente elimina una carta dal proprio profilo.
**Schermata**: non nei mockup → firma ipotizzata (endpoint `deleteUserCard`).
**Ruolo**: cliente autenticato.

**Input → API** (`sb/deleteUserCard`)
- `userId` (int)
- `cardId` (string)

**Output ← API**
- `status` / `message`

---

## 5. Avvio del pagamento online (creazione PaymentIntent)

**Descrizione**: l'utente conferma il pagamento; il server crea l'intento di pagamento Stripe verso
l'account dello stabilimento, con la commissione come application fee.
**Schermata**: non nei mockup → firma ipotizzata (endpoint `payment` / `cardTransaction`).
**Ruolo**: ospite o cliente autenticato.

**Input → API** (`sb/payment`)
- `bookingId` (int) / `bookingType` (string) — `"location"|"service"|"event"`
- `paymentOption` (string) — `"full"|"deposit"`
- `amount` (number) / `currency` (string)
- `savedCardId` (string, opzionale) — se paga con carta salvata
- `saveCard` (bool) — se salvare la nuova carta
- `billing` ({ name, email, address? })
- `isIframe` (bool)
- `returnUrl` (string) — per redirect 3-D Secure

**Output ← API**
- `paymentIntentId` (string)
- `clientSecret` (string) — per Stripe.js lato client
- `requiresAction` (bool) — se serve 3-D Secure
- `publishableKey` (string) — chiave Stripe (per account/piattaforma)
- `applicationFeeAmount` (number) — commissione trattenuta dalla piattaforma

---

## 6. Conferma / cattura del pagamento

**Descrizione**: completata l'autenticazione carta, il client notifica al server di finalizzare;
la prenotazione passa a "confermata" e viene emessa la ricevuta.
**Schermata**: non nei mockup → firma ipotizzata (endpoint `confirm-payment` / `capturePayment` /
`confirmBookingByReservationCode`).
**Ruolo**: ospite o cliente autenticato.

**Input → API** (`sb/confirm-payment`)
- `paymentIntentId` (string)
- `bookingId` (int) / `reservationCode` (string)

**Output ← API**
- `status` (string) — `"succeeded"|"processing"|"failed"`
- `booking` ({ reservationCode, status: "confirmed", pricing, amountPaid, amountDueAtProperty })
- `receiptUrl` (string)
- `confirmationEmailSent` (bool)

---

## 7. Registrazione dell'intenzione di pagare in struttura

**Descrizione**: se l'utente sceglie "paga in struttura", la prenotazione è confermata senza incasso
online (commissione 0) e si registra l'importo da saldare in loco.
**Schermata**: non nei mockup → firma ipotizzata (endpoint `TransactionEntryForPayAtProperty`).
**Ruolo**: ospite o cliente autenticato.

**Input → API** (`sb/TransactionEntryForPayAtProperty`)
- `bookingId` (int)
- `contact` ({ firstName, lastName, email, phone })

**Output ← API**
- `status` (string) — `"confirmed"`
- `booking` ({ reservationCode, amountDueAtProperty, currency })
- `confirmationEmailSent` (bool)

---

## 8. Recupero della ricevuta / dati fiscali della prenotazione

**Descrizione**: dopo la conferma l'utente scarica/visualizza la ricevuta.
**Schermata**: non nei mockup → firma ipotizzata (endpoint `get-receipt-data`).
**Ruolo**: ospite o cliente autenticato.

**Input → API** (`sb/get-receipt-data`)
- `bookingId` (int) / `reservationCode` (string)
- `email` (string) — verifica incrociata

**Output ← API**
- `receipt` ({ number, issuedAt, propertyName, propertyVat, lines[], taxable, vat, total, currency,
  pdfUrl })

---

## 9. Verifica che lo stabilimento possa incassare online

**Descrizione**: prima di mostrare il form di pagamento, la UI verifica che lo stabilimento abbia un
account Stripe Connect collegato e attivo.
**Schermata**: non nei mockup → firma ipotizzata (endpoint `find-connected-account`).
**Ruolo**: ospite o cliente autenticato.

**Input → API** (`sb/find-connected-account`)
- `propertyId` (int)

**Output ← API**
- `connected` (bool) — se esiste l'account
- `chargesEnabled` (bool) — se può ricevere pagamenti
- `stripeAccountId` (string)

**Note**: se `connected = false` o `chargesEnabled = false`, la UI deve forzare "paga in struttura" o
disabilitare la prenotazione online (coerente con `isAdminDisableOnLinePayment`, vedi
[`03-scheda-stabilimento.md`](03-scheda-stabilimento.md) azione 7).
