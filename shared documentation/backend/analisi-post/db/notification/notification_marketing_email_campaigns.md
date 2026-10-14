# Tabella `notification_marketing_email_campaigns`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` · **schema**: `notification` · **nome
> flat**: `notification_marketing_email_campaigns` · **forma canonica**:
> `notification.marketing_email_campaigns`.
>
> Sostituisce [`marketing_email_campaign`](../../../analisi-pre/db/marketing_email_campaign.md) (25
> righe in prod, 6 `propertyId`; `state` `sent` 19 / `deleted` 5 / `draft` 1). Fonti:
> [`analisi-pre/db/marketing_email_campaign.md`](../../../analisi-pre/db/marketing_email_campaign.md),
> [`docs/03-gestione-dati.md` §2.4quater](../../../progettazione/03-gestione-dati.md),
> [`docs/01-…backend.md` §8](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/12-lingue-e-date.md`](../../../progettazione/12-lingue-e-date.md),
> [`docs/20-jobs.md`](../../../progettazione/20-jobs.md).

---

## Descrizione

**Una riga = una campagna email one-shot** verso un segmento: oggetto + corpo (template) + allegati,
inviata subito o a data/ora pianificata. Il worker la spedisce quando `scheduled_at` (UTC) è
raggiunta e la porta a `sent`, materializzando una riga in
[`notification_marketing_campaign_recipients`](notification_marketing_campaign_recipients.md) per
destinatario e una in [`notification_deliveries`](notification_deliveries.md) per invio.

Correzioni rispetto al legacy:

- **`state ENUM('draft','scheduled','sent','deleted')`** → `state` enum **unificato** con le
  automazioni (`draft`/`scheduled`/`sending`/`sent`/`canceled`) + `deleted_at` separato dal
  soft-delete.
- **`recipientList` JSON snapshot** (i destinatari veri, `contactListId` decorativo) → i destinatari
  si **risolvono dalla lista al momento dell'invio** (con verifica consenso); lo snapshot per-invio è
  in `notification_marketing_campaign_recipients` (retention 6 mesi).
- **`message` inline + rendering EJS da disco** → `template_id` FK a
  [`notification_templates`](notification_templates.md) (`template_kind = campaign`); corpo ad-hoc →
  template inline auto-creato.
- **`name NOT NULL` sempre `""`** → `name` nullable.
- **`scheduledDate` (UTC) + `scheduledTime` (stringa libera ridondante)** con doppia conversione fuso
  in `updateCampaign` → un solo `scheduled_at timestamptz` + `timezone` (IANA) per ricostruire il form.
- **`enablePushNotifications` / `sendCopy`** → `also_send_push` / `send_copy_to_manager` (booleani,
  concetti distinti — non enum).
- **invio sincrono e sequenziale, nessun esito per destinatario** ([`docs/01 §8`](../../../progettazione/01-criticita-e-indicazioni-backend.md))
  → coda/worker + `notification_deliveries` per destinatario.
- **SQL injection** (`getCampaignById` `id` interpolato; query manager di `sendCopy`) → annotato,
  fuori scope schema.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `state` | **enum esclusivo**: `draft` / `scheduled` / `sending` / `sent` / `canceled` (`deleted` → `deleted_at`). Unificato con `notification_marketing_automations.state`. |
| `also_send_push` / `send_copy_to_manager` | **booleani indipendenti** (push FCM al destinatario ≠ copia al manager). |

## Snapshot

Lo snapshot dei destinatari è nella figlia `notification_marketing_campaign_recipients` (`email_snapshot`).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_marketing_email_campaigns` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | API/URL (`campaigns/:id`) |
| `ix_..._due` | `(scheduled_at) WHERE state = 'scheduled' AND deleted_at IS NULL` | btree parziale | il worker prende le campagne dovute (il legacy scansionava tutte le `scheduled` ogni minuto senza indice) |
| `ix_..._property_state` | `(property_id, state, created_at DESC)` | btree | elenco campagne per stabilimento |

## Campi

### id
- **Legacy**: `marketing_email_campaign.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da `notification_marketing_campaign_recipients.campaign_id`.

### public_id
- **Legacy**: — (nuovo; usato nell'URL gestionale)
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `marketing_email_campaign.propertyId`
- **Descrizione**: stabilimento proprietario.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2; [D-01](../_domande-committente.md).

### name
- **Legacy**: `marketing_email_campaign.name` (`NOT NULL`, sempre `""`)
- **Descrizione**: nome interno della campagna.
- **Tipo**: `varchar(160)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### subject
- **Legacy**: `marketing_email_campaign.subject`
- **Descrizione**: oggetto dell'email (anche `title` della push se `also_send_push`).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: chiave di ricerca in elenco.

### template_id
- **Legacy**: `marketing_email_campaign.message` (corpo inline, template EJS globale)
- **Descrizione**: template del corpo (`template_kind = campaign`). Un corpo ad-hoc genera un template
  inline dedicato alla campagna.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`notification_templates.id`](notification_templates.md) `ON DELETE RESTRICT`.

