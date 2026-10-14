# Tabella `subscribes`

> Fonti: schema e dati reali (100 righe nel campione, `createdAt` fino a 2026-07), model
> [`back-pre/app/models/subscribe.model.js`](../../back-pre/app/models/subscribe.model.js)
> (`sequelize.define("subscribe", ...)` → tabella pluralizzata `subscribes`, ORM `db.subscribe`),
> controller [`subscribe.controller.js`](../../back-pre/app/controllers/subscribe.controller.js) (letto
> per intero: `createSubscribe`, `getSubscribe`, `deleteSubscribe`), helper
> [`common.helper.js:624` `createSubscribes(email)`](../../back-pre/app/controllers/helpers/common.helper.js)
> invocato da [`auth.controller.js:58`](../../back-pre/app/controllers/auth.controller.js) e
> [`:224`](../../back-pre/app/controllers/auth.controller.js) (signup/registrazione utente) e da
> [`property.controller.js:79`](../../back-pre/app/controllers/property.controller.js) (creazione
> stabilimento). Route in [`app/routes/user.routes.js`](../../back-pre/app/routes/user.routes.js):
> `POST /api/sb/createSubscribe` (1001), `POST /api/sb/getSubscribe` (1191),
> `POST /api/sb/deleteSubscribe` (1199) — tutte con solo `authJwt.optionalAuth`.
> Import inutilizzato in [`user.controller.js:20`](../../back-pre/app/controllers/user.controller.js) e
> [`sysadmin.controller.js:31`](../../back-pre/app/controllers/sysadmin.controller.js).
>
> **Nota di contesto** — tabella **non censita** prima di questa scheda. È la lista **iscrizioni
> newsletter** raccolte dal sito pubblico (solo `email`), più le email registrate automaticamente ad
> ogni signup utente e ad ogni creazione stabilimento.
>
> **Dominio di destinazione proposto**: **C6 — Comunicazione & marketing**, sottoarea `marketing`.
> Concettualmente è una [`contact_list`](contact_list.md) globale (senza `property_id`) di soli
> indirizzi email; nella riscrittura va unificata con il modello contatti/liste del modulo
> `Notification` (una lista "Newsletter" globale con membri = email non necessariamente legate a un
> [`customers`](customers.md)).

---

## Verifica utilizzo — front, back, job (richiesta esplicita)

**Scrittura — usata, da 3 frontend + 3 flussi backend:**

| Sorgente | Punto | Come |
|---|---|---|
| **front-pre** | `components/homeDetails/details1..5.js` (`submitSubscription`) → `data/actions/managerActions.js:258` `_postSubscriptionData` | `Api.post("sb/createSubscribe", { email })` — input "Iscriviti alla newsletter" in home |
| **front-customer-pre** | `components/homeDetails/details1..5.js` → `data/actions/managerActions.js:243` `_postSubscriptionData` | idem |
| **front-app-pre** | `screens/Dashboard/Home.screen.js`, `HomeScreenNew.js`, `HolidaysInfo/{BeachesInfo,HistoricBathHousesInfo,IcecreamInfo,SummerDishesInfo}.js` → `services/Api.js:56` `createSubscribe` | POST `sb/createSubscribe` |
| **front-modern-pre** | — | nessun uso |
| backend | [`auth.controller.js`](../../back-pre/app/controllers/auth.controller.js) `signup` (58), `usersignup` (224); [`property.controller.js`](../../back-pre/app/controllers/property.controller.js) `register` (79) | `Commonhelper.createSubscribes(req.body.email)` → `subscribeModel.create({ email })` (senza `status` → default `1`) |

**Lettura / cancellazione — endpoint esistenti ma senza alcun chiamante:**

- `POST /api/sb/getSubscribe` (`subscribe.controller.getSubscribe`, `findAll({ where:{ status:1 }})`) e
  `POST /api/sb/deleteSubscribe` (`update({ status:0 })`): **nessun frontend** (front-pre,
  front-customer-pre, front-app-pre, front-modern-pre) li chiama. Non esiste alcuna UI che mostri o
  gestisca la lista iscritti.
- Import `db.subscribe` **inutilizzato** in [`user.controller.js:20`](../../back-pre/app/controllers/user.controller.js)
  e [`sysadmin.controller.js:31`](../../back-pre/app/controllers/sysadmin.controller.js).

**Job / cron — nessuno.** `server.js` avvia `startLifeGuardCron`, `updateBookingStatusCron`,
`startSharingDetailsCron`, `campaignEmail`, `automationEmail`, `updateRevenueNotificationCount` — nessuno
tocca `subscribes`. Il cron newsletter [`cronjob/news-letter.js`](../../back-pre/cronjob/news-letter.js)
è **interamente commentato**, disabilitato in `server.js` (`//cronJobnews.sentMailCronJob()`), e comunque
leggerebbe `email_logs`/`email_configration`, **non** `subscribes`.

