# Tabella `catalog_property_online_stay_limits`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (radice dominio) · **schema**: `catalog` · **nome flat**:
> `catalog_property_online_stay_limits` · **forma canonica**: `catalog.property_online_stay_limits`.
>
> Tabella figlia di
> [`catalog_property_online_booking_settings`](catalog_property_online_booking_settings.md). Sostituisce
> l'array JSON `properties.onlineBookingOptions.locationOptionObj.bookingLimit`. Fonti:
> [`analisi-pre/db/properties.md` (campo `onlineBookingOptions` → `bookingLimit`)](../../../analisi-pre/db/properties.md),
> [`docs/01-criticita-e-indicazioni-backend.md` §11.6](../../../progettazione/01-criticita-e-indicazioni-backend.md).

---

## Descrizione

Limiti **minimo/massimo di giorni consecutivi** prenotabili online, per **settore** e per **finestra di
date**. Una riga = (settore × intervallo × min/max). Nel legacy era `bookingLimit[]`, una tabella
EAV-like dentro il JSON (`{sectorName, fromDate, endDate, index, minvalue, maxValue}`), con `minvalue`/
`maxValue` salvati come **stringhe** e `sectorName` come **riferimento testuale** a un settore. Il limite
era **applicato solo lato UED** (nessun controllo server) — nel target è **enforced server-side** dal
motore di prenotazione C1.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_property_online_stay_limits` | `id` | PK | — |
| `ix_..._settings` | `(settings_id) WHERE deleted_at IS NULL` | parziale | caricamento elenco |
| `ix_..._sector_range` | `(sector_id, starts_on, ends_on)` | — | risoluzione limite per (settore, data) |


> ### ✅ Applicato nel backend (2026-09-21)
>
> Vincolo **`EXCLUDE USING gist`** di non sovrapposizione ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §8), migration
> `AddNoOverlapExcludeConstraints` + estensione `btree_gist`:
>
> ```sql
> EXCLUDE USING gist (settings_id WITH =, sector_id WITH =,
>                     daterange(starts_on, ends_on, '[]') WITH &&)
> WHERE (deleted_at IS NULL)
> ```
>
> Un `CHECK` garantisce solo che la **singola riga** sia coerente (`ends >= starts`); l'`EXCLUDE` è
> l'unico strumento che vincola **due righe fra loro** — ed è quello che serve qui, perché il problema
> non è una riga sbagliata ma una data che cade in due periodi diversi senza che nessuno sappia quale
> vince. Parziale su `deleted_at IS NULL`: una riga soft-eliminata non deve bloccare quella che la
> sostituisce.

## Campi

### id
- **Legacy**: `bookingLimit[].index` (solo posizione UI, non un id di business)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### settings_id
- **Legacy**: contenitore di `onlineBookingOptions.locationOptionObj.bookingLimit`
- **Descrizione**: configurazione canale online a cui appartiene il limite.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_property_online_booking_settings.id`](catalog_property_online_booking_settings.md)
  `ON DELETE CASCADE`.

### sector_id
- **Legacy**: `bookingLimit[].sectorName` (nome testuale del settore)
- **Descrizione**: settore a cui si applica il limite.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_map_sectors.id`](catalog_map_sectors.md) `ON DELETE CASCADE`.
- **Note**: in migrazione il `sectorName` si risolve contro `catalog_map_sectors.name` della property;
  nomi non risolvibili → riga scartata con log.

### starts_on
- **Legacy**: `bookingLimit[].fromDate` (datetime ISO)
- **Descrizione**: primo giorno di validità del limite (incluso).
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### ends_on
- **Legacy**: `bookingLimit[].endDate` (datetime ISO)
- **Descrizione**: ultimo giorno di validità del limite (incluso).
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `CHECK (ends_on >= starts_on)`.

### min_days
- **Legacy**: `bookingLimit[].minvalue` (stringa numerica)
- **Descrizione**: numero minimo di giorni consecutivi prenotabili online per quel settore in quella
  finestra.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` = nessun minimo).
- **Relazioni**: —.

### max_days
- **Legacy**: `bookingLimit[].maxValue` (stringa numerica)
- **Descrizione**: numero massimo di giorni consecutivi prenotabili online per quel settore in quella
  finestra.
- **Tipo**: `smallint`.
- **Default**: nessuno.
- **Nullabile**: Sì (`NULL` = nessun massimo).
- **Relazioni**: —.
- **Note**: `CHECK (max_days IS NULL OR min_days IS NULL OR max_days >= min_days)`.

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

Nessun JSON — elimina `bookingLimit[]` (tabella-nel-JSON). `minvalue`/`maxValue` stringa → `smallint`;
`sectorName` stringa → FK; `index` UI scartato.

## Suggerimenti per le prestazioni

