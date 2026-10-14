# Tabella `fiskaly_cash_register`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **5 righe**, query
> completa; `createdAt` tutte del 2026-06), model
> [`back-pre/app/models/fiskalyCashRegister.model.js`](../../../../../../back-pre/app/models/fiskalyCashRegister.model.js)
> (`{ freezeTableName: true, timestamps: false }`),
> scrittura/lettura in
> [`Service/fiskalyAccountService.js`](../../../../../../back-pre/app/Service/fiskalyAccountService.js)
> (step 10a "auto-create Software Cash Register", step 10b "Create/Update Systems", cascate di
> decommission, `getCashRegisters`, `getAccount`, `deleteEntities`, `closeAccount`) e
> [`Service/fiskalyReceiptManagerService.js`](../../../../../../back-pre/app/Service/fiskalyReceiptManagerService.js)
> (`getCashRegister(fiskalyAccountId, systemId)` — selezione cassa per emettere lo scontrino:
> filtra `deleted_at: null`, `system_mode: 'OPERATIVE'`, `system_state: 'COMMISSIONED'`, poi
> `is_default: 1`, poi la prima disponibile);
> chiamate esterne a Fiskaly SIGN IT (`createSystemWithCredentials`,
> `updateSystemWithCredentials`) in [`utils/fiskalySignIt.helper.js`](../../../../../../back-pre/app/utils/fiskalySignIt.helper.js);
> associazioni in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (righe 338-365):
> `belongsTo(db.fiskalyAccount, { foreignKey: "fiskaly_account_id" })`,
> `hasMany(db.fiskalyReceipt, { foreignKey: "system_id", sourceKey: "system_id" })`,
> `hasMany(db.fiskalyReceiptSequence, { foreignKey: "system_id", sourceKey: "system_id" })`.
> Frontend: `front-modern-pre` (`update-account` / `get-account` / `get-cash-registers`
> in `src/remote/SummerBooking/Fiscalization/FiscalizationRestApi.ts`),
> `front-pre` (`sb/electronic-receipt/get-cash-registers`, `src/data/actions/fiscalizationAction.js`
> → `PrinterSelector.js`).
>
> **Nota di contesto** — **N righe per [`fiskaly_account`](fiskaly_account.md)** (`hasMany`).
> Ogni riga è un **registratore di cassa / *System* di tipo `FISCAL_DEVICE`** registrato su Fiskaly
> SIGN IT, con cui si firmano i documenti commerciali. Almeno una riga "software" è auto-creata dal
> backend (`is_software = 1`), con producer/software configurati via variabili d'ambiente
> (`FISKALY_SW_PRODUCER_NAME`, `FISKALY_SW_PRODUCER_NUMBER`, `FISKALY_SW_NAME`, `FISKALY_SW_VERSION`);
> in più il gestore può aggiungere casse "hardware" definite da lui (`is_software = 0`, tramite il
> blocco `cash_registers[]` di `update-account`).
>
> **Dominio nella riscrittura**: **C4 — modulo `Fiskaly`** (distinto da **C3** `Payment`/Stripe).
> Analisi domini in [`docs/03-gestione-dati.md` §4](../../progettazione/03-gestione-dati.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### fiskaly_account_id
- **Tipo**: `int`
- **Nullabile**: Sì (a schema; nel codice sempre valorizzato alla creazione)
- **Relazioni**: FK reale `fiskaly_cash_register_ibfk_1` → `fiskaly_account.id`,
  `ON DELETE SET NULL ON UPDATE CASCADE`. Indice non-unico `fiskaly_cash_register_fiskaly_account_id`.
- **Descrizione**: account Fiskaly (quindi stabilimento) a cui appartiene la cassa. Tutte le query
  filtrano `where: { fiskaly_account_id: account.id, deleted_at: null }`.
- **Esempio di valori**: `1`, `2`, `5`, `3`, `6`

### system_id
- **Tipo**: `varchar(36)` — UUID v7 restituito da Fiskaly. `UNIQUE` (`system_id`, doppio indice
  `system_id_2`)
- **Nullabile**: Sì (NULL tra la creazione del record DB e la creazione del System su Fiskaly, o
  dopo un decommission che lo azzera per forzare la ricreazione)
- **Relazioni**: **target key** delle FK/associazioni da [`fiskaly_receipt.system_id`](fiskaly_receipt.md)
  (FK reale `fiskaly_receipt_ibfk_1`, `ON DELETE SET NULL ON UPDATE CASCADE`) e
  [`fiskaly_receipt_sequence.system_id`](fiskaly_receipt_sequence.md) (FK reale, `ON UPDATE CASCADE`).
- **Descrizione**: id del *System* su Fiskaly (`systemResponse.content.id`). È la chiave con cui gli
  scontrini e le sequenze numeriche giornaliere si legano alla cassa (non l'`id` interno).
- **Esempio di valori**: `"019e8dc7-ae38-782a-9b97-5fb886342455"`,
  `"019e8e0e-a584-75d8-94ca-ed74795abc0e"`, `"019eb70f-b0aa-7936-b1a4-3121fa22dd74"`

### system_type
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì (default applicativo `"FISCAL_DEVICE"`)
- **Descrizione**: tipo di System su Fiskaly. Sempre `"FISCAL_DEVICE"` sia per la cassa software sia
  per le casse utente (`cr.system_type || "FISCAL_DEVICE"`).
- **Candidato a enumerazione**: **Sì**, `FiskalySystemType { FiscalDevice }` — di fatto costante.
- **Esempio di valori**: `"FISCAL_DEVICE"`

### mpn_number
- **Tipo**: `varchar(128)` — *Manufacturer Part Number*
- **Nullabile**: Sì (obbligatorio in input per le casse utente: `cr.mpn_number` **o**
  `cr.producer.number`)
- **Descrizione**: numero produttore del dispositivo, inviato a Fiskaly come `producer.number`. Per
  la cassa software è `FISKALY_SW_PRODUCER_NUMBER` (default `"1"`). Usato come chiave per
  riconoscere una cassa esistente in `update-account`; **non modificabile** dopo la creazione
  (`update-account` lancia `400` se cambia).
- **Esempio di valori**: `"1"`

### mpn_name
- **Tipo**: `varchar(256)`
- **Nullabile**: Sì
- **Descrizione**: nome/descrizione del produttore, inviato a Fiskaly come `producer.details.name`.
  Per la cassa software è `FISKALY_SW_PRODUCER_NAME` (nel campione: `"Electronic receipt"` per le
  prime due righe, `"SBH Printer Cloud"` per le successive — il default nel codice è `"Qbitsoft"`,
  segno che la env è cambiata nel tempo). Esposto in `getReceipts` come `fiskaly_cash_register.mpn_name`.
- **Esempio di valori**: `"Electronic receipt"`, `"SBH Printer Cloud"`

### software_name
- **Tipo**: `varchar(128)`
- **Nullabile**: Sì (obbligatorio in input per le casse utente)
- **Descrizione**: nome del software gestionale, inviato a Fiskaly come `software.name`. Per la cassa
  software è `FISKALY_SW_NAME` (default `"SummerBooking"`). Un cambio di `software_name`/`_version`
  su una cassa esistente forza decommission + ricreazione del System.
- **Esempio di valori**: `"SummerBooking"`

### software_version
- **Tipo**: `varchar(128)`
- **Nullabile**: Sì (obbligatorio in input per le casse utente)
- **Descrizione**: versione del software, inviata a Fiskaly come `software.version`. Cassa software:
  `FISKALY_SW_VERSION` (default `"1.0"`).
- **Esempio di valori**: `"1.0"`

### system_state
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: stato del System su Fiskaly. Transizioni gestite dal codice:
  `ACQUIRED → COMMISSIONED` (commissioning immediato dopo la creazione),
  `COMMISSIONED → DECOMMISSIONED` (dismissione — è **l'unico** stato da cui Fiskaly permette il
  decommission; entità in `CREATED`/`ACQUIRED` restano orfane). `getCashRegister` (emissione
  scontrino) richiede `system_state = 'COMMISSIONED'`.
- **Candidato a enumerazione**: **Sì**, `FiskalyEntityState { Acquired, Commissioned, Decommissioned }`
  (costante `FiskalyState`).
- **Esempio di valori**: `"COMMISSIONED"`, `null`

### system_mode
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: modalità operativa del System su Fiskaly. `getCashRegister` richiede
  `system_mode = 'OPERATIVE'` per emettere.
- **Candidato a enumerazione**: **Sì**, `FiskalyMode { Inactive, Operative, Suspended, Degraded }`.
- **Esempio di valori**: `"OPERATIVE"`, `null`

### is_default
- **Tipo**: `tinyint`, `NOT NULL`, `DEFAULT 0`
- **Nullabile**: No
- **Descrizione**: `1` = cassa di default dello stabilimento (usata da `getCashRegister` quando non
  si specifica un `system_id`). Regole: se
  [`fiskaly_account.sw_cash_register_default = 1`](fiskaly_account.md) allora il default è la cassa
  software e tutte le casse utente sono forzate a `0`; altrimenti `update-account` esige
  **esattamente una** cassa con `is_default = 1`. `getCashRegisters` ordina `is_default DESC, id ASC`.
- **Candidato a enumerazione**: **booleano**, non enum.
- **Esempio di valori**: `1`

### createdBy
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `users.id` (nessuna FK). Operatore che ha creato la cassa (`createdBy: userId`).
  Nel target → `user_operator_id` ([`docs/03-gestione-dati.md` §3.2](../../progettazione/03-gestione-dati.md)).
- **Esempio di valori**: `514`, `199`, `2823`, `3159`, `3218`

### createdAt
- **Tipo**: `datetime`, `NOT NULL` — `defaultValue: Sequelize.NOW`
- **Nullabile**: No
- **Descrizione**: timestamp di creazione. `timestamps: false`: gestione non automatica.
- **Esempio di valori**: `2026-06-01 16:23:05`, `2026-06-11 14:21:59`

### updatedAt
- **Tipo**: `datetime`, `NOT NULL` — `defaultValue: Sequelize.NOW`
- **Nullabile**: No
- **Descrizione**: aggiornato manualmente a ogni `cr.update(...)`. Esposto in `getCashRegisters`.
- **Esempio di valori**: `2026-06-03 13:58:54`

### is_software
- **Tipo**: `tinyint`, `NOT NULL`, `DEFAULT 0`
- **Nullabile**: No
- **Descrizione**: `1` = registratore di cassa **software** auto-creato dal backend (step 10a);
  `0` = cassa definita dall'utente. `update-account` **vieta** di modificare via API la cassa
  software (match su producer name + software name → `400`). Esposto in API come booleano.
- **Candidato a enumerazione**: **booleano**, non enum.
- **Esempio di valori**: `1`

### deleted_at
- **Tipo**: `datetime`, `DEFAULT NULL`
- **Nullabile**: Sì
- **Descrizione**: **soft-delete**. `NULL` = cassa attiva; valorizzato = dismessa/rimossa. Tutte le
  query di lettura filtrano `deleted_at: null`. Valorizzato da `deleteEntities`, `closeAccount`,
  dalle cascate di decommission in `update-account` (cambio dati fiscali/indirizzo, cambio software).
- **Esempio di valori**: `null` (in tutte le 5 righe), `2026-07-10 09:00:00` (atteso)

---

## Osservazioni verificate nel codice (criticità)

- **Soft-delete non uniforme**: qui `deleted_at` (buona pratica), ma
  [`fiskaly_account`](fiskaly_account.md) usa `decommissioned_at` e le altre tabelle Fiskaly non
  hanno soft-delete. Uniformare su `deleted_at`
  ([`docs/03-gestione-dati.md` §3.5](../../progettazione/03-gestione-dati.md)).
- **Config produttore/software da variabili d'ambiente**: `FISKALY_SW_*` con default cablati nel
  codice (in due punti con default diversi: `"Qbitsoft"` vs `"SBH Printer Cloud"` vs
  `"Electronic receipt"` nei dati). Il match "è la cassa software?" è fatto per **stringa esatta**
  su producer name + software name → fragile se le env cambiano (già successo nei dati).
- **Stati `varchar` liberi** (`system_state`, `system_mode`, `system_type`) passthrough Fiskaly,
  senza `ENUM`/`CHECK` — vedi Enumerazioni.
- **`mpn_number` non modificabile** ma senza vincolo DB: la regola è solo applicativa.
- **`is_default` senza vincolo "una sola default per account"** a schema: garantito solo dal codice
  (nel target, indice unico parziale `WHERE is_default AND deleted_at IS NULL`).
- **`createdBy` senza FK** e con nome ambiguo.
- **Timestamp non automatici** (`timestamps: false` + `defaultValue: NOW`; `updatedAt` a mano).
