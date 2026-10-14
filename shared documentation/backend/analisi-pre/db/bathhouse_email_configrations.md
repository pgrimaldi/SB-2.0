# Tabella `bathhouse_email_configrations`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`, **22 righe**, query
> completa; il DB dev `summer_booking_dev` ne aveva 1 sola), model
> [`back-pre/app/models/email-config.model.js`](../../../../../../back-pre/app/models/email-config.model.js)
> (`sequelize.define("bathhouse_email_configration", …)` senza `freezeTableName` → nome **pluralizzato**
> `bathhouse_email_configrations`; senza `timestamps: false` → `createdAt`/`updatedAt` gestiti da
> Sequelize, a schema `datetime NULL`), registrato come `db.email_configration` in
> [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga 178). **Nessuna associazione ORM,
> nessuna FK, nessun indice** oltre la PK.
>
> **Scrittori** — [`controllers/emailConfig.controller.js`](../../../../../../back-pre/app/controllers/emailConfig.controller.js):
> - `addEmailConfig` (`POST /api/sb/add-email-config`, `authJwt.optionalAuth`) — **upsert per
>   `propertyId`**: se esiste una riga con quel `propertyId` la aggiorna, altrimenti la crea. Tutti i
>   campi (incluso `smtpPassword` e `status`) dal body.
> - `deleteEmailConfig` (`POST /api/sb/delete-email-config`, `authJwt.optionalAuth`) — soft-delete:
>   `update({ status: false }, { where: { propertyId, status: true } })`.
>
> **Lettori** (tutti filtrano `status: true`, cioè `1`):
> - `emailConfig.getEmailConfig` (`POST /api/sb/get-email-config`) — restituisce la config al pannello,
>   `smtpPassword` **in chiaro** compreso; fa `JSON.parse(config.testEmailRecipients)`.
> - `emailConfig.sendTestEmail` (`POST /api/sb/send-test-email`) — costruisce un transporter nodemailer
>   e invia una mail di prova.
> - [`booking.controller.js`](../../../../../../back-pre/app/controllers/booking.controller.js) `sendReservationSummery`
>   (`POST /api/sb/sendReservationSummery`) — se esiste config `status: true`, invia il riepilogo
>   prenotazione al cliente con l'SMTP della property; altrimenti usa il transporter globale.
> - [`newsletter.controller.js`](../../../../../../back-pre/app/controllers/newsletter.controller.js) `sendmessage`
>   — legge **solo** `fromEmail` (senza filtro `status`).
> - [`cronjob/news-letter.js`](../../../../../../back-pre/cronjob/news-letter.js) `sentMailCronJob` — sceglie
>   l'SMTP per inviare le righe [`email_logs`](email_logs.md) `status: 0`. **Cron disattivato**
>   (commentato in `server.js`).
>
> **Non** usata da `cron.helper.js` per le campagne / automazioni marketing: `sendCampaignEmail` e
> `sendAutomationEmail` usano il `mailOptions.from` globale (`noreply@summerbooking.it`), non l'SMTP
> della property. Vedi [`marketing_email_campaign`](marketing_email_campaign.md) /
> [`marketing_automation`](marketing_automation.md).
>
> **Frontend**:
> [`front-pre/src/data/actions/managerActions.js`](../../../../../../front-pre/src/data/actions/managerActions.js)
> (`getEmailConfigAction` → `sb/get-email-config`, `addEmailConfigAction` → `sb/add-email-config`);
> pannello impostazioni email del gestore (`components/…/setting`), copia in `front-customer-pre`.
>
> **Tabella sorella legacy `email_configrations`** (senza model, **0 righe**, `AUTO_INCREMENT` di
> default): stesso set di colonne ma `testEmailRecipients` è `varchar(255)` invece di `longtext`/JSON e
> mancano `status` e il record. È il **predecessore** di questa tabella, non più referenziato da alcun
> model (`db.email_configration` punta a `bathhouse_email_configrations`). Da **non migrare**.

---

## Nota di contesto

Configurazione **SMTP in uscita per stabilimento**: consente a una property di spedire le email
transazionali (conferme, riepiloghi, newsletter) dal proprio dominio/mittente invece che da
`noreply@summerbooking.it`. Modello "0 o 1 riga per `propertyId`".

Stato reale in **produzione** (`sb_prod_dump_2026_09_08`, 22 righe, 1 per `propertyId`):

- **`status`**: `1` (attiva) per **10 righe**, `0` per 12. Quindi ~10 stabilimenti hanno oggi una
  config SMTP realmente attiva — la vecchia affermazione "*nessuno* stabilimento ha una config SMTP
  attiva" (basata sull'unica riga del DB dev) **non è più valida**.
