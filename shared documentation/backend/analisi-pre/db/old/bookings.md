# Tabella `bookings`

> Fonti: schema e dati reali (100 righe, DB dev), model
> [`back-pre/app/models/booking.modle.js`](../../back-pre/app/models/booking.modle.js) (il model si chiama
> `"booking"` ma è definito con 4 argomenti a `sequelize.define`; il 4° `{ freezeTableName, timestamps }`
> viene **ignorato** da Sequelize, quindi il nome tabella viene pluralizzato in **`bookings`** — la tabella
> `booking` esiste ma è vuota, vedi [nota tabelle legacy in fondo]), controller
> [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js) (12.798 righe — letti i
> path attivi: `booking`, `bookingDetailsById`, `checkBookingDetails`, `updateBooking`, `checkInBooking`,
> `checkOutBooking`, `deleteBooking`, `cancelPackageBooking`, `holdLocationBooking`, `upsertBookingGroup`,
> `sendReservationSummery`, `databaseChanges`, `creditDetails`, `getBookingData`), helper
> [`helpers/booking.helper.js`](../../back-pre/app/controllers/helpers/booking.helper.js) (funzioni attive:
> `Booking`/`handleRegularBooking`/`handleSplitBooking`, `prepareBookingData`, `createNewBooking`,
> `updateBooking`, `bookingPayments`, `bookingCountsDetails1`, `additionalItemForCertainDaysCountsDetails`,
> `updateAbsenceDetails`, `updateSharingDetails`, `aggregateBookingItems`, `isLocationAvailable`),
> [`helpers/common.helper.js`](../../back-pre/app/controllers/helpers/common.helper.js) (`createBookingNumber`),
> [`Service/bookingService.js`](../../back-pre/app/Service/bookingService.js),
> [`Service/transactionsPaymentsService.js`](../../back-pre/app/Service/transactionsPaymentsService.js),
> [`Service/ofsaleService.js`](../../back-pre/app/Service/ofsaleService.js),
> cron [`cronjob/cron-updatebooking-summary.js`](../../back-pre/cronjob/cron-updatebooking-summary.js) e
> [`cronjob/cron-cancel-booking.js`](../../back-pre/cronjob/cron-cancel-booking.js);
> frontend `front-pre` (hub gestionale) e `front-customer-pre` (booking online cliente): payload di
> creazione costruito in `src/services/ReservationServices.js` (`multiBookPayloadObject`) e
> `src/components/reserveArticle/**` (`ReservationModal.js`, `Modal/Summary.js`, `Modal/LocationDetails.js`),
> lettura in `src/components/ThePlanner*/ThePlannerPage.js`. I frontend `front-app-pre` (wrapper React
> Native con WebView) e `front-modern-pre` (libreria antd parziale) **non** contengono logica di
> prenotazione.
>
> **Nota di contesto** — è la tabella-radice dell'aggregato "Prenotazione ombrellone/postazione" (dominio
> C1). **Una riga = una singola prenotazione per un intervallo di date contiguo e un `booking_period`.**
> Una prenotazione "multi-data spezzata" (periodi diversi) o "split location" produce **più righe con lo
> stesso `bookingNumber`**. Quasi tutti i campi rilevanti sono JSON serializzati come stringa in colonne
> `longtext` (nessun tipo JSON nativo a schema).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Relazioni**: referenziato (senza FK reale) da `booking_payments.booking_id`,
  `booking_items_counts_details.booking_id`, `additional_items_counts_details.booking_id`,
  `booking_areas.booking_id`, `booking_absence_details.booked_by` / `.booking_id`,
  `bookings_requested_details.booking_id`, `bookings_send_payment_summaries.booking_id`,
  `hourly_service_booking_management.booking_id`, `card_transactions.booking_id`,
  `transactions_payments.bookingId`, `online_transaction_details.booking_id`,
  `refund_transaction_details.booking_id`, `booking_receipts.bookingId`,
  `services_booking_counts.reference_id`, `booking_groups.groupBookings[]`,
  `sharing_details.booked_by`, `hotel_payments.bookingId`.
- **Esempio di valori**: 1, 2, 3, ..

### bookingNumber
- **Tipo**: `varchar(255)` — default a schema `'0'`
- **Nullabile**: Sì (default `'0'`)
- **Descrizione**: numero prenotazione **condiviso da tutte le righe** che compongono una stessa
  prenotazione (multi-data / split / hotel). Generato da `common.helper.createBookingNumber(propertyId)`:
  la prima prenotazione di uno stabilimento vale `propertyId` **concatenato come stringa** a `"10000"`
  (es. property `10795` → `"1079510000"`), poi `parseInt(booking_number) + 1` ad ogni nuova
  prenotazione, con `SELECT ... FOR UPDATE` sulla riga di [`booking_logs`](booking_logs.md) come
  contatore per-property. È la chiave di correlazione verso `booking_number_informations`,
  `booking_payments`, `booking_items_counts_details`, `services_booking_counts`, `sharing_details`,
  `fiskaly_receipt.booking_number`, ecc.
- **Cosa succede alla prenotazione n. 100.000 di uno stabilimento / rischio sovrapposizione**: lo
  schema è **rotto per costruzione**. Il numero iniziale è `String(propertyId) + "10000"` e poi
  `parseInt(...) + 1`. Ogni property ha quindi una "finestra" larga **esattamente 100.000** numeri
  prima di collidere con quella della property con `propertyId` successivo:
  - property `96` parte da `9610000`; property `97` parte da `9710000`; differenza = **100.000**.
    Alla **100.000ª** prenotazione, property `96` emette `9710000` — **identico** al primo numero di
    property `97`. Da quel punto le due property generano gli stessi numeri (collisione garantita).
    Già dopo ~90.000 prenotazioni (numero `9700000`) property 96 sfora nello "spazio testuale 97xxxxx".
  - Con `propertyId` a numero di cifre diverso la sovrapposizione è ancora più caotica: property `9`
    → `910000`; dopo ~100k prenotazioni → `1010000` = inizio di property `10` (`1010000`); property
    `100` → `10010000` interseca lo spazio di property `1001`, ecc.
  - Il numero **non è unico a DB** (nessun `UNIQUE`), è ricavato con `parseInt` di una stringa e c'è
    anche una `create` ridondante in `booking_logs` (vedi [`booking_logs.md`](booking_logs.md)).
  - **In riscrittura**: sequenza/`IDENTITY` per-property realmente separata, oppure numero =
    `propertyId` + separatore + progressivo con zero-padding a lunghezza fissa **ampia** (es.
    `10795-0000012345`), con vincolo di unicità e generazione server-side.
- **Esempio di valori**: `"1079511235"`, `"1074911835"`, `"1061129866"`

### reservation_code
- **Tipo**: `varchar(255)` — default a schema `'0'`
- **Nullabile**: Sì
- **Descrizione**: codice alfanumerico usato negli URL pubblici cliente
  (`{basePath}/onlinebooking/consumer/{it|en}/{reservation_code}`) e nel QR code generato in
  `bookingDetailsById`. Ricercabile via `getBookingDetailsByReservationCode` /
  `confirmBookingByReservationCode` / `getBookingByReservationCode`. Condiviso fra le righe della
  stessa prenotazione.
