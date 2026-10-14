# stabilimento — Planner: mappa spiaggia e strumenti

> Area funzionale: **unica parte del gestionale presente nei mockup**. Sono disegnati: la **mappa
> spiaggia** con postazioni numerate, settori, bar/ingresso/passerelle e una **barra strumenti**
> (occhio/anteprima, stampa, lista, zoom + / −, e sul lato destro calendario e vista affiancata); il
> **calendario del planner** ("Giugno 2026", giorno 1 selezionato); i tre strumenti operativi
> **Selezione multipla**, **Sposta prenotazione**, **Crea gruppo**.
> **Schermate**: mappa pp. 29, 35 (`immagine Mappa interattiva 1.png`, `Mappa postazioni libere.2*.svg`);
> strumenti pp. 31, 34; calendario pp. 32, 33.
>
> La logica di business dettagliata del planner **non è ancora redatta** (vedi
> [`business-logic/_indice.md`](../../business-logic/_indice.md), sezione "stabilimento — da fare").
> Le firme qui sono **ipotizzate**, coerenti con la mappa cliente (`sb/viewconfimaps`,
> `sb/get-booked-location`) e con endpoint gestionali oggi presenti (`sb/getBookingAreaList`,
> `sb/getSeasonalBookings`, `sb/reset-beach-map`, `sb/getLocationStatisticsBySector`, `sb/booking`).

---

## 1. Apertura della mappa spiaggia del planner

**Descrizione**: il gestore apre la piantina dello stabilimento per il giorno/periodo selezionato, con
lo stato di ogni postazione (libera, occupata, mattina/pomeriggio, in scadenza, ecc.).
**Schermata**: pp. 29, 35.
**Ruolo**: gestore.

**Input → API** (`sb/viewconfimaps` + `sb/get-booked-location`)
- `propertyId` (int) / `bathHouseId` (int)
- `date` (date) — giorno selezionato nel calendario (default: oggi)
- `seasonal` (bool) — vista stagionale
- `slot` (string, opzionale) — `"morning"|"afternoon"|"full"`

**Output ← API**
- `map` ({ width, height, backgroundImage, scale })
- `sectors` (array) — `{ id, name, priceListId }`
- `elements` (array) — `{ id, type, sectorId, label, x, y, rotation }`
- `locationStatus` (array) — `{ elementId, status, bookingId?, customerName?, paymentStatus?,
  checkIn?, checkOut?, slotFlags: { morning, afternoon } }`
- `legend` (array) — `{ key, color, label }` (verde=libero, rosso=occupato, "M"/"P"=mattina/pomeriggio,
  giallo/viola/arancio = altri stati)

---

## 2. Navigazione del calendario del planner

**Descrizione**: con le frecce ‹ › del calendario ("Giugno 2026") o selezionando un giorno, il gestore
cambia la data di riferimento e la mappa si aggiorna.
**Schermata**: pp. 32, 33.
**Ruolo**: gestore.

**Input → API**: come azione 1 con `date` aggiornata. Per marcare i giorni con occupazione alta/piena
nel calendario: `sb/getTotalBookingCountByPropertyId` con `{ propertyId, month: "YYYY-MM" }` →
`{ days: [ { date, bookedCount, totalCount, occupancyPercent } ] }`.

**Output ← API**: come azione 1.

---

## 3. Zoom della mappa (+ / −) e adattamento vista

**Descrizione**: dai pulsanti lente+ / lente− il gestore ingrandisce/riduce la piantina; l'icona
"occhio" attiva l'anteprima/adatta-a-schermo.
**Schermata**: p. 29 (icone in alto a destra e occhio in alto a sinistra).
**Ruolo**: gestore.

**Input → API**: nessuna (trasformazione solo lato client su `map.scale`).
**Output ← API**: n/a.

---

## 4. Stampa della mappa / esportazione

**Descrizione**: dall'icona stampante il gestore genera una versione stampabile della piantina con lo
stato del giorno.
**Schermata**: p. 29 (icona stampante).
**Ruolo**: gestore.

**Input → API** (`sb/printBeachMap` / render lato client)
- `propertyId` / `bathHouseId` (int)
- `date` (date) / `seasonal` (bool)
- `format` (string) — `"pdf"|"png"`

**Output ← API**
- `fileUrl` (string) — documento pronto per stampa/download

---

## 5. Vista elenco delle prenotazioni del giorno

**Descrizione**: dall'icona "lista" il gestore passa dalla mappa a una tabella delle prenotazioni della
data selezionata.
**Schermata**: p. 29 (icona lista in alto a destra).
**Ruolo**: gestore.

**Input → API** (`sb/getBookingAreaList` + `sb/getBookingAreaListCount`)
- `propertyId` / `bathHouseId` (int)
- `date` (date) / `seasonal` (bool)
- `search` (string, opzionale) — nome cliente / codice
- `status` (string, opzionale) — `"confirmed"|"pending"|"checkedIn"|"cancelled"`
- `page` (int) / `pageSize` (int)

