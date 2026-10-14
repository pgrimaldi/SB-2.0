# stabilimento — Gestionale: Convenzioni (nuova voce)

> Area funzionale: la voce **Convenzioni** (nuova, dopo «Prenotazioni»). Sotto-menu:
> Strutture convenzionate, Prenotazioni con convenzionati, Tutti i report. Dalla riga di un hotel si
> entra in una sezione dedicata con: Prenotazioni, Prenotazioni voucher, Listino, Voucher, Report,
> Info.
>
> **Fonte**: `docs/pre/Pagine principali - azioni.docx`, sezioni «CONVENZIONI», «SEZIONE CONVENZIONI
> SULLA PAGINA DI PRENOTAZIONE», «Regola globale per le tabelle»; mockup FIGMA
> *Gestionale Summer Booking 1 e Sito Hub* (node `4274-410217` sotto-menu, `4273-376585` strutture,
> `4305-306037` sezione hotel).
>
> Logica di business:
> [`../../business-logic/stabilimento/06-convenzioni.md`](../../business-logic/stabilimento/06-convenzioni.md).
> La selezione della convenzione in fase di prenotazione è in
> [`06-gestionale-dettagli-e-prenotazioni.md`](06-gestionale-dettagli-e-prenotazioni.md) az. 8.
> Endpoint `sb/hotel*` già presenti; firme **ipotizzate** dove indicato.

---

## 1. Elenco delle strutture convenzionate

**Descrizione**: tabella hotel/strutture convenzionate. In alto: Cerca per nome, Esporta, Stampa,
Aggiungi hotel. Ogni riga: cestino (conferma) + freccia verso la sezione dell'hotel.
**Schermata**: node `4273-376585`.
**Ruolo**: gestore.

**Input → API** (`sb/hotel/list`)
- `propertyId` (int)
- `search` (string, opzionale)
- `sortBy` / `sortDir` / `page` / `pageSize` (default 10)

**Output ← API**
- `total` (int)
- `rows` (array) — `{ hotelId, name, city, contact, bookingsCount, hasPricelist, hasVoucher }`

---

## 2. Aggiunta / eliminazione di un hotel

**Descrizione**: «Aggiungi hotel» apre un modale coi dati della struttura; «Salva» la aggiunge alla
lista. Cestino sulla riga → modale di conferma.
**Schermata**: node `4273-384240` (aggiungi), `5369-438903` (elimina).
**Ruolo**: gestore.

**Input → API** (`sb/hotel` per creare, `sb/hotel/update` per modificare, `sb/hotel/delete` /
`sb/hotel/delete/bulk`)
- create/update: `{ hotelId?, name, address, city, email, phone, ...campi struttura }`
- delete: `{ hotelId, confirm: true }` **o** `{ hotelIds: [int] }`

**Output ← API**: `status` / `message`, `hotelId`.

---

## 3. Sezione hotel — Prenotazioni e Prenotazioni voucher

**Descrizione**: entrando nella riga di un hotel si apre un sotto-menu con nome hotel + freccia
indietro. «Prenotazioni» elenca tutte le prenotazioni in cui è stata applicata la convenzione con
quell'hotel; «Prenotazioni voucher» quelle con i prezzi voucher di quell'hotel. Pulsante «Filtri»
(modale; ogni filtro con checkbox multi-opzione).
**Schermata**: node `4275-411442` (prenotazioni), `4312-305123` (voucher), `5214-314719` /
`5214-315783` (filtri).
**Ruolo**: gestore.

**Input → API** (`sb/hotel/details` + `sb/getBookingAreaList` filtrato per `conventionHotelId`)
- `hotelId` (int), `propertyId` (int)
- `voucherOnly` (bool)
- `dateFilter` (vedi [`11-gestionale-prenotazioni.md`](11-gestionale-prenotazioni.md) az. 1)
- `filters` (oggetto multi-valore), `search` (string), `page` / `pageSize`

**Output ← API**
- `total` (int)
- `rows` (array) — `{ bookingId, customerName, checkIn, checkOut, elementLabel, total, amountDue,
  voucherApplied (bool) }`

---

## 4. Sezione hotel — Listino dedicato

**Descrizione**: come la pagina Listino principale, ma associata all'hotel. Selezione singola o
checkbox (con «seleziona tutti» separate per pacchetti e per articoli/servizi aggiuntivi); azioni
multiple Duplica / Elimina (in alto a sinistra e per riga, con conferma). Nuova opzione pacchetto
«Prezzo cumulativo» nel menu «Calcolo del prezzo» (calcola il prezzo dei giorni totali su più
postazioni, non la somma dei singoli tratti). Per gli articoli/servizi aggiuntivi: menu «Tutti i
pacchetti» (default) / «Pacchetti specifici».
**Schermata**: node `4605-310577`.
**Ruolo**: gestore.

