# Tabella `notification_marketing_automation_triggers`

> **Dominio**: C6 — `notification` · **sottoarea**: `marketing` · **schema**: `notification` · **nome
> flat**: `notification_marketing_automation_triggers` · **forma canonica**:
> `notification.marketing_automation_triggers`.
>
> Sostituisce l'array JSON `scheduleValue` di
> [`marketing_automation`](../../../analisi-pre/db/marketing_automation.md) **nel caso
> `scheduleType = 'custom'`** — dove ha la forma `[{ type, time, days? }]` con
> `type ∈ ofbooking | beforebooking | afterbooking | pendingpayments`. Fonti:
> [`analisi-pre/db/marketing_automation.md`](../../../analisi-pre/db/marketing_automation.md) (§"scheduleValue"),
> [`docs/03-gestione-dati.md` §2.4quater](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga = un trigger custom di un'automazione.** Un'automazione `schedule_type = custom` può avere
**più trigger** (l'array `scheduleValue`): sono attivi tutti (nessuna esclusività) → **N righe**, non
un `enum[]`.

## Verifica di esclusività

| Campi | Esito |
|---|---|
| `trigger_kind` | **enum esclusivo per riga**, ma **più righe** per automazione (l'array legacy ne conteneva più di uno). Vedi [`docs/03 §2.4quater`](../../../progettazione/03-gestione-dati.md) sul criterio "più flag dello stesso argomento → più valori". |

## Snapshot

Non applicabile.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_notification_marketing_automation_triggers` | `id` | PK | — |
| `uq_..._automation_kind` | `(automation_id, trigger_kind) WHERE deleted_at IS NULL` | UNIQUE parziale | un trigger di un tipo per automazione |
| `ix_..._kind` | `(trigger_kind) WHERE deleted_at IS NULL` | btree parziale | "quali automazioni reagiscono a `pending_payments`" |

## Campi

### id
- **Legacy**: elemento di `scheduleValue[]`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### automation_id
- **Legacy**: `marketing_automation.id` (contenitore dell'array)
- **Descrizione**: automazione a cui il trigger appartiene.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK interna** → [`notification_marketing_automations.id`](notification_marketing_automations.md) `ON DELETE CASCADE`.

### trigger_kind
- **Legacy**: `scheduleValue[].type` (`ofbooking` / `beforebooking` / `afterbooking` / `pendingpayments`)
- **Descrizione**: evento della prenotazione che fa scattare l'invio.
- **Tipo**: `enum notification.automation_trigger_kind` — valori: `of_booking` (il giorno della
  prenotazione), `before_booking` (N giorni prima dell'arrivo), `after_booking` (N giorni dopo la
  partenza), `pending_payments` (prenotazione con saldo > 0).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: `pending_payments` richiede la lettura del saldo prenotazione da C1 (proiezione /
  evento), nel legacy `JSON.parse(booking.booking_payments).toPay > 0`.

### days_offset
- **Legacy**: `scheduleValue[].days`
- **Descrizione**: giorni di anticipo/ritardo per `before_booking` / `after_booking`.
- **Tipo**: `smallint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`of_booking` / `pending_payments` non lo usano).
- **Relazioni**: `CHECK (days_offset >= 0)`.

### send_at_time
- **Legacy**: `scheduleValue[].time` (`"HH:mm"`)
- **Descrizione**: ora locale di invio.
- **Tipo**: `time`.
- **Default**: `'09:00'`.
- **Nullabile**: Sì.
- **Relazioni**: interpretata nel `timezone` dell'automazione.

### created_at / updated_at / deleted_at
- **Legacy**: — (elementi di array)
- **Descrizione**: creazione / modifica / soft-delete (rimozione del trigger).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

È la **de-jsonizzazione** di `scheduleValue` per `scheduleType = custom`. Il ramo `before`/`after`
(dove `scheduleValue` è `[{days}]`) resta scalare sulla tabella padre (`days_offset` + `send_at_time`).

## Suggerimenti per le prestazioni

- Poche righe per automazione. Nessun indice speciale oltre a quelli elencati.
- Letta insieme all'automazione; l'indice per `trigger_kind` serve il dispatch per evento
  ("un pagamento è scaduto → quali automazioni?").

## Implementazione .NET

`AutomationTrigger` è l'**entità figlia** dell'automazione (`schedule_type = custom`), modulo
`Module.Notification`, sottoarea `Marketing`, schema `notification`. Ha PK e indici propri (una riga per
`(automation_id, trigger_kind)`), quindi è modellata come entità normale con FK reale verso
`MarketingAutomation` e collezione di navigazione `Triggers` sull'aggregate — non un owned type
`OwnsMany` (che nasconderebbe la PK e complicherebbe l'`UNIQUE` parziale).

### Enum proprio

```csharp
// Domain/Enums/AutomationTriggerKind.cs
public enum AutomationTriggerKind
{
	OfBooking = 0,
	BeforeBooking = 1,
	AfterBooking = 2,
	PendingPayments = 3
}
```

```csharp
// NotificationDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<AutomationTriggerKind>("notification", "automation_trigger_kind");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<AutomationTriggerKind>("notification.automation_trigger_kind");
```

### Configuration

```csharp
namespace Module.Notification.Infrastructure.Configurations;

public sealed class AutomationTriggerConfiguration : IEntityTypeConfiguration<AutomationTrigger>
{
	public void Configure(EntityTypeBuilder<AutomationTrigger> builder)
	{
		builder.ToTable("marketing_automation_triggers", "notification", t => t.HasComment(
			"Trigger custom di un'automazione marketing: evento prenotazione che fa scattare l'invio."));

		builder.HasKey(x => x.Id).HasName("pk_notification_marketing_automation_triggers");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.AutomationId)
			.HasColumnName("automation_id")
			.IsRequired()
			.HasComment("Automazione a cui il trigger appartiene.");

		builder.Property(x => x.TriggerKind)
			.HasColumnName("trigger_kind")
			.IsRequired()
			.HasComment("Evento della prenotazione che fa scattare l'invio (del giorno, prima/dopo l'arrivo/partenza, saldo pendente).");

		builder.Property(x => x.DaysOffset)
			.HasColumnName("days_offset")
			.HasComment("Giorni di anticipo/ritardo per before_booking / after_booking; NULL per of_booking e pending_payments.");

		builder.Property(x => x.SendAtTime)
			.HasColumnName("send_at_time")
			.HasColumnType("time")
			.HasDefaultValue(new TimeOnly(9, 0))
			.HasComment("Ora locale di invio, interpretata nel fuso dell'automazione padre.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di creazione del trigger.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired()
			.HasDefaultValueSql("now()").HasComment("Data di ultima modifica.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Data di rimozione del trigger (soft-delete).");

		builder.HasIndex(x => new { x.AutomationId, x.TriggerKind })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_notification_marketing_automation_triggers_automation_kind");

		builder.HasIndex(x => x.TriggerKind)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_notification_marketing_automation_triggers_kind");

		builder.HasOne(x => x.Automation)
			.WithMany(a => a.Triggers)
			.HasForeignKey(x => x.AutomationId)
			.HasConstraintName("fk_notification_marketing_automation_triggers_automation")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

### Relazioni

- `AutomationId` → FK reale verso [`notification_marketing_automations`](notification_marketing_automations.md)
  (stesso schema), `ON DELETE CASCADE`.

### Note

- `AutomationTrigger` implementa `ISoftDeletable` e `IAuditable`; entità semplice, nessun evento
  proprio (le mutazioni passano dal metodo `MarketingAutomation.AddTrigger(...)` sull'aggregate).
- `CHECK (days_offset >= 0)` a livello DB, in aggiunta alla validazione applicativa (il legacy accettava
  `0`, causando un loop infinito con `custom`).
- **Migrazione**: solo per le automazioni con `scheduleType = 'custom'`, esplodere l'array
  `scheduleValue[]` (`type` → `TriggerKind`, `time` → `SendAtTime`, `days` → `DaysOffset`); dedup su
  `(automation_id, trigger_kind)` tenendo la voce più recente in caso di doppioni nell'array legacy.
