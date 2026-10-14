# Tabella `roles`

> Fonti usate per questa scheda: schema reale della tabella sul DB locale (`local_sb`, MySQL 9.7.1,
> `SHOW CREATE TABLE`/`information_schema`, contenuto reale delle 13 righe — **confermato identico nel
> dump di produzione `sb_prod_dump_2026_09_08`**: stessi 13 nomi ruolo, stesse date), il model Sequelize
> [`back-pre/app/models/role.model.js`](../../../../../../back-pre/app/models/role.model.js), l'array
> `db.ROLES` dichiarato in [`back-pre/app/models/index.js`](../../../../../../back-pre/app/models/index.js) (righe
> 312-326), il middleware di autorizzazione
> [`back-pre/app/middleware/authJwt.js`](../../../../../../back-pre/app/middleware/authJwt.js) (unico punto in cui i
> nomi dei ruoli sono usati per decidere cosa un utente può fare lato server), e — per capire cosa cambia
> realmente lato client per ciascun ruolo — i frontend del repository: `front-pre` (gestionale
> stabilimento), `front-customer-pre` (sito pubblico cliente), `front-app-pre` (app mobile),
> `front-modern-pre` (libreria di migrazione) e, **dal 2026-09-09, `front-admin/`** (pannello del
> `systemadmin`, React/Metronic, WIP — vedi §1). Nessuna descrizione qui è dedotta dal solo nome del
> ruolo: ogni riga elenca dove il ruolo è verificato o usato, con riferimento a file/riga.
>
> **Nota di contesto**: `roles` è la tabella "master" di un sistema di ruoli globale (non scoped per
> stabilimento a livello di schema — vedi [`user_roles.md`](user_roles.md) per la relazione con `users` e
> per la nota sullo scoping applicativo tramite `users.propertyId`). Le 13 righe sembrano essere state
> inserite manualmide/una tantum: **non è stato trovato nel repository alcun file di seed/migration** che
> le crei in modo scriptato — l'unico riscontro è l'array `db.ROLES` in `models/index.js`, che ne rispecchia
> esattamente nome e ordine (probabile fonte usata per un inserimento manuale iniziale, non un seed
> automatico eseguito ad ogni avvio).

---

## Campi

### id
- **Tipo**: `int`
- **Vincoli**: chiave primaria, autoincrementale (`AUTO_INCREMENT`), `NOT NULL`
- **Relazioni**: referenziato da `user_roles.roleId` — **nessun vincolo FK reale a livello DB** (verificato:
  `user_roles` non ha alcuna `CONSTRAINT`/`FOREIGN KEY` nello schema, solo indici non univoci su `roleId` e
  `userId`); l'associazione è imposta solo a livello applicativo Sequelize
  (`role.belongsToMany(user, { through: "user_roles" })`, `models/index.js:291-295`).
- **Descrizione**: identificativo tecnico del ruolo. Nel DB locale va da `1` a `13`, in corrispondenza
  esatta e nello stesso ordine dell'array `db.ROLES` (`models/index.js:312-326`) — cioè l'`id` non è
  arbitrario ma riflette l'ordine di inserimento storico dei ruoli nel sistema (i primi id, 1-9, coincidono
  con `createdAt = 2021-02-24`/`2021-03-04`/`2021-03-08`/`2021-06-24`; `waiter` id 10 aggiunto il
  2022-07-28; `affiliate`/`advancedlifeguard` id 11-12 aggiunti il 2025-04-28; `marketing` id 13 aggiunto il
  2026-07-14 — quest'ultimo così recente da non essere ancora presente nella copia di
  `front-customer-pre`, vedi più sotto).
