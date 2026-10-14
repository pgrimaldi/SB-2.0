# Tabella `switchboard_call_log`

> Fonti: schema del DB `local_sb` (**0 righe**, `AUTO_INCREMENT = 1`, `CREATE_TIME 2026-07-27`); model
> [`back-pre/app/models/switchboardCallLog.model.js`](../../back-pre/app/models/switchboardCallLog.model.js)
> (`sequelize.define("switchboard_call_log", …, { freezeTableName: true, timestamps: true, updatedAt: false, underscored: true })`
> → solo `created_at`), registrato come `db.switchboardCallLog` in
> [`models/index.js`](../../back-pre/app/models/index.js) (riga 270).
>
> **FK InnoDB reali**: `property_id → properties.id` (`_ibfk_3`), `switchboard_config_id →
> switchboard_config.id` (`_ibfk_4`); indici non unici su entrambe. ORM: `belongsTo(property)` /
> `belongsTo(switchboardConfig)` (+ `hasMany` inversi).
>
> **Interruttore globale**: **tutte** le scritture sono condizionate a
> `process.env.SB_CALL_LOGGING_ENABLED === "1"`. Con il default (variabile non impostata) la tabella
> **non viene mai scritta**.
>
> **Scrittori** (solo se logging abilitato):
> - [`switchboard.controller.js`](../../back-pre/app/controllers/switchboard.controller.js)
>   `handleCallWebhook` — `POST /api/public/switchboard/webhook/call` (**no auth**): una riga per
>   webhook Telnyx, con `event_type`/`call_*`/`from`/`to`/`dtmf` da `parseCallWebhook`, `raw_payload`
>   = corpo completo, `handler_result` = risultato di `handleIncomingCall`, oppure `error_message` se
>   l'handler ha lanciato.
> - [`Service/switchboardService.js`](../../back-pre/app/Service/switchboardService.js) `logStep` —
>   fire-and-forget a ogni passo del flusso IVR, con `event_type = "step.<nome>"` (es.
>   `step.config_found`, `step.gather_dtmf_started`, `step.dtmf_received`, `step.action_forward_sip_done`,
>   `step.wake_push_failed`, …); `handler_result` = oggetto dati del passo, `error_message` popolato
>   sui passi di errore.
>
> **Lettori**: **nessuno** in tutto `back-pre` (grep su `app/`, `cronjob/`, `server.js`): nessun
> controller, service o cron rilegge queste righe. Tabella **puramente diagnostica / forensica**.
>
> **Frontend**: nessun consumo.

---

## Verdetto: tabella di sola diagnostica, disabilitata di default

**0 righe**, `AUTO_INCREMENT = 1`. Scritta solo con `SB_CALL_LOGGING_ENABLED=1` e **mai riletta dal
codice**. Utile solo per debug manuale del flusso chiamate. Nella riscrittura: sostituibile da
logging/observability strutturato (non una tabella di dominio). Valori sotto = campi di
`parseCallWebhook` + nomi di step di `logStep`.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### event_type
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: tipo di evento. Due famiglie:
  - **eventi Telnyx** (`callEvent.eventType`): `"call.initiated"`, `"call.answered"`,
    `"call.gather.ended"`, `"call.playback.ended"`, `"call.hangup"` (gli unici gestiti da
    `handleIncomingCall`).
  - **passi interni** (`logStep`): prefisso `"step."` + nome, es. `"step.config_found"`,
    `"step.call_rejected"`, `"step.gather_dtmf_started"`, `"step.dtmf_received"`,
    `"step.menu_item_matched"`, `"step.action_forward_sip_done"`, `"step.forward_failed_hangup"`,
    `"step.wake_push_sent"`, `"step.hangup_after_timeout_no_audio"`.
- **Esempio di valori**: `"call.initiated"`, `"call.gather.ended"`, `"step.config_found"`,
  `"step.action_whatsapp_sent"`
- **Enumeratore**: **non** un enum di dominio: passthrough di nomi evento Telnyx + costanti di
  logging. Da lasciare stringa (diagnostica).

### call_control_id
- **Tipo**: `varchar(150)`
- **Nullabile**: Sì
- **Descrizione**: id di call control Telnyx (`payload.call_control_id`), usato per impartire i comandi
  (answer/gather/hangup/transfer).
- **Esempio di valori**: token base64 Telnyx (~100+ caratteri), es. `"v2:AbC…=="`

### call_leg_id
- **Tipo**: `varchar(150)`
- **Nullabile**: Sì
- **Descrizione**: id del leg di chiamata Telnyx (`payload.call_leg_id`).
- **Esempio di valori**: UUID Telnyx

