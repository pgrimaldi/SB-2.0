# Tabella `bathouses`

> Fonti usate per questa scheda: schema reale della tabella (`SHOW CREATE TABLE`/`information_schema`),
> **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **10.432 righe** — query completa; le
> versioni precedenti di questa scheda si basavano su un campione dev da 100 righe o su un estratto
> parziale da 3.899 righe, entrambi superati dai numeri riportati qui), il model Sequelize
> [`back-pre/app/models/bathouse.model.js`](../../../../../../back-pre/app/models/bathouse.model.js), il controller
> [`bathouse.controller.js`](../../../../../../back-pre/app/controllers/bathouse.controller.js) (letto per intero), le
> associazioni in `models/index.js` (`bathouse.belongsTo(property)`, `property.hasMany(bathouse)` — unica
> relazione con vincolo FK reale nel DB, già documentata in
> [`properties.md`](properties.md#tabelle-collegate)), e il frontend
> (`front-pre/src/components/bathhouse/`: `Page1BathHouse.js`, `ServicesList.js`, `CheckboxContainer.js`,
> `PhotoSection.js`, verificati anche in `front-customer-pre` per differenze).
>
> **Nota di contesto**: come già anticipato in [`properties.md`](properties.md), `bathouses` è la "vetrina
> pubblica" dello stabilimento — indirizzo, contatti, descrizione, foto, servizi offerti — distinta da
> `properties` (dati gestionali/di piattaforma). Relazione 1:1 nella pratica (una `property` ha una sola
> `bathouse`), anche se modellata come 1:N a livello di associazione Sequelize.
>
> **Fotografia di produzione — la tabella è quasi tutta "stub"**: delle 10.432 righe, **~10.250 sono
> segnaposto vuoti** creati insieme alla property e mai compilati: `cap`, `email`, `website`,
> `facebook`, `instagram`, `description`, `regulation`, `openHoursFrom/To`, `cancellation`, `pets`,
> `userId` sono `NULL` in ~10.253 righe (98%); `latitude`/`longitude` `NULL` in 3.241 (31%);
> `priceListPDF`/`menuPDF`/`linkTourVirtual`/`whatsApp` di fatto sempre `NULL`. Solo ~180 stabilimenti
> hanno una vetrina realmente popolata. `status = 0` su 10.429 righe (vedi campo). Qualsiasi stima di
> "stabilimenti attivi" fatta contando le righe di `bathouses` è quindi sbagliata di due ordini di
> grandezza.
>
> **`properties` non è nel dump `sb_prod_dump_2026_09_08`** (né dati né schema): i riferimenti incrociati
> a `properties` in questa scheda non sono verificabili sui dati di produzione di questo export.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL`
- **Vincoli**: chiave primaria
- **Relazioni**: referenziato da `properties.propertyOwner`? No — referenziato da `property_search_regions.bathHouseId`
  (applicativo, nessuna FK) e da `configure_maps`/`sectors_rows`/ecc. tramite `propertyId` (non tramite
  `bathHouseId`, salvo `property_search_regions`).
- **Descrizione**: identificativo tecnico. In produzione `id` va da `17` a `10487` per **10.432 righe**
  (poche decine di id mancanti → praticamente nessuna cancellazione).
- **Nullabile**: No
- **Esempio di valori** (prod): `17`, `19`, `20`, `21`, …

### slug
- **Tipo**: `varchar(255)`
- **Vincoli**: **caso più anomalo riscontrato finora nello schema**: la colonna ha **60 vincoli `UNIQUE`
  distinti** (`slug`, `slug_2`, `slug_3`, ... fino a `slug_60`), tutti sulla stessa identica colonna —
  verificato in `SHOW CREATE TABLE`. Non è un problema di dati (in produzione solo 4 righe su 10.432
  hanno `slug` `NULL`, le altre sono distinte) ma un difetto di schema: ogni vincolo
  `UNIQUE` aggiunge un indice fisico separato, quindi questa tabella porta **60 indici ridondanti** sullo
  stesso campo — quasi certamente il risultato di ripetute esecuzioni di una sincronizzazione automatica
  dello schema (`Bathouse.sync({ alter: true })`, presente commentata a riga 152 del model, quindi
  eseguita in passato) che ad ogni riavvio ha ri-tentato di aggiungere il vincolo invece di riconoscere
  quello già esistente.
- **Relazioni**: —
- **Descrizione**: identificatore leggibile usato nell'URL pubblico dello stabilimento, generato
  automaticamente da `sequelize-slugify` (`bathouse.model.js:147-151`) a partire da `bathouseName` **più un
  timestamp** (`getterMethods.timestamp()`, riga 140-144: `new Date().getTime()`), con `overwrite: true`.
  **Conseguenza verificata leggendo il model**: essendo `overwrite: true` e il timestamp calcolato al
  momento del salvataggio (non solo alla creazione), **lo slug viene rigenerato ad ogni `save()`
  dell'entità**, non solo alla creazione — quindi, in linea di principio, ogni volta che un record
  `Bathouse` viene ricaricato e salvato (anche senza che l'operatore tocchi il nome), l'URL pubblico dello
  stabilimento potrebbe cambiare. Non è stato verificato in questa analisi se `updatebathouse`
  (che usa `contentObj.update({...})`, un metodo diverso da `save()`) triggeri effettivamente
  `sequelize-slugify` — meriterebbe un test mirato prima della riscrittura, perché un URL pubblico che
  cambia ad ogni modifica del profilo romperebbe SEO e link condivisi.
- **Nullabile**: Sì (nonostante i 60 vincoli `UNIQUE`)
- **Esempio di valori**: "bar-medusa"

### bathouseName
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: nome commerciale dello stabilimento, sorgente dello `slug`. Raccolto nel form
  "Informazioni" (`Page1BathHouse.js`).
- **Nullabile**: Sì
- **Esempio di valori**: "Stabilimento Balneare Da Vincenzo", "Lido delle Rose Mattinatelle"

### country / address / city / cap / province / region
- **Tipo**: `varchar(255)` ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: indirizzo completo dello stabilimento. `country` è l'unico punto di consumo verificato
  della tabella di lookup [`country`](country.md) (`Page1BathHouse.js`, tramite l'azione
  `getCountryListApi()`) — a differenza dei campi `country` su `properties`/`users`/`customers` (verificati
  tutti come mai realmente popolati dai rispettivi form), qui il campo è effettivamente presentato con una
  select popolata da dati reali. `region`/`province` sono usati anche per la ricerca pubblica
  (`property.controller.js:serchProperty`, già documentato in `properties.md`, filtra anche su `region`).
  **Candidato a enumerazione**: `country` in produzione ha 7 valori distinti su 10.432 righe ("Italia"
  10.253, "Italy" 162, "Greece" 3, "France" 3, "Turkey" 1, "Montenegro" 1, "Croatia" 1; `NULL` su 8) —
  vocabolario chiuso ma con la duplicazione "Italia"/"Italy" (versione italiana vs inglese dello stesso
  paese, 10.253 vs 162); dato che esiste già la tabella di lookup
  [`country`](country.md), si raccomanda di normalizzare con una FK verso quella tabella invece di un ENUM
  fisso, così da unificare le varianti e supportare eventuali traduzioni. Gli altri campi del gruppo
  (`address`, `city`, `cap`, `province`, `region`) restano testo libero non enumerabile (troppi valori
  distinti, dati anche sporchi).
- **Nullabile**: Sì per tutti e sei i campi (`cap` è `NULL` in **10.253 righe su 10.432**, 98% — coerente
  con la natura "stub" della maggioranza delle righe)
- **Esempio di valori** (prod): `country`: "Italia", "Italy"; `address`: "C.da Calalunga", "Viale Roma 34",
  ma anche il placeholder `"-----"`; `city`: "Montauro", "Candolim", "test", `"-----"`; `cap`: "88060",
  "55045", ma anche valori sporchi come "20"; `province`: "Catanzaro", "Lucca", "Goa"; `region`:
  "Calabria", "Toscana", ma anche il valore anomalo "South". Molte righe stub hanno `"-----"` come
  segnaposto in tutti i campi indirizzo.

### mobile / email / website / facebook / instagram / whatsApp
- **Tipo**: `varchar(255)` ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: contatti e link social pubblici. **Bug verificato nel codice**: la funzione di
  creazione (`bathouse.controller.js:63`, `exports.bathouse`) scrive il campo con la chiave
  **`whatsAPP`** (doppia P maiuscola) nel payload passato a `Bathouse.create()`, mentre il model dichiara
  il campo `whatsApp` (P maiuscola singola, `bathouse.model.js:52-55`). Essendo JavaScript case-sensitive
  e Sequelize case-sensitive sui nomi degli attributi, `whatsAPP` **non corrisponde a nessun attributo del
  model**: il valore inviato dal frontend alla creazione viene silenziosamente scartato, e il campo resta
  `NULL`. La funzione di modifica (`updatebathouse`, riga 508) usa invece la chiave corretta
  `whatsApp: req.body.whatsApp` — quindi il bug colpisce **solo la creazione iniziale**, non le modifiche
  successive: uno stabilimento registrato e mai più modificato su questo campo avrebbe `whatsApp = NULL`
  anche se l'operatore lo aveva compilato in fase di registrazione.
- **Nullabile**: Sì per tutti i campi
- **Esempio di valori** (prod): `mobile`: "3924692059", "3356996896", "+917020797989" (formati
  eterogenei, con e senza prefisso internazionale); `email`: "info@lido-onthebeach.it",
  "s.cunsolo@hotmail.it" — `NULL` su **10.253 righe su 10.432** (98%); `website`/`facebook`/`instagram`:
  per lo più `NULL`, quando presenti sia URL completi ("http://lidoulisse.it/") sia frammenti
  ("lidoulisse_", "facebook.com/LidoUlisseSquillace") sia placeholder ("https://facebook.com");
  `whatsApp`: `NULL` in 10.313 righe su 10.432 (99% — coerente col bug `whatsAPP` in creazione descritto
  sopra).

### description / regulation
- **Tipo**: `text` ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: descrizione libera dello stabilimento e regolamento interno, editabili con un editor di
  testo ricco (`DescriptionEditor.js`).
- **Nullabile**: Sì per entrambi
- **Esempio di valori**: testo libero lungo (HTML prodotto dall'editor ricco), es. descrizione dello
  stabilimento e regolamento interno troncati a qualche centinaio di caratteri

### openHoursFrom / openHoursTo
- **Tipo**: `varchar(255)` ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: orari di apertura, campo testo libero (non un vero orario strutturato — nessuna
  validazione di formato verificata nel controller).
- **Nullabile**: Sì per entrambi
- **Esempio di valori** (prod): sui ~180 record compilati il formato è in realtà abbastanza uniforme
  `HH:MM` — `openHoursFrom`: "08:00" (78), "09:00" (38), "08:30" (22), "07:00" (10); `openHoursTo`:
  "20:00" (66), "19:00" (40), "19:30" (17), "24:00" (11) — con qualche stringa vuota e valori improbabili
  ("01:00", "02:00"). `NULL` in 10.253 righe.

### cancellation
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: politica di cancellazione, testo libero, mostrato anche nella risposta pubblica
  `getPropertyIdAndBathhouseFromSlug` (riga 381) insieme a nome/descrizione/foto principale — cioè fa parte
  dei dati minimi mostrati quando si risolve un singolo stabilimento dal suo slug pubblico.
  **Candidato a enumerazione**: sui dati reali di produzione il campo assume 6 valori non vuoti distinti
  (su 179 righe valorizzate, `NULL` su 10.253): "Not Allowed" 104, "Free within 24 hours" 24,
  "Free within 48 hours" 17, "Free within 3 hours" 10, "Free within 12 hours" 9, "Free Within 48 Hours" 1
  (variante di capitalizzazione di quella sopra), più 14 righe a stringa vuota — vocabolario di fatto
  chiuso: da modellare come ENUM a livello DB oppure, meglio, come campo strutturato (es. ore di
  preavviso numeriche) invece di testo libero, eliminando le varianti di capitalizzazione.
- **Nullabile**: Sì
- **Esempio di valori** (prod): "Not Allowed", "Free within 24 hours", "Free within 48 hours", "" (stringa vuota)

### pets
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: politica animali, testo libero (nel dominio catalogo più ampio esiste anche una costante
  frontend `PETS_ALLOWED` con due opzioni `Allowed`/`Not Allowed` — non è stato verificato con certezza se
  questo campo sia vincolato a quella lista chiusa o accetti testo arbitrario lato backend, dato che il
  controller non applica alcuna validazione/enum).
  **Candidato a enumerazione**: confermato sui dati reali di produzione — solo 3 valori distinti su 179
  righe valorizzate ("Not Allowed" 92, "Allowed" 70, stringa vuota 17; `NULL` su 10.253), cioè di fatto
  un booleano testuale: da modellare come ENUM ("Allowed"/"Not Allowed") o booleano a livello DB invece
  di `varchar` libero.
- **Nullabile**: Sì
- **Esempio di valori** (prod): "Not Allowed", "Allowed", "" (stringa vuota)

### photos
- **Tipo**: dichiarato `Sequelize.JSON`; colonna fisica `longtext` con `CHECK json_valid(photos)`
- **Vincoli**: `CHECK json_valid(...)`
- **Relazioni**: —
- **Descrizione**: array di URL foto su S3, stesso pattern già visto su `properties.photos` — alla
  creazione il campo parte come stringa vuota `""` (riga 53, valore non-JSON) e viene sovrascritto
  dall'upload effettivo poco dopo (riga 81-91). In `updatebathouse` la gestione di aggiunta/rimozione foto
  ha una condizione **logicamente sempre falsa** già individuata nell'analisi di criticità generale
  (`if (photos.length && photos.length === 0)`, righe 415 e 429 di questo controller — un valore troncato a
  booleano non può essere contemporaneamente truthy e `=== 0`) — qui riconfermata leggendo il file per
  intero: il ramo "solo foto rimaste dopo cancellazione, nessun nuovo upload" non viene mai eseguito.
- **Nullabile**: Sì
- **Esempio di valori** (prod): `["https://summerbooking.s3.us-east-2.amazonaws.com/images/bathouse/2yjaja1dknhqmxl5.jpeg"]`,
  spesso `[]` (array vuoto) sulle righe stub; `maxlen` osservata ~5,7 KB (gallerie da decine di URL).

### mainPhoto
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: URL della foto di copertina, gestita separatamente dall'array `photos` (upload singolo
  via `UploadBase64OnS3`, non `_multipleUploadOnS3`).
- **Nullabile**: Sì (`NULL`/vuoto su 726 righe in prod, ~7%)
- **Esempio di valori** (prod): URL singolo su S3, stesso pattern di `photos` (es.
  "https://summerbooking.s3.us-east-2.amazonaws.com/images/bathouse/decui1nwkmk9ib9k.jpeg"); osservati
  anche valori con `\r\n` iniziale (dati sporchi).

### latitude / longitude
- **Tipo**: `varchar(255)` ciascuno (non un tipo numerico/geografico)
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: coordinate GPS per la mappa, salvate come testo libero — nessuna validazione di formato
  numerico verificata nel controller.
  **Conferma dai dati reali di produzione**: `NULL` su 3.241 righe su 10.432 (31%); accanto a valori
  puliti come `"38.732560688996415"` si osservano coordinate fuori Italia (es. `-31.20…` / `145.88…` per
  gli stabilimenti indiani/asiatici presenti nel DB) e, come già noto, valori con separatore di migliaia
  errato — conferma empirica dell'assenza di validazione e del tipo di colonna sbagliato (`varchar`
  invece di `decimal`/point).
- **Nullabile**: Sì per entrambi (`NULL` su 3.241 righe)
- **Esempio di valori** (prod): "38.732560688996415" / "16.548328399658203" (coppia pulita);
  "-31.209053001993215" / "145.8894702706379" (stabilimento non italiano).

### services
- **Tipo**: dichiarato `Sequelize.JSON`; colonna fisica `longtext` con `CHECK json_valid(services)`
- **Vincoli**: `CHECK json_valid(...)`
- **Relazioni**: applicativa verso la tabella [`services`](services.md) — **ma non per riferimento, per
  copia integrale**
- **Descrizione**: **finding rilevante verificato sui dati**: questo campo non contiene un elenco di ID di
  servizi selezionati, ma una **copia completa di ogni riga della tabella master `services`**
  (`id`, `serviceName`, `serviceItalian`, `serviceIcon`, `createdAt`, `updatedAt`, `status` — tutti i campi
  della tabella master, duplicati), con l'unica differenza reale per stabilimento nel flag `isChecked`
  (`true`/`false`, quali servizi offre quello specifico stabilimento). Confermato lato frontend:
  `ServicesList.js` (righe 20-29) recupera l'elenco master dei servizi via API, lo incrocia per
  `serviceName` con l'array già salvato sul bathouse per riportare `isChecked`, e alla modifica risalva
  **l'intero array unito** (non solo gli id selezionati) nel campo `services`. Effetto pratico: se la
  tabella master `services` cambia (nuovo servizio aggiunto, icona aggiornata, nome corretto), **tutti gli
  stabilimenti che non vengono re-salvati dopo la modifica restano con la copia vecchia**, potenzialmente
  disallineata dal master — un'istanza concreta della duplicazione dati via JSON già segnalata a livello
  generale in [01-criticita-e-indicazioni-backend.md](../../progettazione/01-criticita-e-indicazioni-backend.md).
- **Nullabile**: Sì (1 sola riga `NULL` su 10.432; 10 righe con `[]`). In produzione **10.418 righe su
  10.432 hanno letteralmente la stessa stringa** (copia identica del master `services` con il primo
  servizio "Accesible shower" / "Doccia per disabili") → conferma che `isChecked` non viene quasi mai
  toccato: la stragrande maggioranza delle righe porta la copia di default del master, mai personalizzata.
- **Esempio di valori** (prod): array con un oggetto per servizio master, es. `{"id": 1, "serviceName":
  "Accesible shower", "serviceItalian": "Doccia per disabili", "serviceIcon": "https://…", "isChecked":
  …}` — il sotto-campo `isChecked` (`true`/`false`) è l'unico che varia realmente da uno stabilimento
  all'altro, e di fatto raramente.

### userId
- **Tipo**: `int`
- **Vincoli**: nessuno (nessuna FK verso `users.id`, solo indice implicito assente — nessun indice
  dedicato trovato su questa colonna)
- **Relazioni**: applicativa verso `users.id`, non vincolata
- **Descrizione**: proveniente da `req.body.userId` alla creazione/modifica — non è stato individuato con
  certezza, nel tempo disponibile per questa scheda, quale utente venga effettivamente scritto qui rispetto
  a `properties.propertyOwner` (stesso concetto? un utente diverso, es. chi ha fisicamente compilato il
  form?) — i due campi non vengono mai letti insieme nello stesso controller in questa analisi. Da chiarire
  col committente prima della riscrittura se sono ridondanti o hanno scopi diversi.
- **Nullabile**: Sì. **In produzione `userId` è `NULL` in 10.254 righe su 10.432** (98%) — è valorizzato
  quasi solo sulle ~180 vetrine reali. Range dei valori non nulli: `2` … `6100`.
- **Esempio di valori** (prod): `2`, `76`, `85`, `90`.

### createdAt / updatedAt
- **Tipo**: `datetime`, nessun default a livello DB
- **Relazioni**: —
- **Descrizione**: standard.
- **Nullabile**: Sì per entrambi
- **Esempio di valori** (prod): `createdAt` da `2021-03-08 11:44:53` a `2026-08-01 09:14:39`; `updatedAt`
  fino a `2026-09-07 14:12:21`.

### status
- **Tipo**: `int`
- **Vincoli**: nessuno; indice dedicato (`bathouses_status`)
- **Relazioni**: —
- **Descrizione**: **campo verificato come probabilmente vestigiale**: impostato a `0` sia alla creazione
  (`bathouse.controller.js:59`) sia — significativamente — **ad ogni modifica** (`updatebathouse:505`,
  `status: 0` scritto incondizionatamente, non un valore derivato da `req.body`). **Nessun punto del
  backend filtra o verifica `bathouses.status`** per decidere la visibilità pubblica dello stabilimento: i
  filtri di visibilità/approvazione trovati in `serchProperty`, `myfavourite` e altri controller filtrano
  sempre su **`properties.status`** (già documentato in `properties.md`), mai su `bathouses.status`.
  **Confermato dai dati reali di produzione** (`sb_prod_dump_2026_09_08`, 10.432 righe): `status = 0` su
  **10.429 righe**, `status = 5` su 2 righe, `status = 1` su 1 riga. Il campo è quindi di fatto costante a
  `0`; le 3 eccezioni sono isolate e non smentiscono l'ipotesi di campo vestigiale. Nessun percorso del
  codice lo imposta a `1`/`5` — verosimilmente scritture manuali. Da verificare col committente se è un
  residuo di una vecchia logica di approvazione separata (prima della centralizzazione su
  `properties.status`) prima di deciderne il destino nella riscrittura.
- **Nullabile**: Sì
- **Esempio di valori** (prod): `0` (10.429), `5` (2), `1` (1)

### propertyId
- **Tipo**: `int`
- **Vincoli**: **FK reale** verso `properties.id` (`bathouses_ibfk_1`, `ON UPDATE CASCADE`) — una delle 12
  relazioni vincolate del progetto, già elencata in [`properties.md`](properties.md#tabelle-collegate)
- **Relazioni**: FK reale
- **Descrizione**: stabilimento proprietario. Ogni query di questo controller usa `propertyId` (non `id`)
  come chiave di ricerca per trovare "la" bathouse di uno stabilimento — coerente con la relazione 1:1 di
  fatto.
- **Nullabile**: Sì (`NULL` su 1 sola riga in prod)
- **Esempio di valori** (prod): `1`, `15`, `32`, `85` — range `1` … `10856`.

### priceListPDF / menuPDF
- **Tipo**: `varchar(255)` ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: URL di documenti PDF caricati (listino prezzi cartaceo/menu del bar), gestiti da
  `UploadPdfFile.js` lato frontend.
- **Nullabile**: Sì per entrambi. **In produzione sono valorizzati su appena ~6 righe ciascuno** (10.426
  `NULL` su 10.432) → campo di fatto inutilizzato.
- **Esempio di valori** (prod): rari URL S3 (es. `…/file/Listino-Baia-2025.pdf`, `…/pdf/Brochure.pdf`),
  altrimenti `NULL`.

### likeCount
- **Tipo**: `int`, default `0`
- **Vincoli**: nessuno
- **Relazioni**: applicativa verso [`property_like_logs`](property_like_logs.md) (dettaglio dei singoli
  "mi piace" per utente consumer), incontrata in `property.controller.js:getPropertyLikes` /
  `user.controller.js:myfavourite` / `propertylikelogs.controller.js:likeLogDetails`.
- **Descrizione**: contatore "mi piace" pubblico, **totale denormalizzato** di
  [`property_like_logs`](property_like_logs.md). Incrementato (`likeCount + 1`) alla creazione di una
  riga di like e decrementato (`likeCount - 1`) alla sua cancellazione da
  `propertylikelogs.controller.js:likeLogDetails` — aggiornamento non transazionale e senza ricalcolo da
  `COUNT(*)`, quindi soggetto a drift (vedi criticità nella scheda di `property_like_logs`).
- **Nullabile**: Sì
- **Esempio di valori** (prod): `0` su 10.338 righe; valori piccoli fino a ~15 su decine di righe; un
  paio di outlier (`77`, `493`) e — sintomo del drift non transazionale citato sopra — **valori
  negativi** `-1` e `-2` (decrementi sotto zero: like rimosso più volte del dovuto).

### isTwoFactorEnabled
- **Tipo**: `int` (booleano 0/1), default `0`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: **campo già incontrato in `users.md`**: è il vero interruttore del 2FA per tutto lo
  staff dello stabilimento (`auth.controller.js:signin`/`sendToken` lo leggono per decidere se attivare
  l'OTP via email al login) — confermato qui che vive su `bathouses`, non su `properties` né su `users`,
  nonostante concettualmente sia una impostazione di sicurezza dell'account/stabilimento più che della
  "vetrina pubblica". Aggiornabile tramite `auth.controller.js:updateAuthenticationFlag`
  (`Bathouse.update({ isTwoFactorEnabled }, { where: { userId } })` — **da notare**: qui il filtro è per
  `userId`, non per `propertyId`, l'unico punto verificato in tutto il controller che usa `userId` come
  chiave invece di `propertyId`).
- **Nullabile**: Sì
- **Esempio di valori** (prod): `0` su 10.431 righe, `1` su **1 sola riga** — il 2FA staff è attivo per
  un unico stabilimento in tutta la produzione.

### isTwoFactorAuthentication
- **Tipo**: `int` (booleano 0/1), default `0`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: colonna presente nello schema reale del DB ma **non citata da nessun model Sequelize né
  controller individuato in questa analisi** (non è dichiarata in `bathouse.model.js`, a differenza di
  `isTwoFactorEnabled`) — non era documentata nella versione precedente di questa scheda. Sembra un
  doppione mai realmente collegato al vero interruttore 2FA, che è invece
  [`isTwoFactorEnabled`](#istwofactorenabled) (quello letto/scritto da `auth.controller.js`). **Verificato
  sui dati reali di produzione**: il valore è `0` su **tutte le 10.432 righe**, senza eccezioni — a
  differenza di `isTwoFactorEnabled`, che ha 1 riga a `1`. Coerente con un campo creato per errore (es. un refuso di naming durante
  una sincronizzazione automatica dello schema, vista la ricorrenza di problemi simili già documentata per
  `slug`) e mai più utilizzato.
- **Nullabile**: Sì
- **Esempio di valori** (prod): `0` — unico valore osservato su tutte le 10.432 righe.
- **Deprecato**: Sì — colonna duplicata mai referenziata da model/controller/frontend, sempre `0`; distinta
  dal campo realmente usato `isTwoFactorEnabled`

### linkTourVirtual
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: URL di un tour virtuale (es. Matterport/360°) dello stabilimento, mostrato pubblicamente.
- **Nullabile**: Sì
- **Esempio di valori**: —
- **Produzione**: **`NULL` su tutte le 10.432 righe** (`sb_prod_dump_2026_09_08`) — colonna mai
  valorizzata: la feature "tour virtuale" non è mai stata usata. Candidata alla rimozione.

---

## Osservazioni per la riscrittura

- **Vincoli `UNIQUE` duplicati (60x) su `slug`**: da consolidare in un singolo indice univoco nel nuovo
  schema; sintomo di migrazioni automatiche (`sync({ alter: true })`) mai sostituite da migration
  versionate — coerente con la raccomandazione già data altrove in questo progetto.
- **Slug rigenerato ad ogni salvataggio**: se confermato (va testato), è un problema di stabilità URL da
  risolvere esplicitamente nella riscrittura — lo slug pubblico di uno stabilimento non dovrebbe cambiare
  spontaneamente. Generarlo una sola volta alla creazione, o rigenerarlo solo su richiesta esplicita con
  redirect dal vecchio URL.
- **`whatsApp` mai salvato alla creazione** per il typo `whatsAPP`/`whatsApp`: **confermato dai dati di
  produzione** — `whatsApp` è `NULL` in 10.313 righe su 10.432 (99%); i pochi valori presenti sono
  coerenti con l'ipotesi che siano stati inseriti da `updatebathouse` (che usa la chiave corretta), non
  in fase di creazione. Correggere nella riscrittura.
- **`services` duplicato per copia integrale invece che per riferimento**: nella riscrittura va sostituito
  con una vera relazione N:N `bathouse_id ↔ service_id` (tabella di giunzione, eventualmente con solo il
  flag di attivazione), non una copia JSON dell'intera tabella master.
- **`status` probabilmente da eliminare o da chiarire**: sempre `0`, mai letto per decisioni applicative
  nel codice analizzato — verificare col committente se ha ancora un ruolo prima di riportarlo.
- **`userId` vs `properties.propertyOwner`**: chiarire se sono lo stesso concetto duplicato o due concetti
  distinti prima di modellarli nella riscrittura.
- **`isTwoFactorAuthentication` da rimuovere**: colonna reale nel DB, sempre `0` su tutte le 10.432 righe
  di produzione, mai referenziata da model/controller/frontend — doppione morto di `isTwoFactorEnabled`
  (quello realmente usato), da non riportare nella riscrittura.
- **La tabella è per ~98% righe-segnaposto**: in produzione solo ~180 righe su 10.432 hanno una vetrina
  compilata. Nella riscrittura la "vetrina pubblica" andrebbe creata **on-demand** (alla prima
  compilazione) e non automaticamente insieme alla property; in migrazione, le righe stub (tutti i campi
  descrittivi `NULL`, `status = 0`) vanno scartate o marcate come non pubblicate.
