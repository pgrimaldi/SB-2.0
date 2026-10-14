# Tabella `sharing_details`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **4.622 righe**, query
> completa; `createdAt` dal 2023-03-11 al 2026-09-08). Feature usata da ~9 stabilimenti, concentrata:
> `property_id` `10590` (1.940 righe), `231` (973), `10556` (833); presente un `property_id` sporco
> `9999`. `isBooked` `0` (3.027) / `1` (1.595). `credit_value` fra 1 e 15. Model
> [`back-pre/app/models/sharing.model.js`](../../../../../../back-pre/app/models/sharing.model.js)
> (`sequelize.define("sharing_details", ...)` → ORM `db.sharing`). Uso tracciato in
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js)
> (`const sharingDB = db.sharing;` — `createSharing`, `getSharingDates`, `getSharingCreatedBooking`,
> più ~15 query grezze `FROM sharing_details`),
> [`helpers/booking.helper.js`](../../../../../../back-pre/app/controllers/helpers/booking.helper.js)
> (creazione riga "consumo" alla conferma di una prenotazione su data condivisa; `UPDATE sharing_details`
> in 8 rami),
> [`helpers/cron.helper.js`](../../../../../../back-pre/app/controllers/helpers/cron.helper.js) `getSharingDetails`
> (cron `sharingCreditCron`),
> [`Service/ofsaleService.js`](../../../../../../back-pre/app/Service/ofsaleService.js) (`db.sharing.findAll`).
> Route in [`back-pre/app/routes/user.routes.js`](../../../../../../back-pre/app/routes/user.routes.js):
> `POST /api/sb/createSharing` (1562), `POST /api/sb/getSharingDates` (1594),
> `POST /api/sb/sharingCreditCron` (1626 → `cronHelper.getSharingDetails`),
> `POST /api/sb/get-sharing_created` (2458), `POST /api/sb/get-sharing-and-absence-dates` (2467) —
> tutte con solo `authJwt.optionalAuth` (nessuna autorizzazione reale).
>
> **Nota di contesto** — tabella **non censita** in [`_feature-tabelle.md`](_feature-tabelle.md) prima di
> questa scheda. È il registro della feature **"sharing" / rivendita del posto**: quando il titolare di un
> abbonamento (o di una prenotazione lunga) non userà la postazione in certi giorni, la dichiara
> "condivisa"; quel giorno la postazione torna prenotabile da altri e il titolare matura un **credito**.
> Strettamente legata a [`booking_absence_details`](booking_absence_details.md) (l'"assenza pianificata"
> è l'altra faccia della stessa funzione — vedi campo `is_sharing_coming_from_absence`). Esiste una
> tabella gemella precedente e morta **`sharings`** (0 righe, nessun model — vedi
> [`_feature-tabelle.md`](_feature-tabelle.md)) e un archivio
> **`sharing_details_archive_03072026`**.
>
> **Dominio di destinazione proposto**: **C1 — Prenotazioni**, sottoarea rivendita/`hold` (accanto a
> [`booking_absences`](booking_absences.md) / [`booking_absence_details`](booking_absence_details.md)).
> Nella riscrittura confluisce in `resource_consumptions`
> ([`docs/03-gestione-dati.md` §3.8](../../progettazione/03-gestione-dati.md)) con `source_type` dedicato
> (rivendita), più un piccolo ledger crediti. Il campo `email_sent` (canale/esito invio) →
> **C6 Comunicazione**.

---

## Come funziona (dal codice)

1. **Offerta** — `createSharing` (`POST /api/sb/createSharing`): ricevuti `booking_ids`, un elenco di
   `sharing_date` e `number_of_credit` / `credit_value`, per ogni prenotazione fa `sharingDB.destroy({ where: { booking_id, property_id } })`
   e poi un **bulk insert**: una riga per (postazione del pacchetto × data condivisa) — se
   `configurations.enable_sharing_on_all_article` è attivo per tutte le postazioni del pacchetto,
   altrimenti solo per la prima. `credit_value` viene salvato dopo `req.body.credit_value.replace(/€/g,"")`.
2. **Consumo** — alla conferma di una prenotazione che cade su una `(location, sharing_date, property_id)`
   presente in `sharing_details`, [`booking.helper.js`](../../../../../../back-pre/app/controllers/helpers/booking.helper.js)
   inserisce una **nuova riga** con `isBooked = 1`, `isApplied = 1`, `booked_by = <id della nuova prenotazione>`,
   `book_period_allowed = <booking_period della nuova prenotazione>` (rami distinti per FullDay / Morning /
   Afternoon e per articoli aggiuntivi). Da qui più righe con lo stesso `booking_id`/`sharing_date` e
   `booked_by` diversi (vedi campione).
3. **Accredito** — cron `sharingCreditCron` → `cron.helper.getSharingDetails`: seleziona
   `WHERE email_sent = 0 AND isBooked = 1 AND booked_by IS NOT NULL AND b.status = 1`, invia al titolare
   l'email "credito guadagnato", poi
   `UPDATE sharing_details SET isApplied = 1, email_sent = 1 WHERE booking_id = :b AND DATE(sharing_date) = :d`
   e `UPDATE users SET total_number_of_credit = :n, total_credit_value = :v WHERE email = :email`
   (**assegnazione, non incremento** — vedi criticità).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### booking_id
- **Tipo**: `int` (nel model `Sequelize.INTEGER`; nella tabella gemella morta `sharings` era `varchar(255)`)
- **Nullabile**: Sì (nessuna FK dichiarata)
- **Relazioni**: `bookings.id` della prenotazione **di origine** (il titolare che condivide la postazione).
  Join applicativo `LEFT JOIN bookings ON s.booking_id = bookings.id`.
- **Esempio di valori**: `169676`

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì (nessuna FK)
- **Relazioni**: `properties.id` (in alcune query incrociato anche con `bathouses.propertyId`).
- **Esempio di valori**: `10556`

### location
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: etichetta della postazione condivisa (cella mappa spiaggia). Confronto per stringa
  esatta contro `packageSelectedData[].PackageItem[].location` della prenotazione. Valori numerici o
  alfanumerici a seconda di come lo stabilimento nomina le postazioni.
- **Esempio di valori**: `"1"`, `"A12"`, `"R12C4"`
- **Candidato a enumerazione**: no — è un riferimento all'anagrafica postazioni (→ FK a `resource_id`
  nella riscrittura, come indicato in [`docs/03-gestione-dati.md` §3.8](../../progettazione/03-gestione-dati.md)).

