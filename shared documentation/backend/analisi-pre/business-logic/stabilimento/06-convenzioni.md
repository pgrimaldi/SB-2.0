# stabilimento — Convenzioni (strutture partner / hotel)

> **Voce di menù**: sesta icona della sidebar (`_goToAgreements` → `components/agreements/AgreementsParent`,
> rotta `/agreements`). Gestisce le **strutture partner** ("convenzioni": hotel, B&B, camping) che
> inviano clienti allo stabilimento: anagrafica struttura, listini/voucher dedicati, pagamenti dovuti
> dalla/alla struttura, **report periodici** delle prenotazioni con invio via email/WhatsApp, template
> di messaggio.
>
> **Ruoli**: visibile **solo** a `superadmin`, `admin`, `manager` (controllo lato client in
> `App.jsx`). Le rotte hotel sono sotto `authJwt` (vedi `hotel.routes.js`); `hotelService.hotelExists`
> filtra per property dell'utente (`req.userId`) — **è uno dei pochi punti con vero scoping per
> stabilimento**.
>
> **Metodo**: `hotel.routes.js` + `controllers/hotel/*` (hotel, hotelPriceList, hotelPayment,
> hotelReport, hotelMessageTemplate) + `Service/hotel*Service.js`. Schede DB:
> [`hotels.md`](../../db/hotels.md), [`hotel_price_lists.md`](../../db/hotel_price_lists.md),
> [`hotel_payments.md`](../../db/hotel_payments.md), [`hotel_reports.md`](../../db/hotel_reports.md),
> [`hotel_message_templates.md`](../../db/hotel_message_templates.md).
>
> **Nota**: non è un modulo di prenotazione camere. Una "prenotazione hotel" è una normale prenotazione
> di postazioni/servizi **marcata come proveniente da una struttura partner**, con eventuale listino
> dedicato; il legame passa da `req.body.hotelBooking` nel `POST /api/sb/booking` (vedi
> [01-mappa](01-mappa.md) §2.11).

---

## 1. Anagrafica strutture partner

**Azioni** (`hotel.controller.js`): `createHotel`, `getHotelList`, `getHotelDetails`, `updateHotel`,
`deleteHotel`, `duplicateHotel`, `duplicateHotelsBulk`, `deleteHotelsBulk`.

**Logiche**:

- Ogni struttura (`hotels`) è **figlia di `properties`**: nome, indirizzo, referente, **email**,
  **telefono/WhatsApp**, note, eventuale % di commissione/accordo.
- `createHotel`/`updateHotel` validano i campi obbligatori tramite DTO; le operazioni sono scoped alla
  property dell'utente (`hotelExists(hotelId, req.userId)`).
- **Duplica**: copia l'anagrafica (e, in bulk, più strutture in una volta) per velocizzare
  l'inserimento di catene simili.
- **Elimina** (singola/bulk): rimozione della struttura; nessuna cascata descritta sui report/pagamenti
  storici.

**Calcoli**: nessuno. **Eventi**: nessuno (nessuna email/notifica).

---

## 2. Listini e voucher dedicati alla struttura

**Azioni** (`hotelPriceList.controller.js`): `createHotelPriceList`, `getHotelPriceListList`,
`getHotelPriceListDetails`, `updateHotelPriceList`, `deleteHotelPriceList`.

**Logiche**:

- Struttura analoga a [`price_lists`](../../db/price_lists.md) (pacchetti + prezzi per periodo) con in
  più i **campi voucher** (codice, valore, validità): la struttura partner può "vendere" al proprio
  cliente un voucher spendibile allo stabilimento.
- Le impostazioni weekend/mensile (`weekendSetting`/`monthlySetting`) qui risultano **costanti** sul DB
  (a differenza di `price_lists`) — vedi [`hotel_price_lists.md`](../../db/hotel_price_lists.md).

**Calcoli**: il prezzo di una prenotazione con listino hotel è calcolato dal **motore di prezzo lato
client** usando questi valori al posto di quelli standard (vedi
[`_motore-di-prezzo-e-sconti.md`](_motore-di-prezzo-e-sconti.md)); lo sconto voucher è fisso o
percentuale (§5).

**Eventi**: nessuno.

---

## 3. Pagamenti struttura

**Azioni** (`hotelPayment.controller.js`): `createHotelPayment`, `getHotelPaymentList`,
`getHotelPaymentDetails`, `updateHotelPayment`, `getPaymentsByBookingId`, `getPaymentSummary`.

**Logiche**:

- Registra gli importi legati alle prenotazioni provenienti dalla struttura (`hotel_payments`):
  importo, stato (`completed`/`pending`), prenotazione e `bookingNumber` collegati.
