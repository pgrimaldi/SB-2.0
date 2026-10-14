# Tabella `booking_groups`

> Fonti: schema e dati reali (65 righe), model
> [`back-pre/app/models/bookingGroup.model.js`](../../back-pre/app/models/bookingGroup.model.js)
> (`sequelize.define("booking_group", ...)` → tabella `booking_groups`; ORM `db.booking_group`),
> scrittura in [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js)
> `upsertBookingGroup` (righe ~10760-10864, endpoint `POST /api/sb/booking-group/add`), lettura in
> `getGroupsByProperty` (`/api/sb/booking-group/list`), `getBookingGroupDetails`
> (`/api/sb/booking-group/details/:id`), e — soprattutto — in `bookingDetailsById` /
> `checkBookingDetails` per allegare `groupInfo` a ogni prenotazione che compare in un gruppo; usata
> anche come filtro in `checkCredit` (helper, `filter.group`). Cache Redis invalidata con
> `clearGroupCache`. Frontend `front-pre` (solo gestionale): `src/components/...` gestione gruppi
> prenotazioni.
>
> **Nota di contesto — a cosa serve**: raggruppamento **manuale** di prenotazioni **distinte** (con
> `bookingNumber` diversi, clienti diversi, spesso postazioni vicine) sotto un'unica etichetta scelta
> dall'operatore (es. "gli sconosciuti", "Marvel", "amici", "gruppo Rossi 12 pax"). Serve nel
> gestionale a: vedere/filtrare insieme tutte le prenotazioni di una comitiva, applicare azioni
> "di gruppo" e mostrare il `groupInfo` accanto a ciascuna prenotazione del gruppo nel dettaglio e
> nell'agenda (`bookingDetailsById` / `checkBookingDetails` allegano `groupInfo`). Non è la
> prenotazione multipla (quella condivide un solo `bookingNumber`): qui le prenotazioni restano
> indipendenti, il gruppo è solo un raccoglitore. **Una riga = un gruppo**; i membri sono `bookings.id`
> nell'array `groupBookings`. Una prenotazione può stare in **un solo** gruppo (validato lato codice).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### name
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: nome del gruppo, mostrato in UI. `upsertBookingGroup` impone l'**unicità globale**
  del nome (`BookingGroup.findOne({ where: { name } })` senza filtro per `property_id`) — vedi
  criticità.
- **Esempio di valori**: `"gli sconosciuti"`, `"Marvel"`, `"amici"`

### property_id
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: `properties.id` (nessuna FK)
- **Esempio di valori**: 96, 10787

### groupBookings
- **Tipo**: `longtext` (`Sequelize.JSON`), `NOT NULL` — array JSON di `int`
- **Nullabile**: No
- **Relazioni**: ogni elemento è un `bookings.id`. Nessuna FK.
- **Descrizione**: id delle prenotazioni appartenenti al gruppo. `upsertBookingGroup` valida che nessun
  id sia già presente in un altro gruppo (scansione di **tutti** i gruppi). In `bookingDetailsById` la
  ricerca del gruppo di una prenotazione fa `parsedGroupBookings.includes(id)`.
- **Esempio di valori**: `[253436, 253433, 252442]`, `[263318, 263313]`, `[178666, 178661]`

### status
- **Tipo**: `int` — default `1`
- **Nullabile**: Sì
- **Descrizione**: `1` = gruppo attivo. Le letture filtrano `status = 1` (soft-delete: un gruppo
  "cancellato" avrebbe `status` diverso da 1). Nel campione tutte le righe hanno `status = 1`.
- **Candidato a enumerazione**: **Sì** (`Active`/`Inactive`) — stesso concetto "attivo/non attivo".
- **Esempio di valori**: `1`

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW` (timestamp Sequelize standard)
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-23 07:51:21`

---

## Osservazioni verificate nel codice (criticità)

- **Unicità nome globale (bug multi-tenant)**: `upsertBookingGroup` verifica l'unicità di `name` su
  **tutte** le property, non per `property_id`. Due stabilimenti non possono avere un gruppo con lo
  stesso nome.
- **`nameExists.id !== id` con tipi disallineati**: `id` dal body può essere stringa, `nameExists.id`
  è numero → confronto `!==` sempre vero, la guardia "salta il record corrente" non funziona sempre.
- **Validazione O(N·M) in memoria**: per verificare che nessun `booking_id` appartenga ad altri
  gruppi, carica **tutti** i `booking_groups` e scorre gli array. Non scala.
- **`groupBookings` array di id senza FK**: se una prenotazione viene cancellata, resta un id orfano
  nell'array; nessuna pulizia.
- **`status` senza `ENUM`**; soft-delete implicito (nessun endpoint di delete esplicito individuato,
  solo `update` di `status`).
