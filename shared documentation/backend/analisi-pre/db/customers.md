# Tabella `customers`

> Fonti usate per questa scheda: schema reale + **dati reali di produzione** (dump
> `sb_prod_dump_2026_09_08`, **224.467 righe**, query completa — le distribuzioni e percentuali riportate
> qui sotto vengono da questo dump; le versioni precedenti citavano un campione dev da 100 o ~9.670
> righe, entrambi superati), il model
> Sequelize [`back-pre/app/models/customers.model.js`](../../../../../../back-pre/app/models/customers.model.js) (nessuna
> associazione Sequelize dichiarata per questa tabella in `models/index.js` — le relazioni con `bookings`/
> `service_reservation`/`users` sono tutte gestite via query dirette, mai `belongsTo`/`hasMany`), il
> controller [`customers.controller.js`](../../../../../../back-pre/app/controllers/customers.controller.js) (letto per
> intero) e i tre punti sparsi nel resto del backend dove una riga `customers` viene creata "al volo" come
> effetto collaterale di un'altra operazione:
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js) (righe 179-296, creazione
> prenotazione location), [`servicereservation.controller.js`](../../../../../../back-pre/app/controllers/servicereservation.controller.js)
> (righe 140-190 circa, creazione prenotazione servizio), e
> [`cardTransaction.controller.js`](../../../../../../back-pre/app/controllers/cardTransaction.controller.js) (righe
> 13-55, creazione/ricerca cliente per una transazione con carta prepagata). Per il frontend: la sezione
> "Contatti" del gestionale (`front-pre/src/components/contact/`, azioni in
> [`contactActions.js`](../../../../../../front-pre/src/data/actions/contactActions.js)), la sua copia in
> `front-customer-pre` (confrontata riga per riga con l'originale — differenze trascurabili, vedi sotto),
> l'assenza totale di riferimenti in `front-app-pre`, e i riferimenti tangenziali in `front-modern-pre`
> (dati cliente incorporati nello scontrino fiscale, non una query diretta a questa tabella).
>
> **Nota di contesto — cosa NON è questa tabella**: `customers` non è l'anagrafica di chi si logga sul sito
> (quello è [`users`](users.md), con ruolo `consumer`). È l'anagrafica **interna allo stabilimento** dei
> nominativi che compaiono in una prenotazione o in una transazione di cassa — creata automaticamente dal
> backend ogni volta che compare un nome/contatto non ancora noto per quella property, indipendentemente dal
> fatto che quella persona abbia mai creato un account. Il collegamento opzionale con un vero account
> `users` esiste (`referenceId`, vedi sotto) e in produzione risulta valorizzato solo su una minoranza
> di righe.

---

## Campi

### id
- **Tipo**: `bigint`, `AUTO_INCREMENT`, `NOT NULL`
- **Vincoli**: chiave primaria. Il model Sequelize non lo dichiara esplicitamente (Sequelize lo aggiunge
  come PK implicita) — stessa situazione già vista per altre tabelle del progetto.
- **Relazioni**: referenziato **solo a livello applicativo** (nessuna FK dichiarata né in DB né come
  associazione Sequelize) da `bookings.customerId`, `service_reservation.customerId`,
  `card_transactions.user_id` (verificato per nome campo nei controller, non ancora analizzato in dettaglio
  — sarà oggetto di una scheda dedicata) e dal proprio campo `referenceId` verso `users.id` (vedi sotto).
- **Descrizione**: identificativo tecnico del cliente/contatto. **In produzione: 224.467 righe, `id`
  fino a 228.736** → è di gran lunga la tabella anagrafica più grande del sistema (per confronto:
  [`users`](users.md) ha ~6.680 righe). Popolata automaticamente ad ogni prenotazione/transazione: il
  volume cresce con le prenotazioni, non con gli stabilimenti.
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `228.736` — `AUTO_INCREMENT` quasi puro (~4.000 id mancanti su
  228k, ~2%).

### fullName
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: nome del cliente. Scritto sia dal form "Contatti" (`CustomerForm.js`, campo `name` nel
  form → `fullName` nel payload) sia, con valore diverso, dai tre punti di creazione automatica: dal
  `customerData.name`/`customerData.firstName` inviato dal frontend di prenotazione
  (`booking.controller.js:258`, `servicereservation.controller.js:167`), oppure **stringa vuota hardcoded**
  quando la riga nasce da una transazione con carta senza altri dati anagrafici
  (`cardTransaction.controller.js:39`, `fullName: ""`). **Confermato dai dati**: 8 righe su 100 hanno
  valore letterale `"Unknown"` — un placeholder usato in qualche punto del frontend/flusso di prenotazione
  quando il nome non è disponibile (non è uno stringa vuota, è la parola inglese "Unknown" scritta
  letteralmente, anche nella copia italiana del sito — non è stato individuato con certezza in questa
  analisi il punto esatto del frontend che genera questo valore specifico, ma è chiaramente un default
  applicativo, non un nome reale).
- **Nullabile**: Sì
- **Esempio di valori**: "CESARINI", "Mario Rossi" (query estesa su tutte le 224.467 righe di produzione, conferma la
  presenza di nomi reali insieme a placeholder come "Unknown")

### mobile
- **Tipo**: `varchar(20)`
- **Vincoli**: nessun `UNIQUE`, ma **indice** (`customers_mobile`, non univoco)
- **Relazioni**: —
- **Descrizione**: numero di cellulare, il campo insieme a `email` più usato come **chiave di
  corrispondenza** per capire se un cliente esiste già per quella property (`checkingData`/`whereClause` in
  `booking.controller.js`, `checkContactInProperty`, `isUserBlacklisted` — sempre `WHERE propertyId = ...
  AND (email = ... OR mobile = ...)` o combinazioni simili). **Dato di qualità bassa nel campione**: valori
  come `"99999999"` (9 occorrenze), `"3333333333"` (5), `"00000"`/`"0000000"`/`"000000"` (2+2+1) — numeri
  chiaramente segnaposto/di test inseriti da operatori di cassa quando il cliente non fornisce un vero
  numero, non numeri di telefono reali. Rilevante per la riscrittura: usare questi valori come chiave di
  deduplicazione/matching (come fa il codice legacy) rischia di **fondere clienti diversi** che hanno tutti
  inserito lo stesso numero segnaposto.
- **Nullabile**: Sì
- **Esempio di valori**: "3452315223", "3921609954" (query estesa su tutte le 224.467 righe di produzione)

### email
- **Tipo**: `varchar(50)`
- **Vincoli**: nessun `UNIQUE`, ma **indice** (`customers_email`, non univoco). **Nota**: lunghezza massima
  50 caratteri — più corta del campo `email` su `properties`/`users` (255) e potenzialmente insufficiente
  per indirizzi email lunghi legittimi (es. sottodomini aziendali lunghi combinati con nomi utente estesi):
  un vincolo di lunghezza probabilmente troppo stretto per un campo email, da non riprodurre nella
  riscrittura.
- **Relazioni**: —
- **Descrizione**: email di contatto, altro campo chiave di corrispondenza (vedi `mobile`). **Qualità dati
  osservata**: 66 righe su 100 hanno stringa vuota, 12 sono `NULL`, solo 22 hanno un indirizzo realistico —
  cioè **nell'88% dei casi il cliente non ha un'email utilizzabile**, coerente con un flusso dove la
  raccolta avviene spesso in spiaggia/a voce con solo il numero di telefono.
- **Nullabile**: Sì
- **Esempio di valori** (prod): "pradipsolace@gmail.com", "thesolacetesting@yopmail.com" — su tutte le
  224.467 righe **83.126 hanno `email` `NULL` (37%)**, più le stringhe vuote → oltre metà dei clienti
  non ha un'email utilizzabile. Il campo `mobile` è invece `NULL` solo sul 10% (22.644 righe): il
  telefono è il vero identificatore di contatto.

### address / city / cap
- **Tipo**: `varchar(255)` ciascuno
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: indirizzo di residenza/fatturazione, raccolti **solo** dal form completo "Contatti"
  (`CustomerForm.js`) — **mai** scritti dai tre punti di creazione automatica (booking/service
  reservation/card transaction), che non li includono nel payload. **Confermato dai dati**: valorizzati
  (anche solo come stringa vuota, cioè il form è stato aperto e salvato senza compilarli) su **solo 7 righe
  su 100** — le stesse 7 per tutti e tre i campi, verosimilmente gli unici clienti di questo campione
  inseriti/modificati esplicitamente tramite il form "Contatti" invece che generati automaticamente da una
  prenotazione.
- **Nullabile**: Sì
- **Esempio di valori** (prod): quando presenti: "via nazionale 207" / "montepaone" / "88060" — su
  tutte le 224.467 righe **`address` è `NULL` su 215.148 (96%)**, `city` su 215.207 (96%), `cap` su
  215.206 (96%): solo ~4% dei clienti passa dal form completo "Contatti". **Dati sporchi**: "umbrella
  testing", "testing cap", "testing caps".

### vat
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: partita IVA, per clienti business/fatturazione. Stesso pattern di `address`/`city`/`cap`:
  solo dal form "Contatti" completo, 7 righe valorizzate (tutte a stringa vuota nel campione — nessun
  cliente con una vera partita IVA inserita nei 100 record locali).
- **Nullabile**: Sì
- **Esempio di valori** (prod): `vat` è `NULL` su 216.013 righe (96%), e delle ~8.450 righe valorizzate
  **8.430 sono stringa vuota**; i pochi valori reali sono per lo più sporchi ("adfj", "vat  test",
  "test vat 2", "Iure culpa sit tota") con solo una manciata di P.IVA plausibili
  (`03676021003`, `12826361003`). Campo di fatto inutilizzato.

### fiscalCode
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: codice fiscale italiano. Nel form "Contatti" il campo si chiama `fiscale` a livello di
  stato React (`CustomerForm.js:288`, `setValue("fiscale", customer.fiscalCode || "")`) ma viene inviato al
  backend con la chiave corretta `fiscalCode` — solo una differenza di naming interno al componente, non un
  problema di trasmissione dati (verificato: il payload di submit usa `fiscalCode: data.fiscale`). Stesso
  pattern di popolamento di `vat` (solo dal form completo, 7 righe, sempre vuoto nel campione).
- **Nullabile**: Sì
- **Esempio di valori**: "rosh2000", "123456" (query estesa su tutte le 224.467 righe di produzione)

### otherNames
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: campo per nominativi aggiuntivi/alias associati al contatto (etichetta UI non
  verificata con certezza in questa analisi — il campo del form React si chiama `othername`, mappato su
  `otherNames`). Stesso pattern di popolamento degli altri campi "estesi": solo dal form Contatti, 7 righe,
  sempre vuoto nel campione.
- **Nullabile**: Sì
- **Esempio di valori**: "test", "Rohan" — il valore letterale "test" (query estesa su tutte le 224.467 righe di produzione)
  conferma la presenza di dati di prova anche su questo campo

### anyDiscounts
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: campo libero, nel form React chiamato `discount` (`CustomerForm.js`, `setValue("discount",
  customer.anyDiscounts || "")`) — testo libero per annotare eventuali sconti/condizioni particolari
  riservate a questo cliente (non è collegato al motore sconti/voucher strutturato di `discounts`/
  `price_lists` già visto altrove: è un campo nota testuale, non un riferimento a un record di sconto).
  Stesso pattern di popolamento degli altri campi estesi.
- **Nullabile**: Sì
- **Esempio di valori**: "no", "xyz", "20", "90" — valori liberi ed eterogenei (testo, numero, sì/no),
  coerenti con la natura di campo nota testuale libero già descritta sopra piuttosto che un vero flag
  strutturato

### note
- **Tipo**: `varchar(255)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: nota libera sul cliente. Stesso pattern di popolamento (solo form Contatti). **Unico fra
  i campi "estesi" ad avere un valore reale nel campione**: una riga con `"con Monaco 2"` (testo verosimilmente
  riferito a una richiesta/preferenza del cliente, non decifrabile ulteriormente dal solo dato).
- **Nullabile**: Sì
- **Esempio di valori**: "yes", "none", "2° fila penultimo ombrellone cabina n°5" — quest'ultimo (query
  estesa su tutte le 224.467 righe di produzione) conferma che il campo è realmente usato anche per annotazioni operative
  concrete, non solo per test

### propertyId
- **Tipo**: `int`
- **Vincoli**: **nessuna FK** verso `properties.id` (solo indice non univoco `customers_property_id`, e un
  indice composito `idx_propertyId_isDelete` + `idx_propertyId_fullName` per le query di lista/ricerca
  filtrate per stabilimento e ordinate per nome — coerenti con `customerlist`/`searchcustomerlist`)
- **Relazioni**: applicativa verso `properties.id`, non vincolata
- **Descrizione**: stabilimento a cui il contatto appartiene. **È lo scope di isolamento dati più
  importante di questa tabella**: ogni ricerca/creazione/controllo blacklist filtra sempre per questo
  campo — un cliente con lo stesso nome/email/mobile può quindi esistere come **righe distinte** per
  stabilimenti diversi (nessuna anagrafica clienti condivisa fra stabilimenti). In produzione
  `propertyId` va da `0` (valore anomalo — nessuna property) a `10856`; `NULL` su 1 sola riga.
- **Nullabile**: Sì
- **Esempio di valori** (prod): `15`, `1`, `32`, `85`.

### createdBy
- **Tipo**: `int`
- **Vincoli**: nessuno (nessuna FK, concettualmente riferisce `users.id` dell'operatore)
- **Relazioni**: applicativa verso `users.id`, non vincolata
- **Descrizione**: chi ha creato/modificato il contatto tramite il form "Contatti" — **scritto solo da
  `addcustomer`/`updatecustomer`** (`req.body.createdBy`, quindi dichiarato dal client, non dedotto dal
  token, stesso pattern già segnalato per `users.createdBy`), **mai** valorizzato dai tre flussi di
  creazione automatica (booking/service reservation/card transaction non lo includono nel payload di
  creazione). Coerente coi dati: solo 7 righe non-`NULL`, le stesse 7 dei campi "estesi" sopra.
- **Nullabile**: Sì
- **Esempio di valori** (prod): `76`, `2`, `85` — ma **`createdBy` è `NULL` su 215.181 righe (96%)**:
  la stragrande maggioranza dei clienti nasce dai flussi automatici (booking/service/card) che non lo
  valorizzano. Range dei valori presenti: `0` … `5.703`.

### createdAt / updatedAt
- **Tipo**: `datetime`, default applicativo `Sequelize.NOW` (non a livello DB — la colonna fisica ha
  `DEFAULT NULL`, il default è imposto solo se la scrittura passa dall'ORM Sequelize; le tre creazioni
  dirette via `Customers.create()` nei controller lo popolano comunque tramite l'ORM, quindi in pratica è
  sempre valorizzato — **100% non-`NULL`** su entrambi i campi nel campione)
- **Relazioni**: —
- **Descrizione**: standard.
- **Nullabile**: Sì (colonna fisica `DEFAULT NULL`; **in produzione `createdAt` è `NULL` su 1.367
  righe, `updatedAt` su 1.072** — ~0,5-1%, righe create fuori dall'ORM standard).
- **Esempio di valori** (prod): range `2021-03-29 06:41:43` … `2026-09-08 09:02:09` (tabella attiva
  fino all'export).

### isDelete
- **Tipo**: `tinyint(1)` (booleano), default `0`
- **Vincoli**: nessuno, indice dedicato (`customers_is_delete`, anche in combinazione con `propertyId`)
- **Relazioni**: —
- **Descrizione**: soft delete, impostato da `deletecustomer` — **da notare**: la funzione accetta
  `req.body.isDelete` come valore da scrivere (booleano scelto dal client), quindi l'endpoint può anche
  essere usato per **ripristinare** un contatto cancellato (`isDelete: false`), non solo per cancellarlo;
  il nome dell'azione (`deletecustomer`) è quindi più generico di quanto suggerisca. **Confermato dai
  dati**: la funzione consente anche il ripristino (`isDelete: false`).
- **Nullabile**: Sì
- **Esempio di valori** (prod): `0` su 219.532 righe, `1` su **4.935** (~2,2% dei clienti soft-deleted).

### referenceId
- **Tipo**: `int`, default `0`
- **Vincoli**: nessuno, indice dedicato (`customers_reference_id`)
- **Relazioni**: applicativa verso `users.id`, non vincolata da FK
- **Descrizione**: collegamento **opzionale** a un vero account `users` con ruolo `consumer`. Il valore di
  default `0` (non `NULL`) significa esplicitamente "nessun account collegato" — verificato in
  `booking.controller.js:119` (`var referenceId = 0`) e ripreso identico in
  `servicereservation.controller.js`: quando un cliente viene aggiunto a una prenotazione, il backend cerca
  prima un `users` esistente con la stessa email/mobile (`checkUser = await User.findOne({ where:
  checkingData })`); se lo trova, `referenceId = checkUser.id` **e** email/mobile usati per il resto del
  flusso vengono sovrascritti con quelli salvati sull'account utente (non quelli digitati nel form di
  prenotazione) — cioè, se un cliente già registrato prenota (anche a nome di qualcun altro, es. un
  operatore che digita l'email di un cliente esistente), il sistema **ignora silenziosamente** i dati di
  contatto forniti nel form a favore di quelli già in `users`. **Confermato dai dati**: **tutte le 100
  righe del campione hanno `referenceId = 0`** — nessun cliente campionato risulta collegato a un account
  `users` registrato, nonostante il meccanismo di collegamento esista e sia attivamente eseguito ad ogni
  creazione.
- **Nullabile**: Sì
- **Esempio di valori** (prod): il valore resta `0` sulla stragrande maggioranza delle righe; i valori
  non-zero reali (`123`, `105`, `110`, …) arrivano fino a `6.068` — cioè il collegamento a un account
  `users` esistente **avviene** ma solo per una piccola frazione dei 224k clienti (coerente col fatto
  che [`users`](users.md) ha ~6.680 righe totali e la maggior parte dei clienti in spiaggia non ha un
  account).

### otherMobileNumber
- **Tipo**: `varchar(20)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: secondo numero di cellulare/contatto alternativo, etichettato **"Other Mobile Number"**
  nel form Contatti (`CustomerForm.js:744`) — campo distinto sia da `mobile` (primario, usato per il
  matching/deduplica) sia da `phone` (vedi sotto). Popolato solo dal form completo, 7 righe nel campione
  (sempre stringa vuota).
- **Nullabile**: Sì
- **Esempio di valori**: per lo più `NULL`/vuoto anche sull'intera tabella (224.467 righe di produzione) — nessun valore
  significativo emerso dalla query estesa, coerente col fatto che è un campo "esteso" popolato solo dal form
  completo

### dateOfbirth
- **Tipo**: `datetime`
- **Vincoli**: nessuno (da notare l'inconsistenza di naming: `dateOfbirth`, non `dateOfBirth` — "b"
  minuscola, verificata identica sia nel model sia nello schema DB sia nel payload frontend)
- **Relazioni**: —
- **Descrizione**: data di nascita del cliente, raccolta solo dal form Contatti (default nel form:
  `new Date()`, cioè la data odierna, se non specificata — cosa che spiega perché uno dei valori campionati
  sia una data molto recente pur trattandosi presumibilmente di un campo "data di nascita": l'operatore ha
  salvato il form senza modificare il valore precompilato). **Osservazione sui dati**: una riga ha
  `1970-01-01 00:00:00` — il classico valore "epoch zero", tipico di una data non impostata convertita
  da un valore nullo/zero piuttosto che una vera data di nascita nel 1970.
- **Nullabile**: Sì (`NULL` su 221.885 righe, **99%**). Delle ~2.580 righe valorizzate, molte sono la
  data odierna (precompilata dal form), il valore epoch `1970-01-01`, o date future — solo una minoranza
  è una vera data di nascita (il minimo osservato è `1951-07-21`).
- **Esempio di valori** (prod): `1970-01-01 00:00:00`, `2026-03-30 08:20:09` (data odierna, non di
  nascita), raramente date reali.

### phone
- **Tipo**: `varchar(20)`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: terzo campo telefonico, etichettato semplicemente **"Phone"** nel form Contatti (riga
  753) — a differenza di `mobile`/`otherMobileNumber` non è mai usato come chiave di ricerca/matching in
  nessun controller verificato (`isUserBlacklisted`, `checkContactInProperty`, la logica di
  find-or-create in booking/service reservation usano sempre e solo `email`/`mobile`, mai `phone`). Sui 19
  valori non-`NULL` nel campione, 5 coincidono col valore segnaposto `"3333333333"` già visto su `mobile`.
- **Nullabile**: Sì (`NULL` su 159.849 righe, 71%). `otherMobileNumber` è `NULL` sul 99% (222.163).
- **Esempio di valori** (prod): `""`, `"06789748"`. **Nota
  ridondanza**: la tabella ha tre campi telefonici molto simili — `mobile`, `phone`, `otherMobileNumber` —
  con solo `mobile` realmente usato come chiave di matching; da consolidare in fase di riscrittura (es. un
  unico campo telefono primario più un array di contatti secondari, invece di tre colonne piatte separate).

### isBlackList
- **Tipo**: `tinyint`, default `0` (non dichiarato booleano a livello DB, ma trattato come 0/1 ovunque nel
  codice)
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: flag di blacklist **per singolo stabilimento** (essendo la riga `customers` scoped per
  `propertyId`, un cliente blacklistato in uno stabilimento non lo è automaticamente in un altro — sono
  righe distinte). Verificato come **realmente applicato in due punti**:
  1. `booking.controller.js:185-219` — al momento della creazione di una nuova prenotazione, se esiste un
     contatto con la stessa email/mobile per quella property con `isBlackList=1`, la prenotazione viene
     **rifiutata** (a meno che il chiamante non passi esplicitamente
     `allowBookingForBlacklistedCustomer: true` nel payload — un override lato client di un controllo
     pensato per impedire la prenotazione, verificabile solo lato server ma **disattivabile dal client
     stesso** che lo invoca: un potenziale bypass se il frontend che dovrebbe essere l'unico a settare quel
     flag non fosse quello legittimo).
  2. `auth.controller.js:signin`/`sendToken` — al login, l'`isBlackList` del cliente (cercato per email) è
     restituito nella risposta come informazione per l'operatore (`isBlackList` nella response JSON), ma
     **non blocca il login stesso** (solo la creazione di una nuova prenotazione).
  Impostabile manualmente dall'operatore tramite lo switch `switchValue` nel form Contatti
  (`CustomerForm.js:298`).
- **Nullabile**: Sì
- **Esempio di valori** (prod): `0` su 224.427 righe, `1` su **40** (~0,018%) — il flag blacklist è
  usato pochissimo.

### is_guest
- **Tipo**: `tinyint`, default `0`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: distingue il cliente **intestatario** della prenotazione (`is_guest=0`) da un
  **ospite aggiuntivo** nominato nella stessa prenotazione (`is_guest=1`) — verificato in
  `booking.controller.js:270-274` e nell'equivalente in `servicereservation.controller.js`: il codice itera
  su un array di persone (`req.body.data`), crea/trova una riga `customers` per ciascuna, e usa
  `is_guest` per decidere se quell'id va nel campo singolo `customerId` della prenotazione (l'intestatario)
  o in un array `guestIds` (gli altri nominativi — es. i componenti di un gruppo/famiglia che prenota
  insieme). Consumato da un endpoint dedicato, `guestlist` (`customers.controller.js:460-501`), che filtra
  `is_guest=1` e rinomina i campi nella risposta (`fullName`→`guestFirstName`, `email`→`guestEmail`,
  `mobile`→`guestMobileNumber`) per un elenco "ospiti" separato da quello dei clienti intestatari.
