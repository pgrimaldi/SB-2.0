# Tabella `switchboard_wa_template`

> Fonti: schema del DB `local_sb` (**0 righe**); **confermato 0 righe anche nel dump di produzione `sb_prod_dump_2026_09_08`** — la tabella è esportata solo come schema (nessun `INSERT`) su queste due copie; model
> [`back-pre/app/models/switchboardWaTemplate.model.js`](../../../../../../back-pre/app/models/switchboardWaTemplate.model.js)
> (`sequelize.define("switchboard_wa_template", …, { freezeTableName: true, timestamps: false, underscored: true })`),
> registrato come `db.switchboardWaTemplate` in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga 266).
>
> **FK InnoDB reale**: `property_id → properties.id` (`_ibfk_1`, indice non unico). ORM
> `belongsTo(property)` / `property.hasMany`.
>
> **Scrittori** (via [`Service/switchboardService.js`](../../../../../../back-pre/app/Service/switchboardService.js) +
> [`helpers/whatsapp.helper.js`](../../../../../../back-pre/app/controllers/helpers/whatsapp.helper.js)):
> - `createWaTemplate` — `POST /api/sb/switchboard/wa-templates`: crea **prima su Meta**
>   (`whatsappHelper.createTemplate` su `/{WABA}/message_templates`), poi salva la riga con
>   `metaTemplateId = <id Meta>` e `metaStatus = "PENDING"`.
> - `refreshWaTemplateStatus` — `GET .../wa-templates/:propertyId/:templateId/status`: rilegge lo stato
>   da Meta (`whatsappHelper.getTemplate`) e aggiorna `metaStatus = status.toUpperCase()` +
>   `metaRejectionReason = rejected_reason`.
> - `deleteWaTemplate` — `DELETE .../wa-templates/:propertyId/:templateId`: elimina su Meta
>   (per `name`) e poi la riga.
> - `deleteConfig` — elimina tutti i template dello stabilimento (solo DB, **non** su Meta).
>
> **Lettori**:
> - `listWaTemplates` — `GET /api/sb/switchboard/wa-templates/:propertyId`.
> - **Non** è la sorgente dell'invio: `sendWaTemplateMessage` (`POST .../wa-templates/send`) e
>   l'azione IVR `send_whatsapp` inviano passando `templateName` + `language` **direttamente**
>   (nessuna lettura/validazione contro questa tabella; il `templateName` deve esistere lato Meta).
>
> **Frontend**: nessuna UI nei sorgenti presenti. Vedi [`switchboard_config.md`](switchboard_config.md).
>
> **⚠️ Aggiornamento 2026-09-11 — verificato su `summer_booking_dev`** (RDS, vedi
> [`switchboard_config.md`](switchboard_config.md)): **3 righe reali** (`property_id = 96`), **tutte**
> `category = "UTILITY"` e `meta_status = "APPROVED"` (mai `MARKETING`/`AUTHENTICATION`,
> `PENDING`/`REJECTED` nei dati osservati) — `meta_template_id` valorizzato per tutte e 3, a conferma
> che sono state **realmente create e approvate su Meta**, non solo bozze locali: `prenotazione_test001`
> (HEADER testo + BODY con 1 parametro + FOOTER + BUTTONS con 1 bottone URL), `prenotazione_completa001`
> (BODY con 1 parametro + FOOTER), `richiesta_info` (solo BODY, nessun parametro). Struttura `components`
> reale coerente in tutto e per tutto col formato ipotizzato sotto (conferma che il documento descritto
> è accurato). **Nessuna** riga referenziata da `switchboard_menu_action` reale (vedi
> [`switchboard_menu_action.md`](switchboard_menu_action.md)): i template esistono ma **non risultano
> ancora agganciati** a un'azione IVR `send_whatsapp` in questo ambiente.

---

## Verdetto: feature attiva, con template reali approvati su Meta (in un ambiente diverso da questa copia del DB)

**0 righe** su `local_sb` e sul dump di produzione, **3 righe reali** su `summer_booking_dev` (vedi
sopra, tutte `UTILITY`/`APPROVED`). Il campo `components` rispecchia il formato *components* dell'API
Meta Cloud (header/body/footer/buttons) passato as-is da `whatsappHelper.createTemplate` — confermato
identico nei 3 esempi reali.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### property_id
- **Tipo**: `int`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: **FK** `_ibfk_1` → [`properties`](properties.md)`.id`.
- **Descrizione**: stabilimento proprietario del template. Chiave di `listWaTemplates` /
  `getWaTemplatesByProperty`.
- **Esempio di valori**: teorico `9669`

### template_name
- **Tipo**: `varchar(512)`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: nome del template su Meta. Meta richiede unicità per WABA, minuscolo con underscore.
  È il valore passato come `template.name` all'invio (`sendTemplateMessage`) e come `params.name`
  alla cancellazione su Meta (`deleteTemplate`).
- **Esempio di valori**: teorico `"avviso_reception"`, `"conferma_prenotazione"`

