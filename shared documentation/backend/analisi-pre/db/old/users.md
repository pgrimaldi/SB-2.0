# Tabella `users`

> Fonti usate per questa scheda: schema reale della tabella sul DB locale (`local_sb`, MySQL 9.7.1,
> `SHOW CREATE TABLE`/`information_schema`, distribuzione dei valori sulle 100 righe presenti), il model
> Sequelize [`back-pre/app/models/user.model.js`](../../back-pre/app/models/user.model.js), le associazioni
> in [`back-pre/app/models/index.js`](../../back-pre/app/models/index.js), e tutti i controller che
> leggono/scrivono `users`:
> [`auth.controller.js`](../../back-pre/app/controllers/auth.controller.js) (registrazione, login, social
> login, OTP/2FA — letto per intero),
> [`user.controller.js`](../../back-pre/app/controllers/user.controller.js) (CRUD utenti/staff, profilo
> personale — funzioni rilevanti lette per intero),
> [`password.controller.js`](../../back-pre/app/controllers/password.controller.js) (letto per intero),
> [`checkEmail.controller.js`](../../back-pre/app/controllers/checkEmail.controller.js),
> [`sysadmin.controller.js`](../../back-pre/app/controllers/sysadmin.controller.js),
> [`middleware/authJwt.js`](../../back-pre/app/middleware/authJwt.js),
> [`helpers/common.helper.js`](../../back-pre/app/controllers/helpers/common.helper.js) (OTP, creazione
> cliente Stripe) e [`helpers/cron.helper.js`](../../back-pre/app/controllers/helpers/cron.helper.js)
> (notifiche, sincronizzazione crediti sharing, push notification). Per ogni campo è stato verificato,
> quando rilevante, anche il consumo/la scrittura nei **quattro frontend** (`front-pre`, `front-customer-pre`,
> `front-app-pre`, `front-modern-pre`) — nessuna descrizione è dedotta dal nome del campo senza riscontro nel
> codice o nei dati.
>
> **Nota di contesto**: `users` è la tabella account **unica** per tutti i tipi di utente del sistema — il
> cliente finale che prenota online ("consumer"), il gestore/proprietario di uno stabilimento
> ("superadmin"), e tutto lo staff operativo (manager, cassa, bagnini, camerieri, ecc.). Il tipo di
> account è determinato dalla riga in `user_roles` (vedi [`user_roles.md`](user_roles.md) e
> [`roles.md`](roles.md)), non da un campo `type` dedicato su questa tabella. Molti campi hanno senso solo
> per una categoria di utente (es. `license`/`subscriptionStatus` solo per il gestore "superadmin" di uno
> stabilimento) e restano `NULL` per tutti gli altri — questo spiega perché, nel campione locale, solo 26
> delle 100 righe hanno valorizzato l'intero gruppo di campi "abbonamento/licenza".

---

## Campi

### id
- **Tipo**: `bigint`, `AUTO_INCREMENT`, `NOT NULL`
- **Vincoli**: chiave primaria. **Nota**: il model Sequelize lo dichiara `Sequelize.INTEGER`
  (`user.model.js:3-8`), ma la colonna fisica è `bigint` — discrepanza fra model e schema reale (non causa
  problemi pratici sotto i limiti di un `INTEGER`, ma da correggere nel nuovo model se si porta lo stesso
  ORM, e comunque da modellare come tipo a 64 bit nella riscrittura visto che è già così a DB).
