# Tabella `properties`

> Fonti usate per questa scheda: schema reale della tabella sul DB locale (`local_sb`, MySQL 9.7.1, verificato
> con `SHOW CREATE TABLE` e `information_schema`), il model Sequelize
> [`back-pre/app/models/property.model.js`](../../back-pre/app/models/property.model.js), le associazioni
> dichiarate in [`back-pre/app/models/index.js`](../../back-pre/app/models/index.js), i controller che
> leggono/scrivono la tabella (in particolare
> [`property.controller.js`](../../back-pre/app/controllers/property.controller.js),
> [`sysadmin.controller.js`](../../back-pre/app/controllers/sysadmin.controller.js),
> [`auth.controller.js`](../../back-pre/app/controllers/auth.controller.js),
> [`cron-disableproperty.js`](../../back-pre/cronjob/cron-disableproperty.js)), i service applicativi
> collegati a Fiskaly ([`fiskalyAccountService.js`](../../back-pre/app/Service/fiskalyAccountService.js),
> [`fiskalyReceiptManagerService.js`](../../back-pre/app/Service/fiskalyReceiptManagerService.js)) e al
> contratto ([`contractConditionService.js`](../../back-pre/app/Service/contractConditionService.js)), lo
> `swagger.yaml` di progetto dove i controller non erano sufficienti a chiarire un contratto, e, per i campi
> JSON, sia dati reali di esempio presi dal DB sia i **due frontend** presenti nel repository, che ne
> mostrano la struttura completa e il significato dei singoli sotto-campi:
> - `front-pre` — il gestionale usato dal gestore dello stabilimento (impostazioni, prenotazioni via
>   operatore, configurazione), in particolare
>   [`front-pre/src/components/Settings/OnlineBookingIndex.js`](../../front-pre/src/components/Settings/OnlineBookingIndex.js)
>   e componenti collegati;
> - `front-customer-pre` — il sito pubblico di prenotazione rivolto al cliente finale (ricerca stabilimenti,
>   dettaglio, checkout/pagamento), in particolare la cartella
>   [`front-customer-pre/src/components/consumer/`](../../front-customer-pre/src/components/consumer)
>   (assente in `front-pre`) e i servizi
>   [`ArithmaticService.js`](../../front-customer-pre/src/services/ArithmaticService.js)/
>   [`CommonServices.js`](../../front-customer-pre/src/services/CommonServices.js). Il resto
>   dell'albero di `front-customer-pre` risulta in larga parte una copia dello stesso gestionale presente in
>   `front-pre` (stessa cartella `Settings/`, `onlineReservation/`, ecc.) — **ma non identica**: sono state
>   trovate differenze concrete (vedi `photos` più sotto), quindi dove rilevante le due copie sono state
>   confrontate riga per riga invece di essere assunte equivalenti.
> - `front-app-pre` — l'app mobile React Native (cartella `app/`, stesso backend, endpoint
>   `Api.registerProperty` → `POST /api/public/property`, la stessa `register()` usata dal web), in
>   particolare `app/screens/Dashboard/ListOfProperty.screen.js` (registrazione stabilimento da app) e
>   `app/screens/Dashboard/BookNow.screen.js`/`SearchSingleLocation.screen.js` (consumo delle impostazioni di
>   prenotazione online).
> - `front-modern-pre` — **non un quarto sito/app separato**, ma una libreria React 19/TypeScript (Vite,
>   nome pacchetto `modern-lib`) pensata per essere iniettata dentro `front-pre` (vedi
>   `front-modern-pre/docs/general.md`) come via di migrazione incrementale del legacy: espone una funzione
>   `injectModernLib` che monta singole applicazioni React 19 in `<div>` dedicati dentro le pagine React 16
>   esistenti. Contiene la **riscrittura moderna, già in corso, di due moduli direttamente rilevanti per
>   `properties`**: `src/pages/Fiscalization/` (il pannello di configurazione Fiskaly — sostituisce quanto
>   descritto sopra a partire da `getFiskalyFlag`/`updateFiskalyFlag`) e `src/app/Conditions/` +
>   `src/modules/ConditionsUpdate/` (il modulo "Condizioni contrattuali" collegato a `contractDocumentsId`).
>   Essendo codice del target architetturale, è una fonte particolarmente affidabile su **cosa i campi
>   dovrebbero significare** oltre a cosa fanno oggi nel legacy.
>
> **Nota di contesto**: `properties` rappresenta l'anagrafica principale di uno **stabilimento balneare**
> (bathhouse operator) all'interno della piattaforma multi-tenant SummerBooking. Non va confusa con la
> tabella `bathouses`, che rappresenta invece la "vetrina pubblica" dello stabilimento (indirizzo, nome
> commerciale, descrizione, immagini pubbliche, ecc.) — una `property` ha tipicamente **una** `bathouse`
> associata (relazione 1:1 nella pratica, modellata come 1:N a livello di associazione Sequelize).
> `properties` conserva invece i dati "gestionali/di piattaforma": stato di approvazione, contatti interni,
> configurazione delle prenotazioni online, commissioni, integrazione fiscale.

---

## Campi

### id
- **Tipo**: `int`
- **Vincoli**: chiave primaria, campo autoincrementale (`AUTO_INCREMENT`), `NOT NULL`
- **Relazioni**: referenziato (a livello applicativo/Sequelize, **non sempre con vincolo FK reale nel DB** —
  vedi §"Tabelle collegate") da decine di altre tabelle tramite le colonne `propertyId`/`property_id`
- **Descrizione**: identificativo tecnico univoco dello stabilimento/tenant. È la chiave usata in tutto il
  sistema per scopare (filtrare) dati per singolo stabilimento (prenotazioni, listini, utenti, configurazioni,
  ecc.). Nel DB locale l'`AUTO_INCREMENT` corrente è a 10856 pur avendo la tabella solo 100 righe di dati di
  esempio, segno di molte cancellazioni/test nel tempo nell'ambiente da cui è stata esportata la copia.
- **Nullabile**: No
- **Esempio di valori**: `1, 2, 3, ..` (autoincrementale; verifica più recente sul DB di sviluppo: 4135 righe
  presenti, campione molto più ampio delle 100 righe usate nella stesura originale di questa scheda)

### propertyName
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno a livello DB (`NULL` ammesso, nessun `UNIQUE`)
- **Relazioni**: —
- **Default**: nessuno (`NULL`)
- **Descrizione**: nome dello stabilimento, raccolto dal campo "Nome struttura" del form
  `RegisterProperty.js` (stato `PropertyName`, riga 32/213-217) e inviato come `propertyName` nel payload di
  `POST /api/public/property` (riga 101) → `property.controller.js:register` (riga 67). È il nome
  "amministrativo"; il nome commerciale mostrato al pubblico vive invece nel campo `bathouseName` della
  tabella `bathouses`. Nella pratica i due valori coincidono quasi sempre, ma sono colonne indipendenti
  gestite in punti diversi dell'applicazione. **Verificato**: dopo la registrazione questo campo non risulta
  più modificabile da nessun endpoint raggiungibile dal frontend disponibile — l'unico altro punto che scrive
  sulla tabella `properties` con dati anagrafici, `sysadmin.controller.js:updateproperty` (righe 508-535),
  aggiorna `city`, `mobile`, `email`, `status`, `adminComment`, `propertyOwner`, `commissionAmount`, ma **non**
  `propertyName`.
- **Nullabile**: Sì
- **Esempio di valori**: `"Lido Playa Blanca"`, `"Stabilimento Balneare La Conchiglia"`

### city
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Default**: nessuno
- **Descrizione**: città, raccolta dal campo "Città" del form `RegisterProperty.js` (stato `City`, righe
  33/219-228) e inviata come `city` nel payload di registrazione → `register` (riga 68). Duplica
  concettualmente il campo `city` della tabella `bathouses` (usato per la ricerca pubblica, vedi
  `serchProperty`), ma non c'è alcuna sincronizzazione automatica tra i due. **Verificato**: a differenza di
  `propertyName`, questo campo **è** riscrivibile in un secondo momento — ma solo dal system admin, tramite
  `sysadmin.controller.js:updateproperty` (riga 527); nessun endpoint raggiungibile dal frontend disponibile
  in questo repository permette al gestore stesso di modificarlo dopo la registrazione.
- **Nullabile**: Sì
- **Esempio di valori**: `"63822 Porto San Giorgio"` (nota: il valore reale osservato contiene CAP e città
  concatenati nello stesso campo testo libero, non solo il nome della città)

### country
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Default**: nessuno
- **Descrizione**: campo pensato per il paese dello stabilimento, letto lato server da `req.body.country`
  (`register`, riga 69) — ma **nessuno dei tre form di registrazione oggi in uso lo raccoglie mai
  dall'utente**. Verificato su tutti e tre gli entry point che chiamano `POST /api/public/property`:
  `front-pre/src/components/property/RegisterProperty.js:103`, la sua copia in
  `front-customer-pre` (stessa riga), e `front-app-pre/app/screens/Dashboard/ListOfProperty.screen.js:107`
  inviano tutti letteralmente `country: ''` hardcoded — nessuno dei tre form (web o app) ha un campo "paese"
  nella UI. **Confermato dai dati**: nel DB locale 91 righe su 100 hanno `country = ''`, solo 2 hanno un
  valore reale (`'Italia'`, presumibilmente inserito a mano in un secondo momento, es. da un editing
  manuale/sysadmin), 7 sono `NULL` (probabilmente righe precedenti all'introduzione di questo campo o
  inserite fuori da questi endpoint). Nessun riferimento alla tabella `country` (usata altrove nel sistema
  per i prefissi telefonici/selezione paese strutturata) — è un campo testo libero, di fatto sempre vuoto
  da ogni punto di scrittura verificato.
  **Aggiornamento dati (verifica più recente su un campione più ampio del DB di sviluppo, 4135 righe)**: solo
  **4 valori distinti** osservati in tutta la tabella: stringa vuota `""` (275 righe), `"Italia"` (3818,
  stragrande maggioranza), `"india"` (31 righe, minuscolo — dato di test, non un paese realmente gestito da
  nessun flusso di registrazione verificato) e `"Italy"` (10 righe, variante inglese non normalizzata rispetto
  a `"Italia"`). Conferma, su scala più ampia, che il campo è di fatto quasi-fisso a `"Italia"` pur restando
  testo libero non validato.
- **Nullabile**: Sì
- **Esempio di valori**: `"Italia"`, `""`, `"india"` (dato di test), `"Italy"`
- **Candidato a enumerazione**: il vocabolario osservato è di fatto quasi fisso (`Italia`/vuoto, con
  `india`/`Italy` come rumore di test/variante non normalizzata), ma trattandosi di un paese va collegato
  alla tabella `country` **già esistente nel progetto** (usata altrove per i prefissi telefonici) tramite
  una FK reale, non un `ENUM` DB fisso — un vero elenco paesi ha bisogno di crescere/tradursi e la tabella
  di lookup esiste già, mentre oggi questo campo non la referenzia affatto.

### fullName
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Default**: nessuno
- **Descrizione**: nome e cognome della persona di riferimento/proprietario, raccolto dal campo
  "Nome e Cognome" del form `RegisterProperty.js` (stato `FullName`, righe 34/229-240) e inviato come
  `fullName` (`register`, riga 70). È un dato di contatto interno, distinto dal nome dello stabilimento.
  **Verificato**: come `propertyName`, non risulta più modificabile da nessun endpoint raggiungibile dal
  frontend disponibile dopo la registrazione (`updateproperty` non lo include tra i campi aggiornabili).
- **Nullabile**: Sì
- **Esempio di valori**: `"Aditya "`, `"vikey joshi"` — su un campione più ampio del DB di sviluppo (4135
  righe), **3819 righe su 4135 hanno `fullName = NULL`** (quasi il 92%): il campo, pur raccolto dal form di
  registrazione, resta di fatto vuoto nella maggioranza degli stabilimenti reali/di test presenti nel DB.

### mobileCountryCode
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Default**: nessuno
- **Descrizione**: pensato per il prefisso internazionale del numero di cellulare (es. `+39`); il backend lo
  legge da `req.body.mobileCountryCode` (`register`, riga 71). **Verificato su tutti e tre gli entry point**:
  - `front-pre`/`front-customer-pre` (`RegisterProperty.js`): l'utente seleziona effettivamente un prefisso
    (componente `CommonCountryCode`, stato `selectedCode`, default `"+39"`), ma al momento dell'invio il
    valore viene messo nel payload con la chiave **`countryCode`**, non `mobileCountryCode`:
    ```js
    var data = { propertyName, city, country: "", fullName, mobile: MobileNumber, email, photos, countryCode: this.state.selectedCode };
    ```
    il backend legge una chiave diversa da quella inviata — il prefisso scelto dall'utente non arriva mai a
    valorizzare la colonna.
  - `front-app-pre` (`ListOfProperty.screen.js`): il form non ha proprio un selettore di prefisso — solo un
    campo `mobileNo` validato come numero di 10 cifre (`mobileNoRegExp`), nessuna chiave `mobileCountryCode`
    né `countryCode` nel `postData` (righe 104-112). L'app non tenta nemmeno di raccoglierlo.
  In nessuno dei tre flussi la colonna viene valorizzata con un prefisso reale. **Confermato dai dati**: nel
  DB locale 97 righe su 100 hanno `mobileCountryCode = NULL` e le restanti 3 hanno stringa vuota `''` —
  **nessuna riga ha mai un prefisso reale** (es. `+39`).
  **Aggiornamento dati (campione più ampio, 4135 righe)**: quadro confermato — il campo è quasi sempre
  `NULL`/vuoto; l'unico valore reale osservato in tutta la tabella è `"+91"` (31 righe, verosimilmente
  correlato allo stesso gruppo di righe con `country = "india"` osservato sopra, cioè dati di test inseriti
  insieme), a conferma che **nessuno stabilimento con dati reali ha mai un prefisso valorizzato**.
