# systemadmin — Catalogo icone mappa spiaggia (`configure_images`)

> **Tipo di utente**: `systemadmin` (a livello di rotta il controllo dichiarato è però `isSuperAdmin` per
> l'unico endpoint di lettura "attive", e `isSystemAdmin` per gli endpoint di gestione — vedi §0.1).
>
> **Ambito**: gestione del **catalogo master globale** delle icone/immagini con cui si compongono le
> mappe spiaggia di **tutti** gli stabilimenti (tabella `configure_images`, senza `propertyId`). Da
> questo catalogo il gestore del singolo stabilimento **copia** gli URL scelti quando configura la
> propria mappa (`configure_maps`). È l'unica tabella di catalogo di questo dominio con un pannello
> amministrativo completo (crea / attiva-disattiva / elimina).
>
> **Fonti**: `back-pre` — `app/controllers/confimages.controller.js` (letto integralmente),
> `app/routes/user.routes.js` (righe ~280-291 e ~645-692), `app/routes/public.routes.js` (riga 19).
> Schede DB: [`configure_images.md`](../../db/configure_images.md),
> [`configure_categories.md`](../../db/configure_categories.md), [`configure_maps.md`](../../db/configure_maps.md).
>
> **Frontend (aggiornato 2026-09-09)**: il pannello di gestione del catalogo **è ora nel repository**,
> in [`front-admin/`](../../../../../../../front-admin/) → modulo `ConfigurationSettings`, voce di menù
> **"Configuration Settings › Configuration Map"** (rotta `/config-setting/config-list`). Invoca tutti e
> quattro gli endpoint prima "senza consumer": `GET sb/listconfimages` (lista completa), `POST
> sb/addconfimages` ("Add Template" — upload immagine come data URL base64), `POST sb/upstatusconfimages`
> (toggle `react-switch` per riga), `POST sb/deleteconfimage`, più `GET public/conficats` per popolare la
> tendina categoria. **`activeconfimages` non è usato da `front-admin/`** (resta chiamato solo dal
> pannello "Configura mappa spiaggia" del gestore). Contratto client↔API in
> [`analisi-pre/azioni/systemadmin/02-catalogo-icone-mappa-spiaggia.md`](../../azioni/systemadmin/02-catalogo-icone-mappa-spiaggia.md).
>
> **Conferme dal client**: la tendina "image type" del form di aggiunta ha valori `"Free"` / `"Occupied"`
> (maiuscoli) — è **il frontend** a produrre la capitalizzazione non normalizzata descritta in §0.2. La
> lista mostra le colonne Name / Category / Type / Image / Status / Action e non è paginata lato client
> (conferma §1: `listconfimages` restituisce tutto).

---

## 0. Premesse

### 0.1 Autorizzazione (incoerenze rilevate)

| Endpoint | Controllo dichiarato nel codice | Uso reale |
|---|---|---|
| `GET /api/sb/listconfimages` | token + `isSystemAdmin` | `front-admin/` — `ConfigurationSettings/ConfigList.js` |
| `POST /api/sb/addconfimages` | token + `isSystemAdmin` | `front-admin/` — `ConfigurationSettings/AddImage.js` |
| `POST /api/sb/upstatusconfimages` | token + `isSystemAdmin` | `front-admin/` — `ConfigurationSettings/UpdateConfigStatus.js` |
| `POST /api/sb/deleteconfimage` | token + `isSystemAdmin` | `front-admin/` — `ConfigurationSettings/DeleteImageDialog.js` |
| `POST /api/sb/activeconfimages` | token + **`isSuperAdmin`** | chiamato dal pannello mappa del **gestore** (`configuremap/EditTemplate.js`, `EditGrid.js`, `Settings/OnlineBookingModal.js`) in `front-pre` e `front-customer-pre` |
| `GET /api/public/conficats` | nessuno (pubblico) | categorie icone |

**Incoerenza da chiarire**: `activeconfimages` richiede letteralmente il ruolo `superadmin` (account
interno della piattaforma), ma è invocato dall'area di configurazione usata dai **gestori proprietari**.
Se l'enforcement è quello effettivo in produzione, un gestore non-superadmin non riuscirebbe a caricare
l'elenco icone per configurare la propria mappa. Da verificare col team prodotto (permesso di rotta
sbagliato, o componente frontend esposto a un ruolo che non dovrebbe raggiungerlo).

### 0.2 Struttura del catalogo

- Ogni icona ha: **categoria** (`category`), **tipo** (`type`), **immagine** (`image`, URL),
  **nome** (`name`), **stato** (`status` booleano, default `false`).
- **Categorie**: vocabolario chiuso di 5 valori, allineato 1:1 alle 5 righe di `configure_categories`:
  `template`, `umbrella`, `tent`, `sunbed`, `cabin`. Il collegamento è per **stringa**, non per id
  (nessuna FK).
- **Tipo**: due varianti previste — elemento "occupato" vs "libero". Il commento a DB dichiara
  `occupied` / `free` (minuscolo), ma i dati reali contengono `"Occupied"` / `"Free"` (maiuscolo):
  testo libero non normalizzato.
- **Stato**: solo le icone con `status = true` compaiono nel selettore del gestore
  (`activeconfimages`); il pannello systemadmin (`listconfimages`) le vede tutte.

---

## 1. Elenco completo del catalogo

**Azione**: il systemadmin apre la lista di tutte le icone. Rotta: `GET /api/sb/listconfimages`.

