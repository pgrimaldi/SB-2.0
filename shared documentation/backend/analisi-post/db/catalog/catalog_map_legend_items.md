# Tabella `catalog_map_legend_items`

> **Dominio**: C2 — `catalog` · **sottoarea**: `map` · **schema**: `catalog` · **nome flat**:
> `catalog_map_legend_items` · **forma canonica**: `catalog.map_legend_items`.
>
> Rimpiazza l'array JSON `properties.onlineBookingOptions.locationOptionObj.legendList`
> (`[{icon, label}]`). Fonti:
> [`analisi-pre/db/properties.md` (campo `onlineBookingOptions` → `legendList`)](../../../analisi-pre/db/properties.md),
> [`docs/03-gestione-dati.md` §1](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

Voci della **legenda** mostrata sulla mappa di prenotazione online del sito pubblico (es. "Ombrellone
libero" / "Ombrellone occupato", con relativa icona). Una riga per voce, ordinata. È dato di
presentazione della mappa per stabilimento → sottoarea `map`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_map_legend_items` | `id` | PK | — |
| `ix_..._property` | `(property_id, sort_order) WHERE deleted_at IS NULL` | parziale | caricamento legenda ordinata |
| `ix_..._icon_asset` | `(icon_asset_id)` | — | GC asset |

## Campi

### id
- **Legacy**: — (nuovo; il legacy identificava per posizione nell'array)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### property_id
- **Legacy**: `properties.id` (contenitore di `onlineBookingOptions`)
- **Descrizione**: stabilimento a cui appartiene la legenda.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`.
- **Note**: la legenda è per property (una mappa per property); non agganciata a
  `catalog_property_online_booking_settings` perché concettualmente è dato di mappa, non di canale.

### label
- **Legacy**: `legendList[].label`
- **Descrizione**: testo della voce di legenda (es. "Ombrellone libero").
- **Tipo**: `varchar(120)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: nel legacy alcune righe avevano `label = ""` — in migrazione le voci vuote si scartano.

### icon_asset_id
- **Legacy**: `legendList[].icon` (URL S3, spesso `null`)
- **Descrizione**: icona della voce di legenda.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) (`kind = 'icon'`).

### sort_order
- **Legacy**: posizione nell'array `legendList`
- **Descrizione**: ordine di visualizzazione.
- **Tipo**: `smallint`.
- **Default**: nessuno (in coda).
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nel legacy la rimozione era riscrittura dell'array)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON — elimina `legendList[]`.

## Suggerimenti per le prestazioni

- Pochissime righe per property. Caricata col read model di rendering mappa.

## Implementazione .NET

`MapLegendItem` è un'**entità figlia** di `Property` (dato di mappa per stabilimento, non agganciata a
`MapLayout` né a `catalog_property_online_booking_settings` — è concettualmente dato di mappa, non di
canale): `sealed class`, esposta come collezione ordinata `Property.MapLegendItems`. Modulo
`Module.Catalog`, area `Map`, schema `catalog`, tabella `map_legend_items`. Nessun enum proprio.

### `MapLegendItemConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class MapLegendItemConfiguration : IEntityTypeConfiguration<MapLegendItem>
{
	public void Configure(EntityTypeBuilder<MapLegendItem> builder)
	{
		builder.ToTable("map_legend_items", "catalog", t => t.HasComment(
			"Voci della legenda mostrata sulla mappa di prenotazione online del sito pubblico. Sostituisce l'array JSON properties.onlineBookingOptions.locationOptionObj.legendList."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_legend_items");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento a cui appartiene la legenda.");

		builder.Property(x => x.Label)
			.HasColumnName("label")
			.HasMaxLength(120)
			.IsRequired()
			.HasComment("Testo della voce di legenda (es. \"Ombrellone libero\").");

		builder.Property(x => x.IconAssetId)
			.HasColumnName("icon_asset_id")
			.HasComment("Icona della voce di legenda (catalog.media_assets, kind = icon).");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasComment("Ordine di visualizzazione della voce.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete della voce di legenda.");

		builder.HasIndex(x => new { x.PropertyId, x.SortOrder })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_map_legend_items_property");

		builder.HasIndex(x => x.IconAssetId)
			.HasDatabaseName("ix_catalog_map_legend_items_icon_asset");

		builder.HasOne(x => x.Property)
			.WithMany(p => p.MapLegendItems)
			.HasForeignKey(x => x.PropertyId)
			.HasConstraintName("fk_catalog_map_legend_items_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.IconAsset)
			.WithMany()
			.HasForeignKey(x => x.IconAssetId)
			.HasConstraintName("fk_catalog_map_legend_items_icon_asset");
	}
}
```

`MapLegendItem` implementa `IAuditable`/`ISoftDeletable`.

### Relazioni

- `PropertyId` → `Property` (stesso schema): FK reale, `ON DELETE CASCADE`.
- `IconAssetId` → `MediaAsset` (stesso schema): FK reale.

### Note di migrazione

- Esplosione dell'array `legendList[]` per property; voci con `label = ""` **scartate**.
- `icon` (URL S3, spesso `null`) → risoluzione/creazione `MediaAsset` solo dove presente.
