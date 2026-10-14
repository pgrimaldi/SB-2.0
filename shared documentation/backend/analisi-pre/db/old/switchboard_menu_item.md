# Tabella `switchboard_menu_item`

> Fonti: schema del DB `local_sb` (**0 righe**, `AUTO_INCREMENT = 1`, `CREATE_TIME 2026-07-27`); model
> [`back-pre/app/models/switchboardMenuItem.model.js`](../../back-pre/app/models/switchboardMenuItem.model.js)
> (`sequelize.define("switchboard_menu_item", …, { freezeTableName: true, timestamps: false, underscored: true })`),
> registrato come `db.switchboardMenuItem` in [`models/index.js`](../../back-pre/app/models/index.js) (riga 254).
>
> **FK InnoDB reali**: `switchboard_config_id → switchboard_config.id` (`_ibfk_4`),
> `parent_id → switchboard_menu_item.id` (auto-referenza, `_ibfk_5`),
> `audio_file_id → switchboard_audio_file.id` (`_ibfk_6`). Indici non unici su tutte e tre.
> ORM: `hasMany`/`belongsTo` con `switchboard_config`; `hasMany(self, { as: "children", foreignKey:
> "parentId" })` + `belongsTo(self, { as: "parent" })`; `hasMany(switchboard_menu_action)`;
> `belongsTo(switchboard_audio_file, { as: "audioFile" })`.
>
> **Scrittori** (via [`Service/switchboardService.js`](../../back-pre/app/Service/switchboardService.js),
> chiamati da [`switchboard.controller.js`](../../back-pre/app/controllers/switchboard.controller.js)):
> - `createMenuItem` — `POST /api/sb/switchboard/menu` (richiede `propertyId`, `dtmfKey`, `label`).
> - `updateMenuItem` — `PUT /api/sb/switchboard/menu/:menuItemId`.
> - `deleteMenuItem` — `DELETE /api/sb/switchboard/menu/:menuItemId`: cancellazione **ricorsiva** dei
>   figli e delle azioni collegate.
> - `deleteConfig` — cancella tutti gli item della config.
>
> **Lettori**:
> - `getMenuTree` — `GET /api/sb/switchboard/menu/:propertyId`: albero completo (root + fino a 3 livelli
>   di `children`), ognuno con `switchboard_menu_action` ordinati per `executionOrder`, ordinati per
>   `position`.
> - **Runtime IVR** in [`switchboardService.js`](../../back-pre/app/Service/switchboardService.js)
>   `processDtmfInput`: `findOne({ where: { switchboardConfigId, dtmfKey: digits, isActive: 1,
>   parentId: <corrente|null> } })` a ogni pressione di tasto; poi decide se eseguire le azioni
>   (nodo terminale), scendere in un sotto-menù (`children` + `audioFileId`) o riprodurre l'audio
>   foglia.
>
> **Frontend**: nessuna UI nei sorgenti presenti (pannello di back-office esterno). Vedi
> [`switchboard_config.md`](switchboard_config.md).

---

## Verdetto: feature attiva, tabella mai popolata in questa copia DB

Codice completo; **0 righe**, `AUTO_INCREMENT = 1`. Esempi sotto derivati da commenti del model, dal
codice `processDtmfInput` / `gatherDTMF` (`valid_digits: "0123456789*#"`) e dalla struttura ad albero
gestita da `getMenuTree`.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### switchboard_config_id
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: **FK** `_ibfk_4` → [`switchboard_config`](switchboard_config.md)`.id`.
- **Descrizione**: centralino a cui appartiene la voce di menù. Impostato lato server da
  `createMenuItem` a partire dal `propertyId` del body (`config.id`).
- **Esempio di valori**: teorico `1` (id di `switchboard_config`)

### parent_id
- **Tipo**: `int`, `DEFAULT NULL`
- **Nullabile**: Sì
- **Relazioni**: **FK** auto-referenziale `_ibfk_5` → `switchboard_menu_item.id`. ORM alias
  `children` / `parent`.
- **Descrizione**: voce di menù genitore; `null` = voce di primo livello (root). L'IVR naviga l'albero
  usando `parent_id` come contesto corrente (`processDtmfInput` filtra per `parentId` = id del menù
  in cui si trova il chiamante, oppure `null` alla radice). `getMenuTree` carica fino a 3 livelli di
  discendenti.
