# Tabella `catalog_pricing_revenue_discounted_spots`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_revenue_discounted_spots` · **forma canonica**:
> `catalog.pricing_revenue_discounted_spots`.
>
> Rimpiazza l'array JSON `revenue_notifications.enableDiscountsForSelectedLocations`
> (`["R12C4","R12C3",…]` — codici cella mappa). Fonti:
> [`analisi-pre/db/revenue_notifications.md`](../../../analisi-pre/db/revenue_notifications.md).

---

## Descrizione

Le **postazioni** a cui si applica lo sconto Revenue Management quando
[`catalog_pricing_revenue_settings.is_discount_limited_to_spots`](catalog_pricing_revenue_settings.md) è
`true`. Una riga per (configurazione × postazione). Nel legacy erano codici cella (`R{riga}C{colonna}`)
in un array JSON, applicati lato client — qui FK verso [`catalog_map_spots`](catalog_map_spots.md).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_revenue_discounted_spots` | `id` | PK | — |
| `uq_..._settings_spot` | `(settings_id, spot_id) WHERE deleted_at IS NULL` | UNIQUE parziale | niente duplicati |
| `ix_..._spot` | `(spot_id)` | — | "questa postazione è scontata?" |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### settings_id
- **Legacy**: `revenue_notifications.id` (contenitore dell'array)
- **Descrizione**: configurazione Revenue a cui appartiene la restrizione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_revenue_settings.id`](catalog_pricing_revenue_settings.md)
  `ON DELETE CASCADE`.

### spot_id
- **Legacy**: `enableDiscountsForSelectedLocations[]` (codice cella `"R{riga}C{colonna}"`)
- **Descrizione**: postazione a cui si applica lo sconto.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_map_spots.id`](catalog_map_spots.md) `ON DELETE CASCADE`.
- **Note**: in migrazione il codice cella si risolve contro `catalog_map_spots` (posizione griglia /
  label) della property; non risolvibili → riga scartata con log.

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

Nessun JSON — elimina `enableDiscountsForSelectedLocations[]`.

## Suggerimenti per le prestazioni

- Pochissime righe. Verifica per postazione: `EXISTS (... WHERE settings_id = ? AND spot_id = ?)`.

## Implementazione .NET

Entità figlia semplice (PK propria, non owned type) dell'aggregate root
[`RevenueSettings`](catalog_pricing_revenue_settings.md). Modulo `Module.Catalog`, schema `catalog`.
Nessun enum proprio.

```csharp
// Module.Catalog.Domain.Entities/RevenueDiscountedSpot.cs
namespace Module.Catalog.Domain.Entities;

public sealed class RevenueDiscountedSpot : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long SettingsId { get; private set; }
	public RevenueSettings Settings { get; private set; } = null!;
	public long SpotId { get; private set; }
	public MapSpot Spot { get; private set; } = null!;

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public void Delete(DateTime utcNow) => DeletedAt = utcNow;
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog.Infrastructure.Configurations/RevenueDiscountedSpotConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class RevenueDiscountedSpotConfiguration : IEntityTypeConfiguration<RevenueDiscountedSpot>
{
	public void Configure(EntityTypeBuilder<RevenueDiscountedSpot> builder)
	{
		builder.ToTable("catalog_pricing_revenue_discounted_spots", "catalog", t => t.HasComment(
			"Postazioni a cui si applica lo sconto Revenue Management quando la configurazione è limitata a postazioni specifiche."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_revenue_discounted_spots");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.SettingsId)
			.HasColumnName("settings_id")
			.IsRequired()
			.HasComment("Configurazione Revenue Management a cui appartiene la restrizione.");

		builder.Property(x => x.SpotId)
			.HasColumnName("spot_id")
			.IsRequired()
			.HasComment("Postazione a cui si applica lo sconto.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: valorizzata quando la restrizione viene rimossa.");

		builder.HasOne(x => x.Settings)
			.WithMany(s => s.DiscountedSpots)
			.HasForeignKey(x => x.SettingsId)
			.HasConstraintName("fk_catalog_pricing_revenue_discounted_spots_settings")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Spot)
			.WithMany()
			.HasForeignKey(x => x.SpotId)
			.HasConstraintName("fk_catalog_pricing_revenue_discounted_spots_spot")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.SettingsId, x.SpotId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_revenue_discounted_spots_settings_spot");

		builder.HasIndex(x => x.SpotId)
			.HasDatabaseName("ix_catalog_pricing_revenue_discounted_spots_spot");
	}
}
```

**Note di migrazione**: per ogni codice cella in `enableDiscountsForSelectedLocations[]`
(`"R{riga}C{colonna}"`), risolvere contro `catalog_map_spots` della property (via posizione griglia) e
inserire una riga `(settings_id, spot_id)`; codici non risolvibili → riga scartata con log. La FK su
`Settings` chiude la relazione già dichiarata come `HasMany(x => x.DiscountedSpots)` in
`RevenueSettingsConfiguration`.
