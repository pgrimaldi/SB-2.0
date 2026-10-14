# Tabella `catalog_bathhouse_amenities`

> **Dominio**: C2 — `catalog` · **sottoarea**: — (radice dominio; giunzione verso la sottoarea `service`) ·
> **schema**: `catalog` · **nome flat**: `catalog_bathhouse_amenities` · **forma canonica**:
> `catalog.bathhouse_amenities`.
>
> Tabella di **giunzione N:N** che sostituisce la copia integrale JSON `bathouses.services`. Fonti:
> [`analisi-pre/db/bathouses.md` (campo `services`)](../../../analisi-pre/db/bathouses.md),
> [`analisi-pre/db/services.md`](../../../analisi-pre/db/services.md),
> [`docs/03-gestione-dati.md` §2.5, §6](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

Quali **servizi/dotazioni** (`amenity`: Wi-Fi, bar, doccia, …) offre una singola vetrina. Una riga =
(vetrina × amenity selezionata).

Nel legacy `bathouses.services` conteneva una **copia completa di ogni riga** del master `services`
(tutti i campi duplicati), con l'unico dato per-stabilimento nel flag `isChecked` — con la conseguenza
che una modifica al master (nuova amenity, icona corretta) **non** si propagava agli stabilimenti non
ri-salvati. Qui: solo le amenity **selezionate** (`isChecked = true`) diventano righe di questa
giunzione; nome/icona vivono **una volta** su [`catalog_service_amenities`](catalog_service_amenities.md).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_catalog_bathhouse_amenities` | `id` | PK | — |
| `uq_..._bathhouse_amenity` | `(bathhouse_id, amenity_id) WHERE deleted_at IS NULL` | UNIQUE parziale | una amenity per vetrina |
| `ix_..._bathhouse` | `(bathhouse_id) WHERE deleted_at IS NULL` | parziale | "servizi di questa vetrina" |
| `ix_..._amenity` | `(amenity_id) WHERE deleted_at IS NULL` | parziale | filtro ricerca "stabilimenti con Wi-Fi" |

## Campi

### id
- **Legacy**: — (nuovo)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### bathhouse_id
- **Legacy**: `bathouses.id` (contenitore del JSON `services`)
- **Descrizione**: vetrina che offre l'amenity.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_bathhouses.id`](catalog_bathhouses.md) `ON DELETE CASCADE`.

### amenity_id
- **Legacy**: `bathouses.services[].id` (dove `isChecked = true`)
- **Descrizione**: la dotazione offerta.
- **Tipo**: `integer`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`catalog_service_amenities.id`](catalog_service_amenities.md)
  `ON DELETE RESTRICT`.

### created_at / updated_at
- **Legacy**: — (nuovo)
- **Descrizione**: creazione / ultima modifica.
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### deleted_at
- **Legacy**: transizione di `isChecked` da `true` a `false` nel JSON legacy
- **Descrizione**: soft-delete (amenity deselezionata).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Note**: valida anche una scelta di hard-delete (pura giunzione); soft-delete tenuto per uniformità.

## Valutazione JSON

Nessun JSON — è la tabella che *elimina* la copia JSON del master.

## Suggerimenti per le prestazioni

- Per la **ricerca pubblica per servizi** (il legacy filtrava in JavaScript dopo aver caricato tutto):
  materializzare in un **read model** l'insieme delle `amenity_id` per vetrina come array PostgreSQL
  (`smallint[]` con indice GIN) o `tsvector`, aggiornato a eventi — la giunzione normalizzata resta la
  fonte di verità, il read model serve le query di filtro.
- Volume ridotto: ~180 vetrine × ≤48 amenity.

## Implementazione .NET

`BathhouseAmenity` è un'**entità figlia** di `Bathhouse` (giunzione N:N verso `ServiceAmenity`, entità
descritta in [`catalog_service_amenities.md`](catalog_service_amenities.md), fuori da questo batch):
`sealed class`, nessun evento proprio, esposta come collezione `Bathhouse.Amenities`. Modulo
`Module.Catalog`, schema `catalog`, tabella `bathhouse_amenities`. Nessun enum proprio.

### `BathhouseAmenityConfiguration`

```csharp
namespace Module.Catalog.Infrastructure.Configurations;

public sealed class BathhouseAmenityConfiguration : IEntityTypeConfiguration<BathhouseAmenity>
{
	public void Configure(EntityTypeBuilder<BathhouseAmenity> builder)
	{
		builder.ToTable("bathhouse_amenities", "catalog", t => t.HasComment(
			"Giunzione N:N: quali dotazioni/servizi (amenity) offre una vetrina. Sostituisce la copia JSON integrale bathouses.services."));

		builder.HasKey(x => x.Id).HasName("pk_catalog_bathhouse_amenities");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.BathhouseId)
			.HasColumnName("bathhouse_id")
			.IsRequired()
			.HasComment("Vetrina che offre l'amenity.");

		builder.Property(x => x.AmenityId)
			.HasColumnName("amenity_id")
			.IsRequired()
			.HasComment("La dotazione/servizio offerto (catalog.service_amenities).");

		builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired().HasDefaultValueSql("now()").HasComment("Creazione della riga (selezione dell'amenity).");
		builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired().HasDefaultValueSql("now()").HasComment("Ultima modifica della riga.");
		builder.Property(x => x.DeletedAt).HasColumnName("deleted_at").HasComment("Soft-delete: amenity deselezionata dalla vetrina.");

		builder.HasIndex(x => new { x.BathhouseId, x.AmenityId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_catalog_bathhouse_amenities_bathhouse_amenity");

		builder.HasIndex(x => x.BathhouseId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_bathhouse_amenities_bathhouse");

		builder.HasIndex(x => x.AmenityId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_catalog_bathhouse_amenities_amenity");

		builder.HasOne(x => x.Bathhouse)
			.WithMany(b => b.Amenities)
			.HasForeignKey(x => x.BathhouseId)
			.HasConstraintName("fk_catalog_bathhouse_amenities_bathhouse")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Amenity)
			.WithMany()
			.HasForeignKey(x => x.AmenityId)
			.HasConstraintName("fk_catalog_bathhouse_amenities_amenity")
			.OnDelete(DeleteBehavior.Restrict);
	}
}
```

`BathhouseAmenity` implementa `IAuditable`/`ISoftDeletable` per uniformità, pur essendo una pura giunzione
(soft-delete valutato più per coerenza che per necessità reale — vedi Note della scheda).

### Relazioni

- `BathhouseId` → `Bathhouse` (stesso schema): FK reale, `ON DELETE CASCADE`.
- `AmenityId` → `ServiceAmenity` (stesso schema, configurazione in
  [`catalog_service_amenities.md`](catalog_service_amenities.md)): FK reale, `ON DELETE RESTRICT`.

### Note di migrazione

- Dal JSON legacy `bathouses.services[]` prendere **solo** gli oggetti con `isChecked = true`; il resto
  dell'oggetto (nome, icona duplicati) si scarta — vive ora su `ServiceAmenity`.
- `Bathhouse.SetAmenities(ids)` (comando idempotente, diff con lo stato corrente via soft-delete/insert)
  sostituisce il pattern legacy "riscrivi l'intero array JSON".
- Una proiezione (`amenity_id[]` denormalizzato) alimenta il read model di ricerca per servizi — non
  sostituisce questa tabella come fonte di verità.
