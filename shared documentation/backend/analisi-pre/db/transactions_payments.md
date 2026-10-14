# Tabella `transactions_payments`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **2.539 righe**, query
> completa). **Usata di fatto da un solo stabilimento**: `propertyId` `10588` su 2.531 righe, `228` su
> 8. `transactionDate` dal 2025-07-15 → integrazione recente. `type` enum `payment` (2.531) / `deposit`
> (8); `serviceReservationId` `NULL` sul 100% (solo `bookingId`). Model
> [`back-pre/app/models/transactionsPayments.model.js`](../../../../../../back-pre/app/models/transactionsPayments.model.js)
> (`freezeTableName: true`, `timestamps: true`; ORM `db.transactionsPayments`; usa `field:` espliciti
> per mantenere il camelCase a DB), scrittura in
> [`Service/transactionsPaymentsService.js`](../../../../../../back-pre/app/Service/transactionsPaymentsService.js)
> (`processBookingTransactions` chiamata **non bloccante** da
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js) dopo la creazione
> prenotazione; `processServiceReservationTransactions` da
> [`servicereservation.controller.js`](../../../../../../back-pre/app/controllers/servicereservation.controller.js)),
> lettura tramite l'API esterna
> [`transactionApi.controller.js`](../../../../../../back-pre/app/controllers/transactionApi.controller.js) /
> [`Service/transactionApiService.js`](../../../../../../back-pre/app/Service/transactionApiService.js);
> associazioni ORM `db.transactionsPayments.belongsTo(db.booking, { foreignKey: "bookingId" })` e
> `... .belongsTo(db.serviceReservation, { foreignKey: "serviceReservationId" })`.
>
> **Nota di contesto** — **registro contabile normalizzato per riga-articolo e per movimento di pagamento**,
> pensato per un'integrazione esterna (gestionale/contabilità di terze parti). È l'unico posto del
> dominio dove i pagamenti sono esplosi in righe tipizzate (`deposit` / `payment`) con imponibile e
> IVA separati. **Abilitato solo per gli stabilimenti in allow-list** (variabile d'ambiente
> `API_TRANSACTIONS_PROPERTYID_CSV`): se la lista è non vuota e la property non è inclusa, non viene
> scritto nulla.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### propertyId
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: `properties.id` (nessuna FK)
- **Esempio di valori**: 10588

### bookingId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (`belongsTo`). Valorizzato per le righe generate da una prenotazione
  ombrellone; `NULL` per quelle da prenotazione servizi.
- **Esempio di valori**: 269319, 269055

### serviceReservationId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `service_reservation.id` (`belongsTo`). Alternativo a `bookingId` — valorizzato per le
  righe generate da `processServiceReservationTransactions` (prenotazione di soli servizi); `NULL` per
  le righe da prenotazione ombrellone.
- **Esempio di valori**: `6319` (riga da prenotazione servizi); `null` (riga da prenotazione ombrellone)

### transactionDate
- **Tipo**: `datetime`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: data/ora del movimento di pagamento: `deposit.depositDate` o `payment.date` dal JSON
  `bookings.booking_payments`; fallback `new Date()`.
- **Esempio di valori**: `2026-07-27 08:11:53`

### reservationStartDate / reservationEndDate
- **Tipo**: `date` (solo data)
- **Nullabile**: Sì
- **Descrizione**: intervallo della prenotazione/servizio (`bookingDates.startDate` / `.endDate`
  troncati a `YYYY-MM-DD`).
- **Esempio di valori**: `2026-07-27` / `2026-07-28`

### type
- **Tipo**: `enum('deposit','payment')`, `NOT NULL` — **enum nativo MySQL** (raro nel dominio)
- **Nullabile**: No
- **Descrizione**: `deposit` = riga generata da un elemento di `booking_payments.depositList`;
  `payment` = riga generata da un elemento di `booking_payments.paidAmountData`. Nel campione: solo
  `payment`.
