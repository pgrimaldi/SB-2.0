# Tabella `catalog_pricing_price_list_reduced_windows`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_price_list_reduced_windows` · **forma canonica**:
> `catalog.pricing_price_list_reduced_windows`.
>
> Rimpiazza `price_lists.details.setReducedPrice`
> (`[{startDate, endDate, additionalData:[{name, value}]}]`). Fonti:
> [`analisi-pre/db/price_lists.md` (campo `details` → `setReducedPrice`)](../../../analisi-pre/db/price_lists.md),
> `front-customer-pre/src/services/BookingCalcService.js` (`getReducedPrices`, righe 11-32),
> `front-pre/src/components/Price/AddPackage.js` (editor). Semantica **verificata nel codice** —
> [D-15](../_domande-committente.md).

---

## Descrizione

Un pacchetto può avere **prezzi ridotti** per un **intervallo di date preciso**: quando le date della
prenotazione **coincidono esattamente** con `(starts_on, ends_on)`, il prezzo del pacchetto per un dato
**settore** è sostituito dal prezzo ridotto indicato (valore **assoluto**, non percentuale né delta).

Semantica verificata in `BookingCalcService.getReducedPrices(currentRow, getObj, selectedDates)`:

- itera `details.setReducedPrice[]`;
- seleziona l'elemento in cui `startDate == selectedDates.StartDate` **e** `endDate == selectedDates.EndDate`
  (**uguaglianza esatta** dell'intero intervallo prenotato — *non* "la data cade dentro la finestra");
- dentro l'elemento, cerca `additionalData.find(d => d.name === currentRow)`, dove `currentRow` è il
  **nome del settore** (stesso uso di `mainPrices[].name`, vedi `getSeasonalDatesPrices` nello stesso
  file);
- restituisce `matchingData.value` come **prezzo assoluto** per quel settore.

Una riga per (pacchetto × intervallo di date); i prezzi per settore stanno nella figlia
[`catalog_pricing_price_list_reduced_prices`](#tabella-figlia-catalog_pricing_price_list_reduced_prices).

**Nota (regola generale: resta il legacy).** Il match **esatto** dell'intervallo è un vincolo forte e
poco flessibile del legacy (una prenotazione di durata anche solo di un giorno diversa non trova il
prezzo ridotto). Si riproduce così com'è; un'eventuale evoluzione a "contenimento" (`? BETWEEN starts_on
AND ends_on`) è una decisione di prodotto futura, non fatta ora.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_price_list_reduced_windows` | `id` | PK | — |
| `uq_..._package_range` | `(price_list_id, starts_on, ends_on) WHERE deleted_at IS NULL` | UNIQUE parziale | il legacy fa match esatto su (package, start, end) → una sola riga per combinazione |

## Campi

### id
- **Legacy**: — (nuovo; il legacy identificava per posizione nell'array)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da `catalog_pricing_price_list_reduced_prices.window_id`.

### price_list_id
- **Legacy**: `price_lists.id` (contenitore di `details.setReducedPrice`)
- **Descrizione**: pacchetto a cui si applica.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_price_lists.id`](catalog_pricing_price_lists.md) `ON DELETE CASCADE`.

### starts_on / ends_on
- **Legacy**: `setReducedPrice[].startDate` / `.endDate` (datetime ISO)
- **Descrizione**: l'intervallo di date che deve **coincidere esattamente** con quello della prenotazione
  perché i prezzi ridotti si applichino.
- **Tipo**: `date` ciascuno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `CHECK (ends_on >= starts_on)`. In migrazione datetime → `date`.

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

## Tabella figlia `catalog_pricing_price_list_reduced_prices`

Sostituisce `setReducedPrice[].additionalData` (`[{name, value}]`). Una riga per (intervallo × settore).

| Colonna | Legacy | Tipo | Note |
|---|---|---|---|
| `id` | — | `bigint` IDENTITY | PK |
| `window_id` | — | `bigint` | **FK** → `catalog_pricing_price_list_reduced_windows.id` `ON DELETE CASCADE` |
| `sector_id` | `additionalData[].name` (nome testuale del settore) | `bigint` | **FK** → [`catalog_map_sectors.id`](catalog_map_sectors.md) `ON DELETE CASCADE`; in migrazione `name` → `sector_id` per property, nomi non risolvibili → riga scartata con log |
| `price` | `additionalData[].value` | `numeric(12,2)` | **prezzo assoluto** del pacchetto in quel settore per l'intervallo indicato (sostituisce il prezzo standard) |
| `created_at`/`updated_at`/`deleted_at` | — | `timestamptz` | standard |

`UNIQUE (window_id, sector_id) WHERE deleted_at IS NULL`.

## Valutazione JSON

Nessun JSON. `setReducedPrice[]` → tabella intervalli + `additionalData[]` → tabella prezzi per settore
(`name` era il nome del settore, non una voce generica → FK reale; `value` è un prezzo → `numeric`).

## Suggerimenti per le prestazioni

- Poche righe per pacchetto (feature di editing poco usata). Il motore prezzo, avendo l'intervallo
  esatto della prenotazione, fa `WHERE price_list_id = ? AND starts_on = ? AND ends_on = ?` + join alla
  figlia per `sector_id`.
- Se in futuro si passa al contenimento, l'indice `(price_list_id, starts_on, ends_on)` copre comunque
  `? BETWEEN starts_on AND ends_on`.

## Implementazione .NET

**Entità figlia** dell'aggregate `PriceList` (modulo `Module.Catalog`, schema `catalog`), esposta come
collezione `PriceList.ReducedWindows`; ha a sua volta una **figlia di secondo livello**
`PriceListReducedPrice` (tabella `catalog_pricing_price_list_reduced_prices`, non ha una scheda propria —
documentata in questo stesso file) esposta come `PriceListReducedWindow.SectorPrices`. Nessun enum
proprio.

### `PriceListReducedWindowConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class PriceListReducedWindowConfiguration : IEntityTypeConfiguration<PriceListReducedWindow>
{
	public void Configure(EntityTypeBuilder<PriceListReducedWindow> builder)
	{
		builder.ToTable("pricing_price_list_reduced_windows", "catalog", t =>
		{
			t.HasComment("Finestra di date che, se coincide esattamente con quella prenotata, attiva i prezzi ridotti per settore.");
			t.HasCheckConstraint("ck_catalog_pricing_price_list_reduced_windows_range", "ends_on >= starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_reduced_windows");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Pacchetto a cui si applica la finestra di prezzo ridotto.");
		builder.HasOne<PriceList>()
			.WithMany(p => p.ReducedWindows)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_reduced_windows_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.IsRequired()
			.HasComment("Inizio dell'intervallo che deve coincidere esattamente con quello della prenotazione.");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.IsRequired()
			.HasComment("Fine dell'intervallo che deve coincidere esattamente con quello della prenotazione.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della finestra.");

		builder.HasIndex(x => new { x.PriceListId, x.StartsOn, x.EndsOn })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_reduced_windows_package_range");

		builder.HasMany(x => x.SectorPrices).WithOne().HasForeignKey(p => p.WindowId);
	}
}
```

### `PriceListReducedPriceConfiguration` (tabella figlia `catalog_pricing_price_list_reduced_prices`)

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class PriceListReducedPriceConfiguration : IEntityTypeConfiguration<PriceListReducedPrice>
{
	public void Configure(EntityTypeBuilder<PriceListReducedPrice> builder)
	{
		builder.ToTable("pricing_price_list_reduced_prices", "catalog", t => t.HasComment(
			"Prezzo ridotto assoluto per settore, valido per una finestra di date con match esatto."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_reduced_prices");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.WindowId)
			.HasColumnName("window_id")
			.IsRequired()
			.HasComment("Finestra di date a cui appartiene il prezzo ridotto.");
		builder.HasOne<PriceListReducedWindow>()
			.WithMany(w => w.SectorPrices)
			.HasForeignKey(x => x.WindowId)
			.HasConstraintName("fk_catalog_pricing_price_list_reduced_prices_window")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Settore per cui vale il prezzo ridotto.");
		builder.HasOne<MapSector>()
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_pricing_price_list_reduced_prices_sector")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.Price)
			.HasColumnName("price")
			.HasColumnType("numeric(12,2)")
			.IsRequired()
			.HasComment("Prezzo assoluto del pacchetto in quel settore per l'intervallo, sostituisce il prezzo standard.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della riga.");

		builder.HasIndex(x => new { x.WindowId, x.SectorId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_price_list_reduced_prices_window_sector");
	}
}
```

Entrambe le entità implementano `ISoftDeletable` + `IAuditable`.

### Relazioni

- `price_list_id` → `PriceList` (stesso schema): FK reale, `WithMany(p => p.ReducedWindows)`, cascade.
- `window_id` → `PriceListReducedWindow` (stesso schema, figlia della stessa radice): FK reale,
  `WithMany(w => w.SectorPrices)`, cascade.
- `sector_id` → `catalog_map_sectors` (stesso schema): FK reale, `ON DELETE CASCADE`.

### Note di migrazione

- Motore prezzo: livello "reduced" applicato quando l'intervallo prenotato coincide **esattamente** con
  una finestra (match legacy `startDate == selectedDates.StartDate && endDate == selectedDates.EndDate`,
  non un `BETWEEN`); precedenza rispetto a `date override`/`holiday`/`period price`/`sector base price` da
  confermare col committente (indicativamente `reduced > date override > holiday > period > sector base`).
- `setReducedPrice[]` → righe `catalog_pricing_price_list_reduced_windows`; `additionalData[]` (`{name,
  value}`, `name` = nome settore) → righe `catalog_pricing_price_list_reduced_prices` (`name` →
  `sector_id` risolto per property, non risolvibili → riga scartata con log; `value` → `price`).
- `startDate`/`endDate` datetime ISO → `DateOnly`/`date`.
- Regola generale del progetto: il match esatto (non contenimento) **resta com'è nel legacy** — un
  'affinamento a "contenimento" è una decisione di prodotto futura, non fatta in questa migrazione.