### call_session_id
- **Tipo**: `varchar(150)`
- **Nullabile**: Sì
- **Descrizione**: id di sessione Telnyx (`payload.call_session_id`); condiviso da A-leg e B-leg,
  usato in `pendingSipForwards` per correlare il fallimento del forward SIP al leg del chiamante.
- **Esempio di valori**: UUID Telnyx

### direction
- **Tipo**: `varchar(20)`
- **Nullabile**: Sì
- **Descrizione**: verso della chiamata (`payload.direction`). `handleIncomingCall` distingue
  `"incoming"` (chiamata in ingresso da gestire) da `"outgoing"` (B-leg del nostro forward).
- **Esempio di valori**: `"incoming"`, `"outgoing"`
- **Enumeratore**: `enum CallDirection { Incoming, Outgoing }` (se si vuole tipizzare il grezzo Telnyx).

### from_number
- **Tipo**: `varchar(30)`
- **Nullabile**: Sì
- **Descrizione**: numero chiamante (`payload.from`), E.164.
- **Esempio di valori**: `"+393514648861"`

### to_number
- **Tipo**: `varchar(30)`
- **Nullabile**: Sì
- **Descrizione**: numero chiamato (`payload.to`); per gli eventi in ingresso è il numero Telnyx del
  centralino, chiave di lookup di `switchboard_config`.
- **Esempio di valori**: `"+390510000000"`

### dtmf_digits
- **Tipo**: `varchar(20)`
- **Nullabile**: Sì
- **Descrizione**: cifre DTMF raccolte (`payload.digits`) all'evento `call.gather.ended`.
- **Esempio di valori**: `"1"`, `"2"`, `""` (timeout senza input)

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: **FK** `_ibfk_3` → [`properties`](properties.md)`.id`.
- **Descrizione**: stabilimento risolto dall'handler (`result.propertyId` / `data.propertyId`).
  `null` finché il contesto non è stato risolto (es. `config_not_found`).
- **Esempio di valori**: teorico `9669`

### switchboard_config_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: **FK** `_ibfk_4` → [`switchboard_config`](switchboard_config.md)`.id`.
- **Descrizione**: config risolta dall'handler (`result.configId` / `data.configId`).
- **Esempio di valori**: teorico `1`

### raw_payload
- **Tipo**: `json` (`longtext` `utf8mb4_bin`)
- **Nullabile**: Sì
- **Descrizione**: corpo completo del webhook Telnyx così come ricevuto (`req.body`). Per le righe di
  `logStep` non è valorizzato.
- **Esempio di valori** (sotto-valori): `{ "data": { "event_type": "call.initiated", "payload": {
  "call_control_id": "v2:…", "from": "+3935…", "to": "+3905…", "direction": "incoming", "state":
  "parked" } } }`

### handler_result
- **Tipo**: `json` (`longtext` `utf8mb4_bin`)
- **Nullabile**: Sì
- **Descrizione**: valore di ritorno di `handleIncomingCall` (per `handleCallWebhook`) o l'oggetto
  `data` del passo (per `logStep`).
- **Esempio di valori** (sotto-valori): `{ "handled": true, "action": "gathering_dtmf", "configId": 1 }`;
  `{ "handled": false, "reason": "No active switchboard for this number" }`;
  `{ "configId": 1, "propertyId": 9669, "audioFileId": 2 }` (step)

### error_message
- **Tipo**: `text`
- **Nullabile**: Sì
- **Descrizione**: messaggio d'errore se l'handler ha lanciato un'eccezione, o l'errore del singolo
  passo (`logStep(..., errorMessage)`).
- **Esempio di valori**: teorico `"Request failed with status code 422"`

### created_at
- **Tipo**: `datetime`, `NOT NULL` (Sequelize `timestamps: true`, `updatedAt: false`)
- **Nullabile**: No
- **Esempio di valori**: teorico `2026-07-28 15:42:10`

---

## Osservazioni verificate nel codice (criticità)

- **Nessun lettore applicativo** + **disabilitata di default** → non è un dato di dominio: nella
  riscrittura va sostituita da tracing/log strutturati, non migrata come tabella.
- **`event_type` a doppia natura** (eventi Telnyx + `step.*`) rende la colonna inutilizzabile come
  dimensione stabile.
- **Scritture fire-and-forget** (`.catch(console.error)`): perdite silenziose di righe di log sotto
  carico o errore DB.
- **`raw_payload` senza redazione**: memorizza numeri di telefono e l'intero payload Telnyx (dato
  personale) senza TTL/retention.
- **Non ripulita da `deleteConfig`**.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `direction` | `varchar(20)` | `enum CallDirection { Incoming, Outgoing }` (tipizzazione del grezzo Telnyx) |
| `event_type` | `varchar(50)` | non enum: passthrough eventi Telnyx + costanti di logging |