- **Nullabile**: Sì
- **Esempio di valori**: `NULL`, `""`, `"+91"` (unico valore reale osservato, dato di test)

### mobile
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Default**: nessuno
- **Descrizione**: numero di cellulare di contatto, raccolto dal campo "Telefono" del form
  `RegisterProperty.js` (stato `MobileNumber`, righe 35/241-258, con validazione "solo cifre" su
  `onKeyPress`) e inviato come `mobile` (`register`, riga 72; da notare che il prefisso selezionato accanto a
  questo campo, tramite `CommonCountryCode`, **non** viene incluso in questo valore — resta un numero senza
  prefisso, coerentemente con quanto già osservato per `mobileCountryCode` sopra). Modificabile anche in fase
  di modifica da parte del sysadmin (`sysadmin.controller.js:updateproperty`, riga 528) — unico endpoint di
  modifica successiva verificato, non raggiungibile dal frontend disponibile in questo repository.
- **Nullabile**: Sì
- **Esempio di valori**: `"333333333"` (valore osservato **ripetuto identico su un numero rilevante di
  righe** nel campione più ampio del DB di sviluppo — chiaro placeholder/dato di test usato in fase di
  registrazione durante i test, non un numero di telefono reale, da tenere presente per non trattarlo come
  contatto valido in eventuali migrazioni/pulizie dati)

### email
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno a livello DB (nessun `UNIQUE`, nonostante sia di fatto l'identificativo usato per
  inviare le comunicazioni di registrazione/approvazione/rifiuto)
- **Relazioni**: —
- **Default**: nessuno
- **Descrizione**: email di contatto, raccolta dal campo "Email" del form `RegisterProperty.js` (stato
  `Email`, righe 36/259-272, con validazione formato via `validEmailRegex` sull'evento `onBlur`) e inviata
  come `email` (`register`, riga 73). Alla registrazione (`register`) viene anche copiata automaticamente
  nella tabella `subscribes` (newsletter) tramite `Commonhelper.createSubscribes(req.body.email)`
  (riga 79) — la registrazione di uno stabilimento iscrive quindi implicitamente il referente alla mailing
  list. È l'indirizzo a cui vengono inviate le email di benvenuto, approvazione, rifiuto e cancellazione
  dello stabilimento (`bathhouseEmailEN`/`bathhouseEmailIT`, `propertyStatusEmail`, `propertyDeleteEmail` in
  `sysadmin.controller.js`). Modificabile anche dal system admin (`updateproperty`, riga 529).
- **Nullabile**: Sì
- **Esempio di valori**: `"ishan01@yopmail.com"` (dominio `yopmail.com`, servizio di email usa-e-getta
  tipicamente usato per test/registrazioni fittizie — altro segnale di dati di sviluppo/test nel campione),
  `"info@lidoulisse.it"` (esempio di indirizzo reale)

### createdAt
- **Tipo**: `datetime`
- **Vincoli**: nessuno a livello DB (`DEFAULT NULL`); gestito però in modo automatico da Sequelize come
  timestamp di creazione per i record creati tramite l'applicazione (Sequelize valorizza `createdAt` in
  automatico ad ogni `create()`, salvo che sia stato disabilitato — qui non lo è)
- **Relazioni**: —
- **Descrizione**: data/ora di creazione del record. Può risultare `NULL` per righe inserite fuori
  dall'applicazione (import diretto, script, dati di test).
- **Nullabile**: Sì
- **Esempio di valori**: `2024-02-14 10:03:22`, `2025-11-30 18:47:09`

### updatedAt
- **Tipo**: `datetime`
- **Vincoli**: nessuno a livello DB (`DEFAULT NULL`); gestito automaticamente da Sequelize ad ogni `update()`
- **Relazioni**: —
- **Descrizione**: data/ora dell'ultima modifica del record, aggiornata automaticamente dall'ORM.
- **Nullabile**: Sì
- **Esempio di valori**: `2024-02-14 10:03:22`, `2026-08-20 09:11:56`

### status
- **Tipo**: `int`
- **Vincoli**: nessun vincolo DB esplicito oltre l'indice `properties_status` (non univoco); valore
  concettualmente enumerato, documentato solo da un commento nel model
  (`property.model.js:38-42`) e confermato dall'uso nei controller sysadmin:
  - `0` = stabilimento appena registrato dall'utente via UI, in attesa di approvazione (default implicito
    impostato da `register`, riga 74)
  - `1` = stabilimento approvato dal system admin (`sysadmin.controller.js:approvedproperties`)
  - `2` = stabilimento rifiutato dal system admin (`rejectedproperties`)
  - `3` = valore descritto **solo** in un commento nel model (*"status = 3 for properties that are added
    from csv"*), ma **non riscontrato in nessun punto reale del codice controller**.
  - `4` = valore realmente usato per l'import massivo da CSV: `user.controller.js:csvUpload` (riga 2063)
    crea la proprietà con `Property.create({ ..., status: 4 })`. **Il commento nel model (`3`) è quindi
    disallineato dal codice reale (`4`)** — probabile refuso di documentazione mai corretto, dato che è
    solo un commento e non incide sull'esecuzione. Da verificare col committente quale dei due valori sia
    quello voluto.
- **Relazioni**: —
- **Default**: nessun default a livello DB; il valore iniziale `0` è impostato esplicitamente dal codice
  applicativo alla creazione
- **Descrizione**: stato del ciclo di vita amministrativo dello stabilimento. Determina se lo stabilimento è
  visibile/operativo: `serchProperty` (`property.controller.js`) filtra esplicitamente `status: [1, 3]` per
  la ricerca pubblica — sulla base del codice attuale mostra quindi solo gli stabilimenti approvati (`1`),
  dato che nessun punto del codice crea più righe con `status=3`. **Incongruenza verificata**: gli
  stabilimenti importati da CSV, che secondo `csvUpload` ricevono `status=4`, **non compaiono** in questo
  filtro — restano invisibili nella ricerca pubblica finché un system admin non li porta a `1`
  manualmente, a meno che l'intento originale non fosse farli comparire subito con `status=3` (nel qual
  caso è il valore scritto da `csvUpload`, `4`, ad essere sbagliato). Nella copia dati locale: 65 righe con
  `status=1`, 35 con `status=0`.
- **Distribuzione osservata nel DB locale**: `1` → 65 righe, `0` → 35 righe (nessuna riga con `status=2`,
  `status=3` o `status=4` nel campione — il campione non contiene stabilimenti importati da CSV).
  **Aggiornamento rilevante (verifica su un campione molto più ampio, 4135 righe)**: il quadro cambia in modo
  sostanziale — `4` risulta il valore **nettamente più frequente** (3808 righe, la stragrande maggioranza
  della tabella), seguito da `0` (176), `1` (121), `3` (20), `2` (10). Se l'interpretazione di `status=4`
  come "import da CSV" (`csvUpload`, sopra) è corretta, e se il filtro `serchProperty` che mostra solo
  `status: [1, 3]` è ancora quello realmente in uso, questo aggiornamento suggerisce che **la stragrande
  maggioranza degli stabilimenti nel DB non compaia nella ricerca pubblica** — un'implicazione di business
  potenzialmente rilevante da verificare col committente prima della riscrittura (chiarire se `status=4`
  debba davvero restare escluso dalla ricerca pubblica su questa scala, o se il filtro/il valore scritto da
  `csvUpload` andasse comunque rivisto, come già ipotizzato sopra).
