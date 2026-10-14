# Tabella `fiskaly_account`

> Fonti: schema e dati reali (7 righe, `AUTO_INCREMENT=8`), model
> [`back-pre/app/models/fiskalyAccount.model.js`](../../back-pre/app/models/fiskalyAccount.model.js)
> (`sequelize.define("fiskaly_account", …, { freezeTableName: true, timestamps: false })`),
> scrittura/lettura in
> [`Service/fiskalyAccountService.js`](../../back-pre/app/Service/fiskalyAccountService.js)
> (`updateTerms`, `updateAccount`, `getAccount`, `getTerms`, `getFisconlineUpdatedAt`,
> `deleteEntities`, `closeAccount`, `getCashRegisters`), controller
> [`fiskalyAccount.controller.js`](../../back-pre/app/controllers/fiskalyAccount.controller.js),
> uso anche in
> [`Service/fiskalyReceiptManagerService.js`](../../back-pre/app/Service/fiskalyReceiptManagerService.js)
> (`createReceipt`, `deleteReceipt`, `createNonFiscalReceipt`, `createAutoReceiptFromStripe`) e
> [`Service/fiskalyReceiptNotificationService.js`](../../back-pre/app/Service/fiskalyReceiptNotificationService.js)
> (dati venditore per PDF/email/WhatsApp);
> crittografia password in [`utils/fiskalyEncrypt.js`](../../back-pre/app/utils/fiskalyEncrypt.js),
> chiamate all'API esterna Fiskaly SIGN IT in
> [`utils/fiskalySignIt.helper.js`](../../back-pre/app/utils/fiskalySignIt.helper.js);
> associazione `db.fiskalyAccount.belongsTo(db.property, { foreignKey: "property_id" })` /
> `db.fiskalyAccount.hasMany(db.fiskalyCashRegister, { foreignKey: "fiskaly_account_id" })` in
> [`models/index.js`](../../back-pre/app/models/index.js) (righe 334-343).
> Frontend: pannello **Fiscalization** in `front-modern-pre`
> (`src/pages/Fiscalization/`, `src/remote/SummerBooking/Fiscalization/FiscalizationRestApi.ts` →
> endpoint `/api/sb/electronic-receipt/update-terms|get-terms|update-account|get-account|
> get-cash-registers|get-fisconline-updated-at|delete-entities|close-account`);
> `front-pre` usa solo `sb/electronic-receipt/get-cash-registers`
> (`src/data/actions/fiscalizationAction.js`, flusso di stampa scontrino).
>
> **Nota di contesto** — **1 riga per `property_id`** (`findOne({ where: { property_id } })` ovunque;
> nessun vincolo `UNIQUE` a schema sulla colonna, solo un indice non-unico). Rappresenta
> l'**account dello stabilimento presso il fornitore fiscale esterno Fiskaly (SIGN IT / documento
> commerciale online italiano)**: raccoglie l'accettazione dei termini, gli identificativi delle
> quattro entità create su Fiskaly (Organization "UNIT", Subject "API_KEY", Taxpayer "COMPANY",
> Location "BRANCH"), le credenziali API restituite da Fiskaly, i dati anagrafici/fiscali del
> titolare e le credenziali **Fisconline** (Agenzia delle Entrate) necessarie per la messa in
> esercizio ("commissioning") della cassa. Non è una tabella transazionale: gli scontrini emessi
> stanno in [`fiskaly_receipt`](fiskaly_receipt.md) / [`fiskaly_non_fiscal_receipt`](fiskaly_non_fiscal_receipt.md),
> i registratori di cassa in [`fiskaly_cash_register`](fiskaly_cash_register.md).
>
> **Dominio nella riscrittura**: **C4 — modulo `Fiskaly`** (scontrino fiscale elettronico IT;
> distinto da **C3** `Payment`/Stripe). Contiene **segreti**
> (`subject_credential_key/secret`, `taxpayer_fisconline_pin`, `taxpayer_fisconline_password`) che
> vanno spostati in un secret manager (SSM/KMS), non in colonne applicative — vedi criticità.
> L'accoppiamento con `properties` è solo via `property_id`. Analisi domini in
> [`docs/03-gestione-dati.md` §4](../../docs/03-gestione-dati.md); flag `fiskalyEnabled` /
> `fiskalyFlag` / `fiskalyAutoreceipt` documentati in [`properties.md`](properties.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### accept_terms
- **Tipo**: `smallint`
- **Nullabile**: Sì
- **Default**: nessuno a schema; `getTerms` restituisce `0` quando la riga o il campo è assente
- **Descrizione**: accettazione dei termini e condizioni Fiskaly da parte del gestore. Scritto da
  `updateTerms` (crea la riga `fiskaly_account` se non esiste, con `property_id` + `createdBy`).
  È **precondizione bloccante**: `updateAccount` lancia `400 "Terms not accepted"` se
  `accept_terms !== 1`; `getCashRegisters` e `createAutoReceiptFromStripe` richiedono `=== 1`.
  Il costante `FiskalyTerms` (`fiskaly_consts.js`) definisce `ACCEPTED: 1`, `REJECTED: 0`.
- **Candidato a enumerazione**: modellare come **booleano** `terms_accepted` (`0`/`1`), non enum.
- **Esempio di valori**: `1`

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì (a schema; nel codice è sempre valorizzato = `user.propertyId`)
- **Relazioni**: FK reale `fiskaly_account_ibfk_1` → `properties.id`, `ON DELETE SET NULL ON UPDATE
  CASCADE`. Indice non-unico `fiskaly_account_property_id`.
- **Descrizione**: stabilimento proprietario dell'account fiscale. Chiave logica di ricerca in
  tutti i metodi del service (una riga per stabilimento). Filtrato anche da `getPropertyIdByBookingNumber`
  a ritroso per autorizzare le operazioni sugli scontrini.
- **Esempio di valori**: `228`, `96`, `10798`, `10723`, `10804`

### organization_type
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì (NULL finché lo step 5 non è stato completato)
- **Descrizione**: tipo dell'entità *Organization* creata su Fiskaly. `updateAccount` la crea sempre
  con valore fisso `"UNIT"` (`FiskalySignItHelper.createOrganization({ type: "UNIT", … })`).
- **Candidato a enumerazione**: **Sì**, `FiskalyOrganizationType { Unit }` — di fatto costante.
- **Esempio di valori**: `"UNIT"`, `null`

### organization_name
- **Tipo**: `varchar(128)`
- **Nullabile**: Sì
- **Descrizione**: nome assegnato all'Organization su Fiskaly. Generato dal codice come
  ``Beach cash register ${propertyId}``.
- **Esempio di valori**: `"Beach cash register 228"`, `"Beach cash register 10798"`, `null`

### organization_id
- **Tipo**: `varchar(36)` — UUID v7 restituito da Fiskaly. `UNIQUE` (`organization_id`)
- **Nullabile**: Sì
- **Descrizione**: id dell'Organization su Fiskaly (`organizationResponse.content.id`). Usato come
  `X-Scope-Identifier` per la creazione del Subject (step 6). La sua presenza è uno dei check di
  "setup completo".
- **Esempio di valori**: `"8c2e8462-158d-4a87-9625-35bc78b48c8d"`, `null`

### organization_state
- **Tipo**: `varchar(30)`
- **Nullabile**: Sì
- **Descrizione**: stato dell'Organization su Fiskaly (`organizationResponse.content.state`).
  In `closeAccount` viene forzato a `"DECOMMISSIONED"` **solo in DB** (l'API Fiskaly non supporta
  la PATCH dello stato di Organization/Subject).
- **Candidato a enumerazione**: **Sì**, `FiskalyEntityState` (vedi `taxpayer_state`), con l'aggiunta
  di `ENABLED`. Valori reali osservati: `"ENABLED"`.
- **Esempio di valori**: `"ENABLED"`, `null`

### subject_api_key_name
- **Tipo**: `varchar(30)`
- **Nullabile**: Sì
- **Descrizione**: nome del Subject di tipo `API_KEY` creato su Fiskaly, generato come
  ``my-api-key-${propertyId}``.
- **Esempio di valori**: `"my-api-key-228"`, `"my-api-key-10798"`, `null`

### subject_id
- **Tipo**: `varchar(36)` — UUID v7. `UNIQUE` (`subject_id`, doppio indice `subject_id_2`)
- **Nullabile**: Sì
- **Descrizione**: id del Subject `API_KEY` su Fiskaly (`subjectResponse.content.id`). Presenza =
  step 6 completato.
- **Esempio di valori**: `"019e83fe-…"` (UUID v7), `null`

### subject_state
- **Tipo**: `varchar(30)`
- **Nullabile**: Sì
- **Descrizione**: stato del Subject su Fiskaly. Come `organization_state`, in `closeAccount` è
  forzato a `"DECOMMISSIONED"` solo lato DB.
- **Esempio di valori**: `"ENABLED"`, `null`

### subject_credential_key
- **Tipo**: `varchar(128)` — **credenziale/segreto**
- **Nullabile**: Sì
- **Descrizione**: chiave (`credentials.key`) restituita da Fiskaly alla creazione del Subject.
  Usata come `subjectKey` per **tutte** le chiamate SIGN IT successive (creazione Taxpayer,
  Location, System, INTENTION/TRANSACTION degli scontrini). La sua assenza blocca `updateAccount`,
  `createReceipt`, `deleteReceipt`, `deleteEntities`, `closeAccount`.
- **Esempio di valori**: `"live_8armn…"` (prefisso `live_` / `test_`)

### subject_credential_secret
- **Tipo**: `varchar(128)` — **credenziale/segreto**
- **Nullabile**: Sì
- **Descrizione**: segreto (`credentials.secret`) del Subject. **Restituito da Fiskaly solo alla
  creazione** (commento esplicito nel codice), quindi non recuperabile: se si perde va rifatto il
  Subject. Salvato in chiaro nella colonna.
- **Esempio di valori**: `"Hykoxf…"` (stringa opaca)

### location_id
- **Tipo**: `varchar(36)` — UUID v7. `UNIQUE` (`location_id`, doppio indice `location_id_2`)
- **Nullabile**: Sì
- **Descrizione**: id della Location `BRANCH` su Fiskaly (step 9), figlia del Taxpayer. Necessaria
  per creare i System (registratori di cassa). Azzerata da `deleteEntities("location"|"taxpayer")`
  e `updateAccount` quando i dati indirizzo cambiano.
- **Esempio di valori**: UUID v7, `null`

### location_state
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: stato della Location su Fiskaly. Il codice gestisce le transizioni
  `ACQUIRED → COMMISSIONED` (step 9b) e `COMMISSIONED → DECOMMISSIONED` (dismissione).
- **Candidato a enumerazione**: **Sì**, `FiskalyEntityState { Acquired, Commissioned, Decommissioned }`
  (costante `FiskalyState` in `fiskaly_consts.js`).
- **Esempio di valori**: `"COMMISSIONED"`, `null`

### location_mode
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: modalità operativa della Location su Fiskaly.
- **Candidato a enumerazione**: **Sì**, `FiskalyMode { Inactive, Operative, Suspended, Degraded }`
  (costante `FiskalyMode`).
- **Esempio di valori**: `"OPERATIVE"`, `null`

### createdBy
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `users.id` (nessuna FK). È l'operatore/gestore che ha creato la riga
  (`createdBy: userId`), impostato solo in `updateTerms`/step di creazione; mai aggiornato.
- **Descrizione**: nel target va rinominato `user_operator_id` / `created_by_user_id`
  (vedi [`docs/03-gestione-dati.md` §3.2](../../docs/03-gestione-dati.md)).
- **Esempio di valori**: `514`, `199`, `3159`, `2823`, `3218`

### createdAt
- **Tipo**: `datetime`, `NOT NULL` — `defaultValue: Sequelize.NOW` (lato model)
- **Nullabile**: No
- **Descrizione**: timestamp di creazione riga (Sequelize non gestisce i timestamp automatici:
  `timestamps: false`, quindi il valore è quello del `defaultValue`).
- **Esempio di valori**: `2026-06-01 15:44:59`, `2026-06-29 07:40:30`

### updatedAt
- **Tipo**: `datetime`, `NOT NULL` — `defaultValue: Sequelize.NOW`
- **Nullabile**: No
- **Descrizione**: aggiornato manualmente da Sequelize a ogni `account.update(...)` (aggiornamento
  parziale di singole colonne). **Non** è un `ON UPDATE CURRENT_TIMESTAMP` a schema.
- **Esempio di valori**: `2026-06-11 14:21:59`

### taxpayer_id
- **Tipo**: `varchar(36)` — UUID v7. `UNIQUE` (`taxpayer_id`)
- **Nullabile**: Sì
- **Descrizione**: id del Taxpayer `COMPANY` su Fiskaly (step 8). Rappresenta il soggetto passivo
  IVA italiano registrato presso Fiskaly per la fatturazione/documento commerciale. Azzerato da
  `deleteEntities("taxpayer")` e in caso di cambio dati fiscali (cascata di decommission).
- **Esempio di valori**: UUID v7, `null`

### taxpayer_legal_name
- **Tipo**: `varchar(128)`
- **Nullabile**: Sì (campo **obbligatorio in input** a `updateAccount`)
- **Descrizione**: ragione sociale legale del titolare, inviata a Fiskaly come `name.legal` e usata
  nel PDF scontrino come nome venditore (fallback di `taxpayer_trade_name`).
- **Esempio di valori**: `"VA.NA.VE. SRL"`

### taxpayer_trade_name
- **Tipo**: `varchar(128)`
- **Nullabile**: Sì (default = `taxpayer_legal_name` se non fornito)
- **Descrizione**: nome commerciale/insegna, inviato a Fiskaly come `name.trade` e come `name` della
  Location; è il **nome venditore preferito** nel PDF scontrino e nei messaggi email/WhatsApp.
- **Esempio di valori**: `"Beach Resort"`

### taxpayer_address_street_name
- **Tipo**: `varchar(128)`
- **Nullabile**: Sì (obbligatorio in input)
- **Descrizione**: via/indirizzo del titolare, inviato a Fiskaly (`address.line.street`) e usato nel
  PDF/messaggi.
- **Esempio di valori**: `"Via Giuseppe Mazzini"`

### taxpayer_address_street_number
- **Tipo**: `varchar(128)`
- **Nullabile**: Sì (default `"SNC"` quando inviato a Fiskaly)
- **Descrizione**: numero civico. Persistito negli step 8/9. Concatenato a `street_name` nel PDF.
- **Esempio di valori**: `"4"`, `"SNC"`, `null`

### taxpayer_address_postal_code
- **Tipo**: `varchar(128)`
- **Nullabile**: Sì (obbligatorio in input; validato come CAP italiano a 5 cifre — `isValidCAP`)
- **Descrizione**: CAP, inviato a Fiskaly (`address.code`).
- **Esempio di valori**: `"98061"`

### taxpayer_address_city
- **Tipo**: `varchar(128)`
- **Nullabile**: Sì (obbligatorio in input)
- **Descrizione**: comune, inviato a Fiskaly (`address.city`).
- **Esempio di valori**: `"Brolo"`

### taxpayer_address_country
- **Tipo**: `varchar(2)` — ISO 3166-1 alpha-2
- **Nullabile**: Sì (obbligatorio in input; default `"IT"`; validato da `isValidCountryCode`)
- **Descrizione**: nazione, inviata a Fiskaly (`address.country`). Nel PDF viene forzato `"ITA"`.
- **Candidato a enumerazione**: sì, insieme chiuso ISO country codes (tabella/`enum` condivisa).
- **Esempio di valori**: `"IT"`

### taxpayer_fiscal_code
- **Tipo**: `varchar(16)` — **dato fiscale**
- **Nullabile**: Sì (obbligatorio in input; validato come Codice Fiscale *o* Partita IVA)
- **Descrizione**: codice fiscale/partita IVA del soggetto, inviato a Fiskaly come
  `fiscalization.tax_id_number`. Nel PDF è fallback di `taxpayer_vat_number` come "tax_number".
- **Esempio di valori**: `"02624340838"`

### taxpayer_vat_number
- **Tipo**: `varchar(11)` — **dato fiscale**
- **Nullabile**: Sì (obbligatorio in input; validato come Partita IVA italiana, `isValidPartitaIVA`)
- **Descrizione**: partita IVA, inviata a Fiskaly come `fiscalization.vat_id_number`; è il
  `tax_number` mostrato nel PDF scontrino / messaggi.
- **Esempio di valori**: `"02624340838"`

### taxpayer_fisconline_fiscal_code
- **Tipo**: `varchar(16)` — **credenziale AdE**
- **Nullabile**: Sì (obbligatorio in input; **sempre un Codice Fiscale persona fisica**, mai P.IVA —
  validato da `isValidCodiceFiscale`)
- **Descrizione**: codice fiscale della persona abilitata all'accesso a Fisconline dell'Agenzia
  delle Entrate. Inviato a Fiskaly dentro `fiscalization.credentials` (type `FISCONLINE`) per
  autenticare la messa in esercizio del Taxpayer.
- **Esempio di valori**: `"LPSMLS68E58D825C"`

### taxpayer_fisconline_pin
- **Tipo**: `varchar(20)` — **credenziale AdE, salvata in CHIARO**
- **Nullabile**: Sì (obbligatorio solo finché il setup non è stato completato una volta)
- **Descrizione**: PIN Fisconline. Inviato a Fiskaly come `credentials.pin`. **Salvato in chiaro**
  nella colonna (a differenza della password). Aggiornabile con flusso `SUSPEND → update →
  OPERATIVE` sul Taxpayer.
- **Esempio di valori**: `"9690162900"` (10 cifre)

### taxpayer_fisconline_password
- **Tipo**: `varchar(255)` — **credenziale AdE, cifrata**
- **Nullabile**: Sì (obbligatorio solo finché il setup non è stato completato una volta)
- **Descrizione**: password Fisconline, **cifrata AES-256-GCM** con
  `FISKALY_FISCONLINE_ENCRYPTION_KEY` (`utils/fiskalyEncrypt.js`) prima del salvataggio; decifrata
  in memoria quando va inviata a Fiskaly. `getAccount` la omette volutamente dalla risposta.
- **Esempio di valori**: stringa cifrata di ~54 caratteri (es. inizia con `"Ji0+hen598SN…"`)

### fisconline_updated_at
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: data dell'ultima modifica di PIN o password Fisconline. Impostata alla creazione
  del Taxpayer e a ogni cambio password. Esposta dall'endpoint `get-fisconline-updated-at` (il
  frontend la mostra come "credenziali aggiornate il …").
