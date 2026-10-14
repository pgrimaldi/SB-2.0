# Tabella `identity_account_subscriptions`

> **Dominio**: C5 — `identity` · **sottoarea**: `account` · **schema**: `identity` · **nome flat**:
> `identity_account_subscriptions` · **forma canonica**: `identity.account_subscriptions`.
>
> **Nuova** — normalizza i campi abbonamento/licenza oggi sparsi su [`users`](../../../analisi-pre/db/users.md)
> (`license`, `startDate`, `endDate`, `subscriptionStatus`, `subscriptionDisableDate`,
> `subscriptionEnableDate`), che [`docs/03-gestione-dati.md` §6](../../../progettazione/03-gestione-dati.md)
> assegna a C5 sottoarea `account`, isolati dall'anagrafica dell'utente. Fonti:
> [`analisi-pre/db/users.md`](../../../analisi-pre/db/users.md) (gruppo campi "abbonamento/licenza",
> propagazione fra staff della stessa property, cron `cron-disableproperty.js`),
> [`analisi-pre/db/licenses.md`](../../../analisi-pre/db/licenses.md),
> [`analisi-pre/db/properties.md`](../../../analisi-pre/db/properties.md) (`superAdminOnlineFlag`
> guidato da `endDate`), [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md).
>
> **Nota sulla voce "Legacy"**: `nome_tabella.campo` di origine (i campi provengono tutti da `users`);
> `— (nuovo)` = campo senza corrispondente legacy.

---

## Descrizione

