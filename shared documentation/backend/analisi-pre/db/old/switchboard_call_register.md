# Tabella `switchboard_call_register`

> Fonti: schema del DB `local_sb` (**0 righe**, `AUTO_INCREMENT = 1`, `CREATE_TIME 2026-07-27`); model
> [`back-pre/app/models/switchboardCallRegister.model.js`](../../back-pre/app/models/switchboardCallRegister.model.js)
> (`sequelize.define("switchboard_call_register", …, { freezeTableName: true, timestamps: false, underscored: true })`),
> registrato come `db.switchboardCallRegister` in [`models/index.js`](../../back-pre/app/models/index.js) (riga 274).
>
> **Nessuna FK**; **due indici non unici** su `to` e `datetime` (dichiarati nel model, presenti a
> schema come `switchboard_call_register_to` / `switchboard_call_register_datetime`). Nessuna
> associazione ORM.
>
> **Scrittore** — unico: [`Service/switchboardService.js`](../../back-pre/app/Service/switchboardService.js)
> `handleIncomingCall`. Quando arriva l'evento Telnyx `call.initiated` con `direction = "outgoing"` e
> `state = "bridging"` (cioè il **B-leg SIP** creato dal nostro `transfer`/`forward_sip`), inserisce
> `{ from: callEvent.from, to: callEvent.to, datetime: new Date() }` e ritorna
> `action: "sip_register_saved"`.
>
> **Lettore** — unico: [`switchboard.controller.js`](../../back-pre/app/controllers/switchboard.controller.js)
> `getSipCallHistory` — `POST /api/public/switchboard/sip/call-history` (**no JWT**; autenticazione
> tramite `sip_username` + `sip_password` confrontati con `switchboard_config`). Restituisce le righe
> con `to = "sip:<sip_username>@<TELNYX_SIP_DOMAIN>"` e `datetime >= now - N ore`
> (`SB_CALL_REGISTER_LAST_HOURS`, default 24), ordinate `datetime DESC`, **solo colonne `from` e
> `datetime`**. Serve al client softphone per mostrare "chi mi ha chiamato nelle ultime N ore".
>
> **Frontend**: nessun consumo negli app frontend presenti (`front-app-pre` è l'app cliente della
> spiaggia, non il softphone del centralino). Il client SIP che consuma questo endpoint è esterno ai
> sorgenti forniti.

---

## Verdetto: feature attiva, tabella mai popolata in questa copia DB

**0 righe**, `AUTO_INCREMENT = 1`. Formati sotto derivati da `parseCallWebhook` (Telnyx) e da
`getSipCallHistory` (che costruisce `` `sip:${sipUsername}@${sipDomain}` `` come chiave di match su
`to`).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### from
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì (a schema; in pratica valorizzato con `callEvent.from`, che è il numero PSTN
  originale del chiamante)
- **Descrizione**: numero del chiamante PSTN originale in E.164 (il commento del model:
  *"Original PSTN caller"*). È l'**unico dato utile** restituito al client softphone dallo storico
  chiamate.
- **Esempio di valori**: `"+393514648861"` *(dal commento del model)*, `"+39051..."`

### to
- **Tipo**: `varchar(255)`, indice non unico
- **Nullabile**: Sì (a schema; valorizzato con `callEvent.to`)
- **Descrizione**: destinazione SIP del leg inoltrato, cioè il SIP URI dell'endpoint dello
  stabilimento. È la **chiave di filtro** di `getSipCallHistory`
  (`` toMatch = `sip:${sipUsername}@${sipDomain}` ``, con `sipDomain = env.TELNYX_SIP_DOMAIN` default
  `sip.telnyx.com`).
- **Esempio di valori**: `"sip:sbprop9669a14fa3@sip.telnyx.com"` *(dal commento del model; con
  l'attuale `generateSipUsername` = `sb0<propertyId>` sarebbe `"sip:sb09669@sip.telnyx.com"`)*

### datetime
- **Tipo**: `datetime`, `NOT NULL`, indice non unico (a schema **nessun default**; model
  `defaultValue: Sequelize.NOW`; in pratica il codice passa sempre `datetime: new Date()` esplicito)
- **Nullabile**: No
- **Descrizione**: istante di registrazione del forward (`new Date()` lato applicazione). Usato da
  `getSipCallHistory` per la finestra temporale (`>= now - N ore`) e per l'ordinamento `DESC`.
- **Esempio di valori**: teorico `2026-07-28 15:42:10`

---

## Osservazioni verificate nel codice (criticità)

- **Tabella "registro chiamate" molto povera**: solo `from`/`to`/`datetime`; nessun collegamento a
  `switchboard_config`/`property`, nessun esito (risposta/mancata), nessuna durata. Deriva
  esclusivamente dall'evento `call.initiated` del B-leg — se quell'evento non arriva (o cambia forma),
  lo storico resta vuoto.
- **Match per stringa esatta su `to`**: se `TELNYX_SIP_DOMAIN` o il formato `sip_username` cambiano
  fra scrittura e lettura, lo storico "sparisce" silenziosamente.
- **Endpoint pubblico senza rate limiting / lockout**: `getSipCallHistory` autentica con
  `findOne({ where: { sipUsername, sipPassword } })` in chiaro
  ([§9 di 01-…backend.md](../../docs/01-criticita-e-indicazioni-backend.md)).
- **Nessuna retention**: nessun job che cancella le righe vecchie; la query filtra a runtime ma i dati
  restano.
- **Non ripulita da `deleteConfig`** (che rimuove config/menu/audio/wa-template ma non i log).

### Campi candidati a enumeratore

Nessuno. `from`/`to` sono identificatori (E.164 / SIP URI), `datetime` è un timestamp.
Nella riscrittura andrebbe modellata come vera tabella eventi con `switchboard_config_id`, esito e
durata (dominio `switchboard`, sottoarea `voice`).
