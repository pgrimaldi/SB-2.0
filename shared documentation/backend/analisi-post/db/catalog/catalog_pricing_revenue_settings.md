# Tabella `catalog_pricing_revenue_settings`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_revenue_settings` · **forma canonica**: `catalog.pricing_revenue_settings`.
>
> Rimpiazza la legacy [`revenue_notifications`](../../../analisi-pre/db/revenue_notifications.md) (**11
> righe** = 1 per stabilimento; feature recente). Malgrado il nome, **non è C6**: è la configurazione
> per stabilimento della feature **Revenue Management** (prezzo suggerito + banner sconto sulla pagina
> pubblica). Fonti:
> [`analisi-pre/db/revenue_notifications.md`](../../../analisi-pre/db/revenue_notifications.md),
> [`analisi-pre/business-logic/stabilimento/10-revenue.md`](../../../analisi-pre/business-logic/stabilimento/10-revenue.md).

---

## Descrizione

Interruttori e parametri per stabilimento della feature Revenue Management: se le "notifiche prezzo
scontato" sono attive, quante volte al giorno mostrare il banner sulla pagina pubblica, se lo sconto
vale solo su alcune postazioni. 1 riga per property.

Correzioni rispetto al legacy:

- **`discounts` (JSON) → non migrata**: colonna morta (mai scritta/letta da back, cron o front; `NULL`
  ovunque).
- **`enableDiscountsForSelectedLocations` (JSON array di celle mappa)** →
  [`catalog_pricing_revenue_discounted_spots`](catalog_pricing_revenue_discounted_spots.md).
- **`dailyNotificationsDisplayedCount`** (contatore "già mostrato oggi", **incrementato dal frontend**)
  → resta come contatore ma incrementato **server-side**; azzerato ogni notte (come il cron legacy).
- **`UNIQUE(property_id)`** aggiunto (il legacy non ce l'aveva → possibili righe duplicate, upsert non
  atomico).
- la logica "finestra oraria 10–20 divisa in slot / decidere se mostrare il banner", oggi **nel
  frontend**, va portata server-side.
- bug legacy `saveNotificationDetails` (`create({property_id, updateData})` annidato in colonna
  inesistente) → non riprodotto.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_revenue_settings` | `id` | PK | — |
| `uq_catalog_pricing_revenue_settings_property` | `(property_id)` | UNIQUE | 1:1 con la property (mancava nel legacy) |

## Campi

### id
- **Legacy**: `revenue_notifications.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da
  [`catalog_pricing_revenue_discounted_spots.settings_id`](catalog_pricing_revenue_discounted_spots.md).

### property_id
- **Legacy**: `revenue_notifications.property_id`
- **Descrizione**: stabilimento a cui si applica la configurazione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`; `UNIQUE`.

### is_price_discount_enabled
- **Legacy**: `revenue_notifications.enablePriceDiscountNotifications` (`tinyint(1)`, default `false`)
- **Descrizione**: interruttore generale della feature "notifiche prezzo scontato".
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### is_shown_on_public_page
- **Legacy**: `revenue_notifications.showOnEstablishmentPage` (`tinyint(1)`, default `false`)
- **Descrizione**: il banner sconto è visibile sulla pagina pubblica dello stabilimento / flusso di
  prenotazione.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: omonimo ma **indipendente** da `marketing_event.showOnEstablishmentPage` (C6).

### daily_banner_count
- **Legacy**: `revenue_notifications.dailyNotificationsCount` (`int`, default `0`)
- **Descrizione**: quante volte al giorno il banner va mostrato (la finestra 10:00–20:00 è divisa in
  `daily_banner_count` slot).
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### daily_banner_shown_count
- **Legacy**: `revenue_notifications.dailyNotificationsDisplayedCount` (`int`, default `0`; incrementato dal frontend)
- **Descrizione**: quante volte il banner è già stato mostrato **oggi**.
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: incrementato **server-side** dall'endpoint che serve il banner; azzerato ogni notte da
  un job (come il cron `updateNotificationCount`).
- **Note**: nel legacy il client poteva falsarlo — nel target è di sola scrittura server.

### is_discount_limited_to_spots
- **Legacy**: `revenue_notifications.enableDiscountsLocFlag` (`tinyint(1)`, default `false`)
- **Descrizione**: lo sconto è ristretto alle postazioni elencate in
  [`catalog_pricing_revenue_discounted_spots`](catalog_pricing_revenue_discounted_spots.md) (vs. tutto
  lo stabilimento).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: se `true`, la figlia deve avere ≥1 riga.

### created_at / updated_at
- **Legacy**: `revenue_notifications.createdAt` / `revenue_notifications.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Campi legacy **non** migrati

