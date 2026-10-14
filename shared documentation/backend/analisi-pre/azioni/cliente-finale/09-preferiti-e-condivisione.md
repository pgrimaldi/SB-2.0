# cliente-finale — Preferiti ("mi piace") e condivisione

> Area funzionale: "mi piace" su uno stabilimento (icona cuore), elenco dei preferiti, condivisione di
> stabilimento/evento/articolo tramite social ed email.
> **Schermate**: p. 7 (icona **cuore** rossa e icona **share** accanto al nome dello stabilimento);
> p. 10 (blocco "Condividi" con Facebook, Instagram, Email, WhatsApp); p. 9 (icona share su ogni card
> evento); pp. 11–14 (icone social nel footer / articoli magazine).
> Endpoint attuali: `sb/myfavourite`, `sb/createSharing`, `sb/getSharingDates`.
>
> Le **recensioni** non compaiono in alcuna schermata dei mockup: vedi nota finale.

---

## 1. Aggiungere / togliere un "mi piace" a uno stabilimento

**Descrizione**: cliccando il cuore sulla scheda stabilimento (o su una card) il cliente aggiunge o
rimuove la struttura dai preferiti.
**Schermata**: p. 7 (cuore accanto a "On the Beach Club").
**Ruolo**: cliente autenticato (l'ospite viene invitato ad accedere).

**Input → API** (`sb/myfavourite` — toggle)
- `userId` (int)
- `propertyId` (int)
- `bathHouseId` (int, opzionale)
- `action` (string) — `"add"|"remove"` (oppure toggle implicito)

**Output ← API**
- `status` / `message`
- `isLike` (bool) — nuovo stato del cuore
- `favouritesCount` (int) — totale preferiti dell'utente

**Note**: lo stato iniziale `isLike` arriva già da `sb/getPropertyWithBathHouseDetails` quando è
passato `userId` (vedi [`03-scheda-stabilimento.md`](03-scheda-stabilimento.md) azione 1).

---

## 2. Elenco degli stabilimenti preferiti

**Descrizione**: dall'area personale il cliente vede la lista delle strutture a cui ha messo "mi
piace", con accesso rapido alla scheda.
**Schermata**: non disegnata → firma ipotizzata.
**Ruolo**: cliente autenticato.

**Input → API** (`sb/myfavourite` — lista)
- `userId` (int)
- `page` (int) / `pageSize` (int)

**Output ← API**
- `total` (int)
- `favourites` (array) — `{ propertyId, slug, name, city, region, thumbnail, priceFrom,
  hasOnlineReservation, likedAt }`

---

## 3. Condivisione di uno stabilimento

**Descrizione**: dall'icona share sulla scheda l'utente condivide il link della struttura (Web Share
API o canale specifico: Facebook, Instagram, Email, WhatsApp).
**Schermata**: p. 7 (icona share accanto al cuore); footer con icone Instagram/Facebook/Email/YouTube.
**Ruolo**: ospite e cliente autenticato.

**Input → API**: nessuna obbligatoria — la UI usa `property.shareUrl` (URL canonico della scheda).
Per tracciare la condivisione (opzionale, `sb/createSharing`):
- `entityType` (string) — `"property"`
- `entityId` (int) — `propertyId`
- `channel` (string) — `"facebook"|"instagram"|"email"|"whatsapp"|"copy"|"native"`
- `userId` (int, opzionale)

**Output ← API**
- `shareUrl` (string) — link (eventualmente con parametri di tracciamento)
- `shareId` (int, se tracciata)

---

## 4. Condivisione di un evento o di un articolo

**Descrizione**: come sopra, dai pulsanti "Condividi" della pagina evento (p. 10) o dell'articolo
magazine.
**Schermata**: p. 10, pp. 11–14.
**Ruolo**: ospite e cliente autenticato.

**Input → API** (`sb/createSharing`, opzionale)
- `entityType` (string) — `"event"|"article"`
- `entityId` (int)
- `channel` (string)

**Output ← API**
- `shareUrl` (string)
- `shareId` (int, se tracciata)

---

## 5. Condivisione di una prenotazione con altri ospiti ("Sharing")

**Descrizione**: il cliente condivide una postazione/prenotazione con altre persone per date
specifiche (funzione "sharing" già presente nel backend).
**Schermata**: non disegnata → firma ipotizzata (endpoint `getSharingDates` / `createSharing` /
`getSharedLocationsByDate`).
**Ruolo**: cliente autenticato.

**Input → API** (`sb/createSharing`)
- `bookingId` (int)
- `elementId` (int) — postazione condivisa
- `dates` (date[]) — giorni ceduti/condivisi
- `sharedWith` ({ email, firstName, lastName }) — destinatario

**Output ← API**
- `status` / `message`
- `sharingId` (int)
- `sharedDates` (date[])

E per consultare le date già condivise (`sb/getSharingDates`): `{ bookingId }` →
`{ sharingList: [ { sharingId, elementId, dates[], sharedWith } ] }`.

---

## Nota — Recensioni

L'area "Recensioni/condivisione" citata in [`business-logic/_indice.md`](../../business-logic/_indice.md)
**non ha alcuna schermata nei mockup FIGMA** (nessun blocco "Lascia una recensione", nessuna stella di
rating nelle card o nella scheda). Per la copertura scelta (solo ciò che è nei mockup) l'azione di
scrittura recensione non è specificata. Se in futuro emergesse, la firma tipica sarebbe:
`POST reviews` con `{ userId, propertyId, bookingId, rating (1–5), title, body }` →
`{ status, reviewId, moderationStatus }`; lettura con `GET reviews?propertyId=` →
`{ average, count, breakdown, items[] }`.
