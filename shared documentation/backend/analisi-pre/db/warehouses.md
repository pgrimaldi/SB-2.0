# Tabella `warehouses`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **172 righe**, query
> completa; una riga per `propertyId` → ~172 stabilimenti hanno configurato il magazzino, numero
> allineato a [`configurations.md`](configurations.md)/[`sectors_rows.md`](sectors_rows.md)).
> `createdAt` dal 2021-03 al 2026-08. Model
> [`back-pre/app/models/warehouses.model.js`](../../../../../../back-pre/app/models/warehouses.model.js), controller
> [`warehouse.controller.js`](../../../../../../back-pre/app/controllers/warehouse.controller.js) (letto per intero,
> 157 righe), utility di mascheramento
> [`back-pre/app/utils/mask.js`](../../../../../../back-pre/app/utils/mask.js), routing in
> [`back-pre/app/routes/user.routes.js`](../../../../../../back-pre/app/routes/user.routes.js), frontend
> `front-pre/src/components/warehouse/WarehouseParent.js` (presente identico anche in
> `front-customer-pre/src/components/warehouse/WarehouseParent.js` — stessa duplicazione di editor
> gestionale già segnalata per `Price`, vedi [`price_lists.md`](price_lists.md)).
>
> **Nota di contesto**: come `sectors_rows`/`seasons_periods`/`discounts`/`services_types`, è **una riga
> per `propertyId`** (pattern find-one-poi-crea-o-aggiorna in `addwarehouse`) — rappresenta il "magazzino"
> di articoli fisici a corredo (ombrelloni, lettini, ecc.) di cui il gestore tiene traccia della quantità
> disponibile.

---

## Campi