| Campo legacy | Motivo |
|---|---|
| `revenue_notifications.discounts` (JSON) | colonna morta — mai scritta/letta, `NULL` ovunque |
| `revenue_notifications.enableDiscountsForSelectedLocations` (JSON) | → [`catalog_pricing_revenue_discounted_spots`](catalog_pricing_revenue_discounted_spots.md) |

## Valutazione JSON

Nessun JSON. `enableDiscountsForSelectedLocations` → tabella figlia; `discounts` → non migrata.

## Suggerimenti per le prestazioni

- 1 riga per property (poche righe totali). L'endpoint del banner: lettura + `UPDATE ... SET
  daily_banner_shown_count = daily_banner_shown_count + 1` server-side.
- Job notturno di reset: `UPDATE ... SET daily_banner_shown_count = 0 WHERE daily_banner_shown_count <> 0`.

## Implementazione .NET

Aggregate root (1:1 con la property, gestisce la propria collezione di postazioni scontate come invariante:
se `IsDiscountLimitedToSpots` è `true` deve avere ≥1 `DiscountedSpots`). Modulo `Module.Catalog`, schema
`catalog`. Nessun enum proprio.

```csharp
// Module.Catalog.Domain.Entities/RevenueSettings.cs
namespace Module.Catalog.Domain.Entities;

public sealed class RevenueSettings : AggregateRoot, IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PropertyId { get; private set; }
	public Property Property { get; private set; } = null!;
	public bool IsPriceDiscountEnabled { get; private set; }
	public bool IsShownOnPublicPage { get; private set; }
	public short DailyBannerCount { get; private set; }
	public short DailyBannerShownCount { get; private set; }
	public bool IsDiscountLimitedToSpots { get; private set; }

	private readonly List<RevenueDiscountedSpot> _discountedSpots = new();
	public IReadOnlyCollection<RevenueDiscountedSpot> DiscountedSpots => _discountedSpots.AsReadOnly();

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public void RegisterBannerShown() => DailyBannerShownCount++; // server-side, mai dal client
	public void ResetDailyBannerCount() => DailyBannerShownCount = 0; // job notturno

	public void Delete(DateTime utcNow) => DeletedAt = utcNow;
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog.Infrastructure.Configurations/RevenueSettingsConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class RevenueSettingsConfiguration : IEntityTypeConfiguration<RevenueSettings>
{
	public void Configure(EntityTypeBuilder<RevenueSettings> builder)
	{
		builder.ToTable("catalog_pricing_revenue_settings", "catalog", t => t.HasComment(
			"Configurazione per stabilimento della feature Revenue Management (prezzo suggerito e banner sconto in pagina pubblica)."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_revenue_settings");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento a cui si applica la configurazione Revenue Management.");

		builder.Property(x => x.IsPriceDiscountEnabled)
			.HasColumnName("is_price_discount_enabled")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Interruttore generale della feature \"notifiche prezzo scontato\".");

		builder.Property(x => x.IsShownOnPublicPage)
			.HasColumnName("is_shown_on_public_page")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Il banner sconto è visibile sulla pagina pubblica dello stabilimento / flusso di prenotazione (indipendente da marketing_event.showOnEstablishmentPage di C6).");

		builder.Property(x => x.DailyBannerCount)
			.HasColumnName("daily_banner_count")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di volte al giorno in cui mostrare il banner (finestra 10:00–20:00 divisa in altrettanti slot).");

		builder.Property(x => x.DailyBannerShownCount)
			.HasColumnName("daily_banner_shown_count")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di volte in cui il banner è già stato mostrato oggi; incrementata server-side, azzerata ogni notte.");

		builder.Property(x => x.IsDiscountLimitedToSpots)
			.HasColumnName("is_discount_limited_to_spots")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("Se true, lo sconto è ristretto alle postazioni elencate in catalog_pricing_revenue_discounted_spots invece che a tutto lo stabilimento.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete.");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_pricing_revenue_settings_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.DiscountedSpots)
			.WithOne(s => s.Settings)
			.HasForeignKey(s => s.SettingsId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.PropertyId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_pricing_revenue_settings_property");
	}
}
```

**Note di migrazione**: colonne copiate 1:1 (rinominate in `snake_case`); `discounts` (JSON) **non**
migrata (colonna morta legacy); `enableDiscountsForSelectedLocations` esplosa in
[`catalog_pricing_revenue_discounted_spots`](catalog_pricing_revenue_discounted_spots.md); il
`UNIQUE(property_id)` non esisteva nel legacy — in migrazione va deduplicato tenendo la riga più recente
per property prima di applicare il vincolo. La logica "quale slot orario mostrare ora" (oggi in
`Revenue.js`/`ReserveArticle.js` lato frontend) va riscritta come application service server-side che
legge `DailyBannerCount`/`DailyBannerShownCount` e chiama `RegisterBannerShown()`.
