# Tabella `user_stripe_card_details`

> Fonti: schema reale (**0 righe**, `AUTO_INCREMENT=78` → storicamente ~77 righe, oggi svuotata),
> model
> [`back-pre/app/models/userStripeCardDetails.model.js`](../../back-pre/app/models/userStripeCardDetails.model.js)
> (`sequelize.define("user_stripe_card_details", …)` senza `freezeTableName`/`timestamps: false` →
> Sequelize gestisce `createdAt`/`updatedAt`; il model dichiara anche `propertyId` **che a schema NON
> esiste** — vedi criticità),
> uso in
> [`payment.controller.js`](../../back-pre/app/controllers/payment.controller.js) `customerCardData`
> (`findOne({ where: { userId } })` → `stripe.paymentMethods.list({ customer: stripeCustomerCardId,
> type: "card" })`),
> [`stripe.controller.js`](../../back-pre/app/controllers/stripe.controller.js)
> `getCardDetails` / `createCustomerStripeDetails` / `removeCardDetails`
> (`findOne({ where: { userId, propertyId } })` → `stripe.customers.listSources(...)`),
> [`controllers/helpers/common.helper.js`](../../back-pre/app/controllers/helpers/common.helper.js)
> `createCustomerStripeDetails` (`findOne({ where: { userId, propertyId } })`, `create({
> stripeCustomerCardId, userId, propertyId })`) e `createCustomerStripeDetails2`
> (`create({ stripeCustomerCardId, token, userId })` — **unico writer di `token`**, senza chiamanti
> individuati).
> Rotte: `/api/sb/getCardDetails`, `/api/sb/stripe/createCustomerStripeDetails`,
> `/api/sb/stripe/removeCardDetails`, `/api/sb/customer/cards` (× 2, duplicata — →
> `payment.controller.customerCardData`).
> Frontend: `front-pre` / `front-customer-pre` `data/actions/managerActions.js`
> (`createCustomerStripeDetails` / `getCardDetails` / `removeCardDetails`) e
> `consumerAction.js` / `manageAccountAction.js` (`getCardsListForPayments` / `_getUserCardDetails`
> → `sb/customer/cards`); consumato da `PaymentParent.js`, `MyCardsParent.js`,
> `PaymentMethodSelection.js`, `PropertySignIn.js`.
> Nessuna associazione ORM in [`models/index.js`](../../back-pre/app/models/index.js).
>
> **Nota di contesto** — doveva contenere la **mappatura utente → Stripe Customer id** per
> riutilizzare le carte salvate del cliente nei pagamenti online (una riga per utente, o per
> utente×stabilimento nella versione col `propertyId` del model). È la controparte "carte salvate
> Stripe" di [`card_transactions`](card_transactions.md) (borsellino prepagato) e
> [`stripe_configrations`](stripe_configrations.md) (account venditore).
>
> ### ⚠️ Confermato: tabella **NON utilizzata** (feature non operativa end-to-end)
>
> Verificato su schema, dati e codice:
> 1. **0 righe**; `AUTO_INCREMENT = 78` → in passato era popolata, poi svuotata.
> 2. **Model ≠ schema**: il model dichiara `propertyId`, la colonna **non esiste** a DB
>    (`SHOW COLUMNS` → solo `id, stripeCustomerCardId, userId, token, createdAt, updatedAt`;
>    `SELECT … WHERE propertyId` → `ERROR 1054 Unknown column 'propertyId'`).
> 3. **Tutti i percorsi di scrittura falliscono**: `common.helper.createCustomerStripeDetails`
>    (usato sia dalla rotta `/api/sb/stripe/createCustomerStripeDetails` sia internamente dal flusso
>    di pagamento con carta salvata, `stripe.controller.js:710`) fa
>    `findOne({ where: { userId, propertyId } })` e `create({ …, propertyId })` → **errore SQL** →
>    nessun `INSERT` va mai a buon fine.
> 4. **I percorsi di lettura** o falliscono per lo stesso motivo (`stripe.controller.getCardDetails`
>    esige `req.body.propertyId` e poi filtra su `propertyId` → errore) o girano ma non trovano nulla
>    (`payment.controller.customerCardData`, filtra solo `userId`, tabella vuota → sempre "No cards
>    found"); `createCustomerStripeDetails2` (unico writer di `token`, senza `propertyId`) **non ha
>    chiamanti**; `createStripeCrustomerDataByCardData`/`…ByToken` **non sono in nessuna rotta**.
> 5. **Frontend**: le schermate "carte salvate" (`MyCardsParent` usa perfino `userId: 1` hardcoded)
>    ricevono sempre `{status:false}` e mostrano l'elenco vuoto / il form nuova carta. Ogni
>    pagamento passa da un inserimento carta ex-novo.
>
> → In riscrittura: **da rifare da zero** (o **eliminare**), non da migrare.
>
> **Dominio nella riscrittura**: **C3 — modulo `Payment`** (Stripe; distinto da **C4** `Fiskaly`) —
> metodi di pagamento salvati del cliente. Da rifare da zero come `customer_stripe_customers`
> (`customer_id` → Stripe Customer,
> per connected account), oppure eliminare se si delega la gestione carte interamente a Stripe.
> Analisi domini in [`docs/03-gestione-dati.md` §4](../../docs/03-gestione-dati.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, .. *(tabella attualmente vuota)*

### stripeCustomerCardId
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Relazioni**: identificativo esterno **Stripe** — id di un *Customer* (`cus_…`) creato con
  `stripe.customers.create(...)`. Passato a `stripe.paymentMethods.list({ customer })` /
  `stripe.customers.listSources(...)` per elencare le carte salvate, e a
  `stripe.paymentMethods.attach(paymentMethodId, { customer })`.
- **Descrizione**: nonostante il nome ("...CardId"), **non** è l'id di una carta: è l'id del Customer
  Stripe che raccoglie le carte dell'utente.
- **Esempio di valori**: `"cus_Qh9…"` (formato Stripe; nessun dato reale nel campione)

### userId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `users.id` — **nessuna FK reale**, nessuna associazione ORM. Chiave di ricerca
  (`findOne({ where: { userId } })`).
- **Descrizione**: utente (cliente consumer) proprietario del Customer Stripe.
- **Esempio di valori**: (nessun dato reale) es. `1421`, `2093`

### token
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Deprecato**: **Sì** — scritto **solo** da `common.helper.createCustomerStripeDetails2`, che non
  ha chiamanti individuati in `back-pre` (né nelle rotte né in altri controller); mai letto. Il
  flusso attivo (`createCustomerStripeDetails`) non lo popola.
- **Descrizione**: (legacy) id del token carta Stripe (`stripe.tokens.create(...)`), usato dalla
  variante `...2` per `stripe.customers.createSource(customerId, { source: token.id })`.
- **Esempio di valori**: `"tok_…"` (nessun dato reale)

### createdAt
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: creazione riga (gestita da Sequelize; il model non disabilita `timestamps`).
- **Esempio di valori**: (nessun dato reale)

### updatedAt
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: ultimo aggiornamento (gestito da Sequelize).
- **Esempio di valori**: (nessun dato reale)

### propertyId *(solo nel model — assente a schema)*
- **Tipo**: `int` nel model (`defaultValue: null`) — **la colonna NON esiste nella tabella**
- **Nullabile**: n/d
- **Deprecato**: **Sì** (come colonna: non esiste; come attributo del model: rende non eseguibili i
  percorsi che la usano)
- **Descrizione**: nel model doveva legare la carta salvata anche allo stabilimento (Customer Stripe
  creato sul *connected account*). Poiché la colonna manca, ogni query/insert che la referenzia
  (`stripe.controller.getCardDetails`, `common.helper.createCustomerStripeDetails`) produce un
  errore SQL "unknown column 'propertyId'".

---

## Osservazioni verificate nel codice (criticità)

- **Model disallineato dallo schema**: il model dichiara `propertyId`, la tabella no. I due percorsi
  "attivi" (`getCardDetails`, `common.helper.createCustomerStripeDetails`) filtrano/inseriscono per
  `propertyId` → **falliscono a runtime**. Solo `payment.controller.customerCardData` (filtra per
  `userId`) sarebbe eseguibile, ma non è mappato a una rotta usata dal frontend.
- **Feature morta lato frontend**: `front-customer-pre/PaymentParent.getCardDetails` invia solo
  `userId`; il backend `getCardDetails` esige `propertyId` e risponde `{status:false}` → il
  frontend mostra sempre il form "nuova carta", mai le carte salvate. Tabella vuota di conseguenza.
- **`token` colonna morta** (solo `...2`, senza chiamanti).
- **Nome fuorviante**: `stripeCustomerCardId` è un *Customer* id, non una carta.
- **Nessuna FK** verso `users`, nessuna associazione ORM.
- **Nessun `UNIQUE`** su `userId` (né `userId + propertyId`): potrebbero esistere Customer duplicati
  per lo stesso utente.
- **PCI/sicurezza**: la tabella di per sé conserva solo id/token Stripe (non PAN/CVC), il che è
  corretto; il problema PCI vero è altrove ([`online_reservations`](online_reservations.md) con
  colonne carta in chiaro). Qui la raccomandazione è: ricostruire pulito o delegare del tutto a
  Stripe.
- Nessun campo è un enumeratore.
