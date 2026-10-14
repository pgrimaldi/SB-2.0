# stabilimento — Cassa

> **Voce di menù**: terza icona della sidebar (`_gotoServiceRes` → `components/serviceReservation/...`,
> label `cash_desk`, rotta `/services-reservation`). È il **banco cassa**: vendita di servizi
> (bar / articoli / prestazioni a ore), incassi, **scontrino elettronico Fiskaly**, pagamenti Stripe,
> conto/carta prepagata cliente, integrazione POS TeamSystem.
>
> **Ruoli**: visibile a tutti tranne `marketing`, `affiliate`, `lifeguard`, `advancedlifeguard`. Il
> `waiter` viene reindirizzato qui dopo il login e ha una vista dedicata.
>
> **Metodo**: endpoint da `serviceReservation.js` action file + controller
> `servicereservation.controller.js` (~1.900 righe), `booking.controller.js` (credito/pagamenti),
> `cardTransaction.controller.js`, `stripe.controller.js` (~4.500 righe), `fiskalyReceipt.controller.js`,
> `ofsale.controller.js` (TeamSystem), helper `fiskaly.helper.js`, `emailHelper`. Parte modernizzata
> dello scontrino in `front-modern-pre` (`src/app/Fiscalization`, `src/models/Fiscalization`). Prezzi:
> [`_motore-di-prezzo-e-sconti.md`](_motore-di-prezzo-e-sconti.md) §4–§5.

---

## 1. Creare una prenotazione servizio (bar / normale / a ore)

**Azione**: l'operatore registra una vendita di servizi a un cliente (o ospite): seleziona servizi
bar (con varianti), servizi normali (a quantità), servizi a ore (con slot), sceglie modalità di
pagamento, conferma.

**Endpoint**: `POST /api/sb/service/reservation` (`servicereservation.controller.js:serviceReservation`).
Supporto: `service/getServiceBookingCount`, `getHourlyServiceBooking`, `checkHourlyServiceAvailability`.

**Logiche**:

1. **Validazioni**: `propertyId`, `bathHouseId`, `bookingBy` obbligatori; per ogni cliente `firstName`
   + almeno uno fra `email`/`mobile`.
2. **Blacklist**: se il cliente risulta `isBlackList = 1` → blocco "User is blacklisted" (qui **senza**
   la deroga `allowBookingForBlacklistedCustomer` presente nel booking postazioni).
3. **Cliente**: in update (`id` presente) aggiorna l'anagrafica `customers`; in creazione cerca/crea
   il cliente e collega eventuali ospiti (`guest_ids`).
4. **`payment_status`**: default `1` (pagato); preso dal body se presente.
5. **Persistenza**: crea la riga `service_reservation` con `servicesData` (bar/normali), i dati
   cliente, gli importi **calcolati dal client** (`payments`); genera `bookingNumber`
   (`booking_number_informations` + `booking_logs`); inserisce i **conteggi servizi**
   (`services_booking_counts`) e, se presenti, i **dati servizi a ore**
   (`hourly_service_booking_management` via `addBookingManagement`).
6. **Slot orari**: capienza e sovrapposizione degli slot sono verificate **solo lato client**
   (`checkAndAddTimeSlot`); il backend non ricontrolla (vedi [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md) §11.8).
7. **Transazioni pagamento**: `transactionsPaymentsService.processServiceReservationTransactions`
   popola `transactions_payments` (asincrono, non bloccante).

**Calcoli**: totali servizi (bar = `Σ prezzo×qtà + varianti`; normali = `Σ prezzo×qtà`; a ore =
`Σ totalPrice` slot) e sconto — tutti lato client (motore di prezzo §4–§5).

**Eventi**:

- **Notifica in‑app** (`createNotification`, `type = 1`) per ogni nuova prenotazione servizio: testo
  `"<nome>, <dd-mm-yyyy> - <accountName>"` (da operatore) o `"<nome> - In linea, <dd-mm-yyyy>"`
  (online).
