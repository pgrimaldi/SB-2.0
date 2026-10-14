# Tabella `catalog_property_online_allowed_periods`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (radice dominio) · **schema**: `catalog` · **nome flat**:
> `catalog_property_online_allowed_periods` · **forma canonica**:
> `catalog.property_online_allowed_periods`.
>
> Tabella figlia di
> [`catalog_property_online_booking_settings`](catalog_property_online_booking_settings.md). Sostituisce
> l'array JSON `properties.onlineBookingOptions.period` (e `online_reservations.period`). Fonti:
> [`analisi-pre/db/properties.md`](../../../analisi-pre/db/properties.md),
> [`analisi-pre/db/online_reservations.md`](../../../analisi-pre/db/online_reservations.md).

---

## Descrizione

Finestre di date entro cui è consentita la prenotazione online quando
[`catalog_property_online_booking_settings.is_period_restricted`](catalog_property_online_booking_settings.md)
è `true`. Una riga = un intervallo `[starts_on, ends_on]`. Nel legacy era un array (a volte un singolo
oggetto) con chiavi incoerenti (`{start,end}` ISO datetime vs `{startDate,endDate}`) più l'artefatto
`key` di `react-date-range`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_property_online_allowed_periods` | `id` | PK | — |
| `ix_..._settings` | `(settings_id) WHERE deleted_at IS NULL` | parziale | caricamento elenco |
| `ix_..._range` | `(settings_id, starts_on, ends_on)` | — | verifica "la data X è in una finestra?" |
| `ex_catalog_property_online_allowed_periods_no_overlap` | `(settings_id, daterange(starts_on, ends_on, '[]')) WHERE deleted_at IS NULL` | GiST (`EXCLUDE`) | nessuna sovrapposizione di finestre per stabilimento (riquadro sotto) |


> ### ✅ Vincolo di non sovrapposizione applicato (2026-09-30)
>
> In vigore sul DB come progettato da [`docs/05-ottimizzazioni-db.md`](../../../progettazione/05-ottimizzazioni-db.md) §8
> (migration `AddSectorNameUniqueAndPeriodExcludes`, oggi consolidata nella `InitialCreate` di
> `Module.Catalog` — [`docs/42-migration.md` §10](../../../progettazione/42-migration.md)), estensione `btree_gist`:
>
> ```sql
> CONSTRAINT ex_catalog_property_online_allowed_periods_no_overlap
>   EXCLUDE USING gist (settings_id WITH =, daterange(starts_on, ends_on, '[]') WITH &&)
>   WHERE (deleted_at IS NULL)
> ```
>
> **Come sono state risolte le 9 coppie che lo violavano** (8 per contenimento, 1 per sovrapposizione
> parziale): le **fonde l'ETL**. Qui non c'è nome né fascia, quindi la bonifica si è rivelata meccanica e
> non di business come temuto il 2026-09-21: due finestre sovrapposte equivalgono alla loro unione,
> perché dicono entrambe «in queste date si può prenotare online».

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### settings_id
- **Legacy**: contenitore di `onlineBookingOptions.period`
- **Descrizione**: configurazione canale online a cui appartiene la finestra.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_property_online_booking_settings.id`](catalog_property_online_booking_settings.md)
  `ON DELETE CASCADE`.

### starts_on
- **Legacy**: `onlineBookingOptions.period[].startDate` (o `.start`)
- **Descrizione**: primo giorno della finestra (incluso).
- **Tipo**: `date` (il legacy salvava datetime ISO ma l'ora non ha significato).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### ends_on
- **Legacy**: `onlineBookingOptions.period[].endDate` (o `.end`)
- **Descrizione**: ultimo giorno della finestra (incluso).
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `CHECK (ends_on >= starts_on)`.

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

Nessun JSON — elimina l'array `period` e la sua incoerenza di forma (oggetto singolo vs array, chiavi
diverse). `key` di `react-date-range` scartato.

## Suggerimenti per le prestazioni

