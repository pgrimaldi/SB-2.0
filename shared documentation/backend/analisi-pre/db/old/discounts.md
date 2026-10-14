# Tabella `discounts`

> Fonti: schema e dati reali (50 righe), model
> [`back-pre/app/models/discounts.model.js`](../../back-pre/app/models/discounts.model.js), controller
> [`discounts.controller.js`](../../back-pre/app/controllers/discounts.controller.js) (letto per intero,
> 493 righe), funzione di matching condivisa `discountCodeSearch` in
> [`back-pre/app/global/globalConstants.js:240-`](../../back-pre/app/global/globalConstants.js), consumo
> del contatore d'uso in `booking.controller.js` (righe 976-1016) e `servicereservation.controller.js`
> (righe ~500-545), routing in
> [`back-pre/app/routes/user.routes.js`](../../back-pre/app/routes/user.routes.js), frontend
> `front-pre/src/components/discount/AddVoucher.js` (letto per intero il blocco di stato/submit),
> `EditVoucherView.js`, `UpdateDiscountStatus.js`, e i consumer in fase di prenotazione
> (`reserveArticle/Modal/Discount.js`, `serviceReservation/DiscountVouchers.js`).
>
> **Nota di contesto**: come `sectors_rows`/`seasons_periods`, è **una riga per `propertyId`** — i singoli
> voucher/sconti sono gli elementi dell'array `items`, non righe distinte (50 righe in tabella = 50
> stabilimenti che hanno configurato almeno un voucher, su un totale di proprietà molto più ampio).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### items
- **Tipo**: dichiarato `Sequelize.JSON`
- **Descrizione**: array dei voucher/sconti configurati per lo stabilimento. Struttura verificata
  incrociando i dati reali con `AddVoucher.js` (form di creazione) e `discountCodeSearch` (logica di
  validazione lato server):
  - **name**: Tipo `string` — usato **sia come nome visualizzato sia come codice sconto** che il cliente
    digita per riscattare il voucher (confrontato in `discountCodeSearch` con il parametro `code` inviato
    dal client tramite `checkdiscount`) — non esiste un campo "codice" separato dal nome.
  - **type**: `"percentage"` / `"fixed"` — tipo di sconto. **Candidato a enumerazione**: solo 2 valori
    stabili e mai variati nel campione osservato — ENUM a livello DB (o costante applicativa condivisa)
    sufficiente, non serve una tabella di lookup dato che non richiede traduzione né è previsto crescere.
  - **value**: valore numerico dello sconto (percentuale o importo fisso a seconda di `type`).
  - **voucherType**: `"all"` / `"custom"` — se `"all"`, il voucher è valido per qualunque prenotazione nel
    periodo indicato; se `"custom"`, si applicano condizioni aggiuntive (vedi sotto). **Correzione da dato
    reale DB dev**: sul campione locale precedente erano stati osservati solo questi 2 valori, ma
    un'interrogazione diretta sul DB dev (24 righe) ha rilevato anche un **terzo valore reale**,
    `"voucherType":"withoutCustom"`, non documentato finora — la scheda va quindi aggiornata: il campo è
    a **3 stati** (`all`/`custom`/`withoutCustom`), non 2; il significato esatto di `withoutCustom` rispetto
    a `custom` non è stato determinato in questa verifica e andrebbe chiarito nel codice di
    `discountCodeSearch`/`AddVoucher.js` prima della riscrittura. **Candidato a enumerazione**: insieme
    comunque chiuso e stabile (3 valori) — ENUM a livello DB sufficiente, salvo che si scelga di
    esternalizzare le etichette per l'UI, nel qual caso conviene una piccola tabella di lookup con FK.
  - **selectedRow** (denominato `selectedRowsData` nel form): array di nomi settore
    (`sectors_rows.sectors[].name`) a cui il voucher è limitato — di nuovo un riferimento per **stringa**,
    non per id, stesso pattern fragile già segnalato per `seasons_periods`/`price_lists`.
  - **checkBarService / checkNormalService / checkHourlyService / checkAdditionalServices**: `boolean` —
    per i voucher `"custom"`, limitano l'applicabilità alle sole categorie di servizio indicate (confrontate
    contro `additionalSetting.barServices`/`normalServices`/`hourlyServices`/`additionalServices` passati
    dal client al momento della verifica).
  - **checkManagementFlag / checkOnlineFlag**: `boolean` — per i voucher `"custom"`, limitano l'uso al
    canale di prenotazione (gestionale vs. sito online): se entrambi `true`, valido su entrambi i canali;
    altrimenti deve corrispondere esattamente al canale della richiesta corrente.
  - **maximunNoUse**: Tipo `number` — numero massimo di utilizzi consentiti per il voucher (validato come
    obbligatorio e `>0` lato form, `AddVoucher.js:441-442`).
  - **totalNoOfUsage**: Tipo `number` — contatore degli utilizzi già effettuati, confrontato con
    `maximunNoUse` in `checkdiscount` (`item.maximunNoUse - item.totalNoOfUsage > 0`) per stabilire se il
    voucher è ancora disponibile. **Incrementato non atomicamente e a partire da un valore fornito dal
    client** — vedi criticità sotto. **Conferma esplicita (dato reale DB dev)**: è un contatore
    **generale/aggregato per voucher**, non per singolo cliente — non esiste nella struttura alcun campo che
    leghi l'utilizzo a un `customerId` specifico; il contatore è condiviso e incrementato da chiunque riscatti
    quel codice sconto, indipendentemente da chi lo usa.
  - **voucherDateRange**: array di `{ startDate, endDate }` — uno o più intervalli di validità del voucher
    (il voucher può avere più finestre temporali, come i periodi stagionali).
  - **status**: `1` = attivo (gestito da `UpdateDiscountStatus.js`, che aggiorna l'intero array via
    `adddiscounts`/update — non esiste un endpoint dedicato a toggle del singolo voucher).
  - **currentId**: Tipo `number` — **stessa convenzione client-side già documentata** in
    [`sectors_rows.md`](sectors_rows.md#sectors) (`Math.random()`-based, chiave UI temporanea, non un
    identificativo di business).
