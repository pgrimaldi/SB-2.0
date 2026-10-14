# Tabella `email_logs`

> Fonti: schema + **dati reali di produzione** (dump `sb_prod_dump_2026_09_08`; file da 2,06 GB, lettura
> parziale dei primi ~121 MB = **19.594 righe su ~335.000 stimate**; la porzione letta copre gli `id`
> 1…19.596, con `createdAt` dal 2021-03-12 al 2024-06-28 → la tabella **conserva tutto lo storico dal
> 2021**, non è archiviata). È di gran lunga la tabella più pesante del DB dopo le prenotazioni. Model
> [`back-pre/app/models/logemail.model.js`](../../../../../../back-pre/app/models/logemail.model.js)
> (`sequelize.define("email_logs", …)`, senza `timestamps: false` → `createdAt`/`updatedAt` gestiti da
> Sequelize, a schema `datetime NULL`), registrato come `db.logemail` in
> [`models/index.js`](../../../../../../back-pre/app/models/index.js) (riga 27). **Nessuna associazione ORM,
> nessuna FK, nessun indice** oltre la PK.
>
> **Scrittori** — decine di `Logemail.create({...})`, sempre con valori letterali per `status` e
> `cronStatus`:
> - [`controllers/helpers/email.helper.js`](../../../../../../back-pre/app/controllers/helpers/email.helper.js) —
>   ~30 punti: quasi tutti **dopo** un `transporter.sendMail` andato a buon fine (`if (!error)`), con
>   `status: 1`, `cronStatus: 0` → **log a posteriori** di una mail già spedita (conferme
>   prenotazione/servizio, link di pagamento, sharing, credito, PDF bagnino, signup, forgot password,
>   registrazione/cancellazione property, ecc.).
> - [`controllers/helpers/cron.helper.js`](../../../../../../back-pre/app/controllers/helpers/cron.helper.js)
>   `sendCampaignEmail` (`emailType: "Campaign Email"`, `status: 1`, `cronStatus: 1`) e
>   `sendAutomationEmail` (`emailType: "Automation Email"`, `status: 1`, `cronStatus: 1`) — log dopo
>   l'invio ([`marketing_email_campaign`](marketing_email_campaign.md) /
>   [`marketing_automation`](marketing_automation.md)).
> - [`controllers/newsletter.controller.js`](../../../../../../back-pre/app/controllers/newsletter.controller.js)
>   `sendmessage` (`emailType` = costante **non importata** `newsletterEmailType` → `ReferenceError`;
>   `status: 0`, `cronStatus: 0`, `propertyId` valorizzato) — **unico** scrittore che inserisce righe
>   `status: 0`, cioè "in coda, non ancora inviata" (vedi Lettori).
>
> **Lettore** — **uno solo**:
> [`back-pre/cronjob/news-letter.js`](../../../../../../back-pre/cronjob/news-letter.js) `sentMailCronJob`
> (`cron.schedule("* * * * *")`): `Logemail.findAll({ where: { status: 0 } })`, per ogni riga costruisce
> un transporter dalla config SMTP della property ([`bathhouse_email_configrations`](bathhouse_email_configrations.md),
> `db.email_configration`, `status: true`) o dal transporter di default, invia e fa
> `Logemail.update({ status: 1 }, { where: { id } })`. **Questo cron è disattivato**: in
> [`server.js`](../../../../../../back-pre/server.js) `cronJobnews.sentMailCronJob()` è **commentato** (righe 116 e
> 164). Nessun'altra `findAll`/`findOne`/`update`/`destroy` su questa tabella in tutto `back-pre`.
>
> **Conseguenza**: nella pratica `email_logs` è una tabella **di solo append**. Nei ~335.000 record stimati di produzione (verificato sui primi 19.594 letti)
> `status` vale **sempre `1`**; non esiste alcuna riga `status: 0` (il ramo newsletter non produce
> effetti perché il controller lancia `ReferenceError` e comunque il cron consumatore è spento).
> `cronStatus` non è letto da nessuna parte.
>
> **Frontend**: nessun frontend legge o mostra `email_logs`. La newsletter è inviata da
> [`front-pre/src/components/newsLetter/NewsLetter.js`](../../../../../../front-pre/src/components/newsLetter/NewsLetter.js)
> (+ `homeDetails/details*.js`) via
> [`data/actions/newsAction.js`](../../../../../../front-pre/src/data/actions/newsAction.js) → `POST /api/sb/sendmessage`
> (`authJwt.optionalAuth`), che scrive qui ma — vedi sopra — non produce righe utili.

---

## Nota di contesto