- **Generazione e rischio duplicati**: **generato dal frontend**, non dal backend —
  `reservation_code: Math.random().toString(36).slice(2)` (in `front-pre` e `front-customer-pre`,
  `ThePlanner*/modules/PlannerBookingForm.js`, `PropertySignIn.js`) e passato nel payload
  (`prepareBookingData: reservation_code: singleObj.reservation_code`). Problemi:
  - lunghezza **variabile** (`Math.random()` può produrre stringhe corte, gli zeri finali cadono) —
    tipicamente 8-12 caratteri, a volte meno;
  - `Math.random()` **non è crittograficamente sicuro** e il codice compare in URL pubblici →
    potenzialmente enumerabile;
  - **nessun controllo di unicità** (né `UNIQUE` a DB, né `SELECT` prima dell'insert): due
    prenotazioni possono ricevere lo stesso codice; `getBookingDetailsByReservationCode` fa
    `WHERE reservation_code = ?` e itererebbe su entrambe.
  - **In riscrittura**: generare server-side con RNG sicuro, lunghezza fissa, `UNIQUE` a DB, retry su
    collisione.
- **Esempio di valori**: `"w9ap9gsqck"`, `"qdh9fuge7h"`, `"656ido6zvd"`

### booking_start_date
- **Tipo**: `datetime`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: inizio dell'intervallo prenotato, normalizzato a inizio giornata
  (`moment.parseZone(startDate).startOf('day')` in `processBookingDates`). Indicizzato.
- **Esempio di valori**: `2026-08-20 00:00:00`, `2026-07-27 00:00:00`

### booking_end_date
- **Tipo**: `datetime`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: fine dell'intervallo prenotato, normalizzato a fine giornata
  (`moment.parseZone(endDate).endOf('day')`). Per prenotazione di un solo giorno coincide con
  `booking_start_date` alla stessa data (`23:59:59`). Indicizzato.
- **Esempio di valori**: `2026-08-24 23:59:59`, `2026-07-27 23:59:59`

### property_id
- **Tipo**: `int`, `NOT NULL` (default applicativo `2` nel model, mai usato in pratica)
- **Nullabile**: No
- **Relazioni**: `properties.id` / `bathouses.propertyId` (nessuna FK). Indicizzato.
- **Descrizione**: stabilimento proprietario della prenotazione.
- **Esempio di valori**: 10795, 10749, 10611

### bathHouseId
- **Tipo**: `int` — default `0`
- **Nullabile**: Sì
- **Relazioni**: `bathouses.id` (nessuna FK). Indicizzato.
- **Descrizione**: id del `bathouse` (scheda pubblica dello stabilimento) associato. Passato dal client
  (`singleObj.bathHouseId`).
- **Esempio di valori**: 10461, 10434, 10414

### customerId
- **Tipo**: `int` — default `0`
- **Nullabile**: Sì
- **Relazioni**: `customers.id` (nessuna FK)
- **Descrizione**: id del cliente **principale** (non ospite) della prenotazione. Il record `customers`
  viene creato/aggiornato in `booking.controller` prima di creare la prenotazione (match per
  `email`/`mobile` + `propertyId`). `0` = nessun cliente principale collegato.
- **Esempio di valori**: 209301, 124476, 186612

### booking_agent
- **Tipo**: `longtext` — contiene di solito una **stringa** (id utente operatore), non JSON
- **Nullabile**: Sì
- **Relazioni**: `users.id` dell'operatore che ha effettuato la prenotazione da gestionale (nessuna FK).
  In `updateBooking` (helper) il campo viene **rimosso dal payload di update** (`delete data.booking_agent`)
  → l'agente non è più modificabile dopo la creazione.
- **Descrizione**: per prenotazioni da gestionale è l'id operatore; per prenotazioni online resta la
  stringa passata dal client (spesso vuota). `Commonhelper.findAccountName(agent)` lo risolve al nome
  account per le notifiche.
- **Da token, non dal body**: oggi l'id operatore arriva **nel payload del client** (`req.body.agent`)
  ed è quindi manipolabile (si può attribuire una prenotazione a un altro operatore). In riscrittura
  va preso **dal token JWT dell'utente autenticato** (claim `sub`/user id), mai dal body; per le
  prenotazioni online (nessun operatore) il campo resta `NULL`. Rinominare in `user_operator_id` con
  FK reale (vedi [`docs/03-gestione-dati.md` §3.2](../../docs/03-gestione-dati.md)).
- **Esempio di valori**: `"3128"`, `"2870"`, `"3322"`

### booking_consumer
- **Tipo**: `longtext` — oggetto JSON (dati del cliente principale, `is_guest === 0`)
- **Nullabile**: Sì
- **Descrizione**: snapshot dei dati del cliente principale al momento della prenotazione, ricavato da
  `singleObj.customerData.find(c => c.is_guest === 0)` in `prepareBookingData`. Usato per notifiche,
  email, ricevute, mascheramento (`maskBookingConsumer` per utenti non autenticati).
- **Campi**:
  - **name**: `string` — nome/ragione visualizzata del cliente. Esempio: `"Oliviero "`,
    `"Pierandrea Taurelli"`, `"Unknown"`
  - **mobile**: `string` — numero di telefono. Esempio: `"37485"`, `"3346910120"`, `"99999999"`
  - **email**: `string` (può essere `""`). Esempio: `"ptaurelli@gkmail.com"`, `""`
  - **id**: `number | string` — id `customers` del cliente (talvolta salvato come stringa).
    Esempio: `209301`, `"22462"`
  - **is_guest**: `number` — sempre `0` in questo oggetto (i guest stanno in `guest_data`).
    Esempio: `0`
  - **countryCode**: `string` — prefisso internazionale. Esempio: `"+39"`
- **Nullabile**: No (in pratica sempre valorizzato)
- **Esempio di valori**:
  `{"name":"Oliviero ","mobile":"37485","email":"","id":209301,"is_guest":0,"countryCode":"+39"}`

### guest_data
- **Tipo**: `longtext` — array JSON — default `null`
- **Nullabile**: Sì (`null` o `[]`; nel campione DB sempre `[]` — la feature guest è poco usata)
- **Descrizione**: elenco degli **ospiti** della prenotazione. Un "guest" è una **persona che occupa
  la postazione insieme al cliente principale ma non è l'intestatario della prenotazione**: in fase di
  prenotazione l'operatore/cliente può aggiungere più anagrafiche, ognuna con flag `is_guest`. Quella
  con `is_guest = 0` diventa `booking_consumer` + `customerId` (l'intestatario, quello a cui sono
  imputati pagamenti e comunicazioni); quelle con `is_guest = 1` finiscono qui in `guest_data`. Ogni
  guest genera comunque un record in `customers` (creato al volo se non esiste). Serve per: elenco
  presenze, check-in per nome, statistiche, e per far comparire la prenotazione anche nella lista
  "le mie prenotazioni" degli account consumer collegati a quei `customers` (via `guest_ids`).
  `prepareBookingData` lo popola con `singleObj.customerData.filter(c => c.is_guest === 1)`. In lettura
  (`bookingDetailsById`) le chiavi vengono rimappate a
  `guestFirstName`/`guestEmail`/`guestMobileNumber`.
- **Campi** (per elemento): stessa struttura di `booking_consumer`:
  - **name**: `string` — nome del guest. Esempio: `"Marco Bianchi"`
  - **mobile**: `string`. Esempio: `"3401234567"`
  - **email**: `string`. Esempio: `"marco.bianchi@example.it"`
  - **id**: `number` — id `customers` del guest. Esempio: `210044`
  - **is_guest**: `number` — `1`. Esempio: `1`
  - **countryCode**: `string`. Esempio: `"+39"`
- **Esempio di valori**:
  `[{"name":"Marco Bianchi","mobile":"3401234567","email":"marco.bianchi@example.it","id":210044,"is_guest":1,"countryCode":"+39"}]`

### guest_ids
- **Tipo**: `longtext` — array JSON di `int` — default `null`
- **Nullabile**: Sì (nel campione sempre `[]`)
- **Relazioni**: `customers.id` degli ospiti
- **Descrizione**: id `customers` degli ospiti (`guest_data[].id`), estratti in un array a parte per
  poterli usare nelle ricerche per cliente: `bookingDetailsByCustomerId` filtra con
  `[Op.or]: [{ customerId }, JSON_CONTAINS(guest_ids, '<customerId>')]` — così una prenotazione
  compare sia all'intestatario sia agli ospiti.
- **Esempio di valori**: `[210044, 210045]`

### booking_locations
- **Tipo**: `longtext` — array JSON — una voce per postazione prenotata in questa riga
- **Nullabile**: Sì (a schema); **obbligatorio non-vuoto lato applicazione** (`handleRegularBooking`
  rifiuta `locationData.length == 0`)
- **Descrizione**: elenco delle postazioni (ombrelloni/lettini/cabine/…) occupate. È il campo su cui
  operano i controlli di disponibilità (`isLocationAvailable` usa
  `JSON_UNQUOTE(JSON_EXTRACT(booking_locations,'$[*].Location'))`), i contatori inventario
  (`aggregateBookingItems` → `booking_items_counts_details`), la mappa spiaggia.
- **Campi** (per elemento):
  - **Location**: `number | string` — identificativo/etichetta postazione sulla mappa (numero, oppure
    codice tipo `"G14"`, `"A27"`). Esempio: `49`, `"G14"`, `"A27"`
  - **isBooked**: `boolean` — postazione occupata da prenotazione confermata. Esempio: `true`
  - **isAreaBooked**: `boolean` — postazione occupata tramite prenotazione "ad area" (conteggio, non
    posizione puntuale). Esempio: `false`
  - **isReserved**: `boolean` — postazione in stato "riservato" (blocco temporaneo). Esempio: `false`
  - **itemName**: `string` — tipo articolo base della postazione (`"Umbrella"`, `"Sunbed"`, `"Cabin"`, …).
    Esempio: `"Umbrella"`
  - **type**: `string` — `"package"` (pacchetto listino) o `"additional"` (articolo singolo).
    Esempio: `"package"`
  - **packageDetails**: array — composizione del pacchetto (voci con `it`, `name`, `quantity`, `id`).
    Esempio elemento: `{"it":"Ombrellone","name":"Umbrella","quantity":"1","id":"1"}`,
    `{"it":"Lettino","name":"Sunbed","quantity":"2","id":"5"}`
  - **booking_period**: `string` — periodo per la singola postazione (`"FullDay"` / `"Morning"` /
    `"Afternoon"`), aggiunto dal client per ogni location. Esempio: `"FullDay"`
- **Esempio di valori**:
  `[{"Location":49,"isBooked":true,"isAreaBooked":false,"isReserved":false,"itemName":"Umbrella","type":"package","packageDetails":[{"it":"Ombrellone","name":"Umbrella","quantity":"1","id":"1"},{"it":"Lettino","name":"Sunbed","quantity":"2","id":"5"}],"booking_period":"FullDay"}]`

### booking_location_zeroth
- **Tipo**: `varchar(255)` — **colonna presente a DB ma non dichiarata nel model Sequelize**
- **Nullabile**: Sì
- **Verifica utilizzo**: **letta** in query SQL grezze (`getLocationStatisticsBySector`,
  `getLocationStatisticsData` — `b1.booking_location_zeroth AS location`); **mai scritta da `back-pre`**
  (grep su tutto `back-pre`: nessun `INSERT`/`UPDATE` che la tocchi, nessun trigger a DB). Copia
  denormalizzata di `booking_locations[0].Location`. Popolata da backfill esterno una-tantum → può
  essere disallineata rispetto a `booking_locations` (spostamenti postazione, split).
- **Deprecato**: No (è letta), ma **non mantenuta dal codice applicativo**. In riscrittura sparisce:
  le statistiche per postazione devono derivare da una tabella di righe/postazioni della prenotazione,
  non da una colonna "prima location" denormalizzata.
- **Esempio di valori**: `"49"`, `"G14"`, `"A27"`

### consumer_name / consumer_mobile / consumer_email
- **Tipo**: `varchar(255)` — **colonne presenti a DB ma non dichiarate nel model Sequelize**
- **Nullabile**: Sì
- **Verifica utilizzo**: **lette** in query grezze — `getBookingsByName` filtra
  `AND consumer_name='<customerName>' AND consumer_mobile='<customerMobile>'`; `hotelService.js` fa
  `b.consumer_name AS customerName`. **Mai scritte da `back-pre`** (nessun `INSERT`/`UPDATE`/trigger).
  Copie denormalizzate di `booking_consumer.name` / `.mobile` / `.email`. Popolate da backfill esterno.
- **Deprecato**: No (lette), ma non mantenute dal codice → dati potenzialmente stale. In riscrittura
  sostituite da un JOIN alla riga `customers` (dato vivo) o, sui soli fatti storici, da colonne
  snapshot tipizzate popolate una volta (vedi
  [`docs/03-gestione-dati.md` §3.6](../../docs/03-gestione-dati.md)).
- **Esempio di valori**: `consumer_name` `"Oliviero Landolfi"`, `"Pierandrea Taurelli"`;
  `consumer_mobile` `"3374851122"`, `"3346910120"`; `consumer_email` `"ptaurelli@gkmail.com"`,
  `"oliviero.l@example.it"`

### booking_period
- **Tipo**: `varchar(255)` — default a schema `'FullDay'`
- **Nullabile**: Sì
- **Descrizione**: fascia oraria della prenotazione. Valori reali osservati: `"FullDay"` (97/100),
  `"Morning"` (3/100); `"Afternoon"` previsto dal codice ma non presente nel campione. Costante già
  formalizzata in `globalConstants.js` come `BookingPeriod = { FullDay, Morning, Afternoon }`. Usato
  per: filtri di disponibilità (`isLocationAvailable` con `booking_period='<x>'`), query
  `checkBookingDetails` (`FullDay` include anche `Morning`+`Afternoon`), gestione assenze/sharing per
  mezza giornata, logica OfSale (`ofsaleService.sharingCleaning`).
- **Candidato a enumerazione**: **Sì** — vedi
  [`docs/03-gestione-dati.md` §2, `BookingPeriod`](../../docs/03-gestione-dati.md).
- **Esempio di valori**: `"FullDay"`, `"Morning"`, `"Afternoon"`

### bookingByAdmin
- **Tipo**: `varchar(255)` — default a schema `'1'` — commento DB: *"1 is for admin/manager & 0 is for
  customer"*
- **Nullabile**: Sì
- **Descrizione**: canale di origine. `"1"` = prenotazione creata da gestionale (operatore); `"0"` =
  prenotazione creata dal cliente online. Deriva da `bookingBy` nel payload (`bookingBy == 1` →
  `bookingByAdmin = 1`). Valori reali: `"1"` (93/100), `"0"` (7/100). Guida testi notifica (con/senza
  suffisso "Online"), invio email al cliente (solo se `bookingByAdmin == 0`), varie query statistiche
  (`bookingByAdmin = 0` = "online"). In `updateBooking` (helper) il campo viene **rimosso dal payload di
  update**.
- **Candidato a enumerazione**: **Sì** (canale `Manager` / `Customer`) — valore booleano memorizzato come
  stringa `"0"`/`"1"`.
- **Nota riscrittura**: **diventerà un intero** (o meglio un `enum`/`smallint`), non più `varchar`
  `"0"`/`"1"`. Da consolidare con `service_reservation.bookingByAdmin` e
  `services_booking_counts.booking_type` (stesso concetto di canale) — vedi
  [`docs/03-gestione-dati.md` §2.4bis](../../docs/03-gestione-dati.md).
- **Esempio di valori**: `"1"`, `"0"`

### status
- **Tipo**: `tinyint(1)` — default a schema `1`
- **Nullabile**: Sì
- **Descrizione**: stato della prenotazione. Valori reali osservati: `1` (attiva/confermata, la
  stragrande maggioranza), `0` (associato sempre a `isDeleted = 1` — prenotazione cancellata), `2`
  (1 riga nel campione). Il codice usa anche `3`: in `booking.controller` se `payment_status == "2"`
  viene forzato `payment_status = 3` "per evitare doppie prenotazioni stesso giorno / stessa
  postazione". `isLocationAvailable` considera occupanti gli stati `IN (1,3)`. Praticamente tutte le
  query di lettura filtrano `status = 1 AND isDeleted = 0`.
- **Candidato a enumerazione**: **Sì** (stato del ciclo di vita prenotazione: `Confirmed`=1,
  `Cancelled`=0/2, `PendingDuplicateGuard`=3) — semantica da consolidare con il team prodotto.
- **Esempio di valori**: `1`, `0`, `2`, `3`

### isDeleted
- **Tipo**: `int` — default `0`
- **Nullabile**: Sì
- **Descrizione**: soft-delete. `1` = prenotazione cancellata (sempre in coppia con `status = 0`).
  Filtro standard `isDeleted = 0`. Indicizzato.
- **Esempio di valori**: `0`, `1`

### is_booking_confirmed
- **Tipo**: `int` — default a schema `1`
- **Nullabile**: Sì
- **Descrizione**: flag del flusso "prenotazione online che richiede conferma". **Semantica
  controintuitiva** (verificata nel codice):
  - alla creazione, `booking.controller` legge
    `properties.onlineBookingOptions.locationOptionObj.requestBookingConfirmation`: se `"true"` →
    `is_booking_confirmed = 1` = **"in attesa di conferma"** (la mail standard di conferma **non**
    viene inviata: `email.helper.js:952` invia solo se `is_booking_confirmed == 0 || confirm_request
    == 0`); se diverso da `"true"` → `is_booking_confirmed = 0` = **"finale/confermata"**, mail
    inviata subito.
  - `confirmBookingByReservationCode` (chiamato dal cliente dal link) fa
    `Booking.update({ is_booking_confirmed: 1 }, { where: { reservation_code } })` → **rimette/lascia
    `1`**: **non fa flippare lo stato**, si limita a inviare la mail di conferma. Quindi lo stato
    memorizzato non cambia mai al momento della conferma effettiva → **bug**: non è possibile
    distinguere "in attesa" da "confermata dal cliente".
  - `1` ≈ "non (ancora) confermata / da confermare", `0` ≈ "confermata / non serviva conferma".
- **In riscrittura**: rinominare (`confirmation_state` enum `not_required` / `pending` / `confirmed`)
  e far avanzare davvero lo stato alla conferma.
- **Esempio di valori**: `0`, `1`

### checkIn
- **Tipo**: `tinyint(1)` — default `0` (`false`)
- **Nullabile**: Sì
- **Descrizione**: cliente arrivato/registrato. Impostato a `true` da `checkInBooking` (prima riga per
  `booking_start_date` con quel `bookingNumber`). Indicizzato indirettamente via `status`.
- **Esempio di valori**: `0`, `1`

### checkOut
- **Tipo**: `tinyint` — default `0` — commento DB: *"0 is for not checkout & 1 is for checkout"*
- **Nullabile**: Sì
- **Descrizione**: prenotazione terminata anticipatamente / cliente uscito. Impostato da
  `checkOutBooking`; quando `checkOut = 1` le query di disponibilità usano `checkOut_date_time` invece
  di `booking_end_date` come limite superiore. `isLocationAvailable` filtra `checkOut = 0`.
- **Esempio di valori**: `0`, `1`

### checkOut_date_time
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: alla creazione è impostato uguale a `booking_end_date`
  (`data.checkOut_date_time = dateFor`); dopo un check-out anticipato viene aggiornato alla data/ora di
  uscita effettiva. È la data usata come "fine occupazione" quando `checkOut = 1`.
- **Esempio di valori**: `2026-08-24 23:59:59`

### seasonalPass
- **Tipo**: `tinyint(1)` — default `0` (`false`)
- **Nullabile**: Sì
- **Descrizione**: prenotazione stagionale / abbonamento. Filtrata da `getSeasonalBookings` e da vari
  filtri statistici (`seasonalPass = 1/0`). Passata dal client (`singleObj.seasonalPass`).
- **Esempio di valori**: `0`, `1`

### isSplit
- **Tipo**: `tinyint` — default `0`
- **Nullabile**: Sì
- **Descrizione**: la riga fa parte di una prenotazione "split location" (postazioni diverse con periodi
  diversi entro lo stesso `bookingNumber`). `1` se creata via `handleSplitBooking`
  (`convertIntoSplit`/`convertMultipleBookingIntoSplit`). In lettura viene esposto anche
  `isSplitedOnce = (isSplit == 1 ? 1 : 0)`.
- **Esempio di valori**: `0`, `1`

### isRepeat
- **Tipo**: `tinyint` — default `0`
- **Nullabile**: Sì
- **Descrizione**: flag "prenotazione ricorrente/ripetuta", copiato da `singleObj.isRepeat` in
  `prepareBookingData`. Nessuna logica di lettura significativa individuata in `back-pre` oltre alla
  persistenza → **campo scritto ma non consumato** (candidato dismissione o completamento feature).
- **Esempio di valori**: `0`

### isOrderFilled
- **Tipo**: `tinyint(1)` — default `0` (`false`)
- **Nullabile**: Sì
- **Descrizione**: flag "ordine/consumazioni evasi" gestito da `updateOrderFilled`. Riguarda la spunta
  degli ordini bar/servizio collegati alla prenotazione.
- **Esempio di valori**: `0`, `1`

### isRecalculate
- **Tipo**: `tinyint(1)` — default `0` (`false`)
- **Nullabile**: Sì
- **Descrizione**: indica che il prezzo della prenotazione è stato ricalcolato (payload
  `singleObj.isRecalculate`, gestito in `ReservationServices.js`). Usato lato frontend per la logica di
  ricalcolo prezzi; a backend viene solo persistito.
- **Esempio di valori**: `0`, `1`

### isBookingDoneBySuggestedPrice
- **Tipo**: `tinyint(1)` — default `0` (`false`)
- **Nullabile**: Sì
- **Descrizione**: la prenotazione è stata confermata accettando il **prezzo suggerito dal Revenue
  Management** (feature prezzo dinamico) invece del prezzo di listino. Copiato da
  `singleObj.isBookingDoneBySuggestedPrice`.
- **Esempio di valori**: `0`, `1`

### isMarketingCampaign
- **Tipo**: `tinyint(1)` — default `0` (`false`)
- **Nullabile**: Sì
- **Descrizione**: se `true`, alla creazione il cliente principale viene aggiunto alla contact list di
  default per le campagne marketing (`contactListService.addCustomer`).
- **Esempio di valori**: `0`, `1`

### hotelId
- **Tipo**: `int` — default `null` — commento DB: *"Foreign key to hotels table for convention
  agreement"*
- **Nullabile**: Sì
- **Relazioni**: `hotels.id` (convenzioni/strutture partner; nessuna FK reale). `hotel_payments`
  referenzia questa riga via `bookingId`.
- **Descrizione**: se valorizzato, la prenotazione è associata a una struttura convenzionata (hotel).
  Attiva l'elaborazione dei pagamenti hotel (`hotelPaymentService.processHotelBookingPayments`).
- **Esempio di valori**: `null`, `115`

### bookingType
- **Tipo**: `tinyint(1)` — default `0` — commento model: *"0 = non-hotel price list, 1 = hotel price
  list (no voucher), 2 = hotel voucher"*
- **Nullabile**: Sì
- **Descrizione**: tipo di listino applicato. `0` = listino stabilimento normale; `1` = listino
  convenzione hotel senza voucher; `2` = voucher hotel. In `prepareBookingData`/`updateBooking` viene
  forzato a `1` se c'è `hotelId` ma `bookingType` non è in `[0,1,2]`, e a `0` se non c'è `hotelId`.
  Valori reali: `0` (prevalente) e `1`.
- **Candidato a enumerazione**: **Sì** (`BookingListingType`: `Standard`/`HotelPriceList`/`HotelVoucher`).
- **Esempio di valori**: `0`, `1`, `2`

### booking_payments
- **Tipo**: `longtext` — oggetto JSON con il **riepilogo economico** della prenotazione
- **Nullabile**: Sì
- **Descrizione**: snapshot completo del calcolo prezzi/pagamenti prodotto dal frontend
  (`BookingCalcService`) e salvato tale e quale. Da qui `bookingPayments()` (helper) deriva la riga
  `booking_payments` (tabella): `total_amount = payableAmount`, `paid_amount = payableAmount - toPay`,
  `pending_amount = toPay`, `payment_mode = paymentMode`. `booking_number_informations.payments_new`
  ne conserva una copia.
- **Campi** (osservati sui dati reali):
  - **subTotal**: `number` — imponibile/prezzo base prima di sconti e supplementi. Esempio: `116`, `25`,
    `14.5`
  - **payableAmount**: `number` — totale dovuto dopo sconti/supplementi. Esempio: `110`, `25`, `14.5`
  - **appliedPrice**: `string | number` — prezzo forzato manualmente dall'operatore (`""` se non
    forzato). Esempio: `"110"`, `""`
  - **toPay**: `number` — residuo da incassare (0 = saldata). Esempio: `110`, `0`
  - **isPaidFlag**: `boolean` — prenotazione saldata. Esempio: `false`, `true`
  - **paidAmountData**: array di `{ date, paymentMode, amount }` — incassi/saldi registrati.
    Esempio elemento: `{"date":"2026-07-27T08:19:10.429Z","paymentMode":"Credit card","amount":25}`
  - **addPaidAmountData**: array `{ date, paymentMode, amount }` — incassi aggiunti in fase di modifica.
    Esempio: come sopra
  - **paymentMode**: `string | null` — modalità di pagamento principale (`"Cash"`, `"Credit card"`,
    `"Other"`, `null`). Esempio: `"Credit card"`, `null`
  - **deposit**: `number` — importo acconto/caparra corrente. Esempio: `30`
  - **depositList**: array di `{ depositDate, depositAmount, depositPaymentMethod }` — acconti
    registrati (usati da `checkCredit`/`creditDetails`). Esempio:
    `[{"depositDate":"2026-07-20T09:00:00.000Z","depositAmount":30,"depositPaymentMethod":"Cash"}]`
  - **getCustExpenseData**: array di `{ ExpenseDate, ExpenseAmount, ExpenseNote, PaidStatus }` —
    spese/consumazioni addebitate al cliente sulla prenotazione. Esempio:
    `[{"ExpenseDate":"2026-07-27T00:00:00.000Z","ExpenseAmount":8.5,"ExpenseNote":"2 caffè","PaidStatus":true}]`
  - **expenseDataTotal**: `number` — totale spese cliente. Esempio: `8.5`
  - **remainingPayment**: `number` — residuo (variante di `toPay`). Esempio: `80`
  - **addExtraAmount**: `number` — sovrapprezzo extra aggiunto manualmente. Esempio: `5`
  - **totalPaidValue**: `number` — totale già pagato. Esempio: `110`
  - **surchargeObj**: oggetto `{ type, addSurcharge }` — supplemento (commissione/servizio); `{}` se
    nessun supplemento. Esempio: `{"type":"fixed","addSurcharge":"1"}`
  - **reducedPriceObj**: array di `{ date, price }` — righe di riduzione prezzo per singola data.
    Esempio: `[{"date":"2026-08-22T00:00:00.000Z","price":20}]`
  - **selectedCustomerIcons**: array di stringhe — icone/tag cliente selezionati nel riepilogo (es.
    disabile, bambini). Esempio: `["kids","umbrella-front"]`
  - **isEditedFinalPrice**: `boolean | null` — `true` se il prezzo finale è stato modificato a mano.
    Esempio: `true`
