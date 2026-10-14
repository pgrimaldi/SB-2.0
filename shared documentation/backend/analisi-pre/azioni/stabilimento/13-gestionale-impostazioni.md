# stabilimento — Gestionale: sezione Impostazioni (nuovo design)

> Area funzionale: la voce **Impostazioni** del nuovo gestionale, implementata come **menu secondario**
> agganciato alla sidebar (desktop: si espande insieme al menu principale; mobile: menu principale a
> sole icone e sotto-menu Impostazioni sempre aperto). Pagine: Informazioni stabilimento · Gestione
> utenti (+ «Tutti i log») · Stagioni e periodi · Settori e file · Configura mappa · Magazzino ·
> Listino · Fiscalizzazione (ex «Impostazioni stampante») · Database (nuova).
>
> **Fonte**: `docs/pre/Pagine Impostazioni - azioni.docx` — **documento ancora in redazione («file da
> completare»)**: le firme e i campi qui riportati vanno **riverificati** quando il docx sarà
> completo. Board FIGMA *Summer Booking e Summer Booking Hub — mockups* (node `72xx-*`/`73xx-*`) e
> *Gestionale Summer Booking 1 e Sito Hub* (Fiscalizzazione).
>
> Logica di business:
> [`../../business-logic/stabilimento/12-impostazioni.md`](../../business-logic/stabilimento/12-impostazioni.md),
> [`../../business-logic/stabilimento/08-magazzino.md`](../../business-logic/stabilimento/08-magazzino.md),
> [`../../business-logic/stabilimento/_motore-di-prezzo-e-sconti.md`](../../business-logic/stabilimento/_motore-di-prezzo-e-sconti.md).
> Per la maggior parte delle pagine il docx dichiara «struttura e funzionalità invariate, solo
> restyling»: le firme sotto ricalcano quindi l'implementazione attuale. Firme **ipotizzate** dove
> l'endpoint non è noto; endpoint attuali fra parentesi.

---

## 1. Informazioni stabilimento — apertura e salvataggio

**Descrizione**: pagina in box: 1) Informazioni generali; 2) Immagini (Immagine principale +
Galleria immagini); 3) Tour virtuale (URL); 4) Listino e Menu (upload PDF, uno per voce, «Nessun file
selezionato» / nome file); 5) Posizione sulla mappa (Google Maps interattiva: pan, zoom, fullscreen,
click = indirizzo); 6) Servizi dello stabilimento (categorie Accessibilità e comfort · Sport e
attività · Ristorazione e extra · Spiaggia; checkbox per servizio, 3 colonne). Pulsante «Salva
impostazioni».
**Schermata**: node `7200-206817`.
**Ruolo**: gestore.

**Input → API — lettura** (`sb/getPropertyWithBathHouseDetails` + `sb/getPropertyPhotos` +
`sb/getVirtualTour` + `sb/getPropertyServices`)
- `propertyId` / `bathHouseId` (int)

**Output ← API**
- `general` (campi anagrafici attuali)
- `images` ({ main: { url }, gallery: [ { id, url, order } ] })
- `virtualTour` ({ url })
- `documents` ({ pricelistPdf?: { name, url }, menuPdf?: { name, url } })
- `location` ({ lat, lng, address })
- `serviceCategories` (array) — `{ key, title, services: [ { id, name, enabled } ] }`

**Input → API — salvataggio** (`sb/updateproperty`)
- `general` / `virtualTourUrl` / `location: { lat, lng, address }`
- `services` (array) — `[ { id, enabled } ]`
- immagini e PDF: upload separati → `{ mainImage (file), galleryImages ([file]), pricelistPdf
  (file), menuPdf (file) }`

**Output ← API**: `status` / `message`; risorse aggiornate.

**Note**: immagine principale e galleria alimentano la pagina vetrina pubblica dello stabilimento.

---

## 2. Gestione utenti — creazione, lista, modifica, eliminazione

