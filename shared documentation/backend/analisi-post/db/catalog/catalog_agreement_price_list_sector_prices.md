# Tabella `catalog_agreement_price_list_sector_prices`

> **Dominio**: C2 — `catalog` · **sottoarea**: `agreement` · **schema**: `catalog` · **nome flat**:
> `catalog_agreement_price_list_sector_prices` · **forma canonica**:
> `catalog.agreement_price_list_sector_prices`.
>
> Rimpiazza `hotel_price_lists.mainPrices` (`[{name, value, halfday, seasonal}]`, `name` = nome settore).
> Analoga a [`catalog_pricing_price_list_sector_prices`](catalog_pricing_price_list_sector_prices.md). Fonti:
> [`analisi-pre/db/hotel_price_lists.md`](../../../analisi-pre/db/hotel_price_lists.md).

---

## Descrizione

Prezzo base / mezza giornata / stagionale di un listino hotel per **settore**. Una riga per (listino ×
settore). Nel legacy: array `mainPrices` con `name` testuale del settore e valori stringa (es.
`{"name":"Fila 1","value":"7","halfday":"7","seasonal":"9"}`).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_agreement_price_list_sector_prices` | `id` | PK | — |
| `uq_..._pricelist_sector` | `(price_list_id, sector_id) WHERE deleted_at IS NULL` | UNIQUE parziale | un prezzo per (listino, settore) |
| `ix_..._sector` | `(sector_id) WHERE deleted_at IS NULL` | parziale | listini disponibili nel settore |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### price_list_id
- **Legacy**: `hotel_price_lists.id`
- **Descrizione**: listino/voucher hotel.
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_agreement_price_lists.id`](catalog_agreement_price_lists.md)
  `ON DELETE CASCADE`.

### sector_id
- **Legacy**: `hotel_price_lists.mainPrices[].name` (nome testuale del settore)
- **Descrizione**: settore a cui si applica il prezzo.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_map_sectors.id`](catalog_map_sectors.md) `ON DELETE RESTRICT` (i
  settori sono quelli della `property` del partner).
- **Note**: in migrazione `name` → `sector_id` per la property del partner; nomi non risolvibili → log.

### base_price
- **Legacy**: `mainPrices[].value` (stringa)
- **Descrizione**: prezzo giornaliero base nel settore.
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### half_day_price
- **Legacy**: `mainPrices[].halfday` (stringa)
- **Descrizione**: prezzo mezza giornata nel settore.
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

### seasonal_price
- **Legacy**: `mainPrices[].seasonal` (stringa)
- **Descrizione**: prezzo stagionale nel settore.
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

Nessun JSON — `mainPrices[]` → righe; stringhe → `numeric`.

## Suggerimenti per le prestazioni

- Poche righe per listino. Vedi [`catalog_pricing_price_list_sector_prices`](catalog_pricing_price_list_sector_prices.md).

## Implementazione .NET

Entità figlia di [`AgreementPriceList`](catalog_agreement_price_lists.md) (prezzo per settore). Modulo
`Module.Catalog`, schema `catalog`. Nessun enum proprio.

```csharp
// Module.Catalog.Domain.Entities/AgreementPriceListSectorPrice.cs
namespace Module.Catalog.Domain.Entities;

public sealed class AgreementPriceListSectorPrice : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public AgreementPriceList PriceList { get; private set; } = null!;
	public long SectorId { get; private set; }
	public MapSector Sector { get; private set; } = null!;
	public decimal? BasePrice { get; private set; }
	public decimal? HalfDayPrice { get; private set; }
	public decimal? SeasonalPrice { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public void Delete(DateTime utcNow) => DeletedAt = utcNow;
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog.Infrastructure.Configurations/AgreementPriceListSectorPriceConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class AgreementPriceListSectorPriceConfiguration
	: IEntityTypeConfiguration<AgreementPriceListSectorPrice>
{
	public void Configure(EntityTypeBuilder<AgreementPriceListSectorPrice> builder)
	{
		builder.ToTable("catalog_agreement_price_list_sector_prices", "catalog", t => t.HasComment(
			"Prezzo base / mezza giornata / stagionale di un listino/voucher hotel per settore."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_list_sector_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.HasColumnType("integer")
			.IsRequired()
			.HasComment("Listino/voucher hotel a cui appartiene il prezzo di settore.");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Settore (della property del partner) a cui si applica il prezzo.");

		builder.Property(x => x.BasePrice).HasColumnName("base_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo giornaliero base nel settore.");
		builder.Property(x => x.HalfDayPrice).HasColumnName("half_day_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo mezza giornata nel settore.");
		builder.Property(x => x.SeasonalPrice).HasColumnName("seasonal_price")
			.HasColumnType("numeric(12,2)").HasComment("Prezzo stagionale nel settore.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete.");

		builder.HasOne(x => x.PriceList)
			.WithMany(pl => pl.SectorPrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_agreement_price_list_sector_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Sector)
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_agreement_price_list_sector_prices_sector")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(x => new { x.PriceListId, x.SectorId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_agreement_price_list_sector_prices_pricelist_sector");

		builder.HasIndex(x => x.SectorId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_sector_prices_sector");
	}
}
```

**Note di migrazione**: ogni elemento di `mainPrices[]` diventa una riga; `name` (testuale) risolto contro
`catalog_map_sectors` della property del partner → `sector_id`; `value`/`halfday`/`seasonal` (stringhe) →
`base_price`/`half_day_price`/`seasonal_price` (`numeric`); nomi non risolvibili → riga scartata con log.
