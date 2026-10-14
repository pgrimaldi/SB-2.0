# cliente-finale — Prenotazione servizi, Listino e Menu

> Area funzionale: box "Prenotazione servizi" della scheda stabilimento — "Clicca qui e prenota i
> servizi dello stabilimento balneare oppure prenota al bar comodamente dal tuo ombrellone" — con i
> pulsanti **Listino**, **Menu**, **Eventi** e **"Prenota ora"**.
> **Schermata**: p. 7 (box "Prenotazione servizi"). Il dettaglio di listino/menu/carrello servizi
> **non è disegnato**: le firme sotto sono **ipotizzate** dai campi minimi e dagli endpoint oggi usati
> (`sb/allpricelist`, `sb/getHourlyServiceBooking`, `sb/confirmSeriviceBookingByBookingNumber`,
> `sb/serviceReservationDeatailsByBookingNumber`).
>
> "Eventi" è trattato in [`06-eventi.md`](06-eventi.md).

---

## 1. Apertura del Listino servizi dello stabilimento

**Descrizione**: da "Listino" l'utente consulta i servizi acquistabili (lettini, cabine, gazebo,
teli, ombrelloni extra, pacchetti) con i relativi prezzi.
**Schermata**: p. 7 (pulsante "Listino").
**Ruolo**: ospite.

**Input → API** (`sb/allpricelist`)
- `propertyId` (int)
- `bathHouseId` (int)
- `date` (date, opzionale) — per prezzi che variano per periodo
- `languageCode` (int)

**Output ← API**
- `priceLists` (array) — `{ id, name, validFrom, validTo }`
- `services` (array) — `{ id, name, description, category, unit ("day"|"hour"|"piece"), price,
  currency, image, bookable (bool), maxQtyPerBooking, requiresLocation (bool) }`

---

## 2. Apertura del Menu del bar/ristorante

**Descrizione**: da "Menu" l'utente sfoglia il menù consultabile (e ordinabile "dal proprio
ombrellone").
**Schermata**: p. 7 (pulsante "Menu").
**Ruolo**: ospite / cliente autenticato con prenotazione attiva.

**Input → API** (`sb/getPropertyMenu` / campo di `allpricelist` categoria "food")
- `propertyId` (int)
- `bathHouseId` (int)
- `languageCode` (int)

**Output ← API**
- `menuCategories` (array) — `{ key, label, sortOrder }`
- `menuItems` (array) — `{ id, categoryKey, name, description, price, currency, image, allergens[],
  available (bool) }`
- `orderingEnabled` (bool) — se l'ordine al tavolo/ombrellone è attivo
- `orderingHours` ({ from, to }) — fascia oraria in cui si può ordinare

---

## 3. Verifica disponibilità di un servizio a fascia oraria

**Descrizione**: per servizi prenotabili a slot (es. campo da beach tennis, gazebo a ore) la UI mostra
gli orari liberi nel giorno scelto.
**Schermata**: non disegnata → firma ipotizzata (endpoint `getHourlyServiceBooking` /
`checkHourlyServiceAvailability`).
**Ruolo**: ospite / cliente autenticato.

**Input → API** (`sb/checkHourlyServiceAvailability`)
- `propertyId` (int) / `bathHouseId` (int)
- `serviceId` (int)
- `date` (date)

**Output ← API**
- `slots` (array) — `{ startTime, endTime, available (bool), price }`

---

## 4. Prenotazione di uno o più servizi ("Prenota ora")

**Descrizione**: l'utente seleziona quantità/slot dei servizi e li aggiunge come prenotazione servizi
(collegata o meno a una prenotazione postazione).
**Schermata**: p. 7 (pulsante "Prenota ora" nel box servizi); carrello non disegnato → firma ipotizzata.
**Ruolo**: ospite o cliente autenticato.

**Input → API** (`sb/serviceBooking` / `sb/getHourlyServiceBooking`)
- `propertyId` (int) / `bathHouseId` (int)
- `linkedBookingId` (int, opzionale) — prenotazione postazione a cui agganciare i servizi
- `items` (array) — `[ { serviceId, qty, date, slot?: { startTime, endTime }, locationElementId? } ]`
- `contact` ({ firstName, lastName, email, phonePrefix, phone }) — se ospite
- `userId` (int) — se loggato
- `notes` (string, opzionale)

**Output ← API**
- `serviceBookingId` (int) / `bookingNumber` (string)
- `status` (string) — `"pending_payment"|"confirmed"` (confermata subito se pagamento in struttura)
- `pricing` ({ subtotal, commissionAmount, total, currency })
- `paymentRequired` (bool)
- `nextStep` (string) — `"payment"` o `"done"`

---

## 5. Ordine al bar dall'ombrellone

**Descrizione**: il cliente con prenotazione attiva ordina prodotti dal menù indicando la propria
postazione; l'ordine arriva alla cassa dello stabilimento.
**Schermata**: descritto nel testo del box ("prenota al bar comodamente dal tuo ombrellone"); UI di
ordine non disegnata → firma ipotizzata.
**Ruolo**: cliente autenticato con prenotazione in corso.

**Input → API** (`sb/placeBarOrder`)
- `bookingId` (int) — prenotazione/postazione del cliente
- `locationElementId` (int) — ombrellone di consegna
- `items` (array) — `[ { menuItemId, qty, notes } ]`
- `paymentMode` (string) — `"atProperty"|"online"`

**Output ← API**
- `orderId` (int) / `orderNumber` (string)
- `status` (string) — `"received"|"preparing"|"delivered"`
- `estimatedMinutes` (int)
- `pricing` ({ subtotal, total, currency })

---

## 6. Consultazione di una prenotazione servizi tramite numero

**Descrizione**: l'utente verifica il dettaglio e lo stato di una prenotazione servizi.
**Schermata**: non disegnata → firma ipotizzata (endpoint `serviceReservationDeatailsByBookingNumber`).
**Ruolo**: ospite o cliente autenticato.

**Input → API** (`sb/serviceReservationDeatailsByBookingNumber`)
- `bookingNumber` (string)
- `email` (string) — verifica incrociata

**Output ← API**
- `serviceBooking` ({ bookingNumber, status, propertyName, items[], pricing, date, paymentStatus })

---

## 7. Conferma di una prenotazione servizi tramite numero

**Descrizione**: dopo il pagamento (o da link email) la prenotazione servizi passa a "confermata".
**Schermata**: non disegnata → firma ipotizzata (endpoint `confirmSeriviceBookingByBookingNumber`).
**Ruolo**: sistema/ospite (da link).

**Input → API** (`sb/confirmSeriviceBookingByBookingNumber`)
- `bookingNumber` (string)
- `paymentIntentId` (string, opzionale) — riferimento pagamento

**Output ← API**
- `status` (string) — `"confirmed"`
- `receiptUrl` (string) — se emesso documento
