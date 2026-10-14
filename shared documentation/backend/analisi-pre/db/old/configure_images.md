# Tabella `configure_images`

> Fonti: schema e dati reali (89 righe), model
> [`back-pre/app/models/confimages.model.js`](../../back-pre/app/models/confimages.model.js)
> (`sequelize.define("configure_images", ...)`, nome modello JS `Confimages`), controller
> [`confimages.controller.js`](../../back-pre/app/controllers/confimages.controller.js) (letto per intero,
> 228 righe), frontend `front-pre/src/components/configuremap/EditTemplate.js`.
>
> **Nota di contesto**: tabella **globale/master** (nessun `propertyId`) — catalogo delle icone/immagini
> disponibili per comporre la mappa spiaggia, da cui `configure_maps` (vedi
> [`configure_maps.md`](configure_maps.md)) copia gli URL scelti dal gestore. A differenza della maggior
> parte delle altre tabelle di catalogo di questo dominio (`services`, `servicetypes_icons`,
> `configure_categories`), **ha un pannello di amministrazione completo** (create/attiva-disattiva/elimina),
> gestito da SuperAdmin.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..
- **Nota**: un'interrogazione diretta e recente del DB mostra **40 righe totali** nella tabella, non le 89
  indicate in apertura di questo documento — probabile snapshot di ambienti/momenti diversi; le percentuali
  riportate nei nuovi bullet di questo file si basano sulle 40 righe della query più recente.

### category
- **Tipo**: `varchar`
- **Valori osservati**: allineati esattamente alle 5 righe di
  [`configure_categories.md`](configure_categories.md) — `template`, `umbrella`, `tent`, `sunbed`, `cabin`
  (commento a livello DB: `'template, umbrella, tent, sunbed, cabin'`).
- **Relazioni**: applicativa verso `configure_categories.category` — **confrontata per valore stringa, non
  per id** (`activeconfimages` filtra con `where: { category: req.body.category }`, un match testuale
  diretto); nessuna FK dichiarata. **Candidato a enumerazione**: sui dati reali solo 5 valori distinti su 40
  righe ("umbrella" 28, "template" 7, "cabin" 2, "sunbed" 2, "tent" 1) — vocabolario chiuso, coerente con le
  5 righe di `configure_categories`; da modellare con una vera FK verso `configure_categories.id` (vedi
  anche la nota su quella tabella) invece che con un ENUM duplicato in due punti dello schema.
- **Nullabile**: No
- **Esempio di valori**: "umbrella" (28 righe), "template" (7), "cabin" (2), "sunbed" (2), "tent" (1)

### type
- **Tipo**: `varchar`
- **Valori osservati** (commento a livello DB): `occupied` / `free`
- **Descrizione**: per ogni categoria/icona, esistono normalmente due varianti — l'aspetto dell'elemento
  quando è occupato/prenotato e quando è libero — coerente con i campi `umbrellaImg`/`umbrellaImage_free`
  ecc. di `configure_maps`. **Candidato a enumerazione (forte)**: sui dati reali solo 2 valori distinti su
  40 righe, ma con **capitalizzazione diversa dal commento a livello DB** — il commento SQL dichiara
  `occupied | free` (minuscolo), mentre i dati reali contengono `"Occupied"` (30 righe) e `"Free"` (10
  righe), maiuscolo. Ulteriore segnale di fragilità del testo libero non validato: da modellare come vero
  ENUM a livello DB (con valori canonici fissati) invece di `varchar` libero, per evitare la comparsa di
  varianti di capitalizzazione non allineate al commento dello schema.
- **Nullabile**: No
- **Esempio di valori**: "Occupied" (30 righe), "Free" (10 righe)

### image
- **Tipo**: `varchar`
- **Descrizione**: URL dell'immagine, caricata su S3 e poi riscritta con dominio CloudFront
  (`addconfimages`, righe 90-142: upload su `AWSCredentials.AWS_CONF_IMG_PATH`, poi
  `photourl.replace(s3Url, cloudFrontUrl)` con URL hardcoded nel controller — vedi criticità).
- **Nullabile**: No
- **Esempio di valori**: URL CloudFront, es. "https://d26kxhigi9o4u2.cloudfront.net/confimages/xyz123.png"

### name
- **Tipo**: `varchar`
- **Descrizione**: nome descrittivo dell'icona, mostrato nel selettore del gestionale.
- **Nullabile**: No
- **Esempio di valori**: "Umbrella 4", "Cabin 2"

