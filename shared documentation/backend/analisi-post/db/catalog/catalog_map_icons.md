# Tabella `catalog_map_icons`

> **Dominio**: C2 — `catalog` · **sottoarea**: `map` · **schema**: `catalog` · **nome flat**:
> `catalog_map_icons` · **forma canonica**: `catalog.map_icons`.
>
> Rimpiazza la legacy [`configure_images`](../../../analisi-pre/db/configure_images.md) (**89 righe** in
> produzione, globale/master, con pannello di amministrazione — nel legacy il codice di rotta lo chiama
> "SuperAdmin", ma il pannello reale è il modulo "Configuration Settings" di
> [`front-admin/`](../../../../../../../front-admin/), riservato al ruolo `systemadmin`). Fonti:
> [`analisi-pre/db/configure_images.md`](../../../analisi-pre/db/configure_images.md),
> [`analisi-pre/db/configure_maps.md`](../../../analisi-pre/db/configure_maps.md),
> [`analisi-pre/business-logic/systemadmin/02-catalogo-icone-mappa-spiaggia.md`](../../../analisi-pre/business-logic/systemadmin/02-catalogo-icone-mappa-spiaggia.md),
> [`docs/03-gestione-dati.md` §1](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

Catalogo **globale/master** (nessun `property_id`) delle icone/immagini per comporre la mappa spiaggia:
sfondi (`template`), varianti ombrellone/cabina/tenda/lettino nei due stati `occupied`/`free`. Gestito
dal pannello SuperAdmin (create / attiva-disattiva / elimina). [`catalog_map_layouts`](catalog_map_layouts.md)
sceglie le icone da qui.

Correzioni rispetto al legacy:

- **`category` stringa → FK** verso [`catalog_map_icon_categories`](catalog_map_icon_categories.md).
- **`type` (`"Occupied"`/`"Free"`, capitalizzazione incoerente coi commenti DB) → `enum` canonico**.
- **`image` (URL `varchar` con sostituzione S3→CloudFront hardcoded nel controller) → FK** verso
  [`catalog_media_assets`](catalog_media_assets.md) (`kind = 'icon'`); il prefisso vive in configurazione.
- **`deleteconfimage` faceva hard-delete + rimozione file S3 sincrona** → soft-delete + job di GC
  disaccoppiato.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_map_icons` | `id` | PK | — |
| `ix_catalog_map_icons_category_type` | `(category_id, type) WHERE is_active AND deleted_at IS NULL` | parziale | selettore icone (attive per categoria/stato) |
| `ix_catalog_map_icons_media_asset` | `(media_asset_id)` | — | GC asset |

## Campi

### id
- **Legacy**: `configure_images.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità (id legacy preservati).
- **Nullabile**: No.
- **Relazioni**: PK.

### category_id
- **Legacy**: `configure_images.category` (stringa: `umbrella` 77, `template` 8, `sunbed` 2, `cabin` 2; `tent` 0 icone)
- **Descrizione**: categoria dell'icona.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_map_icon_categories.id`](catalog_map_icon_categories.md)
  `ON DELETE RESTRICT`.

### type
- **Legacy**: `configure_images.type` (`"Occupied"` 67, `"Free"` 22; commento DB dice `occupied|free` minuscolo)
- **Descrizione**: stato rappresentato dall'icona.
- **Tipo**: `enum catalog.map_icon_state` — valori: `occupied`, `free`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: per `category = template` il concetto di stato non si applica; in migrazione i `template`
  possono ricevere `type = occupied` per default (campo comunque ignorato per quella categoria) — da
  valutare se rendere `type` nullable per i `template`.

### name
- **Legacy**: `configure_images.name`
- **Descrizione**: nome descrittivo mostrato nel selettore del gestionale (es. "Umbrella 1").
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### media_asset_id
- **Legacy**: `configure_images.image` (URL, S3→CloudFront riscritto a mano nel controller)
- **Descrizione**: il file icona.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) (`kind = 'icon'`)
  `ON DELETE RESTRICT`.

