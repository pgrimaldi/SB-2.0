# Tabella `catalog_pricing_price_list_period_prices`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_price_list_period_prices` · **forma canonica**: `catalog.pricing_price_list_period_prices`.
>
> Rimpiazza l'array JSON `price_lists.detailPrices` — le **tariffe base** per periodo (`half_day_price`,
> `half_day_weekend_price`, `daily_price`, `weekend_price`, `days_7_price`, `days_14_price`,
> `monthly_price`). Il sotto-array `edit_price_data[]` (griglia prezzi per **numero di giorni** 1–90) va in
> [`catalog_pricing_price_list_stay_prices`](catalog_pricing_price_list_stay_prices.md); gli override per
> **data di calendario** e i festivi in
> [`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md). Fonti:
> [`analisi-pre/db/price_lists.md` (campo `detailPrices`)](../../../analisi-pre/db/price_lists.md),
> [`analisi-pre/db/seasons_periods.md`](../../../analisi-pre/db/seasons_periods.md).

---

## Descrizione

Il **dettaglio tariffario per periodo** di un pacchetto: per ogni periodo tariffario dello stabilimento,
i prezzi per giornata / weekend / mezza giornata / mezza giornata weekend / 7 giorni / 14 giorni / mese.
Una riga per (pacchetto × periodo tariffario). Nel legacy il `name` del periodo era matchato per
**substring `LIKE`** contro `seasons_periods.periods[].name` (`getLowestPrice`) — correlazione fragile,
qui sostituita da FK verso il **gruppo di periodo** di
[`catalog_pricing_tariff_periods`](catalog_pricing_tariff_periods.md).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_price_list_period_prices` | `id` | PK | — |
| `uq_..._package_period` | `(price_list_id, tariff_period_group) WHERE deleted_at IS NULL` | UNIQUE parziale | un prezzo per (pacchetto, periodo) |
| `ix_..._period` | `(tariff_period_group)` | — | risoluzione prezzo per data (via periodo) |

## Campi

### id
- **Legacy**: `price_lists.detailPrices[].id` (id locale nel JSON, non stabile fra le forme)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da
  [`catalog_pricing_price_list_date_prices.period_price_id`](catalog_pricing_price_list_date_prices.md).

### price_list_id
- **Legacy**: `price_lists.id`
- **Descrizione**: pacchetto a cui si riferiscono i prezzi.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_price_lists.id`](catalog_pricing_price_lists.md) `ON DELETE CASCADE`.

### tariff_period_group
- **Legacy**: `price_lists.detailPrices[].name` (matchato `LIKE` contro `seasons_periods.periods[].name`)
- **Descrizione**: periodo tariffario a cui si applicano i prezzi.
- **Tipo**: `smallint` (o `uuid`) — la `period_group_key` di
  [`catalog_pricing_tariff_periods`](catalog_pricing_tariff_periods.md) per la stagione della property.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` = tariffa "senza periodo" / default del pacchetto, per i listini che non
  usano stagioni).
- **Relazioni**: **FK** composta `(season_id_implicito, tariff_period_group)` → gruppo di righe
  `catalog_pricing_tariff_periods`. In pratica: FK verso una vista/relazione `(season_id,
  period_group_key)`; la `season_id` deriva da `package.property_id`.
- **Note**: in migrazione il `name` legacy si risolve al `period_group_key` con match esatto (fallback:
  match case-insensitive / substring, con log dei casi ambigui).

### half_day_price / half_day_weekend_price / daily_price / weekend_price / days_7_price / days_14_price / monthly_price
- **Legacy**: `detailPrices[].half_day_price` / `.half_day_weekend_price` / `.daily_price` / `.weekend_price` / `.days_7_price` / `.days_14_price` / `.monthly_price` (tutte stringhe numeriche)
- **Descrizione**: prezzi del pacchetto per la relativa modalità di durata nel periodo indicato.
- **Tipo**: `numeric(12,2)` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì (una modalità non offerta = `NULL`).
- **Relazioni**: —.
- **Note**: `getLowestPrice` usava `min(daily_price, half_day_price)` dei periodi correnti — nel target il
  motore prezzo legge il campo pertinente alla durata richiesta; il minimo per la ricerca è
  materializzato nel read model.

### created_at / updated_at
- **Legacy**: della riga contenitore
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (modifica = riscrittura array)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON. `detailPrices[]` → una riga per periodo con 7 colonne prezzo tipizzate; `edit_price_data[]` (griglia per numero di giorni) → [`catalog_pricing_price_list_stay_prices`](catalog_pricing_price_list_stay_prices.md); `name` `LIKE` → FK al
gruppo di periodo.

