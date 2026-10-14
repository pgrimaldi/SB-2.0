# Tabella `notification_marketing_automations`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` · **schema**: `notification` · **nome
> flat**: `notification_marketing_automations` · **forma canonica**:
> `notification.marketing_automations`.
>
> Sostituisce [`marketing_automation`](../../../analisi-pre/db/marketing_automation.md) (221 righe in
> prod; `name` `Goodbye` 102 / `Welcome` 93 / `''` 26; `state` `scheduled` 186). Fonti:
> [`analisi-pre/db/marketing_automation.md`](../../../analisi-pre/db/marketing_automation.md),
> [`docs/03-gestione-dati.md` §2.4quater](../../../progettazione/03-gestione-dati.md),
> [`docs/01-…backend.md` §8](../../../progettazione/01-criticita-e-indicazioni-backend.md),
> [`docs/20-jobs.md`](../../../progettazione/20-jobs.md).

---

## Descrizione

**Una riga = un'automazione marketing**: email (+ push opzionale) inviata ai contatti di una lista in
relazione alle loro prenotazioni — `before` (N giorni prima dell'arrivo), `after` (N giorni dopo la
partenza), `custom` (trigger multipli: `of_booking` / `before_booking` / `after_booking` /
`pending_payments`). I trigger `custom` sono nella figlia
[`notification_marketing_automation_triggers`](notification_marketing_automation_triggers.md).

Correzioni rispetto al legacy:

- **`name` `varchar` con `switch` per stringa** (`name.toLowerCase() === "welcome"`) → `automation_kind`
  enum (`welcome`/`goodbye`/`custom`). Nota: nel legacy Welcome/Goodbye sono **create ma escluse per
  nome dal cron** → di fatto morte; nel target o si completano o si dismettono ([D-47](../_domande-committente.md)).
- **`state ENUM('draft','sent','scheduled','deleted')`** (ordine diverso da campagne) → enum
  **unificato** con le campagne + `deleted_at`. Il legacy non portava mai a `sent` (update commentato)
  → un'automazione **ricorrente** non ha uno stato "sent" terminale: `active` / `paused`.
- **`scheduleValue` JSON con doppio significato** (`{days}` vs array di `{type,time,days}`) →
  `schedule_type` + `days_offset` + figlia `notification_marketing_automation_triggers`.
- **`textFields` (`@token`) + `subject` + `message`** → `template_id` FK (`template_kind = automation`).
- **`recipients` JSON snapshot** invece della lista → destinatari risolti dalla lista all'esecuzione;
  snapshot per-run in `notification_marketing_campaign_recipients` (`parent_kind = automation`).
- **N query SQL per destinatario ogni 5 minuti** (`SELECT * FROM bookings WHERE consumer_email = …`,
  SQL injection) ([`docs/01 §8`](../../../progettazione/01-criticita-e-indicazioni-backend.md)) → il modulo
  Marketing reagisce a **eventi di dominio** C1 (arrivo/partenza/pagamento pendente) o legge una
  proiezione, non fa query raw sulle tabelle di C1.
