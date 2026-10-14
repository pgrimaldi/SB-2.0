# Tabella `switchboard_messaging_wa_event_logs`

> **Dominio**: C7 — `switchboard` · **sottoarea**: `messaging` · **schema**: `switchboard` · **nome
> flat**: `switchboard_messaging_wa_event_logs` · **forma canonica**:
> `switchboard.messaging_wa_event_logs`.
>
> **Redesign** di [`switchboard_wa_msg_log`](../../../analisi-pre/db/switchboard_wa_msg_log.md)
> (0 righe su `local_sb`/dump di produzione — ma **verificato 2026-09-11**: 11 righe reali su
> `summer_booking_dev`, **tutte** `event_type = "status"`, **zero** `"message_in"`; **nessun lettore
> applicativo** confermato anche lì — il commento del model: *"useful for forensics"*). **Tabella di
> puro log**: espone `deleted_at` per uniformità ma non lo usa.
>
> **Nota di perimetro.** [`docs/03 §2.4quinquies`](../../../progettazione/03-gestione-dati.md) indica che se
> serve tracciare la messaggistica WhatsApp va fatto nell'**outbox del modulo Notification (C6)** con
> contratto stabile, **non** in questa tabella-log grezza del centralino. È stata comunque creata
> **come tabella C7 su decisione del committente** (2026-09-10), come inbox forensica degli eventi
> webhook Meta **specifici del centralino** (stati di consegna dei template `send_whatsapp`, messaggi
> in arrivo dei clienti sul numero del centralino), con retention e pseudonimizzazione. Non è una
> dipendenza di runtime.
>
> **Fonti**: [`analisi-pre/db/switchboard_wa_msg_log.md`](../../../analisi-pre/db/switchboard_wa_msg_log.md),
> [`docs/03 §2.4quinquies / §2.4quater`](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.7 (6–12 mesi) / §4](../../../progettazione/80-retention.md),
> [`docs/11-modulo-privacy.md` §4 / §5 / §"Domande aperte" 1](../../../progettazione/11-modulo-privacy.md),
> [`docs/01-criticita-e-indicazioni-backend.md` §9](../../../progettazione/01-criticita-e-indicazioni-backend.md).
>
> **Aggiornamento 2026-09-17 (D-50, risolta) — retention DECISA dal titolare, sostituisce i "6-12 mesi
> poi 🔒 del payload" sotto**: **6 mesi**, azione a scadenza **cancella direttamente** — il titolare ha
> esplicitamente **saltato la fase di pseudonimizzazione** intermedia proposta, preferendo un job più
> semplice a due stati (hard-delete diretto, non pseudonimizza-poi-cancella) —
> [`docs/05-ottimizzazioni-db.md` §1](../../../progettazione/05-ottimizzazioni-db.md),
> [`docs/82-retention-data.md` §3.7](../../../progettazione/82-retention-data.md). Le colonne/menzioni di
> pseudonimizzazione sotto restano valide come **meccanismo disponibile** (es. se richiesto da un
> futuro obbligo), ma il job di produzione a 6 mesi fa hard-delete, non pseudonimizzazione.

---

## Descrizione

**Una riga = un evento del webhook WhatsApp** Meta Cloud API ricevuto sul numero del centralino:
`status` (aggiornamento di consegna: sent/delivered/read/failed di un nostro invio) **o** `message_in`
(messaggio in arrivo da un utente finale).

Correzioni rispetto al legacy:

- **Webhook POST senza verifica firma** `X-Hub-Signature-256` (pur avendo `WHATSAPP_APP_SECRET`:
  chiunque poteva iniettare righe forgiate) → **verifica obbligatoria della firma** prima
  dell'`INSERT` ([`docs/01 §9`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- **Nessun collegamento** a config/property/prenotazione → `+ config_id` risolto da `phone_number_id`
  (mapping numero → centralino), `+ property_id` denormalizzato. **Limite noto** (osservato realmente
  su `summer_booking_dev`, non solo teorico): quando lo stabilimento **non** ha un
  `whatsapp_phone_number_id` custom (come nel caso reale osservato — tutte le 11 righe reali sono sul
  numero WhatsApp **generico** di piattaforma, condiviso), `phone_number_id` **da solo non basta** a
  risalire a un singolo centralino: più stabilimenti senza numero custom condividono lo stesso
  `phone_number_id`. `config_id` resta quindi `NULL` per questi eventi finché non si introduce una
  correlazione aggiuntiva (es. via `wamid` verso l'invio in outbox) → [D-53](../_domande-committente.md).
- **`message_body` mescola concetti** (testo puro, payload bottone, dump JSON media → colonna non
  interrogabile) → `body_text` (solo testo/etichetta) + `body_payload jsonb` (per bottoni/interactive/
  media, passthrough).
- **PII senza retention** (numeri, nomi, testo, payload grezzo) → **pseudonimizzazione** di
  `from` / `recipient_id` alla scrittura + retention **6–12 mesi** poi 🔒 del payload / 🗑️
  ([`docs/80 §3.7`](../../../progettazione/80-retention.md)).
- **Insert best-effort** (`try/catch` + `console.error`, risposta sempre `200`) → scrittura con
  metrica di scarto; idempotenza su `(wamid, event_type, status)`.
- **`event_type` / `status` / `conversation_origin` / `pricing_category` / `message_type`** stringhe
  libere → `enum` (passthrough Meta con fallback `unknown` dove il vocabolario evolve).
- **Non ripulita da `deleteConfig`** → job `PurgeDiagnosticLogs` mensile.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `event_type` | **enum esclusivo** — `status` **o** `message_in`. |
| `status` | **enum esclusivo** (nullable) — un solo stato di consegna per riga `status` (`sent`/`delivered`/`read`/`failed`). |
| `conversation_origin` · `pricing_category` | **enum esclusivi** (vocabolari Meta; `pricing` evolve → `unknown` di fallback). |
| `message_type` | **enum esclusivo** con `unknown` di fallback (passthrough Meta). |
| `pricing_billable` | `boolean` nullable; nessun gruppo di flag → nessun `enum[]`. |

## Snapshot

Non applicabile (log grezzo di eventi esterni).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_switchboard_messaging_wa_event_logs` | `id` | PK | — |
| `uq_..._event` | `(wamid, event_type, status) NULLS NOT DISTINCT WHERE wamid IS NOT NULL` | UNIQUE parziale | idempotenza del webhook (Meta ripete gli eventi) ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): `coalesce(status,'')` non si può scrivere su una colonna enum; `NULLS NOT DISTINCT` dà la stessa garanzia (due eventi con `status` NULL collidono). L'idempotenza del webhook ora è garantita dal DB. |
| `ix_..._config_time` | `(config_id, received_at DESC)` | — | eventi recenti di un centralino |
| `ix_..._wamid` | `(wamid)` | — | correlazione con l'invio d'origine |
| `ix_..._status` | `(config_id, event_type, status) WHERE event_type = 'status'` | parziale | monitor delle consegne fallite |
| `ix_..._purge` | `(received_at)` | — | `PurgeDiagnosticLogs` (6–12 mesi) |

## Campi

### id
- **Legacy**: `switchboard_wa_msg_log.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### config_id
- **Legacy**: — (il log legacy non si collegava alla config; solo `phone_number_id`)
- **Descrizione**: centralino risolto da `phone_number_id` / `display_phone_number`. `NULL` se il
  numero non è mappato a un centralino noto.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`switchboard_configs`](switchboard_configs.md)`.id`, `ON DELETE SET NULL`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 11 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### property_id
