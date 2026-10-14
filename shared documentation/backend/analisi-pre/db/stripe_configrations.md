# Tabella `stripe_configrations`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **44 righe**, query
> completa; `id` 12…63; `createdAt` dal 2023-05-24 al 2026-08-04 → ~44 stabilimenti hanno collegato un
> account Stripe Connect in ~3 anni). Model
> [`back-pre/app/models/stripeConfigrations.model.js`](../../../../../../back-pre/app/models/stripeConfigrations.model.js)
> (`sequelize.define("stripe_configrations", …)` — il 3° e 4° argomento passati separatamente sono
> in gran parte ignorati; `freezeTableName`/`timestamps` **non** applicati, ma il nome è già plurale
> e `timestamps: true` di fatto: il model dichiara comunque a mano `createdAt`/`updatedAt`),
> scrittura in
> [`payment.controller.js`](../../../../../../back-pre/app/controllers/payment.controller.js)
> `getCustomerIdByAuthCode` (scambia il `authCode` dell'OAuth Stripe Connect con
> `response.stripe_user_id` e fa `stripeConfig.findOne({ where: { propertyId } })` →
> `create` se assente, altrimenti `update(..., { where: { id: req.body.userId } })` — **bug**, vedi
> criticità), cancellazione in
> [`stripe.controller.js`](../../../../../../back-pre/app/controllers/stripe.controller.js)
> `deleteStripeAccount` (`stripe.accounts.del(sellerStripeConnectId)` + `stripeConfig.destroy({
> where: { propertyId } })` + `Property.update({ serviceOnlineBooking: 0 })`),
> lettura in `stripe.controller.js` in **quasi tutti** i flussi di pagamento online
> (`payment`, `capturePayment`, `updateCaptureCharge`, `refundPaymentController`, `capturePayment`,
> `findConnectedAccount`, `createPyamentLink`, `getAccountBalance`, …) sempre come
> `stripeConfig.findOne({ where: { propertyId } })`, e in
> [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js) (riga ~8925) e
> [`auth.controller.js`](../../../../../../back-pre/app/controllers/auth.controller.js) `signin`/`signup`
> (`include` di `stripeConfig` con `attributes: ["id", "sellerStripeConnectId"]`; il primo elemento
> `stripe_configrations[0].sellerStripeConnectId` è restituito al frontend come `stripeConnectId`).
> Associazione in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga 306):
> `db.property.hasMany(db.StripeConfig, { foreignKey: "propertyId" })`.
> Frontend: pannello **Impostazioni Stripe**
> `front-pre/src/components/stripeSetting/stripeSetting.js` e
> `front-customer-pre/src/components/stripeSetting/stripeSetting.js` (OAuth verso
> `https://connect.stripe.com/oauth/authorize?…&state=<propertyId>`, poi `POST
> /api/sb/getCustomerIdByAuthCode`, `POST /api/sb/deleteStripeAccount`, `POST
> /api/sb/stripe/balance`).
>
> **Nota di contesto** — **1 riga per stabilimento** (garantita solo dal codice: `findOne({ where: {
> propertyId } })` prima della `create`; nessun `UNIQUE` a schema — nei dati 43 righe con 43
> `propertyId` distinti). Contiene l'**id dell'account Stripe Connect (connected account) del
> venditore**: è la chiave con cui il backend, come piattaforma, incassa i pagamenti online delle
> prenotazioni sul conto dello stabilimento (`stripeAccount: sellerStripeConnectId` /
> `destination` nelle chiamate Stripe) e ne legge il saldo. La sua presenza abilita di fatto la
> prenotazione online con pagamento (`deleteStripeAccount` spegne `properties.serviceOnlineBooking`).
>
> **Dominio nella riscrittura**: **C3 — modulo `Payment`** (Stripe; distinto da **C4** `Fiskaly`) —
> configurazione del gateway di pagamento per stabilimento. Le transazioni vere stanno in
> [`online_transaction_details`](online_transaction_details.md) /
> [`refund_transaction_details`](refund_transaction_details.md). Analisi domini in
> [`docs/03-gestione-dati.md` §4](../../progettazione/03-gestione-dati.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### sellerStripeConnectId
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì (a schema; nei dati sempre valorizzato)
- **Relazioni**: identificativo esterno **Stripe** (connected account). Indice non-unico
  `stripe_configrations_seller_stripe_connect_id`. Riferito da
  [`online_transaction_details.stripe_connected_id`](online_transaction_details.md) (valore copiato,
  non FK).
- **Descrizione**: id dell'account Stripe Connect dello stabilimento (`response.stripe_user_id`
  restituito dallo scambio OAuth `authorization_code`). Usato come `stripeAccount` (header
  `Stripe-Account`) o `destination` in tutte le operazioni Stripe per conto del venditore;
  restituito al frontend al login (`stripeConnectId`) e da `findConnectedAccount`.
