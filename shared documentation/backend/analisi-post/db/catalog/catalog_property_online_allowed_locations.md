# Tabella `catalog_property_online_allowed_locations`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (radice dominio) · **schema**: `catalog` · **nome flat**:
> `catalog_property_online_allowed_locations` · **forma canonica**:
> `catalog.property_online_allowed_locations`.
>
> Tabella figlia di
> [`catalog_property_online_booking_settings`](catalog_property_online_booking_settings.md). Sostituisce
> l'array JSON `properties.onlineBookingOptions.locations` (e `online_reservations.locations`). Fonti:
> [`analisi-pre/db/properties.md`](../../../analisi-pre/db/properties.md),
> [`analisi-pre/db/online_reservations.md`](../../../analisi-pre/db/online_reservations.md).

---

## Descrizione

Elenco delle **postazioni** prenotabili online quando
[`catalog_property_online_booking_settings.is_location_restricted`](catalog_property_online_booking_settings.md)
è `true`. Una riga = una postazione ammessa. Nel legacy era un array JSON con tipi misti
(`["1","5"]`, `[3,4,5]`, `["U4"]`, con duplicati) e nessuna integrità referenziale.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_property_online_allowed_locations` | `id` | PK | — |
| `uq_..._settings_spot` | `(settings_id, spot_id) WHERE deleted_at IS NULL` | UNIQUE parziale | niente duplicati (li aveva il legacy) |
| `ix_..._settings` | `(settings_id) WHERE deleted_at IS NULL` | parziale | caricamento elenco |
| `ix_..._spot` | `(spot_id)` | — | "questa postazione è online-bookable?" |

## Campi

### id
- **Legacy**: — (nuovo; il legacy identificava per posizione nell'array)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### settings_id
- **Legacy**: `online_reservations.id` / contenitore di `onlineBookingOptions.locations`
- **Descrizione**: configurazione canale online a cui appartiene la restrizione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_property_online_booking_settings.id`](catalog_property_online_booking_settings.md)
  `ON DELETE CASCADE`.

### spot_id
- **Legacy**: elemento dell'array `onlineBookingOptions.locations` (etichetta stringa o intero)
- **Descrizione**: la postazione ammessa alla prenotazione online.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_map_spots.id`](catalog_map_spots.md) `ON DELETE CASCADE` (stesso
  schema, FK reale).
- **Note**: in migrazione le etichette legacy (`"A1"`, `"U4"`, `12`) si risolvono contro
  `catalog_map_spots.label` della stessa property; valori non risolvibili (postazioni non più esistenti)
  → riga scartata con log.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (nel legacy la rimozione era la riscrittura dell'array)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON — è la tabella che *elimina* l'array `locations`. Il tipo misto stringa/intero del legacy si
risolve con una FK reale verso `catalog_map_spots`.

## Suggerimenti per le prestazioni

- Elenco tipicamente ≤ decine di righe per property; una `SELECT ... WHERE settings_id = ?` a inizio
  `quote`.
- L'`UNIQUE` parziale rende idempotente l'aggiunta ripetuta della stessa postazione.

## Implementazione .NET

`OnlineAllowedLocation` è **entità figlia** di
[`OnlineBookingSettings`](catalog_property_online_booking_settings.md) (modulo `Module.Catalog`, radice
del dominio, schema `catalog`): una riga = una postazione ammessa alla prenotazione online. Implementa
`IAuditable`/`ISoftDeletable`.

### Entità

```csharp
// Domain/Entities/OnlineAllowedLocation.cs
namespace Module.Catalog.Domain.Entities;

public sealed class OnlineAllowedLocation : IAuditable, ISoftDeletable
{
    public long Id { get; private set; }
    public long SettingsId { get; private set; }
    public long SpotId { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<OnlineAllowedLocation>`

```csharp
// Infrastructure/Configurations/OnlineAllowedLocationConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class OnlineAllowedLocationConfiguration : IEntityTypeConfiguration<OnlineAllowedLocation>
{
    public void Configure(EntityTypeBuilder<OnlineAllowedLocation> builder)
    {
        builder.ToTable("property_online_allowed_locations", "catalog", t => t.HasComment(
            "Postazione ammessa alla prenotazione online quando is_location_restricted è true."));

        builder.HasKey(x => x.Id).HasName("pk_catalog_property_online_allowed_locations");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.SettingsId)
            .HasColumnName("settings_id")
            .IsRequired()
            .HasComment("Configurazione canale online a cui appartiene la restrizione.");

        builder.Property(x => x.SpotId)
            .HasColumnName("spot_id")
            .IsRequired()
            .HasComment("La postazione ammessa alla prenotazione online.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione della riga.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica della riga.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

        builder.HasIndex(x => new { x.SettingsId, x.SpotId })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_catalog_property_online_allowed_locations_settings_spot");

        builder.HasIndex(x => x.SettingsId)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_catalog_property_online_allowed_locations_settings");

        builder.HasIndex(x => x.SpotId)
            .HasDatabaseName("ix_catalog_property_online_allowed_locations_spot");

        builder.HasOne<OnlineBookingSettings>()
            .WithMany(x => x.AllowedLocations)
            .HasForeignKey(x => x.SettingsId)
            .HasConstraintName("fk_catalog_property_online_allowed_locations_settings")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<MapSpot>()
            .WithMany()
            .HasForeignKey(x => x.SpotId)
            .HasConstraintName("fk_catalog_property_online_allowed_locations_spot")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Relazioni

- `settings_id` → FK reale a `catalog_property_online_booking_settings.id` (stesso schema), `ON DELETE
  CASCADE`.
- `spot_id` → FK reale a `catalog_map_spots.id` (stesso schema), `ON DELETE CASCADE` (una postazione
  cancellata esce automaticamente dall'elenco delle postazioni online-bookable).

### Note di migrazione

- Elimina l'array JSON `onlineBookingOptions.locations` (tipi misti stringa/intero, con duplicati).
- Le etichette legacy (`"A1"`, `"U4"`, `12`) si risolvono contro `catalog_map_spots.label` della stessa
  property; valori non risolvibili (postazioni non più esistenti) → riga scartata con log.
- `UNIQUE(settings_id, spot_id)` deduplica gli ingressi ripetuti già presenti nell'array legacy.