- **Legacy**: —
- **Descrizione**: stabilimento (denormalizzato).
- **Tipo**: `uuid` (= `public_id` di [`catalog_properties`](../catalog/catalog_properties.md)).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: riferimento cross-modulo per valore.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 11 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### event_type
- **Legacy**: `switchboard_wa_msg_log.event_type` (`varchar(20) NOT NULL`)
- **Descrizione**: `status` (da `value.statuses[]`) o `message_in` (da `value.messages[]`).
- **Tipo**: `enum switchboard.wa_webhook_event_type` — valori: `status`, `message_in`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### wamid
- **Legacy**: `switchboard_wa_msg_log.wamid` (`varchar(255)`)
- **Descrizione**: id del messaggio Meta (`st.id` per gli status — combacia con l'id restituito
  all'invio; `m.id` per gli inbound).
- **Tipo**: `varchar(255)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: `ix_..._wamid`; correlabile con la `Delivery` C6 d'origine se l'invio passa
  dall'outbox.

### waba_id / phone_number_id / display_phone_number
- **Legacy**: `switchboard_wa_msg_log.waba_id` / `.phone_number_id` / `.display_phone_number` (`varchar(50)`)
- **Descrizione**: id del WABA (`entry.id`), id e numero leggibile del numero Meta che ha
  inviato/ricevuto (`value.metadata`). `phone_number_id` è la chiave per risolvere `config_id`.
- **Tipo**: `varchar(64)` / `varchar(64)` / `varchar(30)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### counterparty_number
- **Legacy**: `switchboard_wa_msg_log.from` (`message_in`) / `.recipient_id` (`status`) (`varchar(50)`)
- **Descrizione**: numero dell'utente finale (mittente per `message_in`, destinatario per `status`),
  **pseudonimizzato** alla scrittura ([`docs/11 §4`](../../../progettazione/11-modulo-privacy.md)).
- **Tipo**: `varchar(64)` (valore pseudonimizzato).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### contact_name
- **Legacy**: `switchboard_wa_msg_log.contact_name` (`varchar(100)`)
- **Descrizione**: nome profilo del contatto (`contact.profile.name`). PII → pseudonimizzabile a fine
  retention.
- **Tipo**: `varchar(100)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### status
- **Legacy**: `switchboard_wa_msg_log.status` (`varchar(20)`)
- **Descrizione**: stato di consegna (solo `event_type = 'status'`).
- **Tipo**: `enum switchboard.wa_message_status` — valori: `sent`, `delivered`, `read`, `failed`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### error_code / error_title / error_message
- **Legacy**: `switchboard_wa_msg_log.error_code` (`int`) / `.error_title` (`varchar(255)`) / `.error_message` (`text`)
- **Descrizione**: dettaglio errore Meta del primo elemento di `st.errors[]` (quando `status = failed`).
- **Tipo**: `integer` / `varchar(255)` / `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### conversation_id / conversation_origin
- **Legacy**: `switchboard_wa_msg_log.conversation_id` (`varchar(100)`) / `.conversation_origin` (`varchar(50)`)
- **Descrizione**: id della conversazione ai fini fatturazione Meta (`st.conversation.id`) e sua
  origine/categoria.
- **Tipo**: `varchar(100)` / `enum switchboard.wa_conversation_origin` — valori: `marketing`,
  `utility`, `service`, `authentication`, `referral_conversion`, `unknown`.
- **Default**: `NULL` / `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: —.

### pricing_billable / pricing_category / pricing_model
- **Legacy**: `switchboard_wa_msg_log.pricing_billable` (`tinyint(1)`) / `.pricing_category` (`varchar(50)`) / `.pricing_model` (`varchar(50)`)
- **Descrizione**: se la conversazione è fatturabile, categoria e modello di pricing Meta.
- **Tipo**: `boolean` / `enum switchboard.wa_pricing_category` (`marketing`, `utility`,
  `authentication`, `service`, `unknown`) / `varchar(20)` (`CBP` / `PMP` — grezzo Meta).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: **verificato 2026-09-11** su `summer_booking_dev` — tutte le righe con pricing valorizzato
  osservano `pricing_model = "PMP"` (mai `"CBP"`), coerente con la transizione di Meta al *per-message
  pricing* (2025); `pricing_category` osservata: sempre `"utility"`.

### message_type
- **Legacy**: `switchboard_wa_msg_log.message_type` (`varchar(30)`)
- **Descrizione**: tipo del messaggio in arrivo (solo `event_type = 'message_in'`).
- **Tipo**: `enum switchboard.wa_inbound_message_type` — valori: `text`, `button`, `interactive`,
  `reaction`, `image`, `document`, `audio`, `video`, `location`, `sticker`, `contacts`, `unknown`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### body_text
- **Legacy**: parte di `switchboard_wa_msg_log.message_body` (`text`)
- **Descrizione**: **solo** la parte testuale del messaggio in arrivo: `text.body`, `button.text`,
  titolo di `button_reply`/`list_reply`, emoji+riferimento per `reaction`. Per i media/interattivi
  complessi → `NULL` (il contenuto sta in `body_payload`).
- **Tipo**: `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 11 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### body_payload
- **Legacy**: parte di `switchboard_wa_msg_log.message_body` (dump `JSON.stringify` per media/interactive)
- **Descrizione**: payload strutturato per messaggi non testuali (media reference, interactive,
  location, contacts) — passthrough Meta.
- **Tipo**: `jsonb`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 11 righe storiche, con un **segnaposto dichiarato** nella mapping spec. Il motivo si legge nella riga «Legacy» qui sopra: se indica una fonte, il dato **esiste** nel legacy e la conversione non è stata fatta (tipicamente un parsing rinviato); se indica «—», non c'è nulla da migrare e la colonna va popolata dal nuovo backend. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### raw_payload
- **Legacy**: `switchboard_wa_msg_log.raw_payload` (`json`)
- **Descrizione**: oggetto `st` (status) o `m` (inbound) completo — **non** l'intero webhook. Con
  numeri pseudonimizzati. Omissibile/troncabile via config.
- **Tipo**: `jsonb`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### meta_timestamp
- **Legacy**: `switchboard_wa_msg_log.meta_timestamp` (`datetime`)
- **Descrizione**: istante dell'evento lato Meta (`st.timestamp` / `m.timestamp`, Unix → `Date`).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### received_at
- **Legacy**: `switchboard_wa_msg_log.received_at` (`datetime NOT NULL`)
- **Descrizione**: istante in cui il nostro server ha ricevuto il webhook.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: `ix_..._purge`.

### created_at / updated_at / deleted_at
- **Legacy**: `switchboard_wa_msg_log` — `timestamps: false` (solo `received_at`)
- **Descrizione**: `created_at` = istante di persistenza. `updated_at` / `deleted_at` esposti per
  uniformità ma **non usati** (righe immutabili, cancellate in blocco a 6–12 mesi; prima 🔒 del
  payload).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: —.

## Valutazione JSON

- **`raw_payload` / `body_payload` restano `jsonb`**: payload di un sistema esterno (Meta) di puro log
  ([`docs/03 §3.6 punto 5`](../../../progettazione/03-gestione-dati.md); i `raw_payload` di puro log **non
  andrebbero migrati affatto** — qui conservati per scelta, con retention breve).
- **`message_body` legacy → scomposto**: la parte testuale interrogabile va in `body_text`, il resto
  (dump JSON di media/interattivi) in `body_payload jsonb`. Elimina la colonna "non interrogabile in
  modo affidabile" del legacy.
- Tutte le dimensioni (`event_type`, `status`, `*_category`, `message_type`) → `enum` con `unknown`
  di fallback per i vocabolari Meta che evolvono.

## Suggerimenti per le prestazioni

- **Append-only**, scritture dal solo webhook. Idempotenza su `(wamid, event_type, status)` evita i
  duplicati dei retry Meta.
- Tabella **partizionata per `received_at`** (mensile) → purge a 6–12 mesi = `DROP PARTITION`.
- Preferibile comunque che gli **stati di consegna** dei nostri invii vivano nell'outbox C6
  (`notification_deliveries`, correlazione via `wamid`): questa tabella è forense, non il canale di
  verità dello stato d'invio.
- Nessun indice "largo" oltre a correlazione (`wamid`), monitor consegne fallite e purge.

## Implementazione .NET

`MessagingWaEventLog` è un'**entità figlia write-only** (puro log forense, nessun lettore applicativo
confermato — commento legacy: *"useful for forensics"*) del modulo `Module.Switchboard`, area
`Messaging`: un evento del webhook WhatsApp Meta ricevuto sul numero del centralino. Come
[`switchboard_voice_call_diagnostics`](switchboard_voice_call_diagnostics.md), non implementa
`ISoftDeletable`/`IAuditable` pieni — `updated_at`/`deleted_at` sono esposti per uniformità ma non usati,
le righe sono immutabili e cancellate in blocco dal job di retention. Creata come tabella C7 su
decisione del committente (2026-09-10) nonostante
[`docs/03 §2.4quinquies`](../../../progettazione/03-gestione-dati.md) indichi l'outbox C6 come sede naturale
del tracciamento WhatsApp. 0 righe su `local_sb`/dump di produzione, ma **11 righe reali** su
`summer_booking_dev` (tutte `event_type = status`, zero `message_in`).

