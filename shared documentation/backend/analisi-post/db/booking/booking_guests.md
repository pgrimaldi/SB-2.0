# Tabella `booking_guests`

> **Dominio**: C1 — `booking` · **sottoarea**: — (nucleo) · **schema**: `booking` · **nome flat**:
> `booking_guests` · **forma canonica**: `booking.guests`.
>
> Sostituisce i JSON `bookings.guest_data[]` / `bookings.guest_ids[]` e
> `service_reservation.guest_data[]` / `guest_ids[]`. Fonti:
> [`analisi-pre/db/bookings.md`](../../../analisi-pre/db/bookings.md),
> [`analisi-pre/db/service_reservation.md`](../../../analisi-pre/db/service_reservation.md),
> [`docs/03-gestione-dati.md` §3.6](../../../progettazione/03-gestione-dati.md).

---

## Descrizione

**Una riga per ospite di una prenotazione**: persona che occupa la postazione insieme all'intestatario
ma **non** è il titolare (nessun addebito/comunicazione a suo carico). Serve per l'elenco presenze, il
check-in per nome, le statistiche, e per far comparire la prenotazione nell'area personale degli account
consumer collegati a quegli ospiti.

Nel legacy ogni ospite generava comunque un record `customers` (creato al volo) e i suoi id venivano
duplicati in due array (`guest_data` con l'anagrafica, `guest_ids` con i soli id per le ricerche
`JSON_CONTAINS`). Qui: **una FK viva** a `identity_customer_customers` + un piccolo snapshot del nome
per la resa in agenda anche se l'anagrafica cambia.

Correzioni rispetto al legacy:

- **`guest_data[]` (JSON anagrafica) + `guest_ids[]` (JSON id)** → 1 tabella, FK `customer_id` + snapshot
  minimale.
- **chiavi disallineate** (`bookings`: `name`; `service_reservation`: `firstName`) → un solo
  `full_name_snapshot`.
- **ricerca `JSON_CONTAINS(guest_ids, ...)`** → indice su `customer_id`.

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_guests` | `id` | PK | — |
| `uq_..._reservation_customer` | `(reservation_id, customer_id) WHERE deleted_at IS NULL` | UNIQUE parziale | un ospite una volta per prenotazione |
| `ix_..._customer` | `(customer_id) WHERE deleted_at IS NULL` | parziale | "le prenotazioni in cui compaio come ospite" |

## Campi

### id
- **Legacy**: — (elemento JSON)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### reservation_id
- **Legacy**: `bookings.bookingNumber`
- **Descrizione**: prenotazione a cui l'ospite appartiene.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE CASCADE`.

### customer_id
- **Legacy**: `guest_data[].id` / `guest_ids[]`
- **Descrizione**: anagrafica cliente dell'ospite (C5).
- **Tipo**: `uuid` (= `public_id` di `identity_customer_customers`).
- **Default**: nessuno.
- **Nullabile**: Sì (ospite anonimo senza record cliente — nel legacy il record veniva creato sempre;
  qui si ammette il caso "solo nome").
- **Relazioni**: **`public_id` (uuid) copiato** da C5 [`identity_customer_customers`](../identity/identity_customer_customers.md) — riferimento vivo.

### full_name_snapshot
- **Legacy**: `guest_data[].name` / `.firstName`
- **Descrizione**: nome dell'ospite **congelato** (elenco presenze / check-in anche se l'anagrafica poi
  cambia).
- **Tipo**: `varchar(160)`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: snapshot; i contatti (email/telefono) si leggono via JOIN a `customer_id` (dato vivo),
  non si copiano.

