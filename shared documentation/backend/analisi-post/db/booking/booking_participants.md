# Tabella `booking_participants`

> **Dominio**: C1 — `booking` · **sottoarea**: — (nucleo) · **schema**: `booking` · **nome flat**:
> `booking_participants` · **forma canonica**: `booking.participants`.
>
> Sostituisce [`bookings_requested_details`](../../../analisi-pre/db/bookings_requested_details.md)
> (nome legacy fuorviante: non sono "richieste" ma **concessioni di visibilità**). Fonti:
> [`analisi-pre/db/bookings_requested_details.md`](../../../analisi-pre/db/bookings_requested_details.md),
> [`analisi-pre/business-logic/cliente-finale/04-prenotazione-postazioni.md`](../../../analisi-pre/business-logic/cliente-finale/04-prenotazione-postazioni.md).

---

## Descrizione

**Una riga per (prenotazione × account consumer che l'ha "agganciata" col `reservation_code`)**. Quando
un utente loggato apre il link/QR pubblico della prenotazione, si registra qui (idempotente) che quel
consumer **può vedere** quella prenotazione nella sua area personale — oltre a quelle intestate a lui
(`reservation.customer_id`) o in cui è ospite (`booking_guests`).

Correzioni rispetto al legacy:

- **`findOrCreate` senza vincolo** → `UNIQUE (reservation_id, user_id)`.
- **nessuna FK, `booking_request_by` orfano possibile** → FK interna a `booking_reservations`,
  `user_id` = `public_id` copiato da C5.
- **semantica implicita "la riga esiste = può vedere"** → colonna `role` esplicita per un'eventuale
  distinzione futura (titolare invitato / co-partecipante).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_participants` | `id` | PK | — |
| `uq_..._reservation_user` | `(reservation_id, user_id) WHERE deleted_at IS NULL` | UNIQUE parziale | de-dup della concessione |
| `ix_..._user` | `(user_id) WHERE deleted_at IS NULL` | parziale | "le prenotazioni che posso vedere" (area personale consumer) |

## Campi

### id
- **Legacy**: `bookings_requested_details.id`
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### reservation_id
- **Legacy**: `bookings_requested_details.booking_id` (→ `bookings.id`, cioè la singola riga)
- **Descrizione**: prenotazione (testata) visibile. Nel legacy era per riga `bookings`; qui la
  visibilità è dell'**intera prenotazione**.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE CASCADE`.

### user_id
- **Legacy**: `bookings_requested_details.booking_request_by` (`int`, `users.id`, nessuna FK)
- **Descrizione**: account consumer che ha aperto la prenotazione col codice.
- **Tipo**: `uuid` (= `public_id` di `identity_access_users`).
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_access_users`](../identity/identity_access_users.md).

### role
- **Legacy**: — (implicito)
- **Descrizione**: ruolo del partecipante rispetto alla prenotazione. Oggi un solo valore reale; la
  colonna esiste per rendere esplicita la semantica ed estenderla senza migrazione strutturale.
- **Tipo**: `enum booking.participant_role` — valori: `viewer` (ha aperto col codice, può consultare).
- **Default**: `viewer`.
- **Nullabile**: No.
- **Relazioni**: —.

### linked_at
- **Legacy**: `createdAt`
- **Descrizione**: quando l'aggancio è avvenuto.
- **Tipo**: `timestamptz`.
- **Default**: `now()`.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `createdAt` / `updatedAt` / — (nessun soft-delete)
- **Descrizione**: creazione / modifica / soft-delete (revoca visibilità).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON (la tabella legacy non ne aveva).

## Suggerimenti per le prestazioni

- Tabella piccola e a bassa scrittura. L'indice `(user_id)` serve la query dell'area personale consumer
  (`WHERE customer_id = me OR guest OR participant`).
- `UNIQUE (reservation_id, user_id)` chiude la race del `findOrCreate` legacy (due open simultanei del
  link).

## Implementazione .NET

`BookingParticipant` è **entità figlia** dell'aggregato `Reservation` (modulo `Module.Booking`, schema
`booking`): concessione di visibilità a un account consumer che ha "agganciato" la prenotazione col
`reservation_code`. Creata idempotentemente tramite l'aggregate root
(`Reservation.LinkParticipant(userId)`, no-op se la coppia esiste già viva), mai persistita in
isolamento. Insieme a `Reservation.CustomerId` e `booking_guests.CustomerId`, alimenta la query
cross-modulo "prenotazioni che posso vedere" dell'area personale consumer.

### Enum propri

```csharp
// Module.Booking.Domain.Enums

/// Ruolo del partecipante rispetto alla prenotazione — oggi un solo valore reale, colonna predisposta a crescere.
public enum ParticipantRole
{
	Viewer = 0
}
```

```csharp
// Module.Booking.Infrastructure.DbContext.OnModelCreating
modelBuilder.HasPostgresEnum<ParticipantRole>("booking", "participant_role");
```

```csharp
// Module.Booking Infrastructure DI — registrazione data source Npgsql
dataSourceBuilder.MapEnum<ParticipantRole>("booking.participant_role");
```

### `BookingParticipantConfiguration`

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingParticipantConfiguration : IEntityTypeConfiguration<BookingParticipant>
{
	public void Configure(EntityTypeBuilder<BookingParticipant> builder)
	{
		builder.ToTable("booking_participants", "booking", t => t.HasComment(
			"Concessione di visibilità: account consumer che ha agganciato la prenotazione col reservation_code."));

		builder.HasKey(x => x.Id).HasName("pk_booking_participants");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione (testata) resa visibile al partecipante.");

		builder.Property(x => x.UserId)
			.HasColumnName("user_id")
			.IsRequired()
			.HasComment("public_id di identity.access_users, copiato — account consumer che ha aperto la prenotazione col codice.");

		builder.Property(x => x.Role)
			.HasColumnName("role")
			.HasColumnType("booking.participant_role")
			.IsRequired()
			.HasDefaultValueSql("'viewer'")
			.HasComment("Ruolo del partecipante rispetto alla prenotazione.");

		builder.Property(x => x.LinkedAt)
			.HasColumnName("linked_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Quando l'aggancio è avvenuto.");

		builder.Property(x => x.CreatedAt)
			.HasColumnName("created_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Creazione della riga.");

		builder.Property(x => x.UpdatedAt)
			.HasColumnName("updated_at")
			.IsRequired()
			.HasDefaultValueSql("now()")
			.HasComment("Ultima modifica della riga.");

		builder.Property(x => x.DeletedAt)
			.HasColumnName("deleted_at")
			.HasComment("Soft-delete (revoca della visibilità).");

		builder.HasIndex(x => new { x.ReservationId, x.UserId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_participants_reservation_user");

		builder.HasIndex(x => x.UserId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_participants_user");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.Participants)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_participants_reservation")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`BookingParticipant` implementa `ISoftDeletable` (`deleted_at`) e `IAuditable`
(`created_at`/`updated_at`). `user_id` è `public_id` copiato da C5 (solo `HasComment`, nessuna FK
cross-schema); l'unica FK EF reale è `reservation_id` → `booking_reservations` (`Cascade`, stesso
schema `booking`). L'indice unico parziale `(reservation_id, user_id) WHERE deleted_at IS NULL`
sostituisce il `findOrCreate` senza vincolo del legacy — chiude la race di due open simultanei dello
stesso link.

### Note di migrazione

- `bookings_requested_details` → deduplicata per `(booking_id → reservation_id, booking_request_by →
  user_id)`: il legacy era per riga `bookings` (singola postazione), qui la visibilità è
  sull'**intera prenotazione**, quindi più righe legacy con lo stesso `booking_request_by` sullo stesso
  numero collassano in una sola.
- Righe con `booking_request_by` `NULL`/orfano (nessuna FK nel legacy) → **scartate**.
- `role` non ha equivalente legacy (era implicito "la riga esiste = può vedere") → tutte le righe
  migrate con `role = viewer`.
