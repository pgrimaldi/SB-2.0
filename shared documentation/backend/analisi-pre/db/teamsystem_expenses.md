# Tabella `teamsystem_expenses`

> Fonti: schema reale; **0 righe sia in locale sia nel dump di produzione `sb_prod_dump_2026_09_08`**
> (esportata solo come schema). Conferma: l'integrazione POS TeamSystem / Cassa in Cloud non è mai
> stata attivata (nessuna riga `keysecret` con `appName = "TeamSystem"`, `booking_number_informations.teamsystem_payment` sempre `NULL`). Model
> [`back-pre/app/models/teamsystemExpense.model.js`](../../../../../../back-pre/app/models/teamsystemExpense.model.js)
> (`{ freezeTableName: true, timestamps: false }` — il model dichiara `idRef: Sequelize.STRING`, ma a
> schema la colonna è `bigint`: **disallineamento**, vedi criticità),
> scrittura/lettura in
> [`Service/teamsystemService.js`](../../../../../../back-pre/app/Service/teamsystemService.js):
> - `getRegistrations(propertyId, fromTs, toTs)` — legge `bookings` e restituisce le "registrazioni"
>   (prenotazioni raggruppate per `bookingNumber`) al POS;
> - `saveExpense(beachId, registrationId, dto)` — **idempotente su `(beachId, idRef)`**: se la riga
>   esiste ne restituisce l'`id` originale (`isRetry`); altrimenti, in transazione, crea la riga e la
>   "specchia" in [`booking_number_informations`](booking_number_informations.md) (`teamsystem_payment`
>   JSON + rigenerazione della fetta `teamsystem_payment` di `details_payments`/`details_payments_new`
>   via `FiskalyHelper.lineItemsFromTeamSystemPaymentsArray`);
> - `isBookingFiscalized(bookingNumber)` — verifica live su [`fiskaly_receipt`](fiskaly_receipt.md)
>   (`record_state = 'COMPLETED'`, `record_mode = 'FINISHED'`) — è **questo** valore, non la colonna
>   `fiscalized`, a essere restituito al POS.
> Controller [`teamsystem.controller.js`](../../../../../../back-pre/app/controllers/teamsystem.controller.js)
> (`postExpense`, `getRegistrations`; validazione della `payload` contro la spec "Cassa in Cloud"
> v1.0, `app` fisso `"Cassa in Cloud"`), rotte in
> [`routes/teamsystem.routes.js`](../../../../../../back-pre/app/routes/teamsystem.routes.js)
> (`BASE = /api/sb/teamsystem/v1`; `GET {BASE}/beaches/:beachId/registrations`,
> `POST {BASE}/beaches/:beachId/registrations/:registrationId/expenses`), autenticazione OAuth2
> *client credentials* via [`keysecret`](keysecret.md) (`appName = "TeamSystem"`,
> `params.property_id` deve coincidere col `beachId`; token verificato in
> [`middleware/authJwt.js`](../../../../../../back-pre/app/middleware/authJwt.js) → `req.client`).
> DTO in [`models/dto/teamsystem.dto.js`](../../../../../../back-pre/app/models/dto/teamsystem.dto.js).
> Nessuna associazione ORM, nessun uso dal frontend gestionale (è un'API server-to-server con il POS
> **TeamSystem / Cassa in Cloud**).
>
> **Nota di contesto** — **1 riga per "spesa" (scontrino del POS) trasmessa dal registratore di cassa
> TeamSystem/Cassa in Cloud** e associata a una prenotazione dello stabilimento. Serve a portare gli
> addebiti fatti in cassa dentro il conto della prenotazione (analogo a `ofsale_payments` per il POS
> OfSale). **Tabella vuota nel campione**: integrazione predisposta ma non ancora in uso con dati
> reali (o non funzionante per il disallineamento di tipo su `idRef`).
>
> **Dominio nella riscrittura**: **C3 — modulo `Payment`** (integrazione POS esterna Cassa in Cloud:
> il record è una **spesa/addebito** che confluisce nel conto della prenotazione, analogo a
> [`card_transactions`](card_transactions.md) e a `ofsale_payments`; coerente con
> [`docs/03-gestione-dati.md` §4](../../progettazione/03-gestione-dati.md) che colloca `teamsystem_payment` in
> **C3**). Il record alimenta anche il conto della prenotazione (**C1**) via
> `booking_number_informations.teamsystem_payment`; la fiscalizzazione è **solo consultata** in
> lettura sul modulo `Fiskaly` (**C4**) — `isBookingFiscalized` interroga
> [`fiskaly_receipt`](fiskaly_receipt.md) — non è di competenza di questa tabella. Elencata tra i
> domini non ancora coperti in [`_feature-tabelle.md`](_feature-tabelle.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Descrizione**: id auto-incrementale restituito al POS come identificativo della spesa registrata
  (`return res.status(200).json({ id: result.id })`); su un retro con `idRef` già visto viene
  restituito lo **stesso** id (idempotenza).
- **Esempio di valori**: 1, 2, 3, .. *(tabella attualmente vuota)*

### beachId
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: `properties.id` — **nessuna FK reale**. Parte dell'indice `UNIQUE(beachId, idRef)`;
  indice su `bookingNumber` a parte.
- **Descrizione**: `propertyId` dello stabilimento, preso dal token OAuth (`req.client.params.property_id`)
  e verificato contro il `:beachId` dell'URL (`resolveBeachId` → `beach_mismatch` se non coincidono).
- **Esempio di valori**: `10588` (dai `params` di [`keysecret`](keysecret.md))

### registrationId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `bookings.id` — **nessuna FK reale**. È l'`id` di una prenotazione **rappresentativa**
  del gruppo `bookingNumber` (`getRegistrations` raggruppa per `bookingNumber` e usa `group[0].id`).
- **Descrizione**: la "registrazione" a cui il POS associa la spesa (passata nell'URL come
  `:registrationId`). `saveExpense` verifica che appartenga al `beachId` (`BOOKING_NOT_FOUND` → 404).
- **Esempio di valori**: es. `269344`

### bookingNumber
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Relazioni**: `bookings.bookingNumber` — nessuna FK; indice `teamsystem_expenses_booking_number`.
  È la chiave con cui la spesa viene "specchiata" in
  [`booking_number_informations`](booking_number_informations.md).
- **Descrizione**: numero prenotazione risolto da `registrationId` in `saveExpense`.
- **Esempio di valori**: es. `"1058812345"`

### idRef
- **Tipo**: **`bigint`**, `NOT NULL` a schema — ma il model lo dichiara `Sequelize.STRING` e la spec/DTO
  lo trattano come **stringa UUID** (`isNonEmptyString(dto.idRef)`; commento model: *"POS receipt
  document id (UUID string)"*)
- **Nullabile**: No
- **Relazioni**: parte dell'indice `UNIQUE(beachId, idRef)` — **chiave di deduplicazione** per
  stabilimento (un `idRef` già visto per quel `beachId` → retry idempotente, si restituisce l'id
  originale).
- **Descrizione**: id del documento/scontrino sul POS Cassa in Cloud.
- **Deprecato**: no (campo centrale), ma **incoerente**: `bigint` a DB vs stringa UUID attesa dal
  codice → un `idRef` alfanumerico non è inseribile. Nel target `varchar`/`uuid`.
- **Esempio di valori**: *(nessun dato reale)* atteso es. `"a1b2c3d4-…"` (UUID) oppure un intero

### amount
- **Tipo**: `decimal(10,2)`
- **Nullabile**: Sì
- **Descrizione**: totale della spesa (`dto.amount`, IVA inclusa). Il controller **avvisa** (log, non
  blocca) se `amount` non coincide con `Σ data[].price` entro 1 cent. Duplica `payload.amount`.
- **Esempio di valori**: *(nessun dato reale)* es. `24.50`, `7.00`

### payload
- **Tipo**: `longtext` — **oggetto JSON** (`CHECK (json_valid(payload))`)
- **Nullabile**: Sì
- **Descrizione**: copia integrale del corpo della spesa ricevuto dal POS (`{ app, description,
  amount, data }`). Campi:
  - **app** — `string`: sempre `"Cassa in Cloud"` (valore fisso richiesto dalla spec; il controller
    rifiuta valori diversi). Es.: `"Cassa in Cloud"`.
  - **description** — `string`: nome del punto vendita / descrizione della spesa. Es.: `"Bar Centrale"`.
  - **amount** — `number` (decimal): totale scontrino, IVA inclusa. Es.: `24.50`.
  - **data** — `array`: righe della spesa; ogni elemento:
    - **qnt** — `number`: quantità (> 0). Es.: `2`.
    - **name** — `string`: descrizione prodotto. Es.: `"Caffè"`.
    - **price** — `number`: **totale riga** (qnt × prezzo unitario), IVA inclusa (> 0). Es.: `2.40`.
    - **vat** — `number`: aliquota IVA come percentuale intera (≥ 0). Es.: `22`.
- **Esempio di valori**:
  `{"app":"Cassa in Cloud","description":"Bar Centrale","amount":24.50,"data":[{"qnt":2,"name":"Caffè","price":2.40,"vat":22},{"qnt":1,"name":"Panino","price":6.00,"vat":10}]}`

### fiscalized
- **Tipo**: `tinyint(1)`, `NOT NULL`, `DEFAULT 0`
- **Nullabile**: No
- **Deprecato**: **Sì** (come dato persistito) — il commento del model dice *"true once a fiskaly
  receipt covering this expense has been emitted"*, ma nessun percorso di codice aggiorna mai la
  colonna a `1`: viene scritta **solo** `false` alla creazione. Il valore che il controller usa e
  restituisce (`result.fiscalized`) è calcolato **live** da `isBookingFiscalized(bookingNumber)`
  (query su `fiskaly_receipt`), non letto da qui.
- **Descrizione**: (previsto) flag "la spesa è già coperta da uno scontrino elettronico Fiskaly".
- **Candidato a enumerazione**: **booleano**.
- **Esempio di valori**: `0`

### createdAt
- **Tipo**: `datetime` — `defaultValue: Sequelize.NOW`
- **Nullabile**: Sì
- **Descrizione**: creazione riga. Usato come `date` (ISO) dell'entry di pagamento specchiata in
  `booking_number_informations.teamsystem_payment` (`buildPaymentEntry`). `timestamps: false`.
- **Esempio di valori**: *(nessun dato reale)*

### updatedAt
- **Tipo**: `datetime` — `defaultValue: Sequelize.NOW`
- **Nullabile**: Sì
- **Descrizione**: ultimo aggiornamento. Di fatto immutabile (la riga non viene aggiornata).
- **Esempio di valori**: *(nessun dato reale)*

---

## Osservazioni verificate nel codice (criticità)

- **`idRef bigint` vs stringa UUID**: model (`STRING`), DTO (`isNonEmptyString`) e spec la trattano
  come UUID; la colonna è `bigint`. Un `idRef` alfanumerico non è inseribile → probabile causa della
  tabella vuota. Nel target `varchar`/`uuid` con `UNIQUE(beach_id, id_ref)`.
- **`fiscalized` colonna morta**: mai aggiornata a `1`; la fiscalizzazione è verificata a runtime
  (`isBookingFiscalized`). O si mantiene sincronizzata (evento "scontrino emesso" dallo stesso
  modulo `Fiskaly`) o si rimuove.
- **Ridondanza `amount`**: presente sia in colonna `decimal` sia dentro `payload.amount` (più le
  righe in `payload.data`). Nel target: colonna tipizzata + righe figlie normalizzate, non il blob.
- **Nessuna FK**: `beachId` → `properties`, `registrationId` → `bookings`, `bookingNumber` →
  `bookings` sono tutti solo valori copiati.
- **`registrationId` = id di una prenotazione "rappresentativa"** di un gruppo `bookingNumber`:
  semantica fragile (se il gruppo cambia, l'id rappresentativo può non essere più valido). Nel
  target legare la spesa direttamente al `booking_number` (testata C1).
- **Doppia scrittura non idempotente al 100%**: la riga e lo "specchio" in
  `booking_number_informations` sono in transazione, ma esiste una `ensureMirrored` di self-heal per
  i casi in cui lo specchio manca — segno che la coerenza non è garantita in ogni percorso.
- **`payload.app` come stringa fissa** — non un enumeratore vero, ma un valore costante da validare.
- **`payload.data[].vat` come percentuale intera** (`22`) mentre altrove nel sistema l'aliquota è
  `"22.00"` stringa o `22.00` numero: convenzioni diverse per lo stesso concetto.
- **Timestamp non automatici** (`timestamps: false`); charset `latin1`.
