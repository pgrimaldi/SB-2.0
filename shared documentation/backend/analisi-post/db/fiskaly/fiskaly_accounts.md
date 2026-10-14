# Tabella `fiskaly_accounts`

> **Dominio**: C4 — `fiskaly` · **sottoarea**: — (dominio senza sottoaree) · **schema**: `fiskaly` ·
> **nome flat**: `fiskaly_accounts` · **forma canonica**: `fiskaly.accounts`.
>
> Sostituisce [`fiskaly_account`](../../../analisi-pre/db/fiskaly_account.md) (7 righe reali;
> integrazione SIGN IT in produzione da giugno 2026) **+ consolida** i 3 flag fiscali oggi su
> [`properties`](../../../analisi-pre/db/properties.md) (`fiskalyEnabled`, `fiskalyAutoreceipt`,
> `fiskalyFlag`). Fonti:
> [`analisi-pre/db/fiskaly_account.md`](../../../analisi-pre/db/fiskaly_account.md) (§"Nota per la
> riscrittura: consolidare qui i flag Fiskaly di `properties`"),
> [`analisi-pre/db/properties.md`](../../../analisi-pre/db/properties.md),
> [`docs/03-gestione-dati.md` §2.4ter, §3.1bis, §3.5, §6](../../../progettazione/03-gestione-dati.md),
> [`docs/17-secrets.md`](../../../progettazione/17-secrets.md),
> [`docs/80-retention.md`](../../../progettazione/80-retention.md).
>
> **Nota sulla voce "Legacy"**: per ogni campo indica la colonna legacy di origine
> (`nome_tabella.campo`); `— (nuovo)` = campo senza corrispondente legacy diretto.

---

> ### ✅ Applicato nel backend (2026-09-29) — `terms_accepted` eliminato, resta la data
>
> La colonna `terms_accepted` **non esiste più**: l'accettazione dei termini è ora
> **`terms_accepted_at timestamptz NULL`** e basta — `NULL` = non accettati. Migration
> `CollapseRedundantBooleanFlags` (modulo `Module.Fiskaly`), da
> [`docs/07-ottimizzazioni-db-2.md`](../../../progettazione/07-ottimizzazioni-db-2.md) §A.1.
>
> Erano due colonne per lo stesso fatto (`terms_accepted ≡ terms_accepted_at IS NOT NULL`), che il
> vecchio schema permetteva di contraddire: `true` + `NULL` («ha accettato, non si sa quando») e
> `false` + data valorizzata. Nessun `CHECK` lo impediva, e la precondizione bloccante di
> `updateAccount` / `getCashRegisters` / auto-scontrino dipendeva da quale delle due leggeva il codice.
>
> Il `CHECK` della catena di attivazione è stato riscritto di conseguenza — e poi **ridotto**: vedi il
> riquadro successivo, la catena monotòna che conteneva era un'invariante falsa.
>
> **I dati sono stati travasati, non reimpostati.** La migration, *prima* di eliminare la colonna,
> deduce la data da `created_at` dove il flag era `true` senza data, e azzera la data dove il flag era
> `false` (il booleano era la fonte letta dal codice, quindi vince lui). Verificato su
> `summer_booking_dev`: 7 account, tutti con `terms_accepted = true` **e** `terms_accepted_at`
> valorizzato già prima — nessuna divergenza, 7 date intatte dopo.

> ### ✅ Applicato nel backend (2026-09-29) — la "catena di attivazione" era un'invariante falsa
>
> Il `CHECK ck_fiskaly_accounts_activation_chain` conteneva anche
> `(is_enabled = false OR terms_accepted_at IS NOT NULL)` — cioè «Fiskaly abilitato ⇒ termini
> accettati». **Quella clausola è stata rimossa**: resta solo
>
> ```sql
> CHECK (autoreceipt_enabled = false OR is_enabled = true)
> ```
>
> Migration `RelaxFiskalyActivationChain` (modulo `Module.Fiskaly`).
>
> **Perché era sbagliata.** I due atti hanno **attori e ordine diversi**, confermati dal committente il
> 2026-09-29:
>
> 1. il **SystemAdmin** abilita Fiskaly per lo stabilimento (`is_enabled`, e può impostare anche
>    `autoreceipt_enabled`);
> 2. **poi** il **gestore** accetta termini e condizioni (`terms_accepted_at`).
>
> Quindi `is_enabled = true` con `terms_accepted_at IS NULL` è lo stato **normale e transitorio** fra il
> passo 1 e il passo 2 — non un errore. Il vecchio `CHECK` lo vietava, rendendo impossibile a DB la
> sequenza reale di attivazione. Il difetto era **preesistente** e nella forma precedente
> (`is_enabled = false OR terms_accepted = true`), quindi non introdotto dal collasso del flag: solo
> reso visibile mentre lo si riscriveva.
>
> **L'accettazione dei termini resta una precondizione bloccante, ma a runtime** (`updateAccount`,
> `getCashRegisters`, emissione dell'auto-scontrino) e nella derivazione di
> `fiscal_setup_level = fiskaly_active`. Non è un'invariante di tabella: la tabella deve poter
> rappresentare lo stato intermedio, è il codice che rifiuta le operazioni finché i termini mancano.
>
> **Effetto collaterale utile sull'ETL**: il travaso non può più violare il vincolo. L'utility ricava
> `is_enabled` da `properties.fiskalyEnabled` senza guardare `accept_terms`; con il vecchio `CHECK` una
> property abilitata e con termini non accettati avrebbe fatto fallire l'`INSERT`.

## Descrizione

**Una riga = l'account fiscale di uno stabilimento presso il fornitore esterno Fiskaly (SIGN IT —
documento commerciale elettronico italiano)**. Raccoglie: accettazione termini, i 4 identificativi
delle entità create su Fiskaly (Organization `UNIT`, Subject `API_KEY`, Taxpayer `COMPANY`, Location
`BRANCH`) con i loro stati, l'anagrafica/dati fiscali del titolare congelati per l'emissione, i
**riferimenti** (non i valori) alle credenziali nel secret manager, e lo **stato di configurazione
fiscale** dello stabilimento — oggi spezzato su 4 campi in 2 tabelle.