- **Esempio di valori**: `2026-06-01 16:22:59`, `null`

### taxpayer_state
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: stato del Taxpayer su Fiskaly. Il codice gestisce `ACQUIRED` (mai commissionato,
  tipicamente credenziali Fisconline errate → va abbandonato e ricreato), `COMMISSIONED` (operativo,
  unico stato da cui si può passare a `DECOMMISSIONED`).
- **Candidato a enumerazione**: **Sì**, `FiskalyEntityState { Acquired, Commissioned, Decommissioned }`.
- **Esempio di valori**: `"COMMISSIONED"`, `null`

### taxpayer_mode
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: modalità del Taxpayer. Il codice usa `OPERATIVE` ↔ `SUSPENDED` per il flusso di
  aggiornamento credenziali Fisconline; `INACTIVE` per un Taxpayer in `ACQUIRED` non commissionato.
- **Candidato a enumerazione**: **Sì**, `FiskalyMode { Inactive, Operative, Suspended, Degraded }`.
- **Esempio di valori**: `"OPERATIVE"`, `null`

### decommissioned_at
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: data di **chiusura account** (`closeAccount`). Quando valorizzato, `getAccount`
  e `getCashRegisters` rispondono come se l'account non esistesse; un successivo `updateAccount`
  lo azzera e ricrea da zero tutte le entità Fiskaly (gli id vecchi puntano a oggetti
  DECOMMISSIONED e non sono riutilizzabili). Nel campione: sempre `NULL`.
