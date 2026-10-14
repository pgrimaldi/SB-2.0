# Tabella `service_reservation`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/serviceReservation.model.js`](../../back-pre/app/models/serviceReservation.model.js)
> (`freezeTableName: true`, `timestamps: false`; ORM `db.serviceReservation`), controller
> [`servicereservation.controller.js`](../../back-pre/app/controllers/servicereservation.controller.js)
> (1.897 righe — `serviceReservation`, `consumerServiceReservation`, `serviceReservationList`,
> `singleServiceReservationList`, `deleteSingleServiceReservationList`, `reservationDetailsByEmail`,
> `reservationDetailsByCustomerId`, `getServiceBookingCount`, `cancelServiceBooking`,
> `confirmServiceBookingByBookingNumber`, `serviceReservationDeatailsByBookingNumber`), helper
> [`helpers/serviceReservation.helper.js`](../../back-pre/app/controllers/helpers/serviceReservation.helper.js)
> (`serviceReservation`, `reservationPayments`, `serviceReservationCounts`, `checkCredit`,
> `deleteBookingByBookingId`, `checkCancelationCondition`, `refundPayment`),
> [`Service/transactionsPaymentsService.js`](../../back-pre/app/Service/transactionsPaymentsService.js)
> (`processServiceReservationTransactions`), associazioni ORM
> `db.reservationPayments.belongsTo(db.serviceReservation, ...)`,
> `db.transactionsPayments.belongsTo(db.serviceReservation, ...)` in
> [`models/index.js`](../../back-pre/app/models/index.js). Frontend `front-pre` e `front-customer-pre`:
> `src/components/serviceReservation/**` (`ServiceReservation.js`, `ViewServiceReservationModal.js`,
> `editService/editServiceNew.js`), `src/services/ReservationServices.js`.
>
> **Nota di contesto** — aggregato della **prenotazione di soli servizi** (bar / servizi "normali" /
> servizi a ore) **senza prenotazione di una postazione**: es. il cliente ordina online consumazioni o
> ingressi giornalieri. È la controparte di [`bookings`](bookings.md) per il canale "solo servizi".
> **Una riga = una prenotazione servizi** (con il suo `bookingNumber`). Struttura molto simile a
> `bookings` ma più snella; condivide il generatore `createBookingNumber`, la tabella
> [`booking_number_informations`](booking_number_informations.md), i contatori
> [`services_booking_counts`](services_booking_counts.md) e
> [`hourly_service_booking_management`](hourly_service_booking_management.md), i pagamenti
> [`service_reservation_payments`](service_reservation_payments.md) e
> [`transactions_payments`](transactions_payments.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Relazioni**: referenziato da `service_reservation_payments.service_reservation_id`,
  `transactions_payments.serviceReservationId`, `services_booking_counts.reference_id`,
  `online_transaction_details.reservation_id`, `hourly_service_booking_management.booking_id`.
- **Esempio di valori**: 1, 2, 3, ..

### bookingNumber
- **Tipo**: `varchar(255)` — default `0`
- **Nullabile**: Sì
- **Relazioni**: generato da `commonHelper.createBookingNumber(propertyId)` (stesso contatore
  `booking_logs` di `bookings`); chiave verso `booking_number_informations`. In **update** il campo
  viene rimosso dal payload (`delete data["bookingNumber"]`) → immutabile.
- **Esempio di valori**: `"11515247"`, `"11515246"`

### propertyId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK)
- **Esempio di valori**: 115

### bathHouseId
- **Tipo**: `int` — default `0`
- **Nullabile**: Sì
- **Relazioni**: `bathouses.id` (nessuna FK)
- **A cosa serve se c'è `propertyId`**: è una **copia denormalizzata** dell'id del `bathouse` dello
  stabilimento, per evitare una lookup quando serve la "scheda pubblica" (nome, slug, foto, contatti,
  `cancellation`, `pets`, `regulation`) — usata da email/QR/dettaglio cliente. Lo schema *ammette*
  più `bathouse` per `property` (`property.hasMany(bathouse)`), ma **nei dati è 1:1**: 100 bathouse /
  100 property, nessuna property con più di un bathouse. Quindi oggi `bathHouseId` è **ridondante**
  con `propertyId`. In riscrittura: se il rapporto resta 1:1, eliminarlo e derivare il bathouse dalla
  property; se diventa 1:N (più stabilimenti/insegne per stessa ragione sociale), tenerlo ma con FK
  reale. Stesso discorso per `bookings.bathHouseId`.
- **Esempio di valori**: 66

### customerId
- **Tipo**: `int` — default `0`
- **Nullabile**: Sì
- **Relazioni**: `customers.id` (cliente principale, `is_guest = 0`)
- **Esempio di valori**: 209191, 209190

### customerData
- **Tipo**: `longtext` — oggetto JSON (cliente principale)
- **Nullabile**: Sì
- **Descrizione**: snapshot del cliente principale (`body.customerData.find(c => c.is_guest === 0)`).
  **Chiavi diverse** da `bookings.booking_consumer`: qui `firstName` (non `name`).
- **Campi**:
  - **firstName**: `string` — nome/ragione. Esempio: `"Ventura Gaetano"`, `"Test Test"`
  - **mobile**: `string`. Esempio: `"3485974439"`
  - **email**: `string`. Esempio: `"venturagaetano@icloud.com"`, `"Test@gmail.vom"`
  - **is_guest**: `number` — `0`. Esempio: `0`
  - **countryCode**: `string` (opzionale). Esempio: `"+39"`
- **Esempio di valori**:
  `{"firstName":"Ventura Gaetano","mobile":"3485974439","email":"venturagaetano@icloud.com","is_guest":0}`

### guest_data
- **Tipo**: `longtext` — array JSON — default `null`
- **Nullabile**: Sì (nel campione sempre `[]`)
- **Descrizione**: ospiti (`customerData` con `is_guest === 1`). Analogo a
  [`bookings.guest_data`](bookings.md) — qui le chiavi sono `firstName`/`mobile`/`email`/`is_guest`.
- **Esempio di valori**:
  `[{"firstName":"Anna Verdi","mobile":"3339988776","email":"anna.verdi@example.it","is_guest":1}]`

### guest_ids
- **Tipo**: `longtext` — array JSON di `int` — default `null`
- **Nullabile**: Sì (nel campione sempre `[]`)
- **Relazioni**: `customers.id` degli ospiti.
- **Esempio di valori**: `[209192, 209193]`

### agent
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Relazioni**: `users.id` operatore (per prenotazioni da gestionale). Nel campione `null` (queste
  sono prenotazioni online).
- **Esempio di valori**: `null`, `"3313"`

### bookingByAdmin
- **Tipo**: `varchar(255)` — default `1` — commento DB: *"1 is for admin/manager & 0 is for customer"*
- **Nullabile**: Sì
- **Descrizione**: canale di origine (come [`bookings.bookingByAdmin`](bookings.md)). `"0"` = online
  cliente, `"1"` = gestionale.
- **Candidato a enumerazione**: **Sì** — stesso vocabolario canale di `bookings.bookingByAdmin`.
- **Esempio di valori**: `"0"`, `"1"`

### startDate
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: data di riferimento della prenotazione servizi, `new Date(body.startDate)` a inizio
  giornata. Non c'è `endDate` (i servizi hanno date puntuali dentro `servicesData`).
- **Esempio di valori**: `2026-07-26 00:00:00`

### servicesData
- **Tipo**: `longtext` — oggetto JSON — **blob molto grande**: nel campione ogni servizio porta con sé
  **immagini `data:` base64 inline** (`ServicesImg`, `ServiceImg`) da decine/centinaia di KB, più
  interi array di costanti UI (giorni, timeslot, categorie bar)
- **Nullabile**: Sì
- **Descrizione**: servizi selezionati e relative quantità/date. Struttura con chiavi
  `barServiceData` / `normalServiceData` / `hourlyServiceData` (diversa da
  `bookings.servicesData`). `serviceReservationCounts` la scorre per scrivere in
  `services_booking_counts`; `transactionsPaymentsService.extractServiceReservationItems` per le
  righe economiche.
- **Campi**:
  - **barServiceData** / **normalServiceData** / **hourlyServiceData**: array di servizi. Per elemento:
    - **ServiceName**: `string`. Esempio: `"Espresso"`, `"Ginseng"`, `"Gin-Macha"`
    - **ServicePrice**: `string` — prezzo unitario. Esempio: `"1.20"`, `"1.50"`
    - **PurchaseStatus**: `boolean`. Esempio: `true`
    - **count**: `number` — quantità selezionata. Esempio: `1`, `0`
    - **percentIVA**: `string` — aliquota IVA %. Esempio: `"22"`, `"10"`
    - **CategoryType**: `string` — `"Bar"` / `"Normal"` / ... Esempio: `"Bar"`
    - **selectedData**: array `{ date, click }` — per ogni data, quante unità. Esempio:
      `[{"date":"2026-07-26","click":1}]`
    - **VariantList** / **selectedVariant**: varianti prodotto (`{ variantnames, variantvalues }`).
      Esempio: `[{"variantnames":"Macchiato","variantvalues":"0.30"}]`
    - **lineItemId**: `string` (uuid) — riga fiscale. Esempio: `"a35dc38a-38a2-4dcb-9fee-ec089622a68f"`
    - **ServicesImg** / **Servicemage**: URL immagine (S3) **oppure** `data:image/...;base64,...`
      inline (anti-pattern, vedi criticità).
    - vari array di costanti UI: **day_constant** (7 giorni), **timeslot_constants** (11 slot),
      **bar_category_constants** (12+ categorie), **DescriptionNote**, **manual_timeslot**,
      **maximumReservations**, **highlightServices**, **onlyServiceDataObj**, ...
- **Esempio di valori** (troncato):
  `{"barServiceData":[{"ServiceName":"Espresso","ServicePrice":"1.20","count":1,"percentIVA":"22","CategoryType":"Bar","selectedData":[{"date":"2026-07-26","click":1}],"VariantList":[{"variantnames":"Macchiato","variantvalues":"0.30"}],"ServicesImg":"data:image/jpeg;base64,/9j/4AAQ...<12179 chars>","lineItemId":"a35dc38a-..."}],"normalServiceData":[],"hourlyServiceData":[]}`

### payments
- **Tipo**: `longtext` — oggetto JSON (riepilogo economico)
- **Nullabile**: Sì
- **Descrizione**: analogo a [`bookings.booking_payments`](bookings.md) ma con campi specifici della
  commissione servizi. Da qui `reservationPayments()` deriva la riga
  [`service_reservation_payments`](service_reservation_payments.md):
  `total_amount = payableAmount + remainingPayment`, `paid_amount = payableAmount`,
  `pending_amount = remainingPayment`, `payment_mode = payments.paymentMode`.
- **Campi**:
  - **subTotal**: `number`. Esempio: `8.2`, `7`
  - **payableAmount**: `number` — dovuto (con commissione). Esempio: `9.2`, `8`
  - **appliedPrice**: `number` — prezzo forzato. Esempio: `0`
  - **toPay**: `number`. Esempio: `9.2`, `8`
  - **remainingPayment**: `number` — residuo. Esempio: `0`
  - **paymentMode**: `string | null` — `"Cash"` / `"Credit card"` / `null`. Esempio: `"Credit card"`
  - **surchargeObj**: `{ type, addSurcharge }` — supplemento/commissione. Esempio:
    `{"type":"fixed","addSurcharge":"1"}`
  - **depositList**: array di `{ depositDate, depositAmount, depositPaymentMethod }`. Esempio:
    `[{"depositDate":"2026-07-25T10:00:00.000Z","depositAmount":5,"depositPaymentMethod":"Cash"}]`
  - **paidAmountData**: array di `{ date, paymentMode, amount }`. Esempio:
    `[{"date":"2026-07-26T11:33:22.000Z","paymentMode":"Credit card","amount":9.2}]`
  - **paymentDepositeMethod**: `string | null` — modalità dell'acconto. Esempio: `"Cash"`
  - **isCommition**: `number` — % commissione servizio. Esempio: `6`
  - **isShowCommission**: `boolean`. Esempio: `false`
  - **isCumstomerBookPrice**: oggetto `{ commissionAmount, payableAmount, subTotal,
    storeSubtotalPrice }` — scomposizione prezzo con/senza commissione. Esempio:
    `{"commissionAmount":"0.00","payableAmount":"9.20","subTotal":"8.69","storeSubtotalPrice":"9.19"}`
- **Esempio di valori**:
  `{"subTotal":8.2,"payableAmount":9.2,"toPay":9.2,"remainingPayment":0,"paymentMode":null,"surchargeObj":{"type":"fixed","addSurcharge":"1"},"depositList":[],"paidAmountData":[],"isCommition":6,"isShowCommission":false,"isCumstomerBookPrice":{"commissionAmount":"0.00","payableAmount":"9.20","subTotal":"8.69"}}`

### discountData
- **Tipo**: `longtext` — oggetto JSON (`null` se nessuno sconto; nel campione sempre `null`)
- **Nullabile**: Sì
- **Relazioni**: elemento di [`discounts.items`](discounts.md). `TransactionEntryForPayAtProperty`
  legge `discountData.value` / `discountData.type` (`"percentage"`/`"fixed"`).
- **Campi**: stessa struttura di [`bookings.booking_discount`](bookings.md) (`name`, `type`, `value`,
  `voucherType`, `maximunNoUse`, `totalNoOfUsage`, `voucherDateRange[]`, `status`, `currentId`, i
  `check*`).
- **Esempio di valori**:
  `{"name":"BAR10","type":"percentage","value":"10","voucherType":"custom","checkBarService":true,"maximunNoUse":"100","totalNoOfUsage":12,"voucherDateRange":[{"startDate":"2026-06-01T00:00:00Z","endDate":"2026-09-30T00:00:00Z"}],"status":1,"currentId":42.7}`

### note
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Esempio di valori**: `"Consegna al lettino 12"`, `"Cliente allergico lattosio"`

### AttachedFiles
- **Tipo**: `longtext` — array JSON — default `[]` (nel campione sempre `[]`)
- **Nullabile**: Sì
- **Descrizione**: allegati (URL S3 completi).
- **Esempio di valori**:
  `[{"name":"scontrino.pdf","url":"https://summerbooking.s3.us-east-2.amazonaws.com/images/reservation/att_9f1c.pdf"}]`

### status
- **Tipo**: `int` — default `0`
- **Nullabile**: Sì
- **Descrizione**: stato della prenotazione servizi. Impostato al `payment_status` in creazione.
  Valori reali: `1` (attiva/confermata, prevalente), `2` (in attesa — nel campione sempre con
  `bookingByAdmin = 0`, prenotazione online da confermare/pagare). Le query di lettura filtrano
  `status = 1 AND isDeleted = 0`; `confirmServiceBookingByBookingNumber` porta `2 → 1`.
- **Candidato a enumerazione**: **Sì** (`ServiceReservationStatus`: `Confirmed` = 1 /
  `PendingPayment` = 2 [/ `Cancelled`]) — da consolidare con `bookings.status`. Vedi
  [`docs/03-gestione-dati.md` §2](../../docs/03-gestione-dati.md).
- **Esempio di valori**: `1`, `2`

### payNow
- **Tipo**: `int` — default `0`
- **Nullabile**: Sì
- **Descrizione**: `1` = il cliente ha scelto di pagare subito online (`body.payNow`); `0` = pagamento
  differito / a struttura. Nel campione: `1` per prenotazioni con incasso immediato, `2` (status)
  spesso associato a `payNow = 1` (link inviato, non ancora pagato).
- **Candidato a enumerazione**: booleano (`PayNow` sì/no).
- **Esempio di valori**: `0`, `1`

### isDeleted
- **Tipo**: `int` — default `0`
- **Nullabile**: Sì
- **Descrizione**: soft-delete (`deleteBookingByBookingId` / `deleteSingleServiceReservationList`).
- **Esempio di valori**: `0`, `1`

### isOrderFilled
- **Tipo**: `tinyint(1)` — default `0`
- **Nullabile**: Sì
- **Descrizione**: ordine/consumazioni evasi (come [`bookings.isOrderFilled`](bookings.md)).
- **Esempio di valori**: `0`, `1`

### updatedById
- **Tipo**: `int` — default `0`
- **Nullabile**: Sì
- **Relazioni**: `users.id` dell'ultimo operatore che ha modificato la prenotazione servizi.
- **Esempio di valori**: `0`

### created_at / updated_at
- **Tipo**: `datetime` — default `NOW` (`timestamps: false`)
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-26 11:33:24`

---

## Osservazioni verificate nel codice (criticità)

- **Immagini base64 inline in `servicesData`**: nel campione reale il JSON contiene stringhe
  `data:image/...;base64,...` da **oltre 200.000 caratteri** (icone servizi) più array di costanti UI
  (`day_constant`, `timeslot_constants`, `bar_category_constants`). Ogni prenotazione servizi salva
  quindi centinaia di KB di dati puramente presentazionali. Da eliminare: salvare solo `serviceId` +
  quantità + date; le immagini vanno referenziate (vedi
  [`docs/03-gestione-dati.md` §1](../../docs/03-gestione-dati.md)).
- **Chiavi cliente disallineate con `bookings`**: qui `customerData.firstName`, in `bookings`
  `booking_consumer.name`. Doppia convenzione per lo stesso concetto.
- **`status` sovrapposto a `payment_status`** e con numerazione parzialmente diversa da
  `bookings.status`; nessun `ENUM`.
- **`payNow` vs `status = 2` vs link Stripe**: tre segnali per "pagamento non ancora effettuato";
  logica di conferma sparsa tra `confirmServiceBookingByBookingNumber`, webhook Stripe e
  `bookings_send_payment_summaries`.
- **Nessun `endDate`**: le date reali dei servizi sono dentro `servicesData[*].selectedData[].date`.
- **`timestamps: false` + `created_at`/`updated_at` a mano** (coerente con `bookings`, ma
  `service_reservation_payments` ha invece *entrambe* le coppie — vedi
  [`service_reservation_payments.md`](service_reservation_payments.md)).
- **IDOR**: opera su `req.body.propertyId` / `req.body.customerId` / `req.body.id` senza verifica
  contro l'utente autenticato (pattern sistemico del dominio).
