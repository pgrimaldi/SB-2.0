# systemadmin — Gestione stabilimenti

> **Tipo di utente**: `systemadmin` (amministratore della piattaforma Summer Booking, non di un singolo
> stabilimento). È il ruolo di livello più alto: `roles.id = 1`, `roles.name = "systemadmin"`.
>
> **Ambito di questo documento**: tutto il ciclo di vita amministrativo di uno **stabilimento** (entità
> `properties` + `bathouses`) dal punto di vista di chi gestisce la piattaforma — dalla registrazione
> iniziale fatta dal futuro gestore, alla presenza nelle code di lavorazione, alla revisione del
> dettaglio, all'approvazione/rifiuto, alla modifica dei dati amministrativi e della commissione,
> all'eliminazione, alla gestione di licenza/abbonamento del titolare, all'attivazione/disattivazione
> della prenotazione online "da amministratore", all'import massivo da CSV, fino al job notturno che
> disattiva gli stabilimenti scaduti.
>
> **Fonti**: `back-pre` — rotte in `app/routes/user.routes.js` (protette da `authJwt.verifyToken` +
> `authJwt.isSystemAdmin`) e `app/routes/public.routes.js`; controller `app/controllers/sysadmin.controller.js`,
> più `property.controller.js` (`register`), `user.controller.js` (`updatesuperadmin`, `csvUpload`,
> `systemAdminBoard`), `confimages.controller.js` (citato, dettaglio nella scheda 02); middleware
> `app/middleware/authJwt.js` (`isSystemAdmin`); template email `app/mailtemplates/propertyTemplates.js`;
> job `back-pre/cronjob/cron-disableproperty.js`. Schede DB:
> [`properties.md`](../../db/properties.md), [`bathouses`](../../db/bathouses.md), [`users.md`](../../db/users.md),
> [`roles.md`](../../db/roles.md), [`online_reservations.md`](../../db/online_reservations.md),
> [`email_logs.md`](../../db/email_logs.md), [`licenses.md`](../../db/licenses.md),
> [`subscribes.md`](../../db/subscribes.md).
>
> **Nota trasversale (frontend — aggiornata 2026-09-09)**: il pannello systemadmin **è ora nel
> repository**, cartella [`front-admin/`](../../../../../../../front-admin/) (SPA React 16 / Metronic 7.2.2, progetto
> CRA `demo3`, **work-in-progress**). Il modulo `PropertyManagement` invoca direttamente le rotte di
> quest'area: `sb/newproperties`, `sb/approvedproperties`, `sb/rejectedproperties`, `sb/propertydetails`,
> `sb/updateproperty`, `sb/deleteproperty`, `auth/usersignup` (add super admin), `sb/updatesuperadmin`,
> `public/licensetype`. **Non** risulta invece invocata `sb/importcsvdata` (nessuna UI di import CSV) né
> `getOnlineReservationStatusByAdmin` (che resta chiamata solo da gestionale e sito cliente — vedi §7).
> La logica sotto, ricostruita dagli endpoint, è ora **verificabile anche sul client**; le divergenze
> osservate sul frontend sono annotate caso per caso. Il contratto client↔API è in
> [`analisi-pre/azioni/systemadmin/01-gestione-stabilimenti.md`](../../azioni/systemadmin/01-gestione-stabilimenti.md).
>
> **Osservazioni dal client `front-admin/`** (debito aggiuntivo, oltre a quello backend):
> - **Base URL API hardcoded** e **incoerenti** fra i moduli: `config.js` punta a
>   `https://api.summerbooking.it/api`, ma `Auth/_redux/authCrud.js` fa il login su
>   `https://development.summerbooking.it/api/auth/signin` e forgot/reset su `https://api.summerbooking.it`.
> - **Payload API cifrati**: le risposte passano da `DecryptionService.js` (AES-256-GCM, **chiave
>   simmetrica hardcoded nel bundle**) — nel nuovo sistema va sostituito con TLS + eventuale firma, non
>   una chiave condivisa nel client.
> - **"Accesso diretto" come titolare**: la lista "Approved" ha un pulsante che chiama
>   `POST auth/send-token` (endpoint già segnalato come vettore di account-takeover in
>   [`docs/01-criticita-e-indicazioni-backend.md`](../../../progettazione/01-criticita-e-indicazioni-backend.md)),
>   cifra la risposta con una **seconda** chiave AES hardcoded (`SECRET_KEY = "SB_USER_DATA"`) e apre
>   `http://localhost:5001/home?user=<enc>` / un URL devtunnels: funzione di impersonation del gestionale,
>   da riprogettare (SSO/token monouso server-side, audit dell'accesso).
> - **Token**: dopo login si salva `signInUser`/`accessToken` in `localStorage`; l'header usato è
>   `x-access-token`; su `401` il client fa `localStorage.clear()` e redirect a `/home`.
>
> **Dominio dati di riferimento**: C5 (Autenticazione, utenti, ruoli) per l'attore e il suo account; C2
> (Catalogo & configurazione / Anagrafica stabilimento) per l'entità `properties`/`bathouses`; C3
> (Pagamenti) per il campo commissione. Vedi [`analisi-pre/db/_feature-tabelle.md`](../../db/_feature-tabelle.md).

---

## 0. Premesse comuni a tutte le azioni

### 0.1 Chi è autorizzato

- Ogni rotta di quest'area (tranne dove indicato) passa da due controlli in sequenza:
  1. **Token valido**: l'header `x-access-token` deve contenere un JWT firmato valido; in caso contrario
     la richiesta è respinta come non autenticata.
  2. **Ruolo systemadmin**: viene caricato l'utente dal token e recuperati **tutti** i suoi ruoli; se
     fra questi non c'è `systemadmin` la richiesta è respinta con messaggio "richiede ruolo systemadmin".
     Il controllo verifica **solo il nome del ruolo**: non c'è nessuno scoping (un systemadmin è globale
     per definizione, quindi qui è corretto, a differenza di `superadmin` — vedi
     [`roles.md`](../../db/roles.md#2--superadmin)).
- **Eccezioni rilevate** (rotte che *logicamente* sono di amministrazione ma nel codice hanno il
  controllo di ruolo commentato e restano di fatto pubbliche/con auth opzionale): `propertydetails`,
  `getPropertyWithBathHouseDetails`, `importcsvdata`, `getOnlineReservationStatusByAdmin`. Sono segnate
  caso per caso. Nella riscrittura vanno riportate sotto autorizzazione `systemadmin` reale (o spostate,
  se servono anche ad altri attori, dietro un permesso esplicito).
- La rotta `GET /api/sb/systemadmin` (controller `systemAdminBoard`) è un semplice "ping" autorizzato:
  restituisce un messaggio fisso di "contenuto per system admin". Serve solo a verificare, lato client,
  che l'utente loggato abbia davvero il ruolo. Nessun calcolo, nessun evento.

### 0.2 Lingua delle comunicazioni

- Le email transazionali di quest'area esistono in due varianti, **inglese** e **italiano**, scelte in
  base a `users.languageCode` del destinatario (`1` = inglese, `2` = italiano; default `2`).
- **Anomalia verificata** in `updateproperty` e `deleteproperty`: la scelta della lingua legge
  `languageCode` da una variabile che contiene un **array** di utenti (risultato di una `findAll`), non
  un singolo record; la proprietà `languageCode` sull'array è sempre indefinita, quindi la condizione
  "inglese" non è mai vera e **viene sempre inviata la variante italiana**, anche a un titolare con
  `languageCode = 1`. Il ramo inglese di queste due azioni è, di fatto, codice morto. Da correggere
  nella riscrittura (scegliere la lingua dal record del titolare).

### 0.3 Stati dello stabilimento (`properties.status`)

Vocabolario chiuso, oggi un intero senza enum a DB (vedi [`properties.md`](../../db/properties.md#status)):

| Valore | Significato | Chi lo imposta |
|---|---|---|
| `0` | Registrato dall'utente, **in attesa di approvazione** | `property.register` alla creazione |
| `1` | **Approvato** dal systemadmin | `updateproperty` con `status = 1` |
| `2` | **Rifiutato** dal systemadmin | `updateproperty` con `status = 2` |
| `3` | "aggiunto da CSV" — descritto solo in un commento del modello, **mai scritto** dal codice reale | — |
| `4` | Valore realmente scritto dall'import CSV (`csvUpload`) | `csvUpload` |

Solo `status 1` (e, nel filtro pubblico, teoricamente `3`) rende lo stabilimento visibile nella ricerca
del sito cliente. Gli stabilimenti importati da CSV (`status 4`) **non compaiono** nella ricerca finché
un systemadmin non li porta manualmente a `1`. Su un campione DB ampio, `status 4` è il valore di gran
lunga più frequente: la maggioranza degli stabilimenti a DB non sarebbe quindi visibile pubblicamente —
punto da chiarire col committente (vedi §11).

### 0.4 Log e sotto-sistemi toccati

- **`email_logs`**: ogni email inviata da quest'area inserisce una riga di log (tipo email, mittente,
  destinatario, oggetto, corpo HTML, `status = 1`, `cronStatus = 0`, `attachments`). L'inserimento del
  log avviene **solo nel ramo di successo** dell'invio SMTP; se l'invio fallisce si risponde errore e non
  si logga.
- **`subscribes`**: la registrazione di uno stabilimento e la creazione di un utente aggiungono
  l'indirizzo email alla tabella `subscribes` (lista newsletter/marketing) tramite una funzione helper
  comune. È un effetto collaterale silenzioso: l'email del gestore entra nella lista contatti marketing
  al primo contatto con la piattaforma.
- **S3**: le foto degli stabilimenti sono caricate su bucket S3 (percorso "immagini property"); le
  icone della mappa spiaggia su un percorso "immagini configurazione" (scheda 02). Gli URL S3 vengono
  riscritti su dominio CloudFront prima del salvataggio a DB (per le icone; per le foto property si
  salva l'URL così come restituito dall'upload).
- **Nessuna notifica in-app**: quest'area **non** scrive su `notification_details`. Le uniche
  comunicazioni verso il gestore sono email.

---

## 1. Onboarding: registrazione di un nuovo stabilimento

**Azione dell'utente**: un potenziale gestore compila il form pubblico "Registra la tua struttura"
(`RegisterProperty.js`, presente sia in `front-pre` sia in `front-customer-pre`, aperto da una modale
dal menù/nav pubblico) e invia nome struttura, città, nome e cognome referente, prefisso + numero di
telefono, email, ed eventuali foto. Rotta: `POST /api/public/property` (pubblica, nessuna
autenticazione). Non è un'azione del systemadmin, ma è **l'evento che alimenta le sue code di
lavorazione** e va descritto qui.

**Validazioni lato client** (le uniche presenti): tutti i campi testuali obbligatori non vuoti; email
che rispetta la regex di formato; telefono che accetta solo cifre; il prefisso internazionale ha default
`+39`. Nessuna di queste validazioni è ripetuta lato server.

**Logiche lato server**:

- Viene creato **un nuovo stabilimento** (`properties`) con: nome, città, referente (`fullName`),
  prefisso, telefono, email presi dal body; **`country` sempre stringa vuota** (il form non lo
  raccoglie, il prefisso selezionato non viene mappato su un paese); **`status = 0`** (in attesa);
  `photos` inizialmente vuoto.
- L'email indicata viene **aggiunta alla tabella `subscribes`** (lista contatti) tramite l'helper
  comune.
- Se il body contiene foto: le immagini (data URL base64) vengono **caricate su S3** in blocco; poi lo
  stabilimento appena creato viene aggiornato con la lista di URL risultante.
- **Nessun utente** (`users`) viene creato in questo passo: lo stabilimento nasce "orfano"
  (`propertyOwner` nullo). L'account del gestore verrà creato in un secondo momento con un flusso di
  registrazione utente separato (`usersignup`), che — se fra i ruoli richiesti c'è `superadmin` — imposta
  `properties.propertyOwner` all'id del nuovo utente.

**Calcoli**: nessuno.

**Eventi**:

- **Email di benvenuto al gestore** all'indirizzo indicato nel form. La lingua è scelta da
  `languageCode` passato nel body (default `2` = italiano): oggetto "Benvenuto su Summer Booking Hub"
  (IT) oppure "Welcome to Summer Booking Hub" (EN), con in allegato le foto caricate. In caso di errore
  SMTP si risponde errore; in caso di successo si inserisce una riga in `email_logs` (tipo "property
  email") e si risponde "property registered".
- **Email di notifica all'amministratore** ("New property … registered on Summer Booking", con link al
  pannello admin): il template esiste (`propertyEmail`) ma la chiamata di invio è **interamente
  commentata** nel codice. Oggi quindi **nessun avviso automatico raggiunge il systemadmin** quando
  arriva una nuova registrazione: deve accorgersene guardando la coda "in attesa". Da ripristinare/
  ridisegnare nella riscrittura (es. notifica interna o digest).
- Effetto collaterale: comparsa dello stabilimento nella coda "nuovi/in attesa" del systemadmin (§2).

**Anomalie / debito**:

- Se il body non contiene `photos`, il codice usa comunque una variabile lista di URL che in
  quel ramo non è stata inizializzata: la registrazione **senza foto va in errore** per riferimento a
  variabile non definita. In pratica la registrazione richiede almeno una foto, anche se il form la
  presenta come opzionale (e nell'attuale UI il caricamento foto è commentato). Contraddizione da
  sciogliere: decidere se le foto sono obbligatorie.
- `country` non valorizzato e prefisso non mappato: lo stabilimento nasce senza paese.

---

## 2. Code di lavorazione: elenchi stabilimenti per stato

**Azione dell'utente**: il systemadmin apre le tre viste a coda del portale.

| Vista | Rotta | Filtro | Ordinamento |
|---|---|---|---|
| Nuovi / in attesa | `GET /api/sb/newproperties` | `status = 0` **e** `isDelete = false` | per id decrescente (più recenti in cima) |
| Approvati | `GET /api/sb/approvedproperties` | `status = 1` **e** `isDelete = false` | per id decrescente |
| Rifiutati | `GET /api/sb/rejectedproperties` | `status = 2` **e** `isDelete = false` | per id decrescente |

**Logiche**:

- Le tre rotte restituiscono **tutte** le righe che rispettano il filtro, senza paginazione né limite:
  su tabella grande (migliaia di righe) è un problema di performance/carico noto, da paginare nella
  riscrittura.
- Ogni riga è restituita "piatta" con **tutti i campi** dello stabilimento, inclusi campi JSON non
  parsati e dati potenzialmente sensibili (email, telefono, opzioni di pagamento). Da restringere il
  payload a ciò che serve alla lista.
- Se il filtro non trova nulla, la risposta è `status: false` con messaggio "properties not found"
  (semanticamente una lista vuota, non un errore).
- **Gli stati `3` e `4` non hanno una coda**: gli stabilimenti importati da CSV (`status 4`) non
  compaiono in nessuna delle tre viste; per portarli operativi il systemadmin deve conoscerne l'id e
  agire via `updateproperty`. Gap funzionale da colmare (una coda "importati" o l'inclusione di `status
  4` fra i "da lavorare").
- La riga "eliminata" (`isDelete = true`) è esclusa da tutte e tre: non esiste una vista "cestino".

**Calcoli**: nessuno. **Eventi**: nessuno (sola lettura).

---

## 3. Revisione del dettaglio di uno stabilimento

**Azione dell'utente**: dalla coda, il systemadmin apre il dettaglio di uno stabilimento per decidere
se approvarlo, modificarlo o rifiutarlo.

Due rotte, entrambe con **controllo di ruolo commentato** (di fatto ad auth opzionale — anomalia da
correggere):

### 3.1 `POST /api/sb/propertydetails` — scheda amministrativa

Input: id dello stabilimento.

**Logiche**:

- Recupera lo stabilimento per id; se non esiste, risponde "property not found".
- Recupera l'**utente titolare** (`users` con id = `properties.propertyOwner`). Se non c'è (stabilimento
  orfano), il blocco dati utente è un oggetto vuoto.
- Se lo stabilimento ha `paymentOptions` valorizzato, lo restituisce parsato; altrimenti restituisce la
  **configurazione di pagamento di default** definita a livello applicativo (`propertyConfig`).
- Restituisce un oggetto selezionato con: id, nome, città, paese, referente, prefisso/telefono, email,
  date di creazione/aggiornamento, `status`, foto (parsate), `propertyOwner`, dati del titolare,
  `adminComment` (nota interna dell'amministratore), `isDelete`, flag `onlineReservation`,
  `paymentOptions`, `superAdminOnlineFlag`, `commissionAmount` (parsato).

**Calcoli**: solo parsing/normalizzazione JSON (foto, opzioni pagamento, commissione). Nessun calcolo
economico.

**Eventi**: nessuno.

### 3.2 `POST /api/sb/getPropertyWithBathHouseDetails` — scheda amministrativa + vetrina pubblica

Input: `propertyId`, `bathHouseId` (obbligatori; `bathHouseId = 0` ammesso), `userId` opzionale.

Questa rotta è condivisa: la usa anche il **sito cliente** per la pagina pubblica dello stabilimento.
La logica di mascheramento dati serve proprio a quel caso.

**Logiche**:

- Se è passato `userId`, verifica se quell'utente ha messo "mi piace" a quel `bathHouseId`
  (`property_like_logs`): imposta un flag `isLike`.
- Recupera stabilimento (per `propertyId`) e titolare (per `propertyOwner`).
- Recupera la configurazione prenotazione online (`online_reservations` per `propertyId`): se esiste,
  espone `isEnableMap` (mappa attiva), `period` e `locations` (parsati) e `isDisable` (blocco pagamenti
  online deciso dall'amministratore); altrimenti valori neutri (mappa 0, liste vuote, non bloccato).
- Verifica se esiste una configurazione mappa (`configurations` per `propertyId`): flag
  `isConfiguration` 1/0.
- Recupera i dati del `bathouses` per `bathHouseId`; parsa `services`.
- **Mascheramento condizionato dall'autenticazione**: se la richiesta è **autenticata** (token
  presente), i dati di contatto del titolare (username, email, nome, cognome, telefono, ecc.) sono
  restituiti **in chiaro**; se **non** è autenticata, username/email/nome/cognome del titolare sono
  restituiti **mascherati** (funzioni `maskEmail`, `maskName`, `maskPhone`). I contatti di stabilimento
  e bagno (telefono, email, WhatsApp) sono invece lasciati invariati in entrambi i rami (il codice li
  "ricopia su sé stessi": mascheramento previsto ma non applicato — incoerenza da sistemare).
- Restituisce un oggetto ricco che unisce dati stabilimento, dati bagno, dati titolare (eventualmente
  mascherati), `isLike`, `mapEnable`, `isConfiguration`, `paymentOptions` (parsato), `period`,
  `locations`, `isAdminDisableOnLinePayment`.

**Calcoli**: parsing JSON e mascheramento stringhe. Nessun calcolo economico.

**Eventi**: nessuno. **Nota**: la verifica del "mi piace" e la risposta principale sono di fatto due
flussi asincroni non coordinati (la lettura del like non blocca la risposta): il flag `isLike` può
arrivare non aggiornato. Da rendere sequenziale nella riscrittura.

---

## 4. Approvazione / rifiuto / modifica di uno stabilimento

**Azione dell'utente**: dal dettaglio, il systemadmin salva le modifiche allo stabilimento e ne cambia
lo stato (approva, rifiuta, rimette in attesa), scrivendo una nota interna, aggiustando la scadenza
dell'abbonamento del titolare e — se previsto — la commissione.

Rotta: `POST /api/sb/updateproperty` (token + ruolo systemadmin). Input tipico: id stabilimento,
`propertyName`, `propertyOwnerName`, città, telefono, email, `status` (0/1/2), `adminComment`,
`propertyOwner`, `commissionAmount`, `additionalDays` (stringa tipo `"+30"` / `"-15"`), più i campi del
titolare: `ownerfirstName`, `ownerlastName`, `ownermobile`, `ownerlicensetype`, `ownersubscription`
(`"enable"` / `"disable"`).

### 4.1 Aggiornamento dello stabilimento

- Recupera lo stabilimento per id; se non esiste, "property not found".
- Aggiorna sullo stabilimento: nome, referente (`fullName` = `propertyOwnerName`), città, telefono,
  email, `status`, `adminComment`, `propertyOwner`, `commissionAmount`. **Non** tocca foto, opzioni di
  pagamento, `superAdminOnlineFlag` (quello si gestisce da `updatesuperadmin`, §6).
- `commissionAmount` viene salvato **così com'è arrivato dal body**, senza validazione di struttura: è
  atteso un oggetto con `onlineCommission` e `iframeCommission` (percentuali, default 6 e 6), ma a DB
  esistono già righe con valori scalari "sporchi" (una stringa `"2"`/`"4"`). La riscrittura deve
  imporre struttura e range (vedi [`properties.md`](../../db/properties.md#commissionamount)).

**A che serve la commissione** (calcolo a valle, non qui): la percentuale salvata viene poi restituita
al sito cliente nel payload della pagina stabilimento e usata **nel browser** per calcolare la
commissione sulle prenotazioni online: si sceglie `iframeCommission` se la prenotazione avviene dentro
un widget iframe su sito terzo, altrimenti `onlineCommission`; l'importo commissione è
`prezzo-base × percentuale ÷ 100 + 0,50 €` sul totale se si paga tutto subito, oppure sulla metà se si
paga solo l'acconto, zero se si paga in struttura. Cinque `propertyId` specifici sono **esenti da
commissione** per una lista hardcoded nel bundle del sito cliente, indipendentemente dal valore a DB
(da portare a dato nel nuovo modello). Dettaglio completo in
[`properties.md`](../../db/properties.md#commissionamount).

### 4.2 Effetti sul titolare e su tutto lo staff

Se esiste **un utente** con `propertyId` = id dello stabilimento (viene preso il primo trovato, non
necessariamente il titolare):

- **Calcolo della nuova scadenza abbonamento (`additionalDays`)**: si parte dalla `endDate` attuale
  dell'utente; dalla stringa `additionalDays` si legge il **segno** (primo carattere `+` o `-`) e il
  **numero di giorni**; la nuova `endDate` = `endDate attuale ± numero di giorni`. Non ci sono limiti né
  controlli (si può portare la scadenza nel passato).
- **Logica sospensione/riattivazione abbonamento** (indipendente da `additionalDays`, si combina con
  esso):
  - se `ownersubscription` = `"disable"` **e** lo stato precedente non era già `"disable"` → si imposta
    `subscriptionDisableDate` = adesso (timestamp della sospensione);
  - se `ownersubscription` = `"enable"` **e** lo stato precedente non era già `"enable"` **e** esiste
    una `subscriptionDisableDate` precedente → la `endDate` viene **prolungata della durata della
    sospensione**: nuova `endDate` = `endDate + (adesso − subscriptionDisableDate)`. È il tentativo,
    qui implementato in modo corretto, di "congelare" i giorni residui durante una sospensione (a
    differenza della logica analoga in `updateuser`/`updatesuperadmin`, considerata sospetta —
    vedi [`users.md`](../../db/users.md#subscriptionstatus)).
  - **Anomalia verificata**: il "controllo dello stato precedente" legge un campo `ownersubscription`
    sul record utente, che **non esiste** sul modello `users` (il campo reale è `subscriptionStatus`):
    la condizione "precedente diverso da …" è quindi sempre vera, e i due rami scattano ad ogni
    salvataggio in cui `ownersubscription` vale `"disable"`/`"enable"`. Da correggere: confrontare con
    `subscriptionStatus`.
- **Ricalcolo licenza**: se il campo licenza cambia, `startDate` = adesso e `endDate` =
  `adesso + N mesi` (dove N = mesi della nuova licenza). *(Questo ramo è nel controller ma nel caso di
  `updateproperty` la licenza è passata come `ownerlicensetype`; il ricalcolo "a mesi" è esplicito
  soprattutto in `updatesuperadmin`, §6.)*
- **Propagazione a tutto lo staff**: l'aggiornamento dei campi utente (`endDate`, nome, cognome,
  telefono, licenza, stato abbonamento, ed eventuali date di sospensione) viene applicato con una
  singola operazione **a tutti gli utenti** che hanno quel `propertyId`, non solo al titolare. Cambiare
  licenza/scadenza dello stabilimento = cambiarla per ogni account dello staff. Coerente col fatto che
  la licenza è "dello stabilimento", ma da rendere esplicito nel nuovo modello (una entità
  abbonamento/licenza sullo stabilimento, non replicata per utente).

Se **non** esiste alcun utente con quel `propertyId` (stabilimento senza staff): si salta tutta la
parte §4.2 e si risponde semplicemente "property updated", **senza inviare email**.

### 4.3 Calcolo del testo di stato ed evento email

- Mappatura `status` → etichetta nel testo dell'email:
  - `1` → "Approvato" (IT) / "Approved" (EN)
  - `2` → "Rifiutato" (IT) / "Rejected" (EN)
  - qualsiasi altro valore → "in attesa di" (IT) / "Pending" (EN)
- **Email al titolare** all'indirizzo `properties.email` (non all'email dell'utente): oggetto "stato
  struttura aggiornato", corpo con **stato** e **commento dell'amministratore** (`adminComment`)
  riportati in chiaro.
- Per l'anomalia lingua descritta in §0.2, viene **sempre inviata la variante italiana**.
- Su invio riuscito: riga in `email_logs` (tipo "property status email") e risposta "property updated
  <stato>". Su errore SMTP: risposta errore, nessun log.
- **Nessuna notifica in-app**, nessuna chiamata ad API esterne.

**Osservazioni per la riscrittura**:

- Un'unica azione fa da: modifica anagrafica stabilimento + cambio stato (workflow) + modifica
  commissione + modifica scadenza/licenza/abbonamento dello staff + invio email. Andrebbe scomposta in
  operazioni distinte con audit separato (approvazione ≠ modifica dati ≠ gestione abbonamento).
- Manca un **audit trail**: non si registra chi ha approvato/rifiutato, quando, e con quale commento
  storico (`adminComment` è un singolo campo sovrascrivibile).
- Nessun controllo di transizione di stato (si può passare direttamente da "rifiutato" ad "approvato",
  o rimettere "in attesa" uno stabilimento operativo con prenotazioni in corso).

---

## 5. Eliminazione di uno stabilimento

**Azione dell'utente**: il systemadmin elimina uno stabilimento (es. richiesta del gestore, struttura
chiusa, registrazione spam).

Rotta: `POST /api/sb/deleteproperty` (token + ruolo systemadmin). Input: id stabilimento,
`adminComment`, `isDelete`.

**Logiche**:

- Recupera lo stabilimento per id; se non esiste, "property not found".
- **Soft delete**: imposta `properties.isDelete = true`. La riga resta a DB ma sparisce da tutte le
  code (§2) e dalla ricerca pubblica.
- **Disattivazione del titolare**: imposta `users.userStatus = "inactive"` per l'utente con id =
  `propertyOwner`. Questo **blocca il login** di quell'account (il login ammette solo `userStatus =
  "active"`, con messaggio fuorviante "Account has deleted"). Gli **altri account** dello staff dello
  stesso stabilimento **non** vengono disattivati (a differenza di `updateproperty`, qui l'operazione
  colpisce solo il `propertyOwner`).
- Nessuna cancellazione a cascata: prenotazioni, pagamenti, configurazioni, bagno (`bathouses`), utenti
  non-titolari restano invariati. Nessun controllo su prenotazioni future ancora attive.

**Calcoli**:

- Testo di stato per l'email: se `isDelete` = true → "Deleted" (EN) / "Rifiutato" (IT). *(Nel ramo
  italiano il testo usato è "Rifiutato" anche se l'azione è un'eliminazione: etichetta imprecisa.)*

**Eventi**:

- **Email al titolare** all'indirizzo `properties.email`: oggetto "struttura eliminata / stato
  aggiornato", corpo con stato ed `adminComment`. Per l'anomalia lingua (§0.2) è **sempre italiana**.
- Su invio riuscito: riga in `email_logs` (tipo "property delete email") e risposta "property deleted".
  Su errore SMTP: risposta errore, nessun log.
- Nessuna notifica in-app, nessuna API esterna.

**Osservazioni per la riscrittura**: manca la reversibilità esplicita (non c'è un "ripristina"); manca
il controllo su dati collegati; l'eliminazione dovrebbe almeno segnalare/gestire prenotazioni future e
pagamenti pendenti.

---

## 6. Gestione licenza, abbonamento e prenotazione online del titolare

**Azione dell'utente**: il systemadmin modifica l'account del **titolare** (`superadmin`) di uno
stabilimento: dati anagrafici, tipo di licenza e durata, stato dell'abbonamento, e l'interruttore
"prenotazione online abilitata da amministratore".

Rotta: `POST /api/sb/updatesuperadmin` (token + ruolo systemadmin). Input: id utente (`id`),
`propertyId`, `license`, `licenseMonths`, `onlineReservation` (booleano), più anagrafica utente
(`firstname`, `lastname`, `mobileCountryCode`, `mobile`, `country`, `birthDate`, `gender`,
`profilePic`, `userStatus`, `createdBy`, `notifyWhatsapp`, `notifyEmail`, `subscriptionStatus`).

**Logiche e calcoli**:

- Recupera l'utente per id; se non esiste, "user not found".
- **Ricalcolo licenza**: se la licenza richiesta è **diversa** da quella attuale dell'utente →
  `startDate` = adesso, `endDate` = `adesso + licenseMonths mesi` (somma di mesi di calendario). Se la
  licenza **non** cambia → `startDate` ed `endDate` restano quelli esistenti (nessun aggiustamento).
- **Interruttore prenotazione online "da amministratore"**:
  - `properties.superAdminOnlineFlag` viene impostato al valore di `onlineReservation` (per il
    `propertyId` indicato);
  - contestualmente, la riga di configurazione `online_reservations` dello stesso stabilimento riceve
    `isDisable = 0` se `onlineReservation` è vero, `isDisable = 1` se è falso. Sono i due campi che, a
    valle, il sito cliente legge per decidere se mostrare/permettere il pagamento online.
- **Aggiornamento anagrafica utente**: vengono riscritti i campi anagrafici elencati sopra, più
  `license`, `startDate`, `endDate`, `subscriptionStatus` (presi/derivati come sopra). A differenza di
  `updateproperty`, qui l'aggiornamento è **sul singolo utente** indicato, non su tutto lo staff.
- Qui **non** c'è la logica di "congelamento" della sospensione né il calcolo `additionalDays`:
  `subscriptionStatus` viene salvato pari pari dal body.

**Eventi**:

- Nessuna email, nessuna notifica in-app, nessuna API esterna. Risposta "user updated".
- Effetto indiretto: cambiando `superAdminOnlineFlag`/`isDisable` si abilita o disabilita, per tutti i
  clienti finali, la prenotazione/pagamento online di quello stabilimento.

**Osservazioni per la riscrittura**: tre responsabilità in un endpoint (anagrafica utente, ciclo
licenza/abbonamento, interruttore commerciale della prenotazione online). La somma "mesi di calendario"
per la scadenza va fissata come regola esplicita (fine mese, anni bisestili). L'interruttore
`superAdminOnlineFlag` è una **leva commerciale** (lo usa anche il job notturno, §9): va modellato come
stato esplicito con motivo ("scaduto", "sospeso da admin", "attivo").

---

## 7. Lettura dello stato "prenotazione online abilitata da amministratore"

**Azione**: verificare, per uno stabilimento, se l'amministratore ha disabilitato la prenotazione/
pagamento online.

Rotta: `POST /api/sb/getOnlineReservationStatusByAdmin` (controllo di ruolo **commentato** → auth
opzionale). Input: `propertyId`.

**Logiche**:

- Legge `superAdminOnlineFlag` dalla riga `properties` con quell'id (query SQL diretta con
  interpolazione della stringa — **SQL injection** se `propertyId` non è sanificato: da parametrizzare
  nella riscrittura).
- Se non trova la riga → "No data found".
- Altrimenti restituisce un oggetto con **`isAdminDisableOnLinePayment`**: `true` se
  `superAdminOnlineFlag = 1`, `false` altrimenti.
- **Attenzione alla semantica invertita**: il nome del campo restituito suggerisce "l'admin ha
  disabilitato il pagamento online", ma il valore è `true` proprio quando il flag è **attivo** (`1`).
  A monte (`updatesuperadmin`) il flag a `1` significa "online abilitato". C'è quindi un'ambiguità/
  possibile inversione fra produttore e consumatore del dato, da chiarire e normalizzare.

**Consumatori reali** (la rotta `getOnlineReservationStatusByAdmin` **non** è chiamata dal pannello
systemadmin `front-admin/`, che pure invoca le altre rotte `sb/*` di quest'area — vedi nota in testa):

- **Gestionale** (`front-pre`, `Settings/SettingParent.js`): al caricamento delle impostazioni,
  recupera lo stato per lo stabilimento dell'operatore loggato.
- **Sito cliente** (`front-customer-pre`, `PropertyDetails.js`): sulla pagina pubblica dello
  stabilimento, per decidere se mostrare/abilitare il flusso di prenotazione online.

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 8. Import massivo di stabilimenti da CSV

**Azione dell'utente**: il systemadmin carica un file CSV per creare in blocco molti stabilimenti (es.
onboarding di un elenco acquisito, popolamento vetrina).

Rotta: `POST /api/sb/importcsvdata` (controllo di ruolo **commentato** → auth opzionale: anomalia
grave per un'operazione di scrittura massiva; da mettere sotto ruolo systemadmin). Il file viene letto
da un percorso su disco del server (`filePath`), non necessariamente dall'upload della richiesta —
meccanismo da chiarire/rifare.

**Logiche e iterazioni**:

- Il CSV viene letto in streaming; ogni riga è accumulata, poi elaborata una per una.
- **Validazione riga**: l'unica regola è che il campo `name` non sia vuoto. Righe senza `name` finiscono
  nell'elenco errori.
- Per ogni riga valida:
  - le foto indicate (campo `photos`) vengono normalizzate/caricate;
  - si crea uno stabilimento (`properties`) con: nome (`name`), città, paese, telefono, `photos`;
    `mobileCountryCode` = nullo; **`status = 4`**; **`email` e `fullName` non impostati** (righe
    commentate);
  - si crea il record vetrina collegato (`bathouses`) con nome, indirizzo, paese, città, provincia,
    regione, telefono, foto, latitudine/longitudine (se presenti), un array `services` (variabile
    condivisa, non per-riga — potenziale bug: tutti i bagni ricevono gli stessi servizi),
    **`status = 0`**, `propertyId` = id dello stabilimento appena creato;
  - la riga va nell'elenco "importate correttamente".
- Contatori aggiornati ad ogni iterazione: totale, corrette, con errore.
- Se ci sono righe con errore, viene **scritto un file CSV degli scarti** in una cartella dedicata
  (intestazioni fisse con tutti i campi attesi).
- Risposta finale: messaggio riepilogativo "su N record, X importati, Y con errore" più l'elenco delle
  righe in errore. Se il CSV è vuoto: "no data found in csv file".

**Calcoli**: solo conteggi (totali/corrette/errori). Nessun calcolo economico.

**Eventi**:

- Scrittura del file CSV degli scarti su filesystem del server.
- Upload immagini su S3.
- **Nessuna email**, nessuna notifica, nessun avviso ai gestori importati (che non hanno ancora un
  account).

**Conseguenza di business rilevante**: gli stabilimenti importati (`status = 4`, bagno `status = 0`)
**non compaiono** né nelle code del systemadmin (§2) né nella ricerca pubblica (che filtra `status` in
`[1, 3]`). Restano "in limbo" finché qualcuno non li porta manualmente a `status = 1` con
`updateproperty`. Da chiarire col committente se è il comportamento voluto (vedi §11).

---

## 9. Job automatico: disattivazione degli stabilimenti con abbonamento scaduto

**Evento schedulato** (non un'azione manuale): job `cron-disableproperty.js`, pianificato **ogni notte
alle 00:00**.

**Logiche e iterazioni**:

- Seleziona tutti gli utenti che hanno **contemporaneamente**: `endDate` < adesso, `propertyId`
  valorizzato, `subscriptionStatus = "enable"`.
- Per ciascun utente trovato:
  - imposta `subscriptionStatus = "disable"` e `subscriptionDisableDate` = adesso;
  - **disabilita la prenotazione online dello stabilimento**: `properties.superAdminOnlineFlag = 0` per
    il `propertyId` di quell'utente;
  - **appende una riga di log** (id utente, propertyId, data di disattivazione) a un file di testo sul
    server (`propertylog.txt`).

**Calcoli**: solo il confronto `endDate` < adesso. Nessun calcolo economico. **Nessuna email/notifica**
al gestore quando l'abbonamento scade e lo stabilimento viene messo offline: il gestore se ne accorge
solo non riuscendo più a ricevere prenotazioni online.

**Osservazioni per la riscrittura**:

- Il job scrive su un file di testo locale invece che su una tabella di audit / log strutturato: da
  spostare su storage adeguato (tabella o CloudWatch).
- È l'unico automatismo che "spegne" uno stabilimento: la riattivazione (rinnovo) passa poi per
  `updateproperty`/`updatesuperadmin` che rimettono `superAdminOnlineFlag` e `subscriptionStatus`.
- Manca una comunicazione preventiva ("il tuo abbonamento scade fra X giorni") e una di avvenuta
  disattivazione.
- Sulla piattaforma AWS target questo diventa un task schedulato fuori dal processo web (vedi
  [`docs/pre/features.docx`] — "Job da scorporare").

---

## 10. Riepilogo eventi, calcoli e integrazioni dell'area

**Calcoli**:

- Scadenza abbonamento da mesi di licenza: `endDate = adesso + licenseMonths mesi` (calendario).
- Aggiustamento scadenza da giorni: `endDate = endDate ± additionalDays giorni`.
- Congelamento sospensione: `endDate += (adesso − subscriptionDisableDate)` alla riattivazione.
- Mappature stato → etichetta testuale per le email.
- Conteggi righe nell'import CSV.
- Nessun calcolo su prezzi in questa area (la commissione viene solo **salvata** qui; è calcolata a
  valle nel sito cliente — §4.1).

**Eventi**:

| Evento | Trigger | Canale | Log |
|---|---|---|---|
| Email benvenuto stabilimento | Registrazione (§1) | SMTP, IT/EN | `email_logs` (property email) |
| Email "New property registered" all'admin | Registrazione (§1) | — | **disattivata** (codice commentato) |
| Email stato struttura aggiornato | `updateproperty` (§4) | SMTP, **sempre IT** | `email_logs` (property status email) |
| Email struttura eliminata | `deleteproperty` (§5) | SMTP, **sempre IT** | `email_logs` (property delete email) |
| Aggiunta email a lista contatti | Registrazione (§1) | tabella `subscribes` | — |
| Upload foto/icone | §1, §8, scheda 02 | S3 (+ CloudFront per le icone) | — |
| Scrittura CSV scarti | Import CSV (§8) | filesystem server | — |
| Disattivazione stabilimenti scaduti | Cron 00:00 (§9) | `properties` + `users` + file `propertylog.txt` | file di testo |
| Notifiche in-app | — | **nessuna** in tutta l'area | — |
| API esterne | — | **nessuna** in tutta l'area (nessun Stripe/Fiskaly/WhatsApp qui) | — |

**Integrazioni esterne**: nessuna diretta. S3 è l'unico servizio esterno toccato (storage immagini).

---

## 11. Elementi da chiarire col committente

1. **`status = 4` (import CSV) e visibilità pubblica**: la maggior parte degli stabilimenti a DB ha
   `status = 4` e non compare nella ricerca pubblica (filtro `[1, 3]`). È voluto (staging) o è un bug
   storico (il valore giusto doveva essere `3`, o il filtro doveva includere `4`)? Serve una coda
   "importati" nel portale?
2. **Portale systemadmin** (~~assente dal repo~~ — **reperito**: cartella [`front-admin/`](../../../../../../../front-admin/),
   React/Metronic, WIP): ✅ **deciso** — il nuovo back-office **verrà riscritto da zero**. `front-admin/`
   e queste schede sono la **base dei requisiti**; `front-admin/` non va evoluto né migrato. Da scartare
   in toto: chiavi/URL hardcoded, cifratura simmetrica dei payload lato client, "accesso diretto" via
   `auth/send-token` verso `localhost`, moduli demo Metronic. Da preservare come requisito: le **azioni**
   (code per stato, dettaglio, approva/rifiuta/modifica, add/update Super Admin, catalogo icone, gestione
   iscritti) e i **contratti** documentati in [`azioni/systemadmin/`](../../azioni/systemadmin/), ripuliti
   dalle anomalie qui annotate.
3. **Notifica di nuova registrazione all'amministratore**: oggi disattivata. Ripristinarla come email,
   digest, o notifica interna?
4. **Comunicazioni al gestore su scadenza/disattivazione abbonamento**: oggi assenti. Introdurre avviso
   preventivo + avviso di avvenuta disattivazione?
5. **Anomalia lingua** in `updateproperty`/`deleteproperty` (sempre italiano): confermare che va
   corretto scegliendo la lingua dal record del titolare.
6. **Confronto stato abbonamento** su campo inesistente (`ownersubscription`) in `updateproperty`:
   confermare la semantica voluta (sospendi/riattiva scattano solo su reale cambio di stato).
7. **Propagazione licenza/scadenza a tutto lo staff** (`updateproperty`) vs **solo al singolo utente**
   (`updatesuperadmin`): quale comportamento è quello corretto? Verosimilmente la licenza è dello
   stabilimento e va modellata una volta sola.
8. **Semantica `superAdminOnlineFlag` / `isAdminDisableOnLinePayment`**: `1` = online abilitato o
   disabilitato? Il consumatore lo interpreta in modo potenzialmente invertito rispetto al produttore.
9. **Eliminazione stabilimento**: serve reversibilità esplicita? Come gestire prenotazioni future e
   pagamenti pendenti al momento dell'eliminazione?
10. **Foto obbligatorie in registrazione**: il form le tratta come opzionali ma il backend va in errore
    senza foto. Obbligatorie sì/no?
11. **`csvUpload`, `propertydetails`, `getPropertyWithBathHouseDetails`, `getOnlineReservationStatusByAdmin`,
    `importcsvdata`**: oggi senza controllo di ruolo effettivo. Confermare che vanno messe sotto
    `systemadmin` (e quali di esse servono anche ad altri attori, es. la scheda pubblica dello
    stabilimento).
