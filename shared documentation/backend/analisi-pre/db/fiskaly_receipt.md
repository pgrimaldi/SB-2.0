# Tabella `fiskaly_receipt`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **2.596 righe**, query
> completa). Gli scontrini elettronici Fiskaly esistono in produzione **solo da giugno 2026**;
> `system_id` è concentrato su 2 dispositivi (1.788 + 687 righe su 2.596) → di fatto 2-3 stabilimenti
> emettono davvero scontrini elettronici. Model
> [`back-pre/app/models/fiskalyReceipt.model.js`](../../../../../../back-pre/app/models/fiskalyReceipt.model.js)
> (`{ freezeTableName: true, timestamps: false }`),
> scrittura in
> [`Service/fiskalyReceiptManagerService.js`](../../../../../../back-pre/app/Service/fiskalyReceiptManagerService.js)
> (`createReceipt` — flusso INTENTION→TRANSACTION→PDF→S3 in 10 step con logica di *resume*;
> `createReceiptFromBni`, `createAutoReceiptFromStripe`, `deleteReceipt`/`deleteReceipts` —
> annullamento con CANCELLATION);
> lettura in
> [`Service/fiskalyReceiptReaderService.js`](../../../../../../back-pre/app/Service/fiskalyReceiptReaderService.js)
> (`getReceipts` — filtra `record_state = 'COMPLETED'` **e** `record_mode = 'FINISHED'`, join a
> [`fiskaly_cash_register`](fiskaly_cash_register.md) → [`fiskaly_account`](fiskaly_account.md) per
> lo scoping a `property_id`; `getReceiptById` con log; `getReceiptPdf`),
> invio in
> [`Service/fiskalyReceiptNotificationService.js`](../../../../../../back-pre/app/Service/fiskalyReceiptNotificationService.js)
> (`sendReceipt` / `sendReceiptByBookingNumber` via email/WhatsApp);
> controller [`fiskalyReceipt.controller.js`](../../../../../../back-pre/app/controllers/fiskalyReceipt.controller.js)
> (endpoint `/api/sb/electronic-receipt/create|delete|delete-bulk|list|get-receipt/:id|pdf/:id|
> send|send-by-booking-number|auto-create`);
> costruzione righe/importi in
> [`controllers/helpers/fiskaly.helper.js`](../../../../../../back-pre/app/controllers/helpers/fiskaly.helper.js)
> (`buildReceiptCreationData`, `setPaidInLineItems`, `getLatestPaymentInfo`,
> `getPropertyIdByBookingNumber`), generazione PDF in
> [`utils/receiptPdf.helper.js`](../../../../../../back-pre/app/utils/receiptPdf.helper.js), upload S3
> (`AWS_RECEIPT_PATH = "receipts/fiskaly"`);
> `auto-create` è invocato da `front-customer-pre`
> (`src/data/actions/consumerAction.js` → `sb/electronic-receipt/auto-create`,
> chiamato da `PaymentMethodSelection.js` dopo il pagamento Stripe con `stripe_transaction_id`).
> Associazioni in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (righe 345-359):
> `belongsTo(db.fiskalyCashRegister, { foreignKey: "system_id", targetKey: "system_id" })`,
> `hasMany(db.fiskalyReceiptLog, { foreignKey: "receipt_id" })`. **Nessuna** associazione ORM con
> `bookings`/`service_reservation`: il legame è la stringa `booking_number`.
>
> **Nota di contesto** — **1 riga per documento commerciale elettronico ("scontrino") emesso**
> tramite Fiskaly SIGN IT, legata alla prenotazione tramite `booking_number` (che raggruppa
> prenotazioni ombrellone **e** prenotazioni servizi). La riga viene creata all'inizio del flusso
> (stato `INTENTION_CREATED`) e aggiornata step dopo step fino a `COMPLETED`; gli stati intermedi e
> gli errori sono tracciati in [`fiskaly_receipt_log`](fiskaly_receipt_log.md). L'annullamento non
> cancella la riga: `record_state` passa a `CANCELLED` e viene emessa una CANCELLATION su Fiskaly.
> Il numero progressivo fiscale (`document_number`) è preso dalla sequenza giornaliera per cassa
> ([`fiskaly_receipt_sequence`](fiskaly_receipt_sequence.md)) **al momento della firma**.
>
> **Dominio nella riscrittura**: **C4 — modulo `Fiskaly`** — è un **fatto fiscale storico**:
> gli importi e le righe (`entries`, `payments`, `amount_*`) sono uno **snapshot immutabile**
> (vedi [`docs/03-gestione-dati.md` §3.6 punto 2](../../progettazione/03-gestione-dati.md)); verso C1
> (prenotazione) e C3 (incasso Stripe) solo la chiave `booking_number` / gli importi, nessuna FK
> cross-modulo. Analisi domini in [`docs/03-gestione-dati.md` §4](../../progettazione/03-gestione-dati.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Descrizione**: usato come chiave di *resume* del flusso (`_resumeReceiptId`) e come
  `receipt_id` degli endpoint `delete` / `pdf` / `send`. **Da non confondere con
  `document_number`** (vedi sotto).
- **Esempio di valori**: 1, 2, 3, ..

### booking_number
- **Tipo**: `varchar(50)`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: `bookings.bookingNumber` / `service_reservation.bookingNumber` (nessuna FK; solo
  indice `fiskaly_receipt_booking_number`). Da qui si risale a `property_id` via
  `getPropertyIdByBookingNumber` per tutte le autorizzazioni.
- **Descrizione**: numero prenotazione a cui lo scontrino si riferisce; una prenotazione può avere
  **più** scontrini (uno per acconto/incasso successivo).
- **Esempio di valori**: `"1072314293"`, `"1072314292"`, `"1080410652"`

### system_id
- **Tipo**: `varchar(36)` — UUID v7
- **Nullabile**: Sì (a schema; nel codice sempre valorizzato = `cashRegister.system_id`)
- **Relazioni**: FK reale `fiskaly_receipt_ibfk_1` →
  [`fiskaly_cash_register.system_id`](fiskaly_cash_register.md), `ON DELETE SET NULL ON UPDATE
  CASCADE`. Indice `fiskaly_receipt_system_id`.
- **Descrizione**: cassa Fiskaly che ha firmato il documento. `getReceipts` lo espone joinando
  `mpn_name`, `system_mode`, `system_state`.
- **Esempio di valori**: `"019e8dc7-ae38-782a-9b97-5fb886342455"`,
  `"019eb70f-b0aa-7936-b1a4-3121fa22dd74"`

### record_id
- **Tipo**: `varchar(36)` — UUID v7. `UNIQUE` (`record_id`, indici `record_id_2`/`_3`)
- **Nullabile**: Sì (NULL finché la TRANSACTION non è stata creata con successo; tenuto NULL anche
  quando la TRANSACTION torna in stato `FAILED`, così un successivo delete la tratta come "niente da
  annullare su Fiskaly")
- **Descrizione**: id del *record* TRANSACTION::RECEIPT su Fiskaly (`transactionResponse.content.id`).
  Referenziato dalla CANCELLATION (`operation.record.id`).
- **Esempio di valori**: `"019fa264-d923-797d-985c-749f3081b1c7"`, `null`

### intention_record_id
- **Tipo**: `varchar(36)` — UUID v7
- **Nullabile**: Sì (NULL prima dello step 1)
- **Descrizione**: id del *record* INTENTION su Fiskaly, creato prima della TRANSACTION. Un INTENTION
  può essere consumato da **una sola** TRANSACTION: se questa fallisce, il flusso di resume ne conia
  uno nuovo e ripunta la riga.
- **Esempio di valori**: `"019fa264-d85d-713b-8533-dd0adf5878b7"`, `null`

### intention_cancellation_record_id
- **Tipo**: `varchar(36)` — UUID v7
- **Nullabile**: Sì (NULL finché non si avvia un annullamento)
- **Descrizione**: id dell'INTENTION creato per l'operazione di **CANCELLATION** (necessario perché
  la CANCELLATION è a sua volta una TRANSACTION che consuma un INTENTION). Popolato solo quando lo
  scontrino viene annullato. In produzione (2.596 righe): 110 righe `CANCELLED`.
- **Esempio di valori**: UUID v7, `null`

### document_number
- **Tipo**: `varchar(20)`
- **Nullabile**: Sì (resta NULL sulla riga finché il documento non è **firmato** con successo)
- **Descrizione**: **numero progressivo fiscale** del documento commerciale, assegnato *al momento
  dell'emissione* dalla sequenza giornaliera della cassa
  ([`fiskaly_receipt_sequence`](fiskaly_receipt_sequence.md), chiave `system_id` + `date`
  Europe/Rome). Non è un identificativo univoco della riga: **lo stesso valore si ripete ogni
  giorno** ed è condiviso da molte righe (in produzione `document_number` è `NULL` su 53 righe, 2%;
  il commento nel codice segnala un caso in produzione con `document_number = "2"` condiviso da 14
  scontrini). Per questo il resume avviene per `id`, mai per `document_number`.
- **Esempio di valori**: `"1"`, `"2"`, `"5"`, `"9"`

### amount_total
- **Tipo**: `varchar(20)` — importo monetario **come stringa**
- **Nullabile**: Sì
- **Descrizione**: totale incassato con questo scontrino, **al netto dello sconto** applicato dal
  pagamento (`= payments[0].details.amount - payments[0].details.discount`, cioè `paymentNetAmounts[0]`).
  Esposto in email/WhatsApp come `parseFloat(amount_total) - sconto`.
- **Esempio di valori**: `"75.00"`, `"55.00"`

### amount_vat
- **Tipo**: `varchar(20)` — stringa
- **Nullabile**: Sì
- **Descrizione**: totale IVA del documento (`Σ vat_amounts[].vat`, arrotondato a 2 decimali). Valore
  **lordo** (non riflette lo sconto — l'aggiustamento sconto è fatto solo nel PDF).
- **Esempio di valori**: `"13.52"`, `"9.92"`

### amount_net
- **Tipo**: `varchar(20)` — stringa
- **Nullabile**: Sì
- **Descrizione**: imponibile del documento (`= full_amount.excl_vat`).
- **Esempio di valori**: `"61.48"`, `"45.08"`

### entries
- **Tipo**: `longtext` — **array JSON** (`CHECK (json_valid(entries))`); default model `[]`
- **Nullabile**: Sì
- **Descrizione**: righe del documento (`= requestDto.line_items`), costruite da
  `buildReceiptCreationData` a partire dalle voci di
  [`booking_number_informations`](booking_number_informations.md) (`details_payments` /
  `details_payments_new`). Ogni elemento:
  - **type** — `string`: `"SALE"` (vendita) o `"RETURN"` (reso). Nel campione sempre `"SALE"`.
    Candidato a enumerazione (`FiskalyLineItemType { Sale, Return }`).
  - **description** — `string`: descrizione articolo (nome pacchetto/servizio).
    Es.: `"Ombrellone e 3 lettini - … - Non rimborsabile"`, `"Pedalò"`, `"Cabina"`.
  - **category** — `string`: `"SERVICE"` o `"GOODS"` (`item.category || "SERVICE"`; inviato a Fiskaly
    come `details.concept`). Nel campione sempre `"SERVICE"`. Candidato a enumerazione
    (`FiskalyLineItemCategory { Service, Goods }`).
  - **quantity** — `string`: quantità. Es.: `"1"`.
  - **unit_price** — `object`:
    - **incl_vat** — `string`: prezzo unitario IVA inclusa. Es.: `"75.00"`.
    - **excl_vat** — `string`: prezzo unitario IVA esclusa. Es.: `"61.48"`.
  - **vat** — `object`:
    - **type** — `string`: `"VAT_RATE"` o `"VAT_EXEMPTION"`. Nel campione sempre `"VAT_RATE"`.
      Candidato a enumerazione.
    - **code** — `string`: per `VAT_RATE` `"STANDARD"` / `"REDUCED_1"` / `"REDUCED_2"` / `"REDUCED_3"`
      (22/10/5/4% — `fiskaly_consts.js`); per `VAT_EXEMPTION` `"NOT_SUBJECT"` / `"NOT_TAXABLE"` /
      `"CAUSE_1..4"` (Natura N1-N6). Nel campione sempre `"STANDARD"`. Candidato a enumerazione.
    - **rate** — `string`: aliquota %. Es.: `"22.00"`.
    - **amount** — `string`: IVA della riga. Es.: `"13.52"`.
- **Esempio di valori**:
  `[{"type":"SALE","description":"Ombrellone e 3 lettini … Non rimborsabile","category":"SERVICE","quantity":"1","unit_price":{"incl_vat":"75.00","excl_vat":"61.48"},"vat":{"type":"VAT_RATE","code":"STANDARD","rate":"22.00","amount":"13.52"}}]`

### payments
- **Tipo**: `longtext` — **array JSON** (`CHECK (json_valid(payments))`); default model `[]`
- **Nullabile**: Sì
- **Descrizione**: modalità di pagamento con cui è stato incassato l'importo dello scontrino
  (`= requestDto.payments`, normalizzato a un **tipo valido Fiskaly** da `mapToFiskalyPaymentType`).
  Ogni elemento:
  - **type** — `string`: uno di `"CASH"`, `"CARD"`, `"ONLINE"`, `"OTHER"`, `"VOUCHER"`,
    `"OUTSTANDING"` (una mode libera come `"BANK TRANSFER"` viene mappata a `ONLINE` con etichetta in
    `name`). Candidato a enumerazione (`FiskalyPaymentType`).
  - **details** — `object`: `{ amount: string, discount?: string }` — importo lordo e sconto.
    Es.: `{"amount":"75.00","discount":"0.00"}`.
  - **name** — `string` (opzionale): etichetta descrittiva per `ONLINE`/`OTHER` (es.
    `"Pagamento e-commerce"`, `"Bonifico bancario"`).
  - **number**, **kind** — `string` (opzionali): per `CARD` (`kind` `"CREDIT"`/`"DEBIT"`, `number`
    mascherato `"****"`) e `VOUCHER` (`kind` `"SINGLE"`).
  - **concept** — `string` (opzionale): per `OUTSTANDING` (es. `"SERVICE"`).
  - *(nota: negli scontrini creati via `auto-create` il tipo è sempre `ONLINE` /
    `"Pagamento e-commerce"`.)*
- **Esempio di valori**:
  `[{"type":"ONLINE","details":{"amount":"75.00","discount":"0.00"},"name":"Pagamento e-commerce"}]`

### paid_deltas
- **Tipo**: `longtext` — **oggetto JSON** (`CHECK (json_valid(paid_deltas))`); default model `null`
- **Nullabile**: Sì
- **Descrizione**: delta di "pagato" per voce che **questo** scontrino applica a
  [`booking_number_informations`](booking_number_informations.md), conservato per poter **revertire
  esattamente** l'effetto in caso di annullamento (`revertBniForCancelledReceipt`). **Azzerato a
  `null`** una volta revertito. Struttura:
  - **items** — `array`: `[{ id: string, type: string, lastPaid: number }]` — id della voce (UUID),
    tipo voce (es. `"package"`, `"bar_service"`, `"agreement_voucher_package"`, `"surcharge_fixed"`,
    …) e importo che questo scontrino ha "pagato" su quella voce.
  - **payment** — `object|null`: `{ amount: number, date: string (ISO), paymentMethod: string,
    source: string }` — l'acconto/incasso che questo scontrino ha fatturato (per liberarlo in caso
    di revert). `source` osservato: `"paidAmountData"`. `paymentMethod` osservato: `"CREDIT CARD"`.
- **Esempio di valori**:
  `{"items":[{"id":"c1edfc58-…","type":"package","lastPaid":75}],"payment":{"amount":75,"date":"2026-07-27T07:05:06.908Z","paymentMethod":"CREDIT CARD","source":"paidAmountData"}}`
  · `null`

### record_state
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: stato interno del flusso di emissione/annullamento (**non** è lo stato Fiskaly
  grezzo — quello è in `fiskaly_response.state`). Valori scritti dal codice:
  `INTENTION_CREATED` → `TRANSACTION_CREATED`/`TRANSACTION_FAILED` → (`ERECEIPT_FAILED`/
  `S3_UPLOAD_FAILED`) → `COMPLETED`; per l'annullamento `CANCELLATION_INTENTION_CREATED` →
  `CANCELLATION_TRANSACTION_FAILED` → `CANCELLED`. Allo step 5 può anche assumere direttamente il
  valore `content.state` di Fiskaly (es. `COMPLETED`). `getReceipts` mostra **solo**
  `record_state = 'COMPLETED'`.
- **Candidato a enumerazione**: **Sì**, `FiskalyReceiptRecordState` (enum interno del modulo Fiscale;
  la costante `FiskalyReceiptState` in `fiskaly_consts.js` è **disallineata** dai valori reali).
- **Distribuzione reale di produzione** (2.596 righe): `COMPLETED` 2.399 (92%), `CANCELLED` 110,
  `TRANSACTION_FAILED` 53, `INTENTION_CREATED` 23 (rimaste appese), `CANCELLATION_INTENTION_CREATED` 8,
  `CANCELLATION_TRANSACTION_FAILED` 3 → ~2% degli scontrini elettronici fallisce o resta bloccato in
  stato intermedio.
- **Esempio di valori** (prod): `"COMPLETED"`, `"CANCELLED"`, `"TRANSACTION_FAILED"`.

### record_mode
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: `mode` del record TRANSACTION restituito da Fiskaly
  (`transactionResponse.content.mode`). `getReceipts` filtra `record_mode = 'FINISHED'`.
- **Candidato a enumerazione**: **Sì** (`FiskalyRecordMode`; nel campione un solo valore).
- **Esempio di valori**: `"FINISHED"`

### e_receipt_id
- **Tipo**: `varchar(36)` — UUID v4. `UNIQUE` (`e_receipt_id`, indici `_2`/`_3`)
- **Nullabile**: Sì
- **Descrizione**: identificativo dell'*eReceipt* / PDF (generato lato backend con `uuidv4()`), usato
  come **nome file** su S3 (`receipts/fiskaly/<e_receipt_id>.pdf`).
- **Esempio di valori**: `"0d10ac46-9c75-4202-b619-726748666dda"`

### e_receipt_path
- **Tipo**: `varchar(500)`
- **Nullabile**: Sì (NULL se l'upload S3 non è ancora riuscito — stato `S3_UPLOAD_FAILED`)
- **Descrizione**: **URL completo** del PDF su S3. `getReceiptPdf` lo restituisce così com'è.
- **Esempio di valori**:
  `"https://summerbooking.s3.us-east-2.amazonaws.com/receipts/fiskaly/0d10ac46-….pdf"`
- **Nota riscrittura**: URL completo salvato a DB → vedi
  [`docs/03-gestione-dati.md` §1](../../progettazione/03-gestione-dati.md) (`origin` + `path`).

### fiskaly_version
- **Tipo**: `varchar(20)`
- **Nullabile**: Sì
- **Deprecato**: **Sì** — colonna dichiarata nel model ma **mai scritta né letta** in tutto
  `back-pre` (0 righe valorizzate su 100). Da non migrare.
- **Descrizione**: (previsto) versione dell'API Fiskaly usata per il documento.
- **Esempio di valori**: `null`

### fiskaly_response
- **Tipo**: `longtext` — **oggetto JSON** (`CHECK (json_valid(fiskaly_response))`)
- **Nullabile**: Sì
- **Descrizione**: **risposta grezza completa** di Fiskaly per il record TRANSACTION
  (`transactionResponse.content`), o il content dello stato `FAILED`. Serve da audit/diagnostica.
  Campi principali osservati:
  - **id** — `string`: id del record (= `record_id`).
  - **type** — `string`: es. `"TRANSACTION::RECEIPT"`.
  - **state** — `string`: stato Fiskaly grezzo (es. `"COMPLETED"`, `"FAILED"`).
  - **mode** — `string`: es. `"FINISHED"`.
  - **system** — `object`: `{ id: string }` (= `system_id`).
  - **record** — `object`: `{ id: string }` (id dell'INTENTION consumato).
  - **used_in**, **replayed_by** — `null` (osservati).
  - **journal** — `object`: `{ sequence: number, hash: string }`.
  - **file** — `object`: `{ artifact: null, integrity: null }` (osservati).
  - **operation** — `string`: JSON serializzato dell'operazione firmata (document/entries/payments).
- **Esempio di valori**:
  `{"id":"019fa264-…","type":"TRANSACTION::RECEIPT","state":"COMPLETED","mode":"FINISHED","system":{"id":"019e8dc7-…"},"record":{"id":"019fa264-…"},"journal":{"sequence":0,"hash":""}, "operation":"{\"document\":{\"number\":\"5\",…}}"}`

### currency
- **Tipo**: `varchar(3)` — `DEFAULT 'EUR'`
- **Nullabile**: Sì
- **Descrizione**: valuta del documento. Sempre `"EUR"` (hardcoded nel builder).
- **Candidato a enumerazione**: sì, ISO 4217 (di fatto costante `EUR`).
- **Esempio di valori**: `"EUR"`

### signed_date
- **Tipo**: `datetime`
- **Nullabile**: Sì (NULL finché non firmato)
- **Descrizione**: istante di firma (`moment.utc().tz('Europe/Rome')`), salvato come `Date`. Usato
  come data documento nel PDF (formato `DD/MM/YYYY HH:mm`), in `getReceiptPdf` e nei messaggi.
- **Esempio di valori**: `2026-07-27 07:05:45`

### languageCode
- **Tipo**: `varchar(5)` — `DEFAULT 'it'`
- **Nullabile**: Sì
- **Descrizione**: lingua del PDF/eReceipt (`requestDto.languageCode || 'it'`). Nel campione sempre
  `"it"`. Per l'invio, gli endpoint `send` accettano anche `1` (en) / `2` (it).
- **Candidato a enumerazione**: **Sì**, `LanguageCode { It, En }` — da unificare la doppia
  convenzione stringa (`"it"`/`"en"`) vs numerica (`2`/`1`).
- **Esempio di valori**: `"it"`

### emailSent
- **Tipo**: `tinyint`, `NOT NULL`, `DEFAULT 0`
- **Nullabile**: No
- **Descrizione**: `1` dopo un invio riuscito via email (`sendViaEmail` fa
  `FiskalyReceipt.update({ emailSent: 1 })`). Nel campione: sempre `0` (nessun invio email
  registrato).
- **Candidato a enumerazione**: **booleano**.
- **Esempio di valori**: `0`

### whatsappSent
- **Tipo**: `tinyint`, `DEFAULT 0`
- **Nullabile**: Sì
- **Descrizione**: `1` dopo la generazione del deep-link WhatsApp (`sendViaWhatsApp` fa
  `update({ whatsappSent: 1 })`). Nel campione: `1` per 48 righe, `0` per 52.
- **Candidato a enumerazione**: **booleano**.
- **Esempio di valori**: `0`, `1`

### source
- **Tipo**: `varchar(20)`, `NOT NULL`, `DEFAULT 'other'`
- **Nullabile**: No
- **Descrizione**: origine dello scontrino: `"other"` = creato manualmente dall'operatore
  (`create` / `createReceiptFromBni`); `"autoreceipt"` = creato automaticamente dopo un pagamento
  Stripe andato a buon fine (`createAutoReceiptFromStripe`, `_source: 'autoreceipt'`).
- **Candidato a enumerazione**: **Sì**, `ReceiptSource { Manual, AutoReceipt }`.
- **Esempio di valori**: `"other"` (47), `"autoreceipt"` (53)

### createdBy
- **Tipo**: `int`
- **Nullabile**: Sì (**NULL per gli scontrini automatici** — `createReceipt(requestDto, null, …)`)
- **Relazioni**: `users.id` (nessuna FK). Operatore che ha creato lo scontrino.
- **Descrizione**: nel target → `user_operator_id`, nullable per l'automatismo
  ([`docs/03-gestione-dati.md` §3.2](../../progettazione/03-gestione-dati.md)).
- **Esempio di valori**: `2823`, `3218`, `null`

### createdAt
- **Tipo**: `datetime`, `NOT NULL` — `defaultValue: Sequelize.NOW`
- **Nullabile**: No
- **Descrizione**: creazione riga (inizio flusso, stato `INTENTION_CREATED`). `getReceipts` /
  `getNonFiscalReceipts` ordinano e filtrano per intervallo su `createdAt`.
- **Esempio di valori**: `2026-07-27 07:05:45`

### updatedAt
- **Tipo**: `datetime`, `NOT NULL` — `defaultValue: Sequelize.NOW`
- **Nullabile**: No
- **Descrizione**: aggiornato manualmente a ogni `receipt.update(...)` dei vari step.
- **Esempio di valori**: `2026-07-27 07:05:46`

---

## Osservazioni verificate nel codice (criticità)

- **Importi come `varchar`**: `amount_total`, `amount_vat`, `amount_net` e tutti i numeri dentro
  `entries`/`payments` sono stringhe. Nel target `numeric(12,2)` /
  interi in centesimi ([`docs/03-gestione-dati.md` §3.7](../../progettazione/03-gestione-dati.md)).
- **`document_number` non è un identificativo**: è una sequenza per (cassa × giorno), riusata ogni
  giorno e condivisa da molte righe. Il resume corretto è per `id`. In produzione ha già causato il
  resume dello scontrino sbagliato (commento nel codice: `document_number "2"` su 14 righe).
- **`record_state` incoerente**: enum interno + eventuale passthrough del `content.state` Fiskaly
  nella stessa colonna; la costante `FiskalyReceiptState` (`fiskaly_consts.js`) elenca valori
  (`NEW`, `PRINTED`, `TRANSACTION_CANCELLED`, …) che **non** corrispondono a quelli scritti.
- **`fiskaly_version` colonna morta** (mai usata) — non migrare.
- **URL S3 completo** in `e_receipt_path` (500 char) — scomporre in `origin` + `path`
  ([`docs/03-gestione-dati.md` §1](../../progettazione/03-gestione-dati.md)).
- **Nessun `property_id`** sulla tabella: lo scoping passa per `system_id →
  fiskaly_cash_register → fiskaly_account.property_id`, oppure per `getPropertyIdByBookingNumber`
  (due query aggiuntive su `bookings`/`service_reservation`). Nel target conviene copiare
  `property_id` sulla riga (snapshot).
- **`emailSent`/`whatsappSent` come flag booleani** su tinyint con casing camel; il "sent" WhatsApp
  è impostato alla *generazione del link* `wa.me`, non a un invio reale confermato.
- **`entries`/`payments`/`vat` con vocabolari come stringhe libere** (`SALE`/`RETURN`,
  `SERVICE`/`GOODS`, `VAT_RATE`/`VAT_EXEMPTION`, `STANDARD`/`REDUCED_*`, tipi pagamento) — vedi
  Enumerazioni.
- **Doppio significato di `amount_total`**: è il **netto** pagato (lordo − sconto), mentre
  `amount_vat`/`amount_net` sono **lordi**; l'aggiustamento sconto per l'IVA è fatto solo nel
  renderer PDF. Divergenza tra i tre importi salvati.
- **Nessun soft-delete**: l'annullamento è modellato come stato (`CANCELLED`), non come
  `deleted_at`; corretto per un fatto fiscale, ma va reso esplicito nel modello.
