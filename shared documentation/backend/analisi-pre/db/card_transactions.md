# Tabella `card_transactions`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **708 righe**, query
> completa; `createdAt` dal 2022-04-26 al 2026-09-06 → feature attiva ma **di nicchia**: ~700 movimenti
> carta in 4 anni, concentrati su 2-3 stabilimenti). Model
> [`back-pre/app/models/cardTransaction.model.js`](../../../../../../back-pre/app/models/cardTransaction.model.js)
> (ORM `db.cardTransaction`; commento inline: *"transaction_type: 1 for credit, 2 for debit"*),
> controller
> [`cardTransaction.controller.js`](../../../../../../back-pre/app/controllers/cardTransaction.controller.js)
> (`createCardTransaction` → `POST /api/sb/cardTransaction`; `getMovementList` →
> `GET /api/sb/.../getMovementList`; `getBalanceAndIdentifier`, `getAllCardTransaction`,
> `getCardTransactionDetailsByBookingId`, `deleteUserCard`, `sendCardDetails`); email
> [`mailtemplates/cardDetail.js`](../../../../../../back-pre/app/mailtemplates/cardDetail.js). Frontend `front-pre`
> (solo gestionale): `src/services/**` e schermate "carta/tessera cliente" (endpoint
> `sb/getCardDetails`, `sb/send-card-detail`, `sb/getAllCardTransaction`, `sb/deleteUserCard`).
>
> **Nota di contesto** — movimenti della **carta/tessera prepagata (borsellino) del cliente** presso
> uno stabilimento: ricariche (credito) e consumi (debito). **Una riga = un movimento.** Il saldo si
> ottiene come `Σ(credito) − Σ(debito)` per `(identifier)`. Non è un pagamento della prenotazione in
> sé, ma un mezzo di pagamento alternativo il cui uso può essere legato a una prenotazione
> (`booking_id`).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `709` (708 righe).

### user_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `customers.id` (nel controller la variabile si chiama `userID` ma è l'id di un record
  `customers`, creato al volo se non esiste per `email`/`mobile`/`user_id` + `property_id`). Nessuna
  FK.
- **Esempio di valori** (prod): `1`, `3`, `12`, `147` — range `1` … `177.249` (mix di id `customers`
  bassi storici e alti recenti).

### identifier
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: numero della **carta/tessera prepagata fisica** consegnata al cliente dallo
  stabilimento (borsellino ricaricabile con cui pagare consumazioni bar/servizi senza contanti). È la
  chiave logica del "conto tessera": `getBalanceAndIdentifier` / `getAllCardTransaction` calcolano il
  saldo con `GROUP BY identifier` come `Σ(credito) − Σ(debito)`.
- **Come viene valorizzato**: **generato dal frontend** (`front-pre` / `front-customer-pre`,
  `reserveArticle/Modal/CardFeature.js`) alla prima operazione carta per un cliente che non ne ha già
  una: `formatPropertyId(propertyId).padEnd(4,'0') + Math.floor(100000 + Math.random()*900000)` → cioè
  **`<propertyId>` + 6 cifre casuali** (es. property `10590` → `"10590" + "193203"` = `"10590193203"`).
  Se il cliente ha già una tessera (`getCardBalance` ritorna un `identifier` non vuoto) viene
  **riusato** quello. Il campo è anche **editabile a mano** in un input di testo (`handleIdentifier`)
  → l'operatore può digitare il numero stampato su una tessera fisica preesistente.
- **Criticità**: nessun `UNIQUE` a DB, nessun controllo di collisione, RNG non sicuro, prefisso
  property ricavato per concatenazione di stringa (`padEnd(4)` non fa nulla se `propertyId` ha già
  ≥4 cifre); il legame `identifier → (user_id, property_id)` è solo convenzionale (le query filtrano
  comunque per `user_id` + `property_id`). In riscrittura: `wallet` con `id` proprio + FK a
  `customers`/`property`, e un eventuale `card_number` stampato separato e con `UNIQUE`.
