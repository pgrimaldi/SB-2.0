# Tabella `fiskaly_receipt_log`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **8.063 righe**, query
> completa; ~3 righe di log per ogni riga di [`fiskaly_receipt`](fiskaly_receipt.md)), model
> [`back-pre/app/models/fiskalyReceiptLog.model.js`](../../../../../../back-pre/app/models/fiskalyReceiptLog.model.js)
> (`{ freezeTableName: true, timestamps: false }`),
> scrittura in
> [`Service/fiskalyReceiptManagerService.js`](../../../../../../back-pre/app/Service/fiskalyReceiptManagerService.js)
> metodi `createLog(params)` e `createErrorLog(params, error)` — invocati a ogni step del flusso di
> emissione (`createReceipt`) e di annullamento (`deleteReceipt`): `INTENTION_CREATED`,
> `TRANSACTION_CREATED`/`TRANSACTION_FAILED`, `PDF_GENERATION_FAILED`, `S3_UPLOAD_FAILED`,
> `COMPLETED`, `CREATION_FAILED`, `CANCELLATION_INTENTION_CREATED`/`…_FAILED`,
> `CANCELLATION_TRANSACTION_FAILED`, `CANCELLED`. `createLog` **tronca** ogni campo stringa alla
> dimensione della colonna e non lancia mai (un errore di logging non deve mascherare l'errore
> reale). Lettura in
> [`Service/fiskalyReceiptReaderService.js`](../../../../../../back-pre/app/Service/fiskalyReceiptReaderService.js)
> `getReceiptById` (`include` dei log, `separate: true`, `order: [["createdAt", "ASC"]]`, attributi
> selezionati) — mostrati nel dettaglio scontrino del gestionale.
> Associazioni in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (righe 355-359):
> `belongsTo(db.fiskalyReceipt, { foreignKey: "receipt_id" })`,
> `belongsTo(db.property, { foreignKey: "property_id" })`. `booking_number` **non** è una FK.
>
> **Nota di contesto** — **tabella di puro log append-only** del flusso scontrino elettronico
> Fiskaly: N righe per [`fiskaly_receipt`](fiskaly_receipt.md), una per transizione di stato o
> errore. Serve a diagnosticare i fallimenti di dialogo con l'API SIGN IT (che ha molti stati
> intermedi e casi di *resume*). Non contiene dati economici né di business, solo audit tecnico.
>
> **Dominio nella riscrittura**: **C4 — modulo `Fiskaly`** — tabella di log/append; **non**
> implementa `ISoftDeletable` e **non** ha `deleted_at`
> ([`docs/03-gestione-dati.md` §3.5](../../progettazione/03-gestione-dati.md), ultimo capoverso).

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### receipt_id
- **Tipo**: `int`
- **Nullabile**: Sì (a schema; nel codice sempre valorizzato = `receipt.id`; la FK `SET NULL` lo
  azzererebbe se lo scontrino venisse cancellato fisicamente, cosa che non avviene)
- **Relazioni**: FK reale `fiskaly_receipt_log_ibfk_4` → [`fiskaly_receipt.id`](fiskaly_receipt.md),
  `ON DELETE SET NULL ON UPDATE CASCADE`. Indice `fiskaly_receipt_log_receipt_id`.
- **Descrizione**: scontrino a cui il log si riferisce.
- **Esempio di valori**: `1448`, `1447`

### booking_number
- **Tipo**: `varchar(50)` (troncato a 50 da `createLog`)
- **Nullabile**: Sì
- **Relazioni**: `bookings.bookingNumber` / `service_reservation.bookingNumber` (nessuna FK; indice
  `fiskaly_receipt_log_booking_number`). Ridondante con `fiskaly_receipt.booking_number` (copiato
  per query dirette).
- **Descrizione**: numero prenotazione dello scontrino.
- **Esempio di valori**: `"1072314293"`, `"1072314292"`

### property_id
- **Tipo**: `int`
- **Nullabile**: Sì
- **Relazioni**: FK reale `fiskaly_receipt_log_ibfk_5` → `properties.id`, `ON DELETE SET NULL ON
  UPDATE CASCADE`. Indice `fiskaly_receipt_log_property_id`.
