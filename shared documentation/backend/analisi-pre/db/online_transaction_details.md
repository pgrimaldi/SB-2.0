# Tabella `online_transaction_details`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **38.240 righe**,
> query completa), model
> [`back-pre/app/models/onlineTaransactionDetails.js`](../../../../../../back-pre/app/models/onlineTaransactionDetails.js)
> (nome file e ORM con refuso "Taransaction"; `freezeTableName: true`, `timestamps: true`; il model
> chiama `sync({ alter: true })` all'avvio — vedi criticità), scrittura principale in
> [`stripe.controller.js`](../../../../../../back-pre/app/controllers/stripe.controller.js) (numerosi
> `OnlineTaransactionDetails.create({...})`, es. righe ~254, ~823, ~1237, ~1692, ~2065; update di
> `status`/`paid_payment` dopo cattura/conferma; `refundPayment` legge `payment_type` e
> `stripe_transaction_id` per il rimborso), scrittura minima in
> [`onlineTransaction.controller.js`](../../../../../../back-pre/app/controllers/onlineTransaction.controller.js)
> `TransactionEntryForPayAtProperty` (crea una riga con solo `booking_id`/`reservation_id` +
> `pending_payment` per il "paga in struttura"), lettura in
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js)
> (`getOnlinereservationList`, join `td.payment_type, td.status` con `booking_payments`).
> Frontend `front-customer-pre`: flusso di pagamento online (Stripe Connect / iframe), gestionale
> `front-pre`: lista transazioni online.
>
> **Nota di contesto** — dettaglio delle **transazioni di pagamento online tramite Stripe (Connect)**
> per prenotazioni ombrellone o prenotazioni servizi effettuate dal cliente. **Una riga = un
> PaymentIntent Stripe** (o una registrazione "paga in struttura"). Tiene traccia di importo pagato /
> trattenuto (hold) / residuo, commissione di piattaforma e stato Stripe.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): interi crescenti per 38.240 righe.

### stripe_transaction_id
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: id del PaymentIntent / charge Stripe (`pi_...`). Usato da `refundPayment` come
  `charge` per `stripe.refunds.create`.
- **Esempio di valori**: `"pi_3TxjdTEGaeGyBkbc13Dzc4Om"`

### stripe_connected_id
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Relazioni**: `stripe_configrations.sellerStripeConnectId` dello stabilimento (account Stripe
  Connect del venditore).
- **Esempio di valori**: `"acct_1RAoepK6git14X00"`

### booking_type
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione / approfondimento**: indica **cosa** si sta pagando online. Deriva da
  `req.body.booking_type` che il frontend `front-customer-pre` imposta a:
  - **`"1"`** = pagamento di una **prenotazione ombrellone/postazione** — dal checkout
    reserve-article (`propertyDetails/PropertySignIn.js:1294`, insieme a `bookingBy: 2`);
  - **`"2"`** = pagamento di una **prenotazione di soli servizi** (bar / servizi a ore) — dal
    checkout service reservation (`propertyDetails/PropertySignIn.js:861`, insieme a `bookingType: 2`,
    `bookingBy: 2`);
  - **`"0"`** = default in `payment/PaymentParent.js` quando non passato;
  - `NULL` sulle righe create da `TransactionEntryForPayAtProperty` (flusso "paga in struttura", che
    non lo valorizza).
  Nel campione DB compare solo `"1"` (i pagamenti online di sole prenotazioni servizi sono più rari) e
  `NULL`.
  **Attenzione**: **non** è lo stesso di [`bookings.bookingType`](bookings.md) (0/1/2 = tipo listino
  hotel), nonostante il nome quasi identico → fonte di confusione da eliminare.
- **Candidato a enumerazione**: **Sì** — allineare a `PaymentSubjectType` (`Booking` = 1 /
  `ServiceReservation` = 2), rinominando il campo (es. `payment_subject`). Vedi
  [`docs/03-gestione-dati.md` §2.4bis](../../progettazione/03-gestione-dati.md).
- **Esempio di valori**: `"1"` (prenotazione), `"2"` (prenotazione servizi), `null`

### payment_type
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: modalità di pagamento online scelta. Valori reali: `"pay_now"` (paga tutto subito),
  `"advance_balance"` (acconto/saldo). Il codice Stripe usa anche `"pay_at_property"` (paga in
  struttura). `refundPayment` rimborsa solo se `payment_type ∈ { "pay_now", "advance_balance" }`.
- **Candidato a enumerazione**: **Sì** (`OnlinePaymentType`: `PayNow` / `AdvanceBalance` /
  `PayAtProperty`). Vedi [`docs/03-gestione-dati.md` §2](../../progettazione/03-gestione-dati.md).
- **Esempio di valori**: `"pay_now"`, `"advance_balance"`

### status
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: stato della transazione, **valore passthrough dello stato PaymentIntent Stripe**.
  Valori reali: `"succeeded"`, `"pending"`; il codice scrive anche `"Uncaptured"` (per gli hold non
  ancora catturati). Non normalizzato (maiuscole miste).
- **Candidato a enumerazione**: **Sì** (`OnlineTxStatus`: `Succeeded` / `Pending` / `Uncaptured` /
  `Failed` / `Refunded` ...) — con normalizzazione del casing; oppure mantenere lo stato Stripe grezzo
  in un campo dedicato e derivare un enum interno.
