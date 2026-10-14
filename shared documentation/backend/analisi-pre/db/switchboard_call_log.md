# Tabella `switchboard_call_log`

> Fonti: schema del DB `local_sb` (**0 righe**); **confermato 0 righe anche nel dump di produzione `sb_prod_dump_2026_09_08`** — la tabella è esportata solo come schema (nessun `INSERT`) su queste due copie; model
> [`back-pre/app/models/switchboardCallLog.model.js`](../../../../../../back-pre/app/models/switchboardCallLog.model.js)
> (`sequelize.define("switchboard_call_log", …, { freezeTableName: true, timestamps: true, updatedAt: false, underscored: true })`
> → solo `created_at`), registrato come `db.switchboardCallLog` in
> [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga 270).
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
> - [`switchboard.controller.js`](../../../../../../back-pre/app/controllers/switchboard.controller.js)
>   `handleCallWebhook` — `POST /api/public/switchboard/webhook/call` (**no auth**): una riga per
>   webhook Telnyx, con `event_type`/`call_*`/`from`/`to`/`dtmf` da `parseCallWebhook`, `raw_payload`
>   = corpo completo, `handler_result` = risultato di `handleIncomingCall`, oppure `error_message` se
>   l'handler ha lanciato.
> - [`Service/switchboardService.js`](../../../../../../back-pre/app/Service/switchboardService.js) `logStep` —
>   fire-and-forget a ogni passo del flusso IVR, con `event_type = "step.<nome>"` (es.
>   `step.config_found`, `step.gather_dtmf_started`, `step.dtmf_received`, `step.action_forward_sip_done`,
>   `step.wake_push_failed`, …); `handler_result` = oggetto dati del passo, `error_message` popolato
>   sui passi di errore.
>
> **Lettori**: **nessuno** in tutto `back-pre` (grep su `app/`, `cronjob/`, `server.js`): nessun
> controller, service o cron rilegge queste righe. Tabella **puramente diagnostica / forensica**.
>
> **Frontend**: nessun consumo.
>
> **⚠️ Aggiornamento 2026-09-11 — verificato su `summer_booking_dev`** (RDS, vedi
> [`switchboard_config.md`](switchboard_config.md)): **7 355 righe reali**, 2026-04-22 → 2026-07-08
> (~2,5 mesi), **596 sessioni di chiamata distinte** → in quest'ambiente `SB_CALL_LOGGING_ENABLED=1` è
> **attivo** e la tabella è la più popolosa dell'intero dominio Switchboard. Distribuzione reale di
> `event_type` (30 valori distinti osservati, `GROUP BY` decrescente):
> `call.initiated` (860), `call.hangup` (815), `step.config_not_found` (636),
> `call.playback.ended` (514), `call.playback.started` (513), `call.gather.ended` (449),
> `call.dtmf.received` (439), `step.dtmf_received` (437), `step.menu_item_matched` (435),
> `call.answered` (264), `step.call_answered_via_api` (224), `step.config_found` (224),
> `step.gather_dtmf_started` (224), `step.gather_dtmf_starting` (224),
> `step.submenu_gather_starting` (218), `step.action_executing` (204),
> `step.action_forward_sip_done` (204), `step.wake_push_sent` (172), `call.bridged` (152),
> `step.hangup_after_fallback_audio` (44), `step.config_not_found_on_answer` (40),
> `step.forward_failed_fallback_audio` (39), `step.leaf_audio_playing` (7),
> `step.hangup_no_audio_no_children` (6), `step.dtmf_timeout` (5), `step.dtmf_no_menu_match` (2),
> `step.wake_push_failed` (1), `step.hangup_after_leaf_playback` (1), `call.unhold` (1), `call.hold` (1).
> Questo vocabolario reale **differisce sensibilmente** da quello ipotizzato sotto (nomi di step diversi,
> e sono loggati anche eventi Telnyx non citati come "gestiti" — `call.dtmf.received`, `call.playback.*`,
> `call.bridged`, `call.hold`/`call.unhold`): l'elenco sotto resta **parzialmente teorico**, quello sopra
> è **osservato**.

---

## Verdetto: tabella di log **attiva e molto popolata** (in un ambiente diverso da questa copia del DB)

