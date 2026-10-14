# Tabella `hourly_service_booking_management`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **2.381 righe**, query
> completa; `booking_date` dal 2021-06 al 2026-12, `createdAt` fino al 2026-09-07 → feature attiva ma
> di nicchia: ~21 `property_id` distinti, concentrata su pochi stabilimenti — `10807` 486 righe,
> `10804` 436, `274` 417). Model
> [`back-pre/app/models/hourlyServiceBookingManagement.model.js`](../../../../../../back-pre/app/models/hourlyServiceBookingManagement.model.js)
> (`freezeTableName: true`, `timestamps: false`; ORM `db.bookingManagement`), scrittura in
> [`helpers/common.helper.js`](../../../../../../back-pre/app/controllers/helpers/common.helper.js)
> `addBookingManagement` (righe ~564-623) — invocata da
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js) nel flusso `booking`
> quando `req.body.data[0].hourlyBookingServiceData` non è vuoto; lettura in
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js)
> `getHoulyServicesFromLocationBooking` (endpoint `POST /api/sb/getHourlyServiceBooking`) e in
> [`servicereservation.controller.js`](../../../../../../back-pre/app/controllers/servicereservation.controller.js).
> Frontend `front-pre`/`front-customer-pre`: `src/components/serviceReservation/**` e
> `src/components/reserveArticle/**` (prenotazione slot orari, es. SUP, gcommone, campo, ...).
>
> **Nota di contesto** — registro di prenotazione dei **servizi a fascia oraria** ("hourly service":
> attrezzature/attività prenotabili a slot, es. SUP, pedalò, campo da beach volley). **Una riga per
> (prenotazione × servizio orario × slot)**. È la variante "a slot" dei contatori servizi: mentre
> [`services_booking_counts`](services_booking_counts.md) conta bar/normali per giorno, questa tiene
> traccia degli slot `HH:MM-HH:MM` occupati per data.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): interi `1` … `3.176` per 2.381 righe (destroy+recreate lascia buchi).

### hourly_service_name
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: nome del servizio orario prenotato (`data.hourly_service_name`), come da anagrafica
  servizi dello stabilimento. Stringa libera.
- **Esempio di valori** (prod): `"Pedalò"`, e diversi nomi di test (`"abc service"`, `"xyz service"`,
  `"ssk test"`).

### bookingNumber
- **Tipo**: `varchar(255)` — default `0`
- **Nullabile**: Sì
- **Relazioni**: `bookings.bookingNumber`
- **Descrizione**: numero prenotazione. `addBookingManagement` in modifica fa
  `destroy({ where: { bookingNumber } })` e reinserisce.
- **Esempio di valori**: `"1080712211"`, `"1055620561"`

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK). Da `servicereservation.controller` può essere l'id di una
  prenotazione di servizi.
- **Esempio di valori**: 6315, 6312

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK)
- **Esempio di valori**: 10807, 10556

### user_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `users.id` — è l'**agente/operatore** che ha registrato la prenotazione dello slot
  (parametro `user_id` = `req.body.data[0].agent`). Non è il cliente. `999` compare come valore
  "operatore generico/sistema".
- **Esempio di valori**: 3313, 999

### booking_slot_start_time
- **Tipo**: `varchar(255)` — orario `HH:MM`
- **Nullabile**: Sì
- **Descrizione**: inizio dello slot prenotato. **Formato non uniforme in produzione**: sia `"08:00"`
  sia `"07.20"` (punto invece dei due punti) — nessuna validazione.
- **Esempio di valori** (prod): `"08:00"`, `"07.20"`, `"02:00"`.

### booking_slot_end_time
- **Tipo**: `varchar(255)` — orario `HH:MM`
- **Nullabile**: Sì
- **Descrizione**: fine dello slot prenotato (stessa incoerenza `:`/`.`).
- **Esempio di valori** (prod): `"09:00"`, `"07.30"`, `"02:15"`.

### booking_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: giorno dello slot (`new Date(data.booking_date)`). Nel campione l'ora è `10:00:00`
  (valore fisso proveniente dal client, non l'orario dello slot — quello è nei campi `*_time`).
- **Esempio di valori**: `2026-07-25 10:00:00`

### booked_count
- **Tipo**: `int` — default `1`
- **Nullabile**: Sì
- **Descrizione**: numero di posti/unità prenotati per quello slot (`data.count`). Serve per il
  confronto con l'eventuale capienza massima dello slot. **Distribuzione reale di produzione**: `1`
  (2.287), `2` (77), `3` (8), `6` (4), `4` (3), `5` (2).
- **Esempio di valori** (prod): `1` (96% delle righe), `2`.

### status
- **Tipo**: `tinyint(1)` — default `0` nel model, ma `addBookingManagement` scrive sempre `1`
- **Nullabile**: Sì
- **Descrizione**: `1` = slot attivo/occupato. **In produzione è `1` su tutte le 2.381 righe** (le righe
  vengono rimosse con `destroy` alla modifica/cancellazione, non disattivate).
- **Candidato a enumerazione**: **Sì** (`Active`/`Inactive`) — stesso concetto "attivo/non attivo".
- **Esempio di valori** (prod): `1`.

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW` (`timestamps: false`)
- **Nullabile**: Sì
- **Esempio di valori** (prod): range `2021-08-31 20:08:15` … `2026-09-07 15:39:13`.

---

## Osservazioni verificate nel codice (criticità)

- **`user_id` è l'operatore, non il cliente**: il nome del campo suggerisce il contrario; deriva da
  `agent`. `999` è un id "operatore generico" ricorrente in tutto il dominio.
- **Update = destroy + recreate per `bookingNumber`** non transazionale; nessuna disattivazione
  (`status` di fatto costante a `1`).
- **`status` con default a schema incoerente** (`0`) rispetto al valore sempre scritto (`1`).
- **`booking_date` con ora fittizia** (`10:00`): l'informazione oraria reale è solo nei campi
  `booking_slot_start_time`/`booking_slot_end_time`, che sono **stringhe** `HH:MM` (nessun controllo di
  sovrapposizione slot a livello DB/ORM).
- **`hourly_service_name` stringa libera**; nessuna FK verso l'anagrafica dei servizi.
- **Refuso storico nel nome della route/funzione**: `getHoulyServicesFromLocationBooking` /
  `/api/sb/getHourlyServiceBooking` (e `houlryServices` in `bookings.servicesData`).
