# Tabella `catalog_map_layouts`

> **Dominio**: C2 — `catalog` · **sottoarea**: `map` · **schema**: `catalog` · **nome flat**:
> `catalog_map_layouts` · **forma canonica**: `catalog.map_layouts`.
>
> Rimpiazza la legacy [`configure_maps`](../../../analisi-pre/db/configure_maps.md) (**175 righe** = 1 per
> stabilimento; companion 1:1 di `configurations`). Fonti:
> [`analisi-pre/db/configure_maps.md`](../../../analisi-pre/db/configure_maps.md),
> [`analisi-pre/db/configurations.md`](../../../analisi-pre/db/configurations.md),
> [`docs/03-gestione-dati.md` §1](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Layout grafico** della mappa spiaggia di uno stabilimento (1 riga per property): dimensioni della
griglia (righe × colonne), immagine di sfondo/template, colore, e le icone scelte per ogni tipo di
elemento nei due stati (occupato/libero). Lo **stato cella-per-cella** vive invece in
[`catalog_map_spots`](catalog_map_spots.md).

Correzioni rispetto al legacy:

- **Icone come FK** verso [`catalog_media_assets`](catalog_media_assets.md) / riferimento a
  [`catalog_map_icons`](catalog_map_icons.md), non URL `varchar` copiato.
- **`specialCabin` → non migrato** (`NULL` su tutte le 175 righe in produzione).
- Le colonne "ombrellone speciale 1..5" (occupato/libero) → tabella figlia
  [`catalog_map_layout_icons`](#) *(vedi nota sotto)* per non avere 22 colonne icona; **in questa
  iterazione** si adotta la variante compatta con colonne dedicate per i tipi principali + una figlia per
  gli speciali — vedi "Valutazione JSON".
- **Bug di ridimensionamento distruttivo** (ridurre righe/colonne troncava `beachMapData` senza
  conferma) → nel target il resize è un'operazione esplicita che sposta/segnala gli spot fuori griglia,
  non un `slice` silenzioso.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_map_layouts` | `id` | PK | — |
| `uq_catalog_map_layouts_property` | `(property_id)` | UNIQUE | 1:1 con la property |

## Campi

### id
- **Legacy**: `configure_maps.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da [`catalog_map_layout_special_icons`](#tabella-figlia-catalog_map_layout_special_icons).

### property_id
- **Legacy**: `configure_maps.propertyId`
- **Descrizione**: stabilimento proprietario del layout.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_properties.id`](catalog_properties.md) `ON DELETE CASCADE`; `UNIQUE`.

### grid_rows
- **Legacy**: `configure_maps.rows`
- **Descrizione**: numero di righe della griglia (range prod `0`…`40`).
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### grid_columns
- **Legacy**: `configure_maps.items` (**numerico**, nonostante il nome: numero di colonne per riga; range prod `0`…`77`)
- **Descrizione**: numero di colonne della griglia.
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: rinominato `grid_columns` per eliminare l'ambiguità del nome legacy `items`.

### is_custom_template
- **Legacy**: `configure_maps.isCustomTemplate` (prod: `1` 91 righe, `0` 84)
- **Descrizione**: `true` se lo sfondo è un'immagine caricata dal gestore; `false` se è un template
  predefinito ([`catalog_map_icons`](catalog_map_icons.md) `category = template`).
- **Tipo**: `boolean`.
- **Default**: `false`.
- **Nullabile**: No.
- **Relazioni**: —.

### template_asset_id
- **Legacy**: `configure_maps.templateImg` (URL)
- **Descrizione**: immagine di sfondo della griglia.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) (`kind = 'icon'`).

### background_color
- **Legacy**: `configure_maps.colorCode` (hex `#RRGGBB`; `NULL` su 24%; color-picker libero, **non** una palette)
- **Descrizione**: colore di sfondo/tema della griglia.
- **Tipo**: `char(7)` (hex `#RRGGBB`), `CHECK` sul formato.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: resta hex libero (input `<input type="color">` — nessun ENUM/lookup colori).

### umbrella_occupied_asset_id / umbrella_free_asset_id
- **Legacy**: `configure_maps.umbrellaImg` / `configure_maps.umbrellaImage_free` (URL)
- **Descrizione**: icone dell'ombrellone standard nei due stati.
- **Tipo**: `bigint` ciascuna.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) (`kind = 'icon'`).

### created_by_user_id
- **Legacy**: `configure_maps.createdBy`
- **Descrizione**: operatore che ha configurato il layout.
- **Tipo**: `uuid`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 `identity_access_users`.

### created_at / updated_at
- **Legacy**: `configure_maps.createdAt` / `configure_maps.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Tabella figlia `catalog_map_layout_special_icons`

Sostituisce le 10 colonne `specialUmbrella1..5` / `specialUmbrella1_free..5_free` di `configure_maps`.
Una riga per (layout × variante speciale × stato).

| Colonna | Legacy | Tipo | Note |
|---|---|---|---|
| `id` | — | `bigint` IDENTITY | PK |
| `layout_id` | — | `bigint` | **FK** → `catalog_map_layouts.id` `ON DELETE CASCADE` |
| `variant` | indice 1..5 nel nome colonna | `smallint` | 1–5 (`SpecialUmbrella1..5`); estendibile |
| `state` | suffisso `_free` o assenza | `enum catalog.map_icon_state` (`occupied`/`free`) | — |
| `media_asset_id` | valore URL della colonna | `bigint` | **FK** → `catalog_media_assets.id` |
| `created_at` / `updated_at` / `deleted_at` | — | `timestamptz` | standard |

`UNIQUE (layout_id, variant, state) WHERE deleted_at IS NULL`. `specialCabin` (sempre `NULL` in
produzione) **non** riprodotto — se il committente lo vuole, è `variant = 0` / un `state`-less speciale.

## Valutazione JSON

Nessun JSON. Le 22 colonne icona del legacy (2 principali + 10 speciali + template + 5 "cabina/tenda"
mai usate) diventano: FK dirette per i tipi principali + `catalog_map_layout_special_icons` per le
varianti. Le colonne `specialUmbrella*_free` placeholder `umbrella-empty.png` → `NULL` in migrazione.

## Suggerimenti per le prestazioni

- 1 riga per property. Caricamento layout = 1 riga + join a `catalog_media_assets` per gli URL.
- Il **rendering della mappa** per il cliente (griglia + spot + stato occupazione del giorno) si serve da
  un read model che combina `catalog_map_layouts` + `catalog_map_spots` + occupazione (C1) — non da query
  live su queste tabelle ad ogni apertura.

## Implementazione .NET

`MapLayout` è un'**entità figlia** di `Property` (relazione 1:1, non owned type: ha proprio `id`,
timestamp e soft-delete come tutte le tabelle di questo dominio — §5 del template riserva gli owned type
ai cluster di valore, non a questo caso). `MapLayoutSpecialIcon` è a sua volta entità figlia di
`MapLayout`. Modulo `Module.Catalog`, area `Map`, schema `catalog`, tabelle `map_layouts` e
`map_layout_special_icons`.

### Enum

`MapLayoutSpecialIcon.State` usa l'enum **condiviso** `MapIconState` (`catalog.map_icon_state`) —
definizione canonica in [`catalog_map_icons.md`](catalog_map_icons.md), non ridefinito qui.

### `MapLayoutConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class MapLayoutConfiguration : IEntityTypeConfiguration<MapLayout>
{
	public void Configure(EntityTypeBuilder<MapLayout> builder)
	{
		builder.ToTable("map_layouts", "catalog", t => t.HasComment(
			"Layout grafico della mappa spiaggia di uno stabilimento: griglia, sfondo, colore, icone per tipo elemento nei due stati."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_layouts");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("Stabilimento proprietario del layout (1:1).");

		builder.Property(x => x.GridRows)
			.HasColumnName("grid_rows")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di righe della griglia.");

		builder.Property(x => x.GridColumns)
			.HasColumnName("grid_columns")
			.IsRequired()
			.HasDefaultValue((short)0)
			.HasComment("Numero di colonne della griglia (legacy: configure_maps.items, rinominato per eliminare l'ambiguità).");

		builder.Property(x => x.IsCustomTemplate)
			.HasColumnName("is_custom_template")
			.IsRequired()
			.HasDefaultValue(false)
			.HasComment("True se lo sfondo è un'immagine caricata dal gestore; false se è un template predefinito.");

		builder.Property(x => x.TemplateAssetId)
			.HasColumnName("template_asset_id")
			.HasComment("Immagine di sfondo della griglia (catalog.media_assets, kind = icon).");

		builder.Property(x => x.BackgroundColor)
			.HasColumnName("background_color")
			.HasColumnType("char(7)")
			.HasComment("Colore di sfondo/tema della griglia, hex #RRGGBB.");
		builder.ToTable(t => t.HasCheckConstraint(
			"ck_catalog_map_layouts_background_color",
			"background_color IS NULL OR background_color ~ '^#[0-9A-Fa-f]{6}$'"));

		builder.Property(x => x.UmbrellaOccupiedAssetId)
			.HasColumnName("umbrella_occupied_asset_id")
			.HasComment("Icona dell'ombrellone standard nello stato occupato.");
		builder.Property(x => x.UmbrellaFreeAssetId)
			.HasColumnName("umbrella_free_asset_id")
			.HasComment("Icona dell'ombrellone standard nello stato libero.");

		builder.Property(x => x.CreatedByUserPublicId)
			.HasColumnName("created_by_user_id")
			.HasComment("public_id di identity.access_users, copiato — nessuna FK cross-schema. Operatore che ha configurato il layout.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete del layout.");

		builder.HasIndex(x => x.PropertyId).IsUnique().HasDatabaseName("uq_catalog_map_layouts_property");

		builder.HasOne(x => x.Property)
			.WithOne()
			.HasForeignKey<MapLayout>(x => x.PropertyId)
			.HasConstraintName("fk_catalog_map_layouts_property")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.TemplateAsset).WithMany().HasForeignKey(x => x.TemplateAssetId).HasConstraintName("fk_catalog_map_layouts_template_asset");
		builder.HasOne(x => x.UmbrellaOccupiedAsset).WithMany().HasForeignKey(x => x.UmbrellaOccupiedAssetId).HasConstraintName("fk_catalog_map_layouts_umbrella_occupied_asset");
		builder.HasOne(x => x.UmbrellaFreeAsset).WithMany().HasForeignKey(x => x.UmbrellaFreeAssetId).HasConstraintName("fk_catalog_map_layouts_umbrella_free_asset");
	}
}
```

### `MapLayoutSpecialIconConfiguration`

Sostituisce le 10 colonne `specialUmbrella1..5`/`specialUmbrella1_free..5_free`: una riga per (layout ×
variante × stato).

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class MapLayoutSpecialIconConfiguration : IEntityTypeConfiguration<MapLayoutSpecialIcon>
{
	public void Configure(EntityTypeBuilder<MapLayoutSpecialIcon> builder)
	{
		builder.ToTable("map_layout_special_icons", "catalog", t => t.HasComment(
			"Icone delle varianti speciali di ombrellone (1-5) per stato occupato/libero. Sostituisce le colonne specialUmbrella1..5 di configure_maps."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_layout_special_icons");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.LayoutId)
			.HasColumnName("layout_id")
			.IsRequired()
			.HasComment("Layout a cui appartiene la variante.");

		builder.Property(x => x.Variant)
			.HasColumnName("variant")
			.IsRequired()
			.HasComment("Indice della variante speciale (1-5, corrispondente a SpecialUmbrella1..5 nel legacy); estendibile.");

		builder.Property(x => x.State)
			.HasColumnName("state")
			.HasColumnType("catalog.map_icon_state")
			.IsRequired()
			.HasComment("Stato rappresentato dall'icona (occupied/free).");

		builder.Property(x => x.MediaAssetId)
			.HasColumnName("media_asset_id")
			.IsRequired()
			.HasComment("Icona della variante (catalog.media_assets).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete della variante.");

		builder.HasIndex(x => new { x.LayoutId, x.Variant, x.State })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_map_layout_special_icons_layout_variant_state");

		builder.HasOne(x => x.Layout)
			.WithMany(l => l.SpecialIcons)
			.HasForeignKey(x => x.LayoutId)
			.HasConstraintName("fk_catalog_map_layout_special_icons_layout")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.MediaAsset)
			.WithMany()
			.HasForeignKey(x => x.MediaAssetId)
			.HasConstraintName("fk_catalog_map_layout_special_icons_media_asset")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

Entrambe le entità implementano `IAuditable`/`ISoftDeletable`.

### Relazioni

- `MapLayout.PropertyId` → `Property` (stesso schema): FK reale 1:1, `ON DELETE CASCADE`.
- `TemplateAssetId`/`UmbrellaOccupiedAssetId`/`UmbrellaFreeAssetId`/`MapLayoutSpecialIcon.MediaAssetId` →
  `MediaAsset` (stesso schema): FK reali.
- `CreatedByUserPublicId`: id copiato da C5 `identity.access_users` — solo `.HasComment`, nessuna FK.

### Note di migrazione

- `configure_maps.items` (numerico nonostante il nome) → `GridColumns`.
- URL icone (`templateImg`, `umbrellaImg`, `umbrellaImage_free`, `specialUmbrella1..5[_free]`) →
  risoluzione/creazione `MediaAsset`.
- `specialCabin` (sempre `NULL` in produzione) e le colonne placeholder (`umbrella-empty.png`) **non**
  migrate.
- `ResizeGrid(rows, columns)` è un'operazione esplicita del dominio: sposta/segnala gli spot fuori dai
  nuovi confini invece del troncamento silenzioso del bug legacy (`slice` senza conferma).
