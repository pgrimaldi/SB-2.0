# Tabella `identity_account_subscription_periods`

> **Dominio**: C5 — `identity` · **sottoarea**: `account` · **schema**: `identity` · **nome flat**:
> `identity_account_subscription_periods` · **forma canonica**: `identity.account_subscription_periods`.
>
> **Nuova** — storico append-only dei periodi/rinnovi di
> [`identity_account_subscriptions`](identity_account_subscriptions.md). Introdotta su **richiesta del
> committente** ("sarebbe utile avere un resoconto anche per estrapolare velocemente l'andamento"
> degli abbonamenti). Fonti:
> [`analisi-pre/db/users.md`](../../../analisi-pre/db/users.md) (campi `startDate`/`endDate`/`license`),
> [`analisi-pre/db/licenses.md`](../../../analisi-pre/db/licenses.md),
> [`docs/03-gestione-dati.md` §3.6 punto 3 (storico), §3.7 (importi `numeric`)](../../../progettazione/03-gestione-dati.md),
> [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md).
>
> **Nota sulla voce "Legacy"**: **nessun antecedente legacy** — nel legacy non esisteva alcuno storico
> degli abbonamenti (i campi `startDate`/`endDate` su `users` venivano **sovrascritti** a ogni rinnovo).
> Tutti i campi sono `— (nuovo)`.

---

## Descrizione

**Una riga per ogni periodo di abbonamento** di uno stabilimento: il periodo iniziale, ogni rinnovo,
ed eventuali migrazioni dal vecchio modello. Ogni riga è **immutabile** una volta chiusa (`closed_at`).
[`identity_account_subscriptions`](identity_account_subscriptions.md) tiene il **periodo corrente**;
questa tabella tiene **tutti** i periodi (incluso il corrente, con `closed_at IS NULL`).

Serve a:

- **fatturazione / audit**: quale piano, quali date, quale importo per ogni rinnovo;
- **reportistica di andamento**: rinnovi per mese/trimestre, tasso di rinnovo, mix di piani nel tempo,
  ricavi ricorrenti — query aggregate su `starts_at` / `amount` / `license_plan_code`.

Tabella di **puro registro append-only** → `deleted_at` presente solo per uniformità di schema, non
usato funzionalmente (§3.5); la rimozione è solo hard-delete di retention alla scadenza dei termini
fiscali/contabili (10 anni).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_identity_account_subscription_periods` | `id` | PK | — |
| `ix_..._subscription` | `(subscription_id, starts_at DESC)` | — | storico dei periodi di un abbonamento |
| `uq_..._current` | `(subscription_id) WHERE closed_at IS NULL` | UNIQUE parziale | al più un periodo aperto per abbonamento |
| `ix_..._property_time` | `(property_id, starts_at)` | — | reportistica per stabilimento |
| `ix_..._starts_at` | `(starts_at)` | — | reportistica temporale globale (andamento rinnovi/ricavi) |
| `ix_..._ends_at_open` | `(ends_at) WHERE closed_at IS NULL` | parziale | periodi in scadenza (solleciti di rinnovo) |


> ### ✅ Applicato nel backend (2026-09-21)
>
> Vincolo **`EXCLUDE USING gist`** di non sovrapposizione ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §8), migration
> `AddNoOverlapExcludeConstraints` + estensione `btree_gist`:
>
> ```sql
> EXCLUDE USING gist (subscription_id WITH =, tstzrange(starts_at, ends_at) WITH &&)
> WHERE (deleted_at IS NULL)
> ```
>
> Un `CHECK` garantisce solo che la **singola riga** sia coerente (`ends >= starts`); l'`EXCLUDE` è
> l'unico strumento che vincola **due righe fra loro** — ed è quello che serve qui, perché il problema
> non è una riga sbagliata ma una data che cade in due periodi diversi senza che nessuno sappia quale
> vince. Parziale su `deleted_at IS NULL`: una riga soft-eliminata non deve bloccare quella che la
> sostituisce.
>
> `ends_at` nullo diventa un range **illimitato superiormente**, quindi due periodi aperti sullo stesso
> abbonamento ora si escludono a vicenda. Il `UNIQUE` parziale preesistente copriva solo quel caso e
> lasciava passare due periodi *chiusi* sovrapposti: questo vincolo li copre entrambi.

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica del periodo.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.
- **Note**: —.

### subscription_id
- **Legacy**: — (nuovo)
- **Descrizione**: abbonamento a cui il periodo appartiene.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`identity_account_subscriptions.id`](identity_account_subscriptions.md)
  `ON DELETE RESTRICT` (lo storico non sparisce con il record corrente).
- **Note**: —.

### property_id
- **Legacy**: — (nuovo)
- **Descrizione**: `property` (denormalizzata dal `subscription` per le query di reporting).
- **Tipo**: `uuid` — il **`public_id`** della `property` (C2), non il suo `bigint` interno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C2 (`catalog`) — nessuna FK cross-modulo.
- **Note**: copia stabile: se un domani cambiasse l'associazione, i periodi storici restano attribuiti
  correttamente.

### license_plan_id
- **Legacy**: — (nuovo)
- **Descrizione**: piano applicato in **questo** periodo.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`identity_account_license_plans.id`](identity_account_license_plans.md)
  `ON DELETE RESTRICT`.
- **Note**: un rinnovo può cambiare piano rispetto al periodo precedente.

### license_plan_code
- **Legacy**: — (nuovo)
- **Descrizione**: `code` del piano **copiato** al momento dell'apertura del periodo (snapshot leggibile
  per i report senza join).
- **Tipo**: `varchar(40)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: snapshot di [`identity_account_license_plans.code`](identity_account_license_plans.md).
- **Note**: [`docs/03-gestione-dati.md` §3.6 punto 2](../../../progettazione/03-gestione-dati.md).

