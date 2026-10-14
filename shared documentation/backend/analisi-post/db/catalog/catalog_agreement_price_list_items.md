# Tabella `catalog_agreement_price_list_items`

> **Dominio**: C2 — `catalog` · **sottoarea**: `agreement` · **schema**: `catalog` · **nome flat**:
> `catalog_agreement_price_list_items` · **forma canonica**: `catalog.agreement_price_list_items`.
>
> Rimpiazza `hotel_price_lists.details.PackageItemSelected` (+ `IncludedServices` / `selectedPackages`).
> Struttura **analoga** a [`catalog_pricing_price_list_items`](catalog_pricing_price_list_items.md). Fonti:
> [`analisi-pre/db/hotel_price_lists.md`](../../../analisi-pre/db/hotel_price_lists.md),
> [`analisi-pre/db/price_lists.md`](../../../analisi-pre/db/price_lists.md).

---

## Descrizione

Le **componenti** di un listino/voucher hotel: articoli di magazzino inclusi con quantità, servizi
inclusi, pacchetti collegati (per gli `additional`). Una riga per componente.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_agreement_price_list_items` | `id` | PK | — |
| `ix_..._price_list` | `(price_list_id) WHERE deleted_at IS NULL` | parziale | componenti di un listino |
| `ix_..._inventory_item` | `(inventory_item_id) WHERE deleted_at IS NULL` | parziale | consumo magazzino |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### price_list_id
- **Legacy**: `hotel_price_lists.id` (contenitore di `details`)
- **Descrizione**: listino/voucher a cui appartiene la componente.
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_agreement_price_lists.id`](catalog_agreement_price_lists.md)
  `ON DELETE CASCADE`.

### item_kind
- **Legacy**: array di provenienza (`PackageItemSelected` / `IncludedServices` / `selectedPackages`)
- **Descrizione**: natura della componente.
- **Tipo**: `enum catalog.package_item_kind` — valori: `inventory_item`, `area_item`, `included_service`,
  `linked_package` (stesso enum di
  [`catalog_pricing_price_list_items`](catalog_pricing_price_list_items.md)).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### inventory_item_id
- **Legacy**: `details.PackageItemSelected[].name` (es. `"Umbrella"`, `"Sunbed"`) risolto
- **Descrizione**: articolo di magazzino incluso.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì (solo per `inventory_item` / `area_item`).
- **Relazioni**: **FK** → [`catalog_inventory_items.id`](catalog_inventory_items.md) `ON DELETE RESTRICT`.

### included_service_id
- **Legacy**: `details.IncludedServices`
- **Descrizione**: dotazione/servizio incluso.
- **Tipo**: `integer`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_service_amenities.id`](catalog_service_amenities.md)
  `ON DELETE RESTRICT`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 3.550 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### linked_price_list_id
- **Legacy**: `details.selectedPackages[]`
- **Descrizione**: listino hotel a cui questo `additional` è vincolato.
- **Tipo**: `integer`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_agreement_price_lists.id`](catalog_agreement_price_lists.md)
  `ON DELETE CASCADE`.

### quantity
- **Legacy**: `details.PackageItemSelected[].quantity`
- **Descrizione**: quantità dell'**articolo di magazzino** (`inventory_item_id`) inclusa nel
  pacchetto/voucher hotel — es. pacchetto "Ombrellone + 2 lettini" → riga `Umbrella` qty `1`, riga
  `Sunbed` qty `2`. Per `item_kind = included_service` / `linked_price_list` vale `1`.
- **Tipo**: `smallint`.
- **Default**: `1`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: della riga contenitore / —
- **Descrizione**: standard.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON — vedi [`catalog_pricing_price_list_items`](catalog_pricing_price_list_items.md) (stesso principio).

## Suggerimenti per le prestazioni

- Poche righe per listino; `(inventory_item_id)` per il calcolo consumo magazzino (C1).

## Implementazione .NET

Entità figlia di [`AgreementPriceList`](catalog_agreement_price_lists.md) (componenti del listino/voucher:
articoli di magazzino, servizi inclusi, pacchetti collegati). Modulo `Module.Catalog`, schema `catalog`.
`item_kind` è l'enum **condiviso** `PackageItemKind` (`catalog.package_item_kind`) già usato da
[`catalog_pricing_price_list_items`](catalog_pricing_price_list_items.md) — non ridefinito qui.