- **Nullabile**: Sì
- **Esempio di valori** (prod): `0` su 224.335 righe, `1` su **132** (~0,06%) — la funzionalità "ospiti
  aggiuntivi" è usata pochissimo. **Nota
  naming**: `is_guest` è l'unico campo snake_case della tabella, tutti gli altri sono camelCase
  (`fullName`, `createdBy`, `isDelete`, ecc.) — incoerenza di naming da segnalare, probabile aggiunta
  successiva scritta da uno sviluppatore diverso o con convenzioni diverse.

### countryCode
- **Tipo**: `varchar(20)`, default `'+39'`
- **Vincoli**: nessuno
- **Relazioni**: —
- **Descrizione**: prefisso telefonico internazionale — a differenza dell'analogo campo su `properties`
  (`mobileCountryCode`, verificato morto) e in linea con `users.countryCode` (verificato vivo), qui il
  campo **è popolato correttamente**: passato esplicitamente in tutti e tre i flussi di creazione
  automatica (`customerData.countryCode` dal payload di prenotazione) oltre che dal form Contatti (default
  `"+39"`, coerente col default DB). **Unica eccezione verificata**: `cardTransaction.controller.js:45`
  crea il cliente con `countryCode: ""` **hardcoded** (stringa vuota, non il default `"+39"` né un valore
  dal body) — quindi i clienti creati esclusivamente tramite una transazione con carta prepagata (senza
  passare da una prenotazione) hanno sistematicamente il prefisso vuoto anche se il numero di
  cellulare/telefono è presente. **Confermato dai dati**: 98/100 righe `+39`, 1 `+41`, 1 `+1` — nessuna
  riga vuota nel campione (il flusso `cardTransaction` con stringa vuota non risulta rappresentato qui).
  **Candidato a enumerazione**: vocabolario di prefissi telefonici internazionali, in linea di principio
  chiuso e standardizzato (ISO/E.164), ma potenzialmente destinato a crescere con nuovi paesi di
  provenienza dei clienti — meglio una tabella di lookup con FK (eventualmente condivisa con
  `country`/`users.countryCode`) piuttosto che un `varchar` libero, per validare il formato in input ed
  evitare valori come la stringa vuota hardcoded vista sopra.
