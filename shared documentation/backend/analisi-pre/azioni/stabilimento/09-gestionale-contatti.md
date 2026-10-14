# stabilimento — Gestionale: Contatti (nuovo design)

> Area funzionale: la voce **Contatti**. Funzionalità e struttura invariate; cambiano il design e:
> selettore risultati per pagina (10/20/25/50/100/200/300, default 10), paginazione doppia (alto e
> basso a destra), nuovo filtro checkbox «solo Black list», riga con cestino + freccia di dettaglio,
> sezioni condizionali nel modale contatto (Carte associate; Blacklist e Prenotazioni associate solo
> se il contatto ha prenotazioni).
>
> **Fonte**: `docs/pre/Pagine principali - azioni.docx`, sezione «CONTATTI»; mockup FIGMA
> *Gestionale Summer Booking 1 e Sito Hub* (node `3114-333329` lista, `3122-346959` aggiungi
> contatto).
>
> Logica di business:
> [`../../business-logic/stabilimento/04-contatti.md`](../../business-logic/stabilimento/04-contatti.md).
> Firme **ipotizzate** dove l'endpoint non è noto.

---

## 1. Apertura della lista contatti

**Descrizione**: lista dei contatti registrati con paginazione (alto/basso a destra), selettore
risultati per pagina, filtro «solo Black list», pulsanti «Aggiungi contatto» ed «Esporta» in alto a
destra. Ogni riga: informazioni attuali + cestino + freccia di dettaglio. Frecce di ordinamento su
ogni colonna ordinabile (regola globale tabelle).
**Schermata**: node `3114-333329`.
**Ruolo**: gestore.

**Input → API** (`sb/customer-list`)
- `propertyId` / `bathHouseId` (int)
- `search` (string, opzionale) — nome/cognome/telefono/email
- `blacklistOnly` (bool)
- `sortBy` (string) / `sortDir` (`"asc"|"desc"`)
- `page` (int) / `pageSize` (int, default 10)

**Output ← API**
- `total` (int)
- `rows` (array) — `{ customerId, firstName, lastName, phone, email, blacklisted (bool),
  bookingsCount, cardsCount, createdAt }`

---

## 2. Aggiunta / modifica di un contatto

**Descrizione**: «Aggiungi contatto» apre un modale (stesso flusso attuale, solo restyling) con gli
stessi campi e sezioni finali. Sezioni condizionali: «Carte associate» con «Apri» (→ popup dettaglio
carta) se ci sono carte; «Blacklist» e «Prenotazioni associate» solo se il contatto ha prenotazioni.
**Schermata**: node `3122-346959`.
**Ruolo**: gestore.

**Input → API** (`sb/create-contact-list` / `sb/update` / `sb/update-contact-list`)
- `customerId` (int, assente = nuovo)
- `firstName`, `lastName`, `phone`, `email`, ...campi anagrafici attuali
- `blacklisted` (bool)
- `marketing` (bool)

**Output ← API**
- `status` / `message`
- `customerId` (int)
- `cards` (array) — `{ cardId, number, balance }` (per il pulsante «Apri»)
- `bookings` (array) — `{ bookingId, checkIn, checkOut, elementLabel }` (solo se presenti)

---

## 3. Eliminazione di un contatto

**Descrizione**: cestino sulla riga → modale di conferma (stesso flusso di eliminazione del
gestionale).
**Schermata**: node `3114-333329`.
**Ruolo**: gestore.

**Input → API** (`sb/delete-contact-list/` / `sb/deleteuser` — « firma ipotizzata »)
- `customerId` (int), `confirm` (bool)

**Output ← API**: `status` / `message`.

---

## 4. Apertura del dettaglio contatto

**Descrizione**: la freccia a destra della riga apre la pagina di dettaglio del contatto.
**Schermata**: node `3114-333329`.
**Ruolo**: gestore.

**Input → API** (`sb/getpersonalDetails` / `sb/customer/cards` / `sb/mybookingsByUserId`)
- `customerId` (int)

**Output ← API**
- `customer` (anagrafica completa)
- `cards` (array) / `bookings` (array) / `blacklisted` (bool)

---

## 5. Esportazione della lista contatti

**Descrizione**: pulsante «Esporta» in alto a destra.
**Schermata**: node `3114-333329`.
**Ruolo**: gestore.

**Input → API** (`sb/createCsvOwnerDetails` / endpoint export — « firma ipotizzata »)
- `propertyId` / `bathHouseId` (int)
- filtri correnti (`search`, `blacklistOnly`)
- `format` (string) — `"csv" | "xlsx"`

**Output ← API**: `{ fileUrl }`.
