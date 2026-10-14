# stabilimento — Planner

> **Voce di menù**: seconda icona della sidebar (`_gotoPlanner` → `components/NewPlanner/NewPlannerParent`,
> rotta `/planner` e `/new-planner`). È la vista **a calendario/griglia** di tutte le prenotazioni
> (righe = postazioni, colonne = giorni), con strumenti di selezione multipla, spostamento, creazione
> gruppi, prenotazione rapida di postazioni.
>
> **Ruoli**: visibile ad `admin`, `cashoperator` e a chi ha licenza non‑`basic` (escluso `waiter`,
> `marketing`, `affiliate`, `lifeguard`, `advancedlifeguard`). Le limitazioni per `lifeguard`/`waiter`
> (prezzi/pannelli avanzati nascosti) sono **solo lato UI** (vedi [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md)
> §11.2, §11.10).
>
> **Metodo**: endpoint da `NewPlanner/plannerAction.js` (`planner/booking`, `location/reservation`,
> `viewconfiguration`) e controller `booking.controller.js` (`plannerBookings`, `upsertBookingGroup`,
> `getGroupsByProperty`/`getGroupDetails`/`deleteGroup`, `validateBookings`, `updateBookingNumber`,
> `getNextBookingDatesForLocations`, `databaseChanges`) + `locationreservation.controller.js`. La parte
> di prezzo è nel documento trasversale [`_motore-di-prezzo-e-sconti.md`](_motore-di-prezzo-e-sconti.md);
> la creazione/modifica prenotazione "piena" è la stessa della [Mappa](01-mappa.md) (`POST /api/sb/booking`).

---

## 1. Caricare il planner

**Azione**: l'operatore apre il planner su un intervallo di date; vede la griglia postazioni × giorni
con le prenotazioni collocate.

**Endpoint**: `POST planner/booking` (`booking.controller.js:plannerBookings`), `viewconfiguration`
(layout mappa/settori), `booking-group/list`.

**Logiche**:

- Iterazione su tutte le postazioni dei settori/file dello stabilimento e su tutti i giorni
  dell'intervallo; per ogni cella si determina lo stato (libera / occupata / assenza / sharing / hold /
  stagionale) incrociando `bookings`, `booking_absences`, `sharing_details`, `hold_locations`,
  `location_reservations`.
- Le prenotazioni multi‑postazione e i **gruppi** (`booking_groups`) sono resi come blocchi collegati.
- `getNextBookingDatesForLocations` calcola, per ogni postazione, la **prossima data occupata** (per
  mostrare fino a quando è libera).

**Calcoli**: nessuno (aggregazioni di sola lettura). **Eventi**: nessuno.

---

## 2. Prenotazione rapida di una o più postazioni (blocco)

**Azione**: selezione (anche multipla) di celle libere e "riserva" senza compilare una prenotazione
completa (senza cliente/prezzo) — es. per tenere delle postazioni.

**Endpoint**: `POST location/reservation` (`locationreservation.controller.js:locationReservation`);
rilascio con `POST freelocation/reservation` (`freeLocationReservation`).

**Logiche**:

- Il body è un **array** di oggetti prenotazione; per ciascuno servono `agent`, `startDate`, `endDate`,
  `locationId` (400 se mancano).
- **Iterazione**: per ogni oggetto si espande l'intervallo in **tutte le date** comprese
  (`getAllDatesInRange`) e si crea **una riga `location_reservations` per ogni giorno**
  (`startDate` = inizio giornata, `endDate` = fine giornata), con `agent`, `locationId`, `propertyId`,
  `locationData`, `reservedObject`.
- Il rilascio (`freeLocationReservation`) cancella le righe che combaciano con `startDate`/`endDate`/
  `locationId`.

**Calcoli**: nessun prezzo. **Eventi**: nessuno (nessuna email/notifica). È un blocco operativo, non
una vendita.

---

## 3. Creare / spostare una prenotazione completa dal planner

**Azione**: dal planner si apre la stessa pannellistica della Mappa per creare o spostare una
prenotazione con cliente, pacchetti, servizi, prezzo.

**Endpoint**: `POST /api/sb/booking` (create/update). Vedi [01-mappa](01-mappa.md) §2 e §6 per la
logica completa (validazioni, blacklist, disponibilità magazzino, blocco spostamento di prenotazioni
scontrinate, persistenza importi calcolati dal client, notifica in‑app, line item Fiskaly).

**Trascinamento (drag&drop)**: cambiare postazione/giorno a una prenotazione esistente = update con
`data[0].id`; se cambiano date/settore il prezzo è **ricalcolato lato client**.

---

## 4. Gruppi di prenotazioni

**Azione**: raggruppare più prenotazioni (es. una comitiva, una famiglia su più ombrelloni) sotto un
nome, per spostarle/gestirle insieme.

**Endpoint**: `POST booking-group/add` (`upsertBookingGroup`), `booking-group/list`
(`getGroupsByProperty`), `getGroupDetails`, `deleteGroup`.

**Logiche (`upsertBookingGroup`)**:

- Validazioni: `property_id` obbligatorio; `groupBookings` array non vuoto.
- **Unicità del nome**: se esiste già un gruppo con quel `name` e id diverso → errore "Group name
  already exists".
- **Nessuna prenotazione in due gruppi**: si scorrono **tutti** i gruppi (di tutte le property — la
  query non filtra per `property_id`) e se un id prenotazione è già in un altro gruppo → errore
  "Booking ID X is already part of another group".
- Se `id` presente → update del gruppo; altrimenti → create.

**Calcoli**: nessuno. **Eventi**: nessuno.

**Debito**: il controllo di unicità nome e di appartenenza a un solo gruppo è **globale**, non per
stabilimento (due stabilimenti non possono usare lo stesso nome gruppo). Da correggere con scoping per
`property_id`.

---

## 5. Prezzo cumulativo di gruppo

Quando le prenotazioni sono collegate (gruppo o multi‑postazione), il prezzo **non** è la somma delle
singole: il motore di prezzo lato client ridistribuisce un prezzo cumulativo sul totale dei giorni‑
postazione (vedi [`_motore-di-prezzo-e-sconti.md`](_motore-di-prezzo-e-sconti.md) §3). Il planner è il
punto in cui questa logica è più visibile (spostando una postazione dentro/fuori dal gruppo il totale
si ridistribuisce).

---

## 6. Validazione e allineamento dati

- **`validateBookings`**: verifica di coerenza su un insieme di prenotazioni (usato prima di
  operazioni massive dal planner).
- **`updateBookingNumber`**: riallinea/riassegna il `bookingNumber` a una prenotazione.
- **`databaseChanges`**: endpoint di riconciliazione (log/allineamento) — usato internamente.

**Calcoli**: nessuno. **Eventi**: nessuno (operazioni tecniche).

---

## 7. Riepilogo "calcoli / logiche / eventi" della voce Planner

- **Calcoli**: nessuno proprio del planner; il prezzo (incluso il cumulativo di gruppo) è del motore
  lato client, che il planner mostra e ricalcola allo spostamento.
- **Logiche**: stato cella per giorno; prossima data occupata; espansione intervallo → riga per
  giorno (blocco postazione); unicità nome gruppo; una prenotazione in un solo gruppo; regole di
  spostamento (blocco se scontrinata).
- **Eventi**: quelli ereditati dalla creazione/spostamento prenotazione (`POST /api/sb/booking`) —
  notifica in‑app, line item Fiskaly, aggiornamento consumi magazzino, socket.io. Le azioni proprie
  del planner (blocco postazione, gruppi) **non generano email né notifiche**.
