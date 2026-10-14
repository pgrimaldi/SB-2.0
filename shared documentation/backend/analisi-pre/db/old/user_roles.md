# Tabella `user_roles`

> Fonti usate per questa scheda: schema reale della tabella sul DB locale (`local_sb`, MySQL 9.7.1,
> `SHOW CREATE TABLE`/`information_schema`, dati reali delle 100 righe presenti), il model Sequelize
> [`back-pre/app/models/userRoles.model.js`](../../back-pre/app/models/userRoles.model.js), le associazioni
> dichiarate in [`back-pre/app/models/index.js`](../../back-pre/app/models/index.js) (righe 291-301), e
> tutti i punti del backend che leggono o scrivono questa tabella (`auth.controller.js`,
> `user.controller.js`, `booking.controller.js`, `helpers/cron.helper.js`, `helpers/property.helper.js`).
> Nessun frontend interagisce con questa tabella direttamente: è una tabella di solo backend, i quattro
> frontend vedono sempre e solo il campo `roles`/`role` già risolto nella risposta delle API (vedi
> [`users.md`](users.md) e [`roles.md`](roles.md)).

---

## Cos'è

Tabella di giunzione N:N fra `users` e `roles`, creata dall'associazione Sequelize
`role.belongsToMany(user, { through: "user_roles", foreignKey: "roleId", otherKey: "userId" })` /
`user.belongsToMany(role, { through: "user_roles", foreignKey: "userId", otherKey: "roleId" })`
(`models/index.js:291-300`). A livello di **modello dati** un utente potrebbe avere più ruoli
contemporaneamente; a livello di **codice applicativo verificato**, però, un utente ha sempre **al massimo
un ruolo**, per tre motivi concordanti trovati nel codice:

1. Ogni punto che assegna ruoli lo fa con `user.setRoles(roles)` dove `roles` è quasi sempre un array di un
   solo elemento (l'unica eccezione teorica è se il chiamante invia più nomi in `req.body.roles`, ma nessun
   frontend analizzato lo fa mai — vedi sotto).
2. In più punti il codice legge esplicitamente **solo il primo ruolo** trovato, assumendo che sia l'unico:
   `user.user_roles[0].roleId` (`cron.helper.js:65`, `property.helper.js:235`); il ciclo
   `for (roles) authorities = roles[i].name` in `auth.controller.js` (righe 339, 536, 618, 933) itera su
   tutti i ruoli ma **sovrascrive** la variabile ad ogni iterazione, quindi anche con più ruoli il client
   riceverebbe comunque un solo nome (l'ultimo trovato).
3. **Verificato sui dati reali**: nel DB locale, su 55 combinazioni `(userId, roleId)` che puntano a un
   utente realmente presente nell'estratto `users`, **nessun `userId` compare più di una volta** (query
   `GROUP BY userId HAVING COUNT(*) > 1` → zero risultati).

## Campi

### roleId
- **Tipo**: `int`, `NOT NULL`, parte della chiave primaria composita `(roleId, userId)`
- **Vincoli**: **nessun vincolo FK verso `roles.id`** a livello DB (verificato in `SHOW CREATE TABLE
  user_roles`: solo `PRIMARY KEY (roleId, userId)` e due `KEY` non univoci, nessuna `CONSTRAINT`). Il
  legame con `roles` è imposto solo dall'associazione Sequelize.
- **Relazioni**: applicativa verso `roles.id` (nessuna FK reale)
- **Descrizione**: riferimento al ruolo assegnato. Popolato tramite `Role.findAll({ where: { name: {
  [Op.or]: req.body.roles } } })` seguito da `user.setRoles(roles)` — cioè il client invia **nomi** di ruolo
  (stringhe), il backend li risolve in `id` internamente; nessun frontend analizzato invia mai un `roleId`
  numerico direttamente. **Candidato a enumerazione**: il valore chiuso a cui punta (`roles.name`) cresce nel
  tempo (nuovi ruoli aggiunti anche di recente — `marketing` nel 2026, un presunto `ofsale` nella verifica
  più aggiornata del DB, vedi sotto) — è quindi il caso da manuale a favore di una **tabella lookup con FK
  reale**
  (`roleId → roles.id`, oggi assente) invece che un `ENUM` DB statico, proprio perché il vocabolario non è
  fisso nel tempo.
