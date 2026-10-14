# Tabella `catalog_pricing_price_list_items`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_price_list_items` · **forma canonica**: `catalog.pricing_price_list_items`.
>
> Rimpiazza `price_lists.details.PackageItemSelected` (articoli inclusi nel pacchetto),
> `details.selectedAreaItems` (articoli d'area) e `details.selectedPackages` (pacchetti a cui è vincolato
> un `additional`). Fonti:
> [`analisi-pre/db/price_lists.md` (campo `details`)](../../../analisi-pre/db/price_lists.md),
> [`analisi-pre/db/warehouses.md`](../../../analisi-pre/db/warehouses.md),
> [`analisi-pre/db/services_types.md`](../../../analisi-pre/db/services_types.md).

---

## Descrizione

Le **componenti** di una voce di listino:

- per un `package`: gli articoli di magazzino inclusi con relativa quantità (`item_kind =
  inventory_item`), i servizi inclusi (`item_kind = included_service`), gli articoli d'area (`item_kind =
  area_item`);
- per un `additional`: i pacchetti a cui l'articolo è vincolato — venduto solo se il cliente ha già scelto
  uno di quei pacchetti (`item_kind = linked_package`).

Nel legacy erano tre array JSON dentro `details` con riferimenti **per nome/stringa**
(`PackageItemSelected:[{it, name, quantity}]`, dove `name` = `"Umbrella"`/`"Sunbed"`… e `it` un
codice/numero incoerente). Qui: una riga per componente, riferimento **per FK**.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_price_list_items` | `id` | PK | — |
| `ix_..._package` | `(price_list_id) WHERE deleted_at IS NULL` | parziale | componenti di un pacchetto |
| `ix_..._inventory_item` | `(inventory_item_id) WHERE deleted_at IS NULL` | parziale | "quali pacchetti includono questo articolo" (consumo magazzino) |
| `ix_..._linked_package` | `(linked_price_list_id) WHERE deleted_at IS NULL` | parziale | additional vincolati a un pacchetto |

## Campi

