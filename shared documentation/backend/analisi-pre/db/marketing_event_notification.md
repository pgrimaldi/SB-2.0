# Tabella `marketing_event_notification`

> Fonti: schema; **0 righe sia nel DB dev sia nel dump di produzione `sb_prod_dump_2026_09_08`**
> (esportata solo come schema). L'`AUTO_INCREMENT` alto indica che in passato molte righe sono state
> create e distrutte, ma allo stato attuale la tabella è vuota anche in produzione. Model
> [`back-pre/app/models/marketingEventNotification.js`](../../../../../../back-pre/app/models/marketingEventNotification.js)
> (`sequelize.define("marketing_event_notification", …, { freezeTableName: true, timestamps: false })`;
> `createdAt`/`updatedAt` con `defaultValue: Sequelize.NOW`), registrato come
> `db.marketing_event_notification` in [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga
> 192). **Nessuna FK, nessun indice** oltre la PK; nessuna associazione ORM.
>
> **Scrittori** — solo
> [`Service/marketingEventService.js`](../../../../../../back-pre/app/Service/marketingEventService.js):
> - `createEvent` / `updateEvent` / `duplicateEvent` —
>   `EventNotification.create({ eventId, ...notification })` per ogni elemento di `notifications[]`
>   passato nel body dell'endpoint evento.
> - `updateEvent` / `deleteEvent` — `EventNotification.destroy({ where: { eventId } })` prima di
>   ricreare / alla cancellazione dell'evento.
>
> **Lettori** — solo in JOIN raw SQL dentro `getEventById` e `duplicateEvent`
> (`LEFT JOIN marketing_event_notification men ON me.id = men.eventId`, aggregate `JSON_ARRAYAGG`).
>
> **Nessun mittente**: in tutto `back-pre` non esiste un cron, un service o un controller che legga
> queste righe per **inviare** la notifica pianificata. Il campo `status` (`pending`/`sent`/`failed`)
> non viene mai fatto transitare. La feature "avvisami N giorni prima dell'evento" è **incompleta**.
>
> **Frontend**: form notifiche dentro **Marketing → Eventi**
> ([`components/Marketing/Component/Events/Events.js`](../../../../../../front-pre/src/components/Marketing/Component/Events/Events.js));
> l'array `notifications` viaggia dentro il payload di `sb/event` / `sb/update-event`. Nessuna
> chiamata dedicata.

---

## Verdetto: feature NON in funzione (deprecata di fatto)

Verificato sul codice (`grep` su tutto `back-pre/app` + `back-pre/cronjob` + `server.js`):

- **Scrittori**: **solo** [`Service/marketingEventService.js`](../../../../../../back-pre/app/Service/marketingEventService.js)
  (`EventNotification.create` in create/update/duplicate; `EventNotification.destroy({ where: {
  eventId } })` in update/delete).
- **Lettori**: **solo** le due `LEFT JOIN marketing_event_notification … JSON_ARRAYAGG` di
  `getEventById` / `duplicateEvent` — cioè **di sola visualizzazione** nel form evento.
- **Mittente / scheduler**: **nessuno**. Nessuno dei job di
  [`cronjob/cron-job.js`](../../../../../../back-pre/cronjob/cron-job.js) (`startLifeGuardCron`,
  `startSharingDetailsCron`, i 5 backfill `updateBookingNumberIn…`, `updateBookingStatusCron`,
  `updateCurrentSeason`, `campaignEmail`, `automationEmail`, `updateRevenueNotificationCount`,
  `stripeReconciliationCron`) tocca questa tabella; nessun controller/service la legge per
  inviare. `status` (`pending`/`sent`/`failed`) **non transita mai**.
- **Dati**: `0 righe`; `AUTO_INCREMENT = 3174` = solo churn `destroy` + `create` di `updateEvent`.

→ **Da NON migrare**, salvo che il team decida di **completare** la feature (serve uno scheduler nuovo,
vedi sotto). Va dismessa insieme a [`marketing_event`](marketing_event.md)`.notificationsEnabled` /
`.contactListId`.

---

## Nota di contesto