- **Nota rilevante per la riscrittura**: nel codice legacy il ruolo `id=1` (`systemadmin`) è usato come
  **valore di fallback hardcoded** quando un endpoint di registrazione utente non riceve un array `roles`
  nel body — vedi `auth.controller.js:145,275,597` (`user.setRoles([1])`, commento nel codice: *"user role =
  1, in case role parameter value not send in request"*). **Verificato**: nessuno dei form di registrazione
  nei quattro frontend analizzati omette mai il campo `roles` nella richiesta (il consumer signup invia
  sempre `roles: ["consumer"]`, l'admin-signup invia sempre l'array scelto in UI), quindi questo fallback
  non risulta raggiungibile dai flussi UI oggi esistenti — ma resta raggiungibile da una chiamata diretta
  all'endpoint pubblico che ometta il campo, nel qual caso l'utente creato riceverebbe silenziosamente il
  ruolo di livello più alto del sistema. Da correggere strutturalmente nella riscrittura (mai un ruolo
  privilegiato come fallback implicito).
- **Nullabile**: No
- **Esempio di valori**: `1, 2, 3, .. 13` (autoincrementale, coincide con l'ordine storico di inserimento
  dei ruoli — vedi sopra)

### name
- **Tipo**: `varchar(50)`, `NOT NULL`
- **Vincoli**: nessun `UNIQUE` a livello DB (verificato in `SHOW CREATE TABLE`), nonostante il nome sia
  l'unico valore usato per il confronto in **tutte** le verifiche di autorizzazione (`authJwt.js`, sempre
  `roles[i].name === "..."`, mai per `id`) — un'eventuale riga duplicata con lo stesso nome ma `id` diverso
  romperebbe silenziosamente l'assegnazione ruoli via `Role.findAll({ where: { name: { [Op.or]: ... } } })`
  usata in fase di creazione utente.
