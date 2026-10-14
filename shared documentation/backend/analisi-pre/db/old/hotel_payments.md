# Tabella `hotel_payments`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/hotelPayment.model.js`](../../back-pre/app/models/hotelPayment.model.js)
> (ORM `db.hotelPayment`; associazioni in [`models/index.js`](../../back-pre/app/models/index.js):
> `belongsTo(db.hotel)`, `belongsTo(db.booking, { foreignKey: "bookingId" })`,
> `belongsTo(db.hotelPriceList, { foreignKey: "hotelPriceListId" })`), scrittura in
> [`Service/hotelPaymentService.js`](../../back-pre/app/Service/hotelPaymentService.js)
> (`processHotelBookingPayments`, `deletePaymentsByBookingId`, `getPaymentsByBookingIds`,
> `checkVoucherLimit`), invocata dal flusso `booking` / `updateBooking` in
> [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js) quando il payload
> contiene `req.body.hotelBooking`; API dedicata
> [`controllers/hotel/hotelPayment.controller.js`](../../back-pre/app/controllers/hotel/hotelPayment.controller.js);
> alimenta i report in
> [`Service/hotelReportService.js`](../../back-pre/app/Service/hotelReportService.js). Frontend
> `front-pre` (gestionale): sezione "Convenzioni / Hotel" del modale prenotazione
> (`src/components/reserveArticle/**`, `hotelBooking` nel payload).
>
> **Nota di contesto** — parte **transazionale** della feature Convenzioni/Hotel: quando una
> prenotazione ombrellone è associata a una struttura partner (`bookings.hotelId` valorizzato), qui
> viene registrata **una riga per (prenotazione × postazione × voce di listino/voucher hotel)** con lo
> snapshot del prezzo. Logicamente è un dettaglio dell'aggregato Booking (C1), non della
> configurazione hotel (C2 — vedi [`hotels.md`](hotels.md), [`hotel_price_lists.md`](hotel_price_lists.md)).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### hotelId
- **Tipo**: `int`, `NOT NULL` — commento DB: *"Reference to the hotel"*
- **Nullabile**: No
- **Relazioni**: `hotels.id` (`belongsTo`, nessuna FK reale)
- **Esempio di valori**: 6, 101

### hotelPriceListId
- **Tipo**: `int` — commento DB: *"Reference to the hotel price list used"*
- **Nullabile**: Sì
- **Relazioni**: `hotel_price_lists.id` (`belongsTo`)
- **Esempio di valori**: 16, 399

### bookingId
- **Tipo**: `int`, `NOT NULL` — commento DB: *"Reference to the booking (bookings.id)"*
- **Nullabile**: No
- **Relazioni**: `bookings.id`. `deletePaymentsByBookingId` cancella per questo campo prima di
  ricreare in `updateBooking`.
- **Esempio di valori**: 229794, 269298

### bookingNumber
- **Tipo**: `varchar(50)` — commento DB: *"Booking number from bookings table"*
- **Nullabile**: Sì
- **Relazioni**: `bookings.bookingNumber` (copiato per ricerche più veloci)
- **Esempio di valori**: `"1076112340"`, `"1084310311"`

### room
- **Tipo**: `varchar(100)` — commento DB: *"Hotel room identifier"*
- **Nullabile**: Sì
- **Descrizione**: numero/identificativo camera hotel dell'ospite. **Sempre `null` nel campione** →
  colonna di fatto non popolata.
- **Esempio di valori**: (nel campione `null`; valore atteso tipo `"Camera 214"`)

### voucherNumber
- **Tipo**: `varchar(100)` — commento DB: *"Voucher number (free text note)"*
- **Nullabile**: Sì
- **Descrizione**: numero voucher hotel (testo libero). **Sempre `null` nel campione**.
- **Esempio di valori**: (nel campione `null`; valore atteso tipo `"VCH-2026-00842"`)

### locationKey
- **Tipo**: `varchar(100)` — commento DB: *"Location key from booking locations"*
- **Nullabile**: Sì
- **Descrizione**: id cella mappa della postazione (`booking_locations[].id` tipo `"R15C30"`).
- **Esempio di valori**: `"R15C30"`, `"R2C9"`