- **Relazioni**: referenziato da `properties.propertyOwner` (applicativo, nessuna FK — vedi
  [`properties.md`](properties.md#propertyowner)), da `user_roles.userId` (applicativo, nessuna FK — vedi
  [`user_roles.md`](user_roles.md)), e da numerose altre tabelle non ancora documentate in questa fase
  (`bathouses.userId`, `booking.createdBy`, ecc.).
- **Descrizione**: identificativo tecnico dell'account. `AUTO_INCREMENT` corrente a 5985 con solo 100 righe
  nel campione — come già osservato per `properties`, segno di molte righe non incluse in questa copia
  parziale del DB.
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### username
- **Tipo**: `varchar(255)`
- **Vincoli**: **nessun `UNIQUE`** a livello DB (verificato in `SHOW CREATE TABLE`), nonostante sia uno dei
  due identificatori di login (`signin`, `auth.controller.js:309-311`: `WHERE email = :username OR username
  = :username`).
- **Relazioni**: —
- **Descrizione**: nel flusso "consumer" (`front-customer-pre/src/components/auth/SignUp.js`) **non esiste
  un campo username separato nel form** — verificato: il payload di `signup` non include mai `username`
  esplicitamente diverso dall'email in nessun punto della UI consumer trovato. **Confermato dai dati con una
  query mirata**: fra i 74 utenti del campione **senza `propertyId`** (cioè non staff — il sottoinsieme che
  corrisponde ai "consumer"), tutti e 74 hanno `username === email`; le uniche 3 eccezioni su tutte le 100
  righe (`id` 199, 3322, 3822) sono tutti utenti **con `propertyId` valorizzato**, cioè staff, per cui il
  form separato esiste (vedi sotto). Per gli utenti staff creati da `AddUserForm.js`, infatti, esiste un
  campo "username" indipendente nel form (usato ad es. per il login del ruolo `waiter`/`cashoperator` in
  cassa, dove digitare un'email intera è scomodo).
- **Nullabile**: Sì
- **Esempio di valori**: "info@tuaregbeach.it"

### password
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno a livello DB
- **Relazioni**: —
- **Default**: nessuno; `NULL` per gli utenti creati via social login (verificato: 11 righe `NULL` su 100
  nel campione, un numero superiore agli 8 utenti con `loginMethod` social — indica che anche alcuni utenti
  `loginMethod = "normal"` hanno `password = NULL`, situazione che impedirebbe il login normale finché non
  viene impostata una password, es. tramite `forgot`/`reset`)
- **Descrizione**: hash bcrypt della password (`bcrypt.hashSync(req.body.password, 8)` — cost factor `8`,
  già segnalato come sotto lo standard raccomandato in
  [01-criticita-e-indicazioni-backend.md §7](../../docs/01-criticita-e-indicazioni-backend.md)). Scritto in
  `auth.controller.js:signup/usersignup` (creazione) e in `password.controller.js:updatepasswordByEmail`/
  `updatepassword` (reset — vedi le criticità già documentate su questo flusso, chiave di cifratura statica
  ed endpoint privi di autenticazione). **Mai restituito** in nessuna risposta API verificata (corretto).
  **Verificato sul DB dev con un hash reale**: `"$2a$08$Hi0bd492EswkYdWbFPowcubl1EzoAhWWMc.FSH0.9v8dZlepL5Efu"`
  — formato bcrypt (`$2a$`), cost factor `08` confermato, coerente col codice. **Percorso di miglioramento
  della sicurezza senza rompere la compatibilità**: il formato bcrypt incorpora il cost factor nell'hash
  stesso, quindi `bcrypt.compare()` funziona automaticamente con qualunque cost salvato — si può alzare il
  cost per le **nuove** password (es. 10-12 invece di 8) senza invalidare quelle esistenti. Consigliato: (1)
  alzare subito il cost per nuove registrazioni/reset; (2) rehash trasparente al login riuscito se il cost
  salvato è sotto la soglia target; (3) aggiungere un pepper server-side; (4) rate limiting su
  login/reset/OTP.
- **Nullabile**: Sì (41 righe `NULL` + 5 stringa vuota su 809 nel DB dev)
- **Esempio di valori**: "$2a$08$Hi0bd492EswkYdWbFPowcubl1EzoAhWWMc.FSH0.9v8dZlepL5Efu"

### firstname / lastname
- **Tipo**: `varchar(255)` ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: nome e cognome. Raccolti in tutti i form di registrazione verificati (consumer:
  `front-customer-pre/src/components/auth/SignUp.js`, righe 26-27 `FirstName`/`LastName`; staff:
  `AddUserForm.js`); modificabili dal proprietario dell'account tramite
  `user.controller.js:updatepersonalDetails` (profilo personale, self-service) e dall'admin tramite
  `updateuser`/`updatesuperadmin`. **Nel campione locale 5 righe su 100 hanno `firstname`/`lastname`
  entrambi stringa vuota** (non `NULL`) — verosimilmente utenti creati con un flusso che non li richiede
  obbligatoriamente (es. `AddUserForm.js` non ha validazione bloccante su questi campi, a differenza del
  consumer `SignUp.js` che li richiede). **Osservato anche sul DB dev**: alcuni valori letterali `"undefined"`
  (la stringa, non un vero `NULL`) — bug lato frontend che invia la stringa `"undefined"` invece di omettere
  il campo o inviare una stringa vuota.
- **Nullabile**: Sì
- **Esempio di valori**: "Aditya ", "Grimaldi"

### email
- **Tipo**: `varchar(255)`
- **Vincoli**: **nessun `UNIQUE`** a livello DB (verificato) — è però l'identificatore primario usato per
  login (`signin`), reset password (`forgot`/`reset`/`updatepasswordByEmail`), il controllo di disponibilità
  in fase di registrazione (`checkEmail.controller.js:checkemail`, chiamato dal consumer `SignUp.js` prima
  dell'invio) e per l'aggiornamento crediti sharing (`UPDATE users ... WHERE email = :email`, vedi
  `total_number_of_credit` sotto). **L'assenza di un vincolo `UNIQUE`, combinata con l'uso sistematico di
  `email` come chiave di lookup/update invece di `id`, è un rischio concreto di integrità dati verificato
  nello schema**: se due righe condividessero la stessa email (nulla lo impedisce a livello DB), `signin`
  troverebbe una riga arbitraria (la prima restituita da MySQL) e l'update crediti aggiornerebbe **entrambe**
  le righe contemporaneamente (`User.update` senza `LIMIT` colpisce tutte le righe che soddisfano il
  `WHERE`).
- **Relazioni**: —
- **Descrizione**: email di contatto e login. Alla cancellazione "soft" dell'account (`deleteuser`/
  `deleteAccount`, vedi `userStatus` sotto) viene **rinominata** con prefisso `del_DD/MM/YYYY_` per
  liberare l'indirizzo — pattern verificato identico in entrambe le funzioni
  (`user.controller.js:154,574`).
- **Nullabile**: Sì
- **Esempio di valori**: "del_16/6/2026_beuzafroufrauqua-6279@yopmail.com" (esempio reale dal DB dev, conferma
  il pattern di rinomina alla cancellazione)

### mobileCountryCode
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: pensato per il prefisso internazionale del cellulare, letto da `req.body.mobileCountryCode`
  in `auth.controller.js` (`signup`, `usersignup`, `social`) e `user.controller.js` (`updateuser`,
  `updatesuperadmin`). **Verificato come sostanzialmente non popolato da nessun frontend**:
  - il consumer signup (`front-customer-pre/src/components/auth/SignUp.js`) non invia affatto questa
    chiave (solo `countryCode`, vedi sotto — stesso schema già visto su `properties.mobileCountryCode`);
  - il form di creazione staff (`front-pre/src/components/userManagement/AddUserForm.js:194`) la invia
    **hardcoded a stringa vuota** (`mobileCountryCode: ""`).
  **Confermato dai dati**: 74 righe `NULL` (i consumer, per cui la chiave non è mai nel body — Sequelize
  salva `NULL`), 26 righe stringa vuota `''` (i 26 utenti con `propertyId` valorizzato, cioè lo staff creato
  con `AddUserForm`/`usersignup`) — **zero righe con un prefisso reale**, su tutti e 100 gli utenti del
  campione. È il campo equivalente, sulla tabella `users`, dello stesso pattern già documentato per
  `properties.mobileCountryCode`.
  **Correzione dal DB dev (campione più ampio, 809 righe)**: 404 righe `NULL` + 375 stringa vuota, ma esiste
  **un valore reale**: `"+91"` su 30 righe — questo contraddice parzialmente l'affermazione "zero valori
  reali" fatta sopra sul campione locale. Va comunque interpretato con cautela: è un singolo prefisso
  indiano isolato (verosimilmente dati di test/dev, coerente col cluster `country = "india"` osservato più
  sotto), non un vero utilizzo diffuso del campo — la conclusione operativa resta invariata:
  `mobileCountryCode` è sostanzialmente morto in produzione, `countryCode` (vedi sotto) resta la fonte di
  verità.
- **Nullabile**: Sì
- **Esempio di valori**: "" (stringa vuota, il valore più comune); valore reale isolato: "+91"

### mobile
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: numero di cellulare, **senza prefisso internazionale incluso** (il prefisso avrebbe
  dovuto stare in `mobileCountryCode`, di fatto sempre vuoto — vedi sopra — o va dedotto da `countryCode`
  per gli utenti consumer, campo concettualmente analogo ma popolato tramite un percorso diverso, vedi
  sotto). Raccolto in tutti i form di registrazione/creazione utente verificati. 10 righe su 100 nel
  campione hanno `mobile` stringa vuota (non obbligatorio in tutti i flussi).
- **Nullabile**: Sì
- **Esempio di valori**: "0352145789"

### birthDate
- **Tipo**: `date` (`DATEONLY` nel model)
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: **campo verificato completamente morto**: dichiarato nel model, letto/scritto in modo
  generico come pass-through (`req.body.birthDate`) in `signin`/`social`/`sendToken`/`updateuser`/
  `updatesuperadmin`, ma **nessun form in nessuno dei quattro frontend analizzati lo raccoglie**
  (ricerca esaustiva della stringa `birthDate` su tutti e quattro: zero risultati). Coerente con il dato
  osservato: **100% `NULL`** su tutte le 100 righe del campione.
  **Correzione dal DB dev (809 righe)**: il campo **non** è sempre `NULL` come nel campione locale — 708
  righe `NULL`, ma **101 righe hanno un valore fisso identico: `"1899-11-29"`**. Si tratta chiaramente di un
  valore **sentinella/placeholder di sistema** (prossimo all'epoch storico 1899-12-30/1900 usato da molti
  sistemi come "data zero"), non di una vera data di nascita raccolta da un form — nessun form la raccoglie,
  come verificato sopra. Resta quindi coerente con la conclusione "campo morto nella sostanza", ma va
  corretta l'affermazione "100% NULL": quando valorizzato, il valore non è un dato utente reale.
- **Nullabile**: Sì
- **Esempio di valori**: "1899-11-29" (valore sentinella, 101/809 righe — non una vera data di nascita)

### gender
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: stessa situazione di `birthDate` — pass-through generico nel backend, **nessun frontend
  lo raccoglie** (ricerca esaustiva, zero risultati), **100% `NULL`** nel campione. **Confermato sul DB dev**
  (809 righe): 708 `NULL` + 101 stringa vuota, zero valori reali — sostanzialmente morto anche sul campione
  più ampio.
- **Nullabile**: Sì
- **Esempio di valori**: nessun valore reale osservato (solo `NULL` o stringa vuota)
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### googleId / facebookId / appleId
- **Tipo**: `varchar(255)` (`googleId`, `facebookId`), `text` (`appleId`, perché contiene il JWT restituito
  da Apple, non un semplice id — verificato: il valore campionato è una stringa che inizia con
  `eyJraWQi...`, un JWT compatto in Base64URL)
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: identificativi del provider usati per il login social, scritti da
  `auth.controller.js:social` (righe 452-675, gestisce Google/Apple/generico; il flusso Facebook usa un
  controllo separato — `handleFbAccessToken`, righe 677-693 — che verifica il token lato Facebook Graph API
  ma **non risulta scrivere `facebookId` su `users`** in quella funzione: il campo viene comunque accettato
  come `req.body.facebookId` dentro `social`, quindi la scrittura avviene se il frontend lo passa insieme al
  resto). Consumati/generati nei tre form social dedicati di `front-customer-pre`
  (`GoogleLoginPage.js`, `FacebookLogin.js`, `AppleLoginPage.js`). **Confermato dai dati**: `googleId`
  valorizzato per 8 righe, `appleId` per 5, `facebookId` per **0 righe** su 100 — nel campione locale nessun
  utente ha mai effettuato login/registrazione con Facebook, pur essendoci il componente dedicato nel
  frontend.
  **CORREZIONE IMPORTANTE dal DB dev (809 righe)**: l'affermazione "0 righe/nessun utente ha mai fatto login
  con Facebook" è **falsa** sul campione più ampio — sul DB dev esistono **13 valori reali** di `facebookId`
  (es. `"117744167664763"`, `"1989100204612434"`). Il login Facebook esiste ed è stato usato, solo non
  compariva nel campione locale ristretto a 100 righe. Coerente con la correzione analoga su `loginMethod`
  sotto, che sul DB dev mostra 9 utenti con `loginMethod = "facebook"`.
- **Nullabile**: Sì (tutti e tre i campi)
- **Esempio di valori**: `googleId`: "106795873383288219217"; `facebookId`: "117744167664763",
  "1989100204612434"; `appleId`: "eyJraWQi..." (JWT troncato)

### loginMethod
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno (valore libero, non enum a DB)
- **Relazioni**: —
- **Descrizione**: modalità di autenticazione usata l'ultima volta. Valori osservati nel campione:
  `normal` (88), `google` (7), `apple` (5) — nessun `facebook`, coerente con quanto sopra. Impostato a
  `"normal"` esplicitamente in `signup`/`usersignup`/`signin` (anche per chi già esisteva, ad ogni login
  normale viene riscritto — `auth.controller.js:347`) e al valore ricevuto (`google`/`apple`/`facebook`) in
  `social`.
  **CORREZIONE IMPORTANTE dal DB dev (809 righe)**: sono presenti **4 valori reali**, non 3 come nel campione
  locale — `"normal"` (756), `"google"` (36), `"facebook"` (9), `"apple"` (7). L'affermazione precedente
  "nessun facebook, coerente con quanto sopra" era basata solo sul campione locale limitato: sul DB dev il
  login Facebook **esiste ed è usato** da 9 utenti, coerentemente con i 13 `facebookId` reali osservati sopra
  (la lieve differenza fra 9 e 13 è compatibile con utenti che hanno collegato Facebook ma il cui ultimo
  login registrato è avvenuto con un altro metodo). **Candidato a enumerazione**: 4 valori chiusi e stabili,
  determinati dai soli provider di login supportati dal codice — `ENUM('normal','google','facebook','apple')`
  a livello DB è appropriato.
- **Nullabile**: Sì
- **Esempio di valori**: "normal", "google", "facebook", "apple"

### userStatus
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno (valore libero, non enum a DB; **100% delle 100 righe del campione hanno
  `userStatus = "active"`** — nessuna riga `inactive`/altro valore nel campione)
- **Relazioni**: —
- **Descrizione**: stato dell'account, controllato ad ogni tentativo di login (`if (user.userStatus ==
  "active")` in `signin`/`sendToken`) — se diverso da `"active"` il login è rifiutato col messaggio
  fuorviante *"Account has deleted"* anche per stati diversi da una vera cancellazione. Impostato a
  `"inactive"` (unico altro valore trovato nel codice) da `deleteuser`/`deleteAccount` — è quindi il
  meccanismo di **soft delete** dell'account, sullo stesso modello già visto per `properties.isDelete` ma
  qui come stringa invece che booleano, e senza un campo booleano dedicato. **Confermato sul DB dev (809
  righe)**: solo 2 valori reali, `"active"` (598) e `"inactive"` (211) — set chiuso confermato anche sul
  campione più ampio. **Candidato a enumerazione**: solo 2 valori possibili, entrambi scritti da codice
  applicativo esplicito (mai un terzo stato osservato) — `ENUM('active','inactive')` a livello DB è
  appropriato e più esplicito dell'attuale `varchar` libero.
- **Nullabile**: Sì
- **Esempio di valori**: "active", "inactive"

### loginOtp
- **Tipo**: `int`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: **campo verificato completamente morto**: l'unica occorrenza in tutto `back-pre` è la
  sua dichiarazione nel model (`user.model.js:51-53`) — nessun controller lo legge o lo scrive, nessun
  frontend vi fa riferimento. Da non riportare nel nuovo modello (o da chiedere al committente se era
  destinato a un flusso OTP diverso da quello realmente implementato con `two_factor_otp`/`expiry`, vedi
  sotto). **100% `NULL`** nel campione, coerente. **Sul DB dev (809 righe)**: 707 `NULL` + 102 righe con
  valore `0` — nessun vero OTP mai scritto, coerente con "campo morto/mai usato per un vero OTP" anche sul
  campione più ampio.
- **Nullabile**: Sì
- **Esempio di valori**: 0 (mai un vero codice OTP)

### profilePic
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: URL della foto profilo. Per gli utenti social, valorizzato con l'URL fornito dal
  provider (`req.body.profilePic` in `social`, tipicamente popolato lato frontend con l'avatar Google —
  verificato nei 5 valori campionati, tutti URL `https://lh3.googleusercontent.com/...`). Per gli altri
  utenti resta gestito manualmente lato frontend (upload proprio) se previsto dal form profilo.
- **Nullabile**: Sì
- **Esempio di valori**: "https://lh3.googleusercontent.com/a/..." (avatar Google/Facebook)

### createdAt / updatedAt
- **Tipo**: `datetime`, nessun default DB, gestiti da Sequelize
- **Relazioni**: —
- **Descrizione**: standard. Da notare che `updatedAt` viene **riscritto ad ogni login** (`signin`,
  `sendToken`: `User.update({ updatedAt: new Date(), ... })`) — quindi su questa tabella **non riflette
  l'ultima modifica ai dati del profilo**, ma può riflettere anche un semplice login: per quello scopo
  esiste il campo dedicato `lastLogin`.
- **Nullabile**: Sì
- **Esempio di valori**: "2024-05-02 18:11:43", "2026-08-30 07:52:19"

### lastLogin
- **Tipo**: `datetime`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: data/ora dell'ultimo login riuscito, aggiornata in `signin`, `social` (solo per utenti
  già esistenti) e `sendToken` — **cioè anche da `sendToken`**, l'endpoint già segnalato in
  [01-criticita-e-indicazioni-backend.md §7](../../docs/01-criticita-e-indicazioni-backend.md) come privo di
  verifica password: chiunque lo invochi con una email valida aggiorna silenziosamente anche questo campo,
  che quindi non è un indicatore affidabile di un login realmente autenticato dal legittimo proprietario
  dell'account.
