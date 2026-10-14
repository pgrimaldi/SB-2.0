# Tabella `switchboard_wa_msg_log`

> Fonti: schema del DB `local_sb` (**0 righe**); **confermato 0 righe anche nel dump di produzione `sb_prod_dump_2026_09_08`** — la tabella è esportata solo come schema (nessun `INSERT`) su queste due copie; model
> [`back-pre/app/models/switchboardWaMsgLog.model.js`](../../../../../../back-pre/app/models/switchboardWaMsgLog.model.js)
> (`sequelize.define("switchboard_wa_msg_log", …, { freezeTableName: true, timestamps: false, underscored: true })`),
> registrato come `db.switchboardWaMsgLog` in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga 276).
>
> **Nessuna FK**; **4 indici non unici** (dichiarati nel model, presenti a schema): `wamid`, `status`,
> `event_type`, `received_at`. Nessuna associazione ORM.
>
> **Scrittore** — unico: [`switchboard.controller.js`](../../../../../../back-pre/app/controllers/switchboard.controller.js)
> `handleWaWebhook` — `POST /api/public/switchboard/webhook/wa` (**no auth sul POST**; l'handshake
> `GET` verifica `hub.verify_token`, ma il POST **non** valida `X-Hub-Signature-256` pur avendo
> `WHATSAPP_APP_SECRET` — vedi [§9 di 01-…backend.md](../../progettazione/01-criticita-e-indicazioni-backend.md)).
> Per ogni `entry.changes[].value` con `field = "messages"` inserisce:
> - una riga `event_type = "status"` per ogni elemento di `value.statuses[]` (aggiornamenti di
>   consegna: sent/delivered/read/failed);
> - una riga `event_type = "message_in"` per ogni elemento di `value.messages[]` (messaggi in arrivo
>   dagli utenti finali).
> Ogni insert è in `try/catch` con `console.error`; l'endpoint risponde **sempre `200`
> `EVENT_RECEIVED`** per evitare i retry di Meta.
>
> **Lettori**: **nessuno** in tutto `back-pre` (grep su `app/`, `cronjob/`, `server.js`). Tabella
> **puramente forensica** (il commento del model: *"useful for forensics"*).
>
> **Frontend**: nessun consumo.
>
> **⚠️ Aggiornamento 2026-09-11 — verificato su `summer_booking_dev`** (RDS, vedi
> [`switchboard_config.md`](switchboard_config.md)): **11 righe reali**, 2026-05-26 → 2026-06-21. Tutte
> e **11 hanno `event_type = "status"`** — **zero righe `"message_in"`**: nessun messaggio in arrivo è
> mai stato ricevuto in questo ambiente, nemmeno di test. Di conseguenza tutti i campi legati
> all'inbound (`message_type`, `message_body`, `contact_name`, `from`, `conversation_id`,
> `conversation_origin`) risultano **sempre `NULL`** nelle righe reali. 5 righe sono `status = "failed"`
> (`error_code 131026`, `"Message undeliverable"`, verso lo **stesso numero di test** — probabile
> numero WhatsApp non registrato usato per collaudo), le altre 6 sono `sent`/`delivered`/`read` verso 2
> numeri reali distinti. `pricing_model` osservato: **`"PMP"`** in tutte le righe con pricing valorizzato
> — **mai `"CBP"`** (coerente con la transizione di Meta al *per-message pricing* nel 2025; l'esempio
> "CBP" sotto era solo teorico). `pricing_category` osservato: sempre `"utility"`. `phone_number_id` /
> `display_phone_number` osservati **non corrispondono** a un numero custom dello stabilimento 96 (la
> cui `switchboard_config.whatsapp_phone_number_id` è `NULL`): sono quelli del **numero WhatsApp
> generico di piattaforma**, condiviso fra stabilimenti — vedi implicazione di design in
> [D-52/D-53](../../analisi-post/db/_domande-committente.md).

---

