# stabilimento — Gestionale: sezione Prenotazioni (9 sotto-pagine)

> Area funzionale: la voce **Prenotazioni** (ex «Crediti»), con 9 sotto-pagine che condividono lo
> stesso layout (differiscono per filtri e dati): 1) Prenotazioni · 2) Transazioni · 3) Richieste di
> pagamento · 4) Prenotazioni online · 5) Prenotazioni online servizi · 6) Sharing creati ·
> 7) Ingressi (nuova) · 8) Scadenziario · 9) Gruppi.
>
> **Fonte**: `docs/pre/Pagine principali - azioni.docx`, sezione «PRENOTAZIONI» e sotto-pagine;
> mockup FIGMA *Gestionale Summer Booking 1 e Sito Hub* (node `7154-*`).
>
> Logica di business:
> [`../../business-logic/stabilimento/05-prenotazioni.md`](../../business-logic/stabilimento/05-prenotazioni.md).
> Le liste «Prenotazioni online» e «Prenotazioni online servizi» sono **spostate qui dalla pagina
> Statistiche** senza modifiche di contratto dati. Firme **ipotizzate** dove indicato.

---

## 0. Elementi comuni a tutte le sotto-pagine

**Filtro date** (node `7154-439194`): pulsante «Tutte le date» → menu con, in alto, opzioni
predefinite (Tutte le date · Oggi · Questa settimana · Prossimi sette giorni · Prossimo mese ·
Prossimi 6 mesi) applicate immediatamente; sotto, un calendario per una o più date confermate con
«Conferma». Le due modalità convivono; la scelta resta finché non se ne fa un'altra.

**Filtri** (pulsante «Filtri» → modale): ogni opzione è una checkbox (multi-selezione). I filtri
attivi compaiono come tag sotto la barra, ciascuno con «×»; presente «Azzera filtri». La tabella si
aggiorna in tempo reale. **Vale per tutti i filtri del gestionale.**

**Ricerca**: campo «Cerca per nome» → nome/cognome cliente **o** nome/numero postazione.

**Azioni pagina**: «Esporta» e «Stampa» in alto a destra.