- **`smtpServer`**: `authsmtp.securemail.pro` (13), `smtp.gmail.com` (6), `smtps.aruba.it` (1), `NULL`
  (2). Il provider prevalente **non** è Gmail.
- **`smtpPort`**: `465` (18), `25` (1), `587` (1), `NULL` (2) → la maggioranza usa 465 (TLS implicito),
  **coerente** con il `secure: true` cablato nei consumatori; solo la riga a `587` resta mal
  configurata.
- **`smtpSecurity`**: valori reali `SSL` (19) e `None` (1) — **non** `TLS` come ipotizzato dal DB dev.
- **`fromEmail` / `fromName` / `smtpUsername`**: nella maggioranza dei record `noreply@summerbooking.it`
  / `SummerBooking` (12–13 righe) → è di fatto la casella condivisa della piattaforma reimpostata per
  property; le restanti ~9 righe hanno mittenti reali dello stabilimento
  (`prenotazioni@marinevillage.it`, `info@bagno-romeo.it`, caselle gmail, …).
- **`smtpPassword`**: in chiaro; **13 righe condividono la stessa password** (quella della casella
  `noreply@summerbooking.it`) → una compromissione di un singolo pannello espone l'SMTP di piattaforma.
- **`testEmailRecipients`**: `NULL` su tutte le 22 righe (confermato).
- Date: `createdAt` 2025-04-29 → 2026-03-07; `updatedAt` fino a 2026-08-14.

**Dominio nella riscrittura**: **C6 — Comunicazione** (modulo `Notification`): è la config del
**canale email**. `smtpPassword` va in un secret manager (oggi `varchar` in chiaro, come
[`keysecret`](keysecret.md) / [`stripe_configrations`](stripe_configrations.md)). I consumatori vanno
unificati: oggi ognuno ricostruisce il proprio `nodemailer.createTransport` con `secure: true`
cablato.

---

## Campi

### id
- **Tipo**: `int(11)`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `22` (contigui).

### fromEmail
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: indirizzo mittente. Usato come `from` (newsletter: `` `${bathouseName} ${fromEmail}` ``;
  `sendReservationSummery`/`sendTestEmail`: `fromEmail` diretto).
- **Esempio di valori** (prod): `"noreply@summerbooking.it"` (12/22), `"prenotazioni@marinevillage.it"`,
  `"info@bagno-romeo.it"`, `"lidosanfelice1977@gmail.com"`; `NULL` su 2 righe.

### fromName
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: nome visualizzato del mittente. **Non usato** dai consumatori (nessuno compone
  `"fromName" <fromEmail>`); la newsletter usa `bathouses.bathouseName` al suo posto.
- **Esempio di valori** (prod): `"SummerBooking"` (12/22), `"Marine Village"`, `"Lido San Felice"`,
  `"Fiamme Oro Sport Beach"`; a volte contiene erroneamente un indirizzo email.
- **Deprecato**: **parzialmente** — salvato e restituito al pannello, ma nessun invio lo applica.

### smtpServer
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Esempio di valori** (prod): `"authsmtp.securemail.pro"` (13/22), `"smtp.gmail.com"` (6),
  `"smtps.aruba.it"` (1); `NULL` su 2.

### smtpPort
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Descrizione**: porta SMTP. Tutti i consumatori creano il transporter con `secure: true` **cablato**
  (valido per la porta 465, TLS implicito). **In produzione** 18/22 righe usano `465` (coerente),
  1 usa `587` (STARTTLS → mal configurata con `secure: true`), 1 usa `25`, 2 sono `NULL`.
- **Esempio di valori** (prod): `465` (18), `25` (1), `587` (1).

### smtpUsername
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Esempio di valori** (prod): `"noreply@summerbooking.it"` (13/22), poi le stesse caselle di
  `fromEmail` per le config specifiche dello stabilimento.

