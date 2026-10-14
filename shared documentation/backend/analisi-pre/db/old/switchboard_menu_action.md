# Tabella `switchboard_menu_action`

> Fonti: schema del DB `local_sb` (**0 righe**, `AUTO_INCREMENT = 1`, `CREATE_TIME 2026-07-27`); model
> [`back-pre/app/models/switchboardMenuAction.model.js`](../../back-pre/app/models/switchboardMenuAction.model.js)
> (`sequelize.define("switchboard_menu_action", …, { freezeTableName: true, timestamps: false, underscored: true })`),
> registrato come `db.switchboardMenuAction` in [`models/index.js`](../../back-pre/app/models/index.js) (riga 258).
>
> **FK InnoDB reale**: `menu_item_id → switchboard_menu_item.id` (`_ibfk_1`, indice non unico). ORM
> `hasMany`/`belongsTo` con `switchboard_menu_item`.
>
> **Scrittori** (via [`Service/switchboardService.js`](../../back-pre/app/Service/switchboardService.js)):
> - `createMenuAction` — `POST /api/sb/switchboard/action` (richiede `propertyId`, `menuItemId`,
>   `actionType`; il controller rivalida `actionType ∈ {hangup, forward_sip, send_whatsapp}`).
> - `updateMenuAction` — `PUT /api/sb/switchboard/action/:actionId`.
> - `deleteMenuAction` — `DELETE /api/sb/switchboard/action/:actionId`.
> - `deleteMenuItem` / `deleteConfig` — cancellano le azioni dei menù rimossi.
>
> **Lettori**:
> - `getMenuTree` — incluse in ogni voce, ordinate per `executionOrder`.
> - **Runtime IVR** `processDtmfInput` → `executeMenuAction`: dopo il match della voce, esegue le
>   azioni **ordinate per `executionOrder`**; un'azione "terminale" (`hangup`, `forward_sip`)
>   interrompe il ciclo, `send_whatsapp` non è terminale.
>
> **Frontend**: nessuna UI nei sorgenti presenti. Vedi [`switchboard_config.md`](switchboard_config.md).

---

## Verdetto: feature attiva, tabella mai popolata in questa copia DB

Codice completo; **0 righe**, `AUTO_INCREMENT = 1`. Gli esempi di `action_config` sotto sono i payload
che `executeMenuAction` legge (`config.sipEndpointId`, `actionConfig.templateName`,
`actionConfig.recipientPhone`, `actionConfig.language`, `actionConfig.bodyParameters`,
`actionConfig.headerParameters`).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### menu_item_id
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: **FK** `_ibfk_1` → [`switchboard_menu_item`](switchboard_menu_item.md)`.id`.
- **Descrizione**: voce di menù a cui è collegata l'azione. Una voce può avere più azioni, eseguite in
  sequenza per `execution_order`.
- **Esempio di valori**: teorico `1`, `2`

### action_type
- **Tipo**: `enum('hangup','forward_sip','send_whatsapp')`, `NOT NULL` — **già ENUM nativo MySQL**
- **Nullabile**: No
- **Descrizione**: tipo di azione eseguita quando il chiamante seleziona la voce. Semantica in
  `executeMenuAction`:
  - `hangup` → `telnyxHelper.hangupCall`; **terminale** (interrompe le azioni successive).
  - `forward_sip` → inoltra al SIP endpoint dello stabilimento (`sipHelper.forwardCallToSip`) usando
    caller ID secondo `switchboard_config.forward_caller_id_mode`, timeout ring 90 s
    (`SIP_FORWARD_TIMEOUT_SECS`); registra il forward per correlare il fallimento del B-leg e, in caso
    di fallimento, riproduce `fallback_audio_file_id`. Eseguita solo se `config.sipUsername` e
    `config.sipDomain` sono presenti (altrimenti `action_forward_sip_skipped`). **Terminale**.
  - `send_whatsapp` → invia un template WhatsApp (`whatsappHelper.sendTemplateMessage`) al
    `actionConfig.recipientPhone` o, in mancanza, al numero del chiamante; usa il numero WA custom
    della config o quello generico `env.WHATSAPP_PHONE_NUMBER_ID`. **Non** terminale (l'IVR prosegue).
- **Esempio di valori**: `"forward_sip"`, `"send_whatsapp"`, `"hangup"`
- **Enumeratore**: **sì** — `enum SwitchboardActionType { Hangup, ForwardSip, SendWhatsApp }`
  (ogni valore ha un ramo di codice/integrazione dedicato). Il commento del model lo dichiara
  "extensible".

### action_config
- **Tipo**: `json` (`longtext` `utf8mb4_bin`), `DEFAULT ({})` *(default oggetto vuoto lato model;
  a schema `DEFAULT NULL`)*
- **Nullabile**: Sì
- **Descrizione**: configurazione specifica per `action_type`. Campi letti dal codice:
  - `hangup`: nessuno (`{}`).
  - `forward_sip`: il commento del model cita `{ "sipEndpointId": "..." }`, ma `executeMenuAction`
    **non legge `action_config`** per il forward — usa direttamente `switchboard_config`
    (`sipUsername`, `sipDomain`, `forwardCallerIdMode`, `forwardCustomCallerId`, `forwardCallerIdName`,
    `fallbackAudioFileId`). Quindi per `forward_sip` il contenuto è di fatto ignorato.
  - `send_whatsapp`: `templateName` (obbligatorio, altrimenti skip), `recipientPhone` (opzionale;
    default = numero chiamante), `language` (default `"it"`), `bodyParameters` (array, default `[]`),
    `headerParameters` (array, default `[]`).
- **Esempio di valori** (sotto-valori):
  - `{}` — per `hangup`
  - `{ "sipEndpointId": "..." }` — per `forward_sip` (dichiarato dal model; non usato dal runtime)
  - `{ "templateName": "avviso_reception", "language": "it", "recipientPhone": "+3933...",
    "bodyParameters": [ { "type": "text", "text": "Bagno Reception" } ], "headerParameters": [] }`
    — per `send_whatsapp`

### execution_order
- **Tipo**: `int`, `NOT NULL`, `DEFAULT 0`
- **Nullabile**: No
- **Descrizione**: ordine di esecuzione delle azioni della stessa voce. `getMenuTree` e
  `processDtmfInput` ordinano `ASC` prima di eseguire (`actions.sort((a,b) => a.executionOrder -
  b.executionOrder)`).
- **Esempio di valori**: `0` (default), `1`, `2`, ..

---

## Osservazioni verificate nel codice (criticità)

- **`action_config` per `forward_sip` è un contratto morto**: il model lo documenta ma il runtime
  ignora `sipEndpointId` e prende tutto da `switchboard_config`.
- **`send_whatsapp` non terminale**: se posta prima di `forward_sip` nell'ordine, entrambe vengono
  eseguite; se è l'unica azione e non ha `templateName`, la chiamata prosegue verso sotto-menù/audio o
  hangup (nessun errore).
- **Nessun `button_parameters`** nel path IVR (a differenza dell'endpoint API `sendWaTemplateMessage`,
  che li supporta).
- **Enum applicativa duplicata**: la whitelist `["hangup","forward_sip","send_whatsapp"]` è ripetuta
  nel controller oltre che nell'`ENUM` di colonna.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `action_type` | `ENUM('hangup','forward_sip','send_whatsapp')` | `enum SwitchboardActionType { Hangup, ForwardSip, SendWhatsApp }` |
