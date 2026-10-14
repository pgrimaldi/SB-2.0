# stabilimento — Gestionale: Mappa (nuovo design)

> Area funzionale: la voce **Mappa** del nuovo gestionale Summer Booking Hub — la schermata più
> dettagliata dei mockup. Comprende: le due modalità di visualizzazione (predefinita «solo icone» e
> «Vedi info»), il comportamento dello zoom, i menu azioni rapide su postazione libera/occupata, il
> pannello «Informazioni rapide», gli strumenti in basso a sinistra (Selezione multipla, Sposta
> prenotazione, Crea gruppo), quelli in basso a destra (Filtri, Settori, Arrivi e partenze) e la
> **Ricerca smart**.
>
> **Fonte**: `docs/pre/Pagine principali - azioni.docx`, sezione «MAPPA»; mockup FIGMA
> *Gestionale Summer Booking 1 e Sito Hub* (node id citati per singola azione).
>
> Logica di business:
> [`../../business-logic/stabilimento/01-mappa.md`](../../business-logic/stabilimento/01-mappa.md) e
> [`../../business-logic/stabilimento/_motore-di-prezzo-e-sconti.md`](../../business-logic/stabilimento/_motore-di-prezzo-e-sconti.md).
> Il pannello di creazione/apertura prenotazione è in
> [`06-gestionale-dettagli-e-prenotazioni.md`](06-gestionale-dettagli-e-prenotazioni.md).
> Firme **ipotizzate** dove l'endpoint non è noto; endpoint attuali indicati fra parentesi.

---

## 1. Apertura della mappa e caricamento dello stato postazioni

**Descrizione**: il gestore apre la Mappa per la data/slot dell'header. La modalità predefinita è
«Vedi info»: postazioni occupate come riquadri informativi, postazioni libere con icona standard più
un'etichetta con la data della prossima prenotazione. Con «Vedi info» disattivato si vedono solo le
icone e il nome postazione su riquadro colorato per stato pagamento.
**Schermata**: node `7192-410989` (Vedi info), `7192-410988` (solo icone).
**Ruolo**: gestore.

**Input → API** (`sb/viewconfimaps` + `sb/get-booked-location` + `sb/getnext-bookingdates-forlocations`)
- `propertyId` (int) / `bathHouseId` (int)
- `date` (date) — dall'header
- `slot` (string) — `"full" | "morning" | "afternoon"`
- `seasonal` (bool)

**Output ← API**
- `map` ({ width, height, backgroundImage, gridTilt (gradi), scale })
- `sectors` (array) — `{ id, name, row, priceListId }`
- `elements` (array) — `{ id, type, sectorId, label, x, y, rotation, assignable (bool) }`
- `locationStatus` (array) — `{ elementId, status: "free|reserved|occupied", bookingId?, customerName?,
  checkIn?, checkOut?, slotFlags: { morning, afternoon }, paymentStatus: "paid|unpaid|partial",
  bookingType: "daily|seasonal|online", conventionHotelName?, setupPackageCodes: [string],
  setupExtraCodes: [string], statusIcons: [key], entranceMarked (bool), nextBookingDate? }`
- `legend` (array) — `{ key, color, label }` (bordo superiore verde/rosso/giallo = pagata/non
  pagata/parziale; bordo laterale azzurro = giornaliera; arancio = online; "M"/"P" = mattina/
  pomeriggio; punto rosso = check-in non fatto; bordo nero = ingresso segnalato)

**Note**: lo stato è per la sola `date` corrente; il calendario/planner cambia `date` (vedi
[`03-planner-strumenti-mappa.md`](03-planner-strumenti-mappa.md) azione 2).

---

## 2. Cambio e persistenza della modalità di visualizzazione «Vedi info»

**Descrizione**: il gestore attiva/disattiva «Vedi info» (icona a occhio, in alto a sinistra). La
preferenza è **persistente** tra pagine e tra sessioni finché non viene cambiata manualmente.
**Schermata**: node `7192-410989` / `7192-410988`.
**Ruolo**: gestore.

**Input → API** (`sb/updatePersonalDetails`; « firma ipotizzata » per la preferenza di vista mappa)
- `userId` (int) / `propertyId` (int)
- `mapViewMode` (string) — `"seeInfo" | "iconsOnly"`

**Output ← API**
- `status` / `message`
- `mapViewMode` (string) — valore salvato, riletto all'apertura della mappa (azione 1)

**Note**: la commutazione non ricarica lo stato postazioni (già in `locationStatus`); è una
trasformazione di rendering. Lo zoom sotto una soglia mostra nei riquadri solo `setupPackageCodes` +
`setupExtraCodes` e nasconde il resto (comportamento client, nessuna chiamata).

---

## 3. Menu azioni rapide su postazione libera