### term_type
- **Legacy**: — (nuovo)
- **Descrizione**: tipo del periodo (vedi [`identity_account_subscriptions`](identity_account_subscriptions.md)
  → "Due tipi di abbonamento").
- **Tipo**: enum PG `identity.subscription_term_type` — valori: `fixed_term`, `legacy_day_bank`.
- **Default**: `'fixed_term'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: i periodi migrati dal vecchio modello 365 gg sono `legacy_day_bank`; i rinnovi successivi
  sono `fixed_term`.

### starts_at / ends_at
- **Legacy**: — (nuovo; nel legacy `users.startDate`/`users.endDate` venivano sovrascritti, senza storico)
- **Descrizione**: inizio e fine del periodo.
- **Tipo**: `timestamptz` (o `date`) ciascuno.
- **Default**: nessuno.
- **Nullabile**: `starts_at` No; `ends_at` Sì (per `legacy_day_bank` in corso può essere una proiezione
  o `NULL`).
- **Relazioni**: —.
- **Note**: per `fixed_term` sono le date contrattuali; per `legacy_day_bank` `ends_at` è indicativo
  (il dato autorevole del residuo è su `identity_account_subscriptions.remaining_days_balance`).

### days_granted
- **Legacy**: — (nuovo)
- **Descrizione**: giorni concessi in questo periodo (solo `legacy_day_bank`: es. 365; o i giorni
  aggiunti da una migrazione/estensione).
- **Tipo**: `integer`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` per `fixed_term`).
- **Relazioni**: —.
- **Note**: —.

