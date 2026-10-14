# Tabella `catalog_service_type_icons`

> **Dominio**: C2 — `catalog` · **sottoarea**: `service` · **schema**: `catalog` · **nome flat**:
> `catalog_service_type_icons` · **forma canonica**: `catalog.service_type_icons`.
>
> Rimpiazza la legacy [`servicetypes_icons`](../../../analisi-pre/db/servicetypes_icons.md) (**61 righe**
> in produzione: `hourly` 25 / `normal` 25 / `bar` 11; globale/master, tutte del 2021). Fonti:
> [`analisi-pre/db/servicetypes_icons.md`](../../../analisi-pre/db/servicetypes_icons.md),
> [`analisi-pre/db/services_types.md`](../../../analisi-pre/db/services_types.md),
> [`docs/03-gestione-dati.md` §1](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

Catalogo **globale/master** (nessun `property_id`) delle **icone** disponibili per i tre tipi di servizio
venduto ([`catalog_service_sale_items`](catalog_service_sale_items.md): `bar` / `normal` / `hourly`). Il
gestore sceglie un'icona da qui quando crea un servizio.

Correzioni rispetto al legacy:

- **`type` (`"bar"`/`"normal"`/`"hourly"`) → `enum`**.
- **`icon` (URL `varchar(1000)`) → FK** verso [`catalog_media_assets`](catalog_media_assets.md).
- **`status` `tinyint` → `is_active boolean`** (`servicetypeicons` filtrava già `status: true`).
- **`category` (`varchar(500)`) → non migrata**: colonna morta al 100% (`NULL`/`''` su tutte le 61
  righe, mai letta da alcun controller).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_service_type_icons` | `id` | PK | — |
| `ix_catalog_service_type_icons_type` | `(type) WHERE is_active AND deleted_at IS NULL` | parziale | selettore icone per tipo di servizio |
| `ix_catalog_service_type_icons_media_asset` | `(icon_asset_id)` | — | GC asset |

## Campi

### id
- **Legacy**: `servicetypes_icons.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `integer`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità (id legacy preservati).
- **Nullabile**: No.
- **Relazioni**: PK; referenziata da [`catalog_service_sale_items.icon_id`](catalog_service_sale_items.md)
  (dove l'icona è scelta da questo catalogo).

### type
- **Legacy**: `servicetypes_icons.type` (`varchar(100)`: `hourly` / `normal` / `bar`)
- **Descrizione**: tipo di servizio a cui l'icona è destinata.
- **Tipo**: `enum catalog.service_sale_kind` — valori: `bar`, `normal`, `hourly`. (Stesso enum usato da
  [`catalog_service_sale_items.kind`](catalog_service_sale_items.md).)
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### name
- **Legacy**: `servicetypes_icons.name`
- **Descrizione**: nome descrittivo dell'icona (etichetta/tooltip nel selettore).
- **Tipo**: `varchar(255)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### icon_asset_id
- **Legacy**: `servicetypes_icons.icon` (URL `varchar(1000)`)
- **Descrizione**: il file icona.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_media_assets.id`](catalog_media_assets.md) (`kind = 'icon'`)
  `ON DELETE RESTRICT`.

### is_active
- **Legacy**: `servicetypes_icons.status` (`tinyint(1)`; `1` su 60/61 righe)
- **Descrizione**: icona selezionabile.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at
- **Legacy**: `servicetypes_icons.createdAt` / `servicetypes_icons.updatedAt`
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
| `servicetypes_icons.category` (`varchar(500)`) | colonna morta al 100% — `NULL`/`''` su tutte le 61 righe, mai letta |

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- ~61 righe → cache applicativa; raggruppamento per `type` in memoria (o indice parziale).

## Implementazione .NET

`ServiceTypeIcon` è un **lookup di piattaforma** (nessun `property_id`), modulo `Module.Catalog`, area
`Service`, schema `catalog`; referenziata da
[`catalog_service_sale_items.icon_id`](catalog_service_sale_items.md). Implementa
`IAuditable`/`ISoftDeletable`. Non definisce un enum proprio: `type` riusa `ServiceSaleKind`, già
definito in [`catalog_service_sale_items`](catalog_service_sale_items.md) — non ridefinito qui.

### Entità

```csharp
// Domain/Entities/ServiceTypeIcon.cs
namespace Module.Catalog.Domain.Entities;

public sealed class ServiceTypeIcon : IAuditable, ISoftDeletable
{
    public int Id { get; private set; }
    public ServiceSaleKind Type { get; private set; }
    public string Name { get; private set; } = null!;
    public long IconAssetId { get; private set; }
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<ServiceTypeIcon>`

```csharp
// Infrastructure/Configurations/ServiceTypeIconConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class ServiceTypeIconConfiguration : IEntityTypeConfiguration<ServiceTypeIcon>
{
    public void Configure(EntityTypeBuilder<ServiceTypeIcon> builder)
    {
        builder.ToTable("service_type_icons", "catalog", t => t.HasComment(
            "Catalogo master delle icone disponibili per i tre tipi di servizio venduto (bar/normale/a ore)."));

        builder.HasKey(x => x.Id).HasName("pk_catalog_service_type_icons");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.Type)
            .HasColumnName("type")
            .HasColumnType("catalog.service_sale_kind")
            .IsRequired()
            .HasComment("Tipo di servizio a cui l'icona è destinata.");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(255)
            .IsRequired()
            .HasComment("Nome descrittivo dell'icona (etichetta/tooltip nel selettore).");

        builder.Property(x => x.IconAssetId)
            .HasColumnName("icon_asset_id")
            .IsRequired()
            .HasComment("Il file icona.");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired()
            .HasComment("Icona selezionabile.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione dell'icona.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica dell'icona.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

        builder.HasIndex(x => x.Type)
            .HasFilter("is_active AND deleted_at IS NULL")
            .HasDatabaseName("ix_catalog_service_type_icons_type");

        builder.HasIndex(x => x.IconAssetId)
            .HasDatabaseName("ix_catalog_service_type_icons_media_asset");

        builder.HasOne<MediaAsset>()
            .WithMany()
            .HasForeignKey(x => x.IconAssetId)
            .HasConstraintName("fk_catalog_service_type_icons_icon_asset")
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### Relazioni

`icon_asset_id` → FK reale a `catalog_media_assets.id` (stesso schema), `ON DELETE RESTRICT`. Referenziata
da `catalog_service_sale_items.icon_id` (FK definita in quella scheda, `ON DELETE SET NULL`).

### Note di migrazione

- Seed delle 61 righe (`id` preservati): `hourly` 25 / `normal` 25 / `bar` 11.
- `type` (`varchar`) → enum `ServiceSaleKind` (condiviso con `catalog_service_sale_items.kind`).
- `icon` (URL `varchar(1000)`) → risolto in `icon_asset_id` verso `catalog_media_assets`.
- `status` (`tinyint`) → `is_active` bool.
- `category` (`varchar(500)`) **scartata**: colonna morta al 100% nel legacy (`NULL`/`''` su tutte le 61
  righe, mai letta da alcun controller).
