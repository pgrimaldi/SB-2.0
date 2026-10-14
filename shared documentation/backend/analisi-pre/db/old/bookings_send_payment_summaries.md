# Tabella `bookings_send_payment_summaries`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/bookingsSendPaymentSummary.model.js`](../../back-pre/app/models/bookingsSendPaymentSummary.model.js)
> (`sequelize.define("bookings_send_payment_summary", ...)` → tabella pluralizzata
> `bookings_send_payment_summaries`; ORM `db.bookingsSendPaymentSummary`), scrittura in
> [`stripe.controller.js`](../../back-pre/app/controllers/stripe.controller.js) (`create` iniziale
> `payment_status: "unpaid"` + `paymentLinks.create` Stripe, righe ~2904-2963; `update` da webhook
> pagamento riuscito/cancellato) e in
> [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js)
> `updateBookingSendPaymentSummary` (endpoint `PATCH /api/sb/update-booking-sendpayment-summary`,
> righe ~9008), `sendReservationSummery`; `updatePaymentSummary`
> (helper `booking.helper` / `booking.controller`) collega `booking_id` alla prenotazione appena
> creata quando si prenota tramite un link di pagamento inviato in precedenza. Cron
> [`cronjob/cron-cancel-booking.js`](../../back-pre/cronjob/cron-cancel-booking.js) usa `end_date_time`
> per scadere i link non pagati. Frontend `front-pre` (gestionale): pulsante "invia riepilogo/link di
> pagamento" via email/WhatsApp; `front-customer-pre`: pagina `payment-summary-success/:id`.
>
> **Nota di contesto** — traccia i **link di pagamento Stripe** ("riepilogo pagamento") inviati al
> cliente per saldare una prenotazione (o per farla confermare pagando). **Una riga = un link
> inviato**. Collega la prenotazione, la sessione/link Stripe, il canale di invio e l'esito.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Descrizione**: usato anche nell'URL di redirect post-pagamento (`payment-summary-success/{id}`).
- **Esempio di valori**: 1, 2, 3, ..

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK). Può essere valorizzato **dopo** la creazione del link
  (quando il link viene inviato *prima* che la prenotazione esista, `updatePaymentSummary` lo aggancia
  poi).
- **Esempio di valori**: 267081, 266852, 266745

### stripe_session_id
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: id del Payment Link Stripe (`plink_...`) o della sessione; `""` nella riga creata
  prima di generare il link, poi aggiornato.
- **Esempio di valori**: `"plink_1Tx20TCa53qapMGsOOzKnydK"`, `""`

### amount
- **Tipo**: `int` (importo intero — vedi criticità)
- **Nullabile**: Sì
- **Descrizione**: importo del link in **euro interi** (`req.body.amount`; su Stripe viene poi
  moltiplicato ×100). Non gestisce i centesimi.
- **Esempio di valori**: 115, 95

### payment_status
- **Tipo**: `varchar(255)` — (il commento nel model, ora rimosso, diceva `0=initiated, 1=success,
  2=failure/cancel`, ma i dati reali usano **stringhe**)
- **Nullabile**: Sì
- **Descrizione**: esito del pagamento del link. Valori reali: `"unpaid"` (link creato/inviato, non
  ancora pagato), `"paid"` (pagato). Impostato da webhook Stripe.
- **Candidato a enumerazione**: **Sì** (`PaymentLinkStatus`: `Unpaid` / `Paid` [/ `Cancelled`]) —
  con normalizzazione del drift stringhe vs numeri del commento storico. Vedi
  [`docs/03-gestione-dati.md` §2](../../docs/03-gestione-dati.md).
- **Accorpamento con `cancel_payment_flag` (e `end_date_time`) in un unico `status`**: **sì,
  consigliato**. Oggi lo stato del link è spalmato su tre campi: `payment_status` (`unpaid`/`paid`),
  `cancel_payment_flag` (`1` = link annullato) e implicitamente `end_date_time` (scaduto se
  `now() > end_date_time` e ancora `unpaid`). Un solo `enum status`:
  `pending` → `paid` | `cancelled` | `expired` (+ eventuale `failed` per il fallimento Stripe)
  copre tutti i casi senza combinazioni contraddittorie. `email_sent` resta separato (è un fatto di
  invio, non di pagamento — vedi [`docs/03-gestione-dati.md` §4](../../docs/03-gestione-dati.md), la
  parte "canale/esito invio" va in C6). `booking_cancel_flag` **non** va accorpato qui: non è lo stato
  del link ma la decisione "se il link scade, cancella anche la prenotazione" → resta un flag di
  configurazione della richiesta.