- **Nullabile**: Sì
- **Esempio di valori**: `4`, `0`, `1`, `3`, `2` (i 5 valori osservati, vedi distribuzione sopra)
- **Candidato a enumerazione**: vocabolario chiuso e già in parte documentato nel codice (commento nel model
  + uso nei controller sysadmin), ma **non modellato come enum reale**: oggi è un intero arbitrario senza
  nomi leggibili nello schema, con almeno un disallineamento noto tra commento (`3`) e codice reale (`4`).
  Da modellare nel nuovo schema come `ENUM` DB tipizzato (C#: enum dedicato, es. `PropertyStatus { Pending,
  Approved, Rejected, ImportedFromCsv }`) — è un vocabolario di stato del ciclo di vita, stabile e non
  soggetto a traduzione, quindi non richiede necessariamente una tabella di lookup separata.

### photos
- **Tipo**: dichiarato `Sequelize.JSON` nel model; a livello fisico la colonna è `longtext` con un vincolo
  `CHECK (json_valid(photos))` (pattern MySQL per emulare un tipo JSON con collation/charset specifici, qui
  `utf8mb4_bin` — probabilmente per un confronto case-sensitive degli URL)
- **Vincoli**: `CHECK json_valid(photos)`; nessun default a livello DB (il codice applicativo inizializza il
  campo a stringa vuota `""` alla creazione, `register` riga 75, che **non** è un JSON valido — vedi nota
  sotto)
- **Relazioni**: —
- **Descrizione**: array di URL (stringhe) delle foto caricate su S3 durante la registrazione dello
  stabilimento (`multipleUploadLargeImage`, bucket path `AWS_PROP_IMG_PATH`). Esempio reale dal DB:
  `["https://summerbooking.s3.us-east-2.amazonaws.com/images/property/2yjarmxknrawp92.jpeg"]`. Duplica
  concettualmente (con contenuto spesso diverso) le foto pubbliche gestite sulla tabella `bathouses`.
  **Comportamento verificato su tutti e tre gli entry point di registrazione** (stessa `register()` di
  backend, chiamata da tre form indipendenti):
  - `front-pre/src/components/property/RegisterProperty.js` — il blocco JSX che monta il componente di
    upload `MutipleImageUploads` è **commentato** (righe 273-278). `imagesRender` resta sempre `[]`, il
    payload invia sempre `photos: []`: da questo entry point non è possibile allegare foto.
  - `front-customer-pre/src/components/property/RegisterProperty.js` — **copia quasi identica del file
    precedente, ma qui il blocco equivalente (righe 268-273) NON è commentato**: il componente
    `MutipleImageUploads` è attivo, l'utente può davvero allegare foto e il payload porta gli URL/base64
    raccolti.
  - `front-app-pre/app/screens/Dashboard/ListOfProperty.screen.js` (app mobile, stesso endpoint
    `POST /api/public/property` via `Api.registerProperty`) — qui l'upload foto è **obbligatorio**:
    `onSubmit` (righe 97-100) blocca l'invio con un toast se `uploadPhoto.length === 0`, e il payload
    (riga 111) invia un array di immagini in **base64** (`imagrArray.push(e.base64)`), non URL.
  In sintesi: **solo la copia in `front-pre` ha l'upload disabilitato**; le altre due (sito pubblico e app)
  raccolgono foto normalmente, una delle due obbligandolo. Essendo `[]`/un array non vuoto sempre presente e
  troncato a `true` in JavaScript, il backend (`if (photos)`, riga 82) esegue comunque l'update che
  sovrascrive il valore iniziale non-JSON `""` con un array valido — quindi, per **tutti e tre** gli entry
  point oggi raggiungibili, il rischio di lasciare la riga con `photos = ""` non si presenta in pratica.
  Resta un rischio reale solo per chiamate dirette all'endpoint pubblico che omettano del tutto la chiave
  `photos` dal body (bypassando tutti e tre i frontend), condizione in cui `if (photos)` sarebbe `false` e
  l'update non scatterebbe, lasciando la riga con `photos = ""` — valore che rompe
  `JSON.parse(property.photos)` fatto altrove (es. `serchProperty`, riga 309).
- **Nullabile**: Sì
- **Esempio di valori**: `["https://summerbooking.s3.us-east-2.amazonaws.com/images/property/2yjarmxknrawp92.jpeg"]`

### propertyOwner
- **Tipo**: `int`
- **Vincoli**: nessuno a livello DB (nessuna FK)
- **Relazioni**: relazione applicativa (non FK reale) verso `users.id` — è l'utente con ruolo `superadmin`
  associato allo stabilimento
- **Default**: nessuno (`NULL` per gli stabilimenti appena registrati, prima che l'utente admin completi la
  registrazione)
- **Descrizione**: valorizzato in `auth.controller.js` (riga 231) quando, in fase di completamento
  registrazione, viene creato l'utente con ruolo `superadmin` per lo stabilimento — a quel punto
  `propertyOwner` viene impostato all'`id` di quell'utente. Rappresenta quindi il "proprietario"/utente
  amministratore principale registrato per lo stabilimento. Modificabile anche dal system admin
  (`sysadmin.controller.js:updateproperty`, riga 532).
  **Nota rilevante trovata rileggendo il codice con attenzione**: il campo **non è più la fonte primaria**
  usata per risolvere il destinatario delle notifiche email al gestore. In `booking.controller.js` (righe
  6829-6874 e, identico, 11667-11711) un commento esplicito nel codice spiega perché:
  > *"The previous logic resolved the recipient via properties.propertyOwner, which could point to a
  > different/old account [...], so the actual manager never received the [...] email."*

  Il codice attuale risolve quindi il destinatario con una cascata a tre livelli, usando `propertyOwner`
  solo come **ultimo fallback**:
  1. il primo utente attivo (`userStatus = 'active'`), con notifiche abilitate (`notifyEmail = 1`) e uno dei
     ruoli operativi `2, 4, 5, 6` (`user_roles.roleId`), associato alla property tramite `users.propertyId`;
  2. se non trovato, il primo utente attivo con notifiche abilitate associato alla property (senza vincolo
     di ruolo);
  3. **solo se anche questo fallisce**, l'utente collegato tramite `JOIN properties AS p ON p.propertyOwner =
     u.id` — cioè questo campo.
  In altre parole, il codice stesso tratta `propertyOwner` come un riferimento che può essere **stale**
  (l'utente "superadmin" originale della registrazione potrebbe non essere più quello operativo/da
  notificare) e gli preferisce sistematicamente `users.propertyId` + ruolo, che è la relazione realmente
  affidabile oggi. Nella riscrittura andrebbe rivalutato se `propertyOwner` come "utente di riferimento
  storico legato alla registrazione" sia ancora un concetto da mantenere, separato da "utenti con ruolo
  gestionale sulla property" (relazione via `users.propertyId`, già la fonte di verità preferita nel codice
  legacy stesso). **Distribuzione nel DB locale**: `NULL` per 35 righe su 100 (stabilimenti mai
  completati/approvati), gli altri con valori sparsi che puntano a singoli utenti.
  **Aggiornamento (campione più ampio, 4135 righe)**: proporzione di `NULL` ancora più marcata — 3997 righe
  su 4135 (oltre il 96%) hanno `propertyOwner = NULL`, coerente con l'alta quota di stabilimenti con
  `status` diverso da "approvato" osservata sopra.
- **Nullabile**: Sì
- **Esempio di valori**: `221` (id di un utente `superadmin` reale)

### adminComment
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Default**: nessuno
- **Descrizione**: commento testuale libero inserito dal system admin quando approva, rifiuta o cancella uno
  stabilimento (`sysadmin.controller.js: deleteproperty` riga 367, `updateproperty` riga 531). Il valore
  viene incluso nel corpo delle email automatiche inviate al proprietario dello stabilimento
  (`propertyDeleteEmail`/`propertyStatusEmail` e le rispettive varianti italiane), quindi è un testo pensato
  per essere letto dal cliente finale (es. motivazione del rifiuto), non solo per uso interno.
- **Nullabile**: Sì
- **Esempio di valori**: `"Approve"`, `"apporvedtwo"` (refuso/testo poco curato osservato realmente nei dati —
  coerente con l'essere un campo libero digitato a mano dal system admin, senza validazione)

### isDelete
- **Tipo**: `tinyint(1)` (booleano)
- **Vincoli**: indice non univoco `properties_is_delete`
- **Relazioni**: —
- **Default**: `0` (a livello sia DB `DEFAULT '0'` sia model Sequelize `defaultValue: false`)
- **Descrizione**: flag di cancellazione logica (soft delete). Impostato a `1` da
  `sysadmin.controller.js:deleteproperty` invece di eseguire una `DELETE` fisica. Le query di ricerca
  pubblica (`serchProperty`) e le liste sysadmin filtrano esplicitamente `isDelete: 0`/`isDelete: false`.
  **Distribuzione nel DB locale**: 63 righe con `isDelete=0`, 37 con `isDelete=1` — quasi un terzo degli
  stabilimenti nel campione risulta cancellato logicamente.
  **Aggiornamento (campione più ampio, 4135 righe)**: proporzione più bassa ma comunque rilevante — `0` →
  3973 righe, `1` → 162 righe (circa il 4%).
- **Nullabile**: Sì
- **Esempio di valori**: `0`, `1`
- **Candidato a enumerazione**: booleano vero e proprio (soft-delete), non un vocabolario da enumerare — nel
  nuovo schema va tipizzato come `BOOLEAN`/`BIT` reale (già sostanzialmente ciò che è, `tinyint(1)`), non
  serve un `ENUM`.

### onlineReservation
- **Tipo**: `int` (usato come booleano 0/1)
- **Vincoli**: nessuno
- **Relazioni**: —
- **Default**: `1` (sia DB sia model)
- **Descrizione**: interruttore **di proprietà del gestore dello stabilimento** che abilita/disabilita la
  possibilità per i clienti finali di prenotare online le *location* (ombrelloni/lettini). Gestito dal
  pannello impostazioni (`OnlineBookingIndex.js`, sezione "prenotazione online"), sovrascritto lato server
  da `UpdateOnlineBookingSettings`. È concettualmente indipendente da `superAdminOnlineFlag` (vedi sotto),
  che è invece un kill-switch di livello piattaforma.
  **Verificato con ricerca esaustiva** (`grep` su tutto `back-pre/app` delle letture di
  `.onlineReservation`): il campo viene **solo restituito al client** nei vari endpoint di lettura
  (`serchProperty`, `getOnlineBookingSettings`, i due endpoint sysadmin di dettaglio proprietà) — **nessun
  controller verifica `property.onlineReservation` prima di accettare la creazione di una prenotazione
  online**. È quindi, allo stato attuale del codice, un flag puramente informativo lato server per quanto
  riguarda l'enforcement: l'unico punto che lo fa davvero rispettare è il frontend, che in base al suo valore
  mostra/nasconde la UI di prenotazione online. Da correggere nella riscrittura (va ri-verificato anche lato
  server al momento della prenotazione, non solo usato per decidere cosa mostrare in UI).

  **Due punti di scrittura distinti, tracciati a ritroso dal frontend**:
  1. Il pannello impostazioni attivo (`OnlineBookingIndex.js`, raggiungibile dalla UI) → endpoint
     `UpdateOnlineBookingSettings` (`property.controller.js:964-978`) — scrive `onlineReservation` insieme a
     tutto il resto della configurazione online.
  2. Un secondo componente, `EnableDisableReservation.js`/`ReservationParent.js`
     (`front-pre/src/components/onlineReservation/`), con un proprio checkbox "disabilita prenotazione
     online" (`disableReservation` → `isDisable`) → azione `_postReservationSettings` → endpoint
     `POST /api/sb/addonlinereservation`/`updateonlinereservation`
     (`onlinereservation.controller.js:17-120`), che scrive **sia** `Property.onlineReservation` (0/1 in base
     a `isDisable`, righe 58-73) **sia** un record sulla tabella separata `online_reservations` (che
     contiene anche `cardNumber`/`cvc` in chiaro — criticità già nota, vedi
     [01-criticita-e-indicazioni-backend.md §4](../../docs/01-criticita-e-indicazioni-backend.md)).
     **Questo secondo componente esiste nel codice e i suoi endpoint sono attivi**, ma **non è raggiungibile
     dalla navigazione attuale**: in `SettingContent.js` (riga 178) il rendering di `<ReservationParent />`
     è commentato, resta attivo solo `<OnlineBookingIndex />` (riga 179-180). È quindi un flusso "morto in
     UI ma vivo in API" — chiunque conosca l'endpoint può comunque invocarlo direttamente (nessuna
     autorizzazione aggiuntiva verificata oltre al token).

  **Confermato anche dall'app mobile**: `front-app-pre/app/screens/Dashboard/SearchSingleLocation.screen.js:191`
  legge `resData.onlineReservation` con lo stesso pattern read-only delle due web app (mostra/nasconde
  l'opzione di prenotazione online in UI, nessuna verifica lato server).

  **Nota di asimmetria**: `deleteStripeAccount` (`stripe.controller.js:2328-2359`, invocato dal componente
  `StripeSetting.js` — a sua volta montato sia nel pannello attivo `OnlineBookingIndex.js` sia in quello
  morto `EnableDisableReservation.js`) disconnette l'account Stripe Connect e imposta a `0`
  **`serviceOnlineBooking`** (vedi sotto), ma **non tocca `onlineReservation`**: disconnettere Stripe lascia
  quindi la prenotazione online delle location formalmente "abilitata" anche se i pagamenti online non
  funzionerebbero più.

  **Distribuzione nel DB locale**: `1` → 74 righe, `0` → 26 righe.
  **Aggiornamento (campione più ampio, 4135 righe)**: `1` → 4111 righe, `0` → 24 righe — conferma che quasi
  tutti gli stabilimenti hanno la prenotazione online abilitata di default e la disattivazione esplicita è
  rara.
- **Nullabile**: Sì
- **Esempio di valori**: `1`, `0`
- **Candidato a enumerazione**: booleano (0/1), non un vocabolario da enumerare — tipizzare come
  `BOOLEAN`/`BIT` reale nel nuovo schema.

### paymentOptions
- **Tipo**: dichiarato `Sequelize.JSON`; colonna fisica `longtext` con `CHECK json_valid(paymentOptions)`
- **Vincoli**: `CHECK json_valid(...)`
- **Relazioni**: —
- **Default**: **due default diversi e incoerenti tra loro esistono nel codice**:
  - il model Sequelize (`property.model.js:63-68`) dichiara come default
    `{ pay_now_info: 0, pay_in_structure_info: 0, pay_at_property_info: 0, advanced_balance_structure_info: 0 }`
  - il fallback usato dall'endpoint `getPaymentOptions` quando il campo è vuoto
    (`config/propertyConfig.js`) è invece
    `{ pay_now_info: 0, pay_in_structure_info: 0, pay_at_property_info: 1, advanced_balance_structure_info: 0 }`
  - **nessuno dei due corrisponde alle 5 opzioni realmente proposte all'utente** (vedi sotto): il componente
    React attivo che compila questo campo (`PaymentOptionSelection.js`, usato da
    `OnlineBookingIndex.js`/`OnlineBookingModal.js`) offre e salva solo le chiavi `pay_now_info`,
    `pay_at_property_info`, `pay_at_property_no_credit_info`, `deposit_now_half_info`,
    `pay_at_property_prepaid_info` (verificato leggendo `getHandledEvent`, righe 59-69, e i cinque
    checkbox del `render()`, righe 110-193). Le chiavi `pay_in_structure_info` e
    `advanced_balance_structure_info` **non sono verificate come del tutto morte**: hanno ancora una
    label tradotta in tutte le lingue (`pay_in_structure_info_label`/`advanced_balance_structure_info_label`
    in `front-pre/src/locale/{it,en,de,fr,es,ar,zh}.js`), ma non sono renderizzate da nessun componente
    di selezione trovato nel frontend — sono quindi opzioni predisposte a livello di traduzione/default ma
    non più esposte all'utente nell'interfaccia attuale. Da chiarire col committente se vanno recuperate o
    tolte prima di riportarle nel nuovo modello; in ogni caso i due default oggi in contraddizione tra loro
    (`property.model.js` vs `propertyConfig.js`) vanno unificati.
- **Descrizione**: definisce quali modalità di pagamento sono attive per la prenotazione online delle
  *location* (ombrelloni) di questo stabilimento. Letto/scritto da `getPaymentOptions`/
  `UpdateOnlineBookingSettings` (`property.controller.js`) e dal pannello "Impostazioni prenotazione online"
  nel gestionale (`OnlineBookingIndex.js`, `PaymentOptionSelection.js`). **Scritto anche da un secondo punto,
  indipendente dal pannello impostazioni**: `onlinereservation.controller.js:addonlinereservation`
  (righe 17-23) sovrascrive `Property.paymentOptions` con `req.body.paymentOptions` come effetto collaterale
  della creazione di un record sulla tabella `online_reservations`. È lo stesso flusso "morto in UI ma vivo
  in API" già descritto per `onlineReservation` sopra: alimentato dal componente
  `EnableDisableReservation.js` (che monta lo stesso `PaymentOptionSelection.js` usato dal pannello attivo),
  raggiungibile solo tramite `ReservationParent`, il cui rendering è commentato in
  `SettingContent.js:178` — quindi non selezionabile dalla UI oggi, ma l'endpoint che scrive
  `paymentOptions` (e la tabella `online_reservations`, che salva anche `cardNumber`/`cvc` in chiaro —
  criticità già segnalata in
  [01-criticita-e-indicazioni-backend.md §4](../../docs/01-criticita-e-indicazioni-backend.md)) resta attivo
  e richiamabile direttamente. Nel componente `PaymentOptionSelection.js`, i checkbox `pay_now_info`,
  `pay_at_property_info` e
  `deposit_now_half_info` sono mostrati solo se lo stabilimento ha un `stripeCustomerId` valorizzato in
  sessione (righe 122, 133, 158: `{stripeCustomerId !== "" && ...}`) — cioè solo se è collegato un account
  Stripe; `pay_at_property_no_credit_info` e `pay_at_property_prepaid_info` sono invece sempre proposti.
  Il campo influenza anche l'emissione automatica dello scontrino fiscale: la funzione **attiva**
  `FiskalyReceiptManagerService.createAutoReceiptFromStripe` verifica `paymentOptions.pay_now_info` in modo
  indiretto tramite `payment_type !== "pay_now"` sulla transazione Stripe collegata (vedi il campo
  `fiskalyAutoreceipt` più sotto per il dettaglio completo del flusso, che è realmente attivo e non
  disabilitato). **Consumo lato cliente finale verificato** in `front-customer-pre/src/components/consumer/`:
  `BookBtnInfo.js:49-50` e `BookNowPopup.js:786-793` leggono
  `propertyData.paymentOptions.pay_at_property_info`/`pay_at_property_no_credit_info` per decidere quali
  pulsanti/opzioni di pagamento proporre al cliente durante la prenotazione pubblica — è quindi questo
  campo, letto qui, a determinare concretamente cosa il cliente finale può scegliere in fase di checkout,
  non solo cosa il gestore ha impostato a pannello. Stesso pattern confermato nell'app mobile
  (`front-app-pre/app/screens/Dashboard/SearchSingleLocation.screen.js:340-341`, lettura di
  `data.data.paymentOptions.pay_at_property_info`).
- **Campi** (struttura realmente in uso, confermata da `allowedPaymentOptions.js`/`OnlineBookingIndex.js` nel
  frontend e da record reali nel DB):
  - **pay_now_info**
    - Tipo: `int` (0/1, booleano)
    - Vincoli: nessuno esplicito
    - Relazioni: —
    - Default: `0`
    - Descrizione: se `1`, il cliente può pagare l'intero importo online al momento della prenotazione
      (pagamento con carta via Stripe). È anche la condizione che abilita l'emissione automatica dello
      scontrino fiscale (vedi sopra).
    - Esempio di valori: `0`, `1`
  - **pay_at_property_info**
    - Tipo: `int` (0/1)
    - Vincoli: nessuno esplicito
    - Relazioni: —
    - Default: `1` (nel default del frontend `defaultOption`/`paymentOptionObject`)
    - Descrizione: se `1`, il cliente può prenotare online scegliendo di pagare direttamente in struttura
      (tipicamente con richiesta dei dati carta come garanzia, senza addebito immediato).
    - Esempio di valori: `0`, `1`
  - **pay_at_property_no_credit_info**
    - Tipo: `int` (0/1)
    - Vincoli: nessuno esplicito
    - Relazioni: —
    - Default: `0`
    - Descrizione: variante di "paga in struttura" che **non** richiede i dati della carta di credito come
      garanzia — prenotazione libera, pagamento in loco senza pre-autorizzazione.
    - Esempio di valori: `0`, `1`
  - **deposit_now_half_info**
    - Tipo: `int` (0/1)
    - Vincoli: nessuno esplicito
    - Relazioni: —
    - Default: `0`
    - Descrizione: se `1`, il cliente può pagare online un acconto (metà dell'importo, come suggerisce il
      nome) al momento della prenotazione, saldando il resto in struttura.
    - Esempio di valori: `0`, `1`
  - **pay_at_property_prepaid_info**
    - Tipo: `int` (0/1)
    - Vincoli: nessuno esplicito
    - Relazioni: —
    - Default: non presente nei default (`paymentOptionObject`/`defaultOption` non la includono, ma
      `allowedPaymentOptions`/`OnlineBookingIndex.js` la trattano come quinta opzione selezionabile)
    - Descrizione: variante "paga in struttura" con pre-autorizzazione/pre-addebito della carta
      (nome interno per una modalità di pagamento anticipato con carta registrata, distinta dal semplice
      acconto). Presente in modo incompleto/incoerente rispetto alle altre quattro opzioni nei default —
      probabile funzionalità aggiunta successivamente senza allineare tutti i punti del codice.
    - Esempio di valori: `0`, `1`
- **Nullabile**: Sì (85 righe `NULL` su 4135 nella verifica più recente del DB di sviluppo)
- **Esempio di valori**: `{"pay_now_info":0,"pay_in_structure_info":0,"pay_at_property_info":0,"advanced_balance_structure_info":0}`
  — **osservazione rilevante**: questo è l'esempio realmente osservato più di frequente nei dati, e usa
  ancora le **vecchie chiavi** `pay_in_structure_info`/`advanced_balance_structure_info` (gli stessi due
  default incoerenti tra `property.model.js` e `propertyConfig.js` discussi sopra), **non** le 5 chiavi che
  il componente attivo `PaymentOptionSelection.js` scrive oggi (`pay_now_info`, `pay_at_property_info`,
  `pay_at_property_no_credit_info`, `deposit_now_half_info`, `pay_at_property_prepaid_info`). Conferma
  concreta, sui dati, che una parte rilevante delle righe non è mai stata aggiornata tramite il pannello
  attivo e conserva ancora il valore di default "vecchio" — coerente con l'ipotesi già espressa sopra che
  `pay_in_structure_info`/`advanced_balance_structure_info` siano opzioni ormai residuali lato UI ma non
  ancora ripulite lato dati/default.
- **Candidato a enumerazione**: le chiavi del JSON sono un set fisso e piccolo di flag booleani (0/1), non un
  vocabolario a valore singolo — come già indicato nelle "Osservazioni per la riscrittura" più sotto, vanno
  modellate come colonne booleane relazionali tipizzate (o un value object), non come JSON con chiavi
  stringa; l'incoerenza concreta appena osservata tra chiavi "vecchie" nei dati e chiavi "nuove" nel codice
  attivo è un ulteriore argomento a favore di uno schema tipizzato che eviti hardcoding di nomi di proprietà.

### serviceOnlineBooking
- **Tipo**: `int` (booleano 0/1)
- **Vincoli**: nessuno
- **Relazioni**: —
- **Default**: `1`
- **Descrizione**: come `onlineReservation`, ma per l'abilitazione della prenotazione online dei **servizi**
  (bar, servizi a fascia oraria, servizi "normali") invece che delle location. Gestito dallo stesso pannello
  impostazioni, sezione servizi (`OnlineBookingIndex.js`, endpoint `UpdateOnlineBookingSettings`). **Verificato**
  allo stesso modo di `onlineReservation`: le letture nel backend (`property.controller.js:991,1016`,
  `sysadmin.controller.js:339`) lo restituiscono al client, non lo usano come condizione per accettare/
  rifiutare una prenotazione di servizio — stesso limite architetturale del campo precedente.
  **Secondo punto di scrittura verificato**: `stripe.controller.js:deleteStripeAccount` (righe 2328-2359,
  invocato dal componente `StripeSetting.js`, montato nel pannello attivo `OnlineBookingIndex.js`) imposta
  `serviceOnlineBooking = 0` come effetto collaterale della disconnessione dell'account Stripe Connect dello
  stabilimento — indipendentemente da cosa il gestore avesse impostato in precedenza, e senza toccare
  `onlineReservation` (vedi nota di asimmetria sopra). **Distribuzione nel DB locale**: `1` → 94 righe, `0` →
  6 righe (feature quasi sempre attiva dove presente).
  **Aggiornamento (campione più ampio, 4135 righe)**: `1` → 4119 righe, `0` → 16 righe — proporzione confermata.
- **Nullabile**: Sì
- **Esempio di valori**: `1`, `0`
- **Candidato a enumerazione**: booleano (0/1), non un vocabolario da enumerare — tipizzare come
  `BOOLEAN`/`BIT` reale nel nuovo schema.

### servicebookingPaymentOptions
- **Tipo**: dichiarato `Sequelize.JSON`; colonna fisica `longtext` con `CHECK json_valid(...)`
- **Vincoli**: `CHECK json_valid(...)`
- **Relazioni**: —
- **Default**: nessun default dichiarato nel model (a differenza di `paymentOptions`); nei dati osservati il
  valore è sempre `{}` (oggetto vuoto) — la feature esiste nel codice (frontend e nel controller
  `getPaymentOptions`, che la restituisce insieme a `paymentOptions`) ma non risulta configurata in nessuno
  stabilimento del campione locale
- **Descrizione**: equivalente di `paymentOptions` ma per la prenotazione online dei **servizi** invece che
  delle location. Struttura identica (stesse chiavi), confermata da `OnlineBookingIndex.js` (funzione
  `_saveReservation`, righe 245-254).
- **Campi**: stessa struttura di `paymentOptions` — `pay_now_info`, `pay_at_property_info`,
  `pay_at_property_no_credit_info`, `deposit_now_half_info`, `pay_at_property_prepaid_info` (vedi
  descrizioni sopra, identico significato ma applicato al flusso di prenotazione servizi anziché location).
- **Nullabile**: Sì (quasi sempre `NULL` — verifica più recente sul DB di sviluppo: 4115 righe su 4135, cioè
  oltre il 99%, hanno il campo `NULL`, coerente con quanto già scritto sopra sulla feature non configurata
  in pratica in nessuno stabilimento)
- **Esempio di valori**: `{}` (oggetto vuoto, unico valore non-`NULL` osservato nel campione)
- **Candidato a enumerazione**: stessa osservazione fatta per `paymentOptions` — set fisso di flag booleani,
  da modellare come colonne relazionali tipizzate nel nuovo schema, non come JSON.

### onlineBookingOptions
- **Tipo**: dichiarato `Sequelize.JSON`; colonna fisica `longtext` con `CHECK json_valid(...)`
- **Vincoli**: `CHECK json_valid(...)`
- **Relazioni**: il sotto-campo `locations` referenzia (per nome/etichetta, non per id) i settori/righe
  configurati per lo stabilimento (tabella `sectors_rows`); il sotto-campo `bookingLimit` referenzia i
  settori per nome (`sectorName`)
- **Default**: nessuno a livello DB; il frontend, in assenza di dati, inizializza i singoli sotto-campi a
  `false`/`""`/`[]` (vedi `OnlineBookingModal.js`)
- **Descrizione**: blocco di configurazione, **scritto** dal pannello "Impostazioni prenotazione online" del
  gestionale (`front-pre`/`front-customer-pre`, `Settings/OnlineBookingIndex.js`/`OnlineBookingModal.js`) e
  **letto/applicato** dal sito pubblico di prenotazione (`front-customer-pre/src/components/consumer/`), che
  regola **come** un cliente può prenotare online le location dello stabilimento: quali location sono
  prenotabili, in quale periodo dell'anno, se applicare un sovrapprezzo, se richiedere conferma manuale, i
  limiti minimi/massimi di soggiorno per settore, il testo informativo mostrato al cliente e la legenda
  della mappa. **Consumo lato cliente verificato**: `PropertyDetails.js` (righe 414-481, nel flusso
  consumer) legge `resData.onlineBookingOptions.{isEnabledMap, locationOptionObj, locations, isEnablePeriod,
  period}` per costruire la UI di prenotazione pubblica. **Nota di criticità** (già segnalata in
  [01-criticita-e-indicazioni-backend.md §11.6](../../docs/01-criticita-e-indicazioni-backend.md), qui
  ri-confermata leggendo anche il consumer): il sotto-campo `bookingLimit`, letto anch'esso in
  `PropertyDetails.js:450,481`, è **letto e applicato solo lato UI** del sito pubblico (nasconde/valida le
  scelte nel form) — il vincolo min/max giorni non risulta mai ricontrollato lato server al momento in cui
  il backend salva effettivamente la prenotazione. **Terza conferma indipendente**: anche l'app mobile
  (`front-app-pre/app/screens/Dashboard/BookNow.screen.js:1168-1169`) legge
  `onlineBookingOptions.locationOptionObj.bookingLimit` e lo passa a una propria funzione
  `_allowBookingLimitCustomer` per validarlo — stessa logica duplicata lato client, stesso limite: nessuno
  dei tre frontend fa affidamento su un controllo server-side che infatti non esiste.
- **Campi** (livello radice):
  - **isEnableLocation**
    - Tipo: `boolean`
    - Descrizione: se `true`, la prenotazione online è ristretta a un sottoinsieme specifico di location
      (elencate in `locations`) invece che a tutte le location dello stabilimento.
    - Esempio: `true`, `false`
  - **isEnablePeriod**
    - Tipo: `boolean`
    - Descrizione: se `true`, la prenotazione online è ristretta a un periodo/intervallo di date specifico
      (definito in `period`) invece che valida per l'intera stagione.
    - Esempio: `true`, `false`
  - **isEnabledMap**
    - Tipo: `boolean`
    - Descrizione: se `true`, il cliente sceglie la location da prenotare tramite la mappa interattiva dello
      stabilimento (spiaggia/settori) invece che da una lista.
    - Esempio: `true`, `false`
  - **locations**
    - Tipo: `array` di stringhe (nomi/etichette di location)
    - Descrizione: elenco delle location (righe/settori) effettivamente prenotabili online quando
      `isEnableLocation` è `true`. Popolato tramite il componente di selezione mappa
      (`ChooseUmbrellaModal`/`configureAction.setSelectedLocNames`).
    - Esempio: `["Fila A - 1", "Fila A - 2"]`
  - **period**
    - Tipo: `object` (o array di un solo oggetto nei dati osservati) con `startDate`, `endDate` (ISO
      datetime) ed eventualmente `key: "selection"` (artefatto della libreria di selezione date usata nel
      frontend, `react-date-range`)
    - Descrizione: intervallo di date entro cui è permessa la prenotazione online, quando `isEnablePeriod` è
      `true`.
    - Esempio: `{"startDate":"2026-06-01T00:00:00.000Z","endDate":"2026-09-15T00:00:00.000Z","key":"selection"}`
  - **locationOptionObj**
    - Tipo: `object`
    - Descrizione: contenitore delle opzioni di dettaglio per la prenotazione online delle location (vedi
      sotto-campi).
    - Esempio: `{"locationOptionObj":{}}` (nei dati reali, valore quasi sempre vuoto/minimale — vedi
      distribuzione del campo padre più sotto)
    - Campi:
      - **addSurcharge**: Tipo `string` (valore numerico, es. `"0"`) — importo/percentuale del sovrapprezzo
        applicato alle prenotazioni online, interpretato secondo `surchargeType`. Esempio: `"0"`, `"5"`.
      - **surchargeType**: Tipo `string`, valori ammessi `"Disable"` (nessun sovrapprezzo, valore osservato
        anche nella forma non normalizzata `"disable"`), `"percentage"`, `"fixed"` — determina se
        `addSurcharge` è una percentuale o un importo fisso. **Dato osservato da segnalare**: nel campione
        reale coesistono, per lo stesso significato, sia `"Disable"` (maiuscola) sia `"disable"`
        (minuscola) — incoerenza di capitalizzazione concreta dentro **lo stesso campo**, verosimilmente
        scritta da punti diversi del frontend/di versioni diverse del form nel tempo; qualunque confronto
        stretto (`=== "Disable"`) nel codice rischia di non riconoscere l'una o l'altra forma. **Candidato a
        enumerazione**: caso da manuale per un `ENUM` DB (`disable`/`percentage`/`fixed`, vocabolario
        piccolo, stabile, senza bisogno di traduzione) — l'incoerenza di maiuscole/minuscole osservata sui
        dati reali è di per sé la controprova concreta che un `varchar` libero non è sufficiente a
        mantenere questo campo coerente nel tempo.
      - **blockOnlinePayment**: Tipo `string`, valori osservati `"disable"`/`"enable"` — se abilitato, blocca
        la possibilità di pagare online (forzando le sole opzioni "paga in struttura" indipendentemente da
        `paymentOptions`). **Candidato a enumerazione**: booleano travestito da stringa (`"enable"`/
        `"disable"` invece di `true`/`false`) — da tipizzare come `BOOLEAN` reale nel nuovo schema, non
        serve un `ENUM` multi-valore.
      - **requestBookingConfirmation**: Tipo `boolean` — se `true`, le prenotazioni online create dal
        cliente restano in stato "da confermare" e richiedono un'approvazione manuale del gestore invece di
        essere confermate immediatamente. Esempio: `true`, `false`.
      - **blockReservation**: Tipo `int`/`boolean` (osservato `0`) — flag per bloccare temporaneamente ogni
        nuova prenotazione online (kill-switch locale, distinto da `onlineReservation` e
        `superAdminOnlineFlag`). Esempio: `0`, `1`.
      - **currentDayBookable**: Tipo `boolean` (osservato anche come stringa `"true"`/`"false"` — incoerenza
        di tipo tra le righe) — se `true`, permette di prenotare online per il giorno stesso. Esempio:
        `true`, `"false"` (entrambe le forme osservate nei dati reali).
      - **enableHalfDayBook**: Tipo `boolean` — se `true`, abilita la prenotazione online per mezza giornata
        (mattina/pomeriggio) oltre che per giornata intera. Esempio: `true`, `false`.
      - **enablePlaceOnMap**: Tipo `boolean` — se `true`, mostra la posizione della location selezionata sulla
        mappa durante il flusso di prenotazione online. Esempio: `true`, `false`.
      - **enableResrInPeriod**: Tipo `boolean` — variante/opzione collegata a `isEnablePeriod` per abilitare
        la prenotazione ristretta al periodo configurato. Esempio: `true`, `false`.
      - **enableResrInSelectedLoc**: Tipo `boolean` — variante/opzione collegata a `isEnableLocation` per
        abilitare la prenotazione ristretta alle location selezionate. Esempio: `true`, `false`.
      - **maxSeatReservation**: Tipo `int` o `null` — numero massimo di posti/location prenotabili in una
        singola prenotazione online. Esempio: `null`, `5`.
      - **seasonalReservation**: Tipo `boolean` (osservato anche come stringa `"true"`) — se `true`, abilita
        la prenotazione online "stagionale" (multi-settimana/mese) invece che per singolo giorno/periodo
        breve. Esempio: `true`, `"true"` (entrambe le forme osservate).
      - **sharingOptions**: Tipo `boolean` (osservato anche come stringa) — se `true`, abilita la possibilità
        per il cliente di attivare la funzione "sharing" (condivisione/sub-prenotazione della location con
        altri utenti, gestita dalle tabelle `sharing_details`/`sharings`) in fase di prenotazione online.
        Esempio: `false`, `"true"`.
      - **useFulInfo**: Tipo `string` o `null` — testo libero "informazioni utili" mostrato al cliente durante
        la prenotazione online. Esempio: `"Ombrelloni e lettini inclusi nel prezzo."`, `null`.
      - **goodToKnow**: Tipo `string` o `null` — testo libero aggiuntivo ("da sapere") mostrato al cliente.
        Esempio: `"Animali ammessi su richiesta."`, `null`.
      - **bookingLimit**: Tipo `array` di oggetti — limiti minimo/massimo di giorni prenotabili per settore in
        un dato periodo. Ogni elemento ha:
        - `sectorName` (string): nome del settore a cui si applica il limite;
        - `fromDate`/`endDate` (datetime ISO): intervallo in cui il limite è valido;
        - `index` (int): posizione nell'elenco (usata solo per l'editing lato UI);
        - `minvalue`/`maxValue` (string, valore numerico): numero minimo/massimo di giorni consecutivi
          prenotabili per quel settore nel periodo indicato.

        Esempio: `[{"sectorName":"Fila A","fromDate":"2026-06-01T00:00:00.000Z","endDate":"2026-09-15T00:00:00.000Z","index":0,"minvalue":"2","maxValue":"7"}]`
        (da notare, coerentemente con quanto già segnalato altrove nel dominio — es. `warehouses.items` —
        che `minvalue`/`maxValue` sono salvati come **stringhe** anche se concettualmente numerici).
      - **legendList**: Tipo `array` di oggetti `{ icon: string|null, label: string }` — legenda mostrata
        sulla mappa di prenotazione online (es. "Ombrellone libero"/"Ombrellone occupato" con relativa icona
        URL su S3). Esempio: `[{"icon":"https://.../free.png","label":"Ombrellone libero"},{"icon":"https://.../occupied.png","label":"Ombrellone occupato"}]`
      - **allowOnlineBooking**: Tipo `string`. **Aggiornamento**: trovati i valori reali analizzando
        `booking.controller.js:checkContactInProperty` (righe 12556-12595, funzione che decide se una
        prenotazione online può procedere): `"all"` (chiunque può prenotare online, valore di default anche
        quando il campo è assente/falsy — riga 12594: `else if (allowOnlineBooking == "all" || !allowOnlineBooking)`)
        e `"contacts_only"` (la prenotazione online è permessa solo se esiste già una riga in `customers`
        per quella property con lo stesso `mobile` **e** `email` esatti — riga 12592:
        `if (allowOnlineBooking == "contacts_only" && contactData.length == 0) allowBooking = false`).
        Quest'ultimo è il meccanismo concreto dietro l'etichetta "Only contacts registered by the property
        can make a booking" già incontrata come messaggio d'errore nel dominio booking. Nel campione locale
        di `properties` è presente solo il valore `"all"`; l'esistenza di `"contacts_only"` è confermata dal
        codice che lo confronta esplicitamente, non da un dato campionato. Esempio: `"all"`. **Candidato a
        enumerazione**: vocabolario chiuso di due soli valori (`all`/`contacts_only`), stabile e senza
        bisogno di traduzione — caso adatto a un `ENUM` DB nel nuovo schema.
- **Nullabile**: Sì (quasi sempre `NULL` nel campione — verifica più recente sul DB di sviluppo)
- **Esempio di valori**: `{"locationOptionObj":{}}` (valore minimale osservato più di frequente); strutture
  più complete osservate includono `isEnableLocation`, `isEnablePeriod`, `isEnabledMap`, `locations`,
  `period`, `locationOptionObj` valorizzati come descritto nei sotto-campi sopra.

### serviceBookingOptions
- **Tipo**: dichiarato `Sequelize.JSON`; colonna fisica `longtext` con `CHECK json_valid(...)`
- **Vincoli**: `CHECK json_valid(...)`
- **Relazioni**: —
- **Default**: nessuno a livello DB
- **Descrizione**: equivalente semplificato di `onlineBookingOptions.locationOptionObj` ma per la
  prenotazione online dei **servizi**. Struttura più ridotta rispetto alla controparte "location" (niente
  gestione mappa/settori/limiti — coerente col fatto che i servizi non hanno una posizione fisica da
  prenotare come gli ombrelloni).
- **Campi** (confermati da `OnlineBookingIndex.js`, righe 256-279):
  - **addSurcharge**: Tipo `string`/`null` — importo/percentuale di sovrapprezzo sulle prenotazioni online di
    servizi, stesso significato del campo omonimo in `onlineBookingOptions`. Esempio: `"0"`, `null`.
  - **surchargeType**: Tipo `string`, stessi valori ammessi (`Disable`/`percentage`/`fixed`). Stessa
    incoerenza di capitalizzazione (`"Disable"` vs `"disable"`) già segnalata per il campo omonimo in
    `onlineBookingOptions.locationOptionObj` — stesso **Candidato a enumerazione**: `ENUM` DB
    (`disable`/`percentage`/`fixed`), vocabolario chiuso e stabile.
  - **useFulInfo**: Tipo `string` — testo informativo mostrato durante la prenotazione online di un servizio.
    Esempio: `"Prenotazione soggetta a disponibilità."`.
  - **serviceBookingTitle**: Tipo `string` — titolo personalizzato mostrato nella sezione di prenotazione
    online dei servizi. Esempio: `"Prenota i tuoi servizi online"`.
  - **requestBookingConfirmation**: Tipo `string`/`boolean` — come per le location, se valorizzato richiede
    conferma manuale del gestore per le prenotazioni di servizi effettuate online. Esempio: `true`, `"true"`.
  - `blockOnlinePayment` è presente nel codice ma **commentato** (`OnlineBookingIndex.js:266`): campo
    previsto ma non attualmente collegato al salvataggio.
- **Nullabile**: Sì (quasi sempre `NULL` nel campione, coerente con l'assenza di un default a livello DB)
- **Esempio di valori**: `{"addSurcharge":"0","surchargeType":"disable","useFulInfo":null,"serviceBookingTitle":null,"requestBookingConfirmation":false}`

### superAdminOnlineFlag
- **Tipo**: `int` (booleano 0/1)
- **Vincoli**: nessuno
- **Relazioni**: —
- **Default**: `1`
- **Descrizione**: interruttore **di livello piattaforma** (non del gestore) che abilita/disabilita la
  prenotazione/i pagamenti online per lo stabilimento, a prescindere dalle impostazioni proprie del gestore
  (`onlineReservation`/`serviceOnlineBooking`). Impostato a `0` automaticamente dal job schedulato
  `cron-disableproperty.js` (eseguito ogni notte a mezzanotte) quando l'abbonamento dell'utente associato
  allo stabilimento è scaduto (`users.endDate < oggi` e `subscriptionStatus = "enable"`): il cron disattiva
  l'utente (`subscriptionStatus: "disable"`) e contestualmente lo stabilimento
  (`superAdminOnlineFlag: 0`). Oltre al cron, è manovrabile in due punti distinti dal system admin:
  - **Lettura**: `sysadmin.controller.js:getOnlineReservationStatusByAdmin` (righe 726-761) — riceve
    `propertyId` e restituisce `{ isAdminDisableOnLinePayment: true }` quando `superAdminOnlineFlag == 1` e
    `{ isAdminDisableOnLinePayment: false }` quando `== 0`. **Nota**: il nome del campo restituito
    (`isAdminDisableOnLinePayment`) è fuorviante rispetto al valore che porta — non inverte il flag, lo
    rispecchia 1:1 (flag `1` → campo `true`), nonostante il nome suggerisca "il pagamento online è
    disabilitato dall'admin" quando in realtà `1` significa che l'online **è consentito**.
  - **Scrittura**: `user.controller.js:updatesuperadmin` (righe 458-506) — endpoint con cui il system admin
    aggiorna un utente "superadmin" (gestore di uno stabilimento), tipicamente in occasione del rinnovo
    licenza; tra i campi aggiornati imposta `Property.superAdminOnlineFlag = req.body.onlineReservation`
    (riga 483 — da notare che il nome del campo nel body della richiesta, `onlineReservation`, non coincide
    col nome della colonna scritta, `superAdminOnlineFlag`: sono due cose diverse che condividono solo
    l'etichetta usata nel form del pannello sysadmin).
  È quindi il meccanismo con cui la piattaforma **sospende le vendite online** di uno stabilimento moroso
  senza toccarne i dati o le altre funzionalità. **Distribuzione nel DB locale**: `1` → 77 righe, `0` → 23
  righe.
  **Aggiornamento (campione più ampio, 4135 righe)**: `1` → 4109 righe, `0` → 26 righe — proporzione confermata.
- **Nullabile**: Sì
- **Esempio di valori**: `1`, `0`
- **Candidato a enumerazione**: booleano (0/1), non un vocabolario da enumerare — tipizzare come
  `BOOLEAN`/`BIT` reale nel nuovo schema.

### commissionAmount
- **Tipo**: dichiarato `Sequelize.JSON`; colonna fisica `longtext` con `CHECK json_valid(...)`
- **Vincoli**: `CHECK json_valid(...)`
- **Relazioni**: —
- **Default**: `{ onlineCommission: 6, iframeCommission: 6 }` (sia a livello di model Sequelize sia,
  coerentemente, nei dati osservati — ogni riga campionata ha esattamente questo valore)
  **Aggiornamento/correzione (verifica su un campione più ampio del DB di sviluppo, 4135 righe)**: il quadro
  osservato sopra vale solo per il piccolo campione originale. Su scala più ampia il campo risulta invece
  **quasi sempre `NULL`** (4131 righe su 4135, oltre il 99,9%) — non popolato per la stragrande maggioranza
  degli stabilimenti. Nelle righe valorizzate, oltre all'atteso oggetto JSON (es.
  `{"onlineCommission":6,"iframeCommission":0}`, da notare `iframeCommission` diverso dal default `6` in
  questo esempio reale), sono stati osservati anche **valori scalari "sporchi"** non-JSON, es. la stringa
  `"2"` o `"4"` da sole, senza la struttura a oggetto attesa — un'incoerenza di tipo concreta dentro la
  stessa colonna (a volte JSON valido, a volte un numero scritto come stringa semplice), che andrebbe
  gestita con attenzione in un'eventuale migrazione dati (un parsing ingenuo che assume sempre
  `JSON.parse(...).onlineCommission` fallirebbe su queste righe).
- **Descrizione**: percentuale di commissione applicata sulle vendite online dello stabilimento, differenziata
  per canale (sito diretto vs widget iframe embeddato su siti terzi). Esposto in lettura/scrittura
  dall'endpoint sysadmin `getpropertydetail`/`updateproperty` (`sysadmin.controller.js`, righe 176, 533) —
  quel percorso resta senza consumer verificato, come nella versione precedente di questa scheda.
  **Trovato però il vero consumer, dopo l'analisi del secondo frontend `front-customer-pre`** (il sito
  pubblico di prenotazione, assente da `front-pre`): il campo arriva al cliente finale tramite la risposta
  che porta anche `onlineBookingOptions`/`paymentOptions` (lo stesso payload di `getOnlineBookingSettings`
  già documentato sopra, letto in `PropertyDetails.js` come `resData?.commissionAmount` — coerente con la
  presenza sia di `onlineCommission` sia di `iframeCommission` nella stessa risposta) e viene usato **due
  volte** nel flusso di checkout:
  1. **Selezione del tasso** — `Summary.js:componentDidMount` (righe 232-241): sceglie
     `commissionAmount.iframeCommission` se la prenotazione avviene dentro un iframe imbottito su un sito
     terzo (`isIframe === "iframe"`), altrimenti `commissionAmount.onlineCommission`; fallback `?? 6` se il
     valore manca — coerente col default lato backend.
  2. **Calcolo dell'importo** — il tasso scelto (`isCommition`) è passato come `commissionPercent` a
     `ArithmaticService.js:calculateBookingTotal` (righe 29-77), chiamata da `Summary.js` (riga 801) al
     momento di costruire il riepilogo prenotazione:
     ```js
     baseCommissionAmount = depositNowBalFlag
       ? ((baseBookingPrice / 2) * commissionPercent) / 100 + 0.5
       : isPayNowflag
         ? (baseBookingPrice * commissionPercent) / 100 + 0.5
         : 0;
     ```
     Il risultato (`commissionAmount`, `payableAmount`, `subTotal`, `storeSubtotalPrice` — tutti calcolati
     **nel browser**) finisce in `bookingPaymentObject.isCumstomerBookPrice` (`Summary.js`, riga 833), che fa
     parte del `booking_payments` inviato al backend alla creazione della prenotazione (lo stesso payload
     `booking_payments`/`bookingPaymentData` già segnalato come accettato senza ricalcolo server-side in
     [01-criticita-e-indicazioni-backend.md §3.4](../../docs/01-criticita-e-indicazioni-backend.md)). Lo
     stesso valore (`isCumstomerBookPrice.commissionAmount`) viene riletto più avanti nel checkout
     (`CheckoutForm.js`, righe 503-514, e lo stesso pattern in `PropertySignIn.js`, righe 1425-1436) per
     comporre l'importo finale passato a Stripe per i pagamenti "paga ora"/"acconto".
     **Conferma diretta della tua descrizione**: il backend restituisce `commissionAmount`, il frontend lo
     usa per calcolare l'importo (comprensivo di commissione) da inviare al backend/a Stripe — non un
     semplice pass-through.
  - **Eccezioni hardcoded nel frontend, non guidate da questo campo**: `calculateBookingTotal` azzera la
    commissione (`baseCommissionAmount = 0`) per due casi verificati in
    `front-customer-pre/src/services/CommonServices.js` (righe 929-935):
    ```js
    const COMMISSION_FREE_PROPERTY_IDS = [10588, 10723, 10828, 10732, 10561];
    const CONNECTED_AND_PLATFORM_ACCOUNT_PROPERTIES = [10556];
    export const isCommissionFreeProperty = (propertyId) => COMMISSION_FREE_PROPERTY_IDS.includes(propertyId);
    export const isCommissionbothProperty = (propertyId) => CONNECTED_AND_PLATFORM_ACCOUNT_PROPERTIES.includes(propertyId);
    ```
    Sei `propertyId` specifici (5 "commission free" via iframe + 1 "connected and platform account") sono
    quindi esenti dalla commissione **indipendentemente dal valore di `properties.commissionAmount` nel DB**
    — un'eccezione di business hardcoded nel bundle del sito pubblico, invisibile al backend e a chi
    modificasse solo la riga della tabella `properties`. Da portare nel nuovo modello come dato (es. un
    flag/piano commerciale sull'entità property), non come lista di ID nel codice sorgente del frontend.
  - **App mobile (`front-app-pre`)**: nessun riferimento a `commission`/`commissionAmount` in tutto il
    sorgente dell'app (ricerca esaustiva, 0 file). L'app include però una schermata
    `BookingWebView.screen.js` che incorpora una `WebView` puntata su `summerbooking.it/{url}?ismobile=true`
    (cioè sul sito pubblico `front-customer-pre`) — plausibile che il calcolo del totale/commissione avvenga
    delegando quella parte di flusso al sito web invece di reimplementarla nativamente, ma non ho verificato
    con certezza in quali punti esatti del percorso di prenotazione l'app passi alla WebView, quindi non lo
    do per confermato senza ulteriore verifica.
- **Nullabile**: Sì (quasi sempre `NULL`, vedi aggiornamento sopra)
- **Esempio di valori**: `{"onlineCommission":6,"iframeCommission":0}` (JSON valido); `"2"`, `"4"` (valori
  scalari sporchi/non-JSON osservati, vedi nota sopra)
- **Campi**:
  - **onlineCommission**
    - Tipo: `int`/`decimal` (osservato come intero, `6`)
    - Vincoli: nessuno esplicito
    - Relazioni: —
    - Default: `6`
    - Descrizione: percentuale di commissione applicata sulle prenotazioni concluse sul sito di prenotazione
      diretto (non dentro un iframe) — letta in `Summary.js:237-238` come ramo `else` della scelta iframe/non
      iframe.
    - Esempio di valori: `6`
  - **iframeCommission**
    - Tipo: `int`/`decimal` (osservato come intero, `6`)
    - Vincoli: nessuno esplicito
    - Relazioni: —
    - Default: `6`
    - Descrizione: percentuale di commissione applicata quando la prenotazione avviene dentro il widget
      iframe embeddabile su siti esterni (`isIframe === "iframe"`, `Summary.js:236-237`) — salvo che la
      property non rientri nelle eccezioni hardcoded `COMMISSION_FREE_PROPERTY_IDS` (vedi sopra), nel qual
      caso la commissione applicata è comunque `0` a prescindere da questo valore.
    - Esempio di valori: `6`, `0` (osservato anche `0`, diverso dal default — vedi esempio reale sopra)

### fiskalyFlag
- **Tipo**: `int`
- **Vincoli**: nessuno a livello DB; enumerato a livello applicativo (dichiarato esplicitamente in
  `property.controller.js`, righe 1112-1124, e ripetuto nello `swagger.yaml`, righe 16431-16492):
  - `0` = `NOT_CONFIGURED` — integrazione fiscale non configurata
  - `1` = `VAT_CONFIGURED` — configurata solo la gestione IVA (senza integrazione Fiskaly)
  - `2` = `FISKALY_CONFIGURED` — integrazione completa con Fiskaly (scontrino fiscale elettronico) attiva
- **Relazioni**: —
- **Default**: `0`
- **Descrizione**: rappresenta **lo step di avanzamento nel percorso di attivazione della fiscalità
  elettronica** che lo stabilimento sta seguendo (non configurato → solo IVA → Fiskaly attivo), **non** un
  semplice interruttore statico. È letto/scritto tramite gli endpoint dedicati
  `getFiskalyFlag`/`updateFiskalyFlag` (`property.controller.js`, righe 1126-1203), che derivano il
  `propertyId` dall'utente autenticato (`req.userId`), non dal body. Il valore **non viene mai avanzato in
  automatico dal backend** in base ad altre azioni (es. completare la configurazione dell'account Fiskaly non
  imposta da solo `fiskalyFlag = 2`): è il chiamante (il pannello di configurazione fiscale nel gestionale) a
  doverlo aggiornare esplicitamente man mano che l'utente avanza nei passaggi di attivazione.
  **Verificato come condizione di accesso reale**: `fiskalyAccountService.js:getCashRegisters` (righe
  1610-1634) — funzione che risponde all'azione frontend `_getCashRegisters()`
  (`front-pre/src/data/actions/fiscalizationAction.js:7-11`, chiamata `GET /sb/electronic-receipt/get-cash-registers`)
  — verifica esplicitamente `property.fiskalyEnabled === 1` **e** rifiuta con `404 "Fiskaly not available for
  this property"` se `property.fiskalyFlag === 0 || property.fiskalyFlag === 1`: le casse/registratori di
  cassa Fiskaly sono quindi visibili solo quando lo stabilimento ha completato lo step `2`
  (`FISKALY_CONFIGURED`). È questo il meccanismo concreto con cui il valore del campo blocca/sblocca le
  funzionalità Fiskaly più avanzate mano a mano che l'utente procede nei passaggi di attivazione.
  **Effetto concreto lato UI, tracciato a ritroso**: `front-pre/src/components/printerSetting/PrintReservationCta.js`
  (righe 337-378, `integrateOnlineSoftwarePrinterIfAvailable`) chiama `_getCashRegisters()` durante il flusso
  di stampa/incasso di una prenotazione; se la chiamata fallisce con `404` (cioè `fiskalyFlag !== 2` o
  `fiskalyEnabled !== 1`), il codice lo interpreta esplicitamente come *"user is not enabled to use
  electronic receipt features, nothing to do"* (commento riga 374) e **non mostra il pulsante di stampa
  scontrino elettronico** durante l'incasso; se invece la chiamata ha successo, la stampante software online
  viene aggiunta alla lista delle stampanti disponibili per quella prenotazione. È quindi questo l'unico
  punto verificato in cui il valore di `fiskalyFlag` cambia concretamente cosa vede l'operatore in cassa.

  **Mappatura esatta degli step, trovata nella riscrittura moderna** (`front-modern-pre`, che implementa lo
  stesso wizard da zero — la fonte più affidabile su come il campo è *pensato* per funzionare):
  `pages/Fiscalization/Fiscalization.tsx` (righe 39-80) legge `fiskalyFlag` insieme a un secondo stato
  indipendente, `accept_terms` (endpoint `GET/POST /api/sb/electronic-receipt/get-terms`/`update-terms`,
  gate anch'esso su `fiskalyEnabled === 1` — vedi `fiskalyAccount.controller.js:updateTerms`, riga 48 — e
  che valorizza `fiskaly_account.accept_terms`, lo stesso campo già documentato sotto `fiskalyAutoreceipt`),
  per decidere quale pagina dello wizard mostrare:
  | `fiskalyFlag` | `accept_terms` | Step mostrato |
  |---|---|---|
  | `0` NOT_CONFIGURED | `false` | 0 — `TermsAndConditions` (accetta i termini) |
  | `0` NOT_CONFIGURED | `true` | 1 — `ServicesReview` (revisione servizi/configurazione IVA) |
  | `1` VAT_CONFIGURED o `2` FISKLAY_CONFIGURED | `true` | 2 — `AccountConfiguration` (creazione/gestione account Fiskaly) |

  Ed è lì che si trovano i punti esatti in cui il frontend avanza il flag (confermando che **è sempre
  un'azione esplicita del client**, mai un side-effect calcolato dal backend):
  - `pages/Fiscalization/ElectronicReceipt/ServicesReview.tsx:267` → al completamento dello step,
    `updateFlag(VAT_CONFIGURED)` (`1`);
  - `pages/Fiscalization/ElectronicReceipt/AccountConfiguration.tsx:407` → al completamento dello step,
    `updateFlag(FISKLAY_CONFIGURED)` (`2`).

  (Da notare: anche l'enum TypeScript della riscrittura moderna riporta lo stesso refuso del backend legacy,
  `FISKLAY_CONFIGURED` invece di `FISKALY_CONFIGURED` — segno che il nome del valore è stato copiato dal
  contratto API esistente piuttosto che corretto, cosa sensata visto che è solo un'etichetta lato client e
  il valore numerico `2` è l'unico dato che conta.)

  **Distribuzione nel DB locale**: `0` → 94 righe, `2` → 5 righe, `1` → 1 riga.
  **Aggiornamento (campione più ampio, 4135 righe)**: quadro confermato — `0` (`NOT_CONFIGURED`) resta
  nettamente maggioritario, con `4133` righe su `4135` sul totale aggregato dei tre campi Fiskaly
  (`fiskalyFlag`/`fiskalyEnabled`/`fiskalyAutoreceipt`, verifica congiunta) a `0`; i valori residui `1`/`2`
  restano una minoranza esigua, coerente col fatto che il modulo fiscale è una feature opzionale poco
  diffusa nel campione.
- **Nullabile**: Sì
- **Esempio di valori**: `0` (`NOT_CONFIGURED`), `1` (`VAT_CONFIGURED`), `2` (`FISKALY_CONFIGURED`)
- **Candidato a enumerazione**: vocabolario chiuso, stabile, già nominato esplicitamente nel codice
  (`property.controller.js`, `swagger.yaml`) e persino replicato identico (refuso compreso,
  `FISKLAY_CONFIGURED`) nella riscrittura moderna — caso da manuale per un `ENUM` DB reale
  (`NotConfigured`/`VatConfigured`/`FiskalyConfigured`) invece dell'intero arbitrario oggi usato, senza
  bisogno di una tabella di lookup (non richiede traduzione, è uno stato tecnico di avanzamento).

### fiskalyEnabled
- **Tipo**: `int` (booleano 0/1)
- **Vincoli**: nessuno
- **Relazioni**: —
- **Default**: `0`
- **Descrizione**: interruttore master, indipendente da `fiskalyFlag`, che indica se l'integrazione Fiskaly è
  operativamente attiva per lo stabilimento. Letto/scritto tramite gli endpoint dedicati
  `fiskalyAccount.controller.js:getFiskalyEnabled`/`setFiskalyEnabled` (righe 300-332, 466-514): quest'ultimo
  impone esplicitamente la regola "non può esistere `fiskalyAutoreceipt = 1` con `fiskalyEnabled = 0`"
  (righe 487-489: rifiuta la richiesta con `400` se il chiamante prova a impostare questa combinazione) e,
  se `fiskalyEnabled` viene portato a `0`, forza automaticamente anche `fiskalyAutoreceipt = 0` nello stesso
  update (righe 498-503). È verificato, **come condizione indipendente da `fiskalyFlag`**, in due punti reali
  del codice:
  - `fiskalyAccountService.js:getCashRegisters` (riga 1628): deve essere `=== 1`, altrimenti `404`.
  - `fiskalyReceiptManagerService.js:createAutoReceiptFromStripe` (righe 1976-1982 e 2092-2104): deve essere
    `=== 1` (insieme a `fiskalyAutoreceipt === 1` e ad altre condizioni — vedi il campo `fiskalyAutoreceipt`
    sotto), altrimenti l'emissione automatica dello scontrino non procede.
  - `fiskalyAccount.controller.js:updateTerms` (riga 48): deve essere `=== 1`, altrimenti `403` — l'intero
    step "accetta i termini" del wizard fiscale è bloccato senza questo flag.
  - Lato UI, anche l'intera pagina del pannello fiscale moderno (`front-modern-pre/src/pages/Fiscalization/Fiscalization.tsx:102`)
    non renderizza nulla (`return null`) se `fiskalyEnabled.value !== true` — coerente col fatto che è un
    interruttore master indipendente da `fiskalyFlag`.
  In sintesi: `fiskalyEnabled` è il master-switch "l'integrazione Fiskaly è attiva per questo stabilimento";
  `fiskalyFlag` è lo stato/step di avanzamento della configurazione (vedi sopra) usato in aggiunta, non al
  posto, di questo flag. Il codice non lascia dedurre perché servano entrambi invece di un solo stato
  consolidato (es. perché non derivare `fiskalyEnabled` direttamente da `fiskalyFlag === 2`): è una
  ridondanza di modellazione da valutare col committente in fase di progettazione del nuovo modello dati, ma
  — a differenza di quanto scritto in una versione precedente di questa scheda — **entrambi i campi sono
  attivamente controllati a runtime**, non è un campo morto. **Distribuzione nel DB locale**: `0` → 91 righe,
  `1` → 9 righe.
- **Nullabile**: Sì
- **Esempio di valori**: `0`, `1`
- **Candidato a enumerazione**: booleano (0/1) — tipizzare come `BOOLEAN`/`BIT` reale, non serve un `ENUM`.

### fiskalyAutoreceipt
- **Tipo**: `int` (booleano 0/1)
- **Vincoli**: nessuno
- **Relazioni**: —
- **Default**: `0`
- **Descrizione**: se `1` (e con `fiskalyEnabled = 1` sulla stessa proprietà — vincolo imposto anche in
  scrittura da `setFiskalyEnabled`, vedi campo precedente), abilita l'emissione **automatica** dello
  scontrino fiscale elettronico non appena un pagamento Stripe online risulta andato a buon fine, invece di
  richiedere l'emissione manuale da parte dell'operatore.
  **Correzione rispetto a una versione precedente di questa scheda**: la logica non è disattivata. Esiste
  una funzione realmente attiva e collegata a una rotta pubblica,
  `FiskalyReceiptManagerService.createAutoReceiptFromStripe(stripeTransactionId)`
  (`fiskalyReceiptManagerService.js`, righe 1958-2205), invocata da
  `fiskalyReceipt.controller.js:createAutoReceipt` (riga 830) sulla rotta
  `POST /api/sb/electronic-receipt/auto-create` (`user.routes.js`, righe 2403-2409), montata con
  `authJwt.optionalAuth` — commento nel codice: *"Auto Receipt API (no auth required, called from frontend
  after Stripe payment)"*. (Esiste anche, più sotto nello stesso file service, un metodo statico
  `createAutoReceipt(requestDto)` alle righe 2222+ **interamente commentato**: è un secondo tentativo di
  implementazione, più vecchio, mai completato — da non confondere con `createAutoReceiptFromStripe`, che è
  la funzione realmente in uso.)
  La funzione attiva, letta per intero, verifica in sequenza (interrompendo il flusso e notificando
  cliente/stabilimento in caso di esito negativo su una qualunque condizione):
  1. la transazione Stripe (`online_transaction_details`, cercata per `stripe_transaction_id`) esiste;
  2. `property.fiskalyEnabled === 1` **e** `property.fiskalyAutoreceipt === 1` (righe 1976-1982);
  3. la transazione ha `status === "succeeded"`, `payment_type === "pay_now"` e una `transaction_date`
     valorizzata (righe 2060-2062);
  4. la prenotazione (booking o service reservation) collegata risulta interamente pagata (`toPay === 0`,
     riga 2077/2079);
  5. esistono un `BookingNumberInformations`, un `FiskalyAccount` con `accept_terms === 1` e una
     `FiskalyCashRegister` di default con `system_state === "COMMISSIONED"` per la property (righe 2092-2104);
  6. l'importo pagato nell'ultimo movimento non è zero (righe 2119-2122);
  solo se tutte le condizioni sono soddisfatte chiama `createReceipt(...)` (con fino a `MAX_RETRIES`
  tentativi) per emettere realmente lo scontrino, con `payment.type = ONLINE` e sorgente marcata
  `"autoreceipt"`. **Non risulta però una chiamata a questo endpoint nel codice React disponibile in
  `front-pre/src`** (nessun match per `auto-create`/`createAutoReceipt`/`autoreceipt` in `front-pre/src`,
  a parte una stringa di traduzione): la rotta e la logica lato server sono verificatamente attive e
  complete, ma il punto di chiamata dal frontend indicato dal commento nel codice backend non è presente
  in questa copia del repository — non è quindi possibile confermare da qui se sia invocato da una versione
  del frontend non inclusa in questa copia, da un redirect/webhook, o se sia codice predisposto ma non ancora
  collegato lato client. **Distribuzione nel DB locale**: `0` → 92 righe, `1` → 8 righe.