- **Descrizione**: stabilimento (`propertyId` passato ai metodi del service). Ridondante ma comodo
  per filtrare i log per stabilimento senza join.
- **Esempio di valori**: `10723`

### state
- **Tipo**: `varchar(50)` (troncato a 50)
- **Nullabile**: Sì
- **Descrizione**: passo/stato del flusso a cui il log si riferisce. Valori scritti dal codice:
  `INTENTION_CREATED`, `TRANSACTION_CREATED`, `TRANSACTION_FAILED`, `PDF_GENERATION_FAILED`,
  `S3_UPLOAD_FAILED`, `COMPLETED`, `CREATION_FAILED`, `CANCELLATION_INTENTION_CREATED`,
  `CANCELLATION_INTENTION_FAILED`, `CANCELLATION_TRANSACTION_FAILED`, `CANCELLED`. **In produzione**
  (8.063 righe) il campo `fiskaly_record_state` (stato Fiskaly grezzo loggato) si distribuisce così:
  `COMPLETED` 5.062, `ACCEPTED` 2.742, `REJECTED` 70, `FAILED` 14 (175 `NULL`).
- **Candidato a enumerazione**: **Sì**, `FiskalyReceiptLogState` — stesso vocabolario di
  [`fiskaly_receipt.record_state`](fiskaly_receipt.md) più gli step di sola diagnostica
  (`PDF_GENERATION_FAILED`, `CREATION_FAILED`).
- **Esempio di valori**: `"INTENTION_CREATED"`, `"TRANSACTION_CREATED"`, `"COMPLETED"`

### result
- **Tipo**: `varchar(2)` (troncato a 2; default applicativo `'OK'`)
- **Nullabile**: Sì
- **Descrizione**: esito del passo: `"OK"` (`createLog`) o `"KO"` (`createErrorLog`). **In produzione i
  `"KO"` esistono**: ~160 log con `fiskaly_error_code = SIGN_IT_TRANSACTION_FAILED`, ~95 con
  `fiskaly_error_status` HTTP (502 × 83, 405 × 7, 400 × 4, 500 × 1) — errori reali di comunicazione con
  il portale Agenzia delle Entrate (AdE temporaneamente non disponibile, credenziali Fisconline
  scadute, timeout 30s). Il flusso di emissione non è privo di fallimenti.
- **Candidato a enumerazione**: **Sì**, `LogResult { Ok, Ko }` (o booleano `is_ok`).
- **Esempio di valori**: `"OK"`, `"KO"`

### internal_error_messages
- **Tipo**: `varchar(500)` (troncato a 500)
- **Nullabile**: Sì
- **Descrizione**: messaggio d'errore interno (`error.message`), oppure una nota (es. `"Receipt
  cancelled by user"` sul log `CANCELLED`). NULL sui log OK di flusso normale.
- **Esempio di valori**: `null`, `"Receipt cancelled by user"`,
  `"SIGN IT TRANSACTION creation failed: …"`

### fiskaly_record_state
- **Tipo**: `varchar(50)` (troncato a 50)
- **Nullabile**: Sì
- **Descrizione**: stato **grezzo** del record restituito da Fiskaly per quel passo
  (`intentionResponse.content.state` / `transactionResponse.content.state` / `cancellationResponse…`).
  Nel campione: `"ACCEPTED"` (sugli INTENTION), `"COMPLETED"` (su TRANSACTION/COMPLETED).
- **Candidato a enumerazione**: **Sì**, `FiskalyRecordState { Accepted, Rejected, Completed, Failed }`
  (costante `FiskalyRecordState` in `fiskaly_consts.js`).
- **Esempio di valori**: `"ACCEPTED"`, `"COMPLETED"`, `null`

### fiskaly_log_messages
- **Tipo**: `longtext` — **JSON** (`CHECK (json_valid(fiskaly_log_messages))`)
- **Nullabile**: Sì
- **Descrizione**: array/oggetto dei "logs" dell'errore Fiskaly (`error.logs`), quando presente. NULL
  su tutti i log OK.