- **Esempio di valori** (prenotazione con acconto e saldo parziale):
  `{"subTotal":116,"payableAmount":110,"appliedPrice":"110","toPay":80,"isPaidFlag":false,"paidAmountData":[{"date":"2026-07-27T08:19:10.429Z","paymentMode":"Cash","amount":30}],"paymentMode":"Cash","deposit":30,"depositList":[{"depositDate":"2026-07-20T09:00:00.000Z","depositAmount":30,"depositPaymentMethod":"Cash"}],"totalPaidValue":30,"surchargeObj":{"type":"fixed","addSurcharge":"1"},"addPaidAmountData":[],"reducedPriceObj":[],"selectedCustomerIcons":["kids"],"isEditedFinalPrice":true}`

### booking_discount
- **Tipo**: `longtext` — oggetto JSON (`{}` = nessuno sconto applicato)
- **Nullabile**: Sì
- **Relazioni**: replica un elemento di [`discounts.items`](discounts.md) (match per `name`);
  `discounts.items[].totalNoOfUsage` viene incrementato dopo la prenotazione (`booking.controller`,
  `JSON.parse(JSON.parse(discountRecord.items))` — doppio parse per la doppia serializzazione di
  `discounts.items`).
- **Descrizione**: voucher/sconto applicato all'intera prenotazione. Quando valorizzato è un elemento
  di `discounts.items` copiato per intero più i flag di applicazione.
  `TransactionEntryForPayAtProperty` legge `booking_discount.value` e `booking_discount.type`
  (`"percentage"` → `total - total*value/100`; `"fixed"` → `total - value`).