### language
- **Tipo**: `varchar(10)`, `NOT NULL`, `DEFAULT 'it'`
- **Nullabile**: No
- **Descrizione**: codice lingua del template nel formato Meta (`language.code` all'invio). Default
  `"it"` sia a schema sia nel controller (`language || "it"`).
- **Esempio di valori**: `"it"` (default), `"en_US"`, `"en"`
- **Enumeratore**: insieme chiuso "di libreria" (codici lingua Meta, stile BCP-47) — **non** enum di
  dominio; trattamento come `country` / `timezone` ([§2.4 di 03-gestione-dati.md](../../progettazione/03-gestione-dati.md)).

### category
- **Tipo**: `varchar(30)`, `NOT NULL`, `DEFAULT 'UTILITY'`
- **Nullabile**: No
- **Descrizione**: categoria Meta del template. Default `"UTILITY"` a schema e nel controller
  (`category || "UTILITY"`). Passata as-is a `createTemplate`.
- **Esempio di valori**: `"UTILITY"` (default), `"MARKETING"`, `"AUTHENTICATION"`
- **Enumeratore**: **sì** — `enum WaTemplateCategory { Marketing, Utility, Authentication }`
  (vocabolario chiuso Meta; impatta pricing e regole di invio).

### components
- **Tipo**: `json` (`longtext` `utf8mb4_bin`), `NOT NULL`
- **Nullabile**: No
- **Descrizione**: array dei *components* del template nel formato Meta. Salvato così com'è ricevuto
  dal body e inviato a Meta. Struttura tipica per elemento:
  - `type`: `"HEADER"` | `"BODY"` | `"FOOTER"` | `"BUTTONS"`
  - per `HEADER`: `format` (`"TEXT"`|`"IMAGE"`|`"DOCUMENT"`|`"VIDEO"`), `text`, `example`
  - per `BODY`: `text` (con segnaposto `{{1}}`, `{{2}}`…), `example.body_text`
  - per `FOOTER`: `text`
  - per `BUTTONS`: `buttons[]` con `type` (`"QUICK_REPLY"`|`"URL"`|`"PHONE_NUMBER"`|`"COPY_CODE"`),
    `text`, `url`/`phone_number`/`example`
- **Esempio di valori** (sotto-valori):
  ```json
  [
    { "type": "BODY", "text": "Ciao {{1}}, ti contattiamo dallo stabilimento {{2}}.",
      "example": { "body_text": [ ["Mario", "Bagno Reception"] ] } },
    { "type": "FOOTER", "text": "Summer Booking" },
    { "type": "BUTTONS", "buttons": [ { "type": "QUICK_REPLY", "text": "Richiamami" } ] }
  ]
  ```

### meta_status
- **Tipo**: `enum('PENDING','APPROVED','REJECTED')`, `NOT NULL`, `DEFAULT 'PENDING'` — **già ENUM
  nativo MySQL**
- **Nullabile**: No
- **Descrizione**: stato di approvazione lato Meta. Inizializzato a `"PENDING"` alla creazione; poi
  `refreshWaTemplateStatus` lo aggiorna con `metaData.status.toUpperCase()`. **Meta può restituire
  anche valori fuori dall'ENUM** (`PAUSED`, `DISABLED`, `IN_APPEAL`, `PENDING_DELETION`…): in tal caso
  l'`UPDATE` sull'ENUM MySQL fallirebbe / troncherebbe.
- **Esempio di valori**: `"PENDING"` (default), `"APPROVED"`, `"REJECTED"`
- **Enumeratore**: **sì** — `enum WaTemplateStatus { Pending, Approved, Rejected, Paused, Disabled,
  InAppeal, … }` come **passthrough** dello stato Meta (da non vincolare a soli 3 valori); conservare
  eventualmente anche il grezzo.

### meta_template_id
- **Tipo**: `varchar(50)`
- **Nullabile**: Sì
- **Descrizione**: id assegnato da Meta alla creazione del template (`metaResponse.id`). Usato da
  `refreshWaTemplateStatus` per interrogare `/{id}` su Meta. Se assente, il refresh restituisce
  `null`.
- **Esempio di valori**: teorico id numerico Meta come stringa (`"1200000000000000"`)

### meta_rejection_reason
- **Tipo**: `text`
- **Nullabile**: Sì
- **Descrizione**: motivo di rifiuto restituito da Meta (`metaData.rejected_reason`), popolato da
  `refreshWaTemplateStatus`. `null` finché non rifiutato.
- **Esempio di valori**: teorico `"INVALID_FORMAT"`, `"ABUSIVE_CONTENT"`

---

## Osservazioni verificate nel codice (criticità)

- **La tabella non governa l'invio**: `send_whatsapp` (IVR) e `sendWaTemplateMessage` accettano un
  `templateName` libero senza verificare che esista qui né che sia `APPROVED`. Sorgente di verità è
  Meta, non il DB.
- **`meta_status` ENUM troppo stretto**: Meta espone più stati di 3; `refreshWaTemplateStatus` scrive
  `toUpperCase()` grezzo → possibile errore/troncamento su valori non previsti.
- **Disallineamento su `deleteConfig`**: cancella le righe ma **non** i template su Meta (a differenza
  di `deleteWaTemplate`).
- **Nessun vincolo di unicità** `(property_id, template_name, language)` a schema, benché Meta lo
  imponga sul WABA.
- **`components` non validato** contro un vocabolario di segnaposto/formati: nessun controllo prima
  dell'inoltro a Meta.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `category` | `varchar(30)` default `'UTILITY'` | `enum WaTemplateCategory { Marketing, Utility, Authentication }` |
| `meta_status` | `ENUM('PENDING','APPROVED','REJECTED')` | `enum WaTemplateStatus` come passthrough Meta (Pending/Approved/Rejected/Paused/Disabled/InAppeal/…) |
| `language` | `varchar(10)` default `'it'` | non enum: codice lingua Meta (insieme "di libreria", come `country`) |

---

## Modellazione nella riscrittura — `components`: JSON o tabelle plain?

**Risposta: lasciarlo JSON** (`jsonb` in PostgreSQL), **non** spezzarlo in tabelle plain. È uno
**snapshot della definizione template così come vive su Meta**.

Motivazioni (verificate sul codice + contratto Meta Cloud API):

1. **Lo schema non è nostro ed è evolutivo.** La forma di `components` è definita e versionata da
   Meta: `HEADER` / `BODY` / `FOOTER` / `BUTTONS`, con sotto-tipi bottone (`URL`, `QUICK_REPLY`,
   `PHONE_NUMBER`, `COPY_CODE`, `FLOW`, `MPM`…), `format` dell'header (`TEXT`/`IMAGE`/`VIDEO`/
   `DOCUMENT`/`LOCATION`), parametri posizionali `{{1}}` o nominali, blocchi `example`, e novità che
   Meta aggiunge di continuo (carousel, limited-time offer, template di autenticazione con bottone
   OTP, catalog/MPM). Normalizzare in tabelle plain significa trasformare **ogni modifica dell'API
   Meta in una migrazione di schema**.
