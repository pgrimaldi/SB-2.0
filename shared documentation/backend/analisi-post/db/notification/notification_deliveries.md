# Tabella `notification_deliveries`

> **Dominio**: C6 — `notification` · **sottoarea**: — (nucleo del modulo) · **schema**: `notification` ·
> **nome flat**: `notification_deliveries` · **forma canonica**: `notification.deliveries`.
>
> È l'**outbox + audit unico per destinatario** previsto da
> [`docs/03-gestione-dati.md` §3.1bis](../../../progettazione/03-gestione-dati.md) (riga *"(nuova — outbox unico
> per destinatario)"* → `notification.deliveries`) e da
> [`docs/01-criticita-e-indicazioni-backend.md` §8](../../../progettazione/01-criticita-e-indicazioni-backend.md).
> Sostituisce/assorbe: [`email_logs`](../../../analisi-pre/db/email_logs.md) (~335 k righe, log solo del
> percorso felice), i flag **"inviato / canale"** sparsi su altri domini —
> [`bookings_send_payment_summaries.method_flag` / `email_sent`](../../../analisi-pre/db/bookings_send_payment_summaries.md)
> (C3), [`fiskaly_receipt.emailSent` / `whatsappSent`](../../../analisi-pre/db/fiskaly_receipt.md) e
> `fiskaly_non_fiscal_receipt.*Sent` (C4), [`sharing_details.email_sent`](../../../analisi-pre/db/sharing_details.md)
> (C1), [`hotel_reports.path`](../../../analisi-pre/db/hotel_reports.md) + logica di invio (C2),
> [`card_transactions.sendCardDetails`](../../../analisi-pre/db/card_transactions.md) (C3) — e la parte
> "invio email/push per destinatario" dei cron marketing
> ([`marketing_email_campaign`](../../../analisi-pre/db/marketing_email_campaign.md) /
> [`marketing_automation`](../../../analisi-pre/db/marketing_automation.md)). Fonti:
> [`docs/03-gestione-dati.md` §2.4quater, §3.1bis, §6](../../../progettazione/03-gestione-dati.md),
> [`docs/01-…backend.md` §8](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/20-jobs.md`](../../../progettazione/20-jobs.md),
> [`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md).

---

## Descrizione

**Una riga = un messaggio verso un singolo destinatario su un singolo canale.** È il registro di
tutto ciò che il modulo `Notification` prova a consegnare: email transazionali, email/automazioni
marketing, notifiche push (FCM/APNs), messaggi WhatsApp, e la materializzazione degli invii che oggi
sono un flag booleano su un'altra tabella.