### is_active
- **Legacy**: `configure_images.status` (`0-inactive | 1-active`; in produzione sempre `1`)
- **Descrizione**: icona selezionabile. `activeconfimages` restituiva solo le attive; il pannello
  SuperAdmin le mostrava tutte.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: `configure_images.createdAt` / `configure_images.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: il catalogo è ancora aggiornato (range fino al 2026 in produzione).

### deleted_at
- **Legacy**: `configure_images` `deleteconfimage` (hard-delete)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: la rimozione del file su storage è demandata a un job (agisce su asset `deleted_at` senza
  referenti), non sincrona come nel legacy.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- ~90 righe, quasi statiche → cache applicativa (invalidata dagli eventi del pannello SuperAdmin).
- `catalog_map_layouts` copia il **riferimento** (`media_asset_id`) al momento della scelta, non l'URL:
  aggiornare l'asset propaga a tutte le mappe che lo referenziano (nel legacy no — copia dell'URL).
  Valutare col committente se si vuole quel comportamento o lo snapshot.

## Implementazione .NET

`MapIcon` è un'**entità semplice** (catalogo globale/master, nessun `property_id`, gestita dal pannello
SuperAdmin): `sealed class`, nessun evento. Modulo `Module.Catalog`, area `Map`, schema `catalog`, tabella
`map_icons`.

### Enum proprio (definizione canonica — **condiviso** con [`catalog_map_layouts.md`](catalog_map_layouts.md)`.MapLayoutSpecialIcon.State`)

```csharp
// Module.Catalog/Domain/Enums/MapIconState.cs
public enum MapIconState
{
	Occupied = 0,
	Free = 1
}
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<MapIconState>("catalog", "map_icon_state");
```

```csharp
dataSourceBuilder.MapEnum<MapIconState>("catalog.map_icon_state");
```

### `MapIconConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class MapIconConfiguration : IEntityTypeConfiguration<MapIcon>
{
	public void Configure(EntityTypeBuilder<MapIcon> builder)
	{
		builder.ToTable("map_icons", "catalog", t => t.HasComment(
			"Catalogo globale/master delle icone/immagini per comporre la mappa spiaggia: sfondi, varianti ombrellone/cabina/tenda/lettino nei due stati."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_icons");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.CategoryId)
			.HasColumnName("category_id")
			.IsRequired()
			.HasComment("Categoria dell'icona (catalog.map_icon_categories).");

		builder.Property(x => x.Type)
			.HasColumnName("type")
			.HasColumnType("catalog.map_icon_state")
			.IsRequired()
			.HasComment("Stato rappresentato dall'icona; ignorato (ma valorizzato) per category = template.");

		builder.Property(x => x.Name)
			.HasColumnName("name")
			.HasMaxLength(255)
			.IsRequired()
			.HasComment("Nome descrittivo mostrato nel selettore del gestionale.");

		builder.Property(x => x.MediaAssetId)
			.HasColumnName("media_asset_id")
			.IsRequired()
			.HasComment("Il file icona (catalog.media_assets, kind = icon).");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Icona selezionabile nel pannello SuperAdmin.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete; la rimozione del file su storage è demandata a un job asincrono.");

		builder.HasIndex(x => new { x.CategoryId, x.Type })
			.HasFilter("is_active AND deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_map_icons_category_type");

		builder.HasIndex(x => x.MediaAssetId)
			.HasDatabaseName("ix_catalog_map_icons_media_asset");

		builder.HasOne(x => x.Category)
			.WithMany()
			.HasForeignKey(x => x.CategoryId)
			.HasConstraintName("fk_catalog_map_icons_category")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasOne(x => x.MediaAsset)
			.WithMany()
			.HasForeignKey(x => x.MediaAssetId)
			.HasConstraintName("fk_catalog_map_icons_media_asset")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

`MapIcon` implementa `IAuditable`/`ISoftDeletable`.

### Relazioni

- `CategoryId` → `MapIconCategory` (stesso schema): FK reale, `ON DELETE RESTRICT`.
- `MediaAssetId` → `MediaAsset` (stesso schema): FK reale, `ON DELETE RESTRICT`.
- `MapLayout`/`MapLayoutSpecialIcon` **non** hanno FK verso `MapIcon`: copiano direttamente il
  `MediaAssetId` scelto (snapshot del riferimento, coerente con la nota "Suggerimenti per le
  prestazioni" della scheda).

### Note di migrazione

- `category` stringa (`umbrella`/`template`/`sunbed`/`cabin`/`tent`) → FK `category_id` verso
  `MapIconCategory`.
- `type` (`"Occupied"`/`"Free"`, capitalizzazione incoerente col commento DB legacy) → `MapIconState`
  canonico; per `category = template` il default in migrazione è `Occupied` (campo ignorato per quella
  categoria — valutare col committente se rendere `Type` nullable solo per i template).
- `image` (URL S3→CloudFront riscritto a mano nel controller legacy) → risoluzione/creazione `MediaAsset`.
- `deleteconfimage` (hard-delete + rimozione file S3 sincrona) → soft-delete + job di GC disaccoppiato.