- **`enableAppNotifications`** → `also_send_push` (booleano).

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `automation_kind` | **enum esclusivo**: `welcome` / `goodbye` / `custom`. |
| `state` | **enum esclusivo**: `draft` / `active` / `paused` / `canceled` (`deleted` → `deleted_at`). |
| `schedule_type` | **enum esclusivo**: `before` / `after` / `custom`. Con `custom` i trigger sono **più righe** in figlia (uno per `{type}` dell'array `scheduleValue`) — **non** `enum[]`. |
| `also_send_push` | booleano indipendente. |

## Snapshot

Nessuno (i valori congelati per-run sono in `notification_marketing_campaign_recipients`).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_marketing_automations` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | API/URL (`automations/:id`) |
| `ix_..._active` | `(property_id, schedule_type) WHERE state = 'active' AND deleted_at IS NULL` | btree parziale | selezione delle automazioni da valutare per un evento |

## Campi

### id
- **Legacy**: `marketing_automation.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata (FK interna) da
  `notification_marketing_automation_triggers.automation_id`,
  `notification_marketing_campaign_recipients.parent_id` (con `parent_kind = automation`).

### public_id
- **Legacy**: — (nuovo)
- **Tipo**: `uuid` (UUIDv7).
- **Default**: generato.
- **Nullabile**: No.
- **Relazioni**: `UNIQUE`.

### property_id
- **Legacy**: `marketing_automation.propertyId`
- **Descrizione**: stabilimento proprietario.
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2; [D-01](../_domande-committente.md).

### name
- **Legacy**: `marketing_automation.name` (`NOT NULL`, `""` dalle UI)
- **Descrizione**: nome interno dell'automazione.
- **Tipo**: `varchar(160)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### automation_kind
- **Legacy**: `marketing_automation.name` usato in `switch` (`welcome` / `goodbye` / altro)
- **Descrizione**: comportamento dell'automazione.
- **Tipo**: `enum notification.automation_kind` — valori: `welcome`, `goodbye`, `custom`.
- **Default**: `custom`.
- **Nullabile**: No.
- **Relazioni**: `welcome`/`goodbye` sono le auto-generate (oggi inerti — [D-47](../_domande-committente.md)).

### state
- **Legacy**: `marketing_automation.state` (`ENUM`, mai portato a `sent`)
- **Descrizione**: stato dell'automazione ricorrente.
- **Tipo**: `enum notification.automation_state` — valori: `draft`, `active`, `paused`, `canceled`.
- **Default**: `draft`.
- **Nullabile**: No.
- **Relazioni**: `deleted` legacy → `deleted_at`; `scheduled` legacy → `active`.

### schedule_type
- **Legacy**: `marketing_automation.scheduleType` (`ENUM('before','after','custom')`)
- **Descrizione**: strategia di pianificazione.
- **Tipo**: `enum notification.automation_schedule_type` — valori: `before`, `after`, `custom`.
- **Default**: `before`.
- **Nullabile**: No.
- **Relazioni**: `before`/`after` usano `days_offset` + `send_at_time`; `custom` usa la figlia trigger.

### days_offset
- **Legacy**: `scheduleValue[0].days`
- **Descrizione**: giorni prima dell'arrivo (`before`) o dopo la partenza (`after`).
- **Tipo**: `smallint`.
- **Default**: `1`.
- **Nullabile**: Sì (`NULL` per `schedule_type = custom`).
- **Relazioni**: `CHECK (days_offset >= 0)` (il legacy accettava `0` → loop infinito con `custom`).

### send_at_time
- **Legacy**: `scheduleValue[].time` (`"HH:mm"` stringa)
- **Descrizione**: ora locale di invio nel giorno calcolato.
- **Tipo**: `time`.
- **Default**: `'09:00'`.
- **Nullabile**: Sì.
- **Relazioni**: interpretata nel fuso `timezone`.

### timezone
- **Legacy**: `marketing_automation.timezone` (IANA, `DEFAULT 'UTC'`)
- **Descrizione**: fuso per il calcolo di `send_at_time`.
- **Tipo**: `varchar(64)`.
- **Default**: `'Europe/Rome'`.
- **Nullabile**: No.
- **Relazioni**: identificatore IANA (non enum).

### subject
- **Legacy**: `marketing_automation.subject`
- **Descrizione**: oggetto dell'email (e `title` della push).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### template_id
- **Legacy**: `marketing_automation.textFields` / `message`
- **Descrizione**: template del corpo (`template_kind = automation`).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`notification_templates.id`](notification_templates.md) `ON DELETE RESTRICT`.

### contact_list_id
- **Legacy**: `marketing_automation.contactListId` (FK reale, `ON DELETE SET NULL`; l'invio usava `recipients`)
- **Descrizione**: segmento destinatario, risolto all'esecuzione.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK interna** → [`notification_marketing_contact_lists.id`](notification_marketing_contact_lists.md) `ON DELETE SET NULL`.

### also_send_push
- **Legacy**: `marketing_automation.enableAppNotifications`
- **Descrizione**: se inviare anche una push FCM ai destinatari utenti C5 con device token.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: push = riga `notification_deliveries` separata (`channel = push`).

### last_run_at / total_sent
- **Legacy**: — (nessun contatore; `state` mai avanzato)
- **Descrizione**: ultimo tick in cui l'automazione ha prodotto invii / totale storico messaggi
  spediti.
- **Tipo**: `timestamptz` · `integer`.
- **Default**: `NULL` · `0`.
- **Nullabile**: Sì / No.
- **Relazioni**: `last_run_at` guida la retention (24 mesi dall'ultimo invio).
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 221 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_by_user_id
- **Legacy**: — (route senza auth)
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 221 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: `createdAt` / `updatedAt` (`timestamps: false`) / `state = 'deleted'`
- **Descrizione**: creazione / modifica / soft-delete.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun `jsonb`. `scheduleValue` (JSON polimorfo) → `schedule_type` + `days_offset` + `send_at_time` +
figlia trigger. `recipients` → righe in `notification_marketing_campaign_recipients`. `textFields` →
`template_id`. `attachments` (`[]` in prod) → allegati del template.

## Suggerimenti per le prestazioni

- ~221 righe. Nessuna scansione periodica di tutte le tabelle di C1: il modulo reagisce a **eventi**
  (`GuestArrivingIn`, `GuestDeparted`, `PaymentOverdue`) o a una proiezione dedicata.
- L'indice parziale `(property_id, schedule_type) WHERE state = 'active'` seleziona rapidamente le
  automazioni candidate per un evento.
- Anti-doppione: idempotenza sull'outbox (`idempotency_key` da `(automation_id, reservation, day)`),
  non una finestra di 300 s come il legacy.

## Implementazione .NET

`MarketingAutomation` è l'**aggregate root** dell'automazione ricorrente, modulo `Module.Notification`,
sottoarea `Marketing`, schema `notification`. Radice per i trigger custom
([`AutomationTrigger`](notification_marketing_automation_triggers.md)) e per le righe destinatario
prodotte a ogni esecuzione (`CampaignRecipient`, `parent_kind = automation`, entità gestita da
[`notification_marketing_campaign_recipients`](notification_marketing_campaign_recipients.md)).

### Enum propri

```csharp
// Domain/Enums/AutomationKind.cs
public enum AutomationKind
{
	Welcome = 0,
	Goodbye = 1,
	Custom = 2
}

// Domain/Enums/AutomationState.cs
public enum AutomationState
{
	Draft = 0,
	Active = 1,
	Paused = 2,
	Canceled = 3
}

// Domain/Enums/AutomationScheduleType.cs
public enum AutomationScheduleType
{
	Before = 0,
	After = 1,
	Custom = 2
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<AutomationKind>("notification", "automation_kind");
modelBuilder.HasPostgresEnum<AutomationState>("notification", "automation_state");
modelBuilder.HasPostgresEnum<AutomationScheduleType>("notification", "automation_schedule_type");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<AutomationKind>("notification.automation_kind");
dataSourceBuilder.MapEnum<AutomationState>("notification.automation_state");
dataSourceBuilder.MapEnum<AutomationScheduleType>("notification.automation_schedule_type");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class MarketingAutomationConfiguration : IEntityTypeConfiguration<MarketingAutomation>
{
	public void Configure(EntityTypeBuilder<MarketingAutomation> builder)
	{
		builder.ToTable("marketing_automations", "notification", t => t.HasComment(
			"Automazione marketing ricorrente: email (+ push opzionale) inviata ai contatti di una lista in relazione alle loro prenotazioni."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_automations");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile (API/URL).");
		builder.HasIndex(x => x.PublicId).IsUnique()
			.HasDatabaseName("uq_notification_marketing_automations_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(160)
			.HasComment("Nome interno dell'automazione.");

		builder.Property(x => x.AutomationKind)
			.HasColumnName("automation_kind")
			.IsRequired()
			.HasDefaultValue(AutomationKind.Custom)
			.HasComment("Comportamento dell'automazione: welcome, goodbye o custom.");

		builder.Property(x => x.State)
			.HasColumnName("state")
			.IsRequired()
			.HasDefaultValue(AutomationState.Draft)
			.HasComment("Stato dell'automazione ricorrente.");

		builder.Property(x => x.ScheduleType)
			.HasColumnName("schedule_type")
			.IsRequired()
			.HasDefaultValue(AutomationScheduleType.Before)
			.HasComment("Strategia di pianificazione: prima dell'arrivo, dopo la partenza, o trigger custom.");

		builder.Property(x => x.DaysOffset)
			.HasColumnName("days_offset")
			.HasDefaultValue((short)1)
			.HasComment("Giorni prima dell'arrivo (before) o dopo la partenza (after); NULL per schedule_type custom.");

		builder.Property(x => x.SendAtTime)
			.HasColumnName("send_at_time")
			.HasColumnType("time")
			.HasDefaultValue(new TimeOnly(9, 0))
			.HasComment("Ora locale di invio nel giorno calcolato, interpretata nel fuso di Timezone.");

		builder.Property(x => x.Timezone)
			.HasColumnName("timezone")
			.HasMaxLength(64)
			.IsRequired()
			.HasDefaultValue("Europe/Rome")
			.HasComment("Fuso IANA per il calcolo di send_at_time.");

		builder.Property(x => x.Subject)
			.HasColumnName("subject")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Oggetto dell'email (e titolo della push, se also_send_push).");

		builder.Property(x => x.TemplateId)
			.HasColumnName("template_id")
			.IsRequired()
			.HasComment("Template del corpo (template_kind = automation).");

		builder.Property(x => x.ContactListId)
			.HasColumnName("contact_list_id")
			.HasComment("Segmento destinatario, risolto all'esecuzione.");

		builder.Property(x => x.AlsoSendPush)
			.HasColumnName("also_send_push")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se inviare anche una push FCM ai destinatari utenti C5 con device token.");

		builder.Property(x => x.LastRunAt)
			.HasColumnName("last_run_at")
			.HasComment("Ultimo tick in cui l'automazione ha prodotto invii; guida la retention.");

		builder.Property(x => x.TotalSent)
			.HasColumnName("total_sent")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Totale storico messaggi spediti da questa automazione.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access.users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione dell'automazione.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete.");

		builder.HasIndex(x => new { x.PropertyId, x.ScheduleType })
			.HasFilter("state = 'active' AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_automations_active");

		builder.HasOne(x => x.Template)
			.WithMany()
			.HasForeignKey(x => x.TemplateId)
			.HasConstraintName("fk_notification_marketing_automations_template")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.ContactList)
			.WithMany()
			.HasForeignKey(x => x.ContactListId)
			.HasConstraintName("fk_notification_marketing_automations_contact_list")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

### Relazioni

- `TemplateId` → FK reale verso `notification_templates` (stesso schema), `ON DELETE RESTRICT` (non si
  può cancellare un template ancora usato da un'automazione).
- `ContactListId` → FK reale verso [`notification_marketing_contact_lists`](notification_marketing_contact_lists.md)
  (stesso schema), `ON DELETE SET NULL`.
- `Id` referenziato da `AutomationTrigger.AutomationId` (collezione `Triggers`, FK reale configurata
  sul lato figlio) e, per valore (`parent_kind = automation`, nessuna FK EF), da `CampaignRecipient.ParentId`.
- `PropertyId`, `CreatedByUserId`: id copiati da altri moduli (C2, C5), nessuna FK.

### Note

- `MarketingAutomation` implementa `ISoftDeletable` e `IAuditable`; è `AggregateRoot` (solleva
  `AutomationRunTriggered`, `AutomationStateChanged`, ecc.).
- `SendAtTime` mappato a `TimeOnly` (Npgsql mappa nativamente `time` → `TimeOnly` in .NET 10/Npgsql
  recenti; nessun converter manuale necessario).
- Nessuna scansione periodica delle tabelle C1: il modulo reagisce a eventi di dominio (`GuestArrivingIn`,
  `GuestDeparted`, `PaymentOverdue`) o legge una proiezione dedicata; l'`idempotency_key` sull'outbox è
  derivata da `(automation_id, reservation_id, day)` per evitare doppioni.
- Welcome/Goodbye: i trigger `on_contact_added` / `on_last_booking_ended` sono da implementare o la
  sotto-feature va dismessa — decisione pendente ([D-47](../_domande-committente.md)); non modellati
  qui in attesa di decisione.
- **Migrazione**: 221 righe da `marketing_automation`; `name` (`switch` su stringa) →
  `AutomationKind` (`Welcome`/`Goodbye`/`Custom`); `state` mappato (`scheduled`→`Active`,
  `deleted`→`DeletedAt` valorizzato); `scheduleType` → `ScheduleType`; per `before`/`after`,
  `scheduleValue[0].days` → `DaysOffset`, `.time` → `SendAtTime`; per `custom`, l'array `{type,...}` →
  righe in `notification_marketing_automation_triggers`; `textFields`/`message` → template
  `automation` dedicato; `enableAppNotifications` → `AlsoSendPush`.