- **Nullabile**: Sì
- **Esempio di valori**: "+39" (default, quasi sempre presente), "+41", "+1" — sull'intera tabella (224.467
  righe) il valore resta quasi sempre "+39", coerente col campione ridotto

---

## Tabelle collegate

Nessuna relazione è vincolata da una FK reale (coerente con il resto del progetto legacy). Le relazioni
applicative verificate nel codice sono:

| Tabella | Colonna | Verificata in |
|---|---|---|
| `properties` | `customers.propertyId` | quasi tutte le query di questo controller (scoping) |
| `users` | `customers.referenceId` | `booking.controller.js`, `servicereservation.controller.js` (collegamento opzionale ad account registrato) |
| `users` | `customers.createdBy` | `customers.controller.js:addcustomer/updatecustomer` (operatore che ha creato/modificato il contatto) |
| `bookings` | `bookings.customerId` → `customers.id` | `customers.controller.js:updatecustomer` (propagazione dati, vedi sotto), `booking.controller.js` |
| `service_reservation` | `service_reservation.customerId` → `customers.id` | `customers.controller.js:updatecustomer`, `servicereservation.controller.js` |

### Duplicazione dei dati cliente nelle prenotazioni (JSON denormalizzato)

Punto rilevante verificato in `customers.controller.js:updatecustomer` (righe 86-195): quando un operatore
modifica un contatto dal form "Contatti", il backend **non si limita ad aggiornare la riga `customers`** —
propaga esplicitamente `fullName`/`mobile`/`email`/`countryCode` anche dentro:
- la colonna JSON `bookings.booking_consumer`, per **tutte** le prenotazioni con quel `customerId`;
- la colonna JSON `service_reservation.customerData`, per **tutte** le prenotazioni di servizio con quel
  `customerId`.