- **Campi** (struttura verificata su dato reale, riga id 269313):
  - **name**: `string` — **codice sconto** digitato dal cliente / nome del voucher (in `discounts` non
    esiste un campo "codice" separato dal nome). Esempio: `"LandolfiVincenzo26luglio"`, `"SCONTO10"`
  - **type**: `string` — `"fixed"` (importo fisso) o `"percentage"` (percentuale). Esempio: `"fixed"`
  - **value**: `string` — valore dello sconto (importo in € se `fixed`, percentuale se `percentage`);
    salvato come **stringa**. Esempio: `"57"`, `"10"`
  - **voucherType**: `string` — `"all"` (valido per qualunque prenotazione nel periodo) / `"custom"`
    (con condizioni aggiuntive sui `check*`) / `"withoutCustom"`. Esempio: `"all"`
  - **selectedRow**: array di nomi settore/fila a cui il voucher è limitato (riferimento per
    **stringa**). Esempio: `[]`; oppure `["Fila 1","Fila 2"]`
  - **checkBarService / checkNormalService / checkHourlyService / checkAdditionalServices**: `boolean`
    — per i voucher `custom`, limitano l'applicabilità alle sole categorie di servizio indicate.
    Esempio: `false`
  - **checkManagementFlag / checkOnlineFlag**: `boolean` — per i voucher `custom`, limitano il canale
    (gestionale / sito online). Esempio: `false`
  - **checkRoundup**: `boolean` — arrotondamento del prezzo dopo lo sconto. Esempio: `false`
  - **maximunNoUse**: `string` — numero massimo di utilizzi consentiti per il voucher (refuso
    "maximun"). Esempio: `"1"`, `"100"`
  - **totalNoOfUsage**: `number` — contatore utilizzi già effettuati (aggregato per voucher, non per
    cliente); confrontato con `maximunNoUse`. Esempio: `0`, `37`
  - **voucherDateRange**: array di `{ startDate, endDate, key }` — una o più finestre di validità.
    Esempio: `[{"startDate":"2026-07-27T00:00:00Z","endDate":"2026-08-31T00:00:00Z","key":"selection1"}]`
  - **status**: `number` — `1` = voucher attivo. Esempio: `1`
  - **currentId**: `number` — chiave UI temporanea client-side (`Math.random()`-based), **non** un id
    di business. Esempio: `99.99212201984497`
