# Tabella `catalog_service_amenities`

> **Dominio**: C2 — `catalog` · **sottoarea**: `service` · **schema**: `catalog` · **nome flat**:
> `catalog_service_amenities` · **forma canonica**: `catalog.service_amenities`.
>
> Rimpiazza la legacy [`services`](../../../analisi-pre/db/services.md) (**48 righe**, globale/master,
> `COMMENT='Bathouse Services Master'`, tutte del 2021, `status` sempre `"active"`). Fonti:
> [`analisi-pre/db/services.md`](../../../analisi-pre/db/services.md),
> [`analisi-pre/db/bathouses.md` (campo `services`)](../../../analisi-pre/db/bathouses.md),
> [`analisi-pre/business-logic/systemadmin/03-dati-di-piattaforma-e-lookup.md`](../../../analisi-pre/business-logic/systemadmin/03-dati-di-piattaforma-e-lookup.md),
> [`docs/03-gestione-dati.md` §1](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

Catalogo **globale/master** (nessun `property_id`) delle **dotazioni/servizi** che uno stabilimento può
dichiarare di offrire (es. "Wi-Fi", "Doccia", "Bar", "Surf"). Le vetrine collegano le dotazioni offerte
via [`catalog_bathhouse_amenities`](catalog_bathhouse_amenities.md) (giunzione N:N — nel legacy era una
copia integrale JSON). Ogni dotazione appartiene a **una** categoria
([`catalog_service_amenity_categories`](catalog_service_amenity_categories.md)) usata nella ricerca pubblica.

**Nota di nomenclatura**: rinominata `amenities` (non `services`) per non collidere con
[`catalog_service_sale_items`](catalog_service_sale_items.md) (i servizi bar/normali/a ore *venduti*):
sono concetti diversi che nel legacy avevano nomi confondibili (`services` vs `services_types`).

Correzioni rispetto al legacy:

- **`serviceIcon` (URL stringa) → FK** verso [`catalog_media_assets`](catalog_media_assets.md).
- **`serviceFilterType` (int senza FK, valore orfano `0`) → FK reale** verso
  `catalog_service_amenity_categories`.
- **`status` `varchar` sempre `"active"` → `is_active boolean`**.
- **`isChecked`** (sempre `0` nel master; serviva solo alla copia per-stabilimento) → **non migrato**
  (lo stato "offerto da questa vetrina" è l'esistenza della riga in `catalog_bathhouse_amenities`).
- endpoint `GET services` non filtrava per `status` → nel target il catalogo pubblico restituisce solo
  `is_active`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_service_amenities` | `id` | PK | — |
| `ix_catalog_service_amenities_category` | `(category_id) WHERE is_active AND deleted_at IS NULL` | parziale | raggruppamento per categoria nella ricerca pubblica |
| `ix_catalog_service_amenities_media_asset` | `(icon_asset_id)` | — | GC asset |

## Campi

### id
- **Legacy**: `services.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `integer`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità (id legacy preservati).
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da [`catalog_bathhouse_amenities.amenity_id`](catalog_bathhouse_amenities.md),
  [`catalog_pricing_price_list_items.included_service_id`](catalog_pricing_price_list_items.md),
  [`catalog_service_amenity_translations.amenity_id`](catalog_service_amenity_translations.md).

### name
- **Legacy**: `services.serviceName` (nome nella lingua di default)
- **Descrizione**: nome della dotazione nella lingua di default.
- **Tipo**: `varchar(120)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: **master di piattaforma visto dal consumer** (vetrina pubblica + filtri di ricerca) → le
  traduzioni vivono in [`catalog_service_amenity_translations`](catalog_service_amenity_translations.md)
  (N lingue), non in colonne `name_it`/`name_en`. Il legacy aveva `serviceName`/`serviceItalian` (2
  lingue, spesso disallineate — es. `serviceName = "Bar"` / `serviceItalian = "Pacchetti all inclusive"`)
  → in migrazione `serviceName` → `name`, `serviceItalian` → riga `translations` per `it-IT` (con
  bonifica dei casi disallineati).

### category_id
- **Legacy**: `services.serviceFilterType` (int senza FK; valore orfano `0` su 1 riga)
- **Descrizione**: categoria a cui appartiene la dotazione.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì (`0` legacy → `NULL` = "non categorizzata", esclusa dai filtri di ricerca).
- **Relazioni**: **FK** → [`catalog_service_amenity_categories.id`](catalog_service_amenity_categories.md)
  `ON DELETE SET NULL`.

### icon_asset_id
- **Legacy**: `services.serviceIcon` (URL `varchar`)
- **Descrizione**: icona della dotazione, mostrata nel selettore gestionale e nella scheda pubblica.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) (`kind = 'icon'`).

### is_active
- **Legacy**: `services.status` (`varchar(10)`, sempre `"active"`)
- **Descrizione**: dotazione selezionabile / mostrata.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: nel legacy `GET services` non filtrava per `status`; nel target il catalogo restituisce solo
  `is_active = true`.

### sort_order
- **Legacy**: implicito (ordine di `id`)
- **Descrizione**: ordine di visualizzazione dentro la categoria.
- **Tipo**: `smallint`.
- **Default**: `0`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: `services.createdAt` / `services.updatedAt` (identici su tutte le righe — seed unico)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (il legacy non aveva soft-delete; nessun endpoint di scrittura)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Campi legacy **non** migrati

| Campo legacy | Motivo |
|---|---|
| `services.isChecked` | flag di stato della *copia* per-stabilimento — sostituito dall'esistenza della riga in `catalog_bathhouse_amenities` |

## Valutazione JSON

Nessun JSON. Elimina la copia integrale JSON `bathouses.services`: la relazione vetrina↔dotazione è la
giunzione [`catalog_bathhouse_amenities`](catalog_bathhouse_amenities.md).

## Suggerimenti per le prestazioni

- ~48 righe, quasi statiche → cache applicativa.
- Per la ricerca pubblica per servizi, il read model di ricerca porta l'array di `amenity_id` per vetrina
  (vedi [`catalog_bathhouse_amenities`](catalog_bathhouse_amenities.md)).

## Implementazione .NET

`Amenity` è un **lookup di piattaforma** (nessun `property_id`; modulo `Module.Catalog`, area `Service`,
schema `catalog`), aggregate root minimale che possiede la collezione di traduzioni
[`AmenityTranslation`](catalog_service_amenity_translations.md). Implementa `IAuditable`/`ISoftDeletable`.

### Entità

```csharp
// Domain/Entities/Amenity.cs
namespace Module.Catalog.Domain.Entities;

public sealed class Amenity : IAuditable, ISoftDeletable
{
    public int Id { get; private set; }
    public string Name { get; private set; } = null!;
    public short? CategoryId { get; private set; }
    public long? IconAssetId { get; private set; }
    public bool IsActive { get; private set; }
    public short SortOrder { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    private readonly List<AmenityTranslation> _translations = new();
    public IReadOnlyCollection<AmenityTranslation> Translations => _translations.AsReadOnly();

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<Amenity>`

```csharp
// Infrastructure/Configurations/AmenityConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("service_amenities", "catalog", t => t.HasComment(
            "Catalogo master delle dotazioni/servizi che uno stabilimento può dichiarare di offrire (es. Wi-Fi, Doccia, Bar)."));

        builder.HasKey(x => x.Id).HasName("pk_catalog_service_amenities");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired()
            .HasComment("Nome della dotazione nella lingua di default (le traduzioni vivono in service_amenity_translations).");

        builder.Property(x => x.CategoryId)
            .HasColumnName("category_id")
            .HasComment("Categoria a cui appartiene la dotazione; NULL = non categorizzata, esclusa dai filtri di ricerca.");

        builder.Property(x => x.IconAssetId)
            .HasColumnName("icon_asset_id")
            .HasComment("Icona della dotazione, mostrata nel selettore gestionale e nella scheda pubblica.");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired()
            .HasComment("Dotazione selezionabile/mostrata (il catalogo pubblico restituisce solo le attive).");

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue((short)0)
            .IsRequired()
            .HasComment("Ordine di visualizzazione dentro la categoria.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione della dotazione.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica della dotazione.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

        builder.HasIndex(x => x.CategoryId)
            .HasFilter("is_active AND deleted_at IS NULL")
            .HasDatabaseName("ix_catalog_service_amenities_category");

        builder.HasIndex(x => x.IconAssetId)
            .HasDatabaseName("ix_catalog_service_amenities_media_asset");

        builder.HasOne<AmenityCategory>()
            .WithMany()
            .HasForeignKey(x => x.CategoryId)
            .HasConstraintName("fk_catalog_service_amenities_category")
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(x => x.IconAssetId)
            .HasConstraintName("fk_catalog_service_amenities_icon_asset")
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Translations)
            .WithOne()
            .HasForeignKey(x => x.AmenityId)
            .HasConstraintName("fk_catalog_service_amenity_translations_amenity")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Relazioni

- `category_id` → FK reale a `catalog_service_amenity_categories.id` (stesso schema), `ON DELETE SET
  NULL` (coerente con il valore orfano `0` legacy mappato a `NULL`).
- `icon_asset_id` → FK reale a `catalog_media_assets.id` (stesso schema), `ON DELETE RESTRICT`.
- La giunzione N:N verso le vetrine ([`catalog_bathhouse_amenities`](catalog_bathhouse_amenities.md)) e
  il riferimento da `catalog_pricing_price_list_items.included_service_id` vivono nelle rispettive
  schede — qui solo `Translations` è owned dall'aggregate.

### Note di migrazione

- `serviceIcon` (URL) → risolto in `icon_asset_id` verso `catalog_media_assets`.
- `serviceFilterType` (int senza FK) → `category_id`; il valore orfano `0` osservato → `NULL`.
- `serviceName` → `name` (lingua di default) **e** riga `AmenityTranslation` `en-GB`; `serviceItalian` →
  riga `AmenityTranslation` `it-IT`, con bonifica dei casi disallineati (vedi
  [`catalog_service_amenity_translations`](catalog_service_amenity_translations.md)).
- `status` (`varchar` sempre `"active"`) → `is_active` bool; `isChecked` **non migrato** — lo stato
  "offerto da questa vetrina" è l'esistenza della riga in `catalog_bathhouse_amenities`.
- Pannello SuperAdmin di gestione catalogo oggi assente (le 48 righe sono seed di migration): da
  decidere col committente se renderlo gestibile da UI.
