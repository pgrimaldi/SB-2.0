# Tabella `booking_number_informations`

> Fonti: schema e dati reali (100 righe, `bookingNumber` distinti = 100), model
> [`back-pre/app/models/bookingNumberInformations.model.js`](../../back-pre/app/models/bookingNumberInformations.model.js)
> (`freezeTableName: true`, `timestamps: false`), scrittura in
> [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js) (`booking` →
> `BookingNumberInformations.findOrCreate` con `details_payments_new`, `payments_new`,
> `quick_actions_data`; righe ~480-501),
> [`servicereservation.controller.js`](../../back-pre/app/controllers/servicereservation.controller.js)
> (stesso pattern per le prenotazioni di servizi, righe ~252-259),
> [`Service/ofsaleService.js`](../../back-pre/app/Service/ofsaleService.js) (`saveChargeData` →
> `ofsale_payments` + `details_payments`),
> [`Service/teamsystemService.js`](../../back-pre/app/Service/teamsystemService.js) (`teamsystem_payment`),
> lettura in [`helpers/payment.helper.js`](../../back-pre/app/controllers/helpers/payment.helper.js)
> (`getDetailsPaymentsByBookingNumber`, esposto da
> [`bookingPayments.controller.js`](../../back-pre/app/controllers/bookingPayments.controller.js) su
> `GET /api/sb/bookingPayments/detailsPayments/:bookingNumber`) e in `booking.controller`
> (`bookingDetailsById`, `checkBookingDetails` per `quick_actions_data`);
> [`helpers/fiskaly.helper.js`](../../back-pre/app/controllers/helpers/fiskaly.helper.js)
> (`lineItemsFromBooking`, `getLatestPaymentInfo`, `setPaidInLineItems`).
> Frontend: consumato dal gestionale nel flusso emissione scontrino/pre-conto
> (`front-pre/src` schermate pagamenti/scontrino) e da `front-modern-pre` (libreria pagamenti antd).
>
> **Nota di contesto** — riga **"una per `bookingNumber`"** (prenotazione ombrellone **oppure**
> prenotazione servizi) che raccoglie in un unico posto tutte le sorgenti di pagamento/righe fiscali:
> pre-conto interno (`payments_new`/`details_payments_new`), pagamento "consolidato" post-incasso
> (`payments`/`details_payments`), addebiti dall'app OfSale (`ofsale_payments`), spese POS TeamSystem /
> Cassa in Cloud (`teamsystem_payment`), e le date delle "azioni rapide" (`quick_actions_data`). È
> l'input principale per l'anteprima/emissione dello scontrino elettronico Fiskaly.
>
> **Dominio nella riscrittura**: **da spezzare** — i riepiloghi (`payments*`) → C1 testata;
> `details_payments*` (righe fiscali), `ofsale_payments`, `teamsystem_payment` → **C3**;
> `quick_actions_data` → C1 (planner). Vedi
> [`docs/03-gestione-dati.md` §4](../../docs/03-gestione-dati.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### bookingNumber
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì (nella pratica sempre valorizzato e univoco)
- **Relazioni**: `bookings.bookingNumber` **oppure** `service_reservation.bookingNumber`. Indicizzato
  (indice non univoco).
- **Descrizione**: chiave logica della riga. `findOrCreate({ where: { bookingNumber } })` → di fatto
  una riga per numero prenotazione.
- **Esempio di valori**: `"1079511235"`, `"1074911835"`

### payments_new
- **Tipo**: `longtext` — oggetto JSON — commento model: *"booking or service reservation payment info
  before payment"*
- **Nullabile**: Sì
- **Descrizione**: copia di `bookings.booking_payments` (o `service_reservation.payments`) **prima**
  dell'incasso — usata come base per calcolare il pre-conto. `payment.helper` la legge come
  `paymentsNew` e da `paymentsNew.toPay` costruisce `prebillPaymentData = { amount, paymentMethod:
  'ONLINE' }`; `FiskalyLineHelper.getLatestPaymentInfo(paymentsNew)` ne estrae l'ultimo
  incasso/importo.
- **Campi**: stessa struttura di [`bookings.booking_payments`](bookings.md) (`subTotal`,
  `payableAmount`, `toPay`, `isPaidFlag`, `paidAmountData[]`, `addPaidAmountData[]`, `paymentMode`,
  `depositList[]`, `surchargeObj`, ...).
- **Esempio di valori**:
  `{"subTotal":116,"payableAmount":110,"appliedPrice":"110","toPay":110,"isPaidFlag":false,"paidAmountData":[],"paymentMode":null,"totalPaidValue":110,"surchargeObj":{},"addPaidAmountData":[]}`

### payments
- **Tipo**: `longtext` — oggetto JSON — commento model: *"...after payment"*
- **Nullabile**: Sì (`null` finché non è stato consolidato un incasso)
- **Descrizione**: snapshot del riepilogo pagamenti **dopo** l'incasso/consolidamento. Nel campione è
  sempre `null` (viene valorizzato dopo l'emissione dello scontrino/pagamento definitivo).
- **Esempio di valori** (stessa struttura di `payments_new`, con incasso registrato):
  `{"subTotal":116,"payableAmount":110,"toPay":0,"isPaidFlag":true,"paidAmountData":[{"date":"2026-07-27T09:00:00Z","paymentMode":"Cash","amount":110}],"paymentMode":"Cash","totalPaidValue":110,"surchargeObj":{}}`

### details_payments_new
- **Tipo**: `longtext` — array JSON — commento model: *"items created from data before payment"*
- **Nullabile**: Sì
- **Descrizione**: righe fiscali/di dettaglio **calcolate dal pre-conto** tramite
  `FiskalyHelper.lineItemsFromBooking(dataForLineItems, hotelBookingData)`. Ogni riga è un articolo
  (pacchetto, articolo aggiuntivo, servizio, voucher hotel). Usata per correlare ogni `lineItemId` in
  `bookings.packageSelectedData[].lineItemId` alla sua riga; `enrichHotelBookingWithLineItemIds` la
  rilegge per arricchire la risposta hotel.
- **Campi** (per elemento):
  - **id**: `string` (uuid) — identificativo riga (= `lineItemId` in `packageSelectedData`).
    Esempio: `"1749d3ba-0403-441a-ae5f-90c0518f553c"`
  - **bookingId**: `number` — riga `bookings` di provenienza. Esempio: `269344`
  - **locationKey**: `string` — id cella mappa. Esempio: `"R13C6"`, `"R16C4"`
  - **locationName**: `number | string` — etichetta postazione. Esempio: `49`, `"G14"`
  - **name** / **nameIt**: `string` — descrizione articolo (EN / IT). Esempio: `"Ombrellone e 2
    lettini"`
  - **type**: `string` — `"package"` / `"additional"` / `"service"` / `"ofsale_payments"` /
    `"agreement_voucher_package"` ... Esempio: `"package"`
  - **date**: `string | null` — data specifica (per articoli "certi giorni"). Esempio: `null`
  - **quantity**: `number`. Esempio: `1`
  - **unitPrice**: `string` — prezzo unitario (2 decimali). Esempio: `"116.00"`, `"25.00"`
  - **isAgreement**: `boolean` — riga da convenzione hotel. Esempio: `false`
  - **vatRate**: `string` — aliquota IVA %. Esempio: `"22"`
  - **referenceId**: `number | string | null`. Esempio: `null`
  - **paid**: `number` — quota già pagata attribuita alla riga (distribuita da
    `setPaidInLineItems`). Esempio: `0`
  - **total**: `number` — `quantity * unitPrice`. Esempio: `116`, `25`
- **Esempio di valori**:
  `[{"id":"1749d3ba-...","bookingId":269344,"locationKey":"R13C6","locationName":49,"name":"Ombrellone e 2 lettini","nameIt":"Ombrellone e 2 lettini","type":"package","date":null,"quantity":1,"unitPrice":"116.00","isAgreement":false,"vatRate":"22","referenceId":null,"paid":0,"total":116}]`

### details_payments
- **Tipo**: `longtext` — array JSON — commento model: *"items created from data after payment"*
- **Nullabile**: Sì (`null` finché non consolidato / non ci sono addebiti OfSale)
- **Descrizione**: righe di dettaglio "definitive". Valorizzata:
  (a) da `ofsaleService.saveChargeData` che vi accoda le righe `ofsale_payments`
  (`lineItemsFromOfsalePaymentsArray`), preservando gli item non-ofsale già presenti;
  (b) dal consolidamento post-pagamento. `payment.helper` la usa come `itemsDb` (base per
  `setPaidInLineItems`).
- **Campi** (per elemento): stessa forma di `details_payments_new` (con `type` che può valere
  `"ofsale_payments"`).
- **Esempio di valori** (righe consolidate + una riga OfSale):
  `[{"id":"1749d3ba-...","bookingId":269344,"name":"Ombrellone e 2 lettini","type":"package","quantity":1,"unitPrice":"110.00","vatRate":"22","paid":110,"total":110},{"id":"a1b2c3d4-...","name":"2 Birre","type":"ofsale_payments","quantity":2,"unitPrice":"4.00","vatRate":"10","paid":8,"total":8}]`

### ofsale_payments
- **Tipo**: `longtext` — array JSON
- **Nullabile**: Sì (`null` se non ci sono addebiti dall'app OfSale)
- **Relazioni**: alimentato dall'API OfSale (`PUT` charge, chiave API dedicata `APP_NAMES.OFSALE`).
- **Descrizione**: elenco degli "scontrini/addebiti" registrati dall'app di cassa OfSale per quella
  prenotazione. Ogni elemento è un `OfSalePutChargeBaseDto` con `saleItems[]` (a cui `saveChargeData`
  assegna un `id` uuid). `payment.helper` lo restituisce come `ofsale_payments` nell'endpoint di
  dettaglio pagamenti.
- **Campi** (per elemento, dal `OfSalePutChargeBaseDto`): dati dell'addebito (data, totale, riferimento
  cliente/prenotazione) + `saleItems[]` = `[{ id (uuid), description, quantity, unitPrice, vatRate }]`.
- **Esempio di valori**:
  `[{"chargeDate":"2026-07-27T12:30:00Z","total":8,"saleItems":[{"id":"a1b2c3d4-e5f6-7890-abcd-ef1234567890","description":"Birra media","quantity":2,"unitPrice":4,"vatRate":10}]}]`

### teamsystem_payment
- **Tipo**: `longtext` — JSON — commento model: *"data from TeamSystem / Cassa in Cloud POS expenses"*
- **Nullabile**: Sì
- **Relazioni**: integrazione TeamSystem / Cassa in Cloud (tabella `teamsystem_expenses` +
  `Service/teamsystemService.js` — non ancora documentate).
- **Descrizione**: spese/consumazioni importate dal POS TeamSystem / Cassa in Cloud per la
  prenotazione. Restituito dall'endpoint di dettaglio pagamenti come `teamsystem_payment`.
- **Esempio di valori** (forma indicativa — array di documenti POS con righe):
  `[{"documentNumber":"POS-2026-00187","date":"2026-07-27T13:10:00Z","total":15.5,"items":[{"description":"Insalatona","quantity":1,"unitPrice":9.5,"vatRate":10},{"description":"Acqua 1L","quantity":2,"unitPrice":3,"vatRate":10}]}]`

### quick_actions_data
- **Tipo**: `longtext` — array JSON — commento model: *"array of dates coming from booking
  data[0].quick_actions_data"*
- **Nullabile**: Sì (`[]` frequente)
- **Descrizione**: elenco di date associate alle "azioni rapide" della prenotazione (es. giorni marcati
  per operazioni veloci in planner). Scritto in fase di `booking` da
  `req.body.data[0].quick_actions_data` ed è **uguale per tutte le righe** della prenotazione. Riletto
  in `bookingDetailsById` e `checkBookingDetails` (mappato per `bookingNumber`).
- **Esempio di valori**: `[]`; oppure `["2026-07-30","2026-08-01"]`

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW` (`timestamps: false`, gestiti come attributi normali)
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-27 08:19:29`

---

## Osservazioni verificate nel codice (criticità)

- **Tabella "sacco" multi-sorgente**: mescola pre-conto interno, consolidato, addebiti OfSale e spese
  TeamSystem in colonne JSON parallele senza schema. La correlazione con la prenotazione è **solo per
  stringa `bookingNumber`** (nessuna FK), condivisa fra `bookings` e `service_reservation`.
- **Nomi ambigui**: `payments_new` = "prima del pagamento", `payments` = "dopo"; `details_payments_new`
  vs `details_payments` idem. Semantica affidata ai soli commenti nel model.
- **`getDetailsPaymentsByBookingNumber` assume `paymentsNew` non nullo**: costruisce sempre
  `prebillPaymentData = { amount: paymentsNew.toPay, ... }` → se `payments_new` è `null` va in
  eccezione (gestita a monte solo parzialmente con `errCode`).
- **Righe fiscali duplicate della verità**: `details_payments_new` ricalcola da
  `bookings.packageSelectedData` / `servicesData` gli stessi articoli già presenti là; il legame è il
  campo `lineItemId` sparso dentro `packageSelectedData`.
- **Nessun vincolo di unicità su `bookingNumber`**: solo indice non univoco + `findOrCreate`. Una race
  su due create simultanee per lo stesso numero può creare due righe.
- **`ofsale_payments` popolabile da API con chiave dedicata**: `putCharge` verifica
  `req.client.appName === APP_NAMES.OFSALE` ma opera su `bookingId` arbitrario del body senza
  ulteriore autorizzazione per property.