- **Solo se creata online dal cliente** (`bookingByAdmin == 0`, `payment_status == 1`, nuova):
  - **email** al cliente **e** agli indirizzi della property (`sendEmailToCustomerOnServiceReservation`,
    oggetto "Service Reservation Booking Successfully Done", IT/EN da `languageCode`, con
    `useFulInfo` dalle impostazioni online) + righe `email_logs`;
  - **WhatsApp** ai numeri della property + al numero del cliente (`emailHelper.sendWhatsappTo`) —
    chiamata ad **API esterna** WhatsApp.
- **socket.io**: evento `NOTIFIATION_TRIGGER_NAME` sul canale `property_<id>` per refresh in tempo
  reale.
- Da operatore (`bookingByAdmin == 1`): **solo** la notifica in‑app (nessuna email/WhatsApp).

---

## 2. Elenco e dettaglio prenotazioni servizio

**Endpoint**: `service/reservation/list` (`serviceReservationList`), `service/reservation/single`
(`singleServiceReservationList`), `service/reservation/detailsByCustomerId`,
`serviceReservationDeatailsByBookingNumber`.

**Logiche**: elenco filtrato per property/data/cliente; il dettaglio ricompone servizi, pagamenti,
stato ordine. **Calcoli/eventi**: nessuno (lettura).

---

## 3. Modifica / eliminazione prenotazione servizio

- **Modifica**: stesso `POST /api/sb/service/reservation` con `id` — aggiorna `service_reservation`,
  ricalcola importi lato client; risponde "Service Reservation updated"; **nessuna** email/WhatsApp in
  update (solo la notifica non viene rigenerata).
- **Elimina singola**: `POST service/reservation/delete/single` (`deleteSingleServiceReservationList`):
  rimuove una singola voce servizio; aggiorna i conteggi. Nessuna email.
- **Annulla** (con regole): `cancelServiceBooking` — applica le regole di cancellazione della
  property; se c'era pagamento online → **rimborso Stripe**. **Eventi**: email di annullamento +
  `email_logs`.

---

## 4. Stato ordine (bar / cucina)

**Azione**: segnare un ordine come "evaso". **Endpoint**: `POST update-order-filled`
(`updateOrderFilled`). **Logiche**: imposta il flag "order filled" sulla voce/ordine.
**Calcoli/eventi**: nessuno.

---

## 5. Incassi e pagamenti

Dal banco cassa, su una prenotazione (postazione o servizio):

- **Acconto / saldo** (`AddDeposit`, `PaymentModal`, `booking_payments` / `transactions_payments`):
  registra un incasso. Il **tetto** del pagamento parziale è vincolato **solo lato client**
  (`sessionStorage`); il backend controlla solo che l'importo esista (§11.9).
- **Link di pagamento Stripe** — `POST send-link` (`stripe.controller.js:createPyamentLink`): crea una
  sessione Stripe (importo dal client) e invia il link al cliente. **Eventi**: **API Stripe**; invio
  link; al webhook di conferma → aggiornamento `booking_payments` / `bookings_send_payment_summaries` +
  eventuale **notifica in‑app**.
- **Cattura autorizzazione** — `capturePayment` (`stripe.controller.js:capturePayment`).
- **Rimborso** — `refundPayment` / `refundPaymentController` → `refund_transaction_details`.
- **Trasferimento all'account admin** — `TransferTOAdminAccount` (payout verso l'account Stripe della
  piattaforma per la quota commissione).
- **Conto / carta prepagata cliente** — `cardTransaction` (`createCardTransaction`), `getMovementList`,
  `getBalanceAndIdentifier`, `getAllCardTransaction`, `deleteUserCard`, `send-card-detail`
  (`card_transactions`): ricariche e consumi sul "conto" del cliente; `sendCardDetails` invia al
  cliente identificativo/saldo carta (**email**).

**Calcoli**: saldo residuo = totale − incassato (in UI); quota commissione per il payout.

---

## 6. Scontrino elettronico Fiskaly (SIGN IT)

**Prerequisito**: la property deve avere Fiskaly attivo (`fiskaly_account`, `fiskaly_cash_register`,
`getFiskalyEnabled`), termini accettati (`getTerms`/`updateTerms`).

**Azioni** (`fiskalyReceipt.controller.js`):

- **Emettere scontrino** — `POST createReceipt` / `createAutoReceipt`: costruisce i **line item** dalla
  prenotazione (`fiskaly.helper.js:lineItemsFromBooking`), chiama l'**API Fiskaly** per firmare lo
  scontrino, salva `fiskaly_receipt` (+ `fiskaly_receipt_log`, `fiskaly_receipt_sequence`).