Cioè i dati del cliente sono salvati **due volte**: una volta in modo relazionale (`customers`) e una
volta duplicati dentro il JSON di ogni singola prenotazione — coerente con la criticità strutturale del
modello dati JSON-centrico già ampiamente documentata in
[01-criticita-e-indicazioni-backend.md](../../progettazione/01-criticita-e-indicazioni-backend.md). Da notare che
questo endpoint è l'**unico** punto che tenta di tenere sincronizzate le due copie: qualunque altra
scrittura di `customers` (es. le tre creazioni automatiche da booking/service reservation/card transaction)
non ha bisogno di sincronizzare nulla perché scrive `customers` e il JSON della prenotazione nello stesso
momento, ma se una riga `customers` venisse modificata in un modo che bypassa `updatecustomer` (accesso
diretto al DB, script, ecc.) le prenotazioni passate resterebbero con i dati vecchi.

---

## Osservazioni per la riscrittura

- **Deduplica clienti basata su email/mobile senza validazione di qualità**: il codice usa `email`/`mobile`
  come chiave di matching per capire se un cliente esiste già, ma non c'è alcuna validazione che impedisca
  valori segnaposto (`"99999999"`, `"3333333333"`, `"00000"`) — con l'effetto verificabile che clienti
  diversi che inseriscono lo stesso numero fittizio finiscono fusi nella stessa riga `customers` (stesso
  `id`, quindi stesso storico prenotazioni/blacklist). Nella riscrittura va introdotta una validazione reale
  del formato telefono/email prima di usarli come chiave di matching, o un meccanismo di matching più
  robusto che non fonda automaticamente record con dati palesemente non identificativi.