```csharp
// Module.Catalog.Domain.Entities/AgreementPriceListItem.cs
namespace Module.Catalog.Domain.Entities;

public sealed class AgreementPriceListItem : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long PriceListId { get; private set; }
	public AgreementPriceList PriceList { get; private set; } = null!;
	public PackageItemKind ItemKind { get; private set; }
	public long? InventoryItemId { get; private set; }
	public InventoryItem? InventoryItemRef { get; private set; }
	public long? IncludedServiceId { get; private set; }
	public ServiceAmenity? IncludedService { get; private set; }
	public long? LinkedPriceListId { get; private set; }
	public AgreementPriceList? LinkedPriceList { get; private set; }
	public short Quantity { get; private set; }

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public void Delete(DateTime utcNow) => DeletedAt = utcNow;
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog.Infrastructure.Configurations/AgreementPriceListItemConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class AgreementPriceListItemConfiguration : IEntityTypeConfiguration<AgreementPriceListItem>
{
	public void Configure(EntityTypeBuilder<AgreementPriceListItem> builder)
	{
		builder.ToTable("catalog_agreement_price_list_items", "catalog", t => t.HasComment(
			"Componente di un listino/voucher hotel: articolo di magazzino, servizio incluso o pacchetto collegato."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_agreement_price_list_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PriceListId)
			.HasColumnName("price_list_id")
			.HasColumnType("integer")
			.IsRequired()
			.HasComment("Listino/voucher a cui appartiene la componente.");

		builder.Property(x => x.ItemKind)
			.HasColumnName("item_kind")
			.IsRequired()
			.HasComment("Natura della componente: articolo di magazzino, articolo d'area, servizio incluso o pacchetto collegato.");

		builder.Property(x => x.InventoryItemId)
			.HasColumnName("inventory_item_id")
			.HasComment("Articolo di magazzino incluso (valorizzato solo per item_kind inventory_item/area_item).");

		builder.Property(x => x.IncludedServiceId)
			.HasColumnName("included_service_id")
			.HasComment("Dotazione/servizio incluso.");

		builder.Property(x => x.LinkedPriceListId)
			.HasColumnName("linked_price_list_id")
			.HasColumnType("integer")
			.HasComment("Listino hotel a cui questo additional è vincolato.");

		builder.Property(x => x.Quantity)
			.HasColumnName("quantity")
			.HasColumnType("smallint")
			.IsRequired()
			.HasDefaultValue((short)1)
			.HasComment("Quantità dell'articolo di magazzino inclusa; 1 per servizi inclusi/pacchetti collegati.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete.");

		builder.HasOne(x => x.PriceList)
			.WithMany(pl => pl.Items)
			.HasForeignKey(x => x.PriceListId)
			.HasConstraintName("fk_catalog_agreement_price_list_items_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.InventoryItemRef)
			.WithMany()
			.HasForeignKey(x => x.InventoryItemId)
			.HasConstraintName("fk_catalog_agreement_price_list_items_inventory_item")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.IncludedService)
			.WithMany()
			.HasForeignKey(x => x.IncludedServiceId)
			.HasConstraintName("fk_catalog_agreement_price_list_items_included_service")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.LinkedPriceList)
			.WithMany()
			.HasForeignKey(x => x.LinkedPriceListId)
			.HasConstraintName("fk_catalog_agreement_price_list_items_linked_price_list")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => x.PriceListId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_items_price_list");

		builder.HasIndex(x => x.InventoryItemId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_agreement_price_list_items_inventory_item");
	}
}
```

**Note di migrazione**: stesso schema di trasformazione di
[`catalog_pricing_price_list_items`](catalog_pricing_price_list_items.md): `PackageItemSelected[].name`
risolto contro `catalog_inventory_items` della property del partner (`item_kind = inventory_item`/
`area_item`), `IncludedServices[]` → righe `included_service`, `selectedPackages[]` → righe
`linked_package` con `linked_price_list_id`; nomi non risolvibili → riga scartata con log. `LinkedPriceList`
è una self-reference sulla stessa tabella genitrice, non una FK circolare sull'item stesso.