- **Nullabile**: Sì
- **Esempio di valori**: `0`, `1`
- **Candidato a enumerazione**: booleano (0/1) — tipizzare come `BOOLEAN`/`BIT` reale, non serve un `ENUM`.

### contractDocumentsId
- **Tipo**: `int`
- **Vincoli**: nessun vincolo/FK a livello DB (verificato su `information_schema.KEY_COLUMN_USAGE`: nessuna
  foreign key definita sulla colonna, nonostante l'associazione Sequelize dichiarata — è quindi una relazione
  **solo applicativa**)
- **Relazioni**: `belongsTo` verso [`contract_documents`](contract_documents.md)`.id` (associazione
  Sequelize dichiarata in `models/index.js:482-487`); la registrazione dell'accettazione è in
  [`contract_acceptation`](contract_acceptation.md) (1 riga per `propertyId`). Entrambe le tabelle sono
  ora documentate e collocate in **C5** (sottoarea `account`/contratti); **questo campo
  `contractDocumentsId` resta invece in C2** come stato dello stabilimento.
- **Default**: `NULL` (`allowNull: true`); nel campione dati locale **sempre `NULL`** per tutte le 100 righe —
  la feature "contratto/addendum da accettare" non risulta ancora utilizzata nei dati di test disponibili
- **Descrizione**: quando valorizzato, identifica il documento contrattuale (`contract_documents`, es. un
  addendum/versione di contratto commerciale) che questo specifico stabilimento deve accettare — abilita la
  feature "contract condition" (tabella collegata `contract_acceptation`, che traccia se/quando lo
  stabilimento ha accettato il documento). Commento esplicito nel model: *"FK to contract_documents: when
  set, the contract-condition feature is enabled for this property"* — è quindi un campo opzionale che
  attiva un intero sotto-flusso solo per gli stabilimenti per cui è impostato. Comportamento verificato in
  `contractConditionService.js` (righe 63-153), usato dagli endpoint `GET /api/sb/contract-condition` e
  `POST /api/sb/contract-condition/update` (accessibili solo a utenti con ruolo `admin`/`superadmin`,
  verificato via `isAdminOrSuperAdmin`, righe 20-25):
  - se `contractDocumentsId` è `NULL`, la lettura risponde `{ enabled: false }` e un tentativo di
    accettazione risponde con errore `FEATURE_DISABLED` — la feature è quindi silenziosamente "spenta" per
    quello stabilimento, non è un errore per il chiamante;
  - se valorizzato, la lettura risolve il documento (`contract_documents`) e l'eventuale accettazione già
    registrata (`contract_acceptation`, una per property);
  - l'accettazione (`accepted: true`) **scarica realmente il PDF** all'URL `contract_documents.link` e ne
    calcola l'hash SHA-256 (righe 43-52, 116-123), salvato insieme a `userId`, `ipAddress` (normalizzato da
    IPv6-mapped/loopback, righe 14-33) e timestamp in `contract_acceptation` — un meccanismo di
    tracciabilità dell'accettazione più robusto del semplice flag booleano che il nome della tabella
    potrebbe suggerire.

  **Consumer frontend trovato** (assente sia in `front-pre` sia in `front-customer-pre`/`front-app-pre`, ma
  presente e attivo nella riscrittura moderna): `front-modern-pre/src/app/Conditions/ConditionsApi.ts`
  implementa esattamente questo flusso — chiama `getContractCondition()` al bootstrap
  (`loadConditions()`, righe 97-130) e, se `enabled === true && accepted === false` (cioè
  `contractDocumentsId` impostato ma il documento corrente non ancora accettato da questo stabilimento),
  **apre automaticamente una modale bloccante** ("Aggiornamento delle Condizioni Contrattuali", riga 80) che
  l'utente non può ignorare senza accettare (`updateConditions()`, righe 132-159, chiama
  `updateContractCondition(isChecked)`). È quindi un meccanismo di "consenso obbligatorio" mostrato ad ogni
  accesso finché l'utente non accetta, non un semplice banner informativo.