**Input → API** (`sb/hotel/pricelist` / `sb/hotel/pricelist/list` / `sb/hotel/pricelist/update` /
`sb/hotel/pricelist/delete`; duplicazione `sb/hotel/duplicate`)
- list: `{ hotelId, propertyId }`
- create/update pacchetto: `{ hotelId, packageId?, name, priceRules, priceCalc:
  "perDay"|"fixed"|"cumulative", ... }`
- create/update articolo: `{ hotelId, articleId?, name, price, appliesToPackages: "all" | [packageId] }`
- bulk: `{ hotelId, ids: [int], op: "duplicate"|"delete" }`

**Output ← API**
- `packages` (array) / `articles` (array) con i campi salvati
- `status` / `message`

---

## 5. Sezione hotel — Listino Voucher

**Descrizione**: identica a «Listino» ma con diciture «Pacchetti voucher» / «Articoli/servizi
aggiuntivi voucher». I prezzi voucher vengono applicati in prenotazione quando si seleziona l'hotel e
si attiva il flag Voucher.
**Schermata**: node `5239-322840`.
**Ruolo**: gestore.

**Input → API** (`sb/hotel/pricelist` con `kind: "voucher"` — « firma ipotizzata »)
- come az. 4 con `kind: "voucher"`

**Output ← API**: come az. 4.

---

## 6. Sezione hotel — Report + Tutti i report

**Descrizione**: lista dei report (prenotazioni raggruppate per mese/periodo da inviare all'hotel).
Si può: creare, annullare, inviare (stesso flusso di «Invia riepilogo prenotazione»), aprire per il
dettaglio, cercare per nome, esportare, stampare. «Crea report»: si indica il periodo e se filtrare
per data di inizio o di fine (per le prenotazioni a cavallo di due mesi). Stati report: Attivo /
Annullato. La pagina «Tutti i report» (a livello sezione Convenzioni) fa lo stesso per tutti gli
hotel.
**Schermata**: node `5232-317042` (report hotel), `4338-307695` (tutti i report).
**Ruolo**: gestore.

**Input → API** (« firma ipotizzata » — nessun endpoint report noto; base su `sb/hotel/details` +
`sb/getBookingAreaList` + `sb/sendReservationSummery`)
- crea: `{ hotelId?, periodStart, periodEnd, basis: "startDate"|"endDate" }`
- annulla: `{ reportId }`
- invia: `{ reportId, channel, to }`
- lista: `{ hotelId?, search, page, pageSize }`

**Output ← API**
- `reports` (array) — `{ reportId, hotelName, period, bookingsCount, total, status: "active|void",
  createdAt }`
- dettaglio: `{ report: { … }, bookings: [ … ] }`

---

## 7. Sezione hotel — Info

**Descrizione**: dati della struttura (quelli inseriti alla creazione). Modificabili e salvabili.
**Schermata**: node `5232-318620`.
**Ruolo**: gestore.

**Input → API** (`sb/hotel/details` per leggere, `sb/hotel/update` per salvare)
- `hotelId` (int) / campi struttura

**Output ← API**: `hotel` ({ … }), `status` / `message`.

---

## 8. Prenotazioni con convenzionati (livello sezione)

**Descrizione**: tabella di tutte le prenotazioni dello stabilimento con una convenzione applicata.
In alto: menu «Tutte le date» (opzioni predefinite + calendario con «Conferma»), Filtri, Cerca per
nome, Esporta, Stampa. Tre card: Totale prenotazioni, Totale pagato, Voucher. Nella tabella: elimina
o apri (freccia).
**Schermata**: node `4312-306103`, `4273-387845` (menu date).
**Ruolo**: gestore.

**Input → API** (`sb/getBookingAreaList` con `hasConvention: true`)
- `propertyId` / `bathHouseId` (int)
- `dateFilter`, `filters`, `search`, `page`, `pageSize`

**Output ← API**
- `totals` ({ bookingsAmount, paidAmount, voucherAmount })
- `total` (int)
- `rows` (array) — `{ bookingId, customerName, hotelName, checkIn, checkOut, total, amountDue,
  voucherApplied }`

---

## 9. Selezione della struttura convenzionata in prenotazione

Vedi [`06-gestionale-dettagli-e-prenotazioni.md`](06-gestionale-dettagli-e-prenotazioni.md) az. 8:
il menu «Convenzioni» nella pagina «Dettagli e prenotazioni» è di **sola selezione** (non crea/
modifica/elimina). Fonte dati: `sb/hotel/list`. La convenzione scelta è salvata nella prenotazione e
applica il listino dedicato o voucher dell'hotel.