**0 righe** su `local_sb` e sul dump di produzione; **7 355 righe reali** su `summer_booking_dev`
(vedi sopra) — quindi **non** "mai riletta/mai scritta" in assoluto, ma **scritta e mai riletta**:
resta confermato che **nessun processo applicativo rilegge queste righe** (solo scrittura, nessuna
query di lettura nel codice) — la tabella è diagnostica anche dove è popolata. Utile solo per debug
manuale del flusso chiamate. Nella riscrittura: sostituibile da logging/observability strutturato (non
una tabella di dominio) — **oppure**, su decisione del committente per questo dominio, creata comunque
come tabella C7 con retention breve (vedi
[`analisi-post/db/switchboard/switchboard_voice_call_diagnostics.md`](../../analisi-post/db/switchboard/switchboard_voice_call_diagnostics.md)).
Valori sotto = campi di `parseCallWebhook` + nomi di step di `logStep`, integrati con i valori reali
osservati sopra.

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
  `"step.action_whatsapp_sent"`; **verificato** su `summer_booking_dev`: vocabolario reale di **30**
  valori distinti (vedi elenco completo con conteggi nella nota 2026-09-11 in testa alla scheda) — più
  ricco e in parte diverso da quello teorico sopra (nessun `step.action_whatsapp_sent` osservato; in
  compenso `step.config_not_found`/`step.config_not_found_on_answer`,
  `step.forward_failed_fallback_audio`, `step.hangup_after_fallback_audio`,
  `step.hangup_no_audio_no_children`, `step.hangup_after_leaf_playback`, `step.dtmf_timeout`,
  `step.dtmf_no_menu_match`, `step.leaf_audio_playing`, `step.action_executing`,
  `step.submenu_gather_starting`, `step.call_answered_via_api`, `step.gather_dtmf_starting` non erano
  nell'elenco teorico originale).
- **Enumeratore**: **non** un enum di dominio: passthrough di nomi evento Telnyx + costanti di
  logging. Da lasciare stringa (diagnostica) — confermato dalla numerosità reale osservata (30 valori,
  in crescita a ogni nuovo step aggiunto al codice).

### call_control_id
- **Tipo**: `varchar(150)`
- **Nullabile**: Sì
- **Descrizione**: id di call control Telnyx (`payload.call_control_id`), usato per impartire i comandi
  (answer/gather/hangup/transfer).
- **Esempio di valori**: token base64 Telnyx (~100+ caratteri), es. `"v2:AbC…=="`; **verificato** su
  `summer_booking_dev`: prefisso reale `"v3:…"` (non `"v2:"` come ipotizzato) — Telnyx ha
  evidentemente aggiornato il formato del `call_control_id`.

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
- **Esempio di valori**: teorico `9669`; **verificato** su `summer_booking_dev`: `96` quando
  valorizzato — ma **`NULL` in 5 818/7 355 righe (79%)**, molte più di quanto ci si aspetterebbe dal
  solo caso `config_not_found`: anche eventi con `switchboard_config_id` **già risolto** (es.
  `call.initiated`, `call.answered`, `call.hangup`, `call.playback.*`) hanno spesso `property_id NULL`
  nella stessa riga — la risoluzione dei due campi **non è coerente riga per riga**.

### switchboard_config_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: **FK** `_ibfk_4` → [`switchboard_config`](switchboard_config.md)`.id`.
- **Descrizione**: config risolta dall'handler (`result.configId` / `data.configId`).
- **Esempio di valori**: teorico `1`; **verificato** su `summer_booking_dev`: `1` quando valorizzato —
  `NULL` in 4 966/7 355 righe (68%), per lo stesso motivo di `property_id` sopra.

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
  `{ "configId": 1, "propertyId": 9669, "audioFileId": 2 }` (step); **verificato** su
  `summer_booking_dev` (valori reali osservati): per `call.initiated` →
  `{ "handled": true, "action": "answered_via_api", "configId": 1 }`; per `step.config_found` →
  `{ "configId": 1, "propertyId": 96, "mode": "call_control_app" }`; per
  `step.gather_dtmf_starting` → `{ "configId": 1, "propertyId": 96, "audioFileId": 1, "audioUrl":
  "https://…/api/public/switchboard/audio/1" }` — conferma il formato dell'URL pubblico audio
  documentato in [`switchboard_audio_file.md`](switchboard_audio_file.md); `action: "answered_via_api"`
  non era fra i valori teorici elencati.

### error_message
- **Tipo**: `text`
- **Nullabile**: Sì
- **Descrizione**: messaggio d'errore se l'handler ha lanciato un'eccezione, o l'errore del singolo
  passo (`logStep(..., errorMessage)`).
- **Esempio di valori**: teorico `"Request failed with status code 422"`; **verificato** su
  `summer_booking_dev`: un caso reale su `step.wake_push_failed` con
  `"Invalid data payload key: from"` — un **bug reale** nella costruzione del payload del push FCM
  "wake" (probabile conflitto con una chiave riservata `from` nel payload FCM).

### created_at
- **Tipo**: `datetime`, `NOT NULL` (Sequelize `timestamps: true`, `updatedAt: false`)
- **Nullabile**: No
- **Esempio di valori**: teorico `2026-07-28 15:42:10`

---

## Osservazioni verificate nel codice (criticità)

- **Nessun lettore applicativo** (confermato anche su `summer_booking_dev`: 7 355 righe scritte, zero
  query di lettura nel codice) → non è un dato di dominio: nella riscrittura va sostituita da
  tracing/log strutturati, non migrata come tabella di dominio.
- **`event_type` a doppia natura** (eventi Telnyx + `step.*`) rende la colonna inutilizzabile come
  dimensione stabile — **confermato**: 30 valori distinti reali, in crescita ogni volta che il codice
  aggiunge uno step.
- **Scritture fire-and-forget** (`.catch(console.error)`): perdite silenziose di righe di log sotto
  carico o errore DB.
- **`raw_payload` senza redazione**: memorizza numeri di telefono e l'intero payload Telnyx (dato
  personale) senza TTL/retention. Su `summer_booking_dev` la tabella conserva **tutti** i numeri di
  telefono delle chiamate di test degli ultimi 2,5 mesi, senza alcuna scadenza.
- **Non ripulita da `deleteConfig`**.
- **Resa incoerente di `property_id`/`switchboard_config_id`**: anche a config risolta, molte righe
  hanno uno dei due (o entrambi) `NULL` — 79%/68% delle righe reali rispettivamente (vedi campi sopra).
  Non è solo "in attesa di risoluzione": la stessa sessione di chiamata alterna righe con e senza questi
  valori.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `direction` | `varchar(20)` | `enum CallDirection { Incoming, Outgoing }` (tipizzazione del grezzo Telnyx) |
| `event_type` | `varchar(50)` | non enum: passthrough eventi Telnyx + costanti di logging |