- Poche righe per property. La verifica "la data richiesta cade in una finestra" è un
  `EXISTS (... WHERE settings_id = ? AND ? BETWEEN starts_on AND ends_on)`.
- Considerare il tipo `daterange` PostgreSQL + indice GiST se le finestre diventano molte (oggi non
  necessario).

## Implementazione .NET

`OnlineAllowedPeriod` è **entità figlia** di
[`OnlineBookingSettings`](catalog_property_online_booking_settings.md) (modulo `Module.Catalog`, radice
del dominio, schema `catalog`): una riga = una finestra di date `[starts_on, ends_on]` in cui è consentita
la prenotazione online. Implementa `IAuditable`/`ISoftDeletable`.

### Entità

```csharp
// Domain/Entities/OnlineAllowedPeriod.cs
namespace Module.Catalog.Domain.Entities;

public sealed class OnlineAllowedPeriod : IAuditable, ISoftDeletable
{
    public long Id { get; private set; }
    public long SettingsId { get; private set; }
    public DateOnly StartsOn { get; private set; }
    public DateOnly EndsOn { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? DeletedAt { get; private set; }

    public void Delete(DateTime utcNow) => DeletedAt = utcNow;
    public void Restore() => DeletedAt = null;
}
```

### `IEntityTypeConfiguration<OnlineAllowedPeriod>`

```csharp
// Infrastructure/Configurations/OnlineAllowedPeriodConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class OnlineAllowedPeriodConfiguration : IEntityTypeConfiguration<OnlineAllowedPeriod>
{
    public void Configure(EntityTypeBuilder<OnlineAllowedPeriod> builder)
    {
        builder.ToTable("property_online_allowed_periods", "catalog", t => t.HasComment(
            "Finestra di date in cui è consentita la prenotazione online quando is_period_restricted è true.")
            .HasCheckConstraint("ck_catalog_property_online_allowed_periods_range", "ends_on >= starts_on"));

        builder.HasKey(x => x.Id).HasName("pk_catalog_property_online_allowed_periods");
        builder.Property(x => x.Id).UseIdentityAlwaysColumn();

        builder.Property(x => x.SettingsId)
            .HasColumnName("settings_id")
            .IsRequired()
            .HasComment("Configurazione canale online a cui appartiene la finestra.");

        builder.Property(x => x.StartsOn)
            .HasColumnName("starts_on")
            .HasColumnType("date")
            .IsRequired()
            .HasComment("Primo giorno della finestra (incluso).");

        builder.Property(x => x.EndsOn)
            .HasColumnName("ends_on")
            .HasColumnType("date")
            .IsRequired()
            .HasComment("Ultimo giorno della finestra (incluso).");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Creazione della riga.");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()").IsRequired()
            .HasComment("Ultima modifica della riga.");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete.");

        builder.HasIndex(x => x.SettingsId)
            .HasFilter("deleted_at IS NULL")
            .HasDatabaseName("ix_catalog_property_online_allowed_periods_settings");

        builder.HasIndex(x => new { x.SettingsId, x.StartsOn, x.EndsOn })
            .HasDatabaseName("ix_catalog_property_online_allowed_periods_range");

        builder.HasOne<OnlineBookingSettings>()
            .WithMany(x => x.AllowedPeriods)
            .HasForeignKey(x => x.SettingsId)
            .HasConstraintName("fk_catalog_property_online_allowed_periods_settings")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### Relazioni

`settings_id` → FK reale a `catalog_property_online_booking_settings.id` (stesso schema), `ON DELETE
CASCADE`.

### Note di migrazione

- Normalizzazione della forma legacy incoerente: oggetto singolo → 1 riga; array → N righe.
- Chiavi eterogenee (`{start,end}` vs `{startDate,endDate}`), entrambe datetime ISO → `starts_on`/
  `ends_on` `date` (l'ora non ha significato). L'artefatto `key` di `react-date-range` **scartato**.