**Descrizione**: cliccando una postazione libera si apre un dropdown con: Nuova prenotazione, Prenota
subito, Riserva (o «Libera» se già riservata), Storico prenotazioni, Miniplanner.
**Schermata**: node `1203-1690`.
**Ruolo**: gestore.

**Input → API**: nessuna per l'apertura del menu. Le voci avviano:
- **Nuova prenotazione** → apre `Dettagli e prenotazioni` precompilata con la postazione
  (vedi [`06-gestionale-dettagli-e-prenotazioni.md`](06-gestionale-dettagli-e-prenotazioni.md) az. 1).
- **Prenota subito** → azione 4 di questo documento.
- **Riserva / Libera** → azione 5.
- **Storico** → azione 9.
- **Miniplanner** → azione 10.

**Output ← API**: n/a.

**Note**: se la postazione è riservata, scegliendo «Nuova prenotazione» o «Prenota subito» lo stato di
riserva viene rimosso **senza conferma** prima di avviare il flusso.

---

## 4. Prenota subito (pannello laterale rapido)

**Descrizione**: apre un pannello da destra con i campi essenziali per creare subito una prenotazione
sulla postazione selezionata. Toggle «Utente generico» attivo di default (nome cliente precompilato),
check-in già spuntato, date preimpostate a oggi, fascia oraria «Giornata intera», pacchetto = quello
predefinito della postazione (o il primo disponibile), flag «Già pagato» attivo, metodo di pagamento.
Pulsanti: Stampa preconto, Stampa fiscale, Avanzate, Annulla, Salva. «Avanzate»/«Espandi» apre la
pagina `Dettagli e prenotazioni` con i dati già trasferiti.
**Schermata**: node `7190-393990` (base), `7190-394878` (avanzato).
**Ruolo**: gestore.

**Input → API — dati di supporto all'apertura** (`sb/allpricelist` filtrato per elemento;
`sb/getPaymentOptions`)
- `propertyId` / `bathHouseId` (int), `elementId` (int), `date` (date)
- → `location` ({ elementId, label, sector, icon }), `packages` (array `{ id, name, price,
  isDefaultForLocation }`), `extras` (array), `paymentMethods` (array), `genericCustomer`
  ({ firstName, lastName, phone, email })

**Input → API — salvataggio** (`sb/booking` con `channel: "desk"`)
- `items` (array) — `[ { elementId, sectorId, checkIn, checkOut, seasonal, slot, packageId,
  extras: [ { id, qty } ], addedLocations: [elementId] } ]`
- `customer` ({ firstName, lastName, phone, email? }) **o** `genericCustomer: true`
- `checkedIn` (bool) — default `true`
- `paid` (bool) — flag «Già pagato», default `true`
- `paymentMethod` (string)
- `appliedPrice` (number, opzionale) — prezzo personalizzato che sovrascrive lo standard
- `conventionHotelId` (int, opzionale — solo pannello avanzato)
- `notes` (string, opzionale — solo pannello avanzato)
- `customerIcons` (array di key, opzionale — solo pannello avanzato)

**Output ← API**
- `bookingId` (int) / `reservationCode` (string)
- `pricing` ({ subtotal, extrasPrice, total, currency }) — aggiornato dinamicamente lato server a
  ogni modifica di date/pacchetti/postazioni
- `receiptUrl` (string, se emesso preconto/fiscale)

---

## 5. Riserva / libera una postazione

**Descrizione**: «Riserva» marca la postazione come riservata (icona gialla, pallino rosso); il menu
contestuale successivo mostra «Libera» al posto di «Riserva».
**Schermata**: node `7190-395852` (riserva), `7190-397444` (menu su riservata).
**Ruolo**: gestore.

**Input → API** (`sb/hold-booking-locations` per riservare; `sb/delete-hold-booking-locations` per
liberare)
- `propertyId` / `bathHouseId` (int)
- `elementIds` (int[])
- `date` (date) / `seasonal` (bool) / `slot` (string)

**Output ← API**
- `status` / `message`
- `holds` (array) — `{ elementId, status: "reserved", expiresAt? }`

---

## 6. Selezione multipla di postazioni

**Descrizione**: attivato lo strumento, tutte le postazioni diventano selezionabili; un popup in alto
mostra il conteggio; con ≥ 1 selezione compare «Prenota» che apre `Dettagli e prenotazioni` con tutte
le postazioni nella stessa prenotazione e stesse date. La «×» a sinistra deseleziona tutto e chiude.
**Schermata**: node `1295-8341`.
**Ruolo**: gestore.

**Input → API — verifica disponibilità in blocco** (`sb/checkLocationIsAvailable`)
- `propertyId` / `bathHouseId` (int), `elementIds` (int[]), `date` / `checkIn` / `checkOut`,
  `seasonal` (bool), `slot` (string)