- **Candidato a enumerazione**: **già enum** — mappare 1:1 su un `enum` C# `TransactionEntryType
  { Deposit, Payment }`. Vedi [`docs/03-gestione-dati.md` §2](../../progettazione/03-gestione-dati.md).
- **Esempio di valori**: `"payment"`, `"deposit"`

### itemName
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: descrizione della riga-articolo. Per i pacchetti: `"<PackageName> (<Location>)"`
  (da `packageSelectedData`); per i servizi: `ServiceName`; `"Deposit"` / `"Payment"` se non si
  riescono a estrarre articoli. Ogni movimento (deposito/pagamento) viene **ripartito
  proporzionalmente** su tutte le righe-articolo.
- **Esempio di valori**: `"2 lettini e gazebo prato (+2 lettini acquistabili) (GZ4)"`,
  `"2 lettini e ombrellone (+ 1 lettino acquistabile) (304)"`

### itemQuantity
- **Tipo**: `int` — default `1`
- **Nullabile**: Sì
- **Descrizione**: quantità dell'articolo (per i pacchetti `1`; per i servizi `count`).
- **Esempio di valori**: `1`

### itemNetAmount
- **Tipo**: `decimal(10,2)` — default `0.00`
- **Nullabile**: Sì
- **Descrizione**: imponibile della quota di quel movimento attribuita alla riga, calcolato da
  `calculateVat(gross, vatPercentage)` come `gross / (1 + vat/100)`. IVA di default 22% se
  `percentIVA` mancante.
- **Esempio di valori**: `32.79`, `45.08`

### itemVatAmount
- **Tipo**: `decimal(10,2)` — default `0.00`
- **Nullabile**: Sì
- **Descrizione**: quota IVA (`gross − net`).
- **Esempio di valori**: `7.21`, `9.92`

### customerFullName
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: `customerData[0].name` (prenotazione) — snapshot.
- **Esempio di valori**: `"Forestiero Chiara"`, `"Abdulalh Ghadeer "`

### customerMobile
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Esempio di valori**: `"3452368073"`, `"3399803610"`

### customerEmail
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Esempio di valori**: `"Chiara-946@hotmail.it"`

### createdAt / updatedAt
- **Tipo**: `datetime`, `NOT NULL` — timestamp Sequelize (`timestamps: true`)
- **Nullabile**: No
- **Esempio di valori**: `2026-07-27 08:11:54`

---

## Osservazioni verificate nel codice (criticità)

- **Popolamento parziale e best-effort**: scritto solo per property in `API_TRANSACTIONS_PROPERTYID_CSV`,
  in modo **non bloccante** (`.catch(err => console.error(...))`) → se fallisce, il ledger resta
  incompleto senza che nessuno se ne accorga.
- **Full-replace per prenotazione**: `processBookingTransactions` fa
  `transactionsPayments.destroy({ where: { bookingId } })` + `bulkCreate` → ad ogni modifica della
  prenotazione tutte le righe vengono ricreate (id nuovi); non è un ledger append-only.
- **IVA sempre 22% se `percentIVA` assente**: per i pacchetti è **cablata** a `DEFAULT_VAT_PERCENTAGE
  = 22` (non legge l'aliquota reale del listino); solo i servizi passano `percentIVA` dal JSON.
- **Ripartizione proporzionale degli importi**: ogni deposito/pagamento viene spalmato su tutte le
  righe-articolo in proporzione al lordo → gli importi per riga sono stime, non movimenti reali
  distinti.
- **`deposit` mai osservato nei dati** (solo `payment`) — il ramo `depositList` esiste ma nel campione
  non produce righe.
- **Duplica informazioni** già in `booking_payments` / `booking_number_informations.details_payments_new`
  ma con un modello dati migliore (enum `type`, `decimal`, net/vat separati): è di fatto un prototipo
  di come dovrebbe essere il libro pagamenti.
- **Snapshot cliente denormalizzato** (`customerFullName`/`Mobile`/`Email`) senza FK.
