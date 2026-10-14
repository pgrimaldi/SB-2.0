# Tabella `services_booking_counts`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **467.821 righe**,
> query completa — una delle tabelle più grandi del DB). `service_type`: `Normal Service` (255.480) /
> `Bar Service` (212.335) / `Hourly Service` (**solo 6**); `booking_type`: `1` (260.498) / `0`
> (207.315) / `2` (8); **`quantity = 0` su 457.618 righe (98%)** → la stragrande maggioranza delle
> righe non registra alcun consumo effettivo. `status` sempre `1`; `booking_date` include
> `0000-00-00`. Due model che mappano la **stessa** tabella
> `services_booking_counts`:
> [`back-pre/app/models/servicesBookingCounts.model.js`](../../../../../../back-pre/app/models/servicesBookingCounts.model.js)
> (`db.serviceBookingCounts`, versione completa con `bookingNumber`/`service_type`) e
> [`back-pre/app/models/serviceTypeCount.model.js`](../../../../../../back-pre/app/models/serviceTypeCount.model.js)
> (`db.serviceBookingType`, versione più vecchia senza quei campi). Scrittura in
> [`helpers/serviceReservation.helper.js`](../../../../../../back-pre/app/controllers/helpers/serviceReservation.helper.js)
> `serviceReservationCounts` (righe ~101-191), invocata sia da
> [`servicereservation.controller.js`](../../../../../../back-pre/app/controllers/servicereservation.controller.js)
> (prenotazione di soli servizi) sia da
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js) (servizi acquistati
> contestualmente a una prenotazione ombrellone, via `req.body.data[0].servicesData`); cancellazione in
> `servicereservation.controller` `cancelServiceBooking` (`UPDATE ... SET status = false WHERE
> booking_type = 0 AND reference_id = :serviceid`); lettura in
> `servicereservation.controller.getServiceBookingCount`.
> Frontend `front-pre`/`front-customer-pre`: `src/components/serviceReservation/**`.
>
> **Nota di contesto** — registro di consumo dei **servizi bar / servizi "normali"** venduti,
> **una riga per (servizio × data)**. Serve a contare quante unità di un dato servizio sono state
> vendute in un giorno per confrontarle con l'eventuale `maximumReservations` del servizio.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### service_name
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: nome del servizio (`ServiceName` dell'elemento in `servicesData`). Stringa libera
  definita nell'anagrafica servizi dello stabilimento.
- **Esempio di valori**: `"INGRESSO REGALO 15€"`, `"Abbonamento"`, `"Espresso"`

### service_type
- **Tipo**: `varchar(255)` — presente solo nella versione `servicesBookingCounts.model.js`
- **Nullabile**: Sì
- **Descrizione**: categoria del servizio. `serviceReservationCounts` scrive `"Bar Service"` per gli
  elementi in `barServiceData`/`barServices`, `"Normal Service"` per `normalServiceData`/`normalServices`.
  I servizi **orari** (`hourlyServiceData`) **non** vengono contati qui (ramo assente nella funzione)
  → gestiti da [`hourly_service_booking_management`](hourly_service_booking_management.md). Valori reali:
  `"Bar Service"`, `"Normal Service"`.
- **Candidato a enumerazione**: **Sì** (`ServiceCategory`: `Bar` / `Normal` / `Hourly`) — coerente con
  `services_types.type` e `servicetypes_icons.type` (`normal`/`hourly`/`bar`), vedi
  [`docs/03-gestione-dati.md` §2.4](../../progettazione/03-gestione-dati.md). Attenzione al drift: qui le
  etichette sono `"Bar Service"`/`"Normal Service"`, altrove `bar`/`normal`.
- **Esempio di valori**: `"Bar Service"`, `"Normal Service"`

### booking_type
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: origine del conteggio, passato come parametro `booking_type` a
  `serviceReservationCounts`. Dai chiamanti risulta valorizzato con il flag **`bookingByAdmin`**
  (`0` = da cliente/online, `1` = da gestionale) — salvato come **stringa**. `cancelServiceBooking`
  però filtra `booking_type = 0` (numerico) per disattivare le righe → funziona solo per le righe
  scritte con `"0"`. Valori reali: `"0"`, `"1"`.
- **Candidato a enumerazione**: **Sì**, ma è di fatto lo stesso vocabolario di
  [`bookings.bookingByAdmin`](bookings.md) (canale `Manager`/`Customer`) → da consolidare; il nome
  `booking_type` è fuorviante.
- **Esempio di valori**: `"0"`, `"1"`

### reference_id
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (quando i servizi sono acquistati con una prenotazione ombrellone) —
  in `booking.controller` `serviceReservationCounts(..., response.id, ...)` passa l'id della riga
  `bookings`; **oppure** `service_reservation.id` (prenotazione di soli servizi). Nessuna FK,
  salvato come stringa. Indicizzato.
- **Descrizione**: id dell'entità che ha generato il consumo del servizio.
- **Meglio `bookingNumber` che `booking_id`?** **Sì**, per due motivi: (1) la riscrittura basa la
  testata prenotazione sul `booking_number` (una prenotazione multi-riga = più `booking_id`, un solo
  `booking_number`) → riferirsi al `booking_id` frammenta i conteggi e complica cancellazione/modifica;
  (2) qui il campo è già polimorfo (booking **o** service reservation) senza discriminante affidabile —
  `bookingNumber` è l'unica chiave comune a entrambi gli aggregati e c'è già la colonna
  `bookingNumber` in tabella (oggi non popolata, vedi sotto). In riscrittura: `source_type` (enum
  `booking` / `service_reservation`) + `booking_number` (+ eventuale `source_id` per il dettaglio).
- **Esempio di valori**: `"269311"`

### bookingNumber
- **Tipo**: `varchar(255)` — default `0` — solo nella versione `servicesBookingCounts.model.js`
- **Nullabile**: Sì
- **Descrizione**: `serviceReservationCounts` **non** popola questo campo (il `data` costruito non lo
  include) → nel campione è sempre `"0"`.
- **Deprecato**: parzialmente — colonna dichiarata ma non scritta dal flusso attivo.
- **Esempio di valori**: `"0"`

### property_id
- **Tipo**: `varchar(255)` (sì, stringa)
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK). Indicizzato.
- **Esempio di valori**: `"10732"`

