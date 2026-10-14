# Tabella `contract_acceptation`

> Fonti: schema (tabella **vuota**, 0 righe in `local_sb`, `AUTO_INCREMENT = 1`), model
> [`back-pre/app/models/contractAcceptation.model.js`](../../back-pre/app/models/contractAcceptation.model.js)
> (`sequelize.define("contract_acceptation", ..., { freezeTableName: true, timestamps: true })` → ORM
> `db.contractAcceptation`), servizio
> [`Service/contractConditionService.js`](../../back-pre/app/Service/contractConditionService.js) (letto
> per intero), controller
> [`contractCondition.controller.js`](../../back-pre/app/controllers/contractCondition.controller.js),
> associazioni in [`app/models/index.js:489-496`](../../back-pre/app/models/index.js), routing in
> [`app/routes/user.routes.js:2966-2973`](../../back-pre/app/routes/user.routes.js).
>
> **Nota di contesto** — tabella **non censita** prima di questa scheda. È il **registro delle
> accettazioni** dei documenti contrattuali di [`contract_documents`](contract_documents.md) da parte
> dello stabilimento: chi ha accettato, quando, da quale IP, con quale hash del documento. **1 riga per
> `propertyId`** (`unique: true`) — non uno storico: viene aggiornata in place a ogni cambio di stato
> (`existing.update(payload)`).
>
> **Dominio di destinazione proposto**: **C5 — Identity/Account**, sottoarea `account`/`compliance`
> (accettazioni legali con audit chi/quando/IP/hash). Il legame con [`contract_documents`](contract_documents.md)
> è nello stesso dominio; il legame con [`properties`](properties.md) passa per `propertyId` (id copiato,
> non FK cross-modulo — [`docs/03-gestione-dati.md` §3.1bis](../../docs/03-gestione-dati.md)).

---

## Uso nei frontend (via API)

**Sì — ma solo in `front-modern-pre`** (frontend della riscrittura); `front-pre`, `front-customer-pre`,
`front-app-pre` non usano gli endpoint `contract-condition`. La riga di `contract_acceptation` viene
scritta quando l'utente clicca **"Accetto e continuo"** nella modale bloccante "Aggiornamento delle
Condizioni Contrattuali" di `front-modern-pre`
([`ConditionsUpdate.tsx`](../../front-modern-pre/src/modules/ConditionsUpdate/ConditionsUpdate.tsx) →
[`ConditionsApi.updateConditions()`](../../front-modern-pre/src/app/Conditions/ConditionsApi.ts) →
[`GenericRestApi.updateContractCondition(true)`](../../front-modern-pre/src/remote/SummerBooking/Generic/GenericRestApi.ts)
→ `POST /api/sb/contract-condition/update { accepted: true }`). Dettaglio completo del flusso nella
scheda di [`contract_documents`](contract_documents.md) → "Uso nei frontend".

**0 righe in produzione** perché la modale non compare mai: nessuno stabilimento ha
`properties.contractDocumentsId` valorizzato (`local_sb`: 0/100 properties) →
`GET /api/sb/contract-condition` risponde `{ enabled: false }` → nessuna accettazione. Feature completa
ma non attivata.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### propertyId
- **Tipo**: `int`, `NOT NULL`, **`UNIQUE`**
- **Nullabile**: No
- **Relazioni**: `properties.id` (`db.contractAcceptation.belongsTo(db.property, { foreignKey: "propertyId" })`,
  `db.property.hasOne(db.contractAcceptation, ...)`). Ricavato dall'utente loggato
  (`user.propertyId`), non passato dal client.
- **Descrizione**: stabilimento a cui si riferisce l'accettazione. `UNIQUE` → una sola riga per
  stabilimento; l'accettazione di una nuova versione **sovrascrive** la precedente (vedi criticità).
- **Esempio di valori**: `10795`
- **Deprecato**: No

### acceptedAt
- **Tipo**: `datetime` (`Sequelize.DATE`)
- **Nullabile**: Sì
- **Descrizione**: data/ora dell'azione di accettazione. Valorizzata con `new Date()` quando
  `accepted = true`; **rimessa a `null`** quando `accepted = false` (revoca).