**Logiche**: restituisce **tutte** le righe di `configure_images` senza filtro, senza paginazione,
senza ordinamento esplicito. Se la tabella è vuota, risponde `status: false` con "confimages not found".

**Calcoli**: nessuno. **Eventi**: nessuno (sola lettura).

---

## 2. Elenco icone attive per categoria (usato dal gestore)

**Azione**: nel pannello "Configura mappa spiaggia" il gestore seleziona una categoria (es. `umbrella`,
`template`) e vede le icone disponibili. Rotta: `POST /api/sb/activeconfimages`. Input: `category`.

**Logiche**: restituisce le righe di `configure_images` con `category` = valore richiesto **e**
`status = true` (match testuale diretto sulla categoria). Nessun risultato → `status: false`,
"confimages not found".

**Calcoli**: nessuno. **Eventi**: nessuno.

**Uso a valle**: gli URL restituiti vengono copiati dal gestore dentro la propria configurazione mappa
(`configure_maps`: campi `umbrellaImg`/`umbrellaImage_free`, `templateImg`, ecc.). È una **copia per
valore**: se in seguito il systemadmin cambia o elimina l'icona dal catalogo, le mappe che l'avevano
già scelta **non** si aggiornano (e possono puntare a un file S3 non più esistente — vedi §4).

---

## 3. Elenco categorie (pubblico)

**Azione**: caricamento delle 5 categorie. Rotta: `GET /api/public/conficats` (nessuna autenticazione).

**Logiche**: restituisce tutte le righe di `configure_categories` (5). Tabella statica: nessun endpoint
la scrive.

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 4. Aggiunta di una nuova icona

**Azione**: il systemadmin carica una nuova icona. Rotta: `POST /api/sb/addconfimages`. Input:
`category`, `type`, `name`, `status`, `image` (data URL base64).

**Logiche e sequenza**:

1. Crea subito la riga `configure_images` con `category`, `type`, `name`, `status` presi dal body e
   `image` = stringa vuota (placeholder).
2. **Carica l'immagine su S3** nel percorso "immagini configurazione".
3. **Riscrive l'URL** ottenuto sostituendo il dominio S3 con il dominio CloudFront (entrambi i valori
   sono **hardcoded nel controller**: un cambio di bucket/distribuzione richiederebbe modifica di
   codice — da spostare in configurazione).
4. Aggiorna la riga con l'URL CloudFront definitivo.
5. Risponde "confimages added".

**Calcoli**: nessuno. **Eventi**: upload file su S3. Nessuna email/notifica.

**Anomalie / debito**:

- Nessun controllo di **unicità** su categoria + tipo + nome: si possono creare icone duplicate.
- `category` e `type` sono testo libero non validato contro il vocabolario chiuso: si può inserire una
  categoria inesistente (che poi non comparirà mai nel selettore) o un `type` con capitalizzazione
  diversa.
- Se l'upload S3 fallisce, la riga resta creata con `image` vuoto (record "monco").

---

## 5. Attivazione / disattivazione di un'icona

**Azione**: il systemadmin rende un'icona visibile o nascosta nel selettore del gestore. Rotta:
`POST /api/sb/upstatusconfimages`. Input: `id`, `status`.

**Logiche**: recupera la riga per id; se non esiste, "confimages not found". Altrimenti imposta
`status` al valore richiesto.

**Anomalia verificata**: l'operazione di aggiornamento **non viene attesa** prima di rispondere: il
client riceve "status confimages updated" anche se la scrittura fallisce. Da correggere.

**Calcoli**: nessuno. **Eventi**: nessuno.

**Effetto**: disattivare un'icona la fa sparire da `activeconfimages` (selettore gestore) ma **non**
tocca le mappe che l'hanno già copiata.

---

## 6. Eliminazione di un'icona

**Azione**: il systemadmin elimina definitivamente un'icona. Rotta: `POST /api/sb/deleteconfimage`.
Input: `id`.

**Logiche e sequenza**:

1. Recupera la riga per id; se non esiste, "confimages not found".
2. **Elimina il file da S3** (percorso "immagini configurazione").
3. **Solo se** la cancellazione da S3 è andata a buon fine → elimina **fisicamente** la riga dal DB
   (hard delete, non soft-delete: è l'unica tabella di questo dominio a farlo). Risponde "confimage
   deleted".
4. Se la cancellazione da S3 fallisce → la riga **non** viene eliminata; risponde "confimage not
   deleted". (Comportamento corretto: evita riferimenti a file inesistenti.)

**Calcoli**: nessuno. **Eventi**: cancellazione file su S3. Nessuna email/notifica.

**Debito**: nessun controllo se l'icona è **in uso** da qualche `configure_maps`: eliminandola, le
mappe che la referenziano restano con un URL CloudFront che punta a un file cancellato (icona rotta
nella mappa del cliente). Da gestire nella riscrittura (blocco eliminazione se referenziata, oppure
riferimento per id + fallback).

---

## 7. Riepilogo

**Calcoli**: nessuno in tutta l'area.

**Eventi**: solo interazioni con S3 (upload in §4, delete in §6). Nessuna email, nessuna notifica,
nessuna API esterna, nessun job schedulato.

**Punti da chiarire**: vedi [`../da-chiarire.md`](../da-chiarire.md) — sezione "Autorizzazioni e ruoli"
(incoerenza `activeconfimages`/`isSuperAdmin`) e "Integrità referenziale & cataloghi" (copia per valore
degli URL icona, eliminazione senza controllo d'uso, vocabolari chiusi come testo libero).