- **Esempio di valori**: `null` (root), teorico `1`, `2` (id del genitore)

### dtmf_key
- **Tipo**: `varchar(2)`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: tasto che il chiamante deve premere per raggiungere questa voce, nel contesto del
  genitore. La raccolta DTMF è configurata con `valid_digits: "0123456789*#"` e `maximum_digits: 1`
  ([`telnyx.helper.js`](../../back-pre/app/controllers/helpers/telnyx.helper.js) `gatherDTMF`), quindi
  di fatto un singolo carattere fra `0-9`, `*`, `#`. Nessun vincolo di unicità a schema fra fratelli:
  la selezione usa `findOne` (prima riga che combacia).
- **Esempio di valori**: `"1"`, `"2"`, `"0"`, `"*"`, `"#"`
- **Enumeratore**: no — singolo carattere DTMF; eventuale `CHECK` su `[0-9*#]`, non un `enum` di dominio.

### label
- **Tipo**: `varchar(255)`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: descrizione/etichetta della voce di menù, mostrata nel pannello di gestione e
  riportata nei log di step (`menu_item_matched`, `label`). Nessun uso vocale (l'audio è un file, non
  TTS).
- **Esempio di valori**: teorico `"Reception"`, `"Ristorante"`, `"Prenotazioni"`

### audio_file_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: **FK** `_ibfk_6` → [`switchboard_audio_file`](switchboard_audio_file.md)`.id`; ORM alias
  `audioFile`.
- **Descrizione**: audio associato alla voce. Comportamento in `processDtmfInput` dopo il match:
  - se la voce ha `children` **e** `audio_file_id` → riproduce l'audio e riapre un `gather` nel
    sotto-menù (`parentMenuItemId = menuItem.id`);
  - se la voce **non** ha figli ma ha `audio_file_id` → riproduce l'audio come "foglia"
    (`finalNode: true`) e poi chiude;
  - se non ha né figli né audio → chiude la chiamata.
- **Esempio di valori**: teorico `1`, `2`

### position
- **Tipo**: `int`, `NOT NULL`, `DEFAULT 0`
- **Nullabile**: No
- **Descrizione**: ordinamento fra voci con lo stesso genitore. Usato solo da `getMenuTree`
  (`order: [["position", "ASC"]]`) per la resa nel pannello; l'IVR seleziona per `dtmf_key`, non per
  posizione.
- **Esempio di valori**: `0` (default), `1`, `2`, ..

### is_active
- **Tipo**: `smallint`, `NOT NULL`, `DEFAULT 1`
- **Nullabile**: No
- **Descrizione**: voce abilitata. `processDtmfInput` include `isActive: 1` nella `where`: una voce
  disattivata è invisibile all'IVR (input trattato come "DTMF non valido" → hangup). `getMenuTree`
  invece **non** filtra su `is_active` (il pannello le vede tutte).
- **Esempio di valori**: `1` (default, attiva), `0` (disattivata)
- **Enumeratore**: no — booleano (`is_active` → `bool`).

---

## Osservazioni verificate nel codice (criticità)

- **Nessun vincolo di unicità** `(switchboard_config_id, parent_id, dtmf_key)`: due voci con lo stesso
  tasto nello stesso livello sono ammesse; l'IVR ne prende una arbitraria (`findOne`).
- **Profondità albero**: `getMenuTree` carica **fisso 3 livelli** di `children`; `processDtmfInput`
  naviga un livello per pressione senza limite esplicito, ma il pannello non mostrerebbe oltre il 3°.
- **`position` incoerente con la navigazione**: serve solo alla UI; l'IVR ignora l'ordine.
- **`is_active` filtrato solo a runtime**, non in `getMenuTree`.
- **Input non valido → hangup immediato** (`processDtmfInput` `dtmf_no_menu_match`): nessun "hai
  premuto un tasto errato, riprova".

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `is_active` | `smallint` 0/1 | `bool` (non enum) |
| `dtmf_key` | `varchar(2)` | non enum: singolo carattere DTMF, eventuale `CHECK ([0-9*#])` |