- **Nullabile**: Sì (di gran lunga il valore prevalente)
- **Esempio di valori**: `NULL` (praticamente sempre — verifica più recente sul DB di sviluppo: 4134 righe su
  4135 hanno il campo `NULL`), `1` (unico valore reale osservato, un solo stabilimento). **Non marcato come
  deprecato**: a differenza di un campo vestigiale, il testo sopra documenta un flusso applicativo completo e
  attivo (`contractConditionService.js`, endpoint dedicati, consumer nella riscrittura moderna
  `front-modern-pre`) — la quasi totale assenza di valorizzazione è coerente con una feature opt-in appena
  introdotta e non ancora attivata per la maggior parte degli stabilimenti, non con un campo abbandonato.

---

## Tabelle collegate

`properties.id` è referenziato da un numero molto ampio di tabelle (65, contando tutte quelle con una colonna
`propertyId`/`property_id`), ma **solo 12** hanno un vincolo di integrità referenziale reale a livello di
database (verificato su `information_schema.KEY_COLUMN_USAGE`); tutte le altre relazioni sono mantenute solo
a livello applicativo (nessun `FOREIGN KEY` nello schema), coerentemente con quanto già segnalato in
[01-criticita-e-indicazioni-backend.md](../../docs/01-criticita-e-indicazioni-backend.md) sulla modellazione
dati destrutturata del sistema legacy.

