# Tabella `service_reservation_payments`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **6.160 righe**, query
> completa; `created_at` dal 2021-05 al 2026-09). Model
> [`back-pre/app/models/reservationPayments.model.js`](../../../../../../back-pre/app/models/reservationPayments.model.js)
> (definito con 4 argomenti → il 4° `{ freezeTableName, timestamps }` è **ignorato**: `timestamps`
> resta attivo, da qui il doppio set di timestamp — vedi criticità; ORM `db.reservationPayments`),
> scrittura in
> [`helpers/serviceReservation.helper.js`](../../../../../../back-pre/app/controllers/helpers/serviceReservation.helper.js)
> `reservationPayments(response, body, id)` (righe ~23-53), lettura in
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js) `creditDetails`
> (`ServicePayments.findAll` con `include: [{ model: ServiceReservation }]`, righe ~2777-2833) e nelle
> viste crediti/incassi; associazione
> `db.reservationPayments.belongsTo(db.serviceReservation, { foreignKey: "service_reservation_id" })`.
> Frontend: schermate crediti/incassi in `front-pre` (`components/statistics/**`).
>
> **Nota di contesto** — riepilogo economico **"uno per prenotazione servizi"**
> (`service_reservation_id`), gemella di [`booking_payments`](booking_payments.md) per il canale
> servizi. Derivata da [`service_reservation.payments`](service_reservation.md) (JSON) alla
> creazione/modifica.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### service_reservation_id
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: `service_reservation.id` (`belongsTo`, nessuna FK). Indicizzato. In **update** la
  riga è aggiornata con `where: { service_reservation_id }`.
- **Esempio di valori**: 6319, 6318

### property_id
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: `properties.id` (nessuna FK). Indicizzato.
- **Esempio di valori**: 115

### bookingNumber
- **Tipo**: `varchar(255)` — default `0`
- **Nullabile**: Sì
- **Descrizione**: **`reservationPayments()` non popola questo campo** → nel campione è **sempre `"0"`**,
  anche se la `service_reservation` collegata ha un `bookingNumber` valorizzato. Da rivedere.
- **Deprecato**: parzialmente — colonna dichiarata ma non scritta dal flusso attivo.
- **Esempio di valori**: `"0"`

### consumer_name
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: `= service_reservation.payments`… no: `= customerData.firstName` (snapshot nome
  cliente principale).
- **Esempio di valori**: `"Ventura Gaetano"`, `"Test Test"`

### consumer_email
- **Tipo**: `varchar(255)`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: `= customerData.email`. Indicizzato.
- **Esempio di valori**: `"venturagaetano@icloud.com"`, `"Test@gmail.vom"`

### total_amount
- **Tipo**: `float`
- **Nullabile**: Sì
- **Descrizione**: `= parseFloat(payments.payableAmount) + parseFloat(payments.remainingPayment)`.
- **Esempio di valori**: `9.2`, `8.0`

### paid_amount
- **Tipo**: `float`
- **Nullabile**: Sì
- **Descrizione**: `= parseFloat(payments.payableAmount)`.
- **Esempio di valori**: `9.2`, `8.0`

### pending_amount
- **Tipo**: `float`
- **Nullabile**: Sì
- **Descrizione**: `= parseFloat(payments.remainingPayment)`.
- **Esempio di valori**: `0.0`

### payment_mode
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: `= service_reservation.payments.paymentMode`. Valori reali osservati: `"Credit card"`
  (38), `NULL` (26), `"Cash"` (24), **`""` stringa vuota** (12).
- **Candidato a enumerazione**: **Sì** (`PaymentMode`: `Cash` / `CreditCard` / `Other`) — **con drift
  già in atto** (`""` vs `NULL`), da normalizzare in migrazione. Condiviso con
  [`booking_payments.payment_mode`](booking_payments.md). Vedi
  [`docs/03-gestione-dati.md` §2](../../progettazione/03-gestione-dati.md).
- **Esempio di valori**: `"Credit card"`, `"Cash"`, `null`, `""`

### type
- **Tipo**: `int` — default `2`
- **Nullabile**: Sì
- **Descrizione**: soggetto del pagamento. In questa tabella è **sempre `2`** (`Service`),
  complementare a [`booking_payments.type`](booking_payments.md) (`1` = `Booking`). `creditDetails`
  etichetta `type: 0 //Service` in risposta (valore diverso ancora — vedi criticità).
- **Candidato a enumerazione**: **Sì** (`PaymentSubjectType`: `Booking` = 1 / `ServiceReservation` = 2)
  — condiviso con `booking_payments.type`, ma attenzione: il valore restituito al client da
  `creditDetails` è `0` per i servizi e `1` per le prenotazioni (mapping ancora diverso).
- **Esempio di valori**: `2`

### startDate
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: `new Date(Date.parse(body.startDate))` — data della prenotazione servizi. Usata nei
  filtri per intervallo di `creditDetails` (`DATE(startDate) BETWEEN ...`). Nel campione contiene
  data+ora della creazione (`2026-07-26 11:33:22`), non la sola data.
- **Esempio di valori**: `2026-07-26 11:33:22`

### created_at / updated_at
- **Tipo**: `datetime` — default `NOW`
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-26 11:33:25`

### createdAt / updatedAt
- **Tipo**: `datetime` — `NOT NULL` a schema (timestamp Sequelize automatici, il model **non**
  disattiva `timestamps` — a differenza del commento del model)
- **Nullabile**: No
- **Esempio di valori**: `2026-07-26 11:33:25`

---

## Osservazioni verificate nel codice (criticità)

- **Doppio set di timestamp** (`created_at`/`updated_at` + `createdAt`/`updatedAt`), come
  [`booking_payments`](booking_payments.md).
- **`bookingNumber` mai popolato** (`"0"`): la correlazione con `booking_number_informations` /
  `service_reservation` passa solo per `service_reservation_id`.
- **`type` con tre codifiche in gioco**: `1`/`2` a DB (`booking_payments`/`service_reservation_payments`),
  ma `creditDetails` restituisce `1` (booking) / `0` (service). Da unificare.
- **`payment_mode` con drift già presente** (`""` vs `NULL` vs valori) su una tabella di soli 100
  record.
- **Importi `float`** → usare `DECIMAL(10,2)`.
- **Update per `service_reservation_id`**: corretto (una prenotazione servizi = una riga), ma nessun
  `UNIQUE` a DB.
- **`total_amount` = paid + pending** (non "prezzo totale prenotazione" nel senso di listino): la
  semantica dei tre importi è "quanto è stato/deve essere incassato", non il valore del carrello.