**Output ← API**
- `items` (array) — `{ elementId, available (bool), pricing, nearestAlternativeElementId? }`
- `unavailable` (int[]) — postazioni non disponibili nel periodo; per ciascuna il server propone
  l'alternativa più vicina

**Note**: la creazione della prenotazione unica avviene poi in `Dettagli e prenotazioni` /
`sb/booking` con `items[]` su più `elementId`.

---

## 7. Sposta prenotazione (dallo strumento mappa)

**Descrizione**: attivato «Sposta prenotazione», si seleziona una postazione occupata («Seleziona una
prenotazione da spostare»), poi la destinazione («Clicca dove desideri spostarla»). Se la
destinazione è libera compare «Quando vuoi spostare la prenotazione?» con: Intera prenotazione /
Giorno corrente / Giorno successivo / Personalizzato (calendario limitato alle date della
prenotazione). Se occupata: popup di conflitto («Vuoi scambiare le prenotazioni?»).
**Schermata**: node `1307-2751`, `1352-5090`, `1352-6475`, `1355-7884`.
**Ruolo**: gestore.

**Input → API** (`sb/moveBooking`)
- `bookingId` (int)
- `fromElementId` (int) / `toElementId` (int)
- `scope` (string) — `"whole" | "today" | "tomorrow" | "custom"`
- `dates` (date[], obbligatorio se `scope = "custom"`)

**Output ← API**
- `status` / `message` — es. «Prenotazione spostata con successo» (toast)
- `available` (bool) — destinazione libera per tutte le date richieste
- `conflict` ({ targetBookingId, canSwap (bool) }) — presente se la destinazione è occupata
- `booking` ({ bookingId, elementIds, checkIn, checkOut })
- `priceDelta` (number) — differenza se i settori hanno listini diversi

---

## 8. Crea gruppo di prenotazioni

**Descrizione**: attivato «Crea gruppo», si selezionano più prenotazioni esistenti (anche di clienti
diversi), si assegna un nome e si conferma. Ogni prenotazione può appartenere a un solo gruppo.
**Schermata**: node `1411-10718`, `1411-12112`.
**Ruolo**: gestore.

**Input → API** (`sb/booking-group/add`)
- `propertyId` / `bathHouseId` (int)
- `name` (string)
- `bookingIds` (int[])

**Output ← API**
- `status` / `message` — es. «Gruppo creato con successo» (toast)
- `groupId` (int)
- `bookings` (int[]) — prenotazioni effettivamente aggregate
- `conflicts` (array) — `{ bookingId, reason }` (es. già in un altro gruppo)

**Note**: elenco e dettaglio gruppi in
[`11-gestionale-prenotazioni.md`](11-gestionale-prenotazioni.md) (sotto-pagina «Gruppi»).

---

## 9. Storico prenotazioni di una postazione

**Descrizione**: da «Storico» (menu rapido su postazione libera o occupata) si apre un popup con tutte
le prenotazioni passate/correnti/future della postazione. Per riga: nome cliente, check-in/check-out,
nome/numero postazione, totale, importo da pagare, pulsante «Apri» (→ `Dettagli e prenotazioni`).
**Schermata**: node `1376-9004`.
**Ruolo**: gestore.

**Input → API** (`sb/getBookingAreaList` filtrato per elemento; « firma ipotizzata » sul filtro
temporale esteso)
- `propertyId` / `bathHouseId` (int)
- `elementId` (int)
- `range` (string) — `"all"` (passate + correnti + future)
- `page` (int) / `pageSize` (int)

**Output ← API**
- `total` (int)
- `rows` (array) — `{ bookingId, customerName, checkIn, checkOut, elementLabel, total, amountDue }`

---

## 10. Miniplanner di una postazione

**Descrizione**: da «Miniplanner» si apre un pannello orizzontale in basso con un calendario per tutti
i mesi della stagione configurata. Le celle con prenotazioni sono colorate per stato pagamento
(rosso/verde/giallo; celle divise a metà se mattina + pomeriggio). Cliccando una cella attiva si apre
a sinistra un riepilogo con: nome completo, Da, A, Mattina/Pomeriggio, Pacchetto, Importo totale,
«Apri».
**Schermata**: node `1396-45734`.
**Ruolo**: gestore.

**Input → API** (`sb/getSeasonalBookings` per elemento; « firma ipotizzata » per l'aggregato per
giorno)
- `propertyId` / `bathHouseId` (int)
- `elementId` (int)
- `seasonStart` (date) / `seasonEnd` (date)

**Output ← API**
- `days` (array) — `{ date, bookings: [ { bookingId, slot: "full|morning|afternoon",
  paymentStatus } ] }`
- `bookingDetails` (map bookingId → { customerName, checkIn, checkOut, slot, packageName, total })

