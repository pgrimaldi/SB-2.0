# systemadmin — Catalogo icone mappa spiaggia (azioni client↔API)

> **Ruolo**: systemadmin. **Fonte**: [`front-admin/`](../../../../../../../front-admin/) — modulo
> `ConfigurationSettings` (`ConfigList.js`, `AddImage.js`, `UpdateConfigStatus.js`, `DeleteImageDialog.js`,
> `api/ConfigSettingApicalls.js`), voce di menù "Configuration Settings › Configuration Map"
> (`/config-setting/config-list`). Logica di business in
> [`business-logic/systemadmin/02-catalogo-icone-mappa-spiaggia.md`](../../business-logic/systemadmin/02-catalogo-icone-mappa-spiaggia.md).
>
> **Nota**: in `ConfigSettingApicalls.js` gli URL sono costruiti con un **secondo** base URL hardcoded
> (`API_PATH = "https://api.summerbooking.it/api"`), non con quello di `config.js`. Risposte cifrate
> come nelle altre aree (payload in chiaro descritto sotto).

---

## 1. Elenco del catalogo icone

**Descrizione**: apertura della pagina "Configuration Map": tabella di tutte le icone/immagini del
catalogo master.
**Schermata**: `ConfigurationSettings/ConfigList.js`
**Ruolo**: systemadmin

**Input → API** (`GET sb/listconfimages`) — nessun parametro

**Output ← API**
- `status` (bool) — `false` = catalogo vuoto (mostra `message`)
- `data` (array) — per riga: `id` (int), `name` (string), `category` (string), `type` (string),
  `image` (string, URL), `status` (0|1 — reso come toggle `react-switch`)
- `message` (string)

**Note**: nessuna paginazione lato server; ricerca in memoria su `name`/`type`/`category`.

---

## 2. Elenco categorie (per il form di aggiunta)

**Descrizione**: caricamento della tendina "image category" nella modale "Add Template".
**Schermata**: `ConfigurationSettings/AddImage.js`
**Ruolo**: systemadmin (endpoint di per sé pubblico)

**Input → API** (`GET public/conficats`) — nessun parametro

**Output ← API**
- `status` (bool)
- `data` (array) — per riga: `category` (string); i 5 valori attesi: `template`, `umbrella`, `tent`,
  `sunbed`, `cabin`
- `message` (string)

---

## 3. Aggiunta di una nuova icona

**Descrizione**: dalla modale "Add Template" il systemadmin carica un'immagine e ne definisce
nome/categoria/tipo.
**Schermata**: `ConfigurationSettings/AddImage.js`
**Ruolo**: systemadmin

**Input → API** (`POST sb/addconfimages`)
- `name` (string) — "Image name"
- `category` (string) — valore da `public/conficats`
- `type` (string) — tendina con valori **`"Free"` / `"Occupied"`** (maiuscoli: è il client a produrre
  la capitalizzazione non normalizzata segnalata in business-logic §0.2)
- `image` (string) — data URL base64 del file selezionato (letto con `FileReader`)
- `status` (bool) — sempre `true` alla creazione

**Output ← API**
- `status` (bool)
- `message` (string) — Snackbar; su successo `callBack()` (ricarica lista) e chiusura modale dopo 3 s

**Note**: nessun controllo di unicità (`category`+`type`+`name`); `category`/`type` come testo libero.
Upload S3 + riscrittura URL su CloudFront lato server (business-logic §4).

---

## 4. Attivazione / disattivazione di un'icona

**Descrizione**: dalla tabella, il systemadmin gira il toggle di stato di una riga.
**Schermata**: `ConfigurationSettings/UpdateConfigStatus.js`
**Ruolo**: systemadmin

**Input → API** (`POST sb/upstatusconfimages`)
- `id` (int) — id icona
- `status` (bool) — nuovo stato (`Boolean(tmp_status)`)

**Output ← API**
- `status` (bool)
- `message` (string) — Snackbar; il client ricarica comunque la lista (`callback()`)

**Note**: lato server l'`update` non è atteso prima della risposta (business-logic §5). Disattivare
un'icona la nasconde da `activeconfimages` (selettore gestore) ma non tocca le mappe che l'hanno già
copiata.

---

## 5. Eliminazione di un'icona

**Descrizione**: dalla tabella, il systemadmin conferma l'eliminazione nella modale.
**Schermata**: `ConfigurationSettings/DeleteImageDialog.js`
**Ruolo**: systemadmin

**Input → API** (`POST sb/deleteconfimage`)
- `id` (int) — id icona

**Output ← API**
- `status` (bool)
- `message` (string) — Snackbar; su successo ricarica la lista

**Note**: hard-delete reale (riga + file S3) solo se la cancellazione da S3 riesce; nessun controllo se
l'icona è in uso da qualche `configure_maps` (business-logic §6).