## Suggerimenti per le prestazioni

- ~3 righe per pacchetto (bassa/media/alta stagione). Il motore prezzo: risolve `tariff_period_group`
  dalla data → 1 lettura per `(price_list_id, group)`.
- Le prenotazioni storiche (C1) portano **lo snapshot** del prezzo di riga — questa tabella serve al
  calcolo **corrente**, non alla ricostruzione storica.

## Implementazione .NET

**Entità figlia** dell'aggregate `PriceList` (modulo `Module.Catalog`, schema `catalog`), esposta come
collezione `PriceList.PeriodPrices`. È a sua volta radice di due figlie di secondo livello: gli override
per durata ([`catalog_pricing_price_list_stay_prices`](catalog_pricing_price_list_stay_prices.md), FK
`period_price_id`) e — opzionalmente — le deroghe per data
([`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md)).

Nessun enum proprio.

### `PriceListPeriodPriceConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class PriceListPeriodPriceConfiguration : IEntityTypeConfiguration<PriceListPeriodPrice>
{
	public void Configure(EntityTypeBuilder<PriceListPeriodPrice> builder)
	{
		builder.ToTable("pricing_price_list_period_prices", "catalog", t => t.HasComment(
			"Tariffe base (giornaliera/weekend/mezza giornata/7-14gg/mensile) di un pacchetto per periodo tariffario."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_period_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Pacchetto a cui si riferiscono i prezzi.");
		builder.HasOne<PriceList>()
			.WithMany(p => p.PeriodPrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_period_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.TariffPeriodGroup)
			.HasColumnName("tariff_period_group")
			.HasComment("Gruppo di periodo tariffario (period_group_key di catalog_pricing_tariff_periods per la stagione della property); NULL = tariffa senza periodo.");

		builder.Property(x => x.HalfDayPrice)
			.HasColumnName("half_day_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo mezza giornata nel periodo.");

		builder.Property(x => x.HalfDayWeekendPrice)
			.HasColumnName("half_day_weekend_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo mezza giornata weekend nel periodo.");

		builder.Property(x => x.DailyPrice)
			.HasColumnName("daily_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo giornaliero nel periodo.");

		builder.Property(x => x.WeekendPrice)
			.HasColumnName("weekend_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo giornaliero weekend nel periodo.");

		builder.Property(x => x.Days7Price)
			.HasColumnName("days_7_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo per soggiorno di 7 giorni nel periodo.");

		builder.Property(x => x.Days14Price)
			.HasColumnName("days_14_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo per soggiorno di 14 giorni nel periodo.");

		builder.Property(x => x.MonthlyPrice)
			.HasColumnName("monthly_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo mensile nel periodo.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della riga tariffaria.");

		builder.HasIndex(x => new { x.PriceListId, x.TariffPeriodGroup })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_period_prices_package_period");

		builder.HasIndex(x => x.TariffPeriodGroup)
			.HasDatabaseName("ix_catalog_pricing_price_list_period_prices_period");

		builder.HasMany(x => x.StayPriceOverrides).WithOne().HasForeignKey(s => s.PeriodPriceId);
	}
}
```

`PriceListPeriodPrice` implementa `ISoftDeletable` + `IAuditable`.

### Relazioni

- `price_list_id` → `PriceList` (stesso schema): FK reale, `WithMany(p => p.PeriodPrices)`, cascade.
- `tariff_period_group` → **nessuna FK EF**: nel legacy corrisponde a un *gruppo* di righe di
  `catalog_pricing_tariff_periods` (stesso `season_id` + `period_group_key`, tipicamente più righe per i
  periodi non contigui), non a una singola riga con vincolo `UNIQUE` — una `HasOne`/`HasForeignKey` non è
  esprimibile a livello di vincolo DB. Resta una colonna scalare indicizzata, risolta a runtime dal motore
  prezzo con `WHERE season_id = ? AND period_group_key = ?` su `catalog_pricing_tariff_periods`.
- Referenziata da `catalog_pricing_price_list_date_prices.period_price_id` e da
  `catalog_pricing_price_list_stay_prices.period_price_id` (FK reali configurate in quelle entità).

### Note di migrazione

- `detailPrices[]` → una riga per periodo; le 7 colonne prezzo da stringa a `numeric`.
- `name` legacy (matchato `LIKE` contro `seasons_periods.periods[].name`) → risoluzione a
  `period_group_key` con match esatto, fallback case-insensitive/substring con log dei casi ambigui.
- `edit_price_data[]` (griglia per numero di giorni) → esploso in
  [`catalog_pricing_price_list_stay_prices`](catalog_pricing_price_list_stay_prices.md), non su questa
  tabella.