---

## 11. Aggiungi assenze programmate (da postazione occupata)

**Descrizione**: da «Aggiungi assenze programmate» (menu rapido su postazione occupata) si apre un
popup che consente di selezionare, entro il periodo della prenotazione (date evidenziate `#E8F0FF`),
uno o più giorni in cui il cliente non sarà presente; per quei giorni la postazione risulta
disponibile. Mostra anche la sezione «Sharing» con le relative date (se presenti), ciascuna con
cestino. Pulsanti: Annulla, Conferma e ricalcola prezzo, Conferma e non ricalcolare prezzo.
**Schermata**: node `1370-9869`.
**Ruolo**: gestore.

**Input → API — apertura** (`sb/get-sharing-and-absence-dates`)
- `bookingId` (int) → `{ bookingDates: [date], absences: [date], sharing: [ { date, id } ] }`

**Input → API — conferma** (`sb/getAbsenceLocationByDate` per la verifica; salvataggio via
`sb/updateBooking` — « firma ipotizzata »)
- `bookingId` (int)
- `absenceDates` (date[])
- `recalculatePrice` (bool)

**Output ← API**
- `status` / `message` — toast «Assenze programmate aggiunte correttamente» (≈5 s, fade out)
- `absences` (date[]) — elenco aggiornato
- `pricing` ({ total, currency }) — presente solo se `recalculatePrice = true`

---

## 12. Filtri, Settori, Arrivi e partenze (pulsanti in basso a destra)

**Descrizione**: tre pulsanti circolari. «Filtri» e «Settori» aprono un dropdown; l'opzione scelta
diventa un'etichetta con «×». Con un filtro/settore attivo la mappa tiene opache al 100% le
postazioni che soddisfano il criterio e porta le altre al 35%. «Arrivi e partenze» apre un pannello
laterale con prenotazioni giornaliere di oggi, arrivi di oggi, partenze di oggi (nome cliente,
postazione, settore/fila, stato pagamento) e un pulsante di download PDF.
**Schermata**: node `1437-24088`/`1549-124373` (Filtri), `1437-24090`/`1549-129798` (Settori),
`1437-32020`/`1549-132511` (Arrivi e partenze).
**Ruolo**: gestore.

**Filtri disponibili** (sempre gli stessi): Prenotazioni stagionali, Prenotazioni giornaliere,
Prenotazioni con convenzione hotel, Prenotazioni con voucher, Prenotazioni in arrivo, Prenotazioni in
partenza. **Settori**: quelli configurati in Impostazioni > Settori e file.

**Input → API — evidenziazione** (client, su `locationStatus` già caricato; niente chiamata).

**Input → API — Arrivi e partenze** (`sb/getBookingAreaList` con `status`/`range` opportuni;
`sb/printBeachMap` o endpoint dedicato per il PDF)
- `propertyId` / `bathHouseId` (int), `date` (oggi)

**Output ← API**
- `dailyToday` / `arrivalsToday` / `departuresToday` (array) — `{ customerName, elementLabel, sector,
  row, paymentStatus: "unpaid|pending|paid" }`
- `pdfUrl` (string)

---

## 13. Ricerca smart (disponibilità futura multi-postazione)

**Descrizione**: dal pulsante «Ricerca Smart» (in alto a destra) si sceglie un giorno o un intervallo
e si preme «Cerca». La mappa passa in modalità dedicata: postazioni occupate opache al 40%,
disponibili con riquadro bianco che indica «Sempre» o «Fino al [data]». Il gestore seleziona più
postazioni (pannello fisso in basso, un riquadro per postazione con periodo assegnato e «×») finché
l'intero periodo è coperto; solo allora «Prenota» e «Riserva» si attivano.
**Schermata**: node `7062-390363`.
**Ruolo**: gestore.

**Input → API — ricerca** (`sb/checkLocationIsAvailable` esteso; « firma ipotizzata »)
- `propertyId` / `bathHouseId` (int)
- `startDate` (date) / `endDate` (date)
- `seasonal` (bool)

**Output ← API**
- `results` (array) — `{ elementId, label, sector, availableFrom (date), availableTo (date),
  fullPeriod (bool) }`

**Input → API — creazione** («Prenota» → `sb/booking`; «Riserva» → `sb/hold-booking-locations`)
- `segments` (array) — `[ { elementId, checkIn, checkOut } ]` (una riga per postazione/periodo)
- `customerId` (int) **o** `customer` ({ … }) — per «Prenota»

**Output ← API**
- «Prenota»: `bookingId` / `reservationCode`, `pricing`, `segments` confermati (una prenotazione
  unica composta da più postazioni con periodi differenti)
- «Riserva»: `holds` (array) — una riserva per postazione, ciascuna per il proprio periodo