- In fase di creazione/aggiornamento prenotazione con `hotelBooking`, il backend
  (`hotelPaymentService.processHotelBookingPayments`) **cancella** i pagamenti esistenti per quelle
  prenotazioni (flusso update) e li **ricrea** con stato `completed` se `payment_status = 1`, altrimenti
  `pending` (vedi [01-mappa](01-mappa.md) §2.11).

**Calcoli**: somma degli importi per prenotazione / per periodo (`getPaymentSummary`).

**Eventi**: nessuno (scritture su `hotel_payments`).

---

## 4. Report periodici alla struttura

**Azioni** (`hotelReport.controller.js`): `createHotelReport`, `getHotelReportList`,
`getHotelReportDetails`, `updateHotelReportNotes`, `cancelHotelReport`, `cancelHotelReportsBulk`,
`getReportPdf`, `sendReport`, `sendReportMessage`, `previewReportMessage`.

**Logiche (`createHotelReport` → `hotelReportService.createHotelReport`)**:

- Validazioni DTO; la struttura deve esistere per quella property; `bookingStartDate ≤ bookingEndDate`.
- Il report **aggrega le prenotazioni** di quella struttura nell'intervallo scelto e calcola:
  - **`totalBookings`** = numero di prenotazioni (id distinti) nel periodo;
  - **`totalRevenue`** ("amount") = **somma di `hotel_payments.amount`** delle prenotazioni incluse
    (iterazione `totalRevenue += parseFloat(payment.amount || 0)`).
- `generatedBy` = utente autenticato; stato iniziale del report.
- **Annullamento** (`cancelHotelReport`/bulk): marca il report annullato.
- **Note** (`updateHotelReportNotes`): campo libero sul report.

**Calcoli**: conteggio prenotazioni; somma pagamenti; (eventuale quota/commissione secondo l'accordo
con la struttura, se prevista nei dati).

**Eventi**:

- **PDF**: `getReportPdf` genera il PDF del report; `sendReport` lo memorizza
  (`hotelReportNotificationService.storeReportPdf`) e lo invia.
- **Invio via email** (`sendReport` / `sendReportMessage` con `send_via = "email"`): usa la
  configurazione SMTP della property se valida, altrimenti il trasportatore di default (mittente
  `SB_FROM_EMAIL`); destinatario = `hotels.email`; il testo è costruito da un **template di messaggio**
  con sostituzione dei segnaposto; il PDF è **allegato**. Scrive `email_logs`.
- **Invio via WhatsApp** (`send_via = "whatsapp"`): **non** invia direttamente; costruisce un **deep
  link `wa.me`** verso `hotels.phone` con il testo del messaggio + il **link al PDF** incorporato, che
  l'operatore apre per inviare da WhatsApp. (Nessuna API WhatsApp server‑side per questo flusso.)
- **Anteprima** (`previewReportMessage` / `sendReportMessage` preview): costruisce il testo del
  messaggio **senza** inviare nulla e senza generare il PDF.
- **Notifiche**: `hotel_reports` alimenta anche `revenue_notifications`/notifiche interne in alcuni
  flussi — vedi [`hotel_reports.md`](../../db/hotel_reports.md).

---

## 5. Template di messaggio

**Azioni** (`hotelMessageTemplate.controller.js`): `createMessageTemplate`, `getMessageTemplateList`,
`getMessageTemplateDetails`, `updateMessageTemplate`, `deleteMessageTemplate`.

**Logiche**: template **email/WhatsApp** riutilizzabili per l'invio dei report, **condivisi per
property** (non per singola struttura); contengono segnaposto (es. nome struttura, periodo, totale)
sostituiti al momento dell'invio.

**Calcoli**: nessuno. **Eventi**: nessuno (sono dati).

---

## 6. Riepilogo "calcoli / logiche / eventi" della voce Convenzioni

- **Calcoli**: prezzo prenotazione con listino hotel (motore lato client); somma pagamenti struttura
  per prenotazione/periodo; `totalBookings` (conteggio) e `totalRevenue` (somma `hotel_payments.amount`)
  del report; sconto voucher fisso/percentuale.
- **Logiche**: scoping per property (raro nel sistema, qui presente); validazione intervallo date;
  ricreazione pagamenti hotel al salvataggio prenotazione (`completed`/`pending` da `payment_status`);
  sostituzione segnaposto nei template.
- **Eventi**: **email** con PDF allegato alla struttura (SMTP property o default) + `email_logs`;
  **deep link `wa.me`** per l'invio WhatsApp manuale; generazione/archiviazione PDF; scritture su
  `hotel_payments` / `hotel_reports`. Anagrafica, listini e template **non** generano eventi.