- **Esempio di valori**: `null`, `2026-07-20 09:15:42`
- **Deprecato**: No

### userId
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: `users.id` dell'utente (admin/superadmin) che ha eseguito l'azione — `req.userId` dal
  JWT. Nessuna FK dichiarata.
- **Descrizione**: attore dell'accettazione. Nella riscrittura → `user_operator_id` /
  `user_updated_by_id` con FK reale
  ([`docs/03-gestione-dati.md` §3.2](../../docs/03-gestione-dati.md)).
- **Esempio di valori**: `3128`
- **Deprecato**: No

### ipAddress
- **Tipo**: `varchar(64)`
- **Nullabile**: Sì
- **Descrizione**: IP del client al momento dell'accettazione, normalizzato dal controller
  (`::1` → `127.0.0.1`; `::ffff:1.2.3.4` → `1.2.3.4`; primo valore di `X-Forwarded-For`). Dato di
  audit legale.
- **Esempio di valori**: `"127.0.0.1"`, `"93.44.x.x"`
- **Deprecato**: No

### contractDocumentsId
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: [`contract_documents.id`](contract_documents.md)
  (`db.contractAcceptation.belongsTo(db.contractDocuments, { foreignKey: "contractDocumentsId" })`).
  Copiato da `property.contractDocumentsId` al momento dell'azione.
- **Descrizione**: quale documento è stato accettato. Snapshot: resta il valore accettato anche se poi
  la property viene puntata a un altro documento.
- **Esempio di valori**: `1`
- **Deprecato**: No

### documentHash
- **Tipo**: `varchar(128)`
- **Nullabile**: Sì
- **Descrizione**: hash **SHA-256 (hex)** del PDF scaricato da `contract_documents.link` al momento
  dell'accettazione (`crypto.createHash("sha256")...digest("hex")`). Prova di *quale contenuto esatto*
  è stato accettato. `null` quando `accepted = false` (nessun download in revoca).
- **Esempio di valori**: `null`, `"e3b0c44298fc1c149afbf4c8996fb924..."` (64 hex)
- **Deprecato**: No

### accepted
- **Tipo**: `tinyint(1)` (`Sequelize.BOOLEAN`), `NOT NULL`, default `false`
- **Nullabile**: No
- **Descrizione**: stato corrente: `1` = condizioni accettate, `0` = non accettate / revocate. La
  `GET /api/sb/contract-condition` restituisce `accepted: acceptation ? acceptation.accepted : false`.
- **Esempio di valori**: `0`, `1`
- **Candidato a enumerazione**: no — booleano (`is_accepted`); l'eventuale ciclo (accettata → revocata →
  ri-accettata) va tenuto in uno **storico** anziché sovrascritto.
- **Deprecato**: No

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard (`timestamps: true`).
- **Nullabile**: No
- **Esempio di valori**: `2026-07-20 09:15:42`

---

## Osservazioni verificate nel codice (criticità)

- **`propertyId` `UNIQUE` → nessuno storico**: accettare una nuova versione, o revocare e ri-accettare,
  fa `existing.update(payload)` sull'**unica** riga. Per un dato di valenza legale serve invece un
  **registro append-only** (una riga per ogni evento di accettazione/revoca, con la versione e l'hash del
  momento) — vedi [`docs/03-gestione-dati.md` §3.6](../../docs/03-gestione-dati.md).
- **Nessuna FK reale** su `userId` (e `propertyId`/`contractDocumentsId` sono FK Sequelize senza
  vincolo InnoDB garantito nel legacy).
- **Accettazione accoppiata alla rete**: `updateContractCondition` scarica il PDF in sincrono per
  l'hash; se il file non è raggiungibile entro 20 s l'accettazione fallisce (`DOCUMENT_FETCH_FAILED`).
- **Autorizzazione**: solo `admin`/`superadmin` (`isAdminOrSuperAdmin` via `user.getRoles()`), ma la
  route monta comunque `authJwt.optionalAuth` a livello Express — il gate è tutto nel service.
- **`acceptedAt` azzerato in revoca**: si perde la data dell'ultima accettazione valida (di nuovo,
  risolto da uno storico).
- **Tabella vuota**: descrizioni da model + service + commenti, non da dati osservati.