### sharing_date
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: giorno in cui la postazione è liberata/condivisa (l'ora non ha significato; nei dati è
  a mezzanotte UTC → offset di fuso). In `createSharing` la data arriva in formato `dd-mm-yyyy` e viene
  invertita a mano (`.split("-").reverse().join("-")`).
- **Esempio di valori**: `2026-07-14 22:00:00`, `2026-07-15 22:00:00`

### number_of_credit
- **Tipo**: `int`
- **Nullabile**: Sì
- **Descrizione**: numero di crediti maturati dal titolare per quel giorno condiviso. In `createSharing`:
  se `dateData.isBooked != 0` usa `dateData.number_of_credit`, altrimenti `req.body.number_of_credit`
  (valore unico per tutta la richiesta).
- **Esempio di valori**: `1`

### credit_value
- **Tipo**: `int`
- **Nullabile**: Sì
- **Descrizione**: valore (economico) del credito. **Salvato come intero** ma in ingresso è una stringa
  con simbolo valuta rimosso a regex (`req.body.credit_value.replace(/€/g, "")`) → eventuali decimali
  vengono persi/troncati. Da normalizzare a `numeric(12,2)`
  ([`docs/03-gestione-dati.md` §3.7](../../progettazione/03-gestione-dati.md)).
- **Esempio di valori**: `1`

### isBooked
- **Tipo**: `int` (default `0`) — booleano di fatto
- **Nullabile**: Sì
- **Descrizione**: `0` = giorno condiviso ancora libero ("Waiting"); `1` = qualcuno ha prenotato quel
  giorno ("Completed"). Filtrato in `getSharingCreatedBooking` (`s.isBooked = 1/0`) e nel cron crediti
  (`isBooked = 1`).
- **Esempio di valori**: `0`, `1`
- **Candidato a enumerazione**: no — booleano (`is_booked`).

### isApplied
- **Tipo**: `int` (default `0`) — booleano di fatto
- **Nullabile**: Sì
- **Descrizione**: `1` = il credito per quel giorno è stato elaborato/accreditato al titolare (impostato
  sia in `booking.helper.js` alla creazione della riga di consumo, sia dal cron crediti insieme a
  `email_sent = 1`).
- **Esempio di valori**: `0`, `1`
- **Candidato a enumerazione**: no — booleano (`is_applied`).

