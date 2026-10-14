# Tabella `marketing_automation`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **221 righe**, query
> completa; `createdAt` dal 2025-12-04 → feature introdotta a dicembre 2025). In produzione: `name`
> `Goodbye` (102), `Welcome` (93), `''` (26) — sono le automazioni di default create insieme alla
> lista `Marketing` per property (~100 stabilimenti); `state` `scheduled` (186), `deleted` (13),
> `sent` (12), `draft` (10). Model
> [`back-pre/app/models/marketingAuomation.model.js`](../../../../../../back-pre/app/models/marketingAuomation.model.js)
> (nome file con refuso "Auomation"; `sequelize.define("marketing_automation", …, { freezeTableName:
> true, timestamps: false })`; `createdAt`/`updatedAt` con `defaultValue: Sequelize.NOW`), registrato
> come `db.marketing_automation` in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga 200).
> **FK reale** `contactListId` → [`contact_list`](contact_list.md)`(id)` `ON DELETE SET NULL ON UPDATE
> CASCADE` (indice `contactListId`); `belongsTo`/`hasMany` con `contact_list` in
> [`models/index.js:389-393`](../../../../../../back-pre/app/models/index.js).
>
> **Scrittori** —
> [`controllers/marketingAutomation.controller.js`](../../../../../../back-pre/app/controllers/marketingAutomation.controller.js)
> (route in [`routes/marketing.routes.js`](../../../../../../back-pre/app/routes/marketing.routes.js), **senza
> auth**):
> - `createAutomation` (`POST /api/sb/automations`), `updateAutomation` (`PUT /api/sb/automations`),
>   `deleteAutomation` (`DELETE /api/sb/automations/:id` → `state → "deleted"`),
>   `duplicateAutomation` (`POST /api/sb/automations/duplicate` → `state → "draft"`).
> - [`controllers/marketingContactList.controller.js`](../../../../../../back-pre/app/controllers/marketingContactList.controller.js)
>   `createDefaultContactListAndAutomation` — `Automation.bulkCreate([{ name: "Welcome", state:
>   "scheduled", scheduleType: "before", scheduleValue: [{ days: 1 }], … }, { name: "Goodbye", state:
>   "scheduled", scheduleType: "after", scheduleValue: [{ days: 1 }], … }])` con `textFields`
>   precompilati (testi EN con placeholder `@surnamename`, `@dates`, `@email`, `@telephonenumber`,
>   `@establishmentname`).
> - [`controllers/helpers/cron.helper.js`](../../../../../../back-pre/app/controllers/helpers/cron.helper.js)
>   `automationEmail()` — cron **ogni 5 minuti** (`server.js` → `cronJob.automationEmail()`, con guardia
>   `isRunning`): `Automation.findAll({ where: { state: "scheduled" } })`, **salta** quelle con
>   `name.toLowerCase() === "welcome" | "goodbye"`, per le altre `processAutomation` → per ogni email
>   di `recipients` → raw SQL `SELECT * FROM bookings WHERE consumer_email = '<member>' AND isDeleted =
>   0 AND STATUS = 1` → per ogni prenotazione `handleBeforeSchedule` / `handleAfterSchedule` /
>   `handleCustomSchedule` (per `scheduleType`) → `sendAutomationEmail` (invia con transporter
>   **globale**, logga su [`email_logs`](email_logs.md) `emailType: "Automation Email"`, e se
>   `enableAppNotifications && user.deviceTokens` invia anche una **push FCM**). Lo `state` **non**
>   viene portato a `sent` (le due `update({ state: "sent" })` nel codice sono commentate).
>
> **Lettori**:
> - `getAutomations` (`GET /api/sb/automations`) — Sequelize `findAll` + `include ContactList`
>   (filtri `state`, `name` su `subject`, `propertyId`).
> - `getAutomationById` (`GET /api/sb/automations/:id`) — raw SQL join `contact_list` +
>   `contact_list_member` + `customers` (`id` **interpolato**).
>
> **Frontend** (`front-pre`, copia `front-customer-pre`): **Marketing → Automazioni**
> ([`components/Marketing/Component/Automations/`](../../../../../../front-pre/src/components/Marketing/Component/Automations/)),
> via [`data/actions/marketingActions.js`](../../../../../../front-pre/src/data/actions/marketingActions.js)
> (`sb/automations` GET/POST/PUT, `sb/automations/:id`, `sb/automations/duplicate`).

