# stabilimento — Gestionale: Configurazione mappa (nuovo design)

> Area funzionale: la schermata **Configurazione mappa** (accesso dal pulsante «Configurazione»
> nell'header della Mappa). Rivista nel flusso e nella grafica: reticolo righe × colonne con
> inclinazione in gradi, selezione di righe/colonne/riquadri, menu azioni rapide (svuota / elimina /
> aggiungi), pannello laterale di configurazione (articolo prenotabile o elemento spiaggia),
> movimento e rotazione degli articoli.
>
> **Fonte**: `docs/pre/Pagine principali - azioni.docx`, sezione «CONFIGURAZIONE MAPPA»; mockup FIGMA
> *Gestionale Summer Booking 1 e Sito Hub* (node `7154-484484` accesso, `7154-485608` selezione
> colonna, `7154-489444` pannello, `7154-483393` movimento/rotazione).
>
> Logica di business:
> [`../../business-logic/stabilimento/11-configurazione.md`](../../business-logic/stabilimento/11-configurazione.md).
> Endpoint attuali: `sb/viewconfiguration` (lettura), `sb/viewconfimaps`, `sb/reset-beach-map`;
> salvataggio della struttura mappa **« firma ipotizzata »** (`sb/updateproperty` / endpoint
> dedicato).

---

## 1. Impostazioni del reticolo (Impostazioni › Configura mappa)

**Descrizione**: la pagina **Impostazioni › Configura mappa** definisce i parametri del reticolo
(numero di file, numero massimo articoli per fila, **inclinazione mappa in gradi** — nuova), lo
sfondo mappa + colore area esterna, e le icone per Ombrellone / Articolo personalizzato. È
documentata per intero in
[`13-gestionale-impostazioni.md`](13-gestionale-impostazioni.md) az. 6
(fonte: `docs/pre/Pagine Impostazioni - azioni.docx`). Qui interessa solo che questa schermata
(editor del reticolo) **legge** quei parametri all'apertura.
**Schermata**: node `7234-193009` (pagina Impostazioni).
**Ruolo**: gestore.

**Input → API**: nessuna da questa schermata (vedi [`13`](13-gestionale-impostazioni.md) az. 6 per
lettura `sb/viewconfiguration` e salvataggio `sb/updateproperty`).

---

## 2. Apertura della schermata di configurazione

**Descrizione**: dal pulsante «Configurazione» (header Mappa) si apre la schermata con il reticolo
righe × colonne configurato. Alle estremità di ogni riga/colonna un'area cliccabile seleziona
l'intera riga/colonna.
**Schermata**: node `7154-484484`.
**Ruolo**: gestore.

**Input → API** (`sb/viewconfiguration` + `sb/viewconfimaps`)
- `propertyId` / `bathHouseId` (int)

**Output ← API**
- `grid` ({ columns, rows, tiltDegrees, cellSize })
- `cells` (array) — `{ row, col, content: null | { articleId, type: "article"|"articleArea"|
  "beachElement", label?, sector?, rotation, offsetX, offsetY, maxItems?, conventionOnly (bool) } }`
- `articles` (array) — articoli prenotabili disponibili `{ id, name, icon }`
- `beachElements` (array) — elementi visivi `{ id, name, icon }`

---

## 3. Selezione di una colonna / riga e menu azioni rapide

**Descrizione**: selezionando una colonna (o riga) tutti i suoi riquadri si evidenziano (bordo 2px
`#233C7D`) e si aprono insieme il menu azioni rapide e il pannello laterale di configurazione. Menu:
Svuota · Elimina · Aggiungi (colonna/riga).
**Schermata**: node `7154-485608` (colonna), `7154-487510` (riga).
**Ruolo**: gestore.

**Input → API** (« firma ipotizzata » — mutazioni sulla struttura mappa)
- **Svuota**: `{ propertyId, axis: "column"|"row", index }` → rimuove i contenuti dei riquadri, la
  struttura resta.
- **Elimina**: `{ propertyId, axis, index, confirm }` → rimuove asse + riquadri + articoli; se
  contiene articoli, conferma «Questa colonna comprende degli articoli… Confermi?».
- **Aggiungi**: `{ propertyId, axis, index, position: "left"|"right" (colonna) | "above"|"below"
  (riga) }` → nuovo asse nella posizione scelta.

**Output ← API**: `grid` + `cells` aggiornati; `status` / `message`.

---

## 4. Pannello laterale di configurazione (articolo / elemento spiaggia)

**Descrizione**: due schede — «Seleziona l'articolo» (default): articolo prenotabile (lettino,
ombrellone, ecc.); «Seleziona elemento spiaggia»: elemento solo visivo (passerelle, ecc.), non
prenotabile, senza nome/numero. Per un'intera colonna/riga l'articolo si applica a tutti i riquadri e
la numerazione è generata in successione (es. primo nome «A1» → A1, A2, A3…), coerente con
l'ordinamento (verticale per colonna, orizzontale per riga). Caratteristiche configurabili: Tipo di
contenuto (Articolo / Area di articoli con numero massimo), Nome/numero postazione, Fila/settore,
Ordinamento, checkbox «Riserva per strutture convenzionate» («Gli articoli riservati alle strutture
convenzionate non saranno prenotabili online.»). «Salva» applica.
**Schermata**: node `7154-489444`.
**Ruolo**: gestore.