- **Esempio di valori** (riga reale, id 269313):
  `{"name":"LandolfiVincenzo26luglio","type":"fixed","voucherType":"all","value":"57","selectedRow":[],"checkBarService":false,"checkNormalService":false,"checkHourlyService":false,"checkRoundup":false,"maximunNoUse":"1","voucherDateRange":[{"startDate":"2026-07-27T00:00:00Z","endDate":"2026-08-31T00:00:00Z","key":"selection1"}],"currentId":99.99212201984497,"status":1,"totalNoOfUsage":0,"checkAdditionalServices":false,"checkOnlineFlag":false,"checkManagementFlag":false}`

### additionalAutoVoucher
- **Tipo**: `longtext` — array JSON — default `null`
- **Nullabile**: Sì (`null` o `[]`; nel campione DB sempre `[]`)
- **Descrizione**: voucher applicati **automaticamente** su articoli aggiuntivi (non digitati dal
  cliente ma agganciati da regole di listino/promo). Per ciascun elemento con `maximunNoUse` valido,
  `booking.controller` incrementa `discounts.items[].totalNoOfUsage`.
- **Campi** (per elemento): stessa struttura di `booking_discount` (elemento di `discounts.items`):
  `name`, `type` (`"percentage"`/`"fixed"`), `value`, `voucherType`, `maximunNoUse`,
  `totalNoOfUsage`, `voucherDateRange[]`, `status`, `currentId`, i vari `check*`.
- **Esempio di valori**:
  `[{"name":"ExtraLettinoGratis","type":"percentage","value":"100","voucherType":"custom","checkAdditionalServices":true,"maximunNoUse":"50","totalNoOfUsage":3,"voucherDateRange":[{"startDate":"2026-06-01T00:00:00Z","endDate":"2026-09-30T00:00:00Z"}],"status":1,"currentId":12.3456}]`

### additionalList
- **Tipo**: `longtext` — array JSON
- **Nullabile**: Sì (`[]` frequente)
- **Descrizione**: articoli aggiuntivi acquistabili per la prenotazione (lettini extra, cabina, sdraio,
  ...), con quantità selezionata. `value > 0` indica articolo acquistato (genera righe in
  `booking_items_counts_details` via `bookingCountsDetails`). `creditDetails` calcola
  `additionalItemTotalPrice = Σ(value * price)`.
