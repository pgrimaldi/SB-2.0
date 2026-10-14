# Tabella `switchboard_audio_file`

> Fonti: schema del DB `local_sb` (**0 righe**, `AUTO_INCREMENT = 1`, `CREATE_TIME 2026-07-27`); model
> [`back-pre/app/models/switchboardAudioFile.model.js`](../../back-pre/app/models/switchboardAudioFile.model.js)
> (`sequelize.define("switchboard_audio_file", …, { freezeTableName: true, timestamps: false, underscored: true })`),
> registrato come `db.switchboardAudioFile` in [`models/index.js`](../../back-pre/app/models/index.js) (riga 262).
>
> **FK InnoDB reale**: `property_id → properties.id` (`_ibfk_1`, indice non unico). È inoltre il
> **bersaglio** delle FK `switchboard_config.welcome_audio_file_id` / `.fallback_audio_file_id` e
> `switchboard_menu_item.audio_file_id`. ORM: `belongsTo(property)` / `property.hasMany`.
>
> **Scrittori** (via [`Service/switchboardService.js`](../../back-pre/app/Service/switchboardService.js) +
> [`utils/audioUpload.js`](../../back-pre/app/utils/audioUpload.js)):
> - `uploadAudioFile` — `POST /api/sb/switchboard/audio/:propertyId` (campo form `audio`, estensioni
>   `.mp3/.wav/.ogg`, limite upload 50 MB pre-conversione). `audioUpload.processAndUploadAudio`
>   valida, converte in MP3 con `ffmpeg` se serve (`libmp3lame -qscale:a 2`), verifica la dimensione
>   post-conversione (`SWITCHBOARD_MAX_AUDIO_SIZE_MB`, **default 3 MB**), carica su S3 **privato**
>   (nessuna ACL) alla chiave `` `${AUDIO_BASE_PATH}/${propertyId}/${uuidv7}.mp3` `` con
>   `AUDIO_BASE_PATH = "audio/switchboard"`, poi `createAudioFileRecord`.
> - `deleteAudioFile` — `DELETE /api/sb/switchboard/audio/:propertyId/:audioFileId`: elimina da S3 e
>   dal DB.
> - `deleteConfig` — elimina tutti gli audio dello stabilimento (S3 + DB).
>
> **Lettori**:
> - `listAudioFiles` — `GET /api/sb/switchboard/audio/:propertyId`.
> - `getAudioUrl` — `GET .../audio/:propertyId/:audioFileId/url`: presigned URL S3 (scadenza
>   `SWITCHBOARD_AUDIO_URL_EXPIRY_SECS`, default 3600 s) per l'anteprima nel pannello.
> - `getPublicAudioUrl` — `GET /api/public/switchboard/audio/:audioFileId` (**no auth**): fa da proxy
>   in streaming da S3 verso Telnyx (Telnyx non gestisce i presigned URL). L'URL pubblico è quello che
>   `switchboardService.buildPublicAudioUrl` passa ai comandi `gather_using_audio` / `playback_start`.
> - **Runtime IVR**: `handleIncomingCall` / `processDtmfInput` / `handleGatherTimeout` risolvono
>   `welcomeAudio` / `menuItem.audioFileId` in `switchboard_audio_file` e ne usano `id` per costruire
>   l'URL pubblico.
>
> **Frontend**: nessuna UI nei sorgenti presenti. Vedi [`switchboard_config.md`](switchboard_config.md).

---

## Verdetto: feature attiva, tabella mai popolata in questa copia DB

**0 righe**, `AUTO_INCREMENT = 1`. Valori di `s3_key` / `s3_bucket` / `mime_type` / `file_size`
derivati dal codice di `audioUpload.js` (deterministico), non da dati reali.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Descrizione**: usato per costruire l'URL pubblico `/api/public/switchboard/audio/{id}` passato a
  Telnyx.
- **Esempio di valori**: 1, 2, 3, ..

### property_id
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: **FK** `_ibfk_1` → [`properties`](properties.md)`.id`.
- **Descrizione**: stabilimento proprietario del file. `getAudioUrl` / `deleteAudioFile` verificano
  che l'`audioFile.propertyId` combaci con il `:propertyId` di path prima di operare.
- **Esempio di valori**: teorico `9669`

