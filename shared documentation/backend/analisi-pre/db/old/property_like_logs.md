# Tabella `property_like_logs`

> Fonti: schema e dati reali (100 righe, `createdAt` fino a 2026-07), model
> [`back-pre/app/models/propertyLikeLogs.model.js`](../../back-pre/app/models/propertyLikeLogs.model.js)
> (`sequelize.define("property_like_logs", ...)` → ORM `db.propertyLikeLog`), controller
> [`propertylikelogs.controller.js`](../../back-pre/app/controllers/propertylikelogs.controller.js)
> (`likeLogDetails`, letto per intero), lettura in
> [`user.controller.js:811` `myfavourite`](../../back-pre/app/controllers/user.controller.js) (query
> grezza con `JOIN property_like_logs`), in
> [`property.controller.js:468` `getPropertyLikes`](../../back-pre/app/controllers/property.controller.js)
> (`LikeLogs.findAll()`) e in
> [`sysadmin.controller.js:206`](../../back-pre/app/controllers/sysadmin.controller.js)
> (`PropertyLikeLog.findOne` per calcolare `isLike` in scheda stabilimento). Route in
> [`app/routes/user.routes.js`](../../back-pre/app/routes/user.routes.js):
> `POST /api/sb/property/likeLogs` (929, `authJwt.optionalAuth`), `POST /api/sb/myfavourite` (1101).
> Frontend consumer: `front-customer-pre` (`PropertyCard.js`, pagina `/my-favourite`),
> `front-app-pre` (`Favorites.screen.js`, `SearchResult.screen.js`, …) — `Api.likeProperty`.
>
> **Nota di contesto** — tabella **non censita** prima di questa scheda. È il registro dei **"mi piace" /
> preferiti** degli stabilimenti da parte degli **utenti consumer** (app cliente). Ogni "mi piace"
> aggiunge una riga e incrementa il contatore denormalizzato [`bathouses.likeCount`](bathouses.md);
> il "non mi piace" **cancella** la riga (`destroy`) e decrementa il contatore.
>
> **Dominio di destinazione proposto**: **C5 — Identity**, sottoarea `customer` (relazione utente
> consumer ↔ stabilimento). In alternativa un dominio "Vetrina / Ricerca pubblica" insieme a
> [`property_search_regions`](property_search_regions.md). Il contatore aggregato
> [`bathouses.likeCount`](bathouses.md) resta in **C2**.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### userId
- **Tipo**: `int` (model `Sequelize.INTEGER`; indice non-unico dichiarato)
- **Nullabile**: Sì (nessuna FK)
- **Relazioni**: `users.id` dell'utente consumer che ha messo "mi piace". Nella `myfavourite` è
  filtrato per stringa interpolata (`likes.userId='${req.body.userid}'`).
- **Esempio di valori**: `5021`, `2407`, `3794`
- **Deprecato**: No

### bathHouseId
- **Tipo**: `int`, `allowNull: true` (indice non-unico dichiarato)
- **Nullabile**: Sì
- **Relazioni**: [`bathouses.id`](bathouses.md) (dati pubblici dello stabilimento). Join in
  `myfavourite`: `JOIN bathouses ON bathouses.id = likes.bathHouseId`. **Non** punta a `properties.id`
  malgrado il nome della tabella.
- **Esempio di valori**: `10428`, `10414`, `10391`
- **Deprecato**: No

### likedStatus
- **Tipo**: `tinyint(1)` (model `Sequelize.BOOLEAN`)
- **Nullabile**: Sì
- **Descrizione**: impostato a `1` alla creazione della riga (`likedStatus: 1`). **Nei dati è sempre
  `1`**: il "non mi piace" non porta il campo a `0`, cancella la riga. È quindi di fatto una **colonna
  morta** (l'esistenza della riga = "piace"). Restituito comunque come `likedStatus` da `myfavourite`.
- **Esempio di valori**: `1`
- **Candidato a enumerazione**: no — ridondante; eliminare (la presenza della riga è già lo stato).
- **Deprecato**: **Sì** (valore mai diverso da `1`)

### createdAt / updatedAt
- **Tipo**: `datetime` — presenti e **valorizzati** nei dati (timestamp Sequelize di default).
- **Nullabile**: Sì
- **Descrizione**: il model tenta di disattivarli (`{ ..., timestamps: false }`) ma quell'oggetto è
  passato come **4° argomento** di `sequelize.define` (che ne accetta 3) → **ignorato**: i timestamp
  restano attivi. Le colonne `created_at`/`updated_at` commentate nel model sono un residuo.
- **Esempio di valori**: `2026-07-01 20:51:02`

---

## Osservazioni verificate nel codice (criticità)

- **"Non mi piace" rotto da `front-customer-pre`**: `likeLogDetails` inizia con
  `if (!req.body.islike) { return 400 "islike is empty!" }`. `PropertyCard.js` invia
  `islike: status` **booleano**: quando `status === false` la richiesta viene rifiutata con 400 e la
  riga non viene mai rimossa. Da `front-app-pre` funziona per caso perché invia le stringhe `'1'`/`'0'`
  (entrambe truthy) e poi `req.body.islike == true` distingue i due casi (`"1" == true` → vero,
  `"0" == true` → falso).
- **Nessun vincolo di unicità `(userId, bathHouseId)`**: ogni "mi piace" fa un `INSERT` + un
  `bathouses.likeCount + 1` non transazionale → cliccare due volte crea righe duplicate e gonfia il
  contatore; `myfavourite` (che fa `JOIN`) restituirebbe lo stesso stabilimento più volte.
- **Contatore denormalizzato soggetto a drift**: `bathouses.likeCount` è mantenuto a parte con
  `Sequelize.literal('likeCount + 1')` / `- 1`, senza ricalcolo da `COUNT(property_like_logs)`; gli
  `.catch({})` (oggetto vuoto, non funzione) rendono silenziosi gli errori dell'update del contatore.
- **`destroy` come "unlike"**: hard delete → nessuno storico di quando un utente ha tolto il preferito;
  `likedStatus` non viene mai usato per un soft-toggle.
- **SQL injection in `myfavourite`**: `WHERE ... likes.userId='${req.body.userid}'` interpolato
  direttamente.
- **Nessuna FK** su `userId` / `bathHouseId`: righe orfane possibili (utente o bathhouse cancellati).
- **Endpoint `optionalAuth`**: `POST /api/sb/property/likeLogs` accetta `userId` dal body senza
  verificare che coincida con l'utente autenticato → un client può mettere/togliere "mi piace" a nome
  di altri e alterare il `likeCount`.
- **Import inutilizzati**: `db.propertyLikeLog` è importato anche in `property.controller.js` e
  `sysadmin.controller.js`; l'uso reale è minimo (`findAll()` senza filtri in `getPropertyLikes`,
  `findOne` in scheda stabilimento).