## Verdetto: tabella forense **realmente popolata ma mai riletta** (in un ambiente diverso da questa copia del DB)

**0 righe** su `local_sb` e sul dump di produzione; **11 righe reali** su `summer_booking_dev`, tutte di
tipo `status` (nessun `message_in` osservato). Nessun processo legge queste righe (nessun aggancio a
prenotazioni, nessuna notifica in-app, nessun cron) — confermato anche dove la tabella è popolata.
Nella riscrittura: se serve la tracciabilità degli eventi WhatsApp va nell'outbox/inbox del modulo
`Notification` (C6) con un contratto stabile, **oppure** — su decisione del committente per questo
dominio — creata comunque come tabella C7 con retention breve (vedi
[`analisi-post/db/switchboard/switchboard_messaging_wa_event_logs.md`](../../analisi-post/db/switchboard/switchboard_messaging_wa_event_logs.md)).
Valori sotto = campi del payload webhook Meta Cloud API letti da `handleWaWebhook`, integrati con i
valori reali osservati sopra.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### event_type
- **Tipo**: `varchar(20)`, `NOT NULL`, indice non unico
- **Nullabile**: No
- **Descrizione**: discrimina le due tipologie di riga: `"status"` (da `value.statuses[]`) o
  `"message_in"` (da `value.messages[]`).
- **Esempio di valori**: `"status"`, `"message_in"`; **verificato** su `summer_booking_dev`: **100%
  `"status"`** (11/11 righe) — nessun `"message_in"` osservato.
- **Enumeratore**: **sì** — `enum WaWebhookEventType { Status, MessageIn }`.