- **Campi** (per elemento):
  - **name**: `string` — nome articolo in inglese (`"Sunbed"`, `"Cabin"`, `"Deckchair"`,
    `"Directors Chair"`). Esempio: `"Sunbed"`
  - **it**: `string` — nome in italiano (`"Lettino"`, `"Cabina"`, `"Sdraio"`, `"Sedia regista"`).
    Esempio: `"Lettino"`
  - **value**: `number` — quantità selezionata (`0` = non acquistato). Esempio: `2`
  - **price**: `number` — prezzo unitario. Esempio: `12`
  - **location**: `number | string` — postazione a cui l'articolo è associato. Esempio: `49`, `"G14"`
  - **hotelPrefixedName**: `string | null` — nome con prefisso hotel per prenotazioni in convenzione
    (`null` per le prenotazioni non-hotel). Esempio: `"Hotel Miramare - Lettino"`
- **Esempio di valori**:
  `[{"name":"Sunbed","value":2,"it":"Lettino","price":12,"location":49,"hotelPrefixedName":null},{"name":"Cabin","value":1,"it":"Cabina","price":25,"location":49,"hotelPrefixedName":null}]`

### certainAddItemData
- **Tipo**: `longtext` — array JSON
- **Nullabile**: Sì (`[]` nel campione)
- **Descrizione**: articoli aggiuntivi selezionati **solo per certi giorni** dell'intervallo (non per
  tutta la durata). Se non vuoto, `booking.controller` usa
  `additionalItemForCertainDaysCountsDetails` che scrive in
  [`additional_items_counts_details`](additional_items_counts_details.md) una riga per data
  (`selected_date`).
- **Campi** (per elemento): **name** (`string`, nome articolo), **value** (`number`, quantità),
  **dates** (`string`, data specifica `YYYY-MM-DD`), **location** (`number|string`, postazione),
  **it** (`string`, nome IT), **price** (`number`).
- **Esempio di valori**:
  `[{"name":"Sunbed","it":"Lettino","value":1,"dates":"2026-07-31","location":49,"price":12},{"name":"Sunbed","it":"Lettino","value":1,"dates":"2026-08-01","location":49,"price":12}]`

