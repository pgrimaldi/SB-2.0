# Tabella `catalog_inventory_items`

> **Dominio**: C2 — `catalog` · **sottoarea**: `inventory` · **schema**: `catalog` · **nome flat**:
> `catalog_inventory_items` · **forma canonica**: `catalog.inventory_items`.
>
> Rimpiazza la legacy [`warehouses`](../../../analisi-pre/db/warehouses.md) (**172 righe** = 1 riga JSON per
> stabilimento; gli articoli erano gli elementi dell'array `items`). Fonti:
> [`analisi-pre/db/warehouses.md`](../../../analisi-pre/db/warehouses.md),
> [`analisi-pre/business-logic/stabilimento/08-magazzino.md`](../../../analisi-pre/business-logic/stabilimento/08-magazzino.md),
> [`docs/03-gestione-dati.md` §3.8 (consumi di magazzino)](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

Gli **articoli fisici a corredo** (ombrelloni, lettini, tende, sdraio, …) di cui uno stabilimento tiene
la quantità disponibile. Una riga per (property × articolo). Nel legacy erano gli elementi dell'array
JSON `warehouses.items` (`{name, it, quantity, threshold, isEnable, currentId}`, con `quantity`/
`threshold` salvati come **stringhe**).

Il **consumo** (quante unità occupate in una data) **non** vive qui: è C1
(`resource_consumptions` — [`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md)). Questa
tabella è l'anagrafica + la capienza totale.

Correzioni rispetto al legacy:

- **1 riga per articolo**; `quantity`/`threshold` → interi.
- **`isEnable`** (int 0/1) → `is_active boolean`.
- **`currentId`** (numero casuale client-side) → scartato.
- il mascheramento delle quantità per richieste non autenticate (`maskDecimal`) resta una regola
  applicativa, non uno schema.
- l'endpoint `bookingdetails` che non rispondeva mai / `addwarehouse` con `isSuperAdmin` incoerente →
  non riprodotti.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_inventory_items` | `id` | PK | — |
| `uq_catalog_inventory_items_public_id` | `(public_id)` | UNIQUE | referenziato cross-modulo (C1 consumi) |
| `uq_..._property_name` | `(property_id, lower(name)) WHERE deleted_at IS NULL` | UNIQUE parziale | articolo univoco per stabilimento |
| `ix_..._property_active` | `(property_id) WHERE is_active AND deleted_at IS NULL` | parziale | magazzino attivo dello stabilimento |

## Campi

### id
- **Legacy**: `warehouses.items[].currentId` (numero casuale — **non** portato)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### public_id
- **Legacy**: — (nuovo)
- **Descrizione**: id pubblico, **copiato** da C1 come `resource_id` nei consumi inventario
  (`booking_items_counts_details` / `additional_items_counts_details` → `resource_consumptions`).
- **Tipo**: `uuid` (UUIDv7).
- **Default**: UUIDv7.
- **Nullabile**: No.
- **Relazioni**: copiato da C1.

### property_id
- **Legacy**: `warehouses.propertyId`
- **Descrizione**: stabilimento proprietario dell'articolo.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`.

### name
- **Legacy**: `warehouses.items[].name` (e `.it`)
- **Descrizione**: nome dell'articolo.
- **Tipo**: `varchar(120)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **colonna singola**. Verificato in `front-pre/src/components/warehouse/AddWarehouse.js:52`
  (`setState({ name: value, it: value })`): per gli articoli custom il legacy salva `it = name`; il campo
  `it` porta una traduzione solo per i ~10 articoli standard scelti da una tendina predefinita. → una
  colonna `name` (lingua del manager); il set di articoli standard può essere un seed di piattaforma
  con traduzioni se in futuro serve mostrarli localizzati. `name` era la chiave di correlazione con
  `PackageItemSelected[].name` di `price_lists` → ora la correlazione passa per FK
  (`catalog_pricing_price_list_items.inventory_item_id`).

### total_quantity
- **Legacy**: `warehouses.items[].quantity` (salvata come **stringa**, es. `"62"`)
- **Descrizione**: quantità totale disponibile dell'articolo.
- **Tipo**: `integer`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: la disponibilità per data = `total_quantity − Σ consumi(data)` (C1).
- **Note**: `CHECK (total_quantity >= 0)`.

### low_stock_threshold
- **Legacy**: `warehouses.items[].threshold` (stringa, es. `"3"`)
- **Descrizione**: soglia minima sotto la quale segnalare scorta bassa (nel legacy salvata ma **mai
  confrontata** da alcun controller).
- **Tipo**: `integer`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: nel target un servizio/notifica può usarla realmente (C6); qui resta il dato di
  configurazione.

### is_active
- **Legacy**: `warehouses.items[].isEnable` (int 0/1)
- **Descrizione**: articolo attivo/visibile.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_by_user_id
- **Legacy**: `warehouses.createdBy`
- **Descrizione**: operatore che ha configurato il magazzino.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 `identity_access_users`.

### created_at / updated_at
- **Legacy**: `warehouses.createdAt` / `warehouses.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nel legacy si rimuoveva l'elemento dall'array)
- **Descrizione**: soft-delete dell'articolo.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: un articolo con consumi storici (C1) non si cancella fisicamente.

## Campi legacy **non** migrati

| Campo legacy | Motivo |
|---|---|
| `warehouses.items[].currentId` | pseudo-id casuale client-side |

## Valutazione JSON

Nessun JSON. `warehouses.items` (array) → una riga per articolo; `quantity`/`threshold` stringa → `integer`.

## Suggerimenti per le prestazioni

- Poche righe per property. La disponibilità per data si calcola contro `resource_consumptions` (C1) —
  la scelta target di **una sola** tabella di consumo ([§3.8](../../../progettazione/03-gestione-dati.md)) rende
  questo calcolo una singola aggregazione, non 6 tabelle sovrapposte come nel legacy.
- Il mascheramento delle quantità per utenti anonimi è una regola di presentazione (non a schema).

## Implementazione .NET

Aggregate root dell'area `Inventory`, modulo `Module.Catalog`, schema `catalog`. `PublicId` è esposto
perché referenziato cross-modulo da C1 come `resource_id` nei consumi
(`resource_consumptions`/`booking_items_counts_details`/`additional_items_counts_details`). Nessun enum
proprio. Il consumo per data **non** vive su questa entità (calcolato contro C1).

```csharp
// Module.Catalog.Domain.Entities/InventoryItem.cs
namespace Module.Catalog.Domain.Entities;

public sealed class InventoryItem : AggregateRoot, IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public long PropertyId { get; private set; }
	public Property Property { get; private set; } = null!;
	public string Name { get; private set; } = null!;
	public int TotalQuantity { get; private set; }
	public int? LowStockThreshold { get; private set; }
	public bool IsActive { get; private set; }
	public Guid? CreatedByUserId { get; private set; } // public_id di identity_access_users (C5), copiato

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public void Delete(DateTime utcNow) => DeletedAt = utcNow;
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog.Infrastructure.Configurations/InventoryItemConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
	public void Configure(EntityTypeBuilder<InventoryItem> builder)
	{
		builder.ToTable("catalog_inventory_items", "catalog", t =>
		{
			t.HasComment("Articolo fisico a corredo (ombrellone, lettino, tenda, sdraio, …) di cui uno stabilimento tiene la quantità disponibile.");
			t.HasCheckConstraint("ck_catalog_inventory_items_total_quantity", "total_quantity >= 0");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_inventory_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId)
			.HasColumnName("public_id")
			.ValueGeneratedNever()
			.IsRequired()
			.HasComment("Id pubblico (UUIDv7), copiato — C1 lo referenzia come resource_id nei consumi di magazzino.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario dell'articolo.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Nome dell'articolo, nella lingua del manager.");

		builder.Property(x => x.TotalQuantity)
			.HasColumnName("total_quantity")
			.IsRequired()
			.HasDefaultValue(0)
			.HasComment("Quantità totale disponibile dell'articolo; la disponibilità per data è total_quantity meno i consumi (C1).");

		builder.Property(x => x.LowStockThreshold)
			.HasColumnName("low_stock_threshold")
			.HasComment("Soglia minima sotto la quale segnalare scorta bassa.");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Articolo attivo/visibile.");

		builder.Property(x => x.CreatedByUserId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity_access_users (C5), copiato — operatore che ha configurato l'articolo, nessuna FK cross-schema.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete; un articolo con consumi storici (C1) non si cancella fisicamente.");

		builder.HasOne(x => x.Property)
			.WithMany()
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_inventory_items_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.PublicId)
			.IsUnique()
			.HasDatabaseName("uq_catalog_inventory_items_public_id");

		// Univocità su lower(name): espressione funzionale — l'IndexBuilder fluente non genera
		// l'espressione, va rifinita a mano nella migration generata (colonna Name come base).
		builder.HasIndex(x => new { x.PropertyId, x.Name })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_inventory_items_property_name");

		builder.HasIndex(x => x.PropertyId)
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_inventory_items_property_active");
	}
}
```

**Note di migrazione**: esplosione di `warehouses.items[]` in una riga per articolo; `quantity`/
`threshold` (stringhe) → `TotalQuantity`/`LowStockThreshold` (interi); `isEnable` (int) → `IsActive`
(bool); `currentId` (pseudo-id casuale client-side) **scartato**, nuova identità `bigint` assegnata
dall'`IDENTITY`; `public_id` generato ex-novo (UUIDv7). I riferimenti storici
`PackageItemSelected[].name` (in `catalog_pricing_price_list_items` / `catalog_agreement_price_list_items`)
vanno risolti per nome contro questa tabella (stessa `property_id`) e sostituiti con `inventory_item_id`.
Un servizio applicativo `GetAvailability(itemId, date)` calcola `TotalQuantity − consumi(date)` interrogando
C1 (`resource_consumptions`), non uno stato mantenuto qui.