**1 riga per `property_id`** (nel legacy garantito solo dal codice `findOne`; qui `UNIQUE` parziale).

Correzioni rispetto al legacy:

- **Segreti in colonne applicative** (`subject_credential_key`/`subject_credential_secret` in chiaro,
  `taxpayer_fisconline_pin` in chiaro, `taxpayer_fisconline_password` cifrata con chiave da env) →
  **secret manager** (AWS Secrets Manager / SSM SecureString). La tabella tiene solo un **riferimento**
  (`*_secret_ref`, ARN/nome), **zero materiale segreto**. Non si usa un hash: i valori vanno rimandati
  **in chiaro** a Fiskaly a ogni chiamata → serve protezione *reversibile*, non un digest
  ([`docs/17-secrets.md`](../../../progettazione/17-secrets.md), decisione **C4-2** in
  [`_feature-tabelle.md`](../_feature-tabelle.md)).
- **Stato fiscale sparso** su `fiskaly_account.accept_terms` + `properties.fiskalyEnabled` /
  `fiskalyAutoreceipt` / `fiskalyFlag` (0/1/2), **già incoerenti fra loro nei dati** → consolidati qui
  (decisione **C4-3**): `terms_accepted_at` + `is_enabled` + `autoreceipt_enabled` (con
  `autoreceipt_enabled ⇒ is_enabled`) e `fiscal_setup_level` enum (`fiskaly_active` **derivato**, non scrivibile —
  vedi campo).
- **`decommissioned_at`** (soft-delete di fatto, ma senza `deleted_at` — incoerente con
  `fiskaly_cash_register` che ha `deleted_at`) → `deleted_at` uniforme
  ([`docs/03 §3.5`](../../../progettazione/03-gestione-dati.md)).
