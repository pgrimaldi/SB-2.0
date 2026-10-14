# Tabella `catalog_service_amenity_translations`

> **Dominio**: C2 — `catalog` · **sottoarea**: `service` · **schema**: `catalog` · **nome flat**:
> `catalog_service_amenity_translations` · **forma canonica**: `catalog.service_amenity_translations`.
>
> Tabella figlia di [`catalog_service_amenities`](catalog_service_amenities.md). Sostituisce la coppia
> `services.serviceName` / `services.serviceItalian` con traduzioni **per lingua** (N lingue) — regola
> [`docs/03-gestione-dati.md` §2.2.B](../../../progettazione/03-gestione-dati.md): il catalogo dotazioni è un
> master di piattaforma **visto dal consumer** (vetrina pubblica + filtri di ricerca), quindi le etichette
> vanno in una tabella di traduzioni, non in colonne `name_it`/`name_en`. Fonti:
> [`analisi-pre/db/services.md`](../../../analisi-pre/db/services.md).

---

## Descrizione

Nome di una dotazione ([`catalog_service_amenities`](catalog_service_amenities.md)) in una `culture`. Una
riga per (dotazione × lingua). Seed: 2 righe per dotazione (`en`, `it`) dai due campi legacy.

Nel legacy `serviceName`/`serviceItalian` erano spesso **disallineati semanticamente** (es.
`serviceName = "Bar"` / `serviceItalian = "Pacchetti all inclusive"`) — la migrazione deve bonificare
questi casi.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_service_amenity_translations` | `id` | PK | — |
| `uq_..._amenity_culture` | `(amenity_id, culture) WHERE deleted_at IS NULL` | UNIQUE parziale | una traduzione per (dotazione, lingua) |
| `ix_..._culture` | `(culture) WHERE deleted_at IS NULL` | parziale | caricamento catalogo per una lingua |

## Campi

### id
- **Legacy**: — (nuovo; le 2 lingue erano colonne)
- **Descrizione**: PK tecnica.
- **Tipo**: `integer`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### amenity_id
- **Legacy**: `services.id`
- **Descrizione**: dotazione a cui si riferisce la traduzione.
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_service_amenities.id`](catalog_service_amenities.md) `ON DELETE CASCADE`.

### culture
- **Legacy**: implicita (`serviceName` → `en`, `serviceItalian` → `it`)
- **Descrizione**: codice cultura BCP-47.
- **Tipo**: `varchar(10)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### name
- **Legacy**: `services.serviceName` / `services.serviceItalian`
- **Descrizione**: nome della dotazione nella lingua indicata.
- **Tipo**: `varchar(120)`.
- **Default**: nessuno.
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
- **Legacy**: — (nuovo)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON.

## Suggerimenti per le prestazioni

- ~48 dotazioni × N lingue → cache applicativa per `culture`. Il read model di ricerca porta i nomi
  già risolti nella lingua richiesta.

## Implementazione .NET

`AmenityTranslation` è **entità figlia** (tabella `_translations`, `UNIQUE(amenity_id, culture)`) di
[`Amenity`](catalog_service_amenities.md), modulo `Module.Catalog`, area `Service`, schema `catalog`.
Implementa `IAuditable`/`ISoftDeletable`.

### Entità

```csharp
// Domain/Entities/AmenityTranslation.cs
namespace Module.Catalog.Domain.Entities;

public sealed class AmenityTranslation : IAuditable, ISoftDeletable
{
    public int Id { get; private set; }
    public int AmenityId { get; private set; }
    public string Culture { get; private set; } = null!;
    public string Name { get; private set; } = null!;

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<AmenityTranslation>`

```csharp
// Infrastructure/Configurations/AmenityTranslationConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class AmenityTranslationConfiguration : IEntityTypeConfiguration<AmenityTranslation>
{
    public void Configure(EntityTypeBuilder<AmenityTranslation> builder)
    {
        builder.ToTable("service_amenity_translations", "catalog", t => t.HasComment(
            "Nome di una dotazione (catalog_service_amenities) in una specifica culture."));

        builder.HasKey(x => x.Id).HasName("pk_catalog_service_amenity_translations");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.AmenityId)
            .HasColumnName("amenity_id")
            .IsRequired()
            .HasComment("Dotazione a cui si riferisce la traduzione.");

        builder.Property(x => x.Culture)
            .HasColumnName("culture")
            .HasMaxLength(10)
            .IsRequired()
            .HasComment("Codice cultura BCP-47 (es. it-IT, en-GB).");

        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(120)
            .IsRequired()
            .HasComment("Nome della dotazione nella lingua indicata.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione della traduzione.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica della traduzione.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

        builder.HasIndex(x => new { x.AmenityId, x.Culture })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_catalog_service_amenity_translations_amenity_culture");

        builder.HasIndex(x => x.Culture)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_catalog_service_amenity_translations_culture");

        builder.HasOne<Amenity>()
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.AmenityId)
            .HasConstraintName("fk_catalog_service_amenity_translations_amenity")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Relazioni

`amenity_id` → FK reale a `catalog_service_amenities.id` (stesso schema), `ON DELETE CASCADE`; navigazione
inversa `Amenity.Translations` (vedi [`catalog_service_amenities.md`](catalog_service_amenities.md)).

### Note di migrazione

- Seed di 2 righe per dotazione: `serviceName` → riga `culture = "en-GB"`; `serviceItalian` → riga
  `culture = "it-IT"`.
- I casi in cui `serviceName`/`serviceItalian` erano **semanticamente disallineati** nel legacy (es.
  `serviceName = "Bar"` / `serviceItalian = "Pacchetti all inclusive"`) vanno bonificati manualmente in
  migrazione, non copiati automaticamente.
