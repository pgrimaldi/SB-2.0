# Tabella `catalog_map_icon_categories`

> **Dominio**: C2 — `catalog` · **sottoarea**: `map` · **schema**: `catalog` · **nome flat**:
> `catalog_map_icon_categories` · **forma canonica**: `catalog.map_icon_categories`.
>
> Rimpiazza la legacy [`configure_categories`](../../../analisi-pre/db/configure_categories.md) (**5 righe**,
> globale/master, tutte del 2021, mai modificate). Fonti:
> [`analisi-pre/db/configure_categories.md`](../../../analisi-pre/db/configure_categories.md),
> [`analisi-pre/db/configure_images.md`](../../../analisi-pre/db/configure_images.md),
> [`analisi-pre/business-logic/systemadmin/02-catalogo-icone-mappa-spiaggia.md`](../../../analisi-pre/business-logic/systemadmin/02-catalogo-icone-mappa-spiaggia.md).

---

## Descrizione

Lookup **globale/di piattaforma** (nessun `property_id`): le categorie in cui sono raggruppate le icone
della mappa spiaggia ([`catalog_map_icons`](catalog_map_icons.md)). Seed di 5 righe: `template`,
`umbrella`, `tent`, `sunbed`, `cabin`.

Nel legacy `configure_images.category` era collegata **per stringa** a `configure_categories.category`
(nessuna FK). Qui: [`catalog_map_icons.category_id`](catalog_map_icons.md) è una **FK reale** verso
questa tabella.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_map_icon_categories` | `id` | PK | — |
| `uq_catalog_map_icon_categories_code` | `(code)` | UNIQUE | riferimento stabile in codice/seed |

## Campi

### id
- **Legacy**: `configure_categories.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `smallint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità (id legacy 1–5 preservati).
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da [`catalog_map_icons.category_id`](catalog_map_icons.md).

### code
- **Legacy**: `configure_categories.category` (valore stringa)
- **Descrizione**: codice stabile della categoria, usato in codice e come chiave di risorsa di
  localizzazione.
- **Tipo**: `enum catalog.map_icon_category` — valori: `template`, `umbrella`, `tent`, `sunbed`, `cabin`.
  (In alternativa `varchar(32)` + `UNIQUE` se si prevede che il systemadmin ne aggiunga senza deploy;
  i dati storici — 5 righe fisse dal 2021 — suggeriscono l'`enum`.)
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **nessuna colonna etichetta** — il nome mostrato viene da risorse di localizzazione del
  modulo (`MapIconCategories.{culture}.resx`, chiave = `code`)
  ([`docs/03-gestione-dati.md` §2.2.A](../../../progettazione/03-gestione-dati.md)).

### is_active
- **Legacy**: `configure_categories.status` (sempre `1`; mai usato come filtro)
- **Descrizione**: categoria selezionabile.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: nel legacy inerte; mantenuto per poter ritirare una categoria senza cancellarla.

### created_at / updated_at
- **Legacy**: `configure_categories.createdAt` / `configure_categories.updatedAt`
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (il legacy non aveva soft-delete)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: praticamente immutabile; presente per uniformità.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- 5 righe, quasi immutabili → candidata a cache applicativa in memoria (seed in migration).

## Implementazione .NET

`MapIconCategory` è un'**entità lookup** (Pattern B del template, §4: tabella con `code` `UNIQUE`, non
solo un `ENUM` nudo, perché porta anche `IsActive`/timestamp/soft-delete) il cui `code` è comunque
tipizzato come `ENUM` PG (5 righe fisse dal 2021, mai cresciute da pannello senza deploy). Modulo
`Module.Catalog`, area `Map`, schema `catalog`, tabella `map_icon_categories`. Per evitare la collisione
di nome fra l'entità e il suo enum di codice, l'enum si chiama `MapIconCategoryCode`.

### Enum proprio

```csharp
// Module.Catalog/Domain/Enums/MapIconCategoryCode.cs
public enum MapIconCategoryCode
{
	Template = 0,
	Umbrella = 1,
	Tent = 2,
	Sunbed = 3,
	Cabin = 4
}
```

```csharp
modelBuilder.HasPostgresEnum<MapIconCategoryCode>("catalog", "map_icon_category");
dataSourceBuilder.MapEnum<MapIconCategoryCode>("catalog.map_icon_category");
```

### `MapIconCategoryConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class MapIconCategoryConfiguration : IEntityTypeConfiguration<MapIconCategory>
{
	public void Configure(EntityTypeBuilder<MapIconCategory> builder)
	{
		builder.ToTable("map_icon_categories", "catalog", t => t.HasComment(
			"Lookup globale/di piattaforma: le categorie in cui sono raggruppate le icone della mappa spiaggia."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_map_icon_categories");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.Code)
			.HasColumnName("code")
			.HasColumnType("catalog.map_icon_category")
			.IsRequired()
			.HasComment("Codice stabile della categoria, usato in codice e come chiave di risorsa di localizzazione (nessuna colonna etichetta).");

		builder.Property(x => x.IsActive)
			.HasColumnName("is_active")
			.IsRequired()
			.HasDefaultValue(true)
			.HasComment("Categoria selezionabile; consente di ritirarla senza cancellarla.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete; praticamente immutabile, presente per uniformità.");

		builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("uq_catalog_map_icon_categories_code");
	}
}
```

`MapIconCategory` implementa `IAuditable`/`ISoftDeletable`.

### Relazioni

Nessuna FK in uscita. Referenziata da `MapIcon.CategoryId` (FK reale, definita in
[`catalog_map_icons.md`](catalog_map_icons.md)).

### Note di migrazione

- Seed in migration delle 5 righe (`id` 1–5 preservati dal legacy: `template`, `umbrella`, `tent`,
  `sunbed`, `cabin`).
- Il match legacy per stringa (`configure_images.category` ↔ `configure_categories.category`) diventa la
  FK reale `MapIcon.CategoryId`.
- Nessuna colonna etichetta: il nome mostrato viene da risorse di localizzazione (`MapIconCategories.
  {culture}.resx`, chiave = `Code`).