`email_logs` doveva essere l'**outbox + log invii email** della piattaforma: il newsletter controller
inserisce le mail da spedire con `status: 0`, un cron le prende, le invia con l'SMTP della property e
le porta a `status: 1`. La lista dei destinatari newsletter è
[`subscribes`](subscribes.md) (iscrizioni dal sito pubblico) — anch'essa **senza consumatore
attivo**, dato che il cron è spento. Nella realtà il cron è spento e il newsletter controller è rotto, quindi la
tabella raccoglie **solo** i log "questa mail è stata inviata" scritti a posteriori dagli altri flussi
(`email.helper.js`, cron campagne/automazioni). È l'unico registro storico degli invii email, ma è
inconsistente: nessun record di **fallimento** (le `Logemail.create` stanno tutte dentro `if (!error)`),
`propertyId` è `NULL` su **tutte** le righe della porzione di produzione letta (colonna di fatto mai popolata su questo storico), `emailType` è una stringa libera che a volte è la
categoria, a volte l'oggetto della mail, a volte il nome del file PDF allegato.

**Dominio nella riscrittura**: **C6 — Comunicazione** (modulo `Notification`). Nel target è
l'**outbox/audit** del modulo: una riga per messaggio con stato reale (`queued`/`sent`/`failed`/`bounced`),
`channel` (email/whatsapp/push/in-app), riferimento al soggetto per id copiato, retry con backoff e
stato terminale — non un log scritto solo sul percorso felice. Assorbe anche i flag `*Sent` sparsi in
[`fiskaly_receipt`](fiskaly_receipt.md), [`bookings_send_payment_summaries`](bookings_send_payment_summaries.md),
`hotel_reports`, `sharing`.

---

## Campi

### id
- **Tipo**: `bigint(20)`, `AUTO_INCREMENT`, `NOT NULL` — chiave primaria
- **Nullabile**: No
- **Esempio di valori** (prod): `1` … `19.596`+ (contiguo; totale stimato ~335.000).