Ogni riga porta: il **canale**, lo **scopo** (`kind`), il **destinatario** (tipo + riferimento vivo
per `public_id` + indirizzo *congelato* al momento dell'invio), l'**entità di origine** (`subject_type`
+ `subject_public_id`, id copiato — **mai** FK cross-modulo), lo **stato reale** del ciclo di
consegna, i contatori di **retry con backoff**, e — per l'audit — l'oggetto e il corpo *renderizzati*
effettivamente spediti (con retention dedicata).

Correzioni rispetto al legacy:

- **`email_logs` scritto solo dentro `if (!error)`** (nessun fallimento tracciato) → stato terminale
  `failed` / `bounced` dopo N tentativi ([`docs/01 §8`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- **`status` `0`/`1` che mescola "coda" e "log a posteriori"** → `enum` con stati distinti
  (`queued` → `sending` → `sent` → `delivered` | `failed` | `bounced` | `skipped`).
- **`cronStatus` colonna morta** → non migrata.
- **`emailType` sovraccarico** (categoria + oggetto + nome file) → `kind` (enum, la sola *categoria*)
  + `subject_snapshot` (l'oggetto reale) separati.
- **`propertyId` `NULL` su tutte le righe di produzione** → `property_id` obbligatorio (dal claim
  token / dall'entità di origine), unico modo per filtrare gli invii per stabilimento.
- **flag `*Sent` booleani senza data né esito** su C1/C3/C4 → riga qui con `channel`, `status`,
  `sent_at`, `failure_reason`; sulla tabella di origine **non resta nulla** ([`docs/03 §6`](../../../progettazione/03-gestione-dati.md)).
- **`attachments` JSON con 3-4 formati** → l'allegato è **dell'entità di origine** (template,
  campagna, scontrino, report): qui solo `has_attachments` (vedi *Valutazione JSON*).
- **retry infinito ad ogni tick** dei cron legacy ([`docs/01 §8`](../../../progettazione/01-criticita-e-indicazioni-backend.md))
  → `attempt_count` + `next_attempt_at` + `max_attempts` con backoff esponenziale (pattern Polly).
- **invio sincrono e sequenziale** nel web server → questa tabella è alimentata da una coda/worker
  ([`docs/20-jobs.md`](../../../progettazione/20-jobs.md)); la riga è creata `queued` nella **stessa
  transazione** del comando che la origina (outbox), il worker la porta avanti.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `channel` | **enum esclusivo**: un invio è su **un** canale. Un messaggio "email + WhatsApp" = **due righe** (non `enum[]`). Sostituisce `bookings_send_payment_summaries.method_flag` (`0`/`1`/`2`). |
| `status` | **enum esclusivo** (ciclo di vita lineare). |
| `kind` | **enum esclusivo**: uno scopo per riga. Sostituisce la *categoria* dentro `email_logs.emailType`. |
| `recipient_type` | **enum esclusivo**. |
| `subject_type` | **enum esclusivo** (l'entità di origine è una sola). |
| `also_send_push` (legacy `enablePushNotifications` / `enableAppNotifications`) | **non** un flag qui: la push è una **riga separata** con `channel = push` e stesso `subject`. |

## Snapshot

Riga di **log/consegna**: quasi tutti i campi "cliente" sono **congelati** all'invio (il messaggio
spedito non cambia se l'anagrafica cambia dopo). Campi `*_snapshot`: `recipient_address_snapshot`,
`recipient_name_snapshot`, `from_address_snapshot`, `subject_snapshot`. `rendered_body` è
l'**artefatto consegnato** (come per `fiskaly_receipts`): natura congelata descritta nel campo, senza
suffisso. Accanto agli snapshot, i riferimenti **vivi** per `public_id` (`recipient_customer_public_id`
/ `recipient_user_public_id`) per gli endpoint GDPR ([`docs/11-modulo-privacy.md` §5](../../../progettazione/11-modulo-privacy.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_deliveries` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | riferimento da gestionale / export GDPR |
| `ix_..._pending` | `(next_attempt_at) WHERE status IN ('queued','sending')` | btree parziale | il worker prende solo le righe da (ri)provare — non scansiona lo storico |
| `ix_..._property_created` | `(property_id, created_at DESC)` | btree | log invii per stabilimento / intervallo |
| `ix_..._subject` | `(subject_type, subject_public_id)` | btree | "com'è andato l'invio dello scontrino/link X" (ex flag `*Sent`) |
| `ix_..._recipient_customer` | `(recipient_customer_public_id, created_at DESC) WHERE recipient_customer_public_id IS NOT NULL` | btree parziale | GDPR: tutti i messaggi verso una persona |
| `ix_..._campaign` | `(subject_type, subject_public_id, status) WHERE subject_type IN ('marketing_campaign','marketing_automation')` | btree parziale | esito per destinatario di una campagna/automazione (oggi assente) |
| `ix_..._dedup` | `(idempotency_key)` | UNIQUE parziale (`WHERE idempotency_key IS NOT NULL`) | evita il doppio invio se il comando di origine viene riprocessato |

## Campi

### id
- **Legacy**: `email_logs.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile; usato nel gestionale ("dettaglio invio") e nell'export GDPR.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato in factory.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `email_logs.propertyId` (`NULL` su tutte le righe di produzione) / dedotto dall'entità di origine
- **Descrizione**: stabilimento proprietario dell'invio.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: Sì (solo per gli invii **di piattaforma** non legati a uno stabilimento: newsletter
  globale, lead `establishment_claim`, email di sistema al `platform_admin`).
- **Relazioni**: **`public_id` (uuid) copiato** da C2 [`catalog_properties`](../catalog/catalog_properties.md); [D-01](../_domande-committente.md).

### channel
- **Legacy**: `bookings_send_payment_summaries.method_flag` (`1` email / `2` WhatsApp) · `custom_templates.type` · canale scelto in `hotelReport.sendReportMessage`
- **Descrizione**: mezzo di consegna.
- **Tipo**: `enum notification.channel` — valori: `email`, `whatsapp`, `sms`, `push`, `in_app`.
- **Default**: `email`.
- **Nullabile**: No.
- **Relazioni**: `in_app` è raro qui (il feed in-app vive in
  [`notification_inapp_messages`](notification_inapp_messages.md)); presente per gli invii "misti".

### kind
- **Legacy**: parte *categoria* di `email_logs.emailType` (`Newsletter`, `Campaign Email`, `Automation Email`, `Forgot Password`, `Electronic Receipt`, …)
- **Descrizione**: scopo funzionale del messaggio (determina template, priorità, retention, regole
  anti-spam).
- **Tipo**: `enum notification.delivery_kind` — valori: `booking_confirmation`, `reservation_summary`,
  `payment_link`, `payment_receipt`, `e_receipt`, `non_fiscal_receipt`, `agreement_report` (report
  periodico convenzione — ex "hotel report", C2 usa `agreement`),
  `resale_credit_earned`, `wallet_statement`, `lifeguard_attendance`, `forgot_password`,
  `email_verification`, `account_lifecycle`, `newsletter`, `marketing_campaign`,
  `marketing_automation`, `establishment_claim`, `test`, `other`.
- **Default**: `other`.
- **Nullabile**: No.
- **Relazioni**: `marketing_*` ⇒ soggetto a verifica consenso (vedi
  [`notification_marketing_contacts`](notification_marketing_contacts.md) e
  [`notification_marketing_suppressions`](notification_marketing_suppressions.md)); i `kind`
  transazionali **non** sono soggetti a opt-out.

### subject_type
- **Legacy**: implicito (tabella/flag di provenienza)
- **Descrizione**: tipo dell'entità che ha originato l'invio.
- **Tipo**: `enum notification.subject_type` — valori: `booking_reservation`, `service_reservation`,
  `payment_request_link`, `payment_transaction`, `fiskaly_receipt`, `agreement_report`, `resale_offer`,
  `wallet_transaction`, `marketing_campaign`, `marketing_automation`, `marketing_event`,
  `newsletter_subscription`, `user_account`, `none`.
- **Default**: `none`.
- **Nullabile**: No.
- **Relazioni**: con `subject_public_id` ricostruisce il collegamento all'entità (C1/C2/C3/C4/C6).

### subject_public_id
- **Legacy**: `bookings_send_payment_summaries.id` · `fiskaly_receipt.id` · `hotel_reports.id` · `sharing_details.id` · `marketing_email_campaign.id` · …
- **Descrizione**: id pubblico dell'entità di origine (id **copiato per valore**).
- **Tipo**: `uuid`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`subject_type = none`).
- **Relazioni**: **id copiato**, **nessuna FK cross-modulo** ([`docs/03 §6`](../../../progettazione/03-gestione-dati.md)).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 200.884 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### recipient_type
- **Legacy**: implicito
- **Descrizione**: chi riceve.
- **Tipo**: `enum notification.recipient_type` — valori: `customer` (cliente finale C5), `staff_user`
  (utente gestionale C5), `property_contact` (recapito dello stabilimento — es. errori Fiskaly a
  `bathouses` email), `hotel_partner` (struttura convenzionata), `platform_admin`
  (`info@summerbooking.it` cablato nel legacy), `external_address` (indirizzo libero, es. destinatario
  newsletter non in rubrica).
- **Default**: `customer`.
- **Nullabile**: No.
- **Relazioni**: —.

### recipient_customer_public_id
- **Legacy**: `email_logs` non lo tracciava (solo `emailTo` testuale)
- **Descrizione**: riferimento **vivo** al cliente C5, quando il destinatario è un cliente in rubrica.
- **Tipo**: `uuid` (= `public_id` di `identity_customer_customers`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5; usato per l'export/cancellazione GDPR.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 200.884 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### recipient_user_public_id
- **Legacy**: — (le copie al "manager" erano una query raw per email)
- **Descrizione**: riferimento **vivo** all'utente gestionale C5 (copie al manager, notifiche staff,
  push).
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 200.884 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### recipient_address_snapshot
- **Legacy**: `email_logs.emailTo`
- **Descrizione**: indirizzo di consegna **congelato** all'invio: email, numero E.164 (WhatsApp/SMS),
  o device token abbreviato (push). Per `push` il token pieno vive in
  [`notification_push_device_tokens`](notification_push_device_tokens.md).
- **Tipo**: `varchar(320)`.
- **Default**: nessuno.
- **Nullabile**: Sì (invii `push` che referenziano più token; invii legacy con `emailTo = ""`).
- **Relazioni**: snapshot.

### recipient_name_snapshot
- **Legacy**: dedotto dal corpo (nel legacy il nome era concatenato nel testo)
- **Descrizione**: nome del destinatario al momento dell'invio (per l'audit e i "reinvia").
- **Tipo**: `varchar(160)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: snapshot.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 200.884 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### from_address_snapshot
- **Legacy**: `email_logs.emailFrom`
- **Descrizione**: mittente effettivo usato (dal canale email della property o `noreply@summerbooking.it`).
- **Tipo**: `varchar(320)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (canali non-email).
- **Relazioni**: coerente con [`notification_email_channel_settings`](notification_email_channel_settings.md)
  al momento dell'invio.

### template_id
- **Legacy**: `custom_templates.id` (in `sendReservationSummery`) · template EJS da disco
- **Descrizione**: template usato per il rendering (quando l'invio parte da un template di catalogo).
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (messaggi renderizzati da risorse `.resx` di sistema — la maggioranza dei
  transazionali).
- **Relazioni**: **FK interna** → [`notification_templates.id`](notification_templates.md) `ON DELETE SET NULL`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 200.884 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### subject_snapshot
- **Legacy**: `email_logs.subject`
- **Descrizione**: oggetto **renderizzato** effettivamente spedito (email/push title).
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (WhatsApp/SMS senza oggetto).
- **Relazioni**: —.

### rendered_body
- **Legacy**: `email_logs.body` (`text`, HTML completo, nessun cap)
- **Descrizione**: corpo **renderizzato** consegnato (HTML per email, testo per WhatsApp/SMS/push).
  Tenuto per audit/ricerca; **troncato a `NULL` dopo 150 giorni** ✅ (valore validato dal titolare,
  [`docs/82-retention-data.md` §3.6](../../../progettazione/82-retention-data.md) — la proposta era 90 gg)
  ([`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md),
  job `TruncateEmailLogBodies`).
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: natura congelata (artefatto consegnato); la retention lo azzera prima della riga.

### has_attachments
- **Legacy**: `email_logs.attachments` (JSON incoerente)
- **Descrizione**: `true` se il messaggio consegnato aveva allegati. Gli allegati **appartengono
  all'entità di origine** (template → [`notification_template_attachments`](notification_template_attachments.md);
  scontrino → `fiskaly_media_assets`; report → C2), non a questa riga.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### status
- **Legacy**: `email_logs.status` (`0`/`1`) · `*_sent` booleani
- **Descrizione**: stato del ciclo di consegna.
- **Tipo**: `enum notification.delivery_status` — valori: `queued` (creata, in coda), `sending`
  (tentativo in corso), `sent` (accettata dal provider), `delivered` (conferma di recapito, se il
  provider la fornisce), `failed` (errore terminale dopo `max_attempts` o errore permanente),
  `bounced` (respinta dal server destinatario), `skipped` (non inviata per regola: opt-out,
  soppressione, indirizzo mancante, ambiente non-prod).
- **Default**: `queued`.
- **Nullabile**: No.
- **Relazioni**: `skipped` porta `skip_reason`; `failed`/`bounced` portano `failure_reason`.

### attempt_count / max_attempts
- **Legacy**: — (il legacy ritentava all'infinito ad ogni tick)
- **Descrizione**: numero di tentativi già eseguiti / massimo previsto per questo `kind`+`channel`.
- **Tipo**: `smallint` ciascuno.
- **Default**: `0` / `5`.
- **Nullabile**: No / No.
- **Relazioni**: `attempt_count >= max_attempts` ⇒ transizione a `failed`.

### next_attempt_at
- **Legacy**: — (implicito: "subito, sempre")
- **Descrizione**: quando il worker può (ri)provare. Calcolato con backoff esponenziale + jitter.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: Sì (`NULL` quando lo stato è terminale).
- **Relazioni**: colonna guida dell'indice parziale del worker.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 200.884 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### provider
- **Legacy**: implicito (`nodemailer` SMTP / Twilio / FCM)
- **Descrizione**: sistema esterno usato per la consegna.
- **Tipo**: `enum notification.delivery_provider` — valori: `smtp`, `ses`, `twilio_whatsapp`,
  `twilio_sms`, `fcm`, `apns`, `web_push`, `internal`.
- **Default**: `smtp`.
- **Nullabile**: No.
- **Relazioni**: —.

### provider_message_id
- **Legacy**: — (nessun id provider salvato)
- **Descrizione**: id restituito dal provider (SES `MessageId`, Twilio `SID`, FCM message name) — per
  correlare i webhook di stato/bounce.
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `UNIQUE` parziale opzionale in fase implementativa.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 200.884 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### idempotency_key
- **Legacy**: — (nessuna deduplica; i cron reinviavano)
- **Descrizione**: chiave calcolata da `(subject_type, subject_public_id, kind, channel, recipient)` (o
  passata dal comando di origine) per non creare due invii identici se l'evento viene riprocessato.
- **Tipo**: `varchar(200)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `UNIQUE` parziale.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 200.884 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### failure_reason / skip_reason
- **Legacy**: — (i fallimenti erano `console.error`)
- **Descrizione**: dettaglio testuale dell'errore terminale / motivo di `skipped`
  (`opted_out`, `suppressed`, `missing_address`, `non_prod_env`, `hard_bounce_history`,
  `consent_not_verified`).
- **Tipo**: `text` / `enum notification.skip_reason`.
- **Default**: `NULL` / `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 200.884 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### queued_at / sent_at / delivered_at / failed_at
- **Legacy**: `email_logs.createdAt` (unico timestamp)
- **Descrizione**: marche temporali delle transizioni di stato.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `queued_at` = `now()`; gli altri `NULL`.
- **Nullabile**: `queued_at` No; gli altri Sì.
- **Relazioni**: `sent_at` è la decorrenza retention ([`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md)).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 200.884 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `email_logs.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / ultima transizione. `deleted_at` esposto per uniformità: questa è una
  tabella di **registro** — le righe non si cancellano a mano, le rimuove la retention (12 mesi per i
  metadati, hard-delete).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

**Nessun `jsonb` di dominio.** `email_logs.attachments` (3-4 formati incoerenti) → sostituito da
`has_attachments` + gli allegati sull'entità di origine. Gli array email `recipientList` / `recipients`
dei cron marketing → **una riga per destinatario** qui (per gli invii) e in
[`notification_marketing_campaign_recipients`](notification_marketing_campaign_recipients.md) (per lo
snapshot). L'eventuale payload provider grezzo (webhook di bounce/stato) **non** si conserva in tabella:
va all'observability/tracing.

## Suggerimenti per le prestazioni

- Tabella ad **alta scrittura** ([`docs/03 §3.9`](../../../progettazione/03-gestione-dati.md)): set di indici
  **minimo**. L'indice del worker è **parziale** su `status IN ('queued','sending')` → resta piccolo
  anche con milioni di righe storiche.
- **Partizionare per mese di `created_at`**; la retention (`body` a **150 gg**, riga a 12 mesi) tiene calde
  poche partizioni.
- Le viste "campagna X: quanti consegnati / falliti" usano l'indice parziale
  `(subject_type, subject_public_id, status)`; nessun `GROUP BY` sull'array `recipientList` a runtime
  (era il pattern legacy).
- `rendered_body` in colonna `text` fuori riga (TOAST): non pesa sulle scansioni per stato.
- Il worker fa `SELECT … FOR UPDATE SKIP LOCKED` sulle righe `queued`/`sending` con
  `next_attempt_at <= now()` — nessuna contesa fra istanze ([`docs/20-jobs.md`](../../../progettazione/20-jobs.md)).

## Implementazione .NET

`Delivery` è l'**aggregate root** del modulo `Module.Notification` (schema `notification`): rappresenta
l'outbox/audit unico per destinatario. Espone `public_id` (referenziato da gestionale ed export GDPR) e
solleva eventi di dominio sulle transizioni di stato (`DeliverySentDomainEvent`, `DeliveryFailedDomainEvent`,
…) consumati da worker/osservabilità. `template_id` è l'unica FK reale, verso l'entità figlia
`NotificationTemplate` dello stesso schema; tutti gli altri riferimenti (`property_id`,
`subject_public_id`, `recipient_customer_public_id`, `recipient_user_public_id`) sono id copiati da altri
moduli, senza FK.

### Enum propri

```csharp
// Domain/Enums/NotificationChannel.cs
public enum NotificationChannel { Email = 0, Whatsapp = 1, Sms = 2, Push = 3, InApp = 4 }

// Domain/Enums/DeliveryKind.cs
public enum DeliveryKind
{
	BookingConfirmation = 0, ReservationSummary = 1, PaymentLink = 2, PaymentReceipt = 3,
	EReceipt = 4, NonFiscalReceipt = 5, AgreementReport = 6, ResaleCreditEarned = 7,
	WalletStatement = 8, LifeguardAttendance = 9, ForgotPassword = 10, EmailVerification = 11,
	AccountLifecycle = 12, Newsletter = 13, MarketingCampaign = 14, MarketingAutomation = 15,
	EstablishmentClaim = 16, Test = 17, Other = 18
}

// Domain/Enums/NotificationSubjectType.cs
public enum NotificationSubjectType
{
	BookingReservation = 0, ServiceReservation = 1, PaymentRequestLink = 2, PaymentTransaction = 3,
	FiskalyReceipt = 4, AgreementReport = 5, ResaleOffer = 6, WalletTransaction = 7,
	MarketingCampaign = 8, MarketingAutomation = 9, MarketingEvent = 10, NewsletterSubscription = 11,
	UserAccount = 12, None = 13
}

// Domain/Enums/RecipientType.cs
public enum RecipientType
{
	Customer = 0, StaffUser = 1, PropertyContact = 2, HotelPartner = 3, PlatformAdmin = 4,
	ExternalAddress = 5
}

// Domain/Enums/DeliveryStatus.cs
public enum DeliveryStatus { Queued = 0, Sending = 1, Sent = 2, Delivered = 3, Failed = 4, Bounced = 5, Skipped = 6 }

// Domain/Enums/DeliveryProvider.cs
public enum DeliveryProvider { Smtp = 0, Ses = 1, TwilioWhatsapp = 2, TwilioSms = 3, Fcm = 4, Apns = 5, WebPush = 6, Internal = 7 }

// Domain/Enums/SkipReason.cs
public enum SkipReason { OptedOut = 0, Suppressed = 1, MissingAddress = 2, NonProdEnv = 3, HardBounceHistory = 4, ConsentNotVerified = 5 }
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<NotificationChannel>("notification", "channel");
modelBuilder.HasPostgresEnum<DeliveryKind>("notification", "delivery_kind");
modelBuilder.HasPostgresEnum<NotificationSubjectType>("notification", "subject_type");
modelBuilder.HasPostgresEnum<RecipientType>("notification", "recipient_type");
modelBuilder.HasPostgresEnum<DeliveryStatus>("notification", "delivery_status");
modelBuilder.HasPostgresEnum<DeliveryProvider>("notification", "delivery_provider");
modelBuilder.HasPostgresEnum<SkipReason>("notification", "skip_reason");
```

```csharp
// Module.cs / Infrastructure DI — registrazione data source Npgsql
dataSourceBuilder.MapEnum<NotificationChannel>("notification.channel");
dataSourceBuilder.MapEnum<DeliveryKind>("notification.delivery_kind");
dataSourceBuilder.MapEnum<NotificationSubjectType>("notification.subject_type");
dataSourceBuilder.MapEnum<RecipientType>("notification.recipient_type");
dataSourceBuilder.MapEnum<DeliveryStatus>("notification.delivery_status");
dataSourceBuilder.MapEnum<DeliveryProvider>("notification.delivery_provider");
dataSourceBuilder.MapEnum<SkipReason>("notification.skip_reason");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class DeliveryConfiguration : IEntityTypeConfiguration<Delivery>
{
	public void Configure(EntityTypeBuilder<Delivery> builder)
	{
		builder.ToTable("deliveries", "notification", t => t.HasComment(
			"Outbox + audit unico per destinatario: una riga = un messaggio verso un singolo destinatario su un singolo canale."));

		builder.HasKey(x => x.Id).HasName("pk_notification_deliveries");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile usato nel gestionale e nell'export GDPR.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_notification_deliveries_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema. NULL solo per invii di piattaforma non legati a uno stabilimento.");

		builder.Property(x => x.Channel)
			.HasColumnName("channel")
			.HasDefaultValue(NotificationChannel.Email)
			.IsRequired()
			.HasComment("Mezzo di consegna (email/whatsapp/sms/push/in_app); un invio è su un solo canale.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasDefaultValue(DeliveryKind.Other)
			.IsRequired()
			.HasComment("Scopo funzionale del messaggio: determina template, priorità, retention e regole anti-spam.");

		builder.Property(x => x.SubjectType)
			.HasColumnName("subject_type")
			.HasDefaultValue(NotificationSubjectType.None)
			.IsRequired()
			.HasComment("Tipo dell'entità che ha originato l'invio.");

		builder.Property(x => x.SubjectPublicId)
			.HasColumnName("subject_public_id")
			.HasComment("Id pubblico dell'entità di origine, copiato per valore — nessuna FK cross-modulo.");

		builder.Property(x => x.RecipientType)
			.HasColumnName("recipient_type")
			.HasDefaultValue(RecipientType.Customer)
			.IsRequired()
			.HasComment("Categoria del destinatario (cliente, utente gestionale, contatto stabilimento, partner, admin, indirizzo libero).");

		builder.Property(x => x.RecipientCustomerPublicId)
			.HasColumnName("recipient_customer_public_id")
			.HasComment("public_id di identity_customer.customers, copiato — nessuna FK cross-schema. Usato per export/cancellazione GDPR.");

		builder.Property(x => x.RecipientUserPublicId)
			.HasColumnName("recipient_user_public_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.RecipientAddressSnapshot)
			.HasColumnName("recipient_address_snapshot")
			.HasMaxLength(320)
			.HasComment("Indirizzo di consegna congelato all'invio: email, numero E.164 o token push abbreviato.");

		builder.Property(x => x.RecipientNameSnapshot)
			.HasColumnName("recipient_name_snapshot")
			.HasMaxLength(160)
			.HasComment("Nome del destinatario al momento dell'invio, per audit e reinvio.");

		builder.Property(x => x.FromAddressSnapshot)
			.HasColumnName("from_address_snapshot")
			.HasMaxLength(320)
			.HasComment("Mittente effettivo usato (canale email della property o noreply@summerbooking.it).");

		builder.Property(x => x.TemplateId)
			.HasColumnName("template_id")
			.HasComment("Template di catalogo usato per il rendering, quando presente.");

		builder.Property(x => x.SubjectSnapshot)
			.HasColumnName("subject_snapshot")
			.HasMaxLength(255)
			.HasComment("Oggetto renderizzato effettivamente spedito (email/push title).");

		builder.Property(x => x.RenderedBody)
			.HasColumnName("rendered_body")
			.HasColumnType("text")
			.HasComment("Corpo renderizzato consegnato, tenuto per audit/ricerca; troncato a NULL dopo 90 giorni dal job TruncateEmailLogBodies.");

		builder.Property(x => x.HasAttachments)
			.HasColumnName("has_attachments")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("True se il messaggio consegnato aveva allegati; gli allegati appartengono all'entità di origine, non a questa riga.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasDefaultValue(DeliveryStatus.Queued)
			.IsRequired()
			.HasComment("Stato del ciclo di consegna: queued → sending → sent → delivered | failed | bounced | skipped.");

		builder.Property(x => x.AttemptCount)
			.HasColumnName("attempt_count")
			.HasColumnType("smallint")
			.HasDefaultValue((short)0)
			.IsRequired()
			.HasComment("Numero di tentativi di invio già eseguiti.");

		builder.Property(x => x.MaxAttempts)
			.HasColumnName("max_attempts")
			.HasColumnType("smallint")
			.HasDefaultValue((short)5)
			.IsRequired()
			.HasComment("Numero massimo di tentativi previsti per questo kind+channel.");

		builder.Property(x => x.NextAttemptAt)
			.HasColumnName("next_attempt_at")
			.HasDefaultValueSql("now()")
			.HasComment("Istante del prossimo tentativo del worker, calcolato con backoff esponenziale + jitter; NULL quando lo stato è terminale.");

		builder.Property(x => x.Provider)
			.HasColumnName("provider")
			.HasDefaultValue(DeliveryProvider.Smtp)
			.IsRequired()
			.HasComment("Sistema esterno usato per la consegna.");

		builder.Property(x => x.ProviderMessageId)
			.HasColumnName("provider_message_id")
			.HasMaxLength(255)
			.HasComment("Id restituito dal provider (SES MessageId, Twilio SID, FCM message name), per correlare i webhook di stato/bounce.");

		builder.Property(x => x.IdempotencyKey)
			.HasColumnName("idempotency_key")
			.HasMaxLength(200)
			.HasComment("Chiave calcolata da (subject_type, subject_public_id, kind, channel, recipient) per evitare invii duplicati.");

		builder.Property(x => x.FailureReason)
			.HasColumnName("failure_reason")
			.HasColumnType("text")
			.HasComment("Dettaglio testuale dell'errore terminale (failed/bounced).");

		builder.Property(x => x.SkipReason)
			.HasColumnName("skip_reason")
			.HasComment("Motivo dello stato skipped (opt-out, soppressione, indirizzo mancante, ambiente non-prod, consenso non verificato).");

		builder.Property(x => x.QueuedAt)
			.HasColumnName("queued_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Istante di creazione della riga in coda (transazione del comando di origine).");

		builder.Property(x => x.SentAt)
			.HasColumnName("sent_at")
			.HasComment("Istante di accettazione da parte del provider; decorrenza della retention del corpo (150 giorni).");

		builder.Property(x => x.DeliveredAt)
			.HasColumnName("delivered_at")
			.HasComment("Istante di conferma di recapito, quando il provider la fornisce.");

		builder.Property(x => x.FailedAt)
			.HasColumnName("failed_at")
			.HasComment("Istante di transizione allo stato terminale failed/bounced.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
			.HasComment("Data dell'ultima transizione di stato.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Esposto per uniformità: tabella di registro, le righe non si cancellano a mano — le rimuove la retention (12 mesi).");

		builder.HasOne(x => x.Template)
			.WithMany()
			.HasForeignKey(x => x.TemplateId)
			.HasConstraintName("fk_notification_deliveries_template")
			.OnDelete(DeleteBehavior.SetNull);

		builder.HasIndex(x => x.NextAttemptAt)
			.HasFilter("status IN ('queued','sending')")
			.HasDatabaseName("ix_notification_deliveries_pending");

		builder.HasIndex(x => new { x.PropertyId, x.CreatedAt })
			.HasDatabaseName("ix_notification_deliveries_property_created");

		builder.HasIndex(x => new { x.SubjectType, x.SubjectPublicId })
			.HasDatabaseName("ix_notification_deliveries_subject");

		builder.HasIndex(x => new { x.RecipientCustomerPublicId, x.CreatedAt })
			.HasFilter("recipient_customer_public_id IS NOT NULL")
			.HasDatabaseName("ix_notification_deliveries_recipient_customer");

		builder.HasIndex(x => new { x.SubjectType, x.SubjectPublicId, x.Status })
			.HasFilter("subject_type IN ('marketing_campaign','marketing_automation')")
			.HasDatabaseName("ix_notification_deliveries_campaign");

		builder.HasIndex(x => x.IdempotencyKey)
			.IsUnique()
			.HasFilter("idempotency_key IS NOT NULL")
			.HasDatabaseName("ix_notification_deliveries_dedup");
	}
}
```

`Delivery` implementa `IAuditable` (`created_at`/`updated_at`) ma **non** `ISoftDeletable` in senso pieno:
`deleted_at` esiste per uniformità di schema ma la cancellazione logica non è il percorso applicativo
normale (ci pensa il job di retention a 12 mesi, hard-delete) — nessun Global Query Filter da applicare
qui se il job opera direttamente in SQL fuori da EF; se invece il job passa da EF, applicarlo come sulle
altre entità.

### Note di migrazione

- `email_logs` (~335 k righe, solo percorso felice) → righe `sent`; nessun fallimento storico da
  inventare (`status = failed`/`bounced` esiste solo per i nuovi invii).
- `body` (`email_logs.body`) importato solo per gli ultimi 90 giorni rispetto alla data di migrazione;
  righe più vecchie con `rendered_body = NULL` fin da subito, coerente con la retention.

> **Nota sulla finestra di 150 giorni** — non è un valore tondo per caso: il titolare ha esteso la
> proposta di 90 giorni ([`docs/82-retention-data.md` §3.6](../../../progettazione/82-retention-data.md)). Sono
> ~5 mesi, quindi il corpo di un'email inviata a inizio stagione resta leggibile **fino a fine
> stagione** — l'orizzonte entro cui arriva una contestazione del cliente ("non ho mai ricevuto la
> conferma"). A 90 giorni il corpo di giugno sarebbe già sparito a settembre, con la stagione ancora
> aperta. Se questa finestra cambia, si cambia la riga di policy in
> [`privacy_retention_policies`](../privacy/privacy_retention_policies.md), **non** questo commento né
> il partizionamento.
- `emailType` → split in `kind` (categoria da mappa statica) + `subject_snapshot` (oggetto reale).
- `propertyId` sempre `NULL` in produzione → `property_id` va **dedotto** dall'entità di origine
  (`bookings_send_payment_summaries.reservation.propertyId`, `fiskaly_receipt.propertyId`, …) durante
  l'ETL, non copiato direttamente.
- Flag `*Sent` booleani (`bookings_send_payment_summaries.email_sent`, `fiskaly_receipt.emailSent`/
  `whatsappSent`, `sharing_details.email_sent`) → una riga `sent` con `channel`/`subject_type`/
  `subject_public_id` valorizzati e `sent_at = updatedAt` dell'entità di origine (best-effort, nessun
  timestamp reale disponibile); sulla tabella di origine il flag **non** si migra (viene eliminato).
- `cronStatus` (colonna morta su `email_logs`) → non migrata.
- `idempotency_key` e `provider_message_id` nascono `NULL` per lo storico migrato (non calcolabili a
  posteriori); valorizzati solo per gli invii nuovi.