**Output ← API**
- `total` (int)
- `rows` (array) — `{ bookingId, reservationCode, customerName, elements: [label], checkIn, checkOut,
  slot, paymentStatus, amountPaid, amountDue, channel: "online"|"desk", createdAt }`

---

## 6. Selezione multipla di postazioni

**Descrizione**: attivato lo strumento "Selezione multipla", il gestore seleziona più ombrelloni per
applicare un'azione in blocco (crea prenotazione unica, blocca, sposta, raggruppa).
**Schermata**: pp. 31, 34 (bottone "Selezione multipla").
**Ruolo**: gestore.

**Input → API**: la selezione è stato client (`selectedElementIds: int[]`). L'azione batch che segue
(es. creazione prenotazione) usa `sb/booking` con `items[]` su più `elementId` (vedi
[`../cliente-finale/04-prenotazione-postazioni.md`](../cliente-finale/04-prenotazione-postazioni.md)
azione 6). Per la verifica di disponibilità in blocco: `sb/checkLocationIsAvailable` con
`{ propertyId, bathHouseId, elementIds[], date, seasonal, slot }` →
`{ items: [ { elementId, available, pricing } ], unavailable: int[] }`.

**Output ← API**: come sopra.

---

## 7. Spostamento di una prenotazione su un'altra postazione

**Descrizione**: attivato "Sposta prenotazione", il gestore trascina una prenotazione esistente da un
ombrellone a un altro libero (stesse date).
**Schermata**: pp. 31, 34 (bottone "Sposta prenotazione", icona 4 frecce).
**Ruolo**: gestore.

**Input → API** (`sb/moveBooking` — da definire)
- `bookingId` (int)
- `fromElementId` (int) / `toElementId` (int)
- `dates` (date[], opzionale) — se lo spostamento vale solo per alcuni giorni

**Output ← API**
- `status` / `message`
- `available` (bool) — se la postazione di destinazione è libera per tutte le date
- `booking` ({ bookingId, elementIds, checkIn, checkOut }) — stato aggiornato
- `priceDelta` (number) — eventuale differenza se i settori hanno listini diversi

---

## 8. Creazione di un gruppo di postazioni

**Descrizione**: attivato "Crea gruppo", il gestore unisce più postazioni in un gruppo gestito come
un'unica unità (es. per famiglie numerose o eventi privati).
**Schermata**: pp. 31, 34 (bottone "Crea gruppo").
**Ruolo**: gestore.

**Input → API** (`sb/createLocationGroup` — da definire)
- `propertyId` / `bathHouseId` (int)
- `elementIds` (int[]) — postazioni del gruppo
- `name` (string) — etichetta del gruppo
- `validFrom` (date) / `validTo` (date, opzionali) — validità temporanea

**Output ← API**
- `status` / `message`
- `groupId` (int)
- `elements` (int[]) — postazioni effettivamente aggregate
- `conflicts` (array) — `{ elementId, reason }` per postazioni già prenotate/non aggregabili

---

## 9. Creazione di una prenotazione da banco (walk-in)

**Descrizione**: cliccando una postazione libera sulla mappa, il gestore registra una prenotazione per
un cliente presentatosi allo stabilimento (dati minimi + pagamento cassa/online).
**Schermata**: implicito dalla mappa interattiva; UI del form non disegnata → firma ipotizzata.
**Ruolo**: gestore.

**Input → API** (`sb/booking` con `channel: "desk"`)
- `propertyId` / `bathHouseId` (int)
- `items` (array) — `[ { elementId, sectorId, checkIn, checkOut, seasonal, slot, extras[] } ]`
- `customer` ({ firstName, lastName, phone, email? }) **o** `customerId` (int) se cliente già in
  anagrafica
- `paymentMode` (string) — `"cash"|"card"|"online"|"unpaid"`
- `notes` (string, opzionale)

**Output ← API**
- `bookingId` (int) / `reservationCode` (string)
- `status` (string) — `"confirmed"`
- `pricing` ({ subtotal, extrasPrice, total, currency })
- `receiptUrl` (string, se emesso documento di cassa)

---

## 10. Liberazione / reset della mappa

**Descrizione**: strumento per rimuovere assegnazioni provvisorie o ripristinare lo stato della mappa
(es. a fine giornata).
**Schermata**: non disegnata esplicitamente → firma ipotizzata (endpoint `reset-beach-map` presente).
**Ruolo**: gestore.

**Input → API** (`sb/reset-beach-map`)
- `propertyId` / `bathHouseId` (int)
- `date` (date)
- `scope` (string) — `"holdsOnly"|"all"` (solo fermi non pagati / tutto)

**Output ← API**
- `status` / `message`
- `clearedCount` (int)