### quantity
- **Tipo**: `int`
- **Nullabile**: Sì
- **Descrizione**: unità di servizio consumate in quella data (`selectedData[].click`). Nel campione
  compaiono anche righe con `quantity = 0` (servizio presente nel carrello ma non selezionato per quel
  giorno).
- **Esempio di valori**: `0`, `1`

### booking_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: giorno di consumo del servizio (`selectedData[].date`).
- **Esempio di valori**: `2026-07-27 00:00:00`

### status
- **Tipo**: `int` nella versione `servicesBookingCounts.model.js`; `tinyint(1)` (default `1`) nella
  versione vecchia — **discrepanza tra i due model** sullo stesso campo
- **Nullabile**: Sì
- **Descrizione**: `1` = riga attiva (scritta così da `serviceReservationCounts`); `cancelServiceBooking`
  la porta a `false`/`0` per annullare il consumo. `getServiceBookingCount` somma solo `status = 1`.
- **Candidato a enumerazione**: **Sì** (`Active` / `Inactive`) — stesso concetto "attivo/non attivo"
  ricorrente nel dominio, vedi [`docs/03-gestione-dati.md` §2.1 punto 4](../../progettazione/03-gestione-dati.md).
- **Esempio di valori**: `1`

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW`
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-27 08:09:44`

---

## Osservazioni verificate nel codice (criticità)

- **Due model sulla stessa tabella con schema diverso**: `servicesBookingCounts.model.js`
  (`db.serviceBookingCounts`) e `serviceTypeCount.model.js` (`db.serviceBookingType`) definiscono
  entrambi `"services_booking_counts"` ma con campi e tipi diversi (`status` `int` vs `tinyint(1)`,
  presenza di `bookingNumber`/`service_type` solo nel primo). Solo il primo è usato per scrivere.
- **`booking_type` = `bookingByAdmin` travestito**: il nome suggerisce "tipo di prenotazione" ma
  contiene il canale (`0`/`1`). `cancelServiceBooking` fa `WHERE booking_type = 0` (numerico) →
  disattiva solo le righe di servizi acquistati da canale cliente, non quelle da gestionale.
- **Servizi orari non conteggiati qui**: `serviceReservationCounts` non ha il ramo
  `hourlyServiceData` → il "contatore" è incompleto rispetto ai tre tipi di servizio.
- **`reference_id` polimorfo per stringa**: punta a `bookings.id` o `service_reservation.id` a seconda
  del chiamante, senza discriminante esplicita affidabile (solo `booking_type` che però significa
  altro).
- **`bookingNumber` dichiarato ma non scritto**; **`property_id`/`reference_id` come `varchar`**.
- **Righe con `quantity = 0`**: la funzione crea una riga per ogni `selectedData` anche con `click = 0`
  → rumore in tabella.
- **Nessuna FK, nessun `ENUM`, nessun timestamp automatico coerente.**