### booking_line_id
- **Legacy**: — (nel legacy l'ospite non era legato a una postazione)
- **Descrizione**: postazione a cui l'ospite è associato (per prenotazioni multi-postazione). `NULL` =
  ospite della prenotazione nel complesso.
- **Tipo**: `bigint`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: **FK** → [`booking_lines.id`](booking_lines.md) `ON DELETE SET NULL`.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 133 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### checked_in_at
- **Legacy**: — (il check-in legacy era per prenotazione, non per ospite)
- **Descrizione**: check-in per nome dell'ospite (opzionale).
- **Tipo**: `timestamptz`.
- **Default**: `NULL`.
- **Nullabile**: Sì.
- **Relazioni**: —.
- **Stato dopo la migrazione (2026-10-01)**: **interamente NULL** sulle 133 righe storiche: **nessuna mapping spec tocca questa colonna**. Per lo storico migrato resta vuota e la popola il nuovo backend; se la riga «Legacy» qui sopra indica una fonte, vale la pena verificare perché non sia stata mappata. Censita fra le colonne vuote di `analisi-pre/db-no-copied/_colonne-vuote.md` (Parte 1); la voce M13 che ha prodotto questa nota e' nella Parte C di `analisi-pre/db-no-copied/_mancanti-da-sistemare.md`.

### created_at / updated_at / deleted_at
- **Legacy**: della riga contenitore
- **Descrizione**: creazione / modifica / soft-delete (ospite rimosso dalla prenotazione).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. `guest_data[]` + `guest_ids[]` → righe. L'anagrafica dell'ospite **non** si duplica: FK a
C5. Solo il nome è snapshot (fatto storico "chi era presente").

## Suggerimenti per le prestazioni

- Righe pochissime per prenotazione. L'indice `(customer_id)` sostituisce la scansione
  `JSON_CONTAINS(guest_ids, '<id>')` che il legacy faceva su ogni ricerca "le mie prenotazioni".

## Implementazione .NET

`BookingGuest` è **entità figlia** dell'aggregato `Reservation` (modulo `Module.Booking`, schema
`booking`): persona che occupa la postazione insieme all'intestatario ma non è il titolare (nessun
addebito/comunicazione a suo carico). Sempre creata/modificata tramite l'aggregate root
(`Reservation.AddGuest(...)`, `Reservation.RemoveGuest(...)`), mai persistita in isolamento. Alimenta,
insieme a `booking_participants`, la query cross-modulo "prenotazioni in cui compaio" per l'area
personale del cliente finale (unione di `Reservation.CustomerId`, `BookingGuest.CustomerId` e
`BookingParticipant.CustomerId`).

Nessun enum proprio introdotto da questa tabella.

### `BookingGuestConfiguration`

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingGuestConfiguration : IEntityTypeConfiguration<BookingGuest>
{
	public void Configure(EntityTypeBuilder<BookingGuest> builder)
	{
		builder.ToTable("booking_guests", "booking", t => t.HasComment(
			"Ospite di una prenotazione: persona presente ma non intestataria, senza addebito/comunicazioni a suo carico."));

		builder.HasKey(x => x.Id).HasName("pk_booking_guests");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui l'ospite appartiene.");

		builder.Property(x => x.CustomerId)
			.HasColumnName("customer_id")
			.HasComment("public_id di identity.customer_customers, copiato — anagrafica cliente dell'ospite; NULL per ospite senza record cliente (solo nome).");

		builder.Property(x => x.FullNameSnapshot)
			.HasColumnName("full_name_snapshot")
			.HasMaxLength(160)
			.IsRequired()
			.HasComment("Nome dell'ospite congelato, per elenco presenze/check-in anche se l'anagrafica cambia in seguito.");

		builder.Property(x => x.BookingLineId)
			.HasColumnName("booking_line_id")
			.HasComment("Postazione a cui l'ospite è associato (prenotazioni multi-postazione); NULL = ospite della prenotazione nel complesso.");

		builder.Property(x => x.CheckedInAt)
			.HasColumnName("checked_in_at")
			.HasComment("Check-in per nome dell'ospite, opzionale.");

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
			.HasComment("Soft-delete (ospite rimosso dalla prenotazione).");

		builder.HasIndex(x => new { x.ReservationId, x.CustomerId })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_guests_reservation_customer");

		builder.HasIndex(x => x.CustomerId)
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_guests_customer");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.Guests)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_guests_reservation")
			.OnDelete(DeleteBehavior.Cascade);

		builder.HasOne(x => x.BookingLine)
			.WithMany()
			.HasForeignKey(x => x.BookingLineId)
			.HasConstraintName("fk_booking_guests_line")
			.OnDelete(DeleteBehavior.SetNull);
	}
}
```

`BookingGuest` implementa `ISoftDeletable` (`deleted_at`) e `IAuditable`
(`created_at`/`updated_at`). `customer_id` è `public_id` copiato da C5 (solo `HasComment`, nessuna FK
cross-schema, riferimento vivo — i contatti si leggono via JOIN applicativo, non si copiano); le FK EF
reali sono `reservation_id` → `booking_reservations` (`Cascade`) e `booking_line_id` (nullable) →
`booking_lines` (`SetNull`, stesso schema `booking`).

### Note di migrazione

- `guest_data[]` (anagrafica) + `guest_ids[]` (soli id, per `JSON_CONTAINS`) → **una riga per ospite**,
  con `customer_id` risolto incrociando i due array (id da `guest_ids[]`, nome da `guest_data[]`).
- Chiavi disallineate fra le due tabelle legacy (`bookings.guest_data[].name` vs
  `service_reservation.guest_data[].firstName`) → un solo `full_name_snapshot`.
- Nel legacy **ogni ospite generava comunque un record `customers`** anche per una presenza
  occasionale: in migrazione si valuta caso per caso se preservare quei record cliente "leggeri" o
  ammettere `customer_id = NULL` con solo `full_name_snapshot` per gli ospiti senza reale necessità di
  anagrafica (nuova possibilità introdotta da questo schema, non presente nel legacy).
- Ricerca `JSON_CONTAINS(guest_ids, '<id>')` → sostituita dall'indice su `customer_id`.