- **Esempio di valori**: `"succeeded"`, `"pending"`, `"Uncaptured"`

### paid_payment
- **Tipo**: `varchar(255)` (importo come stringa)
- **Nullabile**: Sì
- **Descrizione**: importo effettivamente incassato (al netto/lordo a seconda del flusso). Inizialmente
  `0`, aggiornato dopo la conferma/cattura.
- **Esempio di valori**: `"23"`, `"28.5"`, `"0"`

### hold_payment
- **Tipo**: `varchar(255)` (importo come stringa)
- **Nullabile**: Sì
- **Descrizione — "importo trattenuto"**: importo per cui è stata creata su Stripe
  un'**autorizzazione con cattura manuale** (`PaymentIntent` con `capture_method: manual`), cioè
  fondi **bloccati sulla carta del cliente ma non ancora incassati** dal venditore. Casi d'uso: (a)
  `advance_balance` — si autorizza il saldo residuo alla prenotazione online e lo si cattura solo
  all'arrivo/al momento concordato; (b) garanzia. Quando l'operazione viene **catturata**, l'importo
  passa da `hold_payment` a `paid_payment` e lo `status` da `"Uncaptured"` a `"succeeded"`; se l'hold
  **scade o viene annullato**, i fondi tornano al cliente senza movimento contabile. È diverso da
  `pending_payment` (residuo ancora *da pagare*, senza fondi bloccati) e da `paid_payment` (già
  incassato).
- **Esempio di valori**: `"28.5"` (hold attivo), `"0"` (nessun hold — pagamento immediato)

### pending_payment
- **Tipo**: `varchar(255)` (importo come stringa)
- **Nullabile**: Sì
- **Descrizione**: residuo da pagare (`amount − application_fee_amount` in alcuni rami;
  `req.body.pending_amount` in `TransactionEntryForPayAtProperty`).
- **Esempio di valori**: `"0"`, `"28.5"`

### application_fee_amount
- **Tipo**: `varchar(255)` (importo come stringa)
- **Nullabile**: Sì
- **Descrizione**: commissione di piattaforma trattenuta (Stripe Connect `application_fee_amount`).
- **Esempio di valori**: `"1.88"`, `"0"`

### propertyId
- **Tipo**: `varchar(255)` (stringa)
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK)
- **Esempio di valori**: `"10561"`, `"10828"`

### booking_id
- **Tipo**: `varchar(255)` (stringa)
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK)
- **Esempio di valori**: `"269316"`

### reservation_id
- **Tipo**: `varchar(255)` (stringa)
- **Nullabile**: Sì
- **Relazioni**: `service_reservation.id` (nessuna FK). Alternativo a `booking_id` — valorizzato solo
  quando il pagamento online riguarda una prenotazione di soli servizi (`booking_type = "2"`); `""`
  altrimenti.
- **Esempio di valori**: `"6319"` (pagamento di una prenotazione servizi); `""` (pagamento di una
  prenotazione ombrellone → è valorizzato `booking_id`)

### transaction_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: `new Date()` alla creazione della riga.
- **Esempio di valori**: `2026-07-27 08:11:44`

### isIframe
- **Tipo**: `int`
- **Nullabile**: Sì
- **Descrizione**: `1` = pagamento avvenuto tramite il widget iframe embeddato; `0` = redirect/checkout
  standard. Nel campione entrambi presenti.
- **Esempio di valori**: `0`, `1`

### createdAt / updatedAt
- **Tipo**: `datetime`, `NOT NULL` — timestamp Sequelize (`timestamps: true`)
- **Nullabile**: No
- **Esempio di valori**: `2026-07-27 08:11:44` / `2026-07-27 08:11:47`

---

## Osservazioni verificate nel codice (criticità)

- **`sync({ alter: true })` al require del model**: il file esegue `onlineTransactionDetails.sync({
  alter: true })` all'avvio → altera lo schema della tabella in produzione ad ogni deploy. Da
  rimuovere (usare migrazioni).
- **Tutti gli importi come `varchar`**: `paid_payment`, `hold_payment`, `pending_payment`,
  `application_fee_amount` sono stringhe → aritmetica fatta con `Number(...)` sparsa nel codice
  (`Number(req.body.amount) - Number(req.body.application_fee_amount)`). Da usare `DECIMAL(10,2)`.
- **`propertyId` / `booking_id` / `reservation_id` come `varchar`** (mentre altrove sono `int`); nessuna
  FK; `reservation_id = ""` invece di `NULL`.
- **`status` passthrough di Stripe con casing incoerente** (`"succeeded"` vs `"Uncaptured"`).
- **`booking_type` non documentato** e valorizzato solo in alcuni rami.
- **Molte `create` quasi identiche in `stripe.controller.js`** (≈10 varianti, molte commentate) → forte
  duplicazione; comportamento reale difficile da ricostruire senza eseguire.
- **Refuso nel nome** file/ORM (`onlineTaransactionDetails`).
- **`TransactionEntryForPayAtProperty` con logica sconto fragile**: fa `JSON.parse(booking_discount)`
  senza controllo `null`/`{}` e legge `bookingTotalPrice` / `subTotal` da `booking_payments` (campi non
  sempre presenti).
