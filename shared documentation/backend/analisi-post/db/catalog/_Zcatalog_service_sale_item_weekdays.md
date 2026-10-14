# ~~Tabella `catalog_service_sale_item_weekdays`~~ — ELIMINATA il 2026-09-21

> **Questa tabella non esiste più.** È stata collassata nella colonna array
> [`catalog_service_sale_items.active_weekdays`](catalog_service_sale_items.md)
> ([`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §13), migration `CollapseChildRowsIntoArrays` di `Module.Catalog`.
>
> Sette righe figlie con un flag booleano per esprimere un insieme di al più sette valori, senza FK in
> ingresso e senza audit per riga: il caso da manuale per un array. Il contenuto originale resta qui
> sotto come traccia del disegno precedente — **non è più lo schema corrente**.

---

# Tabella `catalog_service_sale_item_weekdays`

> **Dominio**: C2 — `catalog` · **sottoarea**: `service` · **schema**: `catalog` · **nome flat**:
> `catalog_service_sale_item_weekdays` · **forma canonica**: `catalog.service_sale_item_weekdays`.
>
> Rimpiazza `services_types.hourlyServices[].day_constant` (array di 7 elementi `{id, name, status, it}`,
> uno per giorno). Fonti:
> [`analisi-pre/db/services_types.md` (campo `hourlyServices`)](../../../analisi-pre/db/services_types.md).

---

## Descrizione

Per un servizio a fascia oraria (`kind = hourly`), i **giorni della settimana** in cui è attivo. Nel
legacy un array di 7 elementi con `status` (attivo/no) per giorno. Qui: **una riga per giorno attivo**
(o una riga per ognuno dei 7 con `is_active`, a scelta implementativa — vedi nota).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_service_sale_item_weekdays` | `id` | PK | — |
| `uq_..._item_weekday` | `(sale_item_id, weekday) WHERE deleted_at IS NULL` | UNIQUE parziale | un record per (servizio, giorno) |

## Campi

### id
- **Legacy**: `hourlyServices[].day_constant[].id` (1..7, id locale)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### sale_item_id
- **Legacy**: elemento di `hourlyServices` (contenitore di `day_constant`)
- **Descrizione**: servizio orario a cui si riferisce.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_service_sale_items.id`](catalog_service_sale_items.md)
  `ON DELETE CASCADE`.

### weekday
- **Legacy**: `day_constant[].name` (`"Monday"`…`"Sunday"`) / `day_constant[].it` (`"Lunedì"`…)
- **Descrizione**: giorno della settimana.
- **Tipo**: `enum catalog.weekday` — valori: `monday`, `tuesday`, `wednesday`, `thursday`, `friday`,
  `saturday`, `sunday`. (In alternativa `smallint` ISO 1–7.)
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: l'etichetta tradotta (`it`) va nelle risorse di localizzazione, non a colonna.

### is_active
- **Legacy**: `day_constant[].status` (int 0/1)
- **Descrizione**: il servizio è disponibile in quel giorno.
- **Tipo**: `boolean`.
- **Default**: `true`.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: implementazione alternativa: **non** creare righe per i giorni inattivi (l'assenza di riga =
  non attivo). Consigliata la variante con riga sempre presente + `is_active` per semplificare l'editor.

### created_at / updated_at
- **Legacy**: della riga contenitore
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: — (modifica = riscrittura array)
- **Descrizione**: soft-delete.
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.

## Valutazione JSON

Nessun JSON — elimina `day_constant[]`.

## Suggerimenti per le prestazioni

- Al più 7 righe per servizio orario. Verifica disponibilità: `WHERE sale_item_id = ? AND weekday = ?
  AND is_active`.

## Implementazione .NET

`ServiceSaleItemWeekday` è **entità figlia** di [`ServiceSaleItem`](catalog_service_sale_items.md)
(modulo `Module.Catalog`, area `Service`, schema `catalog`): a differenza della sorella
`ServiceSaleItemTimeSlot` non ha `public_id` (nessun modulo esterno la referenzia), ma resta un'entità
con propria PK (non owned puro) perché soggetta a `UNIQUE(sale_item_id, weekday)` e a operazioni di
editing indipendenti dal padre. Implementa `IAuditable`/`ISoftDeletable`.

### Enum proprio

```csharp
// Domain/Enums/Weekday.cs
public enum Weekday
{
    Monday = 0,
    Tuesday = 1,
    Wednesday = 2,
    Thursday = 3,
    Friday = 4,
    Saturday = 5,
    Sunday = 6
}
```

```csharp
// CatalogDbContext.OnModelCreating
modelBuilder.HasPostgresEnum<Weekday>("catalog", "weekday");
```

```csharp
// Registrazione data source
dataSourceBuilder.MapEnum<Weekday>("catalog.weekday");
```

### Entità

```csharp
// Domain/Entities/ServiceSaleItemWeekday.cs
namespace Module.Catalog.Domain.Entities;

public sealed class ServiceSaleItemWeekday : IAuditable, ISoftDeletable
{
    public long Id { get; private set; }
    public long SaleItemId { get; private set; }
    public Weekday Weekday { get; private set; }
    public bool IsActive { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<ServiceSaleItemWeekday>`

```csharp
// Infrastructure/Configurations/ServiceSaleItemWeekdayConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class ServiceSaleItemWeekdayConfiguration : IEntityTypeConfiguration<ServiceSaleItemWeekday>
{
    public void Configure(EntityTypeBuilder<ServiceSaleItemWeekday> builder)
    {
        builder.ToTable("service_sale_item_weekdays", "catalog", t => t.HasComment(
            "Giorno della settimana in cui un servizio a fascia oraria (kind = hourly) è attivo."));

        builder.HasKey(x => x.Id).HasName("pk_catalog_service_sale_item_weekdays");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.SaleItemId)
            .HasColumnName("sale_item_id")
            .IsRequired()
            .HasComment("Servizio a fascia oraria a cui si riferisce.");

        builder.Property(x => x.Weekday)
            .HasColumnName("weekday")
            .HasColumnType("catalog.weekday")
            .IsRequired()
            .HasComment("Giorno della settimana.");

        builder.Property(x => x.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired()
            .HasComment("Il servizio è disponibile in quel giorno.");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione della riga.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica della riga.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

        builder.HasIndex(x => new { x.SaleItemId, x.Weekday })
            .IsUnique()
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("uq_catalog_service_sale_item_weekdays_item_weekday");

        builder.HasOne<ServiceSaleItem>()
            .WithMany(x => x.Weekdays)
            .HasForeignKey(x => x.SaleItemId)
            .HasConstraintName("fk_catalog_service_sale_item_weekdays_item")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Relazioni

`sale_item_id` → FK reale a `catalog_service_sale_items.id` (stesso schema), `ON DELETE CASCADE`.

### Note di migrazione

- Scelta implementativa (come indicato nella scheda): **riga sempre presente per i 7 giorni**, con
  `is_active` per giorno, invece di righe solo per i giorni attivi — semplifica l'editor gestionale.
- `day_constant[].name`/`.it` → `weekday` enum (l'etichetta tradotta va nelle risorse di localizzazione,
  non a colonna); `day_constant[].status` (0/1) → `IsActive` bool; `id` locale (1..7) scartato.
