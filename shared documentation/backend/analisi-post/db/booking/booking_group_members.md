# Tabella `booking_group_members`

> **Dominio**: C1 — `booking` · **sottoarea**: `group` · **schema**: `booking` · **nome flat**:
> `booking_group_members` · **forma canonica**: `booking.group_members`.
>
> Sostituisce l'array JSON `booking_groups.groupBookings[]` (`bookings.id`). Fonti:
> [`analisi-pre/db/booking_groups.md`](../../../analisi-pre/db/booking_groups.md).

---

## Descrizione

**Giunzione (gruppo × prenotazione)**. Una prenotazione sta in **al più un gruppo** (validato in
applicazione nel legacy, qui garantito da `UNIQUE (reservation_id)` parziale).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_group_members` | `id` | PK | — |
| `uq_..._reservation` | `(reservation_id) WHERE deleted_at IS NULL` | UNIQUE parziale | una prenotazione in un solo gruppo (era una scansione O(N·M) in memoria) |
| `ix_..._group` | `(group_id) WHERE deleted_at IS NULL` | parziale | membri di un gruppo |

## Campi

### id
- **Legacy**: — (elemento di array)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### group_id
- **Legacy**: `booking_groups.id`
- **Descrizione**: gruppo di appartenenza.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_groups.id`](booking_groups.md) `ON DELETE CASCADE`.

### reservation_id
- **Legacy**: elemento di `groupBookings[]` (`bookings.id`)
- **Descrizione**: prenotazione membro del gruppo. Nel legacy erano `bookings.id` (una riga postazione);
  qui è la **prenotazione** intera.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE CASCADE`.

### property_id
- **Legacy**: `booking_groups.property_id`
- **Descrizione**: stabilimento (denormalizzato; coerente sia col gruppo sia con la prenotazione).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dal gruppo.
- **Nullabile**: No.
- **Relazioni**: **`public_id` copiato** da C2.

### added_at
- **Legacy**: `booking_groups.updatedAt` (upsert dell'intero array)
- **Descrizione**: quando la prenotazione è stata aggiunta al gruppo.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: — (elemento JSON)
- **Descrizione**: creazione / modifica / soft-delete (membro rimosso dal gruppo).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON — l'array `groupBookings` diventa righe.

## Suggerimenti per le prestazioni

- `UNIQUE (reservation_id)` parziale: la verifica "una prenotazione in un solo gruppo" è un vincolo DB,
  non una scansione di **tutti** i gruppi di **tutte** le property come nel legacy.
- Se una prenotazione viene cancellata, la FK `ON DELETE CASCADE` rimuove il membro (nel legacy restava
  un id orfano nell'array).

## Implementazione .NET

`BookingGroupMember` è l'**entità figlia** di `BookingGroup` (aggregate root, vedi
[`booking_groups.md`](booking_groups.md)) nel modulo `Module.Booking`, schema `booking`. Ha PK e ciclo di
vita propri (non è un owned type puro: si aggiunge/rimuove un membro indipendentemente dal resto del
gruppo), quindi mappata come entità normale raggiungibile solo tramite la navigazione `BookingGroup.Members`
(nessun `DbSet<BookingGroupMember>` pubblico nel modulo). Implementa `ISoftDeletable`/`IAuditable`. Nessun
enum proprio.

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingGroupMemberConfiguration : IEntityTypeConfiguration<BookingGroupMember>
{
	public void Configure(EntityTypeBuilder<BookingGroupMember> builder)
	{
		builder.ToTable("booking_group_members", "booking", t => t.HasComment(
			"Giunzione gruppo × prenotazione: una prenotazione appartiene ad al più un gruppo."));

		builder.HasKey(x => x.Id).HasName("pk_booking_group_members");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.GroupId)
			.HasColumnName("group_id")
			.IsRequired()
			.HasComment("Gruppo di appartenenza");

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione membro del gruppo (l'intera prenotazione, non una singola riga postazione come nel legacy)");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, denormalizzato dal gruppo — nessuna FK cross-schema");

		builder.Property(x => x.AddedAt)
			.HasColumnName("added_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora in cui la prenotazione è stata aggiunta al gruppo");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di creazione della riga di giunzione");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.HasDefaultValueSql("now()")
			.IsRequired()
			.HasComment("Data/ora di ultima modifica della riga di giunzione");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete: prenotazione rimossa dal gruppo");

		builder.HasIndex(x => x.ReservationId)
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_group_members_reservation");

		builder.HasIndex(x => x.GroupId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_group_members_group");

		builder.HasOne(x => x.Group)
			.WithMany(g => g.Members)
			.HasForeignKey(x => x.GroupId)
			.HasConstraintName("fk_booking_group_members_group")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.Reservation)
			.WithMany()
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_group_members_reservation")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

**Note di migrazione**:

- Esplosione dell'array JSON `booking_groups.groupBookings[]`: ogni elemento (`bookings.id` legacy, una
  riga postazione) va risolto alla **prenotazione** che la contiene (`reservation_id` nuovo, non 1:1 con
  l'id legacy) — vedi mapping già definito per `booking_reservations`.
- Id orfani (prenotazione non trovata) o già presenti in un altro gruppo migrato (violazione della futura
  `UNIQUE (reservation_id)`) → scartati e loggati, non bloccanti per l'intera migrazione del gruppo.
- `added_at` non esisteva come campo singolo in legacy: valorizzato con `booking_groups.updatedAt` al
  momento della migrazione (approssimazione, l'array veniva riscritto per intero ad ogni upsert).