- **Esempio di valori** (prod): oltre al formato `<propertyId>+6 cifre`, in produzione compaiono anche
  valori **sporchi/legacy**: `""` (stringa vuota), `"test"`, e il vecchio formato `"<n>-<timestamp>"`
  (es. `"1-1651202609679"`, `"4-1651202946854"`). Nessuna unicità garantita.

### amount
- **Tipo**: `decimal(10,2)`
- **Nullabile**: Sì (`NULL` su 21 righe su 708)
- **Descrizione**: importo del movimento. **Può essere negativo** anche con `transaction_type = 1`
  (in produzione: da `-1.000,00` a `99.999,00`). Il calcolo del saldo è
  `SUM(CASE WHEN transaction_type=1 THEN amount ELSE 0 END) - SUM(CASE WHEN transaction_type=2 THEN
  amount ELSE 0 END)` → i consumi risultano registrati come **credito negativo** invece che come
  `transaction_type = 2` (vedi criticità).
- **Esempio di valori** (prod): `100.00`, `200.00`, `50.00`, `150.00`.

### transaction_type
- **Tipo**: `int`
- **Nullabile**: Sì (`NULL` su 1 riga)
- **Descrizione**: `1` = credito (ricarica della tessera), `2` = debito (consumo pagato con la
  tessera).
- **Verifica: quali valori sono usati** — **entrambi**, dal codice: il frontend invia
  `transaction_type: 1` dalla ricarica (`reserveArticle/Modal/CardFeature.js:246`, con `amount`
  positivo) e `transaction_type: 2` dal pagamento di un servizio con la carta
  (`reserveArticle/Modal/PayWithCard.js` e `PayWithCardOld.js`, con `debit_details` valorizzato); il
  backend li somma separatamente in `getBalanceAndIdentifier` / `getAllCardTransaction`. **In
  produzione compaiono entrambi i valori**: `1` (ricarica) 637 righe, `2` (consumo) 70 righe — quindi
  il percorso `type = 2` è usato davvero (l'ipotesi precedente "campione solo type=1" era un limite del
  campione dev).
- **Candidato a enumerazione**: **Sì** (`CardMovementType`: `Credit` = 1 / `Debit` = 2). In
  migrazione: normalizzare eventuali righe `type = 1` con `amount < 0` (debiti registrati come
  crediti negativi) in `type = 2` con `amount > 0`. Vedi
  [`docs/03-gestione-dati.md` §2](../../progettazione/03-gestione-dati.md).
- **Esempio di valori** (prod): `1` (637), `2` (70).

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK). Valorizzato quando il movimento è collegato a una
  prenotazione (es. saldo di consumazioni). `getCardTransactionDetailsByBookingId` filtra
  `(user_id, booking_id)`. **In produzione**: `NULL` su 162 righe (23%) e il valore `0` è frequente
  (equivalente a "nessuna prenotazione").
- **Esempio di valori** (prod): `0`, `2`, `5873` — range `0` … `322.170`.

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì (`NULL` su 2 righe)
- **Relazioni**: `properties.id` (nessuna FK)
- **Descrizione**: **in produzione la feature è concentrata**: `10590` (440 righe su 708), `96` (146),
  `228` (39), `10557` (26) — 14 `property_id` distinti in totale.
- **Esempio di valori** (prod): `10590`, `96`, `228`.

### operated_by
- **Tipo**: `int`
- **Nullabile**: Sì (`NULL` su 51 righe)
- **Relazioni**: `users.id` dell'operatore che ha registrato il movimento (`req.body.operated_by`).
- **Esempio di valori** (prod): `1468` (435 righe — un unico operatore domina), `199` (100), `514`
  (35).

### debit_details
- **Tipo**: `longtext` — JSON (oggetto)
- **Nullabile**: Sì (valorizzato **solo** sui movimenti `transaction_type = 2`; `null` sulle
  ricariche)
- **Descrizione**: dettaglio del servizio pagato con la tessera. Costruito dal frontend in
  `PayWithCard.js` (`itemDetails`) e restituito da `getMovementList` come
  `JSON.parse(list.debit_details)`; il gestionale lo mostra in `CardFeature.js`
  (`itm?.debit_details?.name`, `-{totalPrice}`).