- **Esempio di valori**: `null` (in tutte le 7 righe), `2026-07-15 10:00:00` (atteso)

### sw_cash_register_default
- **Tipo**: `tinyint`, `NOT NULL`, `DEFAULT 1`
- **Nullabile**: No
- **Descrizione**: `1` = il **registratore di cassa software** auto-creato (step 10a) è quello di
  default per lo stabilimento; `0` = il default è un registratore di cassa definito dall'utente.
  Quando è `1`, `updateAccount` rifiuta `is_default = 1` sulle casse utente e forza `is_default = 0`
  su tutte le casse non-software. `deleteEntities` lo rimette a `1`. Esposto in API come booleano.
- **Candidato a enumerazione**: **booleano** (`software_cash_register_is_default`), non enum.
- **Esempio di valori**: `1` (in tutte le 7 righe)

---

## Osservazioni verificate nel codice (criticità)

- **Segreti in colonne applicative**: `subject_credential_key`, `subject_credential_secret`
  (in chiaro), `taxpayer_fisconline_pin` (in chiaro), `taxpayer_fisconline_password` (cifrata solo
  con chiave da env). Nel target vanno in un secret manager (AWS Secrets Manager / SSM SecureString
  / KMS), con la tabella che conserva al più un ARN/riferimento. Il PIN Fisconline **non** è cifrato
  come la password: incoerenza.