### Enum

```csharp
// Module.Switchboard.Domain.Enums.Messaging/WaWebhookEventType.cs
public enum WaWebhookEventType
{
	Status = 0,
	MessageIn = 1
}

// Module.Switchboard.Domain.Enums.Messaging/WaMessageStatus.cs
public enum WaMessageStatus
{
	Sent = 0,
	Delivered = 1,
	Read = 2,
	Failed = 3
}

// Module.Switchboard.Domain.Enums.Messaging/WaConversationOrigin.cs
public enum WaConversationOrigin
{
	Marketing = 0,
	Utility = 1,
	Service = 2,
	Authentication = 3,
	ReferralConversion = 4,
	Unknown = 5
}

// Module.Switchboard.Domain.Enums.Messaging/WaPricingCategory.cs
public enum WaPricingCategory
{
	Marketing = 0,
	Utility = 1,
	Authentication = 2,
	Service = 3,
	Unknown = 4
}

// Module.Switchboard.Domain.Enums.Messaging/WaInboundMessageType.cs
public enum WaInboundMessageType
{
	Text = 0,
	Button = 1,
	Interactive = 2,
	Reaction = 3,
	Image = 4,
	Document = 5,
	Audio = 6,
	Video = 7,
	Location = 8,
	Sticker = 9,
	Contacts = 10,
	Unknown = 11
}
```