### original_name
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: nome file originale caricato dall'utente (`file.originalname` di multer). Salvato
  per riferimento/visualizzazione; non usato per la chiave S3 (che è UUID). Restituito nel DTO.
- **Esempio di valori**: teorico `"benvenuto.wav"`, `"menu-principale.mp3"`

### s3_key
- **Tipo**: `varchar(500)`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: chiave dell'oggetto su S3, generata server-side come
  `` audio/switchboard/<propertyId>/<uuidv7>.mp3 `` (`AUDIO_BASE_PATH` da
  `env.SWITCHBOARD_AUDIO_S3_PATH`, default `audio/switchboard`; nome file = UUID v7-like
  time-ordered). Usata da `getPresignedUrl`, `streamFromS3`, `deleteFromS3`.
- **Esempio di valori**: `"audio/switchboard/9669/0190a1b2-c3d4-7e5f-8a9b-0c1d2e3f4a5b.mp3"`

### s3_bucket
- **Tipo**: `varchar(100)`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: nome del bucket S3, salvato al momento dell'upload dal valore di
  `env.AWS_S3_BUCKET` (default `"summerbooking"`). **Mai riletto**: tutte le operazioni S3
  (`getPresignedUrl`/`streamFromS3`/`deleteFromS3`) usano la costante `S3_BUCKET` di
  `audioUpload.js`, non questa colonna.
- **Esempio di valori**: `"summerbooking"`
- **Deprecato**: **Sì** — colonna scritta ma mai letta dal codice (l'informazione di bucket vive in
  configurazione, non a riga). Coerente con [§1 di 03-gestione-dati.md](../../docs/03-gestione-dati.md)
  (link/URL a DB): il bucket va nella mappa `origin` in configurazione, non sulla riga.

### mime_type
- **Tipo**: `varchar(50)`, `NOT NULL`, `DEFAULT 'audio/mpeg'`
- **Nullabile**: No
- **Descrizione**: tipo MIME del file finale. In pratica **sempre `"audio/mpeg"`**: `processAndUploadAudio`
  scrive `mimeType: "audio/mpeg"` fisso (tutto viene convertito in MP3). Restituito nel DTO, non usato
  in logica.
- **Esempio di valori**: `"audio/mpeg"` (costante)

### file_size
- **Tipo**: `int`
- **Nullabile**: Sì
- **Descrizione**: dimensione in byte dell'MP3 finale (`mp3Buffer.length`). Vincolo effettivo:
  `≤ SWITCHBOARD_MAX_AUDIO_SIZE_MB * 1024 * 1024` (default 3 MB). Solo DTO/visualizzazione.
- **Esempio di valori**: teorico `48213`, `250112` *(≤ ~3 145 728)*

### description
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: etichetta leggibile assegnata dall'operatore all'upload (`req.body.description`).
  Solo DTO/visualizzazione.
- **Esempio di valori**: teorico `"Messaggio di benvenuto"`, `"Orari ristorante"`

---

## Osservazioni verificate nel codice (criticità)

- **`s3_bucket` non riletto** → colonna ridondante; il prefisso storage va in configurazione
  ([§1 di 03-gestione-dati.md](../../docs/03-gestione-dati.md)).
- **`mime_type` costante** (`audio/mpeg`) → nessun valore informativo.
- **Nessuna deduplica**: due upload dello stesso file producono due righe e due oggetti S3.
- **Cancellazione parziale su errore S3**: `deleteAudioFile` logga l'errore S3 ma cancella comunque
  la riga (possibile oggetto orfano su S3); `deleteConfig` idem.
- **Endpoint proxy pubblico senza rate limiting** (`getPublicAudioUrl`): chiunque conosca un `id`
  numerico può scaricare l'audio del centralino.
- **Storage privato corretto** (nessuna ACL pubblica) — buona pratica, a differenza di
  `uploadFile.controller.js` (vedi [§10 di 01-…backend.md](../../docs/01-criticita-e-indicazioni-backend.md)).

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `mime_type` | `varchar(50)` default `'audio/mpeg'` | costante → nessun enum (o `enum AudioMimeType { AudioMpeg }` di pura tipizzazione) |
| `s3_bucket` | `varchar(100)` | non enum: da spostare in configurazione come `origin` ([§1 03-gestione-dati.md](../../docs/03-gestione-dati.md)) |