2. **È passthrough puro, scrittura una-tantum.** Il codice riceve `components` dal body, lo inoltra
   **verbatim** a `POST /{WABA}/message_templates` ([`whatsapp.helper.js`](../../../../../../back-pre/app/controllers/helpers/whatsapp.helper.js)
   `createTemplate`), lo salva verbatim e lo rilegge verbatim nel DTO. **Nessun campo interno viene
   mai letto, validato o trasformato dal backend.** `refreshWaTemplateStatus` riprende da Meta solo
   `status` / `rejected_reason`, **non** `components`.
3. **La fonte di verità è Meta.** Dopo la creazione la definizione è immutabile lato nostro (esistono
   solo create / refresh-status / delete; **nessun endpoint di update template**). La riga è una
   **copia di cache/audit** di ciò che Meta possiede.
4. **Nessun accesso relazionale.** Non si interroga mai "tutti i template con bottone URL" o "quelli
   con 3 parametri nel body": non c'è un pattern di query che giustifichi la normalizzazione.
5. **Coerente con [§3.6 di `03-gestione-dati.md`](../../progettazione/03-gestione-dati.md)**: lo *snapshot
   tipizzato in colonne* serve per i **fatti storici di dati nostri** (righe scontrino, movimenti,
   prezzo bloccato). `components` è invece il **documento di un sistema esterno** → resta `jsonb`
   com'è (nuovo punto 5 di §3.6). Decomporlo richiederebbe comunque un catch-all JSON per le parti
   non modellate: anti-pattern "normalizzare il DTO di un'API di terzi".

**Cosa fare invece del blob grezzo di oggi:**

- Tipo colonna **`jsonb`** (non `json`/`text`): validazione strutturale e, se un domani servisse,
  indici GIN.
- **Validazione al write** (non decomposizione): (a) forma base Meta (tipi di componente ammessi, un
  solo `BODY`, ecc.); (b) **insieme chiuso dei segnaposto ammessi** — aggancio a
  [§2.7 di `03-gestione-dati.md`](../../progettazione/03-gestione-dati.md) (vocabolario canonico dei
  placeholder): rifiutare `create` se `components` contiene un parametro/segnaposto fuori vocabolario.
  Questo è il pezzo che va vincolato, **non** la struttura del blob.
- Opzionali: **poche colonne di riepilogo derivate** (read-only, calcolate dal JSON alla scrittura)
  per la lista del pannello admin senza parsare il blob a ogni render — es. `body_preview`,
  `header_format` (`none|text|image|video|document`), `has_buttons`, `buttons_count`, `param_count`.
  Sono **derivate, non autorevoli** (la verità resta `components` + Meta).
- **Header media**: se `components` contiene un header immagine/video/documento, l'`example` è un
  *handle* di upload Meta (token opaco), non un nostro URL S3 → [§1 di `03-gestione-dati.md`](../../progettazione/03-gestione-dati.md)
  (link/URL) **non** si applica; non va scomposto né riscritto.

Stesso ragionamento (JSON grezzo, non normalizzare) vale per i payload di sistemi esterni delle altre
tabelle del dominio — `switchboard_wa_msg_log.raw_payload`, `switchboard_call_log.raw_payload` /
`handler_result` — che sono comunque **log diagnostici da non migrare** come tabelle di dominio (vedi
relative schede).
