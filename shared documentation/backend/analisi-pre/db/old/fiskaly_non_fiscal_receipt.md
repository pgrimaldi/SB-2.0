# Tabella `fiskaly_non_fiscal_receipt`

> Fonti: schema e dati reali (13 righe, `AUTO_INCREMENT=14`), model
> [`back-pre/app/models/fiskalyNonFiscalReceipt.model.js`](../../back-pre/app/models/fiskalyNonFiscalReceipt.model.js)
> (`{ freezeTableName: true, timestamps: false }`),
> scrittura in
> [`Service/fiskalyReceiptManagerService.js`](../../back-pre/app/Service/fiskalyReceiptManagerService.js)
> `createNonFiscalReceipt(bookingNumber, userId, propertyId)` (nessuna chiamata a Fiskaly SIGN IT:
> legge [`booking_number_informations`](booking_number_informations.md), costruisce righe/importi con
> [`controllers/helpers/fiskaly.helper.js`](../../back-pre/app/controllers/helpers/fiskaly.helper.js)
> `buildReceiptCreationData`/`setPaidInLineItems`/`getLatestPaymentInfo`, genera il PDF con
> [`utils/receiptPdf.helper.js`](../../back-pre/app/utils/receiptPdf.helper.js) `generateReceiptPdf`
> passando `documentNumber = "0"` e `isFiscal = false`, carica su S3 in `receipts/fiskaly/`);
> lettura in
> [`Service/fiskalyReceiptManagerService.js`](../../back-pre/app/Service/fiskalyReceiptManagerService.js)
> (`getNonFiscalReceipts`, `getNonFiscalReceiptById`) e
> [`Service/fiskalyReceiptReaderService.js`](../../back-pre/app/Service/fiskalyReceiptReaderService.js)
> (`getNonFiscalReceiptPdf`);
> invio via email/WhatsApp in
> [`Service/fiskalyReceiptNotificationService.js`](../../back-pre/app/Service/fiskalyReceiptNotificationService.js)
> (`sendNonFiscalReceipt`, `resolveNonFiscalReceiptWhatsappNumber`);
> controller [`fiskalyReceipt.controller.js`](../../back-pre/app/controllers/fiskalyReceipt.controller.js)
> endpoint `/api/sb/electronic-receipt/non-fiscal/create|list|get/:id|pdf/:receipt_id|send`
> (`create` verifica che il `booking_number` appartenga alla property dell'utente).
> Frontend: `front-modern-pre`
> (`src/remote/SummerBooking/Receipt/ReceiptRestApi.ts` → `electronic-receipt/non-fiscal/*`).
> Nessuna associazione ORM.
>
> **Nota di contesto** — **1 riga per "pre‑conto" / ricevuta NON fiscale (proforma)** generata per una
> prenotazione. È il documento che l'operatore stampa/invia **prima** dello scontrino fiscale, o
> quando serve una copia di cortesia. Stesso motore di calcolo righe/IVA di
> [`fiskaly_receipt`](fiskaly_receipt.md), ma **senza** firma Fiskaly, senza `system_id`, senza
> `document_number` (nel PDF il numero è `0`), senza stati/annullamento. `receiptNumber` mostrato
> all'utente è calcolato a runtime come `"NF-<id>"`. Se lo scontrino fiscale è già stato emesso
> (imponibile residuo ≤ 0) `createNonFiscalReceipt` rifiuta con *"Scontrino già emesso…"*.
>
> **Dominio nella riscrittura**: **C4 — modulo `Fiskaly`** — documento non fiscale/storico;
> verso C1 solo `booking_number`. Analisi domini in
> [`docs/03-gestione-dati.md` §4](../../docs/03-gestione-dati.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Descrizione**: usato come `receipt_id` degli endpoint `get`/`pdf`/`send` e come base del numero
  mostrato all'utente (`"NF-" + id`).
- **Esempio di valori**: 1, 2, 3, ..

### booking_number
- **Tipo**: `varchar(255)` *(più largo del `varchar(50)` usato in `fiskaly_receipt` — incoerenza)*
- **Nullabile**: Sì (a schema; nel codice sempre valorizzato)
- **Relazioni**: `bookings.bookingNumber` / `service_reservation.bookingNumber` (nessuna FK; indice
  `fiskaly_non_fiscal_receipt_booking_number`). Da qui si risale a `property_id` via
  `getPropertyIdByBookingNumber` per l'autorizzazione.
- **Descrizione**: prenotazione a cui si riferisce il pre‑conto.
- **Esempio di valori**: `"1080410652"`, `"1080410035"`, `"1072312564"`

### e_receipt_id
- **Tipo**: `varchar(36)` — UUID v4. `UNIQUE` (`e_receipt_id`, indice `_2`)
- **Nullabile**: Sì
- **Descrizione**: identificativo del PDF (`uuidv4()`), usato come **nome file** su S3
  (`receipts/fiskaly/<e_receipt_id>.pdf`).
- **Esempio di valori**: `"d4b4a0d6-fa07-4b33-a94a-6a24698a7143"`

### e_receipt_path
- **Tipo**: `varchar(500)`
- **Nullabile**: Sì (NULL se l'upload S3 fallisce)
- **Descrizione**: **URL completo** del PDF su S3. `getNonFiscalReceiptPdf` lo restituisce così.
- **Esempio di valori**:
  `"https://summerbooking.s3.us-east-2.amazonaws.com/receipts/fiskaly/d4b4….pdf"`
- **Nota riscrittura**: URL completo a DB → `origin` + `path`
  ([`docs/03-gestione-dati.md` §1](../../docs/03-gestione-dati.md)).

### amount_total
- **Tipo**: `varchar(20)` — importo **come stringa**
- **Nullabile**: Sì
- **Descrizione**: totale del pre‑conto IVA inclusa (`= full_amount.incl_vat` dal builder).
  In email/WhatsApp mostrato come `parseFloat(amount_total) - sconto`.
- **Esempio di valori**: `"12.00"`, `"3000.00"`, `"900.00"`, `"55.00"`

### amount_vat
- **Tipo**: `varchar(20)` — stringa
- **Nullabile**: Sì
- **Descrizione**: totale IVA (`Σ vat_amounts[].vat`).
- **Esempio di valori**: `"2.16"`, `"540.98"`, `"9.92"`

### amount_net
- **Tipo**: `varchar(20)` — stringa
- **Nullabile**: Sì
- **Descrizione**: imponibile (`= full_amount.excl_vat`).
- **Esempio di valori**: `"9.84"`, `"2459.02"`, `"45.08"`

### entries
- **Tipo**: `longtext` — **array JSON** (`CHECK (json_valid(entries))`); default `[]`
- **Nullabile**: Sì
- **Descrizione**: righe del pre‑conto (`= requestDto.line_items`), stessa struttura di
  [`fiskaly_receipt.entries`](fiskaly_receipt.md): elementi
  `{ type: "SALE"|"RETURN", description, category: "SERVICE"|"GOODS", quantity,
  unit_price: { incl_vat, excl_vat }, vat: { type: "VAT_RATE"|"VAT_EXEMPTION", code, rate, amount } }`.
  Nel campione `type` sempre `"SALE"`, `category` sempre `"SERVICE"`, `vat.type` sempre `"VAT_RATE"`,
  `vat.code` sempre `"STANDARD"`, `rate` `"22.00"`.
- **Esempio di valori**:
  `[{"type":"SALE","description":"Pedalò","category":"SERVICE","quantity":"1","unit_price":{"incl_vat":"12.00","excl_vat":"9.84"},"vat":{"type":"VAT_RATE","code":"STANDARD","rate":"22.00","amount":"2.16"}}]`
  · righe multiple es. `"Tenda, 2 lettini, 2 sdraio e sedia regista"` + `"Cabina"`

### payments
- **Tipo**: `longtext` — **array JSON** (`CHECK (json_valid(payments))`); default `[]`
- **Nullabile**: Sì
- **Descrizione**: modalità di pagamento del pre‑conto. **Struttura "piatta"** (diversa da
  `fiskaly_receipt`, che ha `details`): `{ type?: string, amount: string, discount: string }`.
  `type` è mappato a un valore Fiskaly da `mapToFiskalyPaymentType` a partire da
  `latestPaymentInfo.paymentMethod` (BNI). **Drift osservato nei dati storici**: alcune righe hanno
  `type` assente, altre `"BANK TRANSFER"` (non mappato), altre `"CASH"` / `"ONLINE"`.
- **Candidato a enumerazione**: **Sì**, `FiskalyPaymentType` (con normalizzazione del drift).
- **Esempio di valori**: `[{"type":"ONLINE","amount":"12.00","discount":"0.00"}]` ·
  `[{"amount":"3000.00","discount":"0.00"}]` · `[{"type":"BANK TRANSFER","amount":"900.00","discount":"0.00"}]` ·
  `[{"type":"CASH","amount":"55.00","discount":"0.00"}]`

### currency
- **Tipo**: `varchar(3)` — `DEFAULT 'EUR'`
- **Nullabile**: Sì
- **Descrizione**: valuta (`requestDto.currency`, hardcoded `'EUR'`).
- **Esempio di valori**: `"EUR"`

### languageCode
- **Tipo**: `varchar(5)` — `DEFAULT 'it'`
- **Nullabile**: Sì
- **Descrizione**: lingua del PDF (`'it'` hardcoded alla creazione). Gli endpoint `send`
  accettano `it`/`en`/`1`/`2`.
- **Candidato a enumerazione**: **Sì**, `LanguageCode { It, En }` (unificare stringa/numero).
- **Esempio di valori**: `"it"`

### createdBy
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `users.id` (nessuna FK). Operatore che ha generato il pre‑conto (`createdBy: userId`).
- **Descrizione**: nel target → `user_operator_id`
  ([`docs/03-gestione-dati.md` §3.2](../../docs/03-gestione-dati.md)).
- **Esempio di valori**: `3218`, `2823`

### createdAt
- **Tipo**: `datetime`, `NOT NULL` — `defaultValue: Sequelize.NOW`
- **Nullabile**: No
- **Descrizione**: creazione. `getNonFiscalReceipts` filtra per intervallo e ordina `createdAt DESC`.
  Usata come "data ricevuta" nei messaggi.
- **Esempio di valori**: `2026-07-16 11:47:29`, `2026-07-01 13:52:09`

### updatedAt
- **Tipo**: `datetime`, `NOT NULL` — `defaultValue: Sequelize.NOW`
- **Nullabile**: No
- **Descrizione**: di fatto immutabile (la riga non viene aggiornata dopo la creazione).
- **Esempio di valori**: `2026-07-16 11:47:29`

---

## Osservazioni verificate nel codice (criticità)

- **Importi come `varchar`** (`amount_*`, numeri dentro `entries`/`payments`) → `numeric`/centesimi
  nel target ([`docs/03-gestione-dati.md` §3.7](../../docs/03-gestione-dati.md)).
- **`payments` con struttura diversa da `fiskaly_receipt`** (piatta vs con `details`) e **drift** su
  `type` (assente / `"BANK TRANSFER"` / `"CASH"` / `"ONLINE"`): non normalizzato — vedi Enumerazioni.
- **`booking_number varchar(255)`** qui vs `varchar(50)` in `fiskaly_receipt`: allineare.
- **URL S3 completo** in `e_receipt_path` — `origin` + `path`
  ([`docs/03-gestione-dati.md` §1](../../docs/03-gestione-dati.md)).
- **Nessun `property_id`** sulla riga: risoluzione via `getPropertyIdByBookingNumber` (2 query).
- **Numero documento fittizio**: nel PDF è `0`, il "numero" mostrato è `"NF-<id>"` calcolato a
  runtime, non persistito.
- **`createdBy` senza FK**; **timestamp non automatici** (`timestamps: false`).