### Con vincolo FK reale nel DB

| Tabella | Colonna | Nome vincolo |
|---|---|---|
| `bathouses` | `propertyId` | `bathouses_ibfk_1` |
| `users` | `propertyId` | `users_ibfk_1` |
| `hotels` | `propertyId` | `hotels_ibfk_1` |
| `contract_acceptation` | `propertyId` | `contract_acceptation_ibfk_1` |
| `fiskaly_account` | `property_id` | `fiskaly_account_ibfk_1` |
| `fiskaly_receipt_log` | `property_id` | `fiskaly_receipt_log_ibfk_5` |
| `switchboard_config` | `property_id` | `switchboard_config_ibfk_3` |
| `switchboard_audio_file` | `property_id` | `switchboard_audio_file_ibfk_1` |
| `switchboard_call_log` | `property_id` | `switchboard_call_log_ibfk_3` |
| `switchboard_wa_template` | `property_id` | `switchboard_wa_template_ibfk_1` |
| `chatbot_history` | `property_id` | `chatbot_history_ibfk_1` |
| `chatbot_manager_config` | `property_id` | `chatbot_manager_config_ibfk_1` |

### Associazioni Sequelize dichiarate esplicitamente (in aggiunta o a integrazione delle FK sopra)

Da `models/index.js`: `property.hasMany(bathouse)`, `property.hasMany(seasons)`,
`property.hasMany(StripeConfig)`, `property.hasMany(discounts)`, `property.hasMany(fiskalyAccount)`,
`property.hasMany(fiskalyReceiptLog)`, `property.hasMany(hotel)`, `property.hasMany(hotelMessageTemplate)`,
`property.hasOne(switchboardConfig)`, `property.hasMany(switchboardAudioFile)`,
`property.hasMany(switchboardWaTemplate)`, `property.hasMany(switchboardCallLog)`,
`property.belongsTo(contractDocuments)` (via `contractDocumentsId`), `property.hasOne(contractAcceptation)`,
`user.belongsTo(property)`.