**Una riga per stabilimento**: l'abbonamento alla piattaforma è dello **stabilimento**, non della
singola persona (nel legacy era denormalizzato su ogni account staff, con un meccanismo che
"propagava" `license`/`startDate`/`endDate` da un utente all'altro della stessa property). Tiene il
piano corrente, la finestra di validità, lo stato e i timestamp di sospensione/riattivazione.

È il dato che **guida `properties.superAdminOnlineFlag`**: il cron notturno legacy
(`cron-disableproperty.js`) controlla `endDate < oggi` + `subscriptionStatus = 'enable'` e, se
scaduto, mette `subscriptionStatus = 'disable'` e `properties.superAdminOnlineFlag = 0`. Nel nuovo
modello quel batch legge *questa* tabella ed emette un evento verso C2.

### Due tipi di abbonamento (indicazione committente)

- **`fixed_term`** — **tutti i contratti dall'anno scorso**: durata **annuale / biennale / triennale**
  con **date esplicite** (`starts_at` / `ends_at` contrattuali). La sospensione **non** sposta le date:
  `status = suspended`, nessun calcolo sui giorni.
- **`legacy_day_bank`** — vecchi abbonamenti da **365 giorni "a consumo"**: alla sospensione si
  **congelavano** e alla ripresa venivano scalati **solo i giorni di effettivo utilizzo**. Comportamento
  **dismesso per i nuovi contratti**, ma **qualche stabilimento ha ancora un residuo di giorni** → va
  supportato in lettura/migrazione. Il "congelamento" è reso da `remaining_days_balance` che **smette di
  decrementare** mentre `status = suspended` (sostituisce il calcolo aritmetico errato di
  `user.controller.js:updateuser`).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_account_subscriptions` | `id` | PK | — |
| `uq_..._property` | `property_id` | UNIQUE | 1 riga per stabilimento |
| `ix_..._plan` | `(license_plan_id)` | FK | — |
| `ix_..._expiry_scan` | `(ends_at) WHERE status = 'active'` | parziale | batch notturno "abbonamenti scaduti" senza full scan |

## Campi

### id
- **Legacy**: — (nuovo; nel legacy non esisteva un'entità "subscription")
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.
- **Note**: —.

### property_id
- **Legacy**: — (nuovo come colonna; nel legacy dedotto da `users.propertyId` degli account staff con licenza valorizzata)
- **Descrizione**: `property` (entità gestionale) titolare dell'abbonamento.
- **Tipo**: `uuid` — il **`public_id`** della `property` (C2), non il suo `bigint` interno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 (`catalog`) — nessuna FK cross-modulo; `UNIQUE`.
- **Note**: discriminatore tenant.

### owner_user_id
- **Legacy**: — (nuovo; nel legacy la relazione era implicita — il primo utente `superadmin` con `users.license` valorizzata)
- **Descrizione**: account `superadmin` titolare di riferimento (chi "possiede" il contratto).
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (uno stabilimento potrebbe non avere ancora un `superadmin` collegato).
- **Relazioni**: **FK** → [`identity_access_users.id`](identity_access_users.md) `ON DELETE SET NULL`.
- **Note**: nel legacy la relazione era implicita (il primo utente con `license` valorizzata); qui è
  esplicita. Se cambia il titolare, si aggiorna questo campo (operazione tracciata).

### license_plan_id
- **Legacy**: `users.license` (stringa libera `basic`/`advanced` → FK reale)
- **Descrizione**: piano corrente.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`identity_account_license_plans.id`](identity_account_license_plans.md)
  `ON DELETE RESTRICT`.
- **Note**: sostituisce `users.license` (stringa libera). In migrazione le 78 righe dev con `""` →
  risolte col piano effettivo dello stabilimento o `basic` di default (da decidere in migrazione).

### term_type
- **Legacy**: — (nuovo; nel legacy il tipo era implicito — tutti gli abbonamenti erano "365 giorni a consumo")
- **Descrizione**: tipo di abbonamento (vedi "Due tipi di abbonamento").
- **Tipo**: enum PG `identity.subscription_term_type` — valori: `fixed_term`, `legacy_day_bank`.
- **Default**: `'fixed_term'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: i nuovi contratti sono sempre `fixed_term`. `legacy_day_bank` è assegnato **solo** in
  migrazione agli stabilimenti che alla data di go-live hanno ancora un residuo di giorni del vecchio
  modello 365 gg. Una volta esaurito/convertito, quello stabilimento passa a `fixed_term`.

### status
- **Legacy**: `users.subscriptionStatus` (`enable`/`disable`, + `""` sporco)
- **Descrizione**: stato dell'abbonamento.
- **Tipo**: enum PG `identity.subscription_status` — valori: `active`, `suspended`, `expired`,
  `cancelled`.
- **Default**: `'active'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: mappa `users.subscriptionStatus` (`enable`/`disable`, + `""` sporco) su un vocabolario più
  espressivo. `active` = ex `enable`; `suspended` = sospensione manuale; `expired` = scaduto per data;
  `cancelled` = cessazione contratto. Separato dal soft-delete.

### starts_at
- **Legacy**: `users.startDate`
- **Descrizione**: inizio validità del periodo corrente.
- **Tipo**: `timestamptz` (o `date` se l'ora non ha significato).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: ex `users.startDate`.

### ends_at
- **Legacy**: `users.endDate`
- **Descrizione**: fine validità del periodo corrente.
- **Tipo**: `timestamptz` (o `date`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: ex `users.endDate`.
  - `term_type = fixed_term`: **data contrattuale** (annuale/biennale/triennale), impostata alla
    stipula/rinnovo — **mai** da input client (nel legacy `endDate` era calcolato da
    `req.body.licenseMonths` fornito dal client — [`analisi-pre/db/licenses.md`](../../../analisi-pre/db/licenses.md)).
  - `term_type = legacy_day_bank`: data **proiettata** `now() + remaining_days_balance` mentre
    `status = active`, ricalcolata a ogni ripresa; informativa mentre `status = suspended` (il dato
    autorevole è `remaining_days_balance`).
  - È il campo letto dal batch notturno di disattivazione (per entrambi i tipi: se `ends_at < now()` e
    `status = active` → `expired`).

### suspended_at
- **Legacy**: `users.subscriptionDisableDate`
- **Descrizione**: timestamp dell'ultima transizione a `suspended`/`expired`.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: ex `users.subscriptionDisableDate`. Scritto sia dal batch (scadenza) sia dalla
  sospensione manuale.

### reactivated_at
- **Legacy**: `users.subscriptionEnableDate`
- **Descrizione**: timestamp dell'ultima transizione **a** `active` da uno stato non attivo.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: ex `users.subscriptionEnableDate` — ma con semantica **corretta**: nel legacy veniva
  aggiornato a ogni salvataggio non-disabilitante ("ultima volta che non è stato disabilitato"), non
  solo alla riattivazione reale.

### remaining_days_balance
- **Legacy**: — (nuovo; sostituisce la logica aritmetica errata di `user.controller.js:updateuser`, righe ~389-402)
- **Descrizione**: **banca giorni** residua del vecchio modello "365 giorni a consumo" — giorni di
  abbonamento ancora spendibili.
- **Tipo**: `integer`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: valorizzato **solo** per `term_type = legacy_day_bank`; `NULL` per `fixed_term`.
  Comportamento (indicazione committente):
  - decrementa di **1 al giorno** (job schedulato) mentre `status = active` — si consumano *solo* i
    giorni di effettivo utilizzo;
  - **non** decrementa mentre `status = suspended` → **questo** è il "congelamento" (nel legacy
    `users.updateuser` faceva un calcolo che *raddoppiava* la distanza residua invece di fermarla —
    [`analisi-pre/db/users.md`](../../../analisi-pre/db/users.md));
  - a `remaining_days_balance = 0` → `status = expired` + evento verso C2;
  - in migrazione: valorizzato dai giorni residui reali degli stabilimenti col vecchio modello alla
    data di go-live; a `0`/assenti → si crea direttamente un `fixed_term`.

### created_at / updated_at
- **Legacy**: — (nuovo; nel legacy i campi abbonamento vivevano su `users` con i timestamp dell'account, `users.createdAt`/`users.updatedAt`)
- **Descrizione**: creazione / ultima modifica della riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: §3.3–§3.4.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete (`NULL` ⇔ riga attiva).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: valorizzato alla cessazione dello stabilimento (`status = 'cancelled'` + `deleted_at`).
  Retention: [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md) (dati collegati al fiscale →
  10 anni per i riferimenti).

## Storico dei periodi

Questa tabella tiene **il periodo corrente**. Lo **storico dei rinnovi** — utile a fatturazione,
audit e soprattutto per **estrapolare velocemente l'andamento** degli abbonamenti (richiesto dal
committente) — vive nella tabella dedicata
[`identity_account_subscription_periods`](identity_account_subscription_periods.md) (append-only, una
riga per ogni periodo/rinnovo con piano, date, importo). Da non confondere con
[`identity_account_contract_acceptances`](identity_account_contract_acceptances.md) (accettazioni
legali, non periodi di abbonamento).

## Valutazione JSON

Nessun JSON: 6 campi legacy piatti diventano 6+ colonne tipizzate — esattamente la direzione indicata
da [`docs/note.md`](../../../progettazione/note.md).

## Suggerimenti per le prestazioni

- ~175 righe: irrilevante come volume. L'unico accesso "di massa" è il **batch notturno**
  (`WHERE status = 'active' AND ends_at < now()`), coperto dall'indice parziale `ix_..._expiry_scan`.
- Lettura per stabilimento (`WHERE property_id = $1`) coperta da `UNIQUE(property_id)`; cache-abile con
  invalidazione su update.
- Il feature-gating del menu legge il `code` del piano: precaricare `license_plan` (2 righe) in memoria
  ed evitare il join.

## Implementazione .NET

Aggregate root del modulo `Module.Identity`, sottoarea `Account`, schema `identity`. `Subscription`
espone transizioni di stato esplicite (`Suspend()`, `Reactivate()`, `Expire()`, `Renew(...)`); ogni
`Renew(...)` chiude il periodo corrente e apre una riga in
[`identity_account_subscription_periods`](identity_account_subscription_periods.md). Implementa
`ISoftDeletable`/`IAuditable`.

### Enum propri

```csharp
// Domain/Enums/SubscriptionTermType.cs
public enum SubscriptionTermType { FixedTerm = 0, LegacyDayBank = 1 }

// Domain/Enums/SubscriptionStatus.cs
public enum SubscriptionStatus { Active = 0, Suspended = 1, Expired = 2, Cancelled = 3 }
```

```csharp
modelBuilder.HasPostgresEnum<SubscriptionTermType>("identity", "subscription_term_type");
modelBuilder.HasPostgresEnum<SubscriptionStatus>("identity", "subscription_status");
```

```csharp
dataSourceBuilder.MapEnum<SubscriptionTermType>("identity.subscription_term_type");
dataSourceBuilder.MapEnum<SubscriptionStatus>("identity.subscription_status");
```

### `SubscriptionConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
	public void Configure(EntityTypeBuilder<Subscription> builder)
	{
		builder.ToTable("account_subscriptions", "identity", t => t.HasComment(
			"Abbonamento corrente alla piattaforma per uno stabilimento (property)."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_subscriptions");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id della property (catalog) titolare dell'abbonamento, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.OwnerUserId)
			.HasColumnName("owner_user_id")
			.HasComment("Account superadmin titolare di riferimento del contratto.");

		builder.Property(x => x.LicensePlanId)
			.HasColumnName("license_plan_id")
			.IsRequired()
			.HasComment("Piano corrente.");

		builder.Property(x => x.TermType)
			.HasColumnName("term_type")
			.IsRequired()
			.HasDefaultValue(SubscriptionTermType.FixedTerm)
			.HasComment("Tipo di abbonamento: a termine fisso con date contrattuali, o banca giorni legacy.");

		builder.Property(x => x.Status)
			.HasColumnName("status")
			.IsRequired()
			.HasDefaultValue(SubscriptionStatus.Active)
			.HasComment("Stato dell'abbonamento.");

		builder.Property(x => x.StartsAt)
			.HasColumnName("starts_at")
			.IsRequired()
			.HasComment("Inizio validità del periodo corrente.");

		builder.Property(x => x.EndsAt)
			.HasColumnName("ends_at")
			.IsRequired()
			.HasComment("Fine validità del periodo corrente (contrattuale per fixed_term, proiettata per legacy_day_bank).");

		builder.Property(x => x.SuspendedAt)
			.HasColumnName("suspended_at")
			.HasComment("Timestamp dell'ultima transizione a suspended/expired.");

		builder.Property(x => x.ReactivatedAt)
			.HasColumnName("reactivated_at")
			.HasComment("Timestamp dell'ultima transizione a active da uno stato non attivo.");

		builder.Property(x => x.RemainingDaysBalance)
			.HasColumnName("remaining_days_balance")
			.HasComment("Banca giorni residua del vecchio modello a consumo; valorizzata solo per term_type = legacy_day_bank.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del template).

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_identity_account_subscriptions_property");

		builder.HasIndex(x => x.LicensePlanId)
			.HasDatabaseName("ix_identity_account_subscriptions_plan");

		builder.HasIndex(x => x.EndsAt)
			.HasFilter("status = 'active'")
			.HasDatabaseName("ix_identity_account_subscriptions_expiry_scan");

		builder.HasOne(x => x.LicensePlan)
			.WithMany()
			.HasForeignKey(x => x.LicensePlanId)
			.HasConstraintName("fk_identity_account_subscriptions_plan")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.OwnerUser)
			.WithMany()
			.HasForeignKey(x => x.OwnerUserId)
			.HasConstraintName("fk_identity_account_subscriptions_owner")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

### Batch notturno

`BackgroundService`/Hangfire:

- `TermType = FixedTerm` con `EndsAt < now()` e `Status = Active` → `Status = Expired`.
- `TermType = LegacyDayBank` e `Status = Active` → `RemainingDaysBalance -= 1` (job giornaliero); a `0`
  → `Status = Expired`; mentre `Status = Suspended` il decremento **si ferma** (il "congelamento" —
  sostituisce il calcolo aritmetico errato di `user.controller.js:updateuser`).
- In entrambi i casi, alla transizione a `Expired`: evento `SubscriptionExpired(propertyId)` consumato
  da C2 per `superAdminOnlineFlag = 0` ([`docs/20-jobs.md`](../../../progettazione/20-jobs.md)).
- Sospensione: `FixedTerm` = solo `Status = Suspended` (le date non si spostano); `LegacyDayBank` = stop
  del decremento (`RemainingDaysBalance` resta il dato autorevole).

### Note di migrazione

- Una riga per `property`, dedotta da `users` (staff con `license`/`startDate`/`endDate` valorizzati);
  risolvere i valori `""` su `license`; `owner_user_id` collegato al `superadmin` della property.
  `property_id` = `public_id`, risolto via mappa `properties.id (legacy) → property.public_id`.
- Stabilimenti col vecchio modello 365 gg e residuo > 0 → `TermType = LegacyDayBank` +
  `RemainingDaysBalance`; tutti gli altri → `TermType = FixedTerm`.
