# stabilimento — Gestionale: Cassa (nuovo design)

> Area funzionale: la voce **Cassa**. Struttura, componenti e logica restano quelli attuali; cambiano
> il design e alcune aggiunte: pagina «Ordini» a schermo intero (ex modale «Visualizza prenotazioni
> servizi») con colonne «Stato» ed «Evaso» e filtri a tag, card «Movimenti», card «Riepilogo
> scontrini emessi», anteprime di preconto/scontrino (stampa · email · WhatsApp), modale «Gestione
> scontrino elettronico».
>
> **Fonte**: `docs/pre/Pagine principali - azioni.docx`, sezione «CASSA»; mockup FIGMA
> *Gestionale Summer Booking 1 e Sito Hub* (node `7152-389213` cassa, `7152-396614` pagina Ordini,
> `7152-402728` gestione scontrino elettronico).
>
> Logica di business:
> [`../../business-logic/stabilimento/03-cassa.md`](../../business-logic/stabilimento/03-cassa.md).
> Il riepilogo economico è lo stesso della pagina «Dettagli e prenotazioni»
> ([`06-gestionale-dettagli-e-prenotazioni.md`](06-gestionale-dettagli-e-prenotazioni.md) az. 6).
> Firme **ipotizzate** dove l'endpoint non è noto.

---

## 1. Apertura della pagina Cassa

**Descrizione**: tabella e flussi invariati; in alto «Cosa vuoi fare?» e il pulsante «Ordini»
(sostituisce «Visualizza prenotazioni servizi», stessa funzione).
**Schermata**: node `7152-389213`.
**Ruolo**: gestore.

**Input → API** (`sb/getCreditLedgerDetails` / `sb/getBookingAreaList` — contratto attuale)
- `propertyId` / `bathHouseId` (int), `date` (date)

**Output ← API**: invariato rispetto all'implementazione attuale.

---

## 2. Pagina «Ordini» (prenotazioni servizi)

**Descrizione**: si apre a schermo intero (non più modale), con icona Indietro. Tabella come l'attuale
+ 2 colonne nuove: «Stato» (In attesa di pagamento / Pagato parzialmente / Pagato totalmente) ed
«Evaso» (checkbox per riga; se spuntata l'ordine rientra nel filtro «Ordini evasi»). Cestino per riga
(stesso flusso di eliminazione del resto del gestionale).
**Schermata**: node `7152-396614`.
**Ruolo**: gestore.

**Input → API** (`sb/serviceBooking` / `sb/getHourlyServiceBooking` — lista; « firma ipotizzata » per
i nuovi campi)
- `propertyId` / `bathHouseId` (int)
- `dateFilter` (vedi az. 3) / `paymentStatus` (string[]) / `fulfilled` (string[]: `"all"|"done"|
  "notDone"`)
- `search` (string) — nome cliente / nome-numero postazione
- `page` (int) / `pageSize` (int, default 10)

**Output ← API**
- `total` (int)
- `rows` (array) — `{ orderId, bookingId, customerName, elementLabel, items: [ { name, qty } ],
  amount, paymentStatus, fulfilled (bool), createdAt }`

**Input → API — marca evaso** (`sb/updateBooking` / endpoint ordini — « firma ipotizzata »)
- `orderId` (int), `fulfilled` (bool) → riga aggiornata

**Input → API — elimina** (`sb/cancelBooking` / endpoint ordini): `{ orderId, confirm: true }`

---

## 3. Filtri della pagina Ordini (a tag)

**Descrizione**: pulsante dropdown filtri; ogni opzione è una checkbox (multi-selezione) — regola
valida per **tutti i filtri del gestionale**. Filtri: Stato di pagamento (In attesa / Parziale /
Totale), Stato ordine (Tutti / Evasi / Non evasi). I filtri attivi appaiono come tag sopra la tabella
con «×» per rimuoverli singolarmente; la tabella si aggiorna in tempo reale a ogni modifica.
**Schermata**: node `7152-396881`.
**Ruolo**: gestore.

**Input → API**: come az. 2, con `paymentStatus` e `fulfilled` come array (checkbox multiple).

**Output ← API**: come az. 2.

---

## 4. Card «Movimenti» e «Aggiungi extra / acconto»

**Descrizione**: sotto Saldo finale / Acconto / Resta da pagare, nuova card «Movimenti» con acconti
versati ed extra aggiunti; pulsanti «Aggiungi extra» e «Aggiungi acconto» (modali dedicate).
**Schermata**: node `7152-399713` (extra), `7152-390196` (acconto).
**Ruolo**: gestore.

**Input → API** (`sb/payment` / `sb/accountBalance` / `sb/TransactionEntryForPayAtProperty`)
- `bookingId` (int)
- `type` (string) — `"extra" | "deposit"`
- `amount` (number), `method` (string), `notes` (string, opzionale)

**Output ← API**
- `movimenti` (array) — `{ id, amount, type, method, notes, date }`
- `summary` ({ finalBalance, deposit, remaining })

---

## 5. Card «Riepilogo scontrini emessi» e anteprime

**Descrizione**: card sotto «Movimenti» con gli scontrini della prenotazione ({ data, ora, importo }).
Pulsanti «Stampa preconto» e «Stampa scontrino» (in IT «Stampa ricevuta» → «Stampa scontrino»)
aprono una schermata di anteprima da cui: stampare, inviare per email, inviare via WhatsApp.
«Tessere» apre la relativa modale.
**Schermata**: node `7152-390060`, `7152-391227` (anteprima preconto), `7152-392243` (anteprima
scontrino), `7152-399552` (tessere).
**Ruolo**: gestore.

**Input → API** (`sb/get-receipt-data` + `sb/electronic-receipt/non-fiscal/create` per il preconto,
`sb/electronic-receipt/create` per lo scontrino)
- `bookingId` (int), `kind` (string) — `"proforma" | "receipt"`

**Output ← API**
- `document` ({ id, kind, date, time, amount, previewHtml, pdfUrl })

**Input → API — invio** (`sb/sendmessage` / `sb/send-card-detail`)
- `documentId` (int), `channel` (string) — `"print" | "email" | "whatsapp"`, `to` (string)

**Output ← API**: `status` / `message`.

---

## 6. Modale «Gestione scontrino elettronico»

**Descrizione**: dal menu «Cosa vuoi fare?»; elenco di tutti gli scontrini elettronici emessi con:
filtro per data/periodo (menu «Tutte le date»), annullamento singolo o multiplo (checkbox),
paginazione (10/pagina di default), pulsante «Stampa» in alto a destra.
**Schermata**: node `7152-402728`.
**Ruolo**: gestore.

**Input → API — elenco** (`sb/getAllCardTransaction` / `sb/electronic-receipt/get-cash-registers` —
« firma ipotizzata » per il filtro data)
- `propertyId` / `bathHouseId` (int)
- `dateFilter` (oggetto: opzione predefinita o `{ from, to }`)
- `page` (int) / `pageSize` (int, default 10)

**Output ← API**
- `total` (int)
- `rows` (array) — `{ receiptId, number, date, time, amount, status: "issued|voided", bookingId? }`

**Input → API — annulla** (`sb/electronic-receipt/create` con tipo storno / endpoint di
annullamento — « firma ipotizzata »)
- `receiptIds` (int[])

**Output ← API**: `voided` (int[]), `errors` (array `{ receiptId, reason }`).