### Relazione applicativa soltanto (colonna `propertyId`/`property_id` presente, nessuna associazione
Sequelize dichiarata, nessuna FK) — elenco completo delle restanti tabelle collegate

`additional_items_counts_details`, `bathhouse_email_configrations`, `beacharound_reservation`, `booking`,
`booking_absence_details`, `booking_absence_details_archive_03072026`, `booking_areas`, `booking_groups`,
`booking_items_counts`, `booking_items_counts_details`, `booking_items_counts_details_archive_03072026`,
`booking_items_counts_details_news`, `booking_logs`, `booking_payments`, `booking_receipts`, `bookings`,
`bookings_archive_03072026`, `bookings_old`, `card_transactions`, `configurations`, `configure_maps`,
`contact_list`, `custom_templates`, `customers`, `discounts`, `email_logs`, `hold_locations`,
`hotel_message_templates`, `hourly_service_booking_management`, `location_reservations`,
`location_reservations_old`, `marketing_automation`, `marketing_email_campaign`, `marketing_event`,
`notification_details`, `online_reservations`, `online_transaction_details`, `price_lists`,
`printer_configs`, `property_search_regions`, `receipt_serial_numbers`, `revenue_notifications`,
`seasons_periods`, `sectors_rows`, `service_reservation`, `service_reservation_payments`,
`services_booking_counts`, `services_types`, `sharing_details`, `sharing_details_archive_03072026`,
`stripe_configrations`, `transactions_payments`, `warehouses`.

