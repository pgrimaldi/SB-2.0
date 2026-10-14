# stabilimento — Gestionale: Planner (nuovo design)

> Area funzionale: la voce **Planner** del nuovo gestionale. Struttura, logica e tabella restano
> quelle attuali; cambiano il design e alcune funzionalità: raggruppamento righe/settori
> collassabili, ricerca combinata cliente + postazione, «Ricerca guidata» con filtri avanzati.
> Il **Planner Servizi Orari** resta funzionalmente invariato (solo restyling).
>
> **Fonte**: `docs/pre/Pagine principali - azioni.docx`, sezione «PLANNER»; mockup FIGMA
> *Gestionale Summer Booking e Sito Hub* (node `2811-331225` planner e ricerca guidata,
> `2805-325714` planner servizi orari).
>
> Logica di business:
> [`../../business-logic/stabilimento/02-planner.md`](../../business-logic/stabilimento/02-planner.md).
> Vedi anche [`03-planner-strumenti-mappa.md`](03-planner-strumenti-mappa.md) per calendario/zoom/
> stampa già documentati. Firme **ipotizzate** dove l'endpoint non è noto.

---

## 1. Apertura della tabella Planner

**Descrizione**: griglia con righe = postazioni (raggruppate per riga/settore, ogni gruppo
apribile/chiudibile con barra + freccia) e colonne = giorni. Ogni cella riporta lo stato della
postazione per quel giorno.
**Schermata**: node `2811-331225`.
**Ruolo**: gestore.

**Input → API** (`sb/getSeasonalBookings` + `sb/viewseasonsperiods`)
- `propertyId` / `bathHouseId` (int)
- `rangeStart` (date) / `rangeEnd` (date) — finestra di giorni visibile
- `seasonal` (bool)

**Output ← API**
- `sectors` (array) — `{ id, name, collapsed (bool preferenza), elementIds: [int] }`
- `elements` (array) — `{ id, label, sectorId, row }`
- `cells` (array) — `{ elementId, date, status: "free|reserved|occupied", bookingId?, slotFlags:
  { morning, afternoon }, paymentStatus }`

**Note**: lo stato collassato/espanso di ogni riga/settore è una preferenza (client o
`sb/updatePersonalDetails`).

---

## 2. Selezione multi-cella e creazione prenotazione / riserva

**Descrizione**: click-and-drag seleziona più celle; all'hover compaiono due barre con frecce per
estendere la selezione a sinistra/destra (su tablet/mobile sempre visibili). Con celle selezionate,
in alto a destra compaiono «Prenota» e «Riserva». «Riserva» colora le celle di viola; ricliccando su
una selezione già riservata i due pulsanti diventano un unico «Libera».
**Schermata**: node `2811-331225`.
**Ruolo**: gestore.

**Input → API**
- **Verifica selezione** (`sb/checkLocationIsAvailable`): `{ propertyId, bathHouseId, cells: [ {
  elementId, dateFrom, dateTo, slot } ] }` → `{ items: [ { elementId, available, pricing } ],
  unavailable: [ … ] }`
- **Prenota**: apre `Dettagli e prenotazioni` con le postazioni/date della selezione (vedi
  [`06-gestionale-dettagli-e-prenotazioni.md`](06-gestionale-dettagli-e-prenotazioni.md) az. 1).
- **Riserva**: `sb/hold-booking-locations` `{ elementIds, dateFrom, dateTo, slot }` →
  `{ holds: [ { elementId, dates: [date] } ] }`
- **Libera**: `sb/delete-hold-booking-locations` `{ holdIds }` **o** `{ elementIds, dates }`

**Output ← API**: come sopra.

---

## 3. Ricerca combinata (campo unico cliente + postazione)

**Descrizione**: il campo «Ricerca» accetta più criteri insieme: nome cliente, cognome, nome +
cognome, nome postazione, numero postazione. Restituisce righe che soddisfano uno qualsiasi dei
criteri.
**Schermata**: node `2811-331225`.
**Ruolo**: gestore.

**Input → API** (`sb/locationAutosuggestion` + `sb/customer-list`)
- `propertyId` / `bathHouseId` (int)
- `query` (string)

**Output ← API**
- `matches` (array) — `{ kind: "customer"|"location", elementId?, bookingId?, label, sublabel }`
  con posizionamento/scroll della griglia sul risultato scelto

---

## 4. Ricerca guidata con filtri avanzati

**Descrizione**: pannello con campi base — Selezione periodo (con numero di giorni accanto),
Selezione articolo (menu) — e sezione «Filtri avanzati» apribile con: 1) Righe e settori (menu);
2) Numero massimo di spostamenti (campo numerico); 3) «Vicino a» (menu postazione, priorità alla
prossimità). «Cerca» restituisce le postazioni disponibili; «Prenota» crea una prenotazione unica con
tutte le postazioni trovate, ciascuna per il periodo corretto. Funziona anche senza filtri avanzati.
**Schermata**: node `2811-331225`.
**Ruolo**: gestore.

**Input → API** (`sb/checkLocationIsAvailable` esteso; « firma ipotizzata »)
- `propertyId` / `bathHouseId` (int)
- `startDate` (date) / `endDate` (date)
- `articleId` (int) — tipo di articolo/postazione
- `sectorId` (int, opzionale) — filtro righe/settori
- `maxMoves` (int, opzionale)
- `nearElementId` (int, opzionale)

**Output ← API**
- `results` (array) — `{ elementId, label, sector, availableFrom (date), availableTo (date),
  distanceRank? }`
- `coversFullPeriod` (bool)

**Input → API — «Prenota»** (`sb/booking`)
- `segments` (array) — `[ { elementId, checkIn, checkOut } ]`
- `customerId` (int) **o** `customer` ({ … })

**Output ← API**: `bookingId` / `reservationCode`, `pricing`, `segments` confermati.

---

## 5. Planner Servizi Orari

**Descrizione**: identico all'attuale nel comportamento; solo nuovo design.
**Schermata**: node `2805-325714`.
**Ruolo**: gestore.

**Input → API** (`sb/getHourlyServiceBooking` / `sb/getPropertyServices` con `category: "hourly"`)
- `propertyId` / `bathHouseId` (int), `date` (date)

**Output ← API**: contratto invariato rispetto all'implementazione attuale — `{ services: [ { id,
name, slots: [ { time, capacity, booked } ] } ], bookings: [ { bookingId, serviceId, time, qty,
customerName } ] }`.

**Note**: nessuna modifica funzionale richiesta; documentato solo per completezza della voce Planner.