### wamid
- **Tipo**: `varchar(255)`, indice non unico
- **Nullabile**: Sì
- **Descrizione**: id del messaggio Meta (`st.id` per gli status — corrisponde all'id restituito da
  `POST /messages` all'invio; `m.id` per gli inbound).
- **Esempio di valori**: `"wamid.HBgLMzkz...=="`

### waba_id
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: id del WhatsApp Business Account che possiede la conversazione (`entry.id` del
  payload).
- **Esempio di valori**: teorico id numerico Meta come stringa

### phone_number_id
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: id del numero (Meta) che ha inviato/ricevuto il messaggio
  (`value.metadata.phone_number_id`).
- **Esempio di valori**: teorico id numerico Meta come stringa

### display_phone_number
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: numero in formato leggibile associato a `phone_number_id`
  (`value.metadata.display_phone_number`).
- **Esempio di valori**: `"39 051 0000000"`, `"+390510000000"`

### from
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: mittente del messaggio in arrivo (E.164 senza `+`), popolato solo per
  `event_type = "message_in"` (`m.from`).
- **Esempio di valori**: `"393514648861"`

### recipient_id
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: per gli `status`, l'utente finale che ha ricevuto il nostro messaggio
  (`st.recipient_id`). `null` per gli inbound.
- **Esempio di valori**: `"393514648861"`

### status
- **Tipo**: `varchar(20)`, indice non unico
- **Nullabile**: Sì
- **Descrizione**: stato di consegna (`st.status`); `null` per gli inbound.
- **Esempio di valori**: `"sent"`, `"delivered"`, `"read"`, `"failed"`; **verificato** su
  `summer_booking_dev`: tutti e 4 i valori osservati realmente (5× `failed`, 2× `sent`, 2× `delivered`,
  2× `read`).
- **Enumeratore**: **sì** — `enum WaMessageStatus { Sent, Delivered, Read, Failed }`.

### error_code
- **Tipo**: `int`
- **Nullabile**: Sì
- **Descrizione**: codice errore Meta del primo elemento di `st.errors[]` (presente quando
  `status = "failed"`).
- **Esempio di valori**: teorico `131047`, `470`; **verificato** su `summer_booking_dev`: **`131026`**
  ("Message undeliverable") in tutte le 5 righe fallite, verso lo stesso numero di test — coerente con
  un numero WhatsApp non registrato/non raggiungibile usato per collaudo, non con un bug applicativo.

### error_title
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: titolo errore Meta (`err.title`).
- **Esempio di valori**: teorico `"Re-engagement message"`

### error_message
- **Tipo**: `text`
- **Nullabile**: Sì
- **Descrizione**: messaggio errore Meta (`err.message`), con fallback su `err.error_data.details`.
- **Esempio di valori**: teorico `"Message failed to send because more than 24 hours have passed…"`

### conversation_id
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: id della conversazione ai fini della fatturazione Meta (`st.conversation.id`).
- **Esempio di valori**: teorico stringa esadecimale Meta

### conversation_origin
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: origine/categoria della conversazione (`st.conversation.origin.type`).
- **Esempio di valori**: `"marketing"`, `"utility"`, `"service"`, `"authentication"`,
  `"referral_conversion"`
- **Enumeratore**: **sì** — `enum WaConversationOrigin { Marketing, Utility, Service, Authentication,
  ReferralConversion }` (vocabolario Meta).

### pricing_billable
- **Tipo**: `tinyint(1)` (model `BOOLEAN`)
- **Nullabile**: Sì
- **Descrizione**: se la conversazione è fatturabile (`st.pricing.billable`), copiato solo se
  booleano.
- **Esempio di valori**: `1`, `0`, `null`
- **Enumeratore**: no — booleano (`bool?`).

### pricing_category
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: categoria di pricing Meta (`st.pricing.category`).
- **Esempio di valori**: `"marketing"`, `"utility"`, `"authentication"`, `"service"`
- **Enumeratore**: candidato `enum WaPricingCategory { Marketing, Utility, Authentication, Service }`
  (vocabolario Meta, evolve con la loro tariffazione — valutare passthrough).

### pricing_model
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: modello di pricing Meta (`st.pricing.pricing_model`).
- **Esempio di valori**: `"CBP"` (conversation-based), `"PMP"` *(valori Meta)*; **verificato** su
  `summer_booking_dev`: **sempre `"PMP"`**, mai `"CBP"` — coerente con la transizione di Meta al
  *per-message pricing* (2025) per la maggior parte delle categorie; nella riscrittura trattare `"CBP"`
  come valore storico/di fallback, non come lo scenario tipico atteso.

### message_type
- **Tipo**: `varchar(30)`
- **Nullabile**: Sì
- **Descrizione**: tipo del messaggio in arrivo (`m.type`), per `event_type = "message_in"`.
- **Esempio di valori**: `"text"`, `"button"`, `"interactive"`, `"reaction"`, `"image"`, `"document"`,
  `"audio"`, `"video"`, `"location"`, `"sticker"`, `"contacts"`
- **Enumeratore**: candidato `enum WaInboundMessageType { Text, Button, Interactive, Reaction, Image,
  Document, Audio, Video, Location, Sticker, Contacts, Unknown }` — passthrough Meta con fallback.

### message_body
- **Tipo**: `text`
- **Nullabile**: Sì
- **Descrizione**: testo del messaggio in arrivo. Regole di `handleWaWebhook`:
  - `text` → `m.text.body`
  - `button` → `m.button.text` o `m.button.payload`
  - `interactive` → titolo di `button_reply`/`list_reply` o `JSON.stringify(m.interactive)`
  - `reaction` → `` `${emoji} → ${message_id}` ``
  - tipi media → `JSON.stringify(m[m.type])` (riassunto JSON)
- **Esempio di valori**: `"Buongiorno, avete disponibilità?"`, `"👍 → wamid.HBg…"`,
  `"{\"id\":\"...\",\"mime_type\":\"image/jpeg\",\"sha256\":\"...\"}"`

### contact_name
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: nome profilo del contatto (`contact.profile.name`) associato a `m.from` in
  `value.contacts[]`.
- **Esempio di valori**: teorico `"Mario Rossi"`

### meta_timestamp
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: istante dell'evento lato Meta (`st.timestamp` / `m.timestamp` in secondi Unix,
  convertito in `Date`).
