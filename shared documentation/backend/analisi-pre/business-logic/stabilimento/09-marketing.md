# stabilimento — Marketing

> **Voce di menù**: nona icona della sidebar (`_goToNewsLetter` → etichetta `marketing`; rotte
> `/marketing` → componente `Marketing`, `/newsletter` → componente `NewsLetter`). Comprende:
> **Newsletter** (invio massivo estemporaneo), **Liste di contatti** (segmenti), **Campagne email**
> (programmate), **Automazioni** (email a trigger/ricorrenti), **Eventi** (locandine con notifiche
> pianificate), **Iscrizioni** (opt‑in newsletter).
>
> **Ruoli**: visibile ad `admin`, e a chi ha licenza non‑`basic` (escluso `cashoperator`, `affiliate`,
> `waiter`, `lifeguard`, `advancedlifeguard`). Il ruolo `marketing` esiste apposta per quest'area.
>
> **Metodo**: `newsletter.controller.js`, `marketingContactList.controller.js`,
> `marketingEmailCampaign.controller.js`, `marketingAutomation.controller.js`,
> `marketingEvent.controller.js` + `subscribe.controller.js` + `cronjob/news-letter.js` +
> `controllers/helpers/cron.helper.js` (`campaignEmail`, `automationEmail`). Schede DB (molto
> dettagliate): [`marketing_email_campaign.md`](../../db/marketing_email_campaign.md),
> [`marketing_automation.md`](../../db/marketing_automation.md),
> [`marketing_event.md`](../../db/marketing_event.md),
> [`marketing_event_notification.md`](../../db/marketing_event_notification.md),
> [`contact_list.md`](../../db/contact_list.md),
> [`contact_list_member.md`](../../db/contact_list_member.md),
> [`custom_templates.md`](../../db/custom_templates.md),
> [`email_logs.md`](../../db/email_logs.md), [`subscribes.md`](../../db/subscribes.md).

---

## 1. Newsletter — invio massivo estemporaneo

**Azione** — `POST sendmessage` (`newsletter.controller.js`): l'operatore scrive un messaggio, allega
file, esclude eventuali contatti, invia a **tutti i clienti** dello stabilimento.

**Logiche**:

- Mittente: se la property ha `bathhouse_email_configrations` → `fromEmail` = quello configurato,
  altrimenti l'email passata nel body. Il "from" mostrato è `"<nome bagno> <fromEmail>"`.
- Destinatari: tutti i `customers` con `propertyId` = stabilimento, `isDelete = false`, id **non** in
  `removeContacts`.
- **Iterazione** su ogni cliente (e su ogni file allegato): compone l'HTML col template
  `newsletterEmail` e **inserisce una riga in `email_logs`** con `status = 0`, `cronStatus = 0`,
  `emailType = "Newsletter"`, `propertyId`.
- **Non invia direttamente**: `sendmessage` **accoda** soltanto. L'invio è del cron.

**Evento — cron `news-letter.js` (ogni 1 minuto)**:

- Legge tutte le righe `email_logs` con `status = 0`.
- Per ciascuna: se la property ha una config SMTP valida (`email_configration.status = true`) usa
  quella (host/porta/credenziali), altrimenti il trasportatore di default; invia via **SMTP**; al
  successo imposta `status = 1`.

**Calcoli**: nessuno. **Eventi**: accodamento in `email_logs`; invio SMTP differito (cron 1 min).

---

## 2. Liste di contatti (segmenti)

**Azioni** (`marketingContactList.controller.js`): `create-contact-list`,
`create-default-contact-list`, `get-contact-lists`, `customer-list`, `update-contact-list`,
`contact-list/duplicate` (+ eliminazione).

**Logiche**:

- Una lista (`contact_list`) è un **segmento di clientela**; i membri (`contact_list_member`)
  contengono **nome, email, telefono** dei clienti (snapshot).
- `create-default-contact-list` crea una lista predefinita (es. "tutti i clienti").
- `duplicate` copia lista + membri.
- **Nessuno scoping reale**: il controller può leggere/modificare liste di qualunque property (vedi
  [`contact_list.md`](../../db/contact_list.md)).

**Calcoli**: nessuno. **Eventi**: nessuno.

---

## 3. Campagne email programmate

**Azioni** (`marketingEmailCampaign.controller.js`): `campaigns` (create/list/get/update/delete),
`campaigns/duplicate`.

**Logiche (`createCampaign`)**:

- `state = "scheduled"` se è indicata `scheduledDate`, altrimenti `"draft"`.
- `scheduledDate` + `scheduledTime` vengono **convertiti in UTC** (`moment.tz(...)` sul fuso
  configurato).
- `recipientList` è salvato come **snapshot** (JSON) di indirizzi; `attachments` come JSON.
- `updateCampaign`: `draft → scheduled` se arriva una `scheduledDate`; ri‑converte la data.
- `deleteCampaign`: `state = "deleted"` (soft).

**Evento — cron `campaignEmail()` (ogni 1 minuto, `cron.helper.js`)**:

