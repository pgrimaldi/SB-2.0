# Tabella `catalog_pricing_price_lists`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_price_lists` · **forma canonica**: `catalog.pricing_price_lists`.
>
> Rimpiazza la testata della legacy [`price_lists`](../../../analisi-pre/db/price_lists.md) (**~2.920 righe**
> stimate: `type` `package` ~1.400 / `additional` ~500; il ~59% è soft-deleted sul DB dev). I molti campi
> JSON diventano tabelle figlie. Fonti:
> [`analisi-pre/db/price_lists.md`](../../../analisi-pre/db/price_lists.md),
> [`analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md`](../../../analisi-pre/business-logic/stabilimento/_motore-di-prezzo-e-sconti.md),
> [`analisi-pre/business-logic/stabilimento/10-revenue.md`](../../../analisi-pre/business-logic/stabilimento/10-revenue.md),
> [`docs/03-gestione-dati.md` §3.6, §3.7](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

Testata di una **voce di listino** di uno stabilimento: un **pacchetto** (`kind = package`, es. "1
ombrellone + 2 lettini") o un **articolo aggiuntivo** (`kind = additional`, es. noleggio SUP). Una riga
per voce. Referenziata da C1 (`booking_lines.price_list_package_id`) e dalla feature Revenue Management.

*(La tabella si chiama `price_lists` — non `packages` — perché ogni riga può essere un pacchetto **o** un
articolo aggiuntivo, discriminati da `kind`.)*

Tabelle figlie:

| Aspetto | Tabella figlia |
|---|---|
| articoli inclusi nel pacchetto / pacchetti collegati a un `additional` | [`catalog_pricing_price_list_items`](catalog_pricing_price_list_items.md) |
| prezzo base per settore | [`catalog_pricing_price_list_sector_prices`](catalog_pricing_price_list_sector_prices.md) |
| tariffe base per periodo tariffario (giornaliero/weekend/7-14gg/mensile/½ giornata) | [`catalog_pricing_price_list_period_prices`](catalog_pricing_price_list_period_prices.md) |
| prezzo **per numero di giorni** (griglia 1–90, calcolato + override manuale) | [`catalog_pricing_price_list_stay_prices`](catalog_pricing_price_list_stay_prices.md) |
| override di prezzo per **data di calendario** + date festive | [`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md) |
| finestre di prezzo ridotto (match esatto intervallo) | [`catalog_pricing_price_list_reduced_windows`](catalog_pricing_price_list_reduced_windows.md) |
| prezzo suggerito dinamico (Revenue Mgmt) | [`catalog_pricing_suggested_prices`](catalog_pricing_suggested_prices.md) |

Correzioni rispetto al legacy:

- **`type` stringa → `enum kind`** (`package`/`additional`).
- **`priceCalculation`** ("proportional" / "daily" / "Daily price with weekend" / "logarithmic" / … +
  refusi + `""`, 8 varianti) → **lookup** [`catalog_pricing_calculation_methods`](#lookup-catalog_pricing_calculation_methods)
  (permette di mappare esplicitamente le varianti storiche a un canonico in migrazione, cosa che un ENUM
  non documenterebbe).
- **`monthlySetting` / `weekendSetting`** (6 varianti ciascuno per ~3 concetti reali) → `enum`.
- **`packageName`** (colonna di primo livello, **sempre `""` in produzione**, doppione di
  `details.PackageName`) → **non migrata** (per `price_lists`; è invece **realmente popolata** in
  `hotel_price_lists` — lì migrata, vedi [`catalog_agreement_price_lists`](catalog_agreement_price_lists.md)).
- **`isDeleted` → `deleted_at`**.
- **`enableOnlineReservation` + `enableOnlineOnly`** (2 booleani con una combinazione contraddittoria) →
  **`enum sales_channel`** (`manager_only` / `manager_and_online` / `online_only`).
- **`name_it` non esiste**: nel legacy `details.PackageName` è testo libero digitato dal manager, non
  bilingue (verificato in `AddPackage.js`) → colonna singola `name`.
- **JSON doppiamente incapsulato** (`adddiscounts`-style) e i due percorsi di scrittura divergenti
  (`pricelist.controller.js` ORM + `revenue.controller.js` SQL diretto) → un unico livello dati.
- **`suggestedPriceDetails`** (output Revenue Mgmt, riscritto in-place via SQL, per anno) → tabella figlia
  dedicata.
- **`edit_price_data`** (griglia prezzi per numero di giorni 1–90, con `defaultPrice` calcolato e
  `price`/`isPriceChange` per gli override manuali del gestore) →
  [`catalog_pricing_price_list_stay_prices`](catalog_pricing_price_list_stay_prices.md) (nel legacy sta
  dentro `detailPrices[]`).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_price_lists` | `id` | PK | — |
| `uq_catalog_pricing_price_lists_public_id` | `(public_id)` | UNIQUE | referenziato cross-modulo (C1) |
| `ix_..._property_kind` | `(property_id, kind) WHERE deleted_at IS NULL` | parziale | listino di uno stabilimento (sostituisce `idx_type_propertyId_isDelete`) |
| `ix_..._property_default` | `(property_id) WHERE is_default AND deleted_at IS NULL` | parziale | voce predefinita (Revenue Mgmt / prezzo minimo) ✅ **Creato il 2026-10-06** (migration `AddMissingDesignedIndexes`): a DB mancava perché la causa era EF: due `HasIndex` sulle stesse colonne senza nome sono lo stesso indice, e il secondo sovrascrive il primo. |
| `ix_..._online` | `(property_id) WHERE sales_channel <> 'manager_only' AND deleted_at IS NULL` | parziale | voci nel calcolo prezzo minimo pubblico |

## Campi

### id
- **Legacy**: `price_lists.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità (id legacy preservati; la riga anomala `id = 0` scartata).
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da tutte le figlie `catalog_pricing_price_list_*`.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile, **copiato cross-modulo** (C1 `booking_lines.price_list_package_id`,
  snapshot pacchetto nella prenotazione).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C1.

### property_id
- **Legacy**: `price_lists.propertyId`
- **Descrizione**: stabilimento proprietario.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`.

### kind
- **Legacy**: `price_lists.type` (`package` / `additional`)
- **Descrizione**: tipo di voce di listino.
- **Tipo**: `enum catalog.price_list_kind` — valori: `package`, `additional`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: guida quali figlie sono rilevanti (`additional` non usa sector/period prices; il prezzo
  semplice sta in `base_price`).

### name
- **Legacy**: `price_lists.details.PackageName` / `details.AdditionalItem`
- **Descrizione**: nome della voce di listino (pacchetto o articolo aggiuntivo).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **colonna singola** — verificato in `front-pre/src/components/Price/AddPackage.js` (un solo
  input `PackageName`): nel legacy il nome è testo libero digitato dal manager, **non** bilingue
  (`name_it` rimosso). Vive **qui**, non nel JSON `details` né nella colonna morta `packageName`. Il sito
  pubblico è multilingua: se in futuro serve mostrare il nome nella lingua del cliente è una **decisione
  di prodotto** → tabella figlia `catalog_pricing_price_list_translations` (coerente con
  `catalog_countries`) o colonna `name_i18n jsonb`. Non fatto ora (il legacy non lo fa).

### max_people
- **Legacy**: `price_lists.details.MaxPeopleCount`
- **Descrizione**: numero massimo di persone coperte dal pacchetto.
- **Tipo**: `smallint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### calculation_method_id
- **Legacy**: `price_lists.priceCalculation` (8 varianti sporche)
- **Descrizione**: metodo di calcolo del prezzo per prenotazioni multi-giorno.
- **Tipo**: `smallint` (FK).
- **Default**: nessuno.
- **Nullabile**: Sì (`""` legacy → `NULL` = "non impostato").
- **Relazioni**: **FK** → [`catalog_pricing_calculation_methods.id`](#lookup-catalog_pricing_calculation_methods).
- **Note**: lookup (non ENUM) per poter mappare `"Daily price"` → `daily_with_weekend`,
  `"proportional price"` → `proportional`, ecc. in modo tracciabile.

### monthly_setting
- **Legacy**: `price_lists.monthlySetting` (6 varianti per ~3 concetti)
- **Descrizione**: come conteggiare la durata "mese" nel calcolo mensile.
- **Tipo**: `enum catalog.monthly_setting` — valori: `effective_month_duration`, `actual_month_duration`,
  `fixed30days`.
- **Default**: `effective_month_duration`.
- **Nullabile**: Sì (`""` → `NULL`).
- **Relazioni**: —.
- **Note**: `"monthly"` / `"other"` / varianti di capitalizzazione mappate in migrazione.

### weekend_setting
- **Legacy**: `price_lists.weekendSetting` (6 varianti per 3 concetti)
- **Descrizione**: quali giorni contano come weekend per la tariffa weekend.
- **Tipo**: `enum catalog.weekend_setting` — valori: `saturday_sunday`, `sunday_only`.
- **Default**: `saturday_sunday`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### sales_channel
- **Legacy**: `price_lists.details.enableOnlineReservation` (bookable online) + `details.enableOnlineOnly` (solo online)
- **Descrizione**: su quali canali la voce è vendibile. Collassa i 2 booleani legacy (la combinazione
  `bookable=false & only=true` era **contraddittoria**): 3 stati validi.
- **Tipo**: `enum catalog.sales_channel` — valori: `manager_only` (solo gestionale), `manager_and_online`
  (entrambi), `online_only` (solo sito pubblico).
- **Default**: `manager_only`.
- **Nullabile**: No.
- **Relazioni**: solo `manager_and_online` / `online_only` concorrono al calcolo del prezzo minimo mostrato
  in ricerca pubblica.
- **Migrazione**: `enableOnlineReservation=false` → `manager_only`; `true & enableOnlineOnly=false` →
  `manager_and_online`; `true & enableOnlineOnly=true` → `online_only`; `false & true` (invalido) →
  `manager_only`.

### is_default
- **Legacy**: `price_lists.details.defaultPackage` (`"true"`/`"false"` — letto via `JSON_EXTRACT` da `getLowestPrice` e Revenue Mgmt)
- **Descrizione**: pacchetto "predefinito" dello stabilimento (base per prezzo minimo e prezzo suggerito).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: al più uno `is_default = true` per property (validato in applicazione; opzionale `UNIQUE`
  parziale).

### is_default_online
- **Legacy**: `price_lists.details.defaultPackageOnline`
- **Descrizione**: pacchetto predefinito per il contesto online (significato esatto **da chiarire** col
  committente — nessun consumer individuato nel legacy).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### is_area_package
- **Legacy**: `price_lists.details.areaItemsFlag`
- **Descrizione**: il pacchetto è un "Area Package" (copre un'area con più posti, non una singola
  postazione). Gli articoli d'area stanno in
  [`catalog_pricing_price_list_items`](catalog_pricing_price_list_items.md).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: nel legacy usato solo lato frontend (`selectedAreaItems.length` come segnale) — nel target è
  un flag esplicito.

### is_cumulative
- **Legacy**: `price_lists.details.isCumulativeFlag`
- **Descrizione**: effetto esatto **da chiarire** (nel legacy salvato ma non collegato). Ipotesi: il
  pacchetto è cumulabile con altri articoli/sconti.
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### allows_public_holidays
- **Legacy**: `price_lists.allowPublicHolidays`
- **Descrizione**: il pacchetto ha una tariffa dedicata per le date festive (in
  [`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md) con `kind = holiday`).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_by_user_id
- **Legacy**: `price_lists.createdBy`
- **Descrizione**: operatore che ha creato la voce di listino.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 `identity_access_users`.

### created_at / updated_at
- **Legacy**: `price_lists.createdAt` / `price_lists.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: `price_lists.isDeleted`
- **Descrizione**: soft-delete (ex `deletepricelist`).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: ~59% delle righe legacy sono `isDeleted = 1` — in migrazione vanno portate con `deleted_at`
  valorizzato (servono per risolvere gli snapshot pacchetto delle prenotazioni storiche).

## Lookup `catalog_pricing_calculation_methods`

| Colonna | Tipo | Note |
|---|---|---|
| `id` | `smallint` IDENTITY | PK |
| `code` | `varchar(40)` UNIQUE | `proportional`, `daily`, `daily_with_weekend`, `logarithmic` |
| `is_active` | `boolean` | — |
| `created_at`/`updated_at`/`deleted_at` | `timestamptz` | standard |

Seed: 4 righe. Mapping migrazione: `"proportional"`/`"proportional price"`/`"Proportional price"` →
`proportional`; `"daily"`/`"Daily price"` → `daily`; `"Daily price with weekend"` → `daily_with_weekend`;
`"logarithmic"` → `logarithmic`; `""` → `NULL`. **Etichette da risorse di localizzazione** (chiave =
`code`).

## Valutazione JSON

Nessun JSON. I 6 campi JSON del legacy (`details`, `mainPrices`, `detailPrices`, `packageItemString`,
`holidayDates`, `suggestedPriceDetails`) diventano: flag/scalari su questa tabella + 7 tabelle figlie
(`items`, `sector_prices`, `period_prices`, `stay_prices`, `date_prices`, `reduced_windows`,
`suggested_prices`). `packageItemString` (etichetta compatta generata client-side, es. `"2S+1U"`)
**non** si persiste: si ricalcola in presentazione dalle righe di
[`catalog_pricing_price_list_items`](catalog_pricing_price_list_items.md).

## Suggerimenti per le prestazioni

- Indici parziali per `(property_id, kind)` e per `is_default` / `sales_channel` coprono i lookup di
  editing e di calcolo prezzo.
- Il **prezzo minimo pubblico** e il **preventivo** (`POST /bookings/quote`) usano il motore prezzo
  server-side ([`docs/03-gestione-dati.md` §3.7](../../../progettazione/03-gestione-dati.md)) che risolve
  periodo→prezzo via FK, non `LIKE` su nomi. Il prezzo minimo per la ricerca è materializzato nel read
  model.
- La cache Redis del legacy (chiave per `propertyId`) è sostituita da invalidazione a eventi del read
  model.

## Implementazione .NET

**Aggregate root** del cluster pricing, modulo `Module.Catalog`, schema `catalog`. `PriceList` è la radice
di consistenza per l'intera gerarchia `catalog_pricing_price_list_*` (items, sector/period/stay/date
prices, reduced windows, suggested prices): le figlie sono **entità a sé stanti** con FK reali anche fra
loro (es. `stay_prices.period_price_id → period_prices.id`), incompatibile con `OwnsMany`, ma vivono nello
stesso confine transazionale e si cancellano in cascata con la radice (`ON DELETE CASCADE`).

### Enum propri

```csharp
// Module.Catalog/Domain/Enums/PriceListKind.cs
public enum PriceListKind { Package = 0, Additional = 1 }

// Module.Catalog/Domain/Enums/MonthlySetting.cs
public enum MonthlySetting { EffectiveMonthDuration = 0, ActualMonthDuration = 1, Fixed30Days = 2 }

// Module.Catalog/Domain/Enums/WeekendSetting.cs
public enum WeekendSetting { SaturdaySunday = 0, SundayOnly = 1 }

// Module.Catalog/Domain/Enums/SalesChannel.cs
public enum SalesChannel { ManagerOnly = 0, ManagerAndOnline = 1, OnlineOnly = 2 }
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<PriceListKind>("catalog", "price_list_kind");
modelBuilder.HasPostgresEnum<MonthlySetting>("catalog", "monthly_setting");
modelBuilder.HasPostgresEnum<WeekendSetting>("catalog", "weekend_setting");
modelBuilder.HasPostgresEnum<SalesChannel>("catalog", "sales_channel");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<PriceListKind>("catalog.price_list_kind");
dataSourceBuilder.MapEnum<MonthlySetting>("catalog.monthly_setting");
dataSourceBuilder.MapEnum<WeekendSetting>("catalog.weekend_setting");
dataSourceBuilder.MapEnum<SalesChannel>("catalog.sales_channel");
```

`calculation_method_id` **non** è un ENUM: è FK verso il lookup tabellare `CalculationMethod` sotto, per
poter mappare esplicitamente le 8 varianti storiche di `priceCalculation` in modo tracciabile.

### Lookup `CalculationMethod`

```csharp
// Module.Catalog/Domain/Entities/CalculationMethod.cs
public sealed class CalculationMethod : ISoftDeletable
{
	public short Id { get; private set; }
	public string Code { get; private set; } = default!; // proportional | daily | daily_with_weekend | logarithmic
	public bool IsActive { get; private set; }
	public DateTime? DeletedAt { get; private set; }
	public void Delete(DateTime utcNow) => DeletedAt = utcNow;
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog/Infrastructure/Configurations/CalculationMethodConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class CalculationMethodConfiguration : IEntityTypeConfiguration<CalculationMethod>
{
	public void Configure(EntityTypeBuilder<CalculationMethod> builder)
	{
		builder.ToTable("pricing_calculation_methods", "catalog", t => t.HasComment(
			"Lookup dei metodi di calcolo del prezzo multi-giorno di una voce di listino."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_calculation_methods");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasMaxLength(40)
			.IsRequired()
			.HasComment("Codice canonico del metodo (proportional, daily, daily_with_weekend, logarithmic).");
		builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_catalog_pricing_calculation_methods_code");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasComment("Metodo selezionabile nell'editor prezzi.");
	}
}
```

### `PriceListConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class PriceListConfiguration : IEntityTypeConfiguration<PriceList>
{
	public void Configure(EntityTypeBuilder<PriceList> builder)
	{
		builder.ToTable("pricing_price_lists", "catalog", t => t.HasComment(
			"Voce di listino di uno stabilimento: un pacchetto o un articolo aggiuntivo."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_lists");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.HasComment("Id pubblico stabile, copiato in C1 booking_lines.price_list_package_id.");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_pricing_price_lists_public_id");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario della voce di listino.");
		builder.HasOne<Property>()
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_pricing_price_lists_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.HasColumnType("catalog.price_list_kind")
			.IsRequired()
			.HasComment("Tipo di voce: pacchetto o articolo aggiuntivo.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome della voce di listino, testo libero digitato dal gestore.");

		builder.Property(x => x.BasePrice)
			.HasColumnName("base_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo di riferimento: prezzo di vendita per un additional, fallback per un package.");

		builder.Property(x => x.MaxPeople)
			.HasColumnName("max_people")
			.HasComment("Numero massimo di persone coperte dal pacchetto.");

		builder.Property(x => x.CalculationMethodId)
			.HasColumnName("calculation_method_id")
			.HasComment("Metodo di calcolo del prezzo multi-giorno (NULL = non impostato).");
		builder.HasOne<CalculationMethod>()
			.WithMany()
			.HasForeignKey(x => x.CalculationMethodId)
			.HasConstraintName("fk_catalog_pricing_price_lists_calculation_method")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.MonthlySetting)
			.HasColumnName("monthly_setting")
			.HasColumnType("catalog.monthly_setting")
			.HasDefaultValue(MonthlySetting.EffectiveMonthDuration)
			.HasComment("Come conteggiare la durata 'mese' nel calcolo mensile.");

		builder.Property(x => x.WeekendSetting)
			.HasColumnName("weekend_setting")
			.HasColumnType("catalog.weekend_setting")
			.HasDefaultValue(WeekendSetting.SaturdaySunday)
			.HasComment("Quali giorni contano come weekend per la tariffa weekend.");

		builder.Property(x => x.SalesChannel)
			.HasColumnName("sales_channel")
			.HasColumnType("catalog.sales_channel")
			.HasDefaultValue(SalesChannel.ManagerOnly)
			.IsRequired()
			.HasComment("Canali su cui la voce è vendibile (gestionale / online / entrambi).");

		builder.Property(x => x.IsDefault)
			.HasColumnName("is_default")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Pacchetto predefinito dello stabilimento (base per prezzo minimo e Revenue Mgmt).");

		builder.Property(x => x.IsDefaultOnline)
			.HasColumnName("is_default_online")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Pacchetto predefinito per il contesto online (semantica da confermare col committente).");

		builder.Property(x => x.IsAreaPackage)
			.HasColumnName("is_area_package")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Il pacchetto copre un'area con più posti anziché una singola postazione.");

		builder.Property(x => x.IsCumulative)
			.HasColumnName("is_cumulative")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("Pacchetto cumulabile con altri articoli/sconti (semantica esatta da chiarire).");

		builder.Property(x => x.AllowsPublicHolidays)
			.HasColumnName("allows_public_holidays")
			.HasDefaultValue(false)
			.IsRequired()
			.HasComment("La voce ha una tariffa dedicata per le date festive.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access_users, copiato — nessuna FK cross-schema.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della voce di listino (ex isDeleted).");

		builder.HasIndex(x => new { x.PropertyId, x.Kind })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_lists_property_kind");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("is_default AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_lists_property_default");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("sales_channel <> 'manager_only' AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_lists_online");

		builder.HasMany(x => x.Items).WithOne().HasForeignKey(i => i.PriceListId);
		builder.HasMany(x => x.SectorPrices).WithOne().HasForeignKey(s => s.PriceListId);
		builder.HasMany(x => x.PeriodPrices).WithOne().HasForeignKey(p => p.PriceListId);
		builder.HasMany(x => x.DatePrices).WithOne().HasForeignKey(d => d.PriceListId);
		builder.HasMany(x => x.ReducedWindows).WithOne().HasForeignKey(w => w.PriceListId);
	}
}
```

`PriceList` implementa `ISoftDeletable`/`IAuditable` (`created_at`/`updated_at`/`deleted_at` reali).

### Relazioni verso altri moduli

- `property_id` → `catalog_properties` (stesso schema `catalog`): FK reale.
- `created_by_user_id` → `identity_access_users.public_id` (C5): id copiato, nessuna FK cross-schema.
- `public_id` copiato in C1 `booking_lines.price_list_package_id` (snapshot pacchetto): nessuna FK.

### Note di migrazione

- Doppio `JSON.parse` di `details`/`mainPrices`/`detailPrices`.
- Riga anomala `id = 0` scartata.
- `type` → `kind`; `priceCalculation` (8 varianti sporche) → `calculation_method_id` via lookup;
  `monthlySetting`/`weekendSetting` (6 varianti ciascuno) → enum canonici.
- `enableOnlineReservation`/`enableOnlineOnly` → `sales_channel` (incluso il caso contraddittorio
  `false & true` → `manager_only`, come da scheda).
- `packageName` (colonna morta, sempre `""` su `price_lists`) **non** migrata.
- ~59% delle righe legacy `isDeleted = 1` → migrate con `deleted_at` valorizzato (servono a risolvere gli
  snapshot pacchetto delle prenotazioni storiche in C1).