### id
- **Legacy**: — (nuovo; il legacy identificava per posizione)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### price_list_id
- **Legacy**: `price_lists.id` (contenitore di `details`)
- **Descrizione**: voce di listino a cui appartiene la componente.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_price_lists.id`](catalog_pricing_price_lists.md) `ON DELETE CASCADE`.

### item_kind
- **Legacy**: array di provenienza (`PackageItemSelected` / `selectedAreaItems` / `IncludedServices` / `selectedPackages`)
- **Descrizione**: natura della componente.
- **Tipo**: `enum catalog.package_item_kind` — valori: `inventory_item`, `area_item`, `included_service`,
  `linked_package`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: determina quale colonna di riferimento è valorizzata.

### inventory_item_id
- **Legacy**: `PackageItemSelected[].name` (`"Umbrella"`, `"Sunbed"`, …) risolto verso l'articolo di magazzino
- **Descrizione**: articolo di magazzino incluso (per `inventory_item` / `area_item`).
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (valorizzato solo per `inventory_item` / `area_item`).
- **Relazioni**: **FK** → [`catalog_inventory_items.id`](catalog_inventory_items.md) `ON DELETE RESTRICT`.
- **Note**: in migrazione `name` (+ `it`) → `inventory_item_id` per property; nomi non risolvibili → log.

### included_service_id
- **Legacy**: `details.IncludedServices` (riferimento a `services` / `bathouses.services`)
- **Descrizione**: servizio/dotazione incluso nel pacchetto (per `included_service`).
- **Tipo**: `integer`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_service_amenities.id`](catalog_service_amenities.md) `ON DELETE RESTRICT`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 11.468 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### linked_price_list_id
- **Legacy**: `details.selectedPackages[]`
- **Descrizione**: pacchetto a cui questo `additional` è vincolato (per `linked_package`).
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_pricing_price_lists.id`](catalog_pricing_price_lists.md) `ON DELETE CASCADE`.

### quantity
- **Legacy**: `PackageItemSelected[].quantity`
- **Descrizione**: quantità dell'**articolo di magazzino** (`inventory_item_id`) inclusa nel pacchetto —
  es. "Ombrellone + 2 lettini" → riga `Umbrella` qty `1`, riga `Sunbed` qty `2`. Per
  `item_kind = included_service` / `linked_price_list` vale `1`.
- **Tipo**: `smallint`.
- **Default**: `1`.
- **Nullabile**: No (per `included_service` / `linked_price_list` = `1`).
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

Nessun JSON. Tre array `details.*` → una tabella con `item_kind` discriminatore + FK reali (elimina i
riferimenti per stringa e il refuso `"Restuarant"`-style).

## Suggerimenti per le prestazioni

- Poche righe per pacchetto. L'indice `(inventory_item_id)` serve al calcolo del **consumo di magazzino**
  (C1, `resource_consumptions` — [`docs/03-gestione-dati.md` §3.8](../../../progettazione/03-gestione-dati.md)):
  quando si prenota un pacchetto, si scalano gli articoli inclusi.
- `packageItemString` (etichetta compatta) si ricompone da qui in presentazione (tabella
  `PACKAGE_INITIALS` → risorsa/costante applicativa).

## Implementazione .NET

**Entità figlia** dell'aggregate `PriceList` (modulo `Module.Catalog`, schema `catalog`). Una riga per
componente del pacchetto (o per pacchetto vincolato, per un `additional`); esposta come collezione
`PriceList.Items`, ma con FK reali (non `OwnsMany`) perché referenzia altre entità dello schema
(`InventoryItem`, `ServiceAmenity`, sé stessa via `linked_price_list_id`).

### Enum proprio

```csharp
// Module.Catalog/Domain/Enums/PackageItemKind.cs
public enum PackageItemKind { InventoryItem = 0, AreaItem = 1, IncludedService = 2, LinkedPackage = 3 }
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<PackageItemKind>("catalog", "package_item_kind");
```

```csharp
// Module.cs / Infrastructure DI
dataSourceBuilder.MapEnum<PackageItemKind>("catalog.package_item_kind");
```

### `PriceListItemConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class PriceListItemConfiguration : IEntityTypeConfiguration<PriceListItem>
{
	public void Configure(EntityTypeBuilder<PriceListItem> builder)
	{
		builder.ToTable("pricing_price_list_items", "catalog", t => t.HasComment(
			"Componente di una voce di listino: articolo incluso, servizio incluso o pacchetto vincolato."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_price_list_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.IsRequired()
			.HasComment("Voce di listino a cui appartiene la componente.");
		builder.HasOne<PriceList>()
			.WithMany(p => p.Items)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_items_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.ItemKind)
			.HasColumnName("item_kind")
			.HasColumnType("catalog.package_item_kind")
			.IsRequired()
			.HasComment("Natura della componente: determina quale colonna di riferimento è valorizzata.");

		builder.Property(x => x.InventoryItemId)
			.HasColumnName("inventory_item_id")
			.HasComment("Articolo di magazzino incluso (per inventory_item / area_item).");
		builder.HasOne<InventoryItem>()
			.WithMany()
			.HasForeignKey(x => x.InventoryItemId)
			.HasConstraintName("fk_catalog_pricing_price_list_items_inventory_item")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.IncludedServiceId)
			.HasColumnName("included_service_id")
			.HasComment("Servizio/dotazione incluso nel pacchetto (per included_service).");
		builder.HasOne<ServiceAmenity>()
			.WithMany()
			.HasForeignKey(x => x.IncludedServiceId)
			.HasConstraintName("fk_catalog_pricing_price_list_items_included_service")
			.OnDelete(DeleteBehavior.Restrict);

		builder.Property(x => x.LinkedPriceListId)
			.HasColumnName("linked_price_list_id")
			.HasComment("Pacchetto a cui questo additional è vincolato (per linked_package).");
		builder.HasOne<PriceList>()
			.WithMany()
			.HasForeignKey(x => x.LinkedPriceListId)
			.HasConstraintName("fk_catalog_pricing_price_list_items_linked_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasDefaultValue((short)1)
			.IsRequired()
			.HasComment("Quantità dell'articolo di magazzino inclusa nel pacchetto (1 per service/linked_package).");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della componente.");

		builder.HasIndex(x => x.PriceListId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_items_package");

		builder.HasIndex(x => x.InventoryItemId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_items_inventory_item");

		builder.HasIndex(x => x.LinkedPriceListId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_price_list_items_linked_package");
	}
}
```

`PriceListItem` implementa solo `ISoftDeletable` più `created_at`/`updated_at` (`IAuditable`); nessun
evento di dominio proprio (non è un aggregate root).

### Relazioni

- `price_list_id` → `PriceList` (stesso schema): FK reale, `WithMany(p => p.Items)`.
- `inventory_item_id` → `catalog_inventory_items` (stesso schema, altra area del modulo `Catalog`): FK
  reale, `ON DELETE RESTRICT` (non si cancella un articolo di magazzino referenziato).
- `included_service_id` → `catalog_service_amenities` (stesso schema): FK reale, `ON DELETE RESTRICT`.
- `linked_price_list_id` → `PriceList` (self-reference, stesso schema): FK reale, `ON DELETE CASCADE`.

### Note di migrazione

- Servizio applicativo `PackageLabelBuilder` (ex `checkPackageString`) ricompone `packageItemString` (es.
  `"2S+1U"`) dalle righe, per lingua — non persistito.
- Migrazione: risoluzione `PackageItemSelected[].name` (`"Umbrella"`, `"Sunbed"`, …) → `inventory_item_id`
  per property; nomi non risolvibili → riga scartata con log.