### smtpPassword
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci` — **password in chiaro**
- **Nullabile**: Sì
- **Descrizione**: password SMTP, salvata in chiaro e **restituita in chiaro** da `getEmailConfig` al
  frontend. Da spostare in secret manager.
- **Esempio di valori**: *(non riportati — segreti)*. **In produzione**: 8 valori distinti su 20 righe
  valorizzate; **13 righe condividono la stessa password** (quella della casella di piattaforma
  `noreply@summerbooking.it`).

### smtpSecurity
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: modalità di cifratura del canale. **Non usato**: i consumatori ignorano questo campo
  e cablano `secure: true` + `tls: { rejectUnauthorized: false }`.
- **Enumeratore**: **sì** — i valori reali di produzione sono solo `SSL` (19/20) e `None` (1); da
  modellare come `enum SmtpSecurity { None, StartTls, Ssl }` e usare davvero per `secure`/`requireTLS`.
- **Esempio di valori** (prod): `"SSL"`, `"None"`.

### testEmailRecipients
- **Tipo**: `longtext` `COLLATE utf8mb4_bin`, `CHECK (json_valid(...))` — JSON
- **Nullabile**: Sì
- **Descrizione**: destinatari per l'invio di prova dal pannello. `getEmailConfig` fa
  `JSON.parse(config.testEmailRecipients)`; `sendTestEmail` in realtà usa `req.body.fromEmail` come
  destinatario, **non** questo campo. Nella riga reale è `NULL` (`JSON.parse(null)` → `null`, nessun
  errore).
- **Esempio di valori**: `null`, `["test@example.com"]` *(teorico)*
- **Deprecato**: **parzialmente** — salvato/parsato ma nessun invio lo usa.
- **Produzione**: **nessun valore in produzione** — tutti i record hanno `NULL` o stringa vuota `''`
  (colonna inclusa nell'elenco delle colonne vuote in produzione, verifica 2026-09-07).

### propertyId
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Relazioni**: [`properties`](properties.md)`.id` — **nessuna FK**. Chiave logica dell'upsert e
  filtro di tutti i lettori. Nessun vincolo `UNIQUE(propertyId)` a schema (l'unicità "0 o 1 config per
  property" è solo applicativa, in `addEmailConfig`). **In produzione** i 22 `propertyId` sono tutti
  distinti → l'unicità applicativa ha tenuto.
- **Esempio di valori** (prod): `96`, `10588`, `10559`, `228`, `115` — range `42` … `10762`.

### status
- **Tipo**: `tinyint(1)`, `DEFAULT 1` — attiva / disattivata
- **Nullabile**: Sì
- **Descrizione**: `1` = config attiva (i lettori filtrano `status: true`); `0` = disattivata /
  soft-deleted (`deleteEmailConfig`). **In produzione**: `0` per 12 righe, `1` per **10 righe** → ci
  sono config SMTP realmente attive.
- **Enumeratore**: no — booleano (`bool is_active`).
- **Esempio di valori** (prod): `1` (10), `0` (12).

### createdAt / updatedAt
- **Tipo**: `datetime`, `DEFAULT NULL` (gestiti da Sequelize)
- **Nullabile**: Sì (a schema)
- **Esempio di valori** (prod): `createdAt` da `2025-04-29 15:19:53` a `2026-03-07 15:05:16`;
  `updatedAt` fino a `2026-08-14 10:05:54`.

---

## Osservazioni verificate nel codice (criticità)

- **`smtpPassword` in chiaro** a DB e nella response di `getEmailConfig` verso il frontend. Stesso
  trattamento richiesto per [`keysecret`](keysecret.md): secret manager.
- **`authJwt.optionalAuth`** su `add`/`get`/`delete`/`send-test`: chiunque, senza login, può
  impostare l'SMTP di una property (`propertyId` dal body) o leggerne la password.
- **`secure: true` cablato** in ogni consumatore, `smtpPort`/`smtpSecurity` ignorati. **In produzione**
  il problema è ridotto ma non nullo: 18/22 righe sono a `465` (coerenti con `secure: true`), 1 riga a
  `587` (STARTTLS) resta di fatto mal configurata, 2 righe hanno porta `NULL`.
- **Password SMTP condivisa** (prod): 13 config puntano alla stessa casella/password di piattaforma
  `noreply@summerbooking.it` — non è una vera "config per stabilimento" ma il mittente globale
  reimpostato per property; una fuga da un singolo pannello (`getEmailConfig` la restituisce in chiaro,
  con `optionalAuth`) espone l'SMTP di tutta la piattaforma.
- **Config duplicata ad-hoc**: `sendReservationSummery`, `sendTestEmail`, `news-letter.js` e
  `switchboard.controller.js` creano ciascuno il proprio `nodemailer.createTransport` invece di
  riusare `mailtemplates/mailsetup.js` (già rilevato in
  [`docs/01-…backend.md` §8](../../progettazione/01-criticita-e-indicazioni-backend.md)).
- **`fromName` e `testEmailRecipients` scritti ma non applicati** da nessun invio.
- **Campagne e automazioni marketing ignorano questa config** e spediscono dal mittente globale →
  incoerenza di dominio (mail "della property" inviate da `noreply@summerbooking.it`).
- **Tabella sorella `email_configrations`** (0 righe, nessun model): residuo di una prima versione, da
  eliminare.
- Nessun `UNIQUE(propertyId)`, nessuna FK.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `smtpSecurity` | `varchar` — valori reali di prod: `"SSL"` (19), `"None"` (1) | `enum SmtpSecurity { None, StartTls, Ssl }` — da usare davvero per `secure`/`requireTLS` |

`status` è un booleano (`tinyint`), non un enumeratore.
