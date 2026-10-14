# Tabella `booking_absence_details`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **8.663 righe**, query
> completa), model
> [`back-pre/app/models/bookingAbsenceDetail.model.js`](../../../../../../back-pre/app/models/bookingAbsenceDetail.model.js)
> (ORM `db.bookingAbsenceDetail`; `sequelize.define("booking_absence_details", ...)`), scrittura in
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js): `CreateBookingAbsence`
> (righe ~8383-8449, crea le righe "assenza disponibile" `status=2 isBooked=0`), flusso `booking`
> (`bookingHelper.updateAbsenceDetails`, `BookingAbsenceDetail.destroy` per le assenze non ancora
> prenotate), `checkOutBooking` (`BookingAbsenceDetail.update`), `removeAbsenceDate`,
> `getAbsenceLocationDetails`, `getAbsenceDates`, `getAbsencesLocation`, `cancelPackageBooking`;
> scrittura/lettura anche in
> [`helpers/booking.helper.js`](../../../../../../back-pre/app/controllers/helpers/booking.helper.js)
> `updateAbsenceDetails` (righe ~2633-2752) e `getAbsenceData` (usata da `isLocationAvailable`).
> Frontend `front-pre` (solo gestionale): `src/components/reserveArticle/PlannedAbsence/PlannedAbsenceModal.js`,
> `src/services/ReservationServices.js` (`booking_absences` nel payload), pannello
> "assenze pianificate" / rivendita giorni.
>
> **Nota di contesto** — feature **"assenza pianificata / rivendita del posto"**: il cliente titolare
> (tipicamente uno stagionale) dichiara i giorni in cui **non userà** la sua postazione; per quei
> giorni la postazione torna prenotabile da altri. Ogni riga è **(postazione × giorno × prenotazione
> di origine)**. Alternativa: se `configurations.plan_absence_as_sharing = true` lo stesso concetto
> viene gestito tramite [`sharing_details`](sharing_details.md) (feature "sharing" con crediti al
> titolare) invece che qui; il ponte fra le due è il campo `sharing_details.is_sharing_coming_from_absence`.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): interi `1417` … `33580` per 8.663 righe (l'`AUTO_INCREMENT` è molto più
  alto: la tabella subisce `destroy` frequenti sulle assenze non ancora riprese).

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` della prenotazione **di origine** (quella del cliente titolare che
  rilascia il giorno). Nessuna FK.
- **Esempio di valori** (prod): `52921`, `53003`, `53525` — range `43.647` … `326.090`.

### bookingNumber
- **Tipo**: `varchar(255)` — default a schema `NULL`, default model `0`
- **Nullabile**: Sì
- **Relazioni**: `bookings.bookingNumber` della prenotazione di origine.
- **Esempio di valori** (prod): `"9610945"`, `"22810093"`, `"22810095"`

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK)
- **Esempio di valori** (prod): `96`, `228`, `10555`, `107` — range `42` … `10847`.

### location
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: identificativo/etichetta della postazione rilasciata (`PackageItem[].location` della
  prenotazione di origine). Confronto per stringa con `booking_locations[*].Location`.
- **Esempio di valori** (prod): `"8"`, `"19"`, `"A6"`, `"A9"` — misto di numeri puri e codici
  alfanumerici (conferma la fragilità del confronto per stringa).

### absence_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: giorno in cui la postazione è dichiarata libera. In input arriva come
  `DD/MM/YYYY` (`bookings.booking_absence_dates`) e viene convertito.
- **Esempio di valori** (prod): sempre a mezzanotte, range `2024-01-03` … `2026-09-30` (include giorni
  futuri rispetto all'export).

### status
- **Tipo**: `int`, `NOT NULL` — default `1`
- **Nullabile**: No
- **Descrizione**: stato della riga di assenza:
  - **`2`** = giorno **rilasciato e disponibile** per la rivendita (creato così da
    `CreateBookingAbsence`; `getAbsencesLocation`/`getAbsenceData` mostrano solo `status = 2`).
  - **`1`** = giorno **ripreso da un altro cliente** (assegnato): `updateAbsenceDetails` fa
    `SET isBooked = 1, book_period_allowed = NULL, booked_by = '<id nuova prenotazione>', status = 1`
    quando una nuova prenotazione occupa quel giorno.
  **Distribuzione reale di produzione** (8.663 righe): `2` = 4.758, `1` = 3.905 (≈55% / 45%).
- **Candidato a enumerazione**: **Sì** (`AbsenceSlotState`: `Available` = 2, `Reassigned` = 1) —
  numerazione controintuitiva (2 = disponibile), da normalizzare. Vedi
  [`docs/03-gestione-dati.md` §2](../../progettazione/03-gestione-dati.md).
- **Esempio di valori** (prod): `2` (4.758), `1` (3.905).

### isBooked
- **Tipo**: `int` — default `0`
- **Nullabile**: Sì
- **Descrizione**: `0` = giorno ancora libero da rivendere; `1` = giorno già ripreso da un'altra
  prenotazione. Ridondante con `status` (2↔0, 1↔1). **In produzione**: `0` = 4.725, `1` = 3.938 —
  i totali **non coincidono** con quelli di `status` (4.758 / 3.905): 33 righe hanno `status` e
  `isBooked` disallineati, conferma concreta del rischio segnalato in criticità.
- **Esempio di valori** (prod): `0` (4.725), `1` (3.938).

### booked_by
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` della **nuova** prenotazione che ha ripreso quel giorno (quando
  `isBooked = 1`); `NULL` finché il giorno è libero.