- **`*_state` / `*_mode` come `varchar` liberi** (passthrough SIGN IT) → `enum` PG dedicati
  ([`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)). Organization/Subject non hanno il ciclo
  `ACQUIRED` → enum più ridotto (`fiskaly.org_state`).
- **`createdBy int` senza FK, nome ambiguo** → `created_by_user_id` / `updated_by_user_id` `uuid`
  ([`docs/03 §3.2`](../../../progettazione/03-gestione-dati.md)).
- **`taxpayer_address_*` tutti `varchar(128)`** (anche CAP e civico) → dimensioni reali.
- **`timestamps: false` + `updatedAt` a mano** → `created_at`/`updated_at` con trigger/interceptor
  ([`docs/03 §3.4`](../../../progettazione/03-gestione-dati.md)).

## Verifica di esclusività dei flag

| Gruppo | Campi | Esito |
|---|---|---|
| Stato di configurazione | `fiscal_setup_level` | **enum** (valori mutuamente esclusivi: `not_configured` / `vat_only` / `fiskaly_active`). Non 3 booleani. |
| Interruttori di attivazione | `is_enabled`, `autoreceipt_enabled` | **una sola dipendenza strutturale**: `autoreceipt_enabled ⇒ is_enabled` (`CHECK`) — l'auto-scontrino è una sotto-opzione di Fiskaly attivo. **Non** un `enum[]`: non sono "flag dello stesso argomento" attivabili in modo indipendente. |
| Accettazione dei termini | `terms_accepted_at` | **non** fa parte della catena: è una **data** (non un booleano) e non è prerequisito dell'abilitazione, perché i due atti hanno **attori e ordine diversi** — vedi il riquadro in testa alla scheda. Il gate sui termini è un controllo a **runtime**, non un'invariante di tabella. |
| Ciclo di vita entità Fiskaly | `taxpayer_state` / `taxpayer_mode` (idem `location_*`) | **due enum ortogonali**: `state` = ciclo (`acquired`/`commissioned`/`decommissioned`), `mode` = operatività (`inactive`/`operative`/`suspended`/`degraded`). Servono entrambi (il codice li combina). |

## Snapshot

Nessuno. Questa riga è **configurazione/anagrafica viva** (segue le modifiche del titolare). I dati
anagrafici del venditore **congelati** al momento dell'emissione vivono su
[`fiskaly_receipts`](fiskaly_receipts.md) (`seller_*_snapshot`, accanto alla FK viva
`fiskaly_account_id`).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_fiskaly_accounts` | `id` | PK | — |
| `uq_fiskaly_accounts_public_id` | `(public_id)` | UNIQUE | export GDPR / URL pannello Fiscalization |
| `uq_fiskaly_accounts_property` | `(property_id) WHERE deleted_at IS NULL` | UNIQUE parziale | **1 account per stabilimento** (mancava nel legacy) |
| `uq_fiskaly_accounts_organization` | `(organization_id) WHERE organization_id IS NOT NULL` | UNIQUE parziale | id entità Fiskaly univoco |
| `uq_fiskaly_accounts_subject` | `(subject_id) WHERE subject_id IS NOT NULL` | UNIQUE parziale | idem |
| `uq_fiskaly_accounts_taxpayer` | `(taxpayer_id) WHERE taxpayer_id IS NOT NULL` | UNIQUE parziale | idem |
| `uq_fiskaly_accounts_location` | `(location_id) WHERE location_id IS NOT NULL` | UNIQUE parziale | idem |
| `ix_fiskaly_accounts_setup_level` | `(fiscal_setup_level) WHERE deleted_at IS NULL` | btree parziale | cruscotto SuperAdmin "a che punto è la fiscalità degli stabilimenti" |

## Campi

### id
- **Legacy**: `fiskaly_account.id`
- **Descrizione**: PK tecnica. Id legacy preservati 1:1 in migrazione.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `fiskaly_cash_registers.account_id`,
  `fiskaly_receipts.fiskaly_account_id`.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile (UUIDv7). La riga contiene **dati personali** (codice fiscale
  della persona fisica abilitata a Fisconline) e dati fiscali del titolare → esposta all'export GDPR e
  al pannello *Fiscalization* del gestionale.
- **Tipo**: `uuid`.
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`. Nessun altro modulo la referenzia per valore (gli scontrini di C4 la
  raggiungono via FK interna).

### property_id
- **Legacy**: `fiskaly_account.property_id` (`int`, FK `properties.id` `ON DELETE SET NULL`)
- **Descrizione**: stabilimento proprietario dell'account fiscale.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2
  [`catalog_properties`](../catalog/catalog_properties.md); [D-01](../_domande-committente.md). Prima
  colonna di ogni indice composito multi-tenant.

### fiscal_setup_level
- **Legacy**: `properties.fiskalyFlag` (`int`, `0` NOT_CONFIGURED / `1` VAT_CONFIGURED / `2`
  FISKALY_CONFIGURED)
- **Descrizione**: stato di configurazione fiscale dello stabilimento. **Mantenuto e fatto avanzare
  dall'applicazione** alle transizioni (setup completo / decommission) — **non** un intero
  liberamente scrivibile come nel legacy (che nei dati diverge dallo stato reale delle entità):
  - `not_configured` — nessuna gestione fiscale;
  - `vat_only` — "gestione IVA senza Fiskaly" (ex `1`): **non derivabile**, resta esplicito;
  - `fiskaly_active` — **derivato**: `terms_accepted_at IS NOT NULL AND is_enabled AND organization_id AND subject_id
    AND taxpayer_id AND location_id AND` ≥ 1 cassa `commissioned` (= il check `isSetupComplete` +
    gate `getCashRegisters`). Si può materializzare come colonna generata o calcolare a runtime.
- **Tipo**: `enum fiskaly.fiscal_setup_level` — valori: `not_configured`, `vat_only`, `fiskaly_active`.
- **Default**: `not_configured`.
- **Nullabile**: No.
- **Relazioni**: `getCashRegisters` legacy rifiuta `404` se `∈ {not_configured, vat_only}`.

### terms_accepted_at
- **Legacy**: `fiskaly_account.accept_terms` (`smallint` `0`/`1`; `FiskalyTerms.ACCEPTED = 1`); l'istante
  è dedotto in migrazione da `fiskaly_account.createdAt` se `accept_terms = 1`.
- **Descrizione**: quando il gestore ha accettato termini e condizioni Fiskaly; `NULL` = non accettati.
  **Precondizione bloccante a runtime** di `updateAccount` / `getCashRegisters` / auto-scontrino, che si
  valuta come `terms_accepted_at IS NOT NULL`. **Non** è un vincolo di tabella: l'account può
  legittimamente essere `is_enabled = true` con i termini non ancora accettati (vedi riquadro in testa).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: nessun `CHECK` la lega a `is_enabled` (vedi §"Verifica di esclusività" e il riquadro
  in testa alla scheda).

### is_enabled
- **Legacy**: `properties.fiskalyEnabled` (`int` def. `0`)
- **Descrizione**: interruttore **master** della fiscalità elettronica, gestito da SuperAdmin
  (`setFiskalyEnabled`). Se `false`, forza `autoreceipt_enabled = false`. **Può essere `true` con
  `terms_accepted_at` ancora `NULL`**: l'abilitazione precede l'accettazione dei termini da parte del
  gestore (vedi riquadro in testa alla scheda).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: `CHECK (autoreceipt_enabled = false OR is_enabled = true)`.

### autoreceipt_enabled
- **Legacy**: `properties.fiskalyAutoreceipt` (`int` def. `0`)
- **Descrizione**: emissione **automatica** dello scontrino dopo un pagamento Stripe andato a buon
  fine (`createAutoReceiptFromStripe`).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: invariante `autoreceipt_enabled ⇒ is_enabled` (vincolo `CHECK`).

### organization_id / subject_id / taxpayer_id / location_id
- **Legacy**: `fiskaly_account.organization_id` / `subject_id` / `taxpayer_id` / `location_id`
  (`varchar(36)`, UUID v7 restituiti da Fiskaly, `UNIQUE`)
- **Descrizione**: identificativi opachi delle 4 entità create su Fiskaly SIGN IT. La loro presenza è
  parte del check "setup completo". Azzerati dalle cascate di decommission / `deleteEntities`.
- **Tipo**: `varchar(36)` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì (finché lo step relativo non è completato).
- **Relazioni**: identificativi esterni Fiskaly (non enum). `UNIQUE` parziale ciascuno.

### organization_name
- **Legacy**: `fiskaly_account.organization_name` (`varchar(128)`, generato ``Beach cash register ${propertyId}``)
- **Descrizione**: nome assegnato all'Organization su Fiskaly.
- **Tipo**: `varchar(128)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### subject_api_key_name
- **Legacy**: `fiskaly_account.subject_api_key_name` (`varchar(30)`, generato ``my-api-key-${propertyId}``)
- **Descrizione**: nome del Subject `API_KEY` su Fiskaly.
- **Tipo**: `varchar(40)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### organization_state / subject_state
- **Legacy**: `fiskaly_account.organization_state` / `subject_state` (`varchar(30)`; `closeAccount`
  forza `DECOMMISSIONED` **solo in DB**)
- **Descrizione**: stato di Organization / Subject su Fiskaly (Fiskaly non supporta la PATCH di questi
  stati: quando l'account si chiude, il valore locale può divergere dal remoto — comportamento
  legacy conservato).
- **Tipo**: `enum fiskaly.org_state` — valori: `enabled`, `decommissioned`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: enum ridotto (Organization/Subject non hanno il ciclo `acquired`).

### taxpayer_state / location_state
- **Legacy**: `fiskaly_account.taxpayer_state` / `location_state` (`varchar(50)`)
- **Descrizione**: stato del Taxpayer / della Location su Fiskaly. `commissioned` è l'unico stato da
  cui si può passare a `decommissioned`.
- **Tipo**: `enum fiskaly.entity_state` — valori: `acquired`, `commissioned`, `decommissioned`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: costante `FiskalyState` di `fiskaly_consts.js` portata a enum.

### taxpayer_mode / location_mode
- **Legacy**: `fiskaly_account.taxpayer_mode` / `location_mode` (`varchar(50)`)
- **Descrizione**: modalità operativa del Taxpayer / Location su Fiskaly. Il flusso di aggiornamento
  credenziali Fisconline usa `operative ↔ suspended`.
- **Tipo**: `enum fiskaly.entity_mode` — valori: `inactive`, `operative`, `suspended`, `degraded`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: costante `FiskalyMode` portata a enum.

### subject_credentials_secret_ref
- **Legacy**: `fiskaly_account.subject_credential_key` + `subject_credential_secret` (**entrambi in
  chiaro** in colonna)
- **Descrizione**: **riferimento** (ARN / nome SSM) a **un** segreto il cui valore è il JSON
  `{ "key": "…", "secret": "…" }` del Subject, usato come `subjectKey` per **tutte** le chiamate
  SIGN IT. **Non** contiene PIN/password Fisconline (quelli sono in
  `fisconline_credentials_secret_ref`). Il `secret` è restituito da Fiskaly **solo alla creazione**
  → in pratica write-once; se si perde va ricreato il Subject.
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (valorizzato solo dopo lo step 6).
- **Relazioni**: punta a un segreto in AWS Secrets Manager / SSM. **Nessun materiale segreto in
  questa tabella** ([`docs/17-secrets.md`](../../../progettazione/17-secrets.md), C4-2).

### taxpayer_legal_name
- **Legacy**: `fiskaly_account.taxpayer_legal_name` (`varchar(128)`, obbligatorio in input)
- **Descrizione**: ragione sociale legale del titolare (`name.legal` verso Fiskaly).
- **Tipo**: `varchar(128)`.
- **Default**: nessuno.
- **Nullabile**: Sì (a schema; sempre valorizzato dopo il setup).
- **Relazioni**: fonte degli snapshot venditore su `fiskaly_receipts`.

### taxpayer_trade_name
- **Legacy**: `fiskaly_account.taxpayer_trade_name` (`varchar(128)`, default = `taxpayer_legal_name`)
- **Descrizione**: nome commerciale / insegna (`name.trade`). Nome venditore **preferito** nel PDF
  scontrino e nei messaggi.
- **Tipo**: `varchar(128)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.

### taxpayer_vat_number
- **Legacy**: `fiskaly_account.taxpayer_vat_number` (`varchar(11)`, P.IVA italiana)
- **Descrizione**: partita IVA (`fiscalization.vat_id_number`). `tax_number` nel PDF.
- **Tipo**: `varchar(13)` (spazio per prefisso paese se in futuro non-IT).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **dato fiscale**.

### taxpayer_fiscal_code
- **Legacy**: `fiskaly_account.taxpayer_fiscal_code` (`varchar(16)`, CF o P.IVA)
- **Descrizione**: codice fiscale / partita IVA del soggetto passivo (`fiscalization.tax_id_number`).
- **Tipo**: `varchar(16)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **dato fiscale**.

### taxpayer_address_street / _street_number / _postal_code / _city
- **Legacy**: `fiskaly_account.taxpayer_address_street_name` / `_street_number` / `_postal_code` /
  `_city` (tutti `varchar(128)`)
- **Descrizione**: indirizzo del titolare inviato a Fiskaly (`address.line.street` / `.code` /
  `.city`) e stampato sul PDF. `_street_number` default `"SNC"` quando inviato.
- **Tipo**: `varchar(128)` / `varchar(16)` / `varchar(5)` / `varchar(128)`.
- **Default**: nessuno (`_street_number`: `'SNC'` applicativo).
- **Nullabile**: Sì.
- **Relazioni**: `_postal_code` validato come CAP italiano (5 cifre).

### taxpayer_address_country_code
- **Legacy**: `fiskaly_account.taxpayer_address_country` (`varchar(2)`, default `"IT"`)
- **Descrizione**: nazione ISO 3166-1 alpha-2 (`address.country`).
- **Tipo**: `char(2)`.
- **Default**: `'IT'`.
- **Nullabile**: No.
- **Relazioni**: insieme chiuso ISO — **`SharedKernel`** ([`docs/03 §2.4ter`](../../../progettazione/03-gestione-dati.md)).

### fisconline_holder_fiscal_code
- **Legacy**: `fiskaly_account.taxpayer_fisconline_fiscal_code` (`varchar(16)`, **sempre CF persona
  fisica**)
- **Descrizione**: codice fiscale della persona abilitata all'accesso Fisconline (Agenzia delle
  Entrate). **Dato personale** (possibilmente di un soggetto diverso dagli account operativi) →
  rilevante per l'export GDPR.
- **Tipo**: `varchar(16)`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: inviato a Fiskaly dentro `fiscalization.credentials` (type `FISCONLINE`).

### fisconline_credentials_secret_ref
- **Legacy**: `fiskaly_account.taxpayer_fisconline_pin` (in chiaro) + `taxpayer_fisconline_password`
  (cifrata AES-256-GCM con chiave da env)
- **Descrizione**: **riferimento** (ARN / nome SSM) a **un** segreto il cui valore è il JSON
  `{ "pin": "…", "password": "…" }` Fisconline. Letto in memoria dal secret manager solo quando i
  valori vanno inviati a Fiskaly per il commissioning / l'aggiornamento credenziali
  (`suspend → update → operative`).
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: punta a un segreto; **nessun materiale segreto in tabella**. L'aggiornamento
  (l'utente cambia PIN/password) è una **nuova versione** del segreto, non una `UPDATE` di colonna.

### fisconline_updated_at
- **Legacy**: `fiskaly_account.fisconline_updated_at` (`datetime`)
- **Descrizione**: data dell'ultima modifica di PIN o password Fisconline. Mostrata dal pannello
  ("credenziali aggiornate il …"). Sostituisce anche l'eventuale bisogno di un hash "è cambiato?".
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### software_cash_register_is_default
- **Legacy**: `fiskaly_account.sw_cash_register_default` (`tinyint` `NOT NULL` `DEFAULT 1`)
- **Descrizione**: `true` = la cassa **software** auto-creata è quella di default per lo stabilimento
  e tutte le casse utente sono forzate a `is_default = false`; `false` = il default è una cassa
  definita dall'utente.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: governa la regola "esattamente una `fiskaly_cash_registers.is_default = true` per
  account" (vedi [`fiskaly_cash_registers`](fiskaly_cash_registers.md)).

### created_by_user_id
- **Legacy**: `fiskaly_account.createdBy` (`int`, `users.id`, nessuna FK)
- **Descrizione**: operatore/gestore che ha creato la riga (step `updateTerms`). Mai aggiornato nel
  legacy.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md)
  ([`docs/03 §3.2`](../../../progettazione/03-gestione-dati.md)).

### updated_by_user_id
- **Legacy**: — (nuovo)
- **Descrizione**: ultimo operatore che ha modificato la configurazione fiscale.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: come sopra.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 7 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at
- **Legacy**: `fiskaly_account.createdAt` / `updatedAt` (`datetime`, `defaultValue: NOW`,
  `timestamps: false` → gestiti a mano)
- **Descrizione**: creazione / ultima modifica della riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: valorizzazione automatica (trigger/interceptor, [`docs/03 §3.4`](../../../progettazione/03-gestione-dati.md)).

### deleted_at
- **Legacy**: `fiskaly_account.decommissioned_at` (`datetime`; soft-delete di fatto senza colonna
  `deleted_at`)
- **Descrizione**: soft-delete = **chiusura account** (`closeAccount`). Quando valorizzato,
  `getAccount` / `getCashRegisters` rispondono come se l'account non esistesse; un successivo
  `updateAccount` lo azzera e ricrea da zero tutte le entità Fiskaly (gli id vecchi puntano a oggetti
  `DECOMMISSIONED`, non riutilizzabili).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: Global Query Filter EF; cascade soft-delete sulle
  [`fiskaly_cash_registers`](fiskaly_cash_registers.md) dell'account.

## Valutazione JSON

Nessun JSON. Il legacy non ne aveva su `fiskaly_account`: tutti i campi sono scalari tipizzati. Le
credenziali (che nel legacy erano stringhe opache) escono del tutto dallo schema → secret manager.

## Suggerimenti per le prestazioni

- Tabella minuscola (~1 riga per stabilimento con Fiskaly attivo: 7 oggi, poche centinaia a regime).
  Nessun problema di volume; gli indici servono per l'unicità e per il cruscotto SuperAdmin.
- `fiscal_setup_level = fiskaly_active` come **colonna generata** (`GENERATED ALWAYS AS (…) STORED`)
  oppure ricalcolata a ogni transizione: evita la deriva legacy fra flag scritto e stato reale.
- Il pannello *Fiscalization* legge questa riga + le casse: 2 query per property, entrambe su indice.
- Chiamata al secret manager solo al momento di dialogare con Fiskaly (non a ogni lettura del
  pannello): cache in memoria con TTL breve lato applicazione.

## Implementazione .NET

`FiskalyAccount` è l'**aggregate root** del modulo `Module.Fiskaly` (schema `fiskaly`): rappresenta
l'account fiscale di uno stabilimento presso Fiskaly SIGN IT e possiede, come owned collection, le
[`fiskaly_cash_registers`](fiskaly_cash_registers.md) dello stabilimento (cascade su soft-delete).
`FiskalyReceipt` (aggregate root a sé, [`fiskaly_receipts`](fiskaly_receipts.md)) referenzia
`FiskalyAccount` per FK interna ma non ne fa parte come owned entity.

**Segreti**: nessuna colonna di questa entità contiene materiale segreto in chiaro. Le due colonne
`SubjectCredentialsSecretRef` / `FisconlineCredentialsSecretRef` sono **riferimenti** (ARN/nome) a
bundle su secret manager, risolti solo da `ISecretStore` nell'Infrastructure quando serve dialogare
con Fiskaly — mai proprietà di dominio che restituiscono il segreto.

### Enum propri

```csharp
// Domain/Enums/FiscalSetupLevel.cs
public enum FiscalSetupLevel { NotConfigured = 0, VatOnly = 1, FiskalyActive = 2 }

// Domain/Enums/FiskalyOrgState.cs — Organization/Subject (nessun ciclo "acquired")
public enum FiskalyOrgState { Enabled = 0, Decommissioned = 1 }

// Domain/Enums/FiskalyEntityState.cs — Taxpayer/Location; riusato anche da
// fiskaly_cash_registers.system_state (vedi quella scheda, nessuna ridefinizione)
public enum FiskalyEntityState { Acquired = 0, Commissioned = 1, Decommissioned = 2 }

// Domain/Enums/FiskalyEntityMode.cs — Taxpayer/Location; riusato anche da
// fiskaly_cash_registers.system_mode
public enum FiskalyEntityMode { Inactive = 0, Operative = 1, Suspended = 2, Degraded = 3 }
```

```csharp
// FiskalyDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<FiscalSetupLevel>("fiskaly", "fiscal_setup_level");
modelBuilder.HasPostgresEnum<FiskalyOrgState>("fiskaly", "org_state");
modelBuilder.HasPostgresEnum<FiskalyEntityState>("fiskaly", "entity_state");
modelBuilder.HasPostgresEnum<FiskalyEntityMode>("fiskaly", "entity_mode");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<FiscalSetupLevel>("fiskaly.fiscal_setup_level");
dataSourceBuilder.MapEnum<FiskalyOrgState>("fiskaly.org_state");
dataSourceBuilder.MapEnum<FiskalyEntityState>("fiskaly.entity_state");
dataSourceBuilder.MapEnum<FiskalyEntityMode>("fiskaly.entity_mode");
```

### Configuration

```csharp
namespace Module.Fiskaly.Infrastructure.Configurations;

public sealed class FiskalyAccountConfiguration : IEntityTypeConfiguration<FiskalyAccount>
{
	public void Configure(EntityTypeBuilder<FiskalyAccount> builder)
	{
		builder.ToTable("fiskaly_accounts", "fiskaly", t => t.HasComment(
			"Account fiscale di uno stabilimento presso Fiskaly SIGN IT: identità, credenziali (per riferimento) e stato di configurazione fiscale."));

		builder.HasKey(x => x.Id).HasName("pk_fiskaly_accounts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile (UUIDv7); esposto all'export GDPR e al pannello Fiscalization.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_fiskaly_accounts_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. Stabilimento proprietario dell'account.");
		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_fiskaly_accounts_property");

		builder.Property(x => x.FiscalSetupLevel)
			.HasColumnName("fiscal_setup_level")
			.HasColumnType("fiskaly.fiscal_setup_level")
			.IsRequired()
			.HasDefaultValue(FiscalSetupLevel.NotConfigured)
			.HasComment("Stato di configurazione fiscale: NotConfigured / VatOnly / FiskalyActive. FiskalyActive è derivato e ricalcolato dal dominio, non liberamente scrivibile.");
		builder.HasIndex(x => x.FiscalSetupLevel)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_fiskaly_accounts_setup_level");

		builder.Property(x => x.TermsAcceptedAt)
			.HasColumnName("terms_accepted_at")
			.HasComment("Istante in cui il gestore ha accettato termini e condizioni Fiskaly; NULL = non accettati. Precondizione bloccante per il setup (sostituisce il flag terms_accepted).");

		builder.Property(x => x.IsEnabled)
			.HasColumnName("is_enabled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Interruttore master della fiscalità elettronica, gestito da SuperAdmin.");

		builder.Property(x => x.AutoreceiptEnabled)
			.HasColumnName("autoreceipt_enabled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Emissione automatica dello scontrino dopo un pagamento Stripe andato a buon fine.");

		// NON e' una catena monotona: l'abilitazione (is_enabled, autoreceipt_enabled) la imposta il
		// SystemAdmin, l'accettazione dei termini (terms_accepted_at) la fa il gestore DOPO. Quindi
		// "abilitato senza termini accettati" e' uno stato legittimo e transitorio, non un errore:
		// l'unica dipendenza strutturale e' che l'auto-scontrino richieda Fiskaly abilitato.
		// Il gate sui termini e' un controllo a RUNTIME (updateAccount / getCashRegisters /
		// emissione), non un'invariante di tabella.
		builder.ToTable(t => t.HasCheckConstraint(
			"ck_fiskaly_accounts_activation_chain",
			"autoreceipt_enabled = false OR is_enabled = true"));

		builder.Property(x => x.OrganizationId)
			.HasColumnName("organization_id")
			.HasMaxLength(36)
			.HasComment("Id opaco dell'Organization su Fiskaly SIGN IT.");
		builder.HasIndex(x => x.OrganizationId)
			.IsUnique()
			.HasFilter("organization_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_accounts_organization");

		builder.Property(x => x.SubjectId)
			.HasColumnName("subject_id")
			.HasMaxLength(36)
			.HasComment("Id opaco del Subject API_KEY su Fiskaly SIGN IT.");
		builder.HasIndex(x => x.SubjectId)
			.IsUnique()
			.HasFilter("subject_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_accounts_subject");

		builder.Property(x => x.TaxpayerId)
			.HasColumnName("taxpayer_id")
			.HasMaxLength(36)
			.HasComment("Id opaco del Taxpayer COMPANY su Fiskaly SIGN IT.");
		builder.HasIndex(x => x.TaxpayerId)
			.IsUnique()
			.HasFilter("taxpayer_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_accounts_taxpayer");

		builder.Property(x => x.LocationId)
			.HasColumnName("location_id")
			.HasMaxLength(36)
			.HasComment("Id opaco della Location BRANCH su Fiskaly SIGN IT.");
		builder.HasIndex(x => x.LocationId)
			.IsUnique()
			.HasFilter("location_id IS NOT NULL")
			.HasDatabaseName("uq_fiskaly_accounts_location");

		builder.Property(x => x.OrganizationName)
			.HasColumnName("organization_name")
			.HasMaxLength(128)
			.HasComment("Nome assegnato all'Organization su Fiskaly.");

		builder.Property(x => x.SubjectApiKeyName)
			.HasColumnName("subject_api_key_name")
			.HasMaxLength(40)
			.HasComment("Nome del Subject API_KEY su Fiskaly.");

		builder.Property(x => x.OrganizationState)
			.HasColumnName("organization_state")
			.HasColumnType("fiskaly.org_state")
			.HasComment("Stato dell'Organization su Fiskaly (enabled/decommissioned); può divergere dal remoto dopo closeAccount.");

		builder.Property(x => x.SubjectState)
			.HasColumnName("subject_state")
			.HasColumnType("fiskaly.org_state")
			.HasComment("Stato del Subject su Fiskaly (enabled/decommissioned); può divergere dal remoto dopo closeAccount.");

		builder.Property(x => x.TaxpayerState)
			.HasColumnName("taxpayer_state")
			.HasColumnType("fiskaly.entity_state")
			.HasComment("Ciclo di vita del Taxpayer su Fiskaly (acquired/commissioned/decommissioned).");

		builder.Property(x => x.LocationState)
			.HasColumnName("location_state")
			.HasColumnType("fiskaly.entity_state")
			.HasComment("Ciclo di vita della Location su Fiskaly (acquired/commissioned/decommissioned).");

		builder.Property(x => x.TaxpayerMode)
			.HasColumnName("taxpayer_mode")
			.HasColumnType("fiskaly.entity_mode")
			.HasComment("Modalità operativa del Taxpayer su Fiskaly (inactive/operative/suspended/degraded).");

		builder.Property(x => x.LocationMode)
			.HasColumnName("location_mode")
			.HasColumnType("fiskaly.entity_mode")
			.HasComment("Modalità operativa della Location su Fiskaly (inactive/operative/suspended/degraded).");

		builder.Property(x => x.SubjectCredentialsSecretRef)
			.HasColumnName("subject_credentials_secret_ref")
			.HasMaxLength(255)
			.HasComment("Riferimento (ARN/nome) al segreto {key,secret} del Subject in secret manager — nessun materiale segreto in colonna.");

		builder.Property(x => x.TaxpayerLegalName)
			.HasColumnName("taxpayer_legal_name")
			.HasMaxLength(128)
			.HasComment("Ragione sociale legale del titolare, inviata a Fiskaly come name.legal.");

		builder.Property(x => x.TaxpayerTradeName)
			.HasColumnName("taxpayer_trade_name")
			.HasMaxLength(128)
			.HasComment("Nome commerciale / insegna, preferito nel PDF scontrino e nei messaggi.");

		builder.Property(x => x.TaxpayerVatNumber)
			.HasColumnName("taxpayer_vat_number")
			.HasMaxLength(13)
			.HasComment("Partita IVA del titolare (dato fiscale).");

		builder.Property(x => x.TaxpayerFiscalCode)
			.HasColumnName("taxpayer_fiscal_code")
			.HasMaxLength(16)
			.HasComment("Codice fiscale / partita IVA del soggetto passivo (dato fiscale).");

		builder.Property(x => x.TaxpayerAddressStreet)
			.HasColumnName("taxpayer_address_street")
			.HasMaxLength(128)
			.HasComment("Via dell'indirizzo del titolare inviato a Fiskaly e stampato sul PDF.");

		builder.Property(x => x.TaxpayerAddressStreetNumber)
			.HasColumnName("taxpayer_address_street_number")
			.HasMaxLength(16)
			.HasComment("Numero civico dell'indirizzo del titolare (default applicativo 'SNC' quando inviato).");

		builder.Property(x => x.TaxpayerAddressPostalCode)
			.HasColumnName("taxpayer_address_postal_code")
			.HasMaxLength(5)
			.HasComment("CAP dell'indirizzo del titolare.");

		builder.Property(x => x.TaxpayerAddressCity)
			.HasColumnName("taxpayer_address_city")
			.HasMaxLength(128)
			.HasComment("Città dell'indirizzo del titolare.");

		builder.Property(x => x.TaxpayerAddressCountryCode)
			.HasColumnName("taxpayer_address_country_code")
			.HasColumnType("char(2)")
			.IsRequired()
			.HasDefaultValue("IT")
			.HasComment("Nazione ISO 3166-1 alpha-2 dell'indirizzo del titolare.");

		builder.Property(x => x.FisconlineHolderFiscalCode)
			.HasColumnName("fisconline_holder_fiscal_code")
			.HasMaxLength(16)
			.HasComment("Codice fiscale della persona fisica abilitata a Fisconline — dato personale, rilevante per l'export GDPR.");

		builder.Property(x => x.FisconlineCredentialsSecretRef)
			.HasColumnName("fisconline_credentials_secret_ref")
			.HasMaxLength(255)
			.HasComment("Riferimento (ARN/nome) al segreto {pin,password} Fisconline in secret manager — nessun materiale segreto in colonna.");

		builder.Property(x => x.FisconlineUpdatedAt)
			.HasColumnName("fisconline_updated_at")
			.HasComment("Data dell'ultima modifica di PIN o password Fisconline.");

		builder.Property(x => x.SoftwareCashRegisterIsDefault)
			.HasColumnName("software_cash_register_is_default")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Se true, la cassa software auto-creata è quella di default e le casse utente sono forzate a is_default=false.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha creato la riga.");

		builder.Property(x => x.UpdatedByUserId)
			.HasColumnName("updated_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Ultimo operatore che ha modificato la configurazione.");

		builder.HasMany(x => x.CashRegisters)
			.WithOne()
			.HasForeignKey(cr => cr.AccountId)
			.HasConstraintName("fk_fiskaly_cash_registers_account")
			.OnDelete(DeleteBehavior.Cascade);

		// created_at/updated_at/deleted_at (ISoftDeletable/IAuditable) — plumbing comune, vedi _dotnet-template.md §1-2
	}
}
```

`FiskalyAccount` implementa `IAuditable` e `ISoftDeletable` (soft-delete = chiusura account,
`closeAccount`; cascade sulle `fiskaly_cash_registers` dell'account via `SoftDeleteInterceptor`).

### Note di migrazione

- `fiskaly_account` 1:1 su questa tabella; `properties.fiskalyEnabled` / `fiskalyAutoreceipt` /
  `fiskalyFlag` confluiscono in `is_enabled` / `autoreceipt_enabled` / `fiscal_setup_level`, bonificando
  le combinazioni incoerenti osservate (vedi [`fiskaly_account.md`](../../../analisi-pre/db/fiskaly_account.md)).
- `subject_credential_key` + `subject_credential_secret` (chiaro) → `PutSecretValue` come bundle
  `{key,secret}` → `subject_credentials_secret_ref`; `taxpayer_fisconline_pin` (chiaro) +
  `taxpayer_fisconline_password` (AES-256-GCM, chiave env) → decifrare con la vecchia chiave, poi
  `PutSecretValue` come bundle `{pin,password}` → `fisconline_credentials_secret_ref`. Nessun valore
  segreto transita mai per una colonna della tabella durante la migrazione (solo in memoria verso il
  secret manager).
- `decommissioned_at` → `deleted_at`.
- `FiscalSetupLevel.FiskalyActive`: scelta implementativa — colonna **normale** valorizzata dal metodo
  di dominio `RecomputeSetupLevel()` (chiamato dopo ogni step Fiskaly), non `GENERATED ALWAYS AS ...
  STORED`: la logica di derivazione include il conteggio delle casse `commissioned` (owned collection),
  non esprimibile in una generated column senza subquery.