- **Scontrino non fiscale** — `createNonFiscalReceipt` / `getNonFiscalReceipts` / `sendNonFiscalReceipt`
  (`fiskaly_non_fiscal_receipt`): documento di cortesia senza valore fiscale.
- **PDF e invio** — `getReceiptPDF`, `sendReceipt`, `sendReceiptByBookingNumber`: genera il PDF e lo
  **invia al cliente via email** (+ `email_logs`).
- **Annullare / cancellare** — `deleteReceipt` / `deleteReceipts`: emette lo storno presso Fiskaly.

**Logiche di coerenza**:

- In fase di **modifica** di una prenotazione già scontrinata: `checkAndNotifyPaidItemsOnUpdate` /
  `checkPaidLineItemsWithPrevious` confrontano i nuovi line item con quelli già pagati; se
  incompatibili → **blocco** dell'operazione + **notifica in‑app** con testo
  "Errore nei dati per la generazione dello scontrino elettronico: …".
- **Spostare** una prenotazione già scontrinata su un'altra postazione è vietato finché lo scontrino
  non è annullato (notifica in‑app dedicata — vedi [01-mappa](01-mappa.md) §2.7).

**Calcoli**: nessun ricalcolo prezzo (i line item derivano dagli importi già salvati); numerazione
progressiva scontrini (`fiskaly_receipt_sequence`, `receipt_serial_numbers`).

**Eventi**: **API esterna Fiskaly** (firma, storno); generazione PDF; **email** al cliente con lo
scontrino; **notifica in‑app** sugli errori di coerenza.

**Percorso legacy (RT hardware)**: `printer_configs` + `booking_receipts` + `receipt_serial_numbers`
(`saveReceiptdata`, `getReceiptDetails`) — stampa su registratore di cassa fisico; mantenuto come
componente separato (dominio C4bis, decisione di business). La configurazione stampanti è nella voce
[11-configurazione](11-configurazione.md).

---

## 7. Integrazione POS TeamSystem ("Cassa in Cloud")

**Endpoint** (`ofsale.controller.js`, autenticati con JWT dedicato TeamSystem):

- `getCustomers`: espone l'elenco clienti della property al POS.
- `putCharge`: il POS registra un addebito/consumo su una prenotazione.

**Logiche**: il POS esterno interroga/scrive tramite un token `appName = "TeamSystem"`. **Eventi**:
scrittura su `teamsystem_expenses` / `transactions_payments`; nessuna email.

---

## 8. Credito / ledger cliente

**Endpoint**: `booking/creditDetails` (`creditDetails`), `getCreditLedgerDetails`,
`getCreditsEarnedByBookingId`.

**Logiche**: ricostruisce il **saldo a credito/debito** di un cliente sommando incassi, consumi,
acconti, rimborsi e "crediti maturati" (es. giorni non usati). È usato sia dalla Cassa sia dalla voce
[05-prenotazioni](05-prenotazioni.md).

**Calcoli**: somma algebrica dei movimenti (`booking_payments`, `card_transactions`,
`transactions_payments`, `refund_transaction_details`). **Eventi**: nessuno (lettura).

---

## 9. Riepilogo "calcoli / logiche / eventi" della voce Cassa

- **Calcoli**: totali servizi bar/normali/orari; sconto; saldo residuo; quota commissione per payout;
  numerazione progressiva scontrini; saldo credito/ledger.
- **Logiche**: validazioni cliente; blacklist; capienza slot (solo client); regole di cancellazione;
  coerenza line item vs pagati per lo scontrino; blocco spostamento se scontrinata; token TeamSystem.
- **Eventi**: notifica in‑app (nuova vendita, errori scontrino, conferma pagamento); **email** (conferma
  servizio online, invio scontrino/PDF, dettagli carta, annullamento) con `email_logs`; **WhatsApp**
  (conferma servizio online) — API esterna; **API Stripe** (link, cattura, rimborso, transfer);
  **API Fiskaly** (firma/storno scontrino); socket.io per refresh; scritture su
  `transactions_payments`/`teamsystem_expenses` (TeamSystem).