Doveva contenere le **notifiche pianificate di un evento marketing** ([`marketing_event`](marketing_event.md)):
"1 giorno prima", "3 giorni prima", "1 settimana prima" o data/ora custom. Le righe vengono create e
distrutte insieme all'evento, ma **non c'è nulla che le trasformi in un invio** (vedi verdetto sopra).

**Dominio nella riscrittura**: **C6 — Comunicazione / Marketing**. Se la feature si mantiene, va
completata: uno scheduler che legge le notifiche `pending` con `notificationDate/Time` raggiunta,
invia (email/app/WhatsApp alla `marketing_event.contactListId`) e porta `status` a `sent`/`failed`.
Altrimenti va **dismessa** insieme a `marketing_event.notificationsEnabled` / `contactListId`.

---

## Campi

### id
- **Tipo**: `int(11)`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### eventId
- **Tipo**: `int(11)`, `NOT NULL`
- **Nullabile**: No
- **Relazioni**: [`marketing_event`](marketing_event.md)`.id` — **nessuna FK** (nessun
  `ON DELETE CASCADE`; la pulizia è fatta a mano da `updateEvent`/`deleteEvent`).
- **Esempio di valori**: *(nessun dato — tabella vuota)*; teorico: `4058`, `4063`

### notificationType
- **Tipo**: `enum('1_day_before','3_days_before','1_week_before','custom')` `COLLATE utf8mb4_unicode_ci`,
  `NOT NULL` — **già ENUM nativo MySQL**
- **Nullabile**: No
- **Descrizione**: anticipo rispetto all'evento; con `'custom'` valgono `notificationDate` +
  `notificationTime`.
- **Enumeratore**: **sì** — `enum EventReminderOffset { OneDayBefore, ThreeDaysBefore, OneWeekBefore,
  Custom }`.
- **Esempio di valori**: `"1_day_before"`, `"custom"` *(teorici)*

### notificationDate
- **Tipo**: `datetime`
- **Nullabile**: Sì
- **Descrizione**: data della notifica quando `notificationType = 'custom'`.
- **Esempio di valori**: *(nessun dato)*; teorico `2025-12-20 00:00:00`

### notificationTime
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: orario `"HH:mm"` come stringa.
- **Esempio di valori**: *(nessun dato)*; teorico `"09:00"`

### status
- **Tipo**: `enum('pending','sent','failed')` `COLLATE utf8mb4_unicode_ci`, `DEFAULT 'pending'` —
  **già ENUM**
- **Nullabile**: Sì
- **Descrizione**: stato di invio. **Non transita mai** (nessun mittente): resterebbe `pending`.
- **Enumeratore**: **sì** — `enum ReminderStatus { Pending, Sent, Failed }`.
- **Esempio di valori**: `"pending"` (default; nessun dato reale)
- **Deprecato**: **Sì** — come tutta la tabella: nessun processo la fa evolvere.

### createdAt / updatedAt
- **Tipo**: `datetime`, `DEFAULT NULL` (model `defaultValue: Sequelize.NOW`, `timestamps: false`)
- **Nullabile**: Sì (a schema)
- **Esempio di valori**: *(nessun dato)*

---

## Osservazioni verificate nel codice (criticità)

- **Feature incompleta / tabella morta**: righe create e distrutte con l'evento, **mai inviate**;
  `0 righe`, `status` mai diverso da `pending`.
- **Nessuna FK / cascade** verso [`marketing_event`](marketing_event.md): la coerenza dipende
  interamente dalla `destroy({ where: { eventId } })` in `updateEvent`/`deleteEvent` (che però NON
  viene eseguita da `updateEvent` se il client manda `notifications: []`).
- **`updateEvent` va in errore** se `notifications` è `undefined` (`if (notifications.length > 0)`).
- Da **completare con uno scheduler** o **dismettere** con la feature notifiche evento.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `notificationType` | `ENUM('1_day_before','3_days_before','1_week_before','custom')` | `enum EventReminderOffset { OneDayBefore, ThreeDaysBefore, OneWeekBefore, Custom }` |
| `status` | `ENUM('pending','sent','failed')` | `enum ReminderStatus { Pending, Sent, Failed }` — **stesso** vocabolario di `marketing_event_notification`/invii pianificati |