- **Nessun `UNIQUE(property_id)`** a schema: la semantica "una riga per stabilimento" è garantita
  solo dal codice (`findOne`). Aggiungere indice unico (parziale `WHERE deleted_at IS NULL` nel
  target).
- **Doppio soft-delete concettuale**: `decommissioned_at` è un soft-delete di fatto, ma la tabella
  non ha `deleted_at`; `fiskaly_cash_register` invece ha `deleted_at`. Uniformare
  ([`docs/03-gestione-dati.md` §3.5](../../docs/03-gestione-dati.md)).
- **Timestamp non automatici**: `timestamps: false` + `defaultValue: NOW`; `updatedAt` è aggiornato
  a mano nelle `account.update(...)`. Nessun trigger `ON UPDATE`.
- **Molti stati come `varchar` liberi** (`*_state`, `*_mode`) passthrough di valori Fiskaly, senza
  `ENUM`/`CHECK` — vedi Enumerazioni.
- **Stato Organization/Subject "finto" in `closeAccount`**: l'API Fiskaly non supporta la PATCH di
  quegli stati, quindi `organization_state`/`subject_state` sono portati a `"DECOMMISSIONED"` solo
  in DB, divergendo da Fiskaly.
- **`createdBy` senza FK** e con nome ambiguo (vedi [`docs/03-gestione-dati.md` §3.2](../../docs/03-gestione-dati.md)).
- **`taxpayer_address_*` come `varchar(128)`** anche per CAP (5 cifre) e numero civico: dimensioni
  sovradimensionate e non tipizzate.

