# Tabella `booking_receipts`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **4.476 righe**, query
> completa), model
> [`back-pre/app/models/bookingReceipt.model.js`](../../../../../../back-pre/app/models/bookingReceipt.model.js)
> (`sequelize.define("booking_receipt", ...)` → tabella `booking_receipts`; ORM `db.booking_receipt`;
> importa `sequelize-slugify` ma non lo usa),
> [`printerConfig.controller.js`](../../../../../../back-pre/app/controllers/printerConfig.controller.js) e
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js) (stampa scontrino
> via stampante di rete configurata nella tabella [`printer_configs`](printer_configs.md));
> numero progressivo da [`receipt_serial_numbers`](receipt_serial_numbers.md); associazioni
> ORM tutte **commentate** in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (righe ~329-332).
> Frontend `front-pre` (solo gestionale): flusso "stampa comanda / ricevuta di cassa" per stampanti
> termiche (adapter Epson TM/FP, RCH in `src/services/printers/`).
>
> **Nota di contesto** — registro degli **scontrini/ricevute stampati su stampante termica di rete**
> (non lo scontrino fiscale elettronico Fiskaly, che sta in `fiskaly_receipt*`). Ogni riga conserva
> l'oggetto scontrino inviato alla stampante e la risposta del driver. **In produzione `bookingId` è
> `NULL` su tutte le 4.476 righe**, `receipt_object` contiene sempre l'esito di una stampa fiscale RT
> (registratore telematico) — quindi la tabella è **esclusivamente** un log di stampe di cassa, mai
> collegato a una prenotazione. Uso concentrato su **3-4 stabilimenti**: `propertyId` 10742 (2.963
> righe), 232 (1.386), 10746 (126), 179 (1).
>
> **Dominio nella riscrittura**: **C4bis — Stampa fiscale su hardware RT** (feature a sé): **non**
> C3 (Payment/Stripe) né C4 (`Fiskaly`/SIGN IT — scontrino elettronico). Insieme a
> [`printer_configs`](printer_configs.md) e [`receipt_serial_numbers`](receipt_serial_numbers.md) è
> una delle 3 tabelle del percorso legacy di stampa su registratore di cassa RT fisico, gestito
> quasi tutto lato client. Nel target: **si mantiene** come componente separato (es. `FiscalPrinting`)
> — decisione di business del committente, indipendente dalla copertura dello scontrino elettronico
> Fiskaly. Vedi
> [`_feature-tabelle.md`](_feature-tabelle.md) → "C4bis — Stampa fiscale su hardware RT" e
> [`docs/03-gestione-dati.md` §4](../../progettazione/03-gestione-dati.md) *(dove la riga `booking_receipts`
> era classificata "C3" con la vecchia etichetta unica)*.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `4.476` (contiguo).

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
- **Esempio di valori** (prod): **`"001"` su tutte le 4.476 righe** — il progressivo non incrementa mai
  (conferma pratica dei bug sopra: il `+1` non produce mai un valore diverso da `001`).

### propertyId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK)
- **Descrizione**: solo 4 valori distinti in produzione (10742: 2.963, 232: 1.386, 10746: 126, 179: 1).
- **Esempio di valori** (prod): `10742`, `232`, `10746`.

### bookingId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK; associazione ORM commentata). **In produzione è `NULL` su
  tutte le 4.476 righe** → nessuna stampa è legata a una prenotazione: il nome della colonna (e della
  tabella) è fuorviante.
- **Esempio di valori** (prod): `NULL` (sempre).

### receipt_print_date
- **Tipo**: `varchar(255)` — data come **stringa** `D/M/YYYY`
- **Nullabile**: Sì
- **Esempio di valori** (prod): `"31/5/2023"`, `"1/6/2023"`, `"2/6/2023"`.

### receipt_object
- **Tipo**: `longtext` — JSON (oggetto o array)
- **Nullabile**: Sì
- **Descrizione**: contenuto/esito dello scontrino. Nel campione contiene la risposta di un
  registratore telematico: `lastCommand`, `printerStatus`, `fiscalReceiptNumber`,
  `fiscalReceiptAmount` (importo con virgola decimale, es. `"30,00"`), `fiscalReceiptDate`,
  `fiscalReceiptTime`, `receiptISODateTime` (es. `"20260727T101700"`), `zRepNumber`, `serialNumber`
  (matricola stampante).
- **Esempio di valori** (prod):
  `[{"lastCommand":"74","printerStatus":"00110","fiscalReceiptNumber":"5","fiscalReceiptAmount":"5,00","fiscalReceiptDate":"31/5/2023","fiscalReceiptTime":"14:46","zRepNumber":"2"}]`

### printer_request_object
- **Tipo**: `longtext` — JSON
- **Nullabile**: Sì
- **Descrizione**: payload di comando inviato alla stampante/driver (`req.body.printer_request_object`).
- **Produzione**: **`NULL` su tutte le 4.476 righe** (`sb_prod_dump_2026_09_08`) — colonna mai
  popolata.

### printer_response_object
- **Tipo**: `longtext` — JSON
- **Nullabile**: Sì
- **Descrizione**: risposta grezza del driver/stampante (`req.body.printer_response_object`).
- **Produzione**: **`NULL` su tutte le 4.476 righe** — colonna mai popolata.

### document_no
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: numero documento — in produzione `"001"` su tutte le righe (uguale a
  `receipt_number`).
- **Esempio di valori** (prod): `"001"` (sempre).

### printerId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: [`printer_configs.id`](printer_configs.md) (nessuna FK; associazione ORM commentata)
- **Esempio di valori** (prod): `48` (2.634), `4` (1.386), `21` (329) — pochi valori distinti,
  coerente con i 3-4 stabilimenti che usano la feature.

### createdAt / updatedAt
- **Tipo**: `datetime` — `NOT NULL` a schema (timestamp Sequelize standard)
- **Nullabile**: No
- **Esempio di valori** (prod): range `2023-05-31 12:48:10` … `2026-09-08 08:55:08` (la feature è ancora
  in uso fino all'export, ma solo per pochissimi stabilimenti).

---

## Osservazioni verificate nel codice (criticità)

- **Ambito non solo prenotazioni**: `bookingId` è **`NULL` su tutte le 4.476 righe** di produzione; la
  tabella è puramente un log di stampe di cassa (RT/registratore telematico) per ~3-4 stabilimenti.
  Nessun legame con l'aggregato Booking.
- **`receipt_number` / `document_no` sempre `"001"`** in produzione: la generazione del progressivo non
  funziona (bug confermato dai dati).
- **`printer_request_object` / `printer_response_object` `NULL` su tutte le righe** → colonne di fatto
  inutilizzate, da non riportare.
- **Date come stringhe** in formato locale (`"27/7/2026"`, `"20260727T101700"`) → nessun ordinamento
  temporale affidabile a DB.
- **Importi con virgola decimale in stringa** (`"30,00"`) dentro `receipt_object`.
- **`sequelize-slugify` importato ma non usato**; associazioni ORM verso `property`/`booking`/`printer`
  tutte commentate → nessuna integrità referenziale, nemmeno lato ORM.
- **Duplicazione concettuale con `fiskaly_receipt*`**: lo scontrino "fiscale" vero è nel modulo
  Fiskaly; qui c'è un secondo registro di stampe RT non integrato con quello.
