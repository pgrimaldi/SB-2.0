# Tabella `refund_transaction_details`

> Fonti: schema; **0 righe sia nel DB dev sia nel dump di produzione `sb_prod_dump_2026_09_08`**
> (esportata solo come schema) → i rimborsi non vengono tracciati in questa tabella. Model
> [`back-pre/app/models/refundTransactionDetails.model.js`](../../../../../../back-pre/app/models/refundTransactionDetails.model.js)
> (`sequelize.define("refund_transaction_details", ...)`; ORM `db.refundTransactionDetails`), unica
> scrittura in `refundPayment(booking_id)` — funzione presente **due volte**, in
> [`stripe.controller.js`](../../../../../../back-pre/app/controllers/stripe.controller.js) (righe ~2420-2447) e,
> identica, in
> [`helpers/serviceReservation.helper.js`](../../../../../../back-pre/app/controllers/helpers/serviceReservation.helper.js)
> (righe ~933-960). Chiamata quando una prenotazione/prenotazione servizi pagata online viene
> cancellata (`cancelPackageBooking`, `deleteBookingByBookingId`, `cancelServiceBooking`).
>
> **Nota di contesto** — registra i **rimborsi Stripe** effettuati sulle transazioni online. **Una
> riga = un rimborso.** `0` righe in DB dev semplicemente perché nel campione non ci sono
> cancellazioni con rimborso.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### stripe_transaction_id
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Relazioni**: `online_transaction_details.stripe_transaction_id` — il PaymentIntent/charge
  rimborsato.
- **Esempio di valori**: (nessun dato) — atteso `"pi_..."`

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK). Nota: `refundPayment` riceve `booking_id` come **array**
  (`Op.in`) ma salva solo `booking_id[0]`.
- **Esempio di valori**: (nessun dato)

### refund_id
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: **generato da Stripe** — è l'`id` dell'oggetto `Refund` restituito da
  `stripe.refunds.create({ charge })` (`refund.id`), salvato così com'è. Non è calcolato lato
  applicazione. Serve per correlare la riga all'oggetto rimborso su Stripe (dashboard, webhook
  `charge.refunded`, riconciliazione).
- **Esempio di valori**: `"re_3Txjd...AbCdEf"`

### amount
- **Tipo**: `int` (importo Stripe in **centesimi**)
- **Nullabile**: Sì
- **Descrizione**: `refund.amount` di Stripe, in **centesimi**. **È sempre pari all'importo pieno
  della transazione originaria**: il codice chiama `stripe.refunds.create({ charge:
  stripe_transaction_id })` **senza** parametro `amount` → Stripe rimborsa l'intero charge. Non
  esiste rimborso parziale in `back-pre`.
- **A cosa serve il campo se l'info è già su Stripe / in `online_transaction_details`**: in teoria è
  ridondante (l'importo pieno è già in `online_transaction_details.paid_payment` e su Stripe). Ha un
  minimo di utilità come **copia locale immutabile** per report/estratti senza chiamare l'API Stripe,
  e come traccia che il rimborso è avvenuto (la sola presenza della riga). In riscrittura: se si
  introduce il **rimborso parziale** (probabile: cancellazioni con penale, rimborsi su singola
  giornata), `amount` diventa necessario e va tenuto in `numeric`/centesimi con anche
  `original_amount` e `reason`. Se resta solo full-refund, si può derivare e tenere qui solo
  `refund_id` + `status` + `refunded_at`.
- **Esempio di valori**: `2350` (= 23,50 €), `9500` (= 95,00 €)

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard
- **Nullabile**: Sì
- **Esempio di valori**: (nessun dato)

---

## Osservazioni verificate nel codice (criticità)

- **Funzione duplicata**: `refundPayment` esiste identica in `stripe.controller.js` e in
  `serviceReservation.helper.js` → doppia manutenzione.
- **`amount` in centesimi** (convenzione Stripe) mentre `online_transaction_details.paid_payment` è in
  euro-stringa: unità disallineate tra le due tabelle di pagamento online.
- **Solo rimborso totale**: `stripe.refunds.create({ charge })` senza `amount` → rimborsa sempre
  l'intero importo; non esiste rimborso parziale.
- **`refundPayment` accetta un array di `booking_id` ma logga un solo rimborso** (`booking_id[0]`): se
  una cancellazione riguarda più righe `bookings` (prenotazione multi-data), il rimborso è associato
  solo alla prima.
- **Nessun campo di stato/esito** (es. `succeeded`/`failed`); in caso di errore la funzione ritorna
  `false` e non scrive nulla.
- **Nessuna FK.** Nessun collegamento a `service_reservation` anche quando il rimborso riguarda una
  prenotazione servizi (si salva comunque in `booking_id`).