- **`email` limitato a 50 caratteri**: vincolo di lunghezza probabilmente troppo stretto, da rivedere.
- **Bypass lato client del controllo blacklist**: `allowBookingForBlacklistedCustomer` è un flag inviato dal
  client che disattiva un controllo pensato per essere una garanzia lato server — nella riscrittura
  l'override deve essere una decisione esplicita e autorizzata (es. richiede un ruolo/permesso specifico
  verificato server-side), non un semplice booleano nel payload della richiesta.
- **`referenceId` come legame opzionale con `users`**: nella riscrittura va valutato se mantenere due
  entità separate (`Customer` interno allo stabilimento vs `User` con login) collegate per id, oppure se
  unificarle — la separazione attuale sembra motivata dal fatto che la maggior parte dei clienti non ha mai
  un account (in produzione `users` ha ~6.680 righe contro 224.467 `customers`), ma il meccanismo di
  collegamento automatico silenzioso (che
  sovrascrive i dati inseriti nel form con quelli dell'account trovato) va reso esplicito e visibile
  all'operatore, non implicito.
- **Denormalizzazione JSON nelle prenotazioni**: la copia dei dati cliente dentro `booking_consumer`/
  `customerData` (colonne JSON) va eliminata nella riscrittura a favore di una vera relazione FK verso
  `customers` — coerente con l'indicazione generale già data per l'intero progetto.
- **Campi "estesi" (indirizzo, P.IVA, codice fiscale, ecc.) usati solo da ~4% dei clienti in
  produzione** (`address`/`city`/`cap`/`vat`/`fiscalCode`/`otherNames`/`note` sono `NULL` sul 96%):
  nella riscrittura vanno resi opzionali/collassabili in UI (dati di fatturazione avanzati), dato che la
  maggioranza dei clienti reali ha solo nome + un contatto minimo (spesso solo il telefono).