- **Nullabile**: No (colonna `longtext` `NOT NULL`)
- **Esempio di valori**: array con un elemento tipo
  `{"name":"SCONTO10","type":"percentage","value":10,"voucherType":"all","maximunNoUse":100,
  "totalNoOfUsage":37,"voucherDateRange":[{"startDate":"2024-06-01","endDate":"2024-09-30"}],"status":1,
  "currentId":123456789}`; per il sotto-campo `voucherType`: `"all"`, `"custom"`, `"withoutCustom"`; per
  `type`: `"percentage"`, `"fixed"`

### createdBy / propertyId
- **Tipo**: `int`
- **Vincoli**: nessuna FK
- **Descrizione**: come per le altre tabelle di questo dominio.
- **Nullabile**: No
- **Esempio di valori**: `createdBy`: 99; `propertyId`: 4460

### createdAt / updatedAt
- **Tipo**: `datetime` — standard.
- **Nullabile**: No
- **Esempio di valori**: `2024-03-15 10:22:31`

---

## Osservazioni verificate nel codice (criticità)

- **Doppia serializzazione JSON confermata e origine tracciata**: `adddiscounts` (righe 25-29, 45-49)
  chiama `JSON.stringify(req.body.items)` **prima** di passare il valore a un campo di tipo
  `Sequelize.JSON`, che a sua volta serializza di nuovo il valore al salvataggio — il risultato è una
  stringa JSON "doppiamente incapsulata" nel DB (confermato sui dati reali). Tutti i punti che leggono
  correttamente il campo devono quindi fare **due `JSON.parse` consecutivi**
  (`viewdiscounts`, `checkdiscount`, `getpropertyDiscount`, e l'aggiornamento contatore in
  `booking.controller.js:986-988`) — pattern già segnalato a livello generale in
  [01-criticita-e-indicazioni-backend.md](../../docs/01-criticita-e-indicazioni-backend.md).
