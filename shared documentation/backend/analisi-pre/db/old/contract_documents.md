# Tabella `contract_documents`

> Fonti: schema (tabella **vuota**, 0 righe in `local_sb`, `AUTO_INCREMENT = 1`), model
> [`back-pre/app/models/contractDocuments.model.js`](../../back-pre/app/models/contractDocuments.model.js)
> (`sequelize.define("contract_documents", ..., { freezeTableName: true, timestamps: true })` → ORM
> `db.contractDocuments`), servizio
> [`Service/contractConditionService.js`](../../back-pre/app/Service/contractConditionService.js) (letto
> per intero), controller
> [`contractCondition.controller.js`](../../back-pre/app/controllers/contractCondition.controller.js),
> associazioni in [`app/models/index.js:482-496`](../../back-pre/app/models/index.js), routing in
> [`app/routes/user.routes.js:2966-2973`](../../back-pre/app/routes/user.routes.js)
> (`GET /api/sb/contract-condition`, `POST /api/sb/contract-condition/update`), campo collegato
> [`properties.contractDocumentsId`](properties.md) (model
> [`property.model.js:118-121`](../../back-pre/app/models/property.model.js)).
>
> **Nota di contesto** — tabella **non censita** prima di questa scheda. È il **catalogo dei documenti
> contrattuali** (addendum, es. "Addendum Service Fee") che la piattaforma pubblica e che l'admin dello
> stabilimento deve accettare. Coppia con [`contract_acceptation`](contract_acceptation.md) (il registro
> delle accettazioni). Feature **presente nel codice ma non ancora esercitata** su questa base dati
> (entrambe le tabelle a 0 righe; nessuna `properties.contractDocumentsId` valorizzata → la funzione
> risponde `{ enabled: false }` per tutti).
>
> **Dominio di destinazione proposto**: **C5 — Identity/Account**, nuova sottoarea `account`/`contratti`
> (relazione contrattuale piattaforma ↔ stabilimento; gestione documenti verosimilmente lato SuperAdmin).
> Il puntatore `properties.contractDocumentsId` resta in **C2** (è uno stato dello stabilimento) — caso
> tipico di "tabella in un dominio, campo in un altro".

---

## Uso nei frontend (via API)

**Sì — ma solo in `front-modern-pre`** (il frontend della riscrittura). `front-pre`,
`front-customer-pre` e `front-app-pre` **non** chiamano gli endpoint `contract-condition`.

- Client REST: [`front-modern-pre/src/remote/SummerBooking/Generic/GenericRestApi.ts`](../../front-modern-pre/src/remote/SummerBooking/Generic/GenericRestApi.ts)
  — `getContractCondition()` → `GET sb/contract-condition`;
  `updateContractCondition(accepted)` → `POST sb/contract-condition/update` `{ accepted }`.
- Orchestrazione: [`front-modern-pre/src/app/Conditions/ConditionsApi.ts`](../../front-modern-pre/src/app/Conditions/ConditionsApi.ts)
  — `loadConditions()` gira **al bootstrap** (signals `effect`, una sola volta) e chiama
  `getContractCondition()`. Se la risposta ha `enabled === true && accepted === false` imposta
  `showPrompt = true`.
- UI: [`front-modern-pre/src/modules/ConditionsUpdate/ConditionsUpdate.tsx`](../../front-modern-pre/src/modules/ConditionsUpdate/ConditionsUpdate.tsx),
  montata in [`modules/Main/Main.tsx`](../../front-modern-pre/src/modules/Main/Main.tsx) (`<ConditionsUpdate />`,
  attiva). Mostra una **modale bloccante** "Aggiornamento delle Condizioni Contrattuali" con: link
  "Scarica PDF" (= `contract_documents.link`, campo `pdfLink` della risposta), checkbox obbligatoria
  ("Dichiaro di aver letto e di accettare l'Addendum…"), pulsanti "Leggi dopo" (chiude) e "Accetto e
  continuo" → `updateConditions()` → `POST .../update { accepted: true }` (che fa scrivere la riga in
  [`contract_acceptation`](contract_acceptation.md)).