> Sono le tabelle candidate ad avere una relazione dichiarata esplicitamente (con vincolo FK reale) nel nuovo
> schema relazionale .NET/EF Core, coerentemente con l'abbandono del modello "colonna presente ma non
> vincolata" adottato nel sistema legacy.

---

## Osservazioni per la riscrittura (riepilogo)

- **`paymentOptions`/`servicebookingPaymentOptions`**: la struttura realmente in uso (5 chiavi booleane:
  `pay_now_info`, `pay_at_property_info`, `pay_at_property_no_credit_info`, `deposit_now_half_info`,
  `pay_at_property_prepaid_info`) va modellata come colonne relazionali tipizzate (o un value object), non
  come JSON — sono un set fisso e piccolo di opzioni, non dati dinamici. I due default incoerenti presenti
  nel codice legacy (`pay_in_structure_info`/`advanced_balance_structure_info`) sono debito da **non**
  portare nel nuovo modello.
- **`onlineBookingOptions`**: è il campo JSON più articolato della tabella; nella riscrittura andrebbe
  scomposto in entità relazionali distinte — configurazione generale (surcharge, conferma manuale, mappa),
  limiti di prenotazione per settore/periodo (`bookingLimit`, oggi una tabella EAV-like dentro il JSON, in
  realtà una vera tabella `booking_limits(sectorId, fromDate, endDate, minDays, maxDays)`), e legenda mappa.
  Il fatto che questi limiti siano oggi salvati ma **non applicati lato server** (vedi
  [01-criticita-e-indicazioni-backend.md §11.6](../../docs/01-criticita-e-indicazioni-backend.md)) è un
  comportamento da correggere nella riscrittura, non da riprodurre.
- **`superAdminOnlineFlag` vs `onlineReservation`/`serviceOnlineBooking`**: sono due livelli di controllo
  concettualmente diversi (piattaforma vs gestore) che vanno mantenuti distinti nel nuovo modello, ma
  varrebbe la pena esprimerli in modo più esplicito (es. un `SubscriptionStatus` sull'account e una policy di
  autorizzazione che lo controlla, invece di un flag booleano duplicato sulla tabella property).
- **`commissionAmount`**: consumer trovato in `front-customer-pre` (sito pubblico di prenotazione, non
  presente in `front-pre`) — il tasso (`onlineCommission`/`iframeCommission` a seconda del canale) è usato
  client-side da `ArithmaticService.calculateBookingTotal` per calcolare l'importo di commissione che si
  somma al prezzo base e finisce nel `booking_payments` inviato al backend e nell'importo passato a Stripe.
  Come per il resto dei prezzi (§3.4 del documento di criticità), **il calcolo avviene solo nel browser e il
  backend non lo ricalcola**: nella riscrittura il calcolo commissione va portato server-side. Da portare
  anche i due elenchi di eccezioni oggi hardcoded nel frontend (`COMMISSION_FREE_PROPERTY_IDS`,
  `CONNECTED_AND_PLATFORM_ACCOUNT_PROPERTIES`, 6 `propertyId` specifici in
  `front-customer-pre/src/services/CommonServices.js:929-930`) come dato di dominio (es. un piano
  commerciale/flag sulla property), non come lista di ID nel codice sorgente.
- **Pannelli di configurazione duplicati**: `onlineReservation`, `paymentOptions` e la tabella collegata
  `online_reservations` hanno **due** flussi di scrittura nel codice legacy — quello attivo
  (`OnlineBookingIndex.js` → `UpdateOnlineBookingSettings`) e uno più vecchio rimasto nel codice ma con il
  rendering disattivato (`EnableDisableReservation.js`/`ReservationParent.js` → `addonlinereservation`/
  `updateonlinereservation`, commentato in `SettingContent.js:178`) i cui endpoint restano comunque
  richiamabili direttamente. Nella riscrittura va portata **solo** la logica del flusso attivo; il flusso
  morto (e la tabella `online_reservations`, con dati carta in chiaro) è candidato a non essere migrato,
  previa conferma col committente che non serva più.
- **Campi anagrafici di registrazione mai realmente popolati**: `country` (sempre stringa vuota, confermato
  su tutti e tre gli entry point — `front-pre`, `front-customer-pre`, `front-app-pre`) e `mobileCountryCode`
  (mai valorizzato: mismatch di nome col frontend web, `countryCode` vs `mobileCountryCode`; assente del
  tutto nel form dell'app) sono colonne che esistono nello schema e nel model ma che, in nessuno dei tre
  flussi di registrazione oggi in uso, vengono mai popolate con un valore reale. Da decidere col committente
  se recuperare la raccolta di questi dati (ha senso per un campo come il prefisso telefonico) o eliminarli
  dal nuovo modello.
- **`photos` in registrazione: comportamento incoerente tra i tre frontend**, non un campo semplicemente
  "spento": `front-pre` ha l'upload disabilitato (commentato nel JSX), mentre `front-customer-pre` (stesso
  form, copia quasi identica) e `front-app-pre` (app mobile, dove l'upload è addirittura obbligatorio) lo
  hanno attivo. Nella riscrittura, oltre a portare la raccolta foto come funzionalità sempre attiva e
  coerente, va isolato/rimosso il codice del form `front-pre` con l'upload disattivato, che oggi è solo
  debito silenzioso (nessun errore, semplicemente non raccoglie l'informazione).
- **`fiskalyFlag` vs `fiskalyEnabled`**: **entrambi i campi sono attivamente usati come condizioni di
  accesso reali** (non uno dei due morto): `fiskalyFlag` traccia lo step di avanzamento nel percorso di
  attivazione fiscale (0 → 1 → 2, aggiornato esplicitamente dal client al completamento di ogni step del
  wizard — vedi la mappatura esatta sopra, confermata anche dalla riscrittura moderna in
  `front-modern-pre/src/pages/Fiscalization/`) e deve valere `2` per sbloccare la gestione dei registratori
  di cassa (`fiskalyAccountService.getCashRegisters`); `fiskalyEnabled` è il master-switch verificato sia lì
  sia nel flusso di scontrino automatico (`createAutoReceiptFromStripe`). **La riscrittura moderna
  riproduce la stessa distinzione a due campi** (non li unifica), quindi è ragionevolmente una scelta di
  design intenzionale — probabilmente `fiskalyEnabled` = "il modulo fiscale è visibile/attivabile per questo
  stabilimento" (una decisione di business/piano, verosimilmente impostata dal system admin) mentre
  `fiskalyFlag` = "a che punto del wizard è arrivato il gestore" — comunque da confermare col committente
  prima di riprodurla nel nuovo modello, invece di dare per scontata l'interpretazione.
- **`propertyOwner`**: il codice legacy stesso lo tratta ormai come riferimento potenzialmente stale e gli
  preferisce sistematicamente la relazione `users.propertyId` + ruolo per individuare il gestore da
  notificare (vedi sopra). Nella riscrittura va deciso se mantenere un concetto di "proprietario storico
  della registrazione" separato dagli utenti con ruolo gestionale sulla property, o se unificare i due.
- **Relazioni**: nel nuovo schema EF Core, tutte le ~65 tabelle che oggi referenziano `properties.id` solo a
  livello applicativo devono avere una vera foreign key (coerente con la regola "un modulo possiede i suoi
  dati, riferimenti per ID ma con integrità reale" indicata in
  [architettura-evolutiva-microservizi.md](../../docs/pre/architettura-evolutiva-microservizi.md)).