```csharp
// SwitchboardDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<WaWebhookEventType>("switchboard", "wa_webhook_event_type");
modelBuilder.HasPostgresEnum<WaMessageStatus>("switchboard", "wa_message_status");
modelBuilder.HasPostgresEnum<WaConversationOrigin>("switchboard", "wa_conversation_origin");
modelBuilder.HasPostgresEnum<WaPricingCategory>("switchboard", "wa_pricing_category");
modelBuilder.HasPostgresEnum<WaInboundMessageType>("switchboard", "wa_inbound_message_type");
```

```csharp
// Module.Switchboard.Infrastructure DI
dataSourceBuilder.MapEnum<WaWebhookEventType>("switchboard.wa_webhook_event_type");
dataSourceBuilder.MapEnum<WaMessageStatus>("switchboard.wa_message_status");
dataSourceBuilder.MapEnum<WaConversationOrigin>("switchboard.wa_conversation_origin");
dataSourceBuilder.MapEnum<WaPricingCategory>("switchboard.wa_pricing_category");
dataSourceBuilder.MapEnum<WaInboundMessageType>("switchboard.wa_inbound_message_type");
```

### Configuration

```csharp
namespace Module.Switchboard.Infrastructure.Configurations.Messaging;

public sealed class MessagingWaEventLogConfiguration : IEntityTypeConfiguration<MessagingWaEventLog>
{
	public void Configure(EntityTypeBuilder<MessagingWaEventLog> builder)
	{
		builder.ToTable("switchboard_messaging_wa_event_logs", "switchboard", t => t.HasComment(
			"Evento del webhook WhatsApp Meta Cloud API ricevuto sul numero del centralino: aggiornamento di consegna o messaggio in arrivo. Log forense write-only."));

		builder.HasKey(x => x.Id).HasName("pk_switchboard_messaging_wa_event_logs");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ConfigId)
			.HasColumnName("config_id")
			.HasComment("Centralino risolto da phone_number_id/display_phone_number; NULL se il numero non è mappato a un centralino noto.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema; denormalizzato quando risolvibile.");

		builder.Property(x => x.EventType)
			.HasColumnName("event_type")
			.HasColumnType("switchboard.wa_webhook_event_type")
			.IsRequired()
			.HasComment("status (da value.statuses[]) o message_in (da value.messages[]).");

		builder.Property(x => x.Wamid)
			.HasColumnName("wamid")
			.HasMaxLength(255)
			.HasComment("Id del messaggio Meta: st.id per gli status, m.id per gli inbound.");

		builder.Property(x => x.WabaId)
			.HasColumnName("waba_id")
			.HasMaxLength(64)
			.HasComment("Id del WhatsApp Business Account (entry.id).");

		builder.Property(x => x.PhoneNumberId)
			.HasColumnName("phone_number_id")
			.HasMaxLength(64)
			.HasComment("Id del numero Meta che ha inviato/ricevuto (value.metadata); chiave per risolvere config_id.");

		builder.Property(x => x.DisplayPhoneNumber)
			.HasColumnName("display_phone_number")
			.HasMaxLength(30)
			.HasComment("Numero leggibile del numero Meta (value.metadata).");

		builder.Property(x => x.CounterpartyNumber)
			.HasColumnName("counterparty_number")
			.HasMaxLength(64)
			.HasComment("Numero dell'utente finale (mittente per message_in, destinatario per status), pseudonimizzato alla scrittura.");

		builder.Property(x => x.ContactName)
			.HasColumnName("contact_name")
			.HasMaxLength(100)
			.HasComment("Nome profilo del contatto (contact.profile.name); PII, pseudonimizzabile a fine retention.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.HasColumnType("switchboard.wa_message_status")
			.HasComment("Stato di consegna, valorizzato solo per event_type = status.");

		builder.Property(x => x.ErrorCode)
			.HasColumnName("error_code")
			.HasComment("Codice errore Meta del primo elemento di st.errors[] (status = failed).");

		builder.Property(x => x.ErrorTitle)
			.HasColumnName("error_title")
			.HasMaxLength(255)
			.HasComment("Titolo errore Meta del primo elemento di st.errors[].");

		builder.Property(x => x.ErrorMessage)
			.HasColumnName("error_message")
			.HasColumnType("text")
			.HasComment("Messaggio di dettaglio dell'errore Meta.");

		builder.Property(x => x.ConversationId)
			.HasColumnName("conversation_id")
			.HasMaxLength(100)
			.HasComment("Id della conversazione ai fini fatturazione Meta (st.conversation.id).");

		builder.Property(x => x.ConversationOrigin)
			.HasColumnName("conversation_origin")
			.HasColumnType("switchboard.wa_conversation_origin")
			.HasComment("Origine/categoria della conversazione (passthrough Meta, unknown di fallback).");

		builder.Property(x => x.PricingBillable)
			.HasColumnName("pricing_billable")
			.HasComment("Se la conversazione è fatturabile.");

		builder.Property(x => x.PricingCategory)
			.HasColumnName("pricing_category")
			.HasColumnType("switchboard.wa_pricing_category")
			.HasComment("Categoria di pricing Meta (passthrough, unknown di fallback).");

		builder.Property(x => x.PricingModel)
			.HasColumnName("pricing_model")
			.HasMaxLength(20)
			.HasComment("Modello di pricing Meta grezzo (CBP/PMP).");

		builder.Property(x => x.MessageType)
			.HasColumnName("message_type")
			.HasColumnType("switchboard.wa_inbound_message_type")
			.HasComment("Tipo del messaggio in arrivo, valorizzato solo per event_type = message_in.");

		builder.Property(x => x.BodyText)
			.HasColumnName("body_text")
			.HasColumnType("text")
			.HasComment("Parte testuale del messaggio in arrivo (text.body, button.text, titolo reply, emoji di reaction); NULL per media/interattivi complessi.");

		builder.Property(x => x.BodyPayload)
			.HasColumnName("body_payload")
			.HasColumnType("jsonb")
			.HasComment("Payload strutturato per messaggi non testuali (media, interactive, location, contacts), passthrough Meta.");

		builder.Property(x => x.RawPayload)
			.HasColumnName("raw_payload")
			.HasColumnType("jsonb")
			.HasComment("Oggetto st/m completo (non l'intero webhook), con numeri pseudonimizzati.");

		builder.Property(x => x.MetaTimestamp)
			.HasColumnName("meta_timestamp")
			.HasComment("Istante dell'evento lato Meta (st.timestamp/m.timestamp).");

		builder.Property(x => x.ReceivedAt)
			.HasColumnName("received_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Istante in cui il nostro server ha ricevuto il webhook.");

		builder.HasIndex(x => new { x.Wamid, x.EventType, x.Status })
			.IsUnique()
			.HasFilter("wamid IS NOT NULL")
			.HasDatabaseName("uq_switchboard_messaging_wa_event_logs_event");

		builder.HasIndex(x => new { x.ConfigId, x.ReceivedAt })
			.HasDatabaseName("ix_switchboard_messaging_wa_event_logs_config_time");

		builder.HasIndex(x => x.Wamid)
			.HasDatabaseName("ix_switchboard_messaging_wa_event_logs_wamid");

		builder.HasIndex(x => new { x.ConfigId, x.EventType, x.Status })
			.HasFilter("event_type = 'status'")
			.HasDatabaseName("ix_switchboard_messaging_wa_event_logs_status");

		builder.HasIndex(x => x.ReceivedAt)
			.HasDatabaseName("ix_switchboard_messaging_wa_event_logs_purge");

		builder.HasOne(x => x.Config)
			.WithMany()
			.HasForeignKey(x => x.ConfigId)
			.HasConstraintName("fk_switchboard_messaging_wa_event_logs_config")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

`uq_switchboard_messaging_wa_event_logs_event` traduce l'indice logico
`(wamid, event_type, coalesce(status,''))` della scheda: EF/Npgsql mappa la tupla `(Wamid, EventType,
Status)` con `Status` nullable, ma l'idempotenza voluta (due righe con lo stesso `wamid`/`event_type` e
`status` entrambi `NULL` non devono considerarsi distinte) richiede `NULLS NOT DISTINCT` sull'indice
(Postgres 15+) o l'espressione `coalesce(status, '')` esplicita in migration — non esprimibile 1:1 con
`HasIndex` su proprietà semplici, da rifinire in migration.

### Note di migrazione

- Handler webhook: verifica **obbligatoria** di `X-Hub-Signature-256` prima dell'`INSERT` (il legacy
  aveva `WHATSAPP_APP_SECRET` ma non verificava — chiunque poteva iniettare righe forgiate,
  [`docs/01 §9`](../../../progettazione/01-criticita-e-indicazioni-backend.md)); risoluzione di `config_id` da
  `phone_number_id`; pseudonimizzazione di `counterparty_number`/`contact_name` **prima** della
  scrittura; idempotenza su `(wamid, event_type, status)`.
- **Limite noto, osservato realmente**: quando lo stabilimento non ha un `whatsapp_phone_number_id`
  custom (come tutte le 11 righe reali di `summer_booking_dev`, sul numero generico di piattaforma
  condiviso), `phone_number_id` da solo non basta a risalire a un singolo centralino — `config_id` resta
  `NULL` per questi eventi finché non si introduce una correlazione aggiuntiva (es. via `wamid` verso
  l'invio in outbox) → [D-53](../_domande-committente.md).
- Correlazione `wamid` → `notification_deliveries` (C6) per aggiornare lo stato d'invio reale: questa
  tabella resta forense, non il canale di verità dello stato d'invio.
- Job `PurgeDiagnosticLogs` (mensile): `DROP PARTITION` oltre la finestra di 6–12 mesi, con 🔒 del
  payload prima della cancellazione definitiva.
- 0 righe su `local_sb`/dump di produzione → nessun backfill da quelle copie; **11 righe reali** su
  `summer_booking_dev` (tutte `status`, nessuna `message_in`) migrabili 1:1 se quell'ambiente va portato
  al nuovo schema.