- **Esempio di valori**: teorico `2026-07-28 15:42:10`

### received_at
- **Tipo**: `datetime`, `NOT NULL`, indice non unico (a schema **nessun default**; model
  `defaultValue: Sequelize.NOW`)
- **Nullabile**: No
- **Descrizione**: istante in cui **il nostro** server ha ricevuto il webhook.
- **Esempio di valori**: teorico `2026-07-28 15:42:11`

### raw_payload
- **Tipo**: `json` (`longtext` `utf8mb4_bin`)
- **Nullabile**: Sì
- **Descrizione**: per gli `status`, l'oggetto `st` completo; per gli inbound, l'oggetto `m` completo
  (**non** l'intero payload del webhook). Serve per forensica quando i campi estratti non bastano.
- **Esempio di valori** (sotto-valori):
  - status: `{ "id": "wamid.…", "status": "delivered", "timestamp": "1722180130", "recipient_id":
    "393514648861", "conversation": { "id": "…", "origin": { "type": "utility" } }, "pricing": {
    "billable": true, "category": "utility", "pricing_model": "CBP" } }`
  - message_in: `{ "from": "393514648861", "id": "wamid.…", "timestamp": "1722180130", "type":
    "text", "text": { "body": "Ciao" } }`

---

## Osservazioni verificate nel codice (criticità)

- **Webhook POST senza verifica firma** `X-Hub-Signature-256` (pur avendo `WHATSAPP_APP_SECRET`):
  chiunque può iniettare righe forgiate — [§9 di 01-…backend.md](../../progettazione/01-criticita-e-indicazioni-backend.md).
- **Nessun lettore** → dato non di dominio; se serve tracciare la messaggistica va nell'outbox del
  modulo `Notification` (C6) con contratto stabile.
- **`message_body` mescola concetti** (testo puro, payload bottone, dump JSON media) → colonna non
  interrogabile in modo affidabile.
- **Nessun collegamento** a `switchboard_config` / `property` / prenotazione: impossibile sapere a
  quale stabilimento appartiene una riga senancarla via `phone_number_id`.
- **Dati personali senza retention** (numeri, nomi, testo messaggi, payload grezzo) — su
  `summer_booking_dev` le 11 righe reali (con numeri destinatario) sono ancora tutte presenti a oltre
  3 mesi dall'evento più vecchio.
- **Insert best-effort** (`try/catch` + `console.error`, risposta sempre `200`): eventi persi in
  silenzio in caso di errore DB.
- **Non ripulita da `deleteConfig`**.
- **Attribuzione allo stabilimento fragile per il numero generico**: quando (come nel caso reale
  osservato) lo stabilimento **non** ha un `whatsapp_phone_number_id` custom, gli eventi arrivano sul
  numero WhatsApp **condiviso** di piattaforma — `phone_number_id`/`waba_id` da soli **non bastano** a
  risalire allo stabilimento che ha inviato/ricevuto, perché lo stesso numero è usato da più
  stabilimenti. Nel legacy questo non è un problema (la tabella non ha comunque `property_id`), ma è
  un vincolo di design da tenere presente nella riscrittura.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `event_type` | `varchar(20)` | `enum WaWebhookEventType { Status, MessageIn }` |
| `status` | `varchar(20)` | `enum WaMessageStatus { Sent, Delivered, Read, Failed }` |
| `conversation_origin` | `varchar(50)` | `enum WaConversationOrigin { Marketing, Utility, Service, Authentication, ReferralConversion }` |
| `pricing_category` | `varchar(50)` | `enum WaPricingCategory { Marketing, Utility, Authentication, Service }` (passthrough Meta) |
| `message_type` | `varchar(30)` | `enum WaInboundMessageType { Text, Button, Interactive, Reaction, Image, Document, Audio, Video, Location, Sticker, Contacts, Unknown }` |
| `pricing_billable` | `tinyint(1)` | `bool?` (non enum) |
