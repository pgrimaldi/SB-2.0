# stabilimento — Configurazione

> **Voce di menù**: icona ingranaggio/"frame" nell'header a destra (`_gotoConfigure` →
> `components/configuration/ConfigParent`, rotta `/configure`). È il **setup dello stabilimento**:
> mappa spiaggia (settori, griglia, layout), stagioni e periodi tariffari, listini (pacchetti e
> articoli aggiuntivi), sconti/voucher, servizi (bar / normali / a ore), magazzino, configurazione
> email (SMTP), Stripe, scontrino elettronico Fiskaly, stampanti fiscali.
>
> **Ruoli**: **nascosta** a `manager`, `cashoperator`, `waiter`, `advancedlifeguard`, `affiliate`,
> `lifeguard` (visibile ad `admin`/`superadmin`). Le rotte di catalogo sono protette da `isSuperAdmin`
> a livello di route, ma **senza scoping per stabilimento** (un superadmin di uno stabilimento potrebbe
> operare sui dati di un altro — vedi [`roles.md`](../../db/roles.md#2--superadmin)).
>
> **Metodo**: `managerActions.js` / `configureAction.js` / `priceAction.js` + controller
> `sectors.controller.js`, `seasons.controller.js`, `configuration.controller.js`,
> `confimaps.controller.js`, `pricelist.controller.js`, `discounts.controller.js`,
> `servicetypes.controller.js`, `services.controller.js`, `warehouse.controller.js`,
> `emailConfig.controller.js`, `printerConfig.controller.js`, `stripe.controller.js`,
> `fiskalyAccount.controller.js` + `Service/fiskalyAccountService.js`. Schede DB: sezione "Catalogo &
> configurazione" di [`_feature-tabelle.md`](../../db/_feature-tabelle.md).

---

## 1. Mappa spiaggia — settori, griglia, layout

**Azioni**: `addsectors`/`viewsectors`/`updatesectors` (`sectors_rows`), `addconfiguration`/
`viewconfiguration` (`configurations`: stato cella‑per‑cella + opzioni sharing), `addconfimaps`/
`viewconfimaps` (`configure_maps`: dimensioni griglia, icone scelte), `reset-beach-map`
(`resetBeachMap`), `activeconfimages` (icone attive dal catalogo master — vedi
[systemadmin/02](../systemadmin/02-catalogo-icone-mappa-spiaggia.md)), `confImage`.

**Logiche**:

- I settori/file sono **1 riga JSON per stabilimento** (`sectors_rows.sectors[]`, per **nome**, non
  per id — accoppiamento debole ripetuto in tutto il dominio).
- `configurations` + `configure_maps` sono **companion 1:1**: la prima tiene lo stato di ogni cella
  (libera/occupata/tipo elemento) e le opzioni sharing, la seconda il layout grafico (righe/colonne,
  URL icone copiati dal catalogo master).
- `addconfiguration` è un **upsert** che riscrive lo stato griglia; `resetBeachMap` azzera la mappa.

**Calcoli**: nessuno (dimensionamento griglia). **Eventi**: nessuno.

---

## 2. Stagioni e periodi tariffari

**Azioni**: `addseasonsperiods`/`viewseasonsperiods`/`updateseasonsperiods` (`seasons_periods`).

**Logiche**:

- 1 riga per stabilimento: confini della **stagione corrente** e **successiva**, più i **periodi
  tariffari** (`periods[]`: nome + fasce di date, es. "Alta stagione" 01/07–31/08).
- I nomi dei periodi sono la chiave (per **substring/LIKE**) con cui il motore di prezzo abbina le voci
  di `detailPrices` (vedi [`_motore-di-prezzo-e-sconti.md`](_motore-di-prezzo-e-sconti.md) §1).

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 3. Listini — pacchetti e articoli aggiuntivi

**Azioni**: `addpricelist`/`allpricelist`/`pricelistdetail`/`updatepricelist`/`deletepricelist`
(`price_lists`), `seasonsectors`, `seasonalprice`.

**Logiche**:

- Un `price_lists` è di `type = "package"` (pacchetto vendibile: articoli di magazzino inclusi con
  quantità, `MaxPeopleCount`, servizi inclusi, settori applicabili, flag `defaultPackage`/
  `enableOnlineReservation`) o `type = "additional"` (articolo venduto a parte, eventualmente
  vincolato a specifici pacchetti).
- **Prezzi**: `detailPrices[]` per periodo (`half_day_price`, `daily_price`, `weekend_price`,
  `days_7_price`, `days_14_price`, `monthly_price`, `edit_price_data[]` per deroghe su singole date),
  più `mainPrices[]` per settore; `priceCalculation` sceglie il metodo (proporzionale / a giornata /
  con weekend / logaritmico); `weekendSetting` / `monthlySetting` / `allowPublicHolidays` +
  `holidayDates[]` regolano weekend/mese/festivi.
- **Soft delete** (`isDeleted`); `allpricelist` filtra sempre `isDeleted = false`.
- I campi `setReducedPrice`, `selectedAreaItems`, `areaItemsFlag`, `isCumulativeFlag`,
  `suggestedPriceDetails` sono usati dal **frontend** / dal Revenue, non rielaborati dal backend
  (vedi [`price_lists.md`](../../db/price_lists.md)).

**Calcoli**: il calcolo del prezzo di prenotazione è a valle, lato client (motore di prezzo). Qui si
salvano solo i **parametri**.

**Eventi**: nessuno.

---

## 4. Sconti e voucher

**Azioni**: `adddiscounts`/`viewdiscounts` (`discounts` — 1 riga JSON per stabilimento con array
`items`), `checkdiscount` (validazione in prenotazione), `updateVoucherDateRange`,
`getpropertyDiscount`/`getPropertyDiscountDetails`, `getBookingDiscountList`.

**Logiche**:

- `adddiscounts` è un **upsert** per `propertyId`: riscrive l'intero array `items` (per ogni voucher:
  codice, `type` `fixed`/percentuale, `voucher_value`, validità, righe/postazioni ammesse,
  `maximunNoUse`, `totalNoOfUsage`).
- La **validazione** in prenotazione (`checkdiscount`) controlla esistenza codice, data corrente nella
  validità, utilizzi residui (`maximunNoUse − totalNoOfUsage > 0`). Il **calcolo dell'importo
  scontato** è lato client (vedi [`_motore-di-prezzo-e-sconti.md`](_motore-di-prezzo-e-sconti.md) §5).

**Calcoli**: nessuno qui. **Eventi**: nessuno.

---

## 5. Servizi (bar / normali / a ore)

**Azioni**: `addservicetypes`/`viewservicetypes` (`services_types`: i servizi effettivamente venduti
dallo stabilimento, con prezzi, varianti, fasce orarie, `maximumReservation` per gli orari),
`services` (catalogo master, sola lettura — vedi [systemadmin/03](../systemadmin/03-dati-di-piattaforma-e-lookup.md)),
`servicetypeicons`, `serviceFilter`.

**Logiche**: `addservicetypes` è un upsert dei servizi dello stabilimento; per i servizi a ore si
definiscono slot e capienza (`maximumReservation`), **verificati però solo lato client** in fase di
vendita (vedi [`docs/01`](../../../progettazione/01-criticita-e-indicazioni-backend.md) §11.8).

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 6. Magazzino

Vedi voce dedicata [08-magazzino](08-magazzino.md): `addwarehouse` (upsert dell'array `items`),
`viewwarehouse`, `warehouse/bookingdetails`.

---

## 7. Configurazione email (SMTP dello stabilimento)

**Azioni** (`emailConfig.controller.js`): `add-email-config` (`addEmailConfig`), `get-email-config`
(`getEmailConfig`), `getEmailConfigDetails`, `send-test-email` (`sendTestEmail`), `deleteEmailConfig`.

**Logiche**:

- Salva host/porta/utente/password SMTP + `fromEmail` + `status` in `bathhouse_email_configrations`
  (1 per property). Quando `status = true` e valida, **tutte** le email transazionali/newsletter/report
  della property vengono inviate da questo SMTP invece che dal mittente di default.

**Eventi**:

- **`sendTestEmail`**: crea un trasportatore Nodemailer con i parametri inseriti e **invia
  un'email di prova** all'indirizzo indicato (verifica immediata della configurazione). Nessun
  `email_logs` per il test.
- Le altre azioni non inviano nulla.

---

## 8. Stampanti fiscali (percorso legacy RT)

**Azioni** (`printerConfig.controller.js`): `savePrintersettings`, `updatePrinterConfigDetails`,
`changeStatusPrinter`, `deletePrinter`, `getPrinterConfiguration`, `saveReceiptdata`,
`getReceiptDetails`.

**Logiche**: configura le stampanti/registratori di cassa RT fisici (`printer_configs`); `saveReceiptdata`
salva i dati di uno scontrino stampato su hardware (`booking_receipts`, `receipt_serial_numbers`).
Dominio **C4bis**, mantenuto come componente separato accanto a Fiskaly (decisione di business).

**Calcoli**: numerazione seriale scontrini. **Eventi**: stampa su hardware (fuori dal backend).

---

## 9. Scontrino elettronico Fiskaly (attivazione e account)

**Azioni** (`fiskalyAccount.controller.js` + `Service/fiskalyAccountService.js`): `update-terms`/
`get-terms`, `update-account`/`get-account`, `get-cash-registers`, `electronic-receipt/fiskaly-enabled`
(`getFiskalyEnabled`), `set-fiskaly-enabled` (`setFiskalyEnabled`), `deleteEntities`, `closeAccount`,
`getFisconlineUpdatedAt`.

**Logiche**:

- Ogni azione verifica prima che `properties.fiskalyEnabled = 1` per la property dell'utente.
- Gestisce l'**account Fiskaly** dello stabilimento (dati fiscali, accettazione termini, elenco
  registratori di cassa, credenziali Fisconline).

**Eventi**: **chiamate ad API esterna Fiskaly (SIGN IT / Fisconline)** per creare/aggiornare/chiudere
l'account e leggere i registratori di cassa; salvataggi su `fiskaly_account` / `fiskaly_cash_register`
/ `keysecret`.

---

## 10. Configurazione Stripe

**Azioni** (`stripe.controller.js`): `stripe/createCustomerStripeDetails`
(`createCustomerStripeDetails`), `stripe/removeCardDetails`, `deleteStripeAccount`, `accountBalance`
(`getAccountBalance`), `getCustomerIdByAuthCode`, `findConnectedAccount`.

**Logiche**: collega l'account Stripe (Connect) dello stabilimento per incassare i pagamenti online;
gestisce le carte salvate e il saldo dell'account.

**Eventi**: **chiamate ad API esterna Stripe** (creazione customer/connected account, lettura saldo,
rimozione carte, cancellazione account).

---

## 11. Impostazioni prenotazione online

`addonlinereservation`/`viewonlinereservation` (`online_reservations`: `isEnableMap`, `period`,
`locations`, `isDisable`), `getOnlineBookingSettings`/`updateOnlineBookingSettings`,
`getPaymentOptions` (`properties.paymentOptions` / `servicebookingPaymentOptions`). Dettaglio nella
voce [12-impostazioni](12-impostazioni.md) §online.

---

## 12. Riepilogo "calcoli / logiche / eventi" della voce Configurazione

- **Calcoli**: nessun calcolo economico (si salvano solo i **parametri** che il motore di prezzo userà
  a valle); numerazione seriale scontrini (percorso RT).
- **Logiche**: upsert "1 riga JSON per stabilimento" per settori / griglia / stagioni / sconti /
  magazzino; soft‑delete listini; accoppiamento per **nome stringa** (settori, periodi); gate
  `fiskalyEnabled` per le azioni Fiskaly; `status` dello SMTP che dirotta tutte le email della property.
- **Eventi**:
  - **email di prova** SMTP (`sendTestEmail`);
  - **API esterna Fiskaly** (account/termini/registratori/chiusura);
  - **API esterna Stripe** (customer/connected account/saldo/carte);
  - stampa su hardware RT (percorso legacy).
  - Le configurazioni di mappa / stagioni / listini / sconti / servizi **non** generano eventi.
