# Tabella `catalog_bathhouse_photos`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (radice dominio) · **schema**: `catalog` · **nome flat**:
> `catalog_bathhouse_photos` · **forma canonica**: `catalog.bathhouse_photos`.
>
> Tabella di **giunzione ordinata** che sostituisce l'array JSON `bathouses.photos`. Fonti:
> [`docs/03-gestione-dati.md` §1.3](../../../progettazione/03-gestione-dati.md),
> [`analisi-pre/db/bathouses.md` (campo `photos`)](../../../analisi-pre/db/bathouses.md).

---

## Descrizione

Elenco ordinato delle foto della galleria pubblica di una vetrina. Una riga = una foto in posizione
`sort_order`. Sostituisce la riscrittura dell'intero blob JSON `photos` ad ogni aggiunta/rimozione (nel
legacy con una condizione `if (photos.length && photos.length === 0)` **sempre falsa** che rompeva la
rimozione): aggiungere/togliere una foto è un `INSERT`/`DELETE`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_bathhouse_photos` | `id` | PK | — |
| `uq_..._bathhouse_sort` | `(bathhouse_id, sort_order) WHERE deleted_at IS NULL` | UNIQUE parziale | ordine stabile, niente posizioni duplicate |
| `ix_..._bathhouse` | `(bathhouse_id) WHERE deleted_at IS NULL` | parziale | caricamento galleria |
| `ix_..._media_asset` | `(media_asset_id)` | — | "quali vetrine usano questo file" (GC asset) |

## Campi

### id
- **Legacy**: — (nuovo; il legacy identificava la foto solo per posizione nell'array)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### bathhouse_id
- **Legacy**: `bathouses.id` (contenitore dell'array `photos`)
- **Descrizione**: vetrina a cui appartiene la foto.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_bathhouses.id`](catalog_bathhouses.md) `ON DELETE CASCADE`.

### media_asset_id
- **Legacy**: elemento dell'array `bathouses.photos` (URL stringa)
- **Descrizione**: il file immagine.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) (`kind = 'photo'`)
  `ON DELETE RESTRICT`.

### sort_order
- **Legacy**: posizione dell'URL nell'array `bathouses.photos`
- **Descrizione**: ordine di visualizzazione in galleria (0-based).
- **Tipo**: `smallint`.
- **Default**: nessuno (assegnato in coda all'inserimento).
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: il riordino aggiorna solo questa colonna sulle righe interessate.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nel legacy la rimozione era la riscrittura dell'array)
- **Descrizione**: soft-delete (foto rimossa dalla galleria).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: si può anche scegliere hard-delete qui (tabella di puro collegamento) + GC dell'asset; il
  soft-delete è mantenuto per uniformità e per un eventuale "annulla".

## Valutazione JSON

Nessun JSON — è la tabella che *elimina* il JSON `photos`.

## Suggerimenti per le prestazioni

- Caricamento galleria: singola query `WHERE bathhouse_id = ? AND deleted_at IS NULL ORDER BY sort_order`,
  join a `catalog_media_assets` per comporre gli URL in applicazione.
- Volume modesto: ~180 vetrine × decine di foto ⇒ poche migliaia di righe.

## Implementazione .NET

`BathhousePhoto` è un'**entità figlia** di `Bathhouse` (giunzione ordinata verso `MediaAsset`): `sealed
class`, esposta come collezione `Bathhouse.Photos` ordinata per `SortOrder`. Modulo `Module.Catalog`,
schema `catalog`, tabella `bathhouse_photos`. Nessun enum proprio.

### `BathhousePhotoConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class BathhousePhotoConfiguration : IEntityTypeConfiguration<BathhousePhoto>
{
	public void Configure(EntityTypeBuilder<BathhousePhoto> builder)
	{
		builder.ToTable("bathhouse_photos", "catalog", t => t.HasComment(
			"Elenco ordinato delle foto della galleria pubblica di una vetrina. Sostituisce l'array JSON bathouses.photos."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_bathhouse_photos");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.BathhouseId)
			.HasColumnName("bathhouse_id")
			.IsRequired()
			.HasComment("Vetrina a cui appartiene la foto.");

		builder.Property(x => x.MediaAssetId)
			.HasColumnName("media_asset_id")
			.IsRequired()
			.HasComment("Il file immagine (catalog.media_assets, kind = photo).");

		builder.Property(x => x.SortOrder)
			.HasColumnName("sort_order")
			.IsRequired()
			.HasComment("Ordine di visualizzazione in galleria (0-based); il riordino aggiorna solo questa colonna.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete: foto rimossa dalla galleria.");

		builder.HasIndex(x => new { x.BathhouseId, x.SortOrder })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_bathhouse_photos_bathhouse_sort");

		builder.HasIndex(x => x.BathhouseId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_bathhouse_photos_bathhouse");

		builder.HasIndex(x => x.MediaAssetId)
			.HasDatabaseName("ix_catalog_bathhouse_photos_media_asset");

		builder.HasOne(x => x.Bathhouse)
			.WithMany(b => b.Photos)
			.HasForeignKey(x => x.BathhouseId)
			.HasConstraintName("fk_catalog_bathhouse_photos_bathhouse")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.MediaAsset)
			.WithMany()
			.HasForeignKey(x => x.MediaAssetId)
			.HasConstraintName("fk_catalog_bathhouse_photos_media_asset")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

`BathhousePhoto` implementa `IAuditable`/`ISoftDeletable` per uniformità (la scheda ammette anche
hard-delete essendo pura giunzione, ma si mantiene soft-delete per un eventuale "annulla").

### Relazioni

- `BathhouseId` → `Bathhouse` (stesso schema): FK reale, `ON DELETE CASCADE`.
- `MediaAssetId` → `MediaAsset` (stesso schema): FK reale, `ON DELETE RESTRICT`.

### Note di migrazione

- Comandi di dominio `AddPhoto` / `RemovePhoto` / `ReorderPhotos` sostituiscono la riscrittura dell'intero
  array JSON legacy (che aveva un bug di rimozione per condizione sempre falsa).
- Migrazione: per ogni URL nell'array legacy, risolvi/crea il `MediaAsset` corrispondente (deduplica per
  URL se non già presente), poi `INSERT` con `sort_order` = indice nell'array originale.
