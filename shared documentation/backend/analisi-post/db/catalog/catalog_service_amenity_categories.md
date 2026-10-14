# Tabella `catalog_service_amenity_categories`

> **Dominio**: C2 — `catalog` · **sottoarea**: `service` · **schema**: `catalog` · **nome flat**:
> `catalog_service_amenity_categories` · **forma canonica**: `catalog.service_amenity_categories`.
>
> Rimpiazza la legacy [`service_type_filters`](../../../analisi-pre/db/service_type_filters.md) (**6 righe**
> reali + 1 di test, globale/master, tutte del 2021). Fonti:
> [`analisi-pre/db/service_type_filters.md`](../../../analisi-pre/db/service_type_filters.md),
> [`analisi-pre/db/services.md`](../../../analisi-pre/db/services.md),
> [`analisi-pre/business-logic/systemadmin/03-dati-di-piattaforma-e-lookup.md`](../../../analisi-pre/business-logic/systemadmin/03-dati-di-piattaforma-e-lookup.md).

---

## Descrizione

Lookup **globale/di piattaforma** (nessun `property_id`): le categorie in cui sono raggruppate le
dotazioni ([`catalog_service_amenities`](catalog_service_amenities.md)) nella ricerca pubblica. Seed di 6 righe: `Popular filters`, `Accessibility`, `Restaurant` (refuso legacy `"Restuarant"`
**corretto**), `Sports & Activities`, `Wellness & Personal Services`, `Beach Type`.

Nel legacy `services.serviceFilterType` referenziava questa tabella **per `id`** ma **senza FK** (con un
valore orfano `0` osservato) e senza colonna di traduzione. Qui: FK reale + etichette da risorse di
localizzazione.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_service_amenity_categories` | `id` | PK | — |
| `uq_catalog_service_amenity_categories_code` | `(code)` | UNIQUE | riferimento stabile in codice/seed |

## Campi

### id
- **Legacy**: `service_type_filters.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `smallint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità (id legacy 1–6 preservati; la riga di test `id=7` scartata).
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da [`catalog_service_amenities.category_id`](catalog_service_amenities.md).

### code
- **Legacy**: `service_type_filters.serviceTypes` (nome inglese, confrontato per stringa esatta nel frontend, refuso `"Restuarant"` compreso)
- **Descrizione**: codice stabile della categoria.
- **Tipo**: `varchar(48)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `UNIQUE`. Slug canonico (es. `popular`, `accessibility`, `restaurant`, `sports_activities`,
  `wellness_personal_services`, `beach_type`) — **non** la stringa mostrata (che va nelle risorse). In
  migrazione `"Restuarant"` → `restaurant` (refuso corretto una volta, non più hardcoded nel frontend).

### is_active
- **Legacy**: — (il legacy non aveva un flag di stato; le 6 categorie erano sempre presenti)
- **Descrizione**: categoria visibile nella ricerca pubblica.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### sort_order
- **Legacy**: implicito (ordine di `id`)
- **Descrizione**: ordine di visualizzazione nella ricerca pubblica.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: `service_type_filters.createdAt` / `service_type_filters.updatedAt`
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

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- 6 righe, quasi immutabili → cache applicativa (seed in migration).
- Sostituire il match per stringa hardcoded del frontend con l'uso di `code`.

## Implementazione .NET

`AmenityCategory` è un **lookup di piattaforma** (pattern B del template — vocabolario che potrebbe
crescere da pannello, non `ENUM` PG: 6 righe seed ma senza garanzia di stabilità pari a un vero enum di
dominio), modulo `Module.Catalog`, area `Service`, schema `catalog`. Nessun figlio proprio; è referenziata
da [`catalog_service_amenities.category_id`](catalog_service_amenities.md). Implementa
`IAuditable`/`ISoftDeletable`.

### Entità

```csharp
// Domain/Entities/AmenityCategory.cs
namespace Module.Catalog.Domain.Entities;

public sealed class AmenityCategory : IAuditable, ISoftDeletable
{
    public short Id { get; private set; }
    public string Code { get; private set; } = null!;
    public bool IsActive { get; private set; }
    public short SortOrder { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<AmenityCategory>`

```csharp
// Infrastructure/Configurations/AmenityCategoryConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class AmenityCategoryConfiguration : IEntityTypeConfiguration<AmenityCategory>
{
    public void Configure(EntityTypeBuilder<AmenityCategory> builder)
    {
        builder.ToTable("service_amenity_categories", "catalog", t => t.HasComment(
            "Categoria di raggruppamento delle dotazioni (catalog_service_amenities) nella ricerca pubblica."));

        builder.HasKey(x => x.Id).HasName("pk_catalog_service_amenity_categories");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.Code)
            .HasColumnName("code")
            .HasMaxLength(48)
            .IsRequired()
            .HasComment("Codice stabile della categoria (slug: popular, accessibility, restaurant, ...); l'etichetta mostrata sta nelle risorse di localizzazione.");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired()
            .HasComment("Categoria visibile nella ricerca pubblica.");

        builder.Property(x => x.SortOrder)
            .HasColumnName("sort_order")
            .IsRequired()
            .HasComment("Ordine di visualizzazione nella ricerca pubblica.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione della categoria.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica della categoria.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("uq_catalog_service_amenity_categories_code");
    }
}
```

### Relazioni

Nessuna FK in uscita. Referenziata da `catalog_service_amenities.category_id` (FK definita in quella
scheda, `ON DELETE SET NULL`).

### Note di migrazione

- Seed delle 6 righe reali (`id` 1–6 preservati); la riga di test `id = 7` **scartata**.
- `serviceTypes` (nome inglese confrontato per stringa esatta nel frontend) → `code` slug canonico; il
  refuso legacy `"Restuarant"` corretto una volta per tutte in `restaurant`.
- Nessun flag di stato nel legacy → `is_active` valorizzato `true` per le 6 righe migrate.
- Risorse `AmenityCategories.{culture}.resx` (chiave = `code`) da creare per le etichette — la traduzione
  italiana **mancava del tutto** nel legacy.
