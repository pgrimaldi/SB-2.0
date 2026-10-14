# Tabella `switchboard_config`

> Fonti: schema del DB `local_sb` (**0 righe**, `AUTO_INCREMENT = 1`, `CREATE_TIME 2026-07-27` — tabella
> mai popolata in questa copia); model
> [`back-pre/app/models/switchboardConfig.model.js`](../../back-pre/app/models/switchboardConfig.model.js)
> (`sequelize.define("switchboard_config", …, { freezeTableName: true, timestamps: false, underscored: true })`),
> registrato come `db.switchboardConfig` in [`models/index.js`](../../back-pre/app/models/index.js) (riga 250).
>
> **A differenza della quasi totalità del legacy, il dominio Switchboard usa FK InnoDB reali**:
> `property_id → properties.id`, `welcome_audio_file_id → switchboard_audio_file.id`,
> `fallback_audio_file_id → switchboard_audio_file.id` (constraint `switchboard_config_ibfk_3/4/5`).
> `property_id` ha vincolo **UNIQUE** (una configurazione per stabilimento; a schema risultano due
> indici unici ridondanti su `property_id`, `property_id` e `property_id_2`).
>
> **Scrittori** (tutti in [`Service/switchboardService.js`](../../back-pre/app/Service/switchboardService.js),
> chiamati da [`controllers/switchboard.controller.js`](../../back-pre/app/controllers/switchboard.controller.js)):
> - `createConfig` — `POST /api/sb/switchboard/config` (`createConfig`) e, se manca, come effetto
>   collaterale di `POST /api/sb/switchboard/sip/create` (`createSipEndpoint`).
> - `updateConfig` — `PUT /api/sb/switchboard/config/:propertyId` più **decine** di endpoint di
>   dettaglio che scrivono un sottoinsieme di colonne: ordine numero Telnyx, creazione SIP endpoint,
>   creazione Call Control App, messaging profile, dati/documenti/indirizzi/requirement group
>   regolatori, numero WhatsApp custom, ecc.
> - `deleteConfig` — `DELETE /api/sb/switchboard/config/:propertyId`: cancella la riga **e a cascata
>   applicativa** menu items, menu actions, audio file (anche da S3) e wa template dello stabilimento.
> - `registerPushToken` — `POST /api/public/switchboard/sip/register-push` scrive `deviceFcmToken` /
>   `deviceVoipToken` (autenticazione via `sip_username` + `sip_password`, **nessun JWT**).
>
> **Lettori**: `getConfigByPropertyId` (usato ovunque nel modulo), più la **logica di gestione
> chiamata** in `handleIncomingCall` / `processDtmfInput` / `executeMenuAction`
> (lookup per `telnyx_phone_number` + `is_active = 1`) e `getSipCallHistory` / `registerPushToken`
> (lookup per `sip_username` + `sip_password`).
>
> **Frontend**: nessuna UI in `front-pre` / `front-modern-pre` / `front-customer-pre` /
> `front-app-pre`. Il modulo è pilotato da un pannello di back-office esterno (i commenti nel
> controller lo chiamano *"BACKOFFICE API (FOR PYTHON APP)"*) e dai webhook Telnyx/Meta. Nei frontend
> presenti "centralino virtuale" / "virtual switchboard" compare **solo** come voce marketing nella
> pagina pubblica delle funzionalità ([`front-pre/src/locale/it.js:2289`](../../front-pre/src/locale/it.js)).

---

## Verdetto: feature attiva ma non collaudata su questa base dati

Codice completo e cablato (routing in [`routes/user.routes.js`](../../back-pre/app/routes/user.routes.js)
righe ~2523-2946 e [`routes/public.routes.js`](../../back-pre/app/routes/public.routes.js) righe 26-61),
ma **0 righe** in tutte le 8 tabelle `switchboard_*` e `AUTO_INCREMENT = 1`: in questa copia del DB la
feature non è mai stata usata. Gli "Esempio di valori" sotto derivano quindi da: commenti del model,
payload che il codice costruisce/consuma, costanti (`sip.helper.js`, `audioUpload.js`) e formati delle
API Telnyx/Meta richiamate.

