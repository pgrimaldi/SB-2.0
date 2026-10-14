# stabilimento — Gestionale: pagina «Dettagli e prenotazioni» e pannello Informazioni rapide

> Area funzionale: la pagina di **creazione/modifica prenotazione** del nuovo gestionale
> («Dettagli e prenotazioni», ex schermata di prenotazione struttura) e il **pannello laterale
> «Informazioni rapide»** che si apre dalla mappa cliccando una postazione occupata. Le due viste
> condividono le stesse sezioni e devono restare coerenti.
>
> **Fonte**: `docs/pre/Pagine principali - azioni.docx`, sezioni «PAGINA DI PRENOTAZIONE "DETTAGLI E
> PRENOTAZIONI"», «Pannello informazioni rapide di una prenotazione (su mappa)», «SEZIONE CONVENZIONI
> SULLA PAGINA DI PRENOTAZIONE»; mockup FIGMA *Gestionale Summer Booking 1 e Sito Hub*.
>
> Logica di business (calcoli prezzo, sconti, acconti, assenze, sharing):
> [`../../business-logic/stabilimento/_motore-di-prezzo-e-sconti.md`](../../business-logic/stabilimento/_motore-di-prezzo-e-sconti.md),
> [`../../business-logic/stabilimento/01-mappa.md`](../../business-logic/stabilimento/01-mappa.md),
> [`../../business-logic/stabilimento/03-cassa.md`](../../business-logic/stabilimento/03-cassa.md).
> Firme **ipotizzate** dove l'endpoint non è noto; endpoint attuali fra parentesi. Molte voci del
> menu «Cosa vuoi fare?» dichiarano esplicitamente «stesso flusso dell'implementazione attuale».

---

## 1. Apertura della pagina «Dettagli e prenotazioni»

**Descrizione**: si apre per una nuova prenotazione (da mappa: «Nuova prenotazione», «Prenota subito
› Espandi», Selezione multipla › «Prenota») o per una esistente («Apri prenotazione», «Apri» dallo
Storico, freccia da una lista). In alto: Indietro, checkbox Check-in / Check-out, «Cosa vuoi fare?»,
«Nuova prenotazione». Colonna centrale in box: Informazioni cliente, Postazione/i, Prenotazione
multipla, Aggiungi (servizi), Note. Colonna destra: Riepilogo.
**Schermata**: node `6899-383324` (colonna centrale), `7192-417501` (riepilogo).
**Ruolo**: gestore.

**Input → API — nuova** (`sb/allpricelist`, `sb/getPropertyServices`, `sb/getPaymentOptions`,
`sb/hotel/list`)
- `propertyId` / `bathHouseId` (int)
- `elementIds` (int[]) — postazioni preselezionate (da mappa)
- `checkIn` / `checkOut` (date) — default oggi
- `slot` (string)

**Input → API — esistente** (`sb/getBookingByReservationCode` / `sb/bookingSummaryById`)
- `bookingId` (int) **o** `reservationCode` (string)

**Output ← API — esistente**
- `booking` ({ id, reservationCode, status, checkIn, checkOut, checkedIn, checkedOut,
  customer, guests: [], locations: [ … vedi az. 3 ], services, notes,
  notesLastEdit: { at, operator }, conventionHotelId?, groupId?, customerIcons: [key],
  entrances: [ { date, time } ], lastEntrance?: { date, time } })
- `summary` ({ totalDays, perLocation: [], perServiceBox: [], appliedPrice, finalBalance, deposit,
  remaining, discounts, movimenti: [], receipts: [], paymentStatus, paymentMethod })
- `pricelists` / `services` / `paymentMethods` / `hotels` — dati per i menu

---

## 2. Menu «Cosa vuoi fare?»

**Descrizione**: dropdown in alto con le azioni sulla prenotazione. Voci: Segnala ingresso, Vedi
codice prenotazione, Invia riepilogo prenotazione, Invia pagamento, Dividi conto (solo se ≥ 2
postazioni), Applica codice sconto, Applica sconto, Aggiungi incremento, Stampa, Storico, Rinnova
prenotazione, Elimina prenotazione.
**Schermata**: node `3923-372787`.
**Ruolo**: gestore.

**Input/Output → API** per voce:

| Voce | Endpoint | Input principali | Output |
|---|---|---|---|
| Segnala ingresso | `sb/updateBooking` (« ipotizzata ») | `bookingId`, `entrance: { date: oggi, time: adesso }` | `lastEntrance`, `entrances[]`; sulla mappa il riquadro prende bordo nero |
| Vedi codice prenotazione | nessuna (dato già presente) | — | modale con `reservationCode` |
| Invia riepilogo prenotazione | `sb/sendReservationSummery` | `bookingId`, `channel: "email"\|"whatsapp"`, `to` | `status` |
| Invia pagamento | `sb/update-booking-sendpayment-summary` | `bookingId`, `to`, `amount?` | `status`, `paymentLink` |
| Dividi conto | `sb/booking` / endpoint di split (« ipotizzata ») | `bookingId` | `bookings[]` — una prenotazione per postazione, stesso cliente/date, importo e acconti divisi in parti uguali; se già pagata, residuo €0 |
| Applica codice sconto | `sb/checkdiscount` | `bookingId`, `code` | `discount` ({ code, type, value }), `summary` aggiornato |
| Applica sconto | `sb/updateBooking` (« ipotizzata ») | `bookingId`, `discount: { type: "amount"\|"percent", value }` | `summary` aggiornato |
| Aggiungi incremento | `sb/updateBooking` (« ipotizzata ») | `bookingId`, `surcharge: { type, value, label }` | `summary` aggiornato |
| Stampa | `sb/bookingSummaryById` | `bookingId` | HTML/PDF riepilogo per stampa |
| Storico | `sb/getBookingAreaList` per elemento | `elementId`, `range: "all"` | righe `{ customerName, checkIn, checkOut, elementLabel, total, amountDue }` + «Apri» |
| Rinnova prenotazione | `sb/updateBooking` / duplicazione stagione (« ipotizzata ») | `bookingId`, `confirm: true` | `newBookingId` per la stagione successiva, con postazioni/servizi/extra |
| Elimina prenotazione | `sb/cancelBooking` | `bookingId`, `confirm: true` | `status`; redirect alla mappa + toast |

**Note**: «Applica codice sconto» rimuove il campo «Sconti e voucher» dal riepilogo a destra (il
codice si inserisce solo da qui). I codici sono creati in Impostazioni › «Sconti e voucher».

---

## 3. Box Postazione (per ogni postazione della prenotazione)

**Descrizione**: in alto icona/nome postazione + fila, cestino (se > 1 postazione). Poi: date con
totale giorni; menu mezza giornata/giornata intera (solo se abilitato in impostazioni); checkbox
Stagionale con pulsante «Configura postazioni diverse per periodo stagionale»; sezione Convenzioni
(menu struttura + eventuale flag Voucher); scelta pacchetto + articoli/servizi aggiuntivi (con
«Seleziona solo per determinati giorni»); sezione «Articoli/servizi aggiuntivi assegnabili» (se ce ne
sono di assegnabili da listino); pulsanti: Aggiungi postazione, Aggiungi assenze programmate,
Prenotazione ripetuta, Sposta prenotazione.
**Schermata**: node `6899-383324`, `7039-378595`, `7042-383299`, `7043-385306`/`7043-395764`,
`6914-377154`/`6941-374318`/`6941-375864`.
**Ruolo**: gestore.

**Input → API**
- **Cambio pacchetto / extra / date / slot** (`sb/updateBooking`; a nuova prenotazione: stato client
  fino a Salva): `{ bookingId?, locationId, packageId, extras: [ { id, qty, onlyDates?: [date] } ],
  checkIn, checkOut, slot }` → `pricing` per postazione + `summary` globale ricalcolati.
- **Rimuovi postazione**: `{ bookingId, locationId }` → `summary` aggiornato.
- **Configura postazioni per periodo stagionale**: `{ bookingId?, seasonSegments: [ { from, to,
  elementId } ] }` — i sotto-periodi non si sovrappongono e coprono l'intero periodo stagionale →
  `locations[]` una per sotto-periodo.
- **Sezione Convenzioni**: `sb/hotel/pricelist/details` `{ hotelId, propertyId }` → se l'hotel ha un
  listino dedicato i prezzi di pacchetti/extra vengono sostituiti; se ha anche listino Voucher, con
  flag «Voucher» attivo compare un secondo menu con i prezzi voucher (selezionabile un pacchetto
  voucher, oppure un pacchetto normale con differenza di prezzo applicata).
- **Articoli assegnabili**: `sb/get-booked-location` filtrato per tipo assegnabile e date →
  `assignable` (array `{ elementId, label, available }`); assegnazione salvata in
  `items[].assignedElements: [elementId]`.
- **Aggiungi postazione**: apre popup (date default = quelle della prenotazione; menu postazioni
  disponibili nelle date, o «Scegli sulla mappa»). Verifica: `sb/checkLocationIsAvailable`
  `{ elementId, checkIn, checkOut }` → nuova box postazione.
- **Prenotazione ripetuta**: `{ bookingId?, weekdays: [0..6], periodStart, periodEnd }` → duplica la
  box postazione per ogni ricorrenza nel periodo.
- **Sposta prenotazione**: come [`05-gestionale-mappa.md`](05-gestionale-mappa.md) az. 7
  (`sb/moveBooking`); gli spostamenti compaiono in fondo alla box con cestino per annullarli.