### packageSelectedData
- **Tipo**: `longtext` — array JSON — **il blob più grande della tabella** (contiene per ogni pacchetto
  l'intera griglia prezzi giorno-per-giorno, decine di KB per riga)
- **Nullabile**: Sì
- **Descrizione**: dettaglio completo dei pacchetti listino selezionati per la prenotazione, prodotto dal
  frontend. Usato per: calcolo/aggregazione articoli inventario (`aggregateBookingItems`,
  `bookingCountsDetails`), aggiornamento assenze/sharing (`updateAbsenceDetails`,
  `updateSharingDetails` iterano `PackageItem[].location`), estrazione righe fiscali
  (`FiskalyHelper.lineItemsFromBooking`), `transactionsPaymentsService.extractItems`
  (`TotalSumItem`/`PackageItemPrice`/`AdditionalItemPrice`), backfill `databaseChanges`.
- **Campi** (per elemento — pacchetto per una postazione):
  - **Location** / **selectedLoc**: `string` — postazione. Esempio: `"49"`, `"G14"`
  - **selectedItem**: `string` — articolo base. Esempio: `"Umbrella"`
  - **PackageName** / **packageName**: `string` — nome pacchetto listino. Esempio:
    `"Ombrellone e 2 lettini"`, `"Ombrellone e 2 lettini - Dipendente"`
  - **PkgData** / **PackageItem**: array — voci di dettaglio pacchetto; ogni voce ha `location`, `item`,
    `type` (`"package"`), `price`, `data` (metadati icona/mappa: `id` cella `"R13C6"`, `items` con
    `id`/`name`/`it`, url immagini CloudFront/S3, `currentRow`, `incrementId`, ...), `packageDetails`,
    `selectedPackageDetails`, `lineItemId` (uuid riga fiscale o `null`).
  - **selectedPackageDetails**: array — per ogni pacchetto: `packageId` (→ `price_lists`), `packageName`,
    `isCumulativeFlag`, `sectorRowsPkg` (es. `"Custom"`, `"Tutti Settori e file"`), `currentRow`
    (es. `"Fila Retro"`, `"Fila unica"`, `"Dalla 4ª Fila in poi"`), `bookingDays`, `seasonPeriodName`
    (es. `"Alta stagione"`, `"Stagione 2026"`), `edit_price_data` (array di ~90 elementi
    `{date, day, price, defaultPrice?, isPriceChange?}` — griglia prezzi per giorno di durata),
    `daily_price`, `weekend_price`, `days_7_price`, `days_14_price`, `monthly_price`, `weekendSetting`
    (es. `"Consider weekend saturday and sunday"`), `startDate`, `endDate`, `bookingPeriod`,
    `getSeasonalDateFlag`.
  - **AdditionalItem** / **AdditionalItemPrice**: array degli articoli aggiuntivi del pacchetto e loro
    prezzo complessivo. Esempio:
    `[{"name":"Sunbed","quantity":1,"price":12}]`, `AdditionalItemPrice`: `12`
  - **PackageItemPrice** / **TotalSumItem**: `number` — prezzo pacchetto / totale voce. Esempio: `116`
  - **packageItemString**: `{ english_initial, italian_initial }` — sigla sintetica (es. `"1U+2S"` /
    `"1O+2L"`). Esempio: `{"english_initial":"1U+2S","italian_initial":"1O+2L"}`
  - **selectedAdditionalItemDetails**: `null | array` — dettaglio degli articoli aggiuntivi scelti
    (nome, quantità, prezzo per giorno). Esempio:
    `[{"name":"Sunbed","it":"Lettino","quantity":1,"pricePerDay":"12.00"}]`
- **Esempio di valori** (troncato): `[{"Location":"49","selectedItem":"Umbrella","PackageName":"Ombrellone e 2 lettini","PkgData":[...],"PackageItem":[{"location":49,"item":"Umbrella","type":"package","price":116,"lineItemId":"1749d3ba-..."}],"PackageItemPrice":116,"TotalSumItem":116,"packageItemString":{"english_initial":"1U+2S","italian_initial":"1O+2L"},"selectedPackageDetails":[{"packageId":2123,"packageName":"Ombrellone e 2 lettini","currentRow":"Dalla 4ª Fila in poi","bookingDays":5,"seasonPeriodName":"Alta stagione","edit_price_data":[{"date":"2026-05-23T00:00:00.000Z","day":1,"price":27,"defaultPrice":27,"isPriceChange":0}, ...90 elementi...],"daily_price":"27","monthly_price":"525","weekendSetting":"Consider weekend saturday and sunday","startDate":"2026/08/20","endDate":"2026/08/24","bookingPeriod":"FullDay"}]}]`

### booking_map
- **Tipo**: `longtext` — JSON
- **Nullabile**: Sì
- **Verifica utilizzo / a cosa serve**: **colonna di fatto morta**. Doveva contenere uno snapshot
  della configurazione mappa spiaggia usata per la prenotazione. In `prepareBookingData` è impostata a
  `singleObj.bookindMap` e in `updateBooking` (controller) a `req.body.bookindMap` — ma **la chiave
  `bookindMap` non è mai prodotta dai frontend** (grep su `front-pre` / `front-customer-pre`: zero
  occorrenze) → il valore salvato è sempre `undefined`/`null`. Nel campione DB: **0 righe su 100** con
  `booking_map` valorizzato. A backend il campo viene solo **riletto e restituito** al client in ~15
  endpoint (mai usato in logica), spesso **senza `JSON.parse`** (incoerenza fra endpoint). Nella
  logica di split-booking (`convertIntoSplit`) è trattato come **array** e spezzato per-location
  (`booking_map: [item.booking_map[i]]`), ma su un valore che è sempre nullo. **Bug collaterale**:
  `updateBooking` (controller) imposta anche `packageSelectedData: req.body.bookindMap` → in modifica
  `packageSelectedData` verrebbe azzerato (mitigato solo dal fatto che `updateBooking` "vecchio" è in
  parte bypassato dal flusso `booking`).
- **Deprecato**: **Sì** — da eliminare in riscrittura. La configurazione mappa vive in
  `configure_maps` / `configurations`; se serve uno snapshot per-prenotazione, va ricavato da lì.
- **Esempio di valori**: — (nessuna riga valorizzata nel campione; il valore atteso sarebbe un
  oggetto/array con celle e icone della griglia mappa)
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### servicesData
- **Tipo**: `longtext` — oggetto JSON
- **Nullabile**: Sì
- **Descrizione**: servizi bar/normali/orari **prenotati contestualmente** alla postazione. Se presente,
  `booking.controller` chiama `serviceHelper.serviceReservationCounts` → scrive in
  `services_booking_counts`. Struttura diversa da quella di `service_reservation.servicesData` (qui
  chiavi `barServices`/`normalServices`/`houlryServices`/`HourServiceData`; là
  `barServiceData`/`normalServiceData`/`hourlyServiceData`).
- **Campi**:
  - **barServices**: array — servizi bar selezionati. Per elemento: `ServiceName` (`"Espresso"`),
    `ServicePrice` (`"1.20"`), `count` (numero), `percentIVA` (`"22"`), `CategoryType` (`"Bar"`),
    `selectedData` (array di `{date, click}` — per ogni giorno quante unità), `VariantList` /
    `selectedVariant`. Esempio elemento:
    `{"ServiceName":"Espresso","ServicePrice":"1.20","count":2,"percentIVA":"22","CategoryType":"Bar","selectedData":[{"date":"2026-07-27","click":2}],"selectedVariant":[]}`
  - **normalServices**: array — servizi "normali" selezionati, stessa struttura. Esempio elemento:
    `{"ServiceName":"Ingresso","ServicePrice":"5.00","count":1,"percentIVA":"10","CategoryType":"Normal","selectedData":[{"date":"2026-07-27","click":1}]}`
  - **houlryServices**: oggetto `{ bookedHour, bookedDate, bookedTime }` (refuso "houlry") — slot orario
    scelto per un servizio a ore. Esempio:
    `{"bookedHour":"1","bookedDate":["2026-07-27"],"bookedTime":"16:00-17:00"}`
  - **HourServiceData**: array — servizi a ore selezionati (nome, prezzo, slot). Esempio elemento:
    `{"hourly_service_name":"SUP","booking_slot_start_time":"16:00","booking_slot_end_time":"17:00","count":1,"booking_date":"2026-07-27"}`
- **Esempio di valori** (prenotazione con 2 caffè e un ingresso):
  `{"barServices":[{"ServiceName":"Espresso","ServicePrice":"1.20","count":2,"percentIVA":"22","CategoryType":"Bar","selectedData":[{"date":"2026-07-27","click":2}]}],"normalServices":[{"ServiceName":"Ingresso","ServicePrice":"5.00","count":1,"percentIVA":"10","selectedData":[{"date":"2026-07-27","click":1}]}],"houlryServices":{"bookedHour":"","bookedDate":[],"bookedTime":""},"HourServiceData":[]}`

### summaryServicesData
- **Tipo**: `longtext` — oggetto JSON
- **Nullabile**: Sì (`null` o oggetto; nel campione valorizzato in 90/100 righe)
- **Descrizione**: riepilogo delle **consumazioni bar / servizi "normali" pagate con la
  carta/tessera prepagata del cliente** (feature "Paga con carta"), da mostrare nel dettaglio
  prenotazione. È distinto da `servicesData` (servizi prenotati normalmente, pagati a struttura o
  online): qui ci sono solo le voci addebitate sul borsellino del cliente
  ([`card_transactions`](card_transactions.md), `transaction_type = 2`, `debit_details`). Popolato
  lato frontend da `reserveArticle/Modal/BarService.js` / `CardServiceTable.js` / `PayWithCard.js`
  (stato `barCardServices` / `normalCardServices`) e copiato da `prepareBookingData`
  (`summaryServicesData: singleObj.summaryServicesData`).
- **Campi**:
  - **barCardServices**: array — servizi bar pagati con carta. Per elemento (da `PayWithCard`
    `itemDetails`): `name` (`"Espresso"`), `price` (`"1.20"`), `serviceCount` (numero),
    `selectedVariantData` (array varianti), `totalPrice` (numero).
    Esempio: `[{"name":"Espresso","price":"1.20","serviceCount":2,"selectedVariantData":[],"totalPrice":2.4}]`
  - **normalCardServices**: array — servizi "normali" pagati con carta, stessa struttura.
    Esempio: `[{"name":"Ingresso","price":"5.00","serviceCount":1,"selectedVariantData":[],"totalPrice":5}]`
- **Esempio di valori**:
  `{"barCardServices":[{"name":"Espresso","price":"1.20","serviceCount":2,"selectedVariantData":[],"totalPrice":2.4}],"normalCardServices":[]}`

### booking_absence_dates
- **Tipo**: `longtext` — array JSON di date (stringhe `DD/MM/YYYY`)
- **Nullabile**: Sì (`[]` nella quasi totalità del campione; 1/100 valorizzato)
- **Descrizione**: date in cui il cliente/titolare ha dichiarato che **non userà** la postazione
  ("assenza pianificata"), rendendola disponibile per la rivendita. È l'**unico dei due campi
  effettivamente scritto** (`prepareBookingData: booking_absence_dates: singleObj.booking_absences`).
- **Relazione con `sharing_dates`**: sono **due rappresentazioni della stessa esigenza** ("libero il
  mio posto in questi giorni"), scelte in base a `configurations.plan_absence_as_sharing`:
  - se `plan_absence_as_sharing != true` → `booking.controller` chiama `CreateBookingAbsence` /
    `bookingHelper.updateAbsenceDetails` → righe in
    [`booking_absence_details`](booking_absence_details.md) (`status = 2` = disponibile);
  - se `plan_absence_as_sharing == true` → chiama `createSharingForAbsence` → righe in
    `sharing_details` (con crediti `credit_perday_resold` / `credit_value`), **e la colonna
    `bookings.sharing_dates` resta comunque `null`** (non viene scritta — vedi sotto).
  Quindi: `booking_absence_dates` è l'input, `booking_absence_details` **o** `sharing_details` è
  l'output, a seconda della configurazione dello stabilimento.
- **Esempio di valori**: `["30/07/2026","31/07/2026","05/08/2026"]`

### sharing_dates
- **Tipo**: `longtext` — array JSON
- **Nullabile**: Sì
- **Verifica utilizzo**: **colonna morta**. Dichiarata nel model ma **non presente in
  `prepareBookingData`** → nessun percorso di scrittura in `back-pre`. Nel campione: **0 righe su
  100** valorizzate. Viene solo **riletta** (con `safeParse`, che ritorna `[]`) in
  `checkBookingDetails` e in `user.controller` (`myBooking`). La condivisione reale della postazione
  è tracciata nella tabella `sharing_details` (`booked_by`, `bookingNumber`, `sharing_date`,
  `isBooked`, `isApplied`), non qui.
- **Deprecato**: **Sì** — da eliminare in riscrittura; l'informazione "in che giorni questa
  prenotazione ha ceduto/ripreso il posto in sharing" si ricava da `sharing_details`.
- **Esempio di valori**: — (nessuna riga valorizzata; il valore atteso sarebbe un array di date
  tipo `["2026-08-02","2026-08-03"]`)
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### areaDetails
- **Tipo**: `longtext` — array JSON — default `null`
- **Nullabile**: Sì (nel campione: 0/100 valorizzate — feature poco usata)
- **Descrizione**: distingue una prenotazione **"ad area"** da una prenotazione **"a postazione"**.
  In alcune spiagge esistono zone a **capienza** (es. "Prato", "Gazebo", area sacco) dove il cliente
  non sceglie un ombrellone puntuale ma **occupa N posti su un totale** dell'area. Quando
  `areaDetails != null`:
  - la prenotazione **non blocca una `Location` specifica** sulla mappa → viene **esclusa** dai
    controlli di disponibilità puntuale (`isLocationAvailable` ha `AND areaDetails IS NULL`) e, in
    `checkBookingDetails`, la riga è scartata a meno che una sua `booking_locations[].isAreaBooked`
    sia `false`;
  - la disponibilità dell'area si calcola sommando i `booked_count` per `areaName` / data
    (`Service/bookingService.getBookingAreaList` / `getBookingAreaListCount` leggono
    **`bookings.areaDetails`** con `JSON_CONTAINS(areaDetails, JSON_OBJECT('areaName', :areaName))` e
    confrontano con `totalAreaCount`);
  - la tabella [`booking_areas`](booking_areas.md) doveva contenere questi conteggi ma il suo writer
    `createBookingArea` **non è mai invocato** → l'unica fonte è questo campo JSON.
  Popolato in `prepareBookingData` con `singleObj.areaItemsCountDetails` (se `length > 0`), altrimenti
  `null`.
- **Campi** (per elemento — un'area occupata dalla prenotazione):
  - **areaName**: `string` — nome dell'area. Esempio: `"Prato"`, `"Gazebo prato"`
  - **selectedAreaCount**: `number | string` — posti occupati da questa prenotazione (il codice fa
    `Number(area.selectedAreaCount)` per gestire il caso stringa). Esempio: `2`
  - **totalAreaCount**: `number | string` — capienza totale dell'area. Esempio: `50`
- **Esempio di valori**:
  `[{"areaName":"Prato","selectedAreaCount":2,"totalAreaCount":50}]`

### mainPhoto
- **Tipo**: `longtext` — JSON
- **Nullabile**: Sì
- **Verifica utilizzo**: **colonna morta**. Doveva essere lo snapshot della foto principale dello
  stabilimento al momento della prenotazione (per email/PDF cliente). `prepareBookingData` **non la
  imposta** → nessun percorso di scrittura. Nel campione DB: **0 righe su 100** valorizzate. Gli unici
  `mainPhoto` letti in `booking.controller` (`getBookingDetailsByReservationCode`) provengono da
  `bathouses.mainPhoto` (la query fa `SELECT ... bathouses.mainPhoto ...`), non da `bookings.mainPhoto`.
- **Deprecato**: **Sì** — da eliminare. La foto si prende da [`bathouses`](bathouses.md) (con la
  scomposizione `origin`+`path`, vedi [`docs/03-gestione-dati.md` §1](../../docs/03-gestione-dati.md)).
- **Esempio di valori**: — (nessuna riga valorizzata)
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### AttachedFiles
- **Tipo**: `longtext` — array JSON — default `[]`
- **Nullabile**: Sì
- **Descrizione**: allegati caricati sulla prenotazione (documenti/immagini). Salva **URL S3
  completi** (stesso anti-pattern link-a-DB descritto in
  [`docs/03-gestione-dati.md` §1](../../docs/03-gestione-dati.md)).
- **Campi** (per elemento): tipicamente `{ name, url }` oppure la sola stringa URL.
- **Esempio di valori**:
  `[{"name":"documento.pdf","url":"https://summerbooking.s3.us-east-2.amazonaws.com/images/booking/att_3b2a9.pdf"}]`

### note
- **Tipo**: `text`
- **Nullabile**: Sì
- **Descrizione**: nota libera dell'operatore sulla prenotazione. In prenotazione multipla è preso da
  `note[multipleIndex]`.
- **Esempio di valori**: `"Amici Monachino - sotto ombrellone di Rossi"`, `"Cliente arriva dopo le 15"`

### ofsale_payments
- **Tipo**: `longtext` — **colonna presente a DB ma non dichiarata nel model Sequelize di `bookings`**
- **Nullabile**: Sì
- **Descrizione**: colonna omonima di quella "vera" in `booking_number_informations.ofsale_payments`.
  Su `bookings` **non ha percorso di scrittura** (il flusso OfSale scrive su
  `booking_number_informations`). Residuo/duplicato.
- **Deprecato**: **Sì** — colonna non usata dal codice su questa tabella.
- **Esempio di valori**: `null`

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW` (gestiti manualmente, `timestamps` Sequelize disattivati)
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-27 08:19:28`

---

## Osservazioni verificate nel codice (criticità)

- **Nome tabella involontario**: `booking.modle.js` passa 4 argomenti a `sequelize.define`; l'ultimo
  `{ freezeTableName: true, timestamps: false }` è **ignorato**, quindi il model `"booking"` mappa la
  tabella **`bookings`** (pluralizzata) e i `timestamps` restano di fatto disattivati solo perché il
  model non li dichiarerebbe comunque come attributi automatici. La tabella `booking` (singolare)
  esiste ma è vuota. Da chiarire e rinominare in modo esplicito nella riscrittura.
- **Tutto JSON in `longtext`**: nessuna colonna è di tipo `JSON` nativo; ogni lettura fa `JSON.parse`
  manuale (spesso con helper `safeJSONParse` che ritorna `[]`/`{}` in caso di errore, mascherando
  corruzioni). Alcuni endpoint (`checkBookingDetails`, `bookingDetails`) passano `booking_map` /
  `booking_discount` **senza** parse, incoerenza tra endpoint.
- **`packageSelectedData` gigantesco**: ogni riga replica l'intera griglia prezzi giorno-per-giorno
  (`edit_price_data`, ~90 elementi per pacchetto) più metadati mappa e URL immagini completi. Blob da
  decine di KB per riga, duplicati anche in `booking_payments.locationData` e
  `booking_number_informations`. Da normalizzare drasticamente (righe pacchetto + riferimento a
  listino/periodo, non snapshot completo).
- **Colonne denormalizzate non mantenute**: `booking_location_zeroth`, `consumer_name`,
  `consumer_mobile`, `consumer_email` sono lette da query SQL grezze ma **non vengono mai scritte da
  `back-pre`** (nessun trigger a DB). Popolate da backfill esterno → rischio dati stale/incoerenti con
  `booking_locations` / `booking_consumer`.
- **`booking_agent` / `bookingByAdmin` immutabili dopo la creazione**: `updateBooking` (helper) fa
  `delete data.booking_agent` e `delete data.bookingByAdmin` → un'eventuale modifica non li aggiorna
  mai. `booking_agent` è una stringa libera (id utente), senza FK.
- **`status` sovraccarico e non vincolato**: convivono i significati `0` (cancellata, con
  `isDeleted=1`), `1` (attiva), `2` (osservato, semantica ignota), `3` (guardia anti-doppia
  prenotazione stesso giorno, generato da `payment_status == "2"`). Nessun `ENUM`/`CHECK`.
- **`isRepeat` scritto ma non consumato**: nessuna logica di lettura in `back-pre`. Feature incompleta.
- **Colonne morte (dichiarate, mai scritte)**: `booking_map` (la chiave payload `bookindMap` non è
  prodotta da nessun frontend), `sharing_dates` (assente da `prepareBookingData`), `mainPhoto`
  (assente da `prepareBookingData`; i `mainPhoto` letti vengono da `bathouses`), `ofsale_payments`
  (duplicato della colonna vera su `booking_number_informations`). Nel campione: 0/100 righe
  valorizzate per ognuna. Da eliminare in riscrittura.
- **Contatore `bookingNumber` fragile**: `createBookingNumber` genera il numero come **concatenazione
  di stringhe** `propertyId + "10000"` e poi `parseInt(...) + 1`. Per `propertyId` con molte cifre o
  con molte prenotazioni i range di property diversi possono avvicinarsi; inoltre `createNewBooking`
  (helper) esegue un ulteriore `BookingLogs.create` **ridondante** ad ogni prenotazione perché il nuovo
  `createBookingNumber` non restituisce più il campo `response` che il ramo `if (bookingNumResponse.response)`
  si aspetta (vedi [`booking_logs.md`](booking_logs.md)).
- **IDOR sistemico**: gli endpoint operano su `req.body.propertyId` / `req.body.id` senza verificarli
  contro l'utente autenticato (`checkBookingDetails` è chiamabile anche non autenticati, con
  mascheramento parziale). Stesso pattern del resto del dominio.
- **Disponibilità postazione non transazionale**: `isLocationAvailable` fa una `SELECT` e poi il codice
  crea la prenotazione senza lock/`SELECT ... FOR UPDATE` sulle righe concorrenti → race condition tra
  due prenotazioni simultanee sulla stessa postazione/data/periodo. Inoltre la funzione, dentro un
  `for...of`, fa `return` dentro un `.then()` e alla prima iterazione: la logica di aggregazione del
  flag su più location è di fatto difettosa.
- **`checkBookingDetails` con `UNION ALL` + confronti `DATE()`**: query costruita per concatenazione di
  stringhe (`booking_id ? "b.id <> :booking_id" : "1=1"`), `LEFT JOIN hotels`, nessun indice mirato su
  `(property_id, booking_period, checkOut, status, isDeleted, booking_start_date, booking_end_date)`.
- **`mainPhoto` / `AttachedFiles` con URL completi**: stesso anti-pattern di
  [`docs/03-gestione-dati.md` §1](../../docs/03-gestione-dati.md).
- **Colonna `ofsale_payments` su `bookings`**: duplicato inutilizzato della colonna vera su
  `booking_number_informations` → da eliminare.