### booked_by
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` della prenotazione che **ha consumato** il giorno condiviso (in
  `booking.helper.js` è `response.id`, cioè la prenotazione appena creata). **Non** è un `users.id`.
- **Esempio di valori**: `250822`, `252470`, `244507`

### book_period_allowed
- **Tipo**: `varchar(255)` (dichiarato **due volte** nel model — vedi criticità)
- **Nullabile**: Sì
- **Descrizione**: fascia della giornata condivisa/consumata. In `createSharing` = `booking_period` della
  prenotazione di origine; nella riga di consumo = `booking_period` della nuova prenotazione. Usato per
  decidere se la nuova prenotazione "collide" con la finestra condivisa.
- **Esempio di valori**: `"FullDay"`, `"Morning"`, `"Afternoon"`
- **Candidato a enumerazione**: **sì** — `enum BookingPeriod { FullDay, Morning, Afternoon }`, lo stesso
  già mappato in [`docs/03-gestione-dati.md` §2.4bis](../../progettazione/03-gestione-dati.md) per
  `bookings.booking_period` / `booking_absence_details.book_period_allowed`.

### bookingNumber
- **Tipo**: `varchar(255)` (model: `defaultValue: 0` — incoerente col tipo)
- **Nullabile**: Sì
- **Relazioni**: `bookings.bookingNumber` della prenotazione di origine (denormalizzato per query
  dirette).
- **Esempio di valori**: `"1055617893"`

### email_sent
- **Tipo**: `int` (default `0`) — booleano di fatto
- **Nullabile**: Sì
- **Descrizione**: `1` = l'email "credito guadagnato" è stata inviata al titolare dal cron crediti.
  È un **flag di canale/esito invio** → nella riscrittura non è un campo di questo aggregato ma una riga
  del log notifiche del modulo **C6** ([`docs/03-gestione-dati.md` §4](../../progettazione/03-gestione-dati.md),
  nota "canale + inviato").
- **Esempio di valori**: `0`, `1`
- **Candidato a enumerazione**: no — booleano; l'esito reale (inviata/fallita/…) va modellato in C6.

### is_sharing_coming_from_absence
- **Tipo**: `tinyint(1)` (model `Sequelize.BOOLEAN`, default `false`)
- **Nullabile**: Sì
- **Descrizione**: `1` = la condivisione è stata generata a partire da un'**assenza pianificata**
  ([`booking_absence_details`](booking_absence_details.md)) e non da una richiesta esplicita di sharing.
  In `createSharing` è passato pari pari da `req.body.is_sharing_coming_from_absence`.
- **Esempio di valori**: `0`, `1`

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard.
- **Nullabile**: No
- **Esempio di valori**: `2026-07-26 07:45:33`

---

## Osservazioni verificate nel codice (criticità)

- **Nessuna FK**: `booking_id`, `property_id`, `booked_by` sono interi liberi; `location` /
  `bookingNumber` sono stringhe. Righe orfane possibili senza vincolo.
- **SQL injection**: `createSharing`, `getSharingCreatedBooking`, `getSharingDates` e la maggior parte
  delle ~15 query costruiscono la stringa SQL per interpolazione diretta di `property_id`,
  `start_date`, `location`, ecc. (stesso pattern del resto di `booking.controller.js`).
- **`destroy` + bulk `INSERT` non transazionale**: `createSharing` cancella tutte le righe della
  prenotazione e le ricrea; un errore a metà lascia la postazione senza righe di sharing.
- **Bug di confronto in `getSharingCreatedBooking`**: `status: (sharing.isBooked = 1 ? "Completed" : "Waiting")`
  è un'**assegnazione**, non un confronto → `status` è sempre `"Completed"` e la riga viene mutata in
  memoria.
- **`credit_value` intero con parsing a regex**: `req.body.credit_value.replace(/€/g, "")` — perde i
  decimali e assume che il client mandi sempre una stringa; da portare a `numeric(12,2)`.
- **Accredito che sovrascrive**: il cron fa `UPDATE users SET total_number_of_credit = :n, total_credit_value = :v`
  (assegnazione secca su `users` per `email`), non `+=` → prenotazioni multiple con crediti si
  sovrascrivono a vicenda invece di sommarsi; inoltre l'ancoraggio è l'**email** dell'utente, non l'id.
- **`book_period_allowed` dichiarato due volte** nel model (una senza `allowNull`, una con) — l'ultima
  definizione vince; residuo di merge.
- **`bookingNumber` con `defaultValue: 0`** su colonna `varchar` — incoerenza tipo/d-default.
- **Granularità ridondante**: più righe per lo stesso `(booking_id, sharing_date)` (una "offerta" +
  una per ogni `booked_by`); il cron deve deduplicare a mano con un `Set` di chiavi
  `booking_id_sharing_date_email` per non inviare email doppie.
- **Endpoint senza autorizzazione**: tutte le route montano solo `authJwt.optionalAuth` — un cron di
  business (`sharingCreditCron`) esposto come POST pubblico.
- **Sovrapposizione con [`booking_absence_details`](booking_absence_details.md)**: stesso concetto
  ("giorno in cui la postazione torna disponibile") modellato due volte, con `is_sharing_coming_from_absence`
  a fare da ponte fragile. Nella riscrittura → un'unica tabella di consumo/disponibilità
  ([`docs/03-gestione-dati.md` §3.8](../../progettazione/03-gestione-dati.md)).
