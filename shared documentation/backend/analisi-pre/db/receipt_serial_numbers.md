# Tabella `receipt_serial_numbers`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **4.476 righe**, query
> completa; stessi 3-4 stabilimenti di [`booking_receipts`](booking_receipts.md): `propertyId` 10742
> (2.963), 232 (1.386), 10746 (126), 179 (1)). **`last_receipt_number` è `"001"` su tutte le 4.476
> righe** → il progressivo non incrementa mai (bug confermato dai dati). Model
> [`back-pre/app/models/receiptSerialNo.model.js`](../../../../../../back-pre/app/models/receiptSerialNo.model.js)
> (`sequelize.define("receipt_serial_number", …)` senza `freezeTableName` → tabella **pluralizzata**
> `receipt_serial_numbers`; senza `timestamps: false` → Sequelize gestisce `createdAt`/`updatedAt`,
> `NOT NULL` a schema),
> unico uso in
> [`printerConfig.controller.js`](../../../../../../back-pre/app/controllers/printerConfig.controller.js)
> `saveReceiptdata` (endpoint `POST /api/sb/save-receipt-data`, `authJwt.optionalAuth`):
> - `receiptSerialNumber.findAll({ where: { last_updated_date: req.body.receipt_date }, limit: 1,
>   plain: true, order: [["id", "DESC"]] })`;
> - se trovata: `receiptlast = +result.last_receipt_number + 1`, `finalreceipt = "00" + receiptlast`,
>   `receiptSerialNumber.update({ propertyId, last_receipt_number: finalreceipt, last_updated_date:
>   receipt_created_date }, { where: { last_updated_date: result.last_updated_date } })`
>   — **`receipt_created_date` è una variabile non definita → `ReferenceError`** (vedi criticità);
> - se non trovata: `finalreceipt = "001"`, `receiptSerialNumber.create({ propertyId,
>   last_receipt_number: "001", last_updated_date: todaydate })`;
> - poi `BookingReceipt.create({ …, receipt_number: finalreceipt, document_no: finalreceipt })`
>   ([`booking_receipts`](booking_receipts.md)).
> Nessuna associazione ORM in [`models/index.js`](../../../../../../back-pre/app/models/index.js).
> Frontend: `front-pre/src/components/printerSetting/PrintReservationCta.js` chiama
> `_saveReceipt({ propertyId, printerId, receipt_date: result.receiptInfo.fiscalReceiptDate,
> receipt_object })` **dopo** la stampa di uno scontrino fiscale su **stampante termica RT fisica**
> (adapter Epson TM / RCH in `src/services/printers/`), solo se `!currentPrinter.isSoftware`.
>
> **Nota di contesto** — doveva essere il **contatore del numero progressivo dello scontrino stampato
> sulla stampante fiscale RT fisica**, per stabilimento e per giorno (analogo a
> [`fiskaly_receipt_sequence`](fiskaly_receipt_sequence.md) per il flusso Fiskaly, e a
> [`booking_logs`](booking_logs.md) per il numero prenotazione). **Di fatto non incrementa mai**: per
> i bug descritti sotto, ogni stampa crea una nuova riga con `last_receipt_number = "001"`. Nel
> campione: 100 righe, tutte `propertyId = 10742`, `last_receipt_number = "001"`, `last_updated_date`
> tra `2026-07-23` e `2026-07-27` (una riga per stampa).
>
> **Dominio nella riscrittura**: **C4bis — Stampa fiscale su hardware RT** (feature a sé): **NON**
> C3 (Payment/Stripe) né C4 (`Fiskaly`/SIGN IT, che ha la sua
> [`fiskaly_receipt_sequence`](fiskaly_receipt_sequence.md)). È il meccanismo *alternativo* — più
> vecchio — per emettere lo scontrino fiscale, cioè la stampa su registratore di cassa RT fisico
> (adapter Epson/RCH lato client). Questa tabella è alimentata **solo** da
> [`printerConfig.controller.js`](../../../../../../back-pre/app/controllers/printerConfig.controller.js)
> `saveReceiptdata`, accanto a [`booking_receipts`](booking_receipts.md) e
> [`printer_configs`](printer_configs.md) (le sole 3 tabelle di C4bis). Nel target: **si mantiene**
> come componente separato (es. `FiscalPrinting`) — decisione di business del committente,
> indipendente dalla copertura dello scontrino elettronico Fiskaly; va **riprogettato** come vera
> sequenza `(property_id, fiscal_date) → last_number` con incremento atomico. Vedi
> [`_feature-tabelle.md`](_feature-tabelle.md) → "C4bis — Stampa fiscale su hardware RT" e
> [`docs/03-gestione-dati.md` §4](../../progettazione/03-gestione-dati.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### propertyId
- **Tipo**: `int`
- **Nullabile**: Sì (a schema; valorizzato da `req.body.propertyId`)
- **Relazioni**: `properties.id` — **nessuna FK reale**, nessuna associazione ORM. **Non** usato come
  filtro nella ricerca (`findAll` filtra solo per `last_updated_date`, vedi criticità).
- **Descrizione**: stabilimento che ha effettuato la stampa.
- **Esempio di valori**: `10742` (unico valore nel campione parziale)

### last_receipt_number
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci` — **numero come stringa**
- **Nullabile**: Sì
- **Descrizione**: ultimo numero progressivo scontrino "emesso" quel giorno. Formattato con padding a
  mano: `"001"` alla prima stampa, poi `"00" + (n+1)` nel ramo di update (mai raggiunto per il bug).
  Copiato in `booking_receipts.receipt_number` / `document_no`. Nei dati: **sempre `"001"`**.
- **Esempio di valori**: `"001"`

### last_updated_date
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci` — **data come stringa**
- **Nullabile**: Sì
- **Descrizione**: giorno a cui si riferisce il contatore. In creazione è
  `moment(new Date()).format("YYYY-MM-DD")` (`todaydate`); in ricerca si confronta per uguaglianza
  esatta con `req.body.receipt_date` (= `result.receiptInfo.fiscalReceiptDate`, la data/ora
  restituita dalla stampante RT — formato **non garantito** `YYYY-MM-DD`, spesso include l'ora →
  il match fallisce sempre → nuova riga `"001"`).
- **Esempio di valori**: `"2026-07-23"`, `"2026-07-27"`

### createdAt
- **Tipo**: `datetime`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: creazione riga (gestita da Sequelize; il model non disabilita `timestamps`). È
  l'unico campo che riflette il timestamp reale della stampa.
- **Esempio di valori**: `2026-07-23 12:16:05`, `2026-07-27 08:18:02`

### updatedAt
- **Tipo**: `datetime`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: ultimo aggiornamento. Nei dati sempre uguale a `createdAt` (il ramo di update non
  viene mai eseguito con successo).
- **Esempio di valori**: `2026-07-23 12:16:05`

---

## Osservazioni verificate nel codice (criticità)

- **Il contatore non incrementa mai**: due bug concorrenti.
  1. `ReferenceError` — nel ramo "riga trovata", `update({ …, last_updated_date: receipt_created_date })`
     usa una variabile **non definita**: l'update lancia.
  2. Match fragile — `findAll` confronta `last_updated_date` (`"YYYY-MM-DD"`) per **uguaglianza
     stringa** con `req.body.receipt_date`, che arriva dalla stampante RT in un formato diverso
     (tipicamente con ora): non matcha mai → si va sempre nel ramo `create` con `"001"`.
  Risultato: ~1 riga per stampa, tutte `"001"` (confermato dai dati).
- **`findAll` senza filtro `propertyId`**: il contatore è cercato **globalmente** per
  `last_updated_date`, non per stabilimento. Semanticamente sbagliato (ogni stabilimento ha la sua
  serie).
- **Date e numeri come `varchar(255)`**: `last_updated_date` dovrebbe essere `date`,
  `last_receipt_number` un intero (`numeric`/`int`) con formattazione a display.
- **Nessun `UNIQUE(property_id, date)`**: non c'è vincolo che impedisca righe multiple per lo stesso
  (stabilimento, giorno) — infatti ce ne sono a migliaia.
- **Nessuna FK** verso `properties`; nessuna verso `booking_receipts` (il legame è solo il valore
  `receipt_number` copiato).
- **`AUTO_INCREMENT=3072`** con contatore fermo a `"001"`: la tabella è pura crescita di righe inutili.
- **`saveReceiptdata` scrive `BookingReceipt` con molti campi `undefined`** (`bookingId`,
  `printer_request_object`, `printer_response_object`) perché il frontend non li invia — vedi
  [`booking_receipts.md`](booking_receipts.md).
- Nessun campo è un enumeratore.