### id
- **Tipo**: `bigint`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria (colonna fisica `bigint`, non `int`
  come nel model Sequelize — stessa discrepanza minore già osservata in altre tabelle di questo dominio,
  es. [`services_types.md`](services_types.md#id))
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `173` per 172 righe.

### items
- **Tipo**: dichiarato `Sequelize.JSON`
- **Descrizione**: array degli articoli di magazzino, struttura verificata sui dati reali —
  `{ name, it, quantity, threshold, isEnable, currentId }`:
  - **name / it**: nome dell'articolo in inglese/italiano.
  - **quantity**: quantità totale disponibile.
  - **threshold**: soglia minima (verosimilmente per notifiche di scorta bassa — nessun controller
    backend esaminato legge o confronta esplicitamente questo valore, l'unico consumo individuato è la
    visualizzazione/mascheramento in `viewwarehouse`).
  - **isEnable**: se l'articolo è attivo/visibile.
  - **currentId**: stessa convenzione client-side `Math.random()`-based già documentata in
    [`sectors_rows.md`](sectors_rows.md#sectors) — non un identificativo di business.

  **Dato osservato da segnalare**: nella verifica più recente sui dati reali, `quantity` e `threshold`
  sono salvate come **stringhe** dentro il JSON (es. `"quantity":"62"`, `"threshold":"3"`), non come numeri
  — tipizzazione debole coerente con quanto già osservato altrove nel dominio (campi numerici salvati come
  testo dentro JSON). Nella riscrittura, se questi valori diventano colonne relazionali (`quantity`,
  `threshold` su una tabella `warehouse_items`), vanno tipizzati come interi reali fin da subito, con
  eventuale validazione/migrazione dei valori esistenti.
  **Candidato a enumerazione**: `isEnable` è un intero 0/1 usato come booleano — da tipizzare come
  `BOOLEAN`/`BIT` reale nella tabella relazionale di destinazione, non serve un `ENUM`.
- **Nullabile**: No (colonna fisica `longtext NOT NULL`)
- **Esempio di valori**: `[{"name":"Tent","it":"Tenda","quantity":"62","threshold":"3","isEnable":1,"currentId":54.76}]`

### createdBy / propertyId
- **Tipo**: `int`
- **Vincoli**: nessuna FK
- **Descrizione**: come per le altre tabelle di questo dominio.
- **Nullabile**: No (verifica più recente su `information_schema`: colonna fisica `bigint NOT NULL`, non
  `int` come nel model, stessa discrepanza minore già segnalata per `id`)
- **Esempio di valori**: `createdBy`: `305`; `propertyId`: `122`

### createdAt / updatedAt
- **Tipo**: `datetime` — standard.
- **Nullabile**: No
- **Esempio di valori**: `2024-05-20 11:22:07`, `2025-08-03 16:05:51`

---

## Osservazioni verificate nel codice (criticità)

- **Mascheramento intenzionale delle quantità per richieste non autenticate**: `viewwarehouse`
  (righe 73-112) applica `maskDecimal` (in `utils/mask.js:39-55`, sostituisce **ogni cifra** con `*`,
  mascheramento totale non parziale) a `quantity`/`threshold` quando `req.user` non è valorizzato. La
  route `/api/sb/warehouse/view` usa `authJwt.optionalAuth` (riga 356 di `user.routes.js`, con
  `authJwt.checkRoleViewWareHouse` esplicitamente commentato alla riga precedente) — quindi il
  comportamento è: **utente autenticato con token valido → vede le quantità reali; richiesta anonima →
  vede quantità/soglie oscurate**. Comportamento intenzionale e coerente (le quantità di magazzino sono
  trattate come informazione sensibile non da esporre pubblicamente), riportato qui per chiarezza e non
  come criticità.
- **`addwarehouse` protetto da `authJwt.isSuperAdmin` a livello di route**
  (`user.routes.js:344-351`, richiede letteralmente il ruolo `"superadmin"`, verificato in
  `authJwt.js:86-102` come nell'analogo caso di `configure_images`), **ma l'azione che lo richiama lato
  frontend è `_addWarehouseData`**, usata da `WarehouseParent.js` — il componente "Magazzino" del
  gestionale, montato nell'area di gestione ordinaria dello stabilimento e presente identico in
  **entrambi** `front-pre` e `front-customer-pre`, non un'area riservata al superadmin. Stessa
  incoerenza concreta fra permesso dichiarato a livello di route e uso reale del componente già
  documentata per `activeconfimages` in
  [`configure_images.md`](configure_images.md#osservazioni-verificate-nel-codice-criticità) — se
  l'enforcement `isSuperAdmin` fosse davvero quello effettivo, un gestore proprietario non potrebbe
  salvare/modificare il proprio magazzino tramite questo endpoint. Da chiarire con il team prodotto prima
  della riscrittura quale sia il comportamento corretto voluto.
- **`bookingdetails` (righe 114-156) non invia mai una risposta HTTP nel percorso normale**: quando il
  magazzino esiste (`warehouses` trovato — il caso atteso), la funzione costruisce `jsonData` ma non lo
  usa mai, interroga `BookingItemsCount.findOne(...)` il cui callback ha **entrambi i rami vuoti**
  (`if (!bookingItemsDetails) {} else {}`) e la funzione termina **senza alcuna chiamata a
  `res.send`/`res.json`**. L'unico caso in cui viene inviata una risposta è quando il magazzino **non**
  esiste (`return res.send(...)` nel ramo `if (!warehouses)`). L'endpoint (`POST
  /api/sb/warehouse/bookingdetails`, live e routato, righe 912-920 di `user.routes.js`, anch'esso con
  `verifyToken`/`isSystemAdmin` commentati) va quindi quasi certamente in **timeout lato client** in tutti
  i casi in cui esiste già un magazzino configurato — verosimilmente una funzionalità lasciata a metà
  (integrazione con `BookingItemsCount` mai completata).
- **IDOR verificato**: `addwarehouse`/`viewwarehouse`/`bookingdetails` operano tutti su
  `req.body.propertyId` senza alcuna verifica contro l'utente autenticato — stesso pattern sistemico del
  dominio C2 (aggravato per `addwarehouse` dal fatto che, se il controllo `isSuperAdmin` risultasse in
  realtà bypassato o non applicato come da codice, chiunque possa autenticarsi come superadmin potrebbe
  sovrascrivere il magazzino di qualunque proprietà).