- **Esempio di valori**: `"acct_1NBFCIIQaR1tmsta"`, `"acct_1N5UjkBe2W4kCxpR"`,
  `"acct_1OiwcRDW9uVQTgbp"`

### propertyId
- **Tipo**: `int`
- **Nullabile**: Sì (a schema; nei dati sempre valorizzato)
- **Relazioni**: `properties.id` — associazione ORM `property.hasMany(StripeConfig, { foreignKey:
  "propertyId" })`; **nessuna FK reale** a DB. Indice non-unico `stripe_configrations_property_id`.
- **Descrizione**: stabilimento proprietario dell'account Stripe. Chiave logica di ricerca in tutti
  i flussi di pagamento (`findOne` / `destroy` per `propertyId`). Impostato dal parametro `state`
  dell'OAuth Stripe Connect.
- **Esempio di valori**: `246`, `81`, `232`, `10554`, `10553`

### createdAt
- **Tipo**: `datetime` — `defaultValue: Sequelize.NOW`
- **Nullabile**: Sì
- **Descrizione**: creazione riga (connessione dell'account). Gestito da Sequelize.
- **Esempio di valori**: `2023-05-24 10:46:27`, `2024-04-15 18:01:00`

### updatedAt
- **Tipo**: `datetime` — `defaultValue: Sequelize.NOW`
- **Nullabile**: Sì
- **Descrizione**: ultimo aggiornamento. Nei dati sempre uguale a `createdAt` (la riga viene creata,
  mai ri-aggiornata con successo — vedi bug su `getCustomerIdByAuthCode`).
- **Esempio di valori**: `2023-05-24 10:46:27`

---

## Osservazioni verificate nel codice (criticità)

- **Bug nell'`update`**: `getCustomerIdByAuthCode` fa `stripeConfig.update(customerCardData, {
  where: { id: req.body.userId } })` — usa `req.body.userId` (non validato, e comunque un id utente)
  come **PK della riga `stripe_configrations`**. Se la riga per quella property esiste già, la
  ri-connessione **non aggiorna nulla** (o aggiorna la riga sbagliata). Nei dati infatti `updatedAt`
  è sempre = `createdAt`.
- **Nessun `UNIQUE(propertyId)`** a schema: "una riga per stabilimento" è solo una convenzione di
  codice; il `include` al login prende comunque `stripe_configrations[0]`.
- **Nessuna FK reale** verso `properties`.
- **Nome tabella con refuso** (`configrations`) e **mix di convenzioni**: colonna `sellerStripeConnectId`
  in `camelCase`, tabella snake-ish. Nel target: `property_stripe_accounts` /
  `stripe_connected_account_id`, `snake_case`, FK reale, indice unico parziale.
- **Cancellazione a cascata implicita**: `deleteStripeAccount` cancella la riga **e** spegne
  `properties.serviceOnlineBooking` — accoppiamento tra configurazione pagamento e flag di
  prenotazione online (in due tabelle) da rendere esplicito.
- **`sellerStripeConnectId` è un id esterno** referenziato per valore da
  `online_transaction_details.stripe_connected_id`: nessun vincolo di integrità.
- Il campo non è un enumeratore.