- **Descrizione**: usato anche in `updateBooking` (helper) per "restituire" un giorno quando la
  prenotazione che l'aveva ripreso viene spostata (`isBooked = 0, booked_by = null, status = 2`).
- **Esempio di valori** (prod): `NULL` su 4.725 righe (55%, giorni ancora liberi); quando valorizzato,
  `bookings.id` nel range `56.947` … `328.180`.

### book_period_allowed
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: fascia oraria per cui il giorno è rilasciato/prenotabile — stesso vocabolario di
  [`bookings.booking_period`](bookings.md) (`FullDay` / `Morning` / `Afternoon`) più `NULL` (quando il
  giorno è stato preso o quando la logica half-day lo azzera). `updateAbsenceDetails` implementa una
  logica: se il titolare rilascia `FullDay` e un nuovo cliente prende `Morning`, la riga passa a
  `Afternoon` ancora disponibile (e viceversa).
- **Candidato a enumerazione**: **Sì** (`BookingPeriod` nullable).
- **Distribuzione reale di produzione**: `FullDay` 4.631, `Morning` 94, `Afternoon` 47, `NULL` 3.891
  (45%). La rivendita a fascia oraria è quasi sempre a giornata intera; le mezze giornate sono
  marginali.
- **Esempio di valori** (prod): `"FullDay"`, `NULL`, raramente `"Morning"`/`"Afternoon"`.

### createdAt / updatedAt
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Esempio di valori** (prod): range `2024-01-02 17:28:55` … `2026-09-08 08:20:12` (la tabella è
  attiva fino alla data dell'export).

---

## Osservazioni verificate nel codice (criticità)

- **Doppia rappresentazione dello stato**: `status` (1/2) e `isBooked` (0/1) codificano la stessa
  informazione; `book_period_allowed` è un terzo pezzo di stato. Facile che si disallineino (nel
  codice ci sono `UPDATE` che toccano solo un sottoinsieme). **Verificato sui dati di produzione**:
  33 righe su 8.663 hanno `status`/`isBooked` incoerenti (`status=2` totali 4.758 vs `isBooked=0`
  totali 4.725).
- **Numerazione `status` controintuitiva**: `2` = disponibile, `1` = assegnato. Le letture pubbliche
  filtrano `status = 2`; una nuova prenotazione che occupa il giorno lo porta a `1`.
- **`updateAbsenceDetails` costruisce SQL per concatenazione di stringhe** (interpolazione diretta di
  `absence_date`, `list.location`, `response.id`, `response.property_id`) → SQL injection potenziale se
  quei valori arrivano dal client (location e id lo sono).
- **`updateAbsenceDetails` è un `new Promise` che risolve/rifiuta dentro un doppio ciclo `for`**:
  risolve alla prima iterazione, quindi il valore di ritorno è di fatto casuale rispetto a quante
  location/date sono state processate.
- **Confronto postazione per stringa** (`location` vs `booking_locations[*].Location`), fragile con
  location numeriche vs codici.
- **Nessuna FK, nessun `ENUM`/`CHECK`**; `book_period_allowed` stringa libera.
- **Tabella gemella legacy**: [`booking_absences`](booking_absences.md) (schema minimale, non più usata
  attivamente — in produzione ferma al 2024-07-30, vedi la sua scheda) e
  `booking_absence_details_archive_03072026` (archivio con 5.760 righe, `createdAt` fino al 2026-07-03:
  è lo split storico effettuato in quella data). Vedi
  [nota tabelle legacy in `_feature-tabelle.md`](_feature-tabelle.md).