**Valori in produzione.** Non è stato possibile interrogare direttamente il DB prod/dev remoto in questa
sessione. In `local_sb` (copia **parziale** dei dati di prod — vedi
[`docs/00-struttura.md`](../../docs/00-struttura.md)): **100 righe** (limite della copia parziale), tutte
`status = 1`, `createdAt` **2026-07-23 → 2026-07-27** (dati recenti), un solo indirizzo duplicato nel
campione. → **In produzione la tabella è popolata e attiva** (il percorso di scrittura è vivo da 3
frontend pubblici + ogni signup/registrazione property); semplicemente **nessuno la legge**.

**Sintesi**: `subscribes` è **write-only in produzione** — raccoglie iscrizioni ma non è consumata da
alcuna UI né da alcun job.

---

## Campi

### id
- **Tipo**: `int`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori**: 1, 2, 3, ..

### email
- **Tipo**: `varchar(255)` (`Sequelize.STRING`); indice **non-unico** dichiarato nel model
- **Nullabile**: Sì (il controller valida solo `if (!req.body.email)` → 400)
- **Descrizione**: indirizzo email iscritto alla newsletter. Nessuna normalizzazione (case,
  trimming), nessun controllo di formato, nessun deduplica: `createSubscribes` fa un `create` secco →
  la stessa email compare più volte (signup + creazione stabilimento + iscrizione manuale).
- **Esempio di valori**: `"shanegrant83@yahoo.ie"`, `"budeveifruva-2125@yopmail.com"`
- **Deprecato**: No

### status
- **Tipo**: `int`, default `1`
- **Nullabile**: Sì
- **Descrizione**: `1` = iscrizione attiva, `0` = "cancellata" (soft-delete). `getSubscribe` filtra
  `where: { status: 1 }`; `deleteSubscribe` fa `update({ status: 0 }, { where: { id } })`. Nei dati del
  campione è sempre `1`.
- **Esempio di valori**: `0`, `1`
- **Candidato a enumerazione**: no — è di fatto un soft-delete: nella riscrittura → `deleted_at`
  ([`docs/03-gestione-dati.md` §3.5](../../docs/03-gestione-dati.md)), non un `enum`.
- **Deprecato**: No (ma nei dati sempre `1`)

### createdAt / updatedAt
- **Tipo**: `datetime`, default `Sequelize.NOW`.
- **Nullabile**: Sì
- **Descrizione**: data di iscrizione (`createdAt`) — l'unico dato utile oltre all'email. Il model prova
  a passare `{ freezeTableName: true, timestamps: false }` come **4° argomento** di `sequelize.define`
  → ignorato, i timestamp restano gestiti.
- **Esempio di valori**: `2026-07-23 05:13:50`

---

## Osservazioni verificate nel codice (criticità)

- **Lista senza consumatore (write-only in prod)**: scritta da 3 frontend pubblici + ogni
  signup/registrazione property, ma **nessun lettore** — `getSubscribe`/`deleteSubscribe` non hanno
  chiamanti in nessun frontend e nessun job la usa (cron `news-letter.js` tutto commentato, vedi
  [`email_logs.md`](email_logs.md)). Si riempie ma non serve a nulla allo stato attuale del codice.
- **Nessun `UNIQUE(email)` e nessuna deduplica**: `createSubscribes` è chiamato ad ogni signup e ad
  ogni creazione property → duplicati garantiti; molte righe sono email di `@yopmail.com` (utenti di
  test).
- **Endpoint aperti**: `getSubscribe` (elenco completo degli indirizzi) e `deleteSubscribe` montano
  solo `authJwt.optionalAuth` → l'intera mailing list è leggibile e modificabile senza autenticazione.
- **`createSubscribes` non attende né gestisce errori**: in `common.helper.js` la promise di `create`
  non è awaited nei call-site di `auth.controller.js:58`, e in caso di errore il `catch` ritorna `res`
  (variabile inesistente in quello scope) → potenziale `ReferenceError`.
- **Nessun consenso/GDPR tracciato**: nessun timestamp di opt-in esplicito, nessun token di
  disiscrizione, nessuna sorgente (`source`) — da aggiungere nella riscrittura se la newsletter viene
  riattivata.
- **Nessun `property_id`**: è una lista globale; convive impropriamente con il modello marketing
  per-stabilimento ([`contact_list`](contact_list.md) / [`contact_list_member`](contact_list_member.md)).
