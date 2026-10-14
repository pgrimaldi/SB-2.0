# Tabella `hotel_reports`

> Fonti: schema (DB dev: **0 righe**), model
> [`back-pre/app/models/hotelReport.model.js`](../../back-pre/app/models/hotelReport.model.js)
> (ORM `db.hotelReport`; associazione `belongsTo(db.hotel)`), logica in
> [`Service/hotelReportService.js`](../../back-pre/app/Service/hotelReportService.js)
> (`createHotelReport`, `getHotelReports`, `getHotelReportById`, `cancelHotelReport`,
> `cancelHotelReportsBulk`, `updateHotelReportNotes`) e
> [`Service/hotelReportNotificationService.js`](../../back-pre/app/Service/hotelReportNotificationService.js)
> (invio del PDF alla struttura partner via email/WhatsApp usando i template di
> [`hotel_message_templates`](hotel_message_templates.md)); API
> [`controllers/hotel/hotelReport.controller.js`](../../back-pre/app/controllers/hotel/hotelReport.controller.js).
> I dati aggregati provengono da [`hotel_payments`](hotel_payments.md) join `bookings`.
> Frontend `front-pre` (gestionale): sezione "Convenzioni / Hotel" → report periodici.
>
> **Nota di contesto** — **report periodico degli arrivi/partenze (e ricavi) per una struttura
> partner**, generato dal gestionale e inviato all'hotel. **Una riga = un report generato** per un
> intervallo di date e un `searchType`. `0` righe in DB dev semplicemente perché nessun report è stato
> generato nel campione.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### hotelId
- **Tipo**: `int`, `NOT NULL` — commento DB: *"Reference to the hotel this report belongs to"*
- **Nullabile**: No
- **Relazioni**: `hotels.id` (`belongsTo`, nessuna FK reale)

### searchType
- **Tipo**: `enum('CHECK_IN','CHECK_OUT')`, `NOT NULL` — default `'CHECK_IN'` — **enum nativo MySQL** —
  commento DB: *"determines if date range refers to checkin or checkout dates"*
- **Nullabile**: No
- **Descrizione**: se l'intervallo `bookingStartDate`..`bookingEndDate` del report si riferisce alle
  date di **check-in** o di **check-out** delle prenotazioni.
- **Candidato a enumerazione**: **già enum** → `enum HotelReportSearchType { CheckIn, CheckOut }`.
- **Esempio di valori**: `"CHECK_IN"`, `"CHECK_OUT"`

### status
- **Tipo**: `enum('pending','created','cancelled')` — default `'pending'` — **enum nativo MySQL** —
  commento DB: *"Report status"*
- **Nullabile**: Sì
- **Descrizione**: `createHotelReport` scrive **sempre `'created'`**; `cancelHotelReport` /
  `cancelHotelReportsBulk` portano a `'cancelled'` (soft-delete "di business" — le righe cancellate
  sono nascoste dalle letture salvo `includeDeleted`). `'pending'` è il default a schema ma non usato
  dal flusso.
- **Candidato a enumerazione**: **già enum** → `enum HotelReportStatus { Pending, Created, Cancelled }`.
- **Esempio di valori**: `"created"`, `"cancelled"`

### bookingStartDate / bookingEndDate
- **Tipo**: `datetime`, `NOT NULL` — commento DB: *"Report period start/end date"*
- **Nullabile**: No
- **Descrizione**: intervallo del periodo coperto dal report.

### reportData
- **Tipo**: `longtext` — JSON — commento DB: *"Cached report data"*
- **Nullabile**: Sì
- **Descrizione**: dati del report pre-calcolati e cache-ati (elenco prenotazioni/ospiti/importi per
  il periodo), prodotti da `hotelReportService.createHotelReport` aggregando `hotel_payments` +
  `bookings`. Restituito solo da `getHotelReportById`, non dalla lista.
- **Campi** (forma indicativa — il generatore produce un array di righe prenotazione più i totali):
  - **rows**: array di `{ bookingNumber, customerFullName, room, checkIn, checkOut, location,
    priceListName, quantity, amount }` — una riga per (prenotazione × voce hotel) del periodo.
  - **totals**: `{ totalBookings, totalRevenue }` — coincidono con le colonne omonime.
- **Esempio di valori**:
  `{"rows":[{"bookingNumber":"1076112340","customerFullName":"Rossi Mario","room":"214","checkIn":"2026-07-25","checkOut":"2026-07-31","location":"100","priceListName":"Riva Alta stagione","quantity":1,"amount":0}],"totals":{"totalBookings":1,"totalRevenue":0}}`

### totalBookings
- **Tipo**: `int` — default `0` — commento DB: *"Total number of bookings in this report"*
- **Nullabile**: Sì

### totalRevenue
- **Tipo**: `decimal(12,2)` — default `0.00` — commento DB: *"Total revenue for the report period"*
- **Nullabile**: Sì
- **Descrizione**: ricavo totale del periodo. Esposto in API con l'alias `amount`
  (`orderColumnAliases = { amount: "totalRevenue" }`).

### generatedBy
- **Tipo**: `int` — commento DB: *"User ID who generated this report"*
- **Nullabile**: Sì
- **Relazioni**: `users.id`

### notes
- **Tipo**: `text` — commento DB: *"Additional notes about the report"*
- **Nullabile**: Sì
- **Descrizione**: nota libera, modificabile con `updateHotelReportNotes`.

### path
- **Tipo**: `varchar(500)` — commento DB: *"S3 URL of the generated report PDF (set when the report is
  sent)"*
- **Nullabile**: Sì
- **Descrizione**: **URL S3 completo** del PDF del report (valorizzato all'invio). Stesso anti-pattern
  link-a-DB di [`docs/03-gestione-dati.md` §1](../../docs/03-gestione-dati.md).

### isDeleted
- **Tipo**: `tinyint(1)` — default `0` — commento DB: *"Soft delete flag"*
- **Nullabile**: Sì
- **Descrizione**: soft-delete tecnico, **distinto** da `status = 'cancelled'` (soft-delete di
  business). Due meccanismi di cancellazione coesistono.

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard
- **Nullabile**: Sì

---

## Osservazioni verificate nel codice (criticità)

- **Due `enum` nativi MySQL** (`searchType`, `status`) — rari nel dominio, mappabili 1:1 su `enum` C#.
- **Doppio soft-delete**: `isDeleted` (tecnico) + `status = 'cancelled'` (business). Da unificare.
- **`status = 'pending'`** previsto a schema ma mai scritto (il create forza `'created'`).
- **`path` = URL S3 completo** in colonna `varchar(500)` → scomporre in `origin` + `path` come da
  linee guida.
- **`reportData` come blob JSON cache-ato**: se le prenotazioni sottostanti cambiano dopo la
  generazione, il report resta "fotografato" (comportamento probabilmente voluto, ma nessun
  versioning/checksum).
- **`totalRevenue` esposto come `amount`** in API: alias che complica il mapping.
- **Nessuna FK.**
