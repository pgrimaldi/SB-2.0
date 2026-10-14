# Tabella `catalog_agreement_price_list_date_prices`

> **Dominio**: C2 — `catalog` · **sottoarea**: `agreement` · **schema**: `catalog` · **nome flat**:
> `catalog_agreement_price_list_date_prices` · **forma canonica**:
> `catalog.agreement_price_list_date_prices`.
>
> Figlia di [`catalog_agreement_price_lists`](catalog_agreement_price_lists.md). Analoga a
> [`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md) per i listini
> hotel: rimpiazza `hotel_price_lists.detailPrices[].edit_price_data` (override per data) e
> `hotel_price_lists.holidayDates` (date festive). Fonti:
> [`analisi-pre/db/hotel_price_lists.md`](../../../analisi-pre/db/hotel_price_lists.md),
> [`analisi-pre/db/price_lists.md`](../../../analisi-pre/db/price_lists.md).

---

## Descrizione

Override di prezzo per **data di calendario** di un listino/voucher hotel (festivi, deroghe puntuali).
Una riga per (listino hotel × data × tipo).

**In produzione questa tabella è quasi sempre vuota**: sul DB dev `holidayDates` è `"[]"` su tutte le 68
righe e `allowPublicHolidays` sempre `0` (feature convenzioni entrata in produzione ad aprile 2026, poco
usata). Si crea per completezza e simmetria con il lato `pricing`; può nascere senza righe.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_agreement_price_list_date_prices` | `id` | PK | — |
| `uq_..._pricelist_date_kind` | `(price_list_id, price_date, kind) WHERE deleted_at IS NULL` | UNIQUE parziale | una deroga per (listino, data, tipo) |
| `ix_..._pricelist_date` | `(price_list_id, price_date) WHERE deleted_at IS NULL` | parziale | "c'è una deroga per il listino L il giorno D?" |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### price_list_id
- **Legacy**: `hotel_price_lists.id` (contenitore di `detailPrices`)
- **Descrizione**: listino/voucher hotel a cui si applica la deroga.
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_agreement_price_lists.id`](catalog_agreement_price_lists.md)
  `ON DELETE CASCADE`.

### period_price_id
- **Legacy**: la riga di `detailPrices` che conteneva l'`edit_price_data`
- **Descrizione**: periodo tariffario a cui la deroga è agganciata (`NULL` per le date festive globali del
  listino).
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_agreement_price_list_period_prices.id`](catalog_agreement_price_list_period_prices.md)
  `ON DELETE CASCADE`.

### kind
- **Legacy**: struttura di provenienza (`edit_price_data` → `override`; `holidayDates` → `holiday`)
- **Descrizione**: natura della deroga.
- **Tipo**: `enum catalog.date_price_kind` — valori: `override`, `holiday` (stesso enum di
  [`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### price_date
- **Legacy**: `edit_price_data[].date` / elemento di `holidayDates[]` (`"DD/MM/YYYY"`)
- **Descrizione**: la data a cui si applica la deroga.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: parsing `DD/MM/YYYY` → `date` in migrazione.

### price
- **Legacy**: `edit_price_data[].price` — per `holiday` non presente
- **Descrizione**: prezzo giornaliero per quella data. Valorizzato per `kind = override`; per
  `kind = holiday` può essere `NULL` (il motore prezzo applica la regola festiva del listino).
- **Tipo**: `numeric(12,2)`.
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

## Valutazione JSON

Nessun JSON — vedi
[`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md) (stesso principio).

## Suggerimenti per le prestazioni

- Tabella tipicamente vuota / pochissime righe. Il motore prezzo hotel controlla `(price_list_id,
  price_date)` prima di applicare la tariffa di periodo.

## Implementazione .NET

Entità figlia di [`AgreementPriceList`](catalog_agreement_price_lists.md), agganciata opzionalmente a un
[`AgreementPriceListPeriodPrice`](catalog_agreement_price_list_period_prices.md). Modulo `Module.Catalog`,
schema `catalog`. `kind` è l'enum **condiviso** `DatePriceKind` (`catalog.date_price_kind`) già usato da
[`catalog_pricing_price_list_date_prices`](catalog_pricing_price_list_date_prices.md) — non ridefinito qui.
Tabella tipicamente vuota o quasi in produzione (feature Convenzioni recente); modellata per completezza e
simmetria col lato `pricing`.

```csharp
// Module.Catalog.Domain.Entities/AgreementPriceListDatePrice.cs
namespace Module.Catalog.Domain.Entities;

public sealed class AgreementPriceListDatePrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public AgreementPriceList PriceList { get; private set; } = null!;
	public long? PeriodPriceId { get; private set; }
	public AgreementPriceListPeriodPrice? PeriodPrice { get; private set; }
	public DatePriceKind Kind { get; private set; }
	public DateOnly PriceDate { get; private set; }
	public decimal? Price { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public void Delete(DateTime utcNow) => DeletedAt = utcNow;
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog.Infrastructure.Configurations/AgreementPriceListDatePriceConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class AgreementPriceListDatePriceConfiguration
	: IEntityTypeConfiguration<AgreementPriceListDatePrice>
{
	public void Configure(EntityTypeBuilder<AgreementPriceListDatePrice> builder)
	{
		builder.ToTable("catalog_agreement_price_list_date_prices", "catalog", t => t.HasComment(
			"Override di prezzo per data di calendario (festivi, deroghe puntuali) di un listino/voucher hotel."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_list_date_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.HasColumnType("integer")
			.IsRequired()
			.HasComment("Listino/voucher hotel a cui si applica la deroga.");

		builder.Property(x => x.PeriodPriceId)
			.HasColumnName("period_price_id")
			.HasComment("Periodo tariffario a cui la deroga è agganciata; NULL per le date festive globali del listino.");

		builder.Property(x => x.Kind)
			.HasColumnName("kind")
			.IsRequired()
			.HasComment("Natura della deroga: override puntuale o data festiva.");

		builder.Property(x => x.PriceDate)
			.HasColumnName("price_date")
			.IsRequired()
			.HasComment("Data a cui si applica la deroga.");

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo giornaliero per la data; valorizzato per kind = override, può essere NULL per kind = holiday.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete.");

		builder.HasOne(x => x.PriceList)
			.WithMany(pl => pl.DatePrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_agreement_price_list_date_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.PeriodPrice)
			.WithMany(p => p.DatePrices)
			.HasForeignKey(x => x.PeriodPriceId)
			.HasConstraintName("fk_catalog_agreement_price_list_date_prices_period_price")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.PriceListId, x.PriceDate, x.Kind })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_agreement_price_list_date_prices_pricelist_date_kind");

		builder.HasIndex(x => new { x.PriceListId, x.PriceDate })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_date_prices_pricelist_date");
	}
}
```

**Note di migrazione**: `edit_price_data[].date` / elementi di `holidayDates[]` in formato `DD/MM/YYYY` →
parsing a `DateOnly`; `edit_price_data` → righe `kind = override` (con `price`); `holidayDates` → righe
`kind = holiday` (`price` tipicamente `NULL`). In produzione (DB dev, 68 righe listino) `holidayDates` è
sempre `"[]"` e `allowPublicHolidays` sempre `0`: la tabella nasce quasi certamente vuota, va comunque
creata per simmetria e per non bloccare l'eventuale uso futuro.
