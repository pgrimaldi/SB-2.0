# Tabella `hotel_message_templates`

> Fonti: schema completo (`SHOW CREATE TABLE`); **0 righe nella copia locale del DB** (tabella vuota — le
> descrizioni sotto si basano quindi solo su model/codice, non su dati reali osservati; segnalato
> esplicitamente ove rilevante), model
> [`back-pre/app/models/hotelMessageTemplate.model.js`](../../back-pre/app/models/hotelMessageTemplate.model.js)
> (`sequelize.define("hotel_message_templates", ...)`, commento in testa al file: "Each property may have
> at most one active default template"), associazioni in
> [`back-pre/app/models/index.js:423-424`](../../back-pre/app/models/index.js), controller
> [`back-pre/app/controllers/hotel/hotelMessageTemplate.controller.js`](../../back-pre/app/controllers/hotel/hotelMessageTemplate.controller.js)
> (letto per intero, 218 righe) e service
> [`back-pre/app/Service/hotelMessageTemplateService.js`](../../back-pre/app/Service/hotelMessageTemplateService.js)
> (letto per intero, 290 righe), DTO
> [`back-pre/app/models/dto/hotel.dto.js`](../../back-pre/app/models/dto/hotel.dto.js) (classi
> `HotelMessageTemplateDto`/`...ListItemDto`/`...DetailDto`), frontend
> `front-modern-pre/src/pages/Agreements/Reports/SendReportTemplate*.tsx` (via `AgreementsRestApi.ts`).
>
> **Nota di contesto**: template di messaggio (email o WhatsApp) riutilizzabili, con placeholder, usati
> quando lo stabilimento invia un **report periodico** ("consuntivo convenzione") a una struttura partner
> — vedi `hotelReportController.sendReport`/`sendReportMessage` (dominio C1/reporting, tabella
> `hotel_reports` non documentata in questa scheda C2). A differenza di `hotels`/`hotel_price_lists`
> (collegati a `hotels.id`), questa tabella è collegata **direttamente a `properties.id`**: i template non
> sono per singola struttura partner, ma condivisi fra tutte le strutture della stessa property.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, .. — **aggiornamento rilevante rispetto alla nota "Fonti" in testa al
  documento**: una query più recente (successiva alla stesura originale di questa scheda) mostra che la
  tabella **non è più vuota**: contiene ora 10 righe reali, a differenza delle 0 righe riportate al momento
  della stesura originale — il DB di sviluppo è evidentemente evoluto nel frattempo. Gli esempi riportati nei
  bullet "Esempio di valori" seguenti si basano su questa query più recente.

### propertyId
- **Tipo**: `int`, `NOT NULL`
- **Vincoli**: nessuna FK dichiarata a livello DB verso `properties` (a differenza di `hotels.propertyId`,
  che invece ha una FK reale — asimmetria fra le due tabelle non spiegata nel codice).
- **Descrizione**: risolto **sempre lato server** da `req.userId` (mai dal body), come per `hotels`.
- **Nullabile**: No
- **Esempio di valori**: 96 — tutte le 10 righe attualmente presenti appartengono alla stessa property,
  stesso pattern di concentrazione già osservato su `hotels` (dato di test concentrato, non rappresentativo
  di un uso multi-tenant reale).

### name
- **Tipo**: `varchar(255)`, `NOT NULL`
- **Descrizione**: nome identificativo del template (per la lista di selezione), non il contenuto del
  messaggio.
- **Nullabile**: No
- **Esempio di valori**: "Default", "Report giornaliero check-in", "Nuovo modello"

### subject
- **Tipo**: `varchar(255)`, opzionale
- **Descrizione**: oggetto del messaggio (rilevante solo per invio email) — "può contenere placeholder"
  (commento a livello DB), sostituiti al momento dell'invio (logica di sostituzione in
  `hotelReportNotificationService.js`, dominio C1/reporting, non documentata in questa scheda).
- **Nullabile**: Sì
- **Esempio di valori**: "Report prenotazioni {hotelName} - {date}" — nota sintassi placeholder a graffe,
  vedi l'incoerenza rilevante segnalata sotto in `text` (che usa invece una sintassi con chiocciola)

### text
- **Tipo**: `text`, opzionale
- **Descrizione**: corpo del messaggio, con placeholder — usato sia per email sia per WhatsApp a seconda
  del canale scelto in fase di invio (`sendReportMessage`).
- **Nullabile**: Sì
- **Esempio di valori**: "Ciao @facilityname \n\nstabilimento @propertyname \n\nnumero prenotazioni
  @totalbookings..." — **incoerenza rilevante individuata**: il sistema usa **due sintassi di placeholder
  diverse** all'interno dello stesso record/dominio: `subject` usa le graffe (`{hotelName}`, `{date}`),
  mentre `text` usa la chiocciola (`@propertyname`, `@totalbookings`) — probabile segno di due motori/
  convenzioni di templating diversi coesistenti, o di un refactor incompleto mai armonizzato; da unificare
  su un'unica sintassi nella riscrittura.

### lang
- **Tipo**: `varchar(5)`, default `"it"`
- **Vincoli applicativi**: validato lato DTO (`hotel.dto.js:411,420`) con `lang.length <= 5` — coerente col
  limite di colonna a livello DB.
- **Descrizione**: codice lingua del template (es. `it`, `en`) — permette di avere più template con lo
  stesso scopo in lingue diverse. **Candidato a enumerazione**: vocabolario dichiarato a (almeno) due
  valori dal commento DB ("e.g. it, en"), ma di fatto **sempre `"it"` sulle 10 righe esistenti**, mai
  `"en"` — un `ENUM('it','en')` a livello DB sarebbe adeguato trattandosi di codici lingua fissi a due
  lettere, a meno di dover supportare in futuro molte più lingue, nel qual caso è preferibile una tabella
  di lookup.