### locationName
- **Tipo**: `varchar(100)` — commento DB: *"Location name from booking locations"*
- **Nullabile**: Sì
- **Descrizione**: etichetta postazione (`booking_locations[].Location`). Usata come chiave di match
  con le righe `bookings` in `processHotelBookingPayments` (`locationBookingMap[trim(Location)]`).
- **Esempio di valori**: `"100"`, `"6"`

### quantity
- **Tipo**: `int` — commento DB: *"Quantity of price list items"*
- **Nullabile**: Sì
- **Esempio di valori**: 1

### assignedPrice
- **Tipo**: `decimal(10,2)` — commento DB: *"Price assigned for this specific payment row"*
- **Nullabile**: Sì
- **Descrizione**: prezzo assegnato alla singola riga. Nel campione `0.00` (voucher a copertura
  totale / convenzione senza addebito diretto).
- **Esempio di valori**: `0.00`

### date
- **Tipo**: `date`
- **Nullabile**: Sì
- **Descrizione**: data specifica della riga (per listini "per giorno"). Usata da `checkVoucherLimit`
  per contare gli utilizzi voucher del cliente **nella stagione corrente**
  (`hp.date >= seasonFrom AND hp.date <= seasonTo`). Nel campione `null`.
- **Esempio di valori**: `null`

### bookingStartDate / bookingEndDate
- **Tipo**: `datetime`, `NOT NULL` — commento DB: *"...for faster searches"*
- **Nullabile**: No
- **Descrizione**: copia di `bookings.booking_start_date` / `booking_end_date` (denormalizzata per i
  report).
- **Esempio di valori**: `2026-07-25 00:00:00` / `2026-07-31 23:59:59`

### mainPrices
- **Tipo**: `longtext` — commento DB: *"Snapshot of main prices at booking time"*
- **Nullabile**: Sì
- **Descrizione**: snapshot dei prezzi "principali" del listino hotel al momento della prenotazione.
  Nei dati reali è **doppiamente serializzato** (`"\"[]\""` = stringa JSON di una stringa JSON).
- **Esempio di valori**: `"[]"` (doppio-encoded)

### detailPrices
- **Tipo**: `longtext` — commento DB: *"Snapshot of detail prices at booking time"*
- **Nullabile**: Sì
- **Descrizione**: snapshot dei prezzi di dettaglio del listino hotel (array di `{ id, name,
  half_day_price, ... }`, come in [`hotel_price_lists`](hotel_price_lists.md)). Anche qui
  **doppiamente serializzato**; blob da decine di KB per riga.
- **Esempio di valori** (troncato): `"[{\"id\":1,\"name\":\"Riva Bassa stagione\"},{\"id\":2,\"name\":\"Riva Alta stagione\"}, ...]"`

### priceCalculation
- **Tipo**: `varchar(100)` — commento DB: *"Snapshot of price calculation method at booking time"*
- **Nullabile**: Sì
- **Descrizione**: metodo di calcolo prezzo del listino hotel al momento della prenotazione. Valori
  reali: `"proportional"` (98), `"daily"` (2).
- **Candidato a enumerazione**: **Sì** — **stesso vocabolario** di
  [`price_lists.priceCalculation` / `hotel_price_lists.priceCalculation`](price_lists.md) (7 varianti
  da consolidare, vedi [`docs/03-gestione-dati.md` §2.1/§2.4](../../docs/03-gestione-dati.md)). Non
  introdurre un enum separato.
- **Esempio di valori**: `"proportional"`, `"daily"`

### amount
- **Tipo**: `decimal(10,2)` — commento DB: *"Total payment amount"*
- **Nullabile**: Sì
- **Descrizione**: importo totale della riga di pagamento. Nel campione `0.00`.
- **Esempio di valori**: `0.00`

### paymentStatus
- **Tipo**: `varchar(50)` — default `'pending'` — commento DB: *"Payment status: pending, completed,
  refunded, failed"*
