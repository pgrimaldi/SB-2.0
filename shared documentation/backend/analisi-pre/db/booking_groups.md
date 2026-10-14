# Tabella `booking_groups`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **69 righe**, query
> completa), model
> [`back-pre/app/models/bookingGroup.model.js`](../../../../../../back-pre/app/models/bookingGroup.model.js)
> (`sequelize.define("booking_group", ...)` → tabella `booking_groups`; ORM `db.booking_group`),
> scrittura in [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js)
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
- **Esempio di valori** (prod): `1` … `69` (contigui — nessuna cancellazione fisica).

### name
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: nome del gruppo, mostrato in UI. `upsertBookingGroup` impone l'**unicità globale**
  del nome (`BookingGroup.findOne({ where: { name } })` senza filtro per `property_id`) — vedi
  criticità.
- **Esempio di valori** (prod): `"January Bookings"`, `"abc"`, `"test_group_1"`, `"test_group_3"` — molti
  nomi sono di test; il campo è testo libero senza convenzioni.

### property_id
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: `properties.id` (nessuna FK)
- **Descrizione**: **in produzione la feature è concentrata su pochissimi stabilimenti**: 9 `property_id`
  distinti su 69 righe, di cui `96` (property di test) da solo 42 righe, `228` 10, `10746` 6; gli altri
  6 stabilimenti hanno 1–3 gruppi. Uso reale marginale.
- **Esempio di valori** (prod): `96`, `228`, `10746` — range `96` … `10843`.

### groupBookings
- **Tipo**: `longtext` (`Sequelize.JSON`), `NOT NULL` — array JSON di `int`
- **Nullabile**: No
- **Relazioni**: ogni elemento è un `bookings.id`. Nessuna FK.
- **Descrizione**: id delle prenotazioni appartenenti al gruppo. `upsertBookingGroup` valida che nessun
  id sia già presente in un altro gruppo (scansione di **tutti** i gruppi). In `bookingDetailsById` la
  ricerca del gruppo di una prenotazione fa `parsedGroupBookings.includes(id)`. In produzione gli array
  sono piccoli (2–3 elementi tipici, massimo osservato ~20 id, `maxlen` 274 caratteri).
- **Esempio di valori** (prod): `[109064,168744]`, `[168787,168756]`, `[169068,168942,169067]`

### status
- **Tipo**: `int` — default `1`
- **Nullabile**: Sì
- **Descrizione**: `1` = gruppo attivo. Le letture filtrano `status = 1` (soft-delete: un gruppo
  "cancellato" avrebbe `status` diverso da 1). **In produzione tutte le 69 righe hanno `status = 1`** —
  il soft-delete non è mai stato esercitato.
- **Candidato a enumerazione**: **Sì** (`Active`/`Inactive`) — stesso concetto "attivo/non attivo".
- **Esempio di valori** (prod): `1` (unico valore).

### createdAt / updatedAt
- **Tipo**: `datetime` — default `NOW` (timestamp Sequelize standard)
- **Nullabile**: Sì
- **Esempio di valori** (prod): range `2026-01-01 07:18:49` … `2026-08-17 18:05:26` (tutti i gruppi sono
  del 2026: feature introdotta di recente).

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
