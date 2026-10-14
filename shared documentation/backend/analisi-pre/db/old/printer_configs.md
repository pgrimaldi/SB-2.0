# Tabella `printer_configs`

> Fonti: schema e dati reali (56 righe, `AUTO_INCREMENT=57`), model
> [`back-pre/app/models/printerconfig.model.js`](../../back-pre/app/models/printerconfig.model.js)
> (`sequelize.define("printer_config", …)` senza `freezeTableName` → tabella **pluralizzata**
> `printer_configs`; senza `timestamps: false` → Sequelize gestisce `createdAt`/`updatedAt`,
> `NOT NULL` a schema; `SequelizeSlugify` importato ma non usato),
> CRUD in
> [`printerConfig.controller.js`](../../back-pre/app/controllers/printerConfig.controller.js):
> `savePrintersettings` (`POST /api/sb/add-printer`), `updatePrinterConfigDetails`
> (`PATCH /api/sb/update-printer`), `changeStatusPrinter` (`PATCH /api/sb/update-printer-status`,
> solo `isActive`), `deletePrinter` (`PATCH /api/sb/delete-printer`, solo `isdeleted = 1`),
> `getPrinterConfiguration` (`POST /api/sb/get-config-printer`,
> `findAll({ where: { [Op.and]: [{ propertyId }, { isdeleted: 0 }] } })`).
> Frontend: `front-pre/src/components/printerSetting/PrinterSelector.js` (scelta/gestione stampanti) e
> `PrintReservationCta.js` (stampa scontrino di una prenotazione); la config è passata alla
> **factory di adapter** `front-pre/src/services/printers/createPrinterAdapter.js` che, in base a
> `model`, istanzia `EpsonFiscalAdapter` (FP-81/FP-90), `RchAdapter` (RCH),
> `EpsonTmAdapter` (TM-T88/TM-T20) o `OnlineAdapter` (ONLINE); `department_1..3` sono usati da
> `departmentResolver.js` per mappare l'aliquota IVA di un servizio sul reparto della stampante
> fiscale. Nessuna associazione ORM in [`models/index.js`](../../back-pre/app/models/index.js).
>
> **Nota di contesto** — **configurazione delle stampanti (termiche / fiscali RT) di uno
> stabilimento**: N righe per `propertyId`. Ogni riga descrive una stampante di rete (IP/porta),
> il suo modello, la modalità d'uso, l'intestazione/piè di pagina degli scontrini e i reparti IVA.
> È la controparte "hardware" del flusso di stampa che alimenta [`booking_receipts`](booking_receipts.md)
> e [`receipt_serial_numbers`](receipt_serial_numbers.md). Distinta dal flusso **Fiskaly**
> (scontrino elettronico, [`fiskaly_*`](fiskaly_account.md)), che non usa questa tabella se non per
> il concetto di stampante "software" (`isSoftware` lato frontend, non persistito qui).
>
> **Dominio nella riscrittura**: **C4bis — Stampa fiscale su hardware RT** (feature a sé): **non**
> C3 (Payment/Stripe) né C4 (`Fiskaly`/SIGN IT, lo scontrino elettronico). È il meccanismo
> *alternativo* — più vecchio — per emettere lo scontrino fiscale, gestito quasi tutto lato client
> (adapter Epson TM/FP, RCH in `front-pre/src/services/printers/`) con `printerConfig.controller.js`
> lato backend; solo 3 tabelle (`printer_configs`, [`booking_receipts`](booking_receipts.md),
> [`receipt_serial_numbers`](receipt_serial_numbers.md)). Nel target: componente separato (es.
> `FiscalPrinting`) **oppure dismesso** se lo scontrino elettronico Fiskaly copre tutti gli
> stabilimenti. Vedi [`_feature-tabelle.md`](_feature-tabelle.md) → "C4bis — Stampa fiscale su
> hardware RT" e [`docs/03-gestione-dati.md` §4](../../docs/03-gestione-dati.md).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Descrizione**: usato come `printerId` negli endpoint di update/status/delete e in
  `booking_receipts.printerId`.