---

## Nota di contesto

**Messaggi automatici ricorrenti / a trigger**: email (+ push opzionale) inviata ai contatti di una
lista in relazione alle loro prenotazioni — `before` (N giorni prima dell'arrivo), `after` (N giorni
dopo la partenza), `custom` (trigger `ofbooking` / `beforebooking` / `afterbooking` /
`pendingpayments`). Le automazioni "Welcome" e "Goodbye" sono create in automatico insieme alla lista
di default (`createDefaultContactListAndAutomation`) ma il cron `automationEmail` **le esclude
esplicitamente per nome** → di fatto **non vengono mai inviate** da questo percorso.

Nei dati di produzione: **221 righe** (id 14…291), `name` `Goodbye`/`Welcome`/`''`, `state`
`scheduled` (186) / `deleted` (13) / `sent` (12) / `draft` (10), `scheduleType` `after` (105) /
`before` (98) / `custom` (18), `scheduleValue`
`[{"type":"pendingpayments","time":"16:11"}]` / `[{"type":"pendingpayments","time":"16:01"}]`,
`recipients` di solito `[]` (189/221) o array di email, `contactListId` valorizzato (range 4…239),
`timezone` `UTC` (201) / `Europe/Rome` (10) / `Asia/Calcutta` (10), `enableAppNotifications` 1 su 12
righe. Con
`state = "sent"` (e le `update({state:"sent"})` commentate → resterebbero comunque `scheduled` se
inviate dal cron) nessuna delle due verrebbe rielaborata.

**Dominio nella riscrittura**: **C6 — Comunicazione / Marketing**. Va normalizzato: `name`
Welcome/Goodbye/custom è un enum di comportamento (oggi il cron ci fa `switch` per stringa),
`scheduleType` + `scheduleValue[].type` un enum di trigger, i destinatari risolti dalla lista (non da
`recipients` snapshot), esito per destinatario nell'outbox, e — se si mantiene — un vero motore a
coda con backoff (oggi è una query SQL per destinatario per automazione, ogni 5 minuti).

---

## Campi

### id
- **Tipo**: `int(11)`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `14` … `291` per 221 righe.