- **Aggiungi assenze programmate**: come [`05-gestionale-mappa.md`](05-gestionale-mappa.md) az. 11.

**Output ← API**: `pricing` per postazione, `summary` globale, `locations[]` aggiornato.

---

## 4. Box Informazioni cliente / Ospiti

**Descrizione**: campi cliente con autocompletamento sui contatti esistenti; matita = modale «Modifica
contatto esistente / Crea nuovo contatto» con salvataggio anche in pagina Contatti; checkbox
«Marketing» (aggiunge il cliente alla lista di default della pagina Marketing); toggle «Utente
generico» / «Giornaliero» (compilano i campi con dati generici, mutuamente esclusivi); «Scheda
cliente» apre l'anagrafica; «Aggiungi ospiti» (autocompletamento, sezione «Ospiti aggiunti» in sola
lettura, modificabile riaprendo la modale dal contatore).
**Schermata**: node `6899-383324`.
**Ruolo**: gestore.

**Input → API**
- Autocompletamento: `sb/customer-list` `{ propertyId, query }` → `{ customerId, fullName, phone,
  email }`
- Modifica/crea contatto: `sb/update` / `sb/create-contact-list` (anagrafica) — `{ customerId?,
  firstName, lastName, phone, email, marketing (bool) }` → `customerId` (applicato alla prenotazione
  e salvato in Contatti)
- Ospiti: salvati in `booking.guests: [ { firstName, lastName, phone, email } ]`

**Output ← API**: `customer`, `guests[]` aggiornati.

---

## 5. Sezioni «Aggiungi» — servizi bar / servizi / servizi orari

**Descrizione**: tre pulsanti categoria (una sola attiva). Card 3 per riga (1 su mobile) con
immagine/placeholder, nome, descrizione (se presente), prezzo (fisso o «Da € X»). Pulsanti «Aggiungi»
e «Paga con tessera»; servizi con varianti aprono la modale varianti; servizi orari aprono la modale
data/fascia/quantità. Le card selezionate hanno bordo sinistro `#7D94BE` 5px. Nel riepilogo: box
separata per bar / servizi / servizi orari con selettore quantità (− fino a 0 = rimozione), nome,
data di utilizzo (solo se prenotazione multi-giorno; sempre per i servizi orari), campo prezzo
modificabile. «Paga con tessera» → box «Riepilogo tessera» con «Sposta nel riepilogo» e footer
(totale + «Paga con tessera»).
**Schermata**: node `7102-379029` (bar), `7105-411118` (servizi), `7106-414812` (orari).
**Ruolo**: gestore.

**Input → API**
- Elenco servizi: `sb/getPropertyServices` / `sb/getPropertyMenu` `{ propertyId, category:
  "bar"|"service"|"hourly" }`
- Aggiunta al riepilogo (nuova prenotazione: stato client): `{ bookingId?, serviceId, qty, variants:
  [id], date?, timeSlot?, target: "summary"|"card" }`
- Verifica servizi orari: `sb/checkHourlyServiceAvailability` `{ serviceId, date, timeSlot, qty }`
- Pagamento con tessera: `sb/cardTransaction` `{ bookingId, items: [ … ], cardId }`

**Output ← API**: `serviceBoxes` (bar/servizi/orari) e `cardSummary` con righe `{ id, name, qty,
date?, timeSlot?, unitPrice, totalPrice }`; `summary.finalBalance` ricalcolato.

---

## 6. Colonna Riepilogo (destra)

**Descrizione**: mostra solo gli elementi presenti, in ordine: Totale giorni (somma dei giorni di
tutte le postazioni, dinamico); box per postazione (nome/fila, date + giorni, convenzione + sconto,
pacchetto + prezzo modificabile, extra + prezzo modificabile); box servizi (bar / servizi / orari);
icone informative cliente (tooltip, multi-selezione, configurabili da amministrazione); sezione
sconti/voucher, prezzo applicato (modificabile), flag «Pagato», metodo di pagamento (modificabili);
campi in sola lettura Saldo finale / Acconto (0,00 € se assente) / Importo residuo; pulsanti
«Aggiungi extra» e «Aggiungi acconto»; box «Movimenti» ({ importo, tipo Extra|Acconto, metodo, note,
data }); box «Scontrini emessi» ({ data, ora, importo }); sezione «Ultimo ingresso» + «Vedi tutti»;
sezione «Allega file» / «Tessera». In fondo: Esci senza salvare, Salva, Salva ed esci, e «Ultima
modifica: data, ora, operatore».
**Schermata**: node `7192-417501`, `6902-402131`/`6902-403455` (ingressi).
**Ruolo**: gestore.

**Input → API**
- Modifica prezzo applicato / flag Pagato / metodo: `sb/updateBooking` `{ bookingId, appliedPrice?,
  paid?, paymentMethod? }`