### emailType
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci` — etichetta libera
- **Nullabile**: Sì
- **Descrizione**: categoria **oppure** oggetto della mail **oppure** nome del file allegato, a
  seconda dello scrittore — nessun vocabolario. Nella porzione di produzione letta (2021-2024, primi
  ~19.600 record) i valori sono etichette di categoria (`"Forgot Password"`, `"Register Property"`,
  `"Property Status"`, `"Delete Property"`, `"Newsletter"`, …); le frequenze aggregate su tutto lo
  storico (che include valori usa-e-getta come `"lifeguardPdf/lifeguard-….pdf"` e stringhe vuote) non
  sono ricalcolabili sui soli 121 MB letti.
- **Enumeratore**: **parziale** — la parte "categoria" (`Newsletter`, `Campaign Email`,
  `Automation Email`, `Forgot Password`, `Electronic Receipt`, …) è un vocabolario chiuso →
  `enum EmailKind`; l'oggetto reale della mail va tenuto in `subject`, non qui.
- **Esempio di valori**: `"Newsletter"`, `"Automation Email"`, `"Campaign Email"`,
  `"Thank you! Your booking is confirmed"`, `"lifeguard-1690513202761.pdf"`

### emailFrom
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: mittente usato per l'invio. Quasi sempre `noreply@summerbooking.it` (il
  `mailOptions.from` globale di `mailtemplates/mailsetup.js`); per la newsletter è
  `` `${bathouseName} ${fromEmail}` `` con `fromEmail` da
  [`bathhouse_email_configrations`](bathhouse_email_configrations.md).
- **Esempio di valori**: `"noreply@summerbooking.it"`, `"Lido San Domenico noreply@summerbooking.it"`

### emailTo
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Descrizione**: **un singolo** indirizzo destinatario (gli invii multipli producono più righe; in
  `email.helper.js` alcuni blocchi fanno `emails.forEach(email => Logemail.create(...))`). Può essere
  stringa vuota se il cliente non ha email.
- **Esempio di valori**: `"gyjarada@yopmail.com"`, `"m.accattoli@qbitsoft.it"`, `""`

### subject
- **Tipo**: `varchar(255)` `COLLATE utf8mb4_unicode_ci`
- **Nullabile**: Sì
- **Esempio di valori**: `"Lista Presenze oggi 2026-09-03"`, `"Thank you! Your booking is confirmed"`,
  `"Test Email"`

### body
- **Tipo**: `text` `COLLATE utf8mb4_unicode_ci` — HTML completo della mail
- **Nullabile**: Sì
- **Descrizione**: corpo HTML renderizzato (template EJS / helper). Nessun troncamento gestito: un
  corpo oltre ~64 KB verrebbe troncato dal tipo `text`.
- **Esempio di valori**: `"<div style=\"...\">Gentile Mario Rossi, …</div>"` *(HTML lungo, troncato)*

### status
- **Tipo**: `int(11)` — stato di invio
- **Nullabile**: Sì (nessun default a schema)
- **Descrizione**: `0` = **in coda** (solo dal newsletter controller; il cron consumatore, spento,
  dovrebbe inviarle e portarle a `1`); `1` = **inviata / loggata dopo invio** (tutti gli altri
  scrittori scrivono direttamente `1`). **In produzione: `1` su tutte le 19.594 righe lette** (0 righe
  con `status = 0`).
- **Enumeratore**: sì ma **da ridisegnare** — l'attuale `0/1` mescola "coda" e "log a posteriori".
  Target: `enum EmailDeliveryStatus { Queued, Sending, Sent, Failed, Bounced }` con stato reale.
- **Esempio di valori**: `1`
- **Deprecato**: **parzialmente** — letto **solo** da un cron disattivato; di fatto sempre `1`.

### cronStatus
- **Tipo**: `int(11)`
- **Nullabile**: Sì
- **Descrizione**: valorizzato `0` (email.helper, newsletter, gran parte di cron.helper) o `1`
  (`sendCampaignEmail`, `sendAutomationEmail`). **Nessun punto del codice lo legge** (`grep` su tutto
  `back-pre`: 0 letture).
- **Esempio di valori** (prod, porzione letta): `0` (19.582 righe), `1` (12).
- **Deprecato**: **Sì** — scritto ovunque, mai letto.

### attachments
- **Tipo**: `longtext` `COLLATE utf8mb4_bin`, `CHECK (json_valid(attachments))` — JSON
- **Nullabile**: Sì
- **Descrizione**: allegati della mail, ma **formato incoerente tra scrittori**: a volte `[]`, a volte
  `""` (stringa vuota — comunque JSON valido), a volte una stringa path singola
  (`newsletter.controller`: `file.path.replace(/,/, "")`), a volte l'array `attachments` passato a
  nodemailer (`[{filename, path}]`). Il cron consumatore (spento) fa `attachments.split('/')[4]` — si
  aspetta una stringa path.
- **Esempio di valori**: `[]`, `""`, `[{"filename":"27.jpg","path":"https://summerbooking.s3…/27.jpg"}]`

### createdAt / updatedAt
- **Tipo**: `datetime`, `DEFAULT NULL` (gestiti da Sequelize; nei dati sempre valorizzati)
- **Nullabile**: Sì (a schema)
- **Descrizione**: `createdAt` = momento del log; `updatedAt` = uguale, salvo il (mai eseguito)
  passaggio `status → 1` del cron newsletter.
- **Esempio di valori** (prod): nella porzione letta (id 1…19.596) vanno dal `2021-03-12 11:04:31` al
  `2024-06-28 13:55:36`; lo storico prosegue fino alla data dell'export sulla parte non letta.

### propertyId
- **Tipo**: `int(11)`, `defaultValue: null`
- **Nullabile**: Sì
- **Relazioni**: [`properties`](properties.md)`.id` — nessuna FK. Serve **solo** al cron newsletter
  (spento) per scegliere l'SMTP della property.
- **Descrizione**: valorizzato **solo** dal newsletter controller (e da qualche blocco cron); `NULL`
  su tutte le 19.594 righe della porzione di produzione letta.
- **Esempio di valori**: `96`, `null`

---

## Osservazioni verificate nel codice (criticità)

- **Solo append, mai letta**: l'unico lettore (`cronjob/news-letter.js`) è **commentato in
  `server.js`**. `email_logs` non alimenta nessuna vista, nessun retry, nessun report.
- **Nessun log dei fallimenti**: ogni `Logemail.create` è dentro `if (!error)` / `try` che ingoia
  l'errore → la tabella registra solo il percorso felice. Impossibile sapere quante mail sono fallite.
- **`cronStatus` colonna morta**; **`status` di fatto costante `1`** (il ramo `0` è irraggiungibile:
  `newsletter.controller` lancia `ReferenceError` su costanti non importate — `newsletterEmailType`,
  `newsletterSuccess`, `customerNotFound`).
- **Race condition** nel newsletter controller (`customers.map(async …)` senza `await`, `res.send`
  dentro il loop su `index === lastIndex`) — già segnalata in
  [`docs/01-…backend.md` §8](../../progettazione/01-criticita-e-indicazioni-backend.md).
- **`emailType` sovraccarico**: categoria + oggetto + filename nello stesso campo `varchar(255)`,
  centinaia di valori distinti.
- **`attachments` con 3-4 formati diversi** a seconda dello scrittore; il consumatore si aspetta il
  formato che quasi nessuno scrive.
- **Nessun `propertyId` sulla maggioranza delle righe** → impossibile filtrare gli invii per
  stabilimento; il multi-tenancy del log non esiste.
- **`body` `text` senza cap**: HTML completo duplicato su 61 k righe (tabella grande e in sola
  crescita, nessuna retention).
- **`authJwt.optionalAuth`** su `/api/sb/sendmessage`: la newsletter è inviabile senza login,
  `propertyId`/`message`/`files` dal body.

### Campi candidati a enumeratore

| Campo | Oggi | Target |
|---|---|---|
| `status` | `int` (di fatto sempre `1`) | `enum EmailDeliveryStatus { Queued, Sending, Sent, Failed, Bounced }` — con stato reale, non solo "inviata" |
| `emailType` (parte categoria) | `varchar` libero | `enum EmailKind { Newsletter, Campaign, Automation, BookingConfirmation, PaymentLink, ForgotPassword, … }` + `subject` separato per l'oggetto reale |
| `cronStatus` | `int` `0`/`1` | **eliminare** (colonna morta) |