**Perché 0 righe in produzione**: nessuno stabilimento ha `properties.contractDocumentsId` valorizzato
(in `local_sb`: 0 su 100 properties) → `getContractCondition` risponde `{ enabled: false }` per tutti →
la modale non compare mai → nessuna accettazione viene registrata e nessun documento è mai stato creato.
La feature è **completa end-to-end** (backend + `front-modern-pre`), ma **non attivata** per alcuna
property.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Relazioni**: referenziata da [`properties.contractDocumentsId`](properties.md) (FK dichiarata in
  `index.js`: `db.property.belongsTo(db.contractDocuments, { foreignKey: "contractDocumentsId" })`) e da
  [`contract_acceptation.contractDocumentsId`](contract_acceptation.md).
- **Esempio di valori**: 1, 2, 3, ..

### version
- **Tipo**: `varchar(255)` (`Sequelize.STRING`), `NOT NULL`
- **Nullabile**: No
- **Descrizione**: etichetta di versione dell'addendum, mostrata all'utente e restituita dalle API
  (`version: document.version`). Commento nel model: *"Version label of the addendum, e.g. 'Addendum
  Service Fee v1.0 del 13/07/2026'"*.
- **Esempio di valori**: `"Addendum Service Fee v1.0 del 13/07/2026"`
- **Deprecato**: No

### link
- **Tipo**: `varchar(500)`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: URL pubblico del PDF del documento. Usato in due modi da
  `contractConditionService.js`: (a) restituito come `pdfLink` alla UI; (b) **scaricato server-side**
  (`axios.get(link, { responseType: "arraybuffer", timeout: 20000 })`) per calcolarne l'hash SHA-256 al
  momento dell'accettazione.
- **Esempio di valori**: `"https://.../addendum-service-fee-v1.0.pdf"`
- **Nota riscrittura**: URL completo salvato a DB → vedi
  [`docs/03-gestione-dati.md` §1](../../docs/03-gestione-dati.md) (scomposizione `origin` + `path`).
- **Deprecato**: No

### note
- **Tipo**: `text` (`Sequelize.TEXT`)
- **Nullabile**: Sì
- **Descrizione**: note libere sul documento; restituita come `note` dalla `GET /api/sb/contract-condition`.
- **Esempio di valori**: `null`, `"Valido dal 01/08/2026"`
- **Deprecato**: No

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize standard (`timestamps: true`).
- **Nullabile**: No
- **Esempio di valori**: `2026-07-13 10:00:00`

---

## Osservazioni verificate nel codice (criticità)

- **Nessun endpoint di gestione (CRUD) esposto**: `contractCondition.controller.js` espone solo lettura
  stato + accettazione. La creazione/aggiornamento dei documenti e l'assegnazione di
  `properties.contractDocumentsId` non hanno API nel backend esaminato → gestione prevista via DB
  diretto o pannello SuperAdmin non presente in `back-pre`.
- **`link` come URL esterno completo**: da scomporre in migrazione (§1 di `03-gestione-dati.md`); inoltre
  il download sincrono in fase di accettazione (`computeDocumentHash`) accoppia l'accettazione alla
  raggiungibilità del file (`DOCUMENT_FETCH_FAILED` se il GET fallisce/scade a 20 s).
- **`version` è testo libero**: nessun vincolo di formato/unicità; è l'unico identificatore "umano"
  della versione accettata.
- **Tabella vuota**: feature mai attivata su questa base dati — le descrizioni derivano da model +
  service + commenti, non da dati osservati.
- **Integrità storica**: se una riga viene modificata dopo che qualche stabilimento l'ha accettata,
  `contract_acceptation.documentHash` permette di rilevare la divergenza, ma nulla impedisce la modifica
  in-place (manca versionamento immutabile — vedi
  [`docs/03-gestione-dati.md` §3.6](../../docs/03-gestione-dati.md), snapshot immutabile).
