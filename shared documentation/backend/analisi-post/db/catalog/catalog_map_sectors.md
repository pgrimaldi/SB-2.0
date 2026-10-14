# Tabella `catalog_map_sectors`

> **Dominio**: C2 — `catalog` · **sottoarea**: `map` · **schema**: `catalog` · **nome flat**:
> `catalog_map_sectors` · **forma canonica**: `catalog.map_sectors`.
>
> Rimpiazza la legacy [`sectors_rows`](../../../analisi-pre/db/sectors_rows.md) (**175 righe** = 1 riga JSON
> per stabilimento; i settori erano gli elementi dell'array `sectors`). Fonti:
> [`analisi-pre/db/sectors_rows.md`](../../../analisi-pre/db/sectors_rows.md),
> [`analisi-pre/business-logic/stabilimento/11-configurazione.md`](../../../analisi-pre/business-logic/stabilimento/11-configurazione.md).

---

## Descrizione

I **settori/file** in cui è divisa la spiaggia di uno stabilimento (es. "Fila 1", "Fila 6-6"). Nel legacy
erano l'array JSON `sectors_rows.sectors` (`[{name, currentId}]`, con `currentId` = numero casuale
client-side, **non** un id di business) in **una** riga per property. Qui: **una riga per (property ×
settore)**, con un id reale.

Il settore è oggi referenziato **per nome** da più tabelle (`bookingLimit[].sectorName`,
`price_lists.sectorRowsPkg`/`selectedRowsData`, `discounts.items[].selectedRow`, righe della mappa) — nel
target quei riferimenti diventano FK verso `catalog_map_sectors.id`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_map_sectors` | `id` | PK | — |
| `uq_..._public_id` | `(public_id)` | UNIQUE | referenziato cross-modulo (C1: `booking_lines.sector_id`, snapshot settore) |
| `uq_..._property_name` | `(property_id, lower(name)) WHERE deleted_at IS NULL` | UNIQUE parziale | nome settore univoco per stabilimento (era la chiave logica di fatto) |
| `ix_..._property` | `(property_id, sort_order) WHERE deleted_at IS NULL` | parziale | elenco ordinato |

## Campi

### id
- **Legacy**: `sectors_rows.sectors[].currentId` (numero casuale client-side — **non** portato: non è un id stabile)
- **Descrizione**: PK tecnica, id **stabile** del settore.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da [`catalog_map_spots.sector_id`](catalog_map_spots.md),
  [`catalog_property_online_stay_limits.sector_id`](catalog_property_online_stay_limits.md),
  [`catalog_pricing_price_list_sector_prices.sector_id`](catalog_pricing_price_list_sector_prices.md),
  [`catalog_pricing_discount_sectors.sector_id`](catalog_pricing_discount_sectors.md).

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico stabile, **copiato cross-modulo**. C1 lo usa come `sector_id` sulle righe
  prenotazione (`booking_lines.sector_id`) accanto allo snapshot testuale `sector_label`, per legare la
  riga al settore contro cui è stata calcolata la tariffa.
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C1 (`booking_lines.sector_id`, `booking_resource_availability_daily.sector_id`).

### property_id
- **Legacy**: `sectors_rows.propertyId`
- **Descrizione**: stabilimento proprietario dei settori.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No (il legacy aveva 1 riga con `NULL`).
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`.

### name
- **Legacy**: `sectors_rows.sectors[].name`
- **Descrizione**: nome del settore, mostrato in tutta l'app.
- **Tipo**: `varchar(100)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: era usato come chiave di correlazione testuale ovunque nel dominio; qui resta il nome
  visualizzato ma le correlazioni passano per `id`. `UNIQUE` per property (case-insensitive).

### sort_order
- **Legacy**: posizione nell'array `sectors` (l'ordine di inserimento nella lista UI)
- **Descrizione**: ordine di visualizzazione dei settori.
- **Tipo**: `smallint`.
- **Default**: nessuno (assegnato in coda).
- **Nullabile**: No.
- **Relazioni**: —.

### created_by_user_id
- **Legacy**: `sectors_rows.createdBy` (dichiarato dal client, non vincolato)
- **Descrizione**: operatore che ha creato/modificato la configurazione settori.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 `identity_access_users` — nessuna FK cross-schema.
- **Note**: nel legacy era un intero non verificato; nel target = utente autenticato.

### created_at / updated_at
- **Legacy**: `sectors_rows.createdAt` / `sectors_rows.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nel legacy si riscriveva l'array)
- **Descrizione**: soft-delete del settore.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: cancellare un settore ancora referenziato da spot/prezzi/sconti va bloccato o gestito
  esplicitamente (nel legacy la rottura era silenziosa perché il legame era per stringa).

