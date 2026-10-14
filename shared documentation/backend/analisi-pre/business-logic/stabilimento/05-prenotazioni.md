# stabilimento — Prenotazioni

> **Voce di menù**: quinta icona della sidebar (`_gotoCredit` → `components/credit/CreditParent` →
> `components/credit/Component/Credit`, rotta `/credit`; etichetta `all_Bookings`, icona
> "prenotazioni"). È la **console di consultazione e gestione di tutte le prenotazioni** dello
> stabilimento, con schede: **Prenotazioni** (elenco), **Prenotazioni online**, **Prenotazioni servizi
> online**, **Richieste di pagamento**, **Transazioni**, **Condivisioni create**, **Ledger/credito**.
>
> **Ruoli**: visibile a chi non è `cashoperator`/`marketing`/`affiliate`/`waiter`/`lifeguard`/
> `advancedlifeguard` **e** ha licenza non‑`basic` (oltre ad `admin` sempre).
>
> **Metodo**: action file `creditAction.js` / `reservationAction.js` + controller
> `booking.controller.js` (`creditDetails`, `getCreditLedgerDetails`, `getOnlinereservationList`,
> `getBookingsWithPaymentRequest`, `getSharingCreatedBooking`, `getSharingDates`, `createSharing`),
> `stripe.controller.js` (`createPyamentLink`, `capturePayment`, `updateBookingSendPaymentSummary`,
> `allowsendPaymentSummary`). Prezzi/importi: già calcolati e salvati (vedi
> [`_motore-di-prezzo-e-sconti.md`](_motore-di-prezzo-e-sconti.md) §7). Il drill‑down su una singola
> prenotazione riusa le azioni della [Mappa](01-mappa.md) (check‑in/out, annulla, pagamenti, riepilogo).

---

## 1. Elenco prenotazioni (scheda "Prenotazioni")

**Azione**: l'operatore filtra per intervallo di date, tipo, metodo di pagamento, gruppo, voucher,
pass stagionale, online/offline, "in scadenza"; vede l'elenco con stato pagamento.

**Endpoint**: `booking/creditDetails` (`creditDetails`), `booking/counts`, `booking/bookingTypeCount`,
`getPaidAndUnpaidBookings`, `getUnpaidBookings`, `getSeasonalBookings`.

**Logiche**:

- `creditDetails` richiede `propertyId`, `startDate`, `endDate`; incrocia `bookings` + `booking_payments`
  e `service_reservation` + `service_reservation_payments` nel periodo, raggruppa per prenotazione e
  calcola per ciascuna: **totale**, **incassato**, **residuo**, stato (pagata / acconto / non pagata),
  canale (online / operatore), eventuale voucher applicato.
- I filtri sono applicati lato query/lista.

**Calcoli**: somme per prenotazione (`totale`, `pagato`, `pendente = totale − pagato`); totali di
periodo; conteggi per tipo/stato. Nessun ricalcolo del prezzo (usa gli importi salvati).

**Eventi**: nessuno (lettura).

---

## 2. Scheda "Prenotazioni online" / "Prenotazioni servizi online"

**Endpoint**: `getOnlinereservationList` (`getOnlinereservationList`), `viewonlinereservation`.

**Logiche**: elenca le prenotazioni provenienti dal sito cliente (con stato pagamento online,
transazione Stripe collegata, eventuale acconto/saldo, commissione applicata). Permette di aprirle e
gestirle come le altre.

