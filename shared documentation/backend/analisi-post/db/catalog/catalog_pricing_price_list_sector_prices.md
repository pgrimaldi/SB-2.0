# Tabella `catalog_pricing_price_list_sector_prices`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_price_list_sector_prices` · **forma canonica**: `catalog.pricing_price_list_sector_prices`.
>
> Rimpiazza `price_lists.mainPrices` (`[{name, value}]`, `name` = nome settore) e i riferimenti
> `details.sectorRowsPkg` / `details.selectedRowsData` (settori a cui il pacchetto è applicabile). Fonti:
> [`analisi-pre/db/price_lists.md` (campi `mainPrices` / `details`)](../../../analisi-pre/db/price_lists.md),
> [`analisi-pre/db/sectors_rows.md`](../../../analisi-pre/db/sectors_rows.md).

---

## Descrizione

Il **prezzo base per settore** di un pacchetto, e — implicitamente — a **quali settori** il pacchetto è
applicabile (l'esistenza di una riga per un settore = pacchetto disponibile in quel settore). Una riga
per (pacchetto × settore). Nel legacy: array `mainPrices` con `name` testuale del settore e `value`
stringa; l'elenco settori applicabili era un secondo array (`sectorRowsPkg`/`selectedRowsData`) — anch'esso
per nome. Qui: FK verso [`catalog_map_sectors`](catalog_map_sectors.md).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_price_list_sector_prices` | `id` | PK | — |
| `uq_..._package_sector` | `(price_list_id, sector_id) WHERE deleted_at IS NULL` | UNIQUE parziale | un prezzo base per (pacchetto, settore) |
| `ix_..._sector` | `(sector_id) WHERE deleted_at IS NULL` | parziale | "pacchetti disponibili nel settore X" |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### price_list_id
- **Legacy**: `price_lists.id`
- **Descrizione**: pacchetto a cui si riferisce il prezzo.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_price_lists.id`](catalog_pricing_price_lists.md) `ON DELETE CASCADE`.

### sector_id
- **Legacy**: `price_lists.mainPrices[].name` / `details.sectorRowsPkg[]` / `details.selectedRowsData[]` (nome testuale)
- **Descrizione**: settore a cui si applica il prezzo/la disponibilità.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_map_sectors.id`](catalog_map_sectors.md) `ON DELETE RESTRICT`.
- **Note**: in migrazione `name` → `sector_id` per property; nomi non risolvibili → riga scartata con log.

### base_price
- **Legacy**: `price_lists.mainPrices[].value` (stringa numerica, es. `"120"`)
- **Descrizione**: prezzo stagionale forfettario del pacchetto in quel settore; usato come
  fallback/riferimento rispetto ai prezzi per periodo di
  [`catalog_pricing_price_list_period_prices`](catalog_pricing_price_list_period_prices.md).
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì (una riga può esistere solo per marcare l'applicabilità del pacchetto al settore,
  senza un prezzo base).
- **Relazioni**: —.

### half_day_price
- **Legacy**: `hotel_price_lists.mainPrices[].halfday` (presente sui listini hotel; su `price_lists` non sempre) — dove disponibile
- **Descrizione**: prezzo forfettario mezza giornata in quel settore.
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: campo presente per allineamento con
  [`catalog_agreement_price_list_sector_prices`](catalog_agreement_price_list_sector_prices.md); su
  `price_lists` puri di solito `NULL`.

### seasonal_price
- **Legacy**: `hotel_price_lists.mainPrices[].seasonal` (dove disponibile)
- **Descrizione**: prezzo per l'intera stagione (pass stagionale) in quel settore.
- **Tipo**: `numeric(12,2)`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

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

Nessun JSON. `mainPrices[]` + `sectorRowsPkg`/`selectedRowsData` → una tabella; `value` stringa →
`numeric`.

## Suggerimenti per le prestazioni

- Poche righe per pacchetto (numero settori). Il motore prezzo risolve il settore della postazione
  richiesta e legge la riga per `(price_list_id, sector_id)`.
- Se un pacchetto ha lo stesso prezzo in tutti i settori, resta comunque una riga per settore (esplicito
  > implicito): il volume è trascurabile.

## Implementazione .NET

**Entità figlia** dell'aggregate `PriceList` (modulo `Module.Catalog`, schema `catalog`), esposta come
collezione `PriceList.SectorPrices`. Nessun enum proprio.

### `PriceListSectorPriceConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class PriceListSectorPriceConfiguration : IEntityTypeConfiguration<PriceListSectorPrice>
{
	public void Configure(EntityTypeBuilder<PriceListSectorPrice> builder)
	{
		builder.ToTable("pricing_price_list_sector_prices", "catalog", t => t.HasComment(
			"Prezzo base per settore di un pacchetto; l'esistenza della riga marca anche l'applicabilità al settore."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_sector_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Pacchetto a cui si riferisce il prezzo.");
		builder.HasOne<PriceList>()
			.WithMany(p => p.SectorPrices)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_sector_prices_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Settore a cui si applica il prezzo/la disponibilità.");
		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_pricing_price_list_sector_prices_sector")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.BasePrice)
			.HasColumnName("base_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo stagionale forfettario del pacchetto nel settore (fallback rispetto ai prezzi per periodo).");

		builder.Property(x => x.HalfDayPrice)
			.HasColumnName("half_day_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo forfettario mezza giornata nel settore (allineamento con i listini hotel; spesso NULL sui price_lists puri).");

		builder.Property(x => x.SeasonalPrice)
			.HasColumnName("seasonal_price")
			.HasColumnType("numeric(12,2)")
			.HasComment("Prezzo per l'intera stagione (pass stagionale) nel settore.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della riga.");

		builder.HasIndex(x => new { x.PriceListId, x.SectorId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_sector_prices_package_sector");

		builder.HasIndex(x => x.SectorId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_sector_prices_sector");
	}
}
```

`PriceListSectorPrice` implementa `ISoftDeletable` + `IAuditable`.

### Relazioni

- `price_list_id` → `PriceList` (stesso schema): FK reale, `WithMany(p => p.SectorPrices)`, cascade.
- `sector_id` → `catalog_map_sectors` (stesso schema, altra area del modulo `Catalog`): FK reale,
  `ON DELETE RESTRICT` (non si cancella un settore con prezzi ancora agganciati).

### Note di migrazione

- `mainPrices[]` (`{name, value}`) + `sectorRowsPkg`/`selectedRowsData` (elenco settori applicabili, per
  nome) → una riga per settore; `name` → `sector_id` risolto per property; nomi non risolvibili → riga
  scartata con log.
- `value` stringa → `numeric(12,2)`.
- Un pacchetto disponibile ma senza prezzo esplicito in un settore mantiene comunque la riga (con
  `base_price = NULL`) per marcare l'applicabilità.