- **Esempio di valori**: 1, 2, 3, ..

### printername
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì (obbligatorio in input: `savePrintersettings` rifiuta senza)
- **Descrizione**: nome libero dato dall'operatore alla stampante (etichetta in UI).
- **Esempio di valori**: `"lido stampante"`, `"Epson TM-20"`, `"gprinter"`

### isActive
- **Tipo**: `tinyint`, `DEFAULT 0` — commento a schema *"0 is for not & 1 is for active"*
- **Nullabile**: Sì
- **Descrizione**: stampante attualmente selezionata/in uso per lo stabilimento. `savePrintersettings`
  crea sempre con `isActive: 1`; `changeStatusPrinter` la aggiorna. **Non** è azzerata da
  `deletePrinter` → nei dati esistono righe `isActive = 1` **e** `isdeleted = 1` (37 su 56).
- **Candidato a enumerazione**: **booleano**, non enum.
- **Esempio di valori**: `1`, `0`

### userId
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: `users.id` — nessuna FK, nessuna associazione. Operatore che ha configurato la
  stampante (`req.body.userId`).
- **Descrizione**: nel target → `user_operator_id`
  ([`docs/03-gestione-dati.md` §3.2](../../docs/03-gestione-dati.md)).
- **Esempio di valori**: `3916`, `675`, `235`, `3300`

### propertyId
- **Tipo**: `int`
- **Nullabile**: Sì (obbligatorio in input)
- **Relazioni**: `properties.id` — **nessuna FK reale**, nessuna associazione. Chiave di filtro in
  `getPrinterConfiguration`.
- **Descrizione**: stabilimento proprietario della stampante.
- **Esempio di valori**: `10828`, `275`, `115`, `10805`

### ip
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì (obbligatorio in input)
- **Descrizione**: indirizzo di rete della stampante (IP LAN o `127.0.0.1` per stampante locale al
  client).
- **Esempio di valori**: `"192.168.1.221"`, `"127.0.0.1"`, `"192.168.0.128"`

### port
- **Tipo**: `varchar(255)` — **stringa libera, non validata**
- **Nullabile**: Sì (obbligatorio in input)
- **Descrizione**: porta TCP della stampante. Nei dati: valori numerici (`9100`, `80`, `443`,
  `5000`), varianti errate (`09100`, `19100`, `1`, `11`, `111`, `69`), un **IP messo per errore nel
  campo porta** (`192.168.0.1`) e persino un nome di device USB (`Port_#0001.Hub_#0001`).
- **Esempio di valori**: `"9100"`, `"80"`, `"443"`, `"Port_#0001.Hub_#0001"`

### model
- **Tipo**: `varchar(255)` — **stringa libera a schema**, ma **vincolata dalla UI**
- **Nullabile**: Sì (obbligatorio in input)
- **Descrizione**: modello della stampante. Determina l'adapter di stampa usato dal frontend
  (`createPrinterAdapter`): `"FP-81"`/`"FP-90"` → `EpsonFiscalAdapter`, `"RCH"` → `RchAdapter`,
  `"TM-T88"`/`"TM-T20"` → `EpsonTmAdapter`, `"ONLINE"` → `OnlineAdapter`. Un valore non riconosciuto
  fa **lanciare** `createPrinterAdapter` (*"Unsupported printer model"*).
- **Non c'è modo di aggiungere valori da UI**: nella maschera corrente
  (`front-pre` `PrinterSettingsDialog.js`, `front-modern-pre` `CreateEditPrinterForm.tsx`) il campo
  è una `<select>` popolata da `Object.values(PRINTER_MODELS)` — costante **hardcoded** in
  `PrinterAdapter.js`. I refusi nei dati (`"dddd"`, `"Asdax"`, `"fi"`, `"tets"`) vengono dalla
  vecchia maschera a testo libero (`printer.js`), non più in uso.
