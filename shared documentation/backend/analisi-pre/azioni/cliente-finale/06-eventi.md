# cliente-finale — Eventi

> Area funzionale: elenco eventi dello stabilimento (calendario mensile + card evento con data e
> orario), pagina di dettaglio evento con testo, "Altri eventi" e pulsanti di condivisione social.
> **Schermate**: p. 9 / `Pagina lista eventi.png` (lista con calendario "Agosto 2025", card "Aperitivo
> al tramonto", "Musica dal vivo", paginazione 1 2 3); p. 10 / `Pagina dettaglio evento.png` (dettaglio
> con "Condividi" → Facebook, Instagram, Email, WhatsApp; sezione "Altri eventi").
>
> Endpoint attuale: `sb/event`. La condivisione riusa il flusso di
> [`09-preferiti-e-condivisione.md`](09-preferiti-e-condivisione.md).

---

## 1. Apertura della lista eventi di uno stabilimento

**Descrizione**: da "Eventi" (box "Prenotazione servizi" della scheda) l'utente vede il calendario del
mese e l'elenco degli eventi in programma, con paginazione.
**Schermata**: p. 9.
**Ruolo**: ospite.

**Input → API** (`sb/event` — lista)
- `propertyId` (int)
- `bathHouseId` (int, opzionale)
- `month` (string `YYYY-MM`) — mese mostrato nel calendario (default: mese corrente)
- `fromDate` / `toDate` (date, opzionali) — filtro range
- `page` (int) / `pageSize` (int)
- `languageCode` (int)

**Output ← API**
- `total` (int) — per la paginazione
- `events` (array) — card:
  `{ id, slug, title, coverImage, address: { street, city, region }, startDate, endDate,
     startTime, endTime, priceFrom?, isBookable (bool), shareUrl }`
- `calendarDays` (array) — `{ date, hasEvents (bool), eventCount }` per evidenziare i giorni con eventi
  (nel mockup 20–25 agosto risultano marcati)

---

## 2. Navigazione del calendario (mese precedente / successivo)

**Descrizione**: con le frecce ‹ › l'utente cambia mese; il calendario e la lista si aggiornano ai
giorni con eventi del nuovo mese.
**Schermata**: p. 9 (calendario "‹ Agosto 2025 ›").
**Ruolo**: ospite.

**Input → API**: come azione 1 con `month` aggiornato.

**Output ← API**: come azione 1.

---

## 3. Selezione di un giorno nel calendario

**Descrizione**: cliccando un giorno l'elenco si filtra sugli eventi di quella data.
**Schermata**: p. 9 (giorno "21" evidenziato).
**Ruolo**: ospite.

**Input → API**: come azione 1 con `fromDate = toDate = <giorno scelto>`.

**Output ← API**: come azione 1.

---

## 4. Apertura del dettaglio di un evento

**Descrizione**: da "Scopri di più" su una card l'utente apre la pagina dell'evento con descrizione
estesa, data/orario, luogo e link di condivisione.
**Schermata**: p. 10.
**Ruolo**: ospite.

**Input → API** (`sb/event` — dettaglio)
- `eventId` (int) **o** `slug` (string)
- `languageCode` (int)

**Output ← API**
- `event` ({ id, slug, title, body (HTML), coverImage, gallery[], address, lat, lng,
  startDate, endDate, startTime, endTime, priceFrom, isBookable, bookingUrl, shareUrl,
  propertyId, propertyName })
- `relatedEvents` (array) — "Altri eventi": stessa forma delle card dell'azione 1

---

## 5. Condivisione di un evento

**Descrizione**: dai pulsanti "Condividi" (Facebook, Instagram, Email, WhatsApp) e dall'icona di share
sulla card, l'utente condivide l'evento.
**Schermata**: p. 10 (blocco "Condividi" con 4 icone); p. 9 (icona share su ogni card).
**Ruolo**: ospite.

**Input → API**: nessuna per la condivisione diretta (usa `event.shareUrl` + Web Share API / intent
del canale). Per tracciare la condivisione (opzionale): `sb/createSharing` con
`{ entityType: "event", entityId, channel }` → `{ status, shareId }`.

**Output ← API**: n/a (o `shareId` se tracciata).

---

## 6. Prenotazione/partecipazione a un evento

**Descrizione**: se l'evento è prenotabile (`isBookable = true`), da "Prenota" l'utente riserva uno o
più posti.
**Schermata**: implicita ("Prenota ora" nella scheda; card evento con orari) → dettaglio prenotazione
non disegnato → firma ipotizzata.
**Ruolo**: ospite o cliente autenticato.

**Input → API** (`sb/serviceBooking` con `type: "event"` / endpoint dedicato `sb/eventBooking`)
- `eventId` (int)
- `qty` (int) — numero di posti/biglietti
- `date` (date) — se l'evento si ripete su più giorni
- `contact` ({ firstName, lastName, email, phonePrefix, phone }) — se ospite
- `userId` (int) — se loggato
- `notes` (string, opzionale)

**Output ← API**
- `eventBookingId` (int) / `bookingNumber` (string)
- `status` (string) — `"pending_payment"|"confirmed"`
- `pricing` ({ unitPrice, qty, subtotal, commissionAmount, total, currency })
- `paymentRequired` (bool)
- `nextStep` (string) — `"payment"` → [`07-pagamento.md`](07-pagamento.md)