- Poche righe per property. Risoluzione limite = `WHERE settings_id = ? AND sector_id = ? AND ? BETWEEN
  starts_on AND ends_on` al momento del `quote`.
- Enforcement obbligatorio server-side (era il gap del legacy).

## Implementazione .NET

`OnlineStayLimit` è **entità figlia** di
[`OnlineBookingSettings`](catalog_property_online_booking_settings.md) (modulo `Module.Catalog`, radice
del dominio, schema `catalog`): una riga = (settore × finestra di date × min/max giorni consecutivi).
Implementa `IAuditable`/`ISoftDeletable`.

### Entità

```csharp
// Domain/Entities/OnlineStayLimit.cs
namespace Module.Catalog.Domain.Entities;

public sealed class OnlineStayLimit : IAuditable, ISoftDeletable
{
    public long Id { get; private set; }
    public long SettingsId { get; private set; }
    public long SectorId { get; private set; }
    public DateOnly StartsOn { get; private set; }
    public DateOnly EndsOn { get; private set; }
    public short? MinDays { get; private set; }
    public short? MaxDays { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<OnlineStayLimit>`

```csharp
// Infrastructure/Configurations/OnlineStayLimitConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class OnlineStayLimitConfiguration : IEntityTypeConfiguration<OnlineStayLimit>
{
    public void Configure(EntityTypeBuilder<OnlineStayLimit> builder)
    {
        builder.ToTable("property_online_stay_limits", "catalog", t => t
            .HasComment("Limite minimo/massimo di giorni consecutivi prenotabili online, per settore e finestra di date.")
            .HasCheckConstraint("ck_catalog_property_online_stay_limits_range", "ends_on >= starts_on")
            .HasCheckConstraint("ck_catalog_property_online_stay_limits_days", "max_days IS NULL OR min_days IS NULL OR max_days >= min_days"));

        builder.HasKey(x => x.Id).HasName("pk_catalog_property_online_stay_limits");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.SettingsId)
            .HasColumnName("settings_id")
            .IsRequired()
            .HasComment("Configurazione canale online a cui appartiene il limite.");

        builder.Property(x => x.SectorId)
            .HasColumnName("sector_id")
            .IsRequired()
            .HasComment("Settore a cui si applica il limite.");

        builder.Property(x => x.StartsOn)
            .HasColumnName("starts_on")
            .HasColumnType("date")
            .IsRequired()
            .HasComment("Primo giorno di validità del limite (incluso).");

        builder.Property(x => x.EndsOn)
            .HasColumnName("ends_on")
            .HasColumnType("date")
            .IsRequired()
            .HasComment("Ultimo giorno di validità del limite (incluso).");

        builder.Property(x => x.MinDays)
            .HasColumnName("min_days")
            .HasComment("Numero minimo di giorni consecutivi prenotabili online per il settore in quella finestra; NULL = nessun minimo.");

        builder.Property(x => x.MaxDays)
            .HasColumnName("max_days")
            .HasComment("Numero massimo di giorni consecutivi prenotabili online per il settore in quella finestra; NULL = nessun massimo.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione della riga.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica della riga.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

        builder.HasIndex(x => x.SettingsId)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_catalog_property_online_stay_limits_settings");

        builder.HasIndex(x => new { x.SectorId, x.StartsOn, x.EndsOn })
            .HasDatabaseName("ix_catalog_property_online_stay_limits_sector_range");

        builder.HasOne<OnlineBookingSettings>()
            .WithMany(x => x.StayLimits)
            .HasForeignKey(x => x.SettingsId)
            .HasConstraintName("fk_catalog_property_online_stay_limits_settings")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<MapSector>()
            .WithMany()
            .HasForeignKey(x => x.SectorId)
            .HasConstraintName("fk_catalog_property_online_stay_limits_sector")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Relazioni

- `settings_id` → FK reale a `catalog_property_online_booking_settings.id` (stesso schema), `ON DELETE
  CASCADE`.
- `sector_id` → FK reale a `catalog_map_sectors.id` (stesso schema), `ON DELETE CASCADE`.

### Note di migrazione

- Elimina la tabella-nel-JSON `bookingLimit[]` (EAV-like dentro `onlineBookingOptions`).
- `sectorName` (riferimento testuale) risolto contro `catalog_map_sectors.name` della stessa property →
  `sector_id`; nomi non risolvibili → riga scartata con log.
- `minvalue`/`maxValue` (stringhe numeriche) → `smallint`; `fromDate`/`endDate` (datetime ISO) →
  `starts_on`/`ends_on` `date`; `index` (posizione UI) scartato.
- Enforcement obbligatorio **server-side** nel motore `quote`/`confirm` (C1) — nel legacy era applicato
  solo lato frontend.