- **Candidato a enumerazione**: **Sì**, `enum` `PrinterModel { Fp81, Fp90, Rch, TmT88, TmT20, Online }`
  — **non** una tabella di lookup: aggiungere un modello richiede comunque **codice nuovo** (una
  classe adapter dedicata che sappia parlare quel protocollo di stampa), quindi renderlo
  gestibile da pannello non avrebbe effetto ([`docs/03-gestione-dati.md` §2.2.A](../../docs/03-gestione-dati.md)).
- **Esempio di valori**: `"TM-T20"`, `"FP-81"`, `"TM-T88"`, `"RCH"`, `"FP-90"`

### mode
- **Tipo**: `varchar(255)` — stringa libera
- **Nullabile**: Sì (obbligatorio in input)
- **Descrizione**: modalità d'uso della stampante. Valori reali osservati: `"fiscal"` (scontrino
  fiscale RT), `"nonfiscal"` (documento non fiscale / preconto), `"orders"` (comanda/ordini).
- **Candidato a enumerazione**: **Sì**, `PrinterMode { Fiscal, NonFiscal, Orders }`.
- **Esempio di valori**: `"nonfiscal"` (28), `"fiscal"` (26), `"orders"` (2)

### department_1 / department_2 / department_3
- **Tipo**: `varchar(255)` ciascuno
- **Nullabile**: Sì
- **Descrizione**: mappa i **3 "reparti" fisici del registratore di cassa RT** sulle **aliquote IVA**.
  Un registratore di cassa fiscale ha reparti numerati (1, 2, 3, …) ognuno pre‑programmato *sulla
  stampante stessa* con una certa aliquota; qui si registra quella stessa corrispondenza così che il
  software sappia su quale reparto battere ogni voce. Valore atteso: una percentuale con simbolo,
  scelta da una `<select>` fissa `0%` / `22%` / `10%` / `4%` (`PrinterSettingsDialog.js`).
- **Logica di scelta del reparto** (`departmentResolver.resolveDepartment(percentIVA, printerConfig)`,
  frontend): **non c'è priorità intrinseca** fra reparto 1, 2 e 3. La funzione:
  1. prende l'aliquota IVA della **voce da stampare** (`percentIVA`; se mancante/non numerica → `22`);
  2. converte `department_1/2/3` da `"22%"` a numero (`parseDeptValue`);
  3. **match esatto**: restituisce il numero del reparto la cui aliquota **coincide** con quella
     della voce (`"1"` / `"2"` / `"3"`);
  4. **fallback**: se nessuno coincide, restituisce il reparto configurato a `22%` (il primo che
     trova, in ordine 1→2→3);
  5. **ultima spiaggia**: `"1"`.
  Quindi `department_1` viene preferito a `department_2`/`_3` *solo* quando è quello che matcha
  l'aliquota, o quando è il primo con `22%` nel fallback.
- Nei dati spesso vuoti o `NULL` (stampanti non fiscali), oppure `"0%"`.
- **Esempio di valori**: `"22%"`, `"10%"`, `"4%"`, `"0%"`, `null`, `""`

### heading_1 / heading_2
- **Tipo**: `varchar(255)` ciascuno
- **Nullabile**: Sì
- **Descrizione**: righe di **intestazione** stampate in testa allo scontrino/comanda (es. nome
  attività). Spesso stringa vuota.
- **Esempio di valori**: `"tuareg"`, `"tuaregbeach"`, `""`

### footer
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: riga di **piè di pagina** stampata in fondo allo scontrino/comanda (es. messaggio
  di ringraziamento).
- **Esempio di valori**: `"grazie"`, `""`

### isdeleted
- **Tipo**: `tinyint`, `DEFAULT 0` — commento a schema *"0 is for not & 1 is for deleted"*
- **Nullabile**: Sì
- **Descrizione**: **soft-delete**. `deletePrinter` imposta `isdeleted = 1` (senza toccare
  `isActive`). `getPrinterConfiguration` filtra `isdeleted: 0`. Nei dati: 42 righe `= 1`.