- **Inconsistenza verificata fra endpoint nello stesso file**: `associateRevenueDiscount2` (riga 229) e
  `updateVoucherDateRange` (riga 427) leggono `discounts.items` con **un solo** `JSON.parse`, non due —
  dato che il valore reale in DB è doppiamente incapsulato, il risultato di questo singolo parse è ancora
  una **stringa** (il JSON interno non ancora decodificato), non l'array atteso. In `updateVoucherDateRange`
  questo porta a chiamare `.forEach` su una stringa, che **lancia un'eccezione a runtime** per qualunque
  riga scritta tramite il percorso standard di `adddiscounts`.
- **`updateVoucherDateRange` non invia mai una risposta in caso di errore**: il blocco `catch` (righe
  444-446) si limita a un `console.error`, senza alcun `res.send`/`res.json` — combinato con il bug sopra,
  una richiesta a questo endpoint va quasi certamente in **timeout lato client** invece di ricevere un
  errore esplicito.
- **`updateVoucherDateRange` esposto senza autenticazione effettiva**: la route `POST /api/sb/update`
  (`user.routes.js:2035-2043`) usa solo `authJwt.optionalAuth`, con i controlli `verifyToken`/`isSuperAdmin`
  **commentati** — l'endpoint, chiamabile da chiunque senza token valido, itera ed effettua un `save()` su
  **tutti** i record della tabella `discounts` di **tutte** le proprietà (`Discounts.findAll()` senza alcun
  filtro) — un endpoint di scrittura di massa, privo di autenticazione e di limiti, verosimilmente residuo
  di una migrazione una-tantum mai rimosso dal routing.
- **Contatore utilizzi (`totalNoOfUsage`) aggiornato in modo non atomico e fidandosi del client**: in
  `booking.controller.js:998-1001` (e analogamente in `servicereservation.controller.js:544-545`), il nuovo
  valore è calcolato come `parseInt(req.body.data[index]?.discountData.totalNoOfUsage) + 1` — cioè il
  valore di partenza **arriva dal client**, non viene ricalcolato leggendo lo stato corrente autoritativo
  lato server (pur essendo disponibile: la riga viene comunque riletta subito prima con `Discounts.findOne`
  per recuperare `itemsArray`, ma il vecchio contatore usato per l'incremento è quello del body, non quello
  appena letto da `itemsArray[positionOfCode].totalNoOfUsage`). Questo rende il contatore sia
  **manipolabile dal client** (un client malevolo può inviare `totalNoOfUsage: 0` per riottenere sempre
  utilizzi disponibili) sia soggetto a **race condition** fra prenotazioni concorrenti sullo stesso voucher
  (nessuna transazione/lock: due richieste quasi simultanee possono leggere lo stesso stato e sovrascriversi
  a vicenda, perdendo un incremento) — un limite di utilizzo che nella pratica può essere aggirato o
  risultare inaffidabile. Da riprogettare nella riscrittura con un contatore atomico lato server (es.
  `UPDATE ... SET usage = usage + 1` con vincolo, o riga dedicata per voucher invece di array JSON).
- **`getPropertyDiscountDetails` e `associateRevenueDiscount2` risultano non collegati a nessuna route**
  (nessun riferimento in `user.routes.js`) — verosimilmente codice morto/non più raggiungibile; **non
  affidabile come riferimento** se in futuro venisse ricollegato, dato che `getPropertyDiscountDetails`
  presenta lo stesso bug di singolo-parse-su-valore-doppio già descritto sopra (userebbe `for...of` su una
  stringa carattere per carattere anziché sull'array di voucher).
- **IDOR verificato**: `adddiscounts`/`viewdiscounts`/`checkdiscount` operano su `req.body.propertyId`
  senza alcuna verifica contro l'utente autenticato — stesso pattern sistemico del dominio C2. Per
  `checkdiscount` in particolare l'impatto è più contenuto (endpoint di sola lettura/verifica codice), ma
  `adddiscounts` permette di sovrascrivere i voucher di una proprietà arbitraria.