### amount
- **Legacy**: — (nuovo; nel legacy l'importo non era registrato da nessuna parte)
- **Descrizione**: importo fatturato per il periodo.
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (sconosciuto per i periodi migrati / contratti gestiti fuori sistema).
- **Relazioni**: —.
- **Note**: mai `float` ([`docs/03-gestione-dati.md` §3.7](../../../progettazione/03-gestione-dati.md)). È il campo
  chiave per i report "ricavi ricorrenti".

### currency
- **Legacy**: — (nuovo)
- **Descrizione**: valuta dell'`amount`.
- **Tipo**: `char(3)` (ISO 4217).
- **Default**: `'EUR'`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: —.

### period_kind
- **Legacy**: — (nuovo)
- **Descrizione**: natura del periodo.
- **Tipo**: enum PG `identity.subscription_period_kind` — valori: `initial`, `renewal`, `migration`,
  `manual_adjustment`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `migration` = periodo creato dallo script di go-live per rappresentare l'abbonamento in
  corso al momento del passaggio dal legacy.

### opened_by_user_id
- **Legacy**: — (nuovo)
- **Descrizione**: account (SuperAdmin piattaforma / operatore) che ha stipulato o rinnovato.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` per i periodi creati da migrazione o da job automatici).
- **Relazioni**: **FK** → [`identity_access_users.id`](identity_access_users.md) `ON DELETE SET NULL`.
- **Note**: allineato a [`docs/03-gestione-dati.md` §3.2](../../../progettazione/03-gestione-dati.md).

### closed_at
- **Legacy**: — (nuovo)
- **Descrizione**: quando il periodo è stato chiuso (dal rinnovo successivo, dalla scadenza o dalla
  cessazione).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì (`NULL` ⇔ periodo corrente).
- **Relazioni**: —.
- **Note**: al più una riga con `closed_at IS NULL` per `subscription_id` (indice unico parziale).

### close_reason
- **Legacy**: — (nuovo)
- **Descrizione**: perché il periodo si è chiuso.
- **Tipo**: enum PG `identity.subscription_period_close_reason` — valori: `renewed`, `expired`,
  `cancelled`, `superseded`.
- **Default**: `NULL`.
- **Nullabile**: Sì (valorizzato con `closed_at`).
- **Relazioni**: —.
- **Note**: `superseded` = correzione/sostituzione manuale del periodo.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica della riga.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: §3.3–§3.4. Dopo la chiusura la riga non cambia più (append-only).

### deleted_at
- **Legacy**: — (nuovo; presente per uniformità, non usato — registro append-only)
- **Descrizione**: presente per uniformità di schema, **non usato** funzionalmente.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: come [`identity_account_contract_acceptances`](identity_account_contract_acceptances.md):
  registro append-only, rimozione solo per hard-delete di retention (10 anni, dati contabili —
  [`docs/80-retention.md` §3.4](../../../progettazione/80-retention.md)).

## Valutazione JSON

Nessun JSON: ogni periodo è una riga tipizzata, aggregabile per data/piano/importo. È esattamente il
modello che [`docs/03-gestione-dati.md` §3.6 punto 3](../../../progettazione/03-gestione-dati.md) indica per lo
storico (righe `*_periods` con `valid_from`/`valid_to`), non un blob.

## Suggerimenti per le prestazioni

- Volume modesto (≈ 1 riga per stabilimento per anno → poche migliaia di righe in vari anni). Nessun
  problema di scala.
- Gli indici su `(property_id, starts_at)` e `(starts_at)` servono le query di reporting/andamento
  senza scansione completa; `(subscription_id, starts_at DESC)` serve il "dettaglio abbonamento".
- Per dashboard con aggregazioni pesanti frequenti: valutare una **vista materializzata**
  (`ricavi_ricorrenti_mensili`) con refresh schedulato, non query aggregate live a ogni apertura —
  [`docs/53-prestazioni.md`](../../../progettazione/53-prestazioni.md).

## Implementazione .NET

Entità figlia (storico append-only) dell'aggregato [`Subscription`](identity_account_subscriptions.md)
(modulo `Module.Identity`, sottoarea `Account`, schema `identity`). `SubscriptionPeriod` è **immutabile
dopo `Close()`**: creata dall'aggregato a ogni `Renew(...)`/`Migrate(...)`/cessazione, mai aggiornata in
altro modo. `deleted_at` presente per uniformità, non usato funzionalmente (§3.5 del template).

### Enum propri

```csharp
// Domain/Enums/SubscriptionPeriodKind.cs
public enum SubscriptionPeriodKind { Initial = 0, Renewal = 1, Migration = 2, ManualAdjustment = 3 }

// Domain/Enums/SubscriptionPeriodCloseReason.cs
public enum SubscriptionPeriodCloseReason { Renewed = 0, Expired = 1, Cancelled = 2, Superseded = 3 }
```

```csharp
modelBuilder.HasPostgresEnum<SubscriptionPeriodKind>("identity", "subscription_period_kind");
modelBuilder.HasPostgresEnum<SubscriptionPeriodCloseReason>("identity", "subscription_period_close_reason");
// subscription_term_type: enum condiviso con identity_account_subscriptions, già registrato una volta.
```

```csharp
dataSourceBuilder.MapEnum<SubscriptionPeriodKind>("identity.subscription_period_kind");
dataSourceBuilder.MapEnum<SubscriptionPeriodCloseReason>("identity.subscription_period_close_reason");
```

### `SubscriptionPeriodConfiguration`

```csharp
namespace Module.Identity.Infrastructure.Configurations;

public sealed class SubscriptionPeriodConfiguration : IEntityTypeConfiguration<SubscriptionPeriod>
{
	public void Configure(EntityTypeBuilder<SubscriptionPeriod> builder)
	{
		builder.ToTable("account_subscription_periods", "identity", t => t.HasComment(
			"Storico append-only dei periodi/rinnovi di abbonamento di uno stabilimento."));

		builder.HasKey(x => x.Id).HasName("pk_identity_account_subscription_periods");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SubscriptionId)
			.HasColumnName("subscription_id")
			.IsRequired()
			.HasComment("Abbonamento a cui il periodo appartiene.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id della property (catalog), denormalizzato per le query di reporting.");

		builder.Property(x => x.LicensePlanId)
			.HasColumnName("license_plan_id")
			.IsRequired()
			.HasComment("Piano applicato in questo periodo.");

		builder.Property(x => x.LicensePlanCode)
			.HasColumnName("license_plan_code")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Code del piano copiato al momento dell'apertura del periodo (snapshot leggibile per i report).");

		builder.Property(x => x.TermType)
			.HasColumnName("term_type")
			.IsRequired()
			.HasDefaultValue(SubscriptionTermType.FixedTerm)
			.HasComment("Tipo del periodo (a termine fisso o banca giorni legacy).");

		builder.Property(x => x.StartsAt)
			.HasColumnName("starts_at")
			.IsRequired()
			.HasComment("Inizio del periodo.");

		builder.Property(x => x.EndsAt)
			.HasColumnName("ends_at")
			.HasComment("Fine del periodo (indicativa per legacy_day_bank, contrattuale per fixed_term).");

		builder.Property(x => x.DaysGranted)
			.HasColumnName("days_granted")
			.HasComment("Giorni concessi in questo periodo (solo legacy_day_bank).");

		builder.Property(x => x.Amount)
			.HasColumnName("amount")
			.HasColumnType("numeric(12,2)")
			.HasComment("Importo fatturato per il periodo.");

		builder.Property(x => x.Currency)
			.HasColumnName("currency")
			.HasColumnType("char(3)")
			.IsRequired()
			.HasDefaultValue("EUR")
			.HasComment("Valuta dell'importo (ISO 4217).");

		builder.Property(x => x.PeriodKind)
			.HasColumnName("period_kind")
			.IsRequired()
			.HasComment("Natura del periodo (iniziale, rinnovo, migrazione, rettifica manuale).");

		builder.Property(x => x.OpenedByUserId)
			.HasColumnName("opened_by_user_id")
			.HasComment("Account che ha stipulato o rinnovato; NULL per i periodi creati da migrazione/job.");

		builder.Property(x => x.ClosedAt)
			.HasColumnName("closed_at")
			.HasComment("Quando il periodo è stato chiuso; NULL = periodo corrente.");

		builder.Property(x => x.CloseReason)
			.HasColumnName("close_reason")
			.HasComment("Perché il periodo si è chiuso.");

		// created_at / updated_at / deleted_at: converter UTC e commenti da convenzione comune (§2, §6 del
		// template); deleted_at non usato funzionalmente (registro append-only).

		builder.HasIndex(x => new { x.SubscriptionId, x.StartsAt })
			.HasDatabaseName("ix_identity_account_subscription_periods_subscription");

		builder.HasIndex(x => x.SubscriptionId)
			.IsUnique()
			.HasFilter("closed_at IS NULL")
			.HasDatabaseName("uq_identity_account_subscription_periods_current");

		builder.HasIndex(x => new { x.PropertyId, x.StartsAt })
			.HasDatabaseName("ix_identity_account_subscription_periods_property_time");

		builder.HasIndex(x => x.StartsAt)
			.HasDatabaseName("ix_identity_account_subscription_periods_starts_at");

		builder.HasIndex(x => x.EndsAt)
			.HasFilter("closed_at IS NULL")
			.HasDatabaseName("ix_identity_account_subscription_periods_ends_at_open");

		builder.HasOne(x => x.Subscription)
			.WithMany(s => s.Periods)
			.HasForeignKey(x => x.SubscriptionId)
			.HasConstraintName("fk_identity_account_subscription_periods_subscription")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.LicensePlan)
			.WithMany()
			.HasForeignKey(x => x.LicensePlanId)
			.HasConstraintName("fk_identity_account_subscription_periods_plan")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.OpenedByUser)
			.WithMany()
			.HasForeignKey(x => x.OpenedByUserId)
			.HasConstraintName("fk_identity_account_subscription_periods_opened_by")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

### Reportistica

Read-model/vista per l'andamento (rinnovi per mese/trimestre, mix di piani, ricavi ricorrenti) sopra
`ix_..._property_time` e `ix_..._starts_at`. Per dashboard con aggregazioni pesanti frequenti: valutare
una **vista materializzata** con refresh schedulato, non query aggregate live a ogni apertura
([`docs/53-prestazioni.md`](../../../progettazione/53-prestazioni.md)).

### Note di migrazione

- Nessun antecedente legacy (il legacy sovrascriveva `startDate`/`endDate` a ogni rinnovo, senza
  storico).
- Migrazione go-live: per ogni stabilimento con abbonamento attivo, una riga
  `PeriodKind = Migration` con le date/residui noti; `Amount = NULL` se l'importo storico non è
  disponibile.
