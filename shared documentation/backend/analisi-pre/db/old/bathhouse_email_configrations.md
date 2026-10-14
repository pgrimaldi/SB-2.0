# Tabella `bathhouse_email_configrations`

> Fonti: schema e dati reali del DB dev `summer_booking_dev` (**1 riga**, `AUTO_INCREMENT=20`), model
> [`back-pre/app/models/email-config.model.js`](../../back-pre/app/models/email-config.model.js)
> (`sequelize.define("bathhouse_email_configration", …)` senza `freezeTableName` → nome **pluralizzato**
> `bathhouse_email_configrations`; senza `timestamps: false` → `createdAt`/`updatedAt` gestiti da
> Sequelize, a schema `datetime NULL`), registrato come `db.email_configration` in
> [`models/index.js`](../../back-pre/app/models/index.js) (riga 178). **Nessuna associazione ORM,
> nessuna FK, nessun indice** oltre la PK.
>
> **Scrittori** — [`controllers/emailConfig.controller.js`](../../back-pre/app/controllers/emailConfig.controller.js):
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
> - [`booking.controller.js`](../../back-pre/app/controllers/booking.controller.js) `sendReservationSummery`
>   (`POST /api/sb/sendReservationSummery`) — se esiste config `status: true`, invia il riepilogo
>   prenotazione al cliente con l'SMTP della property; altrimenti usa il transporter globale.
> - [`newsletter.controller.js`](../../back-pre/app/controllers/newsletter.controller.js) `sendmessage`
>   — legge **solo** `fromEmail` (senza filtro `status`).
> - [`cronjob/news-letter.js`](../../back-pre/cronjob/news-letter.js) `sentMailCronJob` — sceglie
>   l'SMTP per inviare le righe [`email_logs`](email_logs.md) `status: 0`. **Cron disattivato**
>   (commentato in `server.js`).
>
> **Non** usata da `cron.helper.js` per le campagne / automazioni marketing: `sendCampaignEmail` e
> `sendAutomationEmail` usano il `mailOptions.from` globale (`noreply@summerbooking.it`), non l'SMTP
> della property. Vedi [`marketing_email_campaign`](marketing_email_campaign.md) /
> [`marketing_automation`](marketing_automation.md).
>
> **Frontend**:
> [`front-pre/src/data/actions/managerActions.js`](../../front-pre/src/data/actions/managerActions.js)
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

Stato reale nel DB dev: **una sola riga**, `propertyId = 96`, **`status = 0` (disattivata)**, SMTP
Gmail (`smtp.gmail.com:587`, `TLS`). Quindi *nessuno* stabilimento ha oggi una config SMTP attiva: in
pratica tutti gli invii partono dal transporter globale. `AUTO_INCREMENT=20` con 1 riga → ~19
create/cancellate nel tempo (l'upsert per `propertyId` non ricicla gli id; il delete è soft, quindi
il gap viene da vecchi `create`/`sync`).

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
- **Esempio di valori**: 1, 2, 3, ..

### fromEmail
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: indirizzo mittente. Usato come `from` (newsletter: `` `${bathouseName} ${fromEmail}` ``;
  `sendReservationSummery`/`sendTestEmail`: `fromEmail` diretto).
- **Esempio di valori**: `"noreplysummerbooking@gmail.com"`

### fromName
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: nome visualizzato del mittente. **Non usato** dai consumatori (nessuno compone
  `"fromName" <fromEmail>`); la newsletter usa `bathouses.bathouseName` al suo posto.
- **Esempio di valori**: `"SummerBooking"`
- **Deprecato**: **parzialmente** — salvato e restituito al pannello, ma nessun invio lo applica.

### smtpServer
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Esempio di valori**: `"smtp.gmail.com"`

### smtpPort
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Descrizione**: porta SMTP. **Incoerenza**: tutti i consumatori creano il transporter con
  `secure: true` **cablato**, che vale per la porta 465 (TLS implicito); la riga reale ha `587`
  (STARTTLS) → connessione mal configurata.
- **Esempio di valori**: `587`

### smtpUsername
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Esempio di valori**: `"noreplysummerbooking@gmail.com"`

### smtpPassword
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci` — **password in chiaro**
- **Nullabile**: Sì
- **Descrizione**: password SMTP, salvata in chiaro e **restituita in chiaro** da `getEmailConfig` al
  frontend. Da spostare in secret manager.
- **Esempio di valori**: *(non riportato — segreto)*

### smtpSecurity
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: modalità di cifratura del canale (`TLS` nella riga reale). **Non usato**: i
  consumatori ignorano questo campo e cablano `secure: true` + `tls: { rejectUnauthorized: false }`.
- **Enumeratore**: **sì** — `enum SmtpSecurity { None, StartTls, Tls }` (o `Ssl`/`Tls`), da applicare
  davvero nella scelta di `secure`/`requireTLS`.
- **Esempio di valori**: `"TLS"`

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
  property" è solo applicativa, in `addEmailConfig`).
- **Esempio di valori**: `96`

### status
- **Tipo**: `tinyint(1)`, `DEFAULT 1` — attiva / disattivata
- **Nullabile**: Sì
- **Descrizione**: `1` = config attiva (i lettori filtrano `status: true`); `0` = disattivata /
  soft-deleted (`deleteEmailConfig`). Nella riga reale: `0`.
- **Enumeratore**: no — booleano (`bool is_active`).
- **Esempio di valori**: `0`

### createdAt / updatedAt
- **Tipo**: `datetime`, `DEFAULT NULL` (gestiti da Sequelize)
- **Nullabile**: Sì (a schema)
- **Esempio di valori**: `2025-06-25 14:07:16`

---

## Osservazioni verificate nel codice (criticità)

- **`smtpPassword` in chiaro** a DB e nella response di `getEmailConfig` verso il frontend. Stesso
  trattamento richiesto per [`keysecret`](keysecret.md): secret manager.
- **`authJwt.optionalAuth`** su `add`/`get`/`delete`/`send-test`: chiunque, senza login, può
  impostare l'SMTP di una property (`propertyId` dal body) o leggerne la password.
- **`secure: true` cablato** in ogni consumatore, `smtpPort`/`smtpSecurity` ignorati → la sola riga
  reale (`587` + `TLS`) è di fatto mal configurata.
- **Config duplicata ad-hoc**: `sendReservationSummery`, `sendTestEmail`, `news-letter.js` e
  `switchboard.controller.js` creano ciascuno il proprio `nodemailer.createTransport` invece di
  riusare `mailtemplates/mailsetup.js` (già rilevato in
  [`docs/01-…backend.md` §8](../../docs/01-criticita-e-indicazioni-backend.md)).
- **`fromName` e `testEmailRecipients` scritti ma non applicati** da nessun invio.
- **Campagne e automazioni marketing ignorano questa config** e spediscono dal mittente globale →
  incoerenza di dominio (mail "della property" inviate da `noreply@summerbooking.it`).
- **Tabella sorella `email_configrations`** (0 righe, nessun model): residuo di una prima versione, da
  eliminare.
- Nessun `UNIQUE(propertyId)`, nessuna FK.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `smtpSecurity` | `varchar` (`"TLS"`) | `enum SmtpSecurity { None, StartTls, Tls }` — da usare davvero per `secure`/`requireTLS` |

`status` è un booleano (`tinyint`), non un enumeratore.
