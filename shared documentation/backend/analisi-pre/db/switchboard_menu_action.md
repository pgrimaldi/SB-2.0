# Tabella `switchboard_menu_action`

> Fonti: schema del DB `local_sb` (**0 righe**); **confermato 0 righe anche nel dump di produzione `sb_prod_dump_2026_09_08`** — la tabella è esportata solo come schema (nessun `INSERT`) su queste due copie; model
> [`back-pre/app/models/switchboardMenuAction.model.js`](../../../../../../back-pre/app/models/switchboardMenuAction.model.js)
> (`sequelize.define("switchboard_menu_action", …, { freezeTableName: true, timestamps: false, underscored: true })`),
> registrato come `db.switchboardMenuAction` in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga 258).
>
> **FK InnoDB reale**: `menu_item_id → switchboard_menu_item.id` (`_ibfk_1`, indice non unico). ORM
> `hasMany`/`belongsTo` con `switchboard_menu_item`.
>
> **Scrittori** (via [`Service/switchboardService.js`](../../../../../../back-pre/app/Service/switchboardService.js)):
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
>
> **⚠️ Aggiornamento 2026-09-11 — verificato su `summer_booking_dev`** (RDS, vedi
> [`switchboard_config.md`](switchboard_config.md)): **1 riga reale** — `menu_item_id = 4` (la voce
> "redirect" di [`switchboard_menu_item.md`](switchboard_menu_item.md)), `action_type = "forward_sip"`,
> `action_config = {}` (oggetto **vuoto**), `execution_order = 0`. Conferma **esattamente** l'analisi
> sotto: per `forward_sip` il contenuto di `action_config` è realmente ignorato/non popolato, i
> parametri di forward vengono presi da `switchboard_config`. **Nessuna azione `send_whatsapp` reale
> osservata** in nessun ambiente, nonostante esistano sia una voce di menù "whatsapp" (id 3, senza
> azioni) sia 3 template WhatsApp approvati ([`switchboard_wa_template.md`](switchboard_wa_template.md)):
> il collegamento IVR→WhatsApp risulta configurato solo a metà.

---

## Verdetto: feature attiva, un solo esempio reale (`forward_sip`) in un ambiente diverso da questa copia DB

Codice completo; **0 righe** su `local_sb` e sul dump di produzione, **1 riga reale** su
`summer_booking_dev` (vedi sopra, `forward_sip` con `action_config = {}`). Gli esempi di `action_config`
sotto per `send_whatsapp` restano **teorici** (nessuna riga reale osservata) e sono i payload che
`executeMenuAction` legge (`config.sipEndpointId`, `actionConfig.templateName`,
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