## Valutazione JSON

Nessun JSON — elimina l'array `sectors` e il pseudo-id casuale `currentId`.

## Suggerimenti per le prestazioni

- Poche righe per property (unità/decine). L'elenco settori si carica con una query per `property_id`.
- L'`UNIQUE (property_id, lower(name))` garantisce che i riferimenti per nome delle vecchie tabelle si
  risolvano in modo deterministico in migrazione.

## Implementazione .NET

`MapSector` è un'**entità figlia** di `Property` (non aggregate root proprio) ma con `public_id` esposto
perché **referenziato cross-modulo** da C1 (`booking_lines.sector_id`, `booking_resource_availability_daily.sector_id`)
— unico caso in questo batch di entità figlia con `public_id` per un riferimento in uscita dal modulo, non
per esposizione API. Modulo `Module.Catalog`, area `Map`, schema `catalog`, tabella `map_sectors`. Nessun
enum proprio.

### `MapSectorConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class MapSectorConfiguration : IEntityTypeConfiguration<MapSector>
{
	public void Configure(EntityTypeBuilder<MapSector> builder)
	{
		builder.ToTable("map_sectors", "catalog", t => t.HasComment(
			"Settori/file in cui è divisa la spiaggia di uno stabilimento. Sostituisce l'array JSON sectors_rows.sectors."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_sectors");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico stabile, copiato cross-modulo da C1 (booking_lines.sector_id, resource_availability_daily.sector_id).");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario dei settori.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(100)
			.IsRequired()
			.HasComment("Nome del settore, mostrato in tutta l'app; univoco per stabilimento.");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasComment("Ordine di visualizzazione dei settori.");

		builder.Property(x => x.CreatedByUserPublicId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha creato/modificato i settori.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete del settore (bloccato/gestito esplicitamente se ancora referenziato da spot/prezzi/sconti).");

		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_map_sectors_public_id");

		builder.HasIndex(x => new { x.PropertyId, x.Name })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_map_sectors_property_name");

		builder.HasIndex(x => new { x.PropertyId, x.SortOrder })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_map_sectors_property");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_map_sectors_property")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`MapSector` implementa `IAuditable`/`ISoftDeletable`.

### Relazioni

- `PropertyId` → `Property` (stesso schema): FK reale, `ON DELETE CASCADE`.
- `CreatedByUserPublicId`: id copiato da C5 — solo `.HasComment`, nessuna FK.
- `PublicId` copiato in uscita da C1 (nessuna FK ovviamente, contratto cross-modulo).

### Note di migrazione

- L'`UNIQUE (property_id, lower(name))` (`.HasFilter` sopra usa il confronto case-insensitive risolto a
  livello di `Name` normalizzato in scrittura, oppure espressione `lower(name)` da correggere a mano nella
  migration generata, come per `catalog_properties.ix_..._name_trgm`) garantisce la risoluzione
  deterministica dei vecchi riferimenti per nome (`bookingLimit[].sectorName`,
  `price_lists.sectorRowsPkg`/`selectedRowsData`, `discounts.items[].selectedRow`).
- Esplosione dell'array `sectors_rows.sectors` in N righe; `currentId` (pseudo-id casuale client-side)
  scartato — l'`id` reale è generato dal DB.
- Tutti i riferimenti legacy per nome vanno risolti a valle in `sector_id`/`public_id` durante la
  migrazione delle tabelle che li usano (prezzi, sconti, limiti, mappa).
