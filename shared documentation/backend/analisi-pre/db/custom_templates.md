# Tabella `custom_templates`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **103 righe**, query
> completa; `createdAt` dal 2024-04-19 al 2026-07-27; **28 `property_id` distinti** — feature adottata
> da ~28 stabilimenti, non 3 come nel DB dev), model
> [`back-pre/app/models/customeTemplate.model.js`](../../../../../../back-pre/app/models/customeTemplate.model.js)
> (`sequelize.define("custom_template", …)` senza `freezeTableName` → nome **pluralizzato**
> `custom_templates`; senza `timestamps: false` → `createdAt`/`updatedAt` gestiti da Sequelize, a
> schema `datetime NULL`), registrato come `db.customeTemplate` in
> [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga 171). Commento del model su `type`:
> *"1 for email template and 2 for whatsApp template"*. **Nessuna associazione ORM, nessuna FK, nessun
> indice** oltre la PK.
>
> **Scrittori** — [`controllers/customTemplate.controller.js`](../../../../../../back-pre/app/controllers/customTemplate.controller.js)
> (tutte le route `authJwt.optionalAuth`):
> - `createTemplata` (`POST /api/sb/create-template`) — rifiuta se esiste già un template con lo stesso
>   `(title, type, property_id, status: 1)`; se `isDefault` → prima `update({ isDefault: 0 }, { where:
>   { isDefault: 1, property_id } })` (demuove il default precedente della property).
> - `updateTemplate` (`PATCH /api/sb/update-template`) — `status: 0` nel body = **soft-delete**;
>   gestione `isDefault` come sopra.
>
> **Lettori**:
> - `getAllTemplates` (`POST /api/sb/get-all-template`) — per `type` + `property_id`, `status: 1`.
> - `getTemplateById` (`POST /api/sb/get-template-by-id`) — per `id`, `status: 1`.
> - [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js)
>   `sendReservationSummery` (`POST /api/sb/sendReservationSummery`) —
>   `customTemplate.findOne({ where: { id: req.body.templateId } })` **senza filtro `status`/`type`**;
>   usa **solo** `subject` (oggetto della mail) e `attachment` (allegati). Il corpo `text` **non**
>   viene toccato lato server: il frontend invia già l'HTML renderizzato in `req.body.emailHtml`.
>
> **Frontend** (`front-pre`, copia in `front-customer-pre`):
> - editor template in
>   [`components/reserveArticle/Modal/SetTemplateForm.js`](../../../../../../front-pre/src/components/reserveArticle/Modal/SetTemplateForm.js)
>   (`onSaveEmailTemplate` → `type: 1`, `onSaveWhatsappTemplate` → `type: 2`), via
>   [`data/actions/managerActions.js`](../../../../../../front-pre/src/data/actions/managerActions.js)
>   (`sb/create-template`, `sb/get-all-template`, `sb/update-template`).
> - uso in
>   [`components/reserveArticle/Modal/ReservationSummaryModal.js`](../../../../../../front-pre/src/components/reserveArticle/Modal/ReservationSummaryModal.js)
>   ("Invia riepilogo prenotazione" dalla scheda prenotazione): i placeholder `@fullname`, `@location`,
>   `@date`, `@package`, `@total`, `@deposit`, `@phonenumber`, `@row`, `@QRcode`, … vengono sostituiti
>   **lato client**; per **email** (`type 1`) l'HTML risultante è spedito a `sendReservationSummery`;
>   per **WhatsApp** (`type 2`) `sendWhatsApp()` apre `https://wa.me/<numero>?text=<testo encodato>`
>   — **tutto lato client, nessun invio/persistenza server**.

---

## Nota di contesto

Template **email / WhatsApp riutilizzabili** con cui il gestore invia al cliente il **riepilogo della
prenotazione** ("Riepilogo prenotazione di …", preventivo, conferma). Sono per stabilimento
(`property_id`), con un template `isDefault` per property, e con `type` a distinguere il canale.

**Dati reali di produzione** (103 righe): **50 template `type 1` (email), 53 `type 2` (WhatsApp)** —
in produzione i template WhatsApp sono la maggioranza (il DB dev, 47/11, lo sottostimava fortemente);
59 attivi (`status: 1`), 44 soft-deleted (`status: 0`); **15 righe `isDefault: 1`** (su property
diverse → ~15 stabilimenti hanno un template predefinito). `createdBy` è `NULL` su **tutte** le 103
righe. `attachment` è `"[]"` su 95 righe, ma **8 righe hanno allegati reali** (PDF/DOCX di regolamenti
e loghi su S3). Restano molti record di prova (`"gtertretretreterterttre"`, `title` vuoto), ma anche
template reali multilingua.

