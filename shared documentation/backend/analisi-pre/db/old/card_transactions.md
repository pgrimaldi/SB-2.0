# Tabella `card_transactions`

> Fonti: schema e dati reali (100 righe), model
> [`back-pre/app/models/cardTransaction.model.js`](../../back-pre/app/models/cardTransaction.model.js)
> (ORM `db.cardTransaction`; commento inline: *"transaction_type: 1 for credit, 2 for debit"*),
> controller
> [`cardTransaction.controller.js`](../../back-pre/app/controllers/cardTransaction.controller.js)
> (`createCardTransaction` → `POST /api/sb/cardTransaction`; `getMovementList` →
> `GET /api/sb/.../getMovementList`; `getBalanceAndIdentifier`, `getAllCardTransaction`,
> `getCardTransactionDetailsByBookingId`, `deleteUserCard`, `sendCardDetails`); email
> [`mailtemplates/cardDetail.js`](../../back-pre/app/mailtemplates/cardDetail.js). Frontend `front-pre`
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
- **Esempio di valori**: 1, 2, 3, ..

### user_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `customers.id` (nel controller la variabile si chiama `userID` ma è l'id di un record
  `customers`, creato al volo se non esiste per `email`/`mobile`/`user_id` + `property_id`). Nessuna
  FK.
- **Esempio di valori**: 55579

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
- **Esempio di valori**: `"10590193203"`, `"96487215"` (property 96 → `"96" + "487215"`)

### amount
- **Tipo**: `decimal(10,2)`
- **Nullabile**: Sì
- **Descrizione**: importo del movimento. **Può essere negativo** anche con `transaction_type = 1`
  (nel campione: da `-1000.00` a `70.00`). Il calcolo del saldo è
  `SUM(CASE WHEN transaction_type=1 THEN amount ELSE 0 END) - SUM(CASE WHEN transaction_type=2 THEN
  amount ELSE 0 END)` → i consumi risultano registrati come **credito negativo** invece che come
  `transaction_type = 2` (vedi criticità).
- **Esempio di valori**: `-16.00`, `-12.00`, `70.00`

### transaction_type
- **Tipo**: `int`
- **Nullabile**: Sì
- **Descrizione**: `1` = credito (ricarica della tessera), `2` = debito (consumo pagato con la
  tessera).
- **Verifica: quali valori sono usati** — **entrambi**, dal codice: il frontend invia
  `transaction_type: 1` dalla ricarica (`reserveArticle/Modal/CardFeature.js:246`, con `amount`
  positivo) e `transaction_type: 2` dal pagamento di un servizio con la carta
  (`reserveArticle/Modal/PayWithCard.js` e `PayWithCardOld.js`, con `debit_details` valorizzato); il
  backend li somma separatamente in `getBalanceAndIdentifier` / `getAllCardTransaction`
  (`SUM(CASE WHEN transaction_type = 1 ...) AS totalCredit`,
  `SUM(CASE WHEN transaction_type = 2 ...) AS totalDebit`). Nei **dati DB del campione** compaiono
  però solo righe con `transaction_type = 1` (e alcuni `amount` negativi) — il campione non contiene
  operazioni di debito recenti, ma il percorso `type = 2` è vivo nel codice.
- **Candidato a enumerazione**: **Sì** (`CardMovementType`: `Credit` = 1 / `Debit` = 2). In
  migrazione: normalizzare eventuali righe `type = 1` con `amount < 0` (debiti registrati come
  crediti negativi) in `type = 2` con `amount > 0`. Vedi
  [`docs/03-gestione-dati.md` §2](../../docs/03-gestione-dati.md).
- **Esempio di valori**: `1` (ricarica), `2` (consumo)

### booking_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` (nessuna FK). Valorizzato quando il movimento è collegato a una
  prenotazione (es. saldo di consumazioni). `getCardTransactionDetailsByBookingId` filtra
  `(user_id, booking_id)`.
- **Esempio di valori**: 171547

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `properties.id` (nessuna FK)
- **Esempio di valori**: 10590

### operated_by
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `users.id` dell'operatore che ha registrato il movimento (`req.body.operated_by`).
- **Esempio di valori**: 1468

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
- **Esempio di valori**:
  `{"name":"Espresso","price":"1.20","serviceCount":2,"selectedVariantData":[{"variantnames":"Macchiato","variantvalues":"0.30"}],"totalPrice":2.7}`

### IsDeleted
- **Tipo**: `int` — default `0` (nota: nome con **`I` maiuscola** a schema, `isDeleted` nel model —
  vedi criticità)
- **Nullabile**: Sì
- **Descrizione**: soft-delete. `deleteUserCard` fa `UPDATE ... SET isDeleted = 1 WHERE (user_id,
  property_id, isDeleted = 0)` — Sequelize mappa comunque grazie al model, ma le query SQL grezze usano
  `isDeleted` (minuscolo) mentre la colonna è `IsDeleted`.
- **Esempio di valori**: `0`, `1`

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard
- **Nullabile**: Sì
- **Esempio di valori**: `2026-07-26 07:42:40`

---

## Osservazioni verificate nel codice (criticità)

- **`transaction_type` incoerente tra codice e dati storici**: il codice invia sia `1` (ricarica,
  `CardFeature.js`) sia `2` (consumo, `PayWithCard.js`) e il saldo li somma separatamente; ma nel
  campione DB compaiono **solo righe `type = 1`**, alcune con `amount` **negativo** (debiti
  registrati come "crediti negativi", probabilmente da un flusso precedente). Da normalizzare in
  migrazione: `type = 2` + `amount > 0` per i consumi. In alternativa: tenere solo `amount` con segno
  ed eliminare `transaction_type` (ma si perde la distinzione esplicita ricarica/consumo utile per i
  filtri).
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
- **`debit_details` mai popolato** nel campione.
- **Nessuna FK.**
