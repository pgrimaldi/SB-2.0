# Tabella `marketing_email_campaign`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **25 righe**, query
> completa; `createdAt` dal 2025-11-13 al 2026-08-24; 6 `propertyId` distinti — feature usata davvero
> da pochissimi stabilimenti). `name` è **`''` (vuoto) su tutte le 25 righe** (mai valorizzato);
> `state` `sent` (19), `deleted` (5), `draft` (1). Model
> [`back-pre/app/models/marketingEmailCampaign.js`](../../../../../../back-pre/app/models/marketingEmailCampaign.js)
> (`sequelize.define("marketing_email_campaign", …, { freezeTableName: true, timestamps: false })`;
> `createdAt`/`updatedAt` con `defaultValue: Sequelize.NOW`), registrato come
> `db.marketing_email_campaign` in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga 195).
> **FK reale** `contactListId` → [`contact_list`](contact_list.md)`(id)` `ON DELETE SET NULL ON UPDATE
> CASCADE` (indice `contactListId`); `belongsTo`/`hasMany` con `contact_list` in
> [`models/index.js:381-387`](../../../../../../back-pre/app/models/index.js).
>
> **Scrittori** —
> [`controllers/marketingEmailCampaign.controller.js`](../../../../../../back-pre/app/controllers/marketingEmailCampaign.controller.js)
> (route in [`routes/marketing.routes.js`](../../../../../../back-pre/app/routes/marketing.routes.js), **senza
> auth**):
> - `createCampaign` (`POST /api/sb/campaigns`) — `state = scheduledDate ? "scheduled" : "draft"`;
>   `scheduledDate` convertito a UTC da `moment.tz(\`${scheduledDate} ${scheduledTime}\`, timezone)`.
> - `updateCampaign` (`PUT /api/sb/campaigns`) — `draft → scheduled` se arriva `scheduledDate`;
>   ri-converte `scheduledDate` in `moment.tz(...)`.
> - `deleteCampaign` (`DELETE /api/sb/campaigns/:id`) — `state → "deleted"` (soft-delete).
> - `duplicateCampaign` (`POST /api/sb/campaigns/duplicate`) — copia con `state = "draft"`.
> - [`controllers/helpers/cron.helper.js`](../../../../../../back-pre/app/controllers/helpers/cron.helper.js)
>   `campaignEmail()` — cron **ogni 1 minuto** (`server.js` → `cronJob.campaignEmail()`):
>   `EmailCampaign.findAll({ where: { state: "scheduled", scheduledDate: { [Op.lte]: nowUTC } } })` →
>   `sendCampaignEmail(campaign)` → per ogni indirizzo di `recipientList` renderizza
>   `mailtemplates/campaignEmail.ejs` e invia con il **transporter globale** (`mailOptions.from` =
>   `noreply@summerbooking.it`, **non** l'SMTP della property), logga su [`email_logs`](email_logs.md)
>   (`emailType: "Campaign Email"`) e infine `campaign.update({ state: "sent" })`. Se
>   `enablePushNotifications` e il destinatario è un [`users`](users.md) `userStatus: "active"` con
>   `deviceTokens`, invia **anche** una push FCM (`sendPushNotification`, `title = subject`,
>   `body = message`). Se `sendCopy`, invia una copia al **manager** della property (query raw su
>   `roles.id = 2`).
>
> **Lettori**:
> - `getAllCampaigns` (`GET /api/sb/campaigns`) — Sequelize `findAll` + `include ContactList` (filtri
>   `filter`=state, `search` su `subject`, `propertyId`); `JSON.parse` di `recipientList`/`attachments`.
> - `getCampaignById` (`GET /api/sb/campaigns/:id`) — raw SQL join `contact_list` +
>   `contact_list_member` + `customers` (`id` **interpolato**).
>
> **Frontend** (`front-pre`, copia `front-customer-pre`): **Marketing → Campagne Email**
> ([`components/Marketing/Component/EmailMarketingCampaign/`](../../../../../../front-pre/src/components/Marketing/Component/EmailMarketingCampaign/)),
> via [`data/actions/marketingActions.js`](../../../../../../front-pre/src/data/actions/marketingActions.js)
> (`sb/campaigns` GET/POST/PUT, `sb/campaigns/:id`, `sb/campaigns/duplicate`).

---

## Nota di contesto

**Campagna email one-shot**: un messaggio (oggetto + corpo + allegati) inviato a una lista di
contatti, subito ("draft"→invio manuale non presente: di fatto si passa sempre da "scheduled") o a
data/ora pianificata. Il cron `campaignEmail` la spedisce quando `scheduledDate` (UTC) è raggiunta e
la porta a `sent`.

Punto chiave: **i destinatari effettivi sono lo snapshot JSON `recipientList`**, non la lista
`contactListId`. `contactListId` è memorizzato (e c'è la FK), ma `sendCampaignEmail` itera su
`recipientList` — un array di indirizzi congelato alla creazione. Modificare la lista dopo non cambia
i destinatari.

Nei dati di produzione: **25 righe**, `name` **sempre `""`**, `state` `sent` (19) / `deleted` (5) /
`draft` (1), `recipientList` array di email reali (con molte stringhe vuote), `timezone` `Europe/Rome`
(20) / `Asia/Calcutta` (3) / `Asia/Riyadh` (1) / `Europe/Berlin` (1), `scheduledTime` stringa libera
(`"13:31"`, `"9:50"`).

**Dominio nella riscrittura**: **C6 — Comunicazione / Marketing**. La campagna deve usare il canale
email del modulo (SMTP della property), risolvere i destinatari dalla lista al momento dell'invio (o
snapshot esplicito e versionato), e registrare l'esito per destinatario nell'outbox unico.

---

## Campi

### id
- **Tipo**: `int(11)`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `47` per 25 righe (churn).

### name
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`, `NOT NULL`
- **Nullabile**: No (a schema) — ma il frontend non lo invia → salvato come `""`
- **Descrizione**: nome interno della campagna. **Sempre stringa vuota** nei 7 record.
- **Esempio di valori**: `""`
- **Deprecato**: **parzialmente** — colonna `NOT NULL` mai valorizzata dal client.
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### subject
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: oggetto dell'email. Usato anche come chiave di ricerca in `getAllCampaigns`
  (`LIKE %search%`).
- **Esempio di valori**: `"dev test"`, `"test sendgrid"`, `"test01"`

### message
- **Tipo**: `text` `COLLATE utf8mb4_unicode_ci`, `NOT NULL`
- **Nullabile**: No
- **Descrizione**: corpo del messaggio, iniettato come `MESSAGE` nel template
  `mailtemplates/campaignEmail.ejs`. Testo semplice nei dati.
- **Esempio di valori**: `"test"`, `"ttest sendgrid test"`

### recipientList
- **Tipo**: `longtext` `COLLATE utf8mb4_bin`, `CHECK (json_valid(recipientList))`, `NOT NULL` — JSON
  array di stringhe email
- **Nullabile**: No
- **Descrizione**: **destinatari effettivi** dell'invio (snapshot alla creazione). `sendCampaignEmail`
  itera su questo array; `getAllCampaigns`/`getCampaignById` lo `JSON.parse`.
- **Esempio di valori**: `["devnew2026@yopmail.com"]`,
  `["igor.miti@yopmail.com","l.pardini@itsprodigi.it"]`

### state
- **Tipo**: `enum('draft','scheduled','sent','deleted')` `COLLATE utf8mb4_unicode_ci`, `DEFAULT 'draft'`
  — **già ENUM nativo MySQL**
- **Nullabile**: Sì
- **Descrizione**: `draft` (nessuna data) → `scheduled` (data futura) → `sent` (dopo il cron) /
  `deleted` (soft-delete).
- **Enumeratore**: **sì** — `enum CampaignState { Draft, Scheduled, Sent, Deleted }` (mappatura
  diretta; `Deleted` da separare dal soft-delete se si introduce `deleted_at`).
- **Esempio di valori**: `"sent"`, `"deleted"`

### scheduledDate
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: data/ora di invio programmata, **memorizzata in UTC** (`createCampaign` converte da
  `timezone`); il cron confronta `Op.lte` con `moment().utc()`. In `updateCampaign` la conversione è
  rifatta anche quando `scheduledDate` è già in formato datetime (rischio doppio shift).
- **Esempio di valori**: `2026-08-24 07:45:00`, `2026-05-08 10:00:00`

### scheduledTime
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: orario `"H:mm"` come stringa libera, concatenato a `scheduledDate` prima della
  conversione tz. Ridondante con l'ora già dentro `scheduledDate`; usato solo per ricostruire il
  form. Formati non normalizzati (`"13:31"`, `"9:50"`, `"12:00"`).
- **Esempio di valori**: `"13:31"`, `"9:50"`

### enablePushNotifications
- **Tipo**: `tinyint(1)`, `DEFAULT 0`
- **Nullabile**: Sì
- **Descrizione**: se attivo, dopo l'invio email `sendCampaignEmail` cerca l'utente
  [`users`](users.md) (`email = recipient`, `userStatus: "active"`) e, se ha `deviceTokens`, invia una
  **push FCM** (`sendPushNotification`, `title = subject`, `body = message || "Welcome to our
  establishment!"`). Nei dati sempre `0`.
- **Esempio di valori**: `0`

### sendCopy
- **Tipo**: `tinyint(1)`, `DEFAULT 0`
- **Nullabile**: Sì
- **Descrizione**: se attivo, `sendCampaignEmail` invia una **copia** del messaggio all'email del
  **manager** della property — query raw
  `SELECT email FROM users LEFT JOIN user_roles … LEFT JOIN roles … WHERE propertyId = '<propertyId>'
  AND roles.id = 2` (`propertyId` **interpolato**). In produzione: `enablePushNotifications` 1 su 7
righe su 25, `sendCopy` 1 su 6.
- **Esempio di valori**: `0`

### attachments
- **Tipo**: `longtext` `COLLATE utf8mb4_bin`, `CHECK (json_valid(attachments))` — JSON array
- **Nullabile**: Sì
- **Descrizione**: allegati `[{ filename, path }]` passati a nodemailer.
- **Esempio di valori**: `[]`,
  `[{"filename":"27.jpg","path":"https://summerbooking.s3.us-east-2.amazonaws.com/file/27.jpg"}]`

### createdAt / updatedAt
- **Tipo**: `datetime`, `DEFAULT NULL` (model `defaultValue: Sequelize.NOW`, `timestamps: false`)
- **Nullabile**: Sì (a schema)
- **Esempio di valori**: `2026-08-24 07:26:43`

### propertyId
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Relazioni**: [`properties`](properties.md)`.id` — **nessuna FK**.
- **Esempio di valori**: `96`

### contactListId
- **Tipo**: `int(11)`, indice `contactListId`
- **Nullabile**: Sì
- **Relazioni**: [`contact_list`](contact_list.md)`.id` — **FK reale**, `ON DELETE SET NULL ON UPDATE
  CASCADE`. Usato solo per la UI e per `getCampaignById` (mostra i contatti della lista);
  **l'invio NON lo usa** (usa `recipientList`).
- **Esempio di valori**: `258`, `257`, `253`

### timezone
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`, `NOT NULL`, `DEFAULT 'UTC'`
- **Nullabile**: No
- **Descrizione**: fuso IANA del gestore, usato per convertire `scheduledDate`+`scheduledTime` in UTC
  alla create/update.
- **Enumeratore**: no — identificatore IANA (`enum` sarebbe ingestibile: centinaia di valori). Insieme
  chiuso "di libreria", non di dominio.
- **Esempio di valori**: `"Europe/Rome"`, `"Asia/Calcutta"`, `"UTC"`

---

## Osservazioni verificate nel codice (criticità)

- **Route senza auth** (`marketing.routes.js`): create/update/delete/list/duplicate delle campagne
  senza `authJwt.*`; `propertyId` dal body.
- **Destinatari da snapshot `recipientList`**, non dalla lista: `contactListId` + FK sono di fatto
  decorativi per l'invio.
- **Ignora l'SMTP della property**: `sendCampaignEmail` usa `mailOptions.from` globale
  (`noreply@summerbooking.it`) — le mail "della property" partono dal mittente della piattaforma.
- **Nessun esito per destinatario**: un fallimento su un indirizzo viene solo `console.error`; la
  campagna passa comunque a `sent`. `email_logs` registra solo i successi.
- **Doppia conversione fuso in `updateCampaign`**: `moment.tz(\`${scheduledDate} ${scheduledTime}\`,
  timezone)` applicata anche a un `scheduledDate` già datetime → possibile spostamento dell'orario.
- **`name` `NOT NULL` sempre `""`**; **`scheduledTime` ridondante** e non normalizzato.
- **SQL injection** in `getCampaignById` (`id` interpolato) e nella query manager di `sendCopy`
  (`propertyId` interpolato).
- **Cron ogni minuto** che rilegge tutte le campagne `scheduled` (nessun indice su `state`).

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `state` | `ENUM('draft','scheduled','sent','deleted')` | `enum CampaignState { Draft, Scheduled, Sent, Deleted }` — **stesso** vocabolario di [`marketing_automation`](marketing_automation.md)`.state` (che però ha l'ordine `draft,sent,scheduled,deleted`): unificare |

`enablePushNotifications`, `sendCopy` sono booleani; `timezone` è un identificatore IANA (non enum).