**Dominio nella riscrittura**: **C6 — Comunicazione** (modulo `Notification`) — è un catalogo di
**template di messaggio**. Da unificare con gli altri due cataloghi di template del legacy, che oggi
usano sintassi di placeholder diverse:
[`hotel_message_templates`](hotel_message_templates.md) (C2, `{token}` nell'oggetto e `@token` nel
corpo) e i `textFields` di [`marketing_automation`](marketing_automation.md) (`@token`). Il rendering
va portato lato server (template engine con auto-escaping), non fatto con `String.replace` nel
browser.

---

## Campi

### id
- **Tipo**: `int(11)`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `104` per 103 righe.

### title
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci` — nome identificativo (non il contenuto)
- **Nullabile**: Sì
- **Descrizione**: etichetta mostrata nella tendina di selezione template. Unicità applicativa su
  `(title, type, property_id, status: 1)` (`createTemplata` rifiuta i duplicati).
- **Esempio di valori**: `"Preventivo"`, `"Detailed Default"`, `"QR R"`, `"Whats App Real testing"`

### text
- **Tipo**: `text` `COLLATE utf8mb4_unicode_ci` — corpo del messaggio con placeholder
- **Nullabile**: Sì
- **Descrizione**: testo del template con token `@…` sostituiti **lato client**. Token osservati:
  `@fullname`, `@location`, `@row`, `@date`, `@package`, `@total`, `@deposit`, `@phonenumber`,
  `@QRcode`. **Non** usato lato server (per l'email il frontend manda già l'HTML in `emailHtml`).
- **Esempio di valori**: `"@fullname\n@location\n@row\n@date\n@package\n@total\n@deposit\n@phonenumber\n@QRcode"`,
  `"Ciao @fullname\necco il preventivo"`,
  `"Hi, @fullname \n\nwe confirm your reservation at Lido San Domenico. \n\nLocation: @location …"` *(troncato)*

### type
- **Tipo**: `int(11)` — canale del template
- **Nullabile**: Sì (sempre valorizzato dal frontend: `1` o `2`)
- **Descrizione**: `1` = template **email**; `2` = template **WhatsApp**. Filtro di `getAllTemplates`.
- **Enumeratore**: **sì** — `enum MessageChannel { Email, WhatsApp }` (stessa coppia `1`/`2`, ma qui
  significa *canale*; da non confondere con `type` di [`notification_details`](notification_details.md)
  o [`booking_payments`](booking_payments.md), che è *soggetto*).
- **Esempio di valori** (prod): `2` (53 righe, WhatsApp), `1` (50 righe, email).

### createdBy
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Relazioni**: [`users`](users.md)`.id` (da `req.body.createdBy`) — nessuna FK. **`NULL` su tutte le
  103 righe di produzione** (il frontend non lo invia mai).
- **Descrizione**: operatore che ha creato il template.
- **Esempio di valori** (prod): `NULL` (sempre).
- **Deprecato**: **Sì** — mai valorizzato in produzione, mai letto.

### property_id
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Relazioni**: [`properties`](properties.md)`.id` — nessuna FK. Scoping di tutte le query e della
  demozione del default.
- **Esempio di valori** (prod): `96` (22 righe), `228` (19), `115` (9) — 28 property distinte, range
  `96` … `10842`.

### attachment
- **Tipo**: `longtext` `COLLATE utf8mb4_bin`, `CHECK (json_valid(attachment))`, `defaultValue: []` (nel
  model) — JSON array
- **Nullabile**: Sì
- **Descrizione**: allegati del template. `sendReservationSummery` fa `JSON.parse(attachmentData.attachment)`
  e mappa gli elementi (stringa → `{ path }`, oggetto → invariato) verso gli `attachments` nodemailer.
  Il blocco di upload S3 in `createTemplata` è **commentato**: il campo prende quello che manda il
  client. **In produzione**: `"[]"` su 95 righe, ma **8 righe hanno allegati reali** — array di URL S3
  (con lo spazio codificato come `%5B%5D` nel path, un artefatto: `.../files%5B%5D/Regolamento…docx`).
- **Esempio di valori** (prod): `[]`, `["https://summerbooking.s3.us-east-2.amazonaws.com/files%5B%5D/ITA%20-%20Regolamento%20spiaggia%202025.docx"]`.

### status
- **Tipo**: `int(11)`, `DEFAULT 1` — attivo / eliminato
- **Nullabile**: Sì
- **Descrizione**: `1` = attivo (i lettori filtrano `status: 1`); `0` = soft-deleted (`updateTemplate`
  con `status: 0`).
- **Enumeratore**: no — booleano/`enum ActiveState` (ennesima reimplementazione del concetto
  "attivo/non attivo", vedi [`docs/03-gestione-dati.md` §2.1](../../progettazione/03-gestione-dati.md)).
- **Esempio di valori** (prod): `1` (59 righe), `0` (44 righe).

### isDefault
- **Tipo**: `tinyint(1)`, `DEFAULT 0` — template predefinito della property
- **Nullabile**: Sì
- **Descrizione**: al più uno per `property_id` — enforced applicativamente (demozione del precedente
  in `createTemplata`/`updateTemplate`), **senza** transazione (a differenza di
  [`hotel_message_templates`](hotel_message_templates.md), che usa `sequelize.transaction`). Nessuna
  regola impedisce di *togliere* l'ultimo default. **In produzione: 15 righe `1`** (su property
  diverse).
- **Enumeratore**: no — booleano.
- **Esempio di valori** (prod): `0` (88 righe), `1` (15).

### subject
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`, `DEFAULT 'Oggetto da cambiare'`
- **Nullabile**: Sì
- **Descrizione**: oggetto della mail (solo `type 1`). È **l'unico campo di questa tabella usato lato
  server** (`sendReservationSummery`: `attachmentData?.subject ? … : "La tua prenotazione è confermata"`).
  Sui template WhatsApp resta il default `"Oggetto da cambiare"` (inutile). A volte `""`.
