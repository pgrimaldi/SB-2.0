# Tabella `catalog_pricing_discount_sectors`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_discount_sectors` · **forma canonica**: `catalog.pricing_discount_sectors`.
>
> Rimpiazza `discounts.items[].selectedRow` (chiamato `selectedRowsData` nel form) — array di **nomi
> settore** a cui un voucher è limitato. Fonti:
> [`analisi-pre/db/discounts.md` (campo `items` → `selectedRow`)](../../../analisi-pre/db/discounts.md),
> [`analisi-pre/db/sectors_rows.md`](../../../analisi-pre/db/sectors_rows.md).

---

## Descrizione

I **settori** a cui un voucher è ristretto (se ne esistono righe; se non ce ne sono, il voucher non ha
restrizione di settore). Una riga per (voucher × settore). Nel legacy il riferimento era **per stringa**
al nome del settore — qui FK verso [`catalog_map_sectors`](catalog_map_sectors.md).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_discount_sectors` | `id` | PK | — |
| `uq_..._discount_sector` | `(discount_id, sector_id) WHERE deleted_at IS NULL` | UNIQUE parziale | niente duplicati |
| `ix_..._sector` | `(sector_id)` | — | "voucher che si applicano al settore X" |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### discount_id
- **Legacy**: elemento di `discounts.items` (contenitore di `selectedRow`)
- **Descrizione**: voucher a cui si applica la restrizione.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_discounts.id`](catalog_pricing_discounts.md) `ON DELETE CASCADE`.

### sector_id
- **Legacy**: `discounts.items[].selectedRow[]` (nome testuale del settore)
- **Descrizione**: settore a cui il voucher è limitato.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_map_sectors.id`](catalog_map_sectors.md) `ON DELETE CASCADE`.
- **Note**: in migrazione `name` → `sector_id` per property; nomi non risolvibili → riga scartata con log.

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

Nessun JSON — elimina `selectedRow[]` e il riferimento per stringa.

## Suggerimenti per le prestazioni

- Pochissime righe per voucher. Verifica: `NOT EXISTS (... discount_id = ?)` ⇒ nessuna restrizione;
  altrimenti la postazione deve appartenere a un settore presente qui.

## Implementazione .NET

Entità figlia semplice (non owned type: ha PK propria ed è referenziata solo tramite le due FK), collegata
all'aggregate root `Discount`. Modulo `Module.Catalog`, schema `catalog`. Nessun enum proprio.

```csharp
// Module.Catalog.Domain.Entities/DiscountSector.cs
namespace Module.Catalog.Domain.Entities;

public sealed class DiscountSector : IAuditable, ISoftDeletable
{
	public long Id { get; private set; }
	public long DiscountId { get; private set; }
	public Discount Discount { get; private set; } = null!;
	public long SectorId { get; private set; }
	public MapSector Sector { get; private set; } = null!;

	public DateTime CreatedAt { get; private set; }
	public DateTime UpdatedAt { get; private set; }
	public DateTime? DeletedAt { get; private set; }

	public void Delete(DateTime utcNow) => DeletedAt = utcNow;
	public void Restore() => DeletedAt = null;
}
```

```csharp
// Module.Catalog.Infrastructure.Configurations/DiscountSectorConfiguration.cs
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class DiscountSectorConfiguration : IEntityTypeConfiguration<DiscountSector>
{
	public void Configure(EntityTypeBuilder<DiscountSector> builder)
	{
		builder.ToTable("catalog_pricing_discount_sectors", "catalog", t => t.HasComment(
			"Settori a cui un voucher (discount) è ristretto; assenza di righe = nessuna restrizione."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_discount_sectors");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.DiscountId)
			.HasColumnName("discount_id")
			.IsRequired()
			.HasComment("Voucher a cui si applica la restrizione di settore.");

		builder.Property(x => x.SectorId)
			.HasColumnName("sector_id")
			.IsRequired()
			.HasComment("Settore a cui il voucher è limitato.");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di creazione della riga.");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at")
			.HasDefaultValueSql("now()").HasComment("Data/ora di ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at")
			.HasComment("Soft-delete: valorizzata quando la restrizione viene rimossa.");

		builder.HasOne(x => x.Discount)
			.WithMany(d => d.RestrictedSectors)
			.HasForeignKey(x => x.DiscountId)
			.HasConstraintName("fk_catalog_pricing_discount_sectors_discount")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Sector)
			.WithMany()
			.HasForeignKey(x => x.SectorId)
			.HasConstraintName("fk_catalog_pricing_discount_sectors_sector")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasIndex(x => new { x.DiscountId, x.SectorId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_pricing_discount_sectors_discount_sector");

		builder.HasIndex(x => x.SectorId)
			.HasDatabaseName("ix_catalog_pricing_discount_sectors_sector");
	}
}
```

**Note di migrazione**: per ogni elemento di `discounts.items[].selectedRow[]` (nome testuale del settore),
risolvere il nome contro `catalog_map_sectors` della property del voucher e inserire una riga
`(discount_id, sector_id)`; nomi non risolvibili → riga scartata con log. `Discount.RestrictedSectors` è
esposta come collezione di sola lettura sull'aggregate; l'assenza di righe per un `discount_id` significa
"nessuna restrizione di settore" (va interpretato così anche in application layer, non solo in UI).