- **Campi** (dal codice `PayWithCard.js`):
  - **name**: `string` — nome del servizio addebitato (`selectedItemForCard.ServiceName`). Esempio:
    `"Espresso"`
  - **price**: `string` — prezzo unitario del servizio (`selectedItemForCard.ServicePrice`). Esempio:
    `"1.20"`
  - **serviceCount**: `number` — quantità (`count`). Esempio: `2`
  - **selectedVariantData**: array di `{ variantnames, variantvalues }` — varianti scelte (es.
    "Macchiato" +0,30). Esempio: `[{"variantnames":"Macchiato","variantvalues":"0.30"}]`
  - **totalPrice**: `number` — totale della riga: `count * price + Σ(varianti)`. Esempio: `2.7`
- **In produzione**: valorizzato su ~50 righe (le 70 di `type = 2`; `NULL` sul 93%). Dati reali con
  qualche anomalia: `serviceCount: 0` con `price`/`totalPrice` `null` (es. `"Parcheggio"`, `"Panino"`),
  `price` con troppe cifre decimali (`"55.656595"`, `"85.32222"`), `serviceCount: 22` per un `"Wine"`.
- **Esempio di valori** (prod):
  `{"name":"Caffè","price":"1","serviceCount":1,"selectedVariantData":[],"totalPrice":1}`,
  `{"name":"Lettino","price":"8","serviceCount":2,"selectedVariantData":[],"totalPrice":16}`

### IsDeleted
- **Tipo**: `int` — default `0` (nota: nome con **`I` maiuscola** a schema, `isDeleted` nel model —
  vedi criticità)
- **Nullabile**: Sì
- **Descrizione**: soft-delete. `deleteUserCard` fa `UPDATE ... SET isDeleted = 1 WHERE (user_id,
  property_id, isDeleted = 0)` — Sequelize mappa comunque grazie al model, ma le query SQL grezze usano
  `isDeleted` (minuscolo) mentre la colonna è `IsDeleted`.
- **Esempio di valori** (prod): `0` (626), `1` (82).

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard
- **Nullabile**: Sì
- **Esempio di valori** (prod): range `2022-04-26 05:23:59` … `2026-09-06 11:50:18`.

---

## Osservazioni verificate nel codice (criticità)

- **`transaction_type`**: in produzione compaiono entrambi i valori (`1` × 637, `2` × 70) — quindi la
  distinzione ricarica/consumo è usata. Restano da normalizzare le righe `type = 1` con `amount < 0`
  (debiti registrati come crediti negativi da un vecchio flusso) e 1 riga con `type` `NULL`.
- **Incoerenza maiuscola/minuscola su `IsDeleted` / `isDeleted`**: colonna `IsDeleted`, query grezze e
  model usano `isDeleted`. Su MySQL (case-insensitive per i nomi colonna di default su Windows)
  funziona per caso; su PostgreSQL romperebbe.
- **`identifier` non è una vera chiave**: stringa libera fornita dal client; il saldo si calcola per
  `identifier` senza garanzia che un `identifier` appartenga a un solo `user_id`/`property_id`
  (`getBalanceAndIdentifier` raggruppa solo per `identifier`, filtrando per `user_id` + `property_id`).
- **`createCardTransaction` crea `customers` al volo** con `fullName: ""` se non trova match — genera
  anagrafiche vuote.
- **`getAllCardTransaction` richiede `startDate`/`endDate` ma non li usa** nella query (dopo la
  rimozione del `JOIN bookings`): i parametri restano nel `replacements` ma non compaiono nel `WHERE`.
- **`amount` `decimal(10,2)`**: unico campo importo del dominio già tipizzato correttamente (a
  differenza di `booking_payments`/`service_reservation_payments` che usano `float`).
- **`debit_details`**: in produzione è popolato sulle ~50 righe di consumo (`type = 2`), con dati
  sporchi (prezzi a molte cifre decimali, `serviceCount: 0` con totali `null`). Nel target: righe
  figlie tipizzate, non blob JSON.
- **Nessuna FK.**