Criticità di sicurezza del dominio: vedi
[`docs/01-criticita-e-indicazioni-backend.md` §9](../../docs/01-criticita-e-indicazioni-backend.md)
(webhook senza verifica firma, credenziali SIP in chiaro, endpoint pubblici autenticati solo dal
confronto diretto `sip_username`+`sip_password` senza rate limiting).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### property_id
- **Tipo**: `int`, `NOT NULL`, **UNIQUE**
- **Nullabile**: No
- **Relazioni**: **FK** `switchboard_config_ibfk_3` → [`properties`](properties.md)`.id`. `belongsTo`/`hasOne`
  ORM (`db.property.hasOne(db.switchboardConfig)`).
- **Descrizione**: stabilimento proprietario della configurazione centralino. Chiave di accesso di tutti
  gli endpoint del modulo (`/:propertyId`) e dell'`checkAccess` (admin/superadmin/systemadmin → qualsiasi
  proprietà; `manager` → solo `user.propertyId`).
- **Esempio di valori**: teorico `9669`, `3` (id di `properties`)

### telnyx_phone_number
- **Tipo**: `varchar(20)`
- **Nullabile**: Sì
- **Descrizione**: numero PSTN geografico assegnato allo stabilimento su Telnyx, in E.164. È la **chiave
  di lookup** con cui `handleIncomingCall` associa una chiamata in arrivo (`payload.to`) alla
  configurazione (`where: { telnyxPhoneNumber: toNumber, isActive: 1 }`). Valorizzato da
  `orderPhoneNumber` / `getNumberOrderStatus`; azzerato da `deletePhoneNumber`.
- **Esempio di valori**: `"+390510000000"`, `"+39051..."` *(E.164 italiano; nessun dato reale)*

### telnyx_phone_number_id
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: id dell'oggetto *phone number* su Telnyx (`/v2/phone_numbers/{id}`). Usato per
  PATCH del numero (riassegnazione a Call Control App, assegnazione messaging profile), GET stato,
  release.
- **Esempio di valori**: teorico `"3520000000000000000"` *(id numerico Telnyx come stringa)*

### telnyx_number_order_id
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: id del *number order* Telnyx generato all'acquisto del numero. Usato da
  `getNumberOrderStatus` per fare polling dello stato di provisioning.
- **Esempio di valori**: teorico UUID Telnyx `"a1b2c3d4-…"`

### telnyx_phone_number_status
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: **passthrough** dello stato del numero restituito da Telnyx; sincronizzato a DB da
  `getPhoneNumber` / `updatePhoneNumber` / `getNumberOrderStatus`. All'ordine viene inizializzato a
  `"pending"` se Telnyx non restituisce ancora uno stato.