- **Nullabile**: No
- **Esempio di valori**: distribuzione osservata su una verifica più recente del DB di sviluppo (837 righe
  totali, 14 valori distinti): `3` (423 righe, maggioranza assoluta — coerente con `roles.id = 3` =
  `consumer`, il ruolo di gran lunga più diffuso, vedi [`roles.md`](roles.md#name)), `2` (119), `5` (60), `1`
  (52), `4` (39), `11` (35), `6` (25), `7` (24), `9` (23), `10` (17), `12` (13), `15` (5), `8` (1), `14` (1).
  **Nota di incoerenza tra le fonti**: i valori `14` e `15` non trovano corrispondenza in `roles.md`, che
  documenta solo gli `id` da `1` a `13` — coerente con la stessa verifica più recente riportata in
  [`roles.md`](roles.md#name) secondo cui la tabella `roles` ha oggi **15 righe** (non più 13), con un
  valore `"ofsale"` duplicato su due `id`: è verosimile che `roleId = 14`/`15` in questa tabella si
  riferiscano proprio a quelle due righe duplicate, un altro segnale concreto di dato di sviluppo non
  ripulito, da verificare puntualmente prima della riscrittura.

### userId
- **Tipo**: `int` (dichiarato `Sequelize.INTEGER` nel model; a livello fisico DB è `bigint`, coerente col
  tipo di `users.id`), `NOT NULL`, parte della chiave primaria composita
- **Vincoli**: **nessun vincolo FK verso `users.id`** a livello DB (stessa verifica di cui sopra) —
  strutturalmente possibile avere righe `user_roles` che puntano a un `userId` inesistente, e **infatti
  accade nella copia dati locale** (vedi sotto).
- **Relazioni**: applicativa verso `users.id` (nessuna FK reale)
- **Descrizione**: riferimento all'utente. Essendo la tabella un'associazione Sequelize standard, non
  esiste un controller dedicato a `user_roles`: viene sempre scritta/letta indirettamente tramite i metodi
  generati dall'associazione (`user.setRoles()`, `user.getRoles()`) o tramite query SQL dirette/JOIN nei
  vari controller.
- **Nullabile**: No
- **Esempio di valori**: `151`, `90`, `302` (id di `users`, coerente col fatto che a livello fisico la
  colonna è `bigint` come `users.id`)

### createdAt / updatedAt
- **Tipo**: `datetime`, `NOT NULL`, nessun default a livello DB
- **Relazioni**: —
- **Descrizione**: timestamp di creazione/ultima modifica della riga di associazione, gestiti
  automaticamente da Sequelize quando `setRoles()` inserisce la riga. **Verificato**: `setRoles()` è
  chiamato **solo** nei tre flussi di registrazione in `auth.controller.js` (`signup`, `usersignup`,
  `social`) — nessun endpoint di modifica utente (`updateuser`, `updatesuperadmin`, `edituser`) tocca mai
  `user_roles`. Il ruolo di un utente, una volta assegnato alla creazione, **non risulta modificabile da
  nessuna API** trovata in questo repository: cambiare il ruolo di uno staff esistente richiederebbe
  un'operazione diretta sul database, non un'azione disponibile nei pannelli analizzati.
- **Nullabile**: No
- **Esempio di valori**: `2024-06-01 08:12:44`, `2025-09-17 14:03:10`

---

## Osservazione sui dati: righe orfane nella copia locale

Nel campione locale (100 righe in `user_roles`, 100 righe in `users`) **45 righe di `user_roles` puntano a
un `userId` che non esiste in questo estratto di `users`** (verificato con `LEFT JOIN`: 55 `userId` distinti
trovano corrispondenza, 45 no). Le 55 combinazioni valide si distribuiscono così: `superadmin` → 1,
`consumer` → 54; nessuna delle altre 11 righe che il conteggio grezzo su tutta la tabella (senza filtrare
per esistenza dell'utente) mostrerebbe come `waiter`/`advancedlifeguard`/ecc. corrisponde a un utente
presente in questo estratto.

Questo è coerente con la natura di copia **parziale** del database locale (`LOCAL_DB=local_sb`, dichiarata
tale nelle istruzioni del progetto): `users` sembra essere stato campionato con un criterio diverso (o più
restrittivo) rispetto a `user_roles`, lasciando riferimenti "orfani". **Non è quindi la prova di un bug di
integrità in produzione**, ma la mancanza di un vincolo FK reale (verificata sopra, indipendentemente dal
campione) conferma che una situazione simile è strutturalmente possibile anche lì, se mai un utente venisse
cancellato fisicamente senza rimuovere la sua riga `user_roles` (osservazione teorica, nessuna evidenza
diretta di cancellazioni fisiche di utenti nel codice — `deleteuser`/`deleteAccount` fanno sempre soft
delete su `users.userStatus`, mai una `DELETE` reale, quindi in condizioni normali questo scenario specifico
non dovrebbe verificarsi tramite l'applicazione).

---

## Osservazioni per la riscrittura

- **Il modello "N:N con più ruoli per utente" non è mai usato nella pratica**: tutto il codice verificato
  (backend e — indirettamente — tutti i punti che consumano `roles` lato frontend) assume un solo ruolo per
  utente. Nella riscrittura vale la pena valutare se modellare esplicitamente una relazione **1:N**
  (`User.RoleId` singolo, non una tabella di giunzione) — più semplice e che rispecchia il comportamento
  reale — oppure mantenere N:N solo se il committente conferma un'intenzione futura di ruoli multipli non
  ancora sfruttata.
- **FK reali obbligatorie**: sia `roleId → roles.id` sia `userId → users.id` vanno vincolate a livello DB
  nel nuovo schema (oggi assenti), eliminando strutturalmente la possibilità di righe orfane.
- **Ruolo immutabile dopo la creazione**: se questo è un limite involontario del legacy (nessun endpoint di
  "cambia ruolo"), la riscrittura è l'occasione per aggiungere un'operazione esplicita e tracciata di
  cambio ruolo, invece di lasciarlo possibile solo con accesso diretto al database.
- **Chiave primaria composita senza colonna tecnica**: coerente con l'uso previsto (tabella di giunzione
  pura), da mantenere come indice univoco composito nel nuovo schema anche se si passa a una relazione 1:N
  con FK singola su `users`.
