# Tabella `catalog_agreement_price_list_period_prices`

> **Dominio**: C2 — `catalog` · **sottoarea**: `agreement` · **schema**: `catalog` · **nome flat**:
> `catalog_agreement_price_list_period_prices` · **forma canonica**:
> `catalog.agreement_price_list_period_prices`.
>
> Rimpiazza `hotel_price_lists.detailPrices` (`[{id, name, half_day_price, half_day_weekend_price,
> daily_price, weekend_price, days_7_price, days_14_price, monthly_price, edit_price_data}]`). Analoga a
> [`catalog_pricing_price_list_period_prices`](catalog_pricing_price_list_period_prices.md), **ma senza FK a
> `seasons_periods`**: i listini hotel non hanno una tabella stagioni collegata (nomi periodo liberi).
> Fonti: [`analisi-pre/db/hotel_price_lists.md`](../../../analisi-pre/db/hotel_price_lists.md),
> [`analisi-pre/db/price_lists.md`](../../../analisi-pre/db/price_lists.md).

---

## Descrizione

Prezzi per **periodo tariffario** di un listino hotel (giornaliero / weekend / mezza giornata / mezza
giornata weekend / 7 giorni / 14 giorni / mese). Una riga per (listino × periodo). Il periodo è
identificato da un'**etichetta testuale libera** (`period_label`, es. "Fila 1 Bassa stagione") — nessun
matching automatico con stagioni, coerente col legacy.