- **Esempio di valori**: `"paid"`, `"unpaid"`

### method_flag
- **Tipo**: `tinyint` — default `0` — commento DB: *"0 is for default value and 1 is for email & 2
  whatsapp"*
- **Nullabile**: Sì
- **Descrizione**: canale con cui è stato inviato il link. Valori reali: `1` (email, larga
  maggioranza), `2` (WhatsApp), `0` (default, righe di link cancellati/non inviati).
- **Candidato a enumerazione**: **Sì** (`SummaryDeliveryChannel`: `None`=0 / `Email`=1 / `WhatsApp`=2).
- **Esempio di valori**: `1`, `2`, `0`

### email_sent
- **Tipo**: `int` — default `0`
- **Nullabile**: Sì
- **Descrizione**: `1` = notifica (email/WhatsApp) effettivamente inviata. Nel campione `1` per tutte
  le righe con `method_flag ∈ {1,2}`, `0` per quelle `method_flag = 0`.
- **Esempio di valori**: `1`, `0`

### cancel_payment_flag
- **Tipo**: `tinyint` — default `0` — commento DB: *"1 is for cancel & 0 is for default value"*
- **Nullabile**: Sì
- **Descrizione**: `1` = il **pagamento/link** è stato annullato (es. link scaduto o revocato).
- **Esempio di valori**: `0`, `1`

### booking_cancel_flag
- **Tipo**: `tinyint` — default `0` — commento DB: *"1 is for cancel & 0 is for default value"*
- **Nullabile**: Sì
- **Descrizione**: `1` = alla scadenza/annullamento del link va **cancellata anche la prenotazione**
  associata (usato dal cron di cancellazione).
- **Esempio di valori**: `0`, `1`

### end_date_time
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: scadenza del link di pagamento. `cron-cancel-booking` confronta `NOW()` con questo
  valore per scadere i link `unpaid` e (se `booking_cancel_flag = 1`) cancellare la prenotazione.
- **Esempio di valori**: `2026-07-25 15:30:00`

### isCallingFromBookingDetail
- **Tipo**: `tinyint(1)`
- **Nullabile**: Sì
- **Descrizione**: `1` = il link è stato generato dalla schermata di **dettaglio prenotazione**
  (saldo di una prenotazione esistente); `0` = da un altro flusso (es. creazione con richiesta di
  pagamento). Cambia i testi/redirect.
- **Esempio di valori**: `1`, `0`

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-25 09:36:33` / `2026-07-25 11:33:15`

---

## Osservazioni verificate nel codice (criticità)

- **`amount` intero (euro)**: nessun supporto ai centesimi; su Stripe si moltiplica per 100. Da usare
  `DECIMAL(10,2)` o "importo in centesimi" esplicito.
- **`payment_status` stringa libera con storia di significati diversi**: il commento originale del
  model indicava codici numerici (`0/1/2`), i dati reali sono `"paid"/"unpaid"` (mappatura Stripe).
  Drift documentale.
- **Tre flag booleani sovrapposti** (`cancel_payment_flag`, `booking_cancel_flag`, `email_sent`) +
  `method_flag` + `isCallingFromBookingDetail`: molti `tinyint` non vincolati; alcune combinazioni
  ("`method_flag=0, email_sent=0, booking_cancel_flag=1`") rappresentano stati "link scaduto/da
  cancellare".
- **Riga creata in due passi** (prima `stripe_session_id = ""`, poi update dopo `paymentLinks.create`):
  se il secondo passo fallisce resta una riga con link vuoto.
- **`booking_id` nullable e agganciato a posteriori** (`updatePaymentSummary`): finestra in cui il link
  non è collegato ad alcuna prenotazione.
- **Nessuna FK**, nessun `ENUM`.
