# Tabella `booking_payments`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`; file da 277 MB, lettura
> parziale dei primi ~121 MB = **152.815 righe su ~350.000 stimate**). La porzione letta copre gli `id`
> 1…153.044, con `created_at` dal 2021-04-27 al 2025-08 → la tabella **conserva tutto lo storico dal
> 2021** (non è stata archiviata come `bookings`). Model
> [`back-pre/app/models/bookingPayments.model.js`](../../../../../../back-pre/app/models/bookingPayments.model.js)
> (definito con 4 argomenti a `sequelize.define` → il 4° `{ freezeTableName, timestamps }` è
> **ignorato**: il nome `booking_payments` regge solo perché già plurale, e `timestamps` **non** viene
> disattivato — da qui il doppio set di colonne data, vedi criticità), scrittura in
> [`helpers/booking.helper.js`](../../../../../../back-pre/app/controllers/helpers/booking.helper.js)
> (`bookingPayments(response, id, bookingNumber)`, righe ~1629-1660), lettura in
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js) `creditDetails`
> (query SQL grezza con `LEFT JOIN bookings ON bp.bookingNumber = b.bookingNumber`, righe ~2851-2933),
> `getPaidAndUnpaidBookings`, `getUnpaidBookings`, `checkCredit` (helper); associazione
> `db.bookingPayments.belongsTo(db.booking, { foreignKey: "booking_id" })` in
> [`models/index.js`](../../../../../../back-pre/app/models/index.js); frontend `front-pre`/`front-customer-pre`
> `src/services/ReservationServices.js`, schermate credito/incassi
> (`components/statistics/modules/UnpaidBooModal.js`, `AllBookings.js`).
>
> **Nota di contesto** — riga di **riepilogo economico "una per prenotazione"** (`booking_id`), derivata
> da `bookings.booking_payments` (JSON) al momento della creazione/modifica. Non è il libro-cassa dei
> singoli movimenti (quelli stanno dentro il JSON `bookings.booking_payments.paidAmountData` /
> `depositList` e, per gli stabilimenti abilitati, in [`transactions_payments`](transactions_payments.md)).
> Serve soprattutto alle viste "crediti / incassi / non pagati" per intervallo di date.
>
> **Dominio nella riscrittura**: è un dato di **stato riassuntivo della prenotazione** → resta in **C1**
> come parte della testata (chiave `booking_number`); i **movimenti** dentro `paidAmountData`/`depositList`
> vanno estratti in **C3 (Pagamenti)** come `payment_transactions`. Analisi completa in
> [`docs/03-gestione-dati.md` §4](../../progettazione/03-gestione-dati.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `153.044`+ (contiguo; totale stimato ~350.000).

### booking_id
- **Tipo**: `int`, `NOT NULL` — default a schema `0`
- **Nullabile**: No
- **Relazioni**: `bookings.id` (associazione ORM `belongsTo`, nessuna FK a DB). Indicizzato.
- **Descrizione**: prenotazione a cui si riferisce il riepilogo. In fase di **update** la riga viene
  però aggiornata con `where: { bookingNumber }` (non per `booking_id`) — vedi criticità.
- **Esempio di valori** (prod): `1`, `2`, `27` — range `1` … `157.317`.

### bookingNumber
- **Tipo**: `varchar(255)` — default model `0`, default a schema assente (`NULL`)
- **Nullabile**: Sì (`NULL` su 87 righe della porzione letta; presente il valore letterale `"0"`)
- **Relazioni**: `bookings.bookingNumber`
- **Descrizione**: numero prenotazione condiviso; chiave usata da `creditDetails` per fare il join con
  `bookings` e da `bookingPayments()` in update.
- **Esempio di valori** (prod): `"210004"`, `"210005"`, `"0"`.

### property_id
- **Tipo**: `int` — `NOT NULL` nel model, `NULL` ammesso a schema
- **Nullabile**: Sì (a schema; `NULL` su 202 righe della porzione letta)
- **Relazioni**: `properties.id` (nessuna FK). Indicizzato.
- **Esempio di valori** (prod): `1`, `2`, `35` — range `1` … `10622`.

### consumer_name
- **Tipo**: `varchar(255)`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: valorizzato da `response.booking_consumer.name` (snapshot nome cliente principale).
- **Esempio di valori** (prod): `"vishal"`, `"hitesh"`, `"salvo"`, `"AAA"` — testo libero, spesso
  minuscolo o placeholder.

### consumer_email
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: da `response.booking_consumer.email` (può essere `""` se il cliente è stato
  registrato solo con il telefono).
- **Esempio di valori** (prod): `"vp@gmail.com"`, `"peter@yopmail.com"`, `""`.

### total_amount
- **Tipo**: `float`
- **Nullabile**: Sì (`NULL` su 78 righe)
- **Descrizione**: `= bookings.booking_payments.payableAmount` (totale dovuto). Tipo `float` → rischio di
  imprecisione su importi monetari, **confermato dai dati**: il range in produzione va da `-80` a
  **`~2,147e9`** (valore spazzatura da overflow/parse errato) → serve `DECIMAL(10,2)` + validazione.
- **Esempio di valori** (prod): `101`, `410`, ma anche negativi e outlier assurdi.

### paid_amount
- **Tipo**: `float`
- **Nullabile**: Sì (`NULL` su 21 righe)
- **Descrizione**: `= payableAmount - toPay` (quanto già incassato). Range reale `-80` … `23.817`.
- **Esempio di valori** (prod): `50`, `20`, `10`, `5190`.

### pending_amount
- **Tipo**: `float`
- **Nullabile**: Sì (`NULL` su 54 righe)
- **Descrizione**: `= bookings.booking_payments.toPay` (residuo da incassare). Range reale `-3.700` …
  `49.600` (i negativi = incassato più del dovuto / storni).
- **Esempio di valori** (prod): `61`, `400`, `0`, `120`.

### payment_mode
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: modalità di pagamento principale, `= bookings.booking_payments.paymentMode`.
  **Distribuzione reale di produzione** (porzione letta): `NULL` 102.930 (67%), `"Credit card"` 28.979,
  `"Cash"` 13.506, `""` (stringa vuota) 5.584, `"Other"` 933, **`"Bank Transfer"` 883** (valore non
  previsto dalla versione precedente di questa scheda).
- **Candidato a enumerazione**: **Sì** (`PaymentMode`: `Cash` / `CreditCard` / `BankTransfer` / `Other`),
  con normalizzazione — il valore drift `""` è presente **anche qui** (non solo in
  [`service_reservation_payments`](service_reservation_payments.md)). Vedi
  [`docs/03-gestione-dati.md` §2](../../progettazione/03-gestione-dati.md).
- **Esempio di valori** (prod): `NULL`, `"Credit card"`, `"Cash"`, `""`, `"Bank Transfer"`, `"Other"`.

### type
- **Tipo**: `int` — default `1` — commento DB: *"1:Booking, 2:Service"*
- **Nullabile**: Sì
- **Descrizione**: soggetto del pagamento. Nella tabella `booking_payments` è **sempre `1`** (confermato
  in produzione: 152.815/152.815 righe lette). Le righe di tipo servizio stanno in
  `service_reservation_payments` (`type` default `2`). Usato in `creditDetails` per etichettare
  `type: 1 //booking`.
- **Candidato a enumerazione**: **Sì** (`PaymentSubjectType`: `Booking`=1 / `ServiceReservation`=2) —
  condiviso con `service_reservation_payments.type`.
- **Esempio di valori** (prod): `1` (unico).

### status
- **Tipo**: `tinyint(1)` — default `1` (`true`)
- **Nullabile**: Sì
- **Descrizione**: riga attiva. `creditDetails` filtra `bp.status = 1`. **In produzione**: `1` su
  152.494 righe, `0` su **321 righe** (non "solo 1" come nel campione dev).
- **Esempio di valori** (prod): `1` (152.494), `0` (321).

### startDate
- **Tipo**: `datetime`
- **Nullabile**: Sì (`NULL` su 238 righe)
- **Descrizione**: `= bookings.booking_start_date` (parse). Usato nei filtri per intervallo di
  `creditDetails` (`bp.startDate <= :endDate`). **Dati sporchi**: alcune righe hanno un timestamp con ora
  (`2021-04-23 14:23:37`) invece della mezzanotte — non sempre normalizzato a inizio giornata.
- **Esempio di valori** (prod): `2021-05-01 00:00:00`, `2021-04-23 14:23:37`.

### endDate
- **Tipo**: `datetime`
- **Nullabile**: Sì (`NULL` su 238 righe)
- **Descrizione**: `= moment(booking_end_date).endOf('day')`. Usato in `creditDetails`
  (`bp.endDate >= :startDate`). Range reale fino a `2026-11-29 23:59:59`.
- **Esempio di valori** (prod): `2021-05-04 14:08:10` (non normalizzato), `2026-11-29 23:59:59`.

### locationData
- **Tipo**: `longtext` — array JSON
- **Nullabile**: Sì
- **Descrizione**: **copia integrale** di `bookings.booking_locations` (stessa struttura: `Location`,
  `isBooked`, `isAreaBooked`, `itemName`, `type`, `packageDetails`, `booking_period`). Duplicazione di
  dati già presenti in `bookings`. **In produzione arriva fino a ~33 KB per riga** (`maxlen` 32.911) —
  peso significativo su una tabella da ~350 mila righe.
- **Esempio di valori** (prod): `["A1"]`, `["V1"]`, `["P12"]` per i casi minimi, fino a JSON di decine
  di KB.

### created_at / updated_at
- **Tipo**: `datetime` — default `NOW`
- **Nullabile**: Sì
- **Descrizione**: coppia "logica" gestita dal model.
- **Esempio di valori** (prod): range `2021-04-27 13:46:48` … `2025-08-18 09:25:57` nella porzione letta
  (storico completo dal 2021).

### createdAt / updatedAt
- **Tipo**: `datetime` — `NOT NULL` a schema
- **Nullabile**: No (a schema)
- **Descrizione**: coppia timestamp aggiunta automaticamente da Sequelize (il model non disabilita
  `timestamps`), **in parallelo** a `created_at`/`updated_at`. Ridondanza. **Dati sporchi confermati**:
  molte righe hanno `createdAt` / `updatedAt` = **`0000-00-00 00:00:00`** (data zero MySQL) — la coppia
  Sequelize non è stata popolata su tutto lo storico. `updatedAt` reale fino a `2026-09-06`.
- **Esempio di valori** (prod): `0000-00-00 00:00:00`, `2021-09-06 11:00:17`.

---

## Osservazioni verificate nel codice (criticità)

- **Doppio set di timestamp**: coesistono `created_at`/`updated_at` (dichiarati nel model) e
  `createdAt`/`updatedAt` (aggiunti da Sequelize perché `timestamps` non è disattivato). Quattro colonne
  data per lo stesso scopo — e in produzione la coppia `createdAt`/`updatedAt` (`NOT NULL` a schema) è
  spesso `0000-00-00 00:00:00`.
- **Importi `float` con valori impossibili**: `total_amount` in produzione arriva a ~`2,15e9` (overflow)
  e a valori negativi; `pending_amount` scende a `-3.700`. Nessuna validazione. Migrare a `DECIMAL(10,2)`
  con `CHECK`/bonifica.
- **`payment_mode` con 5 varianti + `NULL` al 67%**: include `"Bank Transfer"` e il drift `""`. Le viste
  "crediti/incassi" che raggruppano per `payment_mode` vanno riviste in migrazione.
- **Update per chiave sbagliata**: `bookingPayments(response, id, bookingNumber)` in update esegue
  `BookingPayments.update(data, { where: { bookingNumber } })` — se la prenotazione è multi-riga
  (stesso `bookingNumber`, più `booking_id`) **aggiorna tutte le righe con lo stesso riepilogo**,
  perdendo il dettaglio per singola data/postazione. In creazione invece la riga è per `booking_id`.
- **Importi in `float`**: `total_amount`/`paid_amount`/`pending_amount` sono `float` → usare
  `DECIMAL(10,2)` in riscrittura.
- **Dato derivato e potenzialmente stale**: la riga è uno snapshot di `bookings.booking_payments` al
  momento della create/update; se `bookings.booking_payments` cambia per altra via (es. webhook Stripe,
  OfSale, incassi da gestionale che riscrivono solo il JSON), `booking_payments` resta indietro.
- **`locationData` duplica `bookings.booking_locations`**: dato ridondante (decine di KB) senza
  necessità evidente.
- **Nessuna FK, nessun `ENUM`**: `payment_mode` è stringa libera con già un valore drift (`""`) nella
  tabella gemella dei servizi.
