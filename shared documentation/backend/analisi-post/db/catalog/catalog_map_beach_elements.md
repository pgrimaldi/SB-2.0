# Tabella `catalog_map_beach_elements`

> **Dominio**: C2 — `catalog` · **sottoarea**: `map` · **schema**: `catalog` · **nome flat**:
> `catalog_map_beach_elements` · **forma canonica**: `catalog.map_beach_elements`.
>
> Nuova (2026-10-25), senza corrispondente nel legacy: decisioni C3 e M2 del committente.

---

## Descrizione

Catalogo di piattaforma degli **elementi spiaggia** solo grafici che il gestore posiziona sulla mappa:
Passerella 1–3, Prato, Doccia, Guarda spiaggia, Pianta, Bar, Ingresso, Torretta (seed della migration
`MapEditorLayout`). Non sono prenotabili, non hanno numero né settore e possono occupare più caselle; sulla mappa
compare solo l'icona. Il systemadmin ne aggiunge di nuovi dal pannello di amministrazione. Il posizionamento vive
in [`catalog_map_spots`](catalog_map_spots.md) (`spot_type = beach_element`, `beach_element_id`).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_map_beach_elements` | `id` | PK | — |
| `uq_catalog_map_beach_elements_public_id` | `(public_id)` | UNIQUE | id esposto dalle API |
| `uq_catalog_map_beach_elements_name` | `(name) WHERE deleted_at IS NULL` | UNIQUE parziale | nome unico |
| `ix_catalog_map_beach_elements_media_asset` | `(media_asset_id)` | — | FK |

## Campi

### id
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`. PK; referenziata da `catalog_map_spots.beach_element_id`.

### public_id
- **Tipo**: `uuid` (UUIDv7). Id usato dalle API.

### name
- **Tipo**: `varchar(80)`, obbligatorio. Nome per il pannello di amministrazione.

### media_asset_id
- **Tipo**: `bigint`, nullabile. FK → [`catalog_media_assets.id`](catalog_media_assets.md), `ON DELETE RESTRICT`.
  `NULL` finché il systemadmin non carica l'icona (le 10 voci del seed non l'hanno ancora).

### sort_order
- **Tipo**: `smallint`, default `0`. Ordine nell'elenco mostrato al gestore.

### is_active
- **Tipo**: `boolean`, default `true`. Elemento selezionabile dal gestore; quelli già posizionati restano.

### created_at / updated_at / deleted_at
- `timestamptz`; `deleted_at` per la cancellazione logica.

## Implementazione .NET

### `MapBeachElement`

```csharp
namespace Module.Catalog.Domain.Entities;

/// <summary>
/// Elemento spiaggia del catalogo di piattaforma (passerella, doccia, bar, torretta…): solo grafico, il gestore lo
/// sceglie nella configurazione della mappa e il systemadmin ne aggiunge di nuovi.
/// </summary>
public sealed class MapBeachElement : IAuditable, ISoftDeletable
{
	public const int NameMaxLength = 80;

	public long Id { get; private set; }
	public Guid PublicId { get; private set; }
	public string Name { get; private set; } = null!;
	public long? MediaAssetId { get; private set; }
	public MediaAsset? MediaAsset { get; private set; }
	public short SortOrder { get; private set; }
	public bool IsActive { get; private set; }
	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	private MapBeachElement() { }

	public static MapBeachElement Create(string name, short sortOrder, long? mediaAssetId)
	{
		var element = new MapBeachElement { PublicId = Guid.CreateVersion7(), SortOrder = sortOrder, IsActive = true };
		element.Update(name, mediaAssetId, true);
		return element;
	}

	public void Update(string name, long? mediaAssetId, bool isActive)
	{
		var trimmed = name.Trim();
		if (trimmed.Length is 0 or > NameMaxLength)
			throw new ArgumentException("Nome dell'elemento vuoto o troppo lungo.", nameof(name));
		Name = trimmed;
		MediaAssetId = mediaAssetId;
		IsActive = isActive;
	}
}
```

### `MapBeachElementConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

/// <summary>Mapping EF Core di <see cref="MapBeachElement"/> su catalog.map_beach_elements.</summary>
public sealed class MapBeachElementConfiguration : IEntityTypeConfiguration<MapBeachElement>
{
	public void Configure(EntityTypeBuilder<MapBeachElement> builder)
	{
		builder.ToTable("map_beach_elements", "catalog", t => t.HasComment(
			"Catalogo di piattaforma degli elementi spiaggia solo grafici (passerella, doccia, bar, torretta, ...); il systemadmin ne aggiunge di nuovi."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_beach_elements");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.PublicId).HasColumnName("public_id").ValueGeneratedNever().IsRequired()
			.HasComment("Id pubblico (UUIDv7) usato dalle API.");
		builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(MapBeachElement.NameMaxLength).IsRequired()
			.HasComment("Nome dell'elemento per il pannello di amministrazione; sulla mappa compare solo l'icona.");
		builder.Property(x => x.MediaAssetId).HasColumnName("media_asset_id")
			.HasComment("Icona dell'elemento; NULL finché il systemadmin non la carica.");
		builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired().HasDefaultValue((short)0)
			.HasComment("Ordine nell'elenco mostrato al gestore.");
		builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired().HasDefaultValue(true)
			.HasComment("Elemento selezionabile dal gestore.");
		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

		builder.HasOne(x => x.MediaAsset)
			.WithMany()
			.HasForeignKey(x => x.MediaAssetId)
			.HasConstraintName("fk_catalog_map_beach_elements_media_asset")
			.OnDelete(DeleteBehavior.Restrict);

		builder.HasIndex(x => x.MediaAssetId).HasDatabaseName("ix_catalog_map_beach_elements_media_asset");
		builder.HasIndex(x => x.PublicId).IsUnique().HasDatabaseName("uq_catalog_map_beach_elements_public_id");
		builder.HasIndex(x => x.Name).IsUnique().HasFilter("deleted_at IS NULL").HasDatabaseName("uq_catalog_map_beach_elements_name");
	}
}
```

### Note di migrazione

Nessuna riga dal legacy: le 10 voci arrivano dal seed della migration.