- **Nullabile**: Sì
- **Esempio di valori**: "2026-08-30 07:52:19"

### country
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: stessa storia di `mobileCountryCode`: letto da `req.body.country` in tutti i punti di
  scrittura, ma **il consumer signup non lo invia mai** e `AddUserForm.js:196` lo invia **hardcoded a
  stringa vuota** (`country: ""`). **Confermato dai dati**: 74 `NULL`, 26 stringa vuota, zero valori reali —
  identico schema di `properties.country`.
  **Confermato/precisato sul DB dev (809 righe)**: stesso schema del campione locale, sostanzialmente morto,
  salvo un unico valore reale: `"india"` su 28 righe — un cluster isolato, verosimilmente dati di
  test/dev, non un vero utilizzo diffuso del campo (coerente col cluster analogo osservato su
  `mobileCountryCode = "+91"` sopra).
- **Nullabile**: Sì
- **Esempio di valori**: "" (stringa vuota, il valore più comune); valore reale isolato: "india"

### notifyWhatsapp / notifyEmail
- **Tipo**: `int` (booleano 0/1) ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: preferenze di notifica **per lo staff** di uno stabilimento (non per i clienti finali):
  scritti solo da `usersignup`/`updateuser`/`updatesuperadmin` (mai da `signup`/`social`, i flussi
  consumer) — coerente col fatto che nel campione sono `NULL` per tutte le 74 righe senza `propertyId` e
  valorizzati per le 26 con `propertyId`. `notifyEmail` determina, insieme al ruolo, chi riceve le email di
  notifica booking (vedi `booking.controller.js:6841-6845`, già documentato in
  [`properties.md#propertyowner`](properties.md#propertyowner): il destinatario "manager" viene risolto
  filtrando `user_roles.roleId IN (2,4,5,6)` **e** `notifyEmail = 1`). `notifyWhatsapp` non è stato
  riscontrato come effettivamente letto in nessun punto di invio WhatsApp verificato in questa analisi (solo
  scritto, mai letto come condizione) — da verificare più a fondo in una fase successiva se rilevante per la
  riscrittura del modulo comunicazioni.
- **Nullabile**: Sì
- **Esempio di valori**: 0, 1

### createdBy
- **Tipo**: `int`
- **Vincoli**: nessuno (nessuna FK, anche se concettualmente riferisce `users.id` dell'amministratore che
  ha creato l'account)
- **Relazioni**: applicativa verso `users.id`, non verificata da alcun vincolo
- **Descrizione**: chi ha creato l'account. Scritto da `usersignup`/`updateuser`
  (`createdBy: req.body.createdBy` — cioè è il **frontend** a dichiarare chi è il creatore, non dedotto
  server-side dal token dell'utente autenticato che sta chiamando l'API: un potenziale punto debole se mai
  usato a fini di audit, perché falsificabile dal client). **Confermato dai dati**: 21 dei 26 utenti staff
  nel campione hanno lo stesso `createdBy = 200` — un singolo utente amministratore che ha creato la
  maggioranza dello staff campionato.
- **Nullabile**: Sì
- **Esempio di valori**: "200"

### propertyId
- **Tipo**: `int`
- **Vincoli**: **vincolo FK reale a livello DB** verso `properties.id`
  (`CONSTRAINT users_ibfk_1 FOREIGN KEY (propertyId) REFERENCES properties (id) ON UPDATE CASCADE`) — a
  differenza della quasi totalità delle altre relazioni incontrate finora in questo progetto, **questa è
  una delle 12 relazioni realmente vincolate nel DB** (elencata anche in
  [`properties.md`](properties.md#tabelle-collegate)). Nessun `ON DELETE` esplicito (default `RESTRICT`):
  una property non può essere cancellata fisicamente finché esistono utenti collegati.
- **Relazioni**: FK reale verso `properties.id`
- **Descrizione**: stabilimento di appartenenza per lo staff (incluso il "superadmin" proprietario); `NULL`
  per gli utenti "consumer" (clienti finali, che non appartengono a nessuno stabilimento). È il campo su cui
  si basa la maggior parte delle query "utenti di questa property" nel codice (es. `userlist`,
  risoluzione destinatario notifiche booking).
- **Nullabile**: Sì
- **Esempio di valori**: "179"

### license
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno (valore libero; **unico valore osservato nel campione: `"advanced"`**, su tutte le
  26 righe con `propertyId` valorizzato — non è stato possibile determinare dal solo campione locale quali
  altri valori esistano)
- **Relazioni**: —
- **Descrizione**: piano di abbonamento dello stabilimento, associato allo staff (in pratica soprattutto al
  "superadmin"). Gestito in `usersignup`/`updateuser`/`updatesuperadmin`: se non specificato esplicitamente
  nel body, il backend **eredita** license/startDate/endDate da un altro utente attivo della stessa
  property che li ha già valorizzati (query `User.findOne({ where: { propertyId, license: {[Op.not]:
  null]}, ... } })`) — meccanismo per cui, creando un nuovo membro dello staff, la licenza dello
  stabilimento (non della singola persona) si "propaga" automaticamente al nuovo account.
  **Confermato sul DB dev (809 righe)**: 3 valori — `"advanced"` (288), `""` (78, stringa vuota, dato
  sporco), `"basic"` (28). **Candidato a enumerazione, ma non con ENUM testuale**: l'insieme dei valori è
  chiuso, tuttavia esiste già una tabella dedicata `licenses` con lo stesso scopo (vedi
  [`licenses.md`](licenses.md)) — la soluzione corretta è quindi una **FK verso `licenses.id`**, non un
  `ENUM`, ripulendo prima le 78 righe con stringa vuota.
- **Nullabile**: Sì
- **Esempio di valori**: "advanced", "basic" (dato sporco osservato: "" stringa vuota)

### startDate / endDate
- **Tipo**: `datetime` ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: inizio/fine validità della licenza. `endDate` è il campo controllato ogni notte dal cron
  `cron-disableproperty.js` (già documentato in
  [`properties.md#superadminonlineflag`](properties.md#superadminonlineflag)): se `endDate < oggi` e
  `subscriptionStatus = "enable"`, l'account viene marcato `subscriptionStatus: "disable"` e la property
  collegata riceve `superAdminOnlineFlag: 0`. Calcolati in `usersignup`/`updatesuperadmin` come
  `new Date()` + `dateWithMonthsDelay(licenseMonths)` quando viene specificata una nuova licenza.
- **Nullabile**: Sì
- **Esempio di valori**: "2024-01-15 00:00:00", "2025-01-15 00:00:00"

### subscriptionStatus
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno (valore libero; valori osservati nel codice: `"enable"`/`"disable"`; **100% delle 26
  righe valorizzate nel campione sono `"enable"`** — nessun account scaduto nel campione)
- **Relazioni**: —
- **Descrizione**: stato dell'abbonamento. `updateuser` (righe 388-401) contiene una logica non banale:
  quando lo stato passa da un valore diverso a `"disable"`, **allunga** la `endDate` esistente della
  differenza in giorni fra `endDate` e il momento della disattivazione (`endDate.setDate(endDate.getDate() +
  diffInDays)` — che di fatto, dato `diffInTime = endDate - now`, equivale a raddoppiare la distanza residua
  invece di "congelarla": codice sospetto/probabilmente un bug aritmetico, da verificare con un test
  puntuale prima di riportare lo stesso comportamento nella riscrittura) — un tentativo di "congelare" i
  giorni residui dell'abbonamento durante una sospensione, ma implementato in un modo che merita
  verifica puntuale prima di essere riprodotto identico nella riscrittura.
  **Confermato sul DB dev (809 righe)**: 3 valori — `"disable"` (191), `"enable"` (110), `""` (77, stringa
  vuota, dato sporco). **Candidato a enumerazione**: solo 2 valori reali usati dal codice (`"enable"`/
  `"disable"`) più la pulizia delle stringhe vuote — `ENUM('enable','disable')` a livello DB è appropriato e
  eliminerebbe alla radice il dato sporco osservato.
- **Nullabile**: Sì
- **Esempio di valori**: "enable", "disable" (dato sporco osservato: "" stringa vuota)

### languageCode
- **Tipo**: `int`, default `2`
- **Vincoli**: nessuno; commento nel model: *"1: English, 2: Italian"* (`user.model.js:99-103`)
- **Relazioni**: —
- **Descrizione**: lingua preferita dell'utente, usata per scegliere fra le varianti EN/IT delle email
  transazionali in tutto il backend (pattern `if (languageCode == 1) {...} else {...}` ripetuto in
  `auth.controller.js`, `sysadmin.controller.js`, ecc.). **Confermato dai dati**: 98/100 valore `2`
  (italiano), 2/100 valore `1` — nessun altro valore, coerente con un sistema che di fatto supporta solo
  EN/IT nonostante l'interfaccia frontend esponga molte più lingue nei file di traduzione (`ar`, `de`, `es`,
  `fr`, `zh` sono presenti come file `locale/*.js` ma non hanno un `languageCode` numerico dedicato lato
  backend — le email transazionali multilingua sono quindi limitate a due lingue anche se la UI ne offre
  altre).
  **Confermato sul DB dev (809 righe)**: solo 2 valori, `2` (503) e `1` (306) — nessuna terza lingua mai
  usata lato backend, coerente col campione locale. **Candidato a enumerazione**: solo 2 valori possibili per
  definizione (commento nel model), stabili — `ENUM(1,2)` o un booleano dedicato (`isItalian`) a livello DB
  sarebbe più esplicito dell'attuale `int` generico con significato dedotto da un commento.
- **Nullabile**: Sì
- **Esempio di valori**: 2, 1

### disableNotification
- **Tipo**: `int` (booleano 0/1), default `0`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: interruttore generale delle notifiche in-app, distinto da `notifyEmail`/`notifyWhatsapp`
  (quelli riguardano i canali email/WhatsApp per lo staff; questo è un toggle personale gestito dall'utente
  stesso tramite `user.controller.js:disableNotification`, chiamato dal proprio pannello impostazioni —
  consumato in tutti e tre i frontend con UI (`front-pre`, `front-customer-pre`:
  `NotificationTab.js`; `front-app-pre`: `Notification.screen.js`).
- **Nullabile**: Sì
- **Esempio di valori**: 0, 1

### stripeCustomerId
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: pensato per l'id del Customer Stripe del singolo utente (per pagamenti con carta
  salvata). **Unico punto di scrittura reale verificato**: `common.helper.js:createCustomerStripe` (righe
  537-557), che crea un Customer Stripe e salva `stripeCustomerId: customer.id` filtrando per **email**
  (stesso rischio di `email` senza `UNIQUE` già segnalato sopra) — chiamato da `payment.controller.js` e
  4 volte da `servicereservation.controller.js`, quindi non è codice morto. **Nota**: la funzione gemella
  `createStripeToken` nello stesso file (righe 522-536) contiene un riferimento rotto
  (`exp_year: exp_month.exp_year`, `cvc: exp_year.cvc` — `exp_month`/`exp_year` non sono variabili definite
  in quello scope) che la fa fallire sempre (l'errore è catturato e la funzione ritorna `false`); se questo
  percorso specifico di creazione token fosse l'unico usato prima di `createCustomerStripe`, spiegherebbe
  perché il campo risulta **100% `NULL`** su tutte le 100 righe del campione — ma **attenzione**: non è
  stato verificato con certezza che tutte le chiamate a `createCustomerStripe` passino da questo token
  rotto (alcuni flussi potrebbero fornire un token creato diversamente, es. da Stripe.js lato client), quindi
  questa resta un'ipotesi plausibile e non una conclusione certa. **Da non confondere** con il campo
  `stripeCustomerId` restituito nella risposta di `signin`/`sendToken`: quel valore **non** proviene da
  questa colonna, ma da `property.stripe_configrations[0].sellerStripeConnectId` (l'account Stripe Connect
  dello **stabilimento**, non un customer del singolo utente) — un riuso ambiguo dello stesso nome di
  campo per due concetti diversi fra risposta API e colonna DB.
  **Sul DB dev (809 righe)**: quasi sempre `NULL` (808/809), ma un valore reale osservato è
  `"acct_1J8QLw2RS3MLI6rZ"` — che è a sua volta un **account Stripe Connect** (prefisso `acct_`, non
  `cus_` come un vero Customer id), non un Customer Stripe del singolo utente. Questo conferma
  concretamente, con un dato reale sulla colonna stessa, la criticità già documentata sopra sul riuso
  ambiguo del nome `stripeCustomerId` fra i due concetti diversi (account Connect dello stabilimento vs
  customer del singolo utente).
- **Nullabile**: Sì
- **Esempio di valori**: "acct_1J8QLw2RS3MLI6rZ" (nota: è un account Connect, non un vero customer id)

### totalCreditsEarned
- **Tipo**: `int`, default `0`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: **campo verificato di sola lettura**: restituito in `signin`/`sendToken`
  (`totalCreditsEarned: user.totalCreditsEarned`), ma **nessun punto del codice lo scrive** mai a un valore
  diverso dal default — a differenza dei due campi seguenti (`total_number_of_credit`/`total_credit_value`),
  che sono invece attivamente aggiornati. **100% `0`** su tutte le 100 righe del campione, coerente. Nome
  molto simile a `total_number_of_credit` (rischio di confusione concettuale/di manutenzione, probabile
  residuo di un refactor della feature "crediti" mai completato). **Confermato sul DB dev**: sempre `0` su
  tutte le 809 righe reali — campo confermato morto anche sul campione più ampio.
- **Nullabile**: Sì
- **Esempio di valori**: 0

### total_number_of_credit / total_credit_value
- **Tipo**: `int` ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: numero e valore monetario cumulativo dei crediti maturati dall'utente tramite la
  funzionalità di **sharing** (condivisione di una location fra più prenotazioni, tabella
  `sharing_details` — già incontrata come criticità in
  [01-criticita-e-indicazioni-backend.md](../../docs/01-criticita-e-indicazioni-backend.md)). Aggiornati
  da un job schedulato (`helpers/cron.helper.js:820-828`) con una query raw parametrizzata
  `UPDATE users SET total_number_of_credit = :numOfCredits, total_credit_value = :creditValue WHERE email =
  :email` — **per email, non per id** (stesso rischio già segnalato sopra se esistessero email duplicate).
  **Confermato dai dati**: solo 3 righe su 100 hanno un valore non-`NULL` (`total_number_of_credit = 1` per
  tutte e 3, `total_credit_value` fra 7 e 10) — feature usata raramente nel campione, coerente con l'essere
  legata a un caso d'uso specifico (sharing) non praticato dalla maggioranza degli utenti campionati.
  **Sul DB dev (809 righe)**, quando valorizzato: `total_number_of_credit` = `1` (23), `15` (7), `0` (4),
  `20` (1), `25` (1), `12` (1); `total_credit_value` = `8` (19), `5` (1) — conferma che la feature è
  effettivamente usata ma solo da una minoranza di account, con `1` e `8` come valori nettamente più
  frequenti.
- **Nullabile**: Sì (entrambi)
- **Esempio di valori**: `total_number_of_credit`: 1, 15, 0; `total_credit_value`: 8, 5

### subscriptionDisableDate / subscriptionEnableDate
- **Tipo**: `datetime` ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: timestamp dell'ultima disattivazione/riattivazione dell'abbonamento. `subscriptionDisableDate`
  è scritto sia dal cron `cron-disableproperty.js` (disattivazione automatica per scadenza) sia da
  `updateuser` (disattivazione manuale, con la logica di allungamento `endDate` già descritta sopra).
  `subscriptionEnableDate` risulta scritto solo in `updateuser` quando lo stato **non** passa a
  `"disable"` (branch `else`, riga 397-401) — cioè viene aggiornato ad ogni salvataggio che non sia una
  disattivazione, non solo alla riattivazione vera e propria: il nome del campo suggerisce un significato
  più specifico ("quando è stato riabilitato") di quello che il codice implementa davvero ("ultima volta che
  non è stato disabilitato").
- **Nullabile**: Sì (entrambi)
- **Esempio di valori**: "2025-11-03 09:00:00"

### two_factor_otp / expiry
- **Tipo**: `varchar(255)` (`two_factor_otp`, nonostante contenga solo cifre — usato/confrontato come
  stringa/numero in modo intercambiabile, es. `verifyOtp` lo confronta sia nella query SQL sia con `==` in
  JS), `datetime` (`expiry`)
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: codice OTP e relativa scadenza per il login **a due fattori**, distinto dal flusso di
  reset password. Attivato in `signin`/`sendToken` solo se `roles != "consumer"` **e**
  `bathouses.isTwoFactorEnabled == 1` per lo stabilimento dell'utente (campo su `bathouses`, non su
  `users` — il 2FA è quindi una funzionalità dello **stabilimento**, applicata a tutto il suo staff, non
  un'opzione per singolo utente). Generato/salvato da `common.helper.js:sendOtpOnMail` (righe 687-697),
  verificato da `auth.controller.js:verifyOtp` (righe 833-864) — generazione e rischi già documentati in
  [01-criticita-e-indicazioni-backend.md §7](../../docs/01-criticita-e-indicazioni-backend.md) (OTP a 4
  cifre generato con `Math.random()`, nessun rate limiting, non invalidato dopo l'uso corretto — quest'
  ultimo punto verificato di nuovo qui: `verifyOtp` non azzera `two_factor_otp`/`expiry` dopo una verifica
  riuscita, quindi lo stesso OTP resta valido e riutilizzabile fino alla scadenza naturale).
  **Confermato sul DB dev**: entrambi i campi risultano **sempre `NULL`** su tutte le 809 righe (nessun
  valore distinto osservato) — il flusso 2FA è attivo per definizione nel codice, ma di fatto mai popolato
  in questo ambiente, verosimilmente perché nessuno stabilimento presente nel DB dev ha
  `isTwoFactorEnabled = 1` attivo.
- **Nullabile**: Sì (entrambi)
- **Esempio di valori**: nessun valore reale osservato (sempre `NULL` sul DB dev)

### deviceTokens
- **Tipo**: `varchar(255)`, default `''`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: token del dispositivo per le notifiche push (Firebase Cloud Messaging, `firebase-admin`
  è una dipendenza del progetto). Nome al plurale ma la colonna contiene **una sola stringa** (non un
  array/JSON) — inviato dal frontend ad ogni login (`signin`, riga 348: `deviceTokens: req.body.deviceTokens`)
  e letto dai job di invio notifiche/campagne marketing (`helpers/cron.helper.js:1377-1382,1771-1776,
  1834-1839`, chiamata a `sendPushNotification`). **Osservazione sul codice di invio**
  (`cron.helper.js:2083-2101`): la chiamata a `admin.messaging().send(message)` valorizza il payload con
  `message.tokens = deviceTokens` (chiave al **plurale**, valore singolo) — l'SDK Firebase Admin usa
  convenzionalmente `message.token` (singolare) per `send()` e riserva `tokens` (plurale, array) alle
  chiamate multicast (`sendEachForMulticast`/analoghe); non è stato verificato eseguendo il codice, ma il
  nome del campo usato nel payload non corrisponde a quanto la documentazione dell'SDK associa a `.send()`,
  quindi è **plausibile** (non certo senza un test diretto) che le notifiche push inviate da questi cron non
  raggiungano mai il dispositivo. Da testare esplicitamente prima di riprodurre la stessa integrazione nella
  riscrittura. **100% delle 100 righe del campione hanno stringa vuota** `''` (mai un vero token salvato) —
  coerente sia con l'assenza di un vero client mobile collegato a questi account campionati, sia con un
  eventuale problema upstream nella scrittura del valore. **Confermato sul DB dev**: stringa vuota su tutte
  le 809 righe, senza eccezioni — conferma ulteriore, su un campione molto più ampio, che le push
  notification non funzionano/non vengono mai effettivamente registrate in nessun ambiente verificato.
- **Nullabile**: No (default `''`)
- **Esempio di valori**: "" (sempre stringa vuota, mai un vero token)
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### affiliate_selected_locations
- **Tipo**: dichiarato `Sequelize.JSON`; colonna fisica `longtext` con `CHECK json_valid(...)`, default
  `NULL`
- **Vincoli**: `CHECK json_valid(...)`
- **Relazioni**: —
- **Descrizione**: array delle location (ombrelloni/settori) che un utente con ruolo `affiliate` è
  autorizzato a gestire. Scritto da `usersignup`/`updateuser` (`req.body.affiliate_selected_locations`),
  popolato dal frontend in `AddUserForm.js` quando si seleziona il ruolo "Affiliato" (righe 203-205, 432).
  **Effetto verificato**: `front-pre/src/components/reserveArticle/BeachMap/ConfigSecPanelChildren.js`
  (righe 598-599, 779-798) lo usa (insieme a `isAffiliateFlag`) per disabilitare visivamente, sulla mappa
  spiaggia, tutte le location **non** incluse in questo elenco quando l'utente loggato ha ruolo
  `"affiliate"` — enforcement verificato **solo lato client** (nessun controllo server-side equivalente
  trovato sugli endpoint di creazione prenotazione). **100% `NULL`** nel campione — nessun utente affiliato
  presente nei 100 record locali (coerente con `roles.md`: il ruolo `affiliate` non compare affatto nel
  campione `user_roles` valido).
  **CORREZIONE dal DB dev (809 righe)**: l'affermazione "100% NULL, nessun utente affiliato nei 100 record
  locali" non regge sul campione più ampio — sul DB dev esistono **14 utenti** con questo campo popolato,
  con valori reali come `["R4C18","R4C22"]` e `["R1C18","R1C19","R1C20","R2C19","R2C21"]`. La funzionalità
  "affiliato" **è** usata, solo non compariva nel campione locale ristretto.
- **Nullabile**: Sì
- **Esempio di valori**: `["R4C18","R4C22"]`, `["R1C18","R1C19","R1C20","R2C19","R2C21"]`

### isNewsSectionEnable
- **Tipo**: `tinyint(1)` (booleano), default `1`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: mostra/nasconde una sezione "novità" nel pannello personale, gestita dall'utente stesso
  tramite `UpdateAccountDialog.js` (`front-pre/src/components/account/`) insieme ai due campi seguenti.
  98/100 righe hanno il valore di default `1`. **Sul DB dev (809 righe)**: sempre `1`, senza eccezioni.
- **Nullabile**: Sì
- **Esempio di valori**: 1

### enableManagementBookingSound / enableOnlineBookingSound
- **Tipo**: `tinyint(1)` (booleano) ciascuno, default `1`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: due interruttori indipendenti per l'allerta sonora del pannello gestionale quando arriva
  una nuova prenotazione, differenziati per **origine** della prenotazione — verificato in
  `front-pre/src/layout/header/AdminHeader/AdminHeader.js` (righe 521-535): quando arriva una notifica
  real-time (Socket.io), se il testo della notifica contiene `"Online"` viene controllato
  `enableOnlineBookingSound` (prenotazione creata dal cliente finale), altrimenti
  `enableManagementBookingSound` (prenotazione creata da un operatore/staff). Impostabili dall'utente
  tramite `UpdateAccountDialog.js`. **Sul DB dev (809 righe)**: `1` (806), `0` (3) per entrambi i campi.
- **Nullabile**: Sì (entrambi)
- **Esempio di valori**: 1, 0

### countryCode
- **Tipo**: `varchar(20)`, default `'+39'`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: **a differenza di `mobileCountryCode`, questo campo è realmente popolato e funzionante**:
  è il prefisso telefonico usato nel form di registrazione **consumer**
  (`front-customer-pre/src/components/auth/SignUp.js:123`, componente `CommonCountryCode`, stato
  `selectedCode` default `"+39"`) e nel form di modifica profilo personale
  (`user.controller.js:updatepersonalDetails`, righe 722-754). **Confermato dai dati**: valorizzato su
  **tutte** le 100 righe del campione, con valori reali (`+39` → 98, `+45` → 1, `+33` → 1) — il campo più
  affidabile fra i due candidati a memorizzare il prefisso telefonico. Nella riscrittura, `countryCode`
  (non `mobileCountryCode`) è quindi il campo da considerare la fonte di verità per il prefisso del
  cellulare.
  **Sul DB dev (809 righe)**: solo 2 valori distinti — `"+39"` (808), `"+246"` (1) — set ancora più
  concentrato rispetto al campione locale (che mostrava anche `+45`/`+33`), ma coerente con la stessa
  conclusione: il campo è realmente popolato e affidabile, dominato dal prefisso italiano.
- **Nullabile**: Sì
- **Esempio di valori**: "+39", "+246"

### isAffiliateFlag
- **Tipo**: `tinyint(1)` (booleano), default `0`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: flag collegato a `affiliate_selected_locations` (vedi sopra): scritto da
  `AddUserForm.js` come `selectStations` (variabile locale del form che indica se l'admin ha scelto di
  restringere l'affiliato a location specifiche, invece che lasciarlo libero su tutte) — quindi, a
  differenza di quanto il nome potrebbe suggerire, **non indica semplicemente "questo utente è un
  affiliato"** (quella informazione sta nel ruolo `affiliate` in `user_roles`), ma **"per questo utente è
  attivo un filtro di location ristrette"**. Letto in `ConfigSecPanelChildren.js` insieme al ruolo per
  decidere se applicare la restrizione sulla mappa. **100% `0`** nel campione (nessun utente affiliato
  presente).
  **Sul DB dev (809 righe)**: `0` (802), `1` (7) — coerente col fatto che `affiliate_selected_locations` è
  ora risultato popolato su 14 righe (vedi sopra). Nota: c'è una lieve discrepanza fra i 7 utenti con
  `isAffiliateFlag = 1` e le 14 righe con `affiliate_selected_locations` valorizzato — segnalata qui per
  completezza, ma non è stata approfondita oltre in questa scheda (possibile spiegazione: utenti con
  location selezionate ma senza il flag di restrizione attivo, o viceversa).
- **Nullabile**: Sì
- **Esempio di valori**: 0, 1

### enableManagementServiceBookingSound / enableOnlineServiceBookingSound
- **Tipo**: `tinyint(1)` (booleano) ciascuno, default `0`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: coppia di interruttori analoga a `enableManagementBookingSound`/`enableOnlineBookingSound`
  (vedi sopra), ma per il modulo di **prenotazione servizi extra** (bar, escursioni, noleggi, ecc. — vedi
  [`services.md`](services.md)) invece che per le prenotazioni di postazioni spiaggia: stessa logica di
  differenziazione per **origine** della prenotazione (online da parte del cliente finale vs creata da un
  operatore/staff dal gestionale), applicata al flusso di notifica sonora quando arriva una nuova
  prenotazione di un servizio.
- **Nullabile**: Sì (entrambi)
- **Esempio di valori**: 1, 0 (distribuzione osservata sul DB dev: `1` → 806, `0` → 3, identica ai due campi
  analoghi per la spiaggia)

---

## Osservazioni per la riscrittura (riepilogo)

- **Nessun vincolo `UNIQUE` su `email`/`username`**, pur essendo gli identificatori usati per login,
  reset password e aggiornamenti (`UPDATE ... WHERE email = ...`) in più punti del codice: nella riscrittura
  `Email` deve avere un indice univoco reale (probabilmente case-insensitive), e ogni operazione deve
  riferirsi sempre all'`Id` interno, mai a `Email` come chiave di update.
- **Campi verificati completamente morti**: `birthDate`, `gender`, `loginOtp`, `totalCreditsEarned` (mai
  scritti da nessun frontend/flusso applicativo trovato) — da non riportare nel nuovo modello, o da
  riportare solo se il committente conferma un uso futuro pianificato.
- **Coppie di campi ridondanti/ambigue emerse dall'analisi**: `mobileCountryCode` (morto) vs `countryCode`
  (vivo) per il prefisso telefonico; `country` (morto, sempre vuoto) senza un equivalente "vivo"; il nome
  `stripeCustomerId` riusato per due concetti diversi (customer Stripe del singolo utente sulla colonna DB,
  account Stripe Connect dello stabilimento nella risposta API) — da disambiguare esplicitamente nella
  riscrittura.
- **Ruoli assegnabili solo alla creazione**: nessun endpoint permette di cambiare il ruolo di un utente
  esistente (vedi [`user_roles.md`](user_roles.md)) — funzionalità mancante da considerare per il nuovo
  pannello gestione utenti.
- **`createdBy` dichiarato dal client**: non dedotto server-side dal token dell'utente autenticato — nella
  riscrittura va sempre ricavato dal principal autenticato, mai da un valore inviato dal frontend.
  Il campo va inoltre trasformato in una vera FK verso `users.id`.
- **`subscriptionStatus`/`endDate`/`subscriptionDisableDate`**: la logica di allungamento della scadenza in
  `updateuser` (righe 388-401) va riletta con attenzione e testata esplicitamente prima di essere riprodotta
  — il calcolo, così com'è scritto, non sembra fare quello che il commento implicito suggerisce
  ("congelare" i giorni residui).
- **Verifica push notification**: prima di riportare l'integrazione Firebase nella riscrittura, verificare
  concretamente (con un invio di prova) se il payload `message.tokens` con singolo token funziona con la
  versione installata di `firebase-admin`, o se è un bug che ha reso silenziosamente inefficaci le notifiche
  push per tutta la vita del progetto.
- **2FA a livello di stabilimento, non di singolo utente**: `isTwoFactorEnabled` vive su `bathouses`, non su
  `users` — nella riscrittura va chiarito col committente se il 2FA debba restare una politica per
  stabilimento (tutto lo staff insieme) o diventare configurabile per singolo account, dato che oggi
  comunque non si applica mai ai "consumer".