**Descrizione**: box «Aggiungi nuovo utente» (Email = username, checkbox «email come contatto»,
Password, Cellulare + checkbox «cellulare come contatto», menu «Tipo di account»); «Lista utenti
attivi» (riga: email, ruolo sotto l'email, pulsante «Log», icone Elimina e Modifica); «Modifica
utente» = modale precompilato con «Salva modifiche» / «X» annulla.
**Schermata**: node `7217-198780`, `7394-224752` (modifica).
**Ruolo**: gestore (titolare / ruolo con accesso a Impostazioni).

**Nuovo ruolo**: al menu «Tipo di account» si aggiunge **«Operatore di cassa giornaliero»** — vede
solo le operazioni di cassa del **giorno corrente**, non quelle di altre date.

> **Precisato dal committente il 2026-10-07** — ruolo `dailycashoperator`: come l'operatore di cassa,
> ma una prenotazione finita prima di oggi è invisibile, mentre una iniziata nel passato e ancora in corso
> resta visibile; nel calendario non si possono scegliere date passate. Vedi
> [`identity_access_roles`](../../../analisi-post/db/identity/identity_access_roles.md).

**Input → API**
- lista: `sb/customer-list` / endpoint utenti gestionale (« firma ipotizzata ») `{ propertyId }` →
  `rows: [ { userId, email, role, emailIsContact, phone, phoneIsContact } ]`
- crea: `sb/usersignup` / `auth/usersignup` `{ propertyId, email, password, phone, role,
  emailIsContact (bool), phoneIsContact (bool) }` → `{ userId }`
- modifica: `sb/updatesuperadmin` / `sb/updatePersonalDetails` `{ userId, email?, phone?, role?,
  emailIsContact?, phoneIsContact?, password? }`
- elimina: `sb/deleteuser` `{ userId, confirm: true }`

**Output ← API**: `status` / `message`; riga aggiornata.

---

## 3. Pagina «Tutti i log» e «Log» del singolo utente

**Descrizione**: nuova pagina (da «Vedi tutti i log» nella lista utenti). Tabella delle azioni degli
utenti, con icona per tipologia di azione. In alto: «Indietro», filtro «Tutte le date», «Filtri»,
«Ricerca», «Esporta», «Stampa». Filtri: Tipologia di azione, Utente. Riga: ID utente, ID
prenotazione, nome e cognome operatore, postazione, azione, data e ora, freccia di navigazione (porta
alla pagina/prenotazione oggetto del log). Il pulsante «Log» sulla riga di un utente apre la **stessa
pagina** con il filtro Utente già impostato su quell'utente.
**Schermata**: node `7329-202722` (pagina log), `7327-194468` (filtri), `7411-207495` (lista azioni),
`7329-202722`.
**Ruolo**: gestore.

> **Deciso il 2026-10-07**: i dati della pagina vengono dal nuovo modulo **Audit**
> ([`docs/86-modulo-audit.md`](../../../progettazione/86-modulo-audit.md), tabella
> [`audit_trail_entries`](../../../analisi-post/db/audit/audit_trail_entries.md)), alimentato dagli eventi
> che ogni modulo pubblica per le azioni degli operatori. Nel legacy questo dato non esiste.
>
> **API scritte il 2026-10-13**: [`docs/88-api-audit.md`](../../../progettazione/88-api-audit.md). La firma sotto è quella
> ipotizzata in origine; quella reale segue lo standard backend (stabilimento dal token, `from`/`to`,
> `actionCodes`, `actorUserId` uuid, `PagedResult`, export come file diretto, niente `actionIcon`/`targetUrl`).

**Input → API** (`sb/property/likeLogs` / endpoint audit-log — « firma ipotizzata »)
- `propertyId` (int)
- `dateFilter` (oggetto: `{ preset }` **o** `{ dates: [date] }`)
- `actionTypes` (string[]) / `userId` (int, opzionale — preimpostato dal pulsante «Log»)
- `search` (string)
- `page` (int) / `pageSize` (int, default 10) / `sortBy` / `sortDir`

**Output ← API**
- `total` (int)
- `rows` (array) — `{ logId, userId, bookingId?, operatorName, elementLabel?, actionType,
  actionIcon, at, targetUrl }`

**Input → API — Esporta**: `{ ...filtri, format: "csv"|"xlsx" }` → `{ fileUrl }`.

---

## 4. Stagioni e periodi

**Descrizione**: box 1 «Imposta le date di inizio e fine della stagione» (Inizio/Fine stagione
corrente, Inizio/Fine stagione successiva — la successiva serve al «Rinnova prenotazione»); la
modifica di un campo data **si salva automaticamente**, senza pulsante. Box 2 «Periodi» (lista dei
periodi della stagione corrente, «Aggiungi periodo»). Modale periodo: nome + calendario;
**più intervalli** per lo stesso periodo, **non sovrapponibili**, ognuno con «X»; «Salva» crea il
periodo. Modifica (matita) = modale precompilato; Elimina (cestino).
**Schermata**: node `7217-199698`, `7218-200749` (aggiungi periodo).
**Ruolo**: gestore.

**Input → API — stagioni** (`sb/viewseasonsperiods` per leggere; `sb/updateproperty` per salvare —
« firma ipotizzata » sull'autosave per campo)
- `propertyId` (int)
- `seasons` ({ currentStart, currentEnd, nextStart, nextEnd })

**Input → API — periodi** (« firma ipotizzata »)
- crea/modifica: `{ propertyId, periodId?, name, dateRanges: [ { from, to } ] }`
- elimina: `{ periodId, confirm: true }`

**Output ← API**
- `seasons` ({ … })
- `periods` (array) — `{ periodId, name, dateRanges: [ { from, to } ] }`

---

## 5. Settori e file

**Descrizione**: box con la lista dei settori/file, pulsante «Aggiungi fila/settore» (modale con
campo nome + «Salva»); per riga: Modifica (matita, modale precompilato) ed Elimina (cestino).
Funzionalità invariate, solo restyling.
**Schermata**: node `7230-571`, `7230-842` (aggiungi).
**Ruolo**: gestore.

**Input → API** (`sb/getLocationStatisticsBySector` per la lista/uso; CRUD settori « firma
ipotizzata » — probabile `sb/updateproperty`)
- lista: `{ propertyId }` → `sectors: [ { id, name, elementsCount } ]`
- crea/modifica: `{ propertyId, sectorId?, name }`
- elimina: `{ sectorId, confirm: true }`

**Output ← API**: `sectors` aggiornati; `status` / `message`.

**Note**: questi settori/file alimentano il filtro «Settori» della Mappa
([`05-gestionale-mappa.md`](05-gestionale-mappa.md) az. 12) e l'ordinamento in Configurazione mappa.

---

## 6. Configura mappa (pagina Impostazioni)

**Descrizione**: box: 1) Sfondo mappa (galleria di sfondi predefiniti + upload personalizzato);
2) Colore di sfondo (color picker, area esterna al reticolo); 3) «Imposta reticolo» — Numero di file,
Numero massimo articoli per fila, **Inclinazione mappa (°)** (nuova: inclina l'intero reticolo, righe
e colonne, e la disposizione articoli); 4) Icone delle postazioni («Seleziona icona» → popup icone)
per **Ombrellone** e **Articolo personalizzato** (che sostituisce «Ombrelloni Special»). Pulsante di
salvataggio finale.
**Schermata**: node `7234-193009`.
**Ruolo**: gestore.