- **Esempio di valori**: `"pending"` (default all'ordine); valori Telnyx tipici `"active"`, `"provisioning"`
- **Enumeratore**: stato esterno Telnyx — conservare grezzo; eventuale `enum` interno derivato.

### telnyx_connection_id
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: id della *credential connection* Telnyx creata insieme all'endpoint SIP
  (`createSipEndpoint` → `sipHelper.createSipEndpoint` → `telnyxHelper.createCredentialConnection`).
  Usato per PATCH webhook URL sulla connection e per la cancellazione dell'endpoint SIP.
- **Esempio di valori**: teorico id numerico Telnyx come stringa

### telnyx_call_control_app_id
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: id della *Call Control Application* Telnyx. **Determina la modalità IVR**: se
  valorizzato (`createCallControlApp`), il numero punta alla Call Control App e `handleIncomingCall`
  risponde via API (`answerCall`) così l'audio di benvenuto/menu suona **prima** di squillare sul SIP;
  se `null`, il numero punta direttamente alla credential connection e la chiamata è auto-bridgeata al
  SIP senza IVR.
- **Esempio di valori**: teorico id numerico Telnyx come stringa

### sip_endpoint_id
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: id della *telephony credential* Telnyx (`createTelephonyCredential`). Cancellato con
  l'endpoint SIP.
- **Esempio di valori**: teorico id numerico Telnyx come stringa

### sip_username
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: username SIP dell'utenza softphone dello stabilimento. Generato server-side da
  [`sip.helper.js`](../../back-pre/app/controllers/helpers/sip.helper.js) `generateSipUsername` come
  `` `sb0${propertyId}` `` (la variante con suffisso random è **commentata**). Usato: per costruire il
  SIP URI di forward (`sip:${sipUsername}@${sipDomain}`), come chiave del `to` in
  [`switchboard_call_register`](switchboard_call_register.md), e come **credenziale di autenticazione**
  (assieme a `sip_password`) per gli endpoint pubblici `sip/call-history` e `sip/register-push`.
- **Esempio di valori**: `"sb09669"` (formato `sb0<propertyId>`); nel commento del model
  [`switchboardCallRegister.model.js`](../../back-pre/app/models/switchboardCallRegister.model.js) compare
  un formato più vecchio `"sbprop9669a14fa3"`

### sip_password
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: password SIP. Generata random (`crypto.randomBytes(12)` → 16 caratteri alfanumerici)
  ma **salvata in chiaro** e usata come chiave di lookup diretta (`findOne({ where: { sipUsername,
  sipPassword } })`) negli endpoint pubblici, oltre a essere restituita in chiaro nella response di
  `createSipEndpoint` e inviata via email HTML da `sendSetupEmail`. Criticità già in
  [§9 di 01-…backend.md](../../docs/01-criticita-e-indicazioni-backend.md).
- **Esempio di valori**: 16 caratteri `[A-Za-z0-9]`, es. `"a1B2c3D4e5F6g7H8"` *(nessun dato reale)*

### sip_domain
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: dominio SIP a cui registrarsi. Impostato al valore di
  `process.env.TELNYX_SIP_DOMAIN` (default `"sip.telnyx.com"`) alla creazione dell'endpoint.
- **Esempio di valori**: `"sip.telnyx.com"`

### sip_client_type
- **Tipo**: `varchar(50)`, `DEFAULT 'generic'`
- **Nullabile**: Sì
- **Descrizione**: etichetta descrittiva del tipo di client SIP usato dallo stabilimento. Impostabile via
  `updateSipClientConfig` / `createConfig`. **Non ha alcun effetto sulla logica di chiamata**: usato solo
  per essere restituito nel DTO. Il model cita come esempi "softphone, webrtc, mobile".
- **Esempio di valori**: `"generic"` (default), `"mobile"`, `"webrtc"` *(insieme non vincolato)*
- **Enumeratore**: candidato `enum SipClientType { Generic, Softphone, WebRtc, Mobile }` — puramente
  descrittivo, valore basso; in alternativa lasciare stringa libera.

### sip_client_notes
- **Tipo**: `text`
- **Nullabile**: Sì
- **Descrizione**: note libere sull'installazione del client SIP. Solo lettura/scrittura via API +
  DTO; nessun uso in logica.
- **Esempio di valori**: testo libero *(nessun dato)*

### whatsapp_phone_number_id
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: id (Meta Cloud API) del numero WhatsApp Business **specifico** dello stabilimento.
  Se valorizzato ha priorità sul numero generico `process.env.WHATSAPP_PHONE_NUMBER_ID` per l'invio di
  template/testo (mode `"custom"` vs `"generic"`). Valorizzato da `setWaPhone` / `waAddPhoneForProperty`
  (step 1 del flusso di registrazione OTP), azzerato da `removeWaPhone`.
- **Esempio di valori**: teorico id numerico Meta come stringa (`"1090000000000000"`)

### whatsapp_business_account_id
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: id del WABA (WhatsApp Business Account) associato. Scrivibile da `createConfig` /
  `updateConfig` e restituito nel DTO, ma **mai letto dalla logica**: le operazioni Meta usano
  `process.env.WHATSAPP_BUSINESS_ACCOUNT_ID`. Colonna di sola anagrafica.
- **Esempio di valori**: teorico id numerico Meta come stringa
- **Deprecato**: **Sì** — mai riletto dal codice (nessun ramo logico lo usa).

### welcome_audio_file_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: **FK** `switchboard_config_ibfk_4` → [`switchboard_audio_file`](switchboard_audio_file.md)`.id`;
  ORM `belongsTo` con alias `welcomeAudio`.
- **Descrizione**: audio riprodotto quando la chiamata viene risposta; se presente, `handleIncomingCall`
  avvia il `gather_using_audio` (DTMF) su questo file, altrimenti (in modalità Call Control App) chiude
  la chiamata. È anche l'audio ri-riprodotto in caso di timeout senza input (`handleGatherTimeout`).
- **Esempio di valori**: teorico `1`, `2` (id di `switchboard_audio_file`)

### fallback_audio_file_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: **FK** `switchboard_config_ibfk_5` → [`switchboard_audio_file`](switchboard_audio_file.md)`.id`;
  ORM `belongsTo` con alias `fallbackAudio`.
- **Descrizione**: audio riprodotto al chiamante quando un `forward_sip` fallisce (nessuna risposta /
  occupato), subito prima di chiudere. Se `null`, la chiamata viene chiusa senza messaggio. Colonna
  aggiunta dopo la creazione iniziale della tabella (compare in coda allo schema).
- **Esempio di valori**: teorico `1`, `2`

### forward_caller_id_mode
- **Tipo**: `varchar(20)`, `NOT NULL`, `DEFAULT 'caller'`
- **Nullabile**: No
- **Descrizione**: da quale numero il SIP endpoint vede arrivare la chiamata inoltrata (`forward_sip`):
  - `"caller"` (default) → il numero PSTN originale del chiamante;
  - `"centralino"` → il numero Telnyx del centralino (`telnyx_phone_number`);
  - `"custom"` → il valore di `forward_custom_caller_id`.
  Letto in `executeMenuAction` case `forward_sip`. Colonna aggiunta dopo la creazione iniziale.
- **Esempio di valori**: `"caller"` (default), `"centralino"`, `"custom"`
- **Enumeratore**: **sì** — `enum ForwardCallerIdMode { Caller, Centralino, Custom }` (ramo di logica
  dedicato per ciascun valore).

### forward_custom_caller_id
- **Tipo**: `varchar(20)`
- **Nullabile**: Sì
- **Descrizione**: numero (E.164) mostrato come caller ID sul SIP endpoint quando
  `forward_caller_id_mode = 'custom'`. Ignorato negli altri mode.
- **Esempio di valori**: teorico `"+390510000000"`

### forward_caller_id_name
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: display name opzionale mostrato accanto al caller ID sul SIP endpoint durante il
  `forward_sip` (`from_display_name` nel `transfer` Telnyx).
- **Esempio di valori**: teorico `"Bagno Reception"`

### telnyx_webhook_url
- **Tipo**: `varchar(500)`
- **Nullabile**: Sì
- **Descrizione**: URL pubblico su cui Telnyx invia gli eventi di chiamata. Impostato da
  `createSipEndpoint` / `updateWebhookUrl` (sulla credential connection) e da `createCallControlApp` /
  `updateCallControlAppWebhook` (sulla Call Control App). Solo tracciamento a DB: la logica di webhook
  non lo rilegge.
- **Esempio di valori**: teorico `"https://<host>/api/public/switchboard/webhook/call"`

### telnyx_messaging_profile_id
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: id del *messaging profile* Telnyx (per gli SMS, usati p.es. per l'OTP di verifica
  WhatsApp). Valorizzato da `createMessagingProfile`, azzerato da `deleteMessagingProfile`.
- **Esempio di valori**: teorico id numerico Telnyx come stringa

### reg_address_id
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: id dell'*address* Telnyx usato per i requisiti regolatori dell'ordine numero IT.
  Valorizzato da `createAddress` (con `addressType` ≠ `"supplemental"`); mappato al requirement Telnyx
  `0e84f7f9-…` in `IT_LOCAL_REG_MAP`. Azzerato da `deleteAddress`.
- **Esempio di valori**: teorico UUID Telnyx

### reg_supplemental_address_id
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: id dell'*address* supplementare Telnyx (`addressType = "supplemental"`). Requirement
  `ad56ca91-…`.
- **Esempio di valori**: teorico UUID Telnyx

### reg_proof_of_address_doc_id
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: id del *document* Telnyx (PDF) "prova di indirizzo" caricato via `uploadDocument`
  (`documentType = "proofOfAddress"`). Requirement `0ba49bfa-…`.
- **Esempio di valori**: teorico UUID Telnyx

### reg_id_document_copy_doc_id
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: id del *document* Telnyx "copia documento di identità" (`documentType =
  "idDocumentCopy"`). Requirement `540425c2-…`.
- **Esempio di valori**: teorico UUID Telnyx

### reg_company_reg_cert_doc_id
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: id del *document* Telnyx "certificato camerale / visura" (`documentType =
  "companyRegCert"`). Requirement `8b54a9b2-…`.
- **Esempio di valori**: teorico UUID Telnyx

### reg_end_user_name
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: nome dell'utilizzatore finale del numero (persona o azienda). Campo testo dei
  requisiti regolatori (`updateRegData`); requirement `2836da67-…`.
- **Esempio di valori**: teorico `"Stabilimento Balneare Srl"`

### reg_it_customer_type
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: tipo cliente IT richiesto da Telnyx per l'ordine numero locale. Requirement
  `413ad281-…`. Vocabolario chiuso lato Telnyx (tipicamente `individual` / `business`).
- **Esempio di valori**: teorico `"business"`, `"individual"`
- **Enumeratore**: candidato `enum ItCustomerType { Individual, Business }` (valori imposti da Telnyx).

### reg_identity_doc_type
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: tipo del documento di identità del referente. Requirement `240451b2-…`. Vocabolario
  chiuso lato Telnyx.
- **Esempio di valori**: teorico `"passport"`, `"national_id"`, `"driving_license"`
- **Enumeratore**: candidato `enum IdentityDocType { Passport, NationalId, DrivingLicense }` (valori Telnyx).

### reg_identity_doc_number
- **Tipo**: `varchar(40)`
- **Nullabile**: Sì
- **Descrizione**: numero del documento di identità. Requirement `6c65df6f-…`.
- **Esempio di valori**: teorico `"CA12345AA"`

### reg_identity_doc_issuer
- **Tipo**: `varchar(40)`
- **Nullabile**: Sì
- **Descrizione**: ente che ha emesso il documento. Requirement `a3c2682d-…`.
- **Esempio di valori**: teorico `"Comune di Rimini"`

### reg_identity_doc_expiry
- **Tipo**: `varchar(10)`
- **Nullabile**: Sì
- **Descrizione**: data di scadenza del documento come stringa (`"YYYY-MM-DD"`). Requirement `3164f2ae-…`.
- **Esempio di valori**: teorico `"2030-01-31"`

### reg_date_of_birth
- **Tipo**: `varchar(10)`
- **Nullabile**: Sì
- **Descrizione**: data di nascita del referente come stringa (`"YYYY-MM-DD"`). Requirement `d341dbfb-…`.
- **Esempio di valori**: teorico `"1980-05-12"`

### reg_place_of_birth
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: luogo di nascita del referente. Requirement `2d0e8476-…`.
- **Esempio di valori**: teorico `"Rimini"`

### reg_personal_tax_code
- **Tipo**: `varchar(20)`
- **Nullabile**: Sì
- **Descrizione**: codice fiscale personale del referente. Requirement `7dcc2229-…`.
- **Esempio di valori**: teorico `"RSSMRA80E12H294X"`

### reg_company_tax_code
- **Tipo**: `varchar(20)`
- **Nullabile**: Sì
- **Descrizione**: codice fiscale aziendale. Requirement `6bac4095-…`.
- **Esempio di valori**: teorico `"01234567890"`

### reg_vat_number
- **Tipo**: `varchar(20)`
- **Nullabile**: Sì
- **Descrizione**: partita IVA. Requirement `c4cb7670-…`.
- **Esempio di valori**: teorico `"IT01234567890"`

### reg_requirement_group_id
- **Tipo**: `varchar(100)`
- **Nullabile**: Sì
- **Descrizione**: id del *requirement group* Telnyx (contenitore dei requisiti regolatori compilati),
  passato come `requirement_group_id` all'ordine numero. Valorizzato da `createRequirementGroup`,
  azzerato da `deleteRequirementGroup`.
- **Esempio di valori**: teorico UUID Telnyx

### is_active
- **Tipo**: `smallint`, `NOT NULL`, `DEFAULT 0`
- **Nullabile**: No
- **Descrizione**: interruttore del centralino. `handleIncomingCall` accetta la chiamata **solo** se
  esiste una config con `telnyx_phone_number` corrispondente **e** `is_active = 1`; altrimenti la
  chiamata viene rifiutata (`rejectCall`). Impostato via `createConfig` / `updateConfig` (campo
  `isActive`).
- **Esempio di valori**: `0` (default, centralino spento), `1` (attivo)
- **Enumeratore**: no — booleano (`is_active` → `bool`).

### deviceFcmToken
- **Tipo**: `varchar(255)` — **nome colonna in camelCase** (unico assieme a `deviceVoipToken` in una
  tabella per il resto tutta `snake_case`)
- **Nullabile**: Sì
- **Descrizione**: token FCM dell'app Android legata a questo endpoint SIP. Prima di inoltrare una
  chiamata al SIP, `handleIncomingCall` invia un push "wake" (`sendWakePush`) per far riaggiornare la
  registrazione SIP. Scritto da `registerPushToken` (`config.deviceFcmToken = token; config.save()`).
- **Esempio di valori**: teorico token FCM (~150+ caratteri)
- **Osservazione**: il model dichiara `underscored: true`, che mappa l'attributo `deviceFcmToken` sulla
  colonna `device_fcm_token`; in questa copia del DB la colonna si chiama invece `deviceFcmToken`. Da
  verificare l'allineamento sul DB di produzione (potenziale drift di schema).

### deviceVoipToken
- **Tipo**: `varchar(255)` — **nome colonna in camelCase**
- **Nullabile**: Sì
- **Descrizione**: token APNs VoIP (PushKit) dell'app iOS legata a questo endpoint SIP. Usato per il
  push "wake" VoIP (`sendVoipPush`) prima dell'inoltro al SIP. Scritto da `registerPushToken` quando
  `platform = "ios"` o quando è presente solo `voip_token`.
- **Esempio di valori**: teorico token APNs VoIP (64 hex o token moderno più lungo)
- **Osservazione**: stessa nota di `deviceFcmToken` sul mapping `underscored` → `device_voip_token`.

---

## Osservazioni verificate nel codice (criticità)

- **Segreti SIP in chiaro** (`sip_password`) usati come chiave di lookup e restituiti/emailati —
  [§9 di 01-…backend.md](../../docs/01-criticita-e-indicazioni-backend.md).
- **Colonna morta**: `whatsapp_business_account_id` (scritta e restituita, mai riletta).
- **Solo descrittivi, nessun effetto logico**: `sip_client_type`, `sip_client_notes`, `telnyx_webhook_url`.
- **Drift di naming**: `deviceFcmToken` / `deviceVoipToken` in camelCase contro `underscored: true` del
  model e contro tutte le altre colonne `snake_case`.
- **Indici unici ridondanti** su `property_id` (`property_id` + `property_id_2`).
- **Cancellazione a cascata applicativa** (non FK `ON DELETE CASCADE`): `deleteConfig` rimuove a mano
  menu/azioni/audio(+S3)/wa-template; le altre 3 tabelle di log (`switchboard_call_log`,
  `switchboard_call_register`, `switchboard_wa_msg_log`) **non** vengono ripulite.
- **Modello di configurazione mono-numero**: una sola coppia (numero Telnyx, endpoint SIP) per
  stabilimento; nessun supporto multi-linea.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `forward_caller_id_mode` | `varchar(20)` default `'caller'` | `enum ForwardCallerIdMode { Caller, Centralino, Custom }` |
| `is_active` | `smallint` 0/1 | `bool` (non enum) |
| `sip_client_type` | `varchar(50)` default `'generic'` | `enum SipClientType { Generic, Softphone, WebRtc, Mobile }` *(solo se si vuole vincolare; puramente descrittivo)* |
| `telnyx_phone_number_status` | `varchar(50)` | stato esterno Telnyx: conservare grezzo, eventuale `enum` interno derivato |
| `reg_it_customer_type` | `varchar(50)` | `enum ItCustomerType { Individual, Business }` (valori imposti da Telnyx) |
| `reg_identity_doc_type` | `varchar(50)` | `enum IdentityDocType { Passport, NationalId, DrivingLicense }` (valori Telnyx) |