### contact_list_id
- **Legacy**: `marketing_email_campaign.contactListId` (FK reale, `ON DELETE SET NULL`; ma l'invio usava `recipientList`)
- **Descrizione**: segmento destinatario; risolto **all'invio** (non snapshot).
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`ON DELETE SET NULL` — se la lista è cancellata, restano lo snapshot destinatari
  e l'esito).
- **Relazioni**: **FK interna** → [`notification_marketing_contact_lists.id`](notification_marketing_contact_lists.md) `ON DELETE SET NULL`.

### state
- **Legacy**: `marketing_email_campaign.state` (`ENUM('draft','scheduled','sent','deleted')`)
- **Descrizione**: stato del ciclo di vita.
- **Tipo**: `enum notification.campaign_state` — valori: `draft`, `scheduled`, `sending`, `sent`, `canceled`.
- **Default**: `draft`.
- **Nullabile**: No.
- **Relazioni**: `deleted` legacy → `deleted_at` (con `state` congelato allo stato precedente).

### scheduled_at
- **Legacy**: `marketing_email_campaign.scheduledDate` (UTC) + `scheduledTime` (stringa)
- **Descrizione**: istante di invio programmato (assoluto, `timestamptz`).
- **Tipo**: `timestamptz`.
- **Default**: `NULL` (bozza / invio immediato).
- **Nullabile**: Sì.
- **Relazioni**: `state = scheduled` ⇒ `scheduled_at` non `NULL`.

### timezone
- **Legacy**: `marketing_email_campaign.timezone` (IANA, `DEFAULT 'UTC'`)
- **Descrizione**: fuso del gestore, per ricostruire il form (data+ora locali). Non usato per la
  schedulazione (che usa `scheduled_at` assoluto).
- **Tipo**: `varchar(64)`.
- **Default**: `'Europe/Rome'`.
- **Nullabile**: No.
- **Relazioni**: identificatore IANA (non enum).

### also_send_push
- **Legacy**: `marketing_email_campaign.enablePushNotifications`
- **Descrizione**: se, dopo l'email, inviare anche una push FCM ai destinatari che sono utenti C5
  attivi con device token.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: la push è una **riga `notification_deliveries` separata** (`channel = push`).

### send_copy_to_manager
- **Legacy**: `marketing_email_campaign.sendCopy`
- **Descrizione**: se inviare una copia del messaggio al manager della property.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: destinatario risolto via C5 (ruolo manager), non via query raw.

### recipients_total / delivered_count / failed_count
- **Legacy**: — (nessun contatore; l'esito era solo `console.error`)
- **Descrizione**: contatori di esito, aggiornati per evento da `notification_deliveries`.
- **Tipo**: `integer` ciascuno.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: riconciliabili con `COUNT` sull'indice parziale di `notification_deliveries`.

### created_by_user_id
- **Legacy**: — (route senza auth)
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 25 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### sent_at
- **Legacy**: dedotto da `updatedAt` al passaggio `state = sent`
- **Descrizione**: quando l'invio è stato completato.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: decorrenza retention definizione (24 mesi dall'ultimo invio) e snapshot destinatari
  (6 mesi) — [`docs/80-retention.md` §3.6](../../../progettazione/80-retention.md).

### created_at / updated_at / deleted_at
- **Legacy**: `createdAt` / `updatedAt` (Sequelize, `timestamps: false` → non aggiornato) / `state = 'deleted'`
- **Descrizione**: creazione / modifica / soft-delete.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun `jsonb`. `recipientList` (array email snapshot) → righe in
`notification_marketing_campaign_recipients`. `attachments` (`[{filename,path}]`) → allegati del
`template_id` ([`notification_template_attachments`](notification_template_attachments.md)).
`message` inline → `template_id`.

## Suggerimenti per le prestazioni

- Volume minimo (~25 righe). L'indice parziale `(scheduled_at) WHERE state = 'scheduled'` sostituisce
  la scansione al minuto di tutte le campagne ([`docs/01 §8`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- L'invio è **asincrono in coda** ([`docs/20-jobs.md`](../../../progettazione/20-jobs.md)): il worker non blocca
  il web server; il template compilato è in cache.
- I contatori denormalizzati evitano `GROUP BY` sull'outbox per la vista "risultati campagna".

## Implementazione .NET

`EmailCampaign` è l'**aggregate root** della campagna one-shot, modulo `Module.Notification`, sottoarea
`Marketing`, schema `notification`. Radice per le righe destinatario prodotte all'invio
(`CampaignRecipient`, `parent_kind = campaign`, entità gestita da
[`notification_marketing_campaign_recipients`](notification_marketing_campaign_recipients.md)).

### Enum proprio

```csharp
// Domain/Enums/CampaignState.cs
public enum CampaignState
{
	Draft = 0,
	Scheduled = 1,
	Sending = 2,
	Sent = 3,
	Canceled = 4
}
```

> Nota: pur nascendo dalla stessa correzione concettuale di `marketing_automations.state` (unificare gli
> `ENUM` legacy con ordine diverso), i due campi hanno **valori diversi** (l'automazione è ricorrente:
> `draft`/`active`/`paused`/`canceled`; la campagna è one-shot: `draft`/`scheduled`/`sending`/`sent`/
> `canceled`) e restano **due `ENUM` PG distinti** — `notification.campaign_state` qui,
> `notification.automation_state` in [`notification_marketing_automations`](notification_marketing_automations.md).

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<CampaignState>("notification", "campaign_state");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<CampaignState>("notification.campaign_state");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class EmailCampaignConfiguration : IEntityTypeConfiguration<EmailCampaign>
{
	public void Configure(EntityTypeBuilder<EmailCampaign> builder)
	{
		builder.ToTable("marketing_email_campaigns", "notification", t => t.HasComment(
			"Campagna email one-shot verso un segmento: oggetto + corpo + allegati, inviata subito o pianificata."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_email_campaigns");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile, usato nell'URL del gestionale.");
		builder.HasIndex(x => x.PublicId).IsUnique()
			.HasDatabaseName("uq_notification_marketing_email_campaigns_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(160)
			.HasComment("Nome interno della campagna.");

		builder.Property(x => x.Subject)
			.HasColumnName("subject")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Oggetto dell'email (e titolo della push se also_send_push).");

		builder.Property(x => x.TemplateId)
			.HasColumnName("template_id")
			.IsRequired()
			.HasComment("Template del corpo (template_kind = campaign); un corpo ad-hoc genera un template inline dedicato.");

		builder.Property(x => x.ContactListId)
			.HasColumnName("contact_list_id")
			.HasComment("Segmento destinatario, risolto all'invio (non snapshot).");

		builder.Property(x => x.State)
			.HasColumnName("state")
			.IsRequired()
			.HasDefaultValue(CampaignState.Draft)
			.HasComment("Stato del ciclo di vita della campagna.");

		builder.Property(x => x.ScheduledAt)
			.HasColumnName("scheduled_at")
			.HasComment("Istante di invio programmato, assoluto; NULL per bozza o invio immediato.");

		builder.Property(x => x.Timezone)
			.HasColumnName("timezone")
			.HasMaxLength(64)
			.IsRequired()
			.HasDefaultValue("Europe/Rome")
			.HasComment("Fuso del gestore, solo per ricostruire il form (non usato per la schedulazione, che usa scheduled_at assoluto).");

		builder.Property(x => x.AlsoSendPush)
			.HasColumnName("also_send_push")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se, dopo l'email, inviare anche una push FCM ai destinatari utenti C5 con device token.");

		builder.Property(x => x.SendCopyToManager)
			.HasColumnName("send_copy_to_manager")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se inviare una copia del messaggio al manager della property.");

		builder.Property(x => x.RecipientsTotal)
			.HasColumnName("recipients_total")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Numero totale di destinatari risolti per questo invio.");

		builder.Property(x => x.DeliveredCount)
			.HasColumnName("delivered_count")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Numero di consegne riuscite, aggiornato per evento da notification_deliveries.");

		builder.Property(x => x.FailedCount)
			.HasColumnName("failed_count")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Numero di consegne fallite, aggiornato per evento da notification_deliveries.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.SentAt)
			.HasColumnName("sent_at")
			.HasComment("Quando l'invio è stato completato; decorrenza retention definizione e snapshot destinatari.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione della campagna.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete.");

		builder.HasIndex(x => x.ScheduledAt)
			.HasFilter("state = 'scheduled' AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_email_campaigns_due");

		builder.HasIndex(x => new { x.PropertyId, x.State, x.CreatedAt })
			.HasDatabaseName("ix_notification_marketing_email_campaigns_property_state");

		builder.HasOne(x => x.Template)
			.WithMany()
			.HasForeignKey(x => x.TemplateId)
			.HasConstraintName("fk_notification_marketing_email_campaigns_template")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.ContactList)
			.WithMany()
			.HasForeignKey(x => x.ContactListId)
			.HasConstraintName("fk_notification_marketing_email_campaigns_contact_list")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

### Relazioni

- `TemplateId` → FK reale verso `notification_templates` (stesso schema), `ON DELETE RESTRICT`.
- `ContactListId` → FK reale verso [`notification_marketing_contact_lists`](notification_marketing_contact_lists.md)
  (stesso schema), `ON DELETE SET NULL` (la cancellazione della lista non rompe la campagna: restano
  snapshot ed esito).
- `Id` referenziato per valore (`parent_kind = campaign`, nessuna FK EF) da `CampaignRecipient.ParentId`.
- `PropertyId`, `CreatedByUserId`: id copiati da altri moduli (C2, C5), nessuna FK.

### Note

- `EmailCampaign` implementa `ISoftDeletable` e `IAuditable`; è `AggregateRoot` (solleva
  `CampaignScheduled`, `CampaignSent`, ecc.).
- `ScheduleCampaign` → `State = Scheduled` + messaggio ritardato in coda (v.
  [`docs/20-jobs.md`](../../../progettazione/20-jobs.md)); il worker `SendCampaign` risolve i destinatari
  (lista + verifica `IConsentGate`/`ISuppressionList`), crea una riga `CampaignRecipient` +
  `Delivery` per ognuno, aggiorna `RecipientsTotal`/`DeliveredCount`/`FailedCount`, porta `State = Sent`
  e valorizza `SentAt`.
- L'indice parziale `ix_..._due` sostituisce la scansione al minuto di tutte le `scheduled` del legacy
  ([`docs/01 §8`](../../../progettazione/01-criticita-e-indicazioni-backend.md)).
- **Migrazione**: 25 righe da `marketing_email_campaign`; `state` mappato (`deleted` →
  `DeletedAt` valorizzato, `State` congelato al valore precedente); `scheduledDate` (UTC) +
  `scheduledTime` (stringa) → `ScheduledAt` unico `timestamptz` (ricalcolato da `Timezone`, evitando il
  doppio shift di fuso del legacy); `message` inline (EJS) → template dedicato; `recipientList` →
  righe in `notification_marketing_campaign_recipients` (`email_snapshot`); `name = ""` → `NULL`.
