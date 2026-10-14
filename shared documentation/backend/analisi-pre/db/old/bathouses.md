# Tabella `bathouses`

> Fonti usate per questa scheda: schema reale della tabella (`SHOW CREATE TABLE`/`information_schema`,
> dati reali delle 100 righe presenti), il model Sequelize
> [`back-pre/app/models/bathouse.model.js`](../../back-pre/app/models/bathouse.model.js), il controller
> [`bathouse.controller.js`](../../back-pre/app/controllers/bathouse.controller.js) (letto per intero), le
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

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL`
- **Vincoli**: chiave primaria
- **Relazioni**: referenziato da `properties.propertyOwner`? No — referenziato da `property_search_regions.bathHouseId`
  (applicativo, nessuna FK) e da `configure_maps`/`sectors_rows`/ecc. tramite `propertyId` (non tramite
  `bathHouseId`, salvo `property_search_regions`).
- **Descrizione**: identificativo tecnico. `AUTO_INCREMENT` a 10487 con 100 righe campionate.
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### slug
- **Tipo**: `varchar(255)`
- **Vincoli**: **caso più anomalo riscontrato finora nello schema**: la colonna ha **60 vincoli `UNIQUE`
  distinti** (`slug`, `slug_2`, `slug_3`, ... fino a `slug_60`), tutti sulla stessa identica colonna —
  verificato in `SHOW CREATE TABLE`. Non è un problema di dati (nel campione locale gli slug sono
  effettivamente tutti distinti, 100 valori distinti su 100 righe) ma un difetto di schema: ogni vincolo
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
  **Candidato a enumerazione**: `country` mostra solo 6 valori distinti osservati su 3899 righe ("Italia"
  3818, "Italy" 66, null 8, "France" 3, "Greece" 2, "Montenegro" 1, "Turkey" 1) — vocabolario relativamente
  chiuso ma con una duplicazione evidente "Italia"/"Italy"; dato che esiste già la tabella di lookup
  [`country`](country.md), si raccomanda di normalizzare con una FK verso quella tabella invece di un ENUM
  fisso, così da unificare le varianti e supportare eventuali traduzioni. Gli altri campi del gruppo
  (`address`, `city`, `cap`, `province`, `region`) restano testo libero non enumerabile (troppi valori
  distinti, dati anche sporchi).
- **Nullabile**: Sì per tutti e sei i campi (`cap` è quasi sempre `NULL`: 3818 righe su 3899)
- **Esempio di valori**: `country`: "Italia", "Italy"; `address`: "Via Fiumicello 49", "Corso Armando Diaz
  34"; `city`: "71010 Peschici", "87028 Praia a Mare" (nota: contiene CAP+città insieme, non solo il nome
  della città); `cap`: "55045", "88900", ma anche valori sporchi come "20", "MH"; `province`: "Salerno",
  "Trapani", "Foggia"; `region`: "Calabria", "Abruzzo", "Marche", ma anche il valore anomalo "South"

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
- **Esempio di valori**: `mobile`: "377 384 9852", "081 808 7637"; `email`: "bagnoargo@iol.it",
  "dev-beach12@yopmail.com" (`NULL` su 3818 righe su 3899); `website`/`facebook`/`instagram`: per lo più
  vuoti/`NULL`, esempio reale quando presente: "www.lidostellamaris-villapiana.com/"; `whatsApp`: quasi
  sempre `NULL` (3895 righe su 3899)

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
- **Esempio di valori**: `openHoursFrom`: "8:00 AM", "8:00", "08:00"; `openHoursTo`: "20:00", "19:00",
  "20:00 pm" — formati eterogenei osservati, coerente con l'assenza di validazione segnalata sopra

### cancellation
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: politica di cancellazione, testo libero, mostrato anche nella risposta pubblica
  `getPropertyIdAndBathhouseFromSlug` (riga 381) insieme a nome/descrizione/foto principale — cioè fa parte
  dei dati minimi mostrati quando si risolve un singolo stabilimento dal suo slug pubblico.
  **Candidato a enumerazione**: sui dati reali il campo assume solo 5 valori non vuoti distinti su 3899
  righe ("Free within 24 hours" 24, "Free Within 48 Hours" 11, "Not Allowed" 9, "Free within 12 hours" 9,
  "Free within 3 hours" 4, più 24 righe a stringa vuota) — vocabolario di fatto chiuso: da modellare come
  ENUM a livello DB oppure, meglio, come campo strutturato (es. ore di preavviso numeriche) invece di testo
  libero, evitando anche le variazioni di capitalizzazione già osservate ("Free within" vs "Free Within").
- **Nullabile**: Sì
- **Esempio di valori**: "Free within 24 hours", "Free Within 48 Hours", "Not Allowed", "" (stringa vuota)

### pets
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: politica animali, testo libero (nel dominio catalogo più ampio esiste anche una costante
  frontend `PETS_ALLOWED` con due opzioni `Allowed`/`Not Allowed` — non è stato verificato con certezza se
  questo campo sia vincolato a quella lista chiusa o accetti testo arbitrario lato backend, dato che il
  controller non applica alcuna validazione/enum).
  **Candidato a enumerazione**: confermato sui dati reali — solo 3 valori distinti su 3899 righe ("Allowed"
  37, stringa vuota 29, "Not Allowed" 15), cioè di fatto un booleano testuale: da modellare come ENUM
  ("Allowed"/"Not Allowed") o booleano a livello DB invece di `varchar` libero.
- **Nullabile**: Sì
- **Esempio di valori**: "Allowed", "Not Allowed", "" (stringa vuota)

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
- **Esempio di valori**: `["https://summerbooking.s3.us-east-2.amazonaws.com/images/bathouse/fuu7ndv8lk9rt5wd.jpeg"]`

### mainPhoto
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: URL della foto di copertina, gestita separatamente dall'array `photos` (upload singolo
  via `UploadBase64OnS3`, non `_multipleUploadOnS3`).
- **Nullabile**: Sì
- **Esempio di valori**: URL singolo su S3, stesso pattern di `photos` (es.
  "https://summerbooking.s3.us-east-2.amazonaws.com/images/bathouse/fuu7ndv8lk9rt5wd.jpeg")

### latitude / longitude
- **Tipo**: `varchar(255)` ciascuno (non un tipo numerico/geografico)
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: coordinate GPS per la mappa, salvate come testo libero — nessuna validazione di formato
  numerico verificata nel controller.
  **Correzione/conferma dai dati reali**: osservato almeno un valore palesemente corrotto,
  `"41.239.975.271.715.400"` (punti multipli, probabile bug di conversione/locale nell'origine dati),
  accanto a valori puliti come `"38.732560688996415"` — conferma empirica dell'assenza di validazione già
  segnalata sopra.
- **Nullabile**: Sì per entrambi
- **Esempio di valori**: "38.732560688996415" (valore pulito); "41.239.975.271.715.400" (valore
  sporco/corrotto osservato)

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
  generale in [01-criticita-e-indicazioni-backend.md](../../docs/01-criticita-e-indicazioni-backend.md).
- **Nullabile**: Sì (1 sola riga `NULL` su 3899)
- **Esempio di valori**: array con un oggetto per servizio master, es. `{"id": 3, "serviceName": "WiFi",
  "serviceItalian": "WiFi", "serviceIcon": "wifi.svg", "isChecked": true}` — il sotto-campo `isChecked`
  (`true`/`false`) è l'unico che varia realmente da uno stabilimento all'altro

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
- **Nullabile**: Sì
- **Esempio di valori**: "505"

### createdAt / updatedAt
- **Tipo**: `datetime`, nessun default a livello DB
- **Relazioni**: —
- **Descrizione**: standard.
- **Nullabile**: Sì per entrambi
- **Esempio di valori**: "2023-05-12 10:15:00", "2024-11-03 09:42:17"

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
  **Confermato dai dati**: tutte le 100 righe del campione hanno `status = 0` — coerente con un campo che
  non viene mai impostato a un valore diverso da nessun percorso del codice. Da verificare col committente
  se è un residuo di una vecchia logica di approvazione separata (prima che venisse centralizzata su
  `properties.status`) prima di deciderne il destino nella riscrittura.
  **Aggiornamento dal DB reale** (non solo il campione locale di 100 righe): su tutte le 3899 righe, 3898
  hanno `status = 0` e **una sola riga ha `status = 1`** — il campo quindi non è letteralmente costante come
  suggerito dal campione, ma l'unica eccezione osservata è isolata e non basta a smentire l'ipotesi di campo
  vestigiale.
- **Nullabile**: Sì
- **Esempio di valori**: 0 (quasi sempre, 3898 righe su 3899), 1 (una sola riga)

### propertyId
- **Tipo**: `int`
- **Vincoli**: **FK reale** verso `properties.id` (`bathouses_ibfk_1`, `ON UPDATE CASCADE`) — una delle 12
  relazioni vincolate del progetto, già elencata in [`properties.md`](properties.md#tabelle-collegate)
- **Relazioni**: FK reale
- **Descrizione**: stabilimento proprietario. Ogni query di questo controller usa `propertyId` (non `id`)
  come chiave di ricerca per trovare "la" bathouse di uno stabilimento — coerente con la relazione 1:1 di
  fatto.
- **Nullabile**: Sì
- **Esempio di valori**: "2689"

### priceListPDF / menuPDF
- **Tipo**: `varchar(255)` ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: URL di documenti PDF caricati (listino prezzi cartaceo/menu del bar), gestiti da
  `UploadPdfFile.js` lato frontend.
- **Nullabile**: Sì per entrambi (quasi sempre `NULL`)
- **Esempio di valori**: URL PDF quando presenti (es. link S3 al listino/menu), altrimenti `NULL` nella
  maggioranza delle righe

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
- **Esempio di valori**: 0, 1, 2, 4

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
- **Esempio di valori**: 0 (quasi sempre, 3898 righe su 3899), 1

### isTwoFactorAuthentication
- **Tipo**: `int` (booleano 0/1), default `0`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: colonna presente nello schema reale del DB ma **non citata da nessun model Sequelize né
  controller individuato in questa analisi** (non è dichiarata in `bathouse.model.js`, a differenza di
  `isTwoFactorEnabled`) — non era documentata nella versione precedente di questa scheda. Sembra un
  doppione mai realmente collegato al vero interruttore 2FA, che è invece
  [`isTwoFactorEnabled`](#istwofactorenabled) (quello letto/scritto da `auth.controller.js`). **Verificato
  sui dati reali**: il valore è `0` su **tutte** le 3899 righe, senza eccezioni — a differenza di
  `isTwoFactorEnabled`, che varia. Coerente con un campo creato per errore (es. un refuso di naming durante
  una sincronizzazione automatica dello schema, vista la ricorrenza di problemi simili già documentata per
  `slug`) e mai più utilizzato.
- **Nullabile**: Sì
- **Esempio di valori**: 0 (unico valore osservato su tutte le 3899 righe)
- **Deprecato**: Sì — colonna duplicata mai referenziata da model/controller/frontend, sempre `0`; distinta
  dal campo realmente usato `isTwoFactorEnabled`

### linkTourVirtual
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: URL di un tour virtuale (es. Matterport/360°) dello stabilimento, mostrato pubblicamente.
- **Nullabile**: Sì
- **Esempio di valori**: quasi sempre `NULL`; URL quando presente (es. link Matterport)
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

---

## Osservazioni per la riscrittura

- **Vincoli `UNIQUE` duplicati (60x) su `slug`**: da consolidare in un singolo indice univoco nel nuovo
  schema; sintomo di migrazioni automatiche (`sync({ alter: true })`) mai sostituite da migration
  versionate — coerente con la raccomandazione già data altrove in questo progetto.
- **Slug rigenerato ad ogni salvataggio**: se confermato (va testato), è un problema di stabilità URL da
  risolvere esplicitamente nella riscrittura — lo slug pubblico di uno stabilimento non dovrebbe cambiare
  spontaneamente. Generarlo una sola volta alla creazione, o rigenerarlo solo su richiesta esplicita con
  redirect dal vecchio URL.
- **`whatsApp` mai salvato alla creazione** per il typo `whatsAPP`/`whatsApp`: correggere nella riscrittura
  e verificare se in produzione esistono stabilimenti con questo campo mancante solo per questo bug (non
  verificabile dal campione locale, che ha 0 valori non-null su `whatsApp` — coerente col bug ma non
  conclusivo senza un confronto con altri ambienti).
- **`services` duplicato per copia integrale invece che per riferimento**: nella riscrittura va sostituito
  con una vera relazione N:N `bathouse_id ↔ service_id` (tabella di giunzione, eventualmente con solo il
  flag di attivazione), non una copia JSON dell'intera tabella master.
- **`status` probabilmente da eliminare o da chiarire**: sempre `0`, mai letto per decisioni applicative
  nel codice analizzato — verificare col committente se ha ancora un ruolo prima di riportarlo.
- **`userId` vs `properties.propertyOwner`**: chiarire se sono lo stesso concetto duplicato o due concetti
  distinti prima di modellarli nella riscrittura.
- **`isTwoFactorAuthentication` da rimuovere**: colonna reale nel DB, sempre `0` su tutte le 3899 righe, mai
  referenziata da model/controller/frontend — doppione morto di `isTwoFactorEnabled` (quello realmente
  usato), da non riportare nella riscrittura.