- **Nullabile**: Sì
- **Esempio di valori**: "it" (unico valore osservato su tutte le 10 righe)

### isDefault
- **Tipo**: `tinyint(1)`, default `0`
- **Descrizione**: marca il template predefinito della property. **Invariante applicativo enforced con
  transazioni Sequelize esplicite** (`hotelMessageTemplateService.js`, commento in testa al file:
  "If a property has any active template, exactly one of them must have isDefault=1"):
  - **`createTemplate`** (righe 38-86): se è il primo template attivo della property, viene forzato
    `isDefault = true` indipendentemente da cosa richiesto dal client; se il client richiede
    esplicitamente `isDefault: true` su un template non il primo, il precedente default viene demosso
    (`isDefault: false`) nella stessa transazione.
  - **`updateTemplate`** (righe 174-234): promuovere un template a default demuove atomicamente quello
    corrente; **tentare di togliere il flag default senza promuoverne un altro viene esplicitamente
    rifiutato** con errore 400 ("Cannot unset the default template. Set another template as default
    instead.") — impedisce che una property resti senza alcun default.
  - **`deleteTemplate`** (righe 244-289): se il template cancellato era il default, il template attivo
    più recente (`ORDER BY createdAt DESC`) viene automaticamente promosso a nuovo default, nella stessa
    transazione del soft-delete.
  - Questo è l'**unico punto di tutto il dominio C2 documentato finora** (17 tabelle precedenti + queste 3)
    che usa transazioni Sequelize esplicite (`sequelize.transaction`) per proteggere un invariante
    multi-riga — un pattern di qualità nettamente superiore alla media del resto del codice legacy,
    verosimilmente perché scritto in un momento successivo/da mano diversa (coerente con quanto già
    osservato per l'intero modulo hotel in [`hotels.md`](hotels.md)).
- **Nullabile**: Sì
- **Esempio di valori**: 0, 1 — sulle 10 righe esistenti: 0 in 9 casi, 1 in 1 caso, coerente con l'invariante
  "un solo default per property" descritto sopra.

### isDeleted
- **Tipo**: `tinyint(1)`, default `0`
- **Descrizione**: soft-delete, con gestione coordinata del flag `isDefault` come sopra descritto.
- **Nullabile**: Sì
- **Esempio di valori**: 0, 1 — sulle 10 righe esistenti: 1 in 8 casi, 0 in 2 casi — la maggioranza dei
  template attuali risulta soft-cancellata, verosimilmente dati di test creati e poi rimossi durante lo
  sviluppo (dato coerente con la natura di ambiente di sviluppo del DB).

### createdBy
- **Tipo**: `int`, opzionale
- **Descrizione**: impostato server-side da `req.userId` (`hotelMessageTemplateService.createTemplate`,
  passato come `userId`).
- **Nullabile**: Sì
- **Esempio di valori**: 199 — **sempre 199 su tutte le 10 righe esistenti**, coerente con la creazione da
  parte di un singolo utente/operatore durante lo sviluppo/test, non un dato rappresentativo di un uso
  multi-operatore in produzione.

### createdAt / updatedAt
- **Tipo**: `datetime`, `NOT NULL` (a differenza di `hotels`/`hotel_price_lists`, qui sono dichiarati
  `NOT NULL` senza default esplicito a livello DB — popolati comunque da Sequelize in applicazione).
- **Nullabile**: No
- **Esempio di valori**: datetime standard, es. "2024-02-20 08:00:00"

---

## Endpoint (tutti `POST`, tutti sotto `/api/sb/hotel/message-template*`)

| Path | Funzione | Note |
|---|---|---|
| `/api/sb/hotel/message-template` | `createMessageTemplate` | applica l'invariante "primo template = default" |
| `/api/sb/hotel/message-template/list` | `getMessageTemplateList` | solo `id`/`name`/`isDefault`, ordinato con il default sempre primo (`ORDER BY isDefault DESC, name ASC`) |
| `/api/sb/hotel/message-template/details` | `getMessageTemplateDetails` | scoping per `propertyId` risolto da `req.userId` |
| `/api/sb/hotel/message-template/update` | `updateMessageTemplate` | gestisce le transizioni del flag `isDefault` come sopra |
| `/api/sb/hotel/message-template/delete` | `deleteMessageTemplate` | soft-delete + eventuale promozione automatica di un nuovo default |

---

## Osservazioni verificate nel codice

- **Tabella vuota nella copia locale del DB**: nessun template creato nei dati campione — le descrizioni
  sopra si basano interamente su model/service/DTO, non su valori reali osservati. Da non trattare come
  "feature non usata": la logica applicativa è completa e ben testata concettualmente (invarianti espliciti
  con transazioni), semplicemente non ci sono ancora dati nella copia parziale del DB usata per questa
  analisi.
- **`propertyId` senza FK dichiarata**, a differenza di `hotels.propertyId` — inconsistenza minore ma
  verificata fra le tabelle dello stesso modulo, probabilmente da correggere in una migrazione futura o
  semplicemente da replicare come FK reale nella riscrittura.
- **Route protette solo da `verifyToken`** (stesso rilievo già fatto per `hotels`/`hotel_price_lists`):
  `authJwt.isManager` commentato su tutte le route di questo controller
  (`hotel.routes.js:338-382`).
- **Nessuna relazione dichiarata fra questa tabella e `hotels`**: essendo scoped per `propertyId` e non
  per `hotelId`, i template sono condivisi fra tutte le strutture partner della stessa property — un
  design intenzionale (un unico "biglietto da visita" per i report, indipendente da quale struttura viene
  contattata), coerente con l'uso che se ne fa in `hotelReportController.sendReportMessage`.