**Calcoli**: importi e commissione già salvati (commissione calcolata lato cliente al checkout — vedi
[`properties.md`](../../db/properties.md#commissionamount)). **Eventi**: nessuno in lettura; le azioni
sul singolo record (conferma, rimborso) sono quelle di [01-mappa](01-mappa.md) §5 e §7.

---

## 3. Scheda "Richieste di pagamento"

**Azione**: gestire i **link di pagamento** inviati ai clienti e il loro stato (inviato, pagato,
scaduto).

**Endpoint**: `getBookingsWithPaymentRequest` (`getBookingsWithPaymentRequest`),
`allow-sendpayment-summary` (`allowsendPaymentSummary`), `send-link` (`createPyamentLink`),
`updateBookingSendPaymentSummary`, `insertIntoPaymentSummery`, `capturePayment`.

**Logiche**:

- `getBookingsWithPaymentRequest` unisce `bookings_send_payment_summaries` (link creati, con
  `end_date_time` = scadenza, `amount`) e `booking_payments` (`total_amount`, `paid_amount`,
  `pending_amount`); filtri per tipo, metodo, gruppo, "in scadenza", online, voucher, pass stagionale,
  intervallo di creazione link.
- **Creare/inviare un link**: `createPyamentLink` genera una sessione Stripe per l'importo indicato
  (controlla solo `!amount`; **non** verifica che l'importo sia ≤ residuo — §11.9) e la registra;
  `updateBookingSendPaymentSummary`/`insertIntoPaymentSummery` tracciano l'invio.
- **Alla conferma di pagamento** (webhook Stripe): la prenotazione viene aggiornata
  (`payment_status`), il record del link marcato pagato, e può scattare una **notifica in‑app**
  (`stripe.controller.js`).

**Calcoli**: importo del link (deciso dall'operatore); `pending = total − paid`.

**Eventi**: **API Stripe** (creazione sessione, cattura); invio del link al cliente (email/SMS);
**notifica in‑app** alla conferma; scritture su `bookings_send_payment_summaries` /
`online_transaction_details` / `card_transactions`.

---

## 4. Scheda "Transazioni"

**Endpoint**: `getMovementList` (`cardTransaction.controller.js`), `getAllCardTransaction`,
`online_transaction_details`, `transactions_payments`.

**Logiche**: elenco cronologico di tutti i movimenti (incassi, acconti, rimborsi, ricariche/consumi
conto, transazioni online) con importo, metodo, prenotazione collegata, operatore.

**Calcoli**: totali/saldi progressivi. **Eventi**: nessuno (lettura).

---

## 5. Scheda "Condivisioni create"

**Endpoint**: `getSharingCreatedBooking` (`getSharingCreatedBooking`), `getSharingDates`,
`get-sharing-and-absence-dates`, `createSharing`.

**Logiche**: elenca le postazioni condivise (sharing) create e le rispettive date per partecipante;
da qui si può creare una nuova condivisione (`createSharing` — vedi [01-mappa](01-mappa.md) §9).

**Calcoli**: prezzo per partecipante sui suoi giorni (lato client). **Eventi**: email di
riepilogo/condivisione ai partecipanti + `email_logs` (dal `createSharing`).

---

## 6. Ledger / credito cliente

**Endpoint**: `getCreditLedgerDetails` (`getCreditLedgerDetails`), `getCreditsEarnedByBookingId`.

**Logiche**: per un cliente/prenotazione ricostruisce il **saldo a credito/debito**: somma incassi,
acconti, consumi, rimborsi e "crediti maturati" (es. giorni non utilizzati / assenze che generano
credito). Vedi anche [03-cassa](03-cassa.md) §8.

**Calcoli**: somma algebrica dei movimenti; credito da giorni non usati = `giorni_non_usati ×
tariffa_giornaliera` (lato client).

**Eventi**: nessuno.

---

## 7. Impostazioni prenotazione online (accessibili anche da qui)

Alcune schede espongono le impostazioni del canale online: `getOnlineBookingSettings` /
`updateOnlineBookingSettings` / `addonlinereservation` / `getPaymentOptions` / `viewseasonsperiods`.
La logica completa (opzioni di pagamento, `allowOnlineBooking`, mappa online, periodi) è descritta in
[12-impostazioni](12-impostazioni.md) e [11-configurazione](11-configurazione.md); qui si può solo
consultarle/regolarle come scorciatoia.

---

## 8. Riepilogo "calcoli / logiche / eventi" della voce Prenotazioni

- **Calcoli**: aggregazioni per prenotazione (totale / pagato / pendente); totali di periodo;
  conteggi per tipo/stato/canale; saldo ledger e credito da giorni non usati; importo dei link di
  pagamento (input operatore). Nessun ricalcolo del prezzo di listino.
- **Logiche**: filtri multi‑criterio; unione `booking_payments` ↔ `bookings_send_payment_summaries`;
  stato pagamento; scadenza link; non‑sovrapposizione date sharing.
- **Eventi**: **API Stripe** (creazione/cattura link di pagamento); invio link al cliente; **notifica
  in‑app** alla conferma di pagamento; **email** di condivisione (sharing) + `email_logs`. Le schede di
  sola consultazione non generano eventi.