- **Esempio di valori**: `null`, `[{"type":"ERROR","message":"…"}]` (atteso)

### fiskaly_error_status
- **Tipo**: `int`
- **Nullabile**: Sì
- **Descrizione**: HTTP status dell'errore Fiskaly/axios (`error.status`). NULL sui log OK.
- **Esempio di valori**: `null`, `400`, `409`, `422` (attesi)

### fiskaly_error_code
- **Tipo**: `varchar(50)` (troncato a 50)
- **Nullabile**: Sì
- **Descrizione**: codice d'errore applicativo/Fiskaly (`error.code`, es. `SIGN_IT_TRANSACTION_ERROR`,
  `SIGN_IT_TRANSACTION_FAILED`, `SIGN_IT_INTENTION_ERROR`, `SIGN_IT_CANCELLATION_ERROR`,
  `NOTHING_TO_BILL`). NULL sui log OK.
- **Esempio di valori**: `null`, `"SIGN_IT_TRANSACTION_ERROR"`

### fiskaly_error_error
- **Tipo**: `varchar(50)` (troncato a 50)
- **Nullabile**: Sì
- **Descrizione**: campo `error.error` dell'errore Fiskaly (breve descrizione/categoria, es.
  `"TRANSACTION creation failed"`). NULL sui log OK.
- **Esempio di valori**: `null`, `"TRANSACTION failed"`

### fiskaly_error_message
- **Tipo**: `varchar(50)` (**troncato a 50** — vedi criticità)
- **Nullabile**: Sì
- **Descrizione**: messaggio d'errore Fiskaly (`error.message`), tagliato a 50 caratteri (spesso
  insufficiente). NULL sui log OK. Il messaggio completo, non troncato allo stesso modo, sta in
  `internal_error_messages` (500 char).
- **Esempio di valori**: `null`, `"SIGN IT TRANSACTION creation failed: The docum"`

### createdBy
- **Tipo**: `int`
- **Nullabile**: Sì (NULL per gli scontrini automatici — `userId` nullo)
- **Relazioni**: `users.id` (nessuna FK). Operatore che ha innescato l'operazione.
- **Descrizione**: nel target → `user_operator_id`
  ([`docs/03-gestione-dati.md` §3.2](../../progettazione/03-gestione-dati.md)).
- **Esempio di valori**: `null`, `2823`, `3218`

### createdAt
- **Tipo**: `datetime`, `NOT NULL` — `defaultValue: Sequelize.NOW`
- **Nullabile**: No
- **Descrizione**: istante del log. Chiave di ordinamento cronologico dei log di uno scontrino.
- **Esempio di valori**: `2026-07-27 07:05:46`

### updatedAt
- **Tipo**: `datetime`, `NOT NULL` — `defaultValue: Sequelize.NOW`
- **Nullabile**: No
- **Descrizione**: uguale a `createdAt` (le righe di log non vengono mai aggiornate). Colonna di
  fatto ridondante per una tabella append-only.
- **Esempio di valori**: `2026-07-27 07:05:46`

---

## Osservazioni verificate nel codice (criticità)

- **`fiskaly_error_message varchar(50)`**: troppo corto per un messaggio d'errore reale; il codice lo
  tronca a 50. Nel target usare `text` (o eliminarlo e tenere solo `internal_error_messages`).
- **`updatedAt` inutile** su una tabella append-only (sempre = `createdAt`).
- **Ridondanza `booking_number` + `property_id`** copiati dallo scontrino: accettabile come
  denormalizzazione per query dirette, ma nel target va documentato come tale.
- **Vocabolari come `varchar`**: `state`, `result`, `fiskaly_record_state`, `fiskaly_error_code` →
  vedi Enumerazioni. Il model annota `FiskalyReceiptState`/`FiskalyRecordState` ma i valori reali di
  `state` non coincidono con quella costante.
- **`createdBy` senza FK**; timestamp non automatici (`timestamps: false`).
- **Nessuna retention policy**: `AUTO_INCREMENT=4439` per ~1449 scontrini (≈3 log/scontrino), in
  crescita illimitata; prevedere pulizia/partizionamento nel target.