**Input → API — lettura** (`sb/viewconfiguration`)
- `propertyId` / `bathHouseId` (int)

**Output ← API**
- `background` ({ presets: [ { id, url } ], selected: { type: "preset"|"custom", url }, outerColor })
- `grid` ({ rows (int), maxItemsPerRow (int), tiltDegrees (number) })
- `icons` ({ umbrella: { iconId, url }, customArticle: { iconId, url } })

**Input → API — salvataggio** (`sb/updateproperty` / endpoint configurazione — « firma ipotizzata »)
- `background` ({ type, presetId? , image (file)?, outerColor })
- `grid` ({ rows, maxItemsPerRow, tiltDegrees })
- `icons` ({ umbrellaIconId, customArticleIconId })

**Note**: l'**editor** del reticolo (inserimento/spostamento/rotazione articoli, elementi spiaggia,
azioni riga/colonna) è la schermata «Configurazione mappa» descritta in
[`12-gestionale-configurazione-mappa.md`](12-gestionale-configurazione-mappa.md); questa pagina
Impostazioni ne definisce solo i **parametri** (righe, max articoli/fila, inclinazione, sfondo,
icone). Sostituisce il placeholder az. 1 di quel documento.

---

## 7. Magazzino (Impostazioni)

**Descrizione**: box «Lista articoli» (articoli, quantità totale, soglia di disponibilità). «Aggiungi
articolo» → modale con i campi attuali + **nuovo flag «Avviso soglia»** (prima era nella lista):
flag attivo = notifica al raggiungimento della soglia minima; soglia e flag salvati con l'articolo.
Per riga: Modifica (matita, campi + soglia/flag precompilati) ed Elimina (cestino).
**Schermata**: node `7258-199922`, `7329-206963` (aggiungi articolo).
**Ruolo**: gestore.