- **Nullabile**: Sì
- **Descrizione**: stato del pagamento hotel. `processHotelBookingPayments` scrive `"completed"` se
  `payment_status === 1`, altrimenti `"pending"`. Valori reali: solo `"completed"`.
- **Candidato a enumerazione**: **Sì** (`HotelPaymentStatus { Pending, Completed, Refunded, Failed }`).
- **Esempio di valori**: `"completed"`

### paymentMethod
- **Tipo**: `varchar(50)` — commento DB: *"Payment method used (cash, card, bank transfer, etc.)"*
- **Nullabile**: Sì
- **Descrizione**: modalità di pagamento. **Sempre `null` nel campione** (il costo è coperto dal
  voucher/convenzione, vedi criticità).
- **Candidato a enumerazione**: **Sì**, da allineare a
  [`booking_payments.payment_mode`](booking_payments.md) (`PaymentMode`).
- **Esempio di valori**: (nel campione `null`; valori attesi `"cash"` / `"card"` / `"bank transfer"`)

### notes
- **Tipo**: `text` — commento DB: *"Additional notes about the payment"*
- **Nullabile**: Sì
- **Descrizione**: nota libera. **Sempre `null` nel campione**.
- **Esempio di valori**: (nel campione `null`; valore atteso tipo `"Voucher a copertura totale,
  saldo extra a carico cliente"`)

### usedAsDiscount
- **Tipo**: `tinyint(1)`, `NOT NULL` — default `0` — commento DB: *"Whether this price list item is
  used as a discount"*
- **Nullabile**: No
- **Descrizione**: `1` = la voce di listino hotel è applicata come **sconto** sulla prenotazione
  (anziché come addebito). Valori reali: `0` (95), `1` (5).
- **Candidato a enumerazione**: booleano — non enum.
- **Esempio di valori**: `0`, `1`

### isDeleted
- **Tipo**: `tinyint(1)` — default `0` — commento DB: *"Soft delete flag"*
- **Nullabile**: Sì
- **Descrizione**: soft-delete. Tutte le query (`checkVoucherLimit`, report) filtrano `isDeleted = 0`.
- **Esempio di valori**: `0`

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-27 08:18:55`

---

## Osservazioni verificate nel codice (criticità)

- **Doppia serializzazione JSON** su `mainPrices` / `detailPrices` (`"\"[]\""`): stesso anti-pattern
  già documentato per `discounts.items` — chi legge deve fare `JSON.parse` due volte. Da correggere in
  migrazione.
- **Snapshot listino gigante**: `detailPrices` replica l'intero listino hotel (decine di KB) per ogni
  riga di pagamento; `bookingStartDate`/`bookingEndDate`/`bookingNumber` duplicano `bookings`.
- **`priceCalculation` snapshot come stringa libera**: da consolidare con lo stesso vocabolario di
  `price_lists`/`hotel_price_lists` (7 varianti).
- **`paymentStatus` stringa libera** (default `pending`); nei dati solo `completed`.
- **Match postazione per stringa**: `processHotelBookingPayments` costruisce
  `locationBookingMap[trim(Location)]` e associa le righe hotel alle righe `bookings` per nome
  postazione → fragile con location numeriche/duplicate su prenotazioni multiple.
- **Update = deletePaymentsByBookingId + ricrea** (non transazionale rispetto al resto della
  modifica prenotazione).
- **`amount` / `assignedPrice` = `0.00` in tutto il campione**: nel dominio osservato la convenzione
  hotel copre integralmente il costo (voucher), quindi l'importo economico reale della prenotazione è
  altrove (`bookings.booking_payments`); qui la riga serve soprattutto a tracciare *quale* voce di
  listino/voucher è stata usata e a far funzionare `checkVoucherLimit`.
- **`checkVoucherLimit` conta per `hp.date`** ma `date` è quasi sempre `null` nei dati → il limite di
  utilizzi voucher per stagione, di fatto, non conta nulla.
- **`room` / `voucherNumber` / `paymentMethod` / `notes` mai popolati** nel campione.
