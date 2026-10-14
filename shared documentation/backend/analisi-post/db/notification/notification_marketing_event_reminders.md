# Tabella `notification_marketing_event_reminders`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` · **schema**: `notification` · **nome
> flat**: `notification_marketing_event_reminders` · **forma canonica**:
> `notification.marketing_event_reminders`.
>
> Sostituisce [`marketing_event_notification`](../../../analisi-pre/db/marketing_event_notification.md)
> (**0 righe** in dev e in prod; `AUTO_INCREMENT` alto = solo churn `destroy`+`create`). Fonti:
> [`analisi-pre/db/marketing_event_notification.md`](../../../analisi-pre/db/marketing_event_notification.md),
> [`docs/03-gestione-dati.md` §2.4quater / §3.1bis](../../../progettazione/03-gestione-dati.md).
>
> ⚠️ **Feature incompleta nel legacy — nessun mittente**: le righe erano create/distrutte con
> l'evento ma **nulla le inviava** (`status` mai diverso da `pending`). In `_feature-tabelle.md` la
> tabella target è annotata *"(se non dismessa)"*. Creata qui su richiesta, **marcata candidata a
> dismissione / da completare con uno scheduler** ([D-47](../_domande-committente.md)).

---

## Descrizione

**Una riga = un reminder pianificato di un evento marketing**: "1 giorno prima", "3 giorni prima",
"1 settimana prima" o data/ora custom. Alla scadenza (se la feature viene completata) uno scheduler
invia il messaggio ai contatti di [`notification_marketing_events.contact_list_id`](notification_marketing_events.md)
e porta lo stato a `sent` / `failed`.

Correzioni rispetto al legacy:

- **`notificationType` `ENUM('1_day_before','3_days_before','1_week_before','custom')`** → enum PG
  `offset` (`one_day_before` / `three_days_before` / `one_week_before` / `custom`).
- **`status` `ENUM('pending','sent','failed')` mai fatto transitare** → enum + `canceled`; scheduler
  che lo avanza (da progettare).
- **`notificationDate` `datetime` + `notificationTime` `varchar "HH:mm"`** → `scheduled_at`
  `timestamptz` calcolato (o `custom_date` + `custom_time`).
- **Nessuna FK / cascade** verso `marketing_event` (pulizia a mano da `updateEvent`/`deleteEvent`,
  saltata se `notifications: []`) → **FK interna** con `ON DELETE CASCADE`.
- **`updateEvent` va in `TypeError` se `notifications` è `undefined`** → gestito lato applicazione.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `offset` | **enum esclusivo per riga**; più righe per evento (l'array `notifications[]` ne conteneva più d'uno) → **N righe**, non `enum[]`. |
| `status` | **enum esclusivo**: `pending` / `sent` / `failed` / `canceled`. |

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_marketing_event_reminders` | `id` | PK | — |
| `ix_..._due` | `(scheduled_at) WHERE status = 'pending' AND deleted_at IS NULL` | btree parziale | lo scheduler prende i reminder dovuti |
| `ix_..._event` | `(event_id) WHERE deleted_at IS NULL` | btree parziale | reminder di un evento (form) |

## Campi

### id
- **Legacy**: `marketing_event_notification.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### event_id
- **Legacy**: `marketing_event_notification.eventId` (nessuna FK)
- **Descrizione**: evento a cui il reminder appartiene.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`notification_marketing_events.id`](notification_marketing_events.md) `ON DELETE CASCADE`.

### property_id
- **Legacy**: ereditato dall'evento
- **Descrizione**: stabilimento (denormalizzato).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dall'evento.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2.

### offset
- **Legacy**: `marketing_event_notification.notificationType`
- **Descrizione**: anticipo rispetto all'evento.
- **Tipo**: `enum notification.event_reminder_offset` — valori: `one_day_before`, `three_days_before`,
  `one_week_before`, `custom`.
- **Default**: `one_day_before`.
- **Nullabile**: No.
- **Relazioni**: `custom` ⇒ `custom_date` + `custom_time`.

### custom_date / custom_time
- **Legacy**: `notificationDate` / `notificationTime`
- **Descrizione**: data/ora esplicite per `offset = custom`.
- **Tipo**: `date` · `time`.
- **Default**: `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: —.

### scheduled_at
- **Legacy**: — (calcolato a runtime, mai memorizzato)
- **Descrizione**: istante assoluto di invio, calcolato da `offset` (o `custom_*`) rispetto a
  `notification_marketing_events.starts_on` + fuso della property.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì (ricalcolato se l'evento cambia data).
- **Relazioni**: colonna guida dell'indice dello scheduler.

### status
- **Legacy**: `marketing_event_notification.status` (`ENUM`, sempre `pending`)
- **Descrizione**: stato di invio del reminder.
- **Tipo**: `enum notification.reminder_status` — valori: `pending`, `sent`, `failed`, `canceled`.
- **Default**: `pending`.
- **Nullabile**: No.
- **Relazioni**: `sent` genera righe in [`notification_deliveries`](notification_deliveries.md) per
  destinatario.

### sent_at / failure_reason
- **Legacy**: — (mai valorizzati)
- **Descrizione**: esito dell'invio.
- **Tipo**: `timestamptz` · `text`.
- **Default**: `NULL`.
- **Nullabile**: Sì / Sì.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `createdAt` / `updatedAt` (`timestamps: false`) / —
- **Descrizione**: creazione / modifica / soft-delete.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

È la **de-jsonizzazione** dell'array `notifications[]` passato nel payload dell'endpoint evento.

## Suggerimenti per le prestazioni

- 0 righe oggi. Se la feature si completa: indice parziale `(scheduled_at) WHERE status = 'pending'`
  per lo scheduler; volumi bassi.

## Implementazione .NET

`EventReminder` è l'**entità figlia** di `MarketingEvent`, modulo `Module.Notification`, sottoarea
`Marketing`, schema `notification`. Modellata come entità normale (non owned type) con FK reale e
collezione di navigazione `Reminders` sull'aggregate, coerente con `AutomationTrigger`.

> ⚠️ Tabella a **0 righe** e feature incompleta nel legacy (nessun mittente). Implementazione qui
> definita nell'ipotesi che [D-47](../_domande-committente.md) confermi il completamento; se la
> decisione è "dismetti", questa Configuration e l'entità non vanno scritte affatto (si rimuove anche la
> tabella dallo schema).

### Enum propri

```csharp
// Domain/Enums/EventReminderOffset.cs
public enum EventReminderOffset
{
	OneDayBefore = 0,
	ThreeDaysBefore = 1,
	OneWeekBefore = 2,
	Custom = 3
}

