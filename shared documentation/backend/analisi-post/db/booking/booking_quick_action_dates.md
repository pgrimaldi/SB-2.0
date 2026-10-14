# Tabella `booking_quick_action_dates`

> **Dominio**: C1 — `booking` · **sottoarea**: — (nucleo) · **schema**: `booking` · **nome flat**:
> `booking_quick_action_dates` · **forma canonica**: `booking.quick_action_dates`.
>
> Sostituisce `booking_number_informations.quick_actions_data` (array JSON di date, formato legacy
> `DD-MM-YYYY`). Fonti:
> [`analisi-pre/db/booking_number_informations.md`](../../../analisi-pre/db/booking_number_informations.md),
> [`analisi-pre/business-logic/stabilimento/02-planner.md`](../../../analisi-pre/business-logic/stabilimento/02-planner.md).

---

## Descrizione

**Una riga per (prenotazione × data)** marcata dall'operatore per le "azioni rapide" del planner (giorni
su cui eseguire operazioni veloci — check-in di gruppo, ecc.). Nel legacy era un array uguale per tutte
le righe della prenotazione dentro `booking_number_informations`; qui è una figlia della testata.

Correzioni rispetto al legacy:

- **array JSON `["19-08-2026", …]` (`DD-MM-YYYY`)** → righe con `action_date date`.
- **`booking_number_informations` "sacco" multi-sorgente** → questa è **l'unico pezzo di C1** di quella
  tabella (il resto: `payments*` → testata C1; `details_payments*` → C4; `ofsale_payments` /
  `teamsystem_payment` → C3 — [`docs/03-gestione-dati.md` §4](../../../progettazione/03-gestione-dati.md)).

## Indici

| Indice | Colonne | Tipo | Motivo |
|---|---|---|---|
| `pk_booking_quick_action_dates` | `id` | PK | — |
| `uq_..._reservation_date` | `(reservation_id, action_date) WHERE deleted_at IS NULL` | UNIQUE parziale | una data una volta per prenotazione |
| `ix_..._property_date` | `(property_id, action_date) WHERE deleted_at IS NULL` | parziale | "azioni rapide del {giorno}" nel planner |

## Campi

### id
- **Legacy**: — (elemento JSON)
- **Descrizione**: PK tecnica.
- **Tipo**: `bigint`, `GENERATED ALWAYS AS IDENTITY`.
- **Default**: identità.
- **Nullabile**: No.
- **Relazioni**: PK.

### reservation_id
- **Legacy**: `booking_number_informations.bookingNumber`
- **Descrizione**: prenotazione a cui le date sono associate.
- **Tipo**: `bigint`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: **FK** → [`booking_reservations.id`](booking_reservations.md) `ON DELETE CASCADE`.

### property_id
- **Legacy**: derivato
- **Descrizione**: stabilimento (denormalizzato per l'indice del planner).
- **Tipo**: `uuid` (= `public_id` di `catalog_properties`).
- **Default**: dalla testata.
- **Nullabile**: No.
- **Relazioni**: **`public_id` copiato** da C2.

### action_date
- **Legacy**: elemento dell'array (`"DD-MM-YYYY"`)
- **Descrizione**: giorno marcato per un'azione rapida.
- **Tipo**: `date`.
- **Default**: nessuno.
- **Nullabile**: No.
- **Relazioni**: —.

### created_at / updated_at / deleted_at
- **Legacy**: `booking_number_informations.createdAt` / `updatedAt` / —
- **Descrizione**: creazione / modifica / soft-delete (data smarcata).
- **Tipo**: `timestamptz` ciascuno.
- **Default**: `now()` / `now()` / `NULL`.
- **Nullabile**: No / No / Sì.
- **Relazioni**: Global Query Filter EF su `deleted_at`.

## Valutazione JSON

Nessun JSON. Array di date → righe. È l'unico contenuto di
`booking_number_informations` che resta in C1.

## Suggerimenti per le prestazioni

- Tabella minuscola. L'indice `(property_id, action_date)` serve il rendering del planner per giorno.

## Implementazione .NET

`BookingQuickActionDate` è **entità figlia** dell'aggregato `Reservation` (modulo `Module.Booking`,
schema `booking`): un giorno marcato dall'operatore per le "azioni rapide" del planner (es. check-in
di gruppo). Sempre creata/rimossa tramite l'aggregate root (`Reservation.MarkQuickActionDate(date)`,
`Reservation.UnmarkQuickActionDate(date)`), mai persistita in isolamento — è l'unico frammento di
`booking_number_informations` che resta in C1 (il resto della tabella legacy si è già smistato su
testata C1/C4/C3, vedi Descrizione).

Nessun enum proprio introdotto da questa tabella.

### `BookingQuickActionDateConfiguration`

```csharp
namespace Module.Booking.Infrastructure.Configurations;

public sealed class BookingQuickActionDateConfiguration : IEntityTypeConfiguration<BookingQuickActionDate>
{
	public void Configure(EntityTypeBuilder<BookingQuickActionDate> builder)
	{
		builder.ToTable("booking_quick_action_dates", "booking", t => t.HasComment(
			"Giorno marcato dall'operatore per un'azione rapida del planner su questa prenotazione."));

		builder.HasKey(x => x.Id).HasName("pk_booking_quick_action_dates");
		builder.Property(x => x.Id).UseIdentityAlwaysColumn();

		builder.Property(x => x.ReservationId)
			.HasColumnName("reservation_id")
			.IsRequired()
			.HasComment("Prenotazione a cui la data marcata è associata.");

		builder.Property(x => x.PropertyId)
			.HasColumnName("property_id")
			.IsRequired()
			.HasComment("public_id di catalog.properties, copiato dalla testata — nessuna FK cross-schema.");

		builder.Property(x => x.ActionDate)
			.HasColumnName("action_date")
			.HasColumnType("date")
			.IsRequired()
			.HasComment("Giorno marcato per un'azione rapida del planner.");

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
			.HasComment("Soft-delete (data smarcata).");

		builder.HasIndex(x => new { x.ReservationId, x.ActionDate })
			.IsUnique()
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("uq_booking_quick_action_dates_reservation_date");

		builder.HasIndex(x => new { x.PropertyId, x.ActionDate })
			.HasFilter("deleted_at IS NULL")
			.HasDatabaseName("ix_booking_quick_action_dates_property_date");

		builder.HasOne(x => x.Reservation)
			.WithMany(r => r.QuickActionDates)
			.HasForeignKey(x => x.ReservationId)
			.HasConstraintName("fk_booking_quick_action_dates_reservation")
			.OnDelete(DeleteBehavior.Cascade);
	}
}
```

`BookingQuickActionDate` implementa `ISoftDeletable` (`deleted_at`) e `IAuditable`
(`created_at`/`updated_at`). `property_id` è `public_id` copiato da C2 (solo `HasComment`, nessuna FK
cross-schema); l'unica FK EF reale è `reservation_id` → `booking_reservations` (`Cascade`, stesso
schema `booking`).

### Note di migrazione

- `booking_number_informations.quick_actions_data` (array JSON di stringhe `DD-MM-YYYY`) → parse in
  `date` reale e **una riga per elemento dell'array**.
- Deduplica per `(bookingNumber → reservation_id, date)` prima dell'insert (l'unique parziale
  `(reservation_id, action_date)` non ammetterebbe duplicati).
- Array `NULL`/vuoti nel legacy → **nessuna riga generata** (non un errore di migrazione).