**Tabella**: checkbox per riga a sinistra (l'header seleziona solo le righe della pagina corrente);
selettore risultati per pagina (default 10; opzioni 20/50/100/200/300) e paginazione, sia sopra sia
sotto la tabella; frecce di ordinamento su ogni colonna (alfabetico / numerico, ascendente e
discendente), con allineamento delle altre colonne alla colonna ordinata. Icone a destra di ogni
riga: cestino (elimina) e freccia (apri → `Dettagli e prenotazioni`). L'eliminazione singola segue
esattamente lo stesso flusso/validazioni/conferme dell'eliminazione multipla.

**Input → API — forma comune** (endpoint per sotto-pagina, vedi sotto)
- `propertyId` / `bathHouseId` (int)
- `dateFilter` (oggetto: `{ preset }` **o** `{ dates: [date] }`)
- `filters` (oggetto campo → array di valori)
- `search` (string)
- `sortBy` (string) / `sortDir` (`"asc"|"desc"`)
- `page` (int) / `pageSize` (int, default 10)

**Output ← API — forma comune**
- `total` (int) e, dove previsto, `totals` (aggregati di fondo elenco)
- `rows` (array) — colonne specifiche per sotto-pagina

---

## 1. Sotto-pagina «Prenotazioni»

**Endpoint**: `sb/getBookingAreaList` + `sb/getBookingAreaListCount` (node `7154-428508`).
**Righe**: `{ bookingId, reservationCode, customerName, elementLabels: [string], checkIn, checkOut,
slot, paymentStatus, amountPaid, amountDue, channel, createdAt }`.

**Azioni multiple** (compaiono a sinistra sopra la tabella con ≥ 1 selezione):
- **Duplica** = rinnova per la stagione successiva. `POST` (« firma ipotizzata ») `{ bookingIds }` →
  conferma; messaggi: «Rinnovo della prenotazione/e effettuato con successo» / errore «Impossibile
  effettuare il rinnovo della prenotazione/i».
- **Elimina** (`sb/cancelBooking` `{ bookingIds, confirm }`) — la conferma cambia in base ai
  pagamenti: Caso 1 (almeno una non pagata / parziale) → finestra specifica; Caso 2 (tutte pagate) →
  finestra diversa; entrambe si adattano a selezione singola/multipla.

---

## 2. Sotto-pagina «Transazioni»

**Endpoint**: `sb/getCreditLedgerDetails` / `sb/getAllCardTransaction` (node `7154-428842`).
**Righe**: come l'attuale pagina Movimenti; in fondo elenco i **totali**.
**Azioni multiple**: solo **Elimina** (`{ transactionIds, confirm }`). Freccia riga → apre la
prenotazione collegata in `Dettagli e prenotazioni`.

---

## 3. Sotto-pagina «Richieste di pagamento»

**Endpoint**: `sb/getOnlineReservationStatusByAdmin` / lista richieste pagamento (node `7154-429916`).
**Righe**: `{ requestId, bookingId, customerName, elementLabel, amount, status, sentAt }`; totali in
fondo.
**Azioni multiple**: solo **Elimina** (`{ requestIds, confirm }`). Freccia → `Dettagli e
prenotazioni`.

---

## 4. Sotto-pagina «Prenotazioni online»

**Endpoint**: `sb/viewonlinereservation` (node `7154-449056`) — **spostata da Statistiche**, contratto
invariato. Totale in fondo.
**Azioni multiple**: **Duplica** (rinnovo, stessi messaggi della sotto-pagina 1) ed **Elimina**
(Caso 1 / Caso 2 come sotto-pagina 1). Prima delle due icone: pulsante **«Richiedi pagamento»**
(`sb/addonlinereservation` / flusso attuale invariato). Freccia → `Dettagli e prenotazioni`.

---

## 5. Sotto-pagina «Prenotazioni online servizi»

**Endpoint**: `sb/viewonlinereservation` (servizi) / `sb/serviceReservationDeatailsByBookingNumber`
(node `7154-453931`) — **spostata da Statistiche**, contratto invariato. Totale in fondo.
**Azioni multiple**: **Duplica** ed **Elimina** (come sotto-pagina 4); pulsante **«Richiedi
pagamento»** prima delle icone. Freccia → `Dettagli e prenotazioni`.

---

## 6. Sotto-pagina «Sharing creati»

**Endpoint**: `sb/get-sharing_created` / `sb/getSharingDates` (node `7154-432570`). Totale in fondo.
**Righe**: `{ sharingId, customerName, elementLabel, dates: [date], status: "pending|completed" }`.
Solo le righe con stato **In attesa** hanno il cestino (eliminazione diretta) e sfondo `#F1F5FB`.
Freccia → dettaglio dello sharing (lo sharing «In attesa» è eliminabile anche dal dettaglio).

---

## 7. Sotto-pagina «Ingressi» (nuova)

**Endpoint**: `sb/getBookingAreaList` con proiezione ingressi (node `7154-448355`) — stessa struttura
di tabella della sotto-pagina «Prenotazioni», **+ colonna «Data e ora ingresso»** (ultimo ingresso
registrato). Filtri = quelli della sotto-pagina «Prenotazioni».
**Azioni multiple**: **Duplica** ed **Elimina** come sotto-pagina 1. Freccia → `Dettagli e
prenotazioni`.

**Registrazione ingresso**: avviene da «Segnala ingresso» (menu «Cosa vuoi fare?» in `Dettagli e
prenotazioni`, e pannello «Informazioni rapide» sulla mappa) — vedi
[`06-gestionale-dettagli-e-prenotazioni.md`](06-gestionale-dettagli-e-prenotazioni.md) az. 2 e 7.
Regole: un solo ingresso per prenotazione per giorno; non altera Check-in/Check-out; sulla mappa il
riquadro prende bordo nero; lo storico è consultabile da pannello rapido, pagina prenotazione e
questa sotto-pagina.

---

## 8. Sotto-pagina «Scadenziario»

**Endpoint**: elenco pagamenti in scadenza (node `7154-430286`) — « firma ipotizzata ».
**Controlli extra**: pulsante **«Aggiungi»** → modale nuovo pagamento (node `7154-444940`); a campi
obbligatori completi la riga viene creata e l'elenco si aggiorna in tempo reale.
**Righe**: `{ paymentId, customerName, elementLabel, amount, dueDate, paymentStatus:
"paid|unpaid" }`.
**Azioni multiple**: solo **Elimina**. Freccia → **pagina dettaglio pagamento** (node `7154-430604`):
ogni campo con matita → editabile + «Salva» per singolo campo; «Stato pagamento» diventa menu
(Pagato / Non pagato); «Data di scadenza pagamento» apre un calendario applicato su «Conferma». Le
modifiche si riflettono subito in dettaglio ed elenco.

**Input → API — crea/modifica** (« firma ipotizzata »)
- crea: `{ propertyId, customerId?, bookingId?, amount, dueDate, paymentStatus, notes? }`
- update campo: `{ paymentId, field, value }`

**Output ← API**: `{ payment: { … } }`, elenco aggiornato.

---

## 9. Sotto-pagina «Gruppi»

**Endpoint**: `sb/booking-group/list` (node `7154-430706`); dettaglio `sb/booking-group/details/`.
**Righe**: `{ groupId, name, bookingsCount, customers: [string], createdAt }`.
**Azioni**: matita a destra della riga → modale «Modifica nome gruppo»; azione multipla **Elimina**;
freccia → **pagina Dettaglio gruppo** (node `7154-431027`).

**Pagina Dettaglio gruppo** — pulsante «Cosa vuoi fare?» con:
- **Modifica nome** → stessa modale della matita in elenco.
- **Effettua pagamento** (node `7154-448251`) → modale «Pagamento unico» / «Pagamento separato».
  - *Pagamento unico* (`7154-447756`): scelta del cliente pagante fra quelli del gruppo + metodo di
    pagamento (opzionale, stesso menu di `Dettagli e prenotazioni`) → «Continua» porta alla pagina di
    pagamento.
  - *Pagamento separato* (`7154-448011`): scelta della prenotazione da pagare + cliente pagante +
    metodo (opzionale) → «Continua» alla pagina di pagamento.

**Input → API**
- rinomina: `sb/booking-group/add` / update `{ groupId, name }`
- elimina: `{ groupIds, confirm }`
- pagamento: `sb/payment` / `sb/capturePayment` `{ groupId, mode: "single"|"separate",
  payerCustomerId, bookingId?, paymentMethod? }`

**Output ← API**: `{ group: { … } }` / `{ status, paymentUrl }`.

---

## 10. Apertura di una riga (comune)

La freccia a destra di ogni riga apre la **pagina `Dettagli e prenotazioni`** della prenotazione
collegata (per Transazioni e Richieste di pagamento: la prenotazione a monte). Vedi
[`06-gestionale-dettagli-e-prenotazioni.md`](06-gestionale-dettagli-e-prenotazioni.md) az. 1.