- **Candidato a enumerazione**: **booleano** → nel target `deleted_at`
  ([`docs/03-gestione-dati.md` §3.5](../../docs/03-gestione-dati.md)).
- **Esempio di valori**: `0`, `1`

### createdAt
- **Tipo**: `datetime`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: creazione riga (gestita da Sequelize).
- **Esempio di valori**: `2026-06-…`

### updatedAt
- **Tipo**: `datetime`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: ultimo aggiornamento (gestito da Sequelize).
- **Esempio di valori**: `2026-06-…`

### SSL
- **Tipo**: `tinyint(1)`, `DEFAULT 0`
- **Nullabile**: Sì
- **Descrizione**: `1` = la comunicazione con la stampante usa HTTPS/TLS; `0` = HTTP in chiaro.
  Impostato da `savePrintersettings`/`updatePrinterConfigDetails` (`SSL: req.body.SSL`). Nome
  colonna in maiuscolo (fuori standard).
- **Candidato a enumerazione**: **booleano** (`use_tls`), non enum.
- **Esempio di valori**: `0`, `1`

---

## Osservazioni verificate nel codice (criticità)

- **`deletePrinter` non azzera `isActive`**: 37 righe su 56 hanno `isActive = 1` **e**
  `isdeleted = 1`. Due flag di stato scollegati; nel target un solo `deleted_at` + `is_active`.
- **`port` non validata**: stringa libera con valori sbagliati (IP, nome device USB, porte non
  numeriche). Nel target `int` con range valido, o separare "connessione USB" da "connessione TCP".
- **`model` come `varchar` libero**: vocabolario in realtà chiuso (adapter dedicati) ma pieno di
  refusi (`"dddd"`, `"Asdax"`, …) → un model non valido rompe la stampa. Vedi Enumerazioni.
- **`mode` come `varchar` libero** (`fiscal`/`nonfiscal`/`orders`) — vedi Enumerazioni.
- **`department_1/2/3` come `varchar` "22%"** — 3 problemi:
  1. **è un numero salvato come stringa con il simbolo** (`"22%"`): ogni lettura deve fare
     `replace("%","")` + `parseFloat`; una stringa fuori formato (`"22"`, `"22 %"`, `"iva22"`) manda
     a vuoto il match esatto e la voce finisce sul reparto `22%` di fallback (aliquota potenzialmente
     sbagliata sullo scontrino fiscale).
  2. **numero di reparti fissato a 3** da 3 colonne: i registratori RT reali ne gestiscono molti di
     più (tipicamente decine); non si possono configurare.
  3. **il legame "reparto ↔ aliquota" è posizionale** (l'indice della colonna *è* il numero di
     reparto): non è esplicito, non è vincolabile, non è indicizzabile.
  **Nel target**: niente 3 colonne. Una tabella figlia
  `printer_departments(printer_id FK, dept_number smallint, vat_rate numeric(5,2))` — **una riga per
  reparto configurato**, con `vat_rate` numerico (`22.00`) e `UNIQUE(printer_id, dept_number)` (ed
  eventualmente `UNIQUE(printer_id, vat_rate)` se un'aliquota può stare su un solo reparto). Il
  resolver diventa un semplice `WHERE printer_id = ? AND vat_rate = ?`.
- **Nessuna FK** verso `properties`/`users`; **nessun `UNIQUE`** (es. su `propertyId + printername`,
  o "una sola stampante attiva per mode").
- **Nome colonna `SSL`** in maiuscolo, fuori dallo `snake_case` del resto — nel target `use_tls`.
- **Validazione asimmetrica**: `savePrintersettings` esige `printername`/`ip`/`port`/`model`/`mode`,
  `updatePrinterConfigDetails` richiede solo `printerId` e ricostruisce l'intero record dal body
  (i campi assenti = `undefined` sono ignorati da Sequelize, ma un `null`/`""` inviato per errore
  azzera il campo). Nessun merge parziale controllato.