- **Esempio di valori**: `"Preventivo"`, `"Riepilogo prenotazione Di Fred"`, `"Oggetto da cambiare"`, `""`

### createdAt / updatedAt
- **Tipo**: `datetime`, `DEFAULT NULL` (gestiti da Sequelize)
- **Nullabile**: Sì (a schema)
- **Esempio di valori** (prod): `createdAt` `2024-04-19 14:25:56` … `2026-07-27 15:20:11`; `updatedAt`
  fino a `2026-08-15 19:24:05`.

---

## Osservazioni verificate nel codice (criticità)

- **Rendering nel browser**: i placeholder `@…` sono sostituiti con `String.replace` lato client e per
  l'email il frontend invia direttamente `emailHtml` — nessun template engine, nessun escaping,
  divergenza fra i client. Il corpo `text` salvato a DB non è la sorgente di verità dell'email.
- **WhatsApp senza tracciamento**: `type 2` produce solo un link `wa.me`; nessun record che il
  messaggio sia stato inviato (contrasto con l'obiettivo "outbox unico" del modulo Notification).
- **`authJwt.optionalAuth`** su tutte le route: chiunque, senza login, può creare/modificare template
  per una `property_id` arbitraria.
- **`isDefault` senza transazione**: demozione del precedente default e set del nuovo sono due
  `update` separati → finestra in cui la property ha 0 o 2 default.
- **Tre sintassi di placeholder** nel legacy (`@token` qui e in `marketing_automation.textFields`,
  `{token}` in `hotel_message_templates.subject`) — da unificare.
- **`subject` default `"Oggetto da cambiare"`** finisce come oggetto reale di email se l'operatore non
  lo cambia.
- **`createdBy` sempre `NULL`** in produzione (mai valorizzato) — audit assente; `attachment` `"[]"`
  sul 92% delle righe; molte righe di test mai ripulite; nessuna retention sui soft-deleted (44/103).
- Nessuna FK verso `properties` / `users`.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `type` | `int` `1`/`2` | `enum MessageChannel { Email, WhatsApp }` (canale — coerente con `custom_templates`/futuri canali del modulo Notification) |
| `status` | `int` `0`/`1` | `enum ActiveState { Active, Inactive }` (concetto condiviso, vedi §2.1) |

`isDefault` è un booleano (`tinyint`), non un enumeratore.