- Seleziona le campagne con `state = "scheduled"` e `scheduledDate ≤ adesso (UTC)`.
- Per ogni indirizzo di `recipientList`: renderizza il contenuto (template/`custom_templates`) e
  **invia via SMTP dal mittente fisso `noreply@summerbooking.it`** (NON l'SMTP della property);
  registra su `email_logs`; aggiorna lo stato della campagna (inviata).

**Calcoli**: conversione fuso/UTC della data di programmazione. **Eventi**: invio email di massa
differito (cron 1 min) dal mittente di piattaforma; `email_logs`.

---

## 4. Automazioni (email a trigger / ricorrenti)

**Azioni** (`marketingAutomation.controller.js`): `automations` (create/list/get/update/delete),
`automations/duplicate`. Alla creazione di una lista/automazione vengono precompilate due automazioni
standard **"Welcome"** e **"Goodbye"** (`scheduleType` `before`/`after`, `scheduleValue` in giorni),
con testi EN e segnaposto `@surnamename`, `@dates`, `@email`, `@telephonenumber`.

**Logiche**:

- Tipi: `welcome` / `goodbye` (benvenuto/saluto rispetto alle date di soggiorno) e `custom` con
  trigger `ofbooking` / `beforebooking` / `afterbooking` / `ofbookingcancellation` (vedi
  [`marketing_automation.md`](../../db/marketing_automation.md)).
- `scheduleType` = `before` / `after` + `scheduleValue` (giorni) rispetto all'evento di riferimento.

**Evento — cron `automationEmail()` (ogni 5 minuti, `cron.helper.js`, con guardia `isRunning`)**:

- Seleziona `Automation` con `state = "scheduled"`; **salta** quelle chiamate `welcome`/`goodbye`
  (rimaste non implementate a runtime).
- Per le altre (`processAutomation`): per ogni email dei `recipients` esegue una query
  `SELECT * FROM bookings WHERE consumer_email = '<member>' AND isDeleted = 0`; in base a
  `scheduleType`/`scheduleValue` (`handleCustomSchedule`) decide se è il momento di inviare, quindi
  `sendAutomationEmail` invia col **trasportatore globale**, logga `email_logs`
  (`emailType = "Automation Email"`), e opzionalmente invia una **push**.

**Calcoli**: calcolo della data di invio = data evento ± `scheduleValue` giorni. **Eventi**: email
(trasportatore globale) + `email_logs`; push opzionale.

---

## 5. Eventi (locandine con notifiche pianificate) — **incompleto**

**Azioni** (`marketingEvent.controller.js`): `event` (create), `update-event`, `events/:id`,
`events/duplicate` (+ delete).

**Logiche (`createEvent`)**:

- Se l'evento è **ricorrente**, viene **espanso in N occorrenze**, ognuna con
  `startDate = endDate = <data occorrenza>`.
- Se il body contiene `notifications[]`, per ogni occorrenza vengono create righe
  `marketing_event_notification` (con `contactListId`, testo, data/ora pianificata, `status = pending`).

**Evento**: **NESSUNO**. In tutto `back-pre` (controller, service, cron, `server.js`) **non esiste
alcun mittente** che legga `marketing_event_notification` per inviare la notifica pianificata: il
campo `status` resta sempre `pending`. La feature è **incompleta/deprecata** (vedi
[`marketing_event_notification.md`](../../db/marketing_event_notification.md)). Da chiarire col
committente se va completata o rimossa.

**Calcoli**: espansione ricorrenza in occorrenze.

---

## 6. Iscrizioni newsletter (opt‑in)

**Azioni** (`subscribe.controller.js`): `createSubscribe`, `getSubscribe`, `deleteSubscribe`
(`subscribes`).

**Logiche**: la tabella `subscribes` raccoglie gli indirizzi email opt‑in; viene **popolata
automaticamente** (helper `createSubscribes`) alla registrazione di uno stabilimento, alla creazione
di un utente e in altri punti di primo contatto — l'iscrizione non è quindi un'azione esplicita
dell'utente finale nella maggior parte dei casi.

**Calcoli**: nessuno. **Eventi**: nessuno (sola scrittura in `subscribes`).

---

## 7. Riepilogo "calcoli / logiche / eventi" della voce Marketing

- **Calcoli**: conversione data programmazione in UTC (campagne); data di invio = evento ±
  `scheduleValue` giorni (automazioni); espansione ricorrenza eventi in occorrenze.
- **Logiche**: stato `draft`/`scheduled`/`deleted`; snapshot `recipientList`/membri lista; trigger
  automazioni (`before`/`after`/`of...`) valutati sui `bookings` del contatto; salto di
  `welcome`/`goodbye` nel cron.
- **Eventi**:
  - **Newsletter** → accoda in `email_logs` (`status 0`); cron `news-letter.js` **ogni 1 min** invia
    via SMTP della property o default.
  - **Campagne** → cron `campaignEmail()` **ogni 1 min** invia dai `noreply@summerbooking.it`
    (mittente di piattaforma) + `email_logs`.
  - **Automazioni** → cron `automationEmail()` **ogni 5 min** invia col trasportatore globale +
    `email_logs` (+ push opzionale).
  - **Eventi (notifiche pianificate)** → **nessun invio** (feature incompleta).
  - Nessuna chiamata WhatsApp/API esterna in quest'area (a parte l'SMTP).