**Input → API** (`sb/warehouse/view` per la lista)
- lista: `{ propertyId }` → `items: [ { id, name, quantity, threshold?, thresholdAlert (bool) } ]`
- crea/modifica (`sb/warehouse/bookingdetails` / endpoint magazzino — « firma ipotizzata »):
  `{ propertyId, itemId?, ...campi attuali, threshold (number), thresholdAlert (bool) }`
- elimina: `{ itemId, confirm: true }`

**Output ← API**: `items` aggiornati; `status` / `message`.

---

## 8. Listino (stabilimento)

**Descrizione**: creazione del listino dello stabilimento (pacchetti + articoli/servizi aggiuntivi).
Selezione singola o checkbox, con «seleziona tutti» **separate** per pacchetti e per articoli.
Azioni multiple (in alto a sinistra e per riga, con conferma): Duplica, Elimina. Pulsanti in alto a
destra: nuovo pacchetto / nuovo articolo aggiuntivo. Novità:
- **Prezzo cumulativo** nel menu «Calcolo del prezzo» del pacchetto: per una prenotazione su più
  postazioni (es. 5 giorni + 2 giorni) calcola direttamente il prezzo dei 7 giorni da listino,
  invece di sommare i due tratti.
- **Articoli/servizi aggiuntivi**: menu (dopo la selezione dell'articolo) «Tutti i pacchetti»
  (default) / «Pacchetti specifici» (elenco dei pacchetti del listino).
- **Prezzi mezza giornata**: nuove colonne nella tabella prezzi (incluso «Prezzo stagionale mezza
  giornata»).
- **Import/Export prezzi intermedi**: pulsanti «Esporta file excel/.csv» e «Importa file excel/.csv»
  per caricare massivamente i prezzi intermedi (pacchetti e articoli).
**Schermata**: node `7329-207536`, `7329-234957` (nuovo pacchetto), `7333-203795` (nuovo articolo),
`7329-224343` (azioni multiple), `7333-202053` (import/export).
**Ruolo**: gestore.

**Input → API** (`sb/allpricelist` per leggere; endpoint listino stabilimento — « firma ipotizzata »,
analogo a `sb/hotel/pricelist*`)
- lista: `{ propertyId }` → `{ packages: [ … ], articles: [ … ] }`
- crea/modifica pacchetto: `{ propertyId, packageId?, name, priceCalc:
  "perDay"|"fixed"|"cumulative", priceRows: [ { periodId?, seasonal (bool), fullDay, halfDay,
  seasonalFullDay, seasonalHalfDay } ], ... }`
- crea/modifica articolo: `{ propertyId, articleId?, name, appliesToPackages: "all" | [packageId],
  priceRows: [ … come sopra ] }`
- bulk: `{ propertyId, kind: "package"|"article", ids: [int], op: "duplicate"|"delete" }`
- export/import prezzi intermedi: `{ propertyId, kind, format: "xlsx"|"csv" }` → `{ fileUrl }` /
  upload `{ propertyId, kind, file }` → `{ imported, errors: [ { row, reason } ] }`

**Output ← API**: `packages` / `articles` aggiornati; `status` / `message`.

**Note**: il listino dedicato / voucher per hotel è in
[`10-gestionale-convenzioni.md`](10-gestionale-convenzioni.md) az. 4–5 (stessa struttura di pagina).

---

## 9. Fiscalizzazione (ex «Impostazioni stampante»)

**Descrizione**: pagina rinominata «Fiscalizzazione», organizzata in **accordion**. Sezione
«Stampanti» (contenuti/funzioni invariati; espansa di default; «Aggiungi stampante» → modale).
Nuova sezione «Scontrini elettronici»: mostra i «Termini e condizioni» fiskaly; dopo l'accettazione
si inseriscono i dati di configurazione dell'account e, al «Salva», compare un messaggio di conferma.
**Schermata**: node `3763-351619`, `3763-353270` (menu secondario), `3974-363568` (aggiungi
stampante), `3974-363070` / `3974-363247` / `4033-377932` (scontrini elettronici).
**Ruolo**: gestore.

**Input → API — stampanti** (`sb/get-config-printer`, `sb/add-printer`, `sb/update-printer`,
`sb/update-printer-status`, `sb/delete-printer`) — contratto invariato.

**Input → API — scontrini elettronici** (`sb/electronic-receipt/get-terms`,
`sb/electronic-receipt/update-terms`, `sb/electronic-receipt/get-cash-registers`)
- leggi termini: `{ propertyId }` → `{ termsText, accepted (bool), acceptedAt? }`
- accetta + configura account: `{ propertyId, termsAccepted: true, account: { ...campi fiskaly } }`

**Output ← API**: `status` / `message` (conferma configurazione).

**Note**: cassa/scontrino fiscale sono online-only (cfr. memoria «Offline hub»); questa pagina
configura il provider fiskaly usato da `front-modern-pre`.

---

## 10. Database — «Cancella dati per gruppo» e Cestino (nuova pagina)

**Descrizione**: pagina nuova per selezionare ed eliminare in blocco i dati delle prenotazioni.
Tabella «Cancella dati per gruppo» con: filtro «Tutte le date» (opzioni predefinite + calendario con
«Conferma»), «Filtri» (modale; opzioni a checkbox; tag dei filtri attivi con «X» + «Azzera filtri»),
«Cerca per nome» (cliente o postazione), selettore risultati/pagina (default 10; 20/50/100/200/300) e
paginazione (sopra e sotto), checkbox per riga (header = solo pagina corrente). Con ≥ 1 selezione si
attiva «Cancella dati» (altrimenti opacità 30%) → conferma «Dati spostati nel cestino».
**Cestino** (pulsante in alto a destra): stessa tabella/filtri sui dati già cancellati, con 3
pulsanti: «Svuota cestino» (sempre attivo; popup di conferma; elimina tutto definitivamente),
«Ripristina» e «Elimina definitivamente» (attivi solo con ≥ 1 selezione; ciascuno con proprio
messaggio/popup di conferma).
**Schermata**: node `7378-198590`, `7388-220878` (filtro date), `7381-206946` (filtri),
`7388-220537` (tag filtri), `7424-209000` / `7393-221904` (cestino), `7424-207283` / `7424-210630` /
`7424-209813` / `7424-208142` (conferme).
**Ruolo**: gestore.

**Input → API — lista** (`sb/getBookingAreaList` / endpoint dedicato — « firma ipotizzata »)
- `propertyId` / `bathHouseId` (int)
- `bin` (bool) — `false` = pagina Database, `true` = Cestino
- `dateFilter` / `filters` / `search` / `page` / `pageSize` / `sortBy` / `sortDir`

**Output ← API**
- `total` (int)
- `rows` (array) — dati prenotazione (colonne come da mockup)

**Input → API — azioni** (« firma ipotizzata »)
- sposta nel cestino: `{ bookingIds }` → toast «Dati spostati nel cestino»
- ripristina: `{ bookingIds }`
- elimina definitivamente: `{ bookingIds, confirm: true }`
- svuota cestino: `{ propertyId, confirm: true }`

**Output ← API**: `status` / `message`; tabella aggiornata.

---

## Stato di completamento

Il docx sorgente **non è definitivo**. Punti dichiarati «in aggiornamento» o non chiusi, da
riverificare:
- «Tutti i log»: la lista delle **tipologie di azione** filtrabili è «in fase di aggiornamento».
- Listino: la tabella delle nuove colonne prezzi mezza giornata è mostrata solo nei mockup (nomi
  colonne da confermare).
- Pagine Impostazioni citate altrove ma **non ancora progettate in Figma**: «Sconti e voucher»
  (usata da [`06-gestionale-dettagli-e-prenotazioni.md`](06-gestionale-dettagli-e-prenotazioni.md)
  az. 2, voci «Applica codice sconto» / «Applica sconto»).
