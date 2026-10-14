# Tabella `booking_receipts`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/bookingReceipt.model.js`](../../back-pre/app/models/bookingReceipt.model.js)
> (`sequelize.define("booking_receipt", ...)` → tabella `booking_receipts`; ORM `db.booking_receipt`;
> importa `sequelize-slugify` ma non lo usa),
> [`printerConfig.controller.js`](../../back-pre/app/controllers/printerConfig.controller.js) e
> [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js) (stampa scontrino
> via stampante di rete configurata nella tabella [`printer_configs`](printer_configs.md));
> numero progressivo da [`receipt_serial_numbers`](receipt_serial_numbers.md); associazioni
> ORM tutte **commentate** in [`models/index.js`](../../back-pre/app/models/index.js) (righe ~329-332).
> Frontend `front-pre` (solo gestionale): flusso "stampa comanda / ricevuta di cassa" per stampanti
> termiche (adapter Epson TM/FP, RCH in `src/services/printers/`).
>
> **Nota di contesto** — registro degli **scontrini/ricevute stampati su stampante termica di rete**
> (non lo scontrino fiscale elettronico Fiskaly, che sta in `fiskaly_receipt*`). Ogni riga conserva
> l'oggetto scontrino inviato alla stampante e la risposta del driver. Nel campione la maggior parte
> delle righe ha `bookingId = NULL` e `receipt_object` con esito di una stampa fiscale RT (registratore
> telematico) di terze parti — quindi la tabella è usata anche come log generico di stampe di cassa,
> non solo per prenotazioni.
>
> **Dominio nella riscrittura**: **C4bis — Stampa fiscale su hardware RT** (feature a sé): **non**
> C3 (Payment/Stripe) né C4 (`Fiskaly`/SIGN IT — scontrino elettronico). Insieme a
> [`printer_configs`](printer_configs.md) e [`receipt_serial_numbers`](receipt_serial_numbers.md) è
> una delle 3 tabelle del percorso legacy di stampa su registratore di cassa RT fisico, gestito
> quasi tutto lato client. Nel target: componente separato (es. `FiscalPrinting`) **oppure dismesso**
> se lo scontrino elettronico Fiskaly copre tutti gli stabilimenti. Vedi
> [`_feature-tabelle.md`](_feature-tabelle.md) → "C4bis — Stampa fiscale su hardware RT" e
> [`docs/03-gestione-dati.md` §4](../../docs/03-gestione-dati.md) *(dove la riga `booking_receipts`
> era classificata "C3" con la vecchia etichetta unica)*.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### receipt_number
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione / verifica generazione**: **autogenerato**, non digitato dall'operatore. In
  `printerConfig.controller.saveReceiptdata` (chiamato dal frontend dopo una stampa): legge l'ultima
  riga della tabella [`receipt_serial_numbers`](receipt_serial_numbers.md) per la data
  (`where: { last_updated_date: <receipt_date> }`), fa `+result.last_receipt_number + 1`, e formatta
  come `"00" + n` → `"001"`, `"002"`, ... (padding fisso di 2 zeri → `"0010"`, `"00100"`, buggy per
  n≥10); se non c'è riga per quella data riparte da `"001"`. `document_no` riceve lo **stesso** valore.
  Bug rilevati: (a) il progressivo si azzera ogni giorno; (b) la lookup su `receipt_serial_numbers`
  è **per sola data**, non per `propertyId` → il contatore è **condiviso tra tutti gli stabilimenti**
  per quella data; (c) il ramo "nuova data" usa `todaydate` mentre il ramo "data esistente" usa
  `receipt_created_date` (variabile non definita).
- **Esempio di valori**: `"001"`, `"002"`, `"0012"`

### propertyId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK)
- **Esempio di valori**: 10742

### bookingId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK; associazione ORM commentata). **Nel campione è quasi sempre
  `NULL`** → molte stampe non sono legate a una prenotazione.
- **Esempio di valori**: `269319` (quando la stampa parte dal dettaglio di una prenotazione); `null`
  (stampa "libera" di cassa)

### receipt_print_date
- **Tipo**: `varchar(255)` — data come **stringa** `D/M/YYYY`
- **Nullabile**: Sì
- **Esempio di valori**: `"27/7/2026"`

### receipt_object
- **Tipo**: `longtext` — JSON (oggetto o array)
- **Nullabile**: Sì
- **Descrizione**: contenuto/esito dello scontrino. Nel campione contiene la risposta di un
  registratore telematico: `lastCommand`, `printerStatus`, `fiscalReceiptNumber`,
  `fiscalReceiptAmount` (importo con virgola decimale, es. `"30,00"`), `fiscalReceiptDate`,
  `fiscalReceiptTime`, `receiptISODateTime` (es. `"20260727T101700"`), `zRepNumber`, `serialNumber`
  (matricola stampante).
- **Esempio di valori**:
  `[{"lastCommand":"74","printerStatus":"00110","fiscalReceiptNumber":"9","fiscalReceiptAmount":"30,00","fiscalReceiptDate":"27/7/2026","fiscalReceiptTime":"10:17","receiptISODateTime":"20260727T101700","zRepNumber":"285","serialNumber":"99IEB126700"}]`

### printer_request_object
- **Tipo**: `longtext` — JSON
- **Nullabile**: Sì
- **Descrizione**: payload di comando inviato alla stampante/driver (`req.body.printer_request_object`).
  **Sempre `null` nel campione** → di fatto non popolato.
- **Esempio di valori** (forma indicativa):
  `{"command":"printFiscalReceipt","lines":[{"description":"Ombrellone","qty":1,"amount":"30,00","vat":"22"}],"payment":{"type":"cash","amount":"30,00"}}`
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### printer_response_object
- **Tipo**: `longtext` — JSON
- **Nullabile**: Sì
- **Descrizione**: risposta grezza del driver/stampante (`req.body.printer_response_object`). **Sempre
  `null` nel campione**.
- **Esempio di valori** (forma indicativa):
  `{"success":true,"httpStatus":200,"raw":"<response status=\"00\"/>"}`
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### document_no
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: numero documento (nel campione `"001"`, uguale a `receipt_number`).
- **Esempio di valori**: `"001"`

### printerId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: [`printer_configs.id`](printer_configs.md) (nessuna FK; associazione ORM commentata)
- **Esempio di valori**: 48

### createdAt / updatedAt
- **Tipo**: `datetime` — `NOT NULL` a schema (timestamp Sequelize standard)
- **Nullabile**: No
- **Esempio di valori**: `2026-07-27 08:18:02`

---

## Osservazioni verificate nel codice (criticità)

- **Ambito non solo prenotazioni**: `bookingId` quasi sempre `NULL`; la tabella funge da log generico
  di stampe di cassa (RT/registratore telematico) per property. Il legame con l'aggregato Booking è
  debole.
- **`printer_request_object` / `printer_response_object` mai popolati** nel campione → colonne di fatto
  inutilizzate (o popolate solo in scenari di errore non presenti nei dati).
- **Date come stringhe** in formato locale (`"27/7/2026"`, `"20260727T101700"`) → nessun ordinamento
  temporale affidabile a DB.
- **Importi con virgola decimale in stringa** (`"30,00"`) dentro `receipt_object`.
- **`sequelize-slugify` importato ma non usato**; associazioni ORM verso `property`/`booking`/`printer`
  tutte commentate → nessuna integrità referenziale, nemmeno lato ORM.
- **Duplicazione concettuale con `fiskaly_receipt*`**: lo scontrino "fiscale" vero è nel modulo
  Fiskaly; qui c'è un secondo registro di stampe RT non integrato con quello.