### status
- **Tipo**: `boolean`, default `false`
- **Valori osservati**: mix di attivo/non attivo (gestito da `upstatusconfimages`). **Correzione dai dati
  reali**: l'interrogazione diretta più recente (40 righe) mostra invece `status = 1` su **tutte** le righe
  attualmente presenti, senza eccezioni — il "mix" descritto sopra non è (più) osservabile nello snapshot
  usato per questa verifica; può darsi che le righe disattivate siano state rimosse/riattivate nel
  frattempo, oppure che il campione originale includesse un ambiente diverso. Il comportamento applicativo
  descritto sotto (filtro in `activeconfimages`) resta comunque corretto e verificato nel codice
  indipendentemente da quali righe siano oggi attive.
- **Descrizione**: solo le icone `status: true` sono restituite da `activeconfimages` (l'endpoint
  effettivamente usato dal selettore in `EditTemplate.js`); `listconfimages` (pannello SuperAdmin) le
  restituisce invece tutte, indipendentemente dallo stato. **Candidato a enumerazione**: il commento a
  livello DB (`0-inactive | 1-active`) documenta già esplicitamente un vocabolario chiuso a due valori — il
  tipo `tinyint(1)`/`boolean` è già la scelta corretta, non serve un ENUM aggiuntivo; da mantenere così
  nella riscrittura.
- **Nullabile**: No
- **Esempio di valori**: 1 (unico valore osservato su tutte le 40 righe attuali)

### createdAt / updatedAt
- **Tipo**: `datetime` — standard.
- **Nullabile**: No
- **Esempio di valori**: "2023-05-12 10:15:00", "2024-11-03 09:42:17"

---

## Osservazioni verificate nel codice (criticità)

- **`upstatusconfimages` non attende l'`update`** (`confimages.controller.js:159`,
  `Confimages.update(...)` senza `await`): stesso bug ricorrente già documentato in più tabelle di questo
  dominio — il client riceve `statusConfimagesUpdated` anche se la scrittura fallisce.
- **URL S3→CloudFront hardcoded nel controller** (`confimages.controller.js:91-92`,
  `s3Url = "https://summerbooking.s3.us-east-2.amazonaws.com"`,
  `cloudFrontUrl = "https://d26kxhigi9o4u2.cloudfront.net"`): valori d'ambiente scritti direttamente nel
  codice invece che in configurazione — un cambio di bucket/distribuzione CloudFront richiederebbe una
  modifica di codice; da spostare in configurazione nella riscrittura (criticità dello stesso tipo di
  quelle già segnalate a livello generale in
  [01-criticita-e-indicazioni-backend.md](../../docs/01-criticita-e-indicazioni-backend.md) per credenziali/
  endpoint hardcoded).
- **`deleteconfimage` esegue una cancellazione fisica reale** (`Confimages.destroy`, righe 183-227), a
  differenza della maggior parte delle altre tabelle di questo dominio che usano soft-delete
  (`isDeleted`/`status`) — coerente qui perché accompagnata dalla cancellazione effettiva del file su S3
  (`deleteFileFromS3`); se la cancellazione da S3 fallisce, la riga **non** viene eliminata dal DB
  (`if (deleteResponse == true)`), evitando quantomeno la creazione di riferimenti a file inesistenti — un
  comportamento corretto, riportato qui per completezza e non come criticità.
- **Nessun controllo di unicità/duplicati su `category`+`type`+`name`**: nulla impedisce di creare più
  icone identiche per la stessa combinazione categoria/tipo — non necessariamente un problema applicativo,
  ma da valutare se serve un vincolo nella riscrittura.
- **`activeconfimages` è protetto da `authJwt.isSuperAdmin` a livello di route**
  (`user.routes.js:280-291`), che richiede letteralmente il ruolo `"superadmin"` (verificato in
  `authJwt.js:86-102` — confronto esatto sul nome ruolo, non un gruppo di ruoli "admin-like"; ruolo già
  documentato come account interno della piattaforma in [`roles.md`](roles.md)). **Ma l'endpoint è
  chiamato da `EditTemplate.js`**, componente del pannello "Configura mappa spiaggia"
  (`configuremap/ConfigureMapPanel.js`, azione `_activeconfimages` in `managerActions.js:70-72`), montato
  dentro `Settings/SettingContent.js` — cioè l'area di configurazione dello stabilimento **usata dai
  gestori proprietari**, non un'area riservata al superadmin. Se l'enforcement `isSuperAdmin` è davvero
  quello effettivo in produzione, un gestore normale **non potrebbe caricare l'elenco icone attive** per
  configurare la propria mappa spiaggia tramite questo endpoint — un'incoerenza concreta fra il controllo
  di accesso dichiarato a livello di route e l'uso reale del componente frontend, da verificare con il
  team prodotto (route con permessi sbagliati, oppure componente frontend erroneamente esposto a un ruolo
  che non dovrebbe raggiungerlo) prima di replicare l'uno o l'altro comportamento nella riscrittura.