Gli override per singola data (`detailPrices[].edit_price_data`) → tabella figlia
`catalog_agreement_price_list_date_prices` (analoga a
[`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md); in produzione hotel
`holidayDates` è sempre `[]` e gli override sono rari — vedi nota).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_agreement_price_list_period_prices` | `id` | PK | — |
| `ix_..._price_list` | `(price_list_id) WHERE deleted_at IS NULL` | parziale | prezzi di un listino |

## Campi

### id
- **Legacy**: `hotel_price_lists.detailPrices[].id` (id locale nel JSON)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata dall'eventuale figlia date-prices.

### price_list_id
- **Legacy**: `hotel_price_lists.id`
- **Descrizione**: listino/voucher hotel.
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_agreement_price_lists.id`](catalog_agreement_price_lists.md)
  `ON DELETE CASCADE`.

### period_label
- **Legacy**: `hotel_price_lists.detailPrices[].name`
- **Descrizione**: etichetta del periodo tariffario (testo libero — nessun collegamento a
  `seasons_periods`).
- **Tipo**: `varchar(120)`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` = tariffa unica del listino).
- **Relazioni**: —.
- **Note**: se in futuro si vuole agganciare i listini hotel alle stagioni della property, aggiungere una
  FK opzionale a [`catalog_pricing_tariff_periods`](catalog_pricing_tariff_periods.md) — oggi
  esplicitamente assente.

### half_day_price / half_day_weekend_price / daily_price / weekend_price / days_7_price / days_14_price / monthly_price
- **Legacy**: `detailPrices[].half_day_price` / `.half_day_weekend_price` / `.daily_price` / `.weekend_price` / `.days_7_price` / `.days_14_price` / `.monthly_price` (stringhe)
- **Descrizione**: prezzi del listino per la relativa modalità di durata nel periodo indicato.
- **Tipo**: `numeric(12,2)` ciascuno.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: della riga contenitore / —
- **Descrizione**: standard.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: —.

## Tabella figlia `catalog_agreement_price_list_date_prices` (se usata)

Analoga a [`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md):
`(id, period_price_id FK, price_date date, price numeric(12,2), kind enum(override|holiday), timestamps)`.
In produzione hotel gli override sono rari e `holidayDates` sempre `[]` — la tabella può nascere vuota.

## Valutazione JSON

Nessun JSON — `detailPrices[]` → righe; `edit_price_data[]` → figlia; stringhe → `numeric`;
`detailPrices[].name` resta etichetta (nessuna FK stagioni, coerente col legacy).

## Suggerimenti per le prestazioni

- ~poche righe per listino. Il motore prezzo hotel riusa lo stesso value object di `Pricing`, con la
  differenza che risolve il periodo per `period_label` (o tariffa unica se `NULL`).

## Implementazione .NET

Entità figlia di [`AgreementPriceList`](catalog_agreement_price_lists.md); radice per la figlia
[`AgreementPriceListDatePrice`](catalog_agreement_price_list_date_prices.md) (override puntuali per
data) e, aggiunta per **D-63**, per
[`AgreementPriceListStayPrice`](catalog_agreement_price_list_stay_prices.md) (override manuali per
numero di giorni, collezione `StayPriceOverrides` — gemella di
`PriceListPeriodPrice.StayPriceOverrides` lato pricing). A
differenza dell'analoga di `pricing`, `PeriodLabel` è testo libero — **nessuna FK** a
`catalog_pricing_tariff_periods` (assente per design, coerente col legacy). Modulo `Module.Catalog`, schema
`catalog`. Nessun enum proprio.

```csharp
// Module.Catalog.Domain.Entities/AgreementPriceListPeriodPrice.cs
namespace Module.Catalog.Domain.Entities;

public sealed class AgreementPriceListPeriodPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public AgreementPriceList PriceList { get; private set; } = null!;
	public string? PeriodLabel { get; private set; }
	public decimal? HalfDayPrice { get; private set; }
	public decimal? HalfDayWeekendPrice { get; private set; }
	public decimal? DailyPrice { get; private set; }
	public decimal? WeekendPrice { get; private set; }
	public decimal? Days7Price { get; private set; }
	public decimal? Days14Price { get; private set; }
	public decimal? MonthlyPrice { get; private set; }

	private readonly List<AgreementPriceListDatePrice> _datePrices = new();
	public IReadOnlyCollection<AgreementPriceListDatePrice> DatePrices => _datePrices.AsReadOnly();

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public void Delete(DateTime utcNow) => DeletedAt = utcNow;
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog.Infrastructure.Configurations/AgreementPriceListPeriodPriceConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class AgreementPriceListPeriodPriceConfiguration
	: IEntityTypeConfiguration<AgreementPriceListPeriodPrice>
{
	public void Configure(EntityTypeBuilder<AgreementPriceListPeriodPrice> builder)
	{
		builder.ToTable("catalog_agreement_price_list_period_prices", "catalog", t => t.HasComment(
			"Prezzi per periodo tariffario (etichetta libera) di un listino/voucher hotel, per modalità di durata."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_list_period_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.HasColumnType("integer")
			.IsRequired()
			.HasComment("Listino/voucher hotel a cui appartiene il prezzo di periodo.");

		builder.Property(x => x.PeriodLabel)
			.HasColumnName("period_label")
			.HasMaxLength(120)
			.HasComment("Etichetta libera del periodo tariffario; NULL = tariffa unica del listino (nessun collegamento a stagioni).");

		builder.Property(x => x.HalfDayPrice).HasColumnName("half_day_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo mezza giornata nel periodo.");
		builder.Property(x => x.HalfDayWeekendPrice).HasColumnName("half_day_weekend_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo mezza giornata weekend nel periodo.");
		builder.Property(x => x.DailyPrice).HasColumnName("daily_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo giornaliero nel periodo.");
		builder.Property(x => x.WeekendPrice).HasColumnName("weekend_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo weekend nel periodo.");
		builder.Property(x => x.Days7Price).HasColumnName("days_7_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo per soggiorno di 7 giorni nel periodo.");
		builder.Property(x => x.Days14Price).HasColumnName("days_14_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo per soggiorno di 14 giorni nel periodo.");
		builder.Property(x => x.MonthlyPrice).HasColumnName("monthly_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo mensile nel periodo.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete.");

		builder.HasOne(x => x.PriceList)
			.WithMany(pl => pl.PeriodPrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_agreement_price_list_period_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasMany(x => x.DatePrices)
			.WithOne(d => d.PeriodPrice)
			.HasForeignKey(d => d.PeriodPriceId)
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.PriceListId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_period_prices_price_list");
	}
}
```

**Note di migrazione**: ogni elemento di `detailPrices[]` diventa una riga (`name` → `period_label`,
prezzi da stringa a `numeric`); l'`id` locale del JSON non si preserva (nuova identità `bigint`). Il
motore prezzo hotel risolve il periodo per `PeriodLabel` (match testuale in Application, non a DB) o
applica la tariffa unica quando `PeriodLabel IS NULL`. In produzione la tabella è popolata ma gli override
puntuali (`edit_price_data`) sono rari.