---

## Nota per la riscrittura: consolidare qui i flag Fiskaly di `properties`

Oggi lo **stato di configurazione fiscale** di uno stabilimento è spezzato su **4 campi in 2
tabelle**, con l'accoppiamento imposto a mano nei controller:

| Campo | Tabella | Valori | Chi lo impone |
|---|---|---|---|
| `accept_terms` | `fiskaly_account` | `0` / `1` | `updateTerms`; precondizione di `updateAccount`, `getCashRegisters`, auto-scontrino |
| `fiskalyEnabled` | `properties` (`int`, def. `0`) | `0` / `1` — interruttore master | `setFiskalyEnabled` (SuperAdmin); se `0` forza `fiskalyAutoreceipt = 0` |
| `fiskalyAutoreceipt` | `properties` (`int`, def. `0`) | `0` / `1` — auto-scontrino post-Stripe | `setFiskalyEnabled`; non può essere `1` se `fiskalyEnabled = 0` |
| `fiskalyFlag` | `properties` (`int`, def. `0`) | `0` NOT_CONFIGURED / `1` VAT_CONFIGURED / `2` FISKALY_CONFIGURED | `updateFiskalyFlag` (`property.controller.js`, `VALID_FISKALY_FLAGS = [0,1,2]`); `getCashRegisters` rifiuta `404` se `fiskalyFlag ∈ {0,1}` |

