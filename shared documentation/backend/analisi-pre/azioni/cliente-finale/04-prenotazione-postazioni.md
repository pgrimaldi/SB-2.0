# cliente-finale — Prenotazione postazioni (mappa spiaggia)

> Area funzionale: scelta di ombrelloni/postazioni sulla mappa interattiva, selezione date
> (giornaliero/stagionale), calcolo prezzo con commissione, aggiunta al carrello e avvio checkout.
> **Schermate**: p. 7 (sezione "Disponibilità" + "Scegli il tuo posto in spiaggia"); mappa spiaggia
> pp. 29, 35 (`immagine Mappa interattiva 1.png`, `Mappa postazioni libere.2*.svg`). La legenda colori
> (verde = libero, rosso = occupato, "M"/"P" = mattina/pomeriggio, giallo/viola/arancio = stati) è
> visibile sui marker.
>
> Logica di business correlata: [`business-logic/systemadmin/01-gestione-stabilimenti.md`](../../business-logic/systemadmin/01-gestione-stabilimenti.md)
> §4.1 (calcolo commissione: `prezzo-base × % ÷ 100 + 0,50 €`; `iframeCommission` se dentro widget
> iframe, `onlineCommission` altrimenti; 5 `propertyId` esenti) e [`db/online_reservations.md`](../../db/online_reservations.md).

---

## 1. Caricamento della mappa spiaggia (configurazione + postazioni)

**Descrizione**: aprendo "Disponibilità"/"Prenota ora" la UI carica la piantina della spiaggia con
settori, file, ombrelloni numerati e servizi (bar, ingresso, passerelle).
**Schermata**: p. 7, pp. 29/35.
**Ruolo**: ospite.

**Input → API** (`sb/viewconfimaps` + `sb/viewconfiguration`)
- `propertyId` (int)
- `bathHouseId` (int)
- `languageCode` (int)

**Output ← API**
- `map` ({ width, height, backgroundImage, scale }) — canvas della piantina
- `sectors` (array) — `{ id, name, shape, priceListId }`
- `elements` (array) — `{ id, type: "umbrella"|"gazebo"|"table"|"cabin"|"bar"|"entrance"|"walkway",
  sectorId, label (numero), x, y, rotation, capacity }`
- `legend` (array) — `{ key: "free"|"booked"|"morning"|"afternoon"|"partial"|"unavailable", color,
  label }`
- `bookingModes` (string[]) — `["daily","seasonal","hourly"]` disponibili per questo stabilimento

---

## 2. Impostazione date e modalità (Giornaliero / Stagionale)

**Descrizione**: l'utente sceglie l'intervallo di date, spunta **Stagionale** o lascia **Giornaliero**,
ed eventualmente la fascia (mattina/pomeriggio/intera giornata); "Modifica ricerca" riapre il selettore.
**Schermata**: p. 7 (barra "21/06/2025 – 21/06/2025 · ☐ Stagionale · Giornaliero ▾ · Modifica ricerca").
**Ruolo**: ospite.

**Input → API**: nessuna dedicata; i valori (`checkIn`, `checkOut`, `seasonal`, `slot`) sono parametri
delle azioni 3–4. Per il calendario dei periodi stagionali disponibili:
`sb/viewseasonsperiods` con `{ propertyId }` → `{ periods: [ { id, label, startDate, endDate, priceListId } ] }`.

**Output ← API**: n/a.

---

## 3. Recupero dello stato di occupazione delle postazioni per le date scelte

**Descrizione**: applicate le date, la mappa colora ogni postazione (libera / occupata / solo mattina /
solo pomeriggio / non disponibile).
**Schermata**: mappa colorata p. 7, pp. 29/35.
**Ruolo**: ospite.

**Input → API** (`sb/get-booked-location` + `sb/getSharedLocationsByDate` + `sb/getAbsenceLocationByDate`)
- `propertyId` (int)
- `bathHouseId` (int)
- `checkIn` (date) / `checkOut` (date)
- `seasonal` (bool)
- `slot` (string, opzionale) — `"morning"|"afternoon"|"full"`

**Output ← API**
- `locationStatus` (array) — `{ elementId, status: "free"|"booked"|"morning"|"afternoon"|"unavailable",
  bookableSlots: string[] }`
- `sharedLocations` (array) — postazioni prenotabili in condivisione: `{ elementId, freeSlots[] }`
- `absences` (array) — `{ elementId, dates[] }` giorni in cui la postazione è fuori servizio

---

## 4. Verifica disponibilità e prezzo di una postazione selezionata

**Descrizione**: cliccando su un ombrellone libero, la UI conferma che è ancora disponibile per le date
e mostra il prezzo (base + eventuali accessori: sdraio/lettini).
**Schermata**: p. 7 (popup della postazione, "On the Beach Club · Prenota ora · A partire da 20,00").
**Ruolo**: ospite.

**Input → API** (`sb/checkLocationIsAvailable` + `sb/getPaymentOptions`)
- `propertyId` (int) / `bathHouseId` (int)
- `elementId` (int) — postazione scelta
- `checkIn` (date) / `checkOut` (date) / `seasonal` (bool) / `slot` (string)
- `extras` (array, opzionale) — `[ { key: "sunbed"|"deckchair"|"cot", qty } ]`
- `isIframe` (bool) — `true` se la prenotazione avviene dentro un widget iframe su sito terzo (cambia la
  commissione applicata)