// Domain/Enums/ReminderStatus.cs
public enum ReminderStatus
{
	Pending = 0,
	Sent = 1,
	Failed = 2,
	Canceled = 3
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<EventReminderOffset>("notification", "event_reminder_offset");
modelBuilder.HasPostgresEnum<ReminderStatus>("notification", "reminder_status");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<EventReminderOffset>("notification.event_reminder_offset");
dataSourceBuilder.MapEnum<ReminderStatus>("notification.reminder_status");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class EventReminderConfiguration : IEntityTypeConfiguration<EventReminder>
{
	public void Configure(EntityTypeBuilder<EventReminder> builder)
	{
		builder.ToTable("marketing_event_reminders", "notification", t => t.HasComment(
			"Reminder pianificato di un evento marketing, inviato ai contatti della lista dell'evento allo scadere."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_event_reminders");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.EventId)
			.HasColumnName("event_id")
			.IsRequired()
			.HasComment("Evento a cui il reminder appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato dall'evento — nessuna FK cross-schema.");

		builder.Property(x => x.Offset)
			.HasColumnName("offset")
			.IsRequired()
			.HasDefaultValue(EventReminderOffset.OneDayBefore)
			.HasComment("Anticipo rispetto all'evento (1 giorno, 3 giorni, 1 settimana prima, o custom).");

		builder.Property(x => x.CustomDate)
			.HasColumnName("custom_date")
			.HasComment("Data esplicita per offset = custom.");

		builder.Property(x => x.CustomTime)
			.HasColumnName("custom_time")
			.HasColumnType("time")
			.HasComment("Ora esplicita per offset = custom.");

		builder.Property(x => x.ScheduledAt)
			.HasColumnName("scheduled_at")
			.HasComment("Istante assoluto di invio, calcolato da offset (o custom_*) rispetto a starts_on dell'evento e al fuso della property; ricalcolato se l'evento cambia data.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.IsRequired()
			.HasDefaultValue(ReminderStatus.Pending)
			.HasComment("Stato di invio del reminder.");

		builder.Property(x => x.SentAt)
			.HasColumnName("sent_at")
			.HasComment("Quando il reminder è stato effettivamente inviato.");

		builder.Property(x => x.FailureReason)
			.HasColumnName("failure_reason")
			.HasColumnType("text")
			.HasComment("Motivo del fallimento dell'invio, se status = failed.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione del reminder.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di soft-delete.");

		builder.HasIndex(x => x.ScheduledAt)
			.HasFilter("status = 'pending' AND deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_event_reminders_due");

		builder.HasIndex(x => x.EventId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_event_reminders_event");

		builder.HasOne(x => x.Event)
			.WithMany(e => e.Reminders)
			.HasForeignKey(x => x.EventId)
			.HasConstraintName("fk_notification_marketing_event_reminders_event")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

### Relazioni

- `EventId` → FK reale verso [`notification_marketing_events`](notification_marketing_events.md)
  (stesso schema), `ON DELETE CASCADE` (correzione rispetto al legacy, che non aveva FK e puliva a mano).
- `PropertyId`: id copiato da C2, nessuna FK.
- `Status = Sent` produce righe in [`notification_deliveries`](notification_deliveries.md) per
  destinatario (nessuna FK diretta: il collegamento passa dall'evento di dominio, non da una colonna).

### Note

- `EventReminder` implementa `ISoftDeletable` e `IAuditable`; entità semplice, mutazioni tramite
  `MarketingEvent.AddReminder(...)` / `RemoveReminder(...)`.
- `ScheduledAt` è **calcolato**, non immesso dall'utente: un domain service ricalcola questa colonna
  ogni volta che `MarketingEvent.StartsOn` cambia (per gli `offset` non-custom) — nel legacy questo
  calcolo avveniva solo a runtime e non era mai persistito.
- **Decisione preliminare** ([D-47](../_domande-committente.md)): completare la feature (scheduler che
  legge i `Pending` dovuti dall'indice `ix_..._due`, crea una `Delivery` per destinatario, avanza
  `Status`) **oppure** dismetterla insieme a `notification_marketing_events.reminders_enabled` /
  `contact_list_id` — in tal caso questa tabella/entità non va creata.
- **Migrazione**: **0 righe** in `marketing_event_notification` (dev e prod) → nessun dato da
  migrare; la tabella nasce vuota.