- Aggiungi extra / acconto: `sb/payment` / `sb/accountBalance` `{ bookingId, type: "extra"|"deposit",
  amount, method, notes }` → riga in «Movimenti», `summary` aggiornato
- Stampa preconto / scontrino: `sb/get-receipt-data` + `sb/electronic-receipt/create` (fiscale) o
  `sb/electronic-receipt/non-fiscal/create` (preconto) `{ bookingId }` → `{ receiptId, date, time,
  amount, pdfUrl }` (riga in «Scontrini emessi»)
- Allega file: upload `{ bookingId, file }` → `{ fileId, url }`
- Tessera: `sb/getCardDetails` `{ customerId }`
- Ingressi «Vedi tutti»: `booking.entrances[]` (già in pagina) o `sb/bookingSummaryById` → popup
  scaricabile con `{ date, time }` per riga

**Output ← API**: `summary` completo (vedi az. 1).

**Input → API — salvataggio finale** (`sb/booking` per nuova, `sb/updateBooking` per esistente)
- l'intero aggregato: `customer`/`customerId`, `guests[]`, `locations[]` (con pacchetto, extra,
  assenze, spostamenti, sotto-periodi stagionali), `services`/`cardItems`, `notes`, `discount`/
  `surcharge`, `appliedPrice`, `paid`, `paymentMethod`, `conventionHotelId`, `customerIcons[]`

**Output ← API**
- `bookingId` / `reservationCode`, `status`, `summary`
- `notesLastEdit` / `bookingLastEdit` ({ at, operator })

---

## 7. Pannello «Informazioni rapide» (dalla mappa)

**Descrizione**: cliccando una postazione occupata resta aperto il menu rapido e si apre da destra il
pannello «Informazioni rapide», con: info postazione (icona, fila/settore, nome, n° giorni, stato
pagamento), «Apri prenotazione», nome cliente + «Scheda cliente», date Dal–Al con matita (modale:
data attuale in sola lettura + nuova data con verifica disponibilità; errore «Impossibile modificare
la data: la postazione risulta già occupata»), pacchetto + articoli aggiuntivi (prefisso «+») con
prezzi, box separate Servizi bar / Servizi / Servizi orari (con data d'acquisto se multi-giorno),
riepilogo economico (Saldo finale con matita, Acconto, Importo residuo = Saldo − Acconto), sezione
«Gruppo prenotazioni» (nome gruppo + n° prenotazioni, solo se in gruppo), riga icone informative,
campo Note con «Ultima modifica: data, ora, operatore», pulsante «Cosa vuoi fare?», pulsante nuovo
«Segnala ingresso» (sotto le date) e sezione «Ultimo ingresso» + «Vedi tutti».
**Schermata**: node `6899-372002`, `6899-372415`, `1222-1724`.
**Ruolo**: gestore.

**Input → API — apertura** (`sb/bookingSummaryById` / `sb/getPropertyBookingNotes`)
- `bookingId` (int)

**Output ← API**: come `summary` + `booking` di az. 1, sufficiente a popolare tutte le sezioni.

**Input → API — modifica data** (`sb/checkLocationIsAvailable` poi `sb/updateBooking`)
- `bookingId`, `elementId`, `field: "checkIn"|"checkOut"`, `newDate`

**Output ← API**
- ok: `{ status, booking: { checkIn, checkOut } }`
- ko: `{ error: "location_unavailable", message }`

**Input → API — Segnala ingresso**: come az. 2 (`entrance: { date: oggi, time: adesso }`); il
pulsante scompare fino al giorno successivo, l'ingresso è visibile qui, in «Dettagli e prenotazioni»
e nella sotto-pagina «Ingressi». Un solo ingresso per giorno per prenotazione.

---

## 8. Selezione della struttura convenzionata (menu Convenzioni)

**Descrizione**: menu a tendina «Convenzioni» di sola selezione (non crea/modifica/elimina
convenzioni). Elenca le strutture create nella pagina Convenzioni. La scelta è salvata nella
prenotazione e mostrata ovunque compaiano i dettagli.
**Schermata**: node `6899-384148`.
**Ruolo**: gestore.

**Input → API — elenco** (`sb/hotel/list`)
- `propertyId` (int) → `hotels` (array `{ id, name, hasDedicatedPricelist, hasVoucherPricelist }`)

**Input → API — applicazione** (`sb/updateBooking` / stato client fino a Salva)
- `bookingId?`, `conventionHotelId` (int), `useVoucher` (bool)

**Output ← API**
- `pricelistOverride` — pacchetti/extra con i prezzi del listino convenzionato/voucher (vedi az. 3)
- `summary` con riga «Convenzione <Hotel> − <sconto> €»

**Note**: se non esistono convenzioni il menu è disabilitato / stato vuoto (da definire).