**Input → API** (« firma ipotizzata »)
- `propertyId` / `bathHouseId` (int)
- `target` ({ axis?: "column"|"row", index?, cells?: [ { row, col } ] })
- `content` ({ kind: "article"|"articleArea"|"beachElement", articleId?, startLabel?, sector?,
  ordering: "vertical"|"horizontal", maxItems?, conventionOnly (bool) })

**Output ← API**
- `cells` aggiornati con la numerazione generata
- `status` / `message`

---

## 5. Selezione di singoli o più riquadri

**Descrizione**: oltre a righe/colonne intere si possono selezionare uno o più riquadri (la selezione
multipla segue comunque la riga/colonna di partenza). Senza una riga/colonna intera selezionata non
compare il menu azioni rapide, solo il pannello di configurazione, da cui inserire uno o più
articoli / aree di articoli / elementi spiaggia sui riquadri selezionati.
**Schermata**: node `7154-489444`.
**Ruolo**: gestore.

**Input → API**: come az. 4 con `target.cells: [ { row, col } ]`.

**Output ← API**: `cells` aggiornati.

---

## 6. Movimento e rotazione di un articolo

**Descrizione**: cliccando un articolo già inserito compaiono i comandi: frecce su/giù (a sinistra),
frecce sinistra/destra (in basso), rotazione oraria/antioraria (in alto), «X» (a destra) per chiudere
i comandi. Tutti i movimenti avvengono **dentro il riquadro**.
**Schermata**: node `7154-483393`.
**Ruolo**: gestore.

**Input → API** (« firma ipotizzata »; può essere batch al salvataggio)
- `propertyId` / `bathHouseId` (int)
- `cell` ({ row, col })
- `transform` ({ offsetX (int), offsetY (int), rotation (gradi) })

**Output ← API**: `cell` aggiornata; `status` / `message`.

---

## 7. Modifica e rimozione di un articolo

**Descrizione**: cliccando un articolo esistente si aprono insieme i comandi di movimento e il
pannello con le sue caratteristiche attuali, modificabili. Per rimuoverlo: si deseleziona la checkbox
dell'articolo nel pannello e si clicca «Salva»; la rimozione è applicata **solo dopo il
salvataggio**.
**Schermata**: node `7154-489444` / `7154-483393`.
**Ruolo**: gestore.

**Input → API** (« firma ipotizzata »)
- update: come az. 4 con `content` aggiornato
- remove: `{ propertyId, cell: { row, col }, content: null }`

**Output ← API**: `cells` aggiornati; `status` / `message`.