**Output ← API**
- `available` (bool) / `reason` (string) — se non più disponibile
- `pricing` ({ basePrice, nights, extrasPrice, subtotal, commissionPercent, commissionAmount,
  total, currency }) — `commissionPercent` = `iframeCommission` se `isIframe`, altrimenti
  `onlineCommission`; `commissionAmount = subtotal × commissionPercent ÷ 100 + 0,50` (o su metà se
  acconto, 0 se pagamento in struttura)
- `paymentOptions` (array) — `["full","deposit","atProperty"]` ammesse per questo stabilimento
- `depositPercent` (number) — quota acconto se `deposit`
- `holdExpiresAt` (datetime) — scadenza del "fermo" della postazione

**Note**: 5 `propertyId` sono **esenti da commissione** (lista oggi hardcoded lato sito cliente): il
server deve restituire `commissionPercent = 0` per essi.

---

## 5. Selezione multipla di postazioni

**Descrizione**: l'utente aggiunge più ombrelloni alla stessa prenotazione (gruppo/famiglia).
**Schermata**: implicito dalla mappa; strumento "Selezione multipla" visto lato gestionale pp. 31–34.
**Ruolo**: ospite.

**Input → API** (`sb/checkLocationIsAvailable` in batch)
- `propertyId` / `bathHouseId` (int)
- `elementIds` (int[]) — tutte le postazioni scelte
- `checkIn` / `checkOut` / `seasonal` / `slot`
- `extras` per elemento (array)

**Output ← API**
- `items` (array) — per ciascun `elementId`: `{ available, pricing }`
- `cartTotals` ({ subtotal, commissionAmount, total, currency })
- `unavailable` (int[]) — postazioni non più libere da rimuovere dalla selezione

---

## 6. Aggiunta al carrello / creazione della prenotazione in bozza

**Descrizione**: confermata la selezione, l'utente crea la prenotazione (stato "in attesa di
pagamento") che tiene bloccate le postazioni fino alla scadenza del fermo.
**Schermata**: transizione da "Scegli il tuo posto" al checkout (schermata di riepilogo non disegnata →
firma ipotizzata).
**Ruolo**: ospite (con richiesta di login/registrazione prima del pagamento) o cliente autenticato.

**Input → API** (`sb/booking`)
- `propertyId` (int) / `bathHouseId` (int)
- `items` (array) — `[ { elementId, sectorId, checkIn, checkOut, seasonal, slot, extras[] } ]`
- `bookingMode` (string) — `"daily"|"seasonal"|"hourly"`
- `contact` ({ firstName, lastName, email, phonePrefix, phone }) — se ospite non loggato
- `userId` (int) — se loggato
- `isIframe` (bool)
- `notes` (string, opzionale)
- `couponCode` (string, opzionale) — vedi azione 7

**Output ← API**
- `bookingId` (int) / `reservationCode` (string)
- `status` (string) — `"pending_payment"`
- `pricing` (come azione 4, totali di carrello)
- `holdExpiresAt` (datetime)
- `paymentRequired` (bool) / `allowedPaymentOptions` (string[])
- `nextStep` (string) — `"payment"` → [`07-pagamento.md`](07-pagamento.md)

---

## 7. Applicazione di un codice sconto / coupon

**Descrizione**: in fase di riepilogo l'utente inserisce un codice promozionale e il totale si aggiorna.
**Schermata**: non disegnata → firma ipotizzata (endpoint `checkdiscount` / `checkuserBlackListed`).
**Ruolo**: ospite o cliente autenticato.

**Input → API** (`sb/checkdiscount`)
- `bookingId` (int) **o** `{ propertyId, items[], subtotal }`
- `couponCode` (string)
- `userId` / `email` (per limiti d'uso per utente)

**Output ← API**
- `valid` (bool) / `message` (string)
- `discountType` (string) — `"percent"|"fixed"`
- `discountValue` (number)
- `newPricing` ({ subtotal, discountAmount, commissionAmount, total, currency })

---

## 8. Verifica delle prossime date disponibili per una postazione

**Descrizione**: se la postazione scelta è occupata nelle date richieste, la UI propone il primo
periodo libero.
**Schermata**: non disegnata → firma ipotizzata (endpoint `getnext-bookingdates-forlocations`).
**Ruolo**: ospite.

**Input → API** (`sb/getnext-bookingdates-forlocations`)
- `propertyId` / `bathHouseId` (int)
- `elementIds` (int[])
- `fromDate` (date)
- `lengthNights` (int)

**Output ← API**
- `suggestions` (array) — `{ elementId, nextCheckIn, nextCheckOut }`

---

## 9. Recupero di una prenotazione tramite codice

**Descrizione**: l'utente (anche ospite) recupera lo stato di una prenotazione inserendo il codice
ricevuto via email.
**Schermata**: non disegnata → firma ipotizzata (endpoint `getBookingByReservationCode`).
**Ruolo**: ospite o cliente autenticato.

**Input → API** (`sb/getBookingByReservationCode`)
- `reservationCode` (string)
- `email` (string) — verifica incrociata

**Output ← API**
- `booking` ({ reservationCode, status, propertyName, items[], pricing, checkIn, checkOut,
  paymentStatus, cancellationPolicy })
