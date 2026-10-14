# Tabella `catalog_pricing_discount_date_ranges`

> **Dominio**: C2 — `catalog` · **sottoarea**: `pricing` · **schema**: `catalog` · **nome flat**:
> `catalog_pricing_discount_date_ranges` · **forma canonica**: `catalog.pricing_discount_date_ranges`.
>
> Rimpiazza `discounts.items[].voucherDateRange` (`[{startDate, endDate}]`). Fonti:
> [`analisi-pre/db/discounts.md` (campo `items` → `voucherDateRange`)](../../../analisi-pre/db/discounts.md).

---

## Descrizione

Le **finestre di validità** di un voucher. Un voucher può avere più intervalli (come i periodi
stagionali). Una riga per (voucher × intervallo).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_pricing_discount_date_ranges` | `id` | PK | — |
| `ix_..._discount_range` | `(discount_id, starts_on, ends_on) WHERE deleted_at IS NULL` | parziale | "il voucher V è valido il giorno D?" |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### discount_id
- **Legacy**: elemento di `discounts.items` (contenitore di `voucherDateRange`)
- **Descrizione**: voucher a cui appartiene la finestra.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_pricing_discounts.id`](catalog_pricing_discounts.md) `ON DELETE CASCADE`.

### starts_on / ends_on
- **Legacy**: `voucherDateRange[].startDate` / `.endDate`
- **Descrizione**: intervallo di date (incluso) in cui il voucher è riscattabile.
- **Tipo**: `date` ciascuno.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.
- **Note**: `CHECK (ends_on >= starts_on)`.

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

Nessun JSON — elimina `voucherDateRange[]`.

## Suggerimenti per le prestazioni

- Poche righe per voucher. Validazione = `EXISTS (... WHERE discount_id = ? AND ? BETWEEN starts_on AND ends_on)`.

## Implementazione .NET

**Entità figlia** dell'aggregate `Discount` (modulo `Module.Catalog`, schema `catalog`), esposta come
collezione `Discount.ValidityWindows`. Nessun enum proprio.

### `DiscountDateRangeConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class DiscountDateRangeConfiguration : IEntityTypeConfiguration<DiscountDateRange>
{
	public void Configure(EntityTypeBuilder<DiscountDateRange> builder)
	{
		builder.ToTable("pricing_discount_date_ranges", "catalog", t =>
		{
			t.HasComment("Finestra di validità (una fra eventualmente più) di un voucher sconto.");
			t.HasCheckConstraint("ck_catalog_pricing_discount_date_ranges_range", "ends_on >= starts_on");
		});

		builder.HasKey(x => x.Id).HasName("pk_catalog_pricing_discount_date_ranges");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.DiscountId)
			.HasColumnName("discount_id")
			.IsRequired()
			.HasComment("Voucher a cui appartiene la finestra di validità.");
		builder.HasOne<Discount>()
			.WithMany(d => d.ValidityWindows)
			.HasForeignKey(x => x.DiscountId)
			.HasConstraintName("fk_catalog_pricing_discount_date_ranges_discount")
			.OnDelete(DeleteBehavior.Cascade);

		builder.Property(x => x.StartsOn)
			.HasColumnName("starts_on")
			.IsRequired()
			.HasComment("Inizio dell'intervallo in cui il voucher è riscattabile.");

		builder.Property(x => x.EndsOn)
			.HasColumnName("ends_on")
			.IsRequired()
			.HasComment("Fine dell'intervallo in cui il voucher è riscattabile.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete della finestra di validità.");

		builder.HasIndex(x => new { x.DiscountId, x.StartsOn, x.EndsOn })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_pricing_discount_date_ranges_discount_range");
	}
}
```

`DiscountDateRange` implementa `ISoftDeletable` + `IAuditable`. `StartsOn`/`EndsOn` sono `DateOnly`.

### Relazioni

- `discount_id` → `Discount` (stesso schema): FK reale, `WithMany(d => d.ValidityWindows)`, cascade.

### Note di migrazione

- `discounts.items[].voucherDateRange[]` (`{startDate, endDate}`) → una riga per intervallo.
- Validazione al checkout: `EXISTS (... WHERE discount_id = ? AND ? BETWEEN starts_on AND ends_on)`.