### name
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`, `NOT NULL`
- **Nullabile**: No (a schema) — `""` nei record creati da UI; `"Welcome"` / `"Goodbye"` per quelle
  auto-generate
- **Descrizione**: **il cron ci fa branching**: `name.toLowerCase()` `=== "welcome"` /
  `=== "goodbye"` seleziona `handleWelcomeAutomation` / `handleGoodbyeAutomation` — ma `automationEmail`
  **scarta** queste due prima di arrivarci, quindi i rami sono di fatto morti. Per gli altri nomi
  conta lo `scheduleType`.
- **Enumeratore**: **sì (di comportamento)** — `enum AutomationKind { Welcome, Goodbye, Custom }` al
  posto del confronto per stringa.
- **Esempio di valori**: `""`, `"Welcome"`, `"Goodbye"`

### state
- **Tipo**: `enum('draft','sent','scheduled','deleted')` `COLLATE utf8mb4_unicode_ci`, `NOT NULL` —
  **già ENUM nativo MySQL** (ordine diverso da [`marketing_email_campaign`](marketing_email_campaign.md)`.state`)
- **Nullabile**: No
- **Descrizione**: `draft` → `scheduled` (attiva, elaborata dal cron) → `sent` — ma il passaggio a
  `sent` è **commentato** nel cron, quindi un'automazione attiva resta `scheduled`. `deleted` =
  soft-delete.
- **Enumeratore**: **sì** — `enum AutomationState { Draft, Scheduled, Sent, Deleted }` — unificare
  con `CampaignState`.
- **Esempio di valori**: `"sent"`

### enableAppNotifications
- **Tipo**: `tinyint(1)`, `DEFAULT 0`
- **Nullabile**: Sì
- **Descrizione**: se attivo e il destinatario ([`users`](users.md) `userStatus: "active"`) ha
  `deviceTokens`, `sendAutomationEmail` invia **anche** una push FCM (`sendPushNotification`) con
  `title = subject`, `body = message`.
- **Esempio di valori**: `0`

### subject
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: oggetto dell'email (e titolo della push). Usato come chiave di ricerca in
  `getAutomations` (`Op.like` su `subject`, non su `name`).
- **Esempio di valori**: `"pending payment"`, `"dev latest"`, `" Benvenuti presso <bathouseName>"`

### message
- **Tipo**: `text` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: testo breve; nelle automazioni auto-generate è `""` (il contenuto vero è in
  `textFields`). Nella push è il `body`.
- **Esempio di valori**: `"payment pending"`, `"pending payment"`, `""`

### recipients
- **Tipo**: `longtext` `COLLATE utf8mb4_bin`, `CHECK (json_valid(recipients))` — JSON array di email
- **Nullabile**: Sì
- **Descrizione**: **destinatari effettivi** (snapshot). `processAutomation` itera su questo array;
  per ognuno cerca le prenotazioni per `consumer_email`. Le auto-generate lo creano come `[]`.
- **Esempio di valori**: `["devnew2026@yopmail.com"]`, `["test17@yopmail.com","johndev@yopmail.com"]`

### textFields
- **Tipo**: `text` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: corpo del messaggio con placeholder `@…` (`@surnamename`, `@dates`, `@email`,
  `@telephonenumber`, `@establishmentname`), renderizzato in `sendAutomationEmail`. Stessa sintassi di
  [`custom_templates`](custom_templates.md)`.text`.
- **Esempio di valori**: `"\nEstablishment name: to display the name"`,
  `"Dear @surnamename\n\nWe are happy to welcome you…"` *(troncato)*

### scheduleType
- **Tipo**: `enum('before','after','custom')` `COLLATE utf8mb4_unicode_ci`, `NOT NULL` — **già ENUM**
- **Nullabile**: No
- **Descrizione**: strategia di pianificazione. `before`/`after` usano `scheduleValue[0].days`
  rispetto a `booking_start_date` / `booking_end_date`; `custom` itera su `scheduleValue` per `type`
  (vedi sotto).
- **Enumeratore**: **sì** — `enum AutomationScheduleType { Before, After, Custom }`.
- **Esempio di valori**: `"custom"`, `"before"`, `"after"`

### scheduleValue
- **Tipo**: `longtext` `COLLATE utf8mb4_bin`, `CHECK (json_valid(scheduleValue))` — JSON array
- **Nullabile**: Sì
- **Descrizione**: parametri di pianificazione.
  - con `scheduleType` `before`/`after` (e Welcome/Goodbye): `[{ "days": <n> }]` (si usa
    `scheduleValue[0].days`).
  - con `scheduleType = custom`: `[{ "type": <trigger>, "time": "HH:mm", "days"?: <n> }, …]` dove
    `<trigger>` ∈ `ofbooking` | `beforebooking` | `afterbooking` | `pendingpayments`
    (`pendingpayments` invia solo se `JSON.parse(booking.booking_payments).toPay > 0`).
- **Enumeratore** (sotto-campo `type`): **sì** — `enum AutomationTrigger { OfBooking, BeforeBooking,
  AfterBooking, PendingPayments }`.
- **Esempio di valori**: `[{"days":1}]`,
  `[{"type":"pendingpayments","time":"16:11"}]`, `[{"type":"beforebooking","time":"09:00","days":3}]`

### attachments
- **Tipo**: `longtext` `COLLATE utf8mb4_bin`, `CHECK (json_valid(attachments))` — JSON array
- **Nullabile**: Sì
- **Esempio di valori**: `[]`

### propertyId
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Relazioni**: [`properties`](properties.md)`.id` — **nessuna FK**.
- **Esempio di valori**: `96`

### createdAt / updatedAt
- **Tipo**: `datetime`, `DEFAULT NULL` (model `defaultValue: Sequelize.NOW`, `timestamps: false`)
- **Nullabile**: Sì (a schema)
- **Esempio di valori**: `2026-05-07 10:36:53`

### contactListId
- **Tipo**: `int(11)`, indice `contactListId`
- **Nullabile**: Sì
- **Relazioni**: [`contact_list`](contact_list.md)`.id` — **FK reale**, `ON DELETE SET NULL`. Usato
  per la UI / `getAutomationById`; **l'invio usa `recipients`**, non la lista. `NULL` in 1 riga su 2.
- **Esempio di valori**: `258`, `null`

### timezone
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`, `NOT NULL`, `DEFAULT 'UTC'`
- **Nullabile**: No
- **Descrizione**: fuso IANA usato in `handleCustomSchedule` per convertire `targetDate + time` in UTC
  e confrontarlo con `now` (finestra di 300 s).
