# Tabella `csv_manager_details`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **13 righe**, query
> completa; `createdAt` dal 2023-07-20 al **2026-08-28** — uso saltuario ma tuttora attivo), model
> [`back-pre/app/models/csv-ownerDetail.model.js`](../../../../../../back-pre/app/models/csv-ownerDetail.model.js)
> (`sequelize.define("csv_manager_detail", ...)` → tabella pluralizzata `csv_manager_details`, ORM
> `db.csv_owner`), controller
> [`booking.controller.js:7806` `createCsvOwnerDetails`](../../../../../../back-pre/app/controllers/booking.controller.js)
> (`const csvOwnerDetail = db.csv_owner;`), template email
> [`mailtemplates/bookingTemplates.js:8387` `ownerDetail`](../../../../../../back-pre/app/mailtemplates/bookingTemplates.js),
> invio [`email.helper.js:2428` `sendOwnerdetailToAdmin`](../../../../../../back-pre/app/controllers/helpers/email.helper.js).
> Route [`app/routes/user.routes.js:1722`](../../../../../../back-pre/app/routes/user.routes.js)
> `POST /api/sb/createCsvOwnerDetails` (`authJwt.optionalAuth`).
>
> **Nota di contesto — a cosa serve.** Malgrado il nome (`csv_manager_detail` / `db.csv_owner` /
> `createCsvOwnerDetails`, residuo di un vecchio import CSV di gestori) è il registro dei **lead di
> "richiesta pagina" da parte di gestori di stabilimento**. Sulla pagina pubblica di dettaglio di un
> bagno (catalogo [`bathouses`](bathouses.md)) c'è il riquadro **"Sei il gestore di questo stabilimento
> balneare? — Richiedi la tua pagina"** (`lang.are_you_the_manager_establishment` /
> `lang.request_your_page`, pulsante `lang.request_now`): un titolare che trova il proprio stabilimento
> già elencato sul sito ma non ancora gestito da lui compila **nome / email / telefono / messaggio** e
> invia. Non è "contatta lo stabilimento" né una richiesta di prenotazione — è un **contatto
> commerciale / di onboarding** ("rivendica la tua scheda").
>
> **Frontend che lo usano:**
> - **front-customer-pre**: [`components/consumer/propertyDetails/PropertyContact.js`](../../../../../../front-customer-pre/src/components/consumer/propertyDetails/PropertyContact.js)
>   (`submitForm` → action `consumerAction.createCsvOwnerDetails` → `POST sb/createCsvOwnerDetails`;
>   toast di conferma `lang.message_sent_successfully` / "Ti ricontatteremo il prima possibile").
> - **front-app-pre**: [`screens/Dashboard/PropertyListContract.js`](../../../../../../front-app-pre/app/screens/Dashboard/PropertyListContract.js)
>   (`Api.createOwnerDetails`, label `are_you_the_manager_establishment`).
> - **front-pre**: esiste l'action `consumerAction.createCsvOwnerDetails` (`data/actions/consumerAction.js:323`)
>   ma non è stato individuato un componente montato che la richiami.
> - **front-modern-pre**: nessun uso.
>
> **Flusso backend** ([`booking.controller.js` `createCsvOwnerDetails`](../../../../../../back-pre/app/controllers/booking.controller.js)):
> body `{ fullName, phoneNumber, email, message, bathhouseId }` → `bathouses.findOne` per il solo
> `bathouseName` → `csvOwnerDetail.create({ fullName, email, phoneNumber, message })` (**`bathhouseId`
> non viene salvato**) → [`email.helper.sendOwnerdetailToAdmin`](../../../../../../back-pre/app/controllers/helpers/email.helper.js)
> invia a **`info@summerbooking.it`** (hardcoded) con oggetto **"Richiesta struttura"**, template
> [`bookingTemplates.js` `ownerDetail`](../../../../../../back-pre/app/mailtemplates/bookingTemplates.js), e logga in
> [`email_logs`](email_logs.md).
>
> **Volumi**: **13 righe in produzione**, `createdAt` 2023-07-20 → 2026-08-28 (una manciata di
> richieste all'anno; uso saltuario ma non cessato).
>
> **Dominio di destinazione proposto**: **C6 — Comunicazione & marketing** (lead / contatti in
> ingresso). In alternativa una piccola area "Sito pubblico / vetrina". Candidata a essere sostituita
> da un modulo contatti/CRM o da un semplice invio email senza persistenza; se si tiene, va rinominata
> (`establishment_claim_requests` / `owner_leads`) e collegata con FK reale a `bathouses`.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `13` (contigui).

### fullName
- **Tipo**: `varchar(255)` (`Sequelize.STRING`)
- **Nullabile**: Sì (nessuna validazione server-side sul campo)
- **Descrizione**: nome e cognome dichiarati dal visitatore nel form. Passato al template email come
  `name`.
- **Esempio di valori**: `"Salvatore Cunsolo"`, `"tivig dana"`
- **Deprecato**: No

### email
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: email di contatto del visitatore. Usata come **destinatario del log**
  (`sendOwnerdetailToAdmin` scrive `emailTo: email` in [`email_logs`](email_logs.md)) mentre il
  destinatario reale dell'email è `info@summerbooking.it`. Nessuna validazione di formato lato backend
  (solo lato form).
- **Esempio di valori**: `"salvoc-@hotmail.it"`, `"daniela.tivig@cleaninglogistic.ro"`
- **Deprecato**: No

### phoneNumber
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: telefono del visitatore (formato libero, spesso con prefisso internazionale).
- **Esempio di valori**: `"+393924692059"`, `"+40720111112"`
- **Deprecato**: No

### message
- **Tipo**: `varchar(255)`
- **Nullabile**: Sì
- **Descrizione**: testo libero della richiesta del gestore. **Nei dati reali di produzione** metà sono
  stringhe di test (`"kkk"`, `"kkkk"`, `"tttt hhhh dkddd"`) e metà sono **vere richieste di prenotazione
  scritte dai clienti** (`"Buongiorno per domani due sdraio e due lettini con ombrellone quanto
  costa"`, `"Vorrei prenotare un ombrellone e due lettini"`, `"Hi, I would like to book 5 beds…"`) —
  conferma che il form "richiedi la tua pagina" viene frainteso e usato come contatto prenotazione.
  Limite a 255 caratteri (troncamento silenzioso).
- **Esempio di valori** (prod): `"2 lettini e due sdraio con un ombrellone, quanto costa?"`, `"kkkk"`.
- **Deprecato**: No

### createdAt / updatedAt
- **Tipo**: `datetime` — timestamp Sequelize (il model li dichiara esplicitamente come attributi
  `Sequelize.DATE` senza default).
- **Nullabile**: Sì
- **Descrizione**: `createdAt` = data della richiesta. `updatedAt` non è mai aggiornato (la riga non
  viene mai modificata).
- **Esempio di valori** (prod): range `2023-07-20 17:26:09` … `2026-08-28 17:33:24`.

---

## Osservazioni verificate nel codice (criticità)

- **`bathhouseId` non persistito**: il controller legge `bathouses.bathouseName` da `req.body.bathhouseId`
  ma **non** salva l'id nella riga → si perde il collegamento allo stabilimento a cui la richiesta si
  riferisce (resta solo il nome, dentro il corpo dell'email). Da aggiungere `bath_house_id` con FK.
- **Crash se `bathhouseId` non valido**: `bathHouseData.bathouseName` viene dereferenziato senza
  controllare che `findOne` abbia restituito una riga → `TypeError` (500) se l'id non esiste.
- **Nessuna validazione né rate limiting**: endpoint `optionalAuth`, nessun captcha, nessun
  `publicLimiter` (commentato) → form pubblico aperto allo spam (coerente con le righe di test nei dati).
- **`emailTo` fuorviante nel log**: in [`email_logs`](email_logs.md) la riga risulta inviata
  all'email del visitatore, mentre il messaggio va all'indirizzo admin cablato
  `info@summerbooking.it` (hardcoded in `email.helper.js`).
- **Nome tabella/legacy**: `csv_manager_detail` / `db.csv_owner` / `createCsvOwnerDetails` non hanno
  relazione con la funzione attuale — residuo di un import CSV. Da rinominare in riscrittura.
- **Feature poco usata ma non morta**: 13 righe totali in produzione, ultima 2026-08-28 → valutare
  dismissione a favore di un invio email semplice o di un modulo contatti unificato (C6).