Nei dati reali (`properties`) i 4 flag sono **già incoerenti fra loro**: es. 3 righe con
`fiskalyFlag = 0` (NOT_CONFIGURED) ma `fiskalyEnabled = 1` e `fiskalyAutoreceipt = 1`; 1 riga
`fiskalyFlag = 2` con `fiskalyAutoreceipt = 0`. (Distribuzione: `(0,0,0)`×91, `(2,1,1)`×4,
`(0,1,1)`×3, `(1,1,1)`×1, `(2,1,0)`×1.)

**Proposta**: nel modulo `Fiskaly` (C4) questi flag appartengono all'**account Fiskaly**, non a
`properties`. Portarli tutti sulla riga `fiskaly_account` (1:1 con lo stabilimento nel modulo),
integrandoli con `accept_terms`:

- `terms_accepted` (bool) ← `accept_terms`
- `is_enabled` (bool) ← `fiskalyEnabled` — interruttore master esplicito (resta gestibile da
  SuperAdmin)
- `autoreceipt_enabled` (bool) ← `fiskalyAutoreceipt` — con invariante applicativa
  `autoreceipt_enabled ⇒ is_enabled`
- `fiskalyFlag` **non** come intero arbitrario memorizzato:
  - il livello **`2` FISKALY_CONFIGURED è derivabile**: `terms_accepted AND is_enabled AND
    organization_id AND subject_id AND taxpayer_id AND location_id AND` ≥ 1 cassa `COMMISSIONED`
    (è già esattamente il check `isSetupComplete` di `updateAccount` + il gate di `getCashRegisters`);
  - resta esplicito solo il livello **`1` VAT_CONFIGURED** ("gestione IVA senza Fiskaly"), che non è
    deducibile → un flag/`enum` `FiscalSetupLevel { NotConfigured, VatOnly, FiskalyActive }` dove
    `FiskalyActive` è calcolato, non scritto.

Effetto: un solo punto di verità per "a che punto è la configurazione fiscale di questo
stabilimento", nessun flag scrivibile che possa divergere dallo stato reale delle entità Fiskaly, e
`properties` non porta più campi di un dominio che non è il suo.