- **Esempio di valori**: `"Asia/Calcutta"`, `"UTC"`, `"Europe/Rome"`

---

## Osservazioni verificate nel codice (criticità)

- **Route senza auth** (`marketing.routes.js`): create/update/delete/list/duplicate senza `authJwt.*`;
  `propertyId` dal body.
- **Welcome/Goodbye morte**: `automationEmail` le esclude per nome; `createDefaultContactListAndAutomation`
  le crea sempre → 2 righe inerti per ogni property che attiva la lista di default.
- **`state` mai portato a `sent`** (`automation.update({ state: "sent" })` commentata): un'automazione
  `scheduled` viene **rielaborata a ogni tick** finché resta `scheduled`; l'anti-doppione è solo
  `emailSent` locale al singolo tick + la finestra `diffSeconds < 300`.
- **N query per destinatario, ogni 5 minuti**: `processMemberAutomation` fa una `SELECT * FROM
  bookings WHERE consumer_email = '<member>'` (**SQL injection**, `member` interpolato) per ogni email
  di ogni automazione attiva.
- **Ignora l'SMTP della property** (`sendAutomationEmail` usa il transporter globale).
- **Nessun esito per destinatario**; `email_logs` registra solo i successi.
- **`recipients` snapshot** invece della lista `contactListId`; **`name` `NOT NULL` = `""`** dalle UI.
- **SQL injection** anche in `getAutomationById` (`id` interpolato).
- Doppio significato di `scheduleValue` (oggetto `{days}` vs array di `{type,time}`) nello stesso
  campo JSON.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `state` | `ENUM('draft','sent','scheduled','deleted')` | `enum AutomationState { Draft, Scheduled, Sent, Deleted }` — unificare con [`marketing_email_campaign`](marketing_email_campaign.md)`.state` |
| `scheduleType` | `ENUM('before','after','custom')` | `enum AutomationScheduleType { Before, After, Custom }` |
| `name` (branch del cron) | `varchar` (`"welcome"`/`"goodbye"`/altro) | `enum AutomationKind { Welcome, Goodbye, Custom }` |
| `scheduleValue[].type` (JSON) | `ofbooking` / `beforebooking` / `afterbooking` / `pendingpayments` | `enum AutomationTrigger { OfBooking, BeforeBooking, AfterBooking, PendingPayments }` |

`enableAppNotifications` è un booleano; `timezone` è un identificatore IANA (non enum).