- **Relazioni**: —
- **Default**: nessuno
- **Descrizione**: nome tecnico del ruolo, in inglese minuscolo, **usato letteralmente come stringa** in
  tutto il codice (backend e frontend) — non esiste un enum/costante centralizzata condivisa fra le parti:
  ogni frontend replica la propria lista di stringhe (vedi sotto). I 13 valori presenti nel DB locale, con
  `id` e data di creazione:

  | id | name | createdAt |
  |---|---|---|
  | 1 | `systemadmin` | 2021-02-24 |
  | 2 | `superadmin` | 2021-02-24 |
  | 3 | `consumer` | 2021-02-24 |
  | 4 | `admin` | 2021-03-04 |
  | 5 | `lifeguard` | 2021-03-04 |
  | 6 | `manager` | 2021-03-04 |
  | 7 | `cashoperator` | 2021-03-04 |
  | 8 | `websiteadmin` | 2021-03-08 |
  | 9 | `user1` | 2021-06-24 |
  | 10 | `waiter` | 2022-07-28 |
  | 11 | `affiliate` | 2025-04-28 |
  | 12 | `advancedlifeguard` | 2025-04-28 |
  | 13 | `marketing` | 2026-07-14 |

  - **Nullabile**: No
  - **Esempio di valori**: `consumer`, `manager`, `admin`, `superadmin` (elenco completo nella tabella sopra)
  - **Candidato a enumerazione**: `roles` è già essa stessa la tabella lookup del vocabolario ruoli (a cui
    `user_roles.roleId` punta solo a livello applicativo, senza FK reale — vedi
    [`user_roles.md`](user_roles.md#roleid)), quindi non serve un ulteriore `ENUM` a livello DB; basterebbe
    aggiungere il vincolo `UNIQUE` oggi mancante sul nome. **Nota dati (verifica più recente sul DB di
    sviluppo)**: una query diretta più aggiornata mostra la tabella con **15 righe e 14 nomi distinti** — un
    valore aggiuntivo `"ofsale"` compare **duplicato due volte con `id` diversi**, conferma pratica del
    rischio di duplicati già segnalato sopra per l'assenza di `UNIQUE` (dato di sviluppo/test, da ripulire
    prima della riscrittura); anche `"user1"` (id 9) resta un nome poco descrittivo e verosimilmente di
    test, coerente con quanto già osservato più sotto in questa scheda.

### createdAt / updatedAt
- **Tipo**: `datetime`, `NOT NULL` (a differenza di quasi tutte le altre tabelle analizzate finora, qui sono
  dichiarati `NOT NULL` senza default a livello DB — devono quindi essere sempre forniti esplicitamente da
  chi inserisce la riga, coerente col fatto che non c'è un flusso applicativo che crei ruoli a runtime)
- **Relazioni**: —
- **Descrizione**: timestamp di creazione/modifica della riga di ruolo. Non risulta alcun endpoint
  applicativo che crei o modifichi righe di `roles` (nessun `Role.create`/`Role.update` trovato in tutto
  `back-pre/app`, a parte le letture `Role.findAll`/`Role.findByPk`) — la tabella è, nei fatti, statica a
  runtime: i 13 ruoli sono stati inseriti (verosimilmente a mano, o con uno script non incluso nel
  repository) e non cambiano più via applicazione.
- **Nullabile**: No
- **Esempio di valori**: `2021-02-24 00:00:00`, `2022-07-28 00:00:00`, `2026-07-14 00:00:00`

---

## I 13 ruoli, uno per uno

Per ciascun ruolo: dove è verificato **lato server** (quali endpoint blocca/consente) e dove è usato **lato
client** in ciascuno dei quattro frontend, con evidenza puntuale. Il principio guida verificato in
`authJwt.js` è: ogni funzione `isXxx`/`checkXxx` carica **tutti** i ruoli dell'utente
(`user.getRoles()`) e verifica se il nome cercato è presente nell'array — quindi a livello di **modello
dati** un utente può avere più ruoli contemporaneamente (relazione N:N reale), anche se **nella pratica
osservata** (dati DB + tutti i flussi di creazione utente trovati nel codice) non risulta mai accadere,
vedi [`user_roles.md`](user_roles.md).

### 1 — `systemadmin`
- **Lato server**: `authJwt.isSystemAdmin` (righe 69-85). Verificato negli endpoint sysadmin (gestione
  approvazione/rifiuto stabilimenti, dettaglio proprietà, ecc. — `user.routes.js`, gruppo di rotte
  `/api/sb/pendingproperties`, `/approvedproperties`, `/rejectedproperties`, `/updateproperty`,
  `/deleteproperty`, verificato in `sysadmin.controller.js`).
- **Lato client**: nei **quattro** frontend storici (`front-pre`, `front-customer-pre`, `front-app-pre`,
  `front-modern-pre`) **nessun riferimento** — zero occorrenze della stringa `systemadmin`.
  **Aggiornamento 2026-09-09**: è stato aggiunto al repository un **quinto** frontend,
  [`front-admin/`](../../../../../../front-admin/) (React 16 / Metronic 7.2.2, WIP), che **è** il pannello di
  back-office del system admin: il login (`Auth/pages/Login.js`) ammette **solo** `roles === "systemadmin"`
  (ogni altro ruolo → "Unauthorized Access") e i moduli `PropertyManagement` / `ConfigurationSettings` /
  `SubscriptionDetails` invocano le rotte `authJwt.isSystemAdmin` (`sb/newproperties`,
  `sb/approvedproperties`, `sb/rejectedproperties`, `sb/updateproperty`, `sb/deleteproperty`,
  `sb/updatesuperadmin`, `sb/listconfimages`/`addconfimages`/`upstatusconfimages`/`deleteconfimage`, …).
  Dettaglio in [`business-logic/systemadmin/`](../business-logic/systemadmin/) e
  [`azioni/systemadmin/`](../azioni/systemadmin/).
- **Nota di sicurezza** (già in [01-criticita-e-indicazioni-backend.md](../../progettazione/01-criticita-e-indicazioni-backend.md)):
  è anche il ruolo assegnato per **fallback silenzioso** (`id=1`) quando un endpoint di registrazione non
  riceve `roles` nel body — vedi campo `id` sopra.

### 2 — `superadmin`
- **Lato server**: `authJwt.isSuperAdmin` (righe 86-102) e `isSuperAdminOrAdmin` (righe 103-119). È il ruolo
  che protegge la maggior parte degli endpoint di configurazione dello stabilimento nel dominio catalogo
  (listini, sconti, settori, stagioni, mappa — vedi
  [01-criticita-e-indicazioni-backend.md §5.1](../../progettazione/01-criticita-e-indicazioni-backend.md), dove è
  già stato segnalato che questo controllo verifica solo il **nome del ruolo**, non l'appartenenza allo
  stabilimento — un "superadmin" di uno stabilimento può quindi operare sui dati di un altro).
  Rappresenta il gestore/proprietario principale dello stabilimento: assegnato in fase di registrazione
  (`auth.controller.js:usersignup`, riga 226-238) — se l'array `roles` inviato include `"superadmin"`, la
  property viene aggiornata con `propertyOwner = <id del nuovo utente>` (vedi
  [`properties.md`](properties.md#propertyowner), incluso il dettaglio per cui questo riferimento può
  diventare stale se un secondo utente "superadmin" viene creato in seguito per la stessa property).
- **Lato client**: **11 file** in `front-pre` (es. `AddUserForm.js`, `SettingParent.js`,
  `ConfigParent.js`, header applicativo) e **8** in `front-customer-pre` verificano
  `role === "superadmin"` per mostrare funzionalità aggiuntive (impostazioni avanzate, gestione utenti,
  ecc.); **non presente in `front-app-pre`/`front-modern-pre`**. **Non selezionabile** dalla UI
  `AddUserForm`/`ACCOUNT_TYPES` (la lista dei ruoli assegnabili da un operatore admin è una lista distinta
  e più corta, vedi ruoli 4-12 sotto) — è quindi un ruolo assegnato **solo** al momento della registrazione
  iniziale dello stabilimento, mai tramite il pannello "aggiungi utente".

### 3 — `consumer`
- **Lato server**: `authJwt.isConsumer` (righe 187-202), poco usato direttamente come gate (il ruolo
  "consumer" è più che altro il default per chi **non** ha ruoli operativi). È il ruolo assegnato in modo
  hardcoded ad ogni nuovo cliente finale: `front-customer-pre/src/components/auth/SignUp.js:122`
  (`roles: "consumer".split(",")`) → `auth.controller.js:signup`. È anche il valore di default assegnato
  lato server come stringa iniziale prima di essere sovrascritto con il ruolo reale trovato
  (`var authorities = "consumer"`, ripetuto identico in `signin`, `social`, `sendToken` — righe 337, 534,
  616, 931 di `auth.controller.js`): se un utente **non ha alcuna riga** in `user_roles` (caso reale e non
  raro, vedi [`user_roles.md`](user_roles.md)), il campo `roles` restituito al login vale `"consumer"` per
  effetto di questo default, anche se nessuna riga `user_roles` lo attesta esplicitamente.
- **Lato client**: è il ruolo più diffuso in assoluto nei riferimenti di codice (95 file in `front-pre`, 119
  in `front-customer-pre`, 3 in `front-app-pre`) — usato ovunque per distinguere "vista cliente finale" da
  "vista operatore/gestionale". **Confermato dai dati**: 97 delle 100 righe di `user_roles` nel campione
  locale assegnano questo ruolo (54 delle quali a utenti realmente presenti nell'estratto `users`, le altre
  46 orfane — vedi [`user_roles.md`](user_roles.md)).

### 4 — `admin`
- **Lato server**: `authJwt.isAdmin` (righe 120-136), `isSuperAdminOrAdmin`, e incluso in `checkRole`
  (righe 219-244) e `checkRoleToViewContactDetails` (righe 288-308). Verificato anche in
  `contractConditionService.isAdminOrSuperAdmin` (righe 20-25) — solo `admin`/`superadmin` possono leggere
  o accettare le condizioni contrattuali (vedi [`properties.md#contractdocumentsid`](properties.md)).
- **Lato client**: selezionabile in `AddUserForm`/`ACCOUNT_TYPES` (`id: 1, name: "admin", it:
  "Amministratore"` — `front-pre/src/components/Common/BathhouseConstant.js:65`) — un utente "superadmin" o
  "admin" può creare altri utenti "admin" per lo stesso stabilimento. 30 riferimenti in `front-pre`, 27 in
  `front-customer-pre`, 1 in `front-app-pre` (probabile falso positivo del termine generico "admin" nel
  codice dell'app, non un controllo di ruolo — nell'app mobile non esiste gestione ruoli, vedi sopra).

### 5 — `lifeguard`
- **Lato server**: `authJwt.isLifeguard` (righe 153-169), incluso in `checkRole` e
  `checkRoleViewWareHouse` (righe 245-266, gate per la visualizzazione del magazzino).
- **Lato client**: selezionabile in `ACCOUNT_TYPES` (`id: 6, it: "Bagnino"`). È anche il ruolo con più
  restrizioni **puramente client-side** trovate nel repository: numerosi componenti (es.
  `ThePlannerPage.js`, già documentato in
  [01-criticita-e-indicazioni-backend.md §11.2](../../progettazione/01-criticita-e-indicazioni-backend.md))
  nascondono modifica prezzi/pannelli avanzati se `role === "lifeguard"`, **senza un controllo server-side
  equivalente** sugli endpoint di scrittura prenotazione — quindi il vincolo esiste solo in UI.

### 6 — `manager`
- **Lato server**: `authJwt.isManager` (righe 137-152), incluso in `checkRole`,
  `checkRoleViewWareHouse`, `checkRoleToViewContactDetails`.
- **Lato client**: selezionabile in `ACCOUNT_TYPES` (`id: 2, it: "Manager"`). Il termine più diffuso in
  assoluto nel codice frontend (135 file in `front-pre`, 140 in `front-customer-pre`) — ma la stragrande
  maggioranza di queste occorrenze è la parola generica "manager" nel senso di "gestore/operatore dello
  stabilimento" usata in commenti, nomi di variabili e testi UI, non necessariamente il controllo del ruolo
  esatto `"manager"`; non è stato possibile, nel tempo disponibile, isolare con certezza quante di queste
  siano controlli di ruolo reali — dato da prendere come indicazione di rilevanza del concetto, non come
  conteggio preciso di gate di autorizzazione.

### 7 — `cashoperator`
- **Lato server**: `authJwt.isCashOperator` (righe 170-186), incluso in `checkRole` e
  `checkSendMessagesRole` (righe 267-286, gate per l'invio di comunicazioni/messaggi).
- **Lato client**: selezionabile in `ACCOUNT_TYPES` (`id: 3, it: "Operatore di cassa"`). 16 riferimenti in
  `front-pre`, 12 in `front-customer-pre`.

### 8 — `websiteadmin`
- **Lato server**: `authJwt.isWebsiteAdmin` (righe 52-68). Nessun altro riferimento trovato negli altri
  controller — sembra un ruolo predisposto per un pannello di amministrazione del sito marketing
  (`hub.summerbooking.it`?) mai implementato o non incluso in questo repository.
- **Lato client**: **zero occorrenze** in tutti e cinque i frontend, incluso `front-admin/` (che è il
  pannello del `systemadmin` — vedi §1 — ma non tocca `websiteadmin`). Ruolo esistente nello schema/
  middleware ma senza alcun consumer verificabile nel materiale disponibile: resta il candidato più
  probabile a "ruolo superato" (vedi [`da-chiarire.md`](../business-logic/da-chiarire.md) §14).

### 9 — `user1`
- **Lato server**: `authJwt.isUser1` (righe 203-218, che — bug verificato nel codice — restituisce
  `requireConsumerRole` come messaggio di errore invece di un messaggio dedicato, riga 214: probabile
  copia-incolla da `isConsumer` mai corretta), incluso in `checkRole`, `checkRoleViewWareHouse`,
  `checkRoleToViewContactDetails`.
- **Lato client**: selezionabile in `ACCOUNT_TYPES` (`id: 7, name: "user1", it: "user1"` — **l'etichetta
  italiana non è stata tradotta**, è rimasta uguale al nome tecnico: verificato letteralmente in
  `BathhouseConstant.js:71`). Il nome stesso del ruolo (`user1`, non descrittivo) e la mancata traduzione
  suggeriscono un ruolo generico aggiunto per un caso d'uso specifico mai documentato nel codice — non è
  stato possibile determinarne il significato di business dal solo codice; da chiedere al committente prima
  di riportarlo nel nuovo modello con un nome più descrittivo.

### 10 — `waiter`
- **Lato server**: **nessuna funzione dedicata** in `authJwt.js` (non `isWaiter`, non incluso in nessuna
  delle funzioni `checkXxx` combinate) — verificato con ricerca esaustiva della stringa `"waiter"` in tutto
  `back-pre/app`: l'unica occorrenza è la sua presenza nell'array `db.ROLES`. **Nessun endpoint richiede
  esplicitamente questo ruolo.**
- **Lato client**: selezionabile in `ACCOUNT_TYPES` (`id: 4, it: "Cameriere"`), ed è invece
  **ampiamente differenziato in UI** (19 file in `front-pre`): redirect dedicato dopo login
  (`SignIn.js:111`, `case "waiter":`), componente `<Waiter />` dedicato mostrato in `AdminHeader.js:1330`
  quando `role === "waiter"`, e numerosi punti che nascondono controlli (modifica prezzo, "torna alla
  mappa") con `roles !== "waiter"` (es. `editService.js:359`, `ServiceReservation.js:855`,
  `Summary.js:661`). **È quindi un ruolo interamente definito e fatto rispettare lato client**, senza alcun
  controllo server-side equivalente — un cameriere autenticato può chiamare direttamente qualunque endpoint
  che il suo account raggiungerebbe se non fosse limitato dalla UI (stesso schema già documentato per
  `lifeguard` e, in generale, in
  [01-criticita-e-indicazioni-backend.md §3.2](../../progettazione/01-criticita-e-indicazioni-backend.md)).

### 11 — `affiliate`
- **Lato server**: **nessuna funzione dedicata** in `authJwt.js`, stessa situazione di `waiter` (verificato
  con ricerca esaustiva). Il valore booleano collegato `users.isAffiliateFlag` e il campo
  `users.affiliate_selected_locations` sono invece scritti/letti da `auth.controller.js`/`user.controller.js`
  (vedi [`users.md`](users.md#isaffiliateflag)), ma senza un controllo di ruolo lato server sugli endpoint
  di prenotazione — l'unico enforcement verificato è lato client.
- **Lato client**: selezionabile in `ACCOUNT_TYPES` (`id: 8, it: "Affiliato"`). Quando in `AddUserForm.js`
  si seleziona questo ruolo, compare una sezione dedicata per scegliere le location che l'affiliato potrà
  gestire (righe 203-205, 432). L'effetto reale è verificato in
  `front-pre/src/components/reserveArticle/BeachMap/ConfigSecPanelChildren.js` (righe 598-599, 779-798):
  se `userRole.role === "affiliate"` e `isAffiliateFlag` è vero, le location della mappa **non incluse**
  nell'insieme assegnato (`config.isAffiliated`) vengono disabilitate/oscurate (opacità ridotta,
  `pointerEvents: "none"`) — di nuovo, un vincolo imposto solo lato client sulla UI della mappa, non
  verificato lato server sugli endpoint di scrittura prenotazione.

### 12 — `advancedlifeguard`
- **Lato server**: **nessuna funzione dedicata**, stessa situazione di `waiter`/`affiliate` (ricerca
  esaustiva). Da notare: `authJwt.isLifeguard` verifica solo `roles[i].name === "lifeguard"` — un utente con
  **solo** il ruolo `advancedlifeguard` (senza anche `lifeguard`) **non supererebbe** quel controllo,
  nonostante il nome suggerisca una variante "potenziata" del ruolo lifeguard.
- **Lato client**: selezionabile in `ACCOUNT_TYPES` (`id: 5, it: "Bagnino Avanzato"`). Compare nelle stesse
  condizioni `roles !== "waiter"` insieme a `lifeguard` in diversi punti (es. la ricerca `"lifeguard"` in
  `front-pre` include occorrenze di `"advancedlifeguard"` per via del match parziale di stringa — 16 file,
  stesso numero esatto di `lifeguard`/`cashoperator`, verosimilmente perché questi controlli sono spesso
  scritti come liste che includono più ruoli insieme nello stesso file/blocco JSX). Non è stato possibile
  isolare, nel tempo disponibile, un punto UI che tratti `advancedlifeguard` in modo **diverso** da
  `lifeguard` — trattamento esatto da chiarire col committente se rilevante per la riscrittura.

### 13 — `marketing`
- **Lato server**: incluso in `authJwt.checkRole` (riga 230) e `checkRoleToViewContactDetails` (riga 296) —
  quindi, a differenza di `waiter`/`affiliate`/`advancedlifeguard`, **ha un vero controllo server-side**,
  seppure solo all'interno di queste due funzioni combinate (nessuna funzione `isMarketing` dedicata).
- **Lato client**: selezionabile in `ACCOUNT_TYPES` **solo nella versione più recente** del file
  (`front-pre/src/components/Common/BathhouseConstant.js:73`, `id: 9, it: "Marketing"`) — **verificato
  assente nella copia equivalente in `front-customer-pre`** (diff riga per riga tra i due file: l'unica
  differenza è proprio questa voce mancante). Essendo il ruolo più recente (creato 2026-07-14, secondo
  `roles.createdAt`), è la conferma più diretta trovata in questa analisi che le quattro codebase **non
  sono sincronizzate fra loro** e possono divergere anche su elementi non banali come l'elenco dei ruoli
  assegnabili.

---

## Osservazioni per la riscrittura (riepilogo)

- **Autorizzazione per nome di stringa, senza `UNIQUE` sul nome**: nella riscrittura, i ruoli vanno
  modellati come enum tipizzato (C#) risolto una volta all'avvio, non come confronto di stringhe ripetuto
  in ogni handler — elimina sia il rischio di typo (già capitato: `requirelifeguardRole` vs
  `requireLifeguardRole`, vedi
  [01-criticita-e-indicazioni-backend.md](../../progettazione/01-criticita-e-indicazioni-backend.md)) sia la
  necessità di un vincolo di unicità applicativo sul nome.
- **Tre ruoli assegnabili in UI ma senza alcun controllo server-side dedicato** (`waiter`, `affiliate`,
  `advancedlifeguard`): la riscrittura deve introdurre un'autorizzazione server-side reale per questi ruoli
  invece di lasciarli come vincoli solo-UI, oppure — se il committente conferma che il loro scopo è
  realmente solo "personalizzare cosa vede l'operatore", modellarli esplicitamente come preferenze di UI e
  non come ruoli di sicurezza, per evitare l'ambiguità attuale.
- **`superadmin` non è scoped per stabilimento a livello di controllo**: il controllo verifica solo il nome
  del ruolo, mai l'appartenenza alla property — coerente con quanto già raccomandato in
  [autenticazione-autorizzazione-dotnet.md](../../progettazione/pre/autenticazione-autorizzazione-dotnet.md) (ruolo
  **e** scoping stabilimento nel token).
- **Ruolo di fallback privilegiato** (`id=1`/`systemadmin` se `roles` è assente dal body): eliminare questo
  pattern nella riscrittura — nessun default deve mai essere un ruolo con privilegi.
- **`systemadmin`**: il pannello di back-office **è ora nel repository**, cartella
  [`front-admin/`](../../../../../../front-admin/) (React/Metronic, WIP) — vedi §1. Ruolo **attivo**, da riportare nel
  nuovo modello. **`websiteadmin`**: resta senza alcun consumer in tutti e cinque i frontend — prima di
  riportarlo, verificare col committente se è un ruolo superato o se esiste un pannello marketing separato
  non incluso qui.
- **`user1`**: nome e scopo non ricostruibili dal solo codice (nessuna label tradotta, nessun uso
  distintivo trovato oltre ai gate generici `checkRole`/`checkRoleViewWareHouse`) — da chiarire col
  committente, non da rinominare per supposizione.
- **Sincronizzazione fra i quattro frontend**: la differenza concreta riscontrata sul ruolo `marketing` (e,
  in `properties.md`, sull'upload foto in registrazione) conferma che le quattro codebase possono divergere;
  nella raccolta requisiti per la riscrittura va chiarito quale sia la versione "di produzione" effettiva
  per ciascuna piattaforma, non assumere che si comportino sempre allo stesso modo.
